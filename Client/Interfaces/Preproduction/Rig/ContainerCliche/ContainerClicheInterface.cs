using Client.Common;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Интерфейс управления клише литой тары
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public class ContainerClicheInterface
    {
        /// <summary>
        /// Интерфейс управления клише литой тары
        /// </summary>
        public ContainerClicheInterface()
        {
            Central.WM.AddTab<ContainerClicheTab>("ClicheContainerMain", true);
            Central.WM.SetActive("ContainerClicheTab");
        }
    }
}
