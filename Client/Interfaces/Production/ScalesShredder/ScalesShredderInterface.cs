using Client.Common;

namespace Client.Interfaces.Production.ScalesShredder
{
    /// <summary>
    /// Интерфейс "Весы шредера"
    /// </summary>
    /// <author>vlasov_ea</author>
    public class ScalesShredderInterface
    {
        public ScalesShredderInterface()
        {
            string scalesShredderName = "ScalesShredder";
            var scalesShredder = Central.WM.CheckAddTab<ScalesShredderTab>(scalesShredderName, "Весы макулатурного пресса", true, "", "top");
            //FIXME:
            scalesShredder.TabName = scalesShredderName;

            Central.WM.SetActive(scalesShredderName);
        }
    }
}
