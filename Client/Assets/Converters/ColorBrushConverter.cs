using Client.Assets.HighLighters;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Client.Assets.Converters
{
    [ValueConversion(typeof(DateTime), typeof(Brush))]
    public class ColorBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var color = HColor.Green;

            if (!string.IsNullOrEmpty(color))
            {
                var bc = new BrushConverter();
                var brush = (Brush)bc.ConvertFrom(color);
                return brush;
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

    }
}
