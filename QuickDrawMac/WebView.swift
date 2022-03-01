//
//  WebView.swift
//  QuickDraw
//
//  Created by Matthew Fraser on 2022-02-28.
//

import SwiftUI
import WebKit

struct WebView: NSViewRepresentable {
    var userContentController = WKUserContentController()
    
    typealias NSViewType = WKWebView
    var url: URL
    
    func makeCoordinator() -> WebView.Coordinator {
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

        let wkWebView = WKWebView(frame: .zero, configuration: configuration)
        wkWebView.navigationDelegate = coordinator
        
        return wkWebView
    }
    
    func updateNSView(_ webView: WKWebView, context: Context) {
        webView.loadFileURL(url, allowingReadAccessTo: url)
    }
    
    class Coordinator: NSObject, WKNavigationDelegate, WKScriptMessageHandler {
        var webView: WKWebView?
        
        func webView(_ webView: WKWebView, didStartProvisionalNavigation navigation: WKNavigation!) {
            self.webView = webView
        }
        
        func userContentController(_ userContentController: WKUserContentController, didReceive message: WKScriptMessage) {
            print(message.body)
        }
    }
    
}
