using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using dnGREP.Common;
using System.Collections.Specialized;
using System.Windows.Threading;
using System.Threading;

namespace dnGREP.WPF
{
	public class MainFormState : DependencyObject
	{
		public MainFormState()
		{
			FileOrFolderPath = Properties.Settings.Default.SearchFolder;
			SearchFor = Properties.Settings.Default.SearchFor;
			ReplaceWith = Properties.Settings.Default.ReplaceWith;
			IncludeHidden = Properties.Settings.Default.IncludeHidden;
			IncludeSubfolder = Properties.Settings.Default.IncludeSubfolder;
			TypeOfSearch = Properties.Settings.Default.TypeOfSearch;
			TypeOfFileSearch = Properties.Settings.Default.TypeOfFileSearch;
			UseFileSizeFilter = Properties.Settings.Default.UseFileSizeFilter;
			CaseSensitive = Properties.Settings.Default.CaseSensitive;
			Multiline = Properties.Settings.Default.Multiline;
			Singleline = Properties.Settings.Default.Singleline;
			SizeFrom = Properties.Settings.Default.SizeFrom;
			SizeTo = Properties.Settings.Default.SizeTo;
			searchResults.CollectionChanged += new NotifyCollectionChangedEventHandler(searchResults_CollectionChanged);
			UpdateState("Multiline");
		}

		void searchResults_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (searchResults.Count > 0)
				FilesFound = true;
		}

		private ObservableGrepSearchResults searchResults = new ObservableGrepSearchResults();
		public ObservableGrepSearchResults SearchResults
		{
			get {				
				return searchResults; 
			}
		}

		/// <summary>
		/// FileOrFolderPath property
		/// </summary>
		public string FileOrFolderPath
		{
			get
			{
				try
				{
					if (this.Dispatcher.CheckAccess())
					{
						return (string)GetValue(FileOrFolderPathProperty);
					}
					else
					{
						return (string)this.Dispatcher.Invoke(
						   System.Windows.Threading.DispatcherPriority.Background,
						   (DispatcherOperationCallback)delegate { return GetValue(FileOrFolderPathProperty); },
						   FileOrFolderPathProperty);
					}
				}
				catch
				{
					return (string)FileOrFolderPathProperty.DefaultMetadata.DefaultValue;
				}
			}
			set
			{
				if (this.Dispatcher.CheckAccess())
				{
					SetValue(FileOrFolderPathProperty, value);
				}
				else
				{
					this.Dispatcher.BeginInvoke(DispatcherPriority.Background,
							(SendOrPostCallback)delegate { SetValue(FileOrFolderPathProperty, value); },
							value);
				}
			}
		}

		public static DependencyProperty FileOrFolderPathProperty =
			DependencyProperty.Register("FileOrFolderPath", typeof(string), typeof(MainFormState),
			new FrameworkPropertyMetadata("", new PropertyChangedCallback(OnFileOrFolderPathChanged)));

