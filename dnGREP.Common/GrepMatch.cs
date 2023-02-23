using System;
using System.Collections.Generic;

namespace dnGREP.Common
{
    public class GrepMatch : IComparable<GrepMatch>, IComparable, IEquatable<GrepMatch>
    {
        public GrepMatch(string searchPattern, int line, int start, int length)
        {
            SearchPattern = searchPattern;
            LineNumber = line;
            StartLocation = start;
            Length = length;

            FileMatchId = Guid.NewGuid().ToString();
        }

        public GrepMatch(string fileMatchId, string searchPattern, int line, int start, int length, IEnumerable<GrepCaptureGroup> toCopy)
        {
            LineNumber = line;
            StartLocation = start;
            Length = length;
            SearchPattern = searchPattern;

            FileMatchId = fileMatchId;

            Groups.AddRange(toCopy);
        }

        public string FileMatchId { get; }

        public int LineNumber { get; } = 0;

        /// <summary>
        /// The start location: could be within the whole file if created for a GrepSearchResult,
        /// or just the within line if created for a GrepLine
        /// </summary>
        public int StartLocation { get; } = 0;

        public int Length { get; private set; } = 0;

        public int EndPosition
        {
            get
            {
                return StartLocation + Length;
            }
            set
            {
                Length = value - StartLocation;
            }
        }

        public List<GrepCaptureGroup> Groups { get; } = new List<GrepCaptureGroup>();

        public string SearchPattern { get; private set; }

        /// <summary>
        /// Gets or sets a flag indicating if this match should be replaced
        /// </summary>
        public bool ReplaceMatch { get; set; } = false;

        public override string ToString()
        {
            return $"{LineNumber}: {StartLocation} + {Length}";
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(LineNumber, StartLocation, Length);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as GrepMatch);
        }

        public bool Equals(GrepMatch? other)
        {
            if (other == null) return false;

            return LineNumber == other.LineNumber &&
                StartLocation == other.StartLocation &&
                Length == other.Length;
        }

        #region IComparable<GrepMatch> Members

        public int CompareTo(GrepMatch? other)
        {
            if (other == null)
                return 1;
            else
                return StartLocation.CompareTo(other.StartLocation);
        }

        #endregion

        #region IComparable Members

        public int CompareTo(object? obj)
        {
            if (obj == null)
                return 1;
            if (obj is GrepMatch match)
                return StartLocation.CompareTo(match.StartLocation);
            else
                return 1;
        }

        #endregion

        /// <summary>
        /// Normalizes the results from multiple searches by sorting the matches, 
        /// and then merging overlapping matches
        /// </summary>
        /// <param name="matches"></param>
        public static void Normalize(List<GrepMatch> matches)
        {
            if (matches != null && matches.Count > 1)
            {
                matches.Sort();
                var overlap = FirstOverlap(matches);
                while (overlap != null)
                {
                    GrepMatch merged = GrepMatch.Merge(overlap.Item1, overlap.Item2);
                    matches.RemoveAt(overlap.Item3 + 1);
                    matches.RemoveAt(overlap.Item3);
                    matches.Insert(overlap.Item3, merged);

                    overlap = FirstOverlap(matches);
                }
            }
        }

        private static Tuple<GrepMatch, GrepMatch, int>? FirstOverlap(List<GrepMatch> matches)
        {
            for (int idx = 0; idx < matches.Count - 1; idx++)
            {
                if (matches[idx].IsOverlap(matches[idx + 1]))
                    return Tuple.Create(matches[idx], matches[idx + 1], idx);
            }
            return null;
        }

        private bool IsOverlap(GrepMatch other)
        {
            if (LineNumber == other.LineNumber)
            {
                if (StartLocation <= other.StartLocation && StartLocation + Length > other.StartLocation)
                {
                    return true;
                }
                else if (other.StartLocation <= StartLocation && other.StartLocation + other.Length > StartLocation)
                {
                    return true;
                }
            }
            return false;
        }

        private static GrepMatch Merge(GrepMatch one, GrepMatch two)
        {
            int start = Math.Min(one.StartLocation, two.StartLocation);
            int end = Math.Max(one.EndPosition, two.EndPosition);
            int line = Math.Min(one.LineNumber, two.LineNumber);

            return new GrepMatch(one.SearchPattern + " & " + two.SearchPattern, line, start, end - start);
        }
    }
}
