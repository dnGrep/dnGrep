using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace dnGREP.WPF
{
    public static class TextBoxCommands
    {
        public static RoutedUICommand MakeLowerCase { get; }
        public static RoutedUICommand MakeUpperCase { get; }

        static TextBoxCommands()
        {
            MakeLowerCase = new RoutedUICommand("Make Lowercase",
                "MakeLowerCase", typeof(TextBoxCommands));

            MakeLowerCase.InputGestures.Add(new KeyGesture(Key.U, ModifierKeys.Control));

            MakeUpperCase = new RoutedUICommand("Make Uppercase",
                "MakeUpperCase", typeof(TextBoxCommands));

            MakeUpperCase.InputGestures.Add(new KeyGesture(Key.U, ModifierKeys.Control | ModifierKeys.Shift));
        }

        public static void BindCommandsToWindow(Window window)
        {
            window.CommandBindings.Add(
                new CommandBinding(MakeLowerCase, OnExecute_MakeLowerCase, CanExecute_MakeLowerCase));
            window.CommandBindings.Add(
                new CommandBinding(MakeUpperCase, OnExecute_MakeUpperCase, CanExecute_MakeUpperCase));
        }
        public static void OnExecute_MakeLowerCase(object sender, ExecutedRoutedEventArgs e)
        {
            if (FocusManager.GetFocusedElement(sender as DependencyObject) is TextBox tb)
            {
                int start = tb.SelectionStart, length = tb.SelectionLength;
                string leading = tb.Text[..start];
                string changing = tb.Text[start..(start + length)].ToLower(CultureInfo.CurrentCulture);
                string trailing = tb.Text[(start + length)..];
                tb.Text = leading + changing + trailing;
                tb.SelectionStart = start;
                tb.SelectionLength = length;
            }
        }

        public static void CanExecute_MakeLowerCase(object sender, CanExecuteRoutedEventArgs e)
        {
            if (FocusManager.GetFocusedElement(sender as DependencyObject) is TextBox tb)
            {
                e.CanExecute = tb.SelectionLength > 0;
            }
        }

        public static void OnExecute_MakeUpperCase(object sender, ExecutedRoutedEventArgs e)
        {
            if (FocusManager.GetFocusedElement(sender as DependencyObject) is TextBox tb)
            {
                int start = tb.SelectionStart, length = tb.SelectionLength;
                string leading = tb.Text[..start];
                string changing = tb.Text[start..(start + length)].ToUpper(CultureInfo.CurrentCulture);
                string trailing = tb.Text[(start + length)..];
                tb.Text = leading + changing + trailing;
                tb.SelectionStart = start;
                tb.SelectionLength = length;
            }
        }

        public static void CanExecute_MakeUpperCase(object sender, CanExecuteRoutedEventArgs e)
        {
            if (FocusManager.GetFocusedElement(sender as DependencyObject) is TextBox tb)
            {
                e.CanExecute = tb.SelectionLength > 0;
            }
        }

    }

}
