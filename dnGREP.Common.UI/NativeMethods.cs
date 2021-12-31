using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace dnGREP.Common.UI
{
    [ComVisible(false)]
    internal sealed class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }
        }

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

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
        public class MONITORINFOEX
        {
            internal int cbSize = Marshal.SizeOf(typeof(MONITORINFOEX));
            internal RECT rcMonitor = new RECT();
            internal RECT rcWork = new RECT();
            internal int dwFlags = 0;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] internal char[] szDevice = new char[32];
        }

        internal static readonly uint MONITORINFOF_PRIMARY = 0x00000001;
        internal static readonly uint MONITOR_DEFAULTTONEAREST = 0x00000002;

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

        internal static readonly int SM_CMONITORS = 80;

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern int GetSystemMetrics(int nIndex);

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        internal static extern bool GetMonitorInfo(IntPtr hmonitor, [In, Out] MONITORINFOEX info);

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr MonitorFromWindow(IntPtr handle, uint flags);

        [DllImport("user32.dll")]
        internal static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, EnumMonitorsDelegate lpfnEnum, IntPtr dwData);

        internal delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);
    }
}
