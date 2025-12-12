using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Интерфейс статистики отгрузок
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public class ShipmentStatisticsInterface
    {
        public ShipmentStatisticsInterface()
        {
            var shipmentStatistics = Central.WM.CheckAddTab<ShipmentStatistics>("ShipmentStatistics", "Статистика отгрузок", true);
            Central.WM.SetActive("ShipmentStatistics");
        }
    }
}
