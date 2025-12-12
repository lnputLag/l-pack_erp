using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Production
{
    public class OperatorsLogInterface2
    {
        public OperatorsLogInterface2()
        {
            Central.WM.AddTab("OperatorsLog2", "Журнал оператора БДМ2");
            var operatorsLog = Central.WM.CheckAddTab<OperatorsLogPaperMachine>("Operators_Log2", "Журнал оператора", false, "OperatorsLog2", "top");
            operatorsLog.MachineId = 1716;

            var operatorslogdecision = Central.WM.CheckAddTab<OperatorsLogDecision>("Operators_Decision2", "Рекомендация технолога", false, "OperatorsLog2", "top");
            operatorslogdecision.MachineId = 1716;

            //ставим фокус на вкладку по умолчанию
            Central.WM.SetActive("Operators_Log2");

        }
    }
}
