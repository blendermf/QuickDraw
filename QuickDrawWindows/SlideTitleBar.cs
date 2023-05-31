using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices.WindowsRuntime;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace QuickDraw
{
    

    public sealed class SlideTitleBar : ContentControl
    {
        
        AppWindow m_appWindow;
        MainWindow m_window;
        AppWindowTitleBar m_titleBar;

        double m_leftInset = 0;
        double m_rightInset = 0;

        public SlideTitleBar()
        {
            this.DefaultStyleKey = typeof(SlideTitleBar);
        }

        protected override void OnApplyTemplate()
        {
            m_window = ((App)Application.Current).Window;
            m_appWindow = m_window.AppWindow;
            m_titleBar = m_appWindow.TitleBar;

            this.Unloaded += SlideTitleBar_Unloaded;

            this.SizeChanged += TitleBar_SizeChanged;

            (GetTemplateChild("BackButton") as Button).Click += SlideTitleBar_BackClick;

            m_titleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            ApplyInset();
            SetDragRegion();
        }

        void ApplyInset()
        {
            var scale = Utilities.GetInvertedScaleAdjustment(m_window);

            m_leftInset = (double)m_titleBar.LeftInset * scale;
            m_rightInset = (double)m_titleBar.RightInset * scale;

            (GetTemplateChild("LeftInsetColumn") as ColumnDefinition).Width = new GridLength(m_leftInset, GridUnitType.Pixel);
            (GetTemplateChild("RightInsetColumn") as ColumnDefinition).Width = new GridLength(m_rightInset, GridUnitType.Pixel);

            // TODO: set min widths of the centering columns, store it for the drag region
        }

        private void SetDragRegion()
        {
            double scale = Utilities.GetScaleAdjustment(m_window);

            var backWidth = (GetTemplateChild("BackColumn") as ColumnDefinition).ActualWidth;
            var centerLeftWidth = (GetTemplateChild("CenterLeftColumn") as ColumnDefinition).ActualWidth;
            var centerRightWidth = (GetTemplateChild("CenterRightColumn") as ColumnDefinition).ActualWidth;

            List<Windows.Graphics.RectInt32> dragRectsList = new();

            Windows.Graphics.RectInt32 dragRectL = new(
                (int)((m_leftInset + backWidth) * scale),
                0,
                (int)((centerLeftWidth - backWidth - m_leftInset) * scale),
                (int)(this.ActualHeight * scale)
            );

            dragRectsList.Add(dragRectL);


            Windows.Graphics.RectInt32 dragRectR = new(
                (int)((this.ActualWidth - centerRightWidth) * scale),
                0,
                (int)((centerRightWidth - m_rightInset) * scale),
                (int)(this.ActualHeight * scale)
            );

            dragRectsList.Add(dragRectR);

            Windows.Graphics.RectInt32[] dragRects = dragRectsList.ToArray();

            m_titleBar.SetDragRectangles(dragRects);
        }

        private void TitleBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetDragRegion();
        }

        void SlideTitleBar_Unloaded(object sender, RoutedEventArgs e)
        {
            m_appWindow = null;
            m_window = null;
            m_titleBar = null;
        }

        void SlideTitleBar_BackClick(object sender, RoutedEventArgs e)
        {
            m_window.NavigateToMain();
        }
    }
}
