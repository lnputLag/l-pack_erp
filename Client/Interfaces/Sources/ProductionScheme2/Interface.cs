using Client.Common;
using Client.Interfaces.Preproduction;
using Client.Interfaces.DeliveryAddresses;
using System.Collections.Generic;

namespace Client.Interfaces.Sources.ProductionScheme2
{
    /// <summary>
    /// Справочники производства
    /// </summary>
    /// <author>motenko_ek</author>
    public class Interface
    {
        public Interface()
        {
            Central.WM.AddTab<ProductionScheme2Tab>("main", true);
            Central.WM.ProcNavigation("main", "ProductionScheme2Tab");
        }
    }
}
