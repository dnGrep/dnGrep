using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Alphaleonis.Win32.Filesystem;
using dnGREP.Common;
using dnGREP.Localization.Properties;
using ICSharpCode.AvalonEdit.Highlighting;
using NLog;

namespace dnGREP.WPF
{
    public class PreviewViewModel : ViewModelBase, INotifyPropertyChanged
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public PreviewViewModel()
        {
            Highlighters = ThemedHighlightingManager.Instance.HighlightingNames.ToList();
            Highlighters.Sort();
            Highlighters.Insert(0, Resources.PreviewSyntax_None);
            CurrentSyntax = Resources.PreviewSyntax_None;

            HighlightsOn = GrepSettings.Instance.Get<bool>(GrepSettings.Key.HighlightMatches);

            ApplicationFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ApplicationFontFamily);
            MainFormFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.MainFormFontSize);

            PropertyChanged += PreviewViewModel_PropertyChanged;
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

        private bool isPdf;
        public bool IsPdf
        {
            get { return isPdf; }
            set
            {
                if (value == isPdf)
                    return;

                isPdf = value;

                base.OnPropertyChanged(() => IsPdf);
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

        private bool highlightsOn = true;
        public bool HighlightsOn
        {
            get { return highlightsOn; }
            set
            {
                if (value == highlightsOn)
                    return;

                highlightsOn = value;
                base.OnPropertyChanged(() => HighlightsOn);
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
                base.OnPropertyChanged(() => ApplicationFontFamily);
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
                base.OnPropertyChanged(() => MainFormFontSize);
            }
        }

        void PreviewViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateState(e.PropertyName);
        }

        private void UpdateState(string name)
        {
            if (name == nameof(FilePath))
            {
                if (!string.IsNullOrEmpty(filePath) &&
                    File.Exists(FilePath))
                {
                    // Set current definition
                    var fileInfo = new FileInfo(FilePath);
                    var definition = ThemedHighlightingManager.Instance.GetDefinitionByExtension(fileInfo.Extension);
                    if (definition != null)
                        CurrentSyntax = definition.Name;
                    else
                        CurrentSyntax = Resources.PreviewSyntax_None;

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
                    ShowPreview?.Invoke(this, new ShowEventArgs { ClearContent = true });
                }
                else
                {
                    // Tell View to show window and clear content
                    ShowPreview?.Invoke(this, new ShowEventArgs { ClearContent = true });
                }
            }

            if (name == nameof(LineNumber))
            {
                // Tell View to show window but not clear content
                ShowPreview?.Invoke(this, new ShowEventArgs { ClearContent = false });
            }

            if (name == nameof(CurrentSyntax))
            {
                // Tell View to show window and clear content
                ShowPreview?.Invoke(this, new ShowEventArgs { ClearContent = true });
            }
        }
    }

    public class ShowEventArgs : EventArgs
    {
        public bool ClearContent { get; set; }
    }
}
