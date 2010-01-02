using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using dnGREP.Common;
using System.Windows.Documents;
using System.Windows.Media;
using System.IO;
using System.Collections.Specialized;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace dnGREP.WPF
{
	public class ObservableGrepSearchResults : ObservableCollection<FormattedGrepResult>
	{
        private string folderPath = "";

		public string FolderPath
		{
			get { return folderPath; }
			set { folderPath = value; }
		}

		public ObservableGrepSearchResults()
		{
            this.CollectionChanged += new NotifyCollectionChangedEventHandler(ObservableGrepSearchResults_CollectionChanged);
        }

        //protected override void ClearItems()
        //{
        //    base.ClearItems();
        //    OnFunctionCalled("Clear");
        //}

        private Dictionary<string, BitmapSource> icons = new Dictionary<string, BitmapSource>(); 

        void ObservableGrepSearchResults_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (FormattedGrepResult newEntry in e.NewItems.Cast<FormattedGrepResult>())
                {
                    string extension = Path.GetExtension(newEntry.GrepResult.FileNameDisplayed);
                    if (extension.Length <= 1)
                        extension = ".na";
                    if (!icons.ContainsKey(extension))
                    {
                        System.Drawing.Bitmap bitmapIcon = IconHandler.IconFromExtensionShell(extension, IconSize.Small);
                        if (bitmapIcon == null)
                            bitmapIcon = dnGREP.Common.Properties.Resources.na_icon;
                        icons[extension] = GetBitmapSource(bitmapIcon);
                    }
                    newEntry.Icon = icons[extension];
                }
            }
        }

		public ObservableGrepSearchResults(List<GrepSearchResult> list) : this()
		{
			AddRange(list);
		}

		public List<GrepSearchResult> GetList()
		{
			List<GrepSearchResult> tempList = new List<GrepSearchResult>();
			foreach (var l in this) tempList.Add(l.GrepResult);
			return tempList;
		}

		public void AddRange(List<GrepSearchResult> list)
		{
			foreach (var l in list) this.Add(new FormattedGrepResult(l, folderPath));
		}

        [DllImport("gdi32.dll")]
        static extern bool DeleteObject(IntPtr hObject);
        public static BitmapSource GetBitmapSource(System.Drawing.Bitmap source)
        {
            IntPtr ip = source.GetHbitmap();
            try
            {
                BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(ip,
                   IntPtr.Zero, Int32Rect.Empty,
                   System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                return bs;
            }
            finally
            {
                DeleteObject(ip);
            }
        }

        //#region PropertyChanged Members
        //// Create the OnPropertyChanged method to raise the event
        //protected void OnFunctionCalled(string name)
        //{
        //    FunctionCallEventHandler handler = FunctionCalled;
        //    if (handler != null)
        //    {
        //        handler(this, new PropertyChangedEventArgs(name));
        //    }
        //}

        //public event FunctionCallEventHandler FunctionCalled;
        //public delegate void FunctionCallEventHandler(object sender, PropertyChangedEventArgs e);

        //#endregion
	}

    public class FormattedGrepResult : INotifyPropertyChanged
	{
		private GrepSearchResult grepResult = new GrepSearchResult();
		public GrepSearchResult GrepResult
		{
			get { return grepResult; }
		}

		private string style = "";
		public string Style
		{
			get { return style; }
			set { style = value; }
		}

		private string label = "";
		public string Label
		{
			get
			{
				return label;
			}
		}

        private bool isExpanded = false;
        public bool IsExpanded
        {
            get { return isExpanded; }
            set { isExpanded = value; OnPropertyChanged("IsExpanded"); }
        }

        private BitmapSource icon;

        public BitmapSource Icon
        {
            get { return icon; }
            set { icon = value; }
        }

		private List<FormattedGrepLine> formattedLines = new List<FormattedGrepLine>();
		public List<FormattedGrepLine> FormattedLines
		{
			get { return formattedLines; }
		}

		public FormattedGrepResult(GrepSearchResult result, string folderPath)
		{
			grepResult = result;

			// Populate icon list
			// TODO

			bool isFileReadOnly = Utils.IsReadOnly(grepResult);
			string displayedName = Path.GetFileName(grepResult.FileNameDisplayed);
			if (Properties.Settings.Default.ShowFilePathInResults &&
				grepResult.FileNameDisplayed.Contains(Utils.GetBaseFolder(folderPath) + "\\"))
			{
				displayedName = grepResult.FileNameDisplayed.Substring(Utils.GetBaseFolder(folderPath).Length + 1);
			}
			int lineCount = Utils.MatchCount(grepResult);
			if (lineCount > 0)
				displayedName = string.Format("{0} ({1})", displayedName, lineCount);
			if (isFileReadOnly)
				displayedName = displayedName + " [read-only]";

			label = displayedName;

			if (isFileReadOnly)
			{
				style = "ReadOnly";
			}

			if (result.SearchResults != null)
			{
				for (int i = 0; i < result.SearchResults.Count; i++)
				{
					GrepSearchResult.GrepLine line = result.SearchResults[i];
					formattedLines.Add(new FormattedGrepLine(line, this));
				}
			}
		}

        #region PropertyChanged Members
        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
	}

    public class FormattedGrepLine : INotifyPropertyChanged
	{
		private GrepSearchResult.GrepLine grepLine;
		public GrepSearchResult.GrepLine GrepLine
		{
			get { return grepLine; }
		}

        private string formattedLineNumber;
        public string FormattedLineNumber
        {
            get { return formattedLineNumber; }
        }

		private InlineCollection formattedText;
		public InlineCollection FormattedText
		{
			get { return formattedText; }
		}

		private string style = "";
		public string Style
		{
			get { return style; }
			set { style = value; }
		}

        private FormattedGrepResult parent;
        public FormattedGrepResult Parent
        {
            get { return parent; }
            set { parent = value; }
        }

		public FormattedGrepLine(GrepSearchResult.GrepLine line, FormattedGrepResult parent)
		{
            Parent = parent;
			grepLine = line;

			string lineSummary = line.LineText.Replace("\n", "").Replace("\t", "").Replace("\r", "").Trim();
			if (lineSummary.Length == 0)
				lineSummary = " ";
			else if (lineSummary.Length > 100)
				lineSummary = lineSummary.Substring(0, 100) + "...";
            
            formattedLineNumber = (line.LineNumber == -1 ? "" : line.LineNumber.ToString());

			string fullText = lineSummary;
			if (line.IsContext)
			{
				style = "Context";
			}
			
			Paragraph paragraph = new Paragraph();
			//Run highlightedRun = new Run("hello ");
			//highlightedRun.Background = Brushes.Yellow;
			//paragraph.Inlines.Add(highlightedRun);
			Run mainRun = new Run(fullText);
			paragraph.Inlines.Add(mainRun);
			formattedText = paragraph.Inlines;
        }

        #region PropertyChanged Members
        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
