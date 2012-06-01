using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Globalization;

namespace dnGREP.WPF
{
	public class EnumBooleanConverter : IValueConverter
	{
		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var ParameterString = parameter as string;
			if (ParameterString == null)
				return DependencyProperty.UnsetValue;

			if (Enum.IsDefined(value.GetType(), value) == false)
				return DependencyProperty.UnsetValue;

			object paramvalue = Enum.Parse(value.GetType(), ParameterString);
			if (paramvalue.Equals(value))
				return true;
			else
				return false;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var ParameterString = parameter as string;
			if (ParameterString == null)
				return DependencyProperty.UnsetValue;

			return Enum.Parse(targetType, ParameterString);
		}

		#endregion
	}
}
