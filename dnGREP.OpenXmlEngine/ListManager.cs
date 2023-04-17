// Translated and converted from org.apache.tika.parser.microsoft.ooxml and org.apache.poi.hwpf.converter
// with some enhancements for bullets and indentation
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Wordprocessing;

namespace dnGREP.Engines.OpenXml
{
    public partial class ListManager
    {
        protected static List<KeyValuePair<string, string>> BulletSubstitutions { get; } = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>(char.ConvertFromUtf32(0xf0b7), char.ConvertFromUtf32(0x2022)),
            new KeyValuePair<string, string>(char.ConvertFromUtf32(0xf0a7), char.ConvertFromUtf32(0x221a)),
            new KeyValuePair<string, string>(char.ConvertFromUtf32(0xf0fc), char.ConvertFromUtf32(0x2192)),
            new KeyValuePair<string, string>(char.ConvertFromUtf32(0xf076), char.ConvertFromUtf32(0x2666)),
            new KeyValuePair<string, string>(char.ConvertFromUtf32(0xf0d8), char.ConvertFromUtf32(0x25ba)),
        };

        protected Dictionary<int, ParagraphLevelCounter> listLevelMap = new();
        protected Dictionary<int, LevelTuple[]> overrideTupleMap = new();

        protected partial class ParagraphLevelCounter
        {
            [GeneratedRegex("%(\\d+)")]
            private static partial Regex LevelRegex();

            //counts can == 0 if the format is decimal, make sure
            //that flag values are < 0
            private readonly int NOT_SEEN_YET = -1;
            private readonly int FIRST_SKIPPED = -2;
            private readonly LevelTuple[] levelTuples;
            private readonly List<int> counts = new();
            private int lastLevel = -1;

            public ParagraphLevelCounter(LevelTuple[] levelTuples)
            {
                this.levelTuples = levelTuples;
            }

            public int LevelCount
            {
                get { return levelTuples.Length; }
            }

            /// <summary>
            /// Apply this to every numbered paragraph in order.
            /// </summary>
            /// <param name="levelNumber">level number that is being incremented</param>
            /// <param name="overrideLevelTuples"></param>
            /// <returns>the new formatted number string for this level</returns>
            public string IncrementLevel(int levelNumber, LevelTuple[] overrideLevelTuples)
            {
                for (int i = lastLevel + 1; i < levelNumber; i++)
                {
                    if (i >= counts.Count)
                    {
                        int val = GetStart(i, overrideLevelTuples);
                        counts.Add(val);
                    }
                    else
                    {
                        int count = counts[i];
                        if (count == NOT_SEEN_YET)
                        {
                            count = GetStart(i, overrideLevelTuples);
                            counts[i] = count;
                        }
                    }
                }

                if (levelNumber < counts.Count)
                {
                    ResetAfter(levelNumber, overrideLevelTuples);
                    int count = counts[levelNumber];
                    if (count == NOT_SEEN_YET)
                    {
                        count = GetStart(levelNumber, overrideLevelTuples);
                    }
                    else
                    {
                        count++;
                    }
                    counts[levelNumber] = count;
                    lastLevel = levelNumber;
                    return Format(levelNumber, overrideLevelTuples);
                }
                else
                {
                    counts.Add(GetStart(levelNumber, overrideLevelTuples));
                    lastLevel = levelNumber;
                    return Format(levelNumber, overrideLevelTuples);
                }
            }

