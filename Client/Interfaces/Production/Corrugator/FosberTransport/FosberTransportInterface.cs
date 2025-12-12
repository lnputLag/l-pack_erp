using Client.Common;
using System.Collections.Generic;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// интерфейс "Транспортная системв Fosber"
    /// </summary>
    /// <author>zelenskiy_sv</author>
    public class FosberTransportSystemInterface
    {
        public FosberTransportSystemInterface()
        {
            var fosberTransportInterface = Central.WM.CheckAddTab<FosberTransportMonitor>("FosberTransportSystem_FosberTransportSystem", "Транспортная система Fosber", true, "FosberTransportSystem", "bottom");
            Central.WM.SetActive("FosberTransportSystem_FosberTransportSystem");
        }
    }
}


