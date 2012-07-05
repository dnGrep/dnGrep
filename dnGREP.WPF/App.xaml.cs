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
            try
            {
                Utils.DeleteTempFolder();
                if (e.Args != null && e.Args.Length > 0)
                {
                    string searchPath = e.Args[0];
                    if (searchPath == "/warmUp")
                    {
                        this.MainWindow = new MainFormEx(false);
                        this.MainWindow.Loaded += new RoutedEventHandler(MainWindow_Loaded);
                    }
                    else
                    {
                        if (searchPath.EndsWith(":\""))
                            searchPath = searchPath.Substring(0, searchPath.Length - 1) + "\\";
                        GrepSettings.Instance.Set<string>(GrepSettings.Key.SearchFolder, searchPath);
                    }
                }
                if (this.MainWindow == null)
                    this.MainWindow = new MainFormEx();

                this.MainWindow.Show();
            }
            catch (Exception ex)
            {
                logger.LogException(LogLevel.Error, ex.Message, ex);
                MessageBox.Show("Something broke down in the program. See event log for details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.MainWindow.Close();
        }
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            try
            {
                Utils.DeleteTempFolder();
            }
            catch (Exception ex)
            {
                logger.LogException(LogLevel.Error, ex.Message, ex);
                MessageBox.Show("Something broke down in the program. See event log for details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            logger.LogException(LogLevel.Error, e.Exception.Message, e.Exception);
            MessageBox.Show("Something broke down in the program. See event log for details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
