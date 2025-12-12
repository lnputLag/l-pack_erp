using Client.Common;
using Client.Interfaces.Stock;

namespace Client.Interfaces.Production.MoldedContainer
{
    /// <summary>
    /// интерфейс управления списание макулатуры в производство
    /// </summary>
    /// <author>sviridov_ae</author>
    public class MoldedContainerConsumptionToProductionInterface
    {
        public MoldedContainerConsumptionToProductionInterface()
        {
            var moldedContainerConsumptionToProduction = Central.WM.CheckAddTab<MoldedContainerConsumptionToProduction>("MoldedContainerConsumptionToProduction", "Списание в производство", true);
            Central.WM.SetActive("MoldedContainerConsumptionToProduction");
        }
    }
}
