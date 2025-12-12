using Client.Common;

namespace Client.Interfaces.Delivery.Shippings
{
    /// <summary>
    /// Загрузки
    /// </summary>
    /// <author>motenko_ek</author>
    public class Interface
    {
        public Interface()
        {
            Central.WM.AddTab<ShippingsTab>("main", true);
            Central.WM.ProcNavigation("main", "ShippingsTab");
        }
    }
}
