namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Управление отгрузками, вкладка "Монитор"
    /// Вспомогательный класс Ячейка строки термнала. Одна ячейка -- одна отгрузка.
    /// </summary>
    /// <author>balchugov_dv</author>   
    public class ShipmentMonitorGridCell
    {
        /// <summary>
        /// ID отгрузки
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// время начала, "HH:MM"
        /// </summary>
        public string StartTime { get; set; }
        /// <summary>
        /// время завершения, "HH:MM"
        /// </summary>
        public string FinishTime { get; set; }
        /// <summary>
        /// длина сегмента, мин.
        /// </summary>
        public int Len { get; set; }

        public string BayerName { get; set; }
        public string TerminalTitle { get; set; }
        public string TerminalNumber { get; set; }
        public int TerminalId { get; set; }
        /// <summary>
        /// Статус отгрузки.
        /// 1-внешние операции, 2-отгрузка, 3-отгружено
        /// </summary>
        public int Status { get; set; }
        public string DriverName { get; set; }
        public string ForkliftDriverName { get; set; }
        public int ForkliftDriverId { get; set; }
        public string SelfShipment { get; set; }
        public string PackagingType { get; set; }
        public string ProductionType { get; set; }
        public string Loaded { get; set; }
        public string ForLoading { get; set; }
        public string ForkliftDriverBrigade { get; set; }
        public string ForkliftDriverPhone { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ShipmentMonitorGridCell()
        {
            Id = "";
            StartTime = "";
            FinishTime = "";
            Len = 0;
            BayerName = "";
            TerminalTitle = "";
            TerminalNumber="";
            TerminalId = 0;
            ForkliftDriverId = 0;
            Status = 0;
            DriverName = "";
            ForkliftDriverName = "";
            SelfShipment = "";
            PackagingType = "";
            ProductionType = "";
            ForkliftDriverBrigade="";
            ForkliftDriverPhone="";
        }

    }


}
