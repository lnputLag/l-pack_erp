using Client.Common;
using Client.Interfaces.Orders.MoldedContainer;
using Client.Interfaces.Production;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Интерфейс по Претензии по складу рулонов
    /// </summary>
    /// <autor>greshnyh_ni</autor>
    public class ClaimStockRollsInterface
    {
        public ClaimStockRollsInterface()
        {
            var view = Central.WM.CheckAddTab<RollsClaimStockList>("ClaimStockRolls", "Претензии по складу рулонов", true, "main");
            Central.WM.SetActive("ClaimStockRolls");
        }
    }
}
