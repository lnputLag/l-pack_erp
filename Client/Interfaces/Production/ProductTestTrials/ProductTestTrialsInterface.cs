using Client.Common;

namespace Client.Interfaces.Production.ProductTestTrials
{
    public class ProductTestTrialsInterface
    {
        public ProductTestTrialsInterface()
        {
            Central.WM.AddTab("ProductionTestingTrialsMain", "Тестовые испытания изделия");

            var measurementsList = Central.WM.CheckAddTab<MeasurementsTab>("MeasurementsList", "Измерения", false, "ProductionTestingTrialsMain", "bottom");

            var testsList = Central.WM.CheckAddTab<TestsTab>("TestsList", "Испытания", false,
                "ProductionTestingTrialsMain", "bottom");

            var temperatureHumidityControlList = Central.WM.CheckAddTab<TemperatureHumidityControlTab>("TemperatureHumidityControlList", "Контроль температуры и влажности", false,
                "ProductionTestingTrialsMain", "bottom");


            Central.WM.SetActive("MeasurementsList");
        }
    }
}
