using System;
using System.Drawing;

namespace dnGREP.WPF.MVHelpers
{
    public class GrepLineEventArgs : EventArgs
    {
        public FormattedGrepLine FormattedGrepLine { get; set; }
        public bool UseCustomEditor { get; set; }
        public RectangleF ParentWindowSize { get; set; }
    }

    public class GrepResultEventArgs : EventArgs
    {
        public FormattedGrepResult FormattedGrepResult { get; set; }
        public bool UseCustomEditor { get; set; }
        public RectangleF ParentWindowSize { get; set; }
    }
}
