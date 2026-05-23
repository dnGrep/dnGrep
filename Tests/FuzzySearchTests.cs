using System.Collections.Generic;
using System.IO;
using System.Text;
using dnGREP.Common;
using dnGREP.Engines;
using Xunit;

namespace Tests
{
    /// <summary>
    /// Tests for fuzzy search (SearchType.Fuzzy) via GrepEnginePlainText.
    ///
    /// Key design facts about the FuzzyMatch implementation:
    ///
    /// 1. TOKEN-BASED: The pattern and the text are each split into word tokens (\w+).
    ///    Every pattern token must fuzzy-match a consecutive text token. Non-word
    ///    characters (spaces, punctuation) between tokens are included in the matched span
    ///    but are not themselves compared.
    ///
    /// 2. EDIT BUDGET per token: AllowedEdits = floor((1 - threshold) * tokenLength).
    ///    Because Match_Threshold is stored as a float, avoid thresholds whose
    ///    complement rounds down unexpectedly (e.g. 0.8f gives (1-0.8f)˜0.19999999f,
    ///    so floor(0.19999999f * 5) = 0, not 1). Use 0.75 to allow 1 edit on 5-char tokens.
    ///
    /// 3. MULTILINE=false: the engine reads the file line-by-line; each line is searched
    ///    separately. All matches across all lines are collected in a single GrepSearchResult.
    ///
    /// 4. MULTILINE=true: the entire file is read as one string. Patterns can span lines.
    ///    Matches are found across the whole document.
    ///
    /// 5. WHOLE WORD: after a fuzzy token match, the matched span must be preceded by a
    ///    non-word character (or start of text) and followed by a non-word character (or
    ///    end of text).
    ///
    /// 6. CASE SENSITIVE: when false (default), both sides are lowercased before comparison.
    ///    When true, comparison is exact-case.
    ///
    /// 7. GLOBAL flag: always used for fuzzy search (does not affect the fuzzy search path).
    /// </summary>
    public class FuzzySearchTests
    {
        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        /// <summary>
        /// Creates a GrepEnginePlainText initialised with the given fuzzy threshold.
        /// </summary>
        private static GrepEnginePlainText CreateEngine(double fuzzyThreshold = 0.7)
        {
            var engine = new GrepEnginePlainText();
            var initParams = new GrepEngineInitParams(0, 0, fuzzyThreshold, true, false);
            engine.Initialize(initParams, new FileFilter(), null);
            return engine;
        }

        /// <summary>
        /// Runs a fuzzy search on an in-memory string.
        /// GrepSearchOption.Global is always included; pass additional flags via searchOptions.
        /// Multiline mode is controlled via the GrepSearchOption.Multiline flag.
        /// </summary>
        private static List<GrepSearchResult> RunSearch(
            string text,
            string pattern,
            GrepSearchOption searchOptions = GrepSearchOption.Global,
            double fuzzyThreshold = 0.7)
        {
            var engine = CreateEngine(fuzzyThreshold);
            var encoding = Encoding.UTF8;
            searchOptions |= GrepSearchOption.Global;
            using Stream stream = new MemoryStream(encoding.GetBytes(text));
            return engine.Search(stream, new FileData("test.txt"), pattern,
                SearchType.Fuzzy, searchOptions, encoding);
        }

        // -------------------------------------------------------------------------
        // Exact match — basic
        // -------------------------------------------------------------------------

        [Fact]
        public void ExactMatch_SingleWord_CaseInsensitive()
        {
            // "brown" matches the word "brown" exactly (0 edits).
            string text = "the quick brown fox";
            var results = RunSearch(text, "brown");

            Assert.Single(results);
            Assert.Single(results[0].Matches);
            Assert.Equal("brown", text.Substring(results[0].Matches[0].StartLocation, results[0].Matches[0].Length));
        }

