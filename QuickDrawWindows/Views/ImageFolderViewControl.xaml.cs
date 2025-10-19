using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using QuickDraw.Core.Models;
using Windows.System;
using QuickDraw.Utilities;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace QuickDraw.Views;

public enum ColumnType
{
    Path,
    ImageCount,
    Grid
}

public sealed partial class ImageFolderViewControl : UserControl
{
    public ImageFolder Folder
    {
        get { return (ImageFolder)GetValue(FolderProperty); }
        set { SetValue(FolderProperty, value); }
    }

    public static readonly DependencyProperty FolderProperty = DependencyProperty.Register(
        nameof(Folder),
        typeof(ImageFolder),
        typeof(ImageFolderViewControl),
        new PropertyMetadata(null));

    public GridLength DesiredPathColumnWidth
    {
        get { return (GridLength)GetValue(DesiredPathColumnWidthProperty); }
        set
        {
            SetValue(DesiredPathColumnWidthProperty, value);
        }
    }

    public static readonly DependencyProperty DesiredPathColumnWidthProperty = DependencyProperty.Register(
        nameof(DesiredPathColumnWidth),
        typeof(GridLength),
        typeof(ImageFolderViewControl),
        new PropertyMetadata(0.0));

    public GridLength DesiredImageCountColumnWidth
    {
        get { return (GridLength)GetValue(DesiredImageCountColumnWidthProperty); }
        set { SetValue(DesiredImageCountColumnWidthProperty, value); }
    }

    public static readonly DependencyProperty DesiredImageCountColumnWidthProperty = DependencyProperty.Register(
        nameof(DesiredImageCountColumnWidth),
        typeof(GridLength),
        typeof(ImageFolderViewControl),
        new PropertyMetadata(0.0));

    public ImageFolderViewControl()
    {
        this.InitializeComponent();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        /*var settings = (App.Current as App)?.Settings;

        settings?.ImageFolderList.UpdateFolderCount(Folder);*/
    }

    private void Folder_Click(object sender, RoutedEventArgs e)
    {
        var path = Folder.Path;
        Task.Run(async () =>
        {
            await Launcher.LaunchFolderPathAsync(path);
        });

        // TODO: probably notify user if this folder no longer exists, maybe offer to delete
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
/*            var settings = (App.Current as App)?.Settings;
        settings?.ImageFolderList.RemoveFolder(Folder);*/
    }
}
