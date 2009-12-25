using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using dnGREP.Common;

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
		}

		/// <summary>
		/// FileOrFolderPath property
		/// </summary>
		public string FileOrFolderPath
		{
			get { return (string)GetValue(FileOrFolderPathProperty); }
			set { SetValue(FileOrFolderPathProperty, value); }			
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
			get { return (string)GetValue(SearchForProperty); }
			set { SetValue(SearchForProperty, value); }
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
			get { return (string)GetValue(ReplaceWithProperty); }
			set { SetValue(ReplaceWithProperty, value); }
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
			get { return (string)GetValue(FilePatternProperty); }
			set { SetValue(FilePatternProperty, value); }
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
			get { return (bool)GetValue(IncludeSubfolderProperty); }
			set { SetValue(IncludeSubfolderProperty, value); }
		}

		public static DependencyProperty IncludeSubfolderProperty =
			DependencyProperty.Register("IncludeSubfolder", typeof(bool), typeof(MainFormState),
			new FrameworkPropertyMetadata(true, new PropertyChangedCallback(OnIncludeSubfolderChanged)));

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
			get { return (bool)GetValue(IncludeHiddenProperty); }
			set { SetValue(IncludeHiddenProperty, value); }
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
			get { return (SearchType)GetValue(TypeOfSearchProperty); }
			set { SetValue(TypeOfSearchProperty, value); }
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
			get { return (FileSearchType)GetValue(TypeOfFileSearchProperty); }
			set { SetValue(TypeOfFileSearchProperty, value); }
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
			get { return (FileSizeFilter)GetValue(UseFileSizeFilterProperty); }
			set { SetValue(UseFileSizeFilterProperty, value); }
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
		public string SizeFrom
		{
			get { return (string)GetValue(SizeFromProperty); }
			set { SetValue(SizeFromProperty, value); }
		}

		public static DependencyProperty SizeFromProperty =
			DependencyProperty.Register("SizeFrom", typeof(string), typeof(MainFormState),
			new FrameworkPropertyMetadata("0", new PropertyChangedCallback(OnSizeFromChanged)));

		private static void OnSizeFromChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			Properties.Settings.Default.SizeFrom = (string)args.NewValue;
			((MainFormState)obj).UpdateState(args.Property.Name);
		}

		/// <summary>
		/// SizeTo property
		/// </summary>
		public string SizeTo
		{
			get { return (string)GetValue(SizeToProperty); }
			set { SetValue(SizeToProperty, value); }
		}

		public static DependencyProperty SizeToProperty =
			DependencyProperty.Register("SizeTo", typeof(string), typeof(MainFormState),
			new FrameworkPropertyMetadata("1000", new PropertyChangedCallback(OnSizeToChanged)));

		private static void OnSizeToChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			Properties.Settings.Default.SizeTo = (string)args.NewValue;
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
			get { return (bool)GetValue(CaseSensitiveProperty); }
			set { SetValue(CaseSensitiveProperty, value); }
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
		/// Multiline property
		/// </summary>
		public bool Multiline
		{
			get { return (bool)GetValue(MultilineProperty); }
			set { SetValue(MultilineProperty, value); }
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
		/// Singleline property
		/// </summary>
		public bool Singleline
		{
			get { return (bool)GetValue(SinglelineProperty); }
			set { SetValue(SinglelineProperty, value); }
		}

		public static DependencyProperty SinglelineProperty =
			DependencyProperty.Register("Singleline", typeof(bool), typeof(MainFormState),
			new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnSinglelineChanged)));

		private static void OnSinglelineChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			Properties.Settings.Default.Singleline = (bool)args.NewValue;
			((MainFormState)obj).UpdateState(args.Property.Name);
		}

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
		/// CurrentGrepOperation property
		/// </summary>
		public GrepOperation CurrentGrepOperation
		{
			get { return (GrepOperation)GetValue(CurrentGrepOperationProperty); }
			set { SetValue(CurrentGrepOperationProperty, value); }
		}

		public static DependencyProperty CurrentGrepOperationProperty =
			DependencyProperty.Register("CurrentGrepOperation", typeof(GrepOperation), typeof(MainFormState),
			new FrameworkPropertyMetadata(GrepOperation.None, new PropertyChangedCallback(OnCurrentGrepOperationChanged)));

		private static void OnCurrentGrepOperationChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			((MainFormState)obj).UpdateState(args.Property.Name);
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
			}

			if (Utils.IsPathValid(FileOrFolderPath) && CurrentGrepOperation == GrepOperation.None &&
				(!string.IsNullOrEmpty(SearchFor) || Properties.Settings.Default.AllowSearchingForFileNamePattern)) 
			{
				CanSearch = true;
			} 
			else 
			{
				CanSearch = false;
				CanSearchInResults = false;
			}
			

			////btnSearch.ShowAdvance
			//if (searchResults.Count > 0)
			//{
			//    //TODO
			//    //btnSearch.ShowSplit = true;
			//}
			//else
			//{
			//    //TODO
			//    //btnSearch.ShowSplit = false;
			//}

			//// btnReplace
			//if (FolderSelected && FilesFound && !IsSearching && !IsReplacing
			//    && SearchPatternEntered)
			//{
			//    btnReplace.IsEnabled = true;
			//}
			//else
			//{
			//    btnReplace.IsEnabled = false;
			//}

			////btnCancel
			//if (IsSearching)
			//{
			//    btnCancel.IsEnabled = true;
			//}
			//else if (IsReplacing)
			//{
			//    btnCancel.IsEnabled = true;
			//}
			//else
			//{
			//    btnCancel.IsEnabled = false;
			//}

			////undoToolStripMenuItem
			//if (CanUndo)
			//{
			//    undoToolStripMenuItem.IsEnabled = true;
			//}
			//else
			//{
			//    undoToolStripMenuItem.IsEnabled = false;
			//}

			////cbCaseSensitive
			//if (rbXPath.IsChecked == true)
			//{
			//    cbCaseSensitive.IsEnabled = false;
			//}
			//else
			//{
			//    cbCaseSensitive.IsEnabled = true;
			//}

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

			////cbMultiline
			//if (rbXPath.IsChecked == true)
			//{
			//    cbMultiline.IsEnabled = false;
			//}
			//else
			//{
			//    cbMultiline.IsEnabled = true;
			//}
		}
	}
}
