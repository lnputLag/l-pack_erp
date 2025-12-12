using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Stock;
using Client.Interfaces.Stock._WaterhouseControl;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
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
using Xceed.Wpf.Toolkit.Primitives;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.MoldedContainer
{
    /// <summary>
    /// Интерфейс управления списанием макулатуры в производство
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class MoldedContainerConsumptionToProduction : ControlBase
    {
        public MoldedContainerConsumptionToProduction()
        {
            ControlTitle = "Списание в производство";
            DocumentationUrl = "/doc/l-pack-erp-new/lt/write-off_lt";
            RoleName = "[erp]molded_contnr_consumpt_task";
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
                SetDefaults();
                TaskGridInit();
                ConsumptionGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                TaskGrid.Destruct();
                ConsumptionGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                TaskGrid.ItemsAutoUpdate = true;
                TaskGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                TaskGrid.ItemsAutoUpdate = false;
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
                    ButtonControl = RefreshButton,
                    ButtonName = "RefreshButton",
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

            Commander.SetCurrentGridName("TaskGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "create_task_consumption_to_production",
                    Title = "Создать задачу",
                    Description = "Создать задачу на списание макулатуры в производство",
                    Group = "task_grid_default",
                    ButtonUse = true,
                    ButtonControl = CreateTaskConsumptionToProductionButton,
                    ButtonName = "CreateTaskConsumptionToProductionButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = true,
                    Action = () =>
                    {
                        CreateTaskConsumptionToProduction();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "accept_task_consumption_to_production",
                    Title = "Принять задачу",
                    Description = "Принять задачу на списание макулатуры в производство",
                    Group = "task_grid_default",
                    MenuUse = true,
                    Enabled = false,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        AcceptTaskConsumptionToProduction();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (TaskGrid != null && TaskGrid.SelectedItem != null && TaskGrid.SelectedItem.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(TaskGrid.SelectedItem.CheckGet("TASK_ID"))
                                && TaskGrid.SelectedItem.CheckGet("TASK_STATUS_ID").ToInt() == 1)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "execute_task_consumption_to_production",
                    Title = "Выполнить задачу",
                    Description = "Выполнить задачу на списание макулатуры в производство",
                    Group = "task_grid_default",
                    MenuUse = true,
                    Enabled = false,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        ExecuteTaskConsumptionToProduction();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (TaskGrid != null && TaskGrid.SelectedItem != null && TaskGrid.SelectedItem.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(TaskGrid.SelectedItem.CheckGet("TASK_ID"))
                                && TaskGrid.SelectedItem.CheckGet("TASK_STATUS_ID").ToInt() == 2)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "complete_task_consumption_to_production",
                    Title = "Завершить задачу",
                    Description = "Завершить задачу на списание макулатуры в производство",
                    Group = "task_grid_default",
                    MenuUse = true,
                    Enabled = false,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        СompleteTaskConsumptionToProduction();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (TaskGrid != null && TaskGrid.SelectedItem != null && TaskGrid.SelectedItem.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(TaskGrid.SelectedItem.CheckGet("TASK_ID"))
                                && TaskGrid.SelectedItem.CheckGet("TASK_STATUS_ID").ToInt() == 2)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "cancel_task_consumption_to_production",
                    Title = "Отменить задачу",
                    Description = "Отменить задачу на списание макулатуры в производство",
                    Group = "task_grid_default",
                    MenuUse = true,
                    Enabled = false,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        CancelTaskConsumptionToProduction();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (TaskGrid != null && TaskGrid.SelectedItem != null && TaskGrid.SelectedItem.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(TaskGrid.SelectedItem.CheckGet("TASK_ID"))
                                && TaskGrid.SelectedItem.CheckGet("TASK_STATUS_ID").ToInt() == 2)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "edit_description_to_task",
                    Title = "Изменить примечание",
                    Description = "Добавить примечание к задаче на списание макулатуры в производство",
                    Group = "task_grid_default",
                    MenuUse = true,
                    Enabled = false,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        EditDescriptionToTask();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (TaskGrid != null && TaskGrid.SelectedItem != null && TaskGrid.SelectedItem.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(TaskGrid.SelectedItem.CheckGet("TASK_ID")))
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
            }

            Commander.SetCurrentGridName("ConsumptionGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "cancel_consumption",
                    Title = "Отменить списание",
                    Description = "Отменить списание выбранной позиции",
                    Group = "consumption_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = CancelConsumptionButton,
                    ButtonName = "CancelConsumptionButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        CancelConsumption();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ConsumptionGrid != null && ConsumptionGrid.Items != null && ConsumptionGrid.Items.Count > 0)
                        {
                            if (ConsumptionGrid.SelectedItem != null && ConsumptionGrid.SelectedItem.Count > 0)
                            {
                                if (TaskGrid != null && TaskGrid.SelectedItem != null && TaskGrid.SelectedItem.Count > 0)
                                {
                                    if (TaskGrid.SelectedItem.CheckGet("TASK_STATUS_ID").ToInt() != 4 || 
                                        (TaskGrid.SelectedItem.CheckGet("TASK_STATUS_ID").ToInt() == 4 && Central.DebugMode))
                                    {
                                        result = true;
                                    }
                                }
                            }
                        }

                        return result;
                    },
                });
            }

            Commander.Init(this);
        }

        private ListDataSet TaskDataSet { get; set; }

        private ListDataSet ConsumptionDataSet { get; set; }

        public void SetDefaults()
        {
            TaskDataSet = new ListDataSet();
            ConsumptionDataSet = new ListDataSet();
        }

        public void Refresh()
        {
            TaskGrid.LoadItems();
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
                        Header="Ид",
                        Description = "Идентификатор задачи",
                        Path="TASK_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Создана",
                        Path="CREATED_DTTM",
                        Description = "Дата создания задачи",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Принята",
                        Description = "Дата принятия задачи",
                        Path="ACCEPTED_DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Выполнена",
                        Path="COMPLETED_DTTM",
                        Description = "Дата выполнения задачи",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Название",
                        Description = "Название задачи",
                        Path="TASK_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 39,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус",
                        Description = "Статус задачи",
                        Path="TASK_STATUS_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Списано, шт",
                        Description = "Количество списанных ТМЦ по этой задаче",
                        Path="CONSUMPTION_COUNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Примечание",
                        Description = "Примечание задачи",
                        Path="TASK_DESCRIPTION",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 38,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Из ячейки",
                        Description = "Ячейка источник",
                        Path="FROM_STORAGE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Создатель",
                         Description = "Создатель задачи",
                        Path="ACCOUNT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 11,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Краткое название",
                        Description = "Краткое название задачи",
                        Path="TASK_SHORT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 26,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Внешний Ид",
                        Description = "Внешний идентификатор задачи",
                        Path="TASK_OUTER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Тип",
                        Description = "Тип задачи",
                        Path="TASK_TYPE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 28,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Зона",
                        Description = "Зона задачи",
                        Path="ZONE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 14,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид акаунта",
                         Description = "Идентификатор акаунта создателя задачи",
                        Path="ACCOUNT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид ячейки источника",
                        Description = "Идентификатор хранилища источника",
                        Path="FROM_STORAGE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид статуса",
                        Path="TASK_STATUS_ID",
                        Description = "Идентификатор статуса задачи",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид типа",
                        Description = "Идентификатор типа задачи",
                        Path="TASK_TYPE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид зоны",
                        Description = "Идентификатор зоны задачи",
                        Path="ZONE_ID",
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
                TaskGrid.AutoUpdateInterval = 60 * 5;
                TaskGrid.Toolbar = TaskGridToolbar;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                TaskGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem != null && selectedItem.Count > 0)
                    {
                        if (TaskGrid != null && TaskGrid.Items != null && TaskGrid.Items.Count > 0)
                        {
                            if (TaskGrid.Items.FirstOrDefault(x => x.CheckGet("TASK_ID").ToInt() == selectedItem.CheckGet("TASK_ID").ToInt()) == null)
                            {
                                TaskGrid.SelectRowFirst();
                            }
                        }

                        ConsumptionGridLoadItems();
                    }
                };

                TaskGrid.OnFilterItems = TaskGridFilterItems;

                TaskGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    // определение цветов фона строк
                    {
                        StylerTypeRef.BackgroundColor,
                        (Dictionary<string, string> row) =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            // задача принята
                            if (row.CheckGet("TASK_STATUS_ID").ToInt() == 2)
                            {
                                color = HColor.Blue;
                            }

                            // задача отменена
                            if (row.CheckGet("TASK_STATUS_ID").ToInt() == 3)
                            {
                                color = HColor.Red;
                            }

                            // задача выполнена
                            if (row.CheckGet("TASK_STATUS_ID").ToInt() == 4)
                            {
                                color = HColor.Green;
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },
                };

                TaskGrid.Commands = Commander;

                TaskGrid.Init();
            }
        }

        public async void TaskGridLoadItems()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "ConsumptionToProduction");
            q.Request.SetParam("Action", "TaskList");
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

        public void TaskGridFilterItems()
        {
            ConsumptionGrid.ClearItems();

            if (TaskGrid != null && TaskGrid.SelectedItem != null && TaskGrid.SelectedItem.Count > 0)
            {
                TaskGrid.Commands.Message = new ItemMessage() { Action = "refresh", Message = $"{TaskGrid.SelectedItem.CheckGet("TASK_ID")}" };
            }
        }

        /// <summary>
        /// Создать задачу на списание макулатуры в производство
        /// </summary>
        public void CreateTaskConsumptionToProduction()
        {
            var selectCell = new SelectCell();
            selectCell.OnSelectedCell += SelectCellForCreateTask;
            selectCell.WarehouseSelectBox.SetSelectedItemByKey("4");
            selectCell.StorageAreaSelectBox.SetSelectedItemByKey("14");
            selectCell.Show();
        }

        public void SelectCellForCreateTask(Dictionary<string, string> storageGridSelectedItem)
        {
            if (storageGridSelectedItem.CheckGet("WMST_ID").ToInt() > 0)
            {
                if (new DialogWindow($"Хотите создать задачу на списание в производство из ячейки {storageGridSelectedItem.CheckGet("NUM")}?", "Сообщение", "", DialogWindowButtons.YesNo).ShowDialog() == true)
                {
                    string comment = "";
                    {
                        var i = new ComplectationCMQuantity("", true, false);
                        i.Show("Примечание к задаче");
                        if (i.OkFlag)
                        {
                            comment = i.QtyString;
                        }
                    }

                    var p = new Dictionary<string, string>();
                    {
                        p.Add("WMST_ID", storageGridSelectedItem["WMST_ID"]);
                        p.Add("DESCRIPTION", comment);
                    }

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Warehouse");
                    q.Request.SetParam("Object", "Task");
                    q.Request.SetParam("Action", "CreateConsumptionToProduction");
                    q.Request.SetParams(p);

                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            if (ds != null && ds.Items != null && ds.Items.Count > 0)
                            {
                                if (ds.Items[0].CheckGet("ID").ToInt() != 0)
                                {
                                    DialogWindow.ShowDialog("Не удалось создать задачу на списание макулатуры в производство!");
                                }
                                else
                                {
                                    Refresh();
                                }
                            }
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
            }
        }

        /// <summary>
        /// Выполнить задачу на списание макулатуры в производство
        /// </summary>
        public void ExecuteTaskConsumptionToProduction()
        {
            int baleCount = 0;
            var i = new ComplectationCMQuantity(0);
            i.Show("Количество ТМЦ");
            if (i.OkFlag)
            {
                baleCount = i.QtyInt;
            }

            if (baleCount > 0)
            {
                if (new DialogWindow($"Хотите списать {baleCount} ТМЦ, находящееся в ячейке {TaskGrid.SelectedItem["FROM_STORAGE_NAME"]} в производство?", "Сообщение", "", DialogWindowButtons.YesNo).ShowDialog() == true)
                {
                    var p = new Dictionary<string, string>();
                    {
                        p.Add("WMTA_ID", TaskGrid.SelectedItem["TASK_ID"]);
                        p.Add("BALE_CNT", $"{baleCount}");
                    }

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Warehouse");
                    q.Request.SetParam("Object", "Task");
                    q.Request.SetParam("Action", "ExecuteConsumptionToProduction");
                    q.Request.SetParams(p);

                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            if (ds != null && ds.Items != null && ds.Items.Count > 0)
                            {
                                if (ds.Items[0].CheckGet("ID").ToInt() != 0)
                                {
                                    DialogWindow.ShowDialog($"Не удалось списать {baleCount} ТМЦ в производство!");
                                }
                                else
                                {
                                    Refresh();
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Принять задачу на списание макулатуры в производство
        /// </summary>
        public void AcceptTaskConsumptionToProduction()
        {
            UpdateStateTaskConsumptionToProduction(2);
        }

        /// <summary>
        /// Завершить задачу на списание макулатуры в производство
        /// </summary>
        public void СompleteTaskConsumptionToProduction()
        {
            UpdateStateTaskConsumptionToProduction(4);
        }

        /// <summary>
        /// Отменить задачу на списание макулатуры в производство
        /// </summary>
        public void CancelTaskConsumptionToProduction()
        {
            UpdateStateTaskConsumptionToProduction(3);
        }

        /// <summary>
        /// Изменить статус задачи на списание макулатуры в производство
        /// </summary>
        /// <param name="stateId"></param>
        public void UpdateStateTaskConsumptionToProduction(int stateId)
        {
            string stateName = "";
            if (stateId == 2)
            {
                stateName = "принять";
            }
            else if (stateId == 4)
            {
                stateName = "завершить";
            }
            else if (stateId == 3)
            {
                stateName = "отменить";
            }

            if (new DialogWindow($"Хотите {stateName} задачу # {TaskGrid.SelectedItem["TASK_ID"].ToInt()} по списанию в производство из ячейки {TaskGrid.SelectedItem["FROM_STORAGE_NAME"]}?", "Сообщение", "", DialogWindowButtons.YesNo).ShowDialog() == true)
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("WMTA_ID", TaskGrid.SelectedItem["TASK_ID"]);
                    p.Add("WMOS_ID", $"{stateId}");
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "Task");
                q.Request.SetParam("Action", "UpdateStateConsumptionToProduction");
                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                        {
                            if (ds.Items[0].CheckGet("ID").ToInt() != 0)
                            {
                                DialogWindow.ShowDialog($"Не удалось {stateName} данную задачу!");
                            }
                            else
                            {
                                Refresh();
                            }
                        }
                    }
                }
            }
        }

        public void EditDescriptionToTask()
        {
            string comment = TaskGrid.SelectedItem["TASK_DESCRIPTION"];
            {
                var i = new ComplectationCMQuantity(comment, true, false);
                i.Show("Примечание к задаче");
                if (i.OkFlag)
                {
                    comment = i.QtyString;
                }
            }

            var p = new Dictionary<string, string>();
            {
                p.Add("WMTA_ID", TaskGrid.SelectedItem["TASK_ID"]);
                p.Add("DESCRIPTION", comment);
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "Task");
            q.Request.SetParam("Action", "UpdateDescription");
            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        if (ds.Items[0].CheckGet("WMTA_ID").ToInt() > 0)
                        {
                            Refresh();
                        }
                        else
                        {
                            DialogWindow.ShowDialog("Не удалось обновить примечание к задаче!");
                        }
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        public void ConsumptionGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header = "*",
                        Path = "_SELECTED",
                        ColumnType = ColumnTypeRef.Boolean,
                        Width2=3,
                        Editable = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД списания",
                        Path="CONSUMPTION_ID",
                        Description="Ид списания складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата",
                        Path="CONSUMPTION_DATE",
                        Description="Дата списания складской единицы",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2 = 15,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Path="QUANTITY",
                        Description="Списанное количество",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width2 = 5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Складская единица",
                        Path="ITEM_NAME",
                        Description="Наименование списанной складской единицы",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 15,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Хранилище",
                        Path="STORAGE_NAME",
                        Description="Наименование хранилища, откуда была списана складская единица",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД складской единицы",
                        Path="ITEM_ID",
                        Description="Ид списанной складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД прихода",
                        Path="INCOMING_ID",
                        Description="Ид прихода складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Склад",
                        Path="WAREHOUSE_NAME",
                        Description="Наименование склада, откуда была списана складская единица",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 13,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Зона",
                        Path="ZONE_NAME",
                        Description="Наименование зоны, откуда была списана складская единица",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 16,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД внешний",
                        Path="OUTER_ID",
                        Description="Внешний Ид складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД хранилища",
                        Path="STORAGE_ID",
                        Description="Ид хранилища, откуда была списана складская единица",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 12,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД склада",
                        Path="WAREHOUSE_ID",
                        Description="Ид склада, откуда была списана складская единица",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 9,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД зоны",
                        Path="ZONE_ID",
                        Description="Ид зоны, откуда была списана складская единица",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 7,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД задачи",
                        Path="TASK_ID",
                        Description="Ид задачи, по которой была списана складская единица",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 7,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование задачи",
                        Path="TASK_NAME",
                        Description="Наименование задачи, по которой была списана складская единица",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 7,
                        Hidden=true,
                    },
                };
                ConsumptionGrid.SetColumns(columns);
                ConsumptionGrid.SetPrimaryKey("CONSUMPTION_ID");
                ConsumptionGrid.SearchText = ConsumptionSearchBox;
                //данные грида
                ConsumptionGrid.OnLoadItems = ConsumptionGridLoadItems;
                ConsumptionGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                ConsumptionGrid.AutoUpdateInterval = 0;
                ConsumptionGrid.ItemsAutoUpdate = false;
                ConsumptionGrid.Toolbar = ConsumptionGridToolbar;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                ConsumptionGrid.OnSelectItem = selectedItem =>
                {
                    if (ConsumptionGrid != null && ConsumptionGrid.Items != null && ConsumptionGrid.Items.Count > 0)
                    {
                        if (ConsumptionGrid.Items.FirstOrDefault(x => x.CheckGet("CONSUMPTION_ID").ToInt() == selectedItem.CheckGet("CONSUMPTION_ID").ToInt()) == null)
                        {
                            ConsumptionGrid.SelectRowFirst();
                        }
                    }
                };

                ConsumptionGrid.Commands = Commander;

                ConsumptionGrid.Init();
            }
        }

        public async void ConsumptionGridLoadItems()
        {
            if (TaskGrid != null && TaskGrid.SelectedItem != null && TaskGrid.SelectedItem.Count > 0)
            {
                var p = new Dictionary<string, string>();
                p.Add("WMTA_ID", TaskGrid.SelectedItem["TASK_ID"]);

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "Consumption");
                q.Request.SetParam("Action", "ListByTask");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                ConsumptionDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        ConsumptionDataSet = ListDataSet.Create(result, "ITEMS");
                    }
                }
                ConsumptionGrid.UpdateItems(ConsumptionDataSet);
            }
        }

        public void CancelConsumption()
        {
            if (ConsumptionGrid != null && ConsumptionGrid.Items != null && ConsumptionGrid.Items.Count > 0)
            {
                var checkedRowList = ConsumptionGrid.GetItemsSelected();
                if (checkedRowList != null && checkedRowList.Count > 0)
                {
                    if (checkedRowList.Count > 1)
                    {
                        if (new DialogWindow($"Хотите отменить списание {checkedRowList.Count} позиций?", "Сообщение", "", DialogWindowButtons.YesNo).ShowDialog() == true)
                        {
                            bool errorFlag = false;

                            foreach (var checkedRow in checkedRowList)
                            {
                                var p = new Dictionary<string, string>();
                                p.Add("WMCO_ID", checkedRow.CheckGet("CONSUMPTION_ID"));

                                var q = new LPackClientQuery();
                                q.Request.SetParam("Module", "Warehouse");
                                q.Request.SetParam("Object", "Consumption");
                                q.Request.SetParam("Action", "Cancel");
                                q.Request.SetParams(p);

                                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                                q.DoQuery();

                                if (q.Answer.Status == 0)
                                {
                                    bool succesfullFlag = false;

                                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                                    if (result != null)
                                    {
                                        var ds = ListDataSet.Create(result, "ITEMS");
                                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                                        {
                                            if (ds.Items.First().CheckGet("ID").ToInt() == 0)
                                            {
                                                succesfullFlag = true;
                                            }
                                        }
                                    }

                                    if (!succesfullFlag)
                                    {
                                        errorFlag = true;
                                    }
                                }
                                else
                                {
                                    errorFlag = true;
                                }
                            }

                            if (errorFlag)
                            {
                                DialogWindow.ShowDialog($"При выполнении отмены списания произошла ошибка. Пожалуйста, сообщите о проблеме.");
                            }

                            Refresh();
                        }
                    }
                    else
                    {
                        var checkedRow = checkedRowList.First();
                        if (new DialogWindow($"Хотите отменить списание # {checkedRow["CONSUMPTION_ID"].ToInt()} позиции # {checkedRow["ITEM_ID"].ToInt()} {checkedRow["ITEM_NAME"]} из ячейки {checkedRow["STORAGE_NAME"]}?", "Сообщение", "", DialogWindowButtons.YesNo).ShowDialog() == true)
                        {
                            var p = new Dictionary<string, string>();
                            p.Add("WMCO_ID", checkedRow.CheckGet("CONSUMPTION_ID"));

                            var q = new LPackClientQuery();
                            q.Request.SetParam("Module", "Warehouse");
                            q.Request.SetParam("Object", "Consumption");
                            q.Request.SetParam("Action", "Cancel");
                            q.Request.SetParams(p);

                            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                            q.DoQuery();

                            if (q.Answer.Status == 0)
                            {
                                bool succesfullFlag = false;

                                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                                if (result != null)
                                {
                                    var ds = ListDataSet.Create(result, "ITEMS");
                                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                                    {
                                        if (ds.Items.First().CheckGet("ID").ToInt() == 0)
                                        {
                                            succesfullFlag = true;
                                        }
                                    }
                                }

                                if (!succesfullFlag)
                                {
                                    DialogWindow.ShowDialog($"При выполнении отмены списания произошла ошибка. Пожалуйста, сообщите о проблеме.");
                                }
                                else
                                {
                                    Refresh();
                                }
                            }
                            else
                            {
                                q.ProcessError();
                            }
                        }
                    }
                }
                else
                {
                    if (ConsumptionGrid.SelectedItem != null && ConsumptionGrid.SelectedItem.Count > 0)
                    {
                        if (new DialogWindow($"Хотите отменить списание # {ConsumptionGrid.SelectedItem["CONSUMPTION_ID"].ToInt()} позиции # {ConsumptionGrid.SelectedItem["ITEM_ID"].ToInt()} {ConsumptionGrid.SelectedItem["ITEM_NAME"]} из ячейки {ConsumptionGrid.SelectedItem["STORAGE_NAME"]}?", "Сообщение", "", DialogWindowButtons.YesNo).ShowDialog() == true)
                        {
                            var p = new Dictionary<string, string>();
                            p.Add("WMCO_ID", ConsumptionGrid.SelectedItem.CheckGet("CONSUMPTION_ID"));

                            var q = new LPackClientQuery();
                            q.Request.SetParam("Module", "Warehouse");
                            q.Request.SetParam("Object", "Consumption");
                            q.Request.SetParam("Action", "Cancel");
                            q.Request.SetParams(p);

                            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                            q.DoQuery();

                            if (q.Answer.Status == 0)
                            {
                                bool succesfullFlag = false;

                                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                                if (result != null)
                                {
                                    var ds = ListDataSet.Create(result, "ITEMS");
                                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                                    {
                                        if (ds.Items.First().CheckGet("ID").ToInt() == 0)
                                        {
                                            succesfullFlag = true;
                                        }
                                    }
                                }

                                if (!succesfullFlag)
                                {
                                    DialogWindow.ShowDialog($"При выполнении отмены списания произошла ошибка. Пожалуйста, сообщите о проблеме.");
                                }
                                else
                                {
                                    Refresh();
                                }
                            }
                            else
                            {
                                q.ProcessError();
                            }
                        }
                    }
                }
            }
        }
    }
}
