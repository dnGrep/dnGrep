using System;
using dnGREP.Common;
using dnGREP.WPF;
using Xunit;

namespace Tests
{
    public partial class CommandLineTest : TestBase
    {
        [Theory]
        [InlineData(01, @"""D:\folder 1"";""D:\folder 2""", @"""D:\folder 1"";""D:\folder 2""")]
        [InlineData(02, @"""D:\folder 1"",""D:\folder 2""", @"""D:\folder 1"",""D:\folder 2""")]
        [InlineData(03, @"""""D:\folder 1"";""D:\folder 2""""", @"""D:\folder 1"";""D:\folder 2""")]
        [InlineData(04, @"D:\folder1;""D:\folder 2""", @"D:\folder1;""D:\folder 2""")]
        [InlineData(05, @"""D:\folder 1"";D:\folder2", @"""D:\folder 1"";D:\folder2")]
        [InlineData(06, @"D:\a,b,c,d", @"""D:\a,b,c,d""")]
        [InlineData(07, @"""D:\a,b,c,d""", @"""D:\a,b,c,d""")]
        [InlineData(08, @"D:\a;b;c;d", @"""D:\a;b;c;d""")]
        [InlineData(09, @"""D:\a;b;c;d""", @"""D:\a;b;c;d""")]
        [InlineData(10, @"""D:\,,,\,""", @"""D:\,,,\,""")]
        [InlineData(11, @"""D:\,,,\,"",D:\folder", @"""D:\,,,\,"",D:\folder")]
        [InlineData(12, @"D:\folder,""D:\,,,\,""", @"D:\folder,""D:\,,,\,""")]
        [InlineData(13, @"D:\results\log*", @"D:\results\log*")]
        [InlineData(14, @"D:\folder1;D:\results\log*", @"D:\folder1;D:\results\log*")]
        [InlineData(15, @"""\\server\share 1"";""\\server\share 2""", @"""\\server\share 1"";""\\server\share 2""")]
        [InlineData(16, @"""\\server\share 1"",""\\server\share 2""", @"""\\server\share 1"",""\\server\share 2""")]
        [InlineData(17, @"\\server\share1;""\\server\share 2""", @"\\server\share1;""\\server\share 2""")]
        [InlineData(18, @"""\\server\share,1"";""\\server\share,2""", @"""\\server\share,1"";""\\server\share,2""")]
        [InlineData(19, @"""\\?\D:\folder 1"";""\\?\D:\folder 2""", @"""\\?\D:\folder 1"";""\\?\D:\folder 2""")]
        public void TestCommandLinePath(int index, string input, string expected)
        {
            // index is used to identify the test case
            Assert.True(index > 0);

            string formatted = CommandLineArgs.FormatPathArgs(input);
            Assert.Equal(expected, formatted);
        }

        [Theory]
        [InlineData(01, @"", 0, false, false, null, null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(02, @" /warmUp", 1, false, true, null, null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(03, @" ""c:\temp\test data\""", 1, false, false, @"""c:\temp\test data\""", null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, null, null, false)] // old style search directory without flag
        [InlineData(04, @" ""c:\temp\test data\"" p\w*", 2, false, false, @"""c:\temp\test data\""", @"p\w*", SearchType.Regex, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]  // old style search directory and regex without flags
        [InlineData(05, @" ""c:\temp\test data"" ""p\w*""", 2, false, false, @"""c:\temp\test data""", @"p\w*", SearchType.Regex, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]  // old style search directory and regex without flags
        [InlineData(06, @" c:\temp\testData\ ""p\w*""", 2, false, false, @"c:\temp\testData\", @"p\w*", SearchType.Regex, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]  // old style search directory and regex without flags
        [InlineData(07, @" c:\temp\testData ""p\w*""", 2, false, false, @"c:\temp\testData", @"p\w*", SearchType.Regex, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]  // old style search directory and regex without flags
        [InlineData(08, @" -f ""c:\temp\test data\""", 2, false, false, @"""c:\temp\test data\""", null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(09, @" -f ""c:\temp\testData\""", 2, false, false, @"c:\temp\testData\", null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(10, @" -f ""c:\temp\testData"";""c:\temp\test files""", 2, false, false, @"c:\temp\testData;""c:\temp\test files""", null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(11, @" -f ""c:\temp\test files"";""c:\temp\testData""", 2, false, false, @"""c:\temp\test files"";c:\temp\testData", null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(12, @" -f ""c:\temp\test files"";""c:\temp\testData"" -s p\w*", 4, false, false, @"""c:\temp\test files"";c:\temp\testData", @"p\w*", null, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(13, @" -f ""c:\temp\test files"";""c:\temp\testData"" -s ""p\w*""", 4, false, false, @"""c:\temp\test files"";c:\temp\testData", @"p\w*", null, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(14, @" -f ""c:\temp\testData;c:\temp\test files""", 2, false, false, @"c:\temp\testData;""c:\temp\test files""", null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(15, @" -f c:\temp\testData;""c:\temp\test files""", 2, false, false, @"c:\temp\testData;""c:\temp\test files""", null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(16, @" -f ""c:\temp\test files"";c:\temp\testData", 2, false, false, @"""c:\temp\test files"";c:\temp\testData", null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(17, @" -f c:\temp\testData;""c:\temp\test files"" -s p\w*", 4, false, false, @"c:\temp\testData;""c:\temp\test files""", @"p\w*", null, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(18, @" -f ""c:\temp\test files"";c:\temp\testData -s p\w*", 4, false, false, @"""c:\temp\test files"";c:\temp\testData", @"p\w*", null, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(19, @" -f c:\temp\testData\", 2, false, false, @"c:\temp\testData\", null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(20, @" -f c:\temp\testData", 2, false, false, @"c:\temp\testData", null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(21, @" -f ""c:\temp\test data\"" -s p\w*", 4, false, false, @"""c:\temp\test data\""", @"p\w*", null, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(22, @" -f ""c:\temp\test data\"" -s ""p\w*""", 4, false, false, @"""c:\temp\test data\""", @"p\w*", null, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(23, @" -f ""c:\temp\testData\"" -s p\w*", 4, false, false, @"c:\temp\testData\", @"p\w*", null, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(24, @" -f c:\temp\testData\ -s p\w*", 4, false, false, @"c:\temp\testData\", @"p\w*", null, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(25, @" -f c:\temp\testData -s p\w*", 4, false, false, @"c:\temp\testData", @"p\w*", null, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(26, @" -f c:\temp\testData -s ""p\w*""", 4, false, false, @"c:\temp\testData", @"p\w*", null, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(27, @" -f c:\temp\testData -s p""\w*", 4, false, false, @"c:\temp\testData", @"p""\w*", null, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(28, @" -f c:\temp\testData -s ""\w*", 4, false, false, @"c:\temp\testData", @"""\w*", null, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(29, @" -f c:\temp\testData -st Regex -s ""p\w*""", 6, false, false, @"c:\temp\testData", @"p\w*", SearchType.Regex, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(30, @" -f c:\temp\testData -pm *.txt;*.xml -s ""p\w*""", 6, false, false, @"c:\temp\testData", @"p\w*", null, "*.txt;*.xml", null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(31, @" -f c:\temp\testData -pt Asterisk -pm *.* -pi *.pdf -s ""p\w*""", 10, false, false, @"c:\temp\testData", @"p\w*", null, "*.*", "*.pdf", FileSearchType.Asterisk, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(32, @" -f c:\temp\testData -s p\w* /cs true /ww True /ml false /dn false /bo False", 14, false, false, @"c:\temp\testData", @"p\w*", null, null, null, null, true, true, false, false, false, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(33, @" -f c:\temp\testData /cs true /ww True /ml false", 8, false, false, @"c:\temp\testData", null, null, null, null, null, true, true, false, null, null, false, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(34, @" -f c:\temp\testData\ -s p\w* -rpt c:\temp\report.txt", 6, false, false, @"c:\temp\testData\", @"p\w*", null, null, null, null, null, null, null, null, null, true, @"c:\temp\report.txt", null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(35, @" -f c:\temp\testData\ -s p\w* -txt c:\temp\report.txt", 6, false, false, @"c:\temp\testData\", @"p\w*", null, null, null, null, null, null, null, null, null, true, null, @"c:\temp\report.txt", null, null, null, null, null, null, null, null, null, false)]
        [InlineData(36, @" -f c:\temp\testData\ -s p\w* -csv c:\temp\report.csv", 6, false, false, @"c:\temp\testData\", @"p\w*", null, null, null, null, null, null, null, null, null, true, null, null, @"c:\temp\report.csv", null, null, null, null, null, null, null, null, false)]
        [InlineData(37, @" -f c:\temp\testData\ -s p\w* -csv c:\temp\report.csv -x", 7, false, false, @"c:\temp\testData\", @"p\w*", null, null, null, null, null, null, null, null, null, true, null, null, @"c:\temp\report.csv", null, null, null, null, null, null, null, null, true)]
        [InlineData(38, @" -f c:\temp\testData\ -s p\w* -x -csv c:\temp\report.csv", 7, false, false, @"c:\temp\testData\", @"p\w*", null, null, null, null, null, null, null, null, null, true, null, null, @"c:\temp\report.csv", null, null, null, null, null, null, null, null, true)]
        [InlineData(39, @" -f c:\temp\testData\ -s p\w* -mode Groups -fi false -unique true -scope Global -sl true -sep "" "" -rpt c:\temp\report.txt", 18, false, false, @"c:\temp\testData\", @"p\w*", null, null, null, null, null, null, null, null, null, true, @"c:\temp\report.txt", null, null, ReportMode.Groups, false, null, true, UniqueScope.Global, true, " ", null, false)]
        [InlineData(30, @" -f c:\temp\testData\ -s p\w* -mode FullLine -fi true -trim true -rpt c:\temp\report.txt", 12, false, false, @"c:\temp\testData\", @"p\w*", null, null, null, null, null, null, null, null, null, true, @"c:\temp\report.txt", null, null, ReportMode.FullLine, true, true, null, null, null, null, null, false)]
        [InlineData(41, @" -sep "" """, 2, false, false, null, null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, " ", null, false)]
        [InlineData(42, @" -script scriptName", 2, false, false, null, null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, null, "scriptName", false)]
        [InlineData(43, @" -folder ""D:\folder 1"";""D:\folder 2""", 2, false, false, @"""D:\folder 1"";""D:\folder 2""", null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(44, @" -folder """"D:\folder 1"";""D:\folder 2""""", 2, false, false, @"""D:\folder 1"";""D:\folder 2""", null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(45, @" -folder ""D:\folder 1\"";""D:\folder 2\""", 2, false, false, @"""D:\folder 1\"";""D:\folder 2\""", null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(46, @" -folder """"D:\folder 1\"";""D:\folder 2\""""", 2, false, false, @"""D:\folder 1\"";""D:\folder 2\""", null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(47, @" -folder ""D:\folder-version 1"";""D:\folder 2""", 2, false, false, @"""D:\folder-version 1"";""D:\folder 2""", null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(48, @" -f c:\temp\testData -f ""c:\temp\test files""", 4, false, false, @"c:\temp\testData;""c:\temp\test files""", null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(49, @" -f ""c:\temp\test files"" -f ""c:\temp\testData""", 4, false, false, @"""c:\temp\test files"";c:\temp\testData", null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(50, @" -f ""c:\temp\test files"" -f c:\temp\testData", 4, false, false, @"""c:\temp\test files"";c:\temp\testData", null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, null, null, false)]
        public void SplitCommandLineTest(int index, string commandLine, int argCount,
            bool? expInvalidArgument, bool? expIsWarmUp, string? expSearchPath, string? expSearchFor,
            SearchType? expSearchType, string? expPatternToInclude, string? expPatternToExclude,
            FileSearchType? expTypeOfFileSearch, bool? expCaseSensitive, bool? expWholeWord,
            bool? expMultiline, bool? expDotAsNewLine, bool? expBooleanOperators, bool? expExecuteSearch,
            string? expReportPath, string? expTextPath, string? expCsvPath, ReportMode? reportMode,
            bool? includeFileInformation, bool? trimWhitespace, bool? filterUniqueValues,
            UniqueScope? uniqueScope, bool? outputOnSeparateLines, string? listItemSeparator,
            string? script, bool? expExit)
        {
            // index is used to identify the test case
            Assert.True(index > 0);

            string program = @"""C:\\Program Files\\dnGREP\\dnGREP.exe""";

            CommandLineArgs args = new(program + commandLine);

            Assert.Equal(argCount, args.Count);
            Assert.Equal(expInvalidArgument, args.InvalidArgument);
            Assert.Equal(expIsWarmUp, args.WarmUp);
            Assert.Equal(expSearchPath, args.SearchPath);
            Assert.Equal(expSearchFor, args.SearchFor);
            Assert.Equal(expSearchType, args.TypeOfSearch);
            Assert.Equal(expPatternToInclude, args.NamePatternToInclude);
            Assert.Equal(expPatternToExclude, args.NamePatternToExclude);
            Assert.Equal(expTypeOfFileSearch, args.TypeOfFileSearch);
            Assert.Equal(expCaseSensitive, args.CaseSensitive);
            Assert.Equal(expWholeWord, args.WholeWord);
            Assert.Equal(expMultiline, args.Multiline);
            Assert.Equal(expDotAsNewLine, args.DotAsNewline);
            Assert.Equal(expBooleanOperators, args.BooleanOperators);
            Assert.Equal(expExecuteSearch, args.ExecuteSearch);
            Assert.Equal(expReportPath, args.ReportPath);
            Assert.Equal(expTextPath, args.TextPath);
            Assert.Equal(expCsvPath, args.CsvPath);
            Assert.Equal(reportMode, args.ReportMode);
            Assert.Equal(includeFileInformation, args.IncludeFileInformation);
            Assert.Equal(trimWhitespace, args.TrimWhitespace);
            Assert.Equal(filterUniqueValues, args.FilterUniqueValues);
            Assert.Equal(uniqueScope, args.UniqueScope);
            Assert.Equal(outputOnSeparateLines, args.OutputOnSeparateLines);
            Assert.Equal(listItemSeparator, args.ListItemSeparator);
            Assert.Equal(script, args.Script);
            Assert.Equal(expExit, args.Exit);
        }

        [Theory]
        [InlineData(01, @" /s p\w* /e c:\temp\testData *.cs", 4, false, @"c:\temp\testData *.cs", @"p\w*", null, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(02, @" /s p\w* /cs true /ww True /ml false /dn false /bo False /e c:\temp\testData *.cs", 14, false, @"c:\temp\testData *.cs", @"p\w*", null, null, null, null, true, true, false, false, false, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(03, @" /e c:\temp\testData *.cs -searchforexact p\w*", 4, false, @"c:\temp\testData *.cs", @"p\w*", null, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(04, @" /se p\w* -everything c:\temp\testData *.cs", 4, false, @"c:\temp\testData *.cs", @"p\w*", null, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        public void SplitCommandLineTestEverything(int index, string commandLine, int argCount,
            bool? expInvalidArgument, string? expEverything, string? expSearchFor,
            SearchType? expSearchType, string? expPatternToInclude, string? expPatternToExclude,
            FileSearchType? expTypeOfFileSearch, bool? expCaseSensitive, bool? expWholeWord,
            bool? expMultiline, bool? expDotAsNewLine, bool? expBooleanOperators, bool? expExecuteSearch,
            string? expReportPath, string? expTextPath, string? expCsvPath, ReportMode? reportMode,
            bool? includeFileInformation, bool? trimWhitespace, bool? filterUniqueValues,
            UniqueScope? uniqueScope, bool? outputOnSeparateLines, string? listItemSeparator,
            string? script, bool? expExit)
        {
            // index is used to identify the test case
            Assert.True(index > 0);

            string program = @"""C:\\Program Files\\dnGREP\\dnGREP.exe""";

            CommandLineArgs args = new(program + commandLine);

            Assert.Equal(argCount, args.Count);
            Assert.Equal(expInvalidArgument, args.InvalidArgument);
            Assert.Equal(expEverything, args.Everything);
            Assert.Equal(expSearchFor, args.SearchFor);
            Assert.Equal(expSearchType, args.TypeOfSearch);
            Assert.Equal(expPatternToInclude, args.NamePatternToInclude);
            Assert.Equal(expPatternToExclude, args.NamePatternToExclude);
            Assert.Equal(expTypeOfFileSearch, args.TypeOfFileSearch);
            Assert.Equal(expCaseSensitive, args.CaseSensitive);
            Assert.Equal(expWholeWord, args.WholeWord);
            Assert.Equal(expMultiline, args.Multiline);
            Assert.Equal(expDotAsNewLine, args.DotAsNewline);
            Assert.Equal(expBooleanOperators, args.BooleanOperators);
            Assert.Equal(expExecuteSearch, args.ExecuteSearch);
            Assert.Equal(expReportPath, args.ReportPath);
            Assert.Equal(expTextPath, args.TextPath);
            Assert.Equal(expCsvPath, args.CsvPath);
            Assert.Equal(reportMode, args.ReportMode);
            Assert.Equal(includeFileInformation, args.IncludeFileInformation);
            Assert.Equal(trimWhitespace, args.TrimWhitespace);
            Assert.Equal(filterUniqueValues, args.FilterUniqueValues);
            Assert.Equal(uniqueScope, args.UniqueScope);
            Assert.Equal(outputOnSeparateLines, args.OutputOnSeparateLines);
            Assert.Equal(listItemSeparator, args.ListItemSeparator);
            Assert.Equal(script, args.Script);
            Assert.Equal(expExit, args.Exit);
        }

    }
}