        [Fact]
        public void ExactMatch_CaseInsensitive_IgnoresMixedCase()
        {
            // Pattern "brown" (lower) matches the token "Brown" (mixed) when case-insensitive.
            string text = "The quick Brown fox";
            var results = RunSearch(text, "brown");

            Assert.Single(results);
            Assert.Single(results[0].Matches);
            Assert.Equal("Brown", text.Substring(results[0].Matches[0].StartLocation, results[0].Matches[0].Length));
        }

        [Fact]
        public void ExactMatch_MultipleOccurrences_SameLine()
        {
            // Three tokens each matching "fox" exactly.
            string text = "fox and fox plus fox";
            var results = RunSearch(text, "fox");

            Assert.Single(results);
            Assert.Equal(3, results[0].Matches.Count);
        }

        [Fact]
        public void ExactMatch_MultipleOccurrences_MultipleLines()
        {
            // Without Multiline flag: processed line-by-line.
            // Both lines that contain "fox" contribute to the single GrepSearchResult.
            string text = "line one fox\nline two fox\nline three";
            var results = RunSearch(text, "fox");

            Assert.Single(results);
            Assert.Equal(2, results[0].Matches.Count);
        }

        // -------------------------------------------------------------------------
        // Exact match — multi-word pattern
        // -------------------------------------------------------------------------

        [Fact]
        public void ExactMatch_MultiWordPattern_BothTokensMustMatch()
        {
            // "brown fox" — two tokens, both must match consecutively.
            string text = "the quick brown fox jumps";
            var results = RunSearch(text, "brown fox");

            Assert.Single(results);
            Assert.Single(results[0].Matches);
            Assert.Equal("brown fox", text.Substring(results[0].Matches[0].StartLocation, results[0].Matches[0].Length));
        }

        [Fact]
        public void ExactMatch_MultiWordPattern_NoMatchWhenTokensNotConsecutive()
        {
            // "brown dog" — "brown" and "dog" are not consecutive tokens.
            string text = "the brown fox and the dog";
            var results = RunSearch(text, "brown dog");

            Assert.Empty(results);
        }

        // -------------------------------------------------------------------------
        // Case sensitivity
        // -------------------------------------------------------------------------

        [Fact]
        public void CaseSensitive_MatchesExactCaseOnly()
        {
            // With CaseSensitive, only the lowercase "fox" token should match.
            string text = "a Fox and a fox and a FOX";
            var results = RunSearch(text, "fox", GrepSearchOption.Global | GrepSearchOption.CaseSensitive);

            Assert.Single(results);
            Assert.Single(results[0].Matches);
            Assert.Equal("fox", text.Substring(results[0].Matches[0].StartLocation, results[0].Matches[0].Length));
        }

        [Fact]
        public void CaseSensitive_UppercasePatternMatchesUppercaseOnly()
        {
            string text = "a Fox and a fox and a FOX";
            var results = RunSearch(text, "FOX", GrepSearchOption.Global | GrepSearchOption.CaseSensitive);

            Assert.Single(results);
            Assert.Single(results[0].Matches);
            Assert.Equal("FOX", text.Substring(results[0].Matches[0].StartLocation, results[0].Matches[0].Length));
        }

        [Fact]
        public void CaseInsensitive_MatchesAllCaseForms()
        {
            // Without CaseSensitive, all three casing variants match.
            string text = "Fox fox FOX";
            var results = RunSearch(text, "fox");

            Assert.Single(results);
            Assert.Equal(3, results[0].Matches.Count);
        }

        [Fact]
        public void CaseSensitive_TypoMatch_OnlyMatchesSameCaseBase()
        {
            // "recieve" (typo, 1 edit from "receive", 7-char token, floor(0.2*7)=1 allowed).
            // CaseSensitive=true: "Recieve" (capital R) does not match "receive" (lower r).
            string text = "I recieve and Recieve letters";
            var results = RunSearch(text, "receive",
                GrepSearchOption.Global | GrepSearchOption.CaseSensitive, 0.8);

            Assert.Single(results);
            Assert.Single(results[0].Matches);
            // Only "recieve" (lowercase r) matches the lowercase pattern.
            Assert.Equal("recieve", text.Substring(results[0].Matches[0].StartLocation, results[0].Matches[0].Length));
        }

