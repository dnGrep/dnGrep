using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using dnGREP.Common;
using dnGREP.Everything;
using dnGREP.Localization;
using Resources = dnGREP.Localization.Properties.Resources;

namespace dnGREP.WPF
{
    public class BookmarkListViewModel : CultureAwareViewModel
    {
        readonly Action<Bookmark> ClearStar;
        readonly Window ownerWnd;

        public BookmarkListViewModel(Window owner, Action<Bookmark> clearStar)
        {
            ownerWnd = owner;
            ClearStar = clearStar;

            var items = BookmarkLibrary.Instance.Bookmarks.Select(bk => new BookmarkViewModel(bk)).ToList();
            Bookmarks = new ListCollectionView(items)
            {
                Filter = BookmarkFilter
            };

            ApplicationFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ApplicationFontFamily);
            DialogFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.DialogFontSize);
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

        private double dialogfontSize;
        public double DialogFontSize
        {
            get { return dialogfontSize; }
            set
            {
                if (dialogfontSize == value)
                    return;

                dialogfontSize = value;
                base.OnPropertyChanged(() => DialogFontSize);
            }
        }

        public ListCollectionView Bookmarks { get; private set; }

        private bool BookmarkFilter(object obj)
        {
            if (obj is BookmarkViewModel bmk)
            {
                if (string.IsNullOrWhiteSpace(FilterText))
                {
                    return true;
                }
                else
                {
                    return bmk.Description.IndexOf(FilterText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                           bmk.SearchFor.IndexOf(FilterText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                           bmk.ReplaceWith.IndexOf(FilterText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                           bmk.FilePattern.IndexOf(FilterText, StringComparison.OrdinalIgnoreCase) >= 0;
                }
            }
            return false;
        }

        private string filterText = string.Empty;
        public string FilterText
        {
            get { return filterText; }
            set
            {
                if (filterText == value)
                    return;

                filterText = value;
                OnPropertyChanged(nameof(FilterText));

                Bookmarks.Refresh();
            }
        }

        private BookmarkViewModel selectedBookmark = null;
        public BookmarkViewModel SelectedBookmark
        {
            get { return selectedBookmark; }
            set
            {
                if (selectedBookmark == value)
                    return;

                selectedBookmark = value;
                OnPropertyChanged(nameof(SelectedBookmark));

                HasSelection = selectedBookmark != null;
            }
        }

        private bool hasSelection = false;
        public bool HasSelection
        {
            get { return hasSelection; }
            set
            {
                if (hasSelection == value)
                    return;

                hasSelection = value;
                OnPropertyChanged(nameof(HasSelection));
            }
        }


        RelayCommand _addCommand;
        public ICommand AddCommand
        {
            get
            {
                if (_addCommand == null)
                {
                    _addCommand = new RelayCommand(
                        param => AddBookmark()
                        );
                }
                return _addCommand;
            }
        }

        RelayCommand _editCommand;
        public ICommand EditCommand
        {
            get
            {
                if (_editCommand == null)
                {
                    _editCommand = new RelayCommand(
                        param => Edit(),
                        param => SelectedBookmark != null
                        );
                }
                return _editCommand;
            }
        }

        RelayCommand _duplicateCommand;
        public ICommand DuplicateCommand
        {
            get
            {
                if (_duplicateCommand == null)
                {
                    _duplicateCommand = new RelayCommand(
                        param => Duplicate(),
                        param => SelectedBookmark != null
                        );
                }
                return _duplicateCommand;
            }
        }

        RelayCommand _deleteCommand;
        public ICommand DeleteCommand
        {
            get
            {
                if (_deleteCommand == null)
                {
                    _deleteCommand = new RelayCommand(
                        param => Delete(),
                        param => SelectedBookmark != null
                        );
                }
                return _deleteCommand;
            }
        }

        private void Delete()
        {
            if (SelectedBookmark != null)
            {
                ClearStar(SelectedBookmark.ToBookmark());

                var bmk = BookmarkLibrary.Instance.Find(SelectedBookmark.ToBookmark());
                if (bmk != null)
                {
                    BookmarkLibrary.Instance.Bookmarks.Remove(bmk);
                    BookmarkLibrary.Save();

                    Bookmarks.Remove(SelectedBookmark);
                }
            }
        }

        private void Edit()
        {
            if (SelectedBookmark != null)
            {
                // edit a copy
                var editBmk = new BookmarkViewModel(SelectedBookmark);
                editBmk.SetEditMode(SelectedBookmark.ToBookmark());
                var dlg = new BookmarkDetailWindow
                {
                    DataContext = editBmk,
                    Owner = ownerWnd
                };

                var result = dlg.ShowDialog();
                if (result.HasValue && result.Value)
                {
                    editBmk.SetExtendedProperties();

                    if (SelectedBookmark != editBmk)
                    {
                        ClearStar(SelectedBookmark.ToBookmark());
                    }

                    var bmk = BookmarkLibrary.Instance.Find(SelectedBookmark.ToBookmark());
                    if (bmk != null)
                    {
                        BookmarkLibrary.Instance.Bookmarks.Remove(bmk);
                        Bookmarks.Remove(SelectedBookmark);
                    }

                    var newBmk = new Bookmark(editBmk.SearchFor, editBmk.ReplaceWith, editBmk.FilePattern)
                    {
                        Description = editBmk.Description,
                        IgnoreFilePattern = editBmk.IgnoreFilePattern,
                        TypeOfFileSearch = editBmk.TypeOfFileSearch,
                        TypeOfSearch = editBmk.TypeOfSearch,
                        CaseSensitive = editBmk.CaseSensitive,
                        WholeWord = editBmk.WholeWord,
                        Multiline = editBmk.Multiline,
                        Singleline = editBmk.Singleline,
                        BooleanOperators = editBmk.BooleanOperators,
                        IncludeSubfolders = editBmk.IncludeSubfolders,
                        MaxSubfolderDepth = editBmk.MaxSubfolderDepth,
                        IncludeHiddenFiles = editBmk.IncludeHidden,
                        IncludeBinaryFiles = editBmk.IncludeBinary,
                        UseGitignore = editBmk.UseGitignore,
                        IncludeArchive = editBmk.IncludeArchive,
                        FollowSymlinks = editBmk.FollowSymlinks,
                        CodePage = editBmk.CodePage,
                    };
                    string[] paths = editBmk.PathReferences.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    newBmk.FolderReferences.AddRange(paths);
                    BookmarkLibrary.Instance.Bookmarks.Add(newBmk);
                    BookmarkLibrary.Save();
                    Bookmarks.AddNewItem(editBmk);
                    Bookmarks.CommitNew();
                    SelectedBookmark = editBmk;
                }
            }
        }

        private void Duplicate()
        {
            if (SelectedBookmark != null)
            {
                // edit a copy
                var duplicateBmk = new BookmarkViewModel(SelectedBookmark);
                var dlg = new BookmarkDetailWindow
                {
                    DataContext = duplicateBmk,
                    Owner = ownerWnd
                };

                var result = dlg.ShowDialog();
                if (result.HasValue && result.Value)
                {
                    duplicateBmk.SetExtendedProperties();

                    var newBmk = new Bookmark(duplicateBmk.SearchFor, duplicateBmk.ReplaceWith, duplicateBmk.FilePattern)
                    {
                        Description = duplicateBmk.Description,
                        IgnoreFilePattern = duplicateBmk.IgnoreFilePattern,
                        TypeOfFileSearch = duplicateBmk.TypeOfFileSearch,
                        TypeOfSearch = duplicateBmk.TypeOfSearch,
                        CaseSensitive = duplicateBmk.CaseSensitive,
                        WholeWord = duplicateBmk.WholeWord,
                        Multiline = duplicateBmk.Multiline,
                        Singleline = duplicateBmk.Singleline,
                        BooleanOperators = duplicateBmk.BooleanOperators,
                        IncludeSubfolders = duplicateBmk.IncludeSubfolders,
                        MaxSubfolderDepth = duplicateBmk.MaxSubfolderDepth,
                        IncludeHiddenFiles = duplicateBmk.IncludeHidden,
                        IncludeBinaryFiles = duplicateBmk.IncludeBinary,
                        UseGitignore = duplicateBmk.UseGitignore,
                        IncludeArchive = duplicateBmk.IncludeArchive,
                        FollowSymlinks = duplicateBmk.FollowSymlinks,
                        CodePage = duplicateBmk.CodePage,
                    };
                    BookmarkLibrary.Instance.Bookmarks.Add(newBmk);
                    BookmarkLibrary.Save();
                    Bookmarks.AddNewItem(duplicateBmk);
                    Bookmarks.CommitNew();
                    SelectedBookmark = duplicateBmk;
                }
            }
        }

        private void AddBookmark()
        {
            var editBmk = new BookmarkViewModel(new Bookmark());
            var dlg = new BookmarkDetailWindow
            {
                DataContext = editBmk,
                Owner = ownerWnd
            };

            var result = dlg.ShowDialog();
            if (result.HasValue && result.Value)
            {
                editBmk.SetExtendedProperties();

                var newBmk = new Bookmark(editBmk.SearchFor, editBmk.ReplaceWith, editBmk.FilePattern)
                {
                    Description = editBmk.Description,
                    IgnoreFilePattern = editBmk.IgnoreFilePattern,
                    TypeOfFileSearch = editBmk.TypeOfFileSearch,
                    TypeOfSearch = editBmk.TypeOfSearch,
                    CaseSensitive = editBmk.CaseSensitive,
                    WholeWord = editBmk.WholeWord,
                    Multiline = editBmk.Multiline,
                    Singleline = editBmk.Singleline,
                    BooleanOperators = editBmk.BooleanOperators,
                    IncludeSubfolders = editBmk.IncludeSubfolders,
                    MaxSubfolderDepth = editBmk.MaxSubfolderDepth,
                    IncludeHiddenFiles = editBmk.IncludeHidden,
                    IncludeBinaryFiles = editBmk.IncludeBinary,
                    UseGitignore = editBmk.UseGitignore,
                    IncludeArchive = editBmk.IncludeArchive,
                    FollowSymlinks = editBmk.FollowSymlinks,
                    CodePage = editBmk.CodePage,
                };
                string[] paths = editBmk.PathReferences.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                newBmk.FolderReferences.AddRange(paths);
                BookmarkLibrary.Instance.Bookmarks.Add(newBmk);
                BookmarkLibrary.Save();
                Bookmarks.AddNewItem(editBmk);
                Bookmarks.CommitNew();
                SelectedBookmark = editBmk;
            }
        }
    }

    public class BookmarkViewModel : CultureAwareViewModel
    {
        public static ObservableCollection<KeyValuePair<string, int>> Encodings { get; } = new ObservableCollection<KeyValuePair<string, int>>();

        private Bookmark _original;

        public void SetEditMode(Bookmark original)
        {
            _original = original;
        }

        public BookmarkViewModel(Bookmark bk)
        {
            IsEverythingAvailable = EverythingSearch.IsAvailable;
            IsGitInstalled = Utils.IsGitInstalled;

            Description = bk.Description;
            FilePattern = bk.FileNames;
            SearchFor = bk.SearchPattern;
            ReplaceWith = bk.ReplacePattern;

            TypeOfSearch = bk.TypeOfSearch;
            CaseSensitive = bk.CaseSensitive;
            WholeWord = bk.WholeWord;
            Multiline = bk.Multiline;
            Singleline = bk.Singleline;
            BooleanOperators = bk.BooleanOperators;

            TypeOfFileSearch = bk.TypeOfFileSearch;
            IgnoreFilePattern = bk.IgnoreFilePattern;
            IncludeBinary = bk.IncludeBinaryFiles;
            IncludeHidden = bk.IncludeHiddenFiles;
            IncludeSubfolders = bk.IncludeSubfolders;
            MaxSubfolderDepth = bk.MaxSubfolderDepth;
            UseGitignore = bk.UseGitignore;
            IncludeArchive = bk.IncludeArchive;
            FollowSymlinks = bk.FollowSymlinks;
            CodePage = bk.CodePage;
            PathReferences = string.Join(Environment.NewLine, bk.FolderReferences);

            UpdateTypeOfSearchState();

            SetExtendedProperties();

            ApplicationFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ApplicationFontFamily);
            DialogFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.DialogFontSize);
        }

        public BookmarkViewModel(BookmarkViewModel toCopy)
        {
            IsEverythingAvailable = EverythingSearch.IsAvailable;
            IsGitInstalled = Utils.IsGitInstalled;

            Description = toCopy.Description;
            FilePattern = toCopy.FilePattern;
            SearchFor = toCopy.SearchFor;
            ReplaceWith = toCopy.ReplaceWith;

            TypeOfSearch = toCopy.TypeOfSearch;
            TypeOfFileSearch = toCopy.TypeOfFileSearch;
            IgnoreFilePattern = toCopy.IgnoreFilePattern;

            IncludeBinary = toCopy.IncludeBinary;
            IncludeHidden = toCopy.IncludeHidden;
            IncludeSubfolders = toCopy.IncludeSubfolders;
            MaxSubfolderDepth = toCopy.MaxSubfolderDepth;
            UseGitignore = toCopy.UseGitignore;
            IncludeArchive = toCopy.IncludeArchive;
            FollowSymlinks = toCopy.FollowSymlinks;
            CodePage = toCopy.CodePage;
            PathReferences = string.Join(Environment.NewLine, toCopy.PathReferences);

            CaseSensitive = toCopy.CaseSensitive;
            IsCaseSensitiveEnabled = toCopy.IsCaseSensitiveEnabled;

            WholeWord = toCopy.WholeWord;
            IsWholeWordEnabled = toCopy.IsWholeWordEnabled;

            Multiline = toCopy.Multiline;
            IsMultilineEnabled = toCopy.IsMultilineEnabled;

            Singleline = toCopy.Singleline;
            IsSinglelineEnabled = toCopy.IsSinglelineEnabled;

            BooleanOperators = toCopy.BooleanOperators;
            IsBooleanOperatorsEnabled = toCopy.IsBooleanOperatorsEnabled;

            SetExtendedProperties();

            ApplicationFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ApplicationFontFamily);
            DialogFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.DialogFontSize);
        }

        internal void SetExtendedProperties()
        {
            var tempList = new List<string>();
            if (IncludeArchive)
                tempList.Add(Resources.Bookmarks_Summary_SearchInArchives);
            if (!IncludeSubfolders || (IncludeSubfolders && MaxSubfolderDepth == 0))
                tempList.Add(Resources.Bookmarks_Summary_NoSubfolders);
            if (IncludeSubfolders && MaxSubfolderDepth > 0)
                tempList.Add(TranslationSource.Format(Resources.Bookmarks_Summary_MaxFolderDepth, MaxSubfolderDepth));
            if (!IncludeHidden)
                tempList.Add(Resources.Bookmarks_Summary_NoHidden);
            if (!IncludeBinary)
                tempList.Add(Resources.Bookmarks_Summary_NoBinary);
            if (!FollowSymlinks)
                tempList.Add(Resources.Bookmarks_Summary_NoSymlinks);
            if (CaseSensitive)
                tempList.Add(Resources.Bookmarks_Summary_CaseSensitive);
            if (WholeWord)
                tempList.Add(Resources.Bookmarks_Summary_WholeWord);
            if (Multiline)
                tempList.Add(Resources.Bookmarks_Summary_Multiline);

            if (tempList.Count == 0)
            {
                ExtendedProperties = string.Empty;
            }
            else
            {
                ExtendedProperties = string.Join(", ", tempList);
            }
        }

        public Bookmark ToBookmark()
        {
            return new Bookmark(SearchFor, ReplaceWith, FilePattern)
            {
                Description = Description,
                IgnoreFilePattern = IgnoreFilePattern,
                TypeOfFileSearch = TypeOfFileSearch,
                TypeOfSearch = TypeOfSearch,
                CaseSensitive = CaseSensitive,
                WholeWord = WholeWord,
                Multiline = Multiline,
                Singleline = Singleline,
                BooleanOperators = BooleanOperators,
                IncludeSubfolders = IncludeSubfolders,
                IncludeHiddenFiles = IncludeHidden,
                IncludeBinaryFiles = IncludeBinary,
                MaxSubfolderDepth = MaxSubfolderDepth,
                UseGitignore = UseGitignore,
                IncludeArchive = IncludeArchive,
                FollowSymlinks = FollowSymlinks,
                CodePage = CodePage,
                FolderReferences = PathReferences.Split(new char[] { '\n', '\r'}, StringSplitOptions.RemoveEmptyEntries).ToList()
            };
        }

        private void UpdateTypeOfSearchState()
        {
            IsCaseSensitiveEnabled = true;
            IsMultilineEnabled = true;
            IsSinglelineEnabled = true;
            IsWholeWordEnabled = true;
            IsBooleanOperatorsEnabled = true;

            if (TypeOfSearch == SearchType.XPath)
            {
                IsCaseSensitiveEnabled = false;
                IsMultilineEnabled = false;
                IsSinglelineEnabled = false;
                IsWholeWordEnabled = false;
                IsBooleanOperatorsEnabled = false;
                CaseSensitive = false;
                Multiline = false;
                Singleline = false;
                WholeWord = false;
                BooleanOperators = false;
            }
            else if (TypeOfSearch == SearchType.PlainText)
            {
                IsSinglelineEnabled = false;
                Singleline = false;
            }
            else if (TypeOfSearch == SearchType.Soundex)
            {
                IsCaseSensitiveEnabled = false;
                IsSinglelineEnabled = false;
                IsBooleanOperatorsEnabled = false;
                CaseSensitive = false;
                Singleline = false;
                BooleanOperators = false;
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

        private double dialogfontSize;
        public double DialogFontSize
        {
            get { return dialogfontSize; }
            set
            {
                if (dialogfontSize == value)
                    return;

                dialogfontSize = value;
                base.OnPropertyChanged(() => DialogFontSize);
            }
        }

        private string extendedProperties = string.Empty;
        public string ExtendedProperties
        {
            get { return extendedProperties; }
            set
            {
                if (extendedProperties == value)
                    return;

                extendedProperties = value;
                OnPropertyChanged(nameof(ExtendedProperties));
            }
        }

        private string description = string.Empty;
        public string Description
        {
            get { return description; }
            set
            {
                if (description == value)
                    return;

                description = value;
                OnPropertyChanged(nameof(Description));
            }
        }

        private string filePattern = string.Empty;
        public string FilePattern
        {
            get { return filePattern; }
            set
            {
                if (filePattern == value)
                    return;

                filePattern = value;
                OnPropertyChanged(nameof(FilePattern));
            }
        }

        private string searchFor = string.Empty;
        public string SearchFor
        {
            get { return searchFor; }
            set
            {
                if (searchFor == value)
                    return;

                searchFor = value;
                OnPropertyChanged(nameof(SearchFor));
            }
        }

        private string replaceWith = string.Empty;
        public string ReplaceWith
        {
            get { return replaceWith; }
            set
            {
                if (replaceWith == value)
                    return;

                replaceWith = value;
                OnPropertyChanged(nameof(ReplaceWith));
            }
        }

        private string ignoreFilePattern = string.Empty;
        public string IgnoreFilePattern
        {
            get { return ignoreFilePattern; }
            set
            {
                if (ignoreFilePattern == value)
                    return;

                ignoreFilePattern = value;
                OnPropertyChanged(nameof(IgnoreFilePattern));
            }
        }

        private FileSearchType typeOfFileSearch = FileSearchType.Asterisk;
        public FileSearchType TypeOfFileSearch
        {
            get { return typeOfFileSearch; }
            set
            {
                if (typeOfFileSearch == value)
                    return;

                typeOfFileSearch = value;
                OnPropertyChanged(nameof(TypeOfFileSearch));
            }
        }

        private SearchType typeOfSearch = SearchType.PlainText;
        public SearchType TypeOfSearch
        {
            get { return typeOfSearch; }
            set
            {
                if (typeOfSearch == value)
                    return;

                typeOfSearch = value;
                OnPropertyChanged(nameof(TypeOfSearch));

                UpdateTypeOfSearchState();
            }
        }

        private bool caseSensitive = false;
        public bool CaseSensitive
        {
            get { return caseSensitive; }
            set
            {
                if (caseSensitive == value)
                    return;

                caseSensitive = value;
                OnPropertyChanged(nameof(CaseSensitive));
            }
        }

        private bool isCaseSensitiveEnabled = false;
        public bool IsCaseSensitiveEnabled
        {
            get { return isCaseSensitiveEnabled; }
            set
            {
                if (isCaseSensitiveEnabled == value)
                    return;

                isCaseSensitiveEnabled = value;
                OnPropertyChanged(nameof(IsCaseSensitiveEnabled));
            }
        }

        private bool wholeWord = false;
        public bool WholeWord
        {
            get { return wholeWord; }
            set
            {
                if (wholeWord == value)
                    return;

                wholeWord = value;
                OnPropertyChanged(nameof(WholeWord));
            }
        }

        private bool isWholeWordEnabled = false;
        public bool IsWholeWordEnabled
        {
            get { return isWholeWordEnabled; }
            set
            {
                if (isWholeWordEnabled == value)
                    return;

                isWholeWordEnabled = value;
                OnPropertyChanged(nameof(IsWholeWordEnabled));
            }
        }

        private bool multiline = false;
        public bool Multiline
        {
            get { return multiline; }
            set
            {
                if (multiline == value)
                    return;

                multiline = value;
                OnPropertyChanged(nameof(Multiline));
            }
        }

        private bool isMultilineEnabled = false;
        public bool IsMultilineEnabled
        {
            get { return isMultilineEnabled; }
            set
            {
                if (isMultilineEnabled == value)
                    return;

                isMultilineEnabled = value;
                OnPropertyChanged(nameof(IsMultilineEnabled));
            }
        }


        private bool singleline = false;
        public bool Singleline
        {
            get { return singleline; }
            set
            {
                if (singleline == value)
                    return;

                singleline = value;
                OnPropertyChanged(nameof(Singleline));
            }
        }

        private bool isSinglelineEnabled = false;
        public bool IsSinglelineEnabled
        {
            get { return isSinglelineEnabled; }
            set
            {
                if (isSinglelineEnabled == value)
                    return;

                isSinglelineEnabled = value;
                OnPropertyChanged(nameof(IsSinglelineEnabled));
            }
        }


        private bool booleanOperators = false;
        public bool BooleanOperators
        {
            get { return booleanOperators; }
            set
            {
                if (booleanOperators == value)
                    return;

                booleanOperators = value;
                OnPropertyChanged(nameof(BooleanOperators));
            }
        }

        private bool isbooleanOperatorsEnabled = false;
        public bool IsBooleanOperatorsEnabled
        {
            get { return isbooleanOperatorsEnabled; }
            set
            {
                if (isbooleanOperatorsEnabled == value)
                    return;

                isbooleanOperatorsEnabled = value;
                OnPropertyChanged(nameof(IsBooleanOperatorsEnabled));
            }
        }

        private bool includeSubfolders = true;
        public bool IncludeSubfolders
        {
            get { return includeSubfolders; }
            set
            {
                if (includeSubfolders == value)
                    return;

                includeSubfolders = value;
                OnPropertyChanged(nameof(IncludeSubfolders));

                if (!includeSubfolders)
                {
                    MaxSubfolderDepth = -1;
                }
            }
        }

        private int maxSubfolderDepth = -1;
        public int MaxSubfolderDepth
        {
            get { return maxSubfolderDepth; }
            set
            {
                if (value == maxSubfolderDepth)
                    return;

                maxSubfolderDepth = value;

                base.OnPropertyChanged(() => MaxSubfolderDepth);
            }
        }

        private bool includeHidden = false;
        public bool IncludeHidden
        {
            get { return includeHidden; }
            set
            {
                if (includeHidden == value)
                    return;

                includeHidden = value;
                OnPropertyChanged(nameof(IncludeHidden));
            }
        }

        private bool includeBinary = false;
        public bool IncludeBinary
        {
            get { return includeBinary; }
            set
            {
                if (includeBinary == value)
                    return;

                includeBinary = value;
                OnPropertyChanged(nameof(IncludeBinary));
            }
        }

        private bool useGitignore = false;
        public bool UseGitignore
        {
            get { return useGitignore; }
            set
            {
                if (useGitignore == value)
                    return;

                useGitignore = value;
                OnPropertyChanged(nameof(UseGitignore));
            }
        }

        private bool includeArchive = false;
        public bool IncludeArchive
        {
            get { return includeArchive; }
            set
            {
                if (includeArchive == value)
                    return;

                includeArchive = value;
                OnPropertyChanged(nameof(IncludeArchive));
            }
        }

        private bool followSymlinks = false;
        public bool FollowSymlinks
        {
            get { return followSymlinks; }
            set
            {
                if (followSymlinks == value)
                    return;

                followSymlinks = value;
                OnPropertyChanged(nameof(FollowSymlinks));
            }
        }

        private int codePage = -1;
        public int CodePage
        {
            get { return codePage; }
            set
            {
                if (codePage == value)
                    return;

                codePage = value;
                OnPropertyChanged(nameof(CodePage));

                EncodingIndex = (Encodings.Select((kv, Index) => new { kv.Value, Index })
                    .FirstOrDefault(a => a.Value == codePage) ?? new { Value = 0, Index = 0 }).Index;
            }
        }

        private int encodingIndex = 0;
        public int EncodingIndex
        {
            get { return encodingIndex; }
            set
            {
                if (encodingIndex == value || encodingIndex < 0 || encodingIndex > Encodings.Count - 1)
                    return;

                encodingIndex = value;
                OnPropertyChanged(nameof(EncodingIndex));
            }
        }

        private string pathReferences = string.Empty;
        public string PathReferences
        {
            get { return pathReferences; }
            set
            {
                if (value == pathReferences)
                    return;

                pathReferences = value;
                OnPropertyChanged(nameof(PathReferences));
            }
        }

        private bool isEverythingAvailable;
        public bool IsEverythingAvailable
        {
            get { return isEverythingAvailable; }
            set
            {
                if (value == isEverythingAvailable)
                    return;

                isEverythingAvailable = value;

                base.OnPropertyChanged(nameof(IsEverythingAvailable));
            }
        }

        private bool isGitInstalled;
        public bool IsGitInstalled
        {
            get { return isGitInstalled; }
            set
            {
                if (value == isGitInstalled)
                    return;

                isGitInstalled = value;

                base.OnPropertyChanged(nameof(IsGitInstalled));
            }
        }

        RelayCommand _saveCommand;
        /// <summary>
        /// Returns a command that copies files
        /// </summary>
        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand == null)
                {
                    _saveCommand = new RelayCommand(
                        param => { /*nothing to do here*/ },
                        param => CanSave()
                        );
                }
                return _saveCommand;
            }
        }

        private bool CanSave()
        {
            // if this bookmark matches another bookmark, disable save
            // when in edit mode, it may equal the original value
            var bmk = BookmarkLibrary.Instance.Find(this.ToBookmark());
            bool isUnique = bmk == null || ToBookmark() == _original;
            return isUnique;
        }

        public override bool Equals(object obj)
        {
            if (obj is BookmarkViewModel otherBookmark)
            {
                return this.Equals(otherBookmark);
            }
            return false;
        }

        public bool Equals(BookmarkViewModel otherVM)
        {
            if (otherVM is null)
                return false;

            return
                TypeOfFileSearch == otherVM.TypeOfFileSearch &&
                FilePattern == otherVM.FilePattern &&
                IgnoreFilePattern == otherVM.IgnoreFilePattern &&
                TypeOfSearch == otherVM.TypeOfSearch &&
                SearchFor == otherVM.SearchFor &&
                ReplaceWith == otherVM.ReplaceWith &&
                CaseSensitive == otherVM.CaseSensitive &&
                WholeWord == otherVM.WholeWord &&
                Multiline == otherVM.Multiline &&
                Singleline == otherVM.Singleline &&
                BooleanOperators == otherVM.BooleanOperators &&
                IncludeSubfolders == otherVM.IncludeSubfolders &&
                IncludeHidden == otherVM.IncludeHidden &&
                IncludeBinary == otherVM.IncludeBinary &&
                FollowSymlinks == otherVM.FollowSymlinks &&
                MaxSubfolderDepth == otherVM.MaxSubfolderDepth &&
                UseGitignore == otherVM.UseGitignore &&
                IncludeArchive == otherVM.IncludeArchive &&
                CodePage == otherVM.CodePage;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 13;
                hashCode = (hashCode * 17) ^ TypeOfFileSearch.GetHashCode();
                hashCode = (hashCode * 17) ^ FilePattern?.GetHashCode() ?? 5;
                hashCode = (hashCode * 17) ^ IgnoreFilePattern?.GetHashCode() ?? 5;
                hashCode = (hashCode * 17) ^ TypeOfSearch.GetHashCode();
                hashCode = (hashCode * 17) ^ SearchFor?.GetHashCode() ?? 5;
                hashCode = (hashCode * 17) ^ ReplaceWith?.GetHashCode() ?? 5;
                hashCode = (hashCode * 17) ^ CaseSensitive.GetHashCode();
                hashCode = (hashCode * 17) ^ WholeWord.GetHashCode();
                hashCode = (hashCode * 17) ^ Multiline.GetHashCode();
                hashCode = (hashCode * 17) ^ Singleline.GetHashCode();
                hashCode = (hashCode * 17) ^ BooleanOperators.GetHashCode();
                hashCode = (hashCode * 17) ^ IncludeSubfolders.GetHashCode();
                hashCode = (hashCode * 17) ^ IncludeHidden.GetHashCode();
                hashCode = (hashCode * 17) ^ IncludeBinary.GetHashCode();
                hashCode = (hashCode * 17) ^ MaxSubfolderDepth.GetHashCode();
                hashCode = (hashCode * 17) ^ UseGitignore.GetHashCode();
                hashCode = (hashCode * 17) ^ IncludeArchive.GetHashCode();
                hashCode = (hashCode * 17) ^ FollowSymlinks.GetHashCode();
                hashCode = (hashCode * 17) ^ CodePage.GetHashCode();
                return hashCode;
            }
        }

        public static bool Equals(BookmarkViewModel b1, BookmarkViewModel b2) => b1 is null ? b2 is null : b1.Equals(b2);

        public static bool operator ==(BookmarkViewModel b1, BookmarkViewModel b2) => Equals(b1, b2);
        public static bool operator !=(BookmarkViewModel b1, BookmarkViewModel b2) => !Equals(b1, b2);

        public override string ToString()
        {
            return $"{SearchFor} to {ReplaceWith} on {FilePattern} :: {Description}";
        }
    }
}
