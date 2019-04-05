using System;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using dnGREP.Common;

namespace dnGREP.WPF
{
    public class BookmarkListViewModel : ViewModelBase
    {
        readonly Action<string, string, string> ClearStar;

        public BookmarkListViewModel(Action<string, string, string> clearStar)
        {
            ClearStar = clearStar;

            var items = BookmarkLibrary.Instance.Bookmarks.Select(bk => new BookmarkViewModel(bk)).ToList();
            Bookmarks = new ListCollectionView(items)
            {
                Filter = BookmarkFilter
            };
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
                OnPropertyChanged("FilterText");

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
                OnPropertyChanged("SelectedBookmark");
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
                ClearStar(SelectedBookmark.SearchFor,
                          SelectedBookmark.ReplaceWith,
                          SelectedBookmark.FilePattern);

                Bookmark oldBookmark = new Bookmark(SelectedBookmark.SearchFor, SelectedBookmark.ReplaceWith,
                    SelectedBookmark.FilePattern, SelectedBookmark.Description);
                BookmarkLibrary.Instance.Bookmarks.Remove(oldBookmark);

                Bookmarks.Remove(SelectedBookmark);
            }
        }

        private void Edit()
        {
            if (SelectedBookmark != null)
            {
                var dlg = new BookmarkDetailWindow();

                // edit a copy
                dlg.DataContext = new BookmarkViewModel(SelectedBookmark);

                var result = dlg.ShowDialog();
                if (result.HasValue && result.Value)
                {

                }
            }
        }

        private void AddBookmark()
        {
            throw new NotImplementedException();
        }
    }

    public class BookmarkViewModel : ViewModelBase
    {
        public BookmarkViewModel(Bookmark bk)
        {
            description = bk.Description;
            filePattern = bk.FileNames;
            searchFor = bk.SearchPattern;
            replaceWith = bk.ReplacePattern;
        }

        public BookmarkViewModel(BookmarkViewModel toCopy)
        {
            description = toCopy.Description;
            filePattern = toCopy.FilePattern;
            searchFor = toCopy.SearchFor;
            replaceWith = toCopy.ReplaceWith;
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
                OnPropertyChanged("Description");
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
                OnPropertyChanged("FilePattern");
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
                OnPropertyChanged("SearchFor");
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
                OnPropertyChanged("ReplaceWith");
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is BookmarkViewModel other)
            {
                return Description == other.Description &&
                    FilePattern == other.FilePattern &&
                    SearchFor == other.SearchFor &&
                    ReplaceWith == other.ReplaceWith;
            }
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 13;
                hashCode = (hashCode * 397) ^ Description.GetHashCode();
                hashCode = (hashCode * 397) ^ FilePattern.GetHashCode();
                hashCode = (hashCode * 397) ^ SearchFor.GetHashCode();
                hashCode = (hashCode * 397) ^ ReplaceWith.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{SearchFor} to {ReplaceWith} on {FilePattern} :: {Description}";
        }
    }
}
