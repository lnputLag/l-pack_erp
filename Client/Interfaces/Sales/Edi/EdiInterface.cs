using Client.Common;
using Client.Interfaces.Preproduction;
using Client.Interfaces.Accounts;
using System.Collections.Generic;

namespace Client.Interfaces.Sales.Edi
{
    /// <summary>
    /// EDI
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2024-09-19</released>
    /// <changed>2024-09-19</changed>
    public class EdiInterface
    {
        public EdiInterface()
        {
            {
                Central.WM.AddTab("edi", "EDI");
                //Central.WM.AddTab<OrdersTab>("edi");
                Central.WM.AddTab<ExchangeTab>("edi");
                Central.WM.AddTab<DocumentsTab>("edi");
                Central.WM.SetActive("ExchangeTab");
            }
        }
    }
}
