using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using dnGREP.Common;
using dnGREP.Common.UI;

namespace dnGREP.WPF.UserControls
{
    /// <summary>
    /// Interaction logic for ContextResultsTree.xaml
    /// </summary>
    public partial class ContextResultsTree : UserControl, INameScope
    {
        private bool inNextPrevious;
        private bool stickyScrollEnabled;

        public ContextResultsTree()
        {
            InitializeComponent();

            // used to map the editor menu items on the TextBlock context menu
            NameScope.SetNameScope(contextMenu, this);

            stickyScrollEnabled = GrepSearchResultsViewModel.StickyScrollEnabled;

            Loaded += (s, e) =>
            {
                contextRoot.Margin = new Thickness(0, 0, SystemParameters.VerticalScrollBarWidth, 0);

                if (resultsTree.treeView.Template.FindName("_tv_scrollviewer_", resultsTree.treeView) is ScrollViewer scrollViewer)
                {
                    scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
                }

                if (DataContext is GrepSearchResultsViewModel vm)
                {
                    vm.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(GrepSearchResultsViewModel.StickyScrollEnabled))
                        {
                            stickyScrollEnabled = GrepSearchResultsViewModel.StickyScrollEnabled;
                            if (stickyScrollEnabled)
                            {
                                ResetContextItemVisible();
                            }
                            else
                            {
                                vm.ContextGrepResult = null;
                                vm.ContextGrepResultVisible = false;
                            }
                        }
                    };
                }
            };

