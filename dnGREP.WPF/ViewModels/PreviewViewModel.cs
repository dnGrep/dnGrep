using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Alphaleonis.Win32.Filesystem;
using dnGREP.Common;
using ICSharpCode.AvalonEdit.Highlighting;

namespace dnGREP.WPF
{
    public class PreviewViewModel : ViewModelBase, INotifyPropertyChanged
    {
        public PreviewViewModel()
        {
            Highlighters = ThemedHighlightingManager.Instance.HighlightingNames.ToList();
            Highlighters.Sort();
            Highlighters.Insert(0, "None");
            CurrentSyntax = "None";

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
                return ThemedHighlightingManager.Instance.GetDefinition(CurrentSyntax);
            }
        }

        void PreviewViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateState(e.PropertyName);
        }

        private void UpdateState(string name)
        {
            if (name == "FilePath")
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
    }

    public class ShowEventArgs : EventArgs
    {
        public bool ClearContent { get; set; }
    }
}
