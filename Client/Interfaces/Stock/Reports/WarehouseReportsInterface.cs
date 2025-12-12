using Client.Common;
using Client.Interfaces.Accounts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Stock
{
    internal class WarehouseReportsInterface
    {
        /// <summary>
        /// отчеты по складу
        /// </summary>
        /// <author>eletskikh_ya</author>
        public WarehouseReportsInterface() 
        {
            Central.WM.AddTab("WarehouseReports", "Отчёты по складу");
            Central.WM.AddTab<ReportProduct>("WarehouseReports");
            Central.WM.AddTab<ReportProductTurnover>("WarehouseReports");
            Central.WM.AddTab<ReportProductStockMovement>("WarehouseReports");
            Central.WM.AddTab<ReportRollMovement>("WarehouseReports");
            Central.WM.AddTab<ReportInventoryItemBalance>("WarehouseReports");
            Central.WM.AddTab<ReportInventoryItemPlace>("WarehouseReports");
            Central.WM.AddTab<ReportInventoryItemMovement>("WarehouseReports");
            Central.WM.AddTab<ReportWarehouseFullness>("WarehouseReports");
            Central.WM.AddTab<ReportEmployeePerfomance>("WarehouseReports");
            Central.WM.AddTab<ReportForkliftPerformance>("WarehouseReports");
            Central.WM.AddTab<ReportForkliftDriverFirstMovingFromStrapper>("WarehouseReports");

            // FIXME: отчет не доработан
            //Central.WM.AddTab<ReportSpeed>("WarehouseReports");

            Central.WM.SetActive("ReportProduct");
        }
    }
}
