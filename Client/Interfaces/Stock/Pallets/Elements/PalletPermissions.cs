using Client.Common;
using System.Collections.Generic;

namespace Client.Interfaces.Stock
{
    public static class PalletPermissions
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
                },
                {
                    // проведение, отмена проведения поддона
                    "set_record", new List<string>() {
                        "[f]admin",
                        "[p]programmer"
                    }
                },
                {
                    // изменение количества поддонов в накладной
                    "change_qty", new List<string>() {
                        "[f]admin",
                        "[p]programmer",
                        "[p]warehouse_stockman",
                        "[p]warehouse_chief",
                        "[p]warehouse_master"
                    }
                },
                {
                    // отметка проверки ОЭБ
                    "check_security", new List<string>()
                    {
                        "[p]programmer",
                        "[p]economic_security_chief",
                        "[p]economic_security_manager",
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
