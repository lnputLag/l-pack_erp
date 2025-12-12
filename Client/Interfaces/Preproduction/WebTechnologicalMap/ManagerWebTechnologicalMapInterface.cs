using Client.Common;
using Client.Interfaces.Production;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Интерфейс для веб-техкарт (Менеджеры)
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public class ManagerWebTechnologicalMapInterface
    {
        /// <summary>
        /// Интерфейс для веб-техкарт (Менеджеры)
        /// </summary>
        public ManagerWebTechnologicalMapInterface()
        {
            Central.WM.AddTab<ManagerWebTechnologicalMapTab>("web_technological_map", true);
            Central.WM.SetActive("ManagerWebTechnologicalMapTab");
        }
    }
}
