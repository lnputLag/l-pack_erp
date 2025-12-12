using Client.Common;
using Client.Interfaces.Preproduction;
using System.Collections.Generic;

namespace Client.Interfaces.Accounts
{
    /// <summary>
    /// тестирвоание гридов
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-11-10</released>
    /// <changed>2023-11-10</changed>
    public class GridBox4Interface
    {
        public GridBox4Interface()
        {
            {
                Central.WM.AddTab("gridbox4", "Тестирование гридов");

                Central.WM.AddTab<DepartmentTab4>("gridbox4");
                Central.WM.AddTab<AccountTab4>("gridbox4");
                Central.WM.AddTab<EmailTab4>("gridbox4");                    

                Central.WM.SetActive("DepartmentTab4");   
            }
        }
    }
}
