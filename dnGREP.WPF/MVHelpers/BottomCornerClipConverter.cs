using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace dnGREP.WPF
{
    public class BottomCornerClipConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 3 &&
                values[0] is double width &&
                values[1] is double height &&
                values[2] is CornerRadius cornerRadius &&
                width > 0 && height > 0)
            {
                var figure = new PathFigure
                {
                    StartPoint = new Point(0, 0),
                    IsClosed = true,
                    IsFilled = true,
                };

                // simple implementation assumption:
                double radius = cornerRadius.BottomLeft;

                // Top-left corner (sharp)
                figure.Segments.Add(new LineSegment(new Point(width, 0), false));

                // Top-right to bottom-right (sharp top-right, rounded bottom-right)
                figure.Segments.Add(new LineSegment(new Point(width, height - radius), false));
                figure.Segments.Add(new ArcSegment(
                    new Point(width - radius, height),
                    new Size(radius, radius),
                    0, false, SweepDirection.Clockwise, false));

                // Bottom-right to bottom-left (rounded bottom-left)
                figure.Segments.Add(new LineSegment(new Point(radius, height), false));
                figure.Segments.Add(new ArcSegment(
                    new Point(0, height - radius),
                    new Size(radius, radius),
                    0, false, SweepDirection.Clockwise, false));

                // Back to start
                figure.Segments.Add(new LineSegment(new Point(0, 0), false));

                var geometry = new PathGeometry();
                geometry.Figures.Add(figure);
                geometry.Freeze();

                return geometry;
            }

            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}