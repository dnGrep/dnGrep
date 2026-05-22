using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using dnGREP.Common;

namespace dnGREP.Engines
{
    /// <summary>
    /// Token-aware fuzzy matcher. The pattern is split into words; a match requires every
    /// pattern token to fuzzy-match a consecutive word in the text within a per-token
    /// edit-distance tolerance derived from <see cref="Match_Threshold"/>. This gives
    /// meaningful typo tolerance for multi-word patterns (e.g. "brown fox") without the
    /// false positives that arise from character-level sliding-window approaches.
    ///
    /// Threshold semantics (0.0–1.0, higher = stricter):
    ///   1.0  → exact match only (0 edits allowed per token)
    ///   0.8  → 1 edit allowed per 5-char token  (floor((1-0.8)*5) = 1)
    ///   0.6  → 1 edit per 2–3 chars (e.g. "recieve" matches "receive")
    ///   0.5  → roughly 1 edit per 2 chars
    /// </summary>
    public partial class FuzzyMatch
    {
        private double match_Threshold = 0.7;

        /// <summary>
        /// Gets or sets the match threshold (0.0–1.0). 1.0 = exact only; lower values
        /// permit more edit operations per token.
        /// </summary>
        public double Match_Threshold
        {
            get => match_Threshold;
            set => match_Threshold = Math.Clamp(value, 0.0, 1.0);
        }

        /// <summary>
        /// Gets or sets whether matching is case-sensitive. Default is false.
        /// </summary>
        public bool IsCaseSensitive { get; set; } = false;

        // Matches runs of word characters (\w+).
        private static readonly Regex WordPattern = WordPatternRegex();

        // -- public API called by GrepEngineBase ----------------------------------

