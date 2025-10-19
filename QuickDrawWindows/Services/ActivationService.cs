using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using QuickDraw.Activation;
using QuickDraw.Contracts.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickDraw.Services;

public class ActivationService(ActivationHandler<LaunchActivatedEventArgs> defaultHandler, IEnumerable<IActivationHandler> activationHandlers, ISettingsService settingsService) : IActivationService
{
    public async Task ActivateAsync(object activationArgs)
    {
        await InitializeAsync();

        App.Window.Activate();

        await HandleActivationAsync(activationArgs);

        await StartupAsync();
    }

    private async Task HandleActivationAsync(object activationArgs)
    {
        var activationHandler = activationHandlers.FirstOrDefault(h => h.CanHandle(activationArgs));

        if (activationHandler != null)
        {
            await activationHandler.HandleAsync(activationArgs);
        }

        if (defaultHandler.CanHandle(activationArgs))
        {
            await defaultHandler.HandleAsync(activationArgs);
        }
    }

    private async Task InitializeAsync()
    {
        await settingsService.InitializeAsync().ConfigureAwait(false);
        await Task.CompletedTask;
    }

    private async Task StartupAsync()
    {
        var presenter = OverlappedPresenter.Create();

        presenter.PreferredMinimumWidth = 512;
        presenter.PreferredMinimumHeight = 312;
        App.Window.AppWindow.SetPresenter(presenter);

        await Task.CompletedTask;
    }
}
