using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using dnGREP.Common;
using dnGREP.Common.UI;
using dnGREP.WPF.Properties;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Search;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for ReplaceWindow.xaml
    /// </summary>
    public partial class ReplaceWindow : ThemedWindow
    {
        private ReplaceViewHighlighter? highlighter;
        private readonly ReplaceViewLineNumberMargin? lineNumberMargin;
        private bool isInitializing;
        private bool isInPropertyChanged;
        private bool isInCaretMoved;
        private readonly SearchPanel? searchPanel;

        public ReplaceWindow()
        {
            InitializeComponent();

            if (LayoutProperties.ReplaceBounds == Rect.Empty ||
                LayoutProperties.ReplaceBounds == new Rect(0, 0, 0, 0) ||
                !ViewModel.IsFullDialog)
            {
                Width = ViewModel.DialogSize.Width;
                Height = ViewModel.DialogSize.Height;
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            else
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                Left = LayoutProperties.ReplaceBounds.Left;
                Top = LayoutProperties.ReplaceBounds.Top;
                Width = LayoutProperties.ReplaceBounds.Width;
                Height = LayoutProperties.ReplaceBounds.Height;
            }

            Loaded += (s, e) =>
            {
                if (!this.IsOnScreen())
                    this.CenterWindow();

                this.ConstrainToScreen();
            };

            if (ViewModel.IsFullDialog)
            {
                cbWrapText.IsChecked = GrepSettings.Instance.Get<bool>(GrepSettings.Key.ReplaceWindowWrap);
                zoomSlider.Value = GrepSettings.Instance.Get<int>(GrepSettings.Key.ReplaceWindowFontSize);

                textEditor.ShowLineNumbers = false; // using custom line numbers

                lineNumberMargin = new ReplaceViewLineNumberMargin();
                Line line = (Line)DottedLineMargin.Create();
                textEditor.TextArea.LeftMargins.Insert(0, lineNumberMargin);
                textEditor.TextArea.LeftMargins.Insert(1, line);
                var lineNumbersForeground = new Binding("LineNumbersForeground") { Source = textEditor };
                line.SetBinding(Line.StrokeProperty, lineNumbersForeground);
                lineNumberMargin.SetBinding(Control.ForegroundProperty, lineNumbersForeground);

                searchPanel = SearchPanel.Install(textEditor);
                searchPanel.MarkerBrush = Application.Current.Resources["Match.Highlight.Background"] as Brush;

                ViewModel.LoadFile += (s, e) => LoadFile();
                ViewModel.ReplaceMatch += (s, e) => textEditor.TextArea.TextView.Redraw();
                ViewModel.PropertyChanged += ViewModel_PropertyChanged;

                textEditor.Loaded += (s, e) =>
                {
                    // once loaded, move to the first file
                    ViewModel.SelectNextFile();
                };

                textEditor.TextArea.Caret.PositionChanged += Caret_PositionChanged;
            }
            else
            {
                Loaded += (s, e) =>
                {
                    // once loaded, move to the first file
                    ViewModel.SelectNextFile();
                };
            }

            ViewModel.CloseTrue += ViewModel_CloseTrue;

            DataContext = ViewModel;

            Closing += (s, e) => SaveSettings();

        }

        private void SaveSettings()
        {
            if (ViewModel.IsFullDialog)
            {
                GrepSettings.Instance.Set(GrepSettings.Key.ReplaceWindowWrap, cbWrapText.IsChecked ?? false);
                GrepSettings.Instance.Set(GrepSettings.Key.ReplaceWindowFontSize, (int)zoomSlider.Value);

                LayoutProperties.ReplaceBounds = new Rect(
                   Left,
                   Top,
                   ActualWidth,
                   ActualHeight);
                LayoutProperties.Save();
            }
        }

        public ReplaceViewModel ViewModel { get; } = new ReplaceViewModel();

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedGrepMatch")
            {
                isInPropertyChanged = true;

                if (highlighter != null)
                {
                    highlighter.SelectedGrepMatch = ViewModel.SelectedGrepMatch;
                }

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
                textEditor.TextArea.TextView.LinkTextForegroundBrush = Application.Current.Resources["AvalonEdit.Link"] as Brush;
                textEditor.TextArea.TextView.Redraw();
            }
        }

        private void LoadFile()
        {
            isInitializing = true;

            lineNumberMargin?.LineNumbers.Clear();
            textEditor.Clear();
            for (int i = textEditor.TextArea.TextView.BackgroundRenderers.Count - 1; i >= 0; i--)
            {
                if (textEditor.TextArea.TextView.BackgroundRenderers[i] is ReplaceViewHighlighter)
                    textEditor.TextArea.TextView.BackgroundRenderers.RemoveAt(i);
            }

            if (ViewModel.IndividualReplaceEnabled && ViewModel.SelectedSearchResult != null)
            {
                highlighter = new ReplaceViewHighlighter(ViewModel.SelectedSearchResult);
                highlighter.LineNumbers.AddRange(ViewModel.LineNumbers);
                textEditor.TextArea.TextView.BackgroundRenderers.Add(highlighter);
                textEditor.Encoding = ViewModel.Encoding;
                textEditor.SyntaxHighlighting = ViewModel.HighlightingDefinition;
                textEditor.TextArea.TextView.LinkTextForegroundBrush = Application.Current.Resources["AvalonEdit.Link"] as Brush;
            }

            lineNumberMargin?.LineNumbers.AddRange(ViewModel.LineNumbers);

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
                // remove the highlighter
                for (int i = textEditor.TextArea.TextView.BackgroundRenderers.Count - 1; i >= 0; i--)
                {
                    if (textEditor.TextArea.TextView.BackgroundRenderers[i] is ReplaceViewHighlighter)
                        textEditor.TextArea.TextView.BackgroundRenderers.RemoveAt(i);
                }
            }

            // recalculate the width of the line number margin
            lineNumberMargin?.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            isInitializing = false;
        }

        private void Caret_PositionChanged(object? sender, EventArgs e)
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
                TraversalRequest request = new(focusDirection);

                // Gets the element with keyboard focus.
                if (Keyboard.FocusedElement is UIElement elementWithFocus)
                {
                    elementWithFocus.MoveFocus(request);
                }
            }
        }

        private void SyntaxButton_Click(object sender, RoutedEventArgs e)
        {
            syntaxContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            syntaxContextMenu.PlacementTarget = (UIElement)sender;
            syntaxContextMenu.IsOpen = true;
        }

        private void ViewModel_CloseTrue(object? sender, EventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OKButton_Click(object? sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
