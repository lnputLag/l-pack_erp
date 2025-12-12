using System.Collections.Generic;
using Client.Common;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Операции по складу
    /// </summary>
    /// <author>Михеев И.С.</author>
    public class WarehouseOperationsInterface
    {
        public WarehouseOperationsInterface()
        {
            var writeOffListView = Central.WM.CheckAddTab<OperationWriteOffList>("OperationWriteOffList", "Список списаний", true);
            Central.WM.SetActive("OperationWriteOffList");

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
                        l-pack://l-pack_erp/stock/stock_operations/writeoff
                     */

                    if (!string.IsNullOrEmpty(Central.Navigator.Address.AddressInner[0]))
                    {
                        var tab = Central.Navigator.Address.AddressInner[0];
                        switch (tab)
                        {
                            case "writeoff":
                                Central.WM.SetActive("OperationWriteOffList");
                                break;
                        }
                    }
                }
            }
        }
    }
}
