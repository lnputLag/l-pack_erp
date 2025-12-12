using Client.Common;
using Client.Interfaces.Preproduction;
using Client.Interfaces.Sources;
using System.Collections.Generic;

namespace Client.Interfaces.Sources
{
    /// <summary>
    /// Схемы производства
    /// </summary>
    /// <author>lavrenteva_ma/author>
    /// <version>1</version>
    /// <released>2025-02-27</released>
    /// <changed>2025-02-27</changed>
    public class ProductionSchemeInterface
    {
        public ProductionSchemeInterface()
        {
            {
                Central.WM.AddTab<ProductionSchemeTab>("production_scheme", true);
                Central.WM.ProcNavigation("production_scheme", "ProductionSchemeTab");
            }
        }
    }
}
