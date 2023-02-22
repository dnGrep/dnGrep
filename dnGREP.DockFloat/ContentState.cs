using System.Windows;

namespace dnGREP.DockFloat
{
    class ContentState
    {
        readonly HorizontalAlignment horizontalAlignment;
        readonly VerticalAlignment verticalAlignment;
        readonly double width;
        readonly double height;

        ContentState(FrameworkElement content)
        {
            horizontalAlignment = content.HorizontalAlignment;
            verticalAlignment = content.VerticalAlignment;
            width = content.Width;
            height = content.Height;

            FloatContent = content;
            ActualWidth = content.ActualWidth;
            ActualHeight = content.ActualHeight;
        }

        internal FrameworkElement FloatContent { get; }
        internal double ActualWidth { get; }
        internal double ActualHeight { get; }

        internal static ContentState Save(FrameworkElement content) => new(content);

        internal FrameworkElement Restore()
        {
            FloatContent.HorizontalAlignment = horizontalAlignment;
            FloatContent.VerticalAlignment = verticalAlignment;
            FloatContent.Width = width;
            FloatContent.Height = height;
            return FloatContent;
        }
    }
}
