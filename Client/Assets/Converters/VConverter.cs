using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Client.Assets.Converters
{
    public class VConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var v = ((string)value)?.ToLower();


            return v == "Collapsed" ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
