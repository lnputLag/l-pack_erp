using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using static Client.Interfaces.Main.DataGridHelperColumn;
using static DevExpress.Data.Filtering.Helpers.SubExprHelper.ThreadHoppingFiltering;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Форма выбора полумуфты для постановки в ячейку хранения
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class CuttingStampKeepingSelectFreeStamp : ControlBase
    {
        public CuttingStampKeepingSelectFreeStamp()
        {
            InitializeComponent();

            InitGrid();

            FrameName = "";
        }

        /// <summary>
        /// Идентификатор типа станка
        /// </summary>
        public int MachineId { get; set; }
        /// <summary>
        /// Идентификатор выбранной ячейки
        /// </summary>
        public int CellId { get; set; }
        /// <summary>
        /// имя вкладки (фрейма)
        /// </summary>
        private string FrameName;
        /// <summary>
        /// Имя вкладки, которая вызвала открытие фрейма, и в которую возвращается фокус после закрытия фрейма
        /// </summary>
        public string ReceiverName { get; set; }

        /// <summary>
        /// Обработка команд
        /// </summary>
        /// <param name="command"></param>
        private void ProcessCommand(string command)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "save":
                        Save();
                        break;
                    case "close":
                        Close();
                        break;
                }
            }
        }

        /// <summary>
        /// Иницифлизация таблицы
        /// </summary>
        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Название полумуфты",
                    Path="STAMP_ITEM_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=26,
                },
                new DataGridHelperColumn
                {
                    Header="Статус",
                    Path="STATUS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=14,
                },
                new DataGridHelperColumn
                {
                    Header="Название штанцформы",
                    Path="STAMP_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=26,
                },
            };
            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("ID");
            Grid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.AutoUpdateInterval = 0;
            Grid.SearchText = GridSearch;

            Grid.OnLoadItems = LoadItems;
            Grid.OnDblClick = selectedItem =>
            {
                Save();
            };

            // Раскраска строк
            Grid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                // Цвета фона строк
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        if (row.CheckGet("BUISY_QTY").ToInt() > 0)
                        {
                            color = HColor.Blue;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };

            Grid.Init();
        }

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        private async void LoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "CuttingStamp");
            q.Request.SetParam("Action", "ListItemCellFree");
            q.Request.SetParam("MACHINE_ID", MachineId.ToString());

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "LIST_ITEMS");
                    Grid.UpdateItems(ds);
                }
            }
        }

        /// <summary>
        /// Отображение вкладки со списком техкарт
        /// </summary>
        public void Show()
        {
            string title = $"Выбор полумуфты";
            FrameName = $"{ControlName}_{CellId}";

            Central.WM.Show(FrameName, title, true, "add", this);
        }

        public void Close()
        {
            Central.WM.Close(FrameName);
        }

        /// <summary>
        /// Сохранение
        /// </summary>
        public void Save()
        {
            if (Grid.SelectedItem != null)
            {
                int itemId = Grid.SelectedItem.CheckGet("ID").ToInt();

                if (itemId > 0)
                {
                    var d = new Dictionary<string, string>
                    {
                        { "ID", itemId.ToString() },
                        { "CELL_ID", CellId.ToString() },
                    };

                    SaveData(d);
                }
            }
        }

        /// <summary>
        /// Сохранение данных в БД
        /// </summary>
        /// <param name="data"></param>
        private async void SaveData(Dictionary<string, string> data)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "CuttingStamp");
            q.Request.SetParam("Action", "SaveCell");
            q.Request.SetParams(data);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    if (result.ContainsKey("ITEM"))
                    {
                        // Отправляем сообщение гриду о необходимости обновить таблицу
                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverGroup = "RigCuttingStamp",
                            ReceiverName = ReceiverName,
                            SenderName = "SetCell",
                            Action = "RefreshStamp",
                        });
                        Close();
                    }
                }
            }
        }

        /// <summary>
        /// Обработка нажатия на кнопку
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonOnClick(object sender, RoutedEventArgs e)
        {
            var b = (Button)sender;
            if (b != null)
            {
                var t = b.Tag.ToString();
                ProcessCommand(t);
            }
        }
    }
}
