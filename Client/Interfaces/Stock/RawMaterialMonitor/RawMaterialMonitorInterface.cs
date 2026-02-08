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
            //Central.WM.AddTab("MonitorControl_RawGroup", "Сырьевые группы", false, "MonitorControl");
            //var monitorRawGroupTable = Central.WM.CheckAddTab<RawGroupMaterialMonitorTableTab>("MonitorTableControl_RawGroup", "Таблица", false, "MonitorControl_RawGroup", "bottom");
            // var monitorRawGroupCards = Central.WM.CheckAddTab<RawGroupMaterialMonitorCardsTab>("MonitorCardsControl_RawGroup", "Карточки", false, "MonitorControl_RawGroup", "bottom");

            //Central.WM.AddTab("MonitorControl_Composition", "Сырьевые композиции", false, "MonitorControl");
            // var monitorCompositionTable = Central.WM.CheckAddTab<RawCompositionMaterialMonitorTableTab>("MonitorTableControl_Composition", "Таблица", false, "MonitorControl_Composition", "bottom");
            var monitorRawGroupCards = Central.WM.CheckAddTab<RawGroupMaterialMonitorCardsTab>("MonitorCardsControl_RawGroup", "Карточки", false, "MonitorControl", "bottom");
            var monitorCompositionCards = Central.WM.CheckAddTab<RawCompositionMaterialMonitorCardsTab>("MonitorCardsControl_Composition", "Карточки", false, "MonitorControl", "bottom");

            Central.WM.SetActive("MonitorCardsControl_Composition");
        }
    }
}
