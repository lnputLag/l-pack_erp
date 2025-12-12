using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Production.ProcessingMachines
{
    /// <summary>
    /// интерфейс "производственные задания на ПР"
    /// </summary>
    /// <author>sviridov_ae</author>
    class ProductionTaskPRInterface
    {
        public ProductionTaskPRInterface()
        {
            Central.WM.AddTab("ProductionTaskPr", "ПЗ на переработку");

            var productionTaskProcessingName = "ProductionTaskPr_productionTaskList";
            var productionTaskProcessingTab = Central.WM.CheckAddTab<ProductionTaskPrList>(productionTaskProcessingName, "ПЗ", false, "ProductionTaskPr", "bottom");
            productionTaskProcessingTab.TabName = productionTaskProcessingName;

            Central.WM.SetActive(productionTaskProcessingName);
        }
    }
}