        /// <summary>
        /// Finds the start character position of the next fuzzy match for
        /// <paramref name="pattern"/> in <paramref name="text"/>.
        /// <paramref name="loc"/> is accepted for interface compatibility but is unused —
        /// callers always pass a pre-sliced string with loc=0.
        /// Returns -1 if no match is found.
        /// </summary>
        public int MatchMain(string text, string pattern, int loc, bool isWholeWord = false)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern))
                return -1;

            string[] patternTokens = Tokenize(pattern);

            // Pattern has no word characters (e.g. "@", "://") — fall back to literal search.
            if (patternTokens.Length == 0)
            {
                var comp = IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                return text.IndexOf(pattern, comp);
            }

            List<TextWord> textWords = GetTextWords(text);
            if (textWords.Count < patternTokens.Length)
                return -1;

            int limit = textWords.Count - patternTokens.Length;
            for (int wi = 0; wi <= limit; wi++)
            {
                if (TryMatchAt(text, textWords, wi, patternTokens, isWholeWord, out int spanStart, out _))
                    return spanStart;
            }

            return -1;
        }

        /// <summary>
        /// Given that a fuzzy match starts at character position <paramref name="loc"/> in
        /// <paramref name="text"/>, returns the character length of the matched span.
        /// Returns -1 if no valid match is found at that position.
        /// </summary>
        public int MatchLengthInstance(string text, string pattern, int loc, bool isWholeWord, double threshold)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern))
                return 0;

            string[] patternTokens = Tokenize(pattern);

            // Pattern has no word characters — literal match, length is always pattern.Length.
            if (patternTokens.Length == 0)
            {
                if (loc + pattern.Length > text.Length) return -1;
                var comp = IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                return text.Substring(loc, pattern.Length).Equals(pattern, comp) ? pattern.Length : -1;
            }

            List<TextWord> textWords = GetTextWords(text);
            if (textWords.Count < patternTokens.Length)
                return -1;

            // Find the text-word index whose start position equals or is nearest to loc.
            int wi = FindWordIndexAtOrAfter(textWords, loc);
            if (wi == -1 || wi > textWords.Count - patternTokens.Length)
                return -1;

            if (TryMatchAt(text, textWords, wi, patternTokens, isWholeWord, out int spanStart, out int spanEnd))
                return spanEnd - spanStart;

            return -1;
        }

        // -- core matching --------------------------------------------------------

        /// <summary>
        /// Tries to match all <paramref name="patternTokens"/> against consecutive text
        /// words starting at word index <paramref name="wi"/>. Pattern token[i] must match
        /// textWords[wi+i] within the allowed edit distance. The matched span covers from
        /// the start of the first word to the end of the last word.
        /// </summary>
        private bool TryMatchAt(
            string text,
            List<TextWord> textWords,
            int wi,
            string[] patternTokens,
            bool isWholeWord,
            out int spanStart,
            out int spanEnd)
        {
            spanStart = spanEnd = 0;

            for (int ti = 0; ti < patternTokens.Length; ti++)
            {
                string patNorm = Normalize(patternTokens[ti]);
                string txtNorm = Normalize(textWords[wi + ti].Word);

                int allowed = AllowedEdits(patternTokens[ti].Length);
                if (OsaDistance(patNorm, txtNorm) > allowed)
                    return false;
            }

            spanStart = textWords[wi].Start;
            spanEnd = textWords[wi + patternTokens.Length - 1].End;

            if (isWholeWord && !IsValidWholeWordMatch(text, spanStart, spanEnd - spanStart))
                return false;

            return true;
        }

        // -- helpers ------------------------------------------------------------?

        /// <summary>
        /// Maximum number of edit operations allowed for a token of
        /// <paramref name="tokenLength"/> characters at the current threshold.
        /// Derived as floor((1 - threshold) * tokenLength).
        /// Examples at threshold 0.8: 4-char → 0, 5-char → 1, 7-char → 1.
        /// Examples at threshold 0.7: 3-char → 0, 4-char → 1, 7-char → 2.
        /// Examples at threshold 0.6: 3-char → 1, 5-char → 2.
        /// </summary>
        private int AllowedEdits(int tokenLength)
        {
            // Round to 10 significant decimal places before flooring to avoid binary
            // floating-point artifacts where a "nice" threshold such as 0.8 cannot be
            // represented exactly (1.0 - 0.8 evaluates to 0.19999...96 rather than 0.2,
            // so without rounding, (1.0-0.8)*5 = 0.9999...978 and floor gives 0 instead of 1).
            double tolerance = Math.Round(1.0 - Match_Threshold, 10);
            return (int)Math.Floor(tolerance * tokenLength);
        }

        private string Normalize(string s) =>
            IsCaseSensitive ? s : s.ToLowerInvariant();

        private static string[] Tokenize(string input)
        {
            var matches = WordPattern.Matches(input);
            var tokens = new string[matches.Count];
            for (int i = 0; i < matches.Count; i++)
                tokens[i] = matches[i].Value;
            return tokens;
        }

        private static List<TextWord> GetTextWords(string text)
        {
            var list = new List<TextWord>();
            foreach (Match m in WordPattern.Matches(text))
                list.Add(new TextWord(m.Value, m.Index, m.Index + m.Length));
            return list;
        }

        /// <summary>
        /// Returns the index of the first word whose <see cref="TextWord.Start"/> is
        /// greater than or equal to <paramref name="loc"/>, or -1 if none.
        /// </summary>
        private static int FindWordIndexAtOrAfter(List<TextWord> words, int loc)
        {
            for (int i = 0; i < words.Count; i++)
            {
                if (words[i].Start >= loc)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Optimal String Alignment distance (restricted Damerau-Levenshtein).
        /// Counts insertions, deletions, substitutions, and transpositions of adjacent
        /// characters, each at cost 1. This means common adjacent-swap typos such as
        /// "teh"/"the", "recieve"/"receive", and "brwon"/"brown" each cost 1 edit.
        /// Uses an (|a|+1)×(|b|+1) DP matrix — O(|a|·|b|) time and space.
        /// </summary>
        private static int OsaDistance(string a, string b)
        {
            if (a.Length == 0) return b.Length;
            if (b.Length == 0) return a.Length;

            int[,] d = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) d[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(
                        d[i - 1, j] + 1,
                        d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);

                    // Transposition of two adjacent characters.
                    if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                        d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1);
                }
            }

            return d[a.Length, b.Length];
        }

        private bool IsValidWholeWordMatch(string text, int loc, int length)
        {
            if (loc > 0 && !Utils.IsValidBeginText(text[..loc]))
                return false;
            if (loc + length < text.Length && !Utils.IsValidEndText(text[(loc + length)..]))
                return false;
            return true;
        }

        // -- value type ----------------------------------------------------------?

        private readonly struct TextWord(string word, int start, int end)
        {
            public string Word { get; } = word;
            public int Start { get; } = start;    // inclusive
            public int End { get; } = end;         // exclusive
        }

        [GeneratedRegex(@"\w+", RegexOptions.Compiled)]
        private static partial Regex WordPatternRegex();
    }
}
