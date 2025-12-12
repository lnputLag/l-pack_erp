using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
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

namespace Client.Interfaces.Production.ProcessingMachines
{
    /// <summary>
    /// Логика взаимодействия для ProductionTaskPrList.xaml
    /// Производственные задания на переработку ПР, общий список
    /// </summary>
    /// <author>sviridov_ae</author>
    public partial class ProductionTaskPrList : UserControl
    {
        public ProductionTaskPrList()
        {
            InitializeComponent();

            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            SetDefaults();
            LoadRef();

            InitGrid();

            ProcessPermissions();
        }

        private string ControlTitle = "ПЗ на переработку";

        public int FactoryId = 1;

        public string RoleName = "[erp]production_task_pr";

        public ListDataSet MachineDS { get; set; }

        Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// форма с полями для фильтрации данных
        /// </summary>
        public FormHelper Form { get; set; }

        public string TabName;

        private void InitGrid()
        {
            //инициализация формы
            {
                Form = new FormHelper();
                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="TODAY_FROM",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=Today_from,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="TODAY_TO",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=Today_to,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="SEARCH",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=SearchText,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="MACHINE",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=Machine,
                        ControlType="SelectBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="STATUSES",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Default="0",
                        Control=Statuses,
                        ControlType="SelectBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                };

                Form.SetFields(fields);
                //перед установкой значений
                Form.BeforeSet = (Dictionary<string, string> v) =>
                {

                };

                //после установки значений
                Form.AfterSet = (Dictionary<string, string> v) =>
                {
                    //фокус на кнопку обновления
                    ResreshButton.Focus();
                };

                {
                    Machine.OnSelectItem = (Dictionary<string, string> selectedItem) =>
                    {
                        bool result = true;
                        if (selectedItem.Count > 0)
                        {
                            int id = selectedItem.CheckGet("ID").ToInt();
                            Grid.UpdateItems();

                        }
                        return result;
                    };
                }

                {
                    Statuses.OnSelectItem = (Dictionary<string, string> selectedItem) =>
                    {
                        bool result = true;
                        if (selectedItem.Count > 0)
                        {
                            int id = selectedItem.CheckGet("ID").ToInt();
                            Grid.UpdateItems();

                        }
                        return result;
                    };
                }
            }

            //инициализация грида
            {
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                             Header="ИД ПЗ",
                             Path="ID_PZ",
                             ColumnType=ColumnTypeRef.Integer,
                             Width=55,
                    },
                    new DataGridHelperColumn
                    {
                             Header="Номер ПЗ",
                             Path="NUM",
                             ColumnType=ColumnTypeRef.String,
                             Width=75,
                    },
                    new DataGridHelperColumn
                    {
                             Header="Дата создания",
                             Path="CREATED",
                             ColumnType=ColumnTypeRef.DateTime,
                             Format="dd.MM.yyyy HH:mm",
                             Width=100,
                    },
                    new DataGridHelperColumn
                    {
                             Header="Дата завершения",
                             Path="DTEND",
                             ColumnType=ColumnTypeRef.DateTime,
                             Format="dd.MM.yyyy HH:mm",
                             MaxWidth=115,
                             Width=115,
                    },
                    new DataGridHelperColumn
                    {
                             Header="Артикул",
                             Path="TOVAR_ARTIKUL",
                             ColumnType=ColumnTypeRef.String,
                             MaxWidth=120,
                             Width=120,
                    },
                    new DataGridHelperColumn
                    {
                             Header="Наименование товара",
                             Path="TOVAR_NAME",
                             ColumnType=ColumnTypeRef.String,
                             Width=350,
                    },
                    new DataGridHelperColumn
                    {
                             Header="По заданию, шт.",
                             Path="KOL_PRIHOD",
                             ColumnType=ColumnTypeRef.Integer,
                             Width=54,
                    },
                    new DataGridHelperColumn
                    {
                             Header="Отсканировано, шт.",
                             Path="KOL_FACT",
                             ColumnType=ColumnTypeRef.Integer,
                             Width=55,
                    },
                    new DataGridHelperColumn
                    {
                             Header="Создано, шт.",
                             Path="QUANTITY_GOODS",
                             ColumnType=ColumnTypeRef.Integer,
                             Width=56,
                    },
                    new DataGridHelperColumn
                    {
                             Header="Заготовок, шт.",
                             Path="QUANTITY_BLANK",
                             ColumnType=ColumnTypeRef.Integer,
                             Width=54,
                    },
                    new DataGridHelperColumn
                    {
                             Header="Станок",
                             Path="MACHINES",
                             ColumnType=ColumnTypeRef.String,
                             Width=100,
                    },
                    new DataGridHelperColumn
                    {
                             Header="Создатель",
                             Path="CREATOR",
                             ColumnType=ColumnTypeRef.String,
                             Width=100,
                    },
                    new DataGridHelperColumn
                    {
                             Header="Выполнено",
                             Path="POSTING",
                             ColumnType=ColumnTypeRef.Boolean,
                             Width=80,
                    },
                    new DataGridHelperColumn
                    {
                             Header="Примечание",
                             Path="NOTE",
                             ColumnType=ColumnTypeRef.String,
                             Width=150,
                    },
                    new DataGridHelperColumn
                    {
                             Header="Заявка",
                             Path="APPLICATION",
                             ColumnType=ColumnTypeRef.String,
                             Width=280,
                    },
                    new DataGridHelperColumn
                    {
                             Header=$"ГА{Environment.NewLine}Количество заданий на ГА, шт.",
                             Path="QUANTITY_CORRUGATOR_TASK",
                             ColumnType=ColumnTypeRef.Integer,
                             Width=30,
                    },
                    new DataGridHelperColumn
                    {
                             Header="ИД позиции заявки",
                             Path="IDORDERDATES",
                             ColumnType=ColumnTypeRef.Integer,
                             Width=84,
                    },
                    new DataGridHelperColumn
                    {
                            Header=" ",
                            Path="_",
                            ColumnType=ColumnTypeRef.String,
                            MinWidth=5,
                            MaxWidth=900,
                    },
                    new DataGridHelperColumn
                    {
                             Header="Группа станков",
                             Path="MACHINE_GROUP",
                             ColumnType=ColumnTypeRef.Integer,
                             Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                             Header="Профиль",
                             Path="ID_PROF",
                             ColumnType=ColumnTypeRef.Integer,
                             Width=100,
                             Hidden=true,
                    },
                };

