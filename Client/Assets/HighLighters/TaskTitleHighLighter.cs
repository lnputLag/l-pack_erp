using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Client.Assets.HighLighters
{
    /// <summary>
    /// список задач: поле "задача", если задача приоритетная, фон становится красным
    /// </summary>
    [ValueConversion(typeof(DateTime), typeof(Brush))]
    public class TaskTitleHighLighter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var v = value?.ToString();
            if (!string.IsNullOrEmpty(v))
            {
                var color = HColor.Blue;
                if (v == "*")
                {
                    color = HColor.RedAccented;
                }

                if (!string.IsNullOrEmpty(color))
                {
                    var bc = new BrushConverter();
                    var brush = (Brush)bc.ConvertFrom(color);
                    return brush;
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

    }
}
