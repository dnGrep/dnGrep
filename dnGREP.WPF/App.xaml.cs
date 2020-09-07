using System;
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
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static string InstanceId { get; } = Guid.NewGuid().ToString();

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                AppTheme.Instance.Initialize();

                CommandLineArgs args = new CommandLineArgs();

                if (args.WarmUp)
                {
                    MainWindow = new MainForm(false);
                    MainWindow.Loaded += MainWindow_Loaded;
                }
                else if (args.ShowHelp)
                {
                    MainWindow = new HelpWindow(args.GetHelpString(), args.InvalidArgument);
                }
                else
                {
                    args.ApplyArgs();
                }

                if (MainWindow == null)
                {
                    MainWindow = new MainForm();
                    Utils.DeleteTempFolder();
                }

                MainWindow.Show();
                if (args.ExecuteSearch && MainWindow.DataContext != null)
                    ((MainViewModel)MainWindow.DataContext).SearchCommand.Execute(null);
            }
            catch (Exception ex)
            {
                logger.Log<Exception>(LogLevel.Error, ex.Message, ex);
                MessageBox.Show("Something broke down in the program. See event log for details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            MainWindow.Close();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            try
            {
                Utils.DeleteTempFolder();
            }
            catch (Exception ex)
            {
                logger.Log<Exception>(LogLevel.Error, ex.Message, ex);
                MessageBox.Show("Something broke down in the program. See event log for details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            logger.Log<Exception>(LogLevel.Error, e.Exception.Message, e.Exception);
            MessageBox.Show("Something broke down in the program. See event log for details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
