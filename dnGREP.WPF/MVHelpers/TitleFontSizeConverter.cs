﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace dnGREP.WPF
{
    public class TitleFontSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double && parameter is string input &&
                double.TryParse(input, out double amount))
            {
                double size = (double)value;
                return size + amount;
            }
            return 16;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
