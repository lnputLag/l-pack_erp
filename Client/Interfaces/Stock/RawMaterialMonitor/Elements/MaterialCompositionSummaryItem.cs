using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Client.Interfaces.Stock.RawMaterialMonitor
{
    public class MaterialCompositionSummaryItem
    {
        public string CartonName { get; set; }
        public int Idc { get; set; }
        public int TotalStockKg { get; set; }
        public string Category { get; set; }
        public int ProblemWidthsCount { get; set; } // Количество проблемных форматов

        public SolidColorBrush CategoryColor
        {
            get
            {
                return Category switch
                {
                    "zero" => new SolidColorBrush(Colors.Gray),
                    "critical" => new SolidColorBrush(Colors.Red),
                    "low" => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                    "high" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
        }

        public string CategoryText
        {
            get
            {
                return Category switch
                {
                    "zero" => $"❌ Композиции с {ProblemWidthsCount} нулевыми форматами",
                    "critical" => $"🔴 Композиции с {ProblemWidthsCount} критическими форматами",
                    "low" => $"🟠 Композиции с {ProblemWidthsCount} низкими форматами",
                    "high" => $"🟢 Композиции с {ProblemWidthsCount} большими форматами",
                    _ => "Неизвестно"
                };
            }
        }
    }
}
