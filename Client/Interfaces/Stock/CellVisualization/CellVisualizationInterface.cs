using Client.Common;
using Client.Interfaces.Production;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Визуализация ячеек
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public class CellVisualizationInterface
    {
        public CellVisualizationInterface()
        {
            var cellVisualizationBuffer = Central.WM.CheckAddTab<CellVisualizationBuffer>("CellVisualizationBuffer", "Визуализация ячеек", true);

            Central.WM.SetActive("CellVisualizationBuffer");
        }
    }
}
