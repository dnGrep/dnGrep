using System;
using System.Linq;
using Microsoft.VisualBasic.ApplicationServices;

namespace dnGREP.WPF
{
    public class Launcher : WindowsFormsApplicationBase
    {
        [STAThread]
        public static void Main(string[] args)
        { 
            (new Launcher()).Run(args); 
        }

        public Launcher()
        { 
            IsSingleInstance = true; 
        }

        public dnGrepApp App { get; private set; }

        protected override bool OnStartup(StartupEventArgs e)
        {
            App = new dnGrepApp();

            if (e.CommandLine != null && e.CommandLine.Count > 0 && e.CommandLine[0] == "/hidden")
            {
                App.Run();
            }
            else
            {
                App.Run();
                App.ProcessArgs(e.CommandLine.ToArray(), true);
            }
            return false;
        }

        protected override void OnStartupNextInstance(
          StartupNextInstanceEventArgs eventArgs)
        {
            base.OnStartupNextInstance(eventArgs);
            App.ProcessArgs(eventArgs.CommandLine.ToArray(), false);
        }
    }
}
