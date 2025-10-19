namespace QuickDraw.Contracts.ViewModels;

public interface IUnloadable
{
    public void Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e);
}

