using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using dnGREP.Common;
using dnGREP.Common.UI;
using dnGREP.WPF.Properties;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for BookmarksWindow.xaml
    /// </summary>
    public partial class BookmarksWindow : ThemedWindow
    {
        public event EventHandler? UseBookmark;
        private WindowState storedWindowState = WindowState.Normal;

        public BookmarksWindow(Action<Bookmark> clearStar)
        {
            InitializeComponent();

            ViewModel = new BookmarkListViewModel(this, clearStar);
            DataContext = ViewModel;
            Closing += BookmarksWindow_Closing;
            ViewModel.SetFocus += ViewModel_SetFocus;

            // fix for placements on monitors with different DPIs:
            // initial placement on the primary monitor, then move it
            // to the saved location when the window is loaded
            Left = 0;
            Top = 0;
            Width = LayoutProperties.BookmarkWindowBounds.Width;
            Height = LayoutProperties.BookmarkWindowBounds.Height;
            WindowState = WindowState.Normal;

            Rect windowBounds = new(
                LayoutProperties.BookmarkWindowBounds.X,
                LayoutProperties.BookmarkWindowBounds.Y,
                LayoutProperties.BookmarkWindowBounds.Width,
                LayoutProperties.BookmarkWindowBounds.Height);

            Loaded += (s, e) =>
            {
                if (windowBounds.IsOnScreen())
                {
                    // setting Left and Top does not work when
                    // moving to a monitor with a different DPI
                    // than the primary monitor
                    this.MoveWindow(
                        LayoutProperties.BookmarkWindowBounds.X,
                        LayoutProperties.BookmarkWindowBounds.Y);
                    WindowState = LayoutProperties.BookmarkWindowState;
                }
                else
                {
                    this.CenterWindow();
                }

                this.ConstrainToScreen();
            };

            StateChanged += (s, e) => storedWindowState = WindowState;
            Application.Current.MainWindow.IsVisibleChanged += MainWindow_IsVisibleChanged;
        }

        private void MainWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Window mainWindow)
            {
                if (mainWindow.IsVisible)
                {
                    Show();
                    WindowState = storedWindowState;
                }
                else
                {
                    Hide();
                }
            }
        }

        private void SaveSettings()
        {
            if (WindowState == WindowState.Normal)
            {
                LayoutProperties.BookmarkWindowBounds = new Rect(
                    Left,
                    Top,
                    ActualWidth,
                    ActualHeight);
            }
            else
            {
                LayoutProperties.BookmarkWindowBounds = RestoreBounds;
            }

            LayoutProperties.BookmarkWindowState = WindowState.Normal;
            if (WindowState == WindowState.Maximized)
                LayoutProperties.BookmarkWindowState = WindowState.Maximized;

            LayoutProperties.Save();
        }

        internal void ApplicationExit()
        {
            SaveSettings();
            ViewModel.BookmarksWindow_Hiding();
            Closing -= BookmarksWindow_Closing;
            Close();
        }

        private void BookmarksWindow_Closing(object? sender, CancelEventArgs e)
        {
            ViewModel.BookmarksWindow_Hiding();
            Hide();
            e.Cancel = true;
        }

        public BookmarkListViewModel ViewModel { get; private set; }

        private void UseButton_Click(object? sender, RoutedEventArgs e)
        {
            UseBookmark?.Invoke(this, EventArgs.Empty);
            if (!ViewModel.IsPinned)
            {
                ViewModel.BookmarksWindow_Hiding();
                Hide();
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    Application.Current.MainWindow.Activate();
                    Application.Current.MainWindow.Focus();
                }));
            }
        }

        private void DataGridRow_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            UseBookmark?.Invoke(this, EventArgs.Empty);
            if (!ViewModel.IsPinned)
            {
                ViewModel.BookmarksWindow_Hiding();
                Hide();
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    Application.Current.MainWindow.Activate();
                    Application.Current.MainWindow.Focus();
                }));
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.BookmarksWindow_Hiding();
            Hide();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ViewModel.BookmarksWindow_Hiding();
                Hide();
            }
        }

        private void ViewModel_SetFocus(object? sender, DataEventArgs<int> e)
        {
            dataGrid.Focus();
            dataGrid.UpdateLayout();
            dataGrid.ScrollIntoView(dataGrid.Items[e.Data]);
            var row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromIndex(e.Data);
            if (row != null)
            {
                row.Focus();
                DataGridCellsPresenter? presenter = row.GetVisualChild<DataGridCellsPresenter>();
                if (presenter != null)
                {
                    DataGridCell cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(1);
                    if (cell != null)
                    {
                        dataGrid.ScrollIntoView(row, dataGrid.Columns[1]);
                        cell.Focus();
                    }
                }
            }
        }
    }
}
