﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace dnGREP.WPF
{
    class PercentValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double && parameter is string input &&
                double.TryParse(input, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double percent))
            {
                return percent * (double)value;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
