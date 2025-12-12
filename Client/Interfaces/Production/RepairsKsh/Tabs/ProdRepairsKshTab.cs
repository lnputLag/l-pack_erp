using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Production.RepairsKsh
{
    public partial class ProdRepairsKshTab : Client.Interfaces.Production.Cmn.Repairs.RepairsTab
    {
        public ProdRepairsKshTab() : base(
            2,
            "[erp]prod_repairs_ksh",
            "Ремонты КШ")
        { }
    }
}
