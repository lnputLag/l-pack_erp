using Client.Common;

namespace Client.Interfaces.Production
{
    public class PilloryKshInterface
    {
        public PilloryKshInterface()
        {
            var view = Central.WM.CheckAddTab<PilloryMonitorKsh>("PilloryMonitorKsh", "Монитор мастера КШ", true, "main");
            view.ProcessNavigation();
            Central.WM.SetActive("PilloryMonitorKsh");
        }
    }
}
