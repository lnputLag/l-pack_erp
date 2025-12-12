using System.Collections.Generic;

namespace Client.Common
{
    /// <summary>
    /// структура "Пользователь"
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    public class User 
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public string Surname { get; set; }
        public string Name { get; set; }
        public string MiddleName { get; set; }
        public string Email { get; set; }
        public int? DepartmentId { get; set; }
        public string InnerPhone { get; set; }
        public string MobilePhone { get; set; }
        public string Password { get; set; }
        public string Description { get; set; }
        public string HostUserId { get; set; }
        /// <summary>
        /// id пользователя
        /// уникальный идентификатор, сквозной через все системы
        /// </summary>
        public int AccountId { get; set; }
        public int EmployeeId { get; set; }

        /// <summary>
        /// Роли пользователя
        /// </summary>
        public Dictionary<string, Role> Roles { get; set; }

        /// <summary>
        /// Список пользовательских настроек 
        /// </summary>
        public List<UserParameter> UserParameterList { get; set; }

    }
}
