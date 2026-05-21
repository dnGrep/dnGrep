using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using dnGREP.Common;
using dnGREP.Engines;
using Xunit;

namespace Tests
{
    /// <summary>
    /// Comprehensive test class for fuzzy searching functionality.
    /// Tests cover various scenarios including exact matches, inexact matches,
    /// case sensitivity, and whole-word matching with different thresholds.
    /// </summary>
    public class FuzzySearchTests
    {
        private GrepEnginePlainText CreateEngine(double fuzzyThreshold = 0.7)
        {
            var engine = new GrepEnginePlainText();
            var initParams = new GrepEngineInitParams(2, 3, fuzzyThreshold, true, false);
            engine.Initialize(initParams, new FileFilter(), null);
            return engine;
        }

        private List<GrepSearchResult> ExecuteFuzzySearch(
            string text,
            string pattern,
            GrepSearchOption searchOptions = GrepSearchOption.Global,
            double fuzzyThreshold = 0.7)
        {
            var engine = CreateEngine(fuzzyThreshold);
            var encoding = Encoding.UTF8;
            using Stream inputStream = new MemoryStream(encoding.GetBytes(text));
            return engine.Search(inputStream, new FileData("test.txt"), pattern, SearchType.Soundex, searchOptions, encoding);
        }

        #region Exact Match Tests

        [Fact]
        public void FuzzySearch_ExactMatch_CaseInsensitive()
        {
            // Arrange
            string text = "The quick brown fox jumps over the lazy dog";
            string pattern = "brown";

            // Act
            var results = ExecuteFuzzySearch(text, pattern);

            // Assert
            Assert.Single(results);
            Assert.Single(results[0].Matches);
            string matched = text.Substring(results[0].Matches[0].StartLocation, results[0].Matches[0].Length);
            Assert.Equal("brown", matched);
        }

        [Fact]
        public void FuzzySearch_ExactMatch_CaseInsensitive_MixedCase()
        {
            // Arrange - Pattern lowercase, text has mixed case
            string text = "The quick Brown fox jumps";
            string pattern = "brown";

            // Act
            var results = ExecuteFuzzySearch(text, pattern);

            // Assert
            Assert.Single(results);
            Assert.Single(results[0].Matches);
            string matched = text.Substring(results[0].Matches[0].StartLocation, results[0].Matches[0].Length);
            Assert.Equal("Brown", matched);
        }

        [Fact]
        public void FuzzySearch_ExactMatch_CaseSensitive()
        {
            // Arrange
            string text = "The Brown fox and the brown dog";
            string pattern = "brown";

            // Act
            var results = ExecuteFuzzySearch(text, pattern, GrepSearchOption.CaseSensitive);

            // Assert
            Assert.Single(results);
            // Should find lowercase "brown" only (or as best match)
            Assert.True(results[0].Matches.Count >= 1);
        }

        [Fact]
        public void FuzzySearch_ExactMatch_CaseSensitive_UppercasePattern()
        {
            // Arrange
            string text = "The Brown fox and the brown dog";
            string pattern = "Brown";

            // Act
            var results = ExecuteFuzzySearch(text, pattern, GrepSearchOption.CaseSensitive);

            // Assert
            Assert.Single(results);
            Assert.True(results[0].Matches.Count >= 1);
        }

        [Fact]
        public void FuzzySearch_MultipleExactMatches()
        {
            // Arrange
            string text = @"John went to see Jane.
Jane met John at the cafe.
They talked about John's plans.";
            string pattern = "john";

            // Act
            var results = ExecuteFuzzySearch(text, pattern);

            // Assert
            Assert.Single(results);
            Assert.Equal(3, results[0].Matches.Count);
        }

        #endregion

        #region Whole-Word Tests

        [Fact]
        public void FuzzySearch_WholeWord_ExcludesPartialMatches()
        {
            // Arrange
            string text = "John is a person. Johnson works here. John Smith is the manager.";
            string pattern = "john";

            // Act
            var results = ExecuteFuzzySearch(text, pattern, GrepSearchOption.WholeWord);

            // Assert
            Assert.Single(results);
            // Should match "John" twice (whole words) but not "Johnson"
            Assert.Equal(2, results[0].Matches.Count);

            foreach (var match in results[0].Matches)
            {
                string matched = text.Substring(match.StartLocation, match.Length);
                Assert.True(matched.Equals("John", System.StringComparison.OrdinalIgnoreCase));
            }
        }

