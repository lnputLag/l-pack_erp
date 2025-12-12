using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Preproduction
{
    public class TechnologicalMapSetsInterface
    {
        public TechnologicalMapSetsInterface()
        {
            Central.WM.AddTab<TechnologicalMapSetsList>("TechnologicalMapMain", true);
            Central.WM.SetActive("TechnologicalMapSetsList");

        }
    }
}