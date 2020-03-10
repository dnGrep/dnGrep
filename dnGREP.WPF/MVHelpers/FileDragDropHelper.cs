using System;
using System.Windows;
using System.Windows.Controls;

namespace dnGREP.WPF
{
    /// <summary>
    /// IFileDragDropTarget Interface
    /// </summary>
    public interface IFileDragDropTarget
    {
        void OnFileDrop(bool append, string[] filepaths);
    }

    /// <summary>
    /// FileDragDropHelper
    /// </summary>
    public class FileDragDropHelper
    {
        public static bool GetIsFileDragDropEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsFileDragDropEnabledProperty);
        }

        public static void SetIsFileDragDropEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsFileDragDropEnabledProperty, value);
        }

        public static bool GetFileDragDropTarget(DependencyObject obj)
        {
            return (bool)obj.GetValue(FileDragDropTargetProperty);
        }

        public static void SetFileDragDropTarget(DependencyObject obj, bool value)
        {
            obj.SetValue(FileDragDropTargetProperty, value);
        }

        public static readonly DependencyProperty IsFileDragDropEnabledProperty =
                DependencyProperty.RegisterAttached("IsFileDragDropEnabled",
                    typeof(bool), typeof(FileDragDropHelper),
                    new PropertyMetadata(OnFileDragDropEnabled));

        public static readonly DependencyProperty FileDragDropTargetProperty =
                DependencyProperty.RegisterAttached("FileDragDropTarget",
                    typeof(object), typeof(FileDragDropHelper), null);

        private static void OnFileDragDropEnabled(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == e.OldValue)
                return;

            if (d is Control control)
            {
                control.Drop += OnDrop;
            }
        }

        private static void OnDrop(object sender, DragEventArgs e)
        {
            if (sender is DependencyObject d &&
                d.GetValue(FileDragDropTargetProperty) is IFileDragDropTarget fileTarget)
            {
                if (e.Data.GetDataPresent(App.InstanceId))
                {
                    // drag source is this application instance: do not drop on ourself
                    // but can accept a drop from another dnGrep or Windows Explorer, etc.
                    return;
                }
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    fileTarget.OnFileDrop(e.KeyStates.HasFlag(DragDropKeyStates.ControlKey),
                        (string[])e.Data.GetData(DataFormats.FileDrop));
                }
            }
            else
            {
                throw new Exception("FileDragDropTarget object must be of type IFileDragDropTarget");
            }
        }
    }
}
