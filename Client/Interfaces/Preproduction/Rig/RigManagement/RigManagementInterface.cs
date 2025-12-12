using Client.Common;
using System.Collections.Generic;

namespace Client.Interfaces.Preproduction.Rig
{
    public class RigManagementInterface
    {
        public RigManagementInterface()
        {
            // Заполняем активных менеджеров
            if (!Central.SessionValues.ContainsKey("ManagersConfig"))
            {
                Central.SessionValues.Add("ManagersConfig", new Dictionary<string, string>());
            }

            if (!Central.SessionValues["ManagersConfig"].ContainsKey("ListActive"))
            {
                Central.SessionValues["ManagersConfig"].Add("ListActive", Central.User.EmployeeId.ToString());
            }

            Central.WM.AddTab("RigManagementMain", "Управление оснасткой");

            Central.WM.AddTab("RigClicheTab", "Клише", false, "RigManagementMain");

            string unpaidClicheName = "ClicheUnpaidList";
            var unpaidClicheTab = Central.WM.CheckAddTab<RigClicheListUnpaid>(unpaidClicheName, "Неразрешенные", false, "RigClicheTab", "bottom");
            unpaidClicheTab.TabName = unpaidClicheName;

            string clicheTransferName = "ClicheTransferList";
            var clicheTransferTab = Central.WM.CheckAddTab<RigClicheTransferList>(clicheTransferName, "На передачу", false, "RigClicheTab", "bottom");
            clicheTransferTab.TabName = clicheTransferName;

            Central.WM.AddTab("RigShtanzTab", "Штанцформы", false, "RigManagementMain");

            string unpaidShtanzName = "ShtanzUnpaidList";
            var unpaidShtanzTab = Central.WM.CheckAddTab<RigShtanzListUnpaid>(unpaidShtanzName, "Неразрешенные", false, "RigShtanzTab", "bottom");
            unpaidShtanzTab.TabName = unpaidShtanzName;
            /*
            string shtanzTransferName = "ShtanzTransferList";
            var shtanzTransferTab = Central.WM.CheckAddTab<RigShtanzTransferList>(shtanzTransferName, "На передачу", false, "RigShtanzTab", "bottom");
            shtanzTransferTab.TabName = shtanzTransferName;
            */
            Central.WM.AddTab<RigShtanzTransferList>("RigShtanzTab");

            Central.WM.AddTab("ContainerRigTab", "Клише ЛТ", false, "RigManagementMain");
            Central.WM.AddTab<ContainerRigListUnallowable>("ContainerRigTab");


            Central.WM.SetActive("RigClicheTab");
            Central.WM.SetActive(unpaidClicheName);
        }
    }
}
