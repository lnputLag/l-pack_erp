using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Client.Assets.Converters
{
    /// <summary>
    /// Где,для чего, как использовать этот конвертер
    /// напишите пожалуйста. непонятно
    /// для чего нужен вертикальный разделитель?
    /// </summary>
    [ValueConversion(typeof(DateTime), typeof(Brush))]
    public class MatrixGetTooltip : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string output = null;

            // todo что насчет пробелов? почему не анализируются здесь почему не tostring.trim?
            if (value != null && !string.IsNullOrEmpty(value.ToString()))
            {
                output = value.ToString().Trim();
                int i = output.IndexOf("|", StringComparison.Ordinal);
                int l = output.Length;
                output = output.Substring(i + 1, l - i - 1);
            }

            return !string.IsNullOrEmpty(output) ? output : null;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

    }
}
