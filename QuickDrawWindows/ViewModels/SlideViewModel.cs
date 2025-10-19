using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using QuickDraw.Contracts.Services;
using QuickDraw.Contracts.ViewModels;
using QuickDraw.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace QuickDraw.ViewModels;

static class IntExtensions
{
    public static int Mod(this int value, int denominator)
    {
        int r = value % denominator;
        return r < 0 ? r + denominator : r;
    }
}

public class LoadImageEventArgs(string imagePath)
{
    public string ImagePath = imagePath;
}

public partial class SlideViewModel(ITitlebarService titlebarService, INavigationService navigationService, ISlideImageService slideImageService) : Base.ViewModelWithTitlebarBase(titlebarService), INavigationAware
{
    public event EventHandler<LoadImageEventArgs>? NextImageHandler;
    public event EventHandler<LoadImageEventArgs>? PreviousImageHandler;

    private int _currentImageIndex = 0;

    public string? CurrentImagePath { get; set; }

    public string? NextImagePath { get; set; }

    public string? PreviousImagePath { get; set; }

    private List<string> Images { get => slideImageService.Images; }

    [ObservableProperty]
    public partial double Progress { get; set; }

    [RelayCommand]
    public void UpdateCurrentImages()
    {
        CurrentImagePath = slideImageService.Images[_currentImageIndex];

        NextImagePath = slideImageService.Images[(_currentImageIndex + 1).Mod(Images.Count)];

        PreviousImagePath = slideImageService.Images[(_currentImageIndex - 1).Mod(Images.Count)];
    }

    [RelayCommand]
    public void NextImage(bool fromSlider = false)
    {
        _ticksElapsed = 0;

        if (!fromSlider)
        {
            Progress = 0;
        }

        if (!Paused && !fromSlider) _slideTimer?.Start();

        _currentImageIndex = (_currentImageIndex + 1).Mod(Images.Count);
        NextImageHandler?.Invoke(this, new(slideImageService.Images[(_currentImageIndex + 1).Mod(Images.Count)]));

    }

    [RelayCommand]
    public void PreviousImage()
    {
        _ticksElapsed = 0;

        Progress = 0;

        if (!Paused) _slideTimer?.Start();

        _currentImageIndex = (_currentImageIndex - 1).Mod(Images.Count);
        PreviousImageHandler?.Invoke(this, new(slideImageService.Images[(_currentImageIndex - 1).Mod(Images.Count)]));
    }

    DispatcherQueueTimer? _slideTimer = null;
    private uint _ticksElapsed = 0;

    public void StartTimer(DispatcherQueue dispatcherQueue)
    {
        var timerDurationEnum = slideImageService.SlideDuration;

        if (timerDurationEnum != TimerEnum.NoLimit)
        {
            _slideTimer = dispatcherQueue.CreateTimer();
            var timerDuration = timerDurationEnum.ToSeconds();

            Progress = 0;
            _slideTimer.IsRepeating = true;
            _slideTimer.Interval = new(TimeSpan.TicksPerMillisecond * (long)1000);
            _slideTimer.Tick += async (sender, e) =>
            {
                _ticksElapsed += 1;
                Progress = 100 * (double)_ticksElapsed / (double)timerDuration;
                if (_ticksElapsed >= timerDuration)
                {
                    _ticksElapsed = 0;

                    NextImage(true);

                    await Task.Delay(100);
                    Progress = 0;

                }
            };
            _slideTimer.Start();
        }
        else
        {
            PauseVisibility = Visibility.Collapsed;
        }
    }

    public event EventHandler? InvalidateCanvas;

    [ObservableProperty]
    public partial bool Grayscale { get; set; }

    [ObservableProperty]
    public partial Visibility PauseVisibility { get; set; }

    [RelayCommand]
    private void ToggleGrayscale() => Grayscale = !Grayscale;

    [RelayCommand]
    public void GoBack() => navigationService.GoBack();

    [RelayCommand]
    public void PausePlay()
    {
        if (Paused)
        {
            _slideTimer?.Start();
            Paused = false;
        }
        else
        {
            _slideTimer?.Stop();
            Paused = true;
        }
    }

    [ObservableProperty]
    public partial bool Paused { get; private set; }

    partial void OnGrayscaleChanged(bool oldValue, bool newValue)
    {
        if (oldValue != newValue)
        {
            InvalidateCanvas?.Invoke(this, EventArgs.Empty);
        }
    }

    public async void OnNavigatedTo(object parameter)
    {
        // TODO: toggle pause visibility based on if there is a timer or not
        // eg. if SlideTimerDuration == Models.TimerEnum.NoLimit
        // Might be a better place, eg if we have the view bind one of their initilization events to the VM

        var delay = TimeSpan.Parse((string)Application.Current.Resources["ControlFastAnimationDuration"]);

        await Task.Delay(delay);

        TitlebarService?.TitleBar?.PreferredHeightOption = TitleBarHeightOption.Tall;
        TitlebarService?.TitleBar?.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
    }

    public void OnNavigatedFrom() { }
}
