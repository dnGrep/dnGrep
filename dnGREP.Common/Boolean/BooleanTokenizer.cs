using System.Collections.Generic;
using System.Linq;

namespace dnGREP.Common
{
    public class BooleanTokenizer
    {
        // A || B && C       means  A || (B && C)
        // A && B || C && D  means  (A && B) || (C && D)
        // A && B && C || D  means  ((A && B) && C) || D
        // !A && B || C      means  ((!A) && B) || C

        private readonly List<Token> tokenDefinitions =
        [
            new(TokenType.NOT, @"\bnot\b", 6), // highest precedence
            new(TokenType.AND, @"\band\b", 5),
            new(TokenType.NAND, @"\bnand\b", 4),
            new(TokenType.XOR, @"\bxor\b", 3),
            new(TokenType.OR, @"\bor\b", 2),
            new(TokenType.NOR, @"\bnor\b", 1),
            new(TokenType.CloseParens, @"\)", 0),
            new(TokenType.OpenParens, @"\(", 0),
            new(TokenType.StringValue, @"`([^`]*)`", 0),
            new(TokenType.StringValue, @"<([^`]*?)>", 0),
        ];

        public IEnumerable<BooleanToken> Tokenize(string input)
        {
            var tokenMatches = FindTokenMatches(input);

            var groupedByIndex = tokenMatches.GroupBy(x => x.StartIndex)
                .OrderBy(x => x.Key)
                .ToList();

            int currentIndex = 0;
            TokenMatch? lastMatch = null;
            for (int i = 0; i < groupedByIndex.Count; i++)
            {
                var bestMatch = groupedByIndex[i].First();
                if (lastMatch != null && bestMatch.StartIndex < lastMatch.EndIndex)
                    continue;

                if (bestMatch.StartIndex > currentIndex)
                {
                    string value = input[currentIndex..bestMatch.StartIndex].Trim();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        yield return new BooleanToken(TokenType.StringValue, value, 0);
                    }
                }
                currentIndex = bestMatch.EndIndex;

                yield return new BooleanToken(bestMatch.TokenType, bestMatch.Value, bestMatch.Precedence);

                lastMatch = bestMatch;
            }

            if (currentIndex < input.Length)
            {
                string value = input[currentIndex..].Trim();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    yield return new BooleanToken(TokenType.StringValue, value, 0);
                }
            }
        }

        private List<TokenMatch> FindTokenMatches(string input)
        {
            List<TokenMatch> tokenMatches = [];

            foreach (var tokenDefinition in tokenDefinitions)
                tokenMatches.AddRange(tokenDefinition.FindMatches(input).ToList());

            return tokenMatches;
        }
    }
}
