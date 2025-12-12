using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Production.Corrugator.Idles
{
    public partial class ProdCorrIdlesTab : Client.Interfaces.Production.Cmn.Idles.IdlesTab
    {
        public ProdCorrIdlesTab() : base(
            1,
            1,
            "[erp]prod_corr_idle",
            "Простои гофропроизводства")
        { }
    }
}
