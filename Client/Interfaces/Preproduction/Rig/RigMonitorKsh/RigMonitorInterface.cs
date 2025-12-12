using Client.Common;
using Client.Interfaces.Preproduction.Rig.RigMonitorKsh.Tabs;

namespace Client.Interfaces.Preproduction.Rig.RigMonitorKsh
{
    public class RigMonitorInterface
    {
        public RigMonitorInterface()
        {
            Central.WM.CheckAddTab<RigMonitor>("RigMonitor", "Монитор оснастки");
            Central.WM.SetActive("RigMonitor");
        }
    }
}
