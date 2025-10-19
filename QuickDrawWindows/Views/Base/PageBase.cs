using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using QuickDraw.Contracts.ViewModels;

namespace QuickDraw.Views.Base;

public partial class PageBase : Page
{
    protected IViewModel ViewModelBase { get; }

    public PageBase(IViewModel viewModel) 
    {
        ViewModelBase = viewModel;
        if (viewModel is IUnloadable unloadable)
        {
            Unloaded += unloadable.Unloaded;
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
    }
}