using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Shapes;
using dnGREP.Common;
using DockFloat;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using WpfScreenHelper;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for ReplaceWindow.xaml
    /// </summary>
    public partial class ReplaceWindow : Window
    {
        private ReplaceViewHighlighter highlighter;
        private ReplaceViewLineNumberMargin lineNumberMargin;
        private bool isInitializing;
        private bool isInPropertyChanged;
        private bool isInCaretMoved;

        public ReplaceWindow()
        {
            InitializeComponent();

            if (Properties.Settings.Default.ReplaceBounds == Rect.Empty ||
                Properties.Settings.Default.ReplaceBounds == new Rect(0, 0, 0, 0))
            {
                Width = 800;
                Height = 980;
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            else
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                Left = Properties.Settings.Default.ReplaceBounds.Left;
                Top = Properties.Settings.Default.ReplaceBounds.Top;
                Width = Properties.Settings.Default.ReplaceBounds.Width;
                Height = Properties.Settings.Default.ReplaceBounds.Height;
            }

            Loaded += (s, e) =>
            {
                if (!this.IsOnScreen())
                    this.CenterWindow();

                this.ConstrainToScreen();
            };

            cbWrapText.IsChecked = GrepSettings.Instance.Get<bool?>(GrepSettings.Key.ReplaceWindowWrap);
            zoomSlider.Value = GrepSettings.Instance.Get<int>(GrepSettings.Key.ReplaceWindowFontSize);

            textEditor.ShowLineNumbers = false; // using custom line numbers

            lineNumberMargin = new ReplaceViewLineNumberMargin();
            Line line = (Line)DottedLineMargin.Create();
            textEditor.TextArea.LeftMargins.Insert(0, lineNumberMargin);
            textEditor.TextArea.LeftMargins.Insert(1, line);
            var lineNumbersForeground = new Binding("LineNumbersForeground") { Source = textEditor };
            line.SetBinding(Line.StrokeProperty, lineNumbersForeground);
            lineNumberMargin.SetBinding(Control.ForegroundProperty, lineNumbersForeground);

            DataContext = ViewModel;

            ViewModel.LoadFile += (s, e) => LoadFile();
            ViewModel.ReplaceMatch += (s, e) => textEditor.TextArea.TextView.Redraw();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ViewModel.CloseTrue += ViewModel_CloseTrue;

            textEditor.Loaded += (s, e) =>
            {
                // once loaded, move to the first file
                ViewModel.SelectNextFile();
            };

            Closing += (s, e) => SaveSettings();

            textEditor.TextArea.Caret.PositionChanged += Caret_PositionChanged;
        }

        private void SaveSettings()
        {
            GrepSettings.Instance.Set(GrepSettings.Key.ReplaceWindowWrap, cbWrapText.IsChecked);
            GrepSettings.Instance.Set(GrepSettings.Key.ReplaceWindowFontSize, (int)zoomSlider.Value);

            Properties.Settings.Default.ReplaceBounds = new Rect(
               Left,
               Top,
               ActualWidth,
               ActualHeight);
            Properties.Settings.Default.Save();
        }

        public ReplaceViewModel ViewModel { get; } = new ReplaceViewModel();

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedGrepMatch")
            {
                isInPropertyChanged = true;

                highlighter.SelectedGrepMatch = ViewModel.SelectedGrepMatch;

                if (ViewModel.SelectedGrepMatch != null && !isInCaretMoved)
                {
                    textEditor.ScrollTo(ViewModel.LineNumber, ViewModel.ColNumber);
                    textEditor.TextArea.Caret.Position = new TextViewPosition(ViewModel.LineNumber, ViewModel.ColNumber + 1);
                }

                textEditor.TextArea.TextView.Redraw();

                isInPropertyChanged = false;
            }
            else if (e.PropertyName == "CurrentSyntax")
            {
                textEditor.SyntaxHighlighting = ViewModel.HighlightingDefinition;
                textEditor.TextArea.TextView.Redraw();
            }
        }

        private void LoadFile()
        {
            isInitializing = true;

            lineNumberMargin.LineNumbers.Clear();
            textEditor.Clear();
            for (int i = textEditor.TextArea.TextView.LineTransformers.Count - 1; i >= 0; i--)
            {
                if (textEditor.TextArea.TextView.LineTransformers[i] is ReplaceViewHighlighter)
                    textEditor.TextArea.TextView.LineTransformers.RemoveAt(i);
            }

            if (ViewModel.IndividualReplaceEnabled)
            {
                highlighter = new ReplaceViewHighlighter(ViewModel.SelectedSearchResult);
                textEditor.TextArea.TextView.LineTransformers.Add(highlighter);
                textEditor.Encoding = ViewModel.Encoding;
                textEditor.SyntaxHighlighting = ViewModel.HighlightingDefinition;
            }

            lineNumberMargin.LineNumbers.AddRange(ViewModel.LineNumbers);

            try
            {
                if (!string.IsNullOrWhiteSpace(ViewModel.FilePath))
                {
                    textEditor.Load(ViewModel.FilePath);
                }
                else
                {
                    textEditor.Text = ViewModel.FileText;
                }
            }
            catch (Exception ex)
            {
                textEditor.Text = "Error opening the file: " + ex.Message;
            }

            // recalculate the width of the line number margin
            lineNumberMargin.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            isInitializing = false;
        }

        private void Caret_PositionChanged(object sender, EventArgs e)
        {
            if (isInitializing || isInPropertyChanged)
                return;

            isInCaretMoved = true;

            int lineNumber = textEditor.TextArea.Caret.Line;

            // if this is a clipped file with just matches and context, the LineNumbers list
            // will contain the real line numbers
            int index = lineNumber - 1;
            if (ViewModel.LineNumbers.Count > 0 && index < ViewModel.LineNumbers.Count)
                lineNumber = ViewModel.LineNumbers[index];

            ViewModel.MoveToMatch(lineNumber, textEditor.TextArea.Caret.Column);

            isInCaretMoved = false;
        }

        private void TextEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // handle tabbing out of the Avalon edit control
            FocusNavigationDirection focusDirection = FocusNavigationDirection.First;
            if (e.Key == Key.Tab && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                // should be Previous, but that doesn't work - it stays in the text editor... idk
                focusDirection = FocusNavigationDirection.Up;
                e.Handled = true;
            }
            else if (e.Key == Key.Tab)
            {
                focusDirection = FocusNavigationDirection.Next;
                e.Handled = true;
            }

            if (focusDirection != FocusNavigationDirection.First)
            {
                TraversalRequest request = new TraversalRequest(focusDirection);

                // Gets the element with keyboard focus.
                if (Keyboard.FocusedElement is UIElement elementWithFocus)
                {
                    elementWithFocus.MoveFocus(request);
                }
            }
        }

        private void ViewModel_CloseTrue(object sender, EventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
