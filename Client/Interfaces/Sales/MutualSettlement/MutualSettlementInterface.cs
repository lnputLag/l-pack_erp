using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Sales
{
    public class MutualSettlementInterface
    {
        /// <summary>
        /// Взаиморасчёты
        /// </summary>
        /// <autor>sviridov_ae</autor>
        public MutualSettlementInterface()
        {
            var mutualSettlementListView = Central.WM.CheckAddTab<MutualSettlementList>("MutualSettlementList", "Взаиморасчёты", true);
            Central.WM.SetActive("MutualSettlementList");
        }
    }
}