		private static void OnFileOrFolderPathChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			Properties.Settings.Default.SearchFolder = (string)args.NewValue;
			((MainFormState)obj).UpdateState(args.Property.Name);
		}

		/// <summary>
		/// SearchFor property
		/// </summary>
		public string SearchFor
		{
			get
			{
				try
				{
					if (this.Dispatcher.CheckAccess())
					{
						return (string)GetValue(SearchForProperty);
					}
					else
					{
						return (string)this.Dispatcher.Invoke(
						   System.Windows.Threading.DispatcherPriority.Background,
						   (DispatcherOperationCallback)delegate { return GetValue(SearchForProperty); },
						   SearchForProperty);
					}
				}
				catch
				{
					return (string)SearchForProperty.DefaultMetadata.DefaultValue;
				}
			}
			set
			{
				if (this.Dispatcher.CheckAccess())
				{
					SetValue(SearchForProperty, value);
				}
				else
				{
					this.Dispatcher.BeginInvoke(DispatcherPriority.Background,
							(SendOrPostCallback)delegate { SetValue(SearchForProperty, value); },
							value);
				}
			}
		}

		public static DependencyProperty SearchForProperty =
			DependencyProperty.Register("SearchFor", typeof(string), typeof(MainFormState),
			new FrameworkPropertyMetadata("", new PropertyChangedCallback(OnSearchForChanged)));

		private static void OnSearchForChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			Properties.Settings.Default.SearchFor = (string)args.NewValue;
			((MainFormState)obj).UpdateState(args.Property.Name);
		}

		/// <summary>
		/// ReplaceWith property
		/// </summary>
		public string ReplaceWith
		{
			get
			{
				try
				{
					if (this.Dispatcher.CheckAccess())
					{
						return (string)GetValue(ReplaceWithProperty);
					}
					else
					{
						return (string)this.Dispatcher.Invoke(
						   System.Windows.Threading.DispatcherPriority.Background,
						   (DispatcherOperationCallback)delegate { return GetValue(ReplaceWithProperty); },
						   ReplaceWithProperty);
					}
				}
				catch
				{
					return (string)ReplaceWithProperty.DefaultMetadata.DefaultValue;
				}
			}
			set
			{
				if (this.Dispatcher.CheckAccess())
				{
					SetValue(ReplaceWithProperty, value);
				}
				else
				{
					this.Dispatcher.BeginInvoke(DispatcherPriority.Background,
							(SendOrPostCallback)delegate { SetValue(ReplaceWithProperty, value); },
							value);
				}
			}
		}

		public static DependencyProperty ReplaceWithProperty =
			DependencyProperty.Register("ReplaceWith", typeof(string), typeof(MainFormState),
			new FrameworkPropertyMetadata("", new PropertyChangedCallback(OnReplaceWithChanged)));

		private static void OnReplaceWithChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			Properties.Settings.Default.ReplaceWith = (string)args.NewValue;
			((MainFormState)obj).UpdateState(args.Property.Name);
		}

		/// <summary>
		/// FilePattern property
		/// </summary>
		public string FilePattern
		{
			get
			{
				try
				{
					if (this.Dispatcher.CheckAccess())
					{
						return (string)GetValue(FilePatternProperty);
					}
					else
					{
						return (string)this.Dispatcher.Invoke(
						   System.Windows.Threading.DispatcherPriority.Background,
						   (DispatcherOperationCallback)delegate { return GetValue(FilePatternProperty); },
						   FilePatternProperty);
					}
				}
				catch
				{
					return (string)FilePatternProperty.DefaultMetadata.DefaultValue;
				}
			}
			set
			{
				if (this.Dispatcher.CheckAccess())
				{
					SetValue(FilePatternProperty, value);
				}
				else
				{
					this.Dispatcher.BeginInvoke(DispatcherPriority.Background,
							(SendOrPostCallback)delegate { SetValue(FilePatternProperty, value); },
							value);
				}
			}
		}

		public static DependencyProperty FilePatternProperty =
			DependencyProperty.Register("FilePattern", typeof(string), typeof(MainFormState),
			new FrameworkPropertyMetadata("", new PropertyChangedCallback(OnFilePatternChanged)));

		private static void OnFilePatternChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			Properties.Settings.Default.FilePattern = (string)args.NewValue;
			((MainFormState)obj).UpdateState(args.Property.Name);
		}

		/// <summary>
		/// IncludeSubfolder property
		/// </summary>
		public bool IncludeSubfolder
		{
			get
			{
				try
				{
					if (this.Dispatcher.CheckAccess())
					{
						return (bool)GetValue(IncludeSubfolderProperty);
					}
					else
					{
						return (bool)this.Dispatcher.Invoke(
						   System.Windows.Threading.DispatcherPriority.Background,
						   (DispatcherOperationCallback)delegate { return GetValue(IncludeSubfolderProperty); },
						   IncludeSubfolderProperty);
					}
				}
				catch
				{
					return (bool)IncludeSubfolderProperty.DefaultMetadata.DefaultValue;
				}
			}
			set
			{
				if (this.Dispatcher.CheckAccess())
				{
					SetValue(IncludeSubfolderProperty, value);
				}
				else
				{
					this.Dispatcher.BeginInvoke(DispatcherPriority.Background,
							(SendOrPostCallback)delegate { SetValue(IncludeSubfolderProperty, value); },
							value);
				}
			}
		}

		public static DependencyProperty IncludeSubfolderProperty =
			DependencyProperty.Register("IncludeSubfolder", typeof(bool), typeof(MainFormState),
			new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnIncludeSubfolderChanged)));

		private static void OnIncludeSubfolderChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			Properties.Settings.Default.IncludeSubfolder = (bool)args.NewValue;
			((MainFormState)obj).UpdateState(args.Property.Name);
		}

		/// <summary>
		/// IncludeHidden property
		/// </summary>
		public bool IncludeHidden
		{
			get
			{
				try
				{
					if (this.Dispatcher.CheckAccess())
					{
						return (bool)GetValue(IncludeHiddenProperty);
					}
					else
					{
						return (bool)this.Dispatcher.Invoke(
						   System.Windows.Threading.DispatcherPriority.Background,
						   (DispatcherOperationCallback)delegate { return GetValue(IncludeHiddenProperty); },
						   IncludeHiddenProperty);
					}
				}
				catch
				{
					return (bool)IncludeHiddenProperty.DefaultMetadata.DefaultValue;
				}
			}
			set
			{
				if (this.Dispatcher.CheckAccess())
				{
					SetValue(IncludeHiddenProperty, value);
				}
				else
				{
					this.Dispatcher.BeginInvoke(DispatcherPriority.Background,
							(SendOrPostCallback)delegate { SetValue(IncludeHiddenProperty, value); },
							value);
				}
			}
		}

		public static DependencyProperty IncludeHiddenProperty =
			DependencyProperty.Register("IncludeHidden", typeof(bool), typeof(MainFormState),
			new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnIncludeHiddenChanged)));

		private static void OnIncludeHiddenChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			Properties.Settings.Default.IncludeHidden = (bool)args.NewValue;
			((MainFormState)obj).UpdateState(args.Property.Name);
		}

		/// <summary>
		/// TypeOfSearch property
		/// </summary>
		public SearchType TypeOfSearch
		{
			get
			{
				try
				{
					if (this.Dispatcher.CheckAccess())
					{
						return (SearchType)GetValue(TypeOfSearchProperty);
					}
					else
					{
						return (SearchType)this.Dispatcher.Invoke(
						   System.Windows.Threading.DispatcherPriority.Background,
						   (DispatcherOperationCallback)delegate { return GetValue(TypeOfSearchProperty); },
						   TypeOfSearchProperty);
					}
				}
				catch
				{
					return (SearchType)TypeOfSearchProperty.DefaultMetadata.DefaultValue;
				}
			}
			set
			{
				if (this.Dispatcher.CheckAccess())
				{
					SetValue(TypeOfSearchProperty, value);
				}
				else
				{
					this.Dispatcher.BeginInvoke(DispatcherPriority.Background,
							(SendOrPostCallback)delegate { SetValue(TypeOfSearchProperty, value); },
							value);
				}
			}
		}

		public static DependencyProperty TypeOfSearchProperty =
			DependencyProperty.Register("TypeOfSearch", typeof(SearchType), typeof(MainFormState),
			new FrameworkPropertyMetadata(SearchType.Regex, new PropertyChangedCallback(OnTypeOfSearchChanged)));

		private static void OnTypeOfSearchChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			Properties.Settings.Default.TypeOfSearch = (SearchType)args.NewValue;
			((MainFormState)obj).UpdateState(args.Property.Name);
		}

		
		/// <summary>
		/// TypeOfFileSearch property
		/// </summary>
		public FileSearchType TypeOfFileSearch
		{
			get
			{
				try
				{
					if (this.Dispatcher.CheckAccess())
					{
						return (FileSearchType)GetValue(TypeOfFileSearchProperty);
					}
					else
					{
						return (FileSearchType)this.Dispatcher.Invoke(
						   System.Windows.Threading.DispatcherPriority.Background,
						   (DispatcherOperationCallback)delegate { return GetValue(TypeOfFileSearchProperty); },
						   TypeOfFileSearchProperty);
					}
				}
				catch
				{
					return (FileSearchType)TypeOfFileSearchProperty.DefaultMetadata.DefaultValue;
				}
			}
			set
			{
				if (this.Dispatcher.CheckAccess())
				{
					SetValue(TypeOfFileSearchProperty, value);
				}
				else
				{
					this.Dispatcher.BeginInvoke(DispatcherPriority.Background,
							(SendOrPostCallback)delegate { SetValue(TypeOfFileSearchProperty, value); },
							value);
				}
			}
		}

		public static DependencyProperty TypeOfFileSearchProperty =
			DependencyProperty.Register("TypeOfFileSearch", typeof(FileSearchType), typeof(MainFormState),
			new FrameworkPropertyMetadata(FileSearchType.Asterisk, new PropertyChangedCallback(OnTypeOfFileSearchChanged)));

		private static void OnTypeOfFileSearchChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			Properties.Settings.Default.TypeOfFileSearch = (FileSearchType)args.NewValue;
			((MainFormState)obj).UpdateState(args.Property.Name);
		}

		/// <summary>
		/// UseFileSizeFilter property
		/// </summary>
		public FileSizeFilter UseFileSizeFilter
		{
			get
			{
				try
				{
					if (this.Dispatcher.CheckAccess())
					{
						return (FileSizeFilter)GetValue(UseFileSizeFilterProperty);
					}
					else
					{
						return (FileSizeFilter)this.Dispatcher.Invoke(
						   System.Windows.Threading.DispatcherPriority.Background,
						   (DispatcherOperationCallback)delegate { return GetValue(UseFileSizeFilterProperty); },
						   UseFileSizeFilterProperty);
					}
				}
				catch
				{
					return (FileSizeFilter)UseFileSizeFilterProperty.DefaultMetadata.DefaultValue;
				}
			}
			set
			{
				if (this.Dispatcher.CheckAccess())
				{
					SetValue(UseFileSizeFilterProperty, value);
				}
				else
				{
					this.Dispatcher.BeginInvoke(DispatcherPriority.Background,
							(SendOrPostCallback)delegate { SetValue(UseFileSizeFilterProperty, value); },
							value);
				}
			}
		}

		public static DependencyProperty UseFileSizeFilterProperty =
			DependencyProperty.Register("UseFileSizeFilter", typeof(FileSizeFilter), typeof(MainFormState),
			new FrameworkPropertyMetadata(FileSizeFilter.No, new PropertyChangedCallback(OnUseFileSizeFilterChanged)));

		private static void OnUseFileSizeFilterChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			Properties.Settings.Default.UseFileSizeFilter = (FileSizeFilter)args.NewValue;
			((MainFormState)obj).UpdateState(args.Property.Name);
		}

		/// <summary>
		/// SizeFrom property
		/// </summary>
		public int SizeFrom
		{
			get
			{
				try
				{
					if (this.Dispatcher.CheckAccess())
					{
						return (int)GetValue(SizeFromProperty);
					}
					else
					{
						return (int)this.Dispatcher.Invoke(
						   System.Windows.Threading.DispatcherPriority.Background,
						   (DispatcherOperationCallback)delegate { return GetValue(SizeFromProperty); },
						   SizeFromProperty);
					}
				}
				catch
				{
					return (int)SizeFromProperty.DefaultMetadata.DefaultValue;
				}
			}
			set
			{
				if (this.Dispatcher.CheckAccess())
				{
					SetValue(SizeFromProperty, value);
				}
				else
				{
					this.Dispatcher.BeginInvoke(DispatcherPriority.Background,
							(SendOrPostCallback)delegate { SetValue(SizeFromProperty, value); },
							value);
				}
			}
		}

		public static DependencyProperty SizeFromProperty =
			DependencyProperty.Register("SizeFrom", typeof(int), typeof(MainFormState),
			new FrameworkPropertyMetadata(0, new PropertyChangedCallback(OnSizeFromChanged)));

		private static void OnSizeFromChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			Properties.Settings.Default.SizeFrom = (int)args.NewValue;
			((MainFormState)obj).UpdateState(args.Property.Name);
		}

		/// <summary>
		/// SizeTo property
		/// </summary>
		public int SizeTo
		{
			get
			{
				try
				{
					if (this.Dispatcher.CheckAccess())
					{
						return (int)GetValue(SizeToProperty);
					}
					else
					{
						return (int)this.Dispatcher.Invoke(
						   System.Windows.Threading.DispatcherPriority.Background,
						   (DispatcherOperationCallback)delegate { return GetValue(SizeToProperty); },
						   SizeToProperty);
					}
				}
				catch
				{
					return (int)SizeToProperty.DefaultMetadata.DefaultValue;
				}
			}
			set
			{
				if (this.Dispatcher.CheckAccess())
				{
					SetValue(SizeToProperty, value);
				}
				else
				{
					this.Dispatcher.BeginInvoke(DispatcherPriority.Background,
							(SendOrPostCallback)delegate { SetValue(SizeToProperty, value); },
							value);
				}
			}
		}

		public static DependencyProperty SizeToProperty =
			DependencyProperty.Register("SizeTo", typeof(int), typeof(MainFormState),
			new FrameworkPropertyMetadata(1000, new PropertyChangedCallback(OnSizeToChanged)));

		private static void OnSizeToChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			Properties.Settings.Default.SizeTo = (int)args.NewValue;
			((MainFormState)obj).UpdateState(args.Property.Name);
		}

		/// <summary>
		/// CustomEditor property
		/// </summary>
		public string CustomEditor
		{
			get { return (string)GetValue(CustomEditorProperty); }
			set { SetValue(CustomEditorProperty, value); }
		}

		public static DependencyProperty CustomEditorProperty =
			DependencyProperty.Register("CustomEditor", typeof(string), typeof(MainFormState),
			new FrameworkPropertyMetadata("", new PropertyChangedCallback(OnCustomEditorChanged)));

		private static void OnCustomEditorChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			Properties.Settings.Default.CustomEditor = (string)args.NewValue;
			((MainFormState)obj).UpdateState(args.Property.Name);
		}

		/// <summary>
		/// UseCustomEditor property
		/// </summary>
		public bool UseCustomEditor
		{
			get { return (bool)GetValue(UseCustomEditorProperty); }
			set { SetValue(UseCustomEditorProperty, value); }
		}

		public static DependencyProperty UseCustomEditorProperty =
			DependencyProperty.Register("UseCustomEditor", typeof(bool), typeof(MainFormState),
			new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnUseCustomEditorChanged)));

		private static void OnUseCustomEditorChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			Properties.Settings.Default.UseCustomEditor = (bool)args.NewValue;
			((MainFormState)obj).UpdateState(args.Property.Name);
		}

		/// <summary>
		/// CustomEditorArgs property
		/// </summary>
		public string CustomEditorArgs
		{
			get { return (string)GetValue(CustomEditorArgsProperty); }
			set { SetValue(CustomEditorArgsProperty, value); }
		}

		public static DependencyProperty CustomEditorArgsProperty =
			DependencyProperty.Register("CustomEditorArgs", typeof(string), typeof(MainFormState),
			new FrameworkPropertyMetadata("", new PropertyChangedCallback(OnCustomEditorArgsChanged)));

		private static void OnCustomEditorArgsChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			Properties.Settings.Default.CustomEditorArgs = (string)args.NewValue;
			((MainFormState)obj).UpdateState(args.Property.Name);
		}

		/// <summary>
		/// CaseSensitive property
		/// </summary>
		public bool CaseSensitive
		{
			get
			{
				try
				{
					if (this.Dispatcher.CheckAccess())
					{
						return (bool)GetValue(CaseSensitiveProperty);
					}
					else
					{
						return (bool)this.Dispatcher.Invoke(
						   System.Windows.Threading.DispatcherPriority.Background,
						   (DispatcherOperationCallback)delegate { return GetValue(CaseSensitiveProperty); },
						   CaseSensitiveProperty);
					}
				}
				catch
				{
					return (bool)CaseSensitiveProperty.DefaultMetadata.DefaultValue;
				}
			}
			set
			{
				if (this.Dispatcher.CheckAccess())
				{
					SetValue(CaseSensitiveProperty, value);
				}
				else
				{
					this.Dispatcher.BeginInvoke(DispatcherPriority.Background,
							(SendOrPostCallback)delegate { SetValue(CaseSensitiveProperty, value); },
							value);
				}
			}
		}

		public static DependencyProperty CaseSensitiveProperty =
			DependencyProperty.Register("CaseSensitive", typeof(bool), typeof(MainFormState),
			new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnCaseSensitiveChanged)));

		private static void OnCaseSensitiveChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			Properties.Settings.Default.CaseSensitive = (bool)args.NewValue;
			((MainFormState)obj).UpdateState(args.Property.Name);
		}

		/// <summary>
		/// IsCaseSensitiveEnabled property
		/// </summary>
		public bool IsCaseSensitiveEnabled
		{
			get { return (bool)GetValue(IsCaseSensitiveEnabledProperty); }
			set { SetValue(IsCaseSensitiveEnabledProperty, value); }
		}

		public static DependencyProperty IsCaseSensitiveEnabledProperty =
			DependencyProperty.Register("IsCaseSensitiveEnabled", typeof(bool), typeof(MainFormState));

		/// <summary>
		/// Multiline property
		/// </summary>
		public bool Multiline
		{
			get
			{
				try
				{
					if (this.Dispatcher.CheckAccess())
					{
						return (bool)GetValue(MultilineProperty);
					}
					else
					{
						return (bool)this.Dispatcher.Invoke(
						   System.Windows.Threading.DispatcherPriority.Background,
						   (DispatcherOperationCallback)delegate { return GetValue(MultilineProperty); },
						   MultilineProperty);
					}
				}
				catch
				{
					return (bool)MultilineProperty.DefaultMetadata.DefaultValue;
				}
			}
			set
			{
				if (this.Dispatcher.CheckAccess())
				{
					SetValue(MultilineProperty, value);
				}
				else
				{
					this.Dispatcher.BeginInvoke(DispatcherPriority.Background,
							(SendOrPostCallback)delegate { SetValue(MultilineProperty, value); },
							value);
				}
			}
		}

		public static DependencyProperty MultilineProperty =
			DependencyProperty.Register("Multiline", typeof(bool), typeof(MainFormState),
			new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnMultilineChanged)));

		private static void OnMultilineChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			Properties.Settings.Default.Multiline = (bool)args.NewValue;
			((MainFormState)obj).UpdateState(args.Property.Name);
		}

		/// <summary>
		/// IsMultilineEnabled property
		/// </summary>
		public bool IsMultilineEnabled
		{
			get { return (bool)GetValue(IsMultilineEnabledProperty); }
			set { SetValue(IsMultilineEnabledProperty, value); }
		}

		public static DependencyProperty IsMultilineEnabledProperty =
			DependencyProperty.Register("IsMultilineEnabled", typeof(bool), typeof(MainFormState));

		/// <summary>
		/// Singleline property
		/// </summary>
		public bool Singleline
		{
			get
			{
				try
				{
					if (this.Dispatcher.CheckAccess())
					{
						return (bool)GetValue(SinglelineProperty);
					}
					else
					{
						return (bool)this.Dispatcher.Invoke(
						   System.Windows.Threading.DispatcherPriority.Background,
						   (DispatcherOperationCallback)delegate { return GetValue(SinglelineProperty); },
						   SinglelineProperty);
					}
				}
				catch
				{
					return (bool)SinglelineProperty.DefaultMetadata.DefaultValue;
				}
			}
			set
			{
				if (this.Dispatcher.CheckAccess())
				{
					SetValue(SinglelineProperty, value);
				}
				else
				{
					this.Dispatcher.BeginInvoke(DispatcherPriority.Background,
							(SendOrPostCallback)delegate { SetValue(SinglelineProperty, value); },
							value);
				}
			}
		}

		public static DependencyProperty SinglelineProperty =
			DependencyProperty.Register("Singleline", typeof(bool), typeof(MainFormState),
			new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnSinglelineChanged)));

		private static void OnSinglelineChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			Properties.Settings.Default.Singleline = (bool)args.NewValue;
			((MainFormState)obj).UpdateState(args.Property.Name);
		}

		/// <summary>
		/// IsSinglelineEnabled property
		/// </summary>
		public bool IsSinglelineEnabled
		{
			get { return (bool)GetValue(IsSinglelineEnabledProperty); }
			set { SetValue(IsSinglelineEnabledProperty, value); }
		}

		public static DependencyProperty IsSinglelineEnabledProperty =
			DependencyProperty.Register("IsSinglelineEnabled", typeof(bool), typeof(MainFormState));

		#region Derived properties
		/// <summary>
		/// IsSizeFilterSet property
		/// </summary>
		public bool IsSizeFilterSet
		{
			get { return (bool)GetValue(IsSizeFilterSetProperty); }
			set { SetValue(IsSizeFilterSetProperty, value); }
		}

		public static DependencyProperty IsSizeFilterSetProperty =
			DependencyProperty.Register("IsSizeFilterSet", typeof(bool), typeof(MainFormState));

		private bool filesFound = false;
		public bool FilesFound
		{
			get { return filesFound; }
			set { filesFound = value; UpdateState("FilesFound"); }
		}

		/// <summary>
		/// CanSearch property
		/// </summary>
		public bool CanSearch
		{
			get { return (bool)GetValue(CanSearchProperty); }
			set { SetValue(CanSearchProperty, value); }
		}

		public static DependencyProperty CanSearchProperty =
			DependencyProperty.Register("CanSearch", typeof(bool), typeof(MainFormState));

		/// <summary>
		/// CanSearchInResults property
		/// </summary>
		public bool CanSearchInResults
		{
			get { return (bool)GetValue(CanSearchInResultsProperty); }
			set { SetValue(CanSearchInResultsProperty, value); }
		}

		public static DependencyProperty CanSearchInResultsProperty =
			DependencyProperty.Register("CanSearchInResults", typeof(bool), typeof(MainFormState));

		/// <summary>
		/// SearchButtonMode property
		/// </summary>
		public string SearchButtonMode
		{
			get { return (string)GetValue(SearchButtonModeProperty); }
			set { SetValue(SearchButtonModeProperty, value); }
		}

		public static DependencyProperty SearchButtonModeProperty =
			DependencyProperty.Register("SearchButtonMode", typeof(string), typeof(MainFormState));

		/// <summary>
		/// CanReplace property
		/// </summary>
		public bool CanReplace
		{
			get { return (bool)GetValue(CanReplaceProperty); }
			set { SetValue(CanReplaceProperty, value); }
		}

		public static DependencyProperty CanReplaceProperty =
			DependencyProperty.Register("CanReplace", typeof(bool), typeof(MainFormState));

		/// <summary>
		/// CanCancel property
		/// </summary>
		public bool CanCancel
		{
			get { return (bool)GetValue(CanCancelProperty); }
			set { SetValue(CanCancelProperty, value); }
		}

		public static DependencyProperty CanCancelProperty =
			DependencyProperty.Register("CanCancel", typeof(bool), typeof(MainFormState));

		/// <summary>
		/// CurrentGrepOperation property
		/// </summary>
		public GrepOperation CurrentGrepOperation
		{
			get {
				try
				{
					if (this.Dispatcher.CheckAccess())
					{
						return (GrepOperation)GetValue(CurrentGrepOperationProperty); 
					}
					else
					{
						return (GrepOperation)this.Dispatcher.Invoke(
						   System.Windows.Threading.DispatcherPriority.Background,
						   (DispatcherOperationCallback)delegate { return GetValue(CurrentGrepOperationProperty); },
						   CurrentGrepOperationProperty);
					}
				}
				catch
				{
					return (GrepOperation)CurrentGrepOperationProperty.DefaultMetadata.DefaultValue;
				}
			}
			set {
				if (this.Dispatcher.CheckAccess())
				{
					SetValue(CurrentGrepOperationProperty, value);
				}
				else
				{
					this.Dispatcher.BeginInvoke(DispatcherPriority.Background,
							(SendOrPostCallback)delegate { SetValue(CurrentGrepOperationProperty, value); },
							value);
				}
			}
		}

		public static DependencyProperty CurrentGrepOperationProperty =
			DependencyProperty.Register("CurrentGrepOperation", typeof(GrepOperation), typeof(MainFormState),
			new FrameworkPropertyMetadata(GrepOperation.None, new PropertyChangedCallback(OnCurrentGrepOperationChanged)));

		private static void OnCurrentGrepOperationChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			((MainFormState)obj).UpdateState(args.Property.Name);
		}

		/// <summary>
		/// OptionsSummary property
		/// </summary>
		public string OptionsSummary
		{
			get { return (string)GetValue(OptionsSummaryProperty); }
			set { SetValue(OptionsSummaryProperty, value); }
		}

		public static DependencyProperty OptionsSummaryProperty =
			DependencyProperty.Register("OptionsSummary", typeof(string), typeof(MainFormState));

		/// <summary>
		/// TextBoxStyle property
		/// </summary>
		public string TextBoxStyle
		{
			get { return (string)GetValue(TextBoxStyleProperty); }
			set { SetValue(TextBoxStyleProperty, value); }
		}

		public static DependencyProperty TextBoxStyleProperty =
			DependencyProperty.Register("TextBoxStyle", typeof(string), typeof(MainFormState));

		/// <summary>
		/// CodePage property
		/// </summary>
		public int CodePage
		{
			get
			{
				try
				{
					if (this.Dispatcher.CheckAccess())
					{
						return (int)GetValue(CodePageProperty);
					}
					else
					{
						return (int)this.Dispatcher.Invoke(
						   System.Windows.Threading.DispatcherPriority.Background,
						   (DispatcherOperationCallback)delegate { return GetValue(CodePageProperty); },
						   CodePageProperty);
					}
				}
				catch
				{
					return (int)CodePageProperty.DefaultMetadata.DefaultValue;
				}
			}
			set
			{
				if (this.Dispatcher.CheckAccess())
				{
					SetValue(CodePageProperty, value);
				}
				else
				{
					this.Dispatcher.BeginInvoke(DispatcherPriority.Background,
							(SendOrPostCallback)delegate { SetValue(CodePageProperty, value); },
							value);
				}
			}
		}

		public static DependencyProperty CodePageProperty =
			DependencyProperty.Register("CodePage", typeof(int), typeof(MainFormState),
			new FrameworkPropertyMetadata(0, new PropertyChangedCallback(OnCodePageChanged)));

		private static void OnCodePageChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			((MainFormState)obj).UpdateState(args.Property.Name);
		}

		/// <summary>
		/// CanUndo property
		/// </summary>
		public bool CanUndo
		{
			get { return (bool)GetValue(CanUndoProperty); }
			set { SetValue(CanUndoProperty, value); }
		}

		public static DependencyProperty CanUndoProperty =
			DependencyProperty.Register("CanUndo", typeof(bool), typeof(MainFormState));

		/// <summary>
		/// Undo folder
		/// </summary>
		private string undoFolder = "";

		public string UndoFolder
		{
			get { return undoFolder; }
			set { undoFolder = value; }
		}

		#endregion
		public void UpdateState(string name)
		{
			switch (name)
			{
				case "UseFileSizeFilter":
					if (UseFileSizeFilter == FileSizeFilter.Yes)
					{
						IsSizeFilterSet = true;
					}
					else
					{
						IsSizeFilterSet = false;
					}
					break;
				case "Multiline":
				case "Singleline":
				case "CaseSensitive":
					List<string> tempList = new List<string>();
					if (CaseSensitive)
						tempList.Add("Case sensitive");
					if (Multiline)
						tempList.Add("Multiline");
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

			//Can search
			if (Utils.IsPathValid(FileOrFolderPath) && CurrentGrepOperation == GrepOperation.None &&
				(!string.IsNullOrEmpty(SearchFor) || Properties.Settings.Default.AllowSearchingForFileNamePattern)) 
			{
				CanSearch = true;
			} 
			else 
			{
				CanSearch = false;
			}

			//btnSearch.ShowAdvance
			if (searchResults.Count > 0)
			{
				//TODO
				CanSearchInResults = true;
				SearchButtonMode = "Split";
			}
			else
			{
				//TODO
				CanSearchInResults = false;
				SearchButtonMode = "Button";
			}

			//searchResults
			searchResults.FolderPath = FileOrFolderPath;

			// btnReplace
			if (Utils.IsPathValid(FileOrFolderPath) && FilesFound && CurrentGrepOperation == GrepOperation.None &&
				!string.IsNullOrEmpty(SearchFor))
			{
				CanReplace = true;
			}
			else
			{
				CanReplace = false;
			}

			//btnCancel
			if (CurrentGrepOperation != GrepOperation.None)
			{
				CanCancel = true;
			}
			else
			{
				CanCancel = false;
			}

			//IsCaseSensitiveEnabled
			if (TypeOfSearch == SearchType.XPath)
			{
				IsCaseSensitiveEnabled = false;
				CaseSensitive = false;
			}
			else
			{
				IsCaseSensitiveEnabled = true;
			}

			//IsMultilineEnabled
			if (TypeOfSearch == SearchType.XPath)
			{
				IsMultilineEnabled = false;
				Multiline = false;
			}
			else
			{
				IsMultilineEnabled = true;
			}

			//IsSinglelineAvailable
			if (TypeOfSearch != SearchType.Regex)
			{
				IsSinglelineEnabled = false;
				Singleline = false;
			}
			else
			{
				IsSinglelineEnabled = true;
			}

			////btnTest
			//if (!IsPlainText &&
			//    !rbXPath.IsChecked == true)
			//{
			//    btnTest.IsEnabled = true;
			//}
			//else
			//{
			//    btnTest.IsEnabled = false;
			//}		
		}
	}
}
