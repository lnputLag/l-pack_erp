namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Управление отгрузками, вкладка "Монитор"
    /// Вспомогательный класс: Вспомогательная структура, генератор временных меток
    /// </summary>
    /// <author>balchugov_dv</author>       
    public class ShipmentTimelineTime
    {
        public int Hours { get; set; }
        public int Minutes { get; set; }
        public int Seconds { get; set; }
        public int Days { get; set; }

        public string ToString()
        {
            return $"{Hours:00}:{Minutes:00}:{Seconds:00}";
        }

        public string OutHoursMinutes()
        {
            return $"{Hours:00}:{Minutes:00}";
        }

        public string OutHoursMinutesSeconds()
        {
            return $"{Hours:00}:{Minutes:00}:{Seconds:00}";
        }

        public void IncMinutes(int minutes)
        {
            Minutes += minutes;
            if (Minutes >= 60)
            {
                Hours += Minutes / 60;
                Minutes %= 60;
            }

            if (Hours > 23)
            {
                Hours = 0;
                Days++;
            }
        }

        public void IncSeconds(int seconds)
        {
            Seconds += seconds;
            if (Seconds >= 60)
            {
                Minutes += Seconds / 60;
                Seconds %= 60;
            }

            if (Minutes >= 60)
            {
                Minutes = 0;
                Hours++;
            }

            if (Hours > 23)
            {
                Hours = 0;
                Days++;
            }
        }
    }



}
