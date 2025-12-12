using Client.Common;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Интерфейс "Образцы"
    /// </summary>
    /// <author>Рясной П.В.</author>
    public class SampleInterface
    {
        public SampleInterface()
        {

            Central.WM.AddTab("SampleMain", "Образцы");

            string sampleAcceptedName = "SampleAcceptedList";
            var sampleAcceptedList = Central.WM.CheckAddTab<SampleListAccepted>(sampleAcceptedName, "Приемка образцов", false, "SampleMain", "bottom");
            sampleAcceptedList.TabName = sampleAcceptedName;

            string sampleName = "SampleList";
            var sampleList = Central.WM.CheckAddTab<SampleList>(sampleName, "Образцы на плоттере", false, "SampleMain", "bottom");
            sampleList.TabName = sampleName;

            string sampleProductionName = "SampleProductionList";
            var sampleProductionList = Central.WM.CheckAddTab<SampleProductionList>(sampleProductionName, "Образцы с линии", false, "SampleMain", "bottom");
            sampleProductionList.TabName = sampleProductionName;

            Central.WM.SetActive(sampleList.TabName);
        }
    }
}
