using Client.Common;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Отчёты по работе гофроагрегата
    /// </summary>
    /// <author>sviridov_ae</author>
    public class CorrugatorMachineReportInterface
    {
        public CorrugatorMachineReportInterface()
        {
            Central.WM.AddTab("ReportCM", "Отчёты ГА");

            Central.WM.CheckAddTab<CorrugatorMachineReportWriteOff>("ReportCM_WriteOff", "Списания ГА", false, "ReportCM", "bottom");
            //Central.WM.CheckAddTab<CorrugatorMachineReportIdles>("ReportCM_Idles", "Простои ГА", false, "ReportCM", "bottom");
            //Central.WM.CheckAddTab<CorrugatorMachineReportDefects>("ReportCM_Defects", "Брак ГА", false, "ReportCM", "bottom");
            Central.WM.CheckAddTab<CorrugatorMachineReportAddition>("ReportCM_Addition", "Добавление метров", false, "ReportCM", "bottom");
            Central.WM.CheckAddTab<CorrugatorMachineReportRework>("ReportCM_Rework", "Перевыгон", false, "ReportCM", "bottom");

            //Central.WM.CheckAddTab<CorrugatorMachineReportIndicators>("ReportCM_Indicators", "Показатели ГА", false, "ReportCM", "bottom");

            Central.WM.SetActive("ReportCM_WriteOff");
        }
    }
}
