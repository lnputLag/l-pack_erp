using Client.Common;
using Client.Interfaces.Production.PaperProduction;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// интерфейс "Отчеты по макулатуре на БДМ"
    /// <author>Грешных Н.И.</author>
    /// <released>2025-09-01</released>
    /// </summary>    
    public class ScrapPaperReportInterface
    {
        public ScrapPaperReportInterface()
        {
            Central.WM.AddTab<ScrapPaperReportTab>("main", true);
            Central.WM.ProcNavigation("main", "ScrapPaperReportTab");
        }
    }
}


