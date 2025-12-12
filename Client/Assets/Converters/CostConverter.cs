using System;
using System.Windows.Data;

namespace Client.Assets.Converters
{
    [ValueConversion(typeof(double), typeof(string))]
    public class CostConverter : IValueConverter
    {
        // Возвращаем строку в формате 123.456.789 руб.
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (!(value is string)) return string.Empty;
            if (!decimal.TryParse(((string)value).Replace('.', ','), out _)) return string.Empty;

            var priceStr = ((string)value).Replace('.', ',');

            return decimal.Parse(priceStr).ToString("0.00") + " руб.";
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value != null && double.TryParse(value.ToString(), System.Globalization.NumberStyles.Any, culture, out var result))
            {
                return result;
            }

            if (value != null && double.TryParse(value.ToString().Replace(" руб.", ""), System.Globalization.NumberStyles.Any,
                    culture, out result))
            {
                return result;
            }
            return value;
        }
    }
}
