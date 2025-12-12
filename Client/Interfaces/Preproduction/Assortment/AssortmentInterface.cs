using Client.Common;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Интерфейс списка продукции
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public class AssortmentInterface
    {
        public AssortmentInterface()
        {
            Central.WM.AddTab<AssortmentList>("AssortmentListMain", true);
            Central.WM.SetActive("AssortmentList");
        }
    }
}
