using System.Windows;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for HelpWindow.xaml
    /// </summary>
    public partial class HelpWindow : ThemedWindow
    {
        public HelpWindow(string helpString, bool showWarning)
        {
            InitializeComponent();

            DataContext = new CultureAwareViewModel();

            WarningMsg.Visibility = showWarning ? Visibility.Visible : Visibility.Collapsed;
            HelpText.Text = helpString;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
