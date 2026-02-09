using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Stock.RawMaterialMonitor
{
    /// <summary>
    /// Конвертер для форматирования чисел
    /// </summary>
    public class MaterialNumberWithSpacesConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int intValue)
            {
                return intValue.ToString("N0").Replace(",", " ");
            }
            else if (value is long longValue)
            {
                return longValue.ToString("N0").Replace(",", " ");
            }

            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
