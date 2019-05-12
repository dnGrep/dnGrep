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
    }
}
