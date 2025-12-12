using Client.Common;

namespace Client.Interfaces.Production.Converting.Idles
{
    /// <summary>
    /// Справочники производства
    /// </summary>
    /// <author>motenko_ek</author>
    public class Interface
    {
        public Interface()
        {
            Central.WM.AddTab<ProdConvIdlesTab>("main", true);
            Central.WM.ProcNavigation("main", "ProdConvIdlesTab");
        }
    }
}
