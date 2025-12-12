using Client.Common;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Интерфейс для работы с оснасткой (приход/расход)
    /// </summary>
    /// <author>volkov_as</author>
    public class RigAccountingInterface
    {
        public RigAccountingInterface()
        {
            Central.WM.AddTab("rig_movement", "Учёт оснастки");

            Central.WM.AddTab<IncomeRigTab>("rig_movement");
            Central.WM.AddTab<ConsumptionRigTab>("rig_movement");
            Central.WM.SetActive("IncomeRigTab");
        }
    }
}
