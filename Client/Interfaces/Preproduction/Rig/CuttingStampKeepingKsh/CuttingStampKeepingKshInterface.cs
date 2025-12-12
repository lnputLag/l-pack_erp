using Client;
using Client.Common;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Интерфейс учета и хранения штанцформ в Кашире
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public class CuttingStampKeepingKshInterface
    {
        /// <summary>
        /// Интерфейс учета и хранения штанцформ в Кашире
        /// </summary>
        public CuttingStampKeepingKshInterface()
        {
            Central.WM.AddTab("CuttingStampKeepingKshMain", "Учет и хранение штанцформ КШ");

            Central.WM.AddTab<CuttingStampKshTab>("CuttingStampKeepingKshMain", false);
            Central.WM.AddTab<CuttingStampKeepingKshTab>("CuttingStampKeepingKshMain", false);
            Central.WM.AddTab<CuttingStampReadyKshTab>("CuttingStampKeepingKshMain", false);

            Central.WM.SetActive("CuttingStampKshTab");
        }
    }
}