        // -------------------------------------------------------------------------
        // Whole word
        // -------------------------------------------------------------------------

        [Fact]
        public void WholeWord_ExcludesTokenThatExceedsEditBudget()
        {
            // "john" (4-char pattern): floor(0.3*4)=1 edit allowed.
            // "Johnson" costs 3 edits (3 extra chars) — exceeds the 1-edit budget. No match.
            // "John" costs 0 edits (case-insensitive). Matches as whole word.
            string text = "John is here. Johnson works here.";
            var results = RunSearch(text, "john",
                GrepSearchOption.Global | GrepSearchOption.WholeWord);

            Assert.Single(results);
            Assert.Single(results[0].Matches);
            Assert.Equal("John", text.Substring(results[0].Matches[0].StartLocation, results[0].Matches[0].Length));
        }

        [Fact]
        public void WholeWord_MatchesWordNextToPunctuation()
        {
            // "Hello" is adjacent to comma and full-stop but is still a whole word token.
            string text = "Hello, world! Hello.";
            var results = RunSearch(text, "hello",
                GrepSearchOption.Global | GrepSearchOption.WholeWord);

            Assert.Single(results);
            Assert.Equal(2, results[0].Matches.Count);
        }

        [Fact]
        public void WholeWord_MatchesAcrossLines_LineByLine()
        {
            // No Multiline flag: each line is searched separately.
            // "test" appears as a whole word in lines 1 and 2; "testing" is a longer token.
            string text = "test line\nanother test\njust testing";
            var results = RunSearch(text, "test",
                GrepSearchOption.Global | GrepSearchOption.WholeWord);

            Assert.Single(results);
            Assert.Equal(2, results[0].Matches.Count);
        }

        [Fact]
        public void NoWholeWord_MatchesFuzzyTokensWithoutBoundaryCheck()
        {
            // Without WholeWord, surrounding-character check is skipped.
            // "john" (4-char pattern): floor(0.3*4)=1 edit allowed.
            // "John" matches (0 edits). "Johny" matches (1 insertion). "Johnson" costs 3 ? no match.
            string text = "John is here. Johny was too. Johnson works.";
            var results = RunSearch(text, "john", GrepSearchOption.Global);

            Assert.Single(results);
            Assert.Equal(2, results[0].Matches.Count);
        }

        [Fact]
        public void WholeWord_CombinedWithCaseSensitive()
        {
            // Both flags: exact-case AND whole-word.
            // At threshold 1.0 (zero edits allowed), CaseSensitive means exact case only.
            // "Hello" costs 1 edit (H/h), "HELLO" costs 5 edits — both exceed the zero budget.
            // Only lowercase "hello" matches.
            string text = "hello Hello HELLO";
            var results = RunSearch(text, "hello",
                GrepSearchOption.Global | GrepSearchOption.WholeWord | GrepSearchOption.CaseSensitive,
                fuzzyThreshold: 1.0);

            Assert.Single(results);
            Assert.Single(results[0].Matches);
            Assert.Equal("hello", text.Substring(results[0].Matches[0].StartLocation, results[0].Matches[0].Length));
        }

        // -------------------------------------------------------------------------
        // Fuzzy / inexact matching
        // -------------------------------------------------------------------------

        [Fact]
        public void FuzzyMatch_OneTransposition_4CharToken_Threshold07()
        {
            // "tset" vs "test": 1 OSA edit (adjacent swap e/s).
            // 4-char token: floor((1-0.7) * 4) = floor(1.2) = 1 edit allowed.
            string text = "tset this";
            var results = RunSearch(text, "test", GrepSearchOption.Global, 0.7);

            Assert.Single(results);
            Assert.Single(results[0].Matches);
            Assert.Equal("tset", text.Substring(results[0].Matches[0].StartLocation, results[0].Matches[0].Length));
        }

