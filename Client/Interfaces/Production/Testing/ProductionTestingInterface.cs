using Client.Common;

namespace Client.Interfaces.Production.Testing
{
    public class ProductionTestingInterface
    {
        public ProductionTestingInterface()
        {
            Central.WM.AddTab("ProductionTestingMain", "Тестирование изделий", true);

            Central.WM.AddTab<ProductionTaskQueueList>("ProductionTestingMain");
            Central.WM.AddTab<ProductionTaskForTesting>("ProductionTestingMain");

            Central.WM.SetActive("ProductionTaskQueueList");
        }
    }
}
