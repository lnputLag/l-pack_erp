using Client.Common;

namespace Client.Interfaces.Production
{
    public class ProductionComplectationPMInterface
    {
        public ProductionComplectationPMInterface()
        {
            var view = Central.WM.CheckAddTab<ComplectationProcesing>("ProductionPMConversion", "Комплектация переработка", true, "main");
            Central.WM.SetActive("ProductionPMConversion");
        }
    }
}
