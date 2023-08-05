using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using dnGREP.Common.UI;

namespace dnGREP.WPF.UserControls
{
    public interface ITreeItem
    {
        bool IsExpanded { get; }
        bool IsSelected { get; set; }

        int Level { get; }

        IEnumerable<ITreeItem> Children { get; }
    }


    public class MultiSelectTreeView : TreeView
    {
        public MultiSelectTreeView()
        {
            GotFocus += OnTreeViewItemGotFocus;
            PreviewMouseDown += OnTreeViewItemPreviewMouseDown;
            PreviewMouseUp += OnTreeViewItemPreviewMouseUp;
        }

        private static TreeViewItem? _selectTreeViewItemOnMouseUp;

        public static readonly DependencyProperty IsItemSelectedProperty =
            DependencyProperty.RegisterAttached("IsItemSelected",
                typeof(bool),
                typeof(MultiSelectTreeView),
                new PropertyMetadata(false, OnIsItemSelectedPropertyChanged));

        public static bool GetIsItemSelected(TreeViewItem element)
        {
            return (bool)element.GetValue(IsItemSelectedProperty);
        }

        public static void SetIsItemSelected(TreeViewItem element, bool value)
        {
            if (element == null) return;

            element.SetValue(IsItemSelectedProperty, value);
        }

        private static void OnIsItemSelectedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var treeViewItem = d as TreeViewItem;
            var treeView = FindTreeView(treeViewItem);
            if (treeViewItem != null && treeView != null)
            {
                var selectedItems = GetSelectedItems(treeView);
                if (selectedItems == null || selectedItems.Count == 0)
                {
                    SetStartItem(treeView, null);
                }
            }
        }

        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.RegisterAttached("SelectedItems",
                typeof(IList), typeof(MultiSelectTreeView));

        public static IList GetSelectedItems(TreeView element)
        {
            return (IList)element.GetValue(SelectedItemsProperty);
        }

        public static void SetSelectedItems(TreeView element, IList value)
        {
            element.SetValue(SelectedItemsProperty, value);
        }


        private static readonly DependencyProperty StartItemProperty =
            DependencyProperty.RegisterAttached("StartItem",
                typeof(TreeViewItem), typeof(MultiSelectTreeView));


        private static TreeViewItem? GetStartItem(TreeView element)
        {
            return (TreeViewItem?)element.GetValue(StartItemProperty);
        }

        private static void SetStartItem(TreeView element, TreeViewItem? value)
        {
            element.SetValue(StartItemProperty, value);
        }

        public TreeViewItem? StartTreeViewItem => GetStartItem(this);

        public void ClearStartTreeViewItem()
        {
            SetStartItem(this, null);
        }

        public bool MultiSelectRootLevelOnly { get; set; }

        private static void OnTreeViewItemGotFocus(object sender, RoutedEventArgs e)
        {
            _selectTreeViewItemOnMouseUp = null;
            if (e.OriginalSource is TreeView) return;

            if (sender is MultiSelectTreeView treeView)
            {
                var treeViewItem = FindTreeViewItem(e.OriginalSource as DependencyObject);
                if (treeViewItem != null)
                {
                    if ((Mouse.LeftButton == MouseButtonState.Pressed ||
                         Mouse.RightButton == MouseButtonState.Pressed ||
                         Mouse.MiddleButton == MouseButtonState.Pressed) &&
                        GetIsItemSelected(treeViewItem) && Keyboard.Modifiers != ModifierKeys.Control)
                    {
                        _selectTreeViewItemOnMouseUp = treeViewItem;
                        return;
                    }

                    MouseButton mouseButton = (Mouse.LeftButton == MouseButtonState.Pressed) ? MouseButton.Left :
                         (Mouse.RightButton == MouseButtonState.Pressed) ? MouseButton.Right :
                         (Mouse.MiddleButton == MouseButtonState.Pressed) ? MouseButton.Middle : (MouseButton)(-1);

                    SelectItems(treeViewItem, treeView, mouseButton);
                }
            }
        }

