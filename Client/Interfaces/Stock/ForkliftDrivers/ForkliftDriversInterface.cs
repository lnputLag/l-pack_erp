using Client.Common;

namespace Client.Interfaces.Stock.ForkliftDrivers
{
    /// <summary>
    /// интерфейс погрузчики
    /// </summary>
    /// <author>Михеев И.С.</author>
    public class ForkliftDriversInterface
    {
        public ForkliftDriversInterface()
        {
            Central.WM.AddTab("ForkliftDrivers", "Погрузчики");

            var forkliftDriverLog=new ForkliftDriverLog();
            Central.WM.AddTab("ForkliftDrivers_Log", "Журнал работ", false, "ForkliftDrivers", forkliftDriverLog, "bottom");

            var forkliftDriverList=new ForkliftDriverList();
            Central.WM.AddTab("ForkliftDrivers_List", "Водители", false, "ForkliftDrivers", forkliftDriverList, "bottom");

            Central.WM.SetActive("ForkliftDrivers_Log");

            //
            //если навигация не отработана, отрабатываем
            if (Central.Navigator.Address.Processed != true)
            {
                //переключим вкладку, если работает навигация            
                if (Central.Navigator.Address.AddressInner.Count > 0)
                {
                    
                    //  Если мы пришли через навигатор, смотрим на первый элемент в списке 
                    //  внутренних частей адреса. В нем должно быть имя таба, который нужно открыть
                    //   http://192.168.3.237/developer/l-pack-erp/client/infra/navigation

                    /*
                        l-pack://l-pack_erp/stock/forkliftdrivers

                        l-pack://l-pack_erp/stock/forkliftdrivers/log
                        l-pack://l-pack_erp/stock/forkliftdrivers/list
                     */
                     
                    if (!string.IsNullOrEmpty(Central.Navigator.Address.AddressInner[0]))
                    {
                        var tab = Central.Navigator.Address.AddressInner[0];
                        switch (tab)
                        {
                            case "log":
                                Central.WM.SetActive("ForkliftDrivers_Log");
                                forkliftDriverLog.ProcessNavigation();
                                break;

                            case "list":
                                Central.WM.SetActive("ForkliftDrivers_List");
                                forkliftDriverList.ProcessNavigation();
                                break;

                        }
                    }
                }

            }
        }

    }
}
