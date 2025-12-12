using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Client.Assets.Converters
{
    [ValueConversion(typeof(DateTime), typeof(Brush))]
    public class DateDateTime : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                var output = "";
                return output;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                var output = value.ToString();
                if (!string.IsNullOrEmpty(output))
                {
                    //2009-05-08 14:40:52,531 -> 08.05.2009 14:40:52
                    var myDate = DateTime.ParseExact(output, "dd.MM.yyyy H:mm:ss", CultureInfo.InvariantCulture);
                    output = myDate.ToString("dd.MM.yyyy HH:mm:ss");
                }

                return output;
            }

            return null;
        }
    }
}
