using Client.Common;
using Client.Interfaces.Production.Strapper;

namespace Client.Interfaces.Production
{
    public class StrapperMonitorInterface
    {
        public StrapperMonitorInterface()
        {
            var view = Central.WM.CheckAddTab<StrapperMonitor>("StrapperMonitor", "Монитор упаковщиков", true, "main");
            Central.WM.SetActive("StrapperMonitor");
        }
    }
}
