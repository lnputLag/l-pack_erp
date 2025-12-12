using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Production.Converting.Idles
{
    public partial class ProdConvIdlesTab : Client.Interfaces.Production.Cmn.Idles.IdlesTab
    {
        public ProdConvIdlesTab() : base(
            1,
            2,
            "[erp]prod_conv_idle",
            "Простои гофропереработки")
        { }
    }
}
