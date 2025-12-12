using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Client.Assets.Converters
{
    /// <summary>
    /// Где,для чего, как использовать этот конвертер
    /// </summary>
    [ValueConversion(typeof(DateTime), typeof(Brush))]
    public class MatrixGetLabel : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                var output = value.ToString();

                var i = output.IndexOf("|", StringComparison.Ordinal);
                output = output.Substring(0, i);

                return output;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

    }
}