        [Fact]
        public void FuzzyMatch_OneTransposition_5CharToken_Threshold08()
        {
            // "brwon" vs "brown": 1 OSA edit (transposition w/o).
            // 5-char token: floor((1-0.8) * 5) = floor(1.0) = 1 edit allowed.
            string text = "the brwon fox";
            var results = RunSearch(text, "brown", GrepSearchOption.Global, 0.8);

            Assert.Single(results);
            Assert.Single(results[0].Matches);
            Assert.Equal("brwon", text.Substring(results[0].Matches[0].StartLocation, results[0].Matches[0].Length));
        }

        [Fact]
        public void FuzzyMatch_OneTransposition_7CharToken_Threshold08()
        {
            // "recieve" vs "receive": 1 OSA edit (transposition ei/ie).
            // 7-char token: floor((1-0.8) * 7) = floor(1.4) = 1 edit allowed.
            string text = "I recieve the letter";
            var results = RunSearch(text, "receive", GrepSearchOption.Global, 0.8);

            Assert.Single(results);
            Assert.Single(results[0].Matches);
            Assert.Equal("recieve", text.Substring(results[0].Matches[0].StartLocation, results[0].Matches[0].Length));
        }

        [Fact]
        public void FuzzyMatch_OneDeletion_LongerToken()
        {
            // "programed" (9 chars) vs "programmed" (10 chars): 1 deletion.
            // floor((1-0.8) * 10) = floor(2.0) = 2 edits allowed. Should match.
            string text = "the programed version";
            var results = RunSearch(text, "programmed", GrepSearchOption.Global, 0.8);

            Assert.Single(results);
            Assert.Single(results[0].Matches);
            Assert.Equal("programed", text.Substring(results[0].Matches[0].StartLocation, results[0].Matches[0].Length));
        }

        [Fact]
        public void FuzzyMatch_MultiWordPattern_OneTypo()
        {
            // "brwon fox": "brwon" costs 1 edit (threshold 0.8, 5-char ? floor(0.2*5)=1 allowed); "fox" exact.
            string text = "the brwon fox jumps";
            var results = RunSearch(text, "brown fox", GrepSearchOption.Global, 0.8);

            Assert.Single(results);
            Assert.Single(results[0].Matches);
            Assert.Equal("brwon fox", text.Substring(results[0].Matches[0].StartLocation, results[0].Matches[0].Length));
        }

        [Fact]
        public void FuzzyMatch_MultiWordPattern_BothTokensTypo_LowerThreshold()
        {
            // "brwon fxo": "brwon" costs 1 edit (5-char, 0.8 ? floor(0.2*5)=1 allowed ?).
            // "fxo" vs "fox": 1 edit (transposition). 3-char at 0.8: floor(0.2*3)=0 — not enough.
            // Lower to 0.6: floor(0.4*3)=1 edit on 3-char tokens. ?
            string text = "the brwon fxo jumps";
            var results = RunSearch(text, "brown fox", GrepSearchOption.Global, 0.6);

            Assert.Single(results);
            Assert.Single(results[0].Matches);
            Assert.Equal("brwon fxo", text.Substring(results[0].Matches[0].StartLocation, results[0].Matches[0].Length));
        }

        [Fact]
        public void FuzzyMatch_MultiWordPattern_SecondTokenFails_NoMatch()
        {
            // "brown xyz": "xyz" costs 3 edits from "fox". At 0.6: floor(0.4*3)=1. 3 > 1.
            string text = "the brown xyz jumps";
            var results = RunSearch(text, "brown fox", GrepSearchOption.Global, 0.6);

            Assert.Empty(results);
        }

        // -------------------------------------------------------------------------
        // Threshold behaviour
        // -------------------------------------------------------------------------