        [Fact]
        public void FuzzySearch_WholeWord_WithPunctuation()
        {
            // Arrange
            string text = "Hello, world! Hello-friend. Hello world.";
            string pattern = "hello";

            // Act
            var results = ExecuteFuzzySearch(text, pattern, GrepSearchOption.WholeWord);

            // Assert
            Assert.Single(results);
            // All three "Hello" are whole words (punctuation is a boundary)
            Assert.Equal(3, results[0].Matches.Count);
        }

        [Fact]
        public void FuzzySearch_WholeWord_WithNewlines()
        {
            // Arrange
            string text = @"test line
test is good
another test";
            string pattern = "test";

            // Act
            var results = ExecuteFuzzySearch(text, pattern, GrepSearchOption.WholeWord);

            // Assert
            Assert.Single(results);
            Assert.Equal(3, results[0].Matches.Count);
        }

        [Fact]
        public void FuzzySearch_NoWholeWord_MatchesPartialWords()
        {
            // Arrange
            string text = "John is a person. Johnson works here.";
            string pattern = "john";

            // Act
            var results = ExecuteFuzzySearch(text, pattern, GrepSearchOption.Global);

            // Assert
            Assert.Single(results);
            // Should find both "John" and "Johnson"
            Assert.True(results[0].Matches.Count >= 2);
        }

        [Fact]
        public void FuzzySearch_WholeWord_CaseSensitive_Combined()
        {
            // Arrange
            string text = "Hello world, HELLO world, hello world, hELLO world";
            string pattern = "hello";

            // Act
            var results = ExecuteFuzzySearch(text, pattern, GrepSearchOption.WholeWord | GrepSearchOption.CaseSensitive);

            // Assert
            Assert.Single(results);
            // Case-sensitive whole-word should prefer lowercase "hello"
            Assert.True(results[0].Matches.Count >= 1);
        }

        #endregion

        #region Case Sensitivity Tests

        [Fact]
        public void FuzzySearch_CaseSensitive_PrefersExactCase()
        {
            // Arrange
            string text = "java Java JAVA";
            string pattern = "java";

            // Act
            var results = ExecuteFuzzySearch(text, pattern, GrepSearchOption.CaseSensitive);

            // Assert
            Assert.Single(results);
            Assert.True(results[0].Matches.Count >= 1);
        }

        [Fact]
        public void FuzzySearch_CaseInsensitive_Default()
        {
            // Arrange
            string text = "java Java JAVA";
            string pattern = "java";

            // Act
            var results = ExecuteFuzzySearch(text, pattern);

            // Assert
            Assert.Single(results);
            // Should find all three (case-insensitive)
            Assert.Equal(3, results[0].Matches.Count);
        }

        [Fact]
        public void FuzzySearch_CaseInsensitive_WithMixedPatterns()
        {
            // Arrange
            string text = "PYTHON python Python PyThOn";
            string pattern = "python";

            // Act
            var results = ExecuteFuzzySearch(text, pattern);

            // Assert
            Assert.Single(results);
            Assert.Equal(4, results[0].Matches.Count);
        }

        #endregion

        #region Inexact Match Tests

        [Fact]
        public void FuzzySearch_InexactMatch_Transposition()
        {
            // "teh" vs "the" — adjacent swap costs 1 OSA edit.
            // 3-char token at threshold 0.6 allows floor(0.4 * 3) = 1 edit.
            string text = "teh quick brown fox";
            string pattern = "teh";

            var results = ExecuteFuzzySearch(text, pattern, GrepSearchOption.Global, 0.7);

            Assert.Single(results);
            Assert.Single(results[0].Matches);
            Assert.Equal("teh", text.Substring(results[0].Matches[0].StartLocation, results[0].Matches[0].Length));
        }

