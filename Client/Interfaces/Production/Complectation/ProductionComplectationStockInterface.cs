using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Common;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// 
    /// </summary>
    /// <author>Михеев И.С.</author>
    public class ProductionComplectationStockInterface
    {
        public ProductionComplectationStockInterface()
        {
            var view = Central.WM.CheckAddTab<ComplectationStock>("ProductionComplectationStock", "Комплектация СГП", true, "main");
            Central.WM.SetActive("ProductionComplectationStock");
        }
    }
}
