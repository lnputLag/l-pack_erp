using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Client.Assets.Converters
{
    [ValueConversion(typeof(DateTime), typeof(Brush))]
    public class StripTags : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var v = value as string;
            if (!string.IsNullOrEmpty(v))
            {
                v = v.Replace("\r\n", string.Empty);
            }
            return v;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

    }
}
