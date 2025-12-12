using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Common;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Комплектация ГА Кашира
    /// </summary>
    /// <author>sviridov_ae</author>
    public class ComplectationCorrugatorKshInterface
    {
        public ComplectationCorrugatorKshInterface()
        {
            var viewComplectationCorrugatorKsh = Central.WM.CheckAddTab<ComplectationCorrugatorKsh>("ComplectationCorrugatorKsh", "Комплектация ГА КШ", true, "main");
            Central.WM.SetActive("ComplectationCorrugatorKsh");
        }
    }
}
