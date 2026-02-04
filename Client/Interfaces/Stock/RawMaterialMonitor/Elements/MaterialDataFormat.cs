using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Stock.RawMaterialMonitor
{
    /// <summary>
    /// Модель для форматов
    /// </summary>
    public class MaterialDataFormat
    {
        public MaterialDataFormat() { }

        public string Name { get; set; }
        public int QUTY { get; set; }
    }
}