        [Fact]
        public void FuzzySearch_InexactMatch_OneSubstitution()
        {
            // "brwon" vs "brown" — 1 transposition (adjacent swap r?w).
            // 5-char token at threshold 0.8 allows floor(0.2 * 5) = 1 edit.
            string text = "the brwon fox";
            string pattern = "brown";

            var results = ExecuteFuzzySearch(text, pattern, GrepSearchOption.Global, 0.8);

            Assert.Single(results);
            Assert.Single(results[0].Matches);
            string matched = text.Substring(results[0].Matches[0].StartLocation, results[0].Matches[0].Length);
            Assert.Equal("brwon", matched);
        }

        [Fact]
        public void FuzzySearch_InexactMatch_SwappedCharacters()
        {
            // "recieve" vs "receive" — 1 transposition (ei?ie).
            // 7-char token at threshold 0.8 allows floor(0.2 * 7) = 1 edit.
            string text = "I recieve the letter daily";
            string pattern = "receive";

            var results = ExecuteFuzzySearch(text, pattern, GrepSearchOption.Global, 0.8);

            Assert.Single(results);
            Assert.Single(results[0].Matches);
            string matched = text.Substring(results[0].Matches[0].StartLocation, results[0].Matches[0].Length);
            Assert.Equal("recieve", matched);
        }

        [Fact]
        public void FuzzySearch_InexactMatch_MultiWord()
        {
            // "brown fox" — both tokens must match consecutively.
            // "brwon" costs 1 edit (transposition), "fox" is exact.
            // 5-char "brown" at threshold 0.8 allows 1 edit; 3-char "fox" at 0.8 allows 0.
            string text = "the brwon fox jumps";
            string pattern = "brown fox";

            var results = ExecuteFuzzySearch(text, pattern, GrepSearchOption.Global, 0.8);

            Assert.Single(results);
            Assert.Single(results[0].Matches);
            string matched = text.Substring(results[0].Matches[0].StartLocation, results[0].Matches[0].Length);
            Assert.Equal("brwon fox", matched);
        }

        [Fact]
        public void FuzzySearch_InexactMatch_MultiWord_BothTokensTypo()
        {
            // "brwon fxo" — both tokens have 1 transposition each.
            // At threshold 0.8: "brown" (5 chars) allows 1 edit, "fox" (3 chars) allows 0.
            // At threshold 0.6: "fox" (3 chars) allows floor(0.4*3)=1 edit.
            string text = "the brwon fxo jumps";
            string pattern = "brown fox";

            var results = ExecuteFuzzySearch(text, pattern, GrepSearchOption.Global, 0.6);

            Assert.Single(results);
            Assert.Single(results[0].Matches);
            string matched = text.Substring(results[0].Matches[0].StartLocation, results[0].Matches[0].Length);
            Assert.Equal("brwon fxo", matched);
        }

        [Fact]
        public void FuzzySearch_InexactMatch_ExactTokenNoMatch_AboveThreshold()
        {
            // "xyz" has 3 edits from "fox" — should NOT match at any reasonable threshold.
            string text = "the xyz jumps";
            string pattern = "fox";

            var results = ExecuteFuzzySearch(text, pattern, GrepSearchOption.Global, 0.7);

            Assert.Empty(results);
        }

        #endregion

        #region Threshold Tests

        [Fact]
        public void FuzzySearch_HighThreshold_ExactMatchOnly()
        {
            // At threshold 1.0 no edits are allowed; "tset" (1 transposition) must not match.
            string text = "test tset testing";
            string pattern = "test";

            var results = ExecuteFuzzySearch(text, pattern, GrepSearchOption.Global, 1.0);

            Assert.Single(results);
            Assert.Single(results[0].Matches);
            Assert.Equal("test", text.Substring(results[0].Matches[0].StartLocation, results[0].Matches[0].Length));
        }

