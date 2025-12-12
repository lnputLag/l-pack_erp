using Client.Common;
using Client.Interfaces.Production.MoldedContainer;
using System.Collections.Generic;

namespace Client.Interfaces.Service
{
    /// <summary>
    /// Список машин для проезда через шлагбаум на литой таре
    /// </summary>
    /// <author>Грешных Н.И.</author>
    /// <version>1</version>
    /// <released>2024-12-11</released>
    public class MoldedContainerGateInterface
    {
        public MoldedContainerGateInterface()
        {
            var gateManagementTab = Central.WM.CheckAddTab<GateManagement>("gateManagementTab", "Управление воротами ЛТ", true);
            Central.WM.SetActive("gateManagementTab");
        }
    }
}
