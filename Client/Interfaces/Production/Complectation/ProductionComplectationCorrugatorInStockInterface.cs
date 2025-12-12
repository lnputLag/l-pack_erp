using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Common;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Комплектация ГА на участке СГП
    /// </summary>
    /// <author>sviridov_ae</author>
    public class ProductionComplectationCorrugatorInStockInterface
    {
        public ProductionComplectationCorrugatorInStockInterface()
        {
            var viewComplectationCorrugatorInStock = Central.WM.CheckAddTab<ComplectationCorrugatorInStock>("ComplectationCorrugatorInStock", "Комплектация ГА на СГП", true, "main");
            Central.WM.SetActive("ComplectationCorrugatorInStock");
        }
    }
}
