using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using dnGREP.Common;
using System.Collections.Specialized;
using System.Windows.Threading;
using System.Threading;
using System.ComponentModel;
using System.Windows.Media;

namespace dnGREP.WPF
{
	public class TestPatternState : INotifyPropertyChanged
	{
		public TestPatternState()
		{
			LoadAppSettings();
			UpdateState("Initial");
			CanSearch = true;
			CanReplace = true;
			CanCancel = false;
			CanSearchInResults = false;
			SearchButtonMode = "Button";
		}

		public GrepSettings settings
		{
			get { return GrepSettings.Instance; }
		}

		public void LoadAppSettings()
		{
			SearchFor = settings.Get<string>(GrepSettings.Key.SearchFor);
			ReplaceWith = settings.Get<string>(GrepSettings.Key.ReplaceWith);
			TypeOfSearch = settings.Get<SearchType>(GrepSettings.Key.TypeOfSearch);
			CaseSensitive = settings.Get<bool>(GrepSettings.Key.CaseSensitive);
			Multiline = settings.Get<bool>(GrepSettings.Key.Multiline);
			Singleline = settings.Get<bool>(GrepSettings.Key.Singleline);
			WholeWord = settings.Get<bool>(GrepSettings.Key.WholeWord);
            TextFormatting = settings.Get<TextFormattingMode>(GrepSettings.Key.TextFormatting);
		}

		private ObservableGrepSearchResults searchResults = new ObservableGrepSearchResults();
		public ObservableGrepSearchResults SearchResults
		{
			get
			{
				return searchResults;
			}
		}

		private string _SearchFor = "";
		/// <summary>
		/// SearchFor property
		/// </summary>
		public string SearchFor
		{
			get { return _SearchFor; }
			set
			{
				_SearchFor = value;
				settings.Set<string>(GrepSettings.Key.SearchFor, value);
				UpdateState("SearchFor");
			}
		}

		private string _ReplaceWith = "";
		/// <summary>
		/// ReplaceWith property
		/// </summary>
		public string ReplaceWith
		{
			get { return _ReplaceWith; }
			set
			{
				_ReplaceWith = value;
				settings.Set<string>(GrepSettings.Key.ReplaceWith, value);
				UpdateState("ReplaceWith");
			}
		}

		private SearchType _TypeOfSearch = SearchType.PlainText;
		/// <summary>
		/// TypeOfSearch property
		/// </summary>
		public SearchType TypeOfSearch
		{
			get { return _TypeOfSearch; }
			set
			{
				_TypeOfSearch = value;
				settings.Set<SearchType>(GrepSettings.Key.TypeOfSearch, value);
				UpdateState("TypeOfSearch");
			}
		}

		private bool _CaseSensitive = true;
		/// <summary>
		/// CaseSensitive property
		/// </summary>
		public bool CaseSensitive
		{
			get { return _CaseSensitive; }
			set
			{
				_CaseSensitive = value;
				settings.Set<bool>(GrepSettings.Key.CaseSensitive, value);
				UpdateState("CaseSensitive");
			}
		}

		private bool _IsCaseSensitiveEnabled = true;
		/// <summary>
		/// IsCaseSensitiveEnabled property
		/// </summary>
		public bool IsCaseSensitiveEnabled
		{
			get { return _IsCaseSensitiveEnabled; }
			set
			{
				_IsCaseSensitiveEnabled = value;
				UpdateState("IsCaseSensitiveEnabled");
			}
		}

		private bool _Multiline = false;
		/// <summary>
		/// Multiline property
		/// </summary>
		public bool Multiline
		{
			get { return _Multiline; }
			set
			{
				_Multiline = value;
				settings.Set<bool>(GrepSettings.Key.Multiline, value);
				UpdateState("Multiline");
			}
		}

		private bool _IsMultilineEnabled = true;
		/// <summary>
		/// IsMultilineEnabled property
		/// </summary>
		public bool IsMultilineEnabled
		{
			get { return _IsMultilineEnabled; }
			set
			{
				_IsMultilineEnabled = value;
				UpdateState("IsMultilineEnabled");
			}
		}

		private bool _Singleline = false;
		/// <summary>
		/// Singleline property
		/// </summary>
		public bool Singleline
		{
			get { return _Singleline; }
			set
			{
				_Singleline = value;
				settings.Set<bool>(GrepSettings.Key.Singleline, value);
				UpdateState("Singleline");
			}
		}

		private bool _IsSinglelineEnables = true;
		/// <summary>
		/// IsSinglelineEnables property
		/// </summary>
		public bool IsSinglelineEnabled
		{
			get { return _IsSinglelineEnables; }
			set
			{
				_IsSinglelineEnables = value;
				UpdateState("IsSinglelineEnabled");
			}
		}

        private TextFormattingMode _TextFormatting = TextFormattingMode.Display;
        /// <summary>
        /// IsOptionsExpanded property
        /// </summary>
        public TextFormattingMode TextFormatting
        {
            get { return _TextFormatting; }
            set
            {
                _TextFormatting = value;
                settings.Set<TextFormattingMode>(GrepSettings.Key.TextFormatting, value);
                UpdateState("TextFormatting");
            }
        }

		private bool _WholeWord = false;
		/// <summary>
		/// WholeWord property
		/// </summary>
		public bool WholeWord
		{
			get { return _WholeWord; }
			set
			{
				_WholeWord = value;
				settings.Set<bool>(GrepSettings.Key.WholeWord, value);
				UpdateState("WholeWord");
			}
		}

