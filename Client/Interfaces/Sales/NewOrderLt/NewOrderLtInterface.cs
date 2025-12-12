using Client.Common;
using Client.Interfaces.Sales.NewOrderLt.Tabs;

namespace Client.Interfaces.Sales.NewOrderLt
{
    public class NewOrderLtInterface
    {
        public NewOrderLtInterface()
        {
            {
                Central.WM.AddTab<NewOrderTab>("new_order_lt", true);
                Central.WM.SetActive("NewOrderTab");
            }
        }
    }
}