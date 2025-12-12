using Client.Common;

namespace Client.Interfaces.Production
{
    public class ComplectationListKshInterface
    {
        public ComplectationListKshInterface()
        {
            Central.WM.AddTab("ComplectationKsh", "Список комплектаций КШ");

            var complectationList = Central.WM.CheckAddTab<ComplectationListKsh>("ComplectationListKsh", "Комплектации КШ", false, "ComplectationKsh");

            var complectationWriteOffList = Central.WM.CheckAddTab<ComplectationWriteOffListKsh>("ComplectationWriteOffListKsh", "Списание КШ", false, "ComplectationKsh");

            Central.WM.SetActive("ComplectationListKsh");
        }
    }
}
