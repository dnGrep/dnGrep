using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Windows.Win32;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.HiDpi;

namespace dnGREP.Common.UI
{
    public static class Extensions
    {
        public static IEnumerable<T> FindLogicalChildren<T>(this DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;

            var dependencyChildren = LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>();
            foreach (var child in dependencyChildren)
            {
                if (child is T typedChild)
                    yield return typedChild;

                foreach (T childOfChild in FindLogicalChildren<T>(child))
                    yield return childOfChild;
            }
        }

        public static T? GetVisualChild<T>(this DependencyObject depObj) where T : Visual
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

                    T? childOfChild = child?.GetVisualChild<T>();
                    if (childOfChild != null)
                    {
                        return childOfChild;
                    }
                }
            }
            return null;
        }

        public static T? GetChildOfType<T>(this DependencyObject depObj) where T : DependencyObject
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

        /// <summary>
        /// Finds a parent of a given item on the visual tree.
        /// </summary>
        /// <typeparam name="T">The type of the queried item.</typeparam>
        /// <param name="child">A direct or indirect child of the
        /// queried item.</param>
        /// <returns>The first parent item that matches the submitted
        /// type parameter. If not matching item can be found, a null
        /// reference is being returned.</returns>
        public static T? TryFindParent<T>(this DependencyObject child)
            where T : DependencyObject
        {
            //get parent item
            DependencyObject? parentObject = GetParentObject(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we're looking for
            if (parentObject is T parent)
            {
                return parent;
            }
            else
            {
                //use recursion to proceed with next level
                return TryFindParent<T>(parentObject);
            }
        }

        /// <summary>
        /// This method is an alternative to WPF's
        /// <see cref="VisualTreeHelper.GetParent"/> method, which also
        /// supports content elements. Keep in mind that for content element,
        /// this method falls back to the logical tree of the element!
        /// </summary>
        /// <param name="child">The item to be processed.</param>
        /// <returns>The submitted item's parent, if available. Otherwise
        /// null.</returns>
        public static DependencyObject? GetParentObject(this DependencyObject child)
        {
            if (child == null) return null;

            //handle content elements separately
            if (child is ContentElement contentElement)
            {
                var parent = ContentOperations.GetParent(contentElement);
                if (parent != null) return parent;

                return contentElement is FrameworkContentElement fce ? fce.Parent : null;
            }

            //also try searching for parent in framework elements (such as DockPanel, etc)
            if (child is FrameworkElement frameworkElement)
            {
                DependencyObject parent = frameworkElement.Parent;
                if (parent != null) return parent;
            }

            //if it's not a ContentElement/FrameworkElement, rely on VisualTreeHelper
            return VisualTreeHelper.GetParent(child);
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
                var pt = new System.Drawing.Point((int)(screen.Bounds.Left + screen.Bounds.Width / 2),
                    (int)(screen.Bounds.Top + screen.Bounds.Height / 2));

                var hMonitor = PInvoke.MonitorFromPoint(pt, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
                _ = PInvoke.GetDpiForMonitor(hMonitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out uint dpiX, out uint dpiY);
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

            Rect result = new(
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
            Rect windowBounds = new(window.Left, window.Top, window.ActualWidth, window.ActualHeight);
            return windowBounds.IsOnScreen();
        }

        public static bool IsOnScreen(this Rect windowBounds)
        {
            // test to see if the center of the title bar is on a screen
            // this will allow the user to easily move the window if partially off screen
            // 44 is the width of a title bar button, 30 is the height
            Rect bounds = new(
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

        public static Screen? ScreenFromWpfPoint(this Point pt)
        {
            Rect bounds = new(pt, new Size(2, 2));
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
            Point pt = new(x, y);
            Screen? screen = pt.ScreenFromWpfPoint();
            if (screen != null)
            {
                Rect bounds = new(x, y, window.ActualWidth, window.ActualHeight);
                Rect r = screen.ToDevicePixels(bounds);
                if (PInvoke.MoveWindow(new(new WindowInteropHelper(window).Handle), (int)r.Left, (int)r.Top, (int)r.Width, (int)r.Height, true))
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
