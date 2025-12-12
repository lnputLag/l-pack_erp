using Client.Common;

namespace Client.Interfaces.Sales
{
    /// <summary>
    /// Продукция не отгружаемая 90 дней
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2021-06-07</released>   
    public class SalesReportUnshippedInterface
    {
        public SalesReportUnshippedInterface()
        {
            Central.WM.AddTab("Reports", "Отчеты");
            
            var reportProductsNotShipped = new SalesUnshippedReport();            
            Central.WM.AddTab("Reports_ProductsNotShipped", "Продукция не отгружаемая 90 дней", true, "Reports", reportProductsNotShipped, "bottom");

            //ставим фокус на вкладку по умолчанию
            Central.WM.SetActive("Reports_ProductsNotShipped");
                        
        }
    }
}
