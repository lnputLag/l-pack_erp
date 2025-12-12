using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Preproduction
{
    public class TechnologicalMapInterface
    {
        public TechnologicalMapInterface()
        {
            Central.WM.AddTab<TechnologicalMapList>("TechnologicalMapMain", true);
            Central.WM.SetActive("TechnologicalMapList");

        }
    }
}
