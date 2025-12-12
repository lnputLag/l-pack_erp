using Client.Common;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Интерфейс "Образцы от клиента"
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public class PatternOrdersInterface
    {
        public PatternOrdersInterface()
        {
            Central.WM.AddTab<PatternOrdersList>("PatternOrderMain", true);

            Central.WM.SetActive("PatternOrdersList");
        }
    }
}
