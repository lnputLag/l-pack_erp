using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Sales
{
    public class OnlineStoreInterface
    {
        public OnlineStoreInterface()
        {
            Central.WM.AddTab("OnlineStore", "Интернет-магазин");

            var viewOnlineStoreOrder = Central.WM.CheckAddTab<OnlineStoreOrderList>("OnlineStoreOrderList", "Заказы интернет-магазина", false, "OnlineStore");
            var viewTechnologicalMapForSite = Central.WM.CheckAddTab<TechnologicalMapForSiteList>("TechnologicalMapForSiteList", "Техкарты для интернет-магазина", false, "OnlineStore");

            Central.WM.SetActive("OnlineStoreOrderList");
        }
    }
}
