using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Интерфейс статистики отгрузок для площадки Кашира
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public class ShipmentStatisticsKshInterface
    {
        public ShipmentStatisticsKshInterface()
        {
            var shipmentStatistics = Central.WM.CheckAddTab<ShipmentStatisticsKsh>("ShipmentStatisticsKsh", "Статистика отгрузок КШ", true);
            Central.WM.SetActive("ShipmentStatisticsKsh");
        }
    }
}
