using Client.Common;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Интерфейс заявок на оснастку
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public class RigOrderInterface
    {
        public RigOrderInterface()
        {
            Central.WM.AddTab<RigOrderContainerTab>("RigOrderContainerMain", true);
            Central.WM.SetActive("RigOrderContainerTab");
        }
    }
}
