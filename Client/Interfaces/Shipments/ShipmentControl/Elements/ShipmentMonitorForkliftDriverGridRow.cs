using System.Collections.Generic;

namespace Client.Interfaces.Shipments
{

    /// <summary>
    /// Управление отгрузками, вкладка "Монитор водителей погрузчика"
    /// Вспомогательный класс "Строка монитора". Одна строка -- один водитель погрузчика.
    /// </summary>
    /// <author>sviridov_ae</author>        
    public class ShipmentMonitorForkliftDriverGridRow
    {
        public ShipmentMonitorForkliftDriverGridRow()
        {
            Cells = new Dictionary<string, ShipmentMonitorGridCell>();
            ForkliftDriverId = 0;
            ForkliftDriverName = "";
            ForkliftDriverType = 0;
            Index = 0;
            Show = false;
        }

        /// <summary>
        /// 
        /// </summary>
        public int ForkliftDriverId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string ForkliftDriverName { get; set; }

        /// <summary>
        /// 1 -- Поддоны
        /// 2 -- Рулоны
        /// 3 -- Поддоны и рулоны
        /// </summary>
        public int ForkliftDriverType { get; set; }

        /// <summary>
        /// "HH:MM"
        /// </summary>
        public Dictionary<string, ShipmentMonitorGridCell> Cells { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool Show { get; set; }
    }
}
