using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using NCalc;

namespace Client.Assets.Converters
{

    /// <summary>
    /// Для чего этот конвертор? Что он делает?
    /// </summary>
    [ValueConversion(typeof(DateTime), typeof(Brush))]
    public class MathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var expressionString = parameter as string;

            if (!string.IsNullOrEmpty(expressionString))
            {
                expressionString = expressionString.Replace(" ", "");
                if (value != null)
                {
                    try
                    {
                        if (int.Parse(value.ToString()) < 300) value = 300;
                    }
                    catch { }


                    expressionString = expressionString.Replace("@VALUE", value.ToString());
                }

                return new Expression(expressionString).Evaluate();
            }

            return null;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}
