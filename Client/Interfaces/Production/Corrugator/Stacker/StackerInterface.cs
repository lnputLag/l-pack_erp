using Client.Common;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Интерфейс рабочего места стекера ГА
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public class StackerInterface
    {
        public StackerInterface()
        {
            var stacker = Central.WM.CheckAddTab<Stacker>("machine_stacker", "Стекер", true, "main", "bottom");
            stacker.ProcessNavigation();

            Central.WM.SetActive("machine_stacker");
        }
    }
}
