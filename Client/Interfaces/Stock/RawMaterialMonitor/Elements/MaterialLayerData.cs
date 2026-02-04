using DevExpress.XtraRichEdit.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Stock.RawMaterialMonitor
{
    /// <summary>
    /// Данные слоя в композиции
    /// </summary>
    public class MaterialLayerData
    {
        public MaterialLayerData()
        {
            Widths = new List<MaterialWidthData>();
        }

        public string LayerNumber { get; set; }
        public string RawGroup { get; set; }    
        public List<MaterialWidthData> Widths { get; set; }
        public int TotalStockKg => Widths.Sum(w => w.StockKg);
    }
}
