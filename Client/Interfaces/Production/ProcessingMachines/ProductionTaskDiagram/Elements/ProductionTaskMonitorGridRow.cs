using System.Collections.Generic;

namespace Client.Interfaces.Production.ProcessingMachines
{
    /// <summary>
    /// Строка строки монитора данных
    /// (Диаграмма ПЗ на переработке)
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>2</version>
    /// <released>2021-11-23</released>
    public class ProductionTaskMonitorGridRow
    {
        /// <summary>
        /// ID строки
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// ID строки в системе координат грида
        /// </summary>
        public int Index { get; set; }
        /// <summary>
        /// заголовок строки
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// "HH:MM"
        /// </summary>
        public Dictionary<string, ProductionTaskMonitorGridCell> Works { get; set; }
        public Dictionary<string, ProductionTaskMonitorGridCell> Idles { get; set; }
        public Dictionary<string, ProductionTaskMonitorGridCell> Counters { get; set; }
        public Dictionary<string, string>Row { get; set; }

        /// <summary>
        /// Флаг отображения
        /// </summary>
        public bool Show { get; set; }

        public ProductionTaskMonitorGridRow()
        {
            Works = new Dictionary<string, ProductionTaskMonitorGridCell>();
            Idles = new Dictionary<string, ProductionTaskMonitorGridCell>();
            Counters = new Dictionary<string, ProductionTaskMonitorGridCell>();
            Row = new Dictionary<string, string>();
            Title = "";
            Index = 0;
            Id = 0;
            Show = false;
        }
    }
}
