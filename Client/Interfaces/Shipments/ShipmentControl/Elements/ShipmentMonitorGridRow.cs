using System.Collections.Generic;

namespace Client.Interfaces.Shipments
{

    /// <summary>
    /// Управление отгрузками, вкладка "Список ожидаемых водителей"
    /// Вспомогательный класс "Строка монитора". Одна строка -- один терминал.
    /// </summary>
    /// <author>balchugov_dv</author>        
    public class ShipmentMonitorGridRow
    {
        /// <summary>
        /// 
        /// </summary>
        public int TerminalNumber { get; set; }
        /// <summary>
        /// 1=pallet 2=roll
        /// </summary>
        public int TerminalType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string TerminalTitle { get; set; }
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

        public ShipmentMonitorGridRow()
        {
            Cells = new Dictionary<string, ShipmentMonitorGridCell>();
            TerminalNumber = 0;
            TerminalType = 1;
            TerminalTitle = "";
            Index = 0;
            Show = false;
        }

    }

}
