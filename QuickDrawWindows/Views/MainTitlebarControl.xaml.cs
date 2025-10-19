using DependencyPropertyGenerator;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using QuickDraw.Utilities;
using QuickDraw.Views.Base;
using Windows.Graphics;

namespace QuickDraw.Views;

public sealed partial class MainTitlebarControl : Base.TitlebarBaseControl
{
    public MainTitlebarControl()
    {
        InitializeComponent();
    }

    protected override RectInt32[] CalculateDragRegions()
    {
        var scale = this.XamlRoot.RasterizationScale;

        RectInt32 dragRect = new(
            (int)(LeftInset.Value * scale),
            0,
            (int)(TitleColumn.ActualWidth * scale),
            (int)(ActualHeight * scale)
        );

        return [dragRect];
    }
}
