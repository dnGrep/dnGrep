using System;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Threading;
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

        /// <summary>The pipe name.</summary>
        private const string UniqueEventName = "{C5475DAC-0582-42DE-B2B8-C17DFF29988A}";
        private const string UniquePipeName = "{C5475DAC-0582-42DE-B2B8-C17DFF29988A}";

        /// <summary>The unique mutex name.</summary>
        private const string UniqueMutexName = "{EB56AF15-5E08-4EEF-B8C4-18749C927C78}";

        /// <summary>The event wait handle.</summary>
        private EventWaitHandle? eventWaitHandle;

        /// <summary>The mutex.</summary>
        private Mutex? mutex;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                LogDir = Path.Combine(Utils.GetDataFolderPath(), "logs");
                GlobalDiagnosticsContext.Set("logDir", LogDir);

                AppArgs = new CommandLineArgs(Environment.CommandLine);

                if (GrepSettings.Instance.Get<bool>(GrepSettings.Key.IsSingletonInstance) &&
                    !ConfigureSingetonInstance(AppArgs))
                {
                    // Terminate this instance.
                    Shutdown();
                    return;
                }

                Assembly? thisAssembly = Assembly.GetAssembly(typeof(App));
                if (thisAssembly != null)
                {
                    var path = Path.GetDirectoryName(thisAssembly.Location) ?? string.Empty;
                    if (Environment.Is64BitProcess)
                        SevenZip.SevenZipBase.SetLibraryPath(Path.Combine(path, @"7z64.dll"));
                    else
                        SevenZip.SevenZipBase.SetLibraryPath(Path.Combine(path, @"7z32.dll"));
                }

                ResourceManagerEx.Initialize();
                TranslationSource.Instance.SetCulture(GrepSettings.Instance.Get<string>(GrepSettings.Key.CurrentCulture));
                AppTheme.Instance.Initialize();

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

        private bool ConfigureSingetonInstance(CommandLineArgs args)
        {
            mutex = new Mutex(true, UniqueMutexName, out bool isOwned);
            //eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, UniqueEventName);

            if (isOwned)
            {
                // Spawn a thread which will be waiting for our event
                var thread = new Thread(
                    () =>
                    {
                        while (true)
                        {
                            try
                            {
                                using (NamedPipeClientStream pipeClientStream = new(".", 
                                    UniquePipeName, PipeDirection.In, PipeOptions.CurrentUserOnly))
                                {
                                    logger.Info("Am first, calling client pipe connect");
                                    // Wait until the pipe is available.
                                    pipeClientStream.Connect();

                                    string path = string.Empty;
                                    using (StreamReader sr = new(pipeClientStream))
                                    {
                                        string? temp;
                                        while ((temp = sr.ReadLine()) != null)
                                        {
                                            path += temp;
                                        }
                                    }

                                    logger.Info("Am first, recieved message: " + path);
                                    Current.Dispatcher.BeginInvoke(
                                        () => ((MainForm)Current.MainWindow).BringToForeground(path));
                                }
                            }
                            catch (IOException ex)
                            {
                                logger.Info("Caught exception on client: " + ex.Message);
                            }
                        }

                        //logger.Info("Am first, waiting on event handle");
                        //while (eventWaitHandle.WaitOne())
                        //{
                        //    Current.Dispatcher.BeginInvoke(
                        //        () => ((MainForm)Current.MainWindow).BringToForeground(""));
                        //}
                    });

                // It is important mark it as background otherwise it will prevent app from exiting.
                thread.IsBackground = true;
                thread.Start();
                return true;
            }

            //logger.Info("Am second, calling eventWaitHandle.Set()");
            //// Notify other instance so it could bring itself to foreground.
            //eventWaitHandle.Set();

            logger.Info("Am second, starting named pipe server to send message");

            using (NamedPipeServerStream namedPipeServer = new NamedPipeServerStream(
                UniquePipeName, PipeDirection.Out, NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Message, PipeOptions.CurrentUserOnly))
            {
                namedPipeServer.WaitForConnection();
                try
                {
                    string path = string.Empty;
                    if (!string.IsNullOrEmpty(args.SearchPath))
                    {
                        path = args.SearchPath;
                    }

                    // Read user input and send that to the client process.
                    using (StreamWriter sw = new(namedPipeServer))
                    {
                        sw.AutoFlush = true;
                        sw.WriteLine(path);
                        Thread.Sleep(200);
                    }
                }
                // Catch the IOException that is raised if the pipe is broken
                // or disconnected.
                catch (IOException ex)
                {
                    logger.Info("Caught exception on server: " + ex.Message);
                }
            }

            return false;
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
