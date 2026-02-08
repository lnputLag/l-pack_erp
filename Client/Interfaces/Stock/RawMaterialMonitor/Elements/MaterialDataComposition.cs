using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Client.Interfaces.Stock.RawMaterialMonitor
{
    /// <summary>
    /// Модель для сырьевой композиции
    /// </summary>
    public class MaterialDataComposition
    {
        public MaterialDataComposition()
        {
            Layers = new List<MaterialLayerData>();
        }

        public int Idc { get; set; }
        public string CartonName { get; set; }
        public List<MaterialLayerData> Layers { get; set; }
        public int TotalStockKg => Layers.Sum(l => l.TotalStockKg);

        // Константы для порогов (можно вынести в настройки)
        private const int CRITICAL_THRESHOLD = 10000;      // 10 кг
        private const int LOW_THRESHOLD = 50000;           // 50 кг
        private const int HIGH_THRESHOLD = 100000;         // 100 кг

        // Новые свойства для DataGrid
        public int LayersCount => Layers?.GroupBy(l => l.LayerNumber).Count() ?? 0;

        public int ProblemLayersCount
        {
            get
            {
                if (Layers == null) return 0;
                return Layers.Count(l => l.TotalStockKg <= CRITICAL_THRESHOLD);
            }
        }

        public bool IsCritical => TotalStockKg <= CRITICAL_THRESHOLD;
        public bool IsLow => TotalStockKg > CRITICAL_THRESHOLD && TotalStockKg <= LOW_THRESHOLD;

        public SolidColorBrush ProblemTextColor
        {
            get
            {
                if (ProblemLayersCount == 0) return new SolidColorBrush(Colors.Gray);
                if (ProblemLayersCount <= 2) return new SolidColorBrush(Colors.Orange);
                return new SolidColorBrush(Colors.Red);
            }
        }

        // Свойства для отображения в UI
        public SolidColorBrush StatusColor
        {
            get
            {
                if (TotalStockKg <= CRITICAL_THRESHOLD)
                    return new SolidColorBrush(Color.FromArgb(30, 244, 67, 67)); // Красный, прозрачный
                else if (TotalStockKg <= LOW_THRESHOLD)  
                    return new SolidColorBrush(Color.FromArgb(30, 255, 152, 0)); // Оранжевый
                return Brushes.Transparent;
            }
        }

        public SolidColorBrush StatusIndicatorColor
        {
            get
            {
                if (TotalStockKg <= CRITICAL_THRESHOLD)
                    return new SolidColorBrush(Colors.Red);
                else if (TotalStockKg <= LOW_THRESHOLD)  
                    return new SolidColorBrush(Colors.Orange);
                else if (TotalStockKg >= HIGH_THRESHOLD)
                    return new SolidColorBrush(Colors.Green);
                return new SolidColorBrush(Colors.Gray);
            }
        }

        public FontWeight StatusFontWeight
        {
            get
            {
                return (TotalStockKg <= CRITICAL_THRESHOLD) ?
                    FontWeights.Bold : FontWeights.Normal;
            }
        }

        public SolidColorBrush StockTextColor
        {
            get
            {
                if (TotalStockKg == 0)
                    return new SolidColorBrush(Colors.Gray);
                if (TotalStockKg <= CRITICAL_THRESHOLD)
                    return new SolidColorBrush(Colors.Red);
                if (TotalStockKg <= LOW_THRESHOLD)  
                    return new SolidColorBrush(Colors.Orange);
                if (TotalStockKg >= HIGH_THRESHOLD)
                    return new SolidColorBrush(Colors.Green);
                return new SolidColorBrush(Colors.Black);
            }
        }
    }

}
