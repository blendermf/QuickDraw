using System;
using System.Diagnostics;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using System.Net;
using System.IO;
using System.ComponentModel;

namespace QuickDraw
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly WebClient webClient = new();
        private InstallingWindow installingWindow;

        private string installerFile;

        private static bool HasWebView2()
        {
            try
            {
                string versionString = CoreWebView2Environment.GetAvailableBrowserVersionString();
                Version requiredVersion = Version.Parse("89.0.774.75");
                Version version = Version.Parse(versionString.Split(" ")[0]);

                return version.CompareTo(requiredVersion) >= 0;
            }
            catch (WebView2RuntimeNotFoundException)
            {
                return false;
            }
        }

        private void DownloadWebView2()
        {
            _ = webClient.DownloadFileTaskAsync(new Uri(@"https://go.microsoft.com/fwlink/p/?LinkId=2124703"), installerFile);
        }

        private void InstallError()
        {
            installingWindow.Hide();

            MessageBoxResult dialogResult = MessageBox.Show("Microsoft Edge WebView2 did not install properly. Click OK to try again or Cancel to quit.",
                                    "QuickDraw", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation);

            if (dialogResult == MessageBoxResult.OK)
            {
                installingWindow.Show();
                if (File.Exists(installerFile))
                {
                    File.Delete(installerFile);
                }
                DownloadWebView2();
            }
            else
            {
                Shutdown();
            }
        }

        protected override /*async*/ void OnStartup(StartupEventArgs e)
        {
            webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadRuntimeCompleted);

            string tempFolder = Path.GetTempPath();
            installerFile = Path.Combine(tempFolder, "MicrosoftEdgeWebview2Setup.exe");

            // Check for WebView2 Runtime, install if needed
            if (HasWebView2())
            {
                MainWindow = new QuickDrawWindow();
                MainWindow.Show();
            }
            else
            {
                installingWindow = new InstallingWindow();
                installingWindow.Show();
                DownloadWebView2();
            }
        }

        private async void DownloadRuntimeCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null)
            {
                Process process = new();
                process.StartInfo.FileName = installerFile;
                process.StartInfo.Arguments = @"/silent /install";
                process.StartInfo.Verb = "runas";
                process.StartInfo.UseShellExecute = true;
                _ = process.Start();
                await process.WaitForExitAsync();

                if (HasWebView2())
                {
                    installingWindow.Hide();
                    MainWindow = new QuickDrawWindow();
                    MainWindow.Show();
                    return;
                }
            }

            InstallError();
        }
    }
}
