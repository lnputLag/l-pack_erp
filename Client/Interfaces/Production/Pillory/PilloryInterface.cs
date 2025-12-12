using Client.Common;
using System.Collections.Generic;

namespace Client.Interfaces.Production
{
    public class PilloryInterface
    {
        public PilloryInterface()
        {
            var view = Central.WM.CheckAddTab<PilloryMonitor>("PilloryMonitor", "Монитор мастера", true, "main");
            view.ProcessNavigation();
            Central.WM.SetActive("PilloryMonitor");

            //{
            //    var p = new Dictionary<string, string>();
            //    p.CheckAdd("width", "900");
            //    p.CheckAdd("height", "720");
            //    p.CheckAdd("no_modal", "1");
            //    Central.WM.FrameMode = 2;
            //    Central.WM.Show("PilloryMonitor1", "Монитор мастера", true, "main", new PilloryMonitor() { DefaultPageId = 0 }, "top", p);
            //}

            //{
            //    var p = new Dictionary<string, string>();
            //    p.CheckAdd("width", "900");
            //    p.CheckAdd("height", "720");
            //    p.CheckAdd("no_modal", "1");
            //    Central.WM.FrameMode = 2;
            //    Central.WM.Show("PilloryMonitor2", "Монитор мастера", true, "main", new PilloryMonitor() { DefaultPageId = 1 }, "top", p);
            //}
        }
    }
}
