using Client.Common;
using Client.Interfaces.Stock.Condition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// интерфейс "Состояние склада"
    /// </summary>
    /// <author>sviridov_ae</author>
    public class WarehouseConditionInterface
    {
        public WarehouseConditionInterface()
        {
            Central.WM.AddTab("stock_condition", "Состояние склада");

            var warehouseConditionList = new WarehouseConditionList();
            Central.WM.AddTab("stock_condition_cells", "Состояние ячеек", false, "stock_condition", warehouseConditionList, "bottom");

            Central.WM.AddTab<StockConditionProduct>("stock_condition");
            
            Central.WM.SetActive("stock_condition_cells");
        }        
    }
}
