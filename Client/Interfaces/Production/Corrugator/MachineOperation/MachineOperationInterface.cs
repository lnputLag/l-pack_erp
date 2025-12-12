using Client.Common;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// интерфейс "диаграмма рулонов на ГА"
    /// </summary>
    /// <author>balchugov_dv</author>
    public class MachineOperationInterface
    {
        public MachineOperationInterface()
        {
            Central.WM.AddTab("MachineOperation", "Работа ГА");

            var productionTaskListComplete = Central.WM.CheckAddTab<ProductionTaskListComplete>("MachineOperation_productionTaskListComplete", "Выполненные задания", false, "MachineOperation", "bottom");
            var machineOperation = Central.WM.CheckAddTab<MachineOperation>("MachineOperation_machineOperation", "Работа ГА", false, "MachineOperation", "bottom");
            var machineSpeed = Central.WM.CheckAddTab<MachineSpeed>("MachineOperation_machineSpeed", "График скорости ГА", false, "MachineOperation", "bottom");

            //ставим фокус на вкладку по умолчанию
            Central.WM.SetActive("MachineOperation_productionTaskListComplete");
        }
    }
}


