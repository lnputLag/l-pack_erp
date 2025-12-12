using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Preproduction
{
    public class ProductionPreparationDepartmentReportsInterface
    {
        public ProductionPreparationDepartmentReportsInterface()
        {
            Central.WM.AddTab("_TechnologicalMapReports", "Отчёты по техкартам");

            Central.WM.CheckAddTab<TechnologicalMapHourlyReport>("TechnologicalMapHourlyReport", "Почасовой срез", false, "_TechnologicalMapReports", "bottom");
            Central.WM.CheckAddTab<TechnologicalMapReworkReport>("TechnologicalMapReworkReport", "Отчёт по доработкам", false, "_TechnologicalMapReports", "bottom");
            Central.WM.CheckAddTab<TechnologicalMapDesignStatusReport>("TechnologicalMapDesignStatusReport", "Отчёт по статусам дизайнеров", false, "_TechnologicalMapReports", "bottom");
            Central.WM.CheckAddTab<TechnologicalMapConstructStatusReport>("TechnologicalMapConstructStatusReport", "Отчёт по статусам конструкторов", false, "_TechnologicalMapReports", "bottom");
            Central.WM.CheckAddTab<TechnologicalMapDetailedReport>("TechnologicalMapDetailedReport", "Отчёт по техкартам", false, "_TechnologicalMapReports", "bottom");

            Central.WM.SetActive("TechnologicalMapHourlyReport");

        }
    }
}
