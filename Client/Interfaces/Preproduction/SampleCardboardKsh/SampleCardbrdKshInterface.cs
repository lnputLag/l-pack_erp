using Client.Common;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Интерфейс Картон для образцов Каширы
    /// </summary>
    public class SampleCardbrdKshInterface
    {
        /// <summary>
        /// Интерфейс Картон для образцов Каширы
        /// </summary>
        public SampleCardbrdKshInterface()
        {
            Central.WM.AddTab("SampleCardboardKshMain", "Картон для образцов КШ");

            Central.WM.AddTab<SampleCardboardKshTab>("SampleCardboardKshMain", false);
            Central.WM.AddTab<SampleCardboardKshTaskTab>("SampleCardboardKshMain", false);

            Central.WM.SetActive("SampleCardboardKshTab");
        }
    }
}
