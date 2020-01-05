// Copyright (C) Microsoft Corporation. All Rights Reserved.
// This code released under the terms of the Microsoft Public License
// (Ms-PL, http://opensource.org/licenses/ms-pl.html).
// Modified from: https://dlaa.me/blog/post/10034983

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace dnGREP.WPF
{
    /// <summary>
    /// Implements a "split button" for WPF.
    /// </summary>
    [TemplatePart(Name = SplitElementName, Type = typeof(UIElement))]
    public class SplitButton : Button
    {
        /// <summary>
        /// Stores the public name of the split element.
        /// </summary>
        private const string SplitElementName = "SplitElement";

        /// <summary>
        /// Stores a reference to the split element.
        /// </summary>
        private UIElement _splitElement;

        /// <summary>
        /// Stores a reference to the ContextMenu.
        /// </summary>
        private ContextMenu _contextMenu;

        /// <summary>
        /// Stores a reference to the ancestor of the ContextMenu added as a logical child.
        /// </summary>
        private DependencyObject _logicalChild;

        /// <summary>
        /// Stores the initial location of the ContextMenu.
        /// </summary>
        private Point _contextMenuInitialOffset;

        /// <summary>
        /// Gets or sets a value indicating whether the mouse is over the split element.
        /// </summary>
        protected bool IsMouseOverSplitElement { get; private set; }

        /// <summary>
        /// Initializes a new instance of the SplitButton class.
        /// </summary>
        public SplitButton()
        {
            DefaultStyleKey = typeof(SplitButton);
        }

        /// <summary>
        /// Called when the template is changed.
        /// </summary>
        public override void OnApplyTemplate()
        {
            // Unhook existing handlers
            if (null != _splitElement)
            {
                _splitElement.MouseEnter -= SplitElement_MouseEnter;
                _splitElement.MouseLeave -= SplitElement_MouseLeave;
                _splitElement = null;
            }
            if (null != _contextMenu)
            {
                _contextMenu.Opened -= ContextMenu_Opened;
                _contextMenu.Closed -= ContextMenu_Closed;
                _contextMenu = null;
            }

            if (null != _logicalChild)
            {
                RemoveLogicalChild(_logicalChild);
                _logicalChild = null;
            }

            // Apply new template
            base.OnApplyTemplate();

            // Hook new event handlers
            _splitElement = GetTemplateChild(SplitElementName) as UIElement;
            if (null != _splitElement)
            {
                _splitElement.MouseEnter += SplitElement_MouseEnter;
                _splitElement.MouseLeave += SplitElement_MouseLeave;

                _contextMenu = ContextMenuService.GetContextMenu(this);
                if (null != _contextMenu)
                {
                    _contextMenu.Opened += ContextMenu_Opened;
                    _contextMenu.Closed += ContextMenu_Closed;
                }
            }
        }

        /// <summary>
        /// Called when the Button is clicked.
        /// </summary>
        protected override void OnClick()
        {
            if (IsMouseOverSplitElement)
            {
                OpenButtonMenu();
            }
            else
            {
                base.OnClick();
            }
        }

        /// <summary>
        /// Called when a key is pressed.
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (null == e)
            {
                throw new ArgumentNullException("e");
            }

            if ((Key.Down == e.Key) || (Key.Up == e.Key))
            {
                // WPF requires this to happen via BeginInvoke
                Dispatcher.BeginInvoke((Action)(() => OpenButtonMenu()));
            }
            else
            {
                base.OnKeyDown(e);
            }
        }

        /// <summary>
        /// Opens the button menu.
        /// </summary>
        protected void OpenButtonMenu()
        {
            if (_contextMenu != null && _contextMenu.Items.Count > 0)
            {
                _contextMenu.HorizontalOffset = 0;
                _contextMenu.VerticalOffset = 0;
                _contextMenu.UpdateDefaultStyle();
                _contextMenu.IsOpen = true;

                if (null == _logicalChild)
                {
                    // Add the ContextMenu as a logical child (for DataContext and RoutedCommands)
                    DependencyObject current = _contextMenu;
                    do
                    {
                        _logicalChild = current;
                        current = LogicalTreeHelper.GetParent(current);
                    } while (null != current);

                    AddLogicalChild(_logicalChild);
                }
            }
        }

        /// <summary>
        /// Called when the mouse goes over the split element.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Event arguments.</param>
        private void SplitElement_MouseEnter(object sender, MouseEventArgs e)
        {
            IsMouseOverSplitElement = true;
        }

        /// <summary>
        /// Called when the mouse goes off the split element.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Event arguments.</param>
        private void SplitElement_MouseLeave(object sender, MouseEventArgs e)
        {
            IsMouseOverSplitElement = false;
        }

        /// <summary>
        /// Called when the ContextMenu is opened.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Event arguments.</param>
        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            // Offset the ContextMenu correctly
            _contextMenuInitialOffset = TranslatePoint(new Point(0, ActualHeight), _contextMenu);
            UpdateContextMenuOffsets();

            // Hook LayoutUpdated to handle application resize and zoom changes
            LayoutUpdated += SplitButton_LayoutUpdated;
        }

        /// <summary>
        /// Called when the ContextMenu is closed.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Event arguments.</param>
        private void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            // No longer need to handle LayoutUpdated
            LayoutUpdated -= SplitButton_LayoutUpdated;

            // Restore focus to the Button
            Focus();
        }

        /// <summary>
        /// Called when the ContextMenu is open and layout is updated.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Event arguments.</param>
        private void SplitButton_LayoutUpdated(object sender, EventArgs e)
        {
            UpdateContextMenuOffsets();
        }

        /// <summary>
        /// Updates the ContextMenu's Horizontal/VerticalOffset properties to keep it under the SplitButton.
        /// </summary>
        private void UpdateContextMenuOffsets()
        {
            // Calculate desired offset to put the ContextMenu below and left-aligned to the Button
            Point currentOffset = new Point();
            Point desiredOffset = _contextMenuInitialOffset;

            _contextMenu.HorizontalOffset = desiredOffset.X - currentOffset.X;
            _contextMenu.VerticalOffset = desiredOffset.Y - currentOffset.Y;
            // Adjust for RTL
            if (FlowDirection.RightToLeft == FlowDirection)
            {
                _contextMenu.HorizontalOffset *= -1;
            }
        }

    }
}
