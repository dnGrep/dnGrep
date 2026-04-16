using System;
using System.Windows;
using System.Windows.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;

namespace dnGREP.Common.UI
{
    public static class Native
    {
        public static readonly IntPtr HTMAXBUTTON = new(9);
        public static readonly int WM_NCHITTEST = 0x0084;

        public static Point GetPoint(IntPtr lParam)
        {
            uint xy = unchecked(IntPtr.Size == 8 ? (uint)lParam.ToInt64() : (uint)lParam.ToInt32());
            int x = unchecked((short)xy);
            int y = unchecked((short)(xy >> 16));
            return new Point(x, y);
        }

        public static unsafe void CloakWindow(Window window, bool cloak)
        {
            HWND hwnd = new(new WindowInteropHelper(window).Handle);
            BOOL cloaked = new(cloak); // 1 to enable cloaking, 0 to disable
            PInvoke.DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_CLOAK,
                &cloaked, sizeof(int));
        }
    }
}
