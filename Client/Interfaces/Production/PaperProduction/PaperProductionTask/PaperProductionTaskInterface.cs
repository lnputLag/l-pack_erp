using Client.Common;
using Client.Interfaces.Shipments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Production
{
    public class PaperProductionTaskInterface
    {
        public PaperProductionTaskInterface()
        {
            var paperProductionTaskList = Central.WM.CheckAddTab<PaperProductionTaskList>("PaperProductionTaskList", "ПЗ БДМ", true);
            Central.WM.SetActive("PaperProductionTaskList");
        }
    }
}
