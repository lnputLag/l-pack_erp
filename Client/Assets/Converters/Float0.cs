using Client.Common;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Client.Assets.Converters
{
    [ValueConversion(typeof(decimal), typeof(int))]
    public class Float0 : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //if (value != null) return (int)decimal.Parse(value.ToString());
            if(value!=null)
            {
                var s=value.ToString();
                if(!string.IsNullOrEmpty(s))
                {
                    var result=s.ToInt();
                    return result;
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
