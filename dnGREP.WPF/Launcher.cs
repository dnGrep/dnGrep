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
            App.Run();
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
