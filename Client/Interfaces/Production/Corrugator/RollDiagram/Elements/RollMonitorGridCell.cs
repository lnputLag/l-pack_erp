using System.Collections.Generic;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Ячейка строки монитора данных
    /// (диаграмма рулонов на ГА)
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>2</version>
    /// <released>2021-11-23</released>
    public class RollMonitorGridCell
    {
        /// <summary>
        /// ID ячейки
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// ID столбца в системе координат грида
        /// </summary>
        public int Index { get; set; }
        /// <summary>
        /// время начала, "HH:MM"
        /// </summary>
        public string TimeBegin { get; set; }
        /// <summary>
        /// время завершения, "HH:MM"
        /// </summary>
        public string TimeEnd { get; set; }
        /// <summary>
        /// длина сегмента, мин.
        /// </summary>
        public int Len { get; set; }

        public int End { get; set; }
        public int X { get; set; }
        public int Width { get; set; }

        public Dictionary<string,string> Row { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public RollMonitorGridCell()
        {
            Id="";
            Index=0;
            TimeBegin="";
            TimeEnd="";
            Len=0;
            End=0;
            X=0;
            Width=0;
            Row=new Dictionary<string,string>();
        }

    }
}
