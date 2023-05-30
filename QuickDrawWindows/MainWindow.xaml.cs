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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.ViewManagement;
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

            titlebar.PreferredHeightOption = TitleBarHeightOption.Standard;

            this.MainFrame.Navigate(typeof(MainPage));

        }

        public void NavigateToSlideshow()
        {
            var titlebar = this.AppWindow.TitleBar;
            var delay = TimeSpan.Parse((string)Application.Current.Resources["ControlFastAnimationDuration"]);
            Task.Run(async delegate { 
                await Task.Delay(delay);
                titlebar.PreferredHeightOption = TitleBarHeightOption.Tall;
            });

            VisualStateManager.GoToState(this.AppTitleBar, "SlideLayout", true);
            this.MainFrame.Navigate(typeof(SlidePage), null, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated)
            {
                VisualStateManager.GoToState(this.AppTitleBar, "Inactive", true);
            }
            else
            {
                VisualStateManager.GoToState(this.AppTitleBar, "Active", true);
            }
        }
    }
}
