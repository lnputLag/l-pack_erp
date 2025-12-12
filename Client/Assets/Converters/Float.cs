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
    public class Float : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string result = "";

            try
            {

                var s = value.ToString();
                if (!string.IsNullOrEmpty(s))
                {
                    s = s.Replace(".", ",");
                    s = string.Format(culture, "{0:N}", double.Parse(s));
                    s = s.Replace(",", "");
                    s = s.Replace(".", ",");
                    result = s;
                }
            }
            catch { }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

    }
}