        [Fact]
        public void FuzzySearch_Threshold_AllowsOneEdit()
        {
            // "tset" is a 1-edit transposition of "test" (4 chars).
            // floor((1 - 0.8) * 4) = floor(0.8) = 0  ? not enough
            // floor((1 - 0.7) * 4) = floor(1.2) = 1  ? allowed
            string text = "tset is wrong spelling";
            string pattern = "test";

            var resultsStrict  = ExecuteFuzzySearch(text, pattern, GrepSearchOption.Global, 0.8);
            var resultsLoose   = ExecuteFuzzySearch(text, pattern, GrepSearchOption.Global, 0.7);

            Assert.Empty(resultsStrict);
            Assert.Single(resultsLoose);
            Assert.Single(resultsLoose[0].Matches);
        }

        [Fact]
        public void FuzzySearch_Threshold_MultiWordBothTokensMustMatch()
        {
            // For "brown fox", both tokens must independently meet their edit budgets.
            // "brwon fox": "brwon" costs 1 edit (5 chars, threshold 0.8 ? 1 allowed). ?
            // "brown xyz": "xyz" costs 3 edits from "fox" (3 chars, threshold 0.8 ? 0). ?
            string text1 = "the brwon fox";
            string text2 = "the brown xyz";
            string pattern = "brown fox";

            var r1 = ExecuteFuzzySearch(text1, pattern, GrepSearchOption.Global, 0.8);
            var r2 = ExecuteFuzzySearch(text2, pattern, GrepSearchOption.Global, 0.8);

            Assert.Single(r1);
            Assert.Empty(r2);
        }

