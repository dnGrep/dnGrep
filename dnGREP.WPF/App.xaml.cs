using System;
using System.Windows;
using Alphaleonis.Win32.Filesystem;
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

        public CommandLineArgs AppArgs { get; private set; }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                GlobalDiagnosticsContext.Set("logDir", Path.Combine(Utils.GetDataFolderPath(), "logs"));

                AppTheme.Instance.Initialize();

                AppArgs = new CommandLineArgs(Environment.CommandLine);

                if (AppArgs.WarmUp)
                {
                    MainWindow = new MainForm(false);
                    MainWindow.Loaded += MainWindow_Loaded;
                }
                else if (AppArgs.ShowHelp)
                {
                    MainWindow = new HelpWindow(AppArgs.GetHelpString(), AppArgs.InvalidArgument);
                }
                else
                {
                    AppArgs.ApplyArgs();
                }

                if (MainWindow == null)
                {
                    MainWindow = new MainForm();
                    Utils.DeleteTempFolder();
                }

                MainWindow.Show();
                if (AppArgs.ExecuteSearch && MainWindow.DataContext != null)
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
