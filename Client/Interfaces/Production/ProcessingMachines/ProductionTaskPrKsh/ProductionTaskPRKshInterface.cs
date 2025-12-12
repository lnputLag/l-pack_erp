using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Production.ProcessingMachines
{
    /// <summary>
    /// интерфейс "производственные задания на ПР" для площадки Кашира
    /// </summary>
    /// <author>sviridov_ae</author>
    class ProductionTaskPRKshInterface
    {
        public ProductionTaskPRKshInterface()
        {
            Central.WM.AddTab("ProductionTaskPrKsh", "ПЗ на переработку КШ");

            var productionTaskProcessingName = "ProductionTaskPrKsh_productionTaskList";
            var productionTaskProcessingTab = Central.WM.CheckAddTab<ProductionTaskPrKshList>(productionTaskProcessingName, "ПЗ КШ", false, "ProductionTaskPrKsh", "bottom");
            productionTaskProcessingTab.TabName = productionTaskProcessingName;

            Central.WM.SetActive(productionTaskProcessingName);
        }
    }
}
