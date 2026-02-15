using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using CommunityToolkit.Mvvm.ComponentModel;
using dnGREP.Common;
using dnGREP.Localization;
using dnGREP.Localization.Properties;
using dnGREP.WPF.UserControls;

namespace dnGREP.WPF
{
    public partial class FormattedGrepLine : CultureAwareViewModel, ITreeItem
    {
        private static readonly string enQuad = char.ConvertFromUtf32(0x2000);
        private static readonly string middot = char.ConvertFromUtf32(0X00B7);
        private static readonly string degree = char.ConvertFromUtf32(0X00B0);
        private static readonly string tab = char.ConvertFromUtf32(0x00BB);
        private static readonly string newLine = char.ConvertFromUtf32(0x00B6);

        public FormattedGrepLine(GrepLine line, FormattedGrepResult parent, int initialColumnWidth, bool breakSection)
        {
            Parent = parent;
            GrepLine = line;
            Parent.PropertyChanged += Parent_PropertyChanged;
            LineNumberColumnWidth = initialColumnWidth;
            IsSectionBreak = breakSection;
            WrapText = Parent.WrapText;
            ViewWhitespace = Parent.ViewWhitespace;
            int lineSize = GrepSettings.Instance.Get<int>(GrepSettings.Key.HexResultByteLength);
            var pdfNumberStyle = GrepSettings.Instance.Get<PdfNumberType>(GrepSettings.Key.PdfNumberStyle);

            LineNumberAlignment = TranslationSource.Instance.CurrentCulture.TextInfo.IsRightToLeft ? TextAlignment.Left : TextAlignment.Right;

            if (pdfNumberStyle == PdfNumberType.PageNumber && line.PageNumber > -1)
            {
                FormattedLineNumber = line.PageNumber.ToString();
            }
            else
            {
                FormattedLineNumber = line.LineNumber == -1 ? string.Empty :
                    line.IsHexFile ? string.Format("{0:X8}", (line.LineNumber - 1) * lineSize) :
                    line.LineNumber.ToString();
            }

            //string fullText = lineSummary;
            if (line.IsContext)
            {
                Style = "Context";
            }
            if (line.LineNumber == -1 && string.IsNullOrEmpty(line.LineText))
            {
                Style = "Empty";
            }
        }

        public GrepLine GrepLine { get; private set; }
        public string FormattedLineNumber { get; private set; }

        public TextAlignment LineNumberAlignment { get; private set; } = TextAlignment.Right;

        private InlineCollection? formattedText;
        public InlineCollection? FormattedText
        {
            get
            {
                LoadFormattedText();
                return formattedText;
            }
        }

        public void LoadFormattedText()
        {
            if (formattedText == null || formattedText.Count == 0)
            {
                formattedText = FormatLine(GrepLine);

                if (GrepLine.IsHexFile)
                {
                    IsHexData = true;
                    ResultColumn1Width = "Auto";
                    ResultColumn2Width = "Auto";
                    ResultColumn1SharedSizeGroupName = "COL1";
                    FormattedHexValues = FormatHexValues(GrepLine);
                }
                else
                {
                    IsHexData = false;
                    ResultColumn1Width = "*";
                    ResultColumn2Width = "0";
                    ResultColumn1SharedSizeGroupName = null;
                }
            }
        }

        [ObservableProperty]
        private string? formattedHexValues;

        [ObservableProperty]
        private bool isHexData;

        [ObservableProperty]
        private string? resultColumn1SharedSizeGroupName = null; // cannot be empty string, but looks like null works

        [ObservableProperty]
        private string resultColumn1Width = "*";

        [ObservableProperty]
        private string resultColumn2Width = "0";

        // FormattedGrepLines don't expand, but the XAML code expects this property on TreeViewItems
        public bool IsExpanded { get; set; }

        [ObservableProperty]
        private bool isSelected;
        partial void OnIsSelectedChanged(bool value)
        {
            GrepSearchResultsViewModel.SearchResultsMessenger.NotifyColleagues("IsSelectedChanged", this);
        }

        [ObservableProperty]
        private bool isSectionBreak = false;

        public string Style { get; private set; } = "";

        [ObservableProperty]
        private int lineNumberColumnWidth = 30;

