using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using dnGREP.Common;
using Microsoft.Xaml.Behaviors;

namespace dnGREP.WPF
{
    public class DataGridColumnOrderBehavior : Behavior<DataGrid>
    {
        // An Dependency Property to pass the Name of the Setting, where the result is saved.
        public static readonly DependencyProperty SettingProperty =
            DependencyProperty.RegisterAttached(nameof(SettingName),
                typeof(string),
                typeof(DataGridColumnOrderBehavior));

        public string SettingName
        {
            get
            {
                return (string)GetValue(SettingProperty);
            }
            set
            {
                SetValue(SettingProperty, value);
            }
        }

        protected override void OnAttached()
        {
            // Load the indexes to DataGrid on Loaded
            AssociatedObject.Loaded += (sender, e) =>
            {
                if (sender is DataGrid grid)
                {
                    // Get the previously saved result
                    var displayIndexes = GrepSettings.Instance.Get<string>(SettingName).Split(',');
                    if (displayIndexes.Length == grid.Columns.Count)
                    {
                        for (int idx = 0; idx < grid.Columns.Count; idx++)
                        {
                            if (int.TryParse(displayIndexes[idx], out int index))
                                grid.Columns[idx].DisplayIndex = index;
                        }
                    }
                }
            };

            //Save the Values when a column is reordered.
            AssociatedObject.ColumnReordered += (sender, e) =>
            {
                if (sender is DataGrid grid)
                {
                    List<int> displayIndexes = new();
                    for (int idx = 0; idx < grid.Columns.Count; idx++)
                    {
                        displayIndexes.Add(grid.Columns[idx].DisplayIndex);
                    }

                    //Save the result to settings.
                    GrepSettings.Instance.Set(SettingName, string.Join(",", displayIndexes));
                }
            };
        }
    }
}
