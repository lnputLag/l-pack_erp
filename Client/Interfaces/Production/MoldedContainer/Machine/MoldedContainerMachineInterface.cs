using Client.Common;

namespace Client.Interfaces.Production.MoldedContainer
{ 
    /// <summary>
    /// интерфейс оператора агрегата литой тары
    /// (вакуумно-формовочная машина, ВФМ)
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2024-07-16</released>
    /// <changed>2024-07-16</changed>
    public class MoldedContainerMachineInterface
    {
        public MoldedContainerMachineInterface()
        {
            Central.WM.AddTab<MachineControl>("molded_container_machine", true);
            //var machinecontrolTab2 = Central.WM.CheckAddTab<MachineControl>("MachineControl", "Оператор ВФМ", true);
            Central.WM.SetActive("MachineControl");
        }
    }
}
