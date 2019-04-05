using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for BookmarksWindow.xaml
    /// </summary>
    public partial class BookmarksWindow : Window
    {
        public event EventHandler UseBookmark;

        public BookmarksWindow(Action<string, string, string> clearStar)
        {
            InitializeComponent();

            ViewModel = new BookmarkListViewModel(clearStar);
            DataContext = ViewModel;
        }

        public BookmarkListViewModel ViewModel { get; private set; }

        private void UseButton_Click(object sender, RoutedEventArgs e)
        {
            UseBookmark?.Invoke(this, EventArgs.Empty);
        }
    }
}
