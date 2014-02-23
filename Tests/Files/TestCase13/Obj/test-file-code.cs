public void TestGetLine(string body, string line, int index, int lineNumber)
{
	int returnedLineNumber = -1;
	string returnedLine = Utils.GetLine(body, index, out returnedLineNumber);
	Assert.Equal(returnedLine, line);
	Assert.Equal(returnedLineNumber, lineNumber);
}