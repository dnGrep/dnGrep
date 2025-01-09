using System;
using System.Collections.Generic;

namespace dnGREP.Common
{
    public class GrepLine : IComparable<GrepLine>, IComparable
    {
        public GrepLine(int number, string text, bool context, List<GrepMatch>? matches)
        {
            LineNumber = number;
            LineText = text;
            IsContext = context;
            if (matches == null)
                Matches = [];
            else
                Matches = matches;
        }

        public int PageNumber { get; set; } = -1;

        public int LineNumber { get; set; }

        public string LineText { get; }

        public bool IsContext { get; } = false;
        public bool IsHexFile { get; set; }

        public List<GrepMatch> Matches { get; }


        /// <summary>
        /// Gets or sets the line number from a display view of the file
        /// clipped to show only matched lines and context lines, or
        /// expanded to show lines inserted above in a diff view
        /// Returns the normal line number if not set
        /// </summary>
        public int DisplayFileLineNumber
        {
            get { return _displayFileLineNumber > -1 ? _displayFileLineNumber : LineNumber; }
            set { _displayFileLineNumber = value; }
        }
        private int _displayFileLineNumber = -1;

        public override string ToString()
        {
            return string.Format("{0}. {1} ({2})", LineNumber, LineText, IsContext);
        }

        #region IComparable<GrepLine> Members

        public int CompareTo(GrepLine? other)
        {
            if (other == null)
                return 1;
            else
                return LineNumber.CompareTo(other.LineNumber);
        }

        #endregion

        #region IComparable Members

        public int CompareTo(object? obj)
        {
            if (obj == null)
                return 1;
            if (obj is GrepLine line)
                return LineNumber.CompareTo(line.LineNumber);
            else
                return 1;
        }

        #endregion
    }
}
