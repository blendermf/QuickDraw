//
//  WebView.swift
//  QuickDraw
//
//  Created by Matthew Fraser on 2022-02-28.
//

import SwiftUI
import WebKit
import UniformTypeIdentifiers

extension Dictionary {
    func toJSONString() -> String {
        let jsonObject = try? JSONSerialization.data(withJSONObject: self, options: [])
        let json = String(data: jsonObject!, encoding: .utf8)!
        return json
        
    }
}

extension URL {
    func modifyScheme(_ scheme: String) -> URL {
        var components = URLComponents.init(url: self, resolvingAgainstBaseURL: false)
        components?.scheme = scheme
        return (components?.url)!
    }
    
    var mimeType: String? {
        guard let type = UTType(filenameExtension: self.pathExtension) else {
            return nil
        }
        return type.preferredMIMEType
    }
}

func GetFolderImages(_ url: URL) -> [URL]
{
    let fileManager = FileManager.init()
    let enumerator = fileManager.enumerator(at: url,
                                                    includingPropertiesForKeys: [.canonicalPathKey],
                                                    options: [.skipsPackageDescendants, .skipsHiddenFiles],
                                            errorHandler: {(url: URL, error: Error) -> Bool in
        print(url)
        debugPrint(error)
        return false
    })!
    
    let fileURLs = (enumerator.allObjects as! [URL]).filter{!$0.hasDirectoryPath && (
        $0.pathExtension.caseInsensitiveCompare("jpg") == .orderedSame ||
        $0.pathExtension.caseInsensitiveCompare("jpeg") == .orderedSame ||
        $0.pathExtension.caseInsensitiveCompare("png") == .orderedSame
    )}
    
    return fileURLs
}

struct QuickDrawWebView: NSViewRepresentable {
    var userContentController = WKUserContentController()
    
    typealias NSViewType = WKWebView
    
    func makeCoordinator() -> QuickDrawWebView.Coordinator {
        Coordinator()
    }
    
    func makeNSView(context: Context) -> WKWebView {
        let coordinator = makeCoordinator()
        
        let initScriptSource = #"window.QuickDrawMacOs = true"#
        let initScript = WKUserScript(source: initScriptSource, injectionTime: .atDocumentStart, forMainFrameOnly: true)
        
        userContentController.add(coordinator, name: "bridge")
        userContentController.addUserScript(initScript)
        
        let configuration = WKWebViewConfiguration()
        configuration.userContentController = userContentController
        configuration.setURLSchemeHandler(coordinator, forURLScheme: "qd")
        configuration.preferences.setValue(true, forKey: "developerExtrasEnabled")
        configuration.setValue(true, forKey: "_allowUniversalAccessFromFileURLs")
        CFPreferencesSetValue("cv_note" as CFString, 0 as CFPropertyList, "com.apple.corevideo" as CFString, kCFPreferencesCurrentUser, kCFPreferencesAnyHost)
        CFPreferencesSynchronize("com.apple.corevideo" as CFString, kCFPreferencesCurrentUser, kCFPreferencesAnyHost)
        let webView = WKWebView(frame: .zero, configuration: configuration)
        webView.navigationDelegate = coordinator
        
        
        let indexURL = Bundle.main.url(forResource: "index", withExtension: "html", subdirectory: "WebSrc")!
        webView.loadFileURL(indexURL, allowingReadAccessTo: indexURL)
        
        return webView
    }
    
    func updateNSView(_ webView: WKWebView, context: Context) { }
    
    class Coordinator: NSObject, WKNavigationDelegate, WKScriptMessageHandler, WKURLSchemeHandler {
        func webView(_ webView: WKWebView, start urlSchemeTask: WKURLSchemeTask) {
            guard let url = urlSchemeTask.request.url?.modifyScheme("file"),
                  let mimeType = url.mimeType,
                  let data = try? Data(contentsOf: url) else { return }
            
            let response = HTTPURLResponse(url: url,
                                           mimeType: mimeType,
                                           expectedContentLength: data.count, textEncodingName: nil)

            urlSchemeTask.didReceive(response)
            urlSchemeTask.didReceive(data)
            urlSchemeTask.didFinish()
        }
        
        func webView(_ webView: WKWebView, stop urlSchemeTask: WKURLSchemeTask) {
            
        }
        
        var webView: WKWebView?
        
        func webView(_ webView: WKWebView, didStartProvisionalNavigation navigation: WKNavigation!) {
            self.webView = webView
        }
        
        func OpenFolders() {
            let dialog = NSOpenPanel()

            dialog.title                    = "Choose Directories"
            dialog.showsResizeIndicator     = true
            dialog.showsHiddenFiles         = false
            dialog.allowsMultipleSelection  = true
            dialog.canChooseFiles           = false
            dialog.canChooseDirectories     = true
            dialog.begin{ [self](result) -> Void in
                if result == .OK {
                    let urls = dialog.urls
                    Task {
                        let folders: [[String:Any]] = await withTaskGroup(of: [String:Any].self) { group in
                            var folders: [[String:Any]] = []
                             
                            for url in urls {
                                group.addTask {
                                    let images = GetFolderImages(url)
                                    return ["Path": url.path, "Count": images.count]
                                }
                            }
                            
                            for await imageFolder in group {
                                folders.append(imageFolder)
                            }
                            
                            return folders
                        }
                        
                        if (urls.count > 0) {
                            await WebViewUpdateFolders(folders)
                        }
                    }
                }
            }
        }
        
