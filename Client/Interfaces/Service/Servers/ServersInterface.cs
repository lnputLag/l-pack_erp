using Client.Common;
using Client.Interfaces.Preproduction;
using Client.Interfaces.Accounts;
using System.Collections.Generic;

namespace Client.Interfaces.Service.Servers
{
    /// <summary>
    /// сессии
    /// (сессии сервера)
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2024-11-11</released>
    /// <changed>2024-11-11</changed>
    public class ServersInterface
    {
        public ServersInterface()
        {
            {
                Central.WM.AddTab("servers_control", "Серверы");                
                Central.WM.AddTab<ServersStatus2Tab>("servers_control");
                Central.WM.SetActive("ServersStatus2Tab");
            }
        }
    }
}
