using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using QuickDraw.Contracts.Services;
using QuickDraw.Utilities;
using Windows.UI;
namespace QuickDraw.Services;

public class TitlebarService : ITitlebarService
{
    private MainWindow? _window;
    private AppWindowTitleBar? _titlebar;

    private GridLength _leftInset;
    private GridLength _rightInset;

    public void Initialize(Window window)
    {
        _window = window as MainWindow;
        var appWindow = _window!.AppWindow;
        _titlebar = appWindow.TitleBar;

        _titlebar.ExtendsContentIntoTitleBar = true;
        _titlebar.ButtonBackgroundColor = ((SolidColorBrush)Application.Current.Resources["WindowCaptionButtonBackground"]).Color;
        _titlebar.ButtonHoverBackgroundColor = ((SolidColorBrush)Application.Current.Resources["WindowCaptionButtonBackgroundPointerOver"]).Color;
        _titlebar.ButtonPressedBackgroundColor = ((SolidColorBrush)Application.Current.Resources["WindowCaptionButtonBackgroundPressed"]).Color;
        _titlebar.ButtonInactiveBackgroundColor = ((SolidColorBrush)Application.Current.Resources["WindowCaptionBackgroundDisabled"]).Color;

        _titlebar.ButtonForegroundColor = ((SolidColorBrush)Application.Current.Resources["WindowCaptionButtonStroke"]).Color;
        _titlebar.ButtonHoverForegroundColor = ((SolidColorBrush)Application.Current.Resources["WindowCaptionButtonStrokePointerOver"]).Color;
        _titlebar.ButtonPressedForegroundColor = ((SolidColorBrush)Application.Current.Resources["WindowCaptionButtonStrokePressed"]).Color;

        _titlebar.ButtonInactiveForegroundColor = Color.FromArgb(0xff, 0x66, 0x66, 0x66); //WindowCaptionForegroundDisabled converted to gray with no alpha, for some reason alpha is ignored here
        //titlebar.ButtonInactiveForegroundColor = ((SolidColorBrush)Application.Current.Resources["WindowCaptionForegroundDisabled"]).Color;

        var scaleInv = MonitorInfo.GetInvertedScaleAdjustment(_window);

        _leftInset = new(_titlebar.LeftInset * scaleInv, GridUnitType.Pixel);
        _rightInset = new(_titlebar.LeftInset * scaleInv, GridUnitType.Pixel);
    }

    public AppWindowTitleBar? TitleBar => _titlebar;

    public GridLength LeftInset => _leftInset;
    public GridLength RightInset => _rightInset;
}
