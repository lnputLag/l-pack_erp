using Client.Common;
using Client.Interfaces.Production.Strapper;

namespace Client.Interfaces.Production
{
    public class StrapperKshInterface
    {
        public StrapperKshInterface()
        {
            var view = Central.WM.CheckAddTab<StrapperKsh>("StrapperKsh", "Упаковщик КШ", true, "main");
            Central.WM.SetActive("StrapperKsh");
        }
    }
}
