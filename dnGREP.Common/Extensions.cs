using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using dnGREP.Localization.Properties;

namespace dnGREP.Common
{
    public static class Extensions
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            if (source == null) return false;
            return source.Contains(toCheck, comp);
        }

        public static bool Contains(this IEnumerable<string> source, string toCheck, StringComparison comp)
        {
            if (source == null) return false;
            return source.Where(s => s.Contains(toCheck, comp)).Any();
        }

        public static int IndexOf<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            var index = 0;
            foreach (var item in source)
            {
                if (predicate.Invoke(item))
                {
                    return index;
                }
                index++;
            }

            return -1;
        }

        public static bool ContainsNotEscaped(this string input, string toCheck)
        {
            bool found = false;
            int startIndex = 0;
            while (startIndex < input.Length)
            {
                int pos = input.IndexOf(toCheck, startIndex, StringComparison.Ordinal);
                if (pos > -1)
                {
                    if (pos > 0 && input[pos - 1] == '\\')
                    {
                        // this value is escaped, does not count
                    }
                    else
                    {
                        found = true;
                        break;
                    }
                    startIndex = pos + toCheck.Length;
                }
                else
                {
                    break;
                }
            }

            return found;
        }

        public static string ReplaceIfNotEscaped(this string input, string oldValue, string? newValue)
        {
            // Note that if newValue is null, we treat it like string.Empty.

            StringBuilder sb = new(input.Length + newValue?.Length ?? 0);

            int startIndex = 0;
            while (startIndex < input.Length)
            {
                int pos = input.IndexOf(oldValue, startIndex, StringComparison.Ordinal);
                if (pos > -1)
                {
                    sb.Append(input[startIndex..pos]);

                    if (pos > 0 && input[pos - 1] == '\\')
                    {
                        // this value is escaped, do not replace
                        sb.Append(oldValue);
                    }
                    else
                    {
                        sb.Append(newValue ?? string.Empty);
                    }
                    startIndex = pos + oldValue.Length;
                }
                else
                {
                    sb.Append(input[startIndex..]);
                    startIndex = input.Length;
                }
            }
            return sb.ToString();
        }

        public static string ToIso8601Date(this DateTime input)
        {
            return input.ToString("yyyy-MM-dd");
        }

        public static string ToIso8601DateTime(this DateTime input)
        {
            return input.ToString("yyyy-MM-ddTHH:mm:ss");
        }

        public static string ToIso8601DateTimeWithZone(this DateTime input)
        {
            return input.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
        }

        public static DateTime? FromIso8601Date(this string input)
        {
            if (DateTime.TryParseExact(input, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime dt))
            {
                return dt;
            }
            return null;
        }

        public static DateTime? FromIso8601DateTime(this string input)
        {
            if (DateTime.TryParseExact(input, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime dt))
            {
                return dt;
            }
            return null;
        }

        public static DateTime? FromIso8601DateTimeWithZone(this string input)
        {
            if (DateTime.TryParseExact(input, "yyyy-MM-ddTHH:mm:ss.fffzzz", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime dt))
            {
                if (input.EndsWith("00:00", StringComparison.Ordinal) || input.EndsWith('Z'))
                {
                    if (dt.Kind == DateTimeKind.Local)
                    {
                        dt = dt.ToUniversalTime();
                    }
                }

                return dt;
            }
            return null;
        }

        public static string ToLocalizedString(this SearchType typeOfSearch)
        {
            string result = string.Empty;
            switch (typeOfSearch)
            {
                case SearchType.XPath:
                    result = Resources.Main_SearchType_XPath;
                    break;
                case SearchType.PlainText:
                    result = Resources.Main_SearchType_Text;
                    break;
                case SearchType.Hex:
                    result = Resources.Main_SearchType_Hex;
                    break;
                case SearchType.Regex:
                    result = Resources.Main_SearchType_Regex;
                    break;
                case SearchType.Soundex:
                    result = Resources.Main_SearchType_Phonetic;
                    break;
            }
            return result.Replace("_", string.Empty, StringComparison.Ordinal);
        }

        public static string ToLocalizedString(this FileSearchType fileSearchType)
        {
            string result = string.Empty;
            switch (fileSearchType)
            {
                case FileSearchType.Regex:
                    result = Resources.Main_PatternType_Regex;
                    break;
                case FileSearchType.Asterisk:
                    result = Resources.Main_PatternType_Asterisk;
                    break;
                case FileSearchType.Everything:
                    result = Resources.Main_PatternType_Everything;
                    break;
            }
            return result.Replace("_", string.Empty, StringComparison.Ordinal);
        }

        public static string ToLocalizedString(this FileDateFilter fileDateFilter)
        {
            string result = string.Empty;
            switch (fileDateFilter)
            {
                case FileDateFilter.None:
                    result = Resources.Main_AllDates;
                    break;
                case FileDateFilter.Created:
                    result = Resources.Main_Created;
                    break;
                case FileDateFilter.Modified:
                    result = Resources.Main_Modified;
                    break;
            }
            return result.Replace("_", string.Empty, StringComparison.Ordinal);
        }

        public static string ToLocalizedString(this FileTimeRange timeRangeFilter)
        {
            string result;
            switch (timeRangeFilter)
            {
                default:
                case FileTimeRange.None:
                case FileTimeRange.Dates:
                    result = string.Empty;
                    break;
                case FileTimeRange.Minutes:
                    result = Resources.Main_DatePastMinutes;
                    break;
                case FileTimeRange.Hours:
                    result = Resources.Main_DatePastHours;
                    break;
                case FileTimeRange.Days:
                    result = Resources.Main_DatePastDays;
                    break;
                case FileTimeRange.Weeks:
                    result = Resources.Main_DatePastWeeks;
                    break;
                case FileTimeRange.Months:
                    result = Resources.Main_DatePastMonths;
                    break;
                case FileTimeRange.Years:
                    result = Resources.Main_DatePastYears;
                    break;
            }
            return result.Replace("_", string.Empty, StringComparison.Ordinal);
        }

        public static string GetElementString(this XElement parent, XName name)
        {
            var child = parent.Element(name);
            if (child != null)
            {
                return child.Value;
            }
            return string.Empty;
        }

        public static bool GetElementBoolean(this XElement parent, XName name)
        {
            var child = parent.Element(name);
            if (child != null && bool.TryParse(child.Value, out bool value))
            {
                return value;
            }
            return false;
        }

        public static int CountChar(this string source, char toCount)
        {
            int count = 0;
            foreach (var c in source.AsSpan())
            {
                if (c == toCount)
                    count++;
            }
            return count;
        }


        public static string StripQuotes(this string input)
        {
            if (input.Length > 2 && input.StartsWith('"') && input.EndsWith('"'))
            {
                input = input[1..^1];
            }
            return input;
        }

    }
}
