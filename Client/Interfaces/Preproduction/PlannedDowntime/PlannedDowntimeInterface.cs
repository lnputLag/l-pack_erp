using Client.Common;
using Client.Interfaces.Preproduction.PlannedDowntime.Tabs;

namespace Client.Interfaces.Preproduction.PlannedDowntime
{
    public class PlannedDowntimeInterface
    {
        /// <summary>
        /// Интерфейс плановые простои
        /// </summary>
        public PlannedDowntimeInterface()
        {
            Central.WM.AddTab("planned_downtime", "Плановые простои");
            
            Central.WM.AddTab<PlannedDowntimeTab>("planned_downtime");
            Central.WM.AddTab<PatternsDowntimeTab>("planned_downtime");
            
            Central.WM.SetActive("PlannedDowntimeTab");
        }
    }
}