        [Fact]
        public void Threshold_ExactOnly_AtThreshold10()
        {
            // Threshold 1.0: floor(0 * n) = 0 edits for all n. Only exact tokens match.
            // "tset" is 1 edit from "test" ? no match. Exact "test" ? matches.
            string text = "test tset testing";
            var results = RunSearch(text, "test", GrepSearchOption.Global, 1.0);

            Assert.Single(results);
            Assert.Single(results[0].Matches);
            Assert.Equal("test", text.Substring(results[0].Matches[0].StartLocation, results[0].Matches[0].Length));
        }

        [Fact]
        public void Threshold_StrictVsLoose_4CharToken()
        {
            // "tset" (4 chars, 1 edit from "test"):
            //   threshold 0.8: (1-0.8)*4 = 0.8 ? floor = 0 ? no match.
            //   threshold 0.7: (1-0.7)*4 = 1.2 ? floor = 1 ? match.
            string text = "tset is misspelled";

            var strict = RunSearch(text, "test", GrepSearchOption.Global, 0.8);
            var loose  = RunSearch(text, "test", GrepSearchOption.Global, 0.7);

            Assert.Empty(strict);
            Assert.Single(loose);
            Assert.Single(loose[0].Matches);
        }

        [Fact]
        public void Threshold_SingleCharToken_AlwaysRequiresExactMatch()
        {
            // 1-char token: floor((1-t)*1) = 0 for any t in (0,1].
            // Pattern "a": only the standalone word "a" matches; "apple" and "and" do not.
            string text = "a apple and";
            var results = RunSearch(text, "a", GrepSearchOption.Global, 0.5);

            Assert.Single(results);
            Assert.Single(results[0].Matches);
            Assert.Equal("a", text.Substring(results[0].Matches[0].StartLocation, results[0].Matches[0].Length));
        }

        [Fact]
        public void Threshold_TwoCharToken_EditAllowedAtLowThreshold()
        {
            // "an" (2 chars) vs "in": 1 substitution.
            //   threshold 0.4: floor(0.6*2) = 1 ? match.
            //   threshold 0.6: floor(0.4*2) = 0 ? no match.
            string text = "an example";

            var match   = RunSearch(text, "in", GrepSearchOption.Global, 0.4);
            var noMatch = RunSearch(text, "in", GrepSearchOption.Global, 0.6);

            Assert.Single(match);
            Assert.Empty(noMatch);
        }

        [Fact]
        public void Threshold_MultiWordBothTokensMustIndependentlyMatch()
        {
            // "brwon fox" at 0.8: "brwon" (5-char, 1 edit, floor(0.2*5)=1 ?), "fox" exact ? ? match.
            // "brown xyz" at 0.8: "xyz" costs 3 edits from "fox", floor(0.2*3)=0 ? no match.
            var r1 = RunSearch("the brwon fox", "brown fox", GrepSearchOption.Global, 0.8);
            var r2 = RunSearch("the brown xyz", "brown fox", GrepSearchOption.Global, 0.8);

            Assert.Single(r1);
            Assert.Empty(r2);
        }

        // -------------------------------------------------------------------------
        // Multiline mode
        // -------------------------------------------------------------------------

        [Fact]
        public void Multiline_False_SearchesLineByLine_AllMatchesCollected()
        {
            // No Multiline flag: each line is processed separately.
            // Both lines contain "fox"; all matches end up in one GrepSearchResult.
            string text = "brown fox\nred fox";
            var results = RunSearch(text, "fox", GrepSearchOption.Global);

            Assert.Single(results);
            Assert.Equal(2, results[0].Matches.Count);
        }

        [Fact]
        public void Multiline_False_PatternCannotSpanLines()
        {
            // Without Multiline, the two-token pattern "fox red" is tested on each line
            // separately. Neither line contains both consecutive tokens.
            string text = "brown fox\nred deer";
            var results = RunSearch(text, "fox red", GrepSearchOption.Global);

            Assert.Empty(results);
        }

