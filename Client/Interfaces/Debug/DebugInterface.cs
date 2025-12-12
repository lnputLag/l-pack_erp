using Client.Common;

namespace Client.Interfaces.Debug
{
    public class DebugInterface
    {
        public DebugInterface()
        {
            Central.WM.AddTab("Debug", "Инструменты"); 
            
            var linkMapView = new LinkMapView();            
            Central.WM.AddTab("Debug_LinkMap", "Карта", false, "Debug", linkMapView, "bottom");

            var posterList = new PosterList();            
            Central.WM.AddTab("Debug_PosterList", "Постер", false, "Debug", posterList, "bottom");

            var logView = new LogView();            
            Central.WM.AddTab("Debug_QueryLogView", "Журнал", false, "Debug", logView, "bottom");

             var queryList = new QueryList();             
            Central.WM.AddTab("Debug_QueryList", "Запросы", false, "Debug", queryList, "bottom");

            //var urlParams = new UrlParams();            
            //Central.WM.AddTab("Debug_UrlParams", "URL", false, "Debug", urlParams, "bottom");  
            //
            Central.WM.SetActive("Debug_LinkMap");
            
            
            //если навигация не отработана, отрабатываем
            if( Central.Navigator.Address.Processed != true )
            {
                //переключим вкладку, если работает навигация            
                if( Central.Navigator.Address.AddressInner.Count > 0 )
                {
                    //  Если мы пришли через навигатор, смотрим на первый элемент в списке 
                    //  внутренних частей адреса. В нем должно быть имя таба, который нужно открыть
                    //   http://192.168.3.237/developer/l-pack-erp/client/infra/navigation   
                    if( !string.IsNullOrEmpty( Central.Navigator.Address.AddressInner[0] ) )
                    {
                        var tab=Central.Navigator.Address.AddressInner[0];
                        switch( tab )
                        {
                            case "map":
                                Central.WM.SetActive("Debug_LinkMap");                            
                                break;

                            case "poster":
                                Central.WM.SetActive("Debug_PosterList");
                                if(Central.Navigator.Address.AddressInner.Count>1) 
                                {
                                    if(Central.Navigator.Address.AddressInner[1] != null)
                                    {
                                        var a2 = Central.Navigator.Address.AddressInner[1];
                                        if(a2 == "label")
                                        {
                                            var p = (PosterView)posterList.Create2();
                                            p.Labels_Make1_Click(null, null);
                                        }
                                    }
                                }
                                
                                break;

                            case "log":
                                Central.WM.SetActive("Debug_QueryLogView");
                                break;
                        }
                    }
                }

            }
            
                        
        }
    }
}
