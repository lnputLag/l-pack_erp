using Client.Common;
using System.Collections.Generic;

namespace Client.Interfaces.Production.Monitor
{
    /// <summary>
    /// интерфейс "Мониторы"
    /// </summary>
    /// <author>balchugov_dv</author>
    public class ProductionTaskMonitorInterface
    {
        public ProductionTaskMonitorInterface()
        {

            if(!Central.WM.TabItems.ContainsKey("monitor"))
            {
                Central.WM.AddTab("monitor", "Мониторы");
            }

            var productionTaskMap1 = new ProductionTaskMap();
            {
                var item=new NavigationItem()
                {
                    AllowedRoles = new List<string>
                    {
                        "[f]admin",
                        "[i]production_task_cm_map1",
                        "[erp]production_task_cm_map1",
                    },
                    AllowedLogins=new List<string>()
                    {
                        "cm1",
                        "cm2",
                    },
                };

                if(Central.Navigator.CheckItemPermissions(item))
                {
                    Central.WM.AddTab("monitor_production_task_map1", "ПЗ ГА1", false, "monitor", productionTaskMap1, "bottom");
                    productionTaskMap1.Init(2);
                }
            }

            var productionTaskMap2 = new ProductionTaskMap();
            {
                var item=new NavigationItem()
                {
                    AllowedRoles = new List<string>
                    {
                        "[f]admin",
                        "[i]production_task_cm_map2",
                        "[erp]production_task_cm_map2",
                    },
                    AllowedLogins=new List<string>()
                    {
                        "cm1",
                        "cm2",
                    }
                };

                if(Central.Navigator.CheckItemPermissions(item))
                {
                    Central.WM.AddTab("monitor_production_task_map2", "ПЗ ГА2", false, "monitor", productionTaskMap2, "bottom");
                    productionTaskMap2.Init(21);
                }
            }
            
            //ставим фокус на вкладку по умолчанию
            //Central.WM.SetActive("monitor_production_task_map2");

            
            //если навигация не отработана, отрабатываем
            if( Central.Navigator.Address.Processed != true )
            {
                //переключим вкладку, если работает навигация            
                if( Central.Navigator.Address.AddressInner.Count > 0 )
                {
                    
                    //Если мы пришли через навигатор, смотрим на первый элемент в списке 
                    //внутренних частей адреса. В нем должно быть имя таба, который нужно открыть
                    //http://192.168.3.237/developer/l-pack-erp/client/infra/navigation                    
                    if( !string.IsNullOrEmpty( Central.Navigator.Address.AddressInner[0] ) )
                    {
                        var tab=Central.Navigator.Address.AddressInner[0];
                        switch( tab )
                        {
                            case "1":
                                if(Central.WM.TabItems.ContainsKey("monitor_production_task_map1"))
                                {
                                    Central.WM.SetActive("monitor_production_task_map1");   
                                    productionTaskMap1.ProcessNavigation();
                                }                                
                                break;

                            case "2":
                                if(Central.WM.TabItems.ContainsKey("monitor_production_task_map2"))
                                {
                                    Central.WM.SetActive("monitor_production_task_map2");   
                                    productionTaskMap2.ProcessNavigation();
                                }                                
                                break;
                        }
                    }
                }

            }
            
              
            
        }
    }
}


