using dnGREP.Common;
using Xunit;

namespace Tests
{
    public class BookmarkTest : TestBase
    {
        [Fact]
        public void TestBookmarks()
        {
            var bookmarkEntity = new BookmarkEntity();
            bookmarkEntity.Bookmarks.Add(new Bookmark() { SearchPattern = "test1", ReplacePattern = "test2", FileNames = "test3", Description = "test4" });
            BookmarkLibrary.Save();
            BookmarkLibrary.Load();
            Assert.Single(bookmarkEntity.Bookmarks);
            Assert.Equal("test4", bookmarkEntity.Bookmarks[0].Description);
        }

        [Fact]
        public void TestBookmarkEquality()
        {
            Bookmark b1 = new()
            {
                SearchPattern = "test1",
                ReplacePattern = "test2",
                FileNames = "test3"
            };
            Bookmark b2 = new()
            {
                SearchPattern = "test1",
                ReplacePattern = "test2",
                FileNames = "test3",
                Description = "test4"
            };
            Bookmark b3 = new()
            {
                SearchPattern = "testA",
                ReplacePattern = "testB",
                FileNames = "testC"
            };
            Bookmark b4 = new()
            {
                SearchPattern = "testA",
                ReplacePattern = "testB",
                FileNames = "testC",
                IgnoreFilePattern = "*.txt",
                WholeWord = true,
                Multiline = false,
                IncludeArchive = true,
            };
            Bookmark b5 = new()
            {
                SearchPattern = "testA",
                ReplacePattern = "testB",
                FileNames = "testC",
                IgnoreFilePattern = "*.txt",
                WholeWord = true,
                Multiline = false,
                IncludeArchive = true,
            };
            Bookmark b6 = new()
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
            BookmarkEntity bookmarkEntity = new();
            bookmarkEntity.Bookmarks.Add(new()
            {
                SearchPattern = "test1",
                ReplacePattern = "test2",
                FileNames = "test3",
                Description = "test4"
            });
            bookmarkEntity.Bookmarks.Add(new()
            {
                SearchPattern = "testA",
                ReplacePattern = "testB",
                FileNames = "testC"
            });
            Assert.Equal(2, bookmarkEntity.Bookmarks.Count);

            var bmk = bookmarkEntity.Find(new()
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
            BookmarkEntity bookmarkEntity = new();
            bookmarkEntity.Bookmarks.Add(new()
            {
                SearchPattern = "test1",
                ReplacePattern = "test2",
                FileNames = "test3",
                Description = "test4"
            });
            bookmarkEntity.Bookmarks.Add(new()
            {
                SearchPattern = "testA",
                ReplacePattern = "testB",
                FileNames = "testC"
            });
            Assert.Equal(2, bookmarkEntity.Bookmarks.Count);

            var bmk = bookmarkEntity.Find(new()
            {
                SearchPattern = "testA",
                ReplacePattern = "testB",
                FileNames = "testC"
            });
            Assert.NotNull(bmk);

            bookmarkEntity.AddFolderReference(bmk, @"c:\test\path");
            Assert.Single(bmk.FolderReferences);
        }

        [Fact]
        public void TestBookmarkAddMultipleFolderReference()
        {
            BookmarkEntity bookmarkEntity = new();
            bookmarkEntity.Bookmarks.Add(new()
            {
                SearchPattern = "test1",
                ReplacePattern = "test2",
                FileNames = "test3",
                Description = "test4"
            });
            bookmarkEntity.Bookmarks.Add(new()
            {
                SearchPattern = "testA",
                ReplacePattern = "testB",
                FileNames = "testC"
            });
            Assert.Equal(2, bookmarkEntity.Bookmarks.Count);

            var bmk = bookmarkEntity.Find(new()
            {
                SearchPattern = "test1",
                ReplacePattern = "test2",
                FileNames = "test3"
            });
            Assert.NotNull(bmk);

            bookmarkEntity.AddFolderReference(bmk, @"c:\test\path");
            bookmarkEntity.AddFolderReference(bmk, @"c:\other\path");
            Assert.Equal(2, bmk.FolderReferences.Count);
        }

        [Fact]
        public void TestBookmarkChangeFolderReference()
        {
            BookmarkEntity bookmarkEntity = new();
            bookmarkEntity.Bookmarks.Add(new()
            {
                SearchPattern = "test1",
                ReplacePattern = "test2",
                FileNames = "test3",
                Description = "test4"
            });
            bookmarkEntity.Bookmarks.Add(new()
            {
                SearchPattern = "testA",
                ReplacePattern = "testB",
                FileNames = "testC"
            });
            Assert.Equal(2, bookmarkEntity.Bookmarks.Count);

            var bmk = bookmarkEntity.Find(new()
            {
                SearchPattern = "test1",
                ReplacePattern = "test2",
                FileNames = "test3"
            });
            Assert.NotNull(bmk);

            bookmarkEntity.AddFolderReference(bmk, @"c:\test\path");
            Assert.Single(bmk.FolderReferences);

            var bmk2 = bookmarkEntity.Find(new()
            {
                SearchPattern = "testA",
                ReplacePattern = "testB",
                FileNames = "testC"
            });
            Assert.NotNull(bmk2);

            bookmarkEntity.AddFolderReference(bmk2, @"c:\test\path");
            Assert.Single(bmk2.FolderReferences);
            Assert.Empty(bmk.FolderReferences);
        }

        [Fact]
        public void TestFindBookmarkWithSection()
        {
            BookmarkEntity bookmarkEntity = new();
            bookmarkEntity.Bookmarks.Add(new()
            {
                SearchPattern = "",
                ReplacePattern = "",
                FileNames = "*.cs",
                Description = "Search for cs",
                TypeOfFileSearch = FileSearchType.Asterisk,
                ApplyFilePropertyFilters = false,
                ApplyContentSearchFilters = false,
            });
            bookmarkEntity.Bookmarks.Add(new()
            {
                SearchPattern = "",
                ReplacePattern = "",
                FileNames = "*.xml",
                Description = "Search for xml",
                TypeOfFileSearch = FileSearchType.Asterisk,
                ApplyFilePropertyFilters = false,
                ApplyContentSearchFilters = false,
            });
            Assert.Equal(2, bookmarkEntity.Bookmarks.Count);

            Bookmark currentBookmarkSettings = new()
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

            Bookmark? bk = bookmarkEntity.Find(currentBookmarkSettings);

            Assert.NotNull(bk);
            Assert.Equal("Search for cs", bk.Description);
        }

        [Fact]
        public void TestNotFindBookmarkWithSection()
        {
            // this test verifies that 'Find' will not return a bookmark
            // that matches on some enabled sections, but not all enabled sections
            BookmarkEntity bookmarkEntity = new();
            bookmarkEntity.Bookmarks.Add(new()
            {
                SearchPattern = "",
                ReplacePattern = "",
                FileNames = "*.cs",
                Description = "Search for cs",
                TypeOfFileSearch = FileSearchType.Asterisk,
                ApplyFilePropertyFilters = false,
                ApplyContentSearchFilters = false,
            });
            bookmarkEntity.Bookmarks.Add(new()
            {
                SearchPattern = "",
                ReplacePattern = "",
                FileNames = "*.xml",
                Description = "Search for xml",
                TypeOfFileSearch = FileSearchType.Asterisk,
                ApplyFilePropertyFilters = true,
                ApplyContentSearchFilters = true,
            });
            Assert.Equal(2, bookmarkEntity.Bookmarks.Count);

            Bookmark currentBookmarkSettings = new()
            {
                SearchPattern = @"\w+\s\w+",
                ReplacePattern = "",
                FileNames = "*.json",
            };


            Bookmark? bk = bookmarkEntity.Find(currentBookmarkSettings);

            Assert.Null(bk);
        }
    }
}
