using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace dnGREP.Common.UI
{
    public static class Extensions
    {
        public static IEnumerable<T> FindLogicalChildren<T>(this DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield return null;

            var dependencyChildren = LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>();
            foreach (var child in dependencyChildren)
            {
                if (child is T typedChild)
                    yield return typedChild;

                foreach (T childOfChild in FindLogicalChildren<T>(child))
                    yield return childOfChild;
            }
        }

        public static T GetVisualChild<T>(this DependencyObject depObj) where T : Visual
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);

                    if (child != null && child is T typedChild)
                    {
                        return typedChild;
                    }

                    T childOfChild = child.GetVisualChild<T>();
                    if (childOfChild != null)
                    {
                        return childOfChild;
                    }
                }
            }
            return null;
        }

        public static T GetChildOfType<T>(this DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);

                var result = (child as T) ?? GetChildOfType<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        public static Point ToDevicePixels(this Window window, Point pt)
        {
            var t = PresentationSource.FromVisual(window).CompositionTarget.TransformToDevice;
            return t.Transform(pt);
        }

        public static Rect ToDevicePixels(this Window window, Rect rect)
        {
            var t = PresentationSource.FromVisual(window).CompositionTarget.TransformToDevice;
            var topLeft = t.Transform(rect.TopLeft);
            var botRight = t.Transform(rect.BottomRight);
            return new Rect(topLeft, botRight);
        }

        public static Rect ToDevicePixels(this Screen screen, Rect rect)
        {
            double scaleX, scaleY;
            if (Environment.OSVersion.Version.Major >= 10 ||
                (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor == 3))
            {
                IntPtr hMonitor = NativeMethods.GetMonitor(screen.Bounds);
                NativeMethods.GetDpiForMonitor(hMonitor, NativeMethods.MonitorDpiType.MDT_EFFECTIVE_DPI, out uint dpiX, out uint dpiY);
                scaleX = (double)dpiX / 96;
                scaleY = (double)dpiY / 96;
            }
            else
            {
                // an old version of Windows (7 or 8)
                // Get scale of main window and assume scale is the same for all monitors
                var dpiScale = VisualTreeHelper.GetDpi(Application.Current.MainWindow);
                scaleX = dpiScale.DpiScaleX;
                scaleY = dpiScale.DpiScaleY;
            }

            Rect result = new Rect(
                rect.X * scaleX,
                rect.Y * scaleY,
                rect.Width * scaleX,
                rect.Height * scaleY);
            return result;
        }

        public static Point FromDevicePixels(this Window window, Point pt)
        {
            var t = PresentationSource.FromVisual(window).CompositionTarget.TransformFromDevice;
            return t.Transform(pt);
        }

        public static Rect FromDevicePixels(this Window window, Rect rect)
        {
            var t = PresentationSource.FromVisual(window).CompositionTarget.TransformFromDevice;
            var topLeft = t.Transform(rect.TopLeft);
            var botRight = t.Transform(rect.BottomRight);
            return new Rect(topLeft, botRight);
        }

        public static bool IsOnScreen(this Window window)
        {
            Rect windowBounds = new Rect(
                window.Left, window.Top, window.ActualWidth, window.ActualHeight);

            return windowBounds.IsOnScreen();
        }

        public static bool IsOnScreen(this Rect windowBounds)
        {
            // test to see if the center of the title bar is on a screen
            // this will allow the user to easily move the window if partially off screen
            // 44 is the width of a title bar button, 30 is the height
            Rect bounds = new Rect(
                windowBounds.Left + 5 + 44,
                windowBounds.Top + 5,
                Math.Max(windowBounds.Width - 3 * 44, 44),  // can't be negative!
                30);

            foreach (Screen screen in Screen.AllScreens)
            {
                Rect deviceRect = screen.ToDevicePixels(bounds);
                if (screen.WorkingArea.IntersectsWith(deviceRect))
                {
                    return true;
                }
            }
            return false;
        }

        public static Screen ScreenFromWpfPoint(this Point pt)
        {
            Rect bounds = new Rect(pt, new Size(2, 2));
            foreach (Screen screen in Screen.AllScreens)
            {
                Rect deviceRect = screen.ToDevicePixels(bounds);
                if (screen.WorkingArea.Contains(deviceRect))
                {
                    return screen;
                }
            }
            return null;
        }

        public static bool MoveWindow(this Window window, double x, double y)
        {
            double width = window.Width;
            double height = window.Height;
            Point pt = new Point(x, y);
            Screen screen = pt.ScreenFromWpfPoint();
            if (screen != null)
            {
                Rect bounds = new Rect(x, y, window.ActualWidth, window.ActualHeight);
                Rect r = screen.ToDevicePixels(bounds);
                if (NativeMethods.MoveWindow(new WindowInteropHelper(window).Handle, (int)r.Left, (int)r.Top, (int)r.Width, (int)r.Height, true))
                {
                    window.Dispatcher.Invoke(() =>
                    {
                        window.Width = width;
                        window.Height = height;

                        //var w = window.ActualWidth;
                        //var h = window.ActualHeight;
                    });
                }
            }
            return false;
        }

        public static void ConstrainToScreen(this Window window)
        {
            // don't let the window grow beyond the right edge of the screen
            var screen = Screen.FromHandle(new WindowInteropHelper(window).Handle);
            var bounds = window.FromDevicePixels(screen.WorkingArea);
            if (window.Left + window.ActualWidth > bounds.Right)
            {
                window.Width = Math.Min(20, bounds.Right - window.Left);  // can't be negative!
            }
        }

        public static void CenterWindow(this Window window)
        {
            var screen = Screen.FromHandle(new WindowInteropHelper(window).Handle);
            var rect = window.FromDevicePixels(screen.WorkingArea);

            double screenWidth = rect.Width;
            double screenHeight = rect.Height;

            if (window.ActualHeight > screenHeight)
                window.Height = screenHeight - 20;
            if (window.ActualWidth > screenWidth)
                window.Width = screenWidth - 20;

            window.Left = (screenWidth / 2) - (window.ActualWidth / 2);
            window.Top = (screenHeight / 2) - (window.ActualHeight / 2);
        }

        public static void ToRightEdge(this Window window)
        {
            var screen = Screen.FromHandle(new WindowInteropHelper(window).Handle);
            var rect = window.FromDevicePixels(screen.WorkingArea);

            double screenWidth = rect.Width;
            double screenHeight = rect.Height;

            if (window.ActualHeight > screenHeight)
                window.Height = screenHeight;
            if (window.ActualWidth > screenWidth)
                window.Width = screenWidth;

            window.Left = screenWidth - window.ActualWidth;
            window.Top = (screenHeight / 2) - (window.ActualHeight / 2);
        }

        public static void SetWindowPosition(this Window window, Point pt, Window relativeTo)
        {
            pt.Offset(relativeTo.Left, relativeTo.Top);
            IntPtr hwnd = new WindowInteropHelper(relativeTo).Handle;
            Screen screen = Screen.FromHandle(hwnd);
            Rect wa = relativeTo.FromDevicePixels(screen.WorkingArea);

            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = pt.X;
            window.Top = pt.Y;
            if (window.Left < wa.Left)
                window.Left = wa.Left;
            if (window.Left + window.Width > wa.Right)
                window.Left = wa.Right - window.Width;
            if (window.Top < wa.Top)
                window.Top = wa.Top;
            if (window.Top + window.Height > wa.Bottom)
                window.Top = wa.Bottom - window.Height;
        }


        public static Size MeasureString(this string candidate, Typeface typeface, double fontSize, FrameworkElement control)
        {
            var formattedText = new FormattedText(candidate,
                                                  CultureInfo.CurrentUICulture,
                                                  FlowDirection.LeftToRight,
                                                  typeface,
                                                  fontSize,
                                                  Brushes.Black,
                                                  VisualTreeHelper.GetDpi(control).PixelsPerDip);

            return new Size(formattedText.Width, formattedText.Height);
        }

    }
}
