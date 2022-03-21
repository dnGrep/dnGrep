using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml;
using dnGREP.Common;
using dnGREP.Engines;
using dnGREP.Localization;
using Resources = dnGREP.Localization.Properties.Resources;

namespace dnGREP.WPF
{
    public class TestPatternViewModel : BaseMainViewModel
    {
        private bool hasMatches;
        private int searchHash;
        private int replaceHash;
        private List<GrepSearchResult> grepResults = new List<GrepSearchResult>();
        private readonly string horizontalBar = new string(char.ConvertFromUtf32(0x2015)[0], 80);
        private readonly int hexLineSize = GrepSettings.Instance.Get<int>(GrepSettings.Key.HexResultByteLength);

        public TestPatternViewModel()
        {
            ApplicationFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ApplicationFontFamily);
            DialogFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.DialogFontSize);
            ResultsFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ResultsFontFamily);
            ResultsFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.ResultsFontSize);
        }

        private int GetSearchHash()
        {
            unchecked
            {
                int hashCode = 13;
                hashCode = (hashCode * 397) ^ SampleText?.GetHashCode() ?? 5;
                hashCode = (hashCode * 397) ^ SearchFor?.GetHashCode() ?? 5;
                hashCode = (hashCode * 397) ^ TypeOfSearch.GetHashCode();
                hashCode = (hashCode * 397) ^ CaseSensitive.GetHashCode();
                hashCode = (hashCode * 397) ^ WholeWord.GetHashCode();
                hashCode = (hashCode * 397) ^ Multiline.GetHashCode();
                hashCode = (hashCode * 397) ^ Singleline.GetHashCode();
                hashCode = (hashCode * 397) ^ BooleanOperators.GetHashCode();
                hashCode = (hashCode * 397) ^ HighlightCaptureGroups.GetHashCode();
                return hashCode;
            }
        }

        private int GetReplaceHash()
        {
            unchecked
            {
                int hashCode = GetSearchHash();
                hashCode = (hashCode * 397) ^ ReplaceWith?.GetHashCode() ?? 5;
                return hashCode;
            }
        }

        public override void UpdateState(string name)
        {
            base.UpdateState(name);

            if (name == nameof(HighlightCaptureGroups))
            {
                settings.Set(GrepSettings.Key.HighlightCaptureGroups, HighlightCaptureGroups);
            }

            switch (name)
            {
                case nameof(SampleText):
                case nameof(SearchFor):
                case nameof(TypeOfSearch):
                case nameof(CaseSensitive):
                case nameof(WholeWord):
                case nameof(Multiline):
                case nameof(Singleline):
                case nameof(BooleanOperators):
                case nameof(HighlightCaptureGroups):
                    int sHash = GetSearchHash();
                    if (IsValidPattern && sHash != searchHash)
                    {
                        Search();
                        if (TypeOfSearch != SearchType.Hex)
                        {
                            Replace();
                        }

                        searchHash = sHash;
                        replaceHash = GetReplaceHash();
                    }

                    IsReplaceReadOnly = TypeOfSearch == SearchType.Hex;
                    if (IsReplaceReadOnly)
                    {
                        ReplaceWith = string.Empty;
                    }
                    break;

                case nameof(ReplaceWith):
                    int rHash = GetReplaceHash();
                    if (IsValidPattern && rHash != replaceHash && TypeOfSearch != SearchType.Hex)
                    {
                        Replace();

                        replaceHash = rHash;
                    }
                    break;
            }
        }

        private string applicationFontFamily;
        public string ApplicationFontFamily
        {
            get { return applicationFontFamily; }
            set
            {
                if (applicationFontFamily == value)
                    return;

                applicationFontFamily = value;
                base.OnPropertyChanged(nameof(ApplicationFontFamily));
            }
        }

        private double dialogfontSize;
        public double DialogFontSize
        {
            get { return dialogfontSize; }
            set
            {
                if (dialogfontSize == value)
                    return;

                dialogfontSize = value;
                base.OnPropertyChanged(nameof(DialogFontSize));
            }
        }

        private static string sampleText;
        public string SampleText
        {
            get { return sampleText; }
            set
            {
                if (value == sampleText)
                    return;

                sampleText = value;
                base.OnPropertyChanged(nameof(SampleText));
            }
        }

        private bool isReplaceReadOnly;
        public bool IsReplaceReadOnly
        {
            get { return isReplaceReadOnly; }
            set
            {
                if (value == isReplaceReadOnly)
                    return;

                isReplaceReadOnly = value;
                base.OnPropertyChanged(nameof(IsReplaceReadOnly));
            }
        }

        private InlineCollection searchOutput;
        public InlineCollection SearchOutput
        {
            get { return searchOutput; }
            set
            {
                if (value == searchOutput)
                    return;

                searchOutput = value;
                base.OnPropertyChanged(nameof(SearchOutput));
            }
        }

        private InlineCollection replaceOutput;
        public InlineCollection ReplaceOutput
        {
            get { return replaceOutput; }
            set
            {
                if (value == replaceOutput)
                    return;

                replaceOutput = value;
                base.OnPropertyChanged(nameof(ReplaceOutput));
            }
        }

        private string replaceOutputText;
        public string ReplaceOutputText
        {
            get { return replaceOutputText; }
            set
            {
                if (value == replaceOutputText)
                    return;

                replaceOutputText = value;

                base.OnPropertyChanged(nameof(ReplaceOutputText));
            }
        }

        private string replaceErrorText;
        public string ReplaceErrorText
        {
            get { return replaceErrorText; }
            set
            {
                if (value == replaceErrorText)
                    return;

                replaceErrorText = value;

                base.OnPropertyChanged(nameof(ReplaceErrorText));
            }
        }

        private GrepEngineInitParams InitParameters
        {
            get
            {
                return new GrepEngineInitParams(
                    GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowLinesInContext),
                    GrepSettings.Instance.Get<int>(GrepSettings.Key.ContextLinesBefore),
                    GrepSettings.Instance.Get<int>(GrepSettings.Key.ContextLinesAfter),
                    GrepSettings.Instance.Get<double>(GrepSettings.Key.FuzzyMatchThreshold),
                    GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowVerboseMatchCount),
                false);
            }
        }

        private GrepSearchOption SearchOptions
        {
            get
            {
                GrepSearchOption searchOptions = GrepSearchOption.None;
                if (Multiline)
                    searchOptions |= GrepSearchOption.Multiline;
                if (CaseSensitive)
                    searchOptions |= GrepSearchOption.CaseSensitive;
                if (Singleline)
                    searchOptions |= GrepSearchOption.SingleLine;
                if (WholeWord)
                    searchOptions |= GrepSearchOption.WholeWord;
                if (BooleanOperators)
                    searchOptions |= GrepSearchOption.BooleanOperators;

                return searchOptions;
            }
        }

        private async void Search()
        {
            hasMatches = false;
            grepResults.Clear();

            if (string.IsNullOrEmpty(SampleText) || string.IsNullOrEmpty(SearchFor))
            {
                SearchOutput = new Paragraph().Inlines;
                return;
            }

            IGrepEngine engine;
            Encoding encoding;
            if (TypeOfSearch == SearchType.Hex)
            {
                engine = new GrepEngineHex();
                encoding = Encoding.UTF8;
            }
            else
            {
                engine = new GrepEnginePlainText();
                encoding = Encoding.Unicode;
            }
            engine.Initialize(InitParameters, new FileFilter());

            using (Stream inputStream = new MemoryStream(encoding.GetBytes(SampleText)))
            {
                try
                {
                    grepResults = engine.Search(inputStream, "test.txt", SearchFor, TypeOfSearch,
                        SearchOptions, encoding);

                    if (grepResults != null)
                    {
                        if (TypeOfSearch == SearchType.Hex)
                        {
                            using (Stream stream = new MemoryStream(encoding.GetBytes(SampleText)))
                            {
                                using (BinaryReader reader = new BinaryReader(stream))
                                {
                                    foreach (var result in grepResults)
                                    {
                                        if (!result.HasSearchResults)
                                        {
                                            result.SearchResults = Utils.GetLinesHexFormat(reader, result.Matches, 0, 0);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            using (StringReader reader = new StringReader(SampleText))
                            {
                                foreach (var result in grepResults)
                                {
                                    if (!result.HasSearchResults)
                                    {
                                        result.SearchResults = Utils.GetLinesEx(reader, result.Matches, 0, 0);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (ArgumentException ex)
                {
                    MessageBox.Show(Resources.MessageBox_IncorrectPattern + ex.Message,
                        Resources.MessageBox_DnGrep,
                        MessageBoxButton.OK, MessageBoxImage.Warning,
                        MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                }
            }

            SearchResults.Clear();
            SearchResults.AddRangeForTestView(grepResults);
            Paragraph paragraph = new Paragraph();
            if (SearchResults.Count == 1)
            {
                await SearchResults[0].FormattedLines.LoadAsync();
                foreach (FormattedGrepLine line in SearchResults[0].FormattedLines)
                {
                    if (line.IsSectionBreak)
                    {
                        paragraph.Inlines.Add(new Run(horizontalBar)
                        {
                            FontWeight = FontWeights.Light,
                            Foreground = Application.Current.Resources["TreeView.Section.Border"] as Brush,
                        });
                        paragraph.Inlines.Add(new LineBreak());
                    }

                    // Copy children Inline to a temporary array.
                    if (TypeOfSearch == SearchType.Hex)
                    {
                        var run = new Run(line.FormattedLineNumber)
                        {
                            Background = new SolidColorBrush(Color.FromArgb(92, 180, 180, 180)),
                            FontFamily = new FontFamily(ApplicationFontFamily),
                        };
                        paragraph.Inlines.Add(run);
                        paragraph.Inlines.Add(new Run(" "));
                        paragraph.Inlines.AddRange(line.FormattedText.ToList());
                        int length = GetLineLength(line.FormattedText);
                        int spaces = (hexLineSize * 3) - length + 3;
                        paragraph.Inlines.Add(new Run(new string(' ', spaces) + line.FormattedHexValues));
                    }
                    else
                    {
                        paragraph.Inlines.AddRange(line.FormattedText.ToList());
                    }
                    paragraph.Inlines.Add(new LineBreak());
                    hasMatches = true;
                }
            }
            else
            {
                paragraph.Inlines.Add(new Run(Resources.Test_NoMatchesFound));
            }
            SearchOutput = paragraph.Inlines;
        }

        private int GetLineLength(InlineCollection formattedText)
        {
            int length = 0;
            foreach (var inline in formattedText)
            {
                if (inline is Run run)
                {
                    length += run.Text.Length;
                }
            }
            return length;
        }

        private void Replace()
        {
            ReplaceErrorText = string.Empty;

            if (string.IsNullOrEmpty(SampleText) || string.IsNullOrEmpty(SearchFor) || !hasMatches)
            {
                ReplaceOutput = new Paragraph().Inlines;
                return;
            }

            string replaceString = ReplaceWith ?? string.Empty;

            GrepEnginePlainText engine = new GrepEnginePlainText();
            engine.Initialize(InitParameters, new FileFilter());

            string replacedString = string.Empty;
            try
            {
                // mark all matches for replace
                if (grepResults.Count > 0)
                {
                    foreach (var match in grepResults[0].Matches)
                    {
                        match.ReplaceMatch = true;
                    }
                }
                else
                {
                    ReplaceOutput = new Paragraph().Inlines;
                    return;
                }

                using (Stream inputStream = new MemoryStream(Encoding.Unicode.GetBytes(SampleText)))
                using (Stream writeStream = new MemoryStream())
                {
                    engine.Replace(inputStream, writeStream, SearchFor, replaceString, TypeOfSearch,
                        SearchOptions, Encoding.Unicode, grepResults[0].Matches);
                    writeStream.Position = 0;
                    using (StreamReader reader = new StreamReader(writeStream))
                    {
                        replacedString = reader.ReadToEnd();
                    }
                }
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(Resources.MessageBox_IncorrectPattern + ex.Message,
                    Resources.MessageBox_DnGrep,
                    MessageBoxButton.OK, MessageBoxImage.Warning,
                    MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
            }
            catch (XmlException)
            {
                ReplaceErrorText = Resources.Test_ReplaceTextIsNotValidXML;
            }
            catch (Exception ex)
            {
                MessageBox.Show(Resources.MessageBox_Error + ex.Message, Resources.MessageBox_DnGrep,
                    MessageBoxButton.OK, MessageBoxImage.Error,
                    MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
            }

            Paragraph paragraph = new Paragraph();
            paragraph.Inlines.Add(new Run(replacedString));
            ReplaceOutput = paragraph.Inlines;
            ReplaceOutputText = replacedString;
        }
    }
}
