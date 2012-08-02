using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using dnGREP;
using dnGREP.Common;

namespace Tests
{	
	public class BookmarkTest : TestBase
	{
		[Fact]
		public void TestBookmarks()
		{
			BookmarkLibrary.Instance.Bookmarks.Clear();
			BookmarkLibrary.Instance.Bookmarks.Add(new Bookmark("test1", "test2", "test3", "test4"));
			BookmarkLibrary.Save();
			BookmarkLibrary.Load();
			Assert.Equal(BookmarkLibrary.Instance.Bookmarks.Count, 1);
			Assert.Equal(BookmarkLibrary.Instance.Bookmarks[0].Description, "test4");
		}
	}
}
