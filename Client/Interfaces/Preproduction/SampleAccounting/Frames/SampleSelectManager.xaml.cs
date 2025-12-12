using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Фрейм выбора нескольких менеджеров для использования в фильтрации списков образцов
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleSelectManager : UserControl
    {
        public SampleSelectManager()
        {
            InitializeComponent();

            InitGrid();
        }

        /// <summary>
        /// Вкладка, откуда вызвали фрейм, и куда будет возвращаться фокус
        /// </summary>
        public string ReceiverName;

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header=" ",
                    Path="CHECKING",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=40,
                    MaxWidth=40,
                    Editable=true,
                },
                new DataGridHelperColumn
                {
                    Header="№",
                    Path="_ROWNUMBER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=50,
                },
                new DataGridHelperColumn
                {
                    Header="Фамилия",
                    Path="FIO",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=250,
                },
            };
            Grid.SetColumns(columns);
            Grid.AutoUpdateInterval = 0;
            Grid.Init();
        }

        private void Destroy()
        {
            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            Grid.Destruct();

            if (!string.IsNullOrEmpty(ReceiverName))
            {
                Central.WM.SetActive(ReceiverName, true);
                ReceiverName = "";
            }

        }

        /// <summary>
        /// Получение данных для таблицы
        /// </summary>
        private async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "ListRef");

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    // менеджеры по работе с клиентами и продажам или пользователи определенной группы, куда входит авторизованный пользователь
                    string managerKey = "MANAGERS";
                    if (result.ContainsKey("USER_GROUP"))
                    {
                        managerKey = "USER_GROUP";
                    }
                    var ds = ListDataSet.Create(result, managerKey);
                    var managersDS = ProcessItems(ds);
                    Grid.UpdateItems(managersDS);
                }
            }
        }

        /// <summary>
        /// Обработка полученных строк
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        private ListDataSet ProcessItems(ListDataSet ds)
        {
            var _ds = ds;
            if (ds.Items != null)
            {
                if (ds.Items.Count > 0)
                {
                    var ids = Central.SessionValues["ManagersConfig"]["ListActive"];
                    var l = new List<int>();

                    if (!string.IsNullOrEmpty(ids))
                    {
                        var arr = ids.Split(',');
                        foreach (var item in arr)
                        {
                            l.Add(item.ToInt());
                        }
                    }
                    var arrayIds = l.ToArray();

                    foreach(var row in _ds.Items)
                    {
                        var rowId = row.CheckGet("ID").ToInt();
                        if (rowId.ContainsIn(arrayIds))
                        {
                            row.CheckAdd("CHECKING", "1");
                        }
                        else
                        {
                            row.CheckAdd("CHECKING", "0");
                        }
                    }
                }
            }    

            return _ds;
        }

        /// <summary>
        /// Отображение вкладки
        /// </summary>
        public void Show()
        {
            string title = "Выбор менеджеров";
            Central.WM.AddTab($"SelectManager", title, true, "add", this);
            GetData();
        }

        public void Close()
        {
            Central.WM.RemoveTab($"SelectManager");
            Destroy();
        }

        /// <summary>
        /// Сохранение выбора в параметрах сессии
        /// </summary>
        public void Save()
        {
            var l = new List<int>();
            foreach (var item in Grid.GridItems)
            {
                if (item.CheckGet("CHECKING").ToBool())
                {
                    l.Add(item["ID"].ToInt());
                }
            }

            string ids = "";
            if (l.Count > 0)
            {
                ids = string.Join(",", l);
            }
            Central.SessionValues["ManagersConfig"]["ListActive"] = ids;

            // Отправляем сообщение гриду
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "PreproductionSample",
                ReceiverName = ReceiverName,
                SenderName = "SelectManager",
                Action = "Refresh",
            });

            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
