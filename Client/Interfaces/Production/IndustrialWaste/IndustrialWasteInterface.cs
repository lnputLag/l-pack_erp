using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Common;

namespace Client.Interfaces.Production.IndustrialWaste
{
    public class IndustrialWasteInterface
    {
        public IndustrialWasteInterface(int factId)
        {
            var unitWeight = new UnitWeight(factId);

            if (factId == 2)
            {
                Central.WM.AddTab("Industrial_Waste_Ksh", "Весы шредера переработки КШ", true, "", unitWeight);
                Central.WM.SetActive("Industrial_Waste_Ksh");
            }
            else if (factId == 1)
            {
                Central.WM.AddTab("Industrial_Waste", "Весы шредера", true, "", unitWeight);
                Central.WM.SetActive("Industrial_Waste");
            }
        }
    }
}
