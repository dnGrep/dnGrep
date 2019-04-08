using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using NLog;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace dnGREP.Common
{
    public class BookmarkLibrary
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private static BookmarkEntity bookmarks;

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

        public static void Load()
        {
            try
            {
                BookmarkEntity bookmarkLib;
                XmlSerializer serializer = new XmlSerializer(typeof(BookmarkEntity));
                if (!File.Exists(BookmarksFile))
                {
                    bookmarks = new BookmarkEntity();
                }
                else
                {
                    using (TextReader reader = new StreamReader(BookmarksFile))
                    {
                        bookmarkLib = (BookmarkEntity)serializer.Deserialize(reader);
                        bookmarks = bookmarkLib;
                    }
                }
            }
            catch
            {
                bookmarks = new BookmarkEntity();
            }
        }

        public static void Save()
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(BookmarkEntity));
                using (TextWriter writer = new StreamWriter(BookmarksFile))
                {
                    serializer.Serialize(writer, bookmarks);
                }
            }
            catch (Exception ex)
            {
                logger.Log<Exception>(LogLevel.Error, ex.Message, ex);
            }
        }
    }

    [Serializable]
    public class BookmarkEntity
    {
        public List<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();

        public BookmarkEntity() { }
    }

    [Serializable]
    public class Bookmark
    {
        public Bookmark() { }
        public Bookmark(string searchFor, string replaceWith, string filePattern)
        {
            Version = 2;
            SearchPattern = searchFor;
            ReplacePattern = replaceWith;
            FileNames = filePattern;
        }

        public int Version { get; set; } = 1;
        public string Description { get; set; } = string.Empty;

        public SearchType TypeOfSearch { get; set; } = SearchType.PlainText;
        public string SearchPattern { get; set; } = string.Empty;
        public string ReplacePattern { get; set; } = string.Empty;
        public bool CaseSensitive { get; set; }
        public bool WholeWord { get; set; }
        public bool Multiline { get; set; }
        public bool Singleline { get; set; }

        public FileSearchType TypeOfFileSearch { get; set; } = FileSearchType.Asterisk;
        public string FileNames { get; set; } = string.Empty;
        public string IgnoreFilePattern { get; set; } = string.Empty;
        public bool IncludeSubfolders { get; set; }
        public bool IncludeHiddenFiles { get; set; }
        public bool IncludeBinaryFiles { get; set; }


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

        public override bool Equals(object obj)
        {
            if (obj is Bookmark otherBookmark)
            {
                return FileNames == otherBookmark.FileNames &&
                    SearchPattern == otherBookmark.SearchPattern &&
                    ReplacePattern == otherBookmark.ReplacePattern;
            }
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 13;
                hashCode = (hashCode * 397) ^ FileNames.GetHashCode();
                hashCode = (hashCode * 397) ^ SearchPattern.GetHashCode();
                hashCode = (hashCode * 397) ^ ReplacePattern.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{SearchPattern} to {ReplacePattern} on {FileNames} :: {Description}";
        }
    }
}
