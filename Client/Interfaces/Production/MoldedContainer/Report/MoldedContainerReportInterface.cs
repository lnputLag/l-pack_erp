using Client.Common;
using Client.Interfaces.Production.MoldedContainer.Report.Tabs;

namespace Client.Interfaces.Production.MoldedContainer
{ 
    /// <summary>
    /// интерфейс для отчетов по производству ЛТ
    /// </summary>
    /// <author>greshnyh_ni</author>
    /// <version>1</version>
    /// <released>2024-07-16</released>
    /// <changed>2024-07-16</changed>
    public class MoldedContainerReportInterface
    {
        public MoldedContainerReportInterface()
        {
            Central.WM.AddTab("MoldedContainerReport", "Отчеты по ЛТ");
            var reportInterface = Central.WM.CheckAddTab<MoldedContainerReportPz>("MoldedContainerReport_Pz", "Отчет по ПЗ", false, "MoldedContainerReport", "bottom");
            var moldedContainerReportProductionTaskByShift = Central.WM.CheckAddTab<MoldedContainerReportProductionTaskByShift>("MoldedContainerReportProductionTaskByShift", "Отчет по сменам", false, "MoldedContainerReport", "bottom");
            var reportInterface2 = Central.WM.CheckAddTab<MoldedContainerReportOrder>("MoldedContainerReport_Order",
                "Отчет по заявкам", false, "MoldedContainerReport", "bottom");
            var reportInterface3 = Central.WM.CheckAddTab<MoldedContainerReportRaw>("MoldedContainerReportRaw", "Отчет по сырью", false, "MoldedContainerReport", "bottom");
            //ставим фокус на вкладку по умолчанию
            Central.WM.SetActive("MoldedContainerReport_Pz");

        }
    }
}
