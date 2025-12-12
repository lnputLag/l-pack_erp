using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Client.Assets.Converters
{
    /// <summary>
    /// форматирует номер телефона
    /// </summary>
    [ValueConversion(typeof(DateTime), typeof(Brush))]
    public class CellPhone : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string result = "";

            var s = value.ToString();
            if (!string.IsNullOrEmpty(s))
            {
                s = s.Replace(" ", "");
                s = s.Replace("-", "");
                s = s.Replace("+", "");

                if (s.Substring(0, 1) == "8")
                {
                    s = $"7{s.Substring(1, s.Length - 1)}";
                }

                s = $"+{s}";
                result = s;
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

    }
}
