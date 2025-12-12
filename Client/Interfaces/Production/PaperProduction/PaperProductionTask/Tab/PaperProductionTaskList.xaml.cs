using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using DevExpress.Mvvm.Xpf;
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
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Логика взаимодействия для PaperProductionTaskList.xaml
    /// </summary>
    public partial class PaperProductionTaskList : ControlBase
    {
        public PaperProductionTaskList()
        {
            ControlTitle = "ПЗ БДМ";
            DocumentationUrl = "/doc/l-pack-erp/";
            InitializeComponent();

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };

            //конструктор, будет вызван, когда объект создается
            //здесь создаются все внутренние структуры
            //впервые этот коллбэк будет вызван, когда данный таб станет активным
            //впервые (до этих пор, никакая работа внутри не происходит, что экономит ресурсы)
            OnLoad = () =>
            {
                ProcessPermissions();
                SetDefaults();
                TaskGridInit();
                PositionGridInit();
                CurrentOperationsGridInit();
                CellGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                TaskGrid.Destruct();
                PositionGrid.Destruct();
                CurrentOperationsGrid.Destruct();
                CellGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                TaskGrid.ItemsAutoUpdate = true;
                TaskGrid.Run();

                CellGrid.ItemsAutoUpdate = true;
                CellGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                TaskGrid.ItemsAutoUpdate = false;

                CellGrid.ItemsAutoUpdate = false;
            };

            {
                Commander.Add(new CommandItem()
                {
                    Name = "refresh",
                    Group = "main",
                    Enabled = true,
                    Title = "Обновить",
                    Description = "Обновить данные",
                    ButtonUse = true,
                    ButtonControl = RefreshItemsButton,
                    ButtonName = "RefreshItemsButton",
                    Action = () =>
                    {
                        Refresh();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "help",
                    Group = "main",
                    Enabled = true,
                    Title = "Справка",
                    Description = "Показать справочную информацию",
                    ButtonUse = true,
                    ButtonControl = HelpButton,
                    ButtonName = "HelpButton",
                    HotKey = "F1",
                    Action = () =>
                    {
                        Central.ShowHelp(DocumentationUrl);
                    },
                });
            }

            Commander.SetCurrentGridName("PositionGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "show_cell_by_position",
                    Title = "Показать ячейку",
                    Group = "position_grid_default",
                    MenuUse = true,
                    Enabled = true,
                    Action = () =>
                    {
                        ShowCell(PositionGrid.SelectedItem);
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "create_task",
                    Title = "Создать задачу",
                    Description = "Создать задачу на списание макулатуры в производство",
                    Group = "position_grid_default",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = CreateTaskButton,
                    ButtonName = "CreateTaskButton",
                    Enabled = false,
                    Action = () =>
                    {
                        CreateTask();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (PositionGrid != null && PositionGrid.Items != null && PositionGrid.Items.Count > 0)
                        {
                            if (PositionGrid.SelectedItem != null && PositionGrid.SelectedItem.Count > 0)
                            {
                                if (string.IsNullOrEmpty(PositionGrid.SelectedItem.CheckGet("TASK_ID")))
                                {
                                    result = true;
                                }
                            }
                        }

                        return result;
                    },
                });
            }

            Commander.SetCurrentGridName("CurrentOperationsGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "show_cell_by_current_operations",
                    Title = "Показать ячейку",
                    Group = "current_operations_grid_default",
                    MenuUse = true,
                    Enabled = true,
                    Action = () =>
                    {
                        ShowCell(CurrentOperationsGrid.SelectedItem);
                    },
                });
            }

            Commander.Init(this);
        }

        private ListDataSet TaskDataSet { get; set; }

        private ListDataSet PositionDataSet { get; set; }

        private ListDataSet CurrentOperationsDataSet { get; set; }

        private ListDataSet CellDataSet { get; set; }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            TaskDataSet = new ListDataSet();
            PositionDataSet = new ListDataSet();
            CurrentOperationsDataSet = new ListDataSet();
            CellDataSet = new ListDataSet();

            GetShiftList();
        }

        public void GetShiftList()
        {
            var p = new Dictionary<string, string>();
            p.Add("MACHINE_ID", "719");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "PaperProductionTask");
            q.Request.SetParam("Action", "ListShiftByMachine");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    ShiftSelectBox.SetItems(ds, "SHIFT_ID", "SHIFT_NAME");

                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        string today = "";
                        if (DateTime.Now.Hour < 8)
                        {
                            today = $"{DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy")} 20:00:00";
                        }
                        else
                        {
                            if (DateTime.Now.Hour < 20)
                            {
                                today = $"{DateTime.Now.ToString("dd.MM.yyyy")} 08:00:00";
                            }
                            else
                            {
                                today = $"{DateTime.Now.ToString("dd.MM.yyyy")} 20:00:00";
                            }
                        }

                        var todayItem = ds.Items.FirstOrDefault(x => x.CheckGet("SHIFT_DTTM") == today);
                        if (todayItem != null)
                        {
                            ShiftSelectBox.SetSelectedItemByKey(todayItem.CheckGet("SHIFT_ID"));
                        }
                    }
                }
            }
        }

        public void TaskGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид задания",
                        Path="TASK_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Станок",
                        Path="MACHINE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Примечание",
                        Path="NOTE",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 98,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид смены",
                        Path="SHIFT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид станка",
                        Path="MACHINE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden = true,
                    },
                };
                TaskGrid.SetColumns(columns);
                TaskGrid.SetPrimaryKey("TASK_ID");
                TaskGrid.SearchText = TaskSearchBox;
                //данные грида
                TaskGrid.OnLoadItems = TaskGridLoadItems;
                TaskGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                TaskGrid.AutoUpdateInterval = 60*5;
                TaskGrid.Toolbar = TaskGridToolbar;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                TaskGrid.OnSelectItem = selectedItem =>
                {
                    ClearDependentGrids();

                    PositionGridLoadItems();
                    CurrentOperationsGridLoadItems();
                };

                TaskGrid.OnDblClick = selectedItem =>
                {
                    //if (selectedItem != null)
                    //{
                    //    EditItem();
                    //}
                };

                TaskGrid.Commands = Commander;

                TaskGrid.Init();
            }
        }

        public async void TaskGridLoadItems()
        {
            ClearDependentGrids();

            var p = new Dictionary<string, string>();
            p.Add("SHIFT_ID", ShiftSelectBox.SelectedItem.Key);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "PaperProductionTask");
            q.Request.SetParam("Action", "ListByShift");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            TaskDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    TaskDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            TaskGrid.UpdateItems(TaskDataSet);
        }

        public void PositionGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Path="TASK_POSITION_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Склад",
                        Path="SKLAD",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ячейка",
                        Path="NUM_PLACE",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Категория макулатуры",
                        Path="SCRAP_CATEGORY",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 18,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата",
                        Path="DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид накладной",
                        Path="ARRIVAL_INVOICE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Задача",
                        Path="TASK_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2 = 5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Пользователь",
                        Path="USER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 12,
                    },                   
                    new DataGridHelperColumn
                    {
                        Header="Ид смены",
                        Path="SHIFT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид станка",
                        Path="MACHINE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид задачи",
                        Path="TASK_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden = true,
                    },
                };
                PositionGrid.SetColumns(columns);
                PositionGrid.SetPrimaryKey("TASK_POSITION_ID");
                PositionGrid.SearchText = PositionSearchBox;
                //данные грида
                PositionGrid.OnLoadItems = PositionGridLoadItems;
                PositionGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                PositionGrid.AutoUpdateInterval = 0;
                PositionGrid.Toolbar = PositionGridToolbar;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                PositionGrid.OnSelectItem = selectedItem =>
                {
                };

                PositionGrid.OnDblClick = selectedItem =>
                {
                    //if (selectedItem != null)
                    //{
                    //    EditItem();
                    //}
                };

                PositionGrid.Commands = Commander;

                PositionGrid.Init();
            }
        }

        public async void PositionGridLoadItems()
        {
            var p = new Dictionary<string, string>();
            p.Add("SHIFT_ID", TaskGrid.SelectedItem.CheckGet("SHIFT_ID"));
            p.Add("MACHINE_ID", TaskGrid.SelectedItem.CheckGet("MACHINE_ID"));

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "PaperProductionTask");
            q.Request.SetParam("Action", "PositionListByShiftAndMachine");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            PositionDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    PositionDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            PositionGrid.UpdateItems(PositionDataSet);
        }

        public async void CreateTask()
        {
            var p = new Dictionary<string, string>();
            p.Add("TASK_POSITION_ID", PositionGrid.SelectedItem.CheckGet("TASK_POSITION_ID"));
            p.Add("SKLAD", PositionGrid.SelectedItem.CheckGet("SKLAD"));
            p.Add("NUM_PLACE", PositionGrid.SelectedItem.CheckGet("NUM_PLACE"));
            p.Add("MACHINE_ID", PositionGrid.SelectedItem.CheckGet("MACHINE_ID"));

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "PaperProductionTask");
            q.Request.SetParam("Action", "CreateTaskWms");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                bool succesfullFlag = false;

                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        if (ds.Items.First().CheckGet("TASK_POSITION_ID").ToInt() > 0)
                        {
                            succesfullFlag = true;
                        }
                    }
                }

                if (!succesfullFlag)
                {
                    string msg = $"При создании задачи произошла ошибка. Пожалуйста, сообщите о проблеме.";
                    var d = new DialogWindow($"{msg}", "ПЗ БДМ", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        public void CurrentOperationsGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид операции",
                        Path="OPERATION_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Операция",
                        Path="OPERATION_TYPE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 21,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Склад",
                        Path="SKLAD",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ячейка",
                        Path="NUM_PLACE",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата создания",
                        Path="DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид типа операции",
                        Path="OPERATION_TYPE_ID",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 5,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид транспорта с макулатурой",
                        Path="TRANSPORT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид станка",
                        Path="MACHINE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden = true,
                    },
                };
                CurrentOperationsGrid.SetColumns(columns);
                CurrentOperationsGrid.SetPrimaryKey("OPERATION_ID");
                CurrentOperationsGrid.SearchText = CurrentOperationsSearchBox;
                //данные грида
                CurrentOperationsGrid.OnLoadItems = CurrentOperationsGridLoadItems;
                CurrentOperationsGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                CurrentOperationsGrid.AutoUpdateInterval = 0;
                CurrentOperationsGrid.Toolbar = CurrentOperationsGridToolbar;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                CurrentOperationsGrid.OnSelectItem = selectedItem =>
                {
                };

                CurrentOperationsGrid.OnDblClick = selectedItem =>
                {
                    //if (selectedItem != null)
                    //{
                    //    EditItem();
                    //}
                };

                CurrentOperationsGrid.Commands = Commander;

                CurrentOperationsGrid.Init();
            }
        }

        public async void CurrentOperationsGridLoadItems()
        {
            var p = new Dictionary<string, string>();
            p.Add("MACHINE_ID", TaskGrid.SelectedItem.CheckGet("MACHINE_ID"));

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "PaperProductionTask");
            q.Request.SetParam("Action", "ListCurrentOperationByMachine");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            CurrentOperationsDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    CurrentOperationsDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            CurrentOperationsGrid.UpdateItems(CurrentOperationsDataSet);
        }

        public void CellGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Склад",
                        Path="SKLAD",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ячейка",
                        Path="NUM_PLACE",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Категория макулатуры",
                        Path="CATEGORY",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 18,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Станок",
                        Path="MACHINE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 7,
                    },
                };
                CellGrid.SetColumns(columns);
                CellGrid.SetPrimaryKey("_ROWNUMBER");
                CellGrid.SearchText = CellSearchBox;
                //данные грида
                CellGrid.OnLoadItems = CellGridLoadItems;
                CellGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                CellGrid.AutoUpdateInterval = 60*5;
                CellGrid.Toolbar = CellGridToolbar;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                CellGrid.OnSelectItem = selectedItem =>
                {
                };

                CellGrid.OnDblClick = selectedItem =>
                {
                    //if (selectedItem != null)
                    //{
                    //    EditItem();
                    //}
                };

                CellGrid.Commands = Commander;

                CellGrid.Init();
            }
        }

        public async void CellGridLoadItems()
        {
            var p = new Dictionary<string, string>();
            p.Add("PRODUCT_TYPE", "3");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "PaperProductionTask");
            q.Request.SetParam("Action", "ListCellByProductType");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            CellDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    CellDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            CellGrid.UpdateItems(CellDataSet);
        }

        public void ShowCell(Dictionary<string, string> selectedItem)
        {
            if (selectedItem != null && selectedItem.Count > 0)
            {
                string sklad = selectedItem.CheckGet("SKLAD");
                int numPlace = selectedItem.CheckGet("NUM_PLACE").ToInt();

                var cellItem = CellGrid.Items.FirstOrDefault(x => x.CheckGet("SKLAD") == sklad && x.CheckGet("NUM_PLACE").ToInt() == numPlace);
                if (cellItem != null && cellItem.Count > 0)
                {
                    CellGrid.SelectRowByKey(cellItem.CheckGet("_ROWNUMBER"));
                }               
            }
        }

        public void Refresh()
        {
            TaskGrid.LoadItems();
        }

        public void ClearDependentGrids()
        {
            if (PositionGrid != null)
            {
                PositionGrid.ClearItems();
            }

            if (CurrentOperationsGrid != null)
            {
                CurrentOperationsGrid.ClearItems();
            }
        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            //// Проверяем уровень доступа
            //var mode = Central.Navigator.GetRoleLevel("[erp]warehouse_control");
            //switch (mode)
            //{
            //    // Если уровень доступа -- "Спецправа",
            //    case Role.AccessMode.Special:
            //        break;

            //    case Role.AccessMode.AllowAll:
            //        break;

            //    // Если уровень доступа -- "Только чтение",
            //    case Role.AccessMode.ReadOnly:
            //        break;

            //    default:
            //        break;
            //}
        }

        private void ShiftSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Refresh();
        }
    }
}
