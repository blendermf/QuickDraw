using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using FolderDialog;
using System.Runtime.InteropServices;

namespace QuickDraw
{
    public class Message
    {
        public string type { get; set; }
    }

    public struct image_folder
    {
        public string Path { get; set; }
        public uint Index { get; set; }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<image_folder> Folders;

        private void UpdateWebViewFolders()
        {
            string jsonString = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                { "type", "UpdateFolders" },
                { "data", Folders }
            });

            webView.CoreWebView2.PostWebMessageAsJson(jsonString);

            Debug.WriteLine(jsonString);
        }

        public void AddImageFolder(string folderPath)
        {
            Folders.Add(new image_folder { Path = folderPath, Index = (uint)Folders.Count });
        }

        public MainWindow()
        {
            InitializeComponent();

            InitializeAsync();

            Folders = new List<image_folder>();
        }

        private async void InitializeAsync()
        {
            await webView.EnsureCoreWebView2Async(null);
            webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "quickdraw.assets", "WebSrc",
                Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.DenyCors
            );

            webView.CoreWebView2.WebMessageReceived += ReceiveMessage;
        }

        private async void OpenFolders()
        {
            uint count = await Task<uint>.Run(() =>
            {
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

                                AddImageFolder(filepath);
                            }
                        }
                    }
                }
                finally
                {
                    if (dialog != null)
                        System.Runtime.InteropServices.Marshal.FinalReleaseComObject(dialog);
                }
                return count;
            });

            if (count > 0)
            {
                UpdateWebViewFolders();
            }
        }

        private void ReceiveMessage(object sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            Message message = JsonSerializer.Deserialize<Message>(args.WebMessageAsJson);
            /*
            switch(message.type)
            {
                case "addFolders":
                    FolderBrowsesr
                    break;
                default:
                    break;
            }*/

            OpenFolders();
        }
    }
}
