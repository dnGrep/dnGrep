using System;
using System.Collections.Generic;
using System.Text;
using MbUnit.Framework;
using dnGREP;
using dnGREP.Common;

namespace Tests
{
	[TestFixture]
	public class BookmarkTest : TestBase
	{
		[Test]
		public void TestBookmarks()
		{
			BookmarkLibrary.Instance.Bookmarks.Clear();
			BookmarkLibrary.Instance.Bookmarks.Add(new Bookmark("test1", "test2", "test3", "test4"));
			BookmarkLibrary.Save();
			BookmarkLibrary.Load();
			Assert.AreEqual(BookmarkLibrary.Instance.Bookmarks.Count, 1);
			Assert.AreEqual(BookmarkLibrary.Instance.Bookmarks[0].Description, "test4");
		}
	}
}
