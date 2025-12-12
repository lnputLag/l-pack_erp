using Client.Common;
using Client.Interfaces.DeliveryAddresses;
using Client.Interfaces.Preproduction;
using System.Collections.Generic;

namespace Client.Interfaces.Production.RepairsKsh
{
    /// <summary>
    /// Справочники производства
    /// </summary>
    /// <author>motenko_ek</author>
    public class Interface
    {
        public Interface()
        {
            Central.WM.AddTab<ProdRepairsKshTab>("main", true);
            Central.WM.ProcNavigation("main", "ProdRepairsKshTab");
        }
    }
}
