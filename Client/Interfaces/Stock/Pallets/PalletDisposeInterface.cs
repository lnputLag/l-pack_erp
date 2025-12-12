using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Stock
{
    public class PalletDisposeInterface
    {
        public PalletDisposeInterface() 
        {
            Central.WM.AddTab<PalletDisposeList>("Pallet_Dispose", true);
            Central.WM.SetActive("PalletDisposeList");
        }
    }
}
