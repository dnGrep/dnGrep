using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using dnGREP.Common;
using Microsoft.Xaml.Behaviors;

namespace dnGREP.WPF
{
    public class DataGridColumnWidthBehavior : Behavior<DataGrid>
    {
        // An Dependency Property to pass the Name of the Setting, where the result is saved.
        public static readonly DependencyProperty SettingProperty =
            DependencyProperty.RegisterAttached(nameof(SettingName),
                typeof(string),
                typeof(DataGridColumnWidthBehavior));

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
                    // Only supporting Auto and Absolute widths
                    var columnWidths = GrepSettings.Instance.Get<string>(SettingName).Split(',');
                    if (columnWidths.Length == grid.Columns.Count)
                    {
                        for (int idx = 0; idx < grid.Columns.Count; idx++)
                        {
                            if (columnWidths[idx].Equals("Auto", StringComparison.Ordinal))
                            {
                                grid.Columns[idx].Width = new(1.0, DataGridLengthUnitType.Auto);
                            }
                            else if (double.TryParse(columnWidths[idx], out double width))
                            {
                                grid.Columns[idx].Width = new(width);
                            }
                        }
                    }
                }
            };

            // Save the values when the grid is hidden (closed)
            AssociatedObject.IsVisibleChanged += (sender, e) =>
            {
                if (sender is DataGrid grid && !grid.IsVisible)
                {
                    // Only supporting Auto and Absolute widths
                    List<string> displayWidths = [];
                    for (int idx = 0; idx < grid.Columns.Count; idx++)
                    {
                        if (grid.Columns[idx].Width.IsAuto)
                        {
                            displayWidths.Add("Auto");
                        }
                        else if (grid.Columns[idx].Width.IsAbsolute)
                        {
                            displayWidths.Add(grid.Columns[idx].Width.Value.ToString("0.0", CultureInfo.InvariantCulture));
                        }
                    }

                    //Save the result to settings.
                    GrepSettings.Instance.Set(SettingName, string.Join(",", displayWidths));
                }
            };
        }
    }
}
