using Client.Common;

namespace Client.Interfaces.Production.ScalesShredderKsh
{
    /// <summary>
    /// Интерфейс "Весы шредера Кашира"
    /// </summary>
    /// <author>greshnyh_ni</author>
    public class ScalesShredderInterfaceKsh
    {
        public ScalesShredderInterfaceKsh()
        {
            string scalesShredderKshName = "ScalesShredderKsh";
            var scalesShredderKsh = Central.WM.CheckAddTab<ScalesShredderKshTab>(scalesShredderKshName, "Весы макулатурного пресса КШ", true, "", "top");
            scalesShredderKsh.TabName = scalesShredderKshName;

            Central.WM.SetActive(scalesShredderKshName);
        }
    }
}
