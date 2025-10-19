using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace QuickDraw.Contracts.Services;

public interface ITitlebarService
{
    public AppWindowTitleBar? TitleBar { get; }

    public GridLength LeftInset { get; }
    public GridLength RightInset { get; }

    public void Initialize(Window window);

}
