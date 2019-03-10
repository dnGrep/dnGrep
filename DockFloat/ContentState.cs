using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DockFloat
{
    class ContentState
    {
        readonly HorizontalAlignment horizontalAlignment;
        readonly VerticalAlignment VerticalAlignment;
        readonly double width;
        readonly double height;

        ContentState(FrameworkElement content)
        {
            horizontalAlignment = content.HorizontalAlignment;
            VerticalAlignment = content.VerticalAlignment;
            width = content.Width;
            height = content.Height;

            FloatContent = content;
            ActualWidth = content.ActualWidth;
            ActualHeight = content.ActualHeight;
        }

        internal FrameworkElement FloatContent { get; }
        internal double ActualWidth { get; }
        internal double ActualHeight { get; }

        internal static ContentState Save(FrameworkElement content) =>
            new ContentState(content);

        internal FrameworkElement Restore()
        {
            FloatContent.HorizontalAlignment = horizontalAlignment;
            FloatContent.VerticalAlignment = VerticalAlignment;
            FloatContent.Width = width;
            FloatContent.Height = height;
            return FloatContent;
        }
    }
}
