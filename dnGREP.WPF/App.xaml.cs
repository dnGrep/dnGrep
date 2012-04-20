using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using dnGREP.Common;
using NLog;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Utils.DeleteTempFolder();
            if (e.Args != null && e.Args.Length > 0)
            {
				string searchPath = e.Args[0];
                if (searchPath == "/warmUp")
                {
                    this.MainWindow = new MainForm(false);
                    this.MainWindow.Loaded += new RoutedEventHandler(MainWindow_Loaded);
                }
				if (searchPath.EndsWith(":\""))
					searchPath = searchPath.Substring(0, searchPath.Length - 1) + "\\";
                GrepSettings.Instance.Set<string>(GrepSettings.Key.SearchFolder, searchPath);
            }
            if (this.MainWindow == null)
                this.MainWindow = new MainForm();

            this.MainWindow.Show();            
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.MainWindow.Close();
        }
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Utils.DeleteTempFolder();
        }
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            logger.LogException(LogLevel.Error, e.Exception.Message, e.Exception);
        }
    }
}
