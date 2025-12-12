using Client.Common;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Client.Assets.Converters
{
    /// <summary>
    /// Преобразует входящее значение в булево. Вернет true, если значение ="1"
    /// </summary>
    [ValueConversion(typeof(DateTime), typeof(Brush))]
    public class ToBool : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool result = false;

            if(value!=null)
            {
                var s = value.ToString();
                if (!string.IsNullOrEmpty(s))
                {
                    var v=(int)s.ToDouble().ToInt();
                    if (v == 1)
                    {
                        result = true;
                    }
                }
            }          
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string result = "0";
            if(value!=null)
            {
                if((bool)value==true)
                {
                    result="1";
                }
            }
            return result;
        }

    }
}
