using Client.Common;
using Client.Interfaces.Preproduction;
using Client.Interfaces.DeliveryAddresses;
using System.Collections.Generic;

namespace Client.Interfaces.ProductionCatalog
{
    /// <summary>
    /// Справочники производства
    /// </summary>
    /// <author>motenko_ek</author>
    public class ProductionCatalogInterface
    {
        public ProductionCatalogInterface()
        {
            {
                Central.WM.AddTab("production_catalog", "Справочники производства");
                
                Central.WM.AddTab<ProductionCatalogMachineTab>("production_catalog");
                Central.WM.AddTab<ProductionCatalogWorkcenterTab>("production_catalog");
                Central.WM.AddTab<ProductionCatalogSchemeTab>("production_catalog");

                Central.WM.ProcNavigation("production_catalog", "ProductionCatalogMachineTab");
            }
        }
    }
}
