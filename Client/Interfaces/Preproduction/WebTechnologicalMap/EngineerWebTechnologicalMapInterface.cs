using Client.Common;
using Client.Interfaces.Production;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Интерфейс для веб-техкарт (Инженеры)
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public class EngineerWebTechnologicalMapInterface
    {
        /// <summary>
        /// Интерфейс для веб-техкарт (Инженеры)
        /// </summary>
        public EngineerWebTechnologicalMapInterface()
        {
            Central.WM.AddTab<EngineerWebTechnologicalMapTab>("web_technological_map", true);
            Central.WM.SetActive("EngineerWebTechnologicalMapTab");
        }
    }
}
