using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Sales
{
    public class SaleInterface
    {
        public SaleInterface()
        {
            Central.WM.AddTab("SaleInterface", "Расходные накладные");

            var saleListView = Central.WM.CheckAddTab<SaleList>("SaleList", "Список продаж", false, "SaleInterface");
            var adjustmentListView = Central.WM.CheckAddTab<AdjustmentList>("AdjustmentList", "Список корректировок", false, "SaleInterface");
            Central.WM.SetActive("SaleList");
        }
    }
}
