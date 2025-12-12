using Client.Common;
using Client.Interfaces.Preproduction;
using Client.Interfaces.Accounts;
using System.Collections.Generic;

namespace Client.Interfaces.Service.Sources
{
    /// <summary>
    /// ресурсы приложения
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2025-02-12</released>
    /// <changed>2025-02-12</changed>
    public class SourcesInterface
    {
        public SourcesInterface()
        {
            {
                Central.WM.AddTab("sources_control", "Ресурсы");
                Central.WM.AddTab<NavigatorTab>("sources_control");
                Central.WM.AddTab<InterfacesTab>("sources_control");
                Central.WM.SetActive("InterfacesTab");
            }
        }
    }
}
