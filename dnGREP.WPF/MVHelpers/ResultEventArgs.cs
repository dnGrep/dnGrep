using System;
using System.Drawing;

namespace dnGREP.WPF.MVHelpers
{
    public class GrepLineEventArgs : EventArgs
    {
        public FormattedGrepLine? FormattedGrepLine { get; set; }
        public bool UseCustomEditor { get; set; }
        public RectangleF ParentWindowSize { get; set; }
    }

    public class GrepResultEventArgs : EventArgs
    {
        public FormattedGrepResult? FormattedGrepResult { get; set; }
        public bool UseCustomEditor { get; set; }
        public RectangleF ParentWindowSize { get; set; }
    }

    public class GrepLineSelectEventArgs : EventArgs
    {
        public GrepLineSelectEventArgs(FormattedGrepLine? formattedGrepLine, int lineMatchCount, int matchOrdinal, int fileMatchCount)
        {
            FormattedGrepLine = formattedGrepLine;
            LineMatchCount = lineMatchCount;
            MatchOrdinal = matchOrdinal;
            FileMatchCount = fileMatchCount;
        }

        public FormattedGrepLine? FormattedGrepLine { get; private set; }
        public int LineMatchCount { get; set; }
        public int MatchOrdinal { get; private set; }
        public int FileMatchCount { get; private set; }
    }

}
