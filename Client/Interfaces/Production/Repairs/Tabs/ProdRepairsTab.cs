using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Production.Repairs
{
    public partial class ProdRepairsTab : Client.Interfaces.Production.Cmn.Repairs.RepairsTab
    {
        public ProdRepairsTab() : base(
            1,
            "[erp]prod_repairs",
            "Ремонты")
        { }
    }
}
