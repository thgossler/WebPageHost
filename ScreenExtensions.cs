#nullable disable warnings

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WebPageHost
{
    public static class ScreenExtensions
    {
        public static Point GetMonitorDpi(this Screen screen, DpiType dpiType)
        {
            var pnt = new Point(screen.Bounds.Left + 1, screen.Bounds.Top + 1);
            var mon = MonitorFromPoint(pnt, 2/*MONITOR_DEFAULTTONEAREST*/);
            GetDpiForMonitor(mon, dpiType, out uint dpiX, out uint dpiY);
            return new Point { X = (int)dpiX, Y = (int)dpiY };
        }

        // Interop
        [DllImport("User32.dll")]
        private static extern IntPtr MonitorFromPoint([In] Point pt, [In] uint dwFlags);

        [DllImport("Shcore.dll")]
        private static extern IntPtr GetDpiForMonitor([In] IntPtr hmonitor, [In] DpiType dpiType, [Out] out uint dpiX, [Out] out uint dpiY);

        public enum DpiType
        {
            Effective = 0,
            Angular = 1,
            Raw = 2,
        }
    }
}
