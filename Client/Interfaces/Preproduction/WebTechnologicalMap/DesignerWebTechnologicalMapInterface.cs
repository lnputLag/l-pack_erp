using Client.Common;
using Client.Interfaces.Production;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Интерфейс для веб-техкарт (Дизайнеры)
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public class DesignerWebTechnologicalMapInterface
    {
        /// <summary>
        /// Интерфейс для веб-техкарт (Дизайнеры)
        /// </summary>
        public DesignerWebTechnologicalMapInterface()
        {
            Central.WM.AddTab<DesignerWebTechnologicalMapTab>("web_technological_map", true);
            Central.WM.SetActive("DesignerWebTechnologicalMapTab");
        }
    }
}
