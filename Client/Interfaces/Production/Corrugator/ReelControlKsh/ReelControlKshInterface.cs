using Client.Common;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// интерфейс управления раскатами для Каширы
    /// </summary>
    /// <author>balchugov_dv</author>
    public class ReelControlKshInterface
    {
        public ReelControlKshInterface()
        {
            var reelControl=new ReelControlKsh();
            Central.WM.AddTab("ReelControlKsh", "Управление раскатом КШ", true, "", reelControl);
            Central.WM.SetActive("ReelControlKsh");

            reelControl.ProcessNavigation();
        }
    }
}


