using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Client.Interfaces.Stock.RawMaterialMonitor
{
    /// <summary>
    /// Класс для группировки по композициям в сводках
    /// </summary>
    public class MaterialCompositionSummaryItem
    {
        public string CartonName { get; set; }
        public int Idc { get; set; }
        public int TotalStockKg { get; set; }
        public string Category { get; set; }

        public SolidColorBrush CategoryColor
        {
            get
            {
                return Category switch
                {
                    "critical" => new SolidColorBrush(Colors.Red),
                    "low" => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                    "high" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
        }

        // Форматированная строка с пробелами вместо запятых
        public string FormattedStock => FormatNumberWithSpaces(TotalStockKg);

        private string FormatNumberWithSpaces(int number)
        {
            return number.ToString("N0").Replace(",", " ");
        }
    }
}
