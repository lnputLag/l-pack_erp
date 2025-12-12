using Client.Common;
using Client.Interfaces.Preproduction;
using Client.Interfaces.Sources;
using System.Collections.Generic;

namespace Client.Interfaces.Sources
{
    /// <summary>
    /// комплектующие
    /// </summary>
    /// <author>lavrenteva_ma/author>
    /// <version>1</version>
    /// <released>2025-02-11</released>
    /// <changed>2025-02-11</changed>
    public class SourcesInterface
    {
        public SourcesInterface()
        {
            {
                Central.WM.AddTab<SetsTab>("sets", true);
                Central.WM.ProcNavigation("sets", "SetsTab");
            }
        }
    }
}
