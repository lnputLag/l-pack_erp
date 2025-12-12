using Client.Common;
using Client.Interfaces.Production;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Интерфейс для веб-техкарт (Инженеры)
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public class EngineerWebTechnologicalMapTestInterface
    {
        /// <summary>
        /// Интерфейс для веб-техкарт (Инженеры)
        /// </summary>
        public EngineerWebTechnologicalMapTestInterface()
        {
            Central.WM.AddTab<EngineerWebTechnologicalMapTestTab>("web_technological_map_test", true);
            Central.WM.SetActive("EngineerWebTechnologicalMapTestTab");
        }
    }
}
