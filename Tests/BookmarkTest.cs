using dnGREP.Common;
using Xunit;

namespace Tests
{
    public class BookmarkTest : TestBase
    {
        [Fact]
        public void TestBookmarks()
        {
            BookmarkLibrary.Instance.Bookmarks.Clear();
            BookmarkLibrary.Instance.Bookmarks.Add(new Bookmark("test1", "test2", "test3") { Description = "test4" });
            BookmarkLibrary.Save();
            BookmarkLibrary.Load();
            Assert.Single(BookmarkLibrary.Instance.Bookmarks);
            Assert.Equal("test4", BookmarkLibrary.Instance.Bookmarks[0].Description);
        }

        [Fact]
        public void TestBookmarkFind()
        {
            BookmarkLibrary.Instance.Bookmarks.Clear();
            BookmarkLibrary.Instance.Bookmarks.Add(new Bookmark("test1", "test2", "test3") { Description = "test4" });
            BookmarkLibrary.Instance.Bookmarks.Add(new Bookmark("testA", "testB", "testC"));
            Assert.Equal(2, BookmarkLibrary.Instance.Bookmarks.Count);

            var bmk = BookmarkLibrary.Instance.Find("test1", "test2", "test3");
            Assert.NotNull(bmk);
        }

        [Fact]
        public void TestBookmarkAddFolderReference()
        {
            BookmarkLibrary.Instance.Bookmarks.Clear();
            BookmarkLibrary.Instance.Bookmarks.Add(new Bookmark("test1", "test2", "test3") { Description = "test4" });
            BookmarkLibrary.Instance.Bookmarks.Add(new Bookmark("testA", "testB", "testC"));
            Assert.Equal(2, BookmarkLibrary.Instance.Bookmarks.Count);

            var bmk = BookmarkLibrary.Instance.Find("test1", "test2", "test3");
            Assert.NotNull(bmk);

            BookmarkLibrary.Instance.AddFolderReference(bmk, @"c:\test\path");
            Assert.Single(bmk.FolderReferences);
        }

        [Fact]
        public void TestBookmarkAddMultipleFolderReference()
        {
            BookmarkLibrary.Instance.Bookmarks.Clear();
            BookmarkLibrary.Instance.Bookmarks.Add(new Bookmark("test1", "test2", "test3") { Description = "test4" });
            BookmarkLibrary.Instance.Bookmarks.Add(new Bookmark("testA", "testB", "testC"));
            Assert.Equal(2, BookmarkLibrary.Instance.Bookmarks.Count);

            var bmk = BookmarkLibrary.Instance.Find("test1", "test2", "test3");
            Assert.NotNull(bmk);

            BookmarkLibrary.Instance.AddFolderReference(bmk, @"c:\test\path");
            BookmarkLibrary.Instance.AddFolderReference(bmk, @"c:\other\path");
            Assert.Equal(2, bmk.FolderReferences.Count);
        }

        [Fact]
        public void TestBookmarkChangeFolderReference()
        {
            BookmarkLibrary.Instance.Bookmarks.Clear();
            BookmarkLibrary.Instance.Bookmarks.Add(new Bookmark("test1", "test2", "test3") { Description = "test4" });
            BookmarkLibrary.Instance.Bookmarks.Add(new Bookmark("testA", "testB", "testC"));
            Assert.Equal(2, BookmarkLibrary.Instance.Bookmarks.Count);

            var bmk = BookmarkLibrary.Instance.Find("test1", "test2", "test3");
            Assert.NotNull(bmk);

            BookmarkLibrary.Instance.AddFolderReference(bmk, @"c:\test\path");
            Assert.Single(bmk.FolderReferences);

            var bmk2 = BookmarkLibrary.Instance.Find("testA", "testB", "testC");
            Assert.NotNull(bmk2);

            BookmarkLibrary.Instance.AddFolderReference(bmk2, @"c:\test\path");
            Assert.Single(bmk2.FolderReferences);
            Assert.Empty(bmk.FolderReferences);
        }
    }
}
