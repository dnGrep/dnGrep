using System;
using System.Collections.Generic;
using System.Text;
using MbUnit.Framework;
using dnGREP;

namespace Tests
{
	[TestFixture]
	public class UtilsTest
	{
		[RowTest]
		[Row("Hello world", "Hello world", 2, 1)]
		[Row("Hi", "Hi", 2, 1)]
		[Row("Hi\r\n\r\nWorld", "", 4, 2)]
		[Row("Hi\r\n\r\nWorld", "World", 6, 3)]
		public void TestGetLine(string body, string line, int index, int lineNumber)
		{
			int returnedLineNumber = -1;
			string returnedLine = Utils.GetLine(body, index, out returnedLineNumber);
			Assert.AreEqual(returnedLine, line);
			Assert.AreEqual(returnedLineNumber, lineNumber);
		}

		[RowTest]
		[Row("0.9.1", "0.9.2", true)]
		[Row("0.9.1", "0.9.2.5556", true)]
		[Row("0.9.1.5554", "0.9.1.5556", true)]
		[Row("0.9.0.5557", "0.9.1.5550", true)]
		[Row("0.9.1", "0.9.0.5556", false)]
		[Row("0.9.5.5000", "0.9.0.5556", false)]
		[Row(null, "0.9.0.5556", false)]
		[Row("0.9.5.5000", "", false)]
		[Row("0.9.5.5000", null, false)]
		[Row("xyz", "abc", false)]
		public void CompareVersions(string v1, string v2, bool result)
		{
			Assert.IsTrue(PublishedVersionExtractor.IsUpdateNeeded(v1, v2) == result);
		}
	}
}
