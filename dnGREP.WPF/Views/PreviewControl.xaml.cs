using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using dnGREP.Common;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Search;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for PreviewControl.xaml
    /// </summary>
    public partial class PreviewControl : UserControl
    {
        private readonly SearchPanel searchPanel;
        private readonly PreviewLineNumberMargin lineNumberMargin;

        public PreviewControl()
        {
            InitializeComponent();

            DataContext = ViewModel;

            searchPanel = SearchPanel.Install(textEditor);
            searchPanel.SearchResultsChanged += SearchPanel_SearchResultsChanged;
            searchPanel.MarkerBrush = Application.Current.Resources["Match.Highlight.Background"] as Brush;

            textEditor.ShowLineNumbers = false; // using custom line numbers

            lineNumberMargin = new PreviewLineNumberMargin();
            Line line = (Line)DottedLineMargin.Create();
            textEditor.TextArea.LeftMargins.Insert(0, lineNumberMargin);
            textEditor.TextArea.LeftMargins.Insert(1, line);
            var lineNumbersForeground = new Binding("LineNumbersForeground") { Source = textEditor };
            line.SetBinding(Line.StrokeProperty, lineNumbersForeground);
            lineNumberMargin.SetBinding(Control.ForegroundProperty, lineNumbersForeground);

            ViewModel.ShowPreview += ViewModel_ShowPreview;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            cbWrapText.IsChecked = GrepSettings.Instance.Get<bool>(GrepSettings.Key.PreviewWindowWrap);
            zoomSlider.Value = GrepSettings.Instance.Get<int>(GrepSettings.Key.PreviewWindowFont);
            zoomSlider.ValueChanged += ZoomSlider_ValueChanged;
            textEditor.TextArea.TextView.SizeChanged += TextView_SizeChanged;
            textEditor.TextArea.TextView.Options.HighlightCurrentLine = true;

            AppTheme.Instance.CurrentThemeChanged += (s, e) =>
            {
                textEditor.SyntaxHighlighting = ViewModel.HighlightingDefinition;
                textEditor.TextArea.TextView.LinkTextForegroundBrush = Application.Current.Resources["AvalonEdit.Link"] as Brush;
                searchPanel.MarkerBrush = Application.Current.Resources["Match.Highlight.Background"] as Brush;
            };
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.HighlightsOn))
            {
                for (int i = textEditor.TextArea.TextView.BackgroundRenderers.Count - 1; i >= 0; i--)
                {
                    if (textEditor.TextArea.TextView.BackgroundRenderers[i] is PreviewHighlighter)
                        textEditor.TextArea.TextView.BackgroundRenderers.RemoveAt(i);
                }

                if (ViewModel.HighlightsOn && !ViewModel.HighlightDisabled && !ViewModel.IsPdf &&
                    ViewModel.GrepResult != null)
                {
                    textEditor.TextArea.TextView.BackgroundRenderers.Add(new PreviewHighlighter(ViewModel.GrepResult));
                }
            }
            else if (e.PropertyName == nameof(ViewModel.LineNumber))
            {
                if (!string.IsNullOrEmpty(textEditor.Text))
                {
                    textEditor.ScrollTo(ViewModel.LineNumber, 0);
                    textEditor.TextArea.Caret.Line = ViewModel.LineNumber;
                }
            }
            else if (e.PropertyName == nameof(ViewModel.CurrentSyntax))
            {
                textEditor.SyntaxHighlighting = ViewModel.HighlightingDefinition;
            }
        }

        public PreviewViewModel ViewModel { get; private set; } = new PreviewViewModel();

        void ViewModel_ShowPreview(object? sender, EventArgs e)
        {
            bool reopenSearchPanel = false;
            if (!searchPanel.IsClosed)
            {
                reopenSearchPanel = true;
                // no callback from the close call
                searchPanel.SearchResultsChanged -= SearchPanel_SearchResultsChanged;
                searchPanel.Close();
            }

            lineNumberMargin.LineToPageMap.Clear();
            textEditor.Clear();
            textEditor.Encoding = ViewModel.Encoding;
            textEditor.SyntaxHighlighting = ViewModel.HighlightingDefinition;
            textEditor.TextArea.TextView.LinkTextForegroundBrush = Application.Current.Resources["AvalonEdit.Link"] as Brush;
            for (int i = textEditor.TextArea.TextView.BackgroundRenderers.Count - 1; i >= 0; i--)
            {
                if (textEditor.TextArea.TextView.BackgroundRenderers[i] is PreviewHighlighter)
                    textEditor.TextArea.TextView.BackgroundRenderers.RemoveAt(i);
            }

            if (ViewModel.HighlightsOn && !ViewModel.HighlightDisabled && !ViewModel.IsPdf &&
                ViewModel.GrepResult != null)
            {
                textEditor.TextArea.TextView.BackgroundRenderers.Add(new PreviewHighlighter(ViewModel.GrepResult));
            }

            bool showPageNumbers = GrepSettings.Instance.Get<PdfNumberType>(GrepSettings.Key.PdfNumberStyle) == PdfNumberType.PageNumber;

            try
            {
                if (!ViewModel.IsLargeOrBinary)
                {
                    if (!string.IsNullOrWhiteSpace(ViewModel.FilePath))
                    {
                        bool isRTL = Utils.IsRTL(ViewModel.FilePath, ViewModel.Encoding);
                        textEditor.FlowDirection = isRTL ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

                        using (FileStream stream = File.Open(ViewModel.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            textEditor.Load(stream);
                        }

                        if (!string.IsNullOrEmpty(textEditor.Text))
                        {
                            if (showPageNumbers && ViewModel.HasPageNumbers)
                            {
                                InitializePageNumbers();

                                string ZWSP = char.ConvertFromUtf32(0x200B); //zero width space 
                                textEditor.BeginChange();
                                foreach (Match match in FormFeedRegex().Matches(textEditor.Text).Cast<Match>())
                                {
                                    textEditor.Document.Replace(match.Index, match.Length, ZWSP);
                                }
                                textEditor.EndChange();
                            }

                            if (reopenSearchPanel)
                            {
                                searchPanel.Open();
                            }

                            UpdatePositionMarkers();

                            textEditor.ScrollTo(ViewModel.LineNumber, 0);
                            textEditor.TextArea.Caret.Line = ViewModel.LineNumber;
                        }
                    }
                    else
                    {
                        textEditor.Text = "";
                        ViewModel.ClearPositionMarkers();
                    }
                }
            }
            catch (Exception ex)
            {
                textEditor.Text = "Error opening the file: " + ex.Message;
            }
            finally
            {
                if (reopenSearchPanel)
                {
                    searchPanel.SearchResultsChanged += SearchPanel_SearchResultsChanged;
                }
            }
        }

        private void InitializePageNumbers()
        {
            using FileStream stream = File.Open(ViewModel.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using StreamReader reader = new(stream);
            int lineNumber = 0;
            int pageNumber = 1;
            lineNumberMargin.LineToPageMap.Add(1, 1);
            while (!reader.EndOfStream)
            {
                lineNumber++;
                string? line = reader.ReadLine();
                if (!string.IsNullOrEmpty(line) && line.Contains('\f', StringComparison.Ordinal) && 
                    !(reader.EndOfStream && line.Equals("\f", StringComparison.Ordinal)))
                {
                    pageNumber += line.Count(c => c.Equals('\f'));
                    if (lineNumberMargin.LineToPageMap.ContainsKey(lineNumber))
                    {
                        lineNumberMargin.LineToPageMap[lineNumber] = pageNumber;
                    }
                    else
                    {
                        lineNumberMargin.LineToPageMap.Add(lineNumber, pageNumber);
                    }
                }
            }
        }

        private void SearchPanel_SearchResultsChanged(object? sender, EventArgs e)
        {
            UpdatePositionMarkers();
        }

        private void TextView_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged)
            {
                UpdatePositionMarkers();
            }
        }

        private void ZoomSlider_ValueChanged(object? sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdatePositionMarkers();
        }

        private void UpdatePositionMarkers()
        {
            if (textEditor != null && textEditor.ViewportHeight > 0 && !string.IsNullOrEmpty(textEditor.Text))
            {
                textEditor.TextArea.TextView.EnsureVisualLines();
                double trackHeight = textEditor.TextArea.TextView.ActualHeight - 2 * SystemParameters.VerticalScrollBarButtonHeight;

                ViewModel.BeginUpdateMarkers();
                var documentHeight = textEditor.TextArea.TextView.DocumentHeight;
                var maxMarkers = trackHeight / 3; //marker height is 3

                if (ViewModel.MarkerLineNumbers.Count < maxMarkers)
                {
                    foreach (int lineNumber in ViewModel.MarkerLineNumbers)
                    {
                        var linePosition = textEditor.TextArea.TextView.GetVisualTopByDocumentLine(lineNumber);

                        ViewModel.AddMarker(linePosition, documentHeight, trackHeight, MarkerType.Global);
                    }
                }

                if (searchPanel.SearchResults.Count < 1000)
                {
                    var lineNumbers = searchPanel.SearchResults
                        .Select(item => textEditor.Document.GetLineByOffset(item.StartOffset).LineNumber).Distinct();

                    if (lineNumbers.Count() < maxMarkers)
                    {
                        foreach (int lineNumber in lineNumbers)
                        {
                            var linePosition = textEditor.TextArea.TextView.GetVisualTopByDocumentLine(lineNumber);

                            ViewModel.AddMarker(linePosition, documentHeight, trackHeight, MarkerType.Local);
                        }
                    }
                }
                ViewModel.EndUpdateMarkers();
            }
        }

        internal void SetFocus()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                textEditor.Focus();
            }), System.Windows.Threading.DispatcherPriority.Normal);
        }

        internal void SaveSettings()
        {
            GrepSettings.Instance.Set<bool>(GrepSettings.Key.PreviewWindowWrap, cbWrapText.IsChecked ?? false);
            GrepSettings.Instance.Set<int>(GrepSettings.Key.PreviewWindowFont, (int)zoomSlider.Value);
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs args)
        {
            base.OnPreviewMouseWheel(args);
            if (Keyboard.IsKeyDown(Key.LeftCtrl) ||
                Keyboard.IsKeyDown(Key.RightCtrl))
            {
                zoomSlider.Value += (args.Delta > 0) ? 1 : -1;
            }
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs args)
        {
            base.OnPreviewMouseDown(args);
            if (Keyboard.IsKeyDown(Key.LeftCtrl) ||
                Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (args.MiddleButton == MouseButtonState.Pressed)
                {
                    zoomSlider.Value = 12;
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsLargeOrBinary = false;
            ViewModel_ShowPreview(this, EventArgs.Empty);
        }

        private void SyntaxButton_Click(object sender, RoutedEventArgs e)
        {
            syntaxContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            syntaxContextMenu.PlacementTarget = (UIElement)sender;
            syntaxContextMenu.IsOpen = true;
        }

        [GeneratedRegex("\f")]
        private static partial Regex FormFeedRegex();
    }
}
