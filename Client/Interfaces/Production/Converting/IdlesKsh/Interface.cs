using Client.Common;
using Client.Interfaces.Preproduction;
using Client.Interfaces.DeliveryAddresses;
using System.Collections.Generic;

namespace Client.Interfaces.Production.Converting.IdlesKsh
{
    /// <summary>
    /// Справочники производства
    /// </summary>
    /// <author>motenko_ek</author>
    public class Interface
    {
        public Interface()
        {
            Central.WM.AddTab<ProdConvIdlesKshTab>("main", true);
            Central.WM.ProcNavigation("main", "ProdConvIdlesKshTab");
        }
    }
}
