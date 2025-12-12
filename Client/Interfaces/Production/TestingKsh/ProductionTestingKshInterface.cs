using Client.Common;

namespace Client.Interfaces.Production.Testing
{
    public class ProductionTestingKshInterface
    {
        public ProductionTestingKshInterface()
        {
            Central.WM.AddTab("ProductionTestingKshMain", "Тестирование изделий КШ", true);

            Central.WM.AddTab<ProductionTaskQueueListKsh>("ProductionTestingKshMain");
            Central.WM.AddTab<ProductionTaskForTestingKsh>("ProductionTestingKshMain");

            Central.WM.SetActive("ProductionTaskQueueListKsh");
        }
    }
}
