using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Client.Assets.HighLighters
{

    /// <summary>
    /// красочный маркер? :)
    /// </summary>
    [ValueConversion(typeof(DateTime), typeof(Brush))]
    public class ColorfulHighLighter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var color = "";

            var v = value?.ToString().ToLower();

            if (!string.IsNullOrEmpty(v))
            {
                switch (v)
                {
                    case "1":
                        color = HColor.Blue;
                        break;
                    case "2":
                        color = HColor.Green;
                        break;
                    case "3":
                        color = HColor.Red;
                        break;
                    case "4":
                        color = HColor.Yellow;
                        break;
                    case "5":
                        color = HColor.Orange;
                        break;
                    case "6":
                        color = HColor.Violet;
                        break;
                    case "7":
                        color = HColor.Olive;
                        break;

                        // todo а какой цвет по умолчанию?
                        //default: break;
                }

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
