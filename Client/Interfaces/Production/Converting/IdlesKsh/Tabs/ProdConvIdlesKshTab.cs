using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Production.Converting.IdlesKsh
{
    public partial class ProdConvIdlesKshTab : Client.Interfaces.Production.Cmn.Idles.IdlesTab
    {
        public ProdConvIdlesKshTab() : base(
            2,
            2,
            "[erp]prod_conv_idle_ksh",
            "Простои гофропереработки КШ")
        { }
    }
}
