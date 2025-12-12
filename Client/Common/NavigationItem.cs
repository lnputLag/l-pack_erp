using NLog.LayoutRenderers;
using Prism.Commands;
using System.Collections.Generic;

namespace Client.Common
{
    /// <summary>
    /// Структура одного элемента дерева навигации
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    public class NavigationItem
    {
        public NavigationItem()
        {
            Id=0;
            Name    = "";
            Title   = "";
            Type    = "element";
            Address = "";
            Align   = "Left";
                        
            SubItems=new List<NavigationItem>();
            Action=new DelegateCommand<string>(
                action =>
                {        
                }
            );

            AllowedRoles= new List<string>();
            AllowedUsers=new List<int>();
            AllowedLogins=new List<string>();

            MinWidth=50;
            Style="";
            Level=1;

            Visible = true;
        }

        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Имя элемента. Техническое имя, латиница, должно быть уникальным
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Подпись, будет показана в пункте меню
        /// </summary> 
        public string Title { get; set; }
        /// <summary>
        /// Тип элемента: раздел или конечный пункт
        /// <para>section -- раздел, содержит другие разделы (у раздела нет обработчика клика)</para>
        /// <para>element -- конечный элемент структуры, по клику быдет вызван обработчик</para>
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// Адрес элемента, абсолютный адрес, будет сформирован автоматически
        /// Содержит имена всех предков.
        /// </summary>
        public string Address { get; set; }        
        /// <summary>
        /// Если это раздел, то это список вложенных элементов
        /// </summary>
        public List<NavigationItem> SubItems { get; set; }
        /// <summary>
        /// Обработчик клика, будет вызван при клике мышью (если это конечный элемент)
        /// </summary>
        public DelegateCommand<string> Action { get; set; }
        /// <summary>
        /// Список разрешенных ролей. Элемент будет доступен текущему пользователю, если
        /// у пользователя есть хотя бы одна из перечисленных ролей.
        /// Если указано "*" или "all", элемент будет доступен всем.
        /// </summary>
        public List<string> AllowedRoles { get; set; }
        /// <summary>
        /// Список разрешенных пользователей.
        /// Если AccountId пользователя есть в этом списке, элемент будет доступен текущему пользователю.
        /// </summary>
        public List<int> AllowedUsers { get; set; }
        /// <summary>
        /// список разрешенных пользователей
        /// Если Login пользователя есть в этом списке, элемент будет доступен текущему пользователю.
        /// </summary>
        public List<string> AllowedLogins { get; set; }
        /// <summary>
        /// Выравнивание в системе меню ("Left", "Right")
        /// </summary>
        public string Align { get; set; }
        /// <summary>
        /// Ширина пункта
        /// </summary>
        public int MinWidth { get;set;}
        /// <summary>
        /// стиль отображения элемента
        /// </summary>
        public string Style { get;set;}
        /// <summary>
        /// уровень, исчисляется с 1
        /// </summary>
        public int Level { get;set;}
        /// <summary>
        /// Видимость в главном меню
        /// </summary>
        public bool Visible { get; set; }
        public Dictionary<string,string> GetDict()
        {
            var allowedRoles="";
            foreach(string role in AllowedRoles)
            {
                allowedRoles=allowedRoles.AddComma();
                allowedRoles=allowedRoles.Append(role);
            }
        
            var result=new Dictionary<string,string>() 
            { 
                {"ID", Id.ToString()},
                {"NAME", Name.ToString()},
                {"TITLE", Title.ToString()},
                {"TYPE", Type.ToString()},
                {"ALLOWED_ROLES", allowedRoles},
                {"ALLOWED_USERS", ""},
                {"ALLOWED_LOGINS", ""},
                {"LEVEL", Level.ToString()},
                {"ADDRESS", Address.ToString()},
            };

            return result;
        }

    }
}
