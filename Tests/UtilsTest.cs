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
	}
}
