using System;
using System.IO;
using System.Reflection;
using System.Windows;
using dnGREP.Common;
using dnGREP.Localization;
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

        public static string LogDir { get; private set; } = string.Empty;

        public CommandLineArgs? AppArgs { get; private set; }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                LogDir = Path.Combine(Utils.GetDataFolderPath(), "logs");
                GlobalDiagnosticsContext.Set("logDir", LogDir);

                Assembly? thisAssembly = Assembly.GetAssembly(typeof(App));
                if (thisAssembly != null)
                {
                    var path = Path.GetDirectoryName(thisAssembly.Location) ?? string.Empty;
                    if (Environment.Is64BitProcess)
                        SevenZip.SevenZipBase.SetLibraryPath(Path.Combine(path, @"7z64.dll"));
                    else
                        SevenZip.SevenZipBase.SetLibraryPath(Path.Combine(path, @"7z.dll"));
                }

                ResourceManagerEx.Initialize();
                TranslationSource.Instance.SetCulture(GrepSettings.Instance.Get<string>(GrepSettings.Key.CurrentCulture));
                AppTheme.Instance.Initialize();

                AppArgs = new CommandLineArgs(Environment.CommandLine);

                if (AppArgs.WarmUp)
                {
                    MainWindow = new MainForm(false);
                    MainWindow.Loaded += MainWindow_Loaded;
                }
                else if (AppArgs.ShowHelp)
                {
                    MainWindow = new HelpWindow(CommandLineArgs.GetHelpString(), AppArgs.InvalidArgument);
                }
                else
                {
                    AppArgs.ApplyArgs();
                }

                if (MainWindow == null)
                {
                    MainWindow = new MainForm();
                    Utils.DeleteTempFolder();
                    Utils.DeleteUndoFolder();
                }

                MainWindow.Show();

                if (!string.IsNullOrEmpty(AppArgs.Script) && MainWindow.DataContext != null)
                {
                    if (MainWindow is MainForm mainView)
                    {
                        mainView.ViewModel.RunScriptCommand.Execute(AppArgs.Script);
                    }
                }
                else if (AppArgs.ExecuteSearch && MainWindow.DataContext != null)
                {
                    if (MainWindow is MainForm mainView)
                    {
                        mainView.ViewModel.SearchCommand.Execute(null);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failure in application startup");
                MessageBox.Show(Localization.Properties.Resources.MessageBox_SomethingBrokeDownInDnGrep + LogDir,
                    Localization.Properties.Resources.MessageBox_DnGrep,
                    MessageBoxButton.OK, MessageBoxImage.Error,
                    MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
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
                Utils.DeleteUndoFolder();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failure in application exit");
                MessageBox.Show(Localization.Properties.Resources.MessageBox_SomethingBrokeDownInDnGrep + LogDir,
                    Localization.Properties.Resources.MessageBox_DnGrep,
                    MessageBoxButton.OK, MessageBoxImage.Error,
                    MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
            }
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            logger.Error(e.Exception, "Unhandled exception caught");
            MessageBox.Show(Localization.Properties.Resources.MessageBox_SomethingBrokeDownInDnGrep + LogDir,
                    Localization.Properties.Resources.MessageBox_DnGrep,
                    MessageBoxButton.OK, MessageBoxImage.Error,
                    MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
            e.Handled = true;
        }
    }
}
