using Client.Common;
using Client.Interfaces.Preproduction;
using Client.Interfaces.Accounts;
using System.Collections.Generic;

namespace Client.Interfaces.Service.Jobs
{
    /// <summary>
    /// сессии
    /// (сессии сервера)
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2024-11-11</released>
    /// <changed>2024-11-11</changed>
    public class JobsInterface
    {
        public JobsInterface()
        {
            {
                Central.WM.AddTab("jobs_control", "Джобы");  
                Central.WM.AddTab<JobList>("jobs_control");
                Central.WM.AddTab<JobRunLog>("jobs_control");
                Central.WM.SetActive("JobList");
            }
        }
    }
}
