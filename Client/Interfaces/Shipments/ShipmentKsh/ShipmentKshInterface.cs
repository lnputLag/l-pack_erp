using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Интерфейс управление отгрузками Кашира
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public class ShipmentKshInterface
    {
        public ShipmentKshInterface()
        {
            Central.WM.AddTab("ShipmentKshControl", "Управление отгрузками КШ");
            /*
            var viewShipmentKshList = Central.WM.CheckAddTab<ShipmentKshList>("ShipmentKshList", "Список", false, "ShipmentKshControl");
            var viewShipmentKshPlan = Central.WM.CheckAddTab<ShipmentKshPlan>("ShipmentKshPlan", "План", false, "ShipmentKshControl");
            var viewShipmentKshMonitor = Central.WM.CheckAddTab<ShipmentKshMonitor>("ShipmentKshMonitor", "Монитор", false, "ShipmentKshControl");
            var viewShipmentKshReport = Central.WM.CheckAddTab<ShipmentKshReport>("ShipmentKshReport", "Отчёт", false, "ShipmentKshControl");
            var viewShipmentKshStatistic = Central.WM.CheckAddTab<ShipmentKshStatistic>("ShipmentKshStatistic", "Статистика", false, "ShipmentKshControl");
            */
            Central.WM.AddTab<ShipmentKshList>("ShipmentKshControl", false);
            var viewShipmentKshPlan = Central.WM.CheckAddTab<ShipmentKshPlan>("ShipmentKshPlan", "План", false, "ShipmentKshControl");
            var viewShipmentKshMonitor = Central.WM.CheckAddTab<ShipmentKshMonitor>("ShipmentKshMonitor", "Монитор", false, "ShipmentKshControl");
            var viewShipmentKshReport = Central.WM.CheckAddTab<ShipmentKshReport>("ShipmentKshReport", "Отчёт", false, "ShipmentKshControl");
            var viewShipmentKshStatistic = Central.WM.CheckAddTab<ShipmentKshStatistic>("ShipmentKshStatistic", "Статистика", false, "ShipmentKshControl");
            Central.WM.AddTab("RigShipment", "Оснастка", false, "ShipmentKshControl");
            Central.WM.AddTab<ShipmentKshSample>("RigShipment", false);
            Central.WM.AddTab<ShipmentKshCliche>("RigShipment", false);
            Central.WM.AddTab<ShipmentKshShtanz>("RigShipment", false);

            Central.WM.SetActive("ShipmentKshList");
        }
    }
}
