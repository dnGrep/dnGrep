namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for StringMapWindow.xaml
    /// </summary>
    public partial class StringMapWindow : ThemedWindow
    {
        private StringMapViewModel viewModel = new();
   
        public StringMapWindow()
        {
            InitializeComponent();

            viewModel.RequestClose += (s, e) => Close();
            DataContext = viewModel;
        }
    }
}
