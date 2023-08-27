using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using dnGREP.Common;
using dnGREP.Everything;
using dnGREP.Localization;
using Resources = dnGREP.Localization.Properties.Resources;

namespace dnGREP.WPF
{
    public partial class BookmarkListViewModel : CultureAwareViewModel
    {
        public event EventHandler<DataEventArgs<int>>? SetFocus;

        private readonly Action<Bookmark> ClearStar;
        private readonly Window ownerWnd;
        private readonly List<BookmarkViewModel> _bookmarks;
        private bool _isDirty;

        public BookmarkListViewModel(Window owner, Action<Bookmark> clearStar)
        {
            ownerWnd = owner;
            ClearStar = clearStar;

            _bookmarks = new List<BookmarkViewModel>();
            SynchToLibrary();

            ApplicationFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ApplicationFontFamily);
            DialogFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.DialogFontSize);
            IsPinned = GrepSettings.Instance.Get<bool>(GrepSettings.Key.PinBookmarkWindow);
        }

        [MemberNotNull(nameof(Bookmarks))]
        public void SynchToLibrary()
        {
            _bookmarks.Clear();
            _bookmarks.AddRange(BookmarkLibrary.Instance.Bookmarks
                .OrderBy(bk => bk.Ordinal)
                .Select(bk => new BookmarkViewModel(bk)));
            Bookmarks = CollectionViewSource.GetDefaultView(_bookmarks);
            Bookmarks.Filter = BookmarkFilter;
        }

        public void Sort()
        {
            _bookmarks.Sort((x, y) => x.Ordinal.CompareTo(y.Ordinal));
        }

        internal void BookmarksWindow_Hiding()
        {
            GrepSettings.Instance.Set(GrepSettings.Key.PinBookmarkWindow, IsPinned);

            if (_isDirty)
            {
                foreach (BookmarkViewModel bk in _bookmarks)
                {
                    bk.PushOrdinalUpdate();
                }
                BookmarkLibrary.Instance.Sort();
                BookmarkLibrary.Save();
            }
        }

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
                    return bmk.BookmarkName.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                           bmk.Description.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                           bmk.SearchFor.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                           bmk.ReplaceWith.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                           bmk.FilePattern.Contains(FilterText, StringComparison.OrdinalIgnoreCase);
                }
            }
            return false;
        }

        public ICollectionView Bookmarks { get; private set; }

        [ObservableProperty]
        private string applicationFontFamily = SystemFonts.MessageFontFamily.Source;

        [ObservableProperty]
        private double dialogFontSize;

        [ObservableProperty]
        private bool isPinned = false;

        [ObservableProperty]
        private string filterText = string.Empty;
        partial void OnFilterTextChanged(string value)
        {
            Bookmarks.Refresh();
        }

        [ObservableProperty]
        private BookmarkViewModel? selectedBookmark = null;
        partial void OnSelectedBookmarkChanged(BookmarkViewModel? value)
        {
            HasSelection = value != null;
        }

        [ObservableProperty]
        private bool hasSelection = false;


        public ICommand AddCommand => new RelayCommand(
            param => AddBookmark());

        public ICommand EditCommand => new RelayCommand(
            param => Edit(),
            param => SelectedBookmark != null);

        public ICommand DuplicateCommand => new RelayCommand(
            param => Duplicate(),
            param => SelectedBookmark != null);

        public ICommand DeleteCommand => new RelayCommand(
            param => Delete(),
            param => SelectedBookmark != null);

        public ICommand MoveToTopCommand => new RelayCommand(
            p => MoveToTop(),
            q => SelectedBookmark != null && SelectedBookmark.Ordinal > 0);

        public ICommand MoveUpCommand => new RelayCommand(
            p => MoveUp(),
            q => SelectedBookmark != null && SelectedBookmark.Ordinal > 0);

        public ICommand MoveDownCommand => new RelayCommand(
            p => MoveDown(),
            q => SelectedBookmark != null && SelectedBookmark.Ordinal < _bookmarks.Count - 1);

        public ICommand MoveToBottomCommand => new RelayCommand(
            p => MoveToBottom(),
            q => SelectedBookmark != null && SelectedBookmark.Ordinal < _bookmarks.Count - 1);

        private void UpdateOrder()
        {
            _isDirty = true;
            Sort();
            Bookmarks.Refresh();
            if (SelectedBookmark != null)
            {
                int idx = _bookmarks.IndexOf(SelectedBookmark);
                SetFocus?.Invoke(this, new DataEventArgs<int>(idx));
            }
        }

        private void MoveToTop()
        {
            if (SelectedBookmark != null)
            {
                int idx = SelectedBookmark.Ordinal;
                if (idx > 0)
                {
                    SelectedBookmark.Ordinal = 0;
                    foreach (BookmarkViewModel item in Bookmarks)
                    {
                        if (item != SelectedBookmark && item.Ordinal < idx)
                        {
                            item.Ordinal++;
                        }
                    }
                    UpdateOrder();
                }
            }
        }

        private void MoveUp()
        {
            if (SelectedBookmark != null)
            {
                int idx = SelectedBookmark.Ordinal;
                if (idx > 0)
                {
                    var prev = _bookmarks.Where(b => b.Ordinal == idx - 1).First();
                    SelectedBookmark.Ordinal = prev.Ordinal;
                    prev.Ordinal = idx;
                    UpdateOrder();
                }
            }
        }

        private void MoveDown()
        {
            if (SelectedBookmark != null)
            {
                int idx = SelectedBookmark.Ordinal;
                if (idx < _bookmarks.Count - 1)
                {
                    var next = _bookmarks.Where(b => b.Ordinal == idx + 1).First();
                    SelectedBookmark.Ordinal = next.Ordinal;
                    next.Ordinal = idx;
                    UpdateOrder();
                }
            }
        }

        private void MoveToBottom()
        {
            if (SelectedBookmark != null)
            {
                int idx = SelectedBookmark.Ordinal;
                if (idx < _bookmarks.Count - 1)
                {
                    SelectedBookmark.Ordinal = _bookmarks.Count - 1;
                    foreach (BookmarkViewModel item in Bookmarks)
                    {
                        if (item != SelectedBookmark && item.Ordinal > idx)
                        {
                            item.Ordinal--;
                        }
                    }
                    UpdateOrder();
                }
            }
        }

        private void Delete()
        {
            if (SelectedBookmark != null)
            {
                ClearStar(SelectedBookmark.ToBookmark());

                var bmk = BookmarkLibrary.Instance.Get(SelectedBookmark.Id);
                if (bmk != null)
                {
                    _bookmarks.Remove(SelectedBookmark);
                    UpdateOrdinals();

                    BookmarkLibrary.Instance.Bookmarks.Remove(bmk);
                    foreach (BookmarkViewModel bk in _bookmarks)
                    {
                        bk.PushOrdinalUpdate();
                    }
                    BookmarkLibrary.Instance.Sort();
                    BookmarkLibrary.Save();
                    Bookmarks.Refresh();
                }
            }
        }

        private void UpdateOrdinals()
        {
            int idx = 0;
            foreach (BookmarkViewModel bmk in _bookmarks)
            {
                bmk.Ordinal = idx++;
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
                    editBmk.UpdateSectionIndex();
                    editBmk.SetExtendedProperties();

                    if (SelectedBookmark != editBmk)
                    {
                        ClearStar(SelectedBookmark.ToBookmark());
                    }

                    string id = SelectedBookmark.Id;
                    var bmk = BookmarkLibrary.Instance.Find(SelectedBookmark.ToBookmark());
                    if (bmk != null)
                    {
                        BookmarkLibrary.Instance.Bookmarks.Remove(bmk);

                        _bookmarks.Remove(SelectedBookmark);
                        _bookmarks.Add(editBmk);
                        SelectedBookmark = null; // two steps to change selected because of bookmark equality
                        SelectedBookmark = editBmk;
                        UpdateOrder();
                    }

                    var newBmk = new Bookmark(id)
                    {
                        BookmarkName = editBmk.BookmarkName,
                        Ordinal = editBmk.Ordinal,
                        Description = editBmk.Description,
                        TypeOfFileSearch = editBmk.TypeOfFileSearch,
                        FileNames = editBmk.FilePattern,
                        IgnoreFilePattern = editBmk.IgnoreFilePattern,
                        TypeOfSearch = editBmk.TypeOfSearch,
                        SearchPattern = editBmk.SearchFor,
                        ReplacePattern = editBmk.ReplaceWith,
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
                        IgnoreFilterName = editBmk.IgnoreFilterName,
                        SkipRemoteCloudStorageFiles = editBmk.SkipRemoteCloudStorageFiles,
                        IncludeArchive = editBmk.IncludeArchive,
                        FollowSymlinks = editBmk.FollowSymlinks,
                        CodePage = editBmk.CodePage,
                        ApplyFileSourceFilters = editBmk.ApplyFileSourceFilters,
                        ApplyFilePropertyFilters = editBmk.ApplyFilePropertyFilters,
                        ApplyContentSearchFilters = editBmk.ApplyContentSearchFilters,
                    };
                    string[] paths = editBmk.PathReferences.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    newBmk.FolderReferences.AddRange(paths);

                    BookmarkLibrary.Instance.Bookmarks.Add(newBmk);
                    BookmarkLibrary.Instance.Sort();
                    BookmarkLibrary.Save();
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
                    duplicateBmk.UpdateSectionIndex();
                    duplicateBmk.SetExtendedProperties();
                    duplicateBmk.Ordinal = BookmarkLibrary.Instance.Bookmarks.Count;

                    _bookmarks.Add(duplicateBmk);
                    SelectedBookmark = null; // two steps to change selected because of bookmark equality
                    SelectedBookmark = duplicateBmk;
                    Bookmarks.MoveCurrentToLast();
                    UpdateOrder();

                    var newBmk = new Bookmark()
                    {
                        BookmarkName = duplicateBmk.BookmarkName,
                        Ordinal = duplicateBmk.Ordinal,
                        Description = duplicateBmk.Description,
                        TypeOfFileSearch = duplicateBmk.TypeOfFileSearch,
                        FileNames = duplicateBmk.FilePattern,
                        IgnoreFilePattern = duplicateBmk.IgnoreFilePattern,
                        TypeOfSearch = duplicateBmk.TypeOfSearch,
                        SearchPattern = duplicateBmk.SearchFor,
                        ReplacePattern = duplicateBmk.ReplaceWith,
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
                        IgnoreFilterName = duplicateBmk.IgnoreFilterName,
                        SkipRemoteCloudStorageFiles = duplicateBmk.SkipRemoteCloudStorageFiles,
                        IncludeArchive = duplicateBmk.IncludeArchive,
                        FollowSymlinks = duplicateBmk.FollowSymlinks,
                        CodePage = duplicateBmk.CodePage,
                        ApplyFileSourceFilters = duplicateBmk.ApplyFileSourceFilters,
                        ApplyFilePropertyFilters = duplicateBmk.ApplyFilePropertyFilters,
                        ApplyContentSearchFilters = duplicateBmk.ApplyContentSearchFilters,
                    };

                    duplicateBmk.Id = newBmk.Id;

                    BookmarkLibrary.Instance.Bookmarks.Add(newBmk);
                    BookmarkLibrary.Instance.Sort();
                    BookmarkLibrary.Save();
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
                editBmk.UpdateSectionIndex();
                editBmk.SetExtendedProperties();
                editBmk.Ordinal = BookmarkLibrary.Instance.Bookmarks.Count;

                _bookmarks.Add(editBmk);
                SelectedBookmark = editBmk;
                Bookmarks.MoveCurrentToLast();
                UpdateOrder();

                var newBmk = new Bookmark()
                {
                    BookmarkName = editBmk.BookmarkName,
                    Ordinal = editBmk.Ordinal,
                    Description = editBmk.Description,
                    TypeOfFileSearch = editBmk.TypeOfFileSearch,
                    FileNames = editBmk.FilePattern,
                    IgnoreFilePattern = editBmk.IgnoreFilePattern,
                    TypeOfSearch = editBmk.TypeOfSearch,
                    SearchPattern = editBmk.SearchFor,
                    ReplacePattern = editBmk.ReplaceWith,
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
                    IgnoreFilterName = editBmk.IgnoreFilterName,
                    SkipRemoteCloudStorageFiles = editBmk.SkipRemoteCloudStorageFiles,
                    IncludeArchive = editBmk.IncludeArchive,
                    FollowSymlinks = editBmk.FollowSymlinks,
                    CodePage = editBmk.CodePage,
                    ApplyFileSourceFilters = editBmk.ApplyFileSourceFilters,
                    ApplyFilePropertyFilters = editBmk.ApplyFilePropertyFilters,
                    ApplyContentSearchFilters = editBmk.ApplyContentSearchFilters,
                };
                string[] paths = editBmk.PathReferences.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                newBmk.FolderReferences.AddRange(paths);

                editBmk.Id = newBmk.Id;

                BookmarkLibrary.Instance.Bookmarks.Add(newBmk);
                BookmarkLibrary.Instance.Sort();
                BookmarkLibrary.Save();
            }
        }
    }

    public partial class BookmarkViewModel : CultureAwareViewModel
    {
        public static ObservableCollection<KeyValuePair<string, int>> Encodings { get; } = new();

        private Bookmark? _original;

        public void SetEditMode(Bookmark original)
        {
            _original = original;
        }

        public BookmarkViewModel(Bookmark bk)
        {
            IsEverythingAvailable = EverythingSearch.IsAvailable;
            IsGitInstalled = Utils.IsGitInstalled;

            Id = bk.Id;
            BookmarkName = bk.BookmarkName;
            Ordinal = bk.Ordinal;

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
            IgnoreFilterName = bk.IgnoreFilterName;
            SkipRemoteCloudStorageFiles = bk.SkipRemoteCloudStorageFiles;
            IncludeArchive = bk.IncludeArchive;
            FollowSymlinks = bk.FollowSymlinks;
            CodePage = bk.CodePage;
            PathReferences = string.Join(Environment.NewLine, bk.FolderReferences);

            ApplyFileSourceFilters = bk.ApplyFileSourceFilters;
            ApplyFilePropertyFilters = bk.ApplyFilePropertyFilters;
            ApplyContentSearchFilters = bk.ApplyContentSearchFilters;

            UpdateSectionIndex();

            UpdateTypeOfSearchState();

            SetExtendedProperties();

            PopulateIgnoreFilters();

            ApplicationFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ApplicationFontFamily);
            DialogFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.DialogFontSize);
        }

        public BookmarkViewModel(BookmarkViewModel toCopy)
        {
            IsEverythingAvailable = EverythingSearch.IsAvailable;
            IsGitInstalled = Utils.IsGitInstalled;

            Id = toCopy.Id;
            BookmarkName = toCopy.BookmarkName;
            Ordinal = toCopy.Ordinal;

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
            IgnoreFilterName = toCopy.IgnoreFilterName;
            SkipRemoteCloudStorageFiles = toCopy.SkipRemoteCloudStorageFiles;
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

            ApplyFileSourceFilters = toCopy.ApplyFileSourceFilters;
            ApplyFilePropertyFilters = toCopy.ApplyFilePropertyFilters;
            ApplyContentSearchFilters = toCopy.ApplyContentSearchFilters;

            UpdateSectionIndex();
            SetExtendedProperties();

            PopulateIgnoreFilters();

            ApplicationFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ApplicationFontFamily);
            DialogFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.DialogFontSize);
        }

        internal void PushOrdinalUpdate()
        {
            Bookmark? bk = BookmarkLibrary.Instance.Get(Id);
            if (bk != null)
            {
                bk.Ordinal = Ordinal;
            }
        }

        internal void SetExtendedProperties()
        {
            var tempList = new List<string>();

            if (ApplyFileSourceFilters)
            {
                if (IncludeArchive)
                    tempList.Add(Resources.Bookmarks_Summary_SearchInArchives);
            }

            if (ApplyFilePropertyFilters)
            {
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
            }

            if (ApplyContentSearchFilters)
            {
                if (CaseSensitive)
                    tempList.Add(Resources.Bookmarks_Summary_CaseSensitive);
                if (WholeWord)
                    tempList.Add(Resources.Bookmarks_Summary_WholeWord);
                if (Multiline)
                    tempList.Add(Resources.Bookmarks_Summary_Multiline);
            }

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
            return new Bookmark(Id)
            {
                BookmarkName = BookmarkName,
                Ordinal = Ordinal,
                Description = Description,
                FileNames = FilePattern,
                IgnoreFilePattern = IgnoreFilePattern,
                TypeOfFileSearch = TypeOfFileSearch,
                TypeOfSearch = TypeOfSearch,
                SearchPattern = SearchFor,
                ReplacePattern = ReplaceWith,
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
                IgnoreFilterName = IgnoreFilterName,
                SkipRemoteCloudStorageFiles = SkipRemoteCloudStorageFiles,
                IncludeArchive = IncludeArchive,
                FollowSymlinks = FollowSymlinks,
                CodePage = CodePage,
                ApplyFileSourceFilters = ApplyFileSourceFilters,
                ApplyFilePropertyFilters = ApplyFilePropertyFilters,
                ApplyContentSearchFilters = ApplyContentSearchFilters,
                FolderReferences = PathReferences.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).ToList()
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

        private void PopulateIgnoreFilters()
        {
            var selectedFilter = IgnoreFilterName;

            if (IgnoreFilterList.Count == 0)
            {
                IgnoreFilterList.Add(string.Empty);
            }
            else
            {
                IgnoreFilterName = string.Empty;
                // do not empty the list: the IgnoreFilterName will be set to null
                while (IgnoreFilterList.Count > 1)
                {
                    IgnoreFilterList.RemoveAt(IgnoreFilterList.Count - 1);
                }
            }

            string dataFolder = Path.Combine(Utils.GetDataFolderPath(), MainViewModel.IgnoreFilterFolder);
            if (!Directory.Exists(dataFolder))
            {
                Directory.CreateDirectory(dataFolder);
            }

            HashSet<string> names = new();
            foreach (string fileName in Directory.GetFiles(dataFolder, "*.ignore", SearchOption.AllDirectories))
            {
                string name = Path.GetFileNameWithoutExtension(fileName);

                if (!names.Contains(name))
                {
                    IgnoreFilterList.Add(name);
                }
            }

            if (!string.IsNullOrEmpty(selectedFilter))
            {
                var filter = IgnoreFilterList.FirstOrDefault(f => f.Equals(selectedFilter, StringComparison.OrdinalIgnoreCase));
                if (filter != null)
                {
                    IgnoreFilterName = filter;
                }
            }
        }

        #region Properties

        public string Id { get; set; }

        [ObservableProperty]
        private string bookmarkName = string.Empty;

        [ObservableProperty]
        private int ordinal = 0;

        [ObservableProperty]
        private string applicationFontFamily = SystemFonts.MessageFontFamily.Source;

        [ObservableProperty]
        private double dialogFontSize;

        [ObservableProperty]
        private string extendedProperties = string.Empty;

        [ObservableProperty]
        private string description = string.Empty;

        [ObservableProperty]
        private string filePattern = string.Empty;

        [ObservableProperty]
        private string searchFor = string.Empty;

        [ObservableProperty]
        private string replaceWith = string.Empty;

        [ObservableProperty]
        private string ignoreFilePattern = string.Empty;

        [ObservableProperty]
        private FileSearchType typeOfFileSearch = FileSearchType.Asterisk;

        [ObservableProperty]
        private SearchType typeOfSearch = SearchType.PlainText;
        partial void OnTypeOfFileSearchChanged(FileSearchType value)
        {
            UpdateTypeOfSearchState();
        }

        [ObservableProperty]
        private bool caseSensitive = false;

        [ObservableProperty]
        private bool isCaseSensitiveEnabled = false;

        [ObservableProperty]
        private bool wholeWord = false;

        [ObservableProperty]
        private bool isWholeWordEnabled = false;

        [ObservableProperty]
        private bool multiline = false;

        [ObservableProperty]
        private bool isMultilineEnabled = false;

        [ObservableProperty]
        private bool singleline = false;

        [ObservableProperty]
        private bool isSinglelineEnabled = false;

        [ObservableProperty]
        private bool booleanOperators = false;

        [ObservableProperty]
        private bool isBooleanOperatorsEnabled = false;

        [ObservableProperty]
        private bool includeSubfolders = true;
        partial void OnIncludeSubfoldersChanged(bool value)
        {
            if (!value)
            {
                MaxSubfolderDepth = -1;
            }
        }

        [ObservableProperty]
        private int maxSubfolderDepth = -1;

        [ObservableProperty]
        private bool includeHidden = false;

        [ObservableProperty]
        private bool includeBinary = false;

        [ObservableProperty]
        private bool useGitignore = false;

        public ObservableCollection<string> IgnoreFilterList { get; } = new();

        [ObservableProperty]
        private string ignoreFilterName = string.Empty;

        [ObservableProperty]
        private bool skipRemoteCloudStorageFiles = true;

        [ObservableProperty]
        private bool includeArchive = false;

        [ObservableProperty]
        private bool followSymlinks = false;

        [ObservableProperty]
        private int codePage = -1;
        partial void OnCodePageChanged(int value)
        {
            int index = (Encodings.Select((kv, Index) => new { kv.Value, Index })
                .FirstOrDefault(a => a.Value == value) ?? new { Value = 0, Index = 0 }).Index;
            if (index >= 0 && index < Encodings.Count)
            {
                EncodingIndex = index;
            }
        }

        [ObservableProperty]
        private int encodingIndex = 0;

        [ObservableProperty]
        private string pathReferences = string.Empty;

        [ObservableProperty]
        private bool isEverythingAvailable;

        [ObservableProperty]
        private bool isGitInstalled;

        [ObservableProperty]
        private bool applyFileSourceFilters = true;
        partial void OnApplyFileSourceFiltersChanged(bool value)
        {
            if (!value)
            {
                FilePattern = string.Empty;
                IgnoreFilePattern = string.Empty;
            }
            UpdateSectionIndex();
        }

        [ObservableProperty]
        private bool applyFilePropertyFilters = true;
        partial void OnApplyFilePropertyFiltersChanged(bool value)
        {
            UpdateSectionIndex();
        }

        [ObservableProperty]
        private bool applyContentSearchFilters = true;
        partial void OnApplyContentSearchFiltersChanged(bool value)
        {
            if (!value)
            {
                SearchFor = string.Empty;
                ReplaceWith = string.Empty;
            }
            UpdateSectionIndex();
        }

        [ObservableProperty]
        private int sectionIndex = 0;

        #endregion

        internal void UpdateSectionIndex()
        {
            int value = 0;
            if (ApplyFileSourceFilters)
                value += 1;
            if (ApplyFilePropertyFilters)
                value += 2;
            if (ApplyContentSearchFilters)
                value += 4;
            SectionIndex = value;
        }

        public ICommand FilterComboBoxDropDownCommand => new RelayCommand(
            p => PopulateIgnoreFilters());

        /// <summary>
        /// Returns a command that checks for can save
        /// </summary>
        public ICommand SaveCommand => new RelayCommand(
            param => { /*nothing to do here*/ },
            param => CanSave());

        private bool CanSave()
        {
            // if this bookmark matches another bookmark, disable save
            // when in edit mode, it may equal the original value
            var current = ToBookmark();
            var bmk = BookmarkLibrary.Instance.Find(current);
            bool isUnique = bmk == null || bmk != current ||
                current.Equals(_original);
            return isUnique;
        }

        public override bool Equals(object? obj)
        {
            if (obj is BookmarkViewModel otherBookmark)
            {
                return this.Equals(otherBookmark);
            }
            return false;
        }

        public bool Equals(BookmarkViewModel? otherVM)
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
                IgnoreFilterName == otherVM.IgnoreFilterName &&
                SkipRemoteCloudStorageFiles == otherVM.SkipRemoteCloudStorageFiles &&
                IncludeArchive == otherVM.IncludeArchive &&
                CodePage == otherVM.CodePage &&
                ApplyFileSourceFilters == otherVM.ApplyFileSourceFilters &&
                ApplyFilePropertyFilters == otherVM.ApplyFilePropertyFilters &&
                ApplyContentSearchFilters == otherVM.ApplyContentSearchFilters;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 13;
                hashCode = (hashCode * 17) ^ TypeOfFileSearch.GetHashCode();
                hashCode = (hashCode * 17) ^ FilePattern?.GetHashCode(StringComparison.Ordinal) ?? 5;
                hashCode = (hashCode * 17) ^ IgnoreFilePattern?.GetHashCode(StringComparison.Ordinal) ?? 5;
                hashCode = (hashCode * 17) ^ TypeOfSearch.GetHashCode();
                hashCode = (hashCode * 17) ^ SearchFor?.GetHashCode(StringComparison.Ordinal) ?? 5;
                hashCode = (hashCode * 17) ^ ReplaceWith?.GetHashCode(StringComparison.Ordinal) ?? 5;
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
                hashCode = (hashCode * 17) ^ IgnoreFilterName.GetHashCode(StringComparison.Ordinal);
                hashCode = (hashCode * 17) ^ SkipRemoteCloudStorageFiles.GetHashCode();
                hashCode = (hashCode * 17) ^ IncludeArchive.GetHashCode();
                hashCode = (hashCode * 17) ^ FollowSymlinks.GetHashCode();
                hashCode = (hashCode * 17) ^ CodePage.GetHashCode();
                hashCode = (hashCode * 17) ^ ApplyFileSourceFilters.GetHashCode();
                hashCode = (hashCode * 17) ^ ApplyFilePropertyFilters.GetHashCode();
                hashCode = (hashCode * 17) ^ ApplyContentSearchFilters.GetHashCode();
                return hashCode;
            }
        }

        public static bool Equals(BookmarkViewModel? b1, BookmarkViewModel? b2) => b1 is null ? b2 is null : b1.Equals(b2);

        public static bool operator ==(BookmarkViewModel? b1, BookmarkViewModel? b2) => Equals(b1, b2);
        public static bool operator !=(BookmarkViewModel? b1, BookmarkViewModel? b2) => !Equals(b1, b2);

        public override string ToString()
        {
            return $"{Ordinal} {BookmarkName} {SearchFor} to {ReplaceWith} on {FilePattern} :: {Description}";
        }
    }
}
