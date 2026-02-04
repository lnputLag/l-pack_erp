using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Client.Interfaces.Stock.RawMaterialMonitor
{
    /// <summary>
    /// Модель для сырьевой группы
    /// </summary>
    public class MaterialData
    {
        public MaterialData()
        {
            MaterialDataFormats = new List<MaterialDataFormat>();
        }

        public int IdRawGroup { get; set; }
        public string Name { get; set; }
        public List<MaterialDataFormat> MaterialDataFormats { get; set; }
        public string SumQuty { get; set; }
    }


}
