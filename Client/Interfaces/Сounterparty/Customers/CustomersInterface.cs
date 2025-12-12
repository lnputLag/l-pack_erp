using Client.Common;
using Client.Interfaces.Сounterparty.Customers.Tabs;

namespace Client.Interfaces.Сounterparty.Customers
{
    public class CustomersInterface
    {
        /// <summary>
        /// Интерфейс для работы с потребителями
        /// </summary>
        /// <author>volkov_as</author>
        public CustomersInterface()
        {
            Central.WM.AddTab<CustomersListTab>("CustomersMain", true);
            
            Central.WM.SetActive("CustomersListTab");
        }
    }
}