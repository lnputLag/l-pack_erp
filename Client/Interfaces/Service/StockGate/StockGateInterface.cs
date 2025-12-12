using Client.Common;
using System.Collections.Generic;

namespace Client.Interfaces.Service
{
    /// <summary>
    /// Список машин для проезда через шлагбаум на СГП
    /// </summary>
    /// <author>Грешных Н.И.</author>
    /// <version>1</version>
    /// <released>2023-09-12</released>
    /// <changed>2023-09-12</changed>
    /// <changed>2023-12-20</changed>
    public class StockGateInterface
    {
        public StockGateInterface()
        {
            Central.WM.AddTab("transport_access", "Допуск автотранспорта");
            Central.WM.CheckAddTab<CarList>("persistent", "Оформление допуска", false, "transport_access", "bottom");
            Central.WM.CheckAddTab<ExpectedCarList>("pending", "Ожидаемый допуск", false, "transport_access", "bottom");

            Central.WM.SetActive("persistent");
        }
    }
}
