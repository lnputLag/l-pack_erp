using System.Collections.Generic;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Строка строки монитора данных
    /// (диаграмма рулонов на ГА)
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>2</version>
    /// <released>2021-11-23</released>
    public class RollMonitorGridRow
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
        public Dictionary<string,RollMonitorGridCell> Rolls { get; set; }
        public Dictionary<string,RollMonitorGridCell> RollsActivities { get; set; }
        public Dictionary<string,string> Row { get; set; }
       
        /// <summary>
        /// Флаг отображения
        /// </summary>
        public bool Show { get; set; }

        public int PositionY { get;set;}
        public int Height { get;set;}

        public RollMonitorGridRow()
        {
            Rolls=new Dictionary<string,RollMonitorGridCell>();
            RollsActivities=new Dictionary<string,RollMonitorGridCell>();          
            Row=new Dictionary<string,string>();

            Title="";
            Index=0;
            Id=0;
            Show=false;
            PositionY=0;
            Height=0;
        }
    }
}
