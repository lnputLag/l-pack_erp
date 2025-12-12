using Client.Common;
using Client.Interfaces.Accounts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Stock.PalletBinding
{
    /// <summary>
    /// 
    /// </summary>
    /// <author>kurasov_dp</author>
    public class PalletBindingInterface
    {
        public PalletBindingInterface()
        {
            Central.WM.AddTab<PalletBindingTab>();
            Central.WM.SetActive("PalletBindingTab");
        }
    }
}
