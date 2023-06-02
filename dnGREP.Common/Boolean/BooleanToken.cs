using System.Collections.Generic;

namespace dnGREP.Common
{
    public class BooleanToken
    {
        public BooleanToken(TokenType tokenType, string value, int precedence)
        {
            TokenType = tokenType;
            Precedence = precedence;
            Value = value;
        }

        public TokenType TokenType { get; private set; }

        public int Precedence { get; private set; }

        public string Value { get; private set; }

        public bool IsOperator => TokenType.Operator.HasFlag(TokenType);
        public bool IsLowerPrecedence(BooleanToken other)
        {
            if (IsOperator && other != null && other.IsOperator)
            {
                return Precedence < other.Precedence;
            }
            return false;
        }

        public bool IsParenthesis => TokenType.Parenthesis.HasFlag(TokenType);

        public bool IsOperand => TokenType == TokenType.StringValue;

        public bool? EvaluatedResult { get; set; }

        public List<GrepMatch>?  Matches { get; set; }

        public override string ToString()
        {
            return $"{TokenType}: {Value}";
        }
    }
}