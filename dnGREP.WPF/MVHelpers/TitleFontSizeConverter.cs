﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace dnGREP.WPF
{
    public class TitleFontSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double)
            {
                double size = (double)value;
                return size + 4;
            }
            return 16;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
