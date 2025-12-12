using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Client.Assets.HighLighters
{
    [ValueConversion(typeof(DateTime), typeof(Brush))]
    public class FirstRowHighLighter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var color = "";

            //   (1)    зеленый  

            var v = value?.ToString().ToLower();
            if (v == "0")
            {
                color = HColor.Green;
            }

            if (!string.IsNullOrEmpty(color))
            {
                var bc = new BrushConverter();
                var brush = (Brush)bc.ConvertFrom(color);
                return brush;
            }

            return null;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

    }
}
