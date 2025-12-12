using Client.Common;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// интерфейс "диаграмма рулонов на ГА"
    /// </summary>
    /// <author>balchugov_dv</author>
    public class ReelControlInterface
    {
        public ReelControlInterface()
        {
            var rollControl=new RollControl();
            Central.WM.AddTab("RollControl", "Управление раскатом", true, "", rollControl);
            Central.WM.SetActive("RollControl");

            rollControl.ProcessNavigation();
        }
    }
}


