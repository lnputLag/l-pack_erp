using Client.Common;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Интерфейс Планировщик образцов
    /// </summary>
    /// <author>ryasnoy_pv</author>
    class SampleTaskPlannerInterface
    {
        public SampleTaskPlannerInterface()
        {
            Central.WM.AddTab<SampleTaskPlanner>("SampleTaskPlannerMain", true);
            
            Central.WM.SetActive("SampleTaskPlanner");
        }
    }
}
