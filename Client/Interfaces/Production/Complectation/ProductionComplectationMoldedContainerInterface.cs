using Client.Common;

namespace Client.Interfaces.Production
{
    public class ProductionComplectationMoldedContainerInterface
    {
        public ProductionComplectationMoldedContainerInterface()
        {
            var view = Central.WM.CheckAddTab<ComplectationMoldedContainer>("ProductionComplectationMoldedContainer", "Комплектация ЛТ", true, "main");
            Central.WM.SetActive("ProductionComplectationMoldedContainer");
        }
    }
}
