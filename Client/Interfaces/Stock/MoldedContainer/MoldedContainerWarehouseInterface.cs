using Client.Common;
using Client.Interfaces.Orders.MoldedContainer;
using Client.Interfaces.Production;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Интерфейс Склад литой тары
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public class MoldedContainerWarehouseInterface
    {
        public MoldedContainerWarehouseInterface()
        {
            Central.WM.AddTab("MoldedContainerWarehouse", "Склад ЛТ");
            var moldedContainerWarehouseScrapPaper = Central.WM.CheckAddTab<MoldedContainerWarehouseScrapPaper>("MoldedContainerWarehouseScrapPaper", "Склад макулатуры", false, "MoldedContainerWarehouse");
            var moldedContainerWarehouseGoods = Central.WM.CheckAddTab<MoldedContainerWarehouseGoods>("MoldedContainerWarehouseGoods", "Склад готовой продукции", false, "MoldedContainerWarehouse");

            Central.WM.SetActive("MoldedContainerWarehouseScrapPaper");
        }
    }
}
