using System.Collections.Generic;
using System.Windows.Input;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for KeyboardShortcutWindow.xaml
    /// </summary>
    public partial class KeyboardShortcutWindow : ThemedWindow
    {
        private KeyboardShortcutViewModel viewModel = new();
        
        public KeyboardShortcutWindow()
        {
            InitializeComponent();

            viewModel.RequestClose += (s, e) => Close();
            DataContext = viewModel;
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            List<Key> nonShortcutKeys = [Key.LeftAlt, Key.RightAlt, Key.LeftCtrl, Key.RightCtrl, Key.LeftShift, Key.RightShift, Key.LWin, Key.RWin, Key.Apps];
            var actualKey = KeyGestureLocalizer.RealKey(e);

            if (e.IsDown && !nonShortcutKeys.Contains(actualKey))
            {
                if (e.Key == Key.Back)
                {
                    viewModel.SelectedCommand.ProposedKeyGesture = string.Empty;
                    e.Handled = true;
                    return;
                }
                if (e.Key == Key.Tab)
                {
                    return;
                }

                List<ModifierKeys> modifiers = [];

                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    modifiers.Add(ModifierKeys.Control);
                }

                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    modifiers.Add(ModifierKeys.Shift);
                }

                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
                {
                    modifiers.Add(ModifierKeys.Alt);
                }

                if (modifiers.Count == 0 && KeyGestureLocalizer.KeyNeedsModifier(actualKey))
                {
                    e.Handled = true;
                    return;
                }

                if (modifiers.Count == 0)
                {
                    viewModel.SelectedCommand.ProposedKeyGesture = actualKey.ToString();
                }
                else
                {
                    viewModel.SelectedCommand.ProposedKeyGesture = string.Format("{0}+{1}", string.Join("+", modifiers), actualKey);
                }

                e.Handled = true;
            }
        }
    }
}