        private static void SelectItems(TreeViewItem treeViewItem, MultiSelectTreeView treeView, MouseButton mouseButton)
        {
            if (treeViewItem != null && treeView != null)
            {
                // if a right click is on a selected node, do not change the selection,
                // just allow the context menu to open
                // don't use treeViewItem.IsSelected here because it reflects the
                // default treeview behavior, not as modified for multiselect
                if (mouseButton == MouseButton.Right && GetIsItemSelected(treeViewItem))
                {
                    return;
                }

                bool isFunctionKeyDown = Keyboard.IsKeyDown(Key.F1) || Keyboard.IsKeyDown(Key.F2) || Keyboard.IsKeyDown(Key.F3) ||
                    Keyboard.IsKeyDown(Key.F4) || Keyboard.IsKeyDown(Key.F5) || Keyboard.IsKeyDown(Key.F6) ||
                    Keyboard.IsKeyDown(Key.F7) || Keyboard.IsKeyDown(Key.F8) || Keyboard.IsKeyDown(Key.F9) ||
                    Keyboard.IsKeyDown(Key.F10) || Keyboard.IsKeyDown(Key.F11) || Keyboard.IsKeyDown(Key.F12);

                if (treeView.MultiSelectRootLevelOnly)
                {
                    bool isRoot = ItemsControl.ItemsControlFromItemContainer(treeViewItem) == treeView;
                    if (isRoot)
                    {
                        treeView.DeselectAllChildItems();

                        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control | ModifierKeys.Shift) && !isFunctionKeyDown)
                        {
                            SelectMultipleItemsContinuously(treeView, treeViewItem, true);
                        }
                        else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && !isFunctionKeyDown)
                        {
                            SelectMultipleItemsRandomly(treeView, treeViewItem);
                        }
                        else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && !isFunctionKeyDown)
                        {
                            SelectMultipleItemsContinuously(treeView, treeViewItem);
                        }
                        else
                        {
                            SelectSingleItem(treeView, treeViewItem);
                        }
                    }
                    else
                    {
                        SelectSingleItem(treeView, treeViewItem);
                    }
                }
                else
                {
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control | ModifierKeys.Shift) && !isFunctionKeyDown)
                    {
                        SelectMultipleItemsContinuously(treeView, treeViewItem, true);
                    }
                    else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && !isFunctionKeyDown)
                    {
                        SelectMultipleItemsRandomly(treeView, treeViewItem);
                    }
                    else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && !isFunctionKeyDown)
                    {
                        SelectMultipleItemsContinuously(treeView, treeViewItem);
                    }
                    else
                    {
                        SelectSingleItem(treeView, treeViewItem);
                    }
                }
            }
        }

        private static void OnTreeViewItemPreviewMouseDown(object sender, MouseEventArgs e)
        {
            var treeView = FindTreeView(e.OriginalSource as DependencyObject);
            var treeViewItem = FindTreeViewItem(e.OriginalSource as DependencyObject);
            var scrollViewer = FindScrollViewer(e.OriginalSource as DependencyObject);

            if (treeViewItem != null && treeViewItem.IsFocused)
            {
                OnTreeViewItemGotFocus(sender, e);
            }
            else if (treeViewItem != null && e.MiddleButton == MouseButtonState.Pressed)
            {
                treeViewItem.Focus();
                OnTreeViewItemGotFocus(sender, e);
            }

            // if the user clicks on the empty space below the tree items, deselect all items
            if (treeViewItem == null && scrollViewer == null && treeView != null)
            {
                // it is possible to click *between* tree view items, so hit test a point
                // below the current point to see if there is a tree view item just below it
                var pt = e.GetPosition(treeView);
                pt.Offset(0, 10);
                var hitTest = VisualTreeHelper.HitTest(treeView, pt);
                if (hitTest != null && hitTest.VisualHit != null)
                {
                    treeViewItem = FindAncestor<TreeViewItem>(hitTest.VisualHit);
                    if (treeViewItem == null)
                    {
                        treeView.DeselectAllItems();
                    }
                }
            }
        }

        private static void OnTreeViewItemPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is MultiSelectTreeView treeView)
            {
                var treeViewItem = FindTreeViewItem(e.OriginalSource as DependencyObject);

                if (treeViewItem != null && treeViewItem == _selectTreeViewItemOnMouseUp)
                {
                    SelectItems(treeViewItem, treeView, e.ChangedButton);
                }
            }
        }

        private static TreeViewItem? FindTreeViewItem(DependencyObject? dependencyObject)
        {
            if (dependencyObject == null)
                return null;

            if (dependencyObject is TreeViewItem treeViewItem)
                return treeViewItem;

            return dependencyObject?.TryFindParent<TreeViewItem>();
        }

        private static MultiSelectTreeView? FindTreeView(DependencyObject? dependencyObject)
        {
            if (dependencyObject == null)
                return null;

            if (dependencyObject is MultiSelectTreeView treeView)
                return treeView;

            return dependencyObject?.TryFindParent<MultiSelectTreeView>();
        }

        private static ScrollViewer? FindScrollViewer(DependencyObject? dependencyObject)
        {
            if (dependencyObject == null)
                return null;

            if (dependencyObject is ScrollViewer scrollViewer)
                return scrollViewer;

            return dependencyObject?.TryFindParent<ScrollViewer>();
        }

        private static void SelectSingleItem(MultiSelectTreeView treeView, TreeViewItem treeViewItem)
        {
            // first deselect all items
            treeView.DeselectAllItems();
            SetIsItemSelected(treeViewItem, true);
            SetStartItem(treeView, treeViewItem);
        }

        public void DeselectAllItems()
        {
            ICollection<ITreeItem> allItems = new List<ITreeItem>();
            GetAllItems(this, allItems);

            foreach (ITreeItem item in allItems)
            {
                item.IsSelected = false;
            }
        }

        public void DeselectAllChildItems()
        {
            ICollection<ITreeItem> allItems = new List<ITreeItem>();
            GetAllItems(this, allItems);

            foreach (ITreeItem item in allItems.Where(i => i.Level > 0))
            {
                item.IsSelected = false;
            }
        }

        private static void SelectMultipleItemsRandomly(MultiSelectTreeView treeView, TreeViewItem treeViewItem)
        {
            SetIsItemSelected(treeViewItem, !GetIsItemSelected(treeViewItem));
            if (GetStartItem(treeView) == null || Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (GetIsItemSelected(treeViewItem))
                {
                    SetStartItem(treeView, treeViewItem);
                }
            }
            else if (GetSelectedItems(treeView).Count == 0)
            {
                SetStartItem(treeView, null);
            }
        }

