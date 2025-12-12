using Client.Common;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Интерфейс заказов штанцфрм
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public class CuttingStampOrderInterface
    {
        /// <summary>
        /// Интерфейс заказов штанцфрм
        /// </summary>
        public CuttingStampOrderInterface()
        {
            Central.WM.AddTab<CuttingStampOrderTab>("CuttingStampOrderMain", true);
            Central.WM.SetActive("CuttingStampOrderTab");
        }
    }
}
