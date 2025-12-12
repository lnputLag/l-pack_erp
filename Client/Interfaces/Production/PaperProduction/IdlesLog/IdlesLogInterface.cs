using Client.Common;
using Client.Interfaces.Production.MoldedContainer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Production
{
    public class IdlesLogInterface
    {
        public IdlesLogInterface()
        {
            var idlesLog = Central.WM.CheckAddTab<PaperMachineReportIdles>("Idles_Log", "Простои БДМ", true);
            //ставим фокус на вкладку по умолчанию
            Central.WM.SetActive("Idles_Log");
        }
    }
}
