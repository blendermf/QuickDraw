using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using QuickDraw.Contracts.Services;
using QuickDraw.Contracts.ViewModels;
using QuickDraw.Core.Models;
using QuickDraw.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace QuickDraw.ViewModels;

public partial class MainViewModel : Base.ViewModelWithTitlebarBase, INavigationAware
{
    [ObservableProperty]
    public partial double TimerSliderValue { get; set; } = TimerEnum.T2m.ToSliderValue();

    [ObservableProperty]
    public partial ObservableCollection<ImageFolderViewModel> ImageFolderCollection { get; set; }

    private INavigationService _navigationService;
    private ISettingsService _settingsService;
    private ISlideImageService _slideImageService;

    public MainViewModel(INavigationService navigationService, ISettingsService settingsService, ITitlebarService titlebarService, ISlideImageService slideImageService) : base(titlebarService)
    {
        _navigationService = navigationService;
        _settingsService = settingsService;
        _slideImageService = slideImageService;

        ImageFolderCollection = new ObservableCollection<ImageFolderViewModel>(_settingsService.Settings!.ImageFolderList.ImageFolders.Select(f => new ImageFolderViewModel(f)));
    }

    public IEnumerable<string> GetSelectedFolders()
    {
        return ImageFolderCollection.Where(f => f.Selected).Select(f => f.Path);
    }

    [RelayCommand]
    private async Task StartSlideShowAsync()
    {
        var count = await _slideImageService.LoadImages(GetSelectedFolders());

        if (count > 0)
        {
            _slideImageService.SlideDuration = TimerSliderValue.ToTimerEnum();
            _navigationService.NavigateTo(typeof(SlideViewModel).FullName!, null, false,
                new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
        }
        else
        {
            // TODO: Display the fact there were no images found in the selected folders
        }
    }

    public async void OnNavigatedTo(object parameter)
    {
        var delay = TimeSpan.Parse((string)Application.Current.Resources["ControlFastAnimationDuration"]);

        await Task.Delay(delay);

        TitlebarService?.TitleBar?.PreferredHeightOption = TitleBarHeightOption.Standard;
        TitlebarService?.TitleBar?.IconShowOptions = IconShowOptions.ShowIconAndSystemMenu;
    }

    public void OnNavigatedFrom() { }
}