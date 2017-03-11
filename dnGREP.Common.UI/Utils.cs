using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;

namespace dnGREP.Common.UI
{
    public class UiUtils
    {
        public static bool IsOnScreen(Window form)
        {
            Screen[] screens = Screen.AllScreens;
            foreach (Screen screen in screens)
            {
                // when the form is snapped to the left side of the screen, the left position is a small negative number, 
                // and the bottom exceeds the working area by a small amount.  Similar for snapped right.
                int left = (int)form.Left + 10;
                int top = (int)form.Top + 10;
                int width = Math.Max(0, (int)form.ActualWidth - 40);
                int height = Math.Max(0, (int)form.ActualHeight - 40);

                Rectangle formRectangle = new Rectangle(left, top, width, height);

                if (screen.WorkingArea.Contains(formRectangle))
                {
                    return true;
                }
            }
            return false;
        }

        public static void CenterWindow(Window form)
        {
            double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            double windowWidth = form.Width;
            double windowHeight = form.Height;
            form.Left = (screenWidth / 2) - (windowWidth / 2);
            form.Top = (screenHeight / 2) - (windowHeight / 2);
        }
    }
}
