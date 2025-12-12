using Client.Common;

namespace Client.Interfaces.Preproduction
{
    public class SampleTaskKshInterface
    {
        public SampleTaskKshInterface()
        {
            Central.WM.AddTab("SampleTaskKshMain", "Задания на образцы КШ");

            string sampleTaskName = "SampleTaskKshList";
            var sampleTaskList = Central.WM.CheckAddTab<SampleTaskKshListTab>(sampleTaskName, "Очередь заданий", false, "SampleTaskKshMain", "bottom");
            sampleTaskList.TabName = sampleTaskName;

            string sampleViewName = "SampleViewKshList";
            var sampleViewList = Central.WM.CheckAddTab<SampleViewKshListTab>(sampleViewName, "Список образцов", false, "SampleTaskKshMain", "bottom");
            sampleViewList.TabName = sampleViewName;

            Central.WM.SetActive(sampleTaskName);

            sampleTaskList.ProcessNavigation();
        }
    }
}
