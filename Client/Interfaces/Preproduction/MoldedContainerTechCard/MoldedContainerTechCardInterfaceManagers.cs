using Client.Common;
using Client.Interfaces.Preproduction.Rig;
using Client.Interfaces.Production;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Интерфейс для технологических карт литой тары (Менеджеры)
    /// </summary>
    /// <author>ryanoy_pv</author>
    public class MoldedContainerTechCardInterfaceManagers
    {
        /// <summary>
        /// Интерфейс для технологических карт литой тары (Менеджеры)
        /// </summary>
        public MoldedContainerTechCardInterfaceManagers()
        {
            Central.WM.AddTab<TechnologicalMapTabManagers>("molded_container", true);
            Central.WM.SetActive("TechnologicalMapTabManagers");

        }
    }
}
