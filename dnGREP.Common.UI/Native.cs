using System;
using System.Windows;

namespace dnGREP.Common.UI
{
    public static class Native
    {
        public static readonly IntPtr HTMAXBUTTON = new IntPtr(9);
        public static readonly int WM_NCHITTEST = 0x0084;

        public static Point GetPoint(IntPtr lParam)
        {
            uint xy = unchecked(IntPtr.Size == 8 ? (uint)lParam.ToInt64() : (uint)lParam.ToInt32());
            int x = unchecked((short)xy);
            int y = unchecked((short)(xy >> 16));
            return new Point(x, y);
        }
    }
}
