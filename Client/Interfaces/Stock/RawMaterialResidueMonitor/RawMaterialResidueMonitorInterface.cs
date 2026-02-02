using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Stock.RawMaterialResidueMonitor
{
    /// <summary>
    /// интерфейс монитора остатков сырья
    /// </summary>
    /// <author>kurasov_dp</author>
    public class RawMaterialResidueMonitorInterface
    {
        public RawMaterialResidueMonitorInterface() 
        {
            Central.WM.AddTab("Monitor", "Монитор");


            Central.WM.AddTab("RawMaterialResidueMonitor", "Сырьевые группы", false, "Monitor");
            var rawGroupMonitorTable = Central.WM.CheckAddTab<RawMaterialResidueMonitorTableTab>("RawGroupControl_MonitorCards","Монитор в табличном виде для групп", false, "RawMaterialResidueMonitor", "bottom");
            var rawGroupMonitorCards = Central.WM.CheckAddTab<RawMaterialResidueMonitorCardsTab>("RawGroupControl_MonitorTable", "Монитор в карточном виде для групп", false, "RawMaterialResidueMonitor", "bottom");

            Central.WM.AddTab("RawMaterialResidueMonitor", "Сырьевые композиции", false, "Monitor");
            //var rawCompositionsMonitorTable = Central.WM.CheckAddTab<RawMaterialResidueMonitorTableTab>("RawGroupControl_MonitorCards", "Монитор в табличном виде для групп", false, "RawMaterialResidueMonitor", "bottom");
            //var rawCompositionsMonitorCards = Central.WM.CheckAddTab<RawMaterialResidueMonitorCardsTab>("RawGroupControl_MonitorTable", "Монитор в карточном виде для групп", false, "RawMaterialResidueMonitor", "bottom");
            Central.WM.SetActive("RawGroupControl_MonitorTable");
        }
    }
}
