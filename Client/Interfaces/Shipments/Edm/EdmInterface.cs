using Client.Common;

namespace Client.Interfaces.Shipments
{
    public class ShipmentEdmInterface
    {
        public ShipmentEdmInterface()
        {
            var shipmentsEdm = new EdmSalesList();
            Central.WM.AddTab("edm", "ЭДО", true, "ShipmentsEdm", shipmentsEdm);
            Central.WM.SetActive("edm");
        }
    }
}
