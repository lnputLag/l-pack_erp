using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Client.Interfaces.Stock.RawMaterialMonitor
{
    public class MaterialFormatDistributionItem
    {
        public int Width { get; set; }
        public int TotalStockKg { get; set; }
        public double Percentage { get; set; }
        public int CompositionCount { get; set; }

        // Форматированная строка с пробелами
        public string FormattedStock => FormatNumberWithSpaces(TotalStockKg);

        public string FormattedPercentage => $"{Percentage:0.0}%";

        private string FormatNumberWithSpaces(int number)
        {
            return number.ToString("N0").Replace(",", " ");
        }

        public SolidColorBrush ChartColor
        {
            get
            {
                // Генерируем цвет на основе ширины формата
                int hue = (Width % 360) * 10;
                return new SolidColorBrush(Color.FromArgb(200,
                    (byte)((hue * 5) % 255),
                    (byte)((hue * 3) % 255),
                    (byte)((hue * 7) % 255)));
            }
        }
    }
}
