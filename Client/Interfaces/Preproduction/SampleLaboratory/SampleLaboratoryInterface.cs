using Client.Common;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Интерфейс образцов для лаборатории
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public class SampleLaboratoryInterface
    {
        public SampleLaboratoryInterface()
        {
            Central.WM.AddTab<SampleLaboratoryList>("SampleLaboratoryMain", true);

            Central.WM.SetActive("SampleLaboratoryList");
        }
    }
}
