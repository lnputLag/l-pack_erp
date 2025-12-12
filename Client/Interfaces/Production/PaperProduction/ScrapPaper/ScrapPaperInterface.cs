using Client.Common;
using Client.Interfaces.Production.PaperProduction;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// интерфейс "Прием макулатуры на БДМ"
    /// <author>Грешных Н.И.</author>
    /// <released>2025-09-19</released>
    /// </summary>    
    public class ScrapPaperInterface
    {
        public ScrapPaperInterface(int idStNumber = 0)
        {
            switch (idStNumber)
            {
                case 1:

                    var scrap_paper1Tab = Central.WM.CheckAddTab<ScrapPaperTab>("Scrap_Paper1Tab", "Прием макулатуры БДМ1", true);
                    scrap_paper1Tab.IdSt = 716;
                    Central.WM.SetActive("Scrap_Paper1Tab");
                    break;
                case 2:
                    Central.WM.AddTab("ScrapPaper", "Прием макулатуры БДМ2");
                    var scrap_paper2Tab = Central.WM.CheckAddTab<ScrapPaperTab>("Scrap_Paper2Tab", "Прием макулатуры", false, "ScrapPaper", "top");
                    var scrapPaper2TerminalSlotInterfsce = Central.WM.CheckAddTab<ScrapPaperTerminalSlotTab>("Scrap_PaperTerminalSlotTab", "План разгрузок", false, "ScrapPaper", "top");
                    scrap_paper2Tab.IdSt = 1716;
                    Central.WM.SetActive("Scrap_Paper2Tab");
                    break;
                case 3:
                    var scrap_paper3Tab = Central.WM.CheckAddTab<ScrapPaperTab>("Scrap_Paper3Tab", "Прием макулатуры ЛТ", true);
                    scrap_paper3Tab.IdSt = 2716;
                    Central.WM.SetActive("Scrap_Paper3Tab");
                    break;
            }

            /*
            Central.WM.AddTab("pm_scrappaper", "Прием макулатуры");
            Central.WM.AddTab<ScrapPaperTab>("pm_scrappaper");
            Central.WM.AddTab<ScrapPaperTerminalSlotTab>("pm_scrappaper");
            Central.WM.ProcNavigation("pm_scrappaper", "ScrapPaperTab");
            */

            //// одна вкладка
            ////var scrap_paperTab = Central.WM.CheckAddTab<ScrapPaperTab>("ScrapPaperTab", "Прием макулатуры", true);
            ////Central.WM.SetActive("ScrapPaperTab");

            //// родительская вкладка
            //Central.WM.AddTab("ScrapPaper", "Прием макулатуры");
            //// дочерние повкладки
            //var scrapPaperInterface = Central.WM.CheckAddTab<ScrapPaperTab>("Scrap_PaperTab", "Разгрузка", false, "ScrapPaper", "bottom");
            //var scrapPaperReportInterfsce = Central.WM.CheckAddTab<ScrapPaperReportTab>("Scrap_PaperReportTab", "Отчеты", false, "ScrapPaper", "bottom");
            //var scrapPaperTerminalSlotInterfsce = Central.WM.CheckAddTab<ScrapPaperTerminalSlotTab>("Scrap_PaperTerminalSlotTab", "План разгрузок", false, "ScrapPaper", "bottom");

            ////ставим фокус на вкладку по умолчанию
            //Central.WM.SetActive("Scrap_PaperTab");
        }
    }
}