            GrepSearchResultsViewModel.SearchResultsMessenger.Register("OpenFiles",
                (Action<OpenFileContext>)(ctx => OpenContextFile(true, ctx)));
        }

        #region INameScope Members

        private readonly Dictionary<string, object> items = [];

        object INameScope.FindName(string name)
        {
            return items[name];
        }

        void INameScope.RegisterName(string name, object scopedElement)
        {
            items.Add(name, scopedElement);
        }

        void INameScope.UnregisterName(string name)
        {
            items.Remove(name);
        }

        #endregion

        #region Results Tree pass-through

        internal MultiSelectTreeView TreeView => resultsTree.treeView;


        internal async Task Next()
        {
            inNextPrevious = true;

            await resultsTree.Next();

            inNextPrevious = false;
        }

        internal async Task NextFile()
        {
            inNextPrevious = true;

            await resultsTree.NextFile();

            inNextPrevious = false;
        }

        internal async Task Previous()
        {
            inNextPrevious = true;

            await resultsTree.Previous();

            inNextPrevious = false;
        }

        internal async Task PreviousFile()
        {
            inNextPrevious = true;

            await resultsTree.PreviousFile();

            inNextPrevious = false;
        }

        internal void SetFocus()
        {
            resultsTree.SetFocus();
        }

        #endregion

        #region Sticky Scroll

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (stickyScrollEnabled && !inNextPrevious && e.VerticalChange != 0)
            {
                ResetContextItemVisible();
            }
        }

        private void ResetContextItemVisible()
        {
            if (DataContext is GrepSearchResultsViewModel vm)
            {
                FormattedGrepResult? item = GetTopVisibleLine(resultsTree.treeView);
                if (!ReferenceEquals(item, vm.ContextGrepResult))
                {
                    vm.ContextGrepResult = item;
                    vm.ContextGrepResultVisible = item != null;
                    return;
                }

                bool newValue = false, currentValue = vm.ContextGrepResultVisible;

                if (contextControl.DataContext is FormattedGrepResult result)
                {
                    if (resultsTree.treeView.ItemContainerGenerator.ContainerFromItem(result) is
                            TreeViewItem treeViewItem)
                    {
                        newValue = !IsUserVisible(resultsTree.treeView, treeViewItem);
                    }
                }
                else
                {
                    newValue = false;
                }

                if (newValue != currentValue)
                {
                    vm.ContextGrepResultVisible = newValue;
                }
            }
        }

        private static bool IsUserVisible(TreeView treeView, TreeViewItem treeViewItem)
        {
            if (!treeViewItem.IsVisible)
            {
                return false;
            }

            // a TreeViewItem Actual Height includes all of its children
            Rect tviRect = new(0.0, 0.0, treeViewItem.ActualWidth, treeViewItem.ActualHeight);
            var header = GetHeaderControl(treeViewItem);
            if (header != null)
            {
                tviRect = new(0.0, 0.0, header.ActualWidth, header.ActualHeight);
            }

            Rect ItemBounds = treeViewItem.TransformToAncestor(treeView).TransformBounds(tviRect);
            Rect containerRect = new(0.0, 0.0, treeView.ActualWidth, treeView.ActualHeight);
            bool visible = ItemBounds.Top + 5 >= containerRect.Top && ItemBounds.Bottom <= containerRect.Bottom;
            return visible;
        }

        private static FrameworkElement GetHeaderControl(TreeViewItem item)
        {
            return (FrameworkElement)item.Template.FindName("PART_Header", item);
        }

        private static FormattedGrepResult? GetTopVisibleLine(TreeView treeView)
        {
            if (treeView.Items.Count > 0)
            {
                foreach (FormattedGrepResult node in treeView.Items.Cast<FormattedGrepResult>())
                {
                    if (node.IsExpanded)
                    {
                        if (treeView.ItemContainerGenerator.ContainerFromItem(node) is TreeViewItem container)
                        {
                            if (IsUserVisible(treeView, container))
                            {
                                return null;
                            }
                            else
                            {
                                foreach (FormattedGrepLine childNode in node.Children.Cast<FormattedGrepLine>())
                                {
                                    if (container.ItemContainerGenerator.ContainerFromItem(childNode) is TreeViewItem treeViewItem &&
                                        IsUserVisible(treeView, treeViewItem))
                                    {
                                        return node;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        #endregion

        #region Tree right click events

        private void BtnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is GrepSearchResultsViewModel vm &&
                vm.ContextGrepResult != null)
            {
                vm.OpenFile(vm.ContextGrepResult, false, string.Empty);
            }
        }

        private void OpenContextFile(bool useCustomEditor, OpenFileContext ctx)
        {
            if (ctx.CommandParameter is string name &&
                name.Equals("ContextGrepResult", StringComparison.OrdinalIgnoreCase))
            {
                if (DataContext is GrepSearchResultsViewModel vm &&
                    vm.ContextGrepResult != null)
                {
                    vm.OpenFile(vm.ContextGrepResult, useCustomEditor, ctx.EditorName);
                }
            }
        }

        private void BtnOpenContainingFolder_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is GrepSearchResultsViewModel vm &&
                vm.ContextGrepResult != null)
            {
                string fileName = vm.ContextGrepResult.GrepResult.FileNameReal;
                Utils.OpenContainingFolder(fileName);
            }
        }

        private void BtnOpenExplorerMenu_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is GrepSearchResultsViewModel vm &&
                vm.ContextGrepResult != null)
            {
                string fileName = vm.ContextGrepResult.GrepResult.FileNameReal;
                ShellContextMenu menu = new();
                menu.ShowContextMenu(new FileInfo[] { new(fileName) },
                    PointToScreen(Mouse.GetPosition(this)));
            }
        }

        private void BtnShowFileProperties_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is GrepSearchResultsViewModel vm &&
               vm.ContextGrepResult != null)
            {
                string fileName = vm.ContextGrepResult.GrepResult.FileNameReal;
                ShellIntegration.ShowFileProperties(fileName);
            }
        }

        private void BtnCopyFileNames_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is GrepSearchResultsViewModel vm &&
                vm.ContextGrepResult != null)
            {
                string name = Path.GetFileName(vm.ContextGrepResult.GrepResult.FileNameDisplayed);
                NativeMethods.SetClipboardText(name);
            }
        }

        private void BtnCopyFullFilePath_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is GrepSearchResultsViewModel vm &&
                vm.ContextGrepResult != null)
            {
                string name = vm.ContextGrepResult.GrepResult.FileNameDisplayed;
                NativeMethods.SetClipboardText(name);
            }
        }

        #endregion
    }
}
