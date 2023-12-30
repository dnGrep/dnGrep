using System.Windows;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for CustomEditorWindow.xaml
    /// </summary>
    public partial class CustomEditorWindow : ThemedWindow
    {
        public CustomEditorWindow()
        {
            InitializeComponent();

            SourceInitialized += (s, e) =>
            {
                MinWidth = ActualWidth;
                MinHeight = ActualHeight;
            };

            Loaded += (s, e) => { TextBoxCommands.BindCommandsToWindow(this); };
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

    }
}
