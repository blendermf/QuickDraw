using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using QuickDraw.Core.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices;
using CommunityToolkit.WinUI;
using System.Threading.Tasks;
using System.Collections.Specialized;
using QuickDraw.Utilities;
using System.Diagnostics;
using WinRT;
using System.Collections.Concurrent;
using Windows.System;
using System.Reflection;
using System.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace QuickDraw.Views;
public sealed partial class ImageFolderListViewControl2 : UserControl
{
    public object? ItemsSource
    {
        get { return (object)GetValue(ItemsSourceProperty); }
        set { SetValue(ItemsSourceProperty, value); }
    }
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource),
        typeof(object),
        typeof(ImageFolderListViewControl2),
        new PropertyMetadata(null)
    );

    public ImageFolderListViewControl2()
    {
        this.InitializeComponent();

/*            var settings = (App.Current as App)?.Settings;

        if (settings != null)
        {
            ImageFolderCollection = [.. settings.ImageFolderList.ImageFolders];
        }*/
    }
}
