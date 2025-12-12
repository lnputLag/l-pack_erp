using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Common;

namespace Client.Interfaces.Preproduction
{
    public class RawMaterialGroupPlannerInterface
    {
        public RawMaterialGroupPlannerInterface()
        {
            Central.WM.AddTab("RawMaterialGroupPlanner", "Планировщик сырьевых групп");

            var tkGridView = Central.WM.CheckAddTab<RawMaterialGroupList>("RawMaterialGroupPlanner_RawMaterialGroup", "(Сырьевые группы)", false, "RawMaterialGroupPlanner", "bottom");

            var compositionListView = Central.WM.CheckAddTab<CompositionList>("RawMaterialGroupPlanner_Composition", "(Композиции)", false, "RawMaterialGroupPlanner", "bottom");

            Central.WM.SetActive("RawMaterialGroupPlanner_RawMaterialGroup");
        }
    }
}
