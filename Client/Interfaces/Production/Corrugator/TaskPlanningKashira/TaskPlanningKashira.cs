using Client.Common;

namespace Client.Interfaces.Production.Corrugator.TaskPlanningKashira
{
    public class TaskPlanningKashiraInterface
    {
        public TaskPlanningKashiraInterface()
        {
            Central.WM.CheckAddTab<TaskPlanningKsh>("TaskPlanningKsh", "Планирование ПЗ на ГА КШ", true, "main");
            Central.WM.SetActive("TaskPlanningKsh");
        }
    }
}
