using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;

namespace dnGREP.Common.UI
{
    public class UiUtils
    {
        public static bool IsOnScreen(Window form)
        {
            Screen[] screens = Screen.AllScreens;
            foreach (Screen screen in screens)
            {
                Rectangle formRectangle = new Rectangle((int)form.Left, (int)form.Top, (int)form.ActualWidth, (int)form.ActualHeight);

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
