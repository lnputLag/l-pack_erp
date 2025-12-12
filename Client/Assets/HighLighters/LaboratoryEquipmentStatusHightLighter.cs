using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Client.Assets.HighLighters
{
    [ValueConversion(typeof(DateTime), typeof(Brush))]
    public class LaboratoryEquipmentStatusHightLighter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            /*
            дописать коммент   
                (1)    Новая         голубой
                (2)    Принята      
                (3)    Отклонена     красный
                (4)    В работе     
                (5)    приемка зеленый + синий шрифт
                (6)    Доработка     желтый
                (7)    Выполнена     зеленый
             */
            var color = "";

            switch (int.Parse(value.ToString()))
            {
                case 2:
                    color = HColor.Yellow;
                    break;
                case 3:
                    color = HColor.Red;
                    break;
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
