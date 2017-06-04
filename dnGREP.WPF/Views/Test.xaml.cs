using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Input;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for Test.xaml
    /// </summary>
    public partial class Test : Window
    {
        public Test()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            cbHistory.IsDropDownOpen = true;
        }

        private void cbHistory_MouseDown(object sender, MouseButtonEventArgs e)
        {
            textBox1.Text = sender.GetType().ToString();
        }

        private static UIElement _draggedElt;
        private static bool _isMouseDown = false;
        private static Point _dragStartPoint;

        private void DragSource_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Make this the new drag source
            _draggedElt = e.Source as UIElement;
            _dragStartPoint = e.GetPosition(GetTopContainer());
            _isMouseDown = true;
        }

        private void DragSource_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isMouseDown && IsDragGesture(e.GetPosition(GetTopContainer())))
            {
                DragStarted(sender as UIElement);
            }
        }

        static void DragStarted(UIElement uiElt)
        {
            _isMouseDown = false;
            Mouse.Capture(uiElt);

            DataObject data = new DataObject();
            //obj.SetData(DataFormats.Text, "Text from WPF - CrossApp example");
            StringCollection files = new StringCollection();
            files.Add(@"C:\Documents and Settings\528046\Desktop\crystal_project\readme.txt");
            data.SetFileDropList(files);
            DragDropEffects supportedEffects = DragDropEffects.Move | DragDropEffects.Copy;

            // Perform DragDrop
            DragDropEffects effects = System.Windows.DragDrop.DoDragDrop(_draggedElt, data, supportedEffects);

            // Clean up
            Mouse.Capture(null);
            _draggedElt = null;
        }

        static bool IsDragGesture(Point point)
        {
            bool hGesture = Math.Abs(point.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance;
            bool vGesture = Math.Abs(point.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance;

            return (hGesture | vGesture);
        }

        static UIElement GetTopContainer()
        {
            return Application.Current.MainWindow.Content as UIElement;
        }
    }
}
