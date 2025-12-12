using Client.Common;
using Client.Interfaces.Preproduction.PreproductionConfirmOrderLt.Tabs;

namespace Client.Interfaces.Preproduction.PreproductionConfirmOrderLt
{
    /// <summary>
    /// Интерфейс для подтверждения заказа в ЛТ
    /// </summary>
    public class PreproductionConfirmOrderLtInterface
    {
        public PreproductionConfirmOrderLtInterface()
        {
            Central.WM.AddTab<OrderListTab>("OrderConfirmLtMain", true);

            Central.WM.SetActive("OrderListTab");
        }
    }
}
