using DependencyPropertyGenerator;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using QuickDraw.Contracts.Services;
using System;
using System.Diagnostics;
using Windows.Graphics;

namespace QuickDraw.Views.Base;

public class DragRegionsChangedEventArgs : EventArgs
{
    public readonly RectInt32[] DragRegions;

    public DragRegionsChangedEventArgs(RectInt32[] dragRegions)
    {
        DragRegions = dragRegions; 
    }
}

[DependencyProperty<bool>("Inactive")]
[DependencyProperty<GridLength>("LeftInset")]
[DependencyProperty<GridLength>("RightInset")]
[DependencyProperty<ITitlebarService>("TitlebarService")]
public abstract partial class TitlebarBaseControl : UserControl
{
    public event EventHandler<DragRegionsChangedEventArgs>? DragRegionsChanged;

    public TitlebarBaseControl()
    {
        SizeChanged += OnSizeChanged;
    }

    protected void OnSizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
    {
        var regions = CalculateDragRegions();

        DragRegionsChanged?.Invoke(this, new(regions));
    }
 
    protected abstract RectInt32[] CalculateDragRegions();
}
