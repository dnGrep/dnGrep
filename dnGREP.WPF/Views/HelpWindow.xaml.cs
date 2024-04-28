using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using dnGREP.Common;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for HelpWindow.xaml
    /// </summary>
    public partial class HelpWindow : ThemedWindow
    {
        public HelpWindow(string helpString, bool showWarning, string commandLine)
        {
            InitializeComponent();

            HelpViewModel viewModel = new();
            viewModel.HelpText = helpString;
            viewModel.ShowWarning = showWarning;
            viewModel.CommandLine = commandLine;
            DataContext = viewModel;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    internal partial class HelpViewModel : CultureAwareViewModel
    {
        public HelpViewModel()
        {
            ApplicationFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ApplicationFontFamily);
            DialogFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.DialogFontSize);
        }

        [ObservableProperty]
        private string applicationFontFamily = SystemFonts.MessageFontFamily.Source;

        [ObservableProperty]
        private double dialogFontSize;

        [ObservableProperty]
        private string helpText = string.Empty;

        [ObservableProperty]
        private bool showWarning;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasCommandLine))]
        private string commandLine = string.Empty;

        public bool HasCommandLine => ShowWarning && !string.IsNullOrEmpty(CommandLine);

    }
}
