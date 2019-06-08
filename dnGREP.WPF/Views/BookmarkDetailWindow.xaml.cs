using System.Windows;

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
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
