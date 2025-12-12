using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Production.MoldedContainer
{
    /// <summary>
    /// интерфейс переоприходования макулатуры литой тары
    /// </summary>
    /// <author>sviridov_ae</author>
    public class RearrivalScrapPaperInterface
    {
        public RearrivalScrapPaperInterface()
        {
            var rearrivalScrapPaper = Central.WM.CheckAddTab<RearrivalScrapPaper>("RearrivalScrapPaper", "Переоприходование макулатуры", true);
            Central.WM.SetActive("RearrivalScrapPaper");
        }
    }
}
