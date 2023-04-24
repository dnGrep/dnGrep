using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using dnGREP.Common;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for RenameWindow.xaml
    /// </summary>
    public partial class RenameWindow : ThemedWindow
    {
        public RenameWindow()
        {
            InitializeComponent();

            btnOK.IsEnabled = false;

            DataContext = new RenameViewModel();

            Loaded += (s, e) => 
            {
                btnOK.IsEnabled = false;
                txtName.Text = Path.GetFileName(SourcePath);
                txtName.TextChanged += Name_TextChanged;

                TextBoxCommands.BindCommandsToWindow(this);
            };
        }

        public string DestinationPath { get; private set; } = string.Empty;

        public string SourcePath { get; set; } = string.Empty;

        private void Name_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnOK.IsEnabled = false;
            txtError.Text = string.Empty;
            DestinationPath = string.Empty;

            if (!string.IsNullOrWhiteSpace(txtName.Text))
            {
                if (txtName.Text.Where(c => Path.GetInvalidFileNameChars().Contains(c)).Any())
                {
                    txtError.Text = dnGREP.Localization.Properties.Resources.Rename_FileNameContainsInvalidCharacters;
                    return;
                }

                string destPath = Path.Combine(Path.GetDirectoryName(SourcePath) ?? string.Empty, txtName.Text);
                if (File.Exists(destPath))
                {
                    txtError.Text = dnGREP.Localization.Properties.Resources.Rename_FileNameAlreadyExistsInThisDirectory;
                    return;
                }

                DestinationPath = destPath;
                btnOK.IsEnabled = true;
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = !string.IsNullOrWhiteSpace(DestinationPath);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DestinationPath = string.Empty;
            DialogResult = false;
        }

        public partial class RenameViewModel : CultureAwareViewModel
        {
            public RenameViewModel()
            {
                ApplicationFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ApplicationFontFamily);
                DialogFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.DialogFontSize);
                DialogWidth = DialogFontSize * 30.0;
            }

            [ObservableProperty]
            private string applicationFontFamily = SystemFonts.MessageFontFamily.Source;

            [ObservableProperty]
            private double dialogFontSize;

            [ObservableProperty]
            private double dialogWidth;
        }

    }
}
