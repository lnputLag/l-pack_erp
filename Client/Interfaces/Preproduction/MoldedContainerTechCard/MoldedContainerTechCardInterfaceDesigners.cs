using Client.Common;
using Client.Interfaces.Production;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Интерфейс для технологических карт литой тары (Дизайнеры)
    /// </summary>
    /// <author>ryanoy_pv</author>
    public class MoldedContainerTechCardInterfaceDesigners
    {
        /// <summary>
        /// Интерфейс для технологических карт литой тары (Дизайнеры)
        /// </summary>
        public MoldedContainerTechCardInterfaceDesigners()
        {
            Central.WM.AddTab<TechnologicalMapTabDesigners>("molded_container", true);
            Central.WM.SetActive("TechnologicalMapTabDesigners");
        }
    }
}
