using Client.Common;
using Client.Interfaces.DeliveryAddresses;
using Client.Interfaces.Preproduction;
using System.Collections.Generic;

namespace Client.Interfaces.Production.Repairs
{
    /// <summary>
    /// Справочники производства
    /// </summary>
    /// <author>motenko_ek</author>
    public class Interface
    {
        public Interface()
        {
            Central.WM.AddTab<ProdRepairsTab>("main", true);
            Central.WM.ProcNavigation("main", "ProdRepairsTab");
        }
    }
}
