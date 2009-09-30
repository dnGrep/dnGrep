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

		[Test]
		public void GetLines_Returns_Correct_Line()
		{
			string text = "Hello world" + Environment.NewLine + "My tests are good" + Environment.NewLine + "How about yours?";
			List<int> lineNumbers = new List<int>();
			List<string> lines = Utils.GetLines(text, 3, 2, out lineNumbers);
			Assert.AreEqual(lines.Count, 1);
			Assert.AreEqual(lines[0], "Hello world");
			Assert.AreEqual(lineNumbers.Count, 1);
			Assert.AreEqual(lineNumbers[0], 1);

			lines = Utils.GetLines(text, 14, 2, out lineNumbers);
			Assert.AreEqual(lines.Count, 1);
			Assert.AreEqual(lines[0], "My tests are good");
			Assert.AreEqual(lineNumbers.Count, 1);
			Assert.AreEqual(lineNumbers[0], 2);

			lines = Utils.GetLines(text, 3, 11, out lineNumbers);
			Assert.AreEqual(lines.Count, 2);
			Assert.AreEqual(lines[0], "Hello world");
			Assert.AreEqual(lines[1], "My tests are good");
			Assert.AreEqual(lineNumbers.Count, 2);
			Assert.AreEqual(lineNumbers[0], 1);
			Assert.AreEqual(lineNumbers[1], 2);

			lines = Utils.GetLines(text, 3, 30, out lineNumbers);
			Assert.AreEqual(lines.Count, 3);
			Assert.AreEqual(lines[0], "Hello world");
			Assert.AreEqual(lines[1], "My tests are good");
			Assert.AreEqual(lines[2], "How about yours?");
			Assert.AreEqual(lineNumbers.Count, 3);
			Assert.AreEqual(lineNumbers[0], 1);
			Assert.AreEqual(lineNumbers[1], 2);
			Assert.AreEqual(lineNumbers[2], 3);

			lines = Utils.GetLines("test", 2, 2, out lineNumbers);
			Assert.AreEqual(lines.Count, 1);
			Assert.AreEqual(lines[0], "test");
			Assert.AreEqual(lineNumbers.Count, 1);
			Assert.AreEqual(lineNumbers[0], 1);

			lines = Utils.GetLines("test", 0, 2, out lineNumbers);
			Assert.AreEqual(lines.Count, 1);
			Assert.AreEqual(lines[0], "test");
			Assert.AreEqual(lineNumbers.Count, 1);
			Assert.AreEqual(lineNumbers[0], 1);

			lines = Utils.GetLines("test", 10, 2, out lineNumbers);
			Assert.IsNull(lines);
			Assert.IsNull(lineNumbers);

			lines = Utils.GetLines("test", 2, 10, out lineNumbers);
			Assert.IsNull(lines);
			Assert.IsNull(lineNumbers);
		}
	}
}
