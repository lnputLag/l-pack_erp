using Client.Common;

namespace Client.Interfaces.Stock.ForkliftDrivers
{
    /// <summary>
    /// Интерфейс погрузчики Кашира
    /// </summary>
    /// <author>sviridov_ae</author>
    public class ForkliftDriversKshInterface
    {
        public ForkliftDriversKshInterface()
        {
            Central.WM.AddTab("ForkliftDriversKsh", "Погрузчики КШ");

            var forkliftDriverLog = new ForkliftDriverKshLog();
            Central.WM.AddTab("ForkliftDriversKshLog", "Журнал работ", false, "ForkliftDriversKsh", forkliftDriverLog, "bottom");

            var forkliftDriverList = new ForkliftDriverKshList();
            Central.WM.AddTab("ForkliftDriverKshList", "Водители", false, "ForkliftDriversKsh", forkliftDriverList, "bottom");

            Central.WM.SetActive("ForkliftDriversKshLog");
        }
    }
}
