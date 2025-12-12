using Client.Common;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Интерфейс учета и хранения штанцформ
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public class CuttingStampKeepingInterface
    {
        /// <summary>
        /// Интерфейс учета и хранения штанцформ
        /// </summary>
        public CuttingStampKeepingInterface()
        {
            Central.WM.AddTab("CuttingStampKeepingMain", "Учет и хранение штанцформ");

            Central.WM.AddTab<CuttingStampTab>("CuttingStampKeepingMain", false);
            Central.WM.AddTab<CuttingStampKeepingTab>("CuttingStampKeepingMain", false);
            Central.WM.AddTab<CuttingStampReadyTab>("CuttingStampKeepingMain", false);

            Central.WM.SetActive("CuttingStampTab");
        }
    }
}
