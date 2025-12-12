using Client.Common;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// интерфейс "производственные задания на ГА"
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <worklog>ryasnoy_pv 24.08.2023 Вкладку Планирование перенес в отдельный интерфейс</worklog>
    public class ProductionTaskCMInterface
    {
        public ProductionTaskCMInterface()
        {

            var productionTaskList = Central.WM.CheckAddTab<ProductionTaskList>("ProductionTask_productionTaskList", "ПЗ на ГА", true, "", "bottom");
            
            Central.WM.SetActive("ProductionTask_productionTaskList");

            //если навигация не отработана, отрабатываем
            if( Central.Navigator.Address.Processed != true )
            {
                //переключим вкладку, если работает навигация            
                if( Central.Navigator.Address.AddressInner.Count > 0 )
                {
                    /*
                        Если мы пришли через навигатор, смотрим на первый элемент в списке 
                        внутренних частей адреса. В нем должно быть имя таба, который нужно открыть
                        http://192.168.3.237/developer/l-pack-erp/client/infra/navigation
                     */
                    if( !string.IsNullOrEmpty( Central.Navigator.Address.AddressInner[0] ) )
                    {
                        var tab=Central.Navigator.Address.AddressInner[0];
                        switch( tab )
                        {
                            case "listing":
                                Central.WM.SetActive("ProductionTask_productionTaskList");                            
                                break;
                        }
                    }
                }

            }
                        
        }
    }
}


