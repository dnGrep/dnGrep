using System;
using System.Linq;
using Microsoft.VisualBasic.ApplicationServices;

namespace dnGREP.WPF
{
    public class Launcher
    {
        [STAThread]
        public static void Main(string[] args)
        {
            dnGrepApp app = new dnGrepApp();
            app.Run();
        }
    }
}
