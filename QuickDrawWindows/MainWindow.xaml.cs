using CommunityToolkit.WinUI.UI;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using WinRT.Interop;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace QuickDraw
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : WinUIEx.WindowEx
    {
        public MainWindow()
        {
            this.InitializeComponent();

            var titlebar = this.AppWindow.TitleBar;
            titlebar.ExtendsContentIntoTitleBar = true;
            titlebar.ButtonBackgroundColor = ((SolidColorBrush)Application.Current.Resources["WindowCaptionButtonBackground"]).Color;
            titlebar.ButtonHoverBackgroundColor = ((SolidColorBrush)Application.Current.Resources["WindowCaptionButtonBackgroundPointerOver"]).Color;
            titlebar.ButtonPressedBackgroundColor = ((SolidColorBrush)Application.Current.Resources["WindowCaptionButtonBackgroundPressed"]).Color;
            titlebar.ButtonInactiveBackgroundColor = ((SolidColorBrush)Application.Current.Resources["WindowCaptionBackgroundDisabled"]).Color;

            titlebar.ButtonForegroundColor = ((SolidColorBrush)Application.Current.Resources["WindowCaptionButtonStroke"]).Color;
            titlebar.ButtonHoverForegroundColor = ((SolidColorBrush)Application.Current.Resources["WindowCaptionButtonStrokePointerOver"]).Color;
            titlebar.ButtonPressedForegroundColor = ((SolidColorBrush)Application.Current.Resources["WindowCaptionButtonStrokePressed"]).Color;
            titlebar.ButtonInactiveForegroundColor = Color.FromArgb(0xff, 0x66,0x66, 0x66); //WindowCaptionForegroundDisabled converted to gray with no alpha, for some reason alpha is ignored here

            this.MainFrame.Navigate(typeof(MainPage));

        }



        public void NavigateToSlideshow()
        {
            MainFrame.Navigate(typeof(SlidePage), null, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
        }

        public void NavigateToMain()
        {
            MainFrame.Navigate(typeof(MainPage), null, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromLeft });
        }


    }
}
