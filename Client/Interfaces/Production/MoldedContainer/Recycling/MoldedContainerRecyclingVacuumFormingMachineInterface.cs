using Client.Common;
using Client.Interfaces.Production.MoldedContainer.Report.Tabs;
using Client.Interfaces.Stock;

namespace Client.Interfaces.Production.MoldedContainer
{ 
    /// <summary>
    /// производственные задания на литую тару для ВФМ (станки 301,302,303,304) 
    /// 306 - ВФМ-1 и 305- ВФМ-2
    /// </summary>
    /// <author>greshnyh_ni</author>
    /// <version>1</version>
    /// <released>2025-04-28</released>3
    /// <changed>2025-04-28</changed>
    public class MoldedContainerRecyclingVacuumFormingMachineInterface
    {
        public MoldedContainerRecyclingVacuumFormingMachineInterface()
        {
//            var VacuumFormingMachineTab = Central.WM.CheckAddTab<RecyclingVacuumFormingMachineTab>("RecyclingVacuumFormingMachineTab", "ВФМ ЛТ", true);
//            Central.WM.SetActive("RecyclingVacuumFormingMachineTab");

            Central.WM.AddTab("RecyclingVacuumFormingMachine", "ВФМ ЛТ");
            var VacuumFormingMachine = Central.WM.CheckAddTab<RecyclingVacuumFormingMachineTab>("RecyclingVacuumFormingMachineTab", "ВФМ ЛТ", false, "RecyclingVacuumFormingMachine", "bottom");
            var VacuumFormingMachineWeight = Central.WM.CheckAddTab<RecyclingVacuumFormingMachineWeight>("RecyclingVacuumFormingMachineWeight", "Веса заготовок", false, "RecyclingVacuumFormingMachine", "bottom");

            //ставим фокус на вкладку по умолчанию
            Central.WM.SetActive("RecyclingVacuumFormingMachineTab");


        }
    }
}
