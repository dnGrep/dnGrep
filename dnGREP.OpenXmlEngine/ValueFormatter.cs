using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnGREP.Common;
using DocumentFormat.OpenXml.Wordprocessing;

namespace dnGREP.Engines.OpenXml
{
    internal static class ValueFormatter
    {
        public static string FormatValue(long value, NumberFormatValues format, FootnoteRefType refType)
        {
            if (refType == FootnoteRefType.None)
            {
                return string.Empty;
            }

            string s = Format(value, format);
            if (format == NumberFormatValues.Chicago)
            {
                return s;
            }
            else if (refType == FootnoteRefType.Superscript)
            {
                return ToSuperscript(s);
            }
            else if (refType == FootnoteRefType.Parenthesis)
            {
                return $"({s})";
            }
            return s;
        }

        internal static string FormatValue(string value, CommentRefType refType)
        {
            if (refType == CommentRefType.None)
            {
                return string.Empty;
            }

            if (refType == CommentRefType.Subscript)
            {
                return $"₍{ToSubscript(value)}₎";
            }

            return $"({value})";
        }

        private static string Format(long value, NumberFormatValues format)
        {
            return format switch
            {
                NumberFormatValues.UpperRoman => ToRoman((int)value),
                NumberFormatValues.LowerRoman => ToRoman((int)value).ToLowerInvariant(),
                NumberFormatValues.UpperLetter => ToLetter((int)value),
                NumberFormatValues.LowerLetter => ToLetter((int)value).ToLowerInvariant(),
                NumberFormatValues.Chicago => ToChicago((int)value),
                _ => value.ToString(),
            };
        }

        private readonly static char[] chicagoChars = new char[] { '*', '†', '‡', '§' };

        private static string ToChicago(int num)
        {
            int idx = (num - 1) % 4;
            int count = 1 + (num - 1) / 4;
            return new string(chicagoChars[idx], count);
        }

        private static string ToLetter(int num)
        {
            char offset = (char)((num - 1) % 26);
            int count = 1 + (num - 1) / 26;
            return new string((char)(65 + offset), count);
        }

        private static string ToRoman(int number)
        {
            if ((number < 0) || (number > 3999)) throw new ArgumentOutOfRangeException(nameof(number), "insert value between 1 and 3999");
            if (number < 1) return string.Empty;
            if (number >= 1000) return "M" + ToRoman(number - 1000);
            if (number >= 900) return "CM" + ToRoman(number - 900);
            if (number >= 500) return "D" + ToRoman(number - 500);
            if (number >= 400) return "CD" + ToRoman(number - 400);
            if (number >= 100) return "C" + ToRoman(number - 100);
            if (number >= 90) return "XC" + ToRoman(number - 90);
            if (number >= 50) return "L" + ToRoman(number - 50);
            if (number >= 40) return "XL" + ToRoman(number - 40);
            if (number >= 10) return "X" + ToRoman(number - 10);
            if (number >= 9) return "IX" + ToRoman(number - 9);
            if (number >= 5) return "V" + ToRoman(number - 5);
            if (number >= 4) return "IV" + ToRoman(number - 4);
            if (number >= 1) return "I" + ToRoman(number - 1);
            throw new UnreachableException("Impossible state reached");
        }

        public static string ToSuperscript(string s)
        {
            return string.Create(s.Length, s, (cc, s) =>
            {
                s.CopyTo(cc);
                for (int i = 0; i < cc.Length; i++)
                {
                    if (superscripts.TryGetValue(cc[i], out char ss))
                    {
                        cc[i] = ss;
                    }
                }
            });
        }

        public static string ToSubscript(string s)
        {
            return string.Create(s.Length, s, (cc, s) =>
            {
                s.CopyTo(cc);
                for (int i = 0; i < cc.Length; i++)
                {
                    if (subscripts.TryGetValue(cc[i], out char ss))
                    {
                        cc[i] = ss;
                    }
                }
            });
        }

        private static readonly Dictionary<char, char> subscripts = new(10)
        {
            {'0', '₀'},
            {'1', '₁'},
            {'2', '₂'},
            {'3', '₃'},
            {'4', '₄'},
            {'5', '₅'},
            {'6', '₆'},
            {'7', '₇'},
            {'8', '₈'},
            {'9', '₉'},
        };

        private static readonly Dictionary<char, char> superscripts = new(64)
        {
            {'0', '⁰'},
            {'1', '¹'},
            {'2', '²'},
            {'3', '³'},
            {'4', '⁴'},
            {'5', '⁵'},
            {'6', '⁶'},
            {'7', '⁷'},
            {'8', '⁸'},
            {'9', '⁹'},
            {'A', 'ᴬ' },
            {'B', 'ᴮ' },
            {'C', 'ꟲ' },
            {'D', 'ᴰ' },
            {'E', 'ᴱ' },
            {'F', 'ꟳ' },
            {'G', 'ᴳ' },
            {'H', 'ᴴ' },
            {'I', 'ᴵ' },
            {'J', 'ᴶ' },
            {'K', 'ᴷ' },
            {'L', 'ᴸ' },
            {'M', 'ᴹ' },
            {'N', 'ᴺ' },
            {'O', 'ᴼ' },
            {'P', 'ᴾ' },
            {'Q', 'ꟴ' },
            {'R', 'ᴿ' },
            {'S', 'ˢ' },
            {'T', 'ᵀ' },
            {'U', 'ᵁ' },
            {'V', 'ⱽ' },
            {'W', 'ᵂ' },
            {'X', 'ᵡ' },
            {'Y', 'ʸ' },
            {'Z', 'ᙆ' },
            {'a', 'ᵃ' },
            {'b', 'ᵇ' },
            {'c', 'ᶜ' },
            {'d', 'ᵈ' },
            {'e', 'ᵉ' },
            {'f', 'ᶠ' },
            {'g', 'ᵍ' },
            {'h', 'ʰ' },
            {'i', 'ⁱ' },
            {'j', 'ʲ' },
            {'k', 'ᵏ' },
            {'l', 'ˡ' },
            {'m', 'ᵐ' },
            {'n', 'ⁿ' },
            {'o', 'ᵒ' },
            {'p', 'ᵖ' },
            {'q', 'ꟴ' },
            {'r', 'ʳ' },
            {'s', 'ˢ' },
            {'t', 'ᵗ' },
            {'u', 'ᵘ' },
            {'v', 'ᵛ' },
            {'w', 'ʷ' },
            {'x', 'ˣ' },
            {'y', 'ʸ' },
            {'z', 'ᶻ' },
        };

    }
}
