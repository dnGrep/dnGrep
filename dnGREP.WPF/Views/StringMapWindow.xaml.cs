using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnGREP.Common;
using dnGREP.Common.UI;

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

            viewModel.RequestClose += (s, e) =>
            {
                DialogResult = true;
                Close();
            };
            DataContext = viewModel;
        }

        private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is DataGrid dataGrid &&
                dataGrid.CurrentCell.Column != null && dataGrid.CurrentCell.Item != null &&
                dataGrid.Columns[dataGrid.CurrentCell.Column.DisplayIndex]
                        .GetCellContent(dataGrid.CurrentCell.Item).Parent is DataGridCell cell &&
                cell.GetVisualChild<TextBlock>() is TextBlock textBlock)
            {
                if (e.Key == Key.C && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    NativeMethods.SetClipboardText(textBlock.Text);
                    e.Handled = true; // Set to true to prevent default handling
                }
            }
        }
    }
}
