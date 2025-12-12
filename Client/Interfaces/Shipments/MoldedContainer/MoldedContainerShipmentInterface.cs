using Client.Common;
using Client.Interfaces.Stock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Интерфейс Отгрузка литой тары
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public class MoldedContainerShipmentInterface
    {
        public MoldedContainerShipmentInterface() 
        {
            var moldedContainerShipmentList = Central.WM.CheckAddTab<MoldedContainerShipmentList>("MoldedContainerShipmentList", "Отгрузка ЛТ", true);
            Central.WM.SetActive("MoldedContainerShipmentList");
        }
    }
}
