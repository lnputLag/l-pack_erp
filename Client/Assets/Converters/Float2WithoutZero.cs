using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Client.Assets.Converters
{
    /// <summary>
    /// форматирует число:
    /// 1,00 => 1
    /// 1.15 => 1,15
    /// </summary>
    [ValueConversion(typeof(decimal), typeof(string))]
    public class Float2WithoutZero : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var result = "";

            if (value != null)
            {
                var s = value.ToString();

                if (!string.IsNullOrEmpty(s))
                {
                    s = s.Replace(".", ",");
                    s = string.Format(culture, "{0:0.##}", decimal.Parse(s));
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
