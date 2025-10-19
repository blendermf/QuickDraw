using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using QuickDraw.Activation;
using QuickDraw.Contracts.Services;
using QuickDraw.Services;
using QuickDraw.ViewModels;
using QuickDraw.Views;
using System;
using System.IO;
using System.Reflection;

namespace QuickDraw;

public partial class App : Application
{
    public IHost Host
    {
        get;
    }

    public static T GetService<T>()
        where T : class
    {
        if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public App()
    {
        try
        {
            string resourceName = "syncfusion.license";

            Assembly assembly = Assembly.GetExecutingAssembly();

            if (assembly != null)
            {
                using Stream? rsrcStream = assembly.GetManifestResourceStream(assembly.GetName().Name + ".Assets." + resourceName);

                if (rsrcStream != null)
                {
                    using StreamReader streamReader = new(rsrcStream);

                    string key = streamReader.ReadToEnd();

                    if (key != "")
                    {
                        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(key);
                    }    
                }
            }
        }
        catch { };

        this.InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.
            CreateDefaultBuilder().
            UseContentRoot(AppContext.BaseDirectory).
            ConfigureServices(services =>
            {
                // Default Activation Handler
                services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

                // Other Activation Handlers

                // Services
                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<IActivationService, ActivationService>();
                services.AddSingleton<IPageService, PageService>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<ITitlebarService, TitlebarService>();
                services.AddSingleton<ISlideImageService, SlideImageService>();

                // Views and ViewModels
                services.AddTransient<MainViewModel>();
                services.AddTransient<MainPage>();

                services.AddTransient<SlideViewModel>();
                services.AddTransient<SlidePage>();
            }).
            Build();
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        await App.GetService<IActivationService>().ActivateAsync(args);
    }

    public static MainWindow Window { get; } = new();
}
