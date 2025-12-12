using Client.Common;
using Client.Interfaces.Preproduction;
using Client.Interfaces.Accounts;
using System.Collections.Generic;

namespace Client.Interfaces.Accounts
{
    /// <summary>
    /// учетные записи
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-11-10</released>
    /// <changed>2024-04-22</changed>
    public class AccountsInterface
    {
        public AccountsInterface()
        {
            {
                Central.WM.AddTab("account_control", "Учетные записи");
                
                Central.WM.AddTab<AccountTab>("account_control");
                Central.WM.AddTab<EmployeeTab>("account_control");
                Central.WM.AddTab<DepartmentPositionTab>("account_control");
                Central.WM.AddTab<RoleTab>("account_control");
                Central.WM.AddTab<GroupTab>("account_control");
                Central.WM.AddTab<NavigatorTab>("account_control");

                Central.WM.ProcNavigation("account_control", "EmployeeTab");
            }
        }
    }
}
