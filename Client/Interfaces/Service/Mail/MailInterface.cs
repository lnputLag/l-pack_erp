using Client.Common;
using System.Collections.Generic;

namespace Client.Interfaces.Service.Mail
{
    /// <summary>
    /// почта (корреспонденция)
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-06-22</released>
    /// <changed>2023-06-22</changed>
    public class MailInterface
    {
        public MailInterface()
        {
            //Central.WM.AddTab("mail", "Почта");
            var labelList = Central.WM.CheckAddTab<LabelList>("mail_label", "Почта", true );             
            Central.WM.SetActive("mail_label");                        
        }
    }
}
