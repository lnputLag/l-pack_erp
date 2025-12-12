using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Client.Assets.HighLighters
{
    /// <summary>
    /// 
    /// </summary>
    [ValueConversion(typeof(DateTime), typeof(Brush))]
    public class DataGridColumnRedGreenHighLighter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            var color = "";

            var v = (Dictionary<string, string>)value;
            //var provider = CultureInfo.InvariantCulture;

            var p = parameter.ToString();
            if (!string.IsNullOrEmpty(p))
            {

                var valueKey = "";
                var conditionKey = "";

                string[] tokens = p.Split(',');
                for (int i = 0; i < tokens.Length; i++)
                {
                    if (i == 0)
                    {
                        valueKey = tokens[i];
                    }
                    if (i == 1)
                    {
                        conditionKey = tokens[i];
                    }
                }

                if (v.ContainsKey(valueKey) && v.ContainsKey(conditionKey))
                {
                    if (!string.IsNullOrEmpty(v[valueKey]))
                    {
                        color = v[conditionKey] == "1" ? HColor.Red : HColor.Green;
                    }
                }
            }



            if (!string.IsNullOrEmpty(color))
            {
                var bc = new BrushConverter();
                var brush = (Brush)bc.ConvertFrom(color);
                return brush;
            }

            return DependencyProperty.UnsetValue;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

    }
}
