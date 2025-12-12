using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.Corrugator.TaskPlanningKashira
{
    /// <summary>
    /// Производственные задания, планирование
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>2</version>
    /// <released>2021-11-16</released>
    /// <refactor>volkov_as</refactor>
    public partial class TaskPlanningKsh : ControlBase
    {
        public TaskPlanningKsh()
        {
            InitializeComponent();

            ControlTitle = "Планирование ПЗ на ГА КШ";

            IsGridsReady = false;

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);

                    if(m.Action== "Refresh")
                    {
                        RefershGrids();

                    }
                }
            };

            OnLoad = () =>
            {
                
                InitGrid();
                Init();
                
                if (!PlanDataSetInit)
                {
                    PlanDataSetInit = true;
                    PlanDataSet.Load(true);
                }
            };

            OnUnload = () =>
            {
                Destroy();
            };

            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    ProcessKeyboard(e);
                }

            };
        }

        private bool PlanDataSetInit = false;

        private DataTable GridTable
        {
            get
            {
                DataTable result = null;

                result = GridShipment.GridControl.ItemsSource as DataTable;

                return result;
            }
        }

        private void InitGrid()
        {
            //список колонок грида
            var Columns = new List<DataGridHelperColumn>()
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="ID_TS",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата отгрузки",
                        Path="DATETS",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Производство",
                        Path="DTTM_PZ",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Отгрузка",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2=32,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Адрес",
                        Path="ADDRESS",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2=32,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Крайний срок",
                        Path="DEADLINE_SHIPPING",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Начальная дата планирования",
                        Path = "PLAN_DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2=12,
                        
                    }
                };

            GridShipment.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            GridShipment.OnSelectItem = (Dictionary<string, string> selectedItem) =>
            {
                if (selectedItem != null)
                {
                    UpdateActions(selectedItem);
                }
            };


            GridShipment.SetColumns(Columns);

            //if (_CurrentMachineId == 0)
            GridShipment.SetPrimaryKey("ID_TS");



            GridShipment.AutoUpdateInterval = 0;

            //данные грида
            GridShipment.OnLoadItems = LoadItems;
            GridShipment.UseProgressSplashAuto = false;
            GridShipment.Init();

            

            //GridShipment.GridControl.SelectedItemChanged += GridControl_SelectedItemChanged;
        }


        private void LoadItems()
        {
            
        }


        private void RefershGrids()
        {
            foreach (var machineGrid in MachineTaskGrids)
            {
                if(machineGrid.Value is TaskDxList tlist)
                {
                    tlist.LoadItems();
                }

            }
        }

        private void HideSplash()
        {
            GridShipment.IsEnabled = true;
            //GridShipment.HideSplash();
        }

        private void ShowSplash()
        {
            GridShipment.IsEnabled = false;
            //GridShipment.ShowSplash();
        }



        /// <summary>
        /// Имя вкладки
        /// </summary>
        public string TabName { get; set; }
        private bool IsInitialized { get; set; } = false;
        private int UpdatedGrids { get; set; }


        /// <summary>
        /// таймер для обновления состояния станка
        /// </summary>
        private DispatcherTimer RefreshGridTimer {  get; set; }


        public Dictionary<TaskPlaningDataSet.TypeStanok, ITaskList> MachineTaskGrids { get; set; }

        private int _indexOfActiveTaskGrid = -1;
        public int IndexOfActiveTaskGrid
        { 
            get 
            {
                return _indexOfActiveTaskGrid;
            }
            set
            {
                CurrentGrid?.SetActive(false);
                _indexOfActiveTaskGrid = value;
                CurrentGrid?.SetActive(true);
            }
        }


        /// <summary>
        /// DataSet в котором хранятся все задания и над которым производятся все операции по движению заданий
        /// </summary>
        public static TaskPlaningDataSet PlanDataSet { get; set; }

        private ITaskList CurrentGrid
        {
            get
            {
                ITaskList result = null;

                switch(IndexOfActiveTaskGrid)
                {
                    case 0:
                        result = MachineTaskGrids[TaskPlaningDataSet.TypeStanok.Unknow];
                        break;
                    case 1:
                        result = MachineTaskGrids[TaskPlaningDataSet.TypeStanok.Js];
                        break;
                }

                return result;
            }
        }

        public Dictionary<string, string> SelectedItem { get; private set; }

        private void Init()
        {
            PlanDataSet = new TaskPlaningDataSet();
            
            PlanDataSet.OnMessage += PlanDataSet_OnMessage;

            PlanDataSet.UpdateGrid += PlanDataSet_UpdateGrid;
            PlanDataSet.EndUpdate += PlanDataSet_EndUpdate;
            PlanDataSet.OnSaveNeeded += PlanDataSet_OnSaveNeeded;
            PlanDataSet.OnError += PlanDataSet_OnError;
            PlanDataSet.OnUpdateLine += PlanDataSet_OnUpdateLine;

            Toolbar.IsEnabled = false;

            RefreshGridTimer = new DispatcherTimer();
            RefreshGridTimer.Tick += RefreshGrid;
            RefreshGridTimer.Interval = new TimeSpan(0, 0, 10);
            RefreshGridTimer.Start();

            {
                var row = new Dictionary<string, string>();
                row.CheckAdd("TIMEOUT","10");
                row.CheckAdd("DESCRIPTION", "");
                Central.Stat.TimerAdd("TaskPlanning_Init", row);
            }

            MachineTaskGrids = new Dictionary<TaskPlaningDataSet.TypeStanok, ITaskList>
            {
                { TaskPlaningDataSet.TypeStanok.Unknow, GridTaskNew },
                { TaskPlaningDataSet.TypeStanok.Js, GridQueue1 },
                // { TaskPlaningDataSet.TypeStanok.Gofra3, GridQueue2 },
                // { TaskPlaningDataSet.TypeStanok.Fosber, GridQueue3 },
            };


            GridTaskNew.Init(this);
            // GridTaskNew.TaskGrid.SearchText = SearchText;

            foreach (var machineTaskGrid in MachineTaskGrids.Values)
            {
                machineTaskGrid.SetActivate(ChangeActiveTaskGrid);
            }
            
            GridQueue1.Init(this, TaskPlaningDataSet.TypeStanok.Js);
            // GridQueue2.Init(this, TaskPlaningDataSet.TypeStanok.Gofra3);
            // GridQueue3.Init(this, TaskPlaningDataSet.TypeStanok.Fosber);

            Brush splitterBrush = HColor.Gray.ToBrush();
            SplitterVertical1.Background = splitterBrush;
            // SplitterHorizontal1.Background = splitterBrush;
            // SplitterHorizontal2.Background = splitterBrush;

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            ProcessPermissions();
        }

        private void PlanDataSet_OnUpdateLine(TaskPlaningDataSet.TypeStanok typeStanok, string key)
        {
            foreach (var machineTaskGrid in MachineTaskGrids.Values)
            {
                machineTaskGrid.UpdateLineAsync(typeStanok, key);
            }
        }

        private void PlanDataSet_OnError(string message)
        {
            var d = new DialogWindow(
                        message,
                        "Данное действие не возможно",
                        "",
                        DialogWindowButtons.OK);
            d.ShowDialog();
        }

        private void PlanDataSet_OnSaveNeeded(bool save)
        {
            if(save)
            {
                ButtonApply.Style = (Style)ButtonApply.TryFindResource("FButtonPrimary");
            }
            else
            {
                ButtonApply.Style = (Style)ButtonApply.TryFindResource("Button");
            }
        }

        private int CodeMessage = 100;

        private void PlanDataSet_OnMessage(string message)
        {
            CodeMessage++;

            // отключил сообщения, так как их сложно очищать
            /*Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "",
                ReceiverName = "Notifications",
                SenderName = "TaskPlanning",
                Action = "Add",
                Message = message,
                ContextObject = CodeMessage
            });*/
        }

        private void PlanDataSet_EndUpdate(TaskPlaningDataSet.TypeStanok type)
        {
            Toolbar.IsEnabled = true;
        }

        private void PlanDataSet_UpdateGrid(TaskPlaningDataSet.TypeStanok stanok)
        {
            if(MachineTaskGrids.ContainsKey(stanok))
            {
                MachineTaskGrids[stanok].LoadItems();

                IsGridsReady = true;
            }
        }

        private void RefreshGrid(object sender, EventArgs e)
        {
            foreach (var task in MachineTaskGrids.Values)
            {
                task.UpdateTask();
            }
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        private void ProcessMessages(ItemMessage obj)
        {
            //Group ProductionTask
            if (obj.ReceiverGroup.IndexOf("TaskPlanning") > -1)
            {
                if (obj.ReceiverName.IndexOf("IdleItem") > -1)
                {
                    switch (obj.Action)
                    {
                        case "Refresh":

                            string[] param = obj.Message.Split(',');
                            if(param.Length > 2)
                            {
                                int dopl_id = param[0].ToInt();
                                DateTime start = param[1].ToDateTime();
                                DateTime end = param[2].ToDateTime();

                                CurrentGrid.UpdateDownTime(dopl_id, start, end);
                            }
                            break;
                    }
                }
            }
        }

        private void Undo()
        {
            Toolbar.IsEnabled = false;
            if (PlanDataSet.Undo())
            {

            }

            Toolbar.IsEnabled = true;
        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        public void ProcessKeyboard(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:
                    CurrentGrid?.Delete();
                    break;
                case Key.Escape:

                    foreach (var machineTaskGrid in MachineTaskGrids.Values)
                    {
                        machineTaskGrid.ClearAllCheckBoxes();
                    }

                    break;
                case Key.Space:
                    {
                        CurrentGrid?.CurrentItemSelect();
                    }
                    break;
                case Key.Z:

                    if(Keyboard.IsKeyDown(Key.LeftCtrl))
                    {
                        Undo();
                    }
                    break;

                case Key.W:

                    MoveTasks("up");
                    break;
                case Key.S:
                    MoveTasks("down");
                    break;
                case Key.F5:
                    //Grid.LoadItems();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

                case Key.Home:
                    //Grid.SetSelectToFirstRow();
                    e.Handled = true;
                    break;

                case Key.End:
                    //Grid.SetSelectToLastRow();
                    e.Handled = true;
                    break;
                // case Key.Add:
                //     ChangeShipmentTime(1);
                //     break;
                // case Key.Subtract:
                //     ChangeShipmentTime(-1);
                //     break;
                    
            }
        }

        /// <summary>
        /// отображение статьи в справочной системе
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/production/production_tasks/planning");
        }

        /// <summary>
        /// деструктор интерфейса
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "ProductionTask",
                ReceiverName = "",
                SenderName = TabName,
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            foreach (var machineTaskGrid in MachineTaskGrids.Values)
            {
                machineTaskGrid.Destruct();
            }

            if(RefreshGridTimer!=null)
            {
                RefreshGridTimer.Stop();
                RefreshGridTimer = null;
            }
        }

        private void LoadGrid(int taskId)
        {

            if (IsGridsReady)
            {

                SelectedItem = null;

                ShowSplash();

                var p = new Dictionary<string, string>()
                {
                    { "ID_PZ", taskId.ToString() },
                };

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "TaskPlanningKashira");
                q.Request.SetParam("Action", "ListShipmentByProdId");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();


                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            GridShipment.UpdateItems(ds);
                        }
                    }
                }

                HideSplash();
            }
        }


        /// <summary>
        /// обновление методов работы с выбранной записью
        /// </summary>
        public void UpdateActions(Dictionary<string, string> selectedItem)
        {
            SelectedItem = selectedItem;

            // Сформируем меню для изменения даты
            if(SelectedItem != null)
            {
                var date = SelectedItem.CheckGet("DATETS").ToDateTime();
                var startDate = SelectedItem.CheckGet("PLAN_DTTM").ToDateTime();

                if (date != DateTime.MinValue)
                {

                    GridShipment.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    {
                        "Start",
                        new DataGridContextMenuItem()
                        {
                            Header = startDate.ToString(),
                            Enabled = false
                        }
                    },

                    {
                        "Down",
                        new DataGridContextMenuItem()
                        {
                            Header = "Раньше",
                            ToolTip = "Перенести дату отгрузки на время ранее"
                             , Tag = "access_mode_full_access"
                        }
                    },

                    {
                        "Up",
                        new DataGridContextMenuItem()
                        {
                            Header = "Позже",
                            ToolTip = "Перенести дату отгрузки на более позднее время"
                             , Tag = "access_mode_full_access"
                        }
                    },
                };

                    var up = GridShipment.Menu["Up"];
                    var Down = GridShipment.Menu["Down"];

                    int i, n = 25;
                    for (i = 1; i < n; i++)
                    {
                        var dateUp = date.AddHours(i);
                        var dateDown = date.AddHours(-i);

                        int jUp = i;
                        int jDown = -i;

                        up.Items.Add(jUp.ToString(), new DataGridContextMenuItem()
                        {
                            Header = $"{jUp} {dateUp.ToString().TrimEnd(":00")}",
                            ToolTip = $"на {jUp} позже",
                            Action = () =>
                            {
                                ChangeShipmentTime(jUp, true);
                            }
                        }
                        );

                        Down.Items.Add(jDown.ToString(), new DataGridContextMenuItem()
                        {
                            Header = $"{jDown} {dateDown.ToString().TrimEnd(":00")}",
                            ToolTip = $"на {jUp} ранее",
                            Action = () =>
                            {
                                ChangeShipmentTime(jDown, true);
                            }
                        }
                        );
                    }
                }

                ProcessPermissions();
            }
        }


        private DispatcherTimer ChangeShipmentTimer { get; set; }
        /// <summary>
        /// флаг готовности одного из грида отображающих планирования гофроагрегатов
        /// 
        /// </summary>
        public bool IsGridsReady { get; private set; }

        /// <summary>
        /// Изменение времени даты отгрузки
        /// </summary>
        /// <param name="hour"></param>
        private void ChangeShipmentTime(int hour, bool AscBeforeChangeDate = false)
        {
            if (SelectedItem != null)
            {
                if (GridTable != null)
                {
                    bool resume = true;

                    if (AscBeforeChangeDate)
                    {
                        resume = false;

                        var date = SelectedItem.CheckGet("DATETS").ToDateTime();
                        var newDate = date.AddHours(hour);

                        var d = new DialogWindow(
                            $"Вы хотите изменить дату отгрузки с {date.ToString()} на {newDate.ToString()}",
                            $"Изменение даты отшрузки на {hour}ч",
                            "",
                            DialogWindowButtons.YesNoCancel);
                        
                        if(d.ShowDialog()== true)
                        {
                            resume = true;

                        }

                    }

                    if (resume)
                    {

                        var Items = GridTable.AsEnumerable();
                        var item = Items.FirstOrDefault(x => x.CheckGet("ID_TS").ToInt() == SelectedItem.CheckGet("ID_TS").ToInt());
                        if (item != null)
                        {

                            var date = item.CheckGet("DATETS").ToDateTime();
                            date = date.AddHours(hour);

                            item["DATETS"] = date.ToString("dd.MM.yyyy HH:mm");
                            SelectedItem["DATETS"] = date.ToString("dd.MM.yyyy HH:mm");


                            if (ChangeShipmentTimer != null)
                            {
                                ChangeShipmentTimer.Stop();
                                ChangeShipmentTimer = null;
                            }

                            ChangeShipmentTimer = new DispatcherTimer()
                            {
                                Interval = new TimeSpan(0, 0, 0, 0, 1000)
                            };

                            {
                                var row = new Dictionary<string, string>();
                                row.CheckAdd("TIMEOUT", "1");
                                row.CheckAdd("DESCRIPTION", "");
                                Central.Stat.TimerAdd("TaskPlanning_ChangeShipmentTime", row);
                            }

                            ChangeShipmentTimer.Tick += (s, e) =>
                            {
                                ChangeShipmentTimer.Stop();
                                ChangeShipmentTimer = null;

                                int idTransport = item.CheckGet("ID_TS").ToInt();

                                if (idTransport > 0)
                                {
                                    var date = item["DATETS"].ToString().ToDateTime();

                                    if (date != DateTime.MinValue)
                                    {
                                        // можно менять дату
                                        var p = new Dictionary<string, string>()
                                        {
                                            { "IDTS", idTransport.ToString() },
                                            { "SHIPMENTDATE", date.ToString("dd.MM.yyyy HH:mm:00") },
                                        };

                                        var q = new LPackClientQuery();
                                        q.Request.SetParam("Module", "Production");
                                        q.Request.SetParam("Object", "TaskPlanningKashira");
                                        q.Request.SetParam("Action", "SetShipmentDateByProdId");
                                        q.Request.SetParams(p);

                                        q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                                        q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                                        q.DoQuery();


                                        if (q.Answer.Status == 0)
                                        {

                                        }
                                    }
                                }
                            };

                            ChangeShipmentTimer.Start();
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
            //if(Grid!=null)
            //{
            //    if(Grid.Items.Count>0)
            //    {
            //        var eg = new ExcelGrid();
            //        var cols=Grid.Columns;
            //        eg.SetColumnsFromGrid(cols);
            //        eg.Items = Grid.Items;
            //        await Task.Run(() =>
            //        {
            //            eg.Make();
            //        });
            //    }
            //}
        }


        /// <summary>
        /// Смена активного гофроагрегата
        /// </summary>
        /// <param name="machineId"></param>
        /// <param name="selectedItem">по ID_TS будет обновлен грил отгрузок</param>
        public void ChangeActiveTaskGrid(TaskPlaningDataSet.TypeStanok machineId, Dictionary<string,string> selectedItem)
        {
            //если активный грид сменился
            if (!(
                   (machineId == TaskPlaningDataSet.TypeStanok.Unknow && _indexOfActiveTaskGrid == 0)
                || (machineId == TaskPlaningDataSet.TypeStanok.Js  && _indexOfActiveTaskGrid == 1)
                // || (machineId == TaskPlaningDataSet.TypeStanok.Gofra3 && _indexOfActiveTaskGrid == 2)
                // || (machineId == TaskPlaningDataSet.TypeStanok.Fosber && _indexOfActiveTaskGrid == 3)
                )) 
            {
                if (_indexOfActiveTaskGrid != -1)
                {
                    //очищаем старый грид от галочек
                    CurrentGrid?.ClearAllCheckBoxes();
                }
                //и меняем активный грид на новый
                ChangeIndexOfActiveTaskGrid(machineId);
            }

            if(selectedItem!=null)
            {
                int TaskId = selectedItem.CheckGet(TaskPlaningDataSet.Dictionary.ProductionTaskId).ToInt();
                if(TaskId>0)
                {
                    LoadGrid(TaskId);
                }
            }
        }

        

        public void ChangeIndexOfActiveTaskGrid(TaskPlaningDataSet.TypeStanok machineId = TaskPlaningDataSet.TypeStanok.Unknow )
        {
            if (machineId == TaskPlaningDataSet.TypeStanok.Unknow)
            {
                IndexOfActiveTaskGrid = 0;
            }
            else if (machineId == TaskPlaningDataSet.TypeStanok.Js)
            {
                IndexOfActiveTaskGrid = 1;
            }
            // else if(machineId == TaskPlaningDataSet.TypeStanok.Gofra3)
            // {
            //     IndexOfActiveTaskGrid = 2;
            // }
            // else if(machineId == TaskPlaningDataSet.TypeStanok.Fosber)
            // {
            //     IndexOfActiveTaskGrid = 3;
            // }
            else
            {
                IndexOfActiveTaskGrid = -1;
            }
        }

        public void ClearOtherMachinesCheckboxes(TaskPlaningDataSet.TypeStanok machineId = TaskPlaningDataSet.TypeStanok.Unknow)
        {
            foreach (var gridQueue in MachineTaskGrids.Values)
            {
                if (gridQueue?.CurrentMachineId != machineId)
                {
                    gridQueue?.ClearAllCheckBoxes();
                }
            }
        }

        /// <summary>
        /// Перемещение заданий вверх/вниз по гриду
        /// </summary>
        /// <param name="direction">Направление перемещения</param>
        public void MoveTasks(string direction)
        {
            if (IndexOfActiveTaskGrid != -1)
            {
                CurrentGrid?.MoveTasks(direction);
            }
        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "[erp]prod_task_plan_kashira")
        {
            var userAccessMode = Central.Navigator.GetRoleLevel(roleCode);

            //UIUtil.SetFrameworkElementEnabledByTagAccessMode(this.Content as DependencyObject, Acl.AccessMode.ReadOnly);

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

            if (GridShipment != null && GridShipment.Menu != null && GridShipment.Menu.Count > 0)
            {
                foreach (var manuItem in GridShipment.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }

            foreach (var machineTaskGrid in MachineTaskGrids.Values)
            {
                if(machineTaskGrid is TaskDxList grid)
                {
                    grid.ProcessPermissions(roleCode);
                }
            }
        }

        public bool CanMoveTask(IEnumerable<Dictionary<string, string>> items, TaskPlaningDataSet.TypeStanok destination, TaskPlaningDataSet.TypeStanok source)
        {
            bool res = true;

            if(destination==TaskPlaningDataSet.TypeStanok.Unknow && source==TaskPlaningDataSet.TypeStanok.Unknow)
            {
                // если двигаем внутри неприсвоенных заданий то все нормально

            }
            else
            { 
                var cantMove = items.Where(x => x.CheckGet(TaskPlaningDataSet.Dictionary.OnMachine).ToInt() != 0).ToList();

                res = cantMove.Count==0;

                if (!res)
                {
                    var d = new DialogWindow(
                            "Задание уже запланировано в производство",
                            "Над заданиями которые уже в производстве действия невозможны!",
                            "Выберите задания которые не запланированны в производство",
                            DialogWindowButtons.OK);
                    d.ShowDialog();
                }
                else
                {
                    //cantMove = items.Where(x => x.CheckGet(TaskPlaningDataSet.Dictionary.DropdownId).ToInt() != 0).ToList();
                    //res = cantMove.Count == 0;

                    // разрешу перемещать простои
                    res = true;


                    if (!res)
                    {
                        var d = new DialogWindow(
                                "В заданиях присутствует простой",
                                "Над простоями действия невозможны!",
                                "Выберите задания которые не содержат простои",
                                DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                    else
                    {
                        // Форматы выше 2500 едет только ФОСБЕР
                        // if (destination != TaskPlaningDataSet.TypeStanok.Fosber && destination != TaskPlaningDataSet.TypeStanok.Unknow)
                        // {
                        //     var formats2500 = items.Where(x => x.CheckGet(TaskPlaningDataSet.Dictionary.Format).ToInt() > 2500);
                        //     res = !formats2500.Any();
                        //
                        //     if (!res)
                        //     {
                        //         var d = new DialogWindow(
                        //                 "В заданиях присутствует форматы более 2500",
                        //                 "Есть задания которые не возможно выполнить на данном станке",
                        //                 "Выберите задания которые можно выполнить на выюранном станке",
                        //                 DialogWindowButtons.OK);
                        //         d.ShowDialog();
                        //     }
                        //
                        // }
                    }

                    if (res)
                    {
                        if (destination != TaskPlaningDataSet.TypeStanok.Js && destination!= TaskPlaningDataSet.TypeStanok.Unknow)
                        {
                            var FanfoldOrGlued = items.Where(x => x.CheckGet(TaskPlaningDataSet.Dictionary.Fanfold).ToInt() != 0 || x.CheckGet(TaskPlaningDataSet.Dictionary.Glued).ToInt() != 0);
                            res = !FanfoldOrGlued.Any();

                            if (!res)
                            {
                                var d = new DialogWindow(
                                        "Одно или больше заданий возможно выполнить только на БХС1",
                                        "Есть задания которые не возможно выполнить на данном станке",
                                        "Выберите задания которые можно выполнить на выбранном станке",
                                        DialogWindowButtons.OK);
                                d.ShowDialog();
                            }
                        }
                    }

                    // пятислойный картон не может быть изготовлен на бхс-2
                    // if(res)
                    // {
                    //     if(destination== TaskPlaningDataSet.TypeStanok.Gofra3)
                    //     {
                    //         var layer5 = items.Where(x =>
                    //             x.CheckGet(TaskPlaningDataSet.Dictionary.Layer1) != string.Empty &&
                    //             x.CheckGet(TaskPlaningDataSet.Dictionary.Layer2) != string.Empty &&
                    //             x.CheckGet(TaskPlaningDataSet.Dictionary.Layer3) != string.Empty &&
                    //             x.CheckGet(TaskPlaningDataSet.Dictionary.Layer4) != string.Empty &&
                    //             x.CheckGet(TaskPlaningDataSet.Dictionary.Layer5) != string.Empty
                    //         );
                    //
                    //         res = !layer5.Any();
                    //
                    //         if (!res)
                    //         {
                    //             var d = new DialogWindow(
                    //                     "Одно или больше заданий не возможно выполнить БХС2",
                    //                     "Есть задания которые не возможно выполнить на данном станке",
                    //                     "Выберите задания которые можно выполнить на выюранном станке",
                    //                     DialogWindowButtons.OK);
                    //             d.ShowDialog();
                    //         }
                    //     }
                    // }

                    // условие перемещения на разрещенные гофроагрегаты
                    if (res)
                    {
                        if (destination != TaskPlaningDataSet.TypeStanok.Unknow && destination!=source)
                        {
                            string message = string.Empty;

                            items.ForEach(x =>
                            {
                                int typeMove = x.CheckGet(TaskPlaningDataSet.Dictionary.PossibleMachine).ToInt();
                                string orderText = "Заказ ";

                                /// 1	Все ГА
                                /// 2	
                                string onlyBHs1 = "предназначен только для BHS-1";
                                /// 3	
                                string onlyBHs2 = "предназначен только для BHS-2";
                                /// 4	
                                string onlyFosber = "предназначен только для Fosber";
                                /// 5	
                                string onlyBHS12 = "предназначен только для BHS-1 и BHS-2";
                                /// 6	
                                string onlyBHS1Fosber = "предназначен только для BHS-1 и Fosber";
                                /// 7	
                                string onlyBHS2Fosber = "предназначен только для BHS-2 и Fosber";

                                string newLine = Environment.NewLine;

                                if (typeMove != 0 && typeMove!=1)
                                {

                                    string order = x.CheckGet(TaskPlaningDataSet.Dictionary.Order) + " ";

                                    if (destination == TaskPlaningDataSet.TypeStanok.Js) // BHS-1
                                    {
                                        if(typeMove==3)
                                        {
                                            message += orderText + order + onlyBHs2 + newLine;
                                        }
                                        else if(typeMove==4)
                                        {
                                            message += orderText + order + onlyFosber + newLine;
                                        }
                                        else if(typeMove==7)
                                        {
                                            message += orderText + order + onlyBHS2Fosber + newLine;
                                        }
                                    }
                                    // else if(destination == TaskPlaningDataSet.TypeStanok.Gofra3) // BHS-2
                                    // {
                                    //     if (typeMove == 1)
                                    //     {
                                    //         message += orderText + order + onlyBHs1 + newLine;
                                    //     }
                                    //     else if (typeMove == 4)
                                    //     {
                                    //         message += orderText + order + onlyFosber + newLine;
                                    //     }
                                    //     else if (typeMove == 6)
                                    //     {
                                    //         message += orderText + order + onlyBHS1Fosber + newLine;
                                    //     }
                                    // }
                                    // else if(destination == TaskPlaningDataSet.TypeStanok.Fosber) // Fosber
                                    // {
                                    //     if (typeMove == 1)
                                    //     {
                                    //         message += orderText + order + onlyBHs1 + newLine;
                                    //     }
                                    //     if (typeMove == 2)
                                    //     {
                                    //         message += orderText + order + onlyBHs1 + newLine;
                                    //     }
                                    //     else if (typeMove == 5)
                                    //     {
                                    //         message += orderText + order + onlyBHS12 + newLine;
                                    //     }
                                    // }
                                }
                            });

                            if (message != string.Empty)
                            {
                                var d = new DialogWindow(
                                        message,
                                        "Есть задания которые не возможно выполнить на данном станке",
                                        "Выберите задания которые можно выполнить на выюранном станке",
                                        DialogWindowButtons.OK);

                                d.ShowDialog();
                            }
                        }
                    }
                }
            }

            return res;
        }

        /// <summary>
        /// Перемещение заданий между разными гридамаи
        /// </summary>
        private void PlaceTask(TaskPlaningDataSet.TypeStanok machineIdFrom, TaskPlaningDataSet.TypeStanok machineIdTo)
        {
            if(machineIdFrom!=machineIdTo)
            {
                var items = MachineTaskGrids[machineIdFrom].GetSelectedItems();// TaskGrid.Items.Where(x => x.CheckGet(TaskPlaningDataSet.Dictionary.Selected).ToInt() == 1);

                if (items != null && items.Count() > 0)
                {
                    //var cantMove = items.Select(x => x.CheckGet(TaskPlaningDataSet.Dictionary.OnMachine).ToInt() != 0);

                    if(CanMoveTask(items, machineIdTo, machineIdFrom))
                    {
                        PlanDataSet.MakeAction(TaskPlaningDataSet.UserAction.ActionType.MoveInsertDown, items, (int)machineIdFrom, (int)machineIdTo);
                        ChangeIndexOfActiveTaskGrid(machineIdTo);
                        // не имеет смысла, так как удаление и перемещение произойдет после перезагрузки грида
                        //CurrentGrid.SetSelectToLastRow();
                    }
                }
                else
                {
                    var d = new DialogWindow(
                        "Выберите хотя бы одно задание.",
                        "Не выбрано ни одного задания!",
                        "Отметьте галочками слева задания, которые надо перенести.",
                        DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
        }

        private void EnableEdit(bool enable)
        {
            Toolbar.IsEnabled = enable;

            foreach (var machineTaskGrid in MachineTaskGrids.Values)
            {
                if (machineTaskGrid != null)
                {
                    machineTaskGrid.EnableEdit(enable);
                }
            }
        }

        private async void ApplyChanges()
        {
            var d = new DialogWindow(
                            "Вы хотите сохранить новые очереди на гофроагрегате?",
                            "Сохранение!",
                            "Если вы хотите сохранить очередь на гофроагрегате, просто нажмите \"Да\".",
                            DialogWindowButtons.YesNo);
            d.ShowDialog();
            if (d.DialogResult == true)
            {
                EnableEdit(false);
                string error = string.Empty;

                List<Task> saveTasks = new List<Task>();

                string result = string.Empty;

                bool resume = false;


                double hours = 0.0;
                // запускаем задачи сохранения для каждого из гридов
                foreach (var machineTaskGrid in MachineTaskGrids.Values)
                {
                    if (machineTaskGrid != null)
                    {
                        if (machineTaskGrid.CurrentMachineId != TaskPlaningDataSet.TypeStanok.Unknow)
                        {
                            resume = false;
                            int AnsverCode = 0;

                            var q =  await machineTaskGrid.SaveQueue();

                            int machineId = q.Request.Params.CheckGet(TaskPlaningDataSet.Dictionary.StanokId).ToInt();
                            if (q.Answer.Status == 0)
                            {
                                if (q.Answer.QueryResult != null)
                                {
                                    if (q.Answer.QueryResult.Items.Count > 0)
                                    {
                                        AnsverCode = q.Answer.QueryResult.Items[0].CheckGet("ID").ToInt();
                                        if (AnsverCode != 0)
                                        {
                                            // сохранение прошло удачно

                                            result += AnsverCode.ToString() + " сохранено " + Environment.NewLine;

                                            hours += machineTaskGrid.GetHours();

                                            resume = true;
                                        }
                                    }
                                }
                            }

                            if (!resume)
                            {
                                // возникла проблема сохранения
                                error += "Возникла проблема с сохранением станка ID = " + machineId + " код ошибки " + AnsverCode.ToString() + Environment.NewLine;
                            }
                        }
                    }
                }

                if (error != string.Empty)
                {
                    var errorDlg = new DialogWindow(
                        error,
                        "При сохранении очередей возникли ошибки!",
                        "Попробуйте повторить попытку позднее",
                        DialogWindowButtons.OK);
                    errorDlg.ShowDialog();
                }
                else
                {
                    var q = await LPackClientQuery.DoQueryAsync("Production", "TaskPlanningKashira", "RestoreFlagUpdate", string.Empty, new Dictionary<string, string>() { { "HOURS", hours.ToInt().ToString() } });
                    if (q.Answer.Status == 0)
                    {
                        // все удачно завершено
                        PlanDataSet_OnSaveNeeded(false);
                    }
                }

                EnableEdit(true);

                
            }
        }


       
        /// <summary>
        /// обновление всех данных
        /// </summary>
        private void LoadData()
        {
            Toolbar.IsEnabled = false;
            PlanDataSet.Load(false);
        }


        private async void ShowButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Toolbar.IsEnabled = false;
            PlanDataSet.Synchronization();
        }

        private void ExportButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ExportToExcel();
        }

        private void HelpButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void ButtonTaskUp_Click(object sender, RoutedEventArgs e)
        {
            MoveTasks("up");
        }
        private void ButtonTaskDown_Click(object sender, RoutedEventArgs e)
        {
            MoveTasks("down");
        }

        private void ButtonApply_Click(object sender, RoutedEventArgs e)
        {
            if (GridQueue1.GetTaskSearchText.IsNullOrEmpty())
            {
                ApplyChanges();
            }
            else
            {
                var dialog = new DialogWindow("Невозможно сохранить очередь. Очистите фильтры перед сохранением!",
                    "Сохранение");
                dialog.ShowDialog();
            }
        }

        private void ButtonDeleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (IndexOfActiveTaskGrid != -1
                && IndexOfActiveTaskGrid != 0)
            {
                PlaceTask(CurrentGrid.CurrentMachineId, 0);
            }
        }


        private void CheckColumnsWidth()
        {
            if (GeneralColumnRight.Width.Value == 0)
            {
                GeneralColumnLeft.Width = new GridLength(2, GridUnitType.Star);
                GeneralColumnRight.Width = new GridLength(1, GridUnitType.Star);
                ButtonArrowImage.Style = (Style)ButtonArrowImage.TryFindResource("ArrowRight2Image");
            }
            else
            {
                GeneralColumnRight.Width = new GridLength(0, GridUnitType.Star);
                ButtonArrowImage.Style = (Style)ButtonArrowImage.TryFindResource("ArrowLeft2Image");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            CheckColumnsWidth();
        }

        private void SplitterVertical1_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            if (GeneralColumnRight.Width.Value == 0)
            {
                ButtonArrowImage.Style = (Style)ButtonArrowImage.TryFindResource("ArrowLeft2Image");
            }
            else
            {
                ButtonArrowImage.Style = (Style)ButtonArrowImage.TryFindResource("ArrowRight2Image");
            }
        }

        private void PlaceToCM1Button_Click(object sender, RoutedEventArgs e)
        {
            if(IndexOfActiveTaskGrid != -1)
            {
                PlaceTask(CurrentGrid.CurrentMachineId, TaskPlaningDataSet.TypeStanok.Js);
            }
        }
        // private void PlaceToCM2Button_Click(object sender, RoutedEventArgs e)
        // {
        //     if (IndexOfActiveTaskGrid != -1)
        //     {
        //         PlaceTask(CurrentGrid.CurrentMachineId, TaskPlaningDataSet.TypeStanok.Gofra3);
        //     }
        // }
        // private void PlaceToCM3Button_Click(object sender, RoutedEventArgs e)
        // {
        //     if (IndexOfActiveTaskGrid != -1)
        //     {
        //         PlaceTask(CurrentGrid.CurrentMachineId, TaskPlaningDataSet.TypeStanok.Fosber);
        //     }
        // }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            var d = new DialogWindow(
                            "Данные будут перезаписаны из БД, продолжить?",
                            "Загрузка!",
                            "Если вы хотите загрузить данные из БД, просто нажмите \"Да\".",
                            DialogWindowButtons.YesNo);
            d.ShowDialog();
            if (d.DialogResult == true)
            {
                Toolbar.IsEnabled = false;

                foreach (var machineTaskGrid in MachineTaskGrids.Values)
                {
                    if(machineTaskGrid is TaskDxList list)
                    {
                        list.EnableSortIdles = true;
                        list.ShowSplash();
                    }


                }

                PlanDataSet.Load(true);

                
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            new Settings().Edit();
        }

        /// <summary>
        /// Установка даты и времени загрузки гофроагрегата
        /// </summary>
        /// <param name="currentMachineId"></param>
        /// <param name="lastDateTime"></param>
        internal void SetLastDataTime(TaskPlaningDataSet.TypeStanok currentMachineId, DateTime lastDateTime, double lastDurationMinutes)
        {
           PlanDataSet.SetLastDataTime(currentMachineId, lastDateTime, lastDurationMinutes);
        }

        private RowDefinition GetRowByName(TaskPlaningDataSet.TypeStanok type)
        {
            if(type== TaskPlaningDataSet.TypeStanok.Js)
            {
                return Grid1;
            }
            // else if(type== TaskPlaningDataSet.TypeStanok.Gofra5)
            // {
            //     return Grid2;
            // }

            return Grid3;
        }

        internal void FullScreen(TaskDxList taskDxList)
        {
            var owner = GetRowByName(taskDxList.CurrentMachineId);

            //foreach (var machineGrid in MachineTaskGrids)
            //{
            //    if (machineGrid.Key != TaskPlaningDataSet.TypeStanok.Unknow)
            //    {
            //        if (machineGrid.Value is TaskDxList tlist)
            //        {
            //            if(tlist!=taskDxList)
            //            {
            //                var grid = GetRowByName(machineGrid.Key);

            //                if (tlist.Visibility == Visibility.Collapsed)
            //                {
            //                    tlist.Visibility = Visibility.Visible;
            //                    //grid.Height = owner.Height;
            //                }
            //                else
            //                {
            //                    tlist.Visibility = Visibility.Collapsed;
            //                    grid.Height = new GridLength(10);
            //                }
            //            }
            //        }
            //    }
            //}
        }
    }
}
