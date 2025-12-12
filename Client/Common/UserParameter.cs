using Client.Interfaces.Main;
using System.Collections.Generic;
using System.Linq;

namespace Client.Common
{
    /// <summary>
    /// Пользовательская настройка
    /// </summary>
    /// <author>sviridov_ae</author>
    public class UserParameter
    {
        public UserParameter(string name, string value, string description, string interfaceName, string login, string hostUserId, string primaryKey)
        {
            Name = name;
            Value = value;
            Description = description;
            Interface = interfaceName;
            Login = login.ToLower();
            HostUserId = hostUserId.ToLower();
            PrimaryKey = primaryKey;
        }

        public UserParameter(string interfaceName, string name, string value, string description = "")
        {
            Name = name;
            Value = value;
            Description = description;
            Interface = interfaceName;
            Login = Central.User.Login.ToLower();
            HostUserId = Central.GetSystemInfo().CheckGet("HOST_USER_ID").ToLower();
            PrimaryKey = $"{interfaceName}~{name}";
        }

        /// <summary>
        /// Наименование параметра пользовательской настройки
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Значение параметра пользовательской настройки
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Описание параметра
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Наименование интерфейса, для которого создана эта пользовательская настройка
        /// </summary>
        public string Interface { get; }

        /// <summary>
        /// Логин пользователя, к которому привязана эта пользовательская настрйка
        /// </summary>
        public string Login { get; }

        /// <summary>
        /// Наименование машины пользователя, к которой привязана эта пользовательская настрйка
        /// </summary>
        public string HostUserId { get; }

        /// <summary>
        /// Значение уникального ключа в файле с этой пользовательской настройкой.
        /// Interface~Name
        /// </summary>
        public string PrimaryKey { get; }

        /// <summary>
        /// Устанавливет порядок колонок 4 грида в соответствии с пользовательскими настройками
        /// </summary>
        public static void SortGridBox4ColumnsByUserParameterList(GridBox4 grid, object tab, string userParameterName)
        {
            if (grid != null && grid.GridControl.Columns != null && grid.GridControl.Columns.Count > 0)
            {
                var positionsGridColumnPositionListParameter = Central.User.UserParameterList.FirstOrDefault(x => x.Interface == tab.GetType().Name && x.Name == userParameterName);
                if (positionsGridColumnPositionListParameter != null)
                {
                    string columnPositionData = positionsGridColumnPositionListParameter.Value;
                    var columnPositionDataItems = columnPositionData.Split(';');
                    if (columnPositionDataItems != null && columnPositionDataItems.Length > 0)
                    {
                        foreach (var columnPosition in columnPositionDataItems)
                        {
                            var positionData = columnPosition.Split(':');
                            if (positionData != null && positionData.Length == 2)
                            {
                                var column = grid.GridControl.Columns.FirstOrDefault(x => x.Name == positionData[0]);
                                if (column != null)
                                {
                                    column.VisibleIndex = positionData[1].ToInt();
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void SaveGridBox4ColumnsByUserParameterList(DevExpress.Xpf.Grid.GridColumnCollection columns, object tab, string userParameterName, string userParameterDescription = "")
        {
            var columnList = columns.OrderBy(x => x.VisibleIndex).ToList();
            string columnPositionData = "";
            foreach (var column in columnList)
            {
                columnPositionData = $"{columnPositionData}{column.Name}:{column.VisibleIndex};";
            }

            var positionsGridColumnPositionListParameter = Central.User.UserParameterList.FirstOrDefault(x => x.Interface == tab.GetType().Name && x.Name == userParameterName);
            if (positionsGridColumnPositionListParameter != null)
            {
                positionsGridColumnPositionListParameter.Value = columnPositionData;
            }
            else
            {
                positionsGridColumnPositionListParameter = new UserParameter(tab.GetType().Name, userParameterName, columnPositionData, userParameterDescription);
                Central.User.UserParameterList.Add(positionsGridColumnPositionListParameter);
            }
        }
    }
}
