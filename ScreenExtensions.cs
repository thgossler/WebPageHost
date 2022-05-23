// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

#nullable disable warnings

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WebPageHost;

public static class ScreenExtensions
{
    /// <summary>
    /// Gets the DPI resolution of the specified screen.
    /// </summary>
    /// <param name="screen">The monitor to be considered.</param>
    /// <param name="dpiType">The type of DPI being queried.</param>
    /// <returns>The horizontal and vertical DPI resolution as Point data structure.</returns>
    public static Point GetMonitorDpi(this Screen screen, DpiType dpiType)
    {
        var pnt = new Point(screen.Bounds.Left + 1, screen.Bounds.Top + 1);
        IntPtr mon = MonitorFromPoint(pnt, 2/*MONITOR_DEFAULTTONEAREST*/);
        _ = GetDpiForMonitor(mon, dpiType, out uint dpiX, out uint dpiY);
        return new Point { X = (int)dpiX, Y = (int)dpiY };
    }

    // Interop
    [DllImport("User32.dll")]
    private static extern IntPtr MonitorFromPoint([In] Point pt, [In] uint dwFlags);

    [DllImport("Shcore.dll")]
    private static extern IntPtr GetDpiForMonitor([In] IntPtr hmonitor, [In] DpiType dpiType, [Out] out uint dpiX, [Out] out uint dpiY);

    /// <summary>
    /// The DPI type, see also: 
    /// https://docs.microsoft.com/en-us/windows/win32/api/shellscalingapi/ne-shellscalingapi-monitor_dpi_type#constants
    /// </summary>
    public enum DpiType
    {
        Effective = 0,
        Angular = 1,
        Raw = 2,
    }
}
