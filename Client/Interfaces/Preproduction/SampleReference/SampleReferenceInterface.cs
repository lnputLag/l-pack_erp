using Client.Common;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Интерфейс "Эталонные образцы"
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public class SampleReferenceInterface
    {
        public SampleReferenceInterface()
        {
            Central.WM.AddTab<SampleReferenceList>("SampleReferenceMain", true);

            Central.WM.SetActive("SampleReferenceList");
        }
    }
}
