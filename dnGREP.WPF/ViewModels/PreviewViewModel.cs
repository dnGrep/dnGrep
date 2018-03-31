using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Xml;
using dnGREP.Common;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace dnGREP.WPF
{
    public class PreviewViewModel : ViewModelBase, INotifyPropertyChanged
    {
        public PreviewViewModel()
        {
            highlightDefinitions = new Dictionary<string, IHighlightingDefinition>();
            Highlighters = new List<string>();
            foreach (var hl in HighlightingManager.Instance.HighlightingDefinitions)
            {
                highlightDefinitions[hl.Name] = hl;
                Highlighters.Add(hl.Name);
            }
            Highlighters.Add("SQL");
            highlightDefinitions["SQL"] = loadHighlightingDefinition("sqlmode.xshd");
            Highlighters.Sort();
            Highlighters.Insert(0, "None");
            CurrentSyntax = "None";

            this.PropertyChanged += PreviewViewModel_PropertyChanged;
        }

        #region Properties and Events
        public event EventHandler<ShowEventArgs> ShowPreview;

        private bool isVisible;
        public bool IsVisible
        {
            get { return isVisible; }
            set
            {
                if (value == isVisible)
                    return;

                isVisible = value;

                base.OnPropertyChanged(() => IsVisible);
            }
        }

        private Visibility isLargeOrBinary;
        public Visibility IsLargeOrBinary
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

        public IHighlightingDefinition HighlightingDefinition
        {
            get
            {
                if (highlightDefinitions.ContainsKey(CurrentSyntax))
                    return highlightDefinitions[CurrentSyntax];
                else
                    return HighlightingManager.Instance.GetDefinitionByExtension("txt");
            }
        }
        #endregion

        void PreviewViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateState(e.PropertyName);
        }

        private Dictionary<string, IHighlightingDefinition> highlightDefinitions { get; set; }

        #region Public Methods
        public virtual void UpdateState(string name)
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
                    if (fileInfo.Length > 4096000 ||
                        Utils.IsBinary(FilePath))
                    {
                        IsLargeOrBinary = System.Windows.Visibility.Visible;
                    }
                    else
                    {
                        IsLargeOrBinary = System.Windows.Visibility.Collapsed;
                    }

                    // Tell View to show window
                    ShowPreview(this, new ShowEventArgs { ClearContent = true });
                }
                else
                {
                    // Tell View to show window and clear content
                    ShowPreview(this, new ShowEventArgs { ClearContent = true });
                }
            }

            if (name == "LineNumber")
            {
                // Tell View to show window but not clear content
                ShowPreview(this, new ShowEventArgs { ClearContent = false });
            }

            if (name == "CurrentSyntax")
            {
                // Tell View to show window and clear content
                ShowPreview(this, new ShowEventArgs { ClearContent = true });
            }
        }
        #endregion

        #region Private Methods
        private IHighlightingDefinition loadHighlightingDefinition(
            string resourceName)
        {
            var type = typeof(PreviewView);
            var fullName = type.Namespace + "." + resourceName;
            using (var stream = type.Assembly.GetManifestResourceStream(fullName))
            using (var reader = new XmlTextReader(stream))
                return HighlightingLoader.Load(reader, HighlightingManager.Instance);
        }
        #endregion
    }

    public class ShowEventArgs : EventArgs
    {
        public bool ClearContent { get; set; }
    }
}
