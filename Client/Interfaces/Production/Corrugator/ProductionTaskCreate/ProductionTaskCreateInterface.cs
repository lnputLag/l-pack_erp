using Client.Common;

namespace Client.Interfaces.Production.CreatingTasks
{
    /// <summary>
    /// интерфейс "создание производственных заданий"
    /// </summary>
    /// <author>balchugov_dv</author>
    public class ProductionTaskCMCreateInterface
    {
        public ProductionTaskCMCreateInterface()
        {
            
            var cuttingAuto=new ProductionTaskAuto();
            Central.WM.AddTab("CreatingTasks_cuttingAuto", "Автораскрой",true, "", cuttingAuto);
            
            //ставим фокус на вкладку по умолчанию
            Central.WM.SetActive("CreatingTasks_cuttingAuto");

            
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
                            case "auto":
                                Central.WM.SetActive("CreatingTasks_cuttingAuto");                            
                                break;

                        }
                    }
                }

            }


        }
    }
}
