using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Custom.Exporter.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        private const string InvertParameter = "inverted";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var bValue = (value is bool b) && b;

            if (parameter is string s && s == InvertParameter) bValue = !bValue;

            return bValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return parameter is string s && s == InvertParameter ? visibility != Visibility.Visible : visibility == Visibility.Visible;
            }

            return false;
        }
    }
}
