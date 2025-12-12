using Client.Common;
using Client.Interfaces.Preproduction;
using Client.Interfaces.Accounts;
using System.Collections.Generic;

namespace Client.Interfaces.Service.Sessions
{
    /// <summary>
    /// сессии
    /// (сессии сервера)
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2024-11-11</released>
    /// <changed>2024-11-11</changed>
    public class SessionsInterface
    {
        public SessionsInterface()
        {
            {
                Central.WM.AddTab("sessions_control", "Сессии");                
                Central.WM.AddTab<DbConnectionTab>("sessions_control");
                Central.WM.AddTab<UserSessionTab>("sessions_control");
                Central.WM.SetActive("DbConnectionTab");
            }
        }
    }
}