            private string Format(int level, LevelTuple[] overrideLevelTuples)
            {
                if (level < 0 || level >= levelTuples.Length)
                {
                    return string.Empty;
                }
                bool isLegal = overrideLevelTuples.Length > 0 ? overrideLevelTuples[level].IsLegal : levelTuples[level].IsLegal;

                string lvlText = overrideLevelTuples.Length > 0 || overrideLevelTuples[level].LevelText == null ?
                        levelTuples[level].LevelText : overrideLevelTuples[level].LevelText;

                string indent = overrideLevelTuples.Length > 0 ? levelTuples[level].Indent : overrideLevelTuples[level].Indent;

                //short circuit bullet
                string numFmt = GetNumFormat(level, isLegal, overrideLevelTuples);
                if ("Bullet".Equals(numFmt, StringComparison.Ordinal))
                {
                    string bullet = lvlText;
                    foreach (var kv in BulletSubstitutions)
                        bullet = bullet.Replace(kv.Key, kv.Value, StringComparison.Ordinal);
                    return indent + bullet + " ";
                }

                StringBuilder sb = new();

                Match m = LevelRegex().Match(lvlText);
                int last = 0;
                if (m.Success && m.Groups.Count > 1)
                {
                    var grp = m.Groups[1];
                    sb.Append(lvlText.AsSpan(0, m.Index));
                    string lvlString = grp.Value;
                    int lvlNum = -1;
                    if (int.TryParse(lvlString, out int num))
                        lvlNum = num;
                    //need to subtract 1 because, e.g. %1 is the format
                    //for the number at array offset 0
                    string numString = FormatNum(lvlNum - 1, isLegal, overrideLevelTuples);

                    sb.Append(numString);
                    last = m.Index + m.Length;
                }
                sb.Append(lvlText.AsSpan(last));
                if (sb.Length > 0)
                {
                    sb.Insert(0, indent);
                    sb.Append(' ');
                }
                return sb.ToString();
            }

            //actual level number; can return empty string if number formatter fails
            private string FormatNum(int lvlNum, bool isLegal, LevelTuple[] overrideLevelTuples)
            {
                NumberFmtStyle numFmtStyle = NumberFmtStyle.Arabic;
                string numFmt = GetNumFormat(lvlNum, isLegal, overrideLevelTuples);

                int count = GetCount(lvlNum);
                if (count < 0)
                {
                    count = 1;
                }
                if ("LowerLetter".Equals(numFmt, StringComparison.Ordinal))
                {
                    numFmtStyle = NumberFmtStyle.LowerLetter;
                }
                else if ("LowerRoman".Equals(numFmt, StringComparison.Ordinal))
                {
                    numFmtStyle = NumberFmtStyle.LowerRoman;
                }
                else if ("Decimal".Equals(numFmt, StringComparison.Ordinal))
                {
                    numFmtStyle = NumberFmtStyle.Arabic;
                }
                else if ("UpperLetter".Equals(numFmt, StringComparison.Ordinal))
                {
                    numFmtStyle = NumberFmtStyle.UpperLetter;
                }
                else if ("UpperRoman".Equals(numFmt, StringComparison.Ordinal))
                {
                    numFmtStyle = NumberFmtStyle.UpperRoman;
                }
                else if ("Ordinal".Equals(numFmt, StringComparison.Ordinal))
                {
                    return Ordinalize(count);
                }
                else if ("DecimalZero".Equals(numFmt, StringComparison.Ordinal))
                {
                    return "0" + NumberFormatter.GetNumber(count, NumberFmtStyle.Arabic);
                }
                else if ("None".Equals(numFmt, StringComparison.Ordinal))
                {
                    return string.Empty;
                }

                try
                {
                    return NumberFormatter.GetNumber(count, numFmtStyle);
                }
                catch (ArgumentException)
                {
                    return string.Empty;
                }
            }

            private static string Ordinalize(int count)
            {
                //this is only good for locale == English
                string countString = count.ToString();
                if (countString.EndsWith("1", StringComparison.Ordinal))
                {
                    return countString + "st";
                }
                else if (countString.EndsWith("2", StringComparison.Ordinal))
                {
                    return countString + "nd";
                }
                else if (countString.EndsWith("3", StringComparison.Ordinal))
                {
                    return countString + "rd";
                }
                return countString + "th";
            }

