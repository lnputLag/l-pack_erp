using Client.Common;

namespace Client.Interfaces.Production.Corrugator.CorrugatorMachineOperatorKsh
{
    /// <summary>
    /// Интерфейс "Оператор гофроагрегата"
    /// </summary>
    /// <author>zelenskiy_sv</author>
    public class CorrugatorMachineOperatorInterfaceKsh
    {
        public CorrugatorMachineOperatorInterfaceKsh()
        {
            if (!Central.WM.TabItems.ContainsKey("tape_counter_ksh"))
            {
                Central.WM.AddTab("tape_counter_ksh", "Оператор ГА КШ");
                Central.WM.CheckAddTab<CorrugatorMachineOperator>("tape_counter_interface_ksh", "ГА КШ", false, "tape_counter_ksh");
                Central.WM.CheckAddTab<CorrugatorMachinePlan>("CorrugatorMachinePlanKsh", "Планирование ГА КШ", false, "tape_counter_ksh");
                Central.WM.SetActive("tape_counter_interface_ksh");
            }
        }
    }
}