#pragma warning disable IDE0075
        private static void SelectMultipleItemsContinuously(MultiSelectTreeView treeView, TreeViewItem treeViewItem, bool shiftControl = false)
        {
            TreeViewItem? startItem = GetStartItem(treeView);
            if (startItem != null)
            {
                if (startItem == treeViewItem)
                {
                    SelectSingleItem(treeView, treeViewItem);
                    return;
                }

                bool msRootOnly = treeView.MultiSelectRootLevelOnly;

                ICollection<ITreeItem> allItems = new List<ITreeItem>();
                GetAllItems(treeView, allItems);
                bool isBetween = false;
                foreach (var item in allItems)
                {
                    bool isRoot = item.Level == 0;

                    if (item == treeViewItem.DataContext || item == startItem.DataContext)
                    {
                        // toggle to true if first element is found and
                        // back to false if last element is found
                        isBetween = !isBetween;

                        // set boundary element
                        item.IsSelected = msRootOnly ? isRoot : true;
                        continue;
                    }

                    if (isBetween)
                    {
                        item.IsSelected = msRootOnly ? isRoot : true;
                        continue;
                    }

                    if (!shiftControl)
                    {
                        item.IsSelected = false;
                    }
                }
            }
        }
#pragma warning restore IDE0075

        private static void GetAllItems(TreeView treeView, ICollection<ITreeItem> allItems)
        {
            if (treeView != null && treeView.Items != null)
            {
                foreach (var treeItem in treeView.Items.Cast<ITreeItem>())
                {
                    allItems.Add(treeItem);
                    GetAllItems(treeItem.Children, allItems);
                }
            }
        }

        private static void GetAllItems(IEnumerable<ITreeItem> items, ICollection<ITreeItem> allItems)
        {
            foreach (var treeItem in items)
            {
                allItems.Add(treeItem);
                GetAllItems(treeItem.Children, allItems);
            }
        }

        private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
        {
            // Search the VisualTree for specified type
            while (current != null)
            {
                if (current is T item)
                {
                    return item;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }

    public class MyVirtualizingStackPanel : VirtualizingStackPanel
    {
        /// <summary>
        /// Expose BringIndexIntoView.
        /// </summary>
        public void BringIntoView(int index)
        {
            this.BringIndexIntoView(index);
        }
    }
}