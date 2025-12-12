using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Production.MoldedContainer
{
    /// <summary>
    /// интерфейс для мониторинга литой тары
    /// </summary>
    /// <author>greshnyh_ni</author>
    public class MoldedContainerMonitoringInterface
    {
        public MoldedContainerMonitoringInterface()
        {
            var monitoringTab = Central.WM.CheckAddTab<MonitoringTab>("MonitoringTab", "Мониторинг ЛТ", true);
            Central.WM.SetActive("MonitoringTab");
        }
    }
}
