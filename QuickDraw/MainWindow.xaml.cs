using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace QuickDraw
{
    public class Message
    {
        public string type { get; set; }
    }

    public class PathOpMessage : Message
    {
        public string path { get; set; }
    }

    public class PathListOpMessage : Message
    {
        public List<string> paths { get; set; }
    }

    public class GetImagesMessage : PathListOpMessage
    {
        public int interval { get; set; }
    }

    public struct ImageFolder
    {
        public string Path { get; set; }

        public int Count { get; set; }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class QuickDrawWindow : Window, System.Windows.Forms.IWin32Window
    {
        private string domain;
        public static readonly RoutedCommand StopPropagation = new();
        private void ExecutedStopPropagation(object sender, ExecutedRoutedEventArgs e)
        {
            // Do nothing, disabling this key combo
            e.Handled = true;
        }

        private void CanExecuteStopPropagation(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private readonly Dictionary<string, string> folderMappings = new();
        public IntPtr Handle => new System.Windows.Interop.WindowInteropHelper(this).Handle;

        private void WebViewUpdateFolders(List<ImageFolder> folders)
        {
            string jsonString = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                { "type", "UpdateFolders" },
                { "data", folders }
            });

            webView.CoreWebView2.PostWebMessageAsJson(jsonString);
        }

        public void OnClosed(object sender, EventArgs args)
        {
            ((App)Application.Current).Shutdown();
        }

        public QuickDrawWindow()
        {
            InitializeComponent();

            Closed += OnClosed;

            InitializeAsync();

        }

        private async void InitializeAsync()
        {
            string userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"QuickDraw");
            Directory.CreateDirectory(userDataFolder);
            CoreWebView2EnvironmentOptions options = new();
            CoreWebView2Environment env = CoreWebView2Environment.CreateAsync("", userDataFolder, options).GetAwaiter().GetResult();

            await webView.EnsureCoreWebView2Async(env);

#if DEBUG
            domain = "http://localhost:8080";
#else
            webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "quickdraw.invalid", "WebSrc",
                Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow
            );
            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            webView.CoreWebView2.Settings.IsZoomControlEnabled = false;
            webView.CoreWebView2.Settings.AreDevToolsEnabled = false;

            webView.InputBindings.Add(new InputBinding(QuickDrawWindow.StopPropagation, new KeyGesture(Key.F, ModifierKeys.Control)));
            webView.InputBindings.Add(new InputBinding(QuickDrawWindow.StopPropagation, new KeyGesture(Key.G, ModifierKeys.Control)));
            webView.InputBindings.Add(new InputBinding(QuickDrawWindow.StopPropagation, new KeyGesture(Key.G, ModifierKeys.Control | ModifierKeys.Shift)));
            webView.InputBindings.Add(new InputBinding(QuickDrawWindow.StopPropagation, new KeyGesture(Key.P, ModifierKeys.Control)));
            webView.InputBindings.Add(new InputBinding(QuickDrawWindow.StopPropagation, new KeyGesture(Key.R, ModifierKeys.Control)));
            webView.InputBindings.Add(new InputBinding(QuickDrawWindow.StopPropagation, new KeyGesture(Key.F5)));
            webView.InputBindings.Add(new InputBinding(QuickDrawWindow.StopPropagation, new KeyGesture(Key.BrowserRefresh)));
            webView.InputBindings.Add(new InputBinding(QuickDrawWindow.StopPropagation, new KeyGesture(Key.R, ModifierKeys.Control | ModifierKeys.Shift)));
            webView.InputBindings.Add(new InputBinding(QuickDrawWindow.StopPropagation, new KeyGesture(Key.F5, ModifierKeys.Control)));
            webView.InputBindings.Add(new InputBinding(QuickDrawWindow.StopPropagation, new KeyGesture(Key.F5, ModifierKeys.Shift)));
            webView.InputBindings.Add(new InputBinding(QuickDrawWindow.StopPropagation, new KeyGesture(Key.BrowserRefresh, ModifierKeys.Control)));
            webView.InputBindings.Add(new InputBinding(QuickDrawWindow.StopPropagation, new KeyGesture(Key.BrowserRefresh, ModifierKeys.Shift)));
            webView.InputBindings.Add(new InputBinding(QuickDrawWindow.StopPropagation, new KeyGesture(Key.F3)));
            webView.InputBindings.Add(new InputBinding(QuickDrawWindow.StopPropagation, new KeyGesture(Key.F3, ModifierKeys.Shift)));
            webView.InputBindings.Add(new InputBinding(QuickDrawWindow.StopPropagation, new KeyGesture(Key.BrowserBack)));
            webView.InputBindings.Add(new InputBinding(QuickDrawWindow.StopPropagation, new KeyGesture(Key.Left, ModifierKeys.Alt)));
            webView.InputBindings.Add(new InputBinding(QuickDrawWindow.StopPropagation, new KeyGesture(Key.BrowserForward)));
            webView.InputBindings.Add(new InputBinding(QuickDrawWindow.StopPropagation, new KeyGesture(Key.Right, ModifierKeys.Alt)));

            domain = "https://quickdraw.invalid"
#endif
            webView.Source = new Uri($"{domain}/index.html");

            webView.CoreWebView2.WebMessageReceived += ReceiveMessage;
        }

        private static void OpenFolderInExplorer(string path)
        {
            if (Directory.Exists(path))
            {
                ProcessStartInfo startInfo = new()
                {
                    Arguments = path,
                    FileName = "explorer.exe"
                };

                _ = Process.Start(startInfo);
            }
        }

        private void OpenImageInExplorer(string path)
        {
            Uri imageUri = new(path);
            string folder = folderMappings[imageUri.Host];
            string imagePath = System.IO.Path.GetFullPath($"{folder}{imageUri.AbsolutePath.Replace("/", "\\")}");

            if (File.Exists(imagePath))
            {
                ProcessStartInfo startInfo = new()
                {
                    Arguments = $"/select,\"{imagePath}\"",
                    FileName = "explorer.exe"
                };

                _ = Process.Start(startInfo);
            }

           
        }

        private static IEnumerable<string> GetFolderImages(string filepath)
        {
            IEnumerable<string> files = Directory.EnumerateFiles(filepath, "*.*", SearchOption.AllDirectories)
                                    .Where(s => s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                                            || s.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                                            || s.EndsWith(".png", StringComparison.OrdinalIgnoreCase));

            return files;
        }

        private void GetImages(List<string> folders, int interval)
        {
            HashSet<string> images = new();
            // clear mappings
            foreach (KeyValuePair<string, string> mapping in folderMappings)
            {
                webView.CoreWebView2.ClearVirtualHostNameToFolderMapping(mapping.Key);
            }
            folderMappings.Clear();

            int folderNum = 0;

            foreach (string folder in folders)
            {
                string hostName = $"quickdraw-folder{folderNum}.invalid";

                webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    hostName, folder,
                    CoreWebView2HostResourceAccessKind.Allow
                );

                folderMappings.Add(hostName, folder);

                IEnumerable<string> files = GetFolderImages(folder).Select(p => p.Replace(folder, $"https://{hostName}").Replace("\\", "/"));

                images.UnionWith(files.ToHashSet());

                folderNum++;
            }

            if (images.Count > 0)
            {
                string jsonString = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    { "interval", interval * 1000 },
                    { "images", images }
                });

                _ = webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync($@"
                    var slideshowData = {jsonString};
                ");

                webView.CoreWebView2.Navigate($"{domain}/slideshow.html");
            }
            else
            {
                using DialogCenteringService centeringService = new(this);
                _ = MessageBox.Show(this, "No images found! Select one or more folders.", "QuickDraw", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        private void RefreshFolderCount(string path)
        {
            int count = GetFolderImages(path).Count();

            if (count > 0)
            {
                WebViewUpdateFolders(new List<ImageFolder> { new ImageFolder { Path = path, Count = count } });
            }
        }

        private void RefreshAllFolderCounts(List<string> paths)
        {
            List<ImageFolder> folders = new();

            foreach (string path in paths)
            {
                int count = GetFolderImages(path).Count();

                if (count > 0)
                {
                    folders.Add(new ImageFolder { Path = path, Count = count });
                }
            }

            WebViewUpdateFolders(folders);
        }

        private async void OpenFolders()
        {
            List<ImageFolder> folders = await Task.Run(() =>
          {
              List<ImageFolder> folders = new();

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
                  _ = dialog.Show(IntPtr.Zero);

                  dialog.GetResults(out IShellItemArray shellItemArray);

                  if (shellItemArray != null)
                  {
                      string filepath = null;
                      shellItemArray.GetCount(out count);

                      for (uint i = 0; i < count; i++)
                      {
                          shellItemArray.GetItemAt(i, out IShellItem shellItem);

                          if (shellItem != null)
                          {
                              shellItem.GetDisplayName(SIGDN.FILESYSPATH, out IntPtr i_result);
                              filepath = Marshal.PtrToStringAuto(i_result);
                              Marshal.FreeCoTaskMem(i_result);

                              IEnumerable<string> files = GetFolderImages(filepath);

                              folders.Add(new ImageFolder { Path = filepath, Count = files.Count() });
                          }
                      }
                  }
              }
              catch (COMException)
              {
                  // No files or other weird error, do nothing.
              }
              finally
              {
                  if (dialog != null)
                  {
                      _ = Marshal.FinalReleaseComObject(dialog);
                  }
              }
              return folders;
          });

            if (folders.Count > 0)
            {
                WebViewUpdateFolders(folders);
            }
        }

        private void ReceiveMessage(object sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            Message message = JsonSerializer.Deserialize<Message>(args.WebMessageAsJson);

            switch (message.type)
            {
                case "addFolders":
                    OpenFolders();
                    break;
                case "refreshFolder":
                    PathOpMessage refreshFolderMessage = JsonSerializer.Deserialize<PathOpMessage>(args.WebMessageAsJson);
                    RefreshFolderCount(refreshFolderMessage.path);
                    break;
                case "refreshFolders":
                    PathListOpMessage refreshFoldersMessage = JsonSerializer.Deserialize<PathListOpMessage>(args.WebMessageAsJson);
                    RefreshAllFolderCounts(refreshFoldersMessage.paths);
                    break;
                case "openFolder":
                    PathOpMessage openFolderMessage = JsonSerializer.Deserialize<PathOpMessage>(args.WebMessageAsJson);
                    OpenFolderInExplorer(openFolderMessage.path);
                    break;
                case "getImages":
                    GetImagesMessage getImagesMessage = JsonSerializer.Deserialize<GetImagesMessage>(args.WebMessageAsJson);
                    GetImages(getImagesMessage.paths, getImagesMessage.interval);
                    break;
                case "openImage":
                    PathOpMessage openImageMessage = JsonSerializer.Deserialize<PathOpMessage>(args.WebMessageAsJson);
                    OpenImageInExplorer(openImageMessage.path);
                    break;
                default:
                    break;
            }
        }
    }
}
