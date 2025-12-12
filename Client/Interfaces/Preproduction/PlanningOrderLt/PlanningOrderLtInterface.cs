using Client.Common;
using Client.Interfaces.Preproduction.PlanningOrderLt.Tabs;

namespace Client.Interfaces.Preproduction.PlanningOrderLt
{
    /// <summary>
    /// Планирование ЛТ
    /// </summary>
    /// <author>volkov_as</author>
    public class PlanningOrderLtInterface
    {
        public PlanningOrderLtInterface()
        {
            Central.WM.AddTab<PlanningOrderTab>("OrderPlanningLtMain", true);

            Central.WM.SetActive("PlanningOrderTab");
        }
    }
}
