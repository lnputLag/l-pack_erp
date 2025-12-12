using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Client.Assets.Converters
{
    /// <summary>
    /// форматирует число: 12345,23
    /// </summary>
    [ValueConversion(typeof(DateTime), typeof(Brush))]
    public class Float3 : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string result = "";

            if (value != null)
            {
                var s = value.ToString();
                if (!string.IsNullOrEmpty(s))
                {
                    s = s.Replace(".", ",");
                    s = string.Format(culture, "{0:N3}", double.Parse(s));
                    s = s.Replace(",", "");
                    s = s.Replace(".", ",");
                    result = s;
                }
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

    }
}
