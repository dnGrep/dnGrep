using System.Windows;
using System.Windows.Input;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for OptionsForm.xaml
    /// </summary>
    public partial class ReportOptionsWindow : ThemedWindow
    {
        private WindowState storedWindowState = WindowState.Normal;

        public ReportOptionsWindow(ReportOptionsViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
            viewModel.RequestClose += (s, e) => { DialogResult = true; Close(); };

            DiginesisHelpProvider.HelpNamespace = "https://github.com/dnGrep/dnGrep/wiki/";
            DiginesisHelpProvider.ShowHelp = true;

            StateChanged += (s, e) => storedWindowState = WindowState;
            Application.Current.MainWindow.IsVisibleChanged += MainWindow_IsVisibleChanged;
        }

        private void MainWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Window mainWindow)
            {
                if (mainWindow.IsVisible)
                {
                    Show();
                    WindowState = storedWindowState;
                }
                else
                {
                    Hide();
                }
            }
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!IsTextAllowed(text))
                    e.CancelCommand();
            }
            else
                e.CancelCommand();
        }

        private static bool IsTextAllowed(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                if (!int.TryParse(text, out _))
                    return false;
            }
            return true;
        }
    }
}
