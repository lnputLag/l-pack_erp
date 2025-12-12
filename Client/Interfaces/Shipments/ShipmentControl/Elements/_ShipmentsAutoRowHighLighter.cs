using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Client.Assets.HighLighters;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// используется в AutoShipmentView
    /// </summary>
    [ValueConversion(typeof(int), typeof(Brush))]
    internal class ShipmentsAutoRowHighLighter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var v = (Dictionary<string, string>) value;

                return v != null && v["Qty"] == "0" ? HColor.Yellow : DependencyProperty.UnsetValue;
            }
            catch
            {


            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
            //   throw new NotImplementedException();
        }
    }
}
