using Client.Common;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Интерфейс технолога-изготовителя образцов
    /// </summary>
    /// <author>ryasnoy_pv</author>
    class SampleTaskInterface
    {
        public SampleTaskInterface()
        {
            Central.WM.AddTab("SampleTaskMain", "Задания на образцы");

            string sampleTaskName = "SampleTaskList";
            var sampleTaskList = Central.WM.CheckAddTab<SampleTaskList>(sampleTaskName, "Очередь заданий", false, "SampleTaskMain", "bottom");
            sampleTaskList.TabName = sampleTaskName;

            string sampleViewName = "SampleViewList";
            var sampleViewList = Central.WM.CheckAddTab<SampleViewList>(sampleViewName, "Список образцов", false, "SampleTaskMain", "bottom");
            sampleViewList.TabName = sampleViewName;

            Central.WM.SetActive(sampleTaskName);

            sampleTaskList.ProcessNavigation();
        }
    }
}
