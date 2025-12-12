using Client.Common;
using System.Collections.Generic;

namespace Client.Interfaces.Preproduction
{
    public class SampleAccountingInterface
    {
        public SampleAccountingInterface()
        {
            Central.WM.AddTab("SampleAccountingMain", "Учет образцов");

            Central.WM.AddTab<SampleConfirmationList>("SampleAccountingMain", false);

            string sampleAccountingName = "SampleAccountingList";
            var sampleAccounting = Central.WM.CheckAddTab<SampleAccountingList>(sampleAccountingName, "Образцы", false, "SampleAccountingMain", "bottom");
            sampleAccounting.TabName = sampleAccountingName;


            // Заполняем активных менеджеров
            if (!Central.SessionValues.ContainsKey("ManagersConfig"))
            {
                Central.SessionValues.Add("ManagersConfig", new Dictionary<string, string>());
            }

            if (!Central.SessionValues["ManagersConfig"].ContainsKey("ListActive"))
            {
                Central.SessionValues["ManagersConfig"].Add("ListActive", Central.User.EmployeeId.ToString());
            }

            Central.WM.SetActive("SampleConfirmationList");
        }
    }
}
