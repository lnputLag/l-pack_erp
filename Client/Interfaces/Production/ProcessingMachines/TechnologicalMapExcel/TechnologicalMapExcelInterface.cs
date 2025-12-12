using Client.Common;

namespace Client.Interfaces.Production.ProcessingMachines
{
    public class TechnologicalMapExcelInterface
    {
        public TechnologicalMapExcelInterface()
        {
            var tkExcelView = Central.WM.CheckAddTab<TechnologicalMapExcel>("TechnologicalMapExcel", "Техкарта по ПЗ", true, "", "bottom");

            Central.WM.SetActive("TechnologicalMapExcel");
        }
    }
}
