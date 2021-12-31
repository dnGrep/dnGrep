using System;
using System.Collections.Generic;
using System.Windows;

namespace dnGREP.Common.UI
{
    public class Screen
    {
        private readonly IntPtr hMonitor;

        private Screen(IntPtr monitor)
        {
            var info = new NativeMethods.MONITORINFOEX();
            NativeMethods.GetMonitorInfo(monitor, info);

            Bounds = new Rect(
                info.rcMonitor.left, info.rcMonitor.top,
                info.rcMonitor.right - info.rcMonitor.left,
                info.rcMonitor.bottom - info.rcMonitor.top);

            WorkingArea = new Rect(
                info.rcWork.left, info.rcWork.top,
                info.rcWork.right - info.rcWork.left,
                info.rcWork.bottom - info.rcWork.top);

            Primary = (info.dwFlags & NativeMethods.MONITORINFOF_PRIMARY) != 0;

            DeviceName = new string(info.szDevice).TrimEnd((char)0);

            hMonitor = monitor;
        }

        public override bool Equals(object obj)
        {
            return obj is Screen screen &&
                   EqualityComparer<IntPtr>.Default.Equals(hMonitor, screen.hMonitor);
        }

        public override int GetHashCode()
        {
            return -1250308577 + hMonitor.GetHashCode();
        }

        public Rect Bounds { get; private set; }

        public Rect WorkingArea { get; private set; }

        public string DeviceName { get; private set; }

        public bool Primary { get; private set; }

        internal static Screen FromHandle(IntPtr handle)
        {
            return new Screen(NativeMethods.MonitorFromWindow(handle, NativeMethods.MONITOR_DEFAULTTONEAREST));
        }

        public static IEnumerable<Screen> AllScreens
        {
            get
            {
                List<Screen> screens = new List<Screen>();

                NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                    delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData)
                    {
                        screens.Add(new Screen(hMonitor));
                        return true;

                    },
                    IntPtr.Zero);

                return screens;
            }
        }
    }
}
