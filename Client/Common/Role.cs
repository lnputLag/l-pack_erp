using System.Collections.Generic;
namespace Client.Common
{
    /// <summary>
    /// структура "роль пользователя"
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2019-07-31</released>
    /// <changed>2022-11-24</changed>
    public class Role
    {
        public Role()
        {
            Id = 0;
            Name = "";
            Description = "";
            Code = "";
            Mode=0;
            Source=0;
        }    
    
        /// <summary>
        /// идентификатор роли
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// название роли
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// примечание
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// символьный идентификатор роли
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// уровень доступа:
        /// 0=запрещен,
        /// 1=только чтение,
        /// 2=полный доступ,
        /// </summary>
        public int Mode { get; set; }
        /// <summary>
        /// код источника:
        /// 1=модель 1: account-employee - role
        /// 2=модель 2: account-role
        /// 3=модель 3: account-employee-work_group-role
        /// </summary>
        public int Source { get; set; }
        /// <summary>
        /// уровни доступа для роли пользователя
        /// </summary>
        public enum AccessMode
        {
            None = -1,
            Deny = 0,
            ReadOnly = 1,
            FullAccess = 2,
            Special = 3,
        }
    }
}
