using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using WpfScreenHelper;

namespace DockFloat
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

        public static bool IsOnScreen(this Window window)
        {
            // when the form is snapped to the left side of the screen, the left position is a small negative number, 
            // and the bottom exceeds the working area by a small amount.  Similar for snapped right.
            double left = window.Left + 10;
            double top = window.Top + 10;
            double width = Math.Max(0, window.ActualWidth - 40);
            double height = Math.Max(0, window.ActualHeight - 40);

            Rect windRect = new Rect(left, top, width, height);

            foreach (Screen screen in Screen.AllScreens)
            {
                if (screen.WorkingArea.Contains(windRect))
                {
                    return true;
                }
            }
            return false;
        }

        public static void CenterWindow(this Window window)
        {
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            double windowWidth = window.ActualWidth;
            double windowHeight = window.ActualHeight;
            window.Left = (screenWidth / 2) - (windowWidth / 2);
            window.Top = (screenHeight / 2) - (windowHeight / 2);
        }
    }
}
