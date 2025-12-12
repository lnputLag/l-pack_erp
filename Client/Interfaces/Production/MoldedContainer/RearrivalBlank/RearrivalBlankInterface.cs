using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Production.MoldedContainer
{
    /// <summary>
    /// интерфейс переоприходования заготовок литой тары
    /// </summary>
    /// <author>sviridov_ae</author>
    public class RearrivalBlankInterface
    {
        public RearrivalBlankInterface()
        {
            var rearrivalBlank = Central.WM.CheckAddTab<RearrivalBlank>("RearrivalBlank", "Переоприходование заготовок", true);
            Central.WM.SetActive("RearrivalBlank");
        }
    }
}
