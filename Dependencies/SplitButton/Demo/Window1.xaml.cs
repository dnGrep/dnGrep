using System.Windows;
using Wpf.Controls;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
        }

        private void SplitButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Click event handler");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            test.IsContextMenuOpen = true;
        }
    }
}