            private string GetNumFormat(int lvlNum, bool isLegal, LevelTuple[] overrideLevelTuples)
            {
                if (lvlNum < 0 || lvlNum >= levelTuples.Length)
                {
                    return "Decimal";
                }
                if (isLegal)
                {
                    //return decimal no matter the level if isLegal is true
                    return "Decimal";
                }
                return overrideLevelTuples.Length > 0 || overrideLevelTuples[lvlNum].NumFmt == null ?
                        levelTuples[lvlNum].NumFmt : overrideLevelTuples[lvlNum].NumFmt;
            }

            private int GetCount(int lvlNum)
            {
                if (lvlNum < 0 || lvlNum >= counts.Count)
                {
                    return 1;
                }
                return counts[lvlNum];
            }

            private void ResetAfter(int startlevelNumber, LevelTuple[] overrideLevelTuples)
            {
                for (int levelNumber = startlevelNumber + 1; levelNumber < counts.Count; levelNumber++)
                {
                    int cnt = counts[levelNumber];
                    if (cnt == NOT_SEEN_YET)
                    {
                        //do nothing
                    }
                    else if (cnt == FIRST_SKIPPED)
                    {
                        //do nothing
                    }
                    else if (levelTuples.Length > levelNumber)
                    {
                        //never reset if restarts == 0
                        int restart = overrideLevelTuples.Length > 0 || overrideLevelTuples[levelNumber].Restart < 0 ?
                                levelTuples[levelNumber].Restart : overrideLevelTuples[levelNumber].Restart;
                        if (restart == 0)
                        {
                            return;
                        }
                        else if (restart == -1 || startlevelNumber <= restart - 1)
                        {
                            counts[levelNumber] = NOT_SEEN_YET;
                        }
                        else
                        {
                            //do nothing/don't reset
                        }
                    }
                    else
                    {
                        //reset!
                        counts[levelNumber] = NOT_SEEN_YET;
                    }
                }
            }

            private int GetStart(int levelNumber, LevelTuple[] overrideLevelTuples)
            {
                if (levelNumber >= levelTuples.Length)
                {
                    return 1;
                }
                else
                {
                    return overrideLevelTuples.Length > 0 || overrideLevelTuples[levelNumber].Start < 0 ?
                            levelTuples[levelNumber].Start : overrideLevelTuples[levelNumber].Start;
                }
            }
        }

        protected class LevelTuple
        {
            internal int Start { get; private set; }
            internal int Restart { get; private set; }
            internal string LevelText { get; private set; }
            internal string NumFmt { get; private set; }
            internal bool IsLegal { get; private set; }
            internal string Indent { get; private set; }

            public LevelTuple(string lvlText)
            {
                LevelText = lvlText;
                Start = 1;
                Restart = -1;
                NumFmt = "Decimal";
                IsLegal = false;
                Indent = string.Empty;
            }

