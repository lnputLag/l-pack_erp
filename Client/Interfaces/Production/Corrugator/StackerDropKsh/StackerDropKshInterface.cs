using Client.Common;
using Client.Interfaces.Production.Corrugator;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Информация по съёмам на стекерах ГА Кашира
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public class StackerDropKshInterface
    {
        public StackerDropKshInterface()
        {
            var stackerDropKshListRedisView = Central.WM.CheckAddTab<StackerDropKshListRedis>("StackerDropKshListRedis", "Список съёмов Redis", true);
            Central.WM.SetActive("StackerDropKshListRedis");
        }
    }
}