        func WebViewUpdateFolders(_ folders: [[String : Any]]) async {

            await MainActor.run {
                PostMessage("UpdateFolders", data: folders)
            }
        }
        
        func PostMessage(_ type: String, data: Any) {
            let dict: [String : Any] = [
                "type": type,
                "data": data
            ]
            let json = dict.toJSONString()
            webView?.evaluateJavaScript("window.dispatchEvent(new CustomEvent('qd-message', {detail: \(json)}));")
        }
        
        func RefreshFolderCount(_ folder: String) {
            let url = URL(fileURLWithPath: folder)

            Task {
                let images = GetFolderImages(url)
                let folder = ["Path": url.path, "Count": images.count] as [String : Any]
                
                if (images.count > 0) {
                    await WebViewUpdateFolders([folder])
                }
            }
        }
        
        func RefreshAllFolderCounts(_ paths: [String]) {
            Task {
                let urls = paths.map{URL(fileURLWithPath: $0)}
                
                let folders: [[String:Any]] = await withTaskGroup(of: [String:Any].self) { group in
                    var folders: [[String:Any]] = []
                    
                    for url in urls {
                        group.addTask {
                            let images = GetFolderImages(url)
                            return ["Path": url.path, "Count": images.count]
                        }
                    }
                    
                    for await imageFolder in group {
                        folders.append(imageFolder)
                    }
                    
                    return folders
                }
                
                if (urls.count > 0) {
                    await WebViewUpdateFolders(folders)
                }
            }
        }
        
        func OpenFolderInFinder(_ folder: String) {
            let url = URL(fileURLWithPath: folder)
            NSWorkspace.shared.selectFile(nil, inFileViewerRootedAtPath: url.path)
        }
        
        func GetImages(_ paths: [String], interval: Int) {
            Task {
                let images: [String] = await withTaskGroup(of: Set<String>.self) { group in
                    var images: Set<String> = []
                    
                    let urls = paths.map{ URL(fileURLWithPath: $0) }
                    for url in urls {
                        
                        group.addTask {
                            let images = GetFolderImages(url)
                            let mimeType = images[0].mimeType
                            let data = try! Data(contentsOf: images[0])
                            return Set<String>(images.map{ $0.modifyScheme("qd").absoluteString })
                        }
                    }
                    
                    for await imageSet in group {
                        images.formUnion(imageSet)
                    }
                    
                    return [String](images)
                }
                
                if (images.count > 0) {
                    let dict: [String : Any] = [
                        "interval": interval * 1000,
                        "images": images
                    ]
                    let json = dict.toJSONString()
                    
                    let initScriptSource = "var slideshowData = \(json);"
                    
                    await MainActor.run {
                        let initScript = WKUserScript(source: initScriptSource, injectionTime: .atDocumentStart, forMainFrameOnly: true)
                        let userContentController = webView?.configuration.userContentController
                        userContentController?.addUserScript(initScript)
                    }
                    
                    let slideshowURL = Bundle.main.url(forResource: "slideshow", withExtension: "html", subdirectory: "WebSrc")!
                    await webView?.loadFileURL(slideshowURL, allowingReadAccessTo: slideshowURL)
                        
                    
                }
            }
        }
        
        func OpenImageInFinder(_ image: String) {
            let url = URL(fileURLWithPath: image)
            let files = [url]
            
            NSWorkspace.shared.activateFileViewerSelecting(files)
        }
        
        func StopSlideshow() {
            let indexURL = Bundle.main.url(forResource: "index", withExtension: "html", subdirectory: "WebSrc")!
            webView?.loadFileURL(indexURL, allowingReadAccessTo: indexURL)
        }
        
        func userContentController(_ userContentController: WKUserContentController, didReceive message: WKScriptMessage) {
            let messageBody = message.body as! Dictionary<String,Any>
            switch messageBody["type"] as! String
            {
                case "addFolders":
                    OpenFolders()
                case "refreshFolder":
                    RefreshFolderCount(messageBody["path"] as! String)
                case "refreshFolders":
                    RefreshAllFolderCounts(messageBody["paths"] as! [String])
                case "openFolder":
                    OpenFolderInFinder(messageBody["path"] as! String)
                case "getImages":
                    GetImages(messageBody["paths"] as! [String], interval: messageBody["interval"] as! Int)
                case "openImage":
                    OpenImageInFinder(messageBody["path"] as! String)
                case "stopSlideshow":
                    StopSlideshow()
                default:
                    break
            }
        }
    }
    
}
