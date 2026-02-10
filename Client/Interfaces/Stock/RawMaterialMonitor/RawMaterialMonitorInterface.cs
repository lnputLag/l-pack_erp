using Client.Common;
using Client.Interfaces.Shipments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Stock.RawMaterialMonitor
{
    /// <summary>
    /// Интерфейс монитора остатков сырья по сырьевой
    /// группе и композиции
    /// </summary>
    /// <author>kurasov_dp</author>
    public class RawMaterialMonitorInterface
    {
        public RawMaterialMonitorInterface()
        {
            Central.WM.AddTab("MonitorControl", "Монитор");
            var monitorRawGroup = Central.WM.CheckAddTab<RawGroupMaterialMonitorTab>("MonitorCardsControl_RawGroup", "Сырьевая группа", false, "MonitorControl", "bottom");
            var monitorRawGroupCards = Central.WM.CheckAddTab<RawGroupMaterialMonitorCardsTab>("MonitorCardsControl_RawGroup", "Сырьевая группа", false, "MonitorControl", "bottom");
            var monitorCompositionCards = Central.WM.CheckAddTab<RawCompositionMaterialMonitorCardsTab>("MonitorCardsControl_Composition", "Сырьевая композиция", false, "MonitorControl", "bottom");

            Central.WM.SetActive("MonitorCardsControl_RawGroup");
        }
    }
}
