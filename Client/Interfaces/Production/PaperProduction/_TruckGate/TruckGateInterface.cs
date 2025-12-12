using Client.Common;
using Client.Interfaces.Production.PaperProduction;
using Client.Interfaces.Stock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Интерфейс Управление въездами
    /// <author>eletskikh_ya</author>
    /// </summary>
    class TruckGateInterface
    {
        public TruckGateInterface()
        {
            //главная вкладка
            Central.WM.AddTab("TGate_manager", "Управление въездами");

            Central.WM.CheckAddTab<TruckGateConfiguration>("TGate_config", "Конфигурация ворот", false, "TGate_manager");
            Central.WM.CheckAddTab<TruckGateDirectory>("TGate_directory", "Состояние датчиков", false, "TGate_manager");
            Central.WM.CheckAddTab<TruckGateManagement>("TGate_management", "Управление воротами", false, "TGate_manager");

            Central.WM.SetActive("TGate_config");
        }
    }
}
