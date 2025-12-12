using Client.Common;
using Client.Interfaces.Preproduction;
using Client.Interfaces.Sources;
using System.Collections.Generic;

namespace Client.Interfaces.Sources
{
    /// <summary>
    /// Перестил
    /// </summary>
    /// <author>lavrenteva_ma/author>
    /// <version>1</version>
    /// <released>2025-02-24</released>
    /// <changed>2025-02-24</changed>
    public class InterlayerInterface
    {
        public InterlayerInterface()
        {
            {
                Central.WM.AddTab<InterlayerTab>("interlayer", true);
                Central.WM.ProcNavigation("interlayer", "InterlayerTab");
            }
        }
    }
}
