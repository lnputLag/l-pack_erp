using Client.Common;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Интерфейс управления штанцформамми
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public class CuttingStampAccountingInterface
    {
        /// <summary>
        /// Интерфейс управления штанцформами
        /// </summary>
        public CuttingStampAccountingInterface()
        {
            Central.WM.AddTab("CuttingStampAccountingMain", "Техкарты со штанцформами");

            Central.WM.AddTab<CuttingStampAccountingTab>("CuttingStampAccountingMain", false);
            Central.WM.AddTab<CuttingStampViewTab>("CuttingStampAccountingMain", false);

            Central.WM.SetActive("CuttingStampAccountingTab");
        }
    }
}
