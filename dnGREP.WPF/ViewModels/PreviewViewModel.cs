using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using dnGREP.Common;
using dnGREP.Localization;
using dnGREP.Localization.Properties;
using ICSharpCode.AvalonEdit.Highlighting;
using NLog;

namespace dnGREP.WPF
{
    public partial class PreviewViewModel : CultureAwareViewModel
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
                bool resetCurrentSyntax = CurrentSyntax == SyntaxItems[0].Header;
                if (resetCurrentSyntax)
                {
                    CurrentSyntax = Resources.Preview_SyntaxNone;
                }
            };
        }

        private void InitializeHighlighters()
        {
            var items = ThemedHighlightingManager.Instance.HighlightingNames;
            var grouping = items.OrderBy(s => s)
                .GroupBy(s => s[0])
                .Select(g => new { g.Key, Items = g.ToArray() });

            string noneItem = Resources.Preview_SyntaxNone;
            SyntaxItems.Add(new MenuItemViewModel(noneItem, true,
                new RelayCommand(p => CurrentSyntax = noneItem)));

            foreach (var group in grouping)
            {
                var parent = new MenuItemViewModel(group.Key.ToString(), null);
                SyntaxItems.Add(parent);

                foreach (var child in group.Items)
                {
                    parent.Children.Add(new MenuItemViewModel(child, true,
                        new RelayCommand(p => CurrentSyntax = child)));
                }
            }

            CurrentSyntax = Resources.Preview_SyntaxNone;
        }

        private void SelectCurrentSyntax(string syntaxName)
        {
            // creates a radio group for all the syntax context menu items
            foreach (var item in SyntaxItems)
            {
                if (item.IsCheckable)
                {
                    item.IsChecked = item.Header.Equals(syntaxName, StringComparison.Ordinal);
                }

                foreach (var child in item.Children)
                {
                    child.IsChecked = child.Header.Equals(syntaxName, StringComparison.Ordinal);
                }
            }
        }

        public DockViewModel DockVM => DockViewModel.Instance;

        public event EventHandler? ShowPreview;

        public ObservableCollection<MenuItemViewModel> SyntaxItems { get; } = new();

        public ObservableCollection<Marker> Markers { get; } = new();

        public List<int> MarkerLineNumbers = new();

        public Encoding Encoding { get; set; } = Encoding.UTF8;

        public IHighlightingDefinition? HighlightingDefinition =>
            ThemedHighlightingManager.Instance.GetDefinition(CurrentSyntax);

        [ObservableProperty]
        private bool isLargeOrBinary;

        [ObservableProperty]
        private bool isPdf;

        [ObservableProperty]
        private string currentSyntax = string.Empty;
        partial void OnCurrentSyntaxChanged(string value)
        {
            SelectCurrentSyntax(value);
        }

        [ObservableProperty]
        private string filePath = string.Empty;

        [ObservableProperty]
        private GrepSearchResult? grepResult;

        [ObservableProperty]
        private int lineNumber;

        [ObservableProperty]
        private bool highlightsOn = true;

        [ObservableProperty]
        private bool highlightDisabled;

        [ObservableProperty]
        private bool hasPageNumbers = false;

        [ObservableProperty]
        private string applicationFontFamily = SystemFonts.MessageFontFamily.Source;

        [ObservableProperty]
        private double mainFormFontSize;

        [ObservableProperty]
        private string resultsFontFamily = GrepSettings.DefaultMonospaceFontFamily;

        [ObservableProperty]
        private bool previewZoomWndVisible = true;

        [ObservableProperty]
        private bool wrapTextPreviewWndVisible = true;

        [ObservableProperty]
        private bool syntaxPreviewWndVisible = true;

        void PreviewViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.PropertyName))
            {
                UpdateState(e.PropertyName);
            }
        }

        private void UpdateState(string name)
        {
            if (name == nameof(GrepResult) && GrepResult != null)
            {
                MarkerLineNumbers = GrepResult.SearchResults.Where(sr => !sr.IsContext)
                    .Select(sr => sr.LineNumber).Distinct().ToList();

            }

            if (name == nameof(FilePath))
            {
                ClearPositionMarkers();
                if (!string.IsNullOrEmpty(FilePath) &&
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

                    HasPageNumbers = GrepResult?.SearchResults?.Any(sr => sr.PageNumber > -1) ?? false;

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
            PreviewZoomWndVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.PreviewZoomWndVisible);
            WrapTextPreviewWndVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.WrapTextPreviewWndVisible);
            SyntaxPreviewWndVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.SyntaxPreviewWndVisible);
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
