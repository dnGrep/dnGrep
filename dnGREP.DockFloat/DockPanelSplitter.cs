using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace dnGREP.DockFloat
{
    /// <summary>
    /// Code Project article
    /// http://www.codeproject.com/KB/WPF/DockPanelSplitter.aspx
    /// 
    /// CodePlex project
    /// http://wpfcontrols.codeplex.com
    ///
    /// DockPanelSplitter is a splitter control for DockPanels.
    /// Add the DockPanelSplitter after the element you want to resize.
    /// Set the DockPanel.Dock to define which edge the splitter should work on.
    ///
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:df="clr-namespace:DockPanelSplitter"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:df="clr-namespace:DockPanelSplitter;assembly=dnGREP.DockFloat"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <df:DockPanelSplitter/>
    ///
    /// </summary>

    public class DockPanelSplitter : Control
    {
        public static double Panel1MinSize => 25;
        public static double Panel2MinSize => 25;
        static DockPanelSplitter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DockPanelSplitter),
                new FrameworkPropertyMetadata(typeof(DockPanelSplitter)));

            // override the Background property
            BackgroundProperty.OverrideMetadata(typeof(DockPanelSplitter), new FrameworkPropertyMetadata(Brushes.Transparent));

            // override the Dock property to get notifications when Dock is changed
            DockPanel.DockProperty.OverrideMetadata(typeof(DockPanelSplitter),
                new FrameworkPropertyMetadata(Dock.Left, new PropertyChangedCallback(DockChanged)));
        }

        /// <summary>
        /// Resize the target element proportionally with the parent container
        /// Set to false if you don't want the element to be resized when the parent is resized.
        /// </summary>
        public bool ProportionalResize
        {
            get { return (bool)GetValue(ProportionalResizeProperty); }
            set { SetValue(ProportionalResizeProperty, value); }
        }

        public static readonly DependencyProperty ProportionalResizeProperty =
            DependencyProperty.Register("ProportionalResize", typeof(bool), typeof(DockPanelSplitter),
            new UIPropertyMetadata(true));

        /// <summary>
        /// Height or width of splitter, depends of orientation of the splitter
        /// </summary>
        public double Thickness
        {
            get { return (double)GetValue(ThicknessProperty); }
            set { SetValue(ThicknessProperty, value); }
        }

        public static readonly DependencyProperty ThicknessProperty =
            DependencyProperty.Register("Thickness", typeof(double), typeof(DockPanelSplitter),
            new UIPropertyMetadata(4.0, ThicknessChanged));


        #region Private fields
        private FrameworkElement? element;    // element to resize (target element)
        private double width;                 // current desired width of the element, can be less than minwidth
        private double height;                // current desired height of the element, can be less than minheight
        private double previousParentWidth;   // current width of parent element, used for proportional resize
        private double previousParentHeight;  // current height of parent element, used for proportional resize
        #endregion

        public DockPanelSplitter()
        {
            Loaded += DockPanelSplitterLoaded;
            Unloaded += DockPanelSplitterUnloaded;

            UpdateHeightOrWidth();
        }

        void DockPanelSplitterLoaded(object sender, RoutedEventArgs e)
        {
            if (Parent is Panel dp)
            {
                // Subscribe to the parent's size changed event
                dp.SizeChanged += ParentSizeChanged;

                // Current size of the parent DockPanel is not yet set
                // Store the current size in the ParentSizeChanged handler
                previousParentWidth = double.NaN;
                previousParentHeight = double.NaN;

                // Find the target element
                UpdateTargetElement();
            }
        }

        void DockPanelSplitterUnloaded(object sender, RoutedEventArgs e)
        {
            if (Parent is Panel dp)
            {
                // Unsubscribe
                dp.SizeChanged -= ParentSizeChanged;
            }
        }

        private static void DockChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DockPanelSplitter)d).UpdateHeightOrWidth();
        }

        private static void ThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DockPanelSplitter)d).UpdateHeightOrWidth();
        }

        private void UpdateHeightOrWidth()
        {
            if (IsHorizontal)
            {
                Height = Thickness;
                Width = double.NaN;
            }
            else
            {
                Width = Thickness;
                Height = double.NaN;
            }
        }

        public bool IsHorizontal
        {
            get
            {
                Dock dock = DockPanel.GetDock(this);
                return dock == Dock.Top || dock == Dock.Bottom;
            }
        }

        /// <summary>
        /// Update the target element (the element the DockPanelSplitter works on)
        /// </summary>
        private void UpdateTargetElement()
        {
            if (Parent is Panel dp)
            {
                int i = dp.Children.IndexOf(this);

                // The splitter cannot be the first child of the parent DockPanel
                // The splitter works on the 'older' sibling 
                if (i > 0 && dp.Children.Count > 0)
                {
                    element = dp.Children[i - 1] as FrameworkElement;
                }
            }
        }

        private void SetTargetWidth(double newWidth)
        {
            if (Parent is Panel dp)
            {
                Dock dock = DockPanel.GetDock(this);
                if (element?.TransformToAncestor(dp) is MatrixTransform t)
                {
                    if (dock == Dock.Left && newWidth > dp.ActualWidth - t.Matrix.OffsetX - Thickness)
                        newWidth = dp.ActualWidth - t.Matrix.OffsetX - Thickness;

                    if (newWidth < element.MinWidth)
                        newWidth = element.MinWidth;
                    if (newWidth > element.MaxWidth)
                        newWidth = element.MaxWidth;

                    if (newWidth > dp.ActualWidth - Panel1MinSize)
                        newWidth = dp.ActualWidth - Panel1MinSize;

                    if (newWidth < Panel2MinSize)
                        newWidth = Panel2MinSize;

                    element.Width = newWidth;
                }
            }
        }

        private void SetTargetHeight(double newHeight)
        {
            if (Parent is Panel dp)
            {
                Dock dock = DockPanel.GetDock(this);
                if (element?.TransformToAncestor(dp) is MatrixTransform t)
                {
                    if (dock == Dock.Top && newHeight > dp.ActualHeight - t.Matrix.OffsetY - Thickness)
                        newHeight = dp.ActualHeight - t.Matrix.OffsetY - Thickness;

                    if (newHeight < element.MinHeight)
                        newHeight = element.MinHeight;
                    if (newHeight > element.MaxHeight)
                        newHeight = element.MaxHeight;

                    if (newHeight > dp.ActualHeight - Panel1MinSize)
                        newHeight = dp.ActualHeight - Panel1MinSize;

                    if (newHeight < Panel2MinSize)
                        newHeight = Panel2MinSize;

                    element.Height = newHeight;
                }
            }
        }

        private void ParentSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!ProportionalResize) return;

            if (Parent is DockPanel dp && element != null)
            {
                if (!double.IsNaN(previousParentWidth) && previousParentWidth != 0 &&
                    !double.IsNaN(previousParentHeight) && previousParentHeight != 0)
                {
                    double sx = dp.ActualWidth / previousParentWidth;
                    double sy = dp.ActualHeight / previousParentHeight;

                    if (!double.IsInfinity(sx))
                        SetTargetWidth(element.Width * sx);
                    if (!double.IsInfinity(sy))
                        SetTargetHeight(element.Height * sy);
                }

                previousParentWidth = dp.ActualWidth;
                previousParentHeight = dp.ActualHeight;
            }
        }

        double AdjustWidth(double dx, Dock dock)
        {
            if (dock == Dock.Right)
                dx = -dx;

            width += dx;
            SetTargetWidth(width);

            return dx;
        }

        double AdjustHeight(double dy, Dock dock)
        {
            if (dock == Dock.Bottom)
                dy = -dy;

            height += dy;
            SetTargetHeight(height);

            return dy;
        }

        Point StartDragPoint;

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            if (!IsEnabled) return;
            Cursor = IsHorizontal ? Cursors.SizeNS : Cursors.SizeWE;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (!IsEnabled) return;

            if (!IsMouseCaptured)
            {
                StartDragPoint = e.GetPosition(Parent as IInputElement);
                UpdateTargetElement();
                if (element != null)
                {
                    width = element.ActualWidth;
                    height = element.ActualHeight;
                    CaptureMouse();
                }
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (IsMouseCaptured)
            {
                Point ptCurrent = e.GetPosition(Parent as IInputElement);
                Point delta = new(ptCurrent.X - StartDragPoint.X, ptCurrent.Y - StartDragPoint.Y);
                Dock dock = DockPanel.GetDock(this);

                if (IsHorizontal)
                    delta.Y = AdjustHeight(delta.Y, dock);
                else
                    delta.X = AdjustWidth(delta.X, dock);

                bool isBottomOrRight = dock == Dock.Right || dock == Dock.Bottom;

                // When docked to the bottom or right, the position has changed after adjusting the size
                if (isBottomOrRight)
                    StartDragPoint = e.GetPosition(Parent as IInputElement);
                else
                    StartDragPoint = new(StartDragPoint.X + delta.X, StartDragPoint.Y + delta.Y);
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (IsMouseCaptured)
            {
                ReleaseMouseCapture();
            }
            base.OnMouseUp(e);
        }

    }
}
