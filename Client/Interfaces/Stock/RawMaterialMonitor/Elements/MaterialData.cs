using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Stock.RawMaterialMonitor
{
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
    
    public class MaterialDataFormat
    {
    public MaterialDataFormat() { }

    public string Name { get; set; }
    public int QUTY { get; set; }
}
}
