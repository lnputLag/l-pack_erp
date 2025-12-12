using Client.Common;

namespace Client.Interfaces.Production
{
    public class ProductionComplectationCMInterface
    {
        public ProductionComplectationCMInterface()
        {
            var view = Central.WM.CheckAddTab<ComplectationCorrugator>("ProductionComplectationCM", "Комплектация ГА", true, "main");
            Central.WM.SetActive("ProductionComplectationCM");
        }
    }
}
