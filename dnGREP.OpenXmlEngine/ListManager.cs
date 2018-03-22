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
    public class ListManager
    {
        protected static List<KeyValuePair<string, string>> bulletSubstitutions = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>(char.ConvertFromUtf32(0xf0b7), char.ConvertFromUtf32(0x2022)),
            new KeyValuePair<string, string>(char.ConvertFromUtf32(0xf0a7), char.ConvertFromUtf32(0x221a)),
            new KeyValuePair<string, string>(char.ConvertFromUtf32(0xf0fc), char.ConvertFromUtf32(0x2192)),
            new KeyValuePair<string, string>(char.ConvertFromUtf32(0xf076), char.ConvertFromUtf32(0x2666)),
            new KeyValuePair<string, string>(char.ConvertFromUtf32(0xf0d8), char.ConvertFromUtf32(0x25ba)),
        };

        protected Dictionary<int, ParagraphLevelCounter> listLevelMap = new Dictionary<int, ParagraphLevelCounter>();
        protected Dictionary<int, LevelTuple[]> overrideTupleMap = new Dictionary<int, LevelTuple[]>();

        protected class ParagraphLevelCounter
        {
            //counts can == 0 if the format is decimal, make sure
            //that flag values are < 0
            private readonly int NOT_SEEN_YET = -1;
            private readonly int FIRST_SKIPPED = -2;
            private readonly LevelTuple[] levelTuples;
            private readonly Regex LEVEL_INTERPOLATOR = new Regex(@"%(\d+)", RegexOptions.Compiled);
            private readonly List<int> counts = new List<int>();
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
                bool isLegal = (overrideLevelTuples != null) ? overrideLevelTuples[level].IsLegal : levelTuples[level].IsLegal;

                string lvlText = (overrideLevelTuples == null || overrideLevelTuples[level].LevelText == null) ?
                        levelTuples[level].LevelText : overrideLevelTuples[level].LevelText;

                string indent = overrideLevelTuples == null ? levelTuples[level].Indent : overrideLevelTuples[level].Indent;

                //short circuit bullet
                string numFmt = GetNumFormat(level, isLegal, overrideLevelTuples);
                if ("Bullet".Equals(numFmt))
                {
                    string bullet = lvlText;
                    foreach (var kv in bulletSubstitutions)
                        bullet = bullet.Replace(kv.Key, kv.Value);
                    return indent + bullet + " ";
                }

                StringBuilder sb = new StringBuilder();

                Match m = LEVEL_INTERPOLATOR.Match(lvlText);
                int last = 0;
                if (m.Success && m.Groups.Count > 1)
                {
                    var grp = m.Groups[1];
                    sb.Append(lvlText.Substring(0, m.Index));
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
                sb.Append(lvlText.Substring(last));
                if (sb.Length > 0)
                {
                    sb.Insert(0, indent);
                    sb.Append(" ");
                }
                return sb.ToString();
            }

            //actual level number; can return empty string if number formatter fails
            private string FormatNum(int lvlNum, bool isLegal, LevelTuple[] overrideLevelTuples)
            {
                int numFmtStyle = 0;
                string numFmt = GetNumFormat(lvlNum, isLegal, overrideLevelTuples);

                int count = GetCount(lvlNum);
                if (count < 0)
                {
                    count = 1;
                }
                if ("LowerLetter".Equals(numFmt))
                {
                    numFmtStyle = 4;
                }
                else if ("LowerRoman".Equals(numFmt))
                {
                    numFmtStyle = 2;
                }
                else if ("Decimal".Equals(numFmt))
                {
                    numFmtStyle = 0;
                }
                else if ("UpperLetter".Equals(numFmt))
                {
                    numFmtStyle = 3;
                }
                else if ("UpperRoman".Equals(numFmt))
                {
                    numFmtStyle = 1;
                }
                else if ("Ordinal".Equals(numFmt))
                {
                    return Ordinalize(count);
                }
                else if ("DecimalZero".Equals(numFmt))
                {
                    return "0" + NumberFormatter.GetNumber(count, 0);
                }
                else if ("None".Equals(numFmt))
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

            private string Ordinalize(int count)
            {
                //this is only good for locale == English
                string countString = count.ToString();
                if (countString.EndsWith("1"))
                {
                    return countString + "st";
                }
                else if (countString.EndsWith("2"))
                {
                    return countString + "nd";
                }
                else if (countString.EndsWith("3"))
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
                return (overrideLevelTuples == null || overrideLevelTuples[lvlNum].NumFmt == null) ?
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
                        int restart = (overrideLevelTuples == null || overrideLevelTuples[levelNumber].Restart < 0) ?
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
                    return (overrideLevelTuples == null || overrideLevelTuples[levelNumber].Start < 0) ?
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
        private Numbering numbering;

        public static WordListManager Empty = new WordListManager(null);

        public WordListManager(Numbering numbering)
        {
            this.numbering = numbering;
        }

        public string GetFormattedNumber(Paragraph paragraph)
        {
            if (paragraph == null || paragraph.ParagraphProperties == null || paragraph.ParagraphProperties.NumberingProperties == null)
                return string.Empty;

            int numId = -1;
            if (paragraph.ParagraphProperties.NumberingProperties.NumberingId.Val.HasValue)
                numId = paragraph.ParagraphProperties.NumberingProperties.NumberingId.Val.Value;

            int levelRef = -1;
            if (paragraph.ParagraphProperties.NumberingProperties.NumberingLevelReference.Val.HasValue)
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
            var instance = instances.Where(r => r.NumberID.HasValue && r.NumberID.Value == numId).FirstOrDefault();

            if (instance == null)
            {
                return string.Empty;
            }

            int currAbNumId = instance.AbstractNumId.Val.Value;

            var abstractNums = numbering.Where(r => r is AbstractNum).Select(r => r as AbstractNum);
            var abNum = abstractNums.Where(r => r.AbstractNumberId.HasValue && r.AbstractNumberId.Value == instance.AbstractNumId.Val.Value)
                .Select(r => r).FirstOrDefault();

            ParagraphLevelCounter lc;
            listLevelMap.TryGetValue(currAbNumId, out lc);

            LevelTuple[] overrideTuples;
            overrideTupleMap.TryGetValue(numId, out overrideTuples);

            if (lc == null)
            {
                lc = LoadLevelTuples(abNum);
            }

            if (overrideTuples == null)
            {
                overrideTuples = LoadOverrideTuples(abNum, lc.LevelCount);
            }

            string formattedString = lc.IncrementLevel(iLvl, overrideTuples);

            listLevelMap[currAbNumId] = lc;
            overrideTupleMap[numId] = overrideTuples;

            return formattedString;
        }

        private LevelTuple[] LoadOverrideTuples(AbstractNum abNum, int length)
        {
            var levels = abNum.ChildElements.Where(c => c is Level).Select(c => c as Level).ToArray();

            if (levels.Length == 0)
            {
                return null;
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
                    if (levels[i] != null)
                    {
                        tuple = BuildTuple(i, levels[i]);
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

        private ParagraphLevelCounter LoadLevelTuples(AbstractNum abNum)
        {
            //Unfortunately, we need to go this far into the underlying structure
            //to get the abstract num information for the edge case where
            //someone skips a level and the format is not context-free, e.g. "1.B.i".

            var lvlArray = abNum.ChildElements.Where(c => c is Level).Select(c => c as Level).ToArray();

            LevelTuple[] levels = new LevelTuple[lvlArray.Length];
            for (int i = 0; i < levels.Length; i++)
            {
                levels[i] = BuildTuple(i, lvlArray[i]);
            }
            return new ParagraphLevelCounter(levels);
        }

        private LevelTuple BuildTuple(int index, Level level)
        {
            bool isLegal = false;
            int start = 1;
            int restart = -1;
            string lvlText = "%" + index + ".";
            string numFmt = "decimal";
            string indent = string.Empty;


            if (level != null && level.IsLegalNumberingStyle != null && level.IsLegalNumberingStyle.Val.HasValue && level.IsLegalNumberingStyle.Val.Value)
            {
                isLegal = true;
            }

            if (level != null && level.NumberingFormat != null && level.NumberingFormat.Val.HasValue)
            {
                numFmt = level.NumberingFormat.Val.Value.ToString();
            }

            if (level != null && level.LevelRestart != null && level.LevelRestart.Val.HasValue)
            {
                restart = level.LevelRestart.Val.Value;
            }

            if (level.PreviousParagraphProperties != null && level.PreviousParagraphProperties.Indentation != null)
            {
                var indentation = level.PreviousParagraphProperties.Indentation;
                if (indentation.Left.HasValue)
                    indent = TwipsToSpaces(indentation.Left.Value);
                else if (indentation.Start.HasValue)
                    indent = TwipsToSpaces(indentation.Start.Value);
            }

            if (level != null && level.StartNumberingValue != null && level.StartNumberingValue.Val.HasValue)
            {
                start = level.StartNumberingValue.Val.Value;
            }
            else
            {
                //this is a hack. Currently, this gets the lowest possible
                //start for a given numFmt.  We should probably try to grab the
                //restartNumberingAfterBreak value in
                //e.g. <w:abstractNum w:abstractNumId="12" w15:restartNumberingAfterBreak="0">???
                if ("decimal".Equals(numFmt) || "ordinal".Equals(numFmt) || "decimalZero".Equals(numFmt))
                {
                    start = 0;
                }
                else
                {
                    start = 1;
                }
            }
            if (level != null && level.LevelText != null && level.LevelText.Val.HasValue)
            {
                lvlText = level.LevelText.Val.Value;
            }
            return new LevelTuple(start, restart, lvlText, numFmt, isLegal, indent);
        }

        internal static string TwipsToSpaces(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                if (int.TryParse(value, out int num))
                {
                    // 1440 twips = 1 inch
                    // make 1 inch == 6 spaces
                    int spaces = num / 240;

                    return new string(' ', spaces);
                }
            }
            return string.Empty;
        }
    }

    public sealed class NumberFormatter
    {
        private static readonly string[] ROMAN_LETTERS = { "m", "cm", "d", "cd", "c",
            "xc", "l", "xl", "x", "ix", "v", "iv", "i" };

        private static readonly int[] ROMAN_VALUES = { 1000, 900, 500, 400, 100, 90,
            50, 40, 10, 9, 5, 4, 1 };

        private const int T_ARABIC = 0;
        private const int T_LOWER_LETTER = 4;
        private const int T_LOWER_ROMAN = 2;
        private const int T_ORDINAL = 5;
        private const int T_UPPER_LETTER = 3;
        private const int T_UPPER_ROMAN = 1;

        public static string GetNumber(int num, int style)
        {
            switch (style)
            {
                case T_UPPER_ROMAN:
                    return ToRoman(num).ToUpper(CultureInfo.CurrentCulture);
                case T_LOWER_ROMAN:
                    return ToRoman(num);
                case T_UPPER_LETTER:
                    return ToLetters(num).ToUpper(CultureInfo.CurrentCulture);
                case T_LOWER_LETTER:
                    return ToLetters(num);
                case T_ARABIC:
                case T_ORDINAL:
                default:
                    return num.ToString(CultureInfo.CurrentCulture);
            }
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

            StringBuilder result = new StringBuilder();

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
