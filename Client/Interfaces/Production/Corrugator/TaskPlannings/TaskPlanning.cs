using Client.Common;

namespace Client.Interfaces.Production.Corrugator.TaskPlannings
{
    public class TaskPlanningInterface
    {
        public TaskPlanningInterface()
        {
            //Central.WM.CheckAddTab<TaskPlanning>("main", "Планирование заданий на ГА", true, "parent");
            //Central.WM.SetActive("TaskPlanning");

            Central.WM.CheckAddTab<TaskPlanning>("TaskPlanning", "Планирование ПЗ на ГА", true, "main");
            Central.WM.SetActive("TaskPlanning");
        }
    }
}
