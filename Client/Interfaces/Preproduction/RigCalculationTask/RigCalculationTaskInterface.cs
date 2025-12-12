using Client.Common;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Интерфейс заданий на расчет стоимости оснастки
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public class RigCalculationTaskInterface
    {
        public RigCalculationTaskInterface()
        {
            Central.WM.AddTab<RigCalculationTaskList>("RigCalculationTaskMain", true);
            Central.WM.SetActive("RigCalculationTaskList");
        }
    }
}
