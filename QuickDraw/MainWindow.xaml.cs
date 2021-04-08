using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using FolderDialog;
using System.Runtime.InteropServices;

namespace QuickDraw
{
    public class Message
    {
        public string type { get; set; }
    }

    public class OpenFolderMessage : Message
    {
        public string path { get; set; }
    }

    public class GetImagesMessage : Message
    {
        public List<string> folders { get; set; }
    }

    public struct ImageFolder
    {
        public string Path { get; set; }

        public int Count { get; set; }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<string> folderMappings = new List<string>();

        private void WebViewAddFolders(List<ImageFolder> folders)
        {
            string jsonString = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                { "type", "AddFolders" },
                { "data", folders }
            });

            webView.CoreWebView2.PostWebMessageAsJson(jsonString);
        }

        public MainWindow()
        {
            InitializeComponent();

            InitializeAsync();

        }

        private async void InitializeAsync()
        {
            await webView.EnsureCoreWebView2Async(null);
            webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "quickdraw.invalid", "WebSrc",
                Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow
            );

            webView.CoreWebView2.WebMessageReceived += ReceiveMessage;
        }

        private void OpenFolderInExplorer(string path)
        {
            if (Directory.Exists(path))
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    Arguments = path,
                    FileName = "explorer.exe"
                };

                Process.Start(startInfo);
            }
        }

        private void GetImages(List<string> folders, int interval)
        {
            HashSet<string> images = new HashSet<string>();
            // clear mappings
            foreach (string hostname in folderMappings)
            {
                webView.CoreWebView2.ClearVirtualHostNameToFolderMapping(hostname);
            }
            folderMappings.Clear();

            int folderNum = 0;

            foreach (string folder in folders)
            {
                string hostName = $"quickdraw-folder{folderNum}.invalid";

                webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    hostName, folder,
                    Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow
                );

                folderMappings.Add(hostName);

                var files = Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
                .Where(s => s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                || s.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                || s.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Replace(folder, $"https://{hostName}"));

                images.UnionWith(files.ToHashSet<string>());

                folderNum++;
            }

            string jsonString = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                { "interval", interval },
                { "images", images }
            });

            webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync($@"
                var slideshowData = {jsonString};
            ");

            webView.CoreWebView2.Navigate("http://localhost:8080/slideshow.html");
        }

        private async void OpenFolders()
        {

            List <ImageFolder> folders  = await Task<uint>.Run(() =>
            {
                List<ImageFolder> folders = new List<ImageFolder>();

                IFileOpenDialog dialog = null;
                uint count = 0;
                try
                {
                    dialog = new NativeFileOpenDialog();
                    dialog.SetOptions(
                        FileOpenDialogOptions.NoChangeDir
                        | FileOpenDialogOptions.PickFolders
                        | FileOpenDialogOptions.AllowMultiSelect
                        | FileOpenDialogOptions.PathMustExist
                    );
                    dialog.Show(IntPtr.Zero);


                    IShellItemArray shellItemArray = null;
                    dialog.GetResults(out shellItemArray);

                    if (shellItemArray != null)
                    {
                        IntPtr i_result;
                        string filepath = null;
                        shellItemArray.GetCount(out count);

                        for (uint i = 0; i < count; i++)
                        {
                            IShellItem shellItem = null;

                            shellItemArray.GetItemAt(i, out shellItem);

                            if (shellItem != null)
                            {
                                shellItem.GetDisplayName(SIGDN.FILESYSPATH, out i_result);
                                filepath = Marshal.PtrToStringAuto(i_result);
                                Marshal.FreeCoTaskMem(i_result);

                                var files = Directory.EnumerateFiles(filepath, "*.*", SearchOption.AllDirectories)
                                    .Where(s => s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                                            || s.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                                            || s.EndsWith(".png", StringComparison.OrdinalIgnoreCase));

                                folders.Add(new ImageFolder { Path = filepath, Count = files.Count() });
                            }
                        }
                    }
                }
                catch(System.Runtime.InteropServices.COMException)
                {
                    // No files or other weird error, do nothing.
                }
                finally
                {
                    if (dialog != null)
                        System.Runtime.InteropServices.Marshal.FinalReleaseComObject(dialog);
                }
                return folders;
            });

            if (folders.Count > 0)
            {
                WebViewAddFolders(folders);
            }
        }

        private void ReceiveMessage(object sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            Message message = JsonSerializer.Deserialize<Message>(args.WebMessageAsJson);
            
            switch(message.type)
            {
                case "addFolders":
                    OpenFolders();
                    break;
                case "openFolder":
                    OpenFolderMessage openFolderMessage = JsonSerializer.Deserialize<OpenFolderMessage>(args.WebMessageAsJson);
                    OpenFolderInExplorer(openFolderMessage.path);
                    break;
                case "getImages":
                    GetImagesMessage getImagesMessage = JsonSerializer.Deserialize<GetImagesMessage>(args.WebMessageAsJson);
                    GetImages(getImagesMessage.folders, 30 * 1000);
                    break;
                default:
                    break;
            }
        }
    }
}