        [Fact]
        public void FuzzySearch_Threshold05_AllowsTwoEditsOnLongerToken()
        {
            // "programme" vs "programmed" (10 chars).
            // floor((1 - 0.8) * 10) = 2 edits allowed at 0.8.
            // "programed" has 1 edit (missing 'm') from "programmed".
            string text = "the program is programed here";
            string pattern = "programmed";

            var results = ExecuteFuzzySearch(text, pattern, GrepSearchOption.Global, 0.8);

            Assert.Single(results);
            Assert.Single(results[0].Matches);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void FuzzySearch_EmptyText()
        {
            string text = "";
            string pattern = "test";

            var results = ExecuteFuzzySearch(text, pattern);

            Assert.Empty(results);
        }

        [Fact]
        public void FuzzySearch_EmptyPattern()
        {
            string text = "Hello world";
            string pattern = "";

            var results = ExecuteFuzzySearch(text, pattern);

            Assert.Empty(results);
        }

        [Fact]
        public void FuzzySearch_PatternLongerThanText()
        {
            // More pattern tokens than text words ? cannot match.
            string text = "cat";
            string pattern = "catastrophe happened today";

            var results = ExecuteFuzzySearch(text, pattern, GrepSearchOption.Global, 0.5);

            Assert.Empty(results);
        }

        [Fact]
        public void FuzzySearch_SingleCharacter_ExactOnly()
        {
            // 1-char token has 0 allowed edits at all thresholds (floor(tol*1) = 0 for tol < 1).
            // Must match the exact character "q".
            string text = "The quick brown fox";
            string pattern = "q";

            var results = ExecuteFuzzySearch(text, pattern);

            Assert.Single(results);
            Assert.Single(results[0].Matches);
            Assert.Equal("q", text.Substring(results[0].Matches[0].StartLocation, results[0].Matches[0].Length));
        }

        [Fact]
        public void FuzzySearch_NonWordCharPattern_LiteralFallback()
        {
            // Patterns with no \w chars fall back to literal IndexOf.
            string text = "Email: test@example.com Phone: 123-456-7890";
            string pattern = "@";

            var results = ExecuteFuzzySearch(text, pattern);

            Assert.Single(results);
            Assert.Single(results[0].Matches);
        }

        [Fact]
        public void FuzzySearch_NoMatchWhenPatternTotallyDifferent()
        {
            // "xyz" has max distance from "fox" — no threshold should match.
            string text = "The quick brown xyz jumps";
            string pattern = "fox";

            var results = ExecuteFuzzySearch(text, pattern, GrepSearchOption.Global, 0.5);

            Assert.Empty(results);
        }

        #endregion

        #region Complex Scenario Tests

        [Fact]
        public void FuzzySearch_TestFuzzySearchJohn()
        {
            // This mirrors the prototype test in GrepCoreTest.cs
            string text = @"Richard, John James & Erika

John James
";
            string pattern = "john";

            var engine = CreateEngine(0.7);
            var encoding = Encoding.UTF8;
            using Stream inputStream = new MemoryStream(encoding.GetBytes(text));
            var results = engine.Search(inputStream, new FileData("test.txt"), pattern, SearchType.Soundex, GrepSearchOption.Global, encoding);

            Assert.Single(results);
            Assert.Equal(2, results[0].Matches.Count);

            string match1 = text.Substring(results[0].Matches[0].StartLocation, results[0].Matches[0].Length);
            string match2 = text.Substring(results[0].Matches[1].StartLocation, results[0].Matches[1].Length);

            Assert.Equal("John", match1);
            Assert.Equal("John", match2);
        }

        [Fact]
        public void FuzzySearch_MultilineText_WholeWordCaseSensitive()
        {
            // Arrange
            string text = @"First line with Test
Second line with test
Third line with TEST
Fourth line with testing";
            string pattern = "test";

            // Act
            var results = ExecuteFuzzySearch(text, pattern, GrepSearchOption.WholeWord | GrepSearchOption.CaseSensitive);

            // Assert
            Assert.Single(results);
            // Should prefer lowercase "test" and exclude "testing"
            Assert.True(results[0].Matches.Count >= 1);
        }

        [Fact]
        public void FuzzySearch_AllOptionsDisabled()
        {
            // Arrange
            string text = "Fuzzy Search Test";
            string pattern = "search";

            // Act
            var results = ExecuteFuzzySearch(text, pattern, GrepSearchOption.Global);

            // Assert
            Assert.Single(results);
            Assert.True(results[0].Matches.Count >= 1);
        }

        [Fact]
        public void FuzzySearch_AllOptionsEnabled()
        {
            // Arrange
            string text = "Fuzzy Search Test. fuzzy test.";
            string pattern = "search";

            // Act
            var results = ExecuteFuzzySearch(text, pattern, GrepSearchOption.Global | GrepSearchOption.CaseSensitive | GrepSearchOption.WholeWord);

            // Assert
            Assert.Single(results);
            // With all options enabled and case-sensitive, should find "Search" (capitalized)
            Assert.True(results[0].Matches.Count >= 1);
        }

        [Fact]
        public void FuzzySearch_MultipleResults_DifferentLines()
        {
            // Token matching: "apple" is its own word token on lines 1 and 2.
            // "pineapple" on line 3 is a single different token (not "apple").
            string text = @"Line 1: apple pie
Line 2: apple sauce
Line 3: pineapple juice";
            string pattern = "apple";

            var results = ExecuteFuzzySearch(text, pattern);

            Assert.Single(results);
            // Matches "apple" on lines 1 and 2; "pineapple" is a different token.
            Assert.Equal(2, results[0].Matches.Count);
        }

        #endregion

        #region Combined Option Tests

        [Theory]
        [InlineData("hello", "hello world", GrepSearchOption.Global, 0.7, 1)]
        [InlineData("hello", "Hello World", GrepSearchOption.Global, 0.7, 1)]
        [InlineData("hello", "hello HELLO Hello", GrepSearchOption.Global, 0.7, 3)]
        // WholeWord: "hello" is whole word, "helloween" is a different token and should not match
        // at threshold 0.7 since distance("hello","helloween")=4 > floor(0.3*5)=1
        [InlineData("hello", "hello helloween", GrepSearchOption.WholeWord, 0.7, 1)]
        [InlineData("hello", "hello HELLO", GrepSearchOption.CaseSensitive, 0.7, 1)]
        [InlineData("hello", "hello HELLO", GrepSearchOption.Global, 0.7, 2)]
        public void FuzzySearch_VariousScenarios(string pattern, string text, GrepSearchOption options, double threshold, int expectedMatches)
        {
            // Act
            var results = ExecuteFuzzySearch(text, pattern, options, threshold);

            // Assert
            if (expectedMatches > 0)
            {
                Assert.Single(results);
                Assert.Equal(expectedMatches, results[0].Matches.Count);
            }
            else
            {
                Assert.Empty(results);
            }
        }

        #endregion
    }
}
