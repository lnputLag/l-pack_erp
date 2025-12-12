using Client.Common;

namespace Client.Interfaces.Production.ProductTestTrialsKsh
{
    public class ProductTestTrialsKshInterface
    {
        public ProductTestTrialsKshInterface()
        {
            Central.WM.AddTab("ProductionTestingTrialsKshMain", "Тестовые испытания изделия КШ");

            var measurementsList = Central.WM.CheckAddTab<MeasurementsTab>("MeasurementsListKsh", "Измерения", false, "ProductionTestingTrialsKshMain", "bottom");

            var testsList = Central.WM.CheckAddTab<TestsTab>("TestsListKsh", "Испытания", false,
                "ProductionTestingTrialsKshMain", "bottom");

            var temperatureHumidityControlList = Central.WM.CheckAddTab<TemperatureHumidityControlTab>("TemperatureHumidityControlListKsh", "Контроль температуры и влажности", false,
                "ProductionTestingTrialsKshMain", "bottom");


            Central.WM.SetActive("MeasurementsListKsh");
        }
    }
}
