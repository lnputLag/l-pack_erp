using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Sales
{
    public class ResponsibleStockInterface
    {
        public ResponsibleStockInterface()
        {
            Central.WM.AddTab("ResponsibleStock", "Склад ответственного хранения");

            var responsibleStockListView = Central.WM.CheckAddTab<ResponsibleStockList>("ResponsibleStockList", "Поставка на СОХ", false, "ResponsibleStock");
            var responsibleStockBalanceView = Central.WM.CheckAddTab<ResponsibleStockBalance>("ResponsibleStockBalance_PalletList", "Список поддонов", false, "ResponsibleStock");
            var responsibleStockProductBalanceView = Central.WM.CheckAddTab<ResponsibleStockProductBalance>("ResponsibleStockBalance_ProductList", "Список продукции", false, "ResponsibleStock");
            var responsibleStockListOperationView = Central.WM.CheckAddTab<ResponsibleStockListOperation>("ResponsibleStockListOperation", "Список операций", false, "ResponsibleStock");

            Central.WM.SetActive("ResponsibleStockList");
        }
    }
}
