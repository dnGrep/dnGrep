namespace dnGREP.Common
{
    public class TokenMatch
    {
        public TokenType TokenType { get; set; }
        public string Value { get; set; } = string.Empty;
        public int Precedence { get; set; }
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
    }
}