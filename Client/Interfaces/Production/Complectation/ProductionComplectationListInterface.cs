using Client.Common;
using Client.Interfaces.Production.Complectation;
using Client.Interfaces.Stock;

namespace Client.Interfaces.Production
{
    public class ProductionComplectationListInterface
    {
        public ProductionComplectationListInterface()
        {
            Central.WM.AddTab("ProductionComplectationList", "Список комплектаций");

            var complectationList = Central.WM.CheckAddTab<ComplectationList>("ProductionComplectationList_ComplectationList", "Комплектации", false, "ProductionComplectationList");

            var complectationWriteOffList = Central.WM.CheckAddTab<ComplectationWriteOffList>("ProductionComplectationList_WriteOffList", "Списание", false, "ProductionComplectationList");

            var complectationMovingCMList = Central.WM.CheckAddTab<ComplectationMovingCMList>("ProductionComplectationList_MovingCMList", "Поддоны в К-1", false, "ProductionComplectationList");

            Central.WM.AddTab<ComplectationCorrugatorInStockList>("ProductionComplectationList");

            Central.WM.SetActive("ProductionComplectationList_ComplectationList");
        }
    }
}
