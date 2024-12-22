using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using dnGREP.Common.UI;
using dnGREP.WPF.Properties;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for OptionsForm.xaml
    /// </summary>
    public partial class OptionsView : ThemedWindow
    {
        private readonly OptionsViewModel viewModel = new();

        public OptionsView()
        {
            InitializeComponent();
            DiginesisHelpProvider.HelpNamespace = "https://github.com/dnGrep/dnGrep/wiki/";
            DiginesisHelpProvider.ShowHelp = true;

            viewModel.RequestClose += (s, e) => Close();
            DataContext = viewModel;

            if (LayoutProperties.OptionsBounds == Rect.Empty ||
                LayoutProperties.OptionsBounds == new Rect(0, 0, 0, 0))
            {
                SizeToContent = SizeToContent.Width;
                Height = 600;
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            else
            {
                SizeToContent = SizeToContent.Manual;
                WindowStartupLocation = WindowStartupLocation.Manual;
                Left = LayoutProperties.OptionsBounds.Left;
                Top = LayoutProperties.OptionsBounds.Top;
                Width = LayoutProperties.OptionsBounds.Width;
                Height = LayoutProperties.OptionsBounds.Height;
            }

            Loaded += (s, e) =>
            {
                if (!this.IsOnScreen())
                    this.CenterWindow();

                this.ConstrainToScreen();

                TextBoxCommands.BindCommandsToWindow(this);
            };
            Closing += (s, e) => SaveSettings();
        }

        public bool PluginCacheCleared => viewModel.PluginCacheCleared;


        private void SaveSettings()
        {
            LayoutProperties.OptionsBounds = new Rect(
               Left,
               Top,
               ActualWidth,
               ActualHeight);
            LayoutProperties.Save();
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!IsTextAllowed(text))
                    e.CancelCommand();
            }
            else
                e.CancelCommand();
        }

        private static bool IsTextAllowed(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                if (!int.TryParse(text, out _))
                    return false;
            }
            return true;
        }
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = e.Uri.ToString(),
                UseShellExecute = true,
            };
            using var proc = Process.Start(startInfo);
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            List<Key> nonShortcutKeys = [Key.LeftAlt, Key.RightAlt, Key.LeftCtrl, Key.RightCtrl, Key.LeftShift, Key.RightShift, Key.LWin, Key.RWin];
            var actualKey = RealKey(e);

            if (e.IsDown && !nonShortcutKeys.Contains(actualKey))
            {
                if (e.Key == Key.Delete || e.Key == Key.Back)
                {
                    viewModel.RestoreWindowKeyboardShortcut = string.Empty;
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

                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
                {
                    modifiers.Add(ModifierKeys.Alt);
                }

                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    modifiers.Add(ModifierKeys.Shift);
                }

                // if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows)) -- does not work
                if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin))
                {
                    modifiers.Add(ModifierKeys.Windows);
                }

                if (modifiers.Count > 0)
                {
                    viewModel.RestoreWindowKeyboardShortcut = string.Format("{0} + {1}", string.Join(" + ", modifiers), actualKey);
                }

                e.Handled = true;
            }

        }
        public static Key RealKey(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.System:
                    return e.SystemKey;

                case Key.ImeProcessed:
                    return e.ImeProcessedKey;

                case Key.DeadCharProcessed:
                    return e.DeadCharProcessedKey;

                default:
                    return e.Key;
            }
        }
    }
}
