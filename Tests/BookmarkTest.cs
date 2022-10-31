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
            BookmarkLibrary.Instance.Bookmarks.Add(new Bookmark() { SearchPattern = "test1", ReplacePattern = "test2", FileNames = "test3", Description = "test4" });
            BookmarkLibrary.Save();
            BookmarkLibrary.Load();
            Assert.Single(BookmarkLibrary.Instance.Bookmarks);
            Assert.Equal("test4", BookmarkLibrary.Instance.Bookmarks[0].Description);
        }

        [Fact]
        public void TestBookmarkEquality()
        {
            Bookmark b1 = new Bookmark()
            {
                SearchPattern = "test1",
                ReplacePattern = "test2",
                FileNames = "test3"
            };
            Bookmark b2 = new Bookmark()
            {
                SearchPattern = "test1",
                ReplacePattern = "test2",
                FileNames = "test3",
                Description = "test4"
            };
            Bookmark b3 = new Bookmark()
            {
                SearchPattern = "testA",
                ReplacePattern = "testB",
                FileNames = "testC"
            };
            Bookmark b4 = new Bookmark()
            {
                SearchPattern = "testA",
                ReplacePattern = "testB",
                FileNames = "testC",
                IgnoreFilePattern = "*.txt",
                WholeWord = true,
                Multiline = false,
                IncludeArchive = true,
            };
            Bookmark b5 = new Bookmark()
            {
                SearchPattern = "testA",
                ReplacePattern = "testB",
                FileNames = "testC",
                IgnoreFilePattern = "*.txt",
                WholeWord = true,
                Multiline = false,
                IncludeArchive = true,
            };
            Bookmark b6 = new Bookmark()
            {
                SearchPattern = "testA",
                ReplacePattern = "testB",
                FileNames = "testC",
                IgnoreFilePattern = "*.txt",
            };

            Assert.False(b1 == null);
            Assert.False(b1.Equals(null));
            Assert.False(Bookmark.Equals(null, b1));
            Assert.False(Bookmark.Equals(b1, null));
            Assert.True(b1 == b2);
            Assert.True(b1.Equals(b2));
            Assert.False(b1 == b3);
            Assert.False(b1.Equals(b3));
            Assert.True(b1 != b3);
            Assert.True(b4 == b5);
            Assert.False(b4 == b6);
        }

        [Fact]
        public void TestBookmarkFind()
        {
            BookmarkLibrary.Instance.Bookmarks.Clear();
            BookmarkLibrary.Instance.Bookmarks.Add(new Bookmark()
            {
                SearchPattern = "test1",
                ReplacePattern = "test2",
                FileNames = "test3",
                Description = "test4"
            });
            BookmarkLibrary.Instance.Bookmarks.Add(new Bookmark()
            {
                SearchPattern = "testA",
                ReplacePattern = "testB",
                FileNames = "testC"
            });
            Assert.Equal(2, BookmarkLibrary.Instance.Bookmarks.Count);

            var bmk = BookmarkLibrary.Instance.Find(new Bookmark()
            {
                SearchPattern = "test1",
                ReplacePattern = "test2",
                FileNames = "test3"
            });
            Assert.NotNull(bmk);
        }

        [Fact]
        public void TestBookmarkAddFolderReference()
        {
            BookmarkLibrary.Instance.Bookmarks.Clear();
            BookmarkLibrary.Instance.Bookmarks.Add(new Bookmark()
            {
                SearchPattern = "test1",
                ReplacePattern = "test2",
                FileNames = "test3",
                Description = "test4"
            });
            BookmarkLibrary.Instance.Bookmarks.Add(new Bookmark()
            {
                SearchPattern = "testA",
                ReplacePattern = "testB",
                FileNames = "testC"
            });
            Assert.Equal(2, BookmarkLibrary.Instance.Bookmarks.Count);

            var bmk = BookmarkLibrary.Instance.Find(new Bookmark()
            {
                SearchPattern = "testA",
                ReplacePattern = "testB",
                FileNames = "testC"
            });
            Assert.NotNull(bmk);

            BookmarkLibrary.Instance.AddFolderReference(bmk, @"c:\test\path");
            Assert.Single(bmk.FolderReferences);
        }

        [Fact]
        public void TestBookmarkAddMultipleFolderReference()
        {
            BookmarkLibrary.Instance.Bookmarks.Clear();
            BookmarkLibrary.Instance.Bookmarks.Add(new Bookmark()
            {
                SearchPattern = "test1",
                ReplacePattern = "test2",
                FileNames = "test3",
                Description = "test4"
            });
            BookmarkLibrary.Instance.Bookmarks.Add(new Bookmark()
            {
                SearchPattern = "testA",
                ReplacePattern = "testB",
                FileNames = "testC"
            });
            Assert.Equal(2, BookmarkLibrary.Instance.Bookmarks.Count);

            var bmk = BookmarkLibrary.Instance.Find(new Bookmark()
            {
                SearchPattern = "test1",
                ReplacePattern = "test2",
                FileNames = "test3"
            });
            Assert.NotNull(bmk);

            BookmarkLibrary.Instance.AddFolderReference(bmk, @"c:\test\path");
            BookmarkLibrary.Instance.AddFolderReference(bmk, @"c:\other\path");
            Assert.Equal(2, bmk.FolderReferences.Count);
        }

        [Fact]
        public void TestBookmarkChangeFolderReference()
        {
            BookmarkLibrary.Instance.Bookmarks.Clear();
            BookmarkLibrary.Instance.Bookmarks.Add(new Bookmark()
            {
                SearchPattern = "test1",
                ReplacePattern = "test2",
                FileNames = "test3",
                Description = "test4"
            });
            BookmarkLibrary.Instance.Bookmarks.Add(new Bookmark()
            {
                SearchPattern = "testA",
                ReplacePattern = "testB",
                FileNames = "testC"
            });
            Assert.Equal(2, BookmarkLibrary.Instance.Bookmarks.Count);

            var bmk = BookmarkLibrary.Instance.Find(new Bookmark()
            {
                SearchPattern = "test1",
                ReplacePattern = "test2",
                FileNames = "test3"
            });
            Assert.NotNull(bmk);

            BookmarkLibrary.Instance.AddFolderReference(bmk, @"c:\test\path");
            Assert.Single(bmk.FolderReferences);

            var bmk2 = BookmarkLibrary.Instance.Find(new Bookmark()
            {
                SearchPattern = "testA",
                ReplacePattern = "testB",
                FileNames = "testC"
            });
            Assert.NotNull(bmk2);

            BookmarkLibrary.Instance.AddFolderReference(bmk2, @"c:\test\path");
            Assert.Single(bmk2.FolderReferences);
            Assert.Empty(bmk.FolderReferences);
        }

        [Fact]
        public void TestFindBookmarkWithSection()
        {
            BookmarkLibrary.Instance.Bookmarks.Clear();
            BookmarkLibrary.Instance.Bookmarks.Add(new Bookmark()
            {
                SearchPattern = "",
                ReplacePattern = "",
                FileNames = "*.cs",
                Description = "Search for cs",
                TypeOfFileSearch = FileSearchType.Asterisk,
                ApplyFilePropertyFilters = false,
                ApplyContentSearchFilters = false,
            });
            BookmarkLibrary.Instance.Bookmarks.Add(new Bookmark()
            {
                SearchPattern = "",
                ReplacePattern = "",
                FileNames = "*.xml",
                Description = "Search for xml",
                TypeOfFileSearch = FileSearchType.Asterisk,
                ApplyFilePropertyFilters = false,
                ApplyContentSearchFilters = false,
            });
            Assert.Equal(2, BookmarkLibrary.Instance.Bookmarks.Count);

            var currentBookmarkSettings = new Bookmark()
            {
                SearchPattern = @"\w+\s\w+",
                ReplacePattern = "",
                FileNames = "*.cs",
                TypeOfFileSearch = FileSearchType.Asterisk,
                TypeOfSearch = SearchType.Regex,
                Multiline = true,
                ApplyFilePropertyFilters = true,
                ApplyContentSearchFilters = true,
            };

            Bookmark bk = BookmarkLibrary.Instance.Find(currentBookmarkSettings);

            Assert.NotNull(bk);
            Assert.Equal("Search for cs", bk.Description);
        }

        [Fact]
        public void TestNotFindBookmarkWithSection()
        {
            // this test verifies that 'Find' will not return a bookmark
            // that matches on some enabled sections, but not all enabled sections
            BookmarkLibrary.Instance.Bookmarks.Clear();
            BookmarkLibrary.Instance.Bookmarks.Add(new Bookmark()
            {
                SearchPattern = "",
                ReplacePattern = "",
                FileNames = "*.cs",
                Description = "Search for cs",
                TypeOfFileSearch = FileSearchType.Asterisk,
                ApplyFilePropertyFilters = false,
                ApplyContentSearchFilters = false,
            });
            BookmarkLibrary.Instance.Bookmarks.Add(new Bookmark()
            {
                SearchPattern = "",
                ReplacePattern = "",
                FileNames = "*.xml",
                Description = "Search for xml",
                TypeOfFileSearch = FileSearchType.Asterisk,
                ApplyFilePropertyFilters = true,
                ApplyContentSearchFilters = true,
            });
            Assert.Equal(2, BookmarkLibrary.Instance.Bookmarks.Count);

            var currentBookmarkSettings = new Bookmark()
            {
                SearchPattern = @"\w+\s\w+",
                ReplacePattern = "",
                FileNames = "*.json",
            };


            Bookmark bk = BookmarkLibrary.Instance.Find(currentBookmarkSettings);

            Assert.Null(bk);
        }
    }
}
