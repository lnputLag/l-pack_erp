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
    /// Сптсок сотрудников для выбора получателей во внутреннем чате по образцам
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleChatRecipient : UserControl
    {
        public SampleChatRecipient()
        {
            InitializeComponent();

            TabName = "SampleChatRecipient";
            InitGrid();
        }

        /// <summary>
        /// Имя вкладки
        /// </summary>
        public string TabName { get; set; }

        /// <summary>
        /// Имя вкладки, которая становится активной после закрытия формы редактирования
        /// </summary>
        public string ReturnTabName { get; set; }

        /// <summary>
        /// Объект, по которому ведется чат
        /// </summary>
        public string ChatObject;

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header=" ",
                    Path="CHECKING",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    Editable=true,
                    MinWidth=40,
                    MaxWidth=40,
                    OnClickAction = (row, el) =>
                    {
                        CheckRow(row);

                        return null;
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Сотрудник",
                    Path="FULL_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=200,
                },
                new DataGridHelperColumn
                {
                    Header="ID",
                    Path="ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Является сотрудником",
                    Path="IS_EMPLOYEE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ID группы",
                    Path="WORK_GROUP_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            Grid.SetColumns(columns);
            Grid.PrimaryKey = "_ROWNUMBER";

            Grid.Init();
        }

        /// <summary>
        /// Получение списка для таблицы
        /// </summary>
        private async void GetData()
        {
            string actionObjectName = $"List{ChatObject}Recipient";

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Messages");
            q.Request.SetParam("Object", "ChatMessage");
            q.Request.SetParam("Action", actionObjectName);

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "RECIPIENTS");
                    Grid.UpdateItems(ds);
                }
            }
        }

        /// <summary>
        /// Отметка строки
        /// </summary>
        /// <param name="row"></param>
        private void CheckRow(Dictionary<string, string> row)
        {
            FormStatus.Text = "";
            // Если выбрана группа, ставим отметки у всех членов группы
            string _checking = row.CheckGet("CHECKING");
            if (row.CheckGet("IS_EMPLOYEE").ToInt() == 0)
            {
                int groupId = row.CheckGet("ID").ToInt();
                foreach(var item in Grid.GridItems)
                {
                    if (item.CheckGet("WORK_GROUP_ID").ToInt() == groupId)
                    {
                        item.CheckAdd("CHECKING", _checking);
                        item.CheckAdd("_SELECTED", _checking);
                    }
                }
            }
            else
            {
                row.CheckAdd("_SELECTED", _checking);
            }

            Grid.UpdateItems();
        }

        /// <summary>
        /// Отображение вкладки
        /// </summary>
        public void Show()
        {
            GetData();
            Central.WM.AddTab(TabName, "Выбор получателей", true, "add", this);
        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        public void Close()
        {
            Central.WM.RemoveTab(TabName);

            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "ChatMessage",
                ReceiverName = ReturnTabName,
                SenderName = TabName,
                Action = "Closed",
            });
            Central.Msg.SendMessage(new ItemMessage()
            {
                ReceiverGroup = "ChatMessage",
                ReceiverName = ReturnTabName,
                SenderName = TabName,
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            if (!string.IsNullOrEmpty(ReturnTabName))
            {
                Central.WM.SetActive(ReturnTabName, true);
                ReturnTabName = "";
            }
        }

        /// <summary>
        /// Обработка нажатия на кнопку выбора
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = Grid.GetSelectedItems();
            if (selectedItems.Count > 0)
            {
                var d = new Dictionary<string, string>() { { "FULL_NAMES", "" }, { "ID_LIST", "" } };
                foreach (var row in selectedItems)
                {
                    if (row.CheckGet("IS_EMPLOYEE").ToInt() == 1)
                    {
                        if (string.IsNullOrEmpty(d["FULL_NAMES"]))
                        {
                            d["FULL_NAMES"] = row.CheckGet("FULL_NAME").Trim();
                            d["ID_LIST"] = row.CheckGet("ID");
                        }
                        else
                        {
                            d["FULL_NAMES"] = $"{d["FULL_NAMES"]}, {row.CheckGet("FULL_NAME").Trim()}";
                            d["ID_LIST"] = $"{d["ID_LIST"]},{row.CheckGet("ID")}";
                        }
                    }
                }

                Central.Msg.SendMessage(new ItemMessage() {
                    ReceiverGroup = "ChatMessage",
                    ReceiverName = ReturnTabName,
                    SenderName = TabName,
                    Action = "SetRecipients",
                    ContextObject = d,
                });
                Close();
            }
            else
            {
                FormStatus.Text = "Нужен хотя бы один получатель";
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