        [Fact]
        public void Multiline_True_PatternCanSpanLines()
        {
            // With Multiline the entire file is one string.
            // Tokens "fox" and "red" are consecutive across the newline boundary.
            string text = "brown fox\nred deer";
            var results = RunSearch(text, "fox red",
                GrepSearchOption.Global | GrepSearchOption.Multiline);

            Assert.Single(results);
            Assert.Single(results[0].Matches);
            string matched = text.Substring(results[0].Matches[0].StartLocation, results[0].Matches[0].Length);
            Assert.Equal("fox\nred", matched);
        }

        [Fact]
        public void Multiline_True_FindsAllMatchesInDocument()
        {
            // All occurrences of "test" are found in one pass over the full document.
            string text = "first test here\nsecond test here\nno match here";
            var results = RunSearch(text, "test",
                GrepSearchOption.Global | GrepSearchOption.Multiline);

            Assert.Single(results);
            Assert.Equal(2, results[0].Matches.Count);
        }

        [Fact]
        public void Multiline_True_WithWholeWord()
        {
            // Whole-word check still applies in multiline mode.
            // "test" is whole word in lines 1 and 3; "testing" is a different longer token.
            string text = "test one\ntesting two\ntest three";
            var results = RunSearch(text, "test",
                GrepSearchOption.Global | GrepSearchOption.Multiline | GrepSearchOption.WholeWord);

            Assert.Single(results);
            Assert.Equal(2, results[0].Matches.Count);
        }

        [Fact]
        public void Multiline_True_WithCaseSensitive()
        {
            // Case-sensitive + multiline at threshold 1.0 (zero edits allowed):
            // "Test" costs 1 edit (T/t) and "TEST" costs 4 edits — both exceed the zero budget.
            // Only the exact-case "test" token matches.
            string text = "Test here\ntest here\nTEST here";
            var results = RunSearch(text, "test",
                GrepSearchOption.Global | GrepSearchOption.Multiline | GrepSearchOption.CaseSensitive,
                fuzzyThreshold: 1.0);

            Assert.Single(results);
            Assert.Single(results[0].Matches);
            string matched = text.Substring(results[0].Matches[0].StartLocation, results[0].Matches[0].Length);
            Assert.Equal("test", matched);
        }

        // -------------------------------------------------------------------------
        // Edge cases
        // -------------------------------------------------------------------------

        [Fact]
        public void EdgeCase_EmptyText_ReturnsNoResults()
        {
            Assert.Empty(RunSearch("", "test"));
        }

        [Fact]
        public void EdgeCase_EmptyPattern_ReturnsNoResults()
        {
            Assert.Empty(RunSearch("hello world", ""));
        }

        [Fact]
        public void EdgeCase_PatternHasMoreTokensThanText_NoMatch()
        {
            // "one two three four" has 4 tokens; text "cat" has 1 token.
            Assert.Empty(RunSearch("cat", "one two three four", GrepSearchOption.Global, 0.5));
        }

        [Fact]
        public void EdgeCase_TotallyDifferentToken_NoMatch()
        {
            // "xyz" vs "fox": 3 edits. At 0.5: floor(0.5*3)=1 edit allowed. 3 > 1 ? no match.
            Assert.Empty(RunSearch("the xyz jumps", "fox", GrepSearchOption.Global, 0.5));
        }

        [Fact]
        public void EdgeCase_NonWordPatternFallsBackToLiteralSearch()
        {
            // Pattern "@" has no \w characters ? literal IndexOf fallback.
            string text = "user@example.com";
            var results = RunSearch(text, "@");

            Assert.Single(results);
            Assert.Single(results[0].Matches);
        }

        [Fact]
        public void EdgeCase_WhitespaceOnlyPattern_NoResults()
        {
            // All-space pattern tokenises to zero tokens ? no match.
            Assert.Empty(RunSearch("hello world", "   "));
        }

