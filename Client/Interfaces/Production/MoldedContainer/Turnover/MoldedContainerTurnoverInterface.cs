using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Production.MoldedContainer
{
    /// <summary>
    /// интерфейс оборотной ведомости по литой таре
    /// </summary>
    /// <author>sviridov_ae</author>
    public class MoldedContainerTurnoverInterface
    {
        public MoldedContainerTurnoverInterface()
        {
            var moldedContainerTurnoverReport = Central.WM.CheckAddTab<MoldedContainerTurnoverReport>("MoldedContainerTurnoverReport", "Оборотная ведомость");
            Central.WM.SetActive("MoldedContainerTurnoverReport");
        }
    }
}
