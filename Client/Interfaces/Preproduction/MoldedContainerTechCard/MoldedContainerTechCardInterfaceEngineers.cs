using Client.Common;
using Client.Interfaces.Production;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Интерфейс для технологических карт литой тары (Инженеры)
    /// </summary>
    /// <author>ryanoy_pv</author>
    public class MoldedContainerTechCardInterfaceEngineers
    {
        /// <summary>
        /// Интерфейс для технологических карт литой тары (Инженеры)
        /// </summary>
        public MoldedContainerTechCardInterfaceEngineers()
        {
            Central.WM.AddTab<TechnologicalMapTabEngineers>("molded_container", true);
            Central.WM.SetActive("TechnologicalMapTabEngineers");
        }
    }
}
