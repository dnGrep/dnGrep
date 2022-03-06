using System;

namespace dnGREP.Common
{
    [Flags]
    public enum TokenType
    {
        NotDefined = 0,
        StringValue = 1,
        OpenParens = 2,
        CloseParens = 4,
        Parenthesis = OpenParens | CloseParens,
        NOT = 8,
        AND = 16,
        NAND = 32,
        OR = 64,
        NOR = 128,
        Operator = NOT | AND | NAND | OR | NOR,
    }
}