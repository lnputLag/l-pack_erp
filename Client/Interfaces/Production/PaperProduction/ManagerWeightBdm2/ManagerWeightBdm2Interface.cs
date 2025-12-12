using Client.Common;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// интерфейс "Весовая на БДМ2"
    /// <author>Грешных Н.И.</author>
    /// </summary>    
    public class _ManagerWeightBdm2Interface
    {
        public _ManagerWeightBdm2Interface()
        {

            //FIXME: здесь нужно использовать новый формат: http://192.168.3.237/developer/erp2/client/dev/notes/2023-10-20_tab_base
            var managerWeightBdm2List = Central.WM.CheckAddTab<ManagerWeightBdm2List>("ManagerWeightBdm2_ManagementList", "Весовая БДМ", true, "", "bottom");
            Central.WM.SetActive("ManagerWeightBdm2_ManagementList");

            //если навигация не отработана, отрабатываем
            if ( Central.Navigator.Address.Processed != true )
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
                                Central.WM.SetActive("ManagerWeightBdm2_ManagementList");                            
                                break;
                        }
                    }
                }

            }
                        
        }
    }
}


