using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using dnGREP.Common;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for TestPattern.xaml
    /// </summary>
    public partial class TestPattern : ThemedWindow
    {
        private readonly TestPatternViewModel inputData = new();

        public TestPattern()
        {
            InitializeComponent();
            DataContext = inputData;

            Loaded += (s, e) => { TextBoxCommands.BindCommandsToWindow(this); };
        }

        private void FormKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            inputData.SaveSettings();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnCopyFile_Click(object sender, RoutedEventArgs e)
        {
            NativeMethods.SetClipboardText(inputData.ReplaceOutputText);
        }
    }
}