            public LevelTuple(int start, int restart, string lvlText, string numFmt, bool isLegal, string indent)
            {
                Start = start;
                Restart = restart;
                LevelText = lvlText;
                NumFmt = numFmt;
                IsLegal = isLegal;
                Indent = indent;
            }
        }
    }

    public class WordListManager : ListManager
    {
        private readonly Numbering numbering;

        public static WordListManager Empty { get; } = new(new());

        public WordListManager(Numbering numbering)
        {
            this.numbering = numbering;
        }

        public string GetFormattedNumber(Paragraph paragraph)
        {
            if (paragraph.ParagraphProperties?.NumberingProperties == null)
                return string.Empty;

            int numId = -1;
            if (paragraph.ParagraphProperties?.NumberingProperties.NumberingId?.Val?.HasValue is true)
                numId = paragraph.ParagraphProperties.NumberingProperties.NumberingId.Val.Value;

            int levelRef = -1;
            if (paragraph.ParagraphProperties?.NumberingProperties.NumberingLevelReference?.Val?.HasValue is true)
                levelRef = paragraph.ParagraphProperties.NumberingProperties.NumberingLevelReference.Val.Value;

            return GetFormattedNumber(numId, levelRef);
        }

        public string GetFormattedNumber(int numId, int iLvl)
        {
            if (numbering == null || iLvl < 0 || numId == -1)
            {
                return string.Empty;
            }

            var instances = numbering.Where(r => r is NumberingInstance).Select(r => r as NumberingInstance).ToList();
            var instance = instances.Where(r => r?.NumberID?.HasValue is true && r.NumberID.Value == numId).FirstOrDefault();

            if (instance == null || instance.AbstractNumId?.Val?.Value == null)
            {
                return string.Empty;
            }

            int currAbNumId = instance.AbstractNumId.Val.Value;

            var abstractNums = numbering.Where(r => r is AbstractNum).Select(r => r as AbstractNum);
            var abNum = abstractNums.Where(r => r?.AbstractNumberId?.HasValue is true && r.AbstractNumberId.Value == instance.AbstractNumId.Val.Value)
                .Select(r => r).FirstOrDefault();
            if (abNum == null)
            {
                return string.Empty;
            }

            if (!listLevelMap.TryGetValue(currAbNumId, out ParagraphLevelCounter? lc))
            {
                lc ??= LoadLevelTuples(abNum);
            }
            if (!overrideTupleMap.TryGetValue(numId, out LevelTuple[]? overrideTuples))
            {
                overrideTuples ??= LoadOverrideTuples(abNum, lc.LevelCount);
            }

            string formattedString = lc.IncrementLevel(iLvl, overrideTuples);

            listLevelMap[currAbNumId] = lc;
            overrideTupleMap[numId] = overrideTuples;

            return formattedString;
        }

        private static LevelTuple[] LoadOverrideTuples(AbstractNum abNum, int length)
        {
            var levels = abNum.ChildElements.Where(c => c is Level).Select(c => c as Level).ToArray();

            if (levels.Length == 0)
            {
                return Array.Empty<LevelTuple>();
            }

            LevelTuple[] levelTuples = new LevelTuple[length];
            for (int i = 0; i < length; i++)
            {
                LevelTuple tuple;
                if (i >= levels.Length)
                {
                    tuple = new LevelTuple("%" + i + ".");
                }
                else
                {
                    Level? level = levels[i];
                    if (level != null)
                    {
                        tuple = BuildTuple(i, level);
                    }
                    else
                    {
                        tuple = new LevelTuple("%" + i + ".");
                    }
                }
                levelTuples[i] = tuple;
            }
            return levelTuples;
        }

        private static ParagraphLevelCounter LoadLevelTuples(AbstractNum abNum)
        {
            //Unfortunately, we need to go this far into the underlying structure
            //to get the abstract num information for the edge case where
            //someone skips a level and the format is not context-free, e.g. "1.B.i".

            var lvlArray = abNum.ChildElements.Where(c => c is Level).Select(c => c as Level).ToArray();

            LevelTuple[] levels = new LevelTuple[lvlArray.Length];
            for (int i = 0; i < levels.Length; i++)
            {
                Level? level = lvlArray[i];
                if (level != null)
                {
                    levels[i] = BuildTuple(i, level);
                }
            }
            return new ParagraphLevelCounter(levels);
        }

        private static LevelTuple BuildTuple(int index, Level level)
        {
            bool isLegal = false;
            int start;
            int restart = -1;
            string lvlText = "%" + index + ".";
            string numFmt = "decimal";
            string indent = string.Empty;


            if (level.IsLegalNumberingStyle?.Val?.HasValue is true && level.IsLegalNumberingStyle.Val.Value)
            {
                isLegal = true;
            }

            if (level.NumberingFormat?.Val?.HasValue is true)
            {
                numFmt = level.NumberingFormat.Val.Value.ToString();
            }

            if (level.LevelRestart?.Val?.HasValue is true)
            {
                restart = level.LevelRestart.Val.Value;
            }

            if (level.PreviousParagraphProperties != null && level.PreviousParagraphProperties.Indentation != null)
            {
                var indentation = level.PreviousParagraphProperties.Indentation;
                if (indentation.Left?.HasValue is true)
                    indent = TwipsToSpaces(indentation.Left.Value);
                else if (indentation.Start?.HasValue is true)
                    indent = TwipsToSpaces(indentation.Start.Value);
            }

            if (level.StartNumberingValue?.Val?.HasValue is true)
            {
                start = level.StartNumberingValue.Val.Value;
            }
            else
            {
                //this is a hack. Currently, this gets the lowest possible
                //start for a given numFmt.  We should probably try to grab the
                //restartNumberingAfterBreak value in
                //e.g. <w:abstractNum w:abstractNumId="12" w15:restartNumberingAfterBreak="0">???
                if ("decimal".Equals(numFmt, StringComparison.Ordinal) || "ordinal".Equals(numFmt, StringComparison.Ordinal) || "decimalZero".Equals(numFmt, StringComparison.Ordinal))
                {
                    start = 0;
                }
                else
                {
                    start = 1;
                }
            }
            if (level.LevelText?.Val?.HasValue is true)
            {
                lvlText = level.LevelText.Val.Value ?? string.Empty;
            }
            return new LevelTuple(start, restart, lvlText, numFmt, isLegal, indent);
        }

        internal static string TwipsToSpaces(string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                if (int.TryParse(value, out int num))
                {
                    // 1440 twips = 1 inch
                    // make 1 inch == 6 spaces
                    int spaces = Math.Max(1, num / 240);

                    return new string(' ', spaces);
                }
            }
            return string.Empty;
        }
    }

    public enum NumberFmtStyle
    {
        Arabic = 0,
        UpperRoman,
        LowerRoman,
        UpperLetter,
        LowerLetter,
        Ordinal,
    }

    public static class NumberFormatter
    {
        private static readonly string[] ROMAN_LETTERS = { "m", "cm", "d", "cd", "c",
            "xc", "l", "xl", "x", "ix", "v", "iv", "i" };

        private static readonly int[] ROMAN_VALUES = { 1000, 900, 500, 400, 100, 90,
            50, 40, 10, 9, 5, 4, 1 };

        public static string GetNumber(int num, NumberFmtStyle style)
        {
            return style switch
            {
                NumberFmtStyle.UpperRoman => ToRoman(num).ToUpper(CultureInfo.CurrentCulture),
                NumberFmtStyle.LowerRoman => ToRoman(num),
                NumberFmtStyle.UpperLetter => ToLetters(num).ToUpper(CultureInfo.CurrentCulture),
                NumberFmtStyle.LowerLetter => ToLetters(num),
                _ => num.ToString(CultureInfo.CurrentCulture),
            };
        }

        private static string ToLetters(int number)
        {
            if (number <= 0)
                throw new ArgumentException("Unsupported number: " + number);

            int num = number;
            int radix = 26;

            char[] buf = new char[33];
            int charPos = buf.Length;

            while (num > 0)
            {
                num--; // 1 => a, not 0 => a
                int remainder = num % radix;
                buf[--charPos] = (char)('a' + remainder);
                num = (num - remainder) / radix;
            }

            return new string(buf, charPos, (buf.Length - charPos));
        }

        private static string ToRoman(int number)
        {
            if (number <= 0)
                throw new ArgumentException("Unsupported number: " + number);

            StringBuilder result = new();

            for (int i = 0; i < ROMAN_LETTERS.Length; i++)
            {
                string letter = ROMAN_LETTERS[i];
                int value = ROMAN_VALUES[i];
                while (number >= value)
                {
                    number -= value;
                    result.Append(letter);
                }
            }
            return result.ToString().PadLeft(4);
        }
    }
}
