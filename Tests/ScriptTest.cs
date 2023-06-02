using dnGREP.WPF;
using Xunit;

namespace Tests
{
    public class ScriptTest : TestBase
    {
        private readonly MainViewModel vm;

        public ScriptTest()
        {
            vm = new MainViewModel();
            vm.InitializeScriptTargets();
        }

        [Theory]
        [InlineData("set folder c:\\test\\directory")]
        [InlineData("set pathtomatch *.txt;*.js")]
        [InlineData("set pathtoignore *.txt")]
        [InlineData("set pathtoignore")]
        [InlineData("set searchinarchives false")]
        [InlineData("set patterntype asterisk")]
        [InlineData("set patterntype regex")]
        [InlineData("set patterntype everything")]
        [InlineData("set searchparallel true")]
        [InlineData("set usegitignore false")]
        [InlineData("set skipremotecloudstoragefiles true")]
        [InlineData("set encoding 65001")]
        [InlineData("set filterbyfilesize yes")]
        [InlineData("set filterbyfilesize no")]
        [InlineData("set filterbyfilesize none")]
        [InlineData("set sizefrom 0")]
        [InlineData("set sizeto 10000")]
        [InlineData("set includesubfolder true")]
        [InlineData("set maxsubfolderdepth -1")]
        [InlineData("set includehidden true")]
        [InlineData("set includebinary false")]
        [InlineData("set followsymlinks false")]
        [InlineData("set filedatefilter none")]
        [InlineData("set filedatefilter all")]
        [InlineData("set filedatefilter modified")]
        [InlineData("set filedatefilter created")]
        [InlineData("set filetimerange none")]
        [InlineData("set filetimerange all")]
        [InlineData("set filetimerange dates")]
        [InlineData("set filetimerange hours")]
        [InlineData("set startdate 2022-11-01T03:00:00Z")]
        [InlineData("set startdate 2022-11-01T03:00:00-07:00")]
        [InlineData("set enddate 2022-11-01T15:42:35Z")]
        [InlineData("set enddate 2022-11-01T15:42:35")]
        [InlineData("set hoursfrom -12")]
        [InlineData("set hoursto 0")]
        [InlineData("set searchtype PlainText")]
        [InlineData("set searchtype Regex")]
        [InlineData("set searchtype XPath")]
        [InlineData("set searchtype Soundex")]
        [InlineData("set searchtype Hex")]
        [InlineData("set searchfor the quick brown dog")]
        [InlineData("set searchfor")]
        [InlineData("set searchfor \"\"")]
        [InlineData("set searchfor \" \"")]
        [InlineData("set replacewith the quick brown fox")]
        [InlineData("set replacewith")]
        [InlineData("set replacewith \"\"")]
        [InlineData("set casesensitive True")]
        [InlineData("set wholeword False")]
        [InlineData("set multiline FALSE")]
        [InlineData("set dotasnewline false")]
        [InlineData("set booleanoperators false")]
        [InlineData("set capturegroupsearch false")]
        [InlineData("set searchinresults false")]
        [InlineData("set previewfile true")]
        [InlineData("set stopafterfirstmatch false")]
        [InlineData("set highlightmatches false")]
        [InlineData("set highlightgroups false")]
        [InlineData("set showcontextlines true")]
        [InlineData("set contextlinesbefore 3")]
        [InlineData("set contextlinesafter 3")]
        [InlineData("set wraptext true")]
        [InlineData("set resultszoom 2.5")]
        [InlineData("set sorttype FileNameOnly")]
        [InlineData("set sorttype FileTypeAndName")]
        [InlineData("set sorttype FileNameDepthFirst")]
        [InlineData("set sorttype FileNameBreadthFirst")]
        [InlineData("set sorttype SIZE")]
        [InlineData("set sorttype DATE")]
        [InlineData("set sorttype MatchCount")]
        [InlineData("set sortdirection ascending")]
        [InlineData("set sortdirection descending")]
        [InlineData("set reportmode FullLine")]
        [InlineData("set reportmode Matches")]
        [InlineData("set reportmode Groups")]
        [InlineData("set fileinformation true")]
        [InlineData("set trimwhitespace true")]
        [InlineData("set uniquevalues true")]
        [InlineData("set uniquescope PerFile")]
        [InlineData("set uniquescope Global")]
        [InlineData("set separatelines true")]
        [InlineData("set listitemseparator ,")]
        [InlineData("set listitemseparator \", \"")]

        [InlineData("bookmark add test bookmark")]
        [InlineData("bookmark use test bookmark")]
        [InlineData("bookmark remove test bookmark")]
        [InlineData("bookmark remove")]
        [InlineData("bookmark addfolder test bookmark")]
        [InlineData("bookmark removefolder")]
        [InlineData("bookmark removefolder name")]

        [InlineData("report full c:\\test\\report.txt")]
        [InlineData("report text c:\\test\\report.txt")]
        [InlineData("report csv c:\\test\\report.csv")]

        [InlineData("run powershell c:\\test\\script.ps1")]
        [InlineData("run cmd c:\\test\\script.bat")]

