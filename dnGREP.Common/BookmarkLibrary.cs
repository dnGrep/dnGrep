using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using NLog;

namespace dnGREP.Common
{
    public class BookmarkLibrary
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private static BookmarkEntity? bookmarks;

        public static readonly int LatestVersion = 6;

        public static bool IsDeserializing { get; private set; } = false;

        public static BookmarkEntity Instance
        {
            get
            {
                if (bookmarks == null)
                    Load();
                return bookmarks;
            }
        }

        private BookmarkLibrary() { }

        private static string BookmarksFile
        {
            get { return Path.Combine(Utils.GetDataFolderPath(), "bookmarks.xml"); }
        }

        [MemberNotNull(nameof(bookmarks))]
        public static void Load()
        {
            try
            {
                IsDeserializing = true;
                BookmarkEntity? bookmarkLib;
                if (!File.Exists(BookmarksFile))
                {
                    bookmarks = new BookmarkEntity();
                }
                else
                {
                    using TextReader reader = new StreamReader(BookmarksFile);
                    XmlSerializer serializer = new(typeof(BookmarkEntity));
                    bookmarkLib = (BookmarkEntity?)serializer.Deserialize(reader);
                    if (bookmarkLib != null)
                    {
                        bookmarkLib.Initialize();
                        bookmarks = bookmarkLib;
                    }
                    else
                    {
                        bookmarks = new BookmarkEntity();
                    }
                }
            }
            catch
            {
                bookmarks = new BookmarkEntity();
            }
            finally
            {
                IsDeserializing = false;
            }
        }

