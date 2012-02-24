using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;

namespace Wpf.Controls
{
    /// <summary>
    /// Implemetation of a Split Button
    /// </summary>
    [TemplatePart(Name = "PART_DropDown", Type = typeof (Button))]
    [ContentProperty("Items")]
    [DefaultProperty("Items")]
    public class SplitButton : Button
    {
        // AddOwner Dependency properties
        public static readonly DependencyProperty HorizontalOffsetProperty;
        public static readonly DependencyProperty IsContextMenuOpenProperty;
        public static readonly DependencyProperty ModeProperty;
        public static readonly DependencyProperty PlacementProperty;
        public static readonly DependencyProperty PlacementRectangleProperty;
        public static readonly DependencyProperty VerticalOffsetProperty;

        /// <summary>
        /// Static Constructor
        /// </summary>
        static SplitButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof (SplitButton), new FrameworkPropertyMetadata(typeof (SplitButton)));
            IsContextMenuOpenProperty = DependencyProperty.Register("IsContextMenuOpen", typeof(bool), typeof(SplitButton), new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnIsContextMenuOpenChanged)));
            ModeProperty = DependencyProperty.Register("Mode", typeof(SplitButtonMode), typeof(SplitButton), new FrameworkPropertyMetadata(SplitButtonMode.Split));

            // AddOwner properties from the ContextMenuService class, we need callbacks from these properties
            // to update the Buttons ContextMenu properties
            PlacementProperty = ContextMenuService.PlacementProperty.AddOwner(typeof (SplitButton), new FrameworkPropertyMetadata(PlacementMode.Bottom, new PropertyChangedCallback(OnPlacementChanged)));
            PlacementRectangleProperty = ContextMenuService.PlacementRectangleProperty.AddOwner(typeof (SplitButton), new FrameworkPropertyMetadata(Rect.Empty, new PropertyChangedCallback(OnPlacementRectangleChanged)));
            HorizontalOffsetProperty = ContextMenuService.HorizontalOffsetProperty.AddOwner(typeof (SplitButton), new FrameworkPropertyMetadata(0.0, new PropertyChangedCallback(OnHorizontalOffsetChanged)));
            VerticalOffsetProperty = ContextMenuService.VerticalOffsetProperty.AddOwner(typeof (SplitButton), new FrameworkPropertyMetadata(0.0, new PropertyChangedCallback(OnVerticalOffsetChanged)));
        }


        /*
         * Overrides
         * 
        */
        /// <summary>
        /// OnApplyTemplate override, set up the click event for the dropdown if present in the template
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // set up the click event handler for the dropdown button
            ButtonBase dropDown = this.Template.FindName("PART_DropDown", this) as ButtonBase;
            if (dropDown != null)
                dropDown.Click += Dropdown_Click;
        }

        /// <summary>
        ///     Handles the Base Buttons OnClick event
        /// </summary>
        protected override void OnClick()
        {
            switch (Mode)
            {
                case SplitButtonMode.Dropdown:
                    OnDropdown();
                    break;
                
                default:
                    base.OnClick(); // forward on the Click event to the user
                    break;
            }
        }

        /*
         * Properties
         * 
        */


        /// <summary>
        /// The Split Button's Items property maps to the base classes ContextMenu.Items property
        /// </summary>
        public ItemCollection Items
        {
            get
            {
                EnsureContextMenuIsValid();
                return this.ContextMenu.Items;
            }
        }

        /*
         * DependencyProperty CLR wrappers
         * 
        */

        /// <summary>
        /// Gets or sets the IsContextMenuOpen property. 
        /// </summary>
        public bool IsContextMenuOpen
        {
            get { return (bool) GetValue(IsContextMenuOpenProperty); }
            set { SetValue(IsContextMenuOpenProperty, value); }
        }


        /// <summary>
        /// Placement of the Context menu
        /// </summary>
        public PlacementMode Placement
        {
            get { return (PlacementMode) GetValue(PlacementProperty); }
            set { SetValue(PlacementProperty, value); }
        }


        /// <summary>
        /// PlacementRectangle of the Context menu
        /// </summary>
        public Rect PlacementRectangle
        {
            get { return (Rect) GetValue(PlacementRectangleProperty); }
            set { SetValue(PlacementRectangleProperty, value); }
        }


        /// <summary>
        /// HorizontalOffset of the Context menu
        /// </summary>
        public double HorizontalOffset
        {
            get { return (double) GetValue(HorizontalOffsetProperty); }
            set { SetValue(HorizontalOffsetProperty, value); }
        }


        /// <summary>
        /// VerticalOffset of the Context menu
        /// </summary>
        public double VerticalOffset
        {
            get { return (double) GetValue(VerticalOffsetProperty); }
            set { SetValue(VerticalOffsetProperty, value); }
        }

        /// <summary>
        /// Defines the Mode of operation of the Button
        /// </summary>
        /// <remarks>
        ///     The SplitButton two Modes are
        ///     Split (default),    - the button has two parts, a normal button and a dropdown which exposes the ContextMenu
        ///     Dropdown            - the button acts like a combobox, clicking anywhere on the button opens the Context Menu
        /// </remarks>
        public SplitButtonMode Mode
        {
            get { return (SplitButtonMode) GetValue(ModeProperty); }
            set { SetValue(ModeProperty, value); }
        }

        /*
         * DependencyPropertyChanged callbacks
         * 
        */

        private static void OnIsContextMenuOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SplitButton s = (SplitButton) d;
            s.EnsureContextMenuIsValid();

            if (!s.ContextMenu.HasItems)
                return;

            bool value = (bool) e.NewValue;

            if (value && !s.ContextMenu.IsOpen)
                s.ContextMenu.IsOpen = true;
            else if (!value && s.ContextMenu.IsOpen)
                s.ContextMenu.IsOpen = false;
        }


        /// <summary>
        /// Placement Property changed callback, pass the value through to the buttons context menu
        /// </summary>
        private static void OnPlacementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SplitButton s = d as SplitButton;
            if (s == null) return;

            s.EnsureContextMenuIsValid();
            s.ContextMenu.Placement = (PlacementMode) e.NewValue;
        }

        /// <summary>
        /// PlacementRectangle Property changed callback, pass the value through to the buttons context menu
        /// </summary>
        private static void OnPlacementRectangleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SplitButton s = d as SplitButton;
            if (s == null) return;

            s.EnsureContextMenuIsValid();
            s.ContextMenu.PlacementRectangle = (Rect) e.NewValue;
        }

        /// <summary>
        /// HorizontalOffset Property changed callback, pass the value through to the buttons context menu
        /// </summary>
        private static void OnHorizontalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SplitButton s = d as SplitButton;
            if (s == null) return;

            s.EnsureContextMenuIsValid();
            s.ContextMenu.HorizontalOffset = (double) e.NewValue;
        }

        /// <summary>
        /// VerticalOffset Property changed callback, pass the value through to the buttons context menu
        /// </summary>
        private static void OnVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SplitButton s = d as SplitButton;
            if (s == null) return;

            s.EnsureContextMenuIsValid();
            s.ContextMenu.VerticalOffset = (double) e.NewValue;
        }

        /*
         * Helper Methods
         * 
        */

        /// <summary>
        /// Make sure the Context menu is not null
        /// </summary>
        private void EnsureContextMenuIsValid()
        {
            if (this.ContextMenu == null)
            {
                this.ContextMenu = new ContextMenu();
                this.ContextMenu.PlacementTarget = this;
                this.ContextMenu.Placement = Placement;

                this.ContextMenu.Opened += ((sender, routedEventArgs) => IsContextMenuOpen = true);
                this.ContextMenu.Closed += ((sender, routedEventArgs) => IsContextMenuOpen = false);
            }
        }

        private void OnDropdown()
        {
            EnsureContextMenuIsValid();
            if (!this.ContextMenu.HasItems)
                return;

            this.ContextMenu.IsOpen = !IsContextMenuOpen; // open it if closed, close it if open
        }

        /*
         * Events
         * 
        */

        /// <summary>
        /// Event Handler for the Drop Down Button's Click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Dropdown_Click(object sender, RoutedEventArgs e)
        {
            OnDropdown();
            e.Handled = true;
        }
    }
}