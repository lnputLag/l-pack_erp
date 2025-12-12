using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Production.Corrugator.IdlesKsh
{
    public partial class ProdCorrIdlesKshTab : Client.Interfaces.Production.Cmn.Idles.IdlesTab
    {
        public ProdCorrIdlesKshTab() : base(
            2,
            1,
            "[erp]prod_corr_idle_ksh",
            "Простои гофропроизводства КШ")
        { }
    }
}
