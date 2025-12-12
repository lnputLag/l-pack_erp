using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Orders
{
    public class ScrapPaperKshInterface
    {
        public ScrapPaperKshInterface()
        {
            var scrapPaperConsumptionKshList = Central.WM.CheckAddTab<OrderScrapPaperConsumptionKshList>("OrderScrapPaperConsumptionKshList", "Макулатура КШ", true);
            Central.WM.SetActive("OrderScrapPaperConsumptionKshList");
        }
    }
}
