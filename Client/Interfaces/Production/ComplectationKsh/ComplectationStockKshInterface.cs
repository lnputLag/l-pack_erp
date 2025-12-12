using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Common;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Комплектация СГП Кашира
    /// </summary>
    /// <author>sviridov_ae</author>
    public class ComplectationStockKshInterface
    {
        public ComplectationStockKshInterface()
        {
            var viewComplectationStockKsh = Central.WM.CheckAddTab<ComplectationStockKsh>("ComplectationStockKsh", "Комплектация СГП КШ", true, "main");
            Central.WM.SetActive("ComplectationStockKsh");
        }
    }
}
