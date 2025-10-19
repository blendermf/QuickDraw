using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using QuickDraw.Contracts.Services;
using QuickDraw.Contracts.ViewModels;
using QuickDraw.Views.Base;

namespace QuickDraw.ViewModels.Base;

public partial class ViewModelWithTitlebarBase : ObservableObject, IUnloadable, IViewModel
{
    public ITitlebarService TitlebarService;

    public GridLength TitlebarLeftInset => TitlebarService.LeftInset;
    public  GridLength TitlebarRightInset => TitlebarService.RightInset;

    [ObservableProperty]
    public partial bool TitlebarInactive { get; set; }

    public ViewModelWithTitlebarBase(ITitlebarService titlebarService)
    {
        TitlebarService = titlebarService;
        App.Window.Activated += Window_Activated;
    }

    public void HandleDragRegionsChanged(object sender, DragRegionsChangedEventArgs args)
    {
        TitlebarService?.TitleBar?.SetDragRectangles(args.DragRegions);
    }

    private void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            TitlebarInactive = true;
        }
        else
        {
            TitlebarInactive = false;
        }
    }

    public virtual void Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        App.Window.Activated -= Window_Activated;
    }
}
