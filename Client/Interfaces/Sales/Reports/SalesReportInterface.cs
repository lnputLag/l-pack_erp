using Client.Common;

namespace Client.Interfaces.Sales
{
    /// <summary>
    /// Продажи
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2021-06-07</released>   
    public class SalesReportInterface
    {
        
        public SalesReportInterface()
        {
            Central.WM.AddTab("Reports", "Отчеты");
            
            
            var reportSalesView = new SalesReport();            
            Central.WM.AddTab("Report_Sales", "Отчет по продажам", true, "Reports", reportSalesView, "bottom");

            //ставим фокус на вкладку по умолчанию
            Central.WM.SetActive("Report_Sales");
                        
        }
    }
}
