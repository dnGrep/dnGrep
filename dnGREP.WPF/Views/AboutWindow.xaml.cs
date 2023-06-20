using System.Diagnostics;
using System.Windows.Navigation;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : ThemedWindow
    {
        public AboutWindow()
        {
            InitializeComponent();

            DataContext = new AboutViewModel();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = e.Uri.ToString(),
                UseShellExecute = true,
            };
            using var proc = Process.Start(startInfo);
        }
    }
}
