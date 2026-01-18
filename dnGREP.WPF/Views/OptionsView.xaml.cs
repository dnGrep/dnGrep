using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;
using dnGREP.Common;
using dnGREP.Common.UI;
using dnGREP.Localization;
using dnGREP.WPF.Properties;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for OptionsForm.xaml
    /// </summary>
    public partial class OptionsView : ThemedWindow
    {
        private readonly OptionsViewModel viewModel = new();
        private readonly DispatcherTimer searchTimer = new();
        private readonly HashSet<TextBlock> modifiedTextBlocks = [];
        private readonly List<SearchMatch> searchMatches = [];
        private int currentMatchIndex = -1;
        private bool isMatchCase;
        private bool isWholeWord;
        private static Brush highlightBrush1 = Brushes.Yellow;
        private static Brush highlightBrush2 = Brushes.Orange;

        public OptionsView()
        {
            InitializeComponent();
            DiginesisHelpProvider.HelpNamespace = "https://github.com/dnGrep/dnGrep/wiki/";
            DiginesisHelpProvider.ShowHelp = true;

            viewModel.RequestClose += (s, e) => Close();
            DataContext = viewModel;

            searchTimer.Interval = TimeSpan.FromMilliseconds(500);
            searchTimer.Tick += SearchTimer_Tick;

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

            highlightBrush1 = Application.Current.Resources["Match.Highlight.Background"] as Brush ?? Brushes.Yellow;
            highlightBrush2 = Application.Current.Resources["Match.Group.1.Highlight.Background"] as Brush ?? Brushes.Orange;

            AppTheme.Instance.CurrentThemeChanged += (s, e) =>
            {
                highlightBrush1 = Application.Current.Resources["Match.Highlight.Background"] as Brush ?? Brushes.Yellow;
                highlightBrush2 = Application.Current.Resources["Match.Group.1.Highlight.Background"] as Brush ?? Brushes.Orange;
                if (currentMatchIndex > -1 && currentMatchIndex < searchMatches.Count)
                    ScrollToMatch(searchMatches[currentMatchIndex]);
            };

            TranslationSource.Instance.CurrentCultureChanging += (s, e) =>
            {
                currentMatchIndex = -1;
                searchMatches.Clear();
                matchStatus.Text = "0/0";
                findBox.Text = string.Empty;

                RestoreOriginalTextBlocks();
                ClearTextBlockCache();
            };

            PreviewKeyDown += OptionsView_PreviewKeyDown;
        }

        public bool SearchListsCleared => viewModel.SearchListsCleared;
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
            List<Key> nonShortcutKeys = [Key.LeftAlt, Key.RightAlt, Key.LeftCtrl, Key.RightCtrl, Key.LeftShift, Key.RightShift, Key.LWin, Key.RWin, Key.Apps];
            var actualKey = KeyGestureLocalizer.RealKey(e);

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
                // technically, the Windows key is reserved for the system
                if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin))
                {
                    modifiers.Add(ModifierKeys.Windows);
                }

                if (modifiers.Count > 0)
                {
                    viewModel.RestoreWindowKeyboardShortcut = string.Format("{0}+{1}", string.Join("+", modifiers), actualKey);
                }

                e.Handled = true;
            }

        }
        private void OptionsView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+F to focus the find box
            if (e.Key == Key.F && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                findBox.Focus();
                findBox.SelectAll();
                e.Handled = true;
                return;
            }

            // F3 navigation (only when there are matches)
            if (searchMatches.Count > 0)
            {
                if (e.Key == Key.F3)
                {
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    {
                        // Shift+F3 for previous match
                        PreviousButton_Click(sender, e);
                        e.Handled = true;
                    }
                    else
                    {
                        // F3 for next match
                        NextButton_Click(sender, e);
                        e.Handled = true;
                    }
                }
            }
        }

        private void FindBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            searchTimer.Stop();
            searchTimer.Start();
        }

        private void SearchTimer_Tick(object? sender, EventArgs e)
        {
            searchTimer.Stop();
            FindAllMatches();
        }

        private void FindAllMatches()
        {
            currentMatchIndex = -1;
            searchMatches.Clear();
            matchStatus.Text = "0/0";

            RestoreOriginalTextBlocks();

            if (string.IsNullOrEmpty(findBox.Text))
            {
                return;
            }

            StringComparison stringComparison = isMatchCase ?
                StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            string searchText = findBox.Text;

            foreach (TextBlock tb in scrollViewer.FindVisualChildren<TextBlock>())
            {
                // don't search in combo boxes
                if (tb.TryFindParent<ComboBox>() is not null)
                {
                    continue;
                }

                // Get the position of the TextBlock relative to the ScrollViewer's content area
                Point relativePoint = tb.TranslatePoint(new Point(0, 0), scrollViewer);
                relativePoint.Offset(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset);

                List<SearchMatch> matches = [];
                foreach (Run run in tb.Inlines.OfType<Run>())
                {
                    string text = run.Text;

                    int index = 0;
                    while (index >= 0)
                    {
                        index = text.IndexOf(searchText, index, stringComparison);
                        if (index >= 0)
                        {
                            if (isWholeWord && (!Utils.IsValidBeginText(text[..index]) ||
                                !Utils.IsValidEndText(text[(index + searchText.Length)..])))
                            {
                                index++;
                                continue;
                            }

                            matches.Add(new(relativePoint, run, index, searchText.Length));
                            break;
                        }
                    }
                }

                if (matches.Count > 0)
                {
                    modifiedTextBlocks.Add(tb);

                    // HighlightMatches returns the updated matches with new Run references
                    var updatedMatches = HighlightMatches(tb, matches);
                    searchMatches.AddRange(updatedMatches);
                }
            }

            var firstMatch = searchMatches.FirstOrDefault();
            if (firstMatch != null)
            {
                currentMatchIndex = 0;
                ScrollToMatch(firstMatch);
            }
        }

        private void RestoreOriginalTextBlocks()
        {
            foreach (TextBlock tb in modifiedTextBlocks.ToList())
            {
                if (tb.Tag is Inline[] array)
                {
                    tb.Inlines.Clear();
                    tb.Inlines.AddRange(array);
                }
            }

            modifiedTextBlocks.Clear();
        }

        private void ClearTextBlockCache()
        {
            foreach (TextBlock tb in scrollViewer.FindVisualChildren<TextBlock>())
            {
                if (tb.TryFindParent<ComboBox>() is not null)
                {
                    continue;
                }

                if (tb.Tag is Inline[] array)
                {
                    tb.Inlines.Clear();
                    tb.Inlines.AddRange(array);
                    tb.Tag = null;
                }
            }
        }

        private static List<SearchMatch> HighlightMatches(TextBlock tb, List<SearchMatch> matches)
        {
            if (matches.Count == 0)
            {
                return matches;
            }

            if (tb.Tag is not Inline[] original)
            {
                original = new Inline[tb.Inlines.Count];
                tb.Inlines.CopyTo(original, 0);
                tb.Tag = original;
            }

            tb.Inlines.Clear();

            // Track the new Run objects created for highlighting
            List<SearchMatch> updatedMatches = [];

            foreach (Inline inline in original)
            {
                if (inline is Run run)
                {
                    string text = run.Text;
                    var runMatches = matches.Where(m => m.Run == run);
                    if (runMatches.Any())
                    {
                        int idx = 0;
                        foreach (var runMatch in runMatches)
                        {
                            if (runMatch.StartIndex > idx)
                            {
                                tb.Inlines.Add(CloneRunWithText(run, text.Substring(idx, runMatch.StartIndex)));
                                idx = runMatch.StartIndex;
                            }

                            Run highlight = CloneRunWithText(run, text.Substring(idx, runMatch.Length));
                            highlight.Background = highlightBrush1;
                            tb.Inlines.Add(highlight);

                            // Create updated match with the new Run reference
                            updatedMatches.Add(new(runMatch.Point, highlight, 0, runMatch.Length));

                            idx += runMatch.Length;
                        }

                        if (idx < text.Length)
                        {
                            tb.Inlines.Add(CloneRunWithText(run, text[idx..]));
                        }
                    }
                    else
                    {
                        tb.Inlines.Add(inline);
                    }
                }
                else
                {
                    tb.Inlines.Add(inline);
                }
            }

            return updatedMatches;
        }

        private static Run CloneRunWithText(Run run, string text)
        {
            Run clone = new(text);

            // Copy all locally set dependency properties
            LocalValueEnumerator enumerator = run.GetLocalValueEnumerator();
            while (enumerator.MoveNext())
            {
                LocalValueEntry entry = enumerator.Current;

                // Skip read-only properties and the Text property (already set)
                if (!entry.Property.ReadOnly && entry.Property != Run.TextProperty)
                {
                    clone.SetValue(entry.Property, entry.Value);
                }
            }

            return clone;
        }

        private void ScrollToMatch(SearchMatch match)
        {
            // Clear previous highlight
            foreach (var m in searchMatches)
            {
                m.Run.Background = highlightBrush1;
            }

            // Highlight current match
            match.Run.Background = highlightBrush2;

            matchStatus.Text = $"{currentMatchIndex + 1}/{searchMatches.Count}";

            // Check if the match is already visible in the viewport
            double viewportTop = scrollViewer.VerticalOffset;
            double viewportBottom = viewportTop + scrollViewer.ViewportHeight;
            double matchY = match.Point.Y;

            // Only scroll if the match is outside the visible area
            if (matchY < viewportTop || matchY > viewportBottom)
            {
                // Put the match at 1/4 the way down in the viewport
                double targetOffset = matchY - (scrollViewer.ViewportHeight / 4);
                scrollViewer.ScrollToVerticalOffset(Math.Max(0, targetOffset));
            }
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (searchMatches.Count > 0)
            {
                if (--currentMatchIndex < 0)
                {
                    currentMatchIndex = searchMatches.Count - 1;
                }

                ScrollToMatch(searchMatches[currentMatchIndex]);
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (searchMatches.Count > 0)
            {
                if (++currentMatchIndex >= searchMatches.Count)
                {
                    currentMatchIndex = 0;
                }

                ScrollToMatch(searchMatches[currentMatchIndex]);
            }
        }

        private void MatchCase_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton button)
            {
                isMatchCase = button.IsChecked ?? false;

                FindAllMatches();
            }
        }

        private void MatchWholeWords_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton button)
            {
                isWholeWord = button.IsChecked ?? false;

                FindAllMatches();
            }
        }

        private record SearchMatch(Point Point, Run Run, int StartIndex, int Length);
    }
}
