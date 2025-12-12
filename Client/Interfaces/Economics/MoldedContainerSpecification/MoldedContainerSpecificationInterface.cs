using Client.Common;

namespace Client.Interfaces.Economics.MoldedContainer
{
    /// <summary>
    /// Интерфейс для спецификаций литой тары
    /// </summary>
    /// <author>ryasnoi_pv</author>
    public class MoldedContainerSpecificationInterface
    {
        public MoldedContainerSpecificationInterface()
        {
            Central.WM.AddTab<MoldedContainerSpecificationTab>("molded_container_specification_main", true);
            Central.WM.SetActive("MoldedContainerSpecificationTab");

        }
    }
}
