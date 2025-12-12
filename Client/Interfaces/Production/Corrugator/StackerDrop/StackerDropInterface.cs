using Client.Common;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Интерфейс съёмов стекера ГА
    /// </summary>
    /// <author>sviridov_ae</author>
    public class StackerDropInterface
    {
        public StackerDropInterface()
        {
            var stackerDropList = Central.WM.CheckAddTab<StackerDropList>("stacker_drop_list", "Список съёмов", true, "main", "bottom");
            Central.WM.SetActive("stacker_drop_list");
        }
    }
}
