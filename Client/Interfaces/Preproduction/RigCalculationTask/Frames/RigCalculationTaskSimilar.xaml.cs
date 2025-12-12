using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Поиск подобных изделий или расчетов для выбранного задания на расчет оснастки
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class RigCalculationTaskSimilar : ControlBase
    {
        public RigCalculationTaskSimilar()
        {
            InitializeComponent();

            Deviation.Text = "5";
            InitGrid();

            OnLoad = () =>
            {
            };

            OnFocusGot = () =>
            {
                Grid.ItemsAutoUpdate = true;
                Grid.Run();
            };

            OnFocusLost = () =>
            {
                Grid.ItemsAutoUpdate = false;
            };

        }

        /// <summary>
        /// Параметры изделия, для котрого делается расчет
        /// </summary>
        public Dictionary<string, string> TaskValues { get; set; }

        /// <summary>
        /// Имя вкладки, откуда вызвана форма и куда передается ответ
        /// </summary>
        public string ReceiverName;

        /// <summary>
        /// Обработка и выполнение команд
        /// </summary>
        /// <param name="command"></param>
        public void ProcessCommand(string command)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "refresh":
                        Grid.LoadItems();
                        break;
                    case "show":
                        ShowFinding();
                        break;
                    case "close":
                        Close();
                        break;
                }
            }
        }

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="№",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=3,
                },
                new DataGridHelperColumn
                {
                    Header="Тип",
                    Path="TYPE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Название изделия",
                    Path="PRODUCT_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Габариты развертки",
                    Path="PROJECTION",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="FEFCO",
                    Path="FEFCO",
                    ColumnType=ColumnTypeRef.String,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Штанцформа",
                    Path="STAMP",
                    ColumnType=ColumnTypeRef.String,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="UNION_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Код типа",
                    Path="TYPE",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Путь у техкарте/чертежу",
                    Path="PATHTK",
                    ColumnType=ColumnTypeRef.String,
                    Hidden=true,
                },
            };
            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("_ROWNUMBER");
            Grid.SetSorting("_ROWNUMBER", ListSortDirection.Descending);
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            Grid.AutoUpdateInterval = 0;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateActions(selectedItem);
                }
            };

            //данные грида
            Grid.OnLoadItems = LoadItems;
            //Grid.OnFilterItems = FilterItems;
            Grid.Init();
        }

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        private async void LoadItems()
        {
            TaskValues.CheckAdd("DEVIATION", Deviation.Text);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "RigCalculationTask");
            q.Request.SetParam("Action", "ListSimilar");
            q.Request.SetParams(TaskValues);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            }
            );

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "RIG_TASK_SIMILAR");
                    Grid.UpdateItems(ds);
                }
            }
        }

        public void UpdateActions(Dictionary<string, string> row)
        {

        }

        /// <summary>
        /// Отображение вкладки с формой
        /// </summary>
        public void Show()
        {
            var taskId = TaskValues.CheckGet("ID").ToInt();
            ControlName = $"RigCalcSimilar{taskId}";
            ControlTitle = $"Подобные задачи {taskId}";
            ProductName.Text = TaskValues.CheckGet("PRODUCT_NAME");

            Central.WM.AddTab(ControlName, ControlTitle, true, "add", this);
            Central.WM.SetActive(ControlName);
        }

        /// <summary>
        /// Показываем результат
        /// </summary>
        private void ShowFinding()
        {
            if (Grid.SelectedItem != null)
            {
                string pathFile = Grid.SelectedItem.CheckGet("PATHTK");
                if (Grid.SelectedItem.CheckGet("TYPE").ToInt() == 1)
                {
                    bool hasDrawing = false;
                    if (!pathFile.IsNullOrEmpty())
                    {
                        if (File.Exists(pathFile))
                        {
                            hasDrawing = true;
                        }
                    }

                    if (hasDrawing)
                    {
                        //Если приложен чертеж, открываем чертеж
                        Central.OpenFile(pathFile);
                    }
                    else
                    {
                        // Открываем предыдущую заявку
                        int rigTaskId = Grid.SelectedItem.CheckGet("UNION_ID").ToInt();
                        var rigTaskEditForm = new RigCalculationTask();
                        rigTaskEditForm.ReceiverName = ControlName;
                        // делаем все поля дступными только для просмотра
                        rigTaskEditForm.SetReadOnly("read-only");
                        rigTaskEditForm.Edit(rigTaskId);
                    }
                }
                else
                {
                    // остальным открываем техкарту или чертеж
                    if (!pathFile.IsNullOrEmpty())
                    {
                        if (File.Exists(pathFile))
                        {
                            Central.OpenFile(pathFile);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Закрытие вкладки с формой
        /// </summary>
        public void Close()
        {
            Central.WM.RemoveTab(ControlName);
            Central.WM.SetActive(ReceiverName);
        }

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
