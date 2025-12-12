using Client.Common;
using Client.Interfaces.Production.Strapper;

namespace Client.Interfaces.Production
{
    public class StrapperMonitorKshInterface
    {
        public StrapperMonitorKshInterface()
        {
            var view = Central.WM.CheckAddTab<StrapperMonitorKsh>("StrapperMonitorKsh", "Монитор упаковщиков КШ", true, "main");
            Central.WM.SetActive("StrapperMonitorKsh");
        }
    }
}
