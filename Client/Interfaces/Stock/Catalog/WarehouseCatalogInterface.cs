using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Stock.WMS
{
    /// <summary>
    /// Интерфейс работы Справочник склада
    /// </summary>
    /// <author>eletskikh_ya</author>
    class WarehouseCatalogInterface
    {
        public WarehouseCatalogInterface()
        {
            Central.WM.AddTab("WarehouseCatalog", "Справочник склада");
            Central.WM.CheckAddTab<CatalogTopology>("CatalogTopology", "Топология склада", false, "WarehouseCatalog");
            Central.WM.CheckAddTab<CatalogCellType>("CatalogCellType", "Тип ячейки", false, "WarehouseCatalog");
            Central.WM.CheckAddTab<CatalogArea>("CatalogArea", "Область хранения", false, "WarehouseCatalog");
            Central.WM.CheckAddTab<CatalogInventory>("CatalogInventory", "Справочник ТМЦ", false, "WarehouseCatalog");

            Central.WM.AddTab("CatalogRack", "Стеллажи", false, "WarehouseCatalog");
            Central.WM.CheckAddTab<CatalogRackTopology>("CatalogRackTopology", "Топология стеллажей", false, "CatalogRack");
            Central.WM.CheckAddTab<CatalogRackShelfType>("CatalogRackShelfType", "Тип полки стеллажа", false, "CatalogRack");

            Central.WM.SetActive("CatalogTopology");
        }
    }
}
