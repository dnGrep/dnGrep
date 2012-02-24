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
    [TemplatePart(Name = "PART_DropDown", Type = typeof(Button))]
    [ContentProperty("Items")]
    [DefaultProperty("Items")]
    public class SplitButton : Button
    {
        // AddOwner Dependancy properties
        public static readonly DependencyProperty PlacementProperty;
        public static readonly DependencyProperty PlacementRectangleProperty;
        public static readonly DependencyProperty HorizontalOffsetProperty;
        public static readonly DependencyProperty VerticalOffsetProperty;

        /// <summary>
        /// Static Constructor
        /// </summary>
        static SplitButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SplitButton), new FrameworkPropertyMetadata(typeof(SplitButton)));

            // AddOwner properties
            PlacementProperty = ContextMenuService.PlacementProperty.AddOwner(typeof(SplitButton), new FrameworkPropertyMetadata(PlacementMode.MousePoint, new PropertyChangedCallback(OnPlacementChanged)));
            PlacementRectangleProperty = ContextMenuService.PlacementRectangleProperty.AddOwner(typeof(SplitButton), new FrameworkPropertyMetadata(Rect.Empty, new PropertyChangedCallback(OnPlacementRectangleChanged)));
            HorizontalOffsetProperty = ContextMenuService.HorizontalOffsetProperty.AddOwner(typeof(SplitButton), new FrameworkPropertyMetadata(0.0, new PropertyChangedCallback(OnHorizontalOffsetChanged)));
            VerticalOffsetProperty = ContextMenuService.VerticalOffsetProperty.AddOwner(typeof(SplitButton), new FrameworkPropertyMetadata(0.0, new PropertyChangedCallback(OnVerticalOffsetChanged)));
        }

        /*
         * Properties
         * 
        */
        /// <summary>
        /// The Split Button's Items property maps to the base classes CopntextMenu.Items property
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
         * Dependancy Properties & Callbacks
         * 
        */
        /// <summary>
        /// Placement of the Context menu
        /// </summary>
        public PlacementMode Placement
        {
            get { return (PlacementMode)GetValue(PlacementProperty); }
            set { SetValue(PlacementProperty, value); }
        }
        /// <summary>
        /// Placement Property changed callback, pass the value through to the buttons context menu
        /// </summary>
        private static void OnPlacementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SplitButton s = d as SplitButton;
            if (s == null) return;

            s.EnsureContextMenuIsValid();
            s.ContextMenu.Placement = (PlacementMode)e.NewValue;
        }


        /// <summary>
        /// PlacementRectangle of the Context menu
        /// </summary>
        public Rect PlacementRectangle
        {
            get { return (Rect)GetValue(PlacementRectangleProperty); }
            set { SetValue(PlacementRectangleProperty, value); }
        }
        /// <summary>
        /// PlacementRectangle Property changed callback, pass the value through to the buttons context menu
        /// </summary>
        private static void OnPlacementRectangleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SplitButton s = d as SplitButton;
            if (s == null) return;

            s.EnsureContextMenuIsValid();
            s.ContextMenu.PlacementRectangle = (Rect)e.NewValue;
        }


        /// <summary>
        /// HorizontalOffset of the Context menu
        /// </summary>
        public double HorizontalOffset
        {
            get { return (double)GetValue(HorizontalOffsetProperty); }
            set { SetValue(HorizontalOffsetProperty, value); }
        }
        /// <summary>
        /// HorizontalOffset Property changed callback, pass the value through to the buttons context menu
        /// </summary>
        private static void OnHorizontalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SplitButton s = d as SplitButton;
            if (s == null) return;

            s.EnsureContextMenuIsValid();
            s.ContextMenu.HorizontalOffset = (double)e.NewValue;
        }


        /// <summary>
        /// VerticalOffset of the Context menu
        /// </summary>
        public double VerticalOffset
        {
            get { return (double)GetValue(VerticalOffsetProperty); }
            set { SetValue(VerticalOffsetProperty, value); }
        }
        /// <summary>
        /// VerticalOffset Property changed callback, pass the value through to the buttons context menu
        /// </summary>
        private static void OnVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SplitButton s = d as SplitButton;
            if (s == null) return;

            s.EnsureContextMenuIsValid();
            s.ContextMenu.HorizontalOffset = (double)e.NewValue;
        }


        /// <summary>
        /// Defines the Mode of operation of the Button
        /// </summary>
        /// <remarks>
        ///     The SplitButton Modes are
        ///     Split (default),    - the button has two parts, a normal button and a dropdown which exposes the ContextMenu
        ///     Dropdown            - the button acts like a combobox, clicking anywhere on the button opens the Context Menu
        /// </remarks>
        public SplitButtonMode Mode
        {
            get { return (SplitButtonMode)GetValue(ModeProperty); }
            set { SetValue(ModeProperty, value); }
        }
        public static readonly DependencyProperty ModeProperty = DependencyProperty.Register("Mode", typeof(SplitButtonMode), typeof(SplitButton), new FrameworkPropertyMetadata(SplitButtonMode.Split));


        /// <summary>
        /// Adds an Icon to the Left Edge of the Split Button
        /// </summary>
        public object Icon
        {
            get { return (object)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }
        public static readonly DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(object), typeof(SplitButton), new UIPropertyMetadata(null));


        /*
         * Methods
         * 
        */
        /// <summary>
        /// OnApplyTemplate override, set up the click event for the dropdown if present in the template
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // set up the event handlers
            ButtonBase dropDown = this.Template.FindName("PART_DropDown", this) as ButtonBase;
            if (dropDown != null)
                dropDown.Click += DoClick;

            this.Click += DoClick;
        }

        /// <summary>
        /// Make sure the Context menu is not null
        /// </summary>
        private void EnsureContextMenuIsValid()
        {
            if (this.ContextMenu == null)
                this.ContextMenu = new ContextMenu();
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
        void DoClick(object sender, RoutedEventArgs e)
        {
            // if we are in split mode and the DropDown button has not been clicked, return
            if (this.Mode == SplitButtonMode.Split && sender.Equals(this))
                return;

            if (this.ContextMenu == null || this.ContextMenu.HasItems == false) return;

            this.ContextMenu.PlacementTarget = this;
            this.ContextMenu.IsOpen = true;

            e.Handled = true;
        }
    }
}
