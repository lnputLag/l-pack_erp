using Client.Common;

namespace Client.Interfaces.Preproduction.SampleDrawing
{
    /// <summary>
    /// Интерфейс "Образцы для чертежа"
    /// </summary>
    /// <author>vlasov_ea</author>
    class SampleDrawingInterface
    {
        public SampleDrawingInterface()
        {
            Central.WM.AddTab("SampleDrawingMain", "Образцы для конструктора");

            Central.WM.AddTab<SampleDrawingList>("SampleDrawingMain");

            string sampleConstructorName = "SampleForConstructor";
            var sampleForConstructor = Central.WM.CheckAddTab<SampleConstructionList>(sampleConstructorName, "Тестовые образцы", false, "SampleDrawingMain", "bottom");
            sampleForConstructor.TabName = sampleConstructorName;

            Central.WM.SetActive("SampleDrawingList");
        }
    }
}
