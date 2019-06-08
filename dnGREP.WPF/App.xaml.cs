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
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                AppTheme.Instance.Initialize();

                string searchFor = null;
                Utils.DeleteTempFolder();
                if (e.Args != null && e.Args.Length > 0)
                {
                    string searchPath = e.Args[0];
                    if (e.Args.Length == 2)
                        searchFor = e.Args[1];
                    if (searchPath == "/warmUp")
                    {
                        this.MainWindow = new MainForm(false);
                        this.MainWindow.Loaded += new RoutedEventHandler(MainWindow_Loaded);
                    }
                    else
                    {
                        if (searchPath.EndsWith(":\""))
                            searchPath = searchPath.Substring(0, searchPath.Length - 1) + "\\";
                        GrepSettings.Instance.Set<string>(GrepSettings.Key.SearchFolder, searchPath);
                        if (searchFor != null)
                        {
                            GrepSettings.Instance.Set<string>(GrepSettings.Key.SearchFor, searchFor);
                            GrepSettings.Instance.Set<SearchType>(GrepSettings.Key.TypeOfSearch, SearchType.Regex);
                        }
                    }
                }
                if (this.MainWindow == null)
                    this.MainWindow = new MainForm();

                this.MainWindow.Show();
                if (searchFor != null && this.MainWindow.DataContext != null)
                    ((MainViewModel)this.MainWindow.DataContext).SearchCommand.Execute(null);
            }
            catch (Exception ex)
            {
                logger.Log<Exception>(LogLevel.Error, ex.Message, ex);
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
