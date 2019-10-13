using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace DockFloat
{
    [ComVisible(false)]
    internal sealed class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        private static uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        [DllImport("User32.dll", SetLastError = true)]
        private static extern IntPtr MonitorFromPoint(POINT pt, uint flags);

        internal static IntPtr GetMonitor(Rect bounds)
        {
            POINT pt = new POINT((int)(bounds.Left + bounds.Width / 2),
                (int)(bounds.Top + bounds.Height / 2));

            return MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
        }

        internal static IntPtr GetMonitor(Point point)
        {
            POINT pt = new POINT((int)point.X, (int)point.Y);

            return MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
        }

        internal enum MonitorDpiType
        {
            MDT_EFFECTIVE_DPI = 0,
            MDT_ANGULAR_DPI,
            MDT_RAW_DPI,
            MDT_DEFAULT
        };

        [DllImport("Shcore.dll")]
        internal static extern uint GetDpiForMonitor(IntPtr hmonitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);

        [DllImport("User32.dll")]
        internal static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);

    }
}
