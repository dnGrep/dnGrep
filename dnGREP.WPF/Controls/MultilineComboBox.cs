using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace dnGREP.WPF
{
    /// <summary>
    /// Overrides the ComboBox to send Up/Down arrow keys to the text box when in multi-line mode
    /// </summary>
    public class MultilineComboBox : ComboBox
    {
        private TextBox editTextBox;

        public MultilineComboBox()
            : base()
        {
            Loaded += EditableComboBox_Loaded;
        }

        private void EditableComboBox_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.Template.FindName("PART_EditableTextBox", this) is TextBox tb)
            {
                editTextBox = tb;
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            // AcceptsReturn means the text box is in multi-line mode
            if (editTextBox != null && editTextBox.AcceptsReturn)
            {
                if (e.Key == Key.Up || e.Key == Key.Down)
                {
                    SendKey(e.Key);
                    e.Handled = true;
                    return;
                }
            }

            base.OnPreviewKeyDown(e);
        }

        private void SendKey(Key key)
        {
            if (editTextBox != null)
            {
                editTextBox.RaiseEvent(
                  new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(editTextBox), 0, key)
                  {
                      RoutedEvent = Keyboard.KeyDownEvent
                  });
            }
        }

        public static readonly DependencyProperty AllowMultilineProperty =
            DependencyProperty.RegisterAttached("AllowMultiline",
            typeof(bool), typeof(MultilineComboBox),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static void SetAllowMultiline(DependencyObject element, bool value)
        {
            element.SetValue(AllowMultilineProperty, value);
        }

        public static bool GetAllowMultiline(DependencyObject element)
        {
            return (bool)element.GetValue(AllowMultilineProperty);
        }
    }

}