        // -------------------------------------------------------------------------
        // Real-world / regression
        // -------------------------------------------------------------------------

        [Fact]
        public void Regression_JohnJames_TwoMatchesInText()
        {
            // "john" matches both "John" tokens; "James", "Erika", "Richard" don't match.
            string text = "Richard, John James & Erika\n\nJohn James\n";
            var results = RunSearch(text, "john", GrepSearchOption.Global);

            Assert.Single(results);
            Assert.Equal(2, results[0].Matches.Count);
            Assert.Equal("John", text.Substring(results[0].Matches[0].StartLocation, results[0].Matches[0].Length));
            Assert.Equal("John", text.Substring(results[0].Matches[1].StartLocation, results[0].Matches[1].Length));
        }

        [Fact]
        public void Regression_CaseSensitiveWholeWord_ExcludesWrongCaseAndPartialTokens()
        {
            // "test" + WholeWord + CaseSensitive at threshold 1.0 (zero edits allowed):
            //   "Test" costs 1 edit (T/t) ? exceeds zero budget ? no match.
            //   "test" (lines 2 and 4) ? exact case, whole word ? matches.
            //   "testing" ? longer token, extra chars ? no match.
            string text = "First Test\nSecond test\nThird testing\nFourth test";
            var results = RunSearch(text, "test",
                GrepSearchOption.Global | GrepSearchOption.WholeWord | GrepSearchOption.CaseSensitive,
                fuzzyThreshold: 1.0);

            Assert.Single(results);
            Assert.Equal(2, results[0].Matches.Count);
            foreach (var m in results[0].Matches)
                Assert.Equal("test", text.Substring(m.StartLocation, m.Length));
        }

        [Fact]
        public void Regression_TokenNotSubstring_PineappleDoesNotMatchApple()
        {
            // Token-based: "pineapple" is a single 9-char token.
            // OSA("apple","pineapple") = 4 insertions. At 0.7: floor(0.3*5)=1 allowed. 4 > 1.
            // So "pineapple" does not match pattern "apple".
            string text = "apple sauce\napple pie\npineapple juice";
            var results = RunSearch(text, "apple");

            Assert.Single(results);
            Assert.Equal(2, results[0].Matches.Count);
        }

        [Theory]
        [InlineData("fox", "the fox",          GrepSearchOption.Global,       0.7, 1)]
        [InlineData("fox", "The Fox",           GrepSearchOption.Global,       0.7, 1)]  // case-insensitive
        [InlineData("fox", "Fox fox FOX",       GrepSearchOption.Global,       0.7, 3)]  // all case forms
        [InlineData("fox", "fox foxy",          GrepSearchOption.WholeWord,    0.7, 1)]  // "foxy" costs 1 edit; pattern "fox" is 3 chars so floor(0.3*3)=0 edits allowed ? never matches
        [InlineData("fox", "fox FOX",           GrepSearchOption.CaseSensitive,0.7, 1)]  // case-sensitive: only "fox"
        [InlineData("fox", "fox FOX",           GrepSearchOption.Global,       0.7, 2)]  // case-insensitive: both
        [InlineData("test", "test tset",        GrepSearchOption.Global,       0.7, 2)]  // "tset" (1 edit, 4-char pattern: floor(0.3*4)=1 allowed)
        [InlineData("test", "test tset",        GrepSearchOption.Global,       1.0, 1)]  // threshold 1.0: only exact "test" matches
        public void Theory_VariousScenarios(
            string pattern, string text, GrepSearchOption options,
            double threshold, int expectedMatchCount)
        {
            var results = RunSearch(text, pattern, options, threshold);

            if (expectedMatchCount == 0)
                Assert.Empty(results);
            else
            {
                Assert.Single(results);
                Assert.Equal(expectedMatchCount, results[0].Matches.Count);
            }
        }
    }
}
