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

        // свойства для DataGrid
        //public int LayersCount => Layers?.GroupBy(l => l.LayerNumber).Count() ?? 0;
   
    }

}