        public static void Save()
        {
            try
            {
                XmlSerializer serializer = new(typeof(BookmarkEntity));
                using TextWriter writer = new StreamWriter(BookmarksFile);
                serializer.Serialize(writer, bookmarks);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to save bookmarks");
            }
        }
    }

    [Serializable]
    public class BookmarkEntity
    {
        public List<Bookmark> Bookmarks { get; private set; } = new();

        internal void Initialize()
        {
            if (BookmarkLibrary.IsDeserializing)
            {
                foreach (var bk in Bookmarks)
                {
                    if (string.IsNullOrEmpty(bk.Id))
                    {
                        bk.Id = Guid.NewGuid().ToString();
                    }
                }
                UpdateOrdinals();
            }
        }

        public Bookmark? Get(string id)
        {
            return Bookmarks.FirstOrDefault(b => b.Id == id);
        }

        public Bookmark? Find(Bookmark bookmark)
        {
            if (!Bookmarks.Any()) return null;

            Bookmark? item = null;

            item = Bookmarks.FirstOrDefault(bk => bk.Equals(bookmark));
            if (item != null) return item;

            item = Bookmarks.FirstOrDefault(bk => bk.ApplyFileSourceFilters && bk.FileSourceEquals(bookmark) &&
                bk.ApplyFilePropertyFilters && bk.FilePropertiesEquals(bookmark) &&
                !bk.ApplyContentSearchFilters);
            if (item != null) return item;

            item = Bookmarks.FirstOrDefault(bk => bk.ApplyFileSourceFilters && bk.FileSourceEquals(bookmark) &&
                bk.ApplyContentSearchFilters && bk.ContentSearchEquals(bookmark) &&
                !bk.ApplyFilePropertyFilters);
            if (item != null) return item;

            item = Bookmarks.FirstOrDefault(bk => bk.ApplyFilePropertyFilters && bk.FilePropertiesEquals(bookmark) &&
                bk.ApplyContentSearchFilters && bk.ContentSearchEquals(bookmark) &&
                !bk.ApplyFileSourceFilters);
            if (item != null) return item;

            item = Bookmarks.FirstOrDefault(bk => bk.ApplyFileSourceFilters && bk.FileSourceEquals(bookmark) &&
                !bk.ApplyFilePropertyFilters &&
                !bk.ApplyContentSearchFilters);
            if (item != null) return item;

            item = Bookmarks.FirstOrDefault(bk => bk.ApplyFilePropertyFilters && bk.FilePropertiesEquals(bookmark) &&
                !bk.ApplyFileSourceFilters &&
                !bk.ApplyContentSearchFilters);
            if (item != null) return item;

            item = Bookmarks.FirstOrDefault(bk => bk.ApplyContentSearchFilters && bk.ContentSearchEquals(bookmark) &&
                !bk.ApplyFileSourceFilters &&
                !bk.ApplyFilePropertyFilters);

            return item;
        }

        public void AddFolderReference(Bookmark bookmark, string folder)
        {
            var oldRefs = Bookmarks.Where(b => b.FolderReferences.Contains(folder)).ToArray();
            foreach (Bookmark bk in oldRefs)
            {
                bk.FolderReferences.Remove(folder);
            }
            bookmark.FolderReferences.Add(folder);
        }

        public void UpdateOrdinals()
        {
            int idx = 0;
            foreach (Bookmark bookmark in Bookmarks)
            {
                bookmark.Ordinal = idx++;
            }
        }

        public void Sort()
        {
            Bookmarks.Sort((x, y) => x.Ordinal.CompareTo(y.Ordinal));
        }

        public BookmarkEntity() { }
    }

    [Serializable]
    public class Bookmark
    {
        private string _id = string.Empty;

        public Bookmark()
        {
            if (!BookmarkLibrary.IsDeserializing)
            {
                _id = Guid.NewGuid().ToString();
            }
        }

        public Bookmark(string id)
        {
            _id = id;
        }

        [XmlIgnore]
        public int Ordinal { get; set; }

        public string Id
        {
            get { return _id; }
            // Setter is public only for XmlSerialization
            set
            {
                if (!BookmarkLibrary.IsDeserializing)
                    throw new InvalidOperationException("Setter is public only for XmlSerialization");

                _id = value;
            }
        }

        public int Version { get; set; } = BookmarkLibrary.LatestVersion;

        public string BookmarkName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public SearchType TypeOfSearch { get; set; } = SearchType.PlainText;
        public string SearchPattern { get; set; } = string.Empty;
        public string ReplacePattern { get; set; } = string.Empty;
        public bool CaseSensitive { get; set; }
        public bool WholeWord { get; set; }
        public bool Multiline { get; set; }
        public bool Singleline { get; set; }
        public bool BooleanOperators { get; set; }

        public FileSearchType TypeOfFileSearch { get; set; } = FileSearchType.Asterisk;
        public string FileNames { get; set; } = string.Empty;
        public string IgnoreFilePattern { get; set; } = string.Empty;
        public bool IncludeSubfolders { get; set; }
        public bool IncludeHiddenFiles { get; set; }
        public bool IncludeBinaryFiles { get; set; }
        public int MaxSubfolderDepth { get; set; } = -1;
        public bool UseGitignore { get; set; }
        public string IgnoreFilterName { get; set; } = string.Empty;
        public bool SkipRemoteCloudStorageFiles { get; set; } = true;
        public bool IncludeArchive { get; set; }
        public bool FollowSymlinks { get; set; }
        public int CodePage { get; set; } = -1;
        public List<string> FolderReferences { get; set; } = new();
        public bool ApplyFileSourceFilters { get; set; } = true;
        public bool ApplyFilePropertyFilters { get; set; } = true;
        public bool ApplyContentSearchFilters { get; set; } = true;


        // do not write v2 properties if the user hasn't updated the bookmark
        public bool ShouldSerializeTypeOfFileSearch() { return Version > 1; }
        public bool ShouldSerializeIgnoreFilePattern() { return Version > 1; }
        public bool ShouldSerializeIncludeSubfolders() { return Version > 1; }
        public bool ShouldSerializeIncludeHiddenFiles() { return Version > 1; }
        public bool ShouldSerializeIncludeBinaryFiles() { return Version > 1; }
        public bool ShouldSerializeTypeOfSearch() { return Version > 1; }
        public bool ShouldSerializeCaseSensitive() { return Version > 1; }
        public bool ShouldSerializeWholeWord() { return Version > 1; }
        public bool ShouldSerializeMultiline() { return Version > 1; }
        public bool ShouldSerializeSingleline() { return Version > 1; }
        public bool ShouldSerializeBooleanOperators() { return Version > 1; }
        public bool ShouldSerializeMaxSubfolderDepth() { return Version > 1; }
        public bool ShouldSerializeUseGitignore() { return Version > 1; }
        public bool ShouldSerializeIncludeArchive() { return Version > 1; }
        public bool ShouldSerializeFollowSymlinks() { return Version > 1; }
        public bool ShouldSerializeCodePage() { return Version > 1; }
        public bool ShouldSerializeFolderReferences() { return Version > 1; }
        public bool ShouldSerializeApplyFileSourceFilters() { return Version > 2; }
        public bool ShouldSerializeApplyFilePropertyFilters() { return Version > 2; }
        public bool ShouldSerializeApplyContentSearchFilters() { return Version > 2; }
        public bool ShouldSerializeApplySearchFilters() { return Version > 2; }
        public bool ShouldSerializeSkipRemoteCloudStorageFiles() { return Version > 3; }
        public bool ShouldSerializeIgnoreFilterName() { return Version > 5; }

        public override bool Equals(object? obj)
        {
            if (obj is Bookmark otherBookmark)
            {
                return Equals(otherBookmark);
            }
            return false;
        }

        public bool FileSourceEquals(Bookmark otherBookmark)
        {
            return TypeOfFileSearch == otherBookmark.TypeOfFileSearch &&
                FileNames == otherBookmark.FileNames &&
                IgnoreFilePattern == otherBookmark.IgnoreFilePattern &&
                IncludeArchive == otherBookmark.IncludeArchive &&
                UseGitignore == otherBookmark.UseGitignore &&
                IgnoreFilterName == otherBookmark.IgnoreFilterName &&
                SkipRemoteCloudStorageFiles == otherBookmark.SkipRemoteCloudStorageFiles &&
                CodePage == otherBookmark.CodePage;
        }

        public bool FilePropertiesEquals(Bookmark otherBookmark)
        {
            return IncludeSubfolders == otherBookmark.IncludeSubfolders &&
                MaxSubfolderDepth == otherBookmark.MaxSubfolderDepth &&
                IncludeHiddenFiles == otherBookmark.IncludeHiddenFiles &&
                IncludeBinaryFiles == otherBookmark.IncludeBinaryFiles &&
                FollowSymlinks == otherBookmark.FollowSymlinks;
        }

        public bool ContentSearchEquals(Bookmark otherBookmark)
        {
            return TypeOfSearch == otherBookmark.TypeOfSearch &&
                    SearchPattern == otherBookmark.SearchPattern &&
                    ReplacePattern == otherBookmark.ReplacePattern &&
                    CaseSensitive == otherBookmark.CaseSensitive &&
                    WholeWord == otherBookmark.WholeWord &&
                    Multiline == otherBookmark.Multiline &&
                    Singleline == otherBookmark.Singleline &&
                    BooleanOperators == otherBookmark.BooleanOperators;
        }

        public bool Equals(Bookmark? otherBookmark)
        {
            if (otherBookmark is null)
                return false;

            // equality is used to determine if two different bookmarks are the same
            // so Id, BookmarkName, Description and Ordinal are not part of equality

            return TypeOfFileSearch == otherBookmark.TypeOfFileSearch &&
                FileNames == otherBookmark.FileNames &&
                IgnoreFilePattern == otherBookmark.IgnoreFilePattern &&
                UseGitignore == otherBookmark.UseGitignore &&
                IgnoreFilterName == otherBookmark.IgnoreFilterName &&
                SkipRemoteCloudStorageFiles == otherBookmark.SkipRemoteCloudStorageFiles &&
                IncludeArchive == otherBookmark.IncludeArchive &&
                CodePage == otherBookmark.CodePage &&

                IncludeSubfolders == otherBookmark.IncludeSubfolders &&
                MaxSubfolderDepth == otherBookmark.MaxSubfolderDepth &&
                IncludeHiddenFiles == otherBookmark.IncludeHiddenFiles &&
                IncludeBinaryFiles == otherBookmark.IncludeBinaryFiles &&
                FollowSymlinks == otherBookmark.FollowSymlinks &&

                TypeOfSearch == otherBookmark.TypeOfSearch &&
                SearchPattern == otherBookmark.SearchPattern &&
                ReplacePattern == otherBookmark.ReplacePattern &&
                CaseSensitive == otherBookmark.CaseSensitive &&
                WholeWord == otherBookmark.WholeWord &&
                Multiline == otherBookmark.Multiline &&
                Singleline == otherBookmark.Singleline &&
                BooleanOperators == otherBookmark.BooleanOperators &&

                ApplyFileSourceFilters == otherBookmark.ApplyFileSourceFilters &&
                ApplyFilePropertyFilters == otherBookmark.ApplyFilePropertyFilters &&
                ApplyContentSearchFilters == otherBookmark.ApplyContentSearchFilters;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 13;
                hashCode = (hashCode * 17) ^ TypeOfFileSearch.GetHashCode();
                hashCode = (hashCode * 17) ^ FileNames?.GetHashCode(StringComparison.Ordinal) ?? 5;
                hashCode = (hashCode * 17) ^ IgnoreFilePattern?.GetHashCode(StringComparison.Ordinal) ?? 5;
                hashCode = (hashCode * 17) ^ UseGitignore.GetHashCode();
                hashCode = (hashCode * 17) ^ IgnoreFilterName.GetHashCode(StringComparison.Ordinal);
                hashCode = (hashCode * 17) ^ SkipRemoteCloudStorageFiles.GetHashCode();
                hashCode = (hashCode * 17) ^ IncludeArchive.GetHashCode();
                hashCode = (hashCode * 17) ^ CodePage.GetHashCode();

                hashCode = (hashCode * 17) ^ IncludeSubfolders.GetHashCode();
                hashCode = (hashCode * 17) ^ MaxSubfolderDepth.GetHashCode();
                hashCode = (hashCode * 17) ^ IncludeHiddenFiles.GetHashCode();
                hashCode = (hashCode * 17) ^ IncludeBinaryFiles.GetHashCode();
                hashCode = (hashCode * 17) ^ FollowSymlinks.GetHashCode();

                hashCode = (hashCode * 17) ^ TypeOfSearch.GetHashCode();
                hashCode = (hashCode * 17) ^ SearchPattern?.GetHashCode(StringComparison.Ordinal) ?? 5;
                hashCode = (hashCode * 17) ^ ReplacePattern?.GetHashCode(StringComparison.Ordinal) ?? 5;
                hashCode = (hashCode * 17) ^ CaseSensitive.GetHashCode();
                hashCode = (hashCode * 17) ^ WholeWord.GetHashCode();
                hashCode = (hashCode * 17) ^ Multiline.GetHashCode();
                hashCode = (hashCode * 17) ^ Singleline.GetHashCode();
                hashCode = (hashCode * 17) ^ BooleanOperators.GetHashCode();

                hashCode = (hashCode * 17) ^ ApplyFileSourceFilters.GetHashCode();
                hashCode = (hashCode * 17) ^ ApplyFilePropertyFilters.GetHashCode();
                hashCode = (hashCode * 17) ^ ApplyContentSearchFilters.GetHashCode();

                return hashCode;
            }
        }

        public static bool Equals(Bookmark? b1, Bookmark? b2) => b1 is null ? b2 is null : b1.Equals(b2);

        public static bool operator ==(Bookmark? b1, Bookmark? b2) => Equals(b1, b2);
        public static bool operator !=(Bookmark? b1, Bookmark? b2) => !Equals(b1, b2);

        public override string ToString()
        {
            return $"{Ordinal} {BookmarkName} {SearchPattern} to {ReplacePattern} on {FileNames} :: {Description}";
        }
    }
}
