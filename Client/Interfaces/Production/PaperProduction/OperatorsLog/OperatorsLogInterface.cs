using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Production
{
    public class OperatorsLogInterface
    {
        public OperatorsLogInterface()
        {
            Central.WM.AddTab("OperatorsLog", "Журнал оператора БДМ1");
            var operatorsLog = Central.WM.CheckAddTab<OperatorsLogPaperMachine>("Operators_Log1", "Журнал оператора", false, "OperatorsLog", "top");
            operatorsLog.MachineId = 716;

            var operatorslogdecision = Central.WM.CheckAddTab<OperatorsLogDecision>("Operators_Decision1", "Рекомендация технолога", false, "OperatorsLog", "top");
            operatorslogdecision.MachineId = 716;

            
            //ставим фокус на вкладку по умолчанию
            Central.WM.SetActive("Operators_Log1");
        }
    }
}
