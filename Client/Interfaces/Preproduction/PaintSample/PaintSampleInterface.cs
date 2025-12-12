using Client.Common;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Интерфейс заявок на изготовление выкрасов
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public class PaintSampleInterface
    {
        /// <summary>
        /// Интерфейс заявок на изготовление выкрасов
        /// </summary>
        public PaintSampleInterface()
        {
            Central.WM.AddTab<PaintSampleTab>("PaintSampleTaskMain", true);
            Central.WM.SetActive("PaintSampleTab");
        }
    }
}
