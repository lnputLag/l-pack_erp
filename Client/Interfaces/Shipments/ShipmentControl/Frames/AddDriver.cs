using Client.Common;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// интерфейс "Добавление водителя"
    /// </summary>
    /// <author>balchugov_dv</author>  
    public class AddDriver
    {
        public AddDriver()
        {
            Central.WM.AddTab("AddDriver", "Регистрация водителя", true, "add");
          
            var driverExpectedList=new DriverListExpected();
            Central.WM.AddTab("AddDriver_ExpectedDrivers", "Ожидаемые водители", false, "AddDriver",driverExpectedList);
            
            var driverAllList=new DriverListAll();
            driverAllList.Init();
            Central.WM.AddTab("AddDriver_AllDrivers", "Все водители", false, "AddDriver",driverAllList);

            Central.WM.SetActive("AddDriver_ExpectedDrivers");                        
        }
    }
}
