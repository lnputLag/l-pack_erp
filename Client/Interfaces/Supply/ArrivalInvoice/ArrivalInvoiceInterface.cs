using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Supply
{
    public class ArrivalInvoiceInterface
    {
        public ArrivalInvoiceInterface()
        {
            var arrivalInvoiceListView = Central.WM.CheckAddTab<ArrivalInvoiceList>("ArrivalInvoiceList", "Приходные накладные", true);
            Central.WM.SetActive("ArrivalInvoiceList");
        }
    }
}
