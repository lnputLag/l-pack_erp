using Client.Common;
using Client.Interfaces.Main;
using System.Collections.Generic;

namespace Client.Interfaces.Service
{
    /// <summary>
    /// статистика по клиентам ERP
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-02-27</released>
    /// <changed>2023-10-30</changed>
    public class ClientInterface: InterfaceBase
    {
        public ClientInterface()
        {
            Central.WM.AddTab("clients", "Клиенты");
            Central.WM.AddTab<AgentList>("clients");
            Central.WM.AddTab<ClientList>("clients");
            Central.WM.AddTab<MobileList>("clients");
            Central.WM.AddTab<SessionList>("clients");

            Central.WM.SetActive("SessionList");
        }
    }
}
