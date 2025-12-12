using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Stock
{
    public class PalletSearcherInterface
    {
        public PalletSearcherInterface()
        {
            var view = Central.WM.CheckAddTab<PalletSearcher>("Pallet_Searcher", "Поиск паллета", true, "main");
            Central.WM.SetActive("Pallet_Searcher");
        }
    }
}
