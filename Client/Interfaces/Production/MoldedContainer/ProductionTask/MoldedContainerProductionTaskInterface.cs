using Client.Common;

namespace Client.Interfaces.Production.MoldedContainer
{ 
    /// <summary>
    /// производственные задания на литую тару
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2024-07-10</released>
    /// <changed>2024-07-10</changed>
    public class MoldedContainerProductionTaskInterface
    {
        public MoldedContainerProductionTaskInterface()
        {
            Central.WM.AddTab<ProductionTaskTab>("molded_container_pt", true);
            Central.WM.SetActive("ProductionTaskTab");
        }
    }
}
