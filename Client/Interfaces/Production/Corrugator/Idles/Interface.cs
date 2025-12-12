using Client.Common;

namespace Client.Interfaces.Production.Corrugator.Idles
{
    /// <summary>
    /// Справочники производства
    /// </summary>
    /// <author>motenko_ek</author>
    public class Interface
    {
        public Interface()
        {
            Central.WM.AddTab<ProdCorrIdlesTab>("main", true);
            Central.WM.ProcNavigation("main", "ProdCorrIdlesTab");
        }
    }
}
