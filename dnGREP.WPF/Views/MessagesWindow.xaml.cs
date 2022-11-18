namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for MessagesWindow.xaml
    /// </summary>
    public partial class MessagesWindow : ThemedWindow
    {
        public MessagesWindow(MainViewModel vm)
        {
            InitializeComponent();

            DataContext = vm;
        }
    }
}