                //при выборе строки в гриде, обновляются актуальные действия для записи
                Grid.OnSelectItem = (Dictionary<string, string> selectedItem) =>
                {
                    if (selectedItem.Count > 0)
                    {
                        UpdateActions(selectedItem);
                    }
                };

                Grid.SetColumns(columns);


            }

            //Заливка строк грида цветом
            Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                {
                    StylerTypeRef.BackgroundColor,
                    (Dictionary<string, string> row) =>
                    {
                        var result = DependencyProperty.UnsetValue;
                        var color = "";

                        //зеленый -- задание выполнено
                        if (row.CheckGet("POSTING").ToInt() == 1)
                        {
                            color = HColor.Green;
                        }

                        if (row.CheckGet("QUANTITY_CORRUGATOR_TASK").ToInt() == 0)
                        {
                            color = HColor.Green;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result = color.ToBrush();
                        }

                        return result;
                    }
                },
            };

            // контекстное меню
            Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
            {
                { "edit_task_note", new DataGridContextMenuItem(){
                    Header="Изменить примечание",
                    Tag = "access_mode_full_access",
                    Action=()=>
                    {
                        EditTaskNote();
                    }
                }},
                { "s0", new DataGridContextMenuItem(){
                    Header="-",
                }},
                { "posting_task", new DataGridContextMenuItem(){
                    Header="Закрыть задание",
                    Tag = "access_mode_full_access",
                    Action=()=>
                    {
                        PostingProductionTask();
                    }
                }},
                { "delete_task", new DataGridContextMenuItem(){
                    Header="Удалить задание",
                    Tag = "access_mode_full_access",
                    Action=()=>
                    {
                        DeletePartitionalTask();
                    }
                }},
            };
            Grid.OnLoadItems = LoadItems;
            Grid.OnFilterItems = FilterItems;
            Grid.SetSorting("ID_PZ", ListSortDirection.Ascending);
            Grid.SearchText = SearchText;
            Grid.Init();
            Grid.Run();
        }


        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel(this.RoleName);
            var userAccessMode = mode;
            switch (mode)
            {
                case Role.AccessMode.Special:
                    break;

                case Role.AccessMode.FullAccess:
                    break;

                case Role.AccessMode.ReadOnly:
                default:
                    break;
            }

            List<Button> buttons = UIUtil.GetVisualChilds<Button>(this.Content as DependencyObject);
            if (buttons != null && buttons.Count > 0)
            {
                foreach (var button in buttons)
                {
                    var buttonTagList = UIUtil.GetTagList(button);
                    var accessMode = Acl.FindTagAccessMode(buttonTagList);
                    if (accessMode > userAccessMode)
                    {
                        button.IsEnabled = false;
                    }
                }
            }

            if (Grid != null && Grid.Menu != null && Grid.Menu.Count > 0)
            {
                foreach (var manuItem in Grid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }
        }

        public async void LoadItems()
        {
            DisableControls();

            var p = new Dictionary<string, string>();
            var v = Form.GetValues(); 
            {
                p.Add("TODAY_FROM", v.CheckGet("TODAY_FROM").ToString());
                p.Add("TODAY_TO", v.CheckGet("TODAY_TO").ToString());
                p.Add("FACTORY_ID", $"{FactoryId}");
            }

            //FIXME: naming

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPr");
            q.Request.SetParam("Object", "ProductionTaskPr");
            q.Request.SetParam("Action", "List");
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var itemsDs = ListDataSet.Create(result, "ITEMS");
                    Grid.UpdateItems(itemsDs);
                }                
            }

            EnableControls();
        }

        public async void FilterItems()
        {
            if (Grid.GridItems != null)
            {
                if (Grid.GridItems.Count > 0)
                {
                    //фильтрация строк

                    //обработка строк
                    foreach (Dictionary<string, string> row in Grid.GridItems)
                    {
                        
                        {
                            if (!row.ContainsKey("SELECTED"))
                            {
                                row.Add("SELECTED", "0");
                            }
                        }
                    }

                    bool doFilteringByStatus = false;
                    int status = 0;
                    if (Statuses.SelectedItem.Key != null)
                    {
                        doFilteringByStatus = true;
                        status = Statuses.SelectedItem.Key.ToInt();
                    }
                    bool doFilteringByMachine = false;
                    string machineName = Machine.SelectedItem.Key.ToString();
                    if (!string.IsNullOrEmpty(machineName))
                    {
                        doFilteringByMachine = true;
                    }

                    if (doFilteringByStatus || doFilteringByMachine)
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in Grid.GridItems)
                        {
                            bool includeByStatus = true;
                            bool includeByMachine = true;

                            if (doFilteringByStatus)
                            {
                                includeByStatus = false;
                                switch (status)
                                {
                                    //0 -- Все
                                    default:
                                        includeByStatus = true;
                                        break;

                                    //1 -- Не выполнено    
                                    case 1:
                                        if (row.ContainsKey("POSTING"))
                                        {
                                            if (row["POSTING"].ToInt() != 1)
                                            {
                                                includeByStatus = true;
                                            }
                                        }
                                        break;
                                    //2 -- Выполнено    
                                    case 2:
                                        if (row.ContainsKey("POSTING"))
                                        {
                                            if (row["POSTING"].ToInt() == 1)
                                            {
                                                includeByStatus = true;
                                            }
                                        }
                                        break;
                                }
                            }
                            if (doFilteringByMachine)
                            {
                                switch (machineName)
                                {
                                    default:
                                        includeByMachine = false;
                                        if (row.CheckGet("MACHINES").IndexOf(machineName) > -1)
                                        {
                                            includeByMachine = true;
                                        }
                                        break;
                                    case "0":
                                        includeByMachine = true;
                                        break;
                                }
                            }

                            if (includeByStatus && includeByMachine)
                            {
                                items.Add(row);
                            }
                        }
                        Grid.GridItems = items;
                    }
                }
            }    
        }

        public void SetDefaults()
        {
            //значения полей по умолчанию
            {
                Today_from.Text = DateTime.Now.ToString("dd.MM.yyyy");
                Today_to.Text = DateTime.Now.ToString("dd.MM.yyyy");


                {
                    var list = new Dictionary<string, string>();
                    list.Add("0", "Все");
                    list.Add("1", "Не выполнено");
                    list.Add("2", "Выполнено");

                    Statuses.Items = list;
                    Statuses.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");
                }

                {
                    var listMachine = new Dictionary<string, string>();
                    listMachine.Add("0", "Все");

                    Machine.Items = listMachine;
                    Machine.SelectedItem = listMachine.FirstOrDefault((x) => x.Key == "0");
                }

            }
        }

        public async void LoadRef()
        {
            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();
                p.Add("FACTORY_ID", $"{FactoryId}");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "ProductionPr");
                q.Request.SetParam("Object", "CutterPr");
                q.Request.SetParam("Action", "GetSources");
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {

                        {
                            MachineDS = ListDataSet.Create(result, "MACHINES");
                            var list = new Dictionary<string, string>();
                            list.Add("0", "Все");
                            foreach (Dictionary<string, string> row in MachineDS.Items)
                            {
                                list.Add(row.CheckGet("NAME2"), $"{row.CheckGet("NAME2")} ({row.CheckGet("NAME")})");
                            }
                            Machine.Items = list;
                            Machine.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");
                        }

                    }
                }

            }
        }

        /// <summary>
        /// экспорт записей грида в Excel
        /// </summary>
        private async void ExportToExcel()
        {
            if (Grid.Items != null)
            {
                if (Grid.Items.Count > 0)
                {
                    var eg = new ExcelGrid();
                    var cols = Grid.Columns;
                    eg.SetColumnsFromGrid(cols);
                    eg.Items = Grid.Items;
                    await Task.Run(() =>
                    {
                        eg.Make();
                    });
                }
            }
        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            //флаг активности текстового ввода
            //когда курсор стоит в поле ввода (например, поиск)
            //мы запрещаем обрабатывать такие клавиши, как Del, Ins etc
            bool inputActive = false;
            if (SearchText.IsFocused)
            {
                inputActive = true;
            }

            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

                case Key.F5:
                    Grid.LoadItems();
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
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
            Grid.ShowSplash();
            SplashControl.Visible = true;
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
            Grid.HideSplash();
            SplashControl.Visible = false;
        }

        private async void DeletePartitionalTask()
        {
            if (SelectedItem != null && SelectedItem.Count > 0)
            {
                {
                    string msg = $"Удалить производственное задание #{SelectedItem.CheckGet("ID_PZ")} {SelectedItem.CheckGet("NUM")} ?";
                    var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.NoYes);
                    if (d.ShowDialog() == false)
                    {
                        return;
                    }
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "ProductionPr");
                q.Request.SetParam("Object", "ProductionTaskPr");
                q.Request.SetParam("Action", "Delete");
                q.Request.SetParam("TASK_ID", SelectedItem.CheckGet("ID_PZ"));

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = 1;

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
                else
                {
                    q.ProcessError();
                }
            }
            else
            {
                string msg = $"Не выбрано производственное задание для удаления";
                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        private async void PostingProductionTask()
        {
            if (SelectedItem != null && SelectedItem.Count > 0)
            {
                {
                    string msg = $"Закрыть производственное задание #{SelectedItem.CheckGet("ID_PZ")} {SelectedItem.CheckGet("NUM")} ?";
                    var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.NoYes);
                    if (d.ShowDialog() == false)
                    {
                        return;
                    }
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "ProductionPr");
                q.Request.SetParam("Object", "ProductionTaskPr");
                q.Request.SetParam("Action", "SetPosting");
                q.Request.SetParam("TASK_ID", SelectedItem.CheckGet("ID_PZ"));
                q.Request.SetParam("POSTING", "1");

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = 1;

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
                else
                {
                    q.ProcessError();
                }
            }
            else
            {
                string msg = $"Не выбрано производственное задание для закрытия";
                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Создание окна для добавления примечания к ПЗГА
        /// </summary>
        public void EditTaskNote()
        {
            if (SelectedItem != null)
            {
                var taskNoteWindow = new ProductionTaskNote();
                taskNoteWindow.ReceiverName = TabName;
                var p = new Dictionary<string, string>()
                {
                    { "ID", SelectedItem.CheckGet("ID_PZ") },
                    { "NOTE", SelectedItem.CheckGet("NOTE") },
                };
                taskNoteWindow.Edit(p);
            }
        }

        /// <summary>
        /// Сохраняет примечание для ПЗПР
        /// </summary>
        /// <param name="p">Параметры для сохранения</param>
        public async void SaveTaskNote(Dictionary<string, string> p)
        {
            int taskId = p.CheckGet("ID").ToInt();
            if (taskId > 0)
            {
                // сохраняем только если сделали изменения в примечании
                if (SelectedItem.CheckGet("NOTE") != p.CheckGet("NOTE"))
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Production");
                    q.Request.SetParam("Object", "ProductionTask");
                    q.Request.SetParam("Action", "SaveNote");
                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    q.Request.SetParams(p);

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    if (q.Answer.Status == 0)
                    {
                        Grid.LoadItems();
                    }
                }
            }
        }


        /// <summary>
        /// Обработка сообщений
        /// </summary>
        /// <param name="obj"></param>
        private void ProcessMessages(ItemMessage obj)
        {
            if (obj.ReceiverGroup.IndexOf("ProductionTaskProcessing") > -1)
            {
                if (obj.ReceiverName.IndexOf(TabName) > -1)
                {
                    switch (obj.Action)
                    {
                        case "Refresh":
                            Grid.LoadItems();
                            break;
                    }
                }
            }
            // Для изменения примечания пользуемся формой для ПЗГА
            if (obj.ReceiverGroup.IndexOf("ProductionTask") > -1)
            {
                if (obj.ReceiverName.IndexOf(TabName) > -1)
                {
                    switch (obj.Action)
                    {
                        case "SaveNote":
                            SaveTaskNote((Dictionary<string, string>)obj.ContextObject);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// деструктор интерфейса
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "ProductionTaskPr",
                ReceiverName = "",
                SenderName = "ProductionTaskPrListView",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            Grid.Destruct();
        }

        /// <summary>
        /// отображение статьи в справочной системе
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp-new/recycling/task_production_pererab");
            //Central.ShowHelp("/doc/l-pack-erp/production/production_tasks_pr");
        }
        private void ShowButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Grid.LoadItems();
        }
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }
        private void ExportButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ExportToExcel();
        }

        /// <summary>
        /// обновление методов работы с выбранной записью
        /// </summary>
        /// <param name="selectedItem"></param>
        public void UpdateActions(Dictionary<string, string> selectedItem)
        {
            SelectedItem = selectedItem;

            DeleteButton.IsEnabled = false;
            Grid.Menu["delete_task"].Enabled = false;
            if (selectedItem.CheckGet("MACHINE_GROUP").ToInt() == 7
                || (selectedItem.CheckGet("KOL_FACT").ToInt() == 0
                && selectedItem.CheckGet("QUANTITY_GOODS").ToInt() == 0
                && selectedItem.CheckGet("QUANTITY_CORRUGATOR_TASK").ToInt() == 0))
            {
                DeleteButton.IsEnabled = true;
                Grid.Menu["delete_task"].Enabled = true;
            }

            bool completed = selectedItem.CheckGet("POSTING").ToBool();
            Grid.Menu["edit_task_note"].Enabled = !completed;
            PostingButton.IsEnabled = !completed;
            Grid.Menu["posting_task"].Enabled = !completed;
            
            ProcessPermissions();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var partitionSet = new ProductionTaskPartitionMap();
            partitionSet.ReceiverName = TabName;
            partitionSet.Edit();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            DeletePartitionalTask();
        }

        private void PostingButton_Click(object sender, RoutedEventArgs e)
        {
            PostingProductionTask();
        }
    }
}