        [InlineData("resetfilters")]
        [InlineData("sort")]
        [InlineData("undo")]
        [InlineData("copyfiles c:\\test\\directory")]
        [InlineData("movefiles c:\\test\\directory")]
        [InlineData("deletefiles")]
        [InlineData("copyfilenames")]
        [InlineData("copyresults")]
        [InlineData("expandfilefilters true")]
        [InlineData("expandfilefilters false")]
        [InlineData("maximizeresults true")]
        [InlineData("maximizeresults false")]
        [InlineData("expandresultoptions true")]
        [InlineData("expandresultoptions false")]
        [InlineData("search")]
        [InlineData("replace")]
        [InlineData("exit")]
        public void TestValidateValidCommand(string line)
        {
            ScriptStatement? statement = ScriptManager.ParseLine(line, 1);
            var error = ScriptManager.Instance.Validate(statement);
            Assert.Null(error);
        }

        [Theory]
        [InlineData("grep \\w+ c:\\test\\directory", ScriptValidationError.InvalidCommand)]
        [InlineData("set searchin c:\\test\\directory", ScriptValidationError.InvalidTargetName)]
        [InlineData("set folder", ScriptValidationError.NullValueNotAllowed)]
        [InlineData("set includesubfolder", ScriptValidationError.NullValueNotAllowed)]
        [InlineData("set", ScriptValidationError.RequiredTargetValueMissing)]
        [InlineData("set filterbyfilesize true", ScriptValidationError.ConvertValueFromStringFailed)]
        [InlineData("set includehidden 1", ScriptValidationError.ConvertValueFromStringFailed)]
        [InlineData("resetfilters all", ScriptValidationError.UnneededValueFound)]
        [InlineData("sort filename ascending", ScriptValidationError.UnneededValueFound)]
        [InlineData("copyfiles", ScriptValidationError.RequiredStringValueMissing)]
        [InlineData("maximizeresults", ScriptValidationError.RequiredBooleanValueMissing)]
        [InlineData("maximizeresults yes", ScriptValidationError.ConvertValueFromStringFailed)]
        [InlineData("set filedatefilter yes", ScriptValidationError.ConvertValueFromStringFailed)]
        [InlineData("report folder c:\\test\\report.csv", ScriptValidationError.InvalidTargetName)]
        [InlineData("run Notepad.exe c:\\test\\script.gsc", ScriptValidationError.InvalidTargetName)]
        public void TestValidateInvalidCommand(string line, ScriptValidationError expected)
        {
            ScriptStatement? statement = ScriptManager.ParseLine(line, 1);
            var error = ScriptManager.Instance.Validate(statement);
            Assert.NotNull(error);
            Assert.Equal(expected, error.Item2);
        }

        [Theory]
        [InlineData("bookmark use test bookmark", "test bookmark")]
        [InlineData("set folder c:\\test\\directory", "c:\\test\\directory")]
        [InlineData("set searchfor", "")]
        [InlineData("set searchfor ", "")]
        [InlineData("set searchfor  ", "")]
        [InlineData("set searchfor \"\"", "")]
        [InlineData("set searchfor \" \"", " ")]
        [InlineData("set searchfor brown bear", "brown bear")]
        [InlineData("set searchfor  brown bear", "brown bear")]
        [InlineData("set searchfor \" brown bear\"", " brown bear")]
        [InlineData("set searchfor \"brown bear \"", "brown bear ")]
        [InlineData("set searchfor \"\"brown bear \"\"", "\"brown bear \"")]
        [InlineData(@"set searchfor `((\"".+?\"")|('.+?'))` AND `test`", @"`((\"".+?\"")|('.+?'))` AND `test`")]
        [InlineData(@"set searchfor <((\"".+?\"")|('.+?'))> AND <test>", @"<((\"".+?\"")|('.+?'))> AND <test>")]
        public void TestStringValues(string line, string expected)
        {
            ScriptStatement? statement = ScriptManager.ParseLine(line, 1);
            Assert.NotNull(statement);
            Assert.Equal(expected, statement.Value);
        }

        [Theory]
        [InlineData("variableA=one", "%variableA%", "one")]
        [InlineData("variableA=one;variableB=two", "%variableA% and %variableB%", "one and two")]
        [InlineData("variableA=dot;variableB=net", "%variableA%%variableB%", "dotnet")]
        [InlineData("variableA=one;variableB=two;variableA=", "%variableA% %variableB%", "%variableA% two")]
        [InlineData("", "%SystemRoot%", "C:\\WINDOWS")]
        public void TestExpandEnvironment(string initialization, string text, string expected)
        {
            var parts = initialization.Split(';');
            foreach (var part in parts)
            {
                var pair = part.Split('=');
                if (pair.Length == 2)
                {
                    ScriptManager.Instance.SetScriptEnvironmentVariable(pair[0], pair[1]);
                }
            }

            string result = ScriptManager.Instance.ExpandEnvironmentVariables(text);
            Assert.Equal(expected, result, true);
        }
    }
}
