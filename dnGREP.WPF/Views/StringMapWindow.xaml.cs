using System;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
            viewModel.SetFocus += ViewModel_SetFocus;
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

        private void ViewModel_SetFocus(object? sender, DataEventArgs<GridLocation> e)
        {
            dataGrid.Focus();
            dataGrid.UpdateLayout();
            dataGrid.ScrollIntoView(dataGrid.Items[e.Data.Row]);
            var row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromIndex(e.Data.Row);
            if (row != null)
            {
                row.Focus();
                DataGridCellsPresenter? presenter = row.GetVisualChild<DataGridCellsPresenter>();
                if (presenter != null)
                {
                    DataGridCell cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(e.Data.Col);
                    if (cell != null)
                    {
                        dataGrid.ScrollIntoView(row, dataGrid.Columns[e.Data.Col]);
                        cell.Focus();
                    }
                }
            }
        }

        private void dataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (dataGrid.SelectedCells.Count > 0 && dataGrid.Columns.Count > 0)
            {
                // Get the first selected cell
                DataGridCellInfo cellInfo = dataGrid.SelectedCells[0];

                int colIndex = dataGrid.Columns.IndexOf(cellInfo.Column);

                // The 'Item' property of the cell is the data object for the entire row
                // Cast it to your specific data type (e.g., YourDataType)
                StringSubstitutionViewModel? selectedItem = cellInfo.Item as StringSubstitutionViewModel;

                viewModel.SelectedItem = selectedItem;
                viewModel.SelectedColumn = colIndex > -1 ? colIndex : 1;
            }
        }
    }

    public record GridLocation(int Row, int Col);
}
