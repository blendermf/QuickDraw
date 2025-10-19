using CommunityToolkit.WinUI.Animations;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using QuickDraw.Contracts.Services;
using QuickDraw.Contracts.ViewModels;
using QuickDraw.Utilities;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace QuickDraw.Services;

internal class NavigationService(IPageService pageService) : INavigationService
{
    private readonly IPageService _pageService = pageService;
    private object? _lastParameterUsed;
    private Frame? _frame;

    public event NavigatedEventHandler? Navigated;

    public Frame? Frame
    {
        get
        {
            if (_frame == null)
            {
                _frame = App.Window.Content as Frame;
                RegisterFrameEvents();
            }

            return _frame;
        }

        set
        {
            UnregisterFrameEvents();
            _frame = value;
            RegisterFrameEvents();
        }
    }

    [MemberNotNullWhen(true, nameof(Frame), nameof(_frame))]
    public bool CanGoBack => Frame != null && Frame.CanGoBack;

    private void RegisterFrameEvents()
    {
        if (_frame != null)
        {
            _frame.Navigated += OnNavigated;
        }
    }

    private void UnregisterFrameEvents()
    {
        if (_frame != null)
        {
            _frame.Navigated -= OnNavigated;
        }
    }

    public bool GoBack(NavigationTransitionInfo? transitionInfo = null)
    {
        if (CanGoBack)
        {
            var vmBeforeNavigation = _frame.GetPageViewModel();

            if (transitionInfo != null)
            {
                _frame.GoBack(transitionInfo);
            }
            else
            {
                _frame.GoBack();
            }
            
            if (vmBeforeNavigation is INavigationAware navigationAware)
            {
                navigationAware.OnNavigatedFrom();
            }

            return true;
        }

        return false;
    }

    public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false, NavigationTransitionInfo? transitionInfo = null)
    {
        var pageType = _pageService.GetPageType(pageKey);

        if (_frame != null && 
            (_frame.Content?.GetType() != pageType || (parameter != null && !parameter.Equals(_lastParameterUsed))))
        {
            _frame.Tag = clearNavigation;
            var vmBeforeNavigation = _frame.GetPageViewModel();

            bool navigated;
            if (transitionInfo != null)
            {
                navigated = _frame.Navigate(pageType, parameter, transitionInfo);
            }
            else
            {
                navigated = _frame.Navigate(pageType, parameter);
            }

            if (navigated)
            {
                _lastParameterUsed = parameter;
                if (vmBeforeNavigation is INavigationAware navigationAware)
                {
                    navigationAware.OnNavigatedFrom();
                }
            }

            return navigated;
        }

        return false;
    }

    private void OnNavigated(object sender, NavigationEventArgs e)
    {
        if (sender is Frame frame)
        {
            var clearNavigation = (bool)frame.Tag;
            if (clearNavigation)
            {
                frame.BackStack.Clear();
            }

            if (frame.GetPageViewModel() is INavigationAware navigationAware)
            {
                navigationAware.OnNavigatedTo(e.Parameter);
            }

            Navigated?.Invoke(sender, e);
        }
    }

    public void SetListDataItemForNextConnectedAnimation(object item) => Frame?.SetListDataItemForNextConnectedAnimation(item);
}