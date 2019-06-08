using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

// modified from: https://github.com/cmyksvoll/MultiSelectTreeView

namespace dnGREP.WPF.UserControls
{
    public class MultiSelectTreeView : TreeView
    {
        public MultiSelectTreeView()
        {
            GotFocus += OnTreeViewItemGotFocus;
            PreviewMouseLeftButtonDown += OnTreeViewItemPreviewMouseDown;
            PreviewMouseLeftButtonUp += OnTreeViewItemPreviewMouseUp;
        }

        private static TreeViewItem _selectTreeViewItemOnMouseUp;

        public static readonly DependencyProperty IsItemSelectedProperty = DependencyProperty.RegisterAttached("IsItemSelected", typeof(Boolean), typeof(MultiSelectTreeView), new PropertyMetadata(false, OnIsItemSelectedPropertyChanged));

        public static bool GetIsItemSelected(TreeViewItem element)
        {
            return (bool)element.GetValue(IsItemSelectedProperty);
        }

        public static void SetIsItemSelected(TreeViewItem element, Boolean value)
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
                if (selectedItems != null)
                {
                    if (GetIsItemSelected(treeViewItem))
                    {
                        selectedItems.Insert(0, treeViewItem.Header);
                        treeView.RaiseEvent(new RoutedEventArgs(MultiSelectTreeView.SelectedItemsChangedEvent));
                    }
                    else
                    {
                        selectedItems.Remove(treeViewItem.Header);
                        treeView.RaiseEvent(new RoutedEventArgs(MultiSelectTreeView.SelectedItemsChangedEvent));
                    }
                }
            }
        }

        public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.RegisterAttached("SelectedItems", typeof(IList), typeof(MultiSelectTreeView));

        public static IList GetSelectedItems(TreeView element)
        {
            return (IList)element.GetValue(SelectedItemsProperty);
        }

        public static void SetSelectedItems(TreeView element, IList value)
        {
            element.SetValue(SelectedItemsProperty, value);
        }

        public static readonly RoutedEvent SelectedItemsChangedEvent =
            EventManager.RegisterRoutedEvent("SelectedItemsChangedEvent", RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(MultiSelectTreeView));

        public event RoutedEventHandler SelectedItemsChanged
        {
            add { AddHandler(SelectedItemsChangedEvent, value); }
            remove { RemoveHandler(SelectedItemsChangedEvent, value); }
        }


        private static readonly DependencyProperty StartItemProperty = DependencyProperty.RegisterAttached("StartItem", typeof(TreeViewItem), typeof(MultiSelectTreeView));


        private static TreeViewItem GetStartItem(TreeView element)
        {
            return (TreeViewItem)element.GetValue(StartItemProperty);
        }

        private static void SetStartItem(TreeView element, TreeViewItem value)
        {
            element.SetValue(StartItemProperty, value);
        }

        public bool MultiSelectRootLevelOnly { get; set; }

        private static void OnTreeViewItemGotFocus(object sender, RoutedEventArgs e)
        {
            _selectTreeViewItemOnMouseUp = null;

            if (e.OriginalSource is TreeView) return;
            if (Mouse.RightButton == MouseButtonState.Pressed) return;

            var treeViewItem = FindTreeViewItem(e.OriginalSource as DependencyObject);
            if (Mouse.LeftButton == MouseButtonState.Pressed && GetIsItemSelected(treeViewItem) && Keyboard.Modifiers != ModifierKeys.Control)
            {
                _selectTreeViewItemOnMouseUp = treeViewItem;
                return;
            }

            SelectItems(treeViewItem, sender as TreeView);
        }

        private static void SelectItems(TreeViewItem treeViewItem, TreeView treeView)
        {
            if (treeViewItem != null && treeView != null)
            {
                if (((MultiSelectTreeView)treeView).MultiSelectRootLevelOnly)
                {
                    bool isRoot = ItemsControl.ItemsControlFromItemContainer(treeViewItem) == treeView;
                    if (isRoot)
                    {
                        DeselectAllChildItems(treeView);

                        if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == (ModifierKeys.Control | ModifierKeys.Shift))
                        {
                            SelectMultipleItemsContinuously(treeView, treeViewItem, true);
                        }
                        else if (Keyboard.Modifiers == ModifierKeys.Control)
                        {
                            SelectMultipleItemsRandomly(treeView, treeViewItem);
                        }
                        else if (Keyboard.Modifiers == ModifierKeys.Shift)
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
                    if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == (ModifierKeys.Control | ModifierKeys.Shift))
                    {
                        SelectMultipleItemsContinuously(treeView, treeViewItem, true);
                    }
                    else if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        SelectMultipleItemsRandomly(treeView, treeViewItem);
                    }
                    else if (Keyboard.Modifiers == ModifierKeys.Shift)
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
                        DeSelectAllItems(treeView, null);
                    }
                }
            }
        }

        private static void OnTreeViewItemPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            var treeViewItem = FindTreeViewItem(e.OriginalSource as DependencyObject);

            if (treeViewItem == _selectTreeViewItemOnMouseUp)
            {
                SelectItems(treeViewItem, sender as TreeView);
            }
        }

        private static TreeViewItem FindTreeViewItem(DependencyObject dependencyObject)
        {
            if (!(dependencyObject is Visual || dependencyObject is Visual3D))
                return null;

            if (dependencyObject is TreeViewItem treeViewItem)
            {
                return treeViewItem;
            }

            return FindTreeViewItem(VisualTreeHelper.GetParent(dependencyObject));
        }

        private static ScrollViewer FindScrollViewer(DependencyObject dependencyObject)
        {
            if (!(dependencyObject is Visual || dependencyObject is Visual3D))
                return null;

            if (dependencyObject is ScrollViewer sv)
            {
                return sv;
            }

            return FindScrollViewer(VisualTreeHelper.GetParent(dependencyObject));
        }

        private static void SelectSingleItem(TreeView treeView, TreeViewItem treeViewItem)
        {
            // first deselect all items
            DeSelectAllItems(treeView, null);
            SetIsItemSelected(treeViewItem, true);
            SetStartItem(treeView, treeViewItem);
        }

        private static void DeselectAllChildItems(TreeView treeView)
        {
            if (treeView != null)
            {
                for (int i = 0; i < treeView.Items.Count; i++)
                {
                    if (treeView.ItemContainerGenerator.ContainerFromIndex(i) is TreeViewItem item)
                    {
                        // do not deselect the root items, 
                        DeSelectAllItems(null, item);
                    }
                }
            }
        }

        private static void DeSelectAllItems(TreeView treeView, TreeViewItem treeViewItem)
        {
            if (treeView != null)
            {
                for (int i = 0; i < treeView.Items.Count; i++)
                {
                    if (treeView.ItemContainerGenerator.ContainerFromIndex(i) is TreeViewItem item)
                    {
                        SetIsItemSelected(item, false);
                        DeSelectAllItems(null, item);
                    }
                }
            }
            else
            {
                for (int i = 0; i < treeViewItem.Items.Count; i++)
                {
                    if (treeViewItem.ItemContainerGenerator.ContainerFromIndex(i) is TreeViewItem item)
                    {
                        SetIsItemSelected(item, false);
                        DeSelectAllItems(null, item);
                    }
                }
            }
        }

        private static TreeView FindTreeView(DependencyObject dependencyObject)
        {
            if (!(dependencyObject is Visual || dependencyObject is Visual3D))
                return null;

            if (dependencyObject == null)
            {
                return null;
            }

            var treeView = dependencyObject as TreeView;

            return treeView ?? FindTreeView(VisualTreeHelper.GetParent(dependencyObject));
        }

        private static void SelectMultipleItemsRandomly(TreeView treeView, TreeViewItem treeViewItem)
        {
            SetIsItemSelected(treeViewItem, !GetIsItemSelected(treeViewItem));
            if (GetStartItem(treeView) == null || Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (GetIsItemSelected(treeViewItem))
                {
                    SetStartItem(treeView, treeViewItem);
                }
            }
            else
            {
                if (GetSelectedItems(treeView).Count == 0)
                {
                    SetStartItem(treeView, null);
                }
            }
        }

        internal static void SelectAll(TreeView treeView)
        {
            ICollection<TreeViewItem> allItems = new List<TreeViewItem>();
            GetAllItems(treeView, null, allItems);
            foreach (var item in allItems)
            {
                SetIsItemSelected(item, true);
            }
        }

        private static void SelectMultipleItemsContinuously(TreeView treeView, TreeViewItem treeViewItem, bool shiftControl = false)
        {
            TreeViewItem startItem = GetStartItem(treeView);
            if (startItem != null)
            {
                if (startItem == treeViewItem)
                {
                    SelectSingleItem(treeView, treeViewItem);
                    return;
                }

                bool msRootOnly = ((MultiSelectTreeView)treeView).MultiSelectRootLevelOnly;

                ICollection<TreeViewItem> allItems = new List<TreeViewItem>();
                GetAllItems(treeView, null, allItems);
                //DeSelectAllItems(treeView, null);
                bool isBetween = false;
                foreach (var item in allItems)
                {
                    bool isRoot = ItemsControl.ItemsControlFromItemContainer(item) == treeView;

                    if (item == treeViewItem || item == startItem)
                    {
                        // toggle to true if first element is found and
                        // back to false if last element is found
                        isBetween = !isBetween;

                        // set boundary element
                        SetIsItemSelected(item, msRootOnly ? isRoot : true);
                        continue;
                    }

                    if (isBetween)
                    {
                        SetIsItemSelected(item, msRootOnly ? isRoot : true);
                        continue;
                    }

                    if (!shiftControl)
                        SetIsItemSelected(item, false);
                }
            }
        }

        private static void GetAllItems(TreeView treeView, TreeViewItem treeViewItem, ICollection<TreeViewItem> allItems)
        {
            if (treeView != null)
            {
                for (int i = 0; i < treeView.Items.Count; i++)
                {
                    if (treeView.ItemContainerGenerator.ContainerFromIndex(i) is TreeViewItem item)
                    {
                        allItems.Add(item);
                        GetAllItems(null, item, allItems);
                    }
                }
            }
            else
            {
                for (int i = 0; i < treeViewItem.Items.Count; i++)
                {
                    if (treeViewItem.ItemContainerGenerator.ContainerFromIndex(i) is TreeViewItem item)
                    {
                        allItems.Add(item);
                        GetAllItems(null, item, allItems);
                    }
                }
            }
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            // Search the VisualTree for specified type
            while (current != null)
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}