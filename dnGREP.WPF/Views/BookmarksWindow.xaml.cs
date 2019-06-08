using System;
using System.Windows;
using System.Windows.Input;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for BookmarksWindow.xaml
    /// </summary>
    public partial class BookmarksWindow : ThemedWindow
    {
        public event EventHandler UseBookmark;

        public BookmarksWindow(Action<string, string, string> clearStar)
        {
            InitializeComponent();

            ViewModel = new BookmarkListViewModel(this, clearStar);
            DataContext = ViewModel;
        }

        public BookmarkListViewModel ViewModel { get; private set; }

        private void UseButton_Click(object sender, RoutedEventArgs e)
        {
            UseBookmark?.Invoke(this, EventArgs.Empty);
            Close();
        }

        private void DataGridRow_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            UseBookmark?.Invoke(this, EventArgs.Empty);
            Close();
        }
    }
}
