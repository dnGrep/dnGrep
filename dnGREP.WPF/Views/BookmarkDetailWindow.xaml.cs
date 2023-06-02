using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for BookmarkDetailWindow.xaml
    /// </summary>
    public partial class BookmarkDetailWindow : ThemedWindow
    {
        public BookmarkDetailWindow()
        {
            InitializeComponent();

            SourceInitialized += (s, e) =>
            {
                MinWidth = ActualWidth;
                MinHeight = ActualHeight;
            };

            Loaded += (s, e) => { TextBoxCommands.BindCommandsToWindow(this); };
        }

        private void TextBoxFocus(object sender, RoutedEventArgs e)
        {
            if (e.Source is TextBox tb)
            {
                tb.SelectAll();
            }
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private static bool IsTextAllowed(string text)
        {
            //regex that matches allowed text
            return AllowedTextRegex().IsMatch(text);
        }

        private void TextBoxPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!IsTextAllowed(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        [GeneratedRegex("\\d+")]
        private static partial Regex AllowedTextRegex();
    }
}
