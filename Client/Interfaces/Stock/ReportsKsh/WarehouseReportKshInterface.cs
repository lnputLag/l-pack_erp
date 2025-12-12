using Client.Common;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// отчеты по складу Кашира
    /// </summary>
    /// <author>sviridov_ae</author>
    public class WarehouseReportKshInterface
    {
        public WarehouseReportKshInterface() 
        {
            Central.WM.AddTab("WarehouseReportKsh", "Отчёты по складу КШ");
            Central.WM.AddTab<ReportProductKsh>("WarehouseReportKsh");
            Central.WM.AddTab<ReportProductTurnoverKsh>("WarehouseReportKsh");
            Central.WM.AddTab<ReportInventoryItemBalanceKsh>("WarehouseReportKsh");
            Central.WM.AddTab<ReportInventoryItemPlaceKsh>("WarehouseReportKsh");
            Central.WM.AddTab<ReportInventoryItemMovementKsh>("WarehouseReportKsh");
            Central.WM.AddTab<ReportWarehouseFullnessKsh>("WarehouseReportKsh");
            Central.WM.AddTab<ReportEmployeePerfomanceKsh>("WarehouseReportKsh");
            Central.WM.AddTab<ReportForkliftPerformanceKsh>("WarehouseReportKsh");
            Central.WM.AddTab<ReportForkliftDriverFirstMovingFromStrapperKsh>("WarehouseReportKsh"); 
            Central.WM.SetActive("ReportProductKsh");
        }
    }
}
