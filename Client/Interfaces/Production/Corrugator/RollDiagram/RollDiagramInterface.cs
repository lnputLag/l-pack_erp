using Client.Common;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// интерфейс "диаграмма рулонов на ГА"
    /// </summary>
    /// <author>balchugov_dv</author>
    public class RollRegistrationInterface
    {
        public RollRegistrationInterface()
        {
            //главная вкладка
            Central.WM.AddTab("RollsCM", "Учет рулонов на ГА");

            var rollListForCompletedTasks = new RollListForCompletedTasks();
            Central.WM.AddTab("RollListForCompletedTasks", "Привязка рулонов к ПЗ", false, "RollsCM", rollListForCompletedTasks, "bottom");

            var rollDiagram =new RollDiagram();
            Central.WM.AddTab("RollDiagram", "Диаграмма рулонов на ГА", false, "RollsCM", rollDiagram, "bottom");

            //ставим фокус на вкладку по умолчанию
            Central.WM.SetActive("RollListForCompletedTasks");


            //FIXME: 2022-07-27_F1
            //    implement Central.Navigator.Address.Processed
        }
    }
}


