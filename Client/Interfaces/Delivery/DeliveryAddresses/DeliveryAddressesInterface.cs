using Client.Common;
using Client.Interfaces.Preproduction;
using Client.Interfaces.DeliveryAddresses;
using System.Collections.Generic;

namespace Client.Interfaces.DeliveryAddresses
{
    /// <summary>
    /// Адреса доставки
    /// </summary>
    /// <author>motenko_ek</author>
    /// <version>1</version>
    /// <released>2025-04-17</released>
    /// <changed>2025-04-17</changed>
    public class DeliveryAddressesInterface
    {
        public DeliveryAddressesInterface()
        {
            {
                Central.WM.AddTab("delivery_addresses", "Адреса доставки");
                
                Central.WM.AddTab<DeliveryToCustomerTab>("delivery_addresses");

                Central.WM.AddTab<ResellerClientTab>("delivery_addresses");

                Central.WM.AddTab<DeliveryFromSupplierTab>("delivery_addresses");

                Central.WM.ProcNavigation("delivery_addresses", "DeliveryToCustomerTab");

                Central.WM.ProcNavigation("delivery_addresses", "ResellerClientTab");

                Central.WM.ProcNavigation("delivery_addresses", "DeliveryFromSupplierTab");
            }
        }
    }
}
