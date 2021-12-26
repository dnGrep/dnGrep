using System.Windows;
using dnGREP.Localization;

namespace dnGREP.TestLocalizedStrings
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static MainWindow()
        {
            ResourceManagerEx.Initialize();
            TranslationSource.Instance.SetCulture("en");
        }

        private readonly TestStringsViewModel vm = new TestStringsViewModel();

        public MainWindow()
        {
            InitializeComponent();

            vm.InitializeStrings();
            DataContext = vm;
        }
    }
}
