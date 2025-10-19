using Microsoft.UI.Windowing;
using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinRT.Interop;
using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Controls;

namespace QuickDraw.Utilities;

internal class MonitorInfo
{

    [DllImport("Shcore.dll", SetLastError = true)]
    internal static extern int GetDpiForMonitor(IntPtr hmonitor, Monitor_DPI_Type dpiType, out uint dpiX, out uint dpiY);

    internal enum Monitor_DPI_Type : int
    {
        MDT_Effective_DPI = 0,
        MDT_Angular_DPI = 1,
        MDT_Raw_DPI = 2,
        MDT_Default = MDT_Effective_DPI
    }

    private static uint GetScaleAdjustmentUInt(Window window)
    {
        IntPtr hWnd = WindowNative.GetWindowHandle(window);
        WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
        DisplayArea displayArea = DisplayArea.GetFromWindowId(wndId, DisplayAreaFallback.Primary);
        IntPtr hMonitor = Win32Interop.GetMonitorFromDisplayId(displayArea.DisplayId);

        // Get DPI.
        int result = GetDpiForMonitor(hMonitor, Monitor_DPI_Type.MDT_Default, out uint dpiX, out uint _);
        if (result != 0)
        {
            throw new Exception("Could not get DPI for monitor.");
        }

        return (uint)(((long)dpiX * 100 + (96 >> 1)) / 96);
    }

    public static double GetScaleAdjustment(Window window)
    {

        return GetScaleAdjustmentUInt(window) / 100.0;
    }

    public static double GetInvertedScaleAdjustment(Window window)
    {

        return 100.0 / GetScaleAdjustmentUInt(window);
    }
}
