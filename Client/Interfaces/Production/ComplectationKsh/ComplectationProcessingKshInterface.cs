using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Common;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Комплектация переработка Кашира
    /// </summary>
    /// <author>sviridov_ae</author>
    public class ComplectationProcessingKshInterface
    {
        public ComplectationProcessingKshInterface()
        {
            var viewComplectationProcessingKsh = Central.WM.CheckAddTab<ComplectationProcessingKsh>("ComplectationProcessingKsh", "Комплектация переработка КШ", true, "main");
            Central.WM.SetActive("ComplectationProcessingKsh");
        }
    }
}
