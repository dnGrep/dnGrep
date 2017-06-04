using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Xml.Serialization;
using NLog;

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

        private const string storageFileName = "Bookmarks";

        public static void Load()
        {
            try
            {
                BookmarkEntity bookmarkLib;
                XmlSerializer serializer = new XmlSerializer(typeof(BookmarkEntity));
                if (!File.Exists(Utils.GetDataFolderPath() + "\\bookmarks.xml"))
                {
                    bookmarks = new BookmarkEntity();
                }
                else
                {
                    using (TextReader reader = new StreamReader(Utils.GetDataFolderPath() + "\\bookmarks.xml"))
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
                using (TextWriter writer = new StreamWriter(Utils.GetDataFolderPath() + "\\bookmarks.xml"))
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
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private List<Bookmark> bookmarks = new List<Bookmark>();

        public List<Bookmark> Bookmarks
        {
            get { return bookmarks; }
            set { bookmarks = value; }
        }

        public DataTable GetDataTable()
        {
            DataTable bookmarkTable = new DataTable();
            bookmarkTable.Columns.Add("Description", typeof(String));
            bookmarkTable.Columns.Add("SearchPattern", typeof(String));
            bookmarkTable.Columns.Add("ReplacePattern", typeof(String));
            bookmarkTable.Columns.Add("FileNames", typeof(String));
            bookmarks.Sort(new BookmarkComparer());
            foreach (Bookmark b in bookmarks)
            {
                bookmarkTable.LoadDataRow(new string[] {b.Description, b.SearchPattern, 
                    b.ReplacePattern, b.FileNames}, true);
            }
            return bookmarkTable;
        }

        public BookmarkEntity() { }
    }

    [Serializable]
    public class Bookmark
    {
        public Bookmark() { }
        public Bookmark(string pattern, string replacement, string files, string desc)
        {
            SearchPattern = pattern;
            ReplacePattern = replacement;
            FileNames = files;
            Description = desc;
        }

        public string SearchPattern { get; set; }
        public string ReplacePattern { get; set; }
        public string FileNames { get; set; }
        public string Description { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Bookmark))
            {
                return false;
            }
            else
            {
                Bookmark otherBookmark = (Bookmark)obj;
                if (this.FileNames == otherBookmark.FileNames &&
                    this.ReplacePattern == otherBookmark.ReplacePattern &&
                    this.SearchPattern == otherBookmark.SearchPattern)
                    return true;
                else
                    return false;
            }
        }
        public override int GetHashCode()
        {
            return (FileNames + ReplacePattern + SearchPattern).GetHashCode();
        }
    }

    public class BookmarkComparer : IComparer<Bookmark>
    {
        public int Compare(Bookmark x, Bookmark y)
        {
            return x.SearchPattern.CompareTo(y.SearchPattern);
        }
    }
}
