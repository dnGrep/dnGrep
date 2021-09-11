using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace dnGREP.Common
{
    public static class Extensions
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            if (source == null) return false;
            return source.IndexOf(toCheck, comp) >= 0;
        }

        public static bool Contains(this IEnumerable<string> source, string toCheck, StringComparison comp)
        {
            if (source == null) return false;
            return source.Where(s => s.Contains(toCheck, comp)).Any();
        }

        public static bool ConstainsNotEscaped(this string input, string toCheck)
        {
            if (toCheck == null)
                throw new ArgumentNullException("toCheck");

            bool found = false;
            int startIndex = 0;
            while (startIndex < input.Length)
            {
                int pos = input.IndexOf(toCheck, startIndex);
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

        public static string ReplaceIfNotEscaped(this string input, string oldValue, string newValue)
        {
            if (oldValue == null)
                throw new ArgumentNullException("oldValue");
            // Note that if newValue is null, we treat it like string.Empty.

            StringBuilder sb = new StringBuilder(input.Length + newValue?.Length ?? 0);

            int startIndex = 0;
            while (startIndex < input.Length)
            {
                int pos = input.IndexOf(oldValue, startIndex);
                if (pos > -1)
                {
                    sb.Append(input.Substring(startIndex, pos - startIndex));

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
                    sb.Append(input.Substring(startIndex));
                    startIndex = input.Length;
                }
            }
            return sb.ToString();
        }

        public static string ToIso8601Date(this DateTime input)
        {
            return input.ToString("yyyyMMdd");
        }

        public static string ToIso8601DateTime(this DateTime input)
        {
            return input.ToString("yyyyMMddTHHmmss");
        }

        public static string ToIso8601DateTimeWithZone(this DateTime input)
        {
            return input.ToString("yyyyMMddTHHmmss.fffzzz");
        }
    } 
}