        void Parent_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LineNumberColumnWidth))
                LineNumberColumnWidth = Parent.LineNumberColumnWidth;
        }

        public static bool HighlightCaptureGroups
        {
            get { return GrepSettings.Instance.Get<bool>(GrepSettings.Key.HighlightCaptureGroups); }
        }

        public FormattedGrepResult Parent { get; private set; }

        [ObservableProperty]
        private bool wrapText;
        partial void OnWrapTextChanged(bool value)
        {
            MaxLineLength = value ? 10000 : 500;
        }

        public int MaxLineLength { get; private set; } = 500;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FormattedText))]
        private bool viewWhitespace;

        partial void OnViewWhitespaceChanging(bool value)
        {
            if (formattedText != null)
            {
                formattedText = null;
            }
        }

        public int Level => 1;

        public IEnumerable<ITreeItem> Children => [];

        private InlineCollection FormatLine(GrepLine line)
        {
            Paragraph paragraph = new();

            string fullLine = line.LineText;
            if (line.LineText.Length > MaxLineLength)
            {
                fullLine = line.LineText[..MaxLineLength];
            }

            int column = 0;

            if (line.Matches.Count == 0)
            {
                Run mainRun = new(MarkWhitespace(fullLine, ref column));
                paragraph.Inlines.Add(mainRun);
            }
            else
            {
                int counter = 0;
                GrepMatch[] lineMatches = new GrepMatch[line.Matches.Count];
                line.Matches.CopyTo(lineMatches);
                foreach (GrepMatch m in lineMatches)
                {
                    _ = Parent.GetMatchNumber(m.FileMatchId);
                    int matchStartLocation = m.StartLocation;
                    int matchLength = m.Length;
                    if (matchStartLocation < counter)
                    {
                        // overlapping match: continue highlight from previous end
                        int overlap = counter - matchStartLocation;
                        matchStartLocation = counter;
                        matchLength -= overlap;
                    }

                    try
                    {
                        string? regLine = null;
                        string? fmtLine = null;
                        if (matchStartLocation < fullLine.Length)
                        {
                            regLine = fullLine[counter..matchStartLocation];
                        }

                        if (matchStartLocation + matchLength <= fullLine.Length)
                        {
                            fmtLine = fullLine.Substring(matchStartLocation, matchLength);
                        }
                        else if (fullLine.Length > matchStartLocation)
                        {
                            // match may include the non-printing newline chars at the end of the line: don't overflow the length
                            fmtLine = fullLine[matchStartLocation..];
                        }
                        else
                        {
                            // past the end of the line: line may be truncated, or it may be the newline chars
                        }

                        if (regLine != null)
                        {
                            Run regularRun = new(MarkWhitespace(regLine, ref column));
                            paragraph.Inlines.Add(regularRun);
                        }
                        if (fmtLine != null)
                        {
                            if (HighlightCaptureGroups && m.Groups.Count > 0)
                            {
                                FormatCaptureGroups(paragraph, m, fmtLine, ref column);
                            }
                            else
                            {
                                Run run = new(MarkWhitespace(fmtLine, ref column));
                                run.SetResourceReference(Run.ForegroundProperty, "Match.Highlight.Foreground");
                                run.SetResourceReference(Run.BackgroundProperty, "Match.Highlight.Background");
                                paragraph.Inlines.Add(run);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    catch
                    {
                        // on error show the whole line with no highlights
                        paragraph.Inlines.Clear();
                        column = 0;
                        Run regularRun = new(MarkWhitespace(fullLine, ref column));
                        paragraph.Inlines.Add(regularRun);
                        // set position to end of line
                        matchStartLocation = fullLine.Length;
                        matchLength = 0;
                    }
                    finally
                    {
                        counter = matchStartLocation + matchLength;
                    }
                }

                if (counter < fullLine.Length)
                {
                    try
                    {
                        string regLine = fullLine[counter..];
                        Run regularRun = new(MarkWhitespace(regLine, ref column));
                        paragraph.Inlines.Add(regularRun);
                    }
                    catch
                    {
                        column = 0;
                        Run regularRun = new(MarkWhitespace(fullLine, ref column));
                        paragraph.Inlines.Add(regularRun);
                    }
                }

                if (line.LineText.Length > MaxLineLength)
                {
                    string msg = TranslationSource.Format(Resources.Main_ResultList_CountAdditionalCharacters, line.LineText.Length - MaxLineLength);

                    var msgRun = new Run(msg);
                    msgRun.SetResourceReference(Run.ForegroundProperty, "TreeView.Message.Highlight.Foreground");
                    msgRun.SetResourceReference(Run.BackgroundProperty, "TreeView.Message.Highlight.Background");
                    paragraph.Inlines.Add(msgRun);

                    var hiddenMatches = line.Matches.Where(m => m.StartLocation > MaxLineLength).Select(m => m);
                    int count = hiddenMatches.Count();
                    if (count > 0)
                    {
                        paragraph.Inlines.Add(new Run(" " + Resources.Main_ResultList_AdditionalMatches));
                    }

                    // if close to getting them all, then take them all,
                    // otherwise, stop at 20 and just show the remaining count
                    int takeCount = count > 25 ? 20 : count;

                    foreach (GrepMatch m in hiddenMatches.Take(takeCount))
                    {
                        if (m.StartLocation + m.Length <= line.LineText.Length)
                        {
                            paragraph.Inlines.Add(new Run(enQuad));
                            string fmtLine = line.LineText.Substring(m.StartLocation, m.Length);
                            // hidden matches are shown out of line context, so column tracking
                            // is not meaningful here; use a separate column starting at 0
                            int hiddenCol = 0;
                            var run = new Run(MarkWhitespace(fmtLine, ref hiddenCol));
                            run.SetResourceReference(Run.ForegroundProperty, "Match.Highlight.Foreground");
                            run.SetResourceReference(Run.BackgroundProperty, "Match.Highlight.Background");
                            paragraph.Inlines.Add(run);

                            if (m.StartLocation + m.Length == line.LineText.Length)
                                paragraph.Inlines.Add(new Run(" " + Resources.Main_ResultList_AtEndOfLine));
                            else
                                paragraph.Inlines.Add(new Run(" " + TranslationSource.Format(Resources.Main_ResultList_AtPosition, m.StartLocation)));
                        }
                    }

                    if (count > takeCount)
                    {
                        paragraph.Inlines.Add(new Run(TranslationSource.Format(Resources.Main_ResultList_PlusCountMoreMatches, count - takeCount)));
                    }
                }
            }
            if (ViewWhitespace)
            {
                Run lastRun = new(newLine);
                paragraph.Inlines.Add(lastRun);
            }
            return paragraph.Inlines;
        }

        private string MarkWhitespace(string text, ref int column)
        {
            if (ViewWhitespace && !string.IsNullOrEmpty(text))
            {
                StringBuilder sb = new(text.Length);
                foreach (char ch in text)
                {
                    if (ch == '\t')
                    {
                        int spacesToNextTab = Utils.WhitespaceTabSize - (column % Utils.WhitespaceTabSize);
                        // Use the glyph as the first character, then pad with spaces
                        // to reach the same position a real tab would have landed on
                        sb.Append(tab);
                        sb.Append(' ', spacesToNextTab - 1);
                        column += spacesToNextTab;
                    }
                    else if (ch == ' ')
                    {
                        // simple space gets the middle dot
                        sb.Append(middot);
                        column++;
                    }
                    else if (char.IsWhiteSpace(ch))
                    {
                        // all other whitespace get the degree sign
                        sb.Append(degree);
                        column++;
                    }
                    else
                    {
                        sb.Append(ch);
                        column++;
                    }
                }
                return sb.ToString();
            }
            column += text.Length;
            return text;
        }

        private void FormatCaptureGroups(Paragraph paragraph, GrepMatch match, string fmtLine, ref int column)
        {
            if (paragraph == null || match == null || string.IsNullOrEmpty(fmtLine))
                return;

            GroupMap map = new(match, fmtLine);
            foreach (var range in map.Ranges.Where(r => r.Length > 0))
            {
                var run = new Run(MarkWhitespace(range.RangeText, ref column));
                if (range.Group == null)
                {
                    run.SetResourceReference(Run.ForegroundProperty, "Match.Highlight.Foreground");
                    run.SetResourceReference(Run.BackgroundProperty, "Match.Highlight.Background");
                    run.ToolTip = TranslationSource.Format(Resources.Main_ResultList_MatchToolTip1, Parent.GetMatchNumber(match.FileMatchId), Environment.NewLine, match.RegexMatchValue.TrimEnd('\r', '\n'));
                    paragraph.Inlines.Add(run);
                }
                else
                {
                    if (!Parent.GroupColors.TryGetValue(range.Group.Name, out string? bgColor))
                    {
                        int groupIdx = Parent.GroupColors.Count % 10;
                        bgColor = $"Match.Group.{groupIdx}.Highlight.Background";
                        Parent.GroupColors.Add(range.Group.Name, bgColor);
                    }
                    run.SetResourceReference(Run.ForegroundProperty, "Match.Highlight.Foreground");
                    run.SetResourceReference(Run.BackgroundProperty, bgColor);
                    run.ToolTip = TranslationSource.Format(Resources.Main_ResultList_MatchToolTip2,
                        Parent.GetMatchNumber(match.FileMatchId), Environment.NewLine, range.Group.Name, range.Group.FullValue.TrimEnd('\r', '\n'));
                    paragraph.Inlines.Add(run);
                }
            }

            if (ViewWhitespace && paragraph.Inlines.Last() is Run last)
            {
                last.Text += newLine;
            }
        }

        private string FormatHexValues(GrepLine grepLine)
        {
            string[] parts = grepLine.LineText.TrimEnd().Split(' ');
            List<byte> list = [];
            foreach (string num in parts)
            {
                if (byte.TryParse(num, System.Globalization.NumberStyles.HexNumber, null, out byte result))
                {
                    list.Add(result);
                }
            }
            string text = Parent.GrepResult.Encoding.GetString(list.ToArray());
            List<char> nonPrintableChars = [];
            for (int idx = 0; idx < text.Length; idx++)
            {
                if (!char.IsLetterOrDigit(text[idx]) && !char.IsPunctuation(text[idx]) && text[idx] != ' ')
                {
                    nonPrintableChars.Add(text[idx]);
                }
            }
            foreach (char c in nonPrintableChars)
            {
                text = text.Replace(c, '.');
            }
            return text;
        }

        private class GroupMap
        {
            private readonly int start;
            private readonly List<Range> ranges = [];
            public GroupMap(GrepMatch match, string text)
            {
                start = match.StartLocation;
                MatchText = text;
                ranges.Add(new Range(0, MatchText.Length, this, null));

                foreach (var group in match.Groups.OrderByDescending(g => g.Length))
                {
                    Insert(group);
                }
                ranges.Sort();
            }

            public IEnumerable<Range> Ranges => ranges;

            public string MatchText { get; }

            private void Insert(GrepCaptureGroup group)
            {
                int startIndex = group.StartLocation - start;
                int endIndex = startIndex + group.Length;

                //gggggg
                //xxxxxx
                var replace = ranges.FirstOrDefault(r => r.Start == startIndex && r.End == endIndex);
                if (replace != null)
                {
                    ranges.Remove(replace);
                    ranges.Add(new Range(startIndex, endIndex, this, group));
                }
                else
                {
                    //gg
                    //xxxxxx
                    var head = ranges.FirstOrDefault(r => r.Start == startIndex && r.End > endIndex);
                    if (head != null)
                    {
                        ranges.Remove(head);
                        ranges.Add(new Range(startIndex, endIndex, this, group));
                        ranges.Add(new Range(endIndex, head.End, this, head.Group));
                    }
                    else
                    {
                        //    gg
                        //xxxxxx
                        var tail = ranges.FirstOrDefault(r => r.Start < startIndex && r.End == endIndex);
                        if (tail != null)
                        {
                            ranges.Remove(tail);
                            ranges.Add(new Range(tail.Start, startIndex, this, tail.Group));
                            ranges.Add(new Range(startIndex, endIndex, this, group));
                        }
                        else
                        {
                            //  gg
                            //xxxxxx
                            var split = ranges.FirstOrDefault(r => r.Start < startIndex && r.End > endIndex);
                            if (split != null)
                            {
                                ranges.Remove(split);
                                ranges.Add(new Range(split.Start, startIndex, this, split.Group));
                                ranges.Add(new Range(startIndex, endIndex, this, group));
                                ranges.Add(new Range(endIndex, split.End, this, split.Group));
                            }
                            else
                            {
                                //   gggg  
                                //xxxxxyyyyy
                                var spans = ranges.Where(r => (r.Start < startIndex && r.End < endIndex) ||
                                    (r.Start > startIndex && r.End > endIndex)).OrderBy(r => r.Start).ToList();

                                if (spans.Count == 2)
                                {
                                    ranges.Remove(spans[0]);
                                    ranges.Remove(spans[1]);
                                    ranges.Add(new Range(spans[0].Start, startIndex, this, spans[0].Group));
                                    ranges.Add(new Range(startIndex, endIndex, this, group));
                                    ranges.Add(new Range(endIndex, spans[1].End, this, spans[1].Group));
                                }
                            }
                        }
                    }
                }
            }
        }

        private class Range(int start, int end, GroupMap parent, GrepCaptureGroup? group)
            : IComparable<Range>, IComparable, IEquatable<Range>
        {
            public int Start { get; } = Math.Min(start, end);
            public int End { get; } = Math.Max(start, end);

            public int Length { get { return End - Start; } }

            public string RangeText { get { return parent.MatchText.Substring(Start, Length); } }

            public GrepCaptureGroup? Group { get; } = group;

            public int CompareTo(object? obj)
            {
                return CompareTo(obj as Range);
            }

            public int CompareTo(Range? other)
            {
                if (other == null)
                    return 1;
                else
                    return Start.CompareTo(other.Start); // should never be equal
            }

            public override bool Equals(object? obj)
            {
                return Equals(obj as Range);
            }

            public bool Equals(Range? other)
            {
                if (other == null) return false;

                return Start == other.Start &&
                    End == other.End;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Start, End);
            }

            public override string ToString()
            {
                return $"{Start} - {End}:  {RangeText}";
            }
        }
    }

}
