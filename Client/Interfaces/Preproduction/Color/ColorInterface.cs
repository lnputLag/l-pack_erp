using Client.Common;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Интерфейс для работы с цветами
    /// </summary>
    /// <author>eletskikh_ya</author>
    public class ColorInterface
    {
        public ColorInterface()
        {
            Central.WM.AddTab("colorMain", "Краски", true);

            string colorTabName = "ColorList";
            var colorListTab = Central.WM.CheckAddTab<ColorList>(colorTabName, "Краска ГП", false, "colorMain", "bottom");
            colorListTab.TabName = colorTabName;

            Central.WM.AddTab<ColorOffsetTab>("colorMain", false, "bottom");

            Central.WM.SetActive(colorTabName);
        }
    }
}
