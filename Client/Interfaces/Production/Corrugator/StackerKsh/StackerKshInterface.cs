using Client.Common;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Интерфейс рабочего места стекера ГА Кашира
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public class StackerInterfaceKsh
    {
        public StackerInterfaceKsh()
        {
            var stacker = Central.WM.CheckAddTab<StackerKsh>("machine_stacker_ksh", "Стекер КШ", true, "main", "bottom");
            stacker.ProcessNavigation();

            Central.WM.SetActive("machine_stacker_ksh");
        }
    }
}
