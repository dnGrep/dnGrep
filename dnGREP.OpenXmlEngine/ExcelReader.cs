using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using dnGREP.Common;
using ExcelDataReader;
using ExcelNumberFormat;
using NLog;

namespace dnGREP.Engines.OpenXml
{
    internal static class ExcelReader
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static List<KeyValuePair<string, string>> ExtractExcelText(Stream stream,
            PauseCancelToken pauseCancelToken)
        {
            List<KeyValuePair<string, string>> results = new();

            // Auto-detect format, supports:
            //  - Binary Excel files (2.0-2003 format; *.xls)
            //  - OpenXml Excel files (2007 format; *.xlsx)
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                do
                {
                    StringBuilder sb = new();
                    while (reader.Read())
                    {
                        pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                        for (int col = 0; col < reader.FieldCount; col++)
                        {
                            sb.Append(GetFormattedValue(reader, col, CultureInfo.CurrentCulture)).Append('\t');
                        }

                        sb.Append(Environment.NewLine);
                    }

                    pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                    results.Add(new KeyValuePair<string, string>(reader.Name, sb.ToString()));

                } while (reader.NextResult());

            }

            pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

            return results;
        }

        private static string GetFormattedValue(IExcelDataReader reader, int columnIndex, CultureInfo culture)
        {
            string? result = null;
            var value = reader.GetValue(columnIndex);
            if (value == null)
            {
                return string.Empty;
            }

            var formatString = reader.GetNumberFormatString(columnIndex);
            if (formatString != null)
            {
                try
                {
                    var format = new NumberFormat(formatString);
                    if (format.IsValid)
                    {
                        result = format.Format(value, culture);
                    }
                }
                catch (OverflowException)
                {
                    // issue 951: there is a known error in ExcelNumberFormat formatting zero in scientific format
                    try
                    {
                        if (value is double num && formatString.Contains("E", StringComparison.OrdinalIgnoreCase))
                        {
                            result = num.ToString(formatString, culture);
                        }
                    }
                    catch { }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Error formatting Excel value '{value}' with format '{formatString}'");
                }
            }

            result ??= Convert.ToString(value, culture) ?? string.Empty;

            return result.Replace('\r', ' ').Replace('\n', ' ');
        }
    }
}
