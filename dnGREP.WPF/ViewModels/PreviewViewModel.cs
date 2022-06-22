using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Alphaleonis.Win32.Filesystem;
using dnGREP.Common;
using dnGREP.Localization;
using dnGREP.Localization.Properties;
using ICSharpCode.AvalonEdit.Highlighting;
using NLog;

namespace dnGREP.WPF
{
    public class PreviewViewModel : CultureAwareViewModel, INotifyPropertyChanged
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public PreviewViewModel()
        {
            InitializeHighlighters();

            HighlightsOn = GrepSettings.Instance.Get<bool>(GrepSettings.Key.HighlightMatches);

            ApplicationFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ApplicationFontFamily);
            MainFormFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.MainFormFontSize);
            ResultsFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ResultsFontFamily);
            UpdatePersonalization(GrepSettings.Instance.Get<bool>(GrepSettings.Key.PersonalizationOn));

            PropertyChanged += PreviewViewModel_PropertyChanged;

            TranslationSource.Instance.CurrentCultureChanged += (s, e) =>
            {
                bool resetCurrentSyntax = CurrentSyntax == Highlighters[0];
                Highlighters[0] = Resources.Preview_SyntaxNone;
                if (resetCurrentSyntax)
                {
                    CurrentSyntax = Resources.Preview_SyntaxNone;
                }
            };
        }

        private void InitializeHighlighters()
        {
            var items = ThemedHighlightingManager.Instance.HighlightingNames.ToList();
            items.Sort();
            items.Insert(0, Resources.Preview_SyntaxNone);
            Highlighters.Clear();
            foreach (var item in items)
            {
                Highlighters.Add(item);
            }

            CurrentSyntax = Resources.Preview_SyntaxNone;
        }

        public event EventHandler ShowPreview;

        private bool isLargeOrBinary;
        public bool IsLargeOrBinary
        {
            get { return isLargeOrBinary; }
            set
            {
                if (value == isLargeOrBinary)
                    return;

                isLargeOrBinary = value;
                base.OnPropertyChanged(nameof(IsLargeOrBinary));
            }
        }

        private bool isPdf;
        public bool IsPdf
        {
            get { return isPdf; }
            set
            {
                if (value == isPdf)
                    return;

                isPdf = value;
                base.OnPropertyChanged(nameof(IsPdf));
            }
        }

        private string currentSyntax;
        public string CurrentSyntax
        {
            get { return currentSyntax; }
            set
            {
                if (value == currentSyntax || value == null)
                    return;

                currentSyntax = value;
                base.OnPropertyChanged(nameof(CurrentSyntax));
            }
        }

        public ObservableCollection<string> Highlighters { get; } = new ObservableCollection<string>();

        public ObservableCollection<Marker> Markers { get; } = new ObservableCollection<Marker>();

        public Encoding Encoding { get; set; }

        private string filePath;
        public string FilePath
        {
            get { return filePath; }
            set
            {
                if (value == filePath)
                    return;

                filePath = value;
                base.OnPropertyChanged(nameof(FilePath));
            }
        }

        private GrepSearchResult grepResult;
        public GrepSearchResult GrepResult
        {
            get { return grepResult; }
            set
            {
                if (value == grepResult)
                    return;

                grepResult = value;
                base.OnPropertyChanged(nameof(GrepResult));
            }
        }

        private int lineNumber;
        public int LineNumber
        {
            get { return lineNumber; }
            set
            {
                if (value == lineNumber)
                    return;

                lineNumber = value;
                base.OnPropertyChanged(nameof(LineNumber));
            }
        }

        private bool highlightsOn = true;
        public bool HighlightsOn
        {
            get { return highlightsOn; }
            set
            {
                if (value == highlightsOn)
                    return;

                highlightsOn = value;
                base.OnPropertyChanged(nameof(HighlightsOn));
            }
        }

        private bool highlightDisabled;
        public bool HighlightDisabled
        {
            get { return highlightDisabled; }
            set
            {
                if (value == highlightDisabled)
                    return;

                highlightDisabled = value;
                base.OnPropertyChanged(nameof(HighlightDisabled));
            }
        }

        public IHighlightingDefinition HighlightingDefinition
        {
            get
            {
                return ThemedHighlightingManager.Instance.GetDefinition(CurrentSyntax);
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

        private double mainFormfontSize;
        public double MainFormFontSize
        {
            get { return mainFormfontSize; }
            set
            {
                if (mainFormfontSize == value)
                    return;

                mainFormfontSize = value;
                base.OnPropertyChanged(nameof(MainFormFontSize));
            }
        }

        private string resultsFontFamily;
        public string ResultsFontFamily
        {
            get { return resultsFontFamily; }
            set
            {
                if (resultsFontFamily == value)
                    return;

                resultsFontFamily = value;
                base.OnPropertyChanged(nameof(ResultsFontFamily));
            }
        }

        public List<int> MarkerLineNumbers = new List<int>();

        void PreviewViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateState(e.PropertyName);
        }

        private void UpdateState(string name)
        {
            if (name == nameof(GrepResult))
            {
                MarkerLineNumbers = GrepResult.SearchResults.Where(sr => !sr.IsContext)
                    .Select(sr => sr.LineNumber).Distinct().ToList();

            }

            if (name == nameof(FilePath))
            {
                ClearPositionMarkers();
                if (!string.IsNullOrEmpty(filePath) &&
                    File.Exists(FilePath))
                {
                    // Set current definition
                    var fileInfo = new FileInfo(FilePath);
                    var definition = ThemedHighlightingManager.Instance.GetDefinitionByExtension(fileInfo.Extension);
                    CurrentSyntax = definition != null ? definition.Name : Resources.Preview_SyntaxNone;

                    try
                    {
                        // Do not preview files over 4MB or binary
                        IsPdf = Utils.IsPdfFile(FilePath);
                        IsLargeOrBinary = fileInfo.Length > 4096000 || Utils.IsBinary(FilePath) || IsPdf;
                    }
                    catch (System.IO.IOException ex)
                    {
                        // Is the file locked and cannot be read by IsBinary?
                        // message is shown in the preview window
                        logger.Error(ex, "Failure in check for large or binary");
                    }

                    // Disable highlighting for large number of matches
                    HighlightDisabled = GrepResult?.Matches?.Count > 5000;

                    // Tell View to show window
                    ShowPreview?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    // Tell View to show window and clear content
                    ShowPreview?.Invoke(this, EventArgs.Empty);
                }
            }

        }

        internal void ClearPositionMarkers()
        {
            Markers.Clear();
            OnPropertyChanged(nameof(Markers));
        }

        internal void BeginUpdateMarkers()
        {
            Markers.Clear();
        }

        internal void AddMarker(double linePosition, double documentHeight, double trackHeight, MarkerType markerType)
        {
            double position = (documentHeight < trackHeight) ? linePosition : linePosition * trackHeight / documentHeight;
            Markers.Add(new Marker(position, markerType));
        }

        internal void EndUpdateMarkers()
        {
            OnPropertyChanged(nameof(Markers));
        }

        internal void UpdatePersonalization(bool personalizationOn)
        {
            PreviewZoomWndVisible = personalizationOn ? GrepSettings.Instance.Get<bool>(GrepSettings.Key.PreviewZoomWndVisible) : true;
            WrapTextPreviewWndVisible = personalizationOn ? GrepSettings.Instance.Get<bool>(GrepSettings.Key.WrapTextPreviewWndVisible) : true;
            SyntaxPreviewWndVisible = personalizationOn ? GrepSettings.Instance.Get<bool>(GrepSettings.Key.SyntaxPreviewWndVisible) : true;
        }


        private bool zoomPreviewWndVisible = true;
        public bool PreviewZoomWndVisible
        {
            get { return zoomPreviewWndVisible; }
            set
            {
                if (zoomPreviewWndVisible == value)
                {
                    return;
                }

                zoomPreviewWndVisible = value;
                OnPropertyChanged(nameof(PreviewZoomWndVisible));
            }
        }


        private bool wrapTextPreviewWndVisible = true;
        public bool WrapTextPreviewWndVisible
        {
            get { return wrapTextPreviewWndVisible; }
            set
            {
                if (wrapTextPreviewWndVisible == value)
                {
                    return;
                }

                wrapTextPreviewWndVisible = value;
                OnPropertyChanged(nameof(WrapTextPreviewWndVisible));
            }
        }


        private bool syntaxPreviewWndVisible = true;
        public bool SyntaxPreviewWndVisible
        {
            get { return syntaxPreviewWndVisible; }
            set
            {
                if (syntaxPreviewWndVisible == value)
                {
                    return;
                }

                syntaxPreviewWndVisible = value;
                OnPropertyChanged(nameof(SyntaxPreviewWndVisible));
            }
        }
    }

    public enum MarkerType { Global, Local }

    public class Marker
    {
        public Marker(double position, MarkerType markerType)
        {
            Position = position;
            MarkerType = markerType;
        }

        public double Position { get; private set; }
        public MarkerType MarkerType { get; private set; }
    }

}
