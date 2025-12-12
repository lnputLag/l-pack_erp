using Client.Common;

namespace Client.Interfaces.Production
{
    public class ProductionRecomplectationInterface
    {
        public ProductionRecomplectationInterface()
        {
            var view = Central.WM.CheckAddTab<Recomplectation>("Recomplectation", "Перекомплектация", true, "main");
            Central.WM.SetActive("Recomplectation");
        }
    }
}
