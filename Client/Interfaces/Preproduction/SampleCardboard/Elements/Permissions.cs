using Client.Common;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// 
    /// </summary>
    /// <author>Рясной П.В.</author>
    public partial class SampleCardboardList : UserControl
    {
        /// <summary>
        /// Статический метод проверки прав на выполнение операции
        /// </summary>
        /// <param name="action">операция</param>
        /// <returns></returns>
        public static bool HasPermission(string action = "read")
        {
            bool result = false;

            // Ключ - название операции, значение - список ролей, которым эта операция доступна
            Dictionary<string, List<string>> Permissions = new Dictionary<string, List<string>>
            {
                {
                    "read", new List<string>() {
                        "[f]admin",
                        "[p]programmer"
                    }
                },
                {
                    "change", new List<string>() {
                        "[f]admin",
                        "[p]programmer"
                    }
                }
            };

            var rolesList = new List<string>();
            if (Permissions.ContainsKey(action))
            {
                rolesList = Permissions[action];
            }

            if ((rolesList.Count > 0) && (Central.User.Roles.Count > 0))
            {
                if (rolesList[0] == "*")
                {
                    result = true;
                }
                else
                {
                    foreach (KeyValuePair<string, Role> ur in Central.User.Roles)
                    {
                        string userRole = ur.Value.Code;
                        userRole = userRole.Trim();
                        userRole = userRole.ToLower();
                        if (rolesList.Contains(userRole))
                        {
                            result = true;
                            break;
                        }
                    }
                }
            }

            return result;
        }
    }
}
