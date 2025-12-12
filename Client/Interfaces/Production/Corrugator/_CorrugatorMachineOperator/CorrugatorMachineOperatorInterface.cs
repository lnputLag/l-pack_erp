using Client.Common;

namespace Client.Interfaces.Production.Corrugator.CorrugatorMachineOperator
{
    /// <summary>
    /// Интерфейс "Оператор гофроагрегата"
    /// </summary>
    /// <author>zelenskiy_sv</author>
    public class CorrugatorMachineOperatorInterface
    {
        public CorrugatorMachineOperatorInterface()
        {
            if (!Central.WM.TabItems.ContainsKey("tape_counter"))
            {
                Central.WM.AddTab("tape_counter", "Оператор ГА");
                Central.WM.CheckAddTab<CorrugatorMachineOperator>("tape_counter_interface", "ГА", false, "tape_counter");
                Central.WM.CheckAddTab<CorrugatorMachinePlan>("CorrugatorMachinePlan", "Планирование ГА", false, "tape_counter");
                Central.WM.SetActive("tape_counter_interface");
            }
        }
    }
}


