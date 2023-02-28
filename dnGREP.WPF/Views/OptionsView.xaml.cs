using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for OptionsForm.xaml
    /// </summary>
    public partial class OptionsView : ThemedWindow
    {
        public OptionsView()
        {
            InitializeComponent();
            DiginesisHelpProvider.HelpNamespace = "https://github.com/dnGrep/dnGrep/wiki/";
            DiginesisHelpProvider.ShowHelp = true;
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
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.ToString());
        }
    }
}