		private bool _IsWholeWordEnabled = true;
		/// <summary>
		/// IsWholeWordEnabled property
		/// </summary>
		public bool IsWholeWordEnabled
		{
			get { return _IsWholeWordEnabled; }
			set { _IsWholeWordEnabled = value; UpdateState("IsWholeWordEnabled"); }
		}


		#region Derived properties
		
		private bool filesFound = false;
		public bool FilesFound
		{
			get { return filesFound; }
			set { filesFound = value; UpdateState("FilesFound"); }
		}

		private bool _CanSearch = true;
		/// <summary>
		/// CanSearch property
		/// </summary>
		public bool CanSearch
		{
			get { return _CanSearch; }
			set { _CanSearch = value; UpdateState("CanSearch"); }
		}

		private bool _CanSearchInResults = true;
		/// <summary>
		/// CanSearchInResults property
		/// </summary>
		public bool CanSearchInResults
		{
			get { return _CanSearchInResults; }
			set { _CanSearchInResults = value; UpdateState("CanSearchInResults"); }
		}

		private string _SearchButtonMode = "";
		/// <summary>
		/// SearchButtonMode property
		/// </summary>
		public string SearchButtonMode
		{
			get { return _SearchButtonMode; }
			set { _SearchButtonMode = value; UpdateState("SearchButtonMode"); }
		}

		private bool _CanReplace = false;
		/// <summary>
		/// CanReplace property
		/// </summary>
		public bool CanReplace
		{
			get { return _CanReplace; }
			set { _CanReplace = value; UpdateState("CanReplace"); }
		}

		private bool _CanCancel = false;
		/// <summary>
		/// CanCancel property
		/// </summary>
		public bool CanCancel
		{
			get { return _CanCancel; }
			set { _CanCancel = value; UpdateState("CanCancel"); }
		}

		private GrepOperation _CurrentGrepOperation = GrepOperation.None;
		/// <summary>
		/// CurrentGrepOperation property
		/// </summary>
		public GrepOperation CurrentGrepOperation
		{
			get { return _CurrentGrepOperation; }
			set { _CurrentGrepOperation = value; UpdateState("CurrentGrepOperation"); }
		}

		private string _OptionsSummary = "";
		/// <summary>
		/// OptionsSummary property
		/// </summary>
		public string OptionsSummary
		{
			get { return _OptionsSummary; }
			set { _OptionsSummary = value; UpdateState("OptionsSummary"); }
		}

		private string _TextBoxStyle = "";
		/// <summary>
		/// TextBoxStyle property
		/// </summary>
		public string TextBoxStyle
		{
			get { return _TextBoxStyle; }
			set { _TextBoxStyle = value; UpdateState("TextBoxStyle"); }
		}

		#endregion

		public void UpdateState(string name)
		{
			OnPropertyChanged(name);

			switch (name)
			{
				case "Initial":
				case "Multiline":
				case "Singleline":
				case "WholeWord":
				case "CaseSensitive":
					List<string> tempList = new List<string>();
					if (CaseSensitive)
						tempList.Add("Case sensitive");
					if (Multiline)
						tempList.Add("Multiline");
					if (WholeWord)
						tempList.Add("Whole word");
					if (Singleline)
						tempList.Add("Match dot as new line");
					OptionsSummary = "[";
					if (tempList.Count == 0)
					{
						OptionsSummary += "None";
					}
					else
					{
						for (int i = 0; i < tempList.Count; i++)
						{
							OptionsSummary += tempList[i];
							if (i < tempList.Count - 1)
								OptionsSummary += ", ";
						}
					}
					OptionsSummary += "]";

					if (Multiline)
						TextBoxStyle = "{StaticResource ExpandedTextbox}";
					else
						TextBoxStyle = "";

					break;
			}

			//Files found
			if (name == "FileOrFolderPath" || name == "SearchFor" || name == "FilePattern")
			{
				FilesFound = false;
			}

			//Search type specific options
			//Search type specific options
			if (name == "TypeOfSearch")
			{
				if (TypeOfSearch == SearchType.XPath)
				{
					IsCaseSensitiveEnabled = false;
					IsMultilineEnabled = false;
					IsSinglelineEnabled = false;
					IsWholeWordEnabled = false;
					CaseSensitive = false;
					Multiline = false;
					Singleline = false;
					WholeWord = false;
				}
				else if (TypeOfSearch == SearchType.PlainText)
				{
					IsCaseSensitiveEnabled = true;
					IsMultilineEnabled = true;
					IsSinglelineEnabled = false;
					IsWholeWordEnabled = true;
					Singleline = false;
				}
				else if (TypeOfSearch == SearchType.Soundex)
				{
					IsMultilineEnabled = true;
					IsCaseSensitiveEnabled = false;
					IsSinglelineEnabled = false;
					IsWholeWordEnabled = true;
					CaseSensitive = false;
					Singleline = false;
				}
				else if (TypeOfSearch == SearchType.Regex)
				{
					IsCaseSensitiveEnabled = true;
					IsMultilineEnabled = true;
					IsSinglelineEnabled = true;
					IsWholeWordEnabled = true;
				}
			}
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		// Create the OnPropertyChanged method to raise the event
		protected void OnPropertyChanged(string name)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(name));
			}
		}

		#endregion
	}
}
