using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Media;
using System.Xml;
using Alphaleonis.Win32.Filesystem;
using dnGREP.Common;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace dnGREP.WPF
{
    public class PreviewViewModel : ViewModelBase, INotifyPropertyChanged
    {
        public PreviewViewModel()
        {
            HighlightDefinitions = new Dictionary<string, IHighlightingDefinition>();
            Highlighters = new List<string>();
            foreach (var hl in HighlightingManager.Instance.HighlightingDefinitions)
            {
                ColorInverter.TranslateThemeColors(hl);
                HighlightDefinitions[hl.Name] = hl;
                Highlighters.Add(hl.Name);
            }
            Highlighters.Add("SQL");
            HighlightDefinitions["SQL"] = LoadHighlightingDefinition("sqlmode.xshd");
            ColorInverter.TranslateThemeColors(HighlightDefinitions["SQL"]);
            Highlighters.Sort();
            Highlighters.Insert(0, "None");
            CurrentSyntax = "None";

            PropertyChanged += PreviewViewModel_PropertyChanged;
        }

        private void TranslateThemeColors(IHighlightingDefinition hl)
        {
            foreach (var item in hl.NamedHighlightingColors)
            {
                if (item != null && !item.IsFrozen)
                {
                    if (item.Foreground != null)
                    {
                        string hex = item.Foreground.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.Foreground = new SimpleHighlightingBrush(Invert(c));
                    }
                    else if (item.Background != null)
                    {
                        string hex = item.Background.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.Background = new SimpleHighlightingBrush(Invert(c));
                    }
                }
            }
            foreach (var item in hl.MainRuleSet.Rules)
            {
                if (item.Color != null && !item.Color.IsFrozen && string.IsNullOrEmpty(item.Color.Name))
                {
                    if (item.Color.Foreground != null)
                    {
                        string hex = item.Color.Foreground.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.Color.Foreground = new SimpleHighlightingBrush(Invert(c));
                    }
                    else if (item.Color.Background != null)
                    {
                        string hex = item.Color.Background.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.Color.Background = new SimpleHighlightingBrush(Invert(c));
                    }
                }
            }
            foreach (var item in hl.MainRuleSet.Spans)
            {
                if (item.SpanColor != null && !item.SpanColor.IsFrozen && string.IsNullOrEmpty(item.SpanColor.Name))
                {
                    if (item.SpanColor.Foreground != null)
                    {
                        string hex = item.SpanColor.Foreground.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.SpanColor.Foreground = new SimpleHighlightingBrush(Invert(c));
                    }
                    else if (item.SpanColor.Background != null)
                    {
                        string hex = item.SpanColor.Background.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.SpanColor.Background = new SimpleHighlightingBrush(Invert(c));
                    }
                }
                if (item.StartColor != null && !item.StartColor.IsFrozen && string.IsNullOrEmpty(item.StartColor.Name))
                {
                    if (item.StartColor.Foreground != null)
                    {
                        string hex = item.StartColor.Foreground.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.StartColor.Foreground = new SimpleHighlightingBrush(Invert(c));
                    }
                    else if (item.StartColor.Background != null)
                    {
                        string hex = item.StartColor.Background.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.StartColor.Background = new SimpleHighlightingBrush(Invert(c));
                    }
                }
                if (item.EndColor != null && !item.EndColor.IsFrozen && string.IsNullOrEmpty(item.EndColor.Name))
                {
                    if (item.EndColor.Foreground != null)
                    {
                        string hex = item.EndColor.Foreground.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.EndColor.Foreground = new SimpleHighlightingBrush(Invert(c));
                    }
                    else if (item.EndColor.Background != null)
                    {
                        string hex = item.EndColor.Background.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.EndColor.Background = new SimpleHighlightingBrush(Invert(c));
                    }
                }
            }
        }

        private Color Invert(Color c)
        {
            int shift = c.A - Math.Min(c.R, Math.Min(c.G, c.B)) - Math.Max(c.R, Math.Max(c.G, c.B));
            Color result = new Color
            {
                A = c.A,
                R = Wrap(shift + c.R),
                G = Wrap(shift + c.G),
                B = Wrap(shift + c.B),
            };
            return result;
        }

        private byte Wrap(int v)
        {
            if (v > byte.MaxValue)
                v -= byte.MaxValue;
            if (v < 0)
                v += byte.MaxValue;
            return (byte)v;
        }

        public event EventHandler<ShowEventArgs> ShowPreview;

        private bool isLargeOrBinary;
        public bool IsLargeOrBinary
        {
            get { return isLargeOrBinary; }
            set
            {
                if (value == isLargeOrBinary)
                    return;

                isLargeOrBinary = value;

                base.OnPropertyChanged(() => IsLargeOrBinary);
            }
        }

        private string currentSyntax;
        public string CurrentSyntax
        {
            get { return currentSyntax; }
            set
            {
                if (value == currentSyntax)
                    return;

                currentSyntax = value;

                base.OnPropertyChanged(() => CurrentSyntax);
            }
        }

        public List<string> Highlighters { get; set; }

        public Encoding Encoding { get; set; }

        private string displayFileName;
        public string DisplayFileName
        {
            get { return displayFileName; }
            set
            {
                if (value == displayFileName)
                    return;

                displayFileName = value;

                base.OnPropertyChanged(() => DisplayFileName);
            }
        }

        private string filePath;
        public string FilePath
        {
            get { return filePath; }
            set
            {
                if (value == filePath)
                    return;

                filePath = value;

                base.OnPropertyChanged(() => FilePath);
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

                base.OnPropertyChanged(() => GrepResult);
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

                base.OnPropertyChanged(() => LineNumber);
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

                base.OnPropertyChanged(() => HighlightDisabled);
            }
        }

        public IHighlightingDefinition HighlightingDefinition
        {
            get
            {
                if (HighlightDefinitions.ContainsKey(CurrentSyntax))
                    return HighlightDefinitions[CurrentSyntax];
                else
                    return HighlightingManager.Instance.GetDefinitionByExtension("txt");
            }
        }

        void PreviewViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateState(e.PropertyName);
        }

        private Dictionary<string, IHighlightingDefinition> HighlightDefinitions { get; set; }

        private void UpdateState(string name)
        {
            if (name == "FilePath")
            {
                if (!string.IsNullOrEmpty(filePath) &&
                    File.Exists(FilePath))
                {
                    // Set current definition
                    var fileInfo = new FileInfo(FilePath);
                    var definition = HighlightingManager.Instance.GetDefinitionByExtension(fileInfo.Extension);
                    if (definition != null)
                        CurrentSyntax = definition.Name;
                    else
                        CurrentSyntax = "None";

                    // Do not preview files over 4MB or binary
                    IsLargeOrBinary = fileInfo.Length > 4096000 || Utils.IsBinary(FilePath);

                    // Disable highlighting for large number of matches
                    HighlightDisabled = GrepResult?.Matches?.Count > 5000;

                    // Tell View to show window
                    ShowPreview?.Invoke(this, new ShowEventArgs { ClearContent = true });
                }
                else
                {
                    // Tell View to show window and clear content
                    ShowPreview?.Invoke(this, new ShowEventArgs { ClearContent = true });
                }
            }

            if (name == "LineNumber")
            {
                // Tell View to show window but not clear content
                ShowPreview?.Invoke(this, new ShowEventArgs { ClearContent = false });
            }

            if (name == "CurrentSyntax")
            {
                // Tell View to show window and clear content
                ShowPreview?.Invoke(this, new ShowEventArgs { ClearContent = true });
            }
        }

        private IHighlightingDefinition LoadHighlightingDefinition(string resourceName)
        {
            var type = typeof(PreviewControl);
            var fullName = type.Namespace + "." + resourceName;
            using (var stream = type.Assembly.GetManifestResourceStream(fullName))
            using (var reader = new XmlTextReader(stream))
                return HighlightingLoader.Load(reader, HighlightingManager.Instance);
        }
    }

    public class ShowEventArgs : EventArgs
    {
        public bool ClearContent { get; set; }
    }
}
