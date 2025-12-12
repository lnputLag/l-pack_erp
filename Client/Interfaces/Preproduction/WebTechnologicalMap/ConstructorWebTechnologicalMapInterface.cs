using Client.Common;
using Client.Interfaces.Production;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Интерфейс для веб-техкарт (Конструкторы)
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public class ConstructorWebTechnologicalMapInterface
    {
        /// <summary>
        /// Интерфейс для веб-техкарт (Конструкторы)
        /// </summary>
        public ConstructorWebTechnologicalMapInterface()
        {
            Central.WM.AddTab<ConstructorWebTechnologicalMapTab>("web_technological_map", true);
            Central.WM.SetActive("ConstructorWebTechnologicalMapTab");
        }
    }
}
