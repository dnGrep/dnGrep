using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace dnGREP.Common
{
    public class Token(TokenType tokenType, string regexPattern, int precedence)
    {
        private readonly Regex regex = new(regexPattern, RegexOptions.IgnoreCase);

        public TokenType TokenType { get; private set; } = tokenType;

        public int Precedence { get; private set; } = precedence;

        public IEnumerable<TokenMatch> FindMatches(string inputString)
        {
            var matches = regex.Matches(inputString);
            for (int i = 0; i < matches.Count; i++)
            {
                yield return new TokenMatch()
                {
                    StartIndex = matches[i].Index,
                    EndIndex = matches[i].Index + matches[i].Length,
                    TokenType = TokenType,
                    Precedence = Precedence,
                    Value = matches[i].Groups.Count == 2 ? matches[i].Groups[1].Value : matches[i].Value,
                };
            }
        }
    }
}