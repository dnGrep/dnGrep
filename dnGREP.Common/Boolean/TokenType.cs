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
        XOR = 64,
        OR = 128,
        NOR = 256,
        Operator = NOT | AND | NAND | XOR | OR | NOR,
    }
}