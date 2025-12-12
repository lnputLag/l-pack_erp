using Client.Common;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// интерфейс "управление отгрузками"
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2020-06-04</released>
    /// <changed>2023-08-23</changed>
    public class ShipmentControlInterface
    {
        public ShipmentControlInterface()
        {
            Central.WM.AddTab("ShipmentsControl", "Управление отгрузками");
                var shipmentsList = Central.WM.CheckAddTab<ShipmentsList>("ShipmentsControl_List", "Список", false, "ShipmentsControl", "bottom");
                var shipmentsPlan = Central.WM.CheckAddTab<ShipmentPlan>("ShipmentsControl_Plan", "План", false, "ShipmentsControl", "bottom");

            Central.WM.AddTab("ShipmentsControl_Monitor", "Монитор", false, "ShipmentsControl");
            var shipmentsMonitor = Central.WM.CheckAddTab<ShipmentsMonitorTerminal>("ShipmentsControl_MonitorTerminal", "Монитор терминалов", false, "ShipmentsControl_Monitor", "bottom");
            var shipmentsMonitorForkliftDriver = Central.WM.CheckAddTab<ShipmentsMonitorForkliftDriver>("ShipmentsControl_MonitorForkliftDriver", "Монитор погрузчиков", false, "ShipmentsControl_Monitor", "bottom");

                var shipmentsReport = Central.WM.CheckAddTab<ShipmentsReport>("ShipmentsControl_Report", "Отчет", false, "ShipmentsControl", "bottom");
                var shipmentsStatistic = Central.WM.CheckAddTab<ShipmentStatistic>("ShipmentStatistic", "Статистика", false, "ShipmentsControl", "bottom");

            Central.WM.AddTab("ShipmentsControl_Equipment", "Оснастка", false, "ShipmentsControl");
                    var shipmentSamples=Central.WM.CheckAddTab<ShipmentSamples>("ShipmentsControl_Equipment_Samples", "Образцы", false, "ShipmentsControl_Equipment", "bottom");
                    var shipmentCliche=Central.WM.CheckAddTab<ShipmentCliche>("ShipmentsControl_Equipment_Cliche", "Клише", false, "ShipmentsControl_Equipment", "bottom");
                    var shipmentShtanz=Central.WM.CheckAddTab<ShipmentShtanz>("ShipmentsControl_Equipment_Shtanzforms", "Штанцформы", false, "ShipmentsControl_Equipment", "bottom");
                    //Central.WM.SetActive("ShipmentsControl_Equipment_Samples");
                

            Central.WM.SetActive("ShipmentsControl_List");
             
            //если навигация не отработана, отрабатываем
            if (Central.Navigator.Address.Processed != true)
            {
                //переключим вкладку, если работает навигация            
                if (Central.Navigator.Address.AddressInner.Count > 0)
                {
                    
                     //  Если мы пришли через навигатор, смотрим на первый элемент в списке 
                     //  внутренних частей адреса. В нем должно быть имя таба, который нужно открыть
                     //   http://192.168.3.237/developer/l-pack-erp/client/infra/navigation
                     
                    if (!string.IsNullOrEmpty(Central.Navigator.Address.AddressInner[0]))
                    {
                        var tab = Central.Navigator.Address.AddressInner[0];
                        switch (tab)
                        {
                            case "list":
                                Central.WM.SetActive("ShipmentsControl_List");
                                shipmentsList.ProcessNavigation();
                                break;

                            case "plan":
                                Central.WM.SetActive("ShipmentsControl_Plan");
                                shipmentsPlan.ProcessNavigation();
                                break;

                            case "monitor":
                                Central.WM.SetActive("ShipmentsControl_Monitor");
                                shipmentsMonitor.ProcessNavigation();
                                break;

                            case "report":
                                Central.WM.SetActive("ShipmentsControl_Report");
                                shipmentsReport.ProcessNavigation();
                                break;

                            case "equipment":
                                Central.WM.SetActive("ShipmentsControl_Equipment");

                                if (!string.IsNullOrEmpty(Central.Navigator.Address.AddressInner[1]))
                                {
                                    var tab2 = Central.Navigator.Address.AddressInner[1];
                                    switch (tab2)
                                    {
                                        case "samples":
                                            Central.WM.SetActive("ShipmentsControl_Equipment_Samples");
                                            shipmentSamples.ProcessNavigation();                                            
                                            break;

                                        case "clishe":
                                            Central.WM.SetActive("ShipmentsControl_Equipment_Cliche");
                                            shipmentCliche.ProcessNavigation();                                            
                                            break;

                                        case "shtanzforms":
                                            Central.WM.SetActive("ShipmentsControl_Equipment_Shtanzforms");
                                            shipmentShtanz.ProcessNavigation();
                                            break;
                                    }
                                }

                                break;
                        }
                    }
                }
            }
            
        }
    }
}
