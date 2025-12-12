using Client.Common;

namespace Client.Interfaces.Sales
{
    /// <summary>
    /// Продажи
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2022-05-16</released>   
    public class SalesReportSecondaryInterface
    {
        
        public SalesReportSecondaryInterface()
        {
            Central.WM.AddTab("Reports", "Отчеты");
            
            
            var salesSecondaryReport = new SalesSecondaryReport();            
            Central.WM.AddTab("reports_sales_secondary", "Вторичные продажи", true, "Reports", salesSecondaryReport, "bottom");

            //ставим фокус на вкладку по умолчанию
            Central.WM.SetActive("Reports");
                        
        }
    }
}
