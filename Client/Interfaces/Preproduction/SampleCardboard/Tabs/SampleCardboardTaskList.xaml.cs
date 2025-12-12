using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Таблица ПЗ на заготовки картона для образцов
    /// </summary>
    /// <author>Рясной П.В.</author>
    public partial class SampleCardboardTaskList : UserControl
    {
        /// <summary>
        /// Инициаизация вкладки с таблицей ПЗ на заготовки картона для образцов
        /// </summary>
        public SampleCardboardTaskList()
        {
            InitializeComponent();
            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/samples_new/carton_samples";

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            SetDefaults();
            InitGrid();
            ProcessPermissions();
        }

        public string RoleName = "[erp]sample_cardboard";

        /// <summary>
        /// данные для таблицы
        /// </summary>
        public ListDataSet SampleCardboardTaskDS { get; set; }

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// Право на выполнение специальных действий
        /// </summary>
        public bool MasterRights;
        /// <summary>
        /// Имя вкладки
        /// </summary>
        public string TabName;

        public string DocumentationUrl;

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Если пользователь имеет спецправа, включаем режим мастера
            var mode = Central.Navigator.GetRoleLevel(this.RoleName);
            switch (mode)
            {
                case Role.AccessMode.Special:
                    MasterRights = true;
                    break;

                default:
                    MasterRights = false;
                    break;
            }

            List<Button> buttons = UIUtil.GetVisualChilds<Button>(this.Content as DependencyObject);
            if (buttons != null && buttons.Count > 0)
            {
                foreach (var button in buttons)
                {
                    var buttonTagList = UIUtil.GetTagList(button);
                    var accessMode = Acl.FindTagAccessMode(buttonTagList);
                    if (accessMode > mode)
                    {
                        button.IsEnabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// обработчик системы навигации по URL
        /// </summary>
        public void ProcessNavigation()
        {

        }

        /// <summary>
        /// Обработка и выполнение команд
        /// </summary>
        /// <param name="command"></param>
        public void ProcessCommand(string command, ItemMessage m = null)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "refresh":
                        Grid.LoadItems();
                        break;
                    case "edit":
                        EditTaskLength();
                        break;
                    case "delete":
                        DeleteTask();
                        break;
                    case "print":
                        PrintGrid();
                        break;
                    case "help":
                        Central.ShowHelp(DocumentationUrl);
                        break;
                }
            }
        }

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        public void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "Ид ПЗ",
                    Path = "ID_PZ",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 60,
                    MaxWidth = 70,
                },
                new DataGridHelperColumn
                {
                    Header="Дата",
                    Path="DATA",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=70,
                },
                new DataGridHelperColumn
                {
                    Header="Номер ПЗ",
                    Path="PZ_NUM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=80,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="Выполнено",
                    Path="POSTING",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=20,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="В плане",
                    Path="WORK",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=20,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество",
                    Path = "KOL",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=60,
                },
                new DataGridHelperColumn
                {
                    Header="Время начала",
                    Path="DTBEGIN",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=120,
                },
                new DataGridHelperColumn
                {
                    Header="Время окончания",
                    Path="DTEND",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=120,
                },
                 new DataGridHelperColumn
                {
                    Header="Образец",
                    Path="NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=100,
                    MaxWidth=200,
                },
                new DataGridHelperColumn
                {
                    Header = "Номер картона",
                    Path = "NUM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=60,
                },
                 new DataGridHelperColumn
                {
                    Header="Картон",
                    Path="NAME_CARTON",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=200,
                },
                new DataGridHelperColumn
                {
                    Header="Сотрудник",
                    Path="FIO1",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header=" ",
                    Path="_",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=5,
                    MaxWidth=1500,
                },
                new DataGridHelperColumn
                {
                    Header="В плане есть такой же картон",
                    Path="IN_PLAN",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="В очереди",
                    Path="PZ_LINE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Повторы",
                    Path="SAME_QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ИД прихода",
                    Path="IDP",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
           };

            Grid.SetColumns(columns);

            // Раскраска строк
            Grid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";
                        // Выполнены
                        if (row["POSTING"].ToInt() == 1)
                        {
                            if (row["IDP"].ToInt() > 0)
                            {
                                color=HColor.Green;
                            }
                            else
                            {
                                color=HColor.Yellow;
                            }
                        }
                        // В очереди на ГА
                        else if (row["PZ_LINE"].ToInt() == 1)
                        {
                            color=HColor.Yellow;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }
                        return result;
                    }
                },
                {
                    DataGridHelperColumn.StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";
                        // Подсвечиваем только невыполненные задания
                        if (row["POSTING"].ToInt() == 0)
                        {
                            // Задание на образцы не в очереди, но в очереди ГА есть задания с таким же картоном
                            // Надо сообщить, чтобы задание на образец включили в план
                            if ((row["PZ_LINE"].ToInt() == 0) && (row["IN_PLAN"].ToInt() == 1))
                            {
                                color=HColor.BlueFG;
                            }
                            // Есть дубли задания с одинаковым картоном
                            if (row["SAME_QTY"].ToInt() > 1)
                            {
                                color=HColor.MagentaFG;
                            }
                        }
                        else
                        {
                            // Неотсканированные завершенные задания
                            if (row["IDP"].ToInt() == 0)
                            {
                                color=HColor.OliveFG;
                            }
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }
                        return result;
                    }
                }
            };

            Grid.Init();
            //данные грида
            Grid.OnLoadItems = LoadItems;
            Grid.Run();

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateActions(selectedItem);
                }
            };

            //фокус ввода           
            Grid.Focus();
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            FromDate.Text = DateTime.Now.AddDays(-3).ToString("dd.MM.yyyy");
            ToDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
        }

        /// <summary>
        /// Обработчики нажатий клавиш
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessKeyboard2()
        {
             var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.F5:
                    Grid.LoadItems();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ProcessCommand("help");
                    e.Handled = true;
                    break;

                case Key.Home:
                    Grid.SetSelectToFirstRow();
                    e.Handled = true;
                    break;

                case Key.End:
                    Grid.SetSelectToLastRow();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// Обработчик сообщений
        /// </summary>
        /// <param name="m">сообщение</param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.IndexOf("Preproduction") > -1)
            {
                if (m.ReceiverName.IndexOf("SampleCardboardTask") > -1)
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            Grid.LoadItems();
                            break;
                        case "EditTaskLength":
                            if (m.ContextObject != null)
                            {
                                var v = (Dictionary<string, string>)m.ContextObject;
                                SaveTaskLength(v);
                            }
                            break;
                    }

                }
                // Если создано новое задание, обновляем таблицу
                if (m.SenderName.IndexOf("SampleCardboardCreateTask") > -1)
                {
                    if (m.Action == "TaskCreated")
                    {
                        Grid.LoadItems();
                    }
                }
            }
        }

        /// <summary>
        /// Деструктор компонента
        /// </summary>
        public void Destroy()
        {
            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры гридов
            Grid.Destruct();
        }

        /// <summary>
        /// Загрузка данных из БД
        /// </summary>
        public async void LoadItems()
        {
            GridToolbar.IsEnabled = false;
            Grid.ShowSplash();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "SampleCardboards");
            q.Request.SetParam("Action", "TaskList");
            q.Request.SetParam("DATE_FROM", FromDate.Text);
            q.Request.SetParam("DATE_TO", ToDate.Text);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    SampleCardboardTaskDS = ListDataSet.Create(result, "TaskList");
                    Grid.UpdateItems(SampleCardboardTaskDS);
                }
                RefreshButton.Style = (Style)RefreshButton.TryFindResource("Button");
            }

            GridToolbar.IsEnabled = true;
            Grid.HideSplash();
        }

        /// <summary>
        /// обновление методов работы с выбранной записью
        /// </summary>
        /// <param name="selectedItem"></param>
        public void UpdateActions(Dictionary<string, string> selectedItem)
        {
            SelectedItem = selectedItem;

            // ПЗГА можно редактировать, если оно не выполнено и не в очереди ГА
            bool editingEnable = false;
            if (SelectedItem.ContainsKey("POSTING") && SelectedItem.ContainsKey("PZ_LINE"))
            {
                editingEnable = (SelectedItem["POSTING"].ToInt() == 0) && (SelectedItem["PZ_LINE"].ToInt() == 0);
            }

            EditButton.IsEnabled = editingEnable;
            // Кнопка доступна если есть спецправа и ПЗ можно редактировать
            DeleteButton.IsEnabled = MasterRights && editingEnable;

            ProcessPermissions();
        }

        /// <summary>
        /// Удаление ПЗ на образцы
        /// </summary>
        public async void DeleteTask()
        {
            if (SelectedItem != null)
            {
                var id = SelectedItem.CheckGet("ID_PZ").ToInt();
                if (id > 0)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Production");
                    q.Request.SetParam("Object", "ProductionTask");
                    q.Request.SetParam("Action", "Delete");
                    q.Request.SetParam("ID", id.ToString());

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
                            if (result.ContainsKey("ITEMS"))
                            {
                                Grid.LoadItems();
                            }
                        }
                    }
                    else if (q.Answer.Error.Code == 145)
                    {
                        var dw = new DialogWindow(q.Answer.Error.Message, "Удаление ПЗ");
                        dw.ShowDialog();
                    }
                }
            }
        }

        /// <summary>
        /// При сммене дат меняет стиль кнопки обновления данных
        /// </summary>
        private void DateChanged()
        {
            RefreshButton.Style = (Style)RefreshButton.TryFindResource("FButtonPrimary");
        }

        /// <summary>
        /// Вызов окна редактирования количества листов в задании
        /// </summary>
        private void EditTaskLength()
        {
            if (SelectedItem != null)
            {
                int taskId = SelectedItem.CheckGet("ID_PZ").ToInt();
                if (taskId > 0) {
                    var d = new Dictionary<string, string>()
                    {
                        { "TASK_ID", taskId.ToString() },
                        { "SHEET_QUANTITY", SelectedItem.CheckGet("KOL") },
                    };
                    var editTaskWin = new SampleCardboardTaskLength();
                    editTaskWin.ReceiverName = "SampleCardboardTask";
                    editTaskWin.Edit(d);
                };
            }
        }

        /// <summary>
        /// Сохраняем задание с новым количеством листов
        /// </summary>
        /// <param name="data"></param>
        private async void SaveTaskLength(Dictionary<string, string> data)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "SampleCardboards");
            q.Request.SetParam("Action", "SaveTaskLength");
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
                        Grid.LoadItems();
                    }
                }
            }
            else if (q.Answer.Error.Code == 145)
            {
                var dw = new DialogWindow(q.Answer.Error.Message, "Изменение ПЗ");
                dw.ShowDialog();
            }
        }

        /// <summary>
        /// Обработчик нажатия на кнопку выгрузки в Excel
        /// </summary>
        private async void PrintGrid()
        {
            var eg = new ExcelGrid();
            eg.Columns = new List<ExcelGridColumn>(){
                {new ExcelGridColumn("DATA", "Дата", 50, ExcelGridColumn.ColumnTypeRef.String) },
                {new ExcelGridColumn("PZ_NUM", "Номер ПЗ", 50, ExcelGridColumn.ColumnTypeRef.String) },
                {new ExcelGridColumn("KOL", "Количество", 30, ExcelGridColumn.ColumnTypeRef.Integer) },
                {new ExcelGridColumn("NAME", "Образец", 120, ExcelGridColumn.ColumnTypeRef.String) },
                {new ExcelGridColumn("NUM", "Номер", 30, ExcelGridColumn.ColumnTypeRef.Integer) },
                {new ExcelGridColumn("NAME_CARTON", "Картон", 120, ExcelGridColumn.ColumnTypeRef.String) },
            };
            eg.Items = Grid.GridItems;
            await Task.Run(() =>
            {
                eg.Make();
            });
        }

        /// <summary>
        /// Обработчик нажатия на кнопку
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonOnClick(object sender, RoutedEventArgs e)
        {
            var b = (Button)sender;
            if (b != null)
            {
                var buttonTagList = UIUtil.GetTagList(b);
                foreach (var tag in buttonTagList)
                {
                    ProcessCommand(tag);
                }
            }
        }

        private void FromDate_TextChanged(object sender, TextChangedEventArgs e)
        {
            DateChanged();
        }

        private void ToDate_TextChanged(object sender, TextChangedEventArgs e)
        {
            DateChanged();
        }
    }
}
