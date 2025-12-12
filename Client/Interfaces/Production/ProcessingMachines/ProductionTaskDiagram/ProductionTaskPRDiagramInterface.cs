using Client.Common;

namespace Client.Interfaces.Production.ProcessingMachines
{
    /// <summary>
    /// интерфейс "Диаграмма ПЗ на переработке"
    /// </summary>
    /// <author>balchugov_dv</author>
    public class ProductionTaskPRDiagramInterface
    {
        public ProductionTaskPRDiagramInterface()
        {
            var productionTaskDiagram=new ProductionTaskDiagram();
            Central.WM.AddTab("ProductionTaskDiagram", "Диаграмма ПЗ на переработке", true, "", productionTaskDiagram);
        }
    }
}


