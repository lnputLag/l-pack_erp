using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Production
{
    public class PaperMakingMachineInterface
    {
        public PaperMakingMachineInterface(int machineNumber = 0)
        {
            var stanok = "БДМ1";
            if (machineNumber == 2)
                stanok = "БДМ2";

            var paperMakingMachine = Central.WM.CheckAddTab<PaperMakingMachineOperator>(stanok, "Мониторинг " + stanok, true, "", "top");
            paperMakingMachine.MachineId = machineNumber==2 ? 1716 : 716;
            Central.WM.SetActive(stanok);
        }
    }
}
