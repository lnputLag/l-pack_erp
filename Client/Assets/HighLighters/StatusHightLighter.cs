using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Client.Assets.HighLighters
{
    [ValueConversion(typeof(DateTime), typeof(Brush))]
    public class StatusHighLighter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            /*
                (1)    Новая         голубой
                (2)    Принята      
                (3)    Отклонена     красный
                (4)    В работе     
                (5)    приемка зеленый + синий шрифт
                (6)    Доработка     желтый
                (7)    Выполнена     зеленый
                (8)    Отменена      красный
             */
            var color = "";

            var v = value?.ToString().ToLower();
            if (!string.IsNullOrEmpty(v))
            {
                if (v.IndexOf("новая", StringComparison.Ordinal) != -1)
                {
                    color = HColor.Blue;
                }

                if (v.IndexOf("доработка", StringComparison.Ordinal) != -1)
                {
                    color = HColor.Yellow;
                }

                if (v.IndexOf("отклонена", StringComparison.Ordinal) != -1)
                {
                    color = HColor.Red;
                }

                if (v.IndexOf("отменена", StringComparison.Ordinal) != -1)
                {
                    color = HColor.Red;
                }

                if (v.IndexOf("выполнена", StringComparison.Ordinal) != -1)
                {
                    color = HColor.Green;
                }

                if (v.IndexOf("приемка", StringComparison.Ordinal) != -1)
                {
                    color = HColor.Green;
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
