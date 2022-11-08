using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Alphaleonis.Win32.Filesystem;
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
            };
        }

        public string DestinationPath { get; private set; }

        public string SourcePath { get; set; }

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

                string destPath = Path.Combine(Path.GetDirectoryName(SourcePath), txtName.Text);
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

        public class RenameViewModel : CultureAwareViewModel
        {
            public RenameViewModel()
            {
                ApplicationFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ApplicationFontFamily);
                DialogFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.DialogFontSize);
                DialogWidth = DialogFontSize * 30.0;
            }

            private string applicationFontFamily;
            public string ApplicationFontFamily
            {
                get { return applicationFontFamily; }
                set
                {
                    if (applicationFontFamily == value)
                        return;

                    applicationFontFamily = value;
                    base.OnPropertyChanged(nameof(ApplicationFontFamily));
                }
            }

            private double dialogfontSize;
            public double DialogFontSize
            {
                get { return dialogfontSize; }
                set
                {
                    if (dialogfontSize == value)
                        return;

                    dialogfontSize = value;
                    base.OnPropertyChanged(nameof(DialogFontSize));
                }
            }

            private double dialogWidth;
            public double DialogWidth
            {
                get { return dialogWidth; }
                set
                {
                    if (dialogWidth == value)
                        return;

                    dialogWidth = value;
                    base.OnPropertyChanged(nameof(DialogWidth));
                }
            }
        }

    }
}
