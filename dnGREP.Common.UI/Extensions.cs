using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;

namespace dnGREP.Common.UI
{
    public static class Extensions
    {
        /// <summary>
        /// http://stackoverflow.com/questions/1600962/displaying-the-build-date
        /// </summary>
        public static DateTime GetLinkerTime(this Assembly assembly, TimeZoneInfo target = null)
        {
            var filePath = assembly.Location;
            const int c_PeHeaderOffset = 60;
            const int c_LinkerTimestampOffset = 8;

            var buffer = new byte[2048];

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                stream.Read(buffer, 0, 2048);

            var offset = BitConverter.ToInt32(buffer, c_PeHeaderOffset);
            var secondsSince1970 = BitConverter.ToInt32(buffer, offset + c_LinkerTimestampOffset);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var linkTimeUtc = epoch.AddSeconds(secondsSince1970);

            var tz = target ?? TimeZoneInfo.Local;
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(linkTimeUtc, tz);

            return localTime;
        }

        public static Rectangle GetBounds(this Window window)
        {
            var loc = window.PointToScreen(new System.Windows.Point(0, 0));

            return new Rectangle((int)loc.X, (int)loc.Y, (int)window.ActualWidth, (int)window.ActualHeight);
        }

        public static RectangleF GetBoundsF(this Window window)
        {
            var loc = window.PointToScreen(new System.Windows.Point(0, 0));

            return new RectangleF((float)loc.X, (float)loc.Y, (float)window.ActualWidth, (float)window.ActualHeight);
        }
    }
}
