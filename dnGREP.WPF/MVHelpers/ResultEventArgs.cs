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

    public class GrepLineSelectEventArgs(FormattedGrepLine? formattedGrepLine, int lineMatchCount, int matchOrdinal, int fileMatchCount) : EventArgs
    {
        public FormattedGrepLine? FormattedGrepLine { get; private set; } = formattedGrepLine;
        public int LineMatchCount { get; set; } = lineMatchCount;
        public int MatchOrdinal { get; private set; } = matchOrdinal;
        public int FileMatchCount { get; private set; } = fileMatchCount;
    }

}
