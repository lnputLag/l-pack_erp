using Client.Common;
using System.Collections.Generic;

namespace Client.Interfaces.Service
{
    /// <summary>
    /// интерфейс "скриншоты"
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-02-27</released>
    /// <changed>2023-02-27</changed>
    public class ScreenShotsInterface
    {
        public ScreenShotsInterface()
        {
            Central.WM.CheckAddTab<MachineScreenShotList>("ScreenShots", "Скриншоты", true, "main");
            Central.WM.SetActive("ScreenShots");
        }
    }
}
