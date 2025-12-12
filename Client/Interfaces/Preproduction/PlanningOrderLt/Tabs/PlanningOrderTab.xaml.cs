using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction.PlanningOrderLt.Elements;
using Newtonsoft.Json;

namespace Client.Interfaces.Preproduction.PlanningOrderLt.Tabs
{
    /// <summary>
    /// Интерфейс для управления планированием на ЛТ
    /// </summary>
    /// <author>volkov_as</author>
    public partial class PlanningOrderTab : ControlBase
    {
        public PlanningOrderTab()
        {
            InitializeComponent();

            ControlSection = "planning_lt";
            RoleName = "[erp]planning_order_lt";
            ControlTitle = "Планирование ПЗ на ЛТ";

            OnLoad = () =>
            {
                FormInit();
                GridInitializer.IniGrid(FirstGrid, LoadItemsForMachine, PanelToolbar, Commander);
                GridInitializer.IniGrid(ThreeGrid, LoadItemsForMachine2, PanelToolbar2, Commander, true);
                GridInitializer.InitGridTaskList(SecondGrid, LoadItemsTaskList, SecondGridToolbar, Commander);
            };

            OnUnload = () =>
            {
                FirstGrid.Destruct();
                SecondGrid.Destruct();
                ThreeGrid.Destruct();
            };

            OnFocusGot = () =>
            {
                FirstGrid.ItemsAutoUpdate = true;
                SecondGrid.ItemsAutoUpdate = true;
                ThreeGrid.ItemsAutoUpdate = true;
            };

            OnFocusLost = () =>
            {
                FirstGrid.ItemsAutoUpdate = false;
                SecondGrid.ItemsAutoUpdate = false;
                ThreeGrid.ItemsAutoUpdate = false;
            };

            OnKeyPressed = (e) =>
            {
                if (!e.Handled)
                {
                    if (e.Key == System.Windows.Input.Key.F5)
                    {
                        FirstGrid.LoadItems();
                        SecondGrid.LoadItems();
                        ThreeGrid.LoadItems();
                    }
                }
            };

            {
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "help",
                        Enabled = true,
                        Title = "Справка",
                        Description = "Показать справочную информацию",
                        ButtonUse = true,
                        ButtonName = "HelpButton",
                        HotKey = "F1",
                        Action = () => { Central.ShowHelp(DocumentationUrl); },
                    });
                }
                Commander.SetCurrentGridName("FirstGrid");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "up",
                        Enabled = true,
                        Title = "Выше",
                        Description = "Поднять в очереди",
                        ButtonUse = true,
                        ButtonName = "UpButton",
                        MenuUse = true,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var row = FirstGrid.SelectedItem;
                            var index = row.CheckGet("TASK_ID").ToInt();
                            ChangeNumPosition(0, FirstGrid, FirstGridItems);
                            FirstGrid.SelectRowByKey(row.CheckGet(PrimaryKey));
                        },
                        CheckEnabled = () =>
                        {
                            var row = FirstGrid.SelectedItem;
                            var status = row.CheckGet("PRTS_ID").ToInt();
                            if (FirstGridItems != null)
                            {
                                var index = FirstGridItems.Items.FindIndex(x =>
                                    x.CheckGet("TASK_ID").ToInt() == row.CheckGet("TASK_ID").ToInt());

                                if (status != 4 && status != 8 && status != 3 && index > 0)
                                {
                                    if (FirstGridItems.Items[index - 1].CheckGet("PRTS_ID").ToInt() != 4 &&
                                        FirstGridItems.Items[index - 1].CheckGet("PRTS_ID").ToInt() != 8 &&
                                        FirstGridItems.Items[index - 1].CheckGet("PRTS_ID").ToInt() != 3)
                                    {
                                        return true;
                                    }
                                }
                            }



                            return false;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "down",
                        Enabled = true,
                        Title = "Ниже",
                        Description = "Опустить в очереди",
                        ButtonUse = true,
                        ButtonName = "DownButton",
                        MenuUse = true,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var row = FirstGrid.SelectedItem;
                            var index = row.CheckGet("TASK_ID").ToInt();
                            ChangeNumPosition(1, FirstGrid, FirstGridItems);
                            FirstGrid.SelectRowByKey(row.CheckGet(PrimaryKey));
                        },
                        CheckEnabled = () =>
                        {
                            var row = FirstGrid.SelectedItem;
                            var status = row.CheckGet("PRTS_ID").ToInt();

                            if (FirstGridItems != null)
                            {
                                var index = FirstGridItems.Items.FindIndex(x =>
                                    x.CheckGet("TASK_ID").ToInt() == row.CheckGet("TASK_ID").ToInt());

                                if (status != 4 && status != 8 && status != 3 && index + 1 < FirstGrid.Items.Count)
                                {
                                    return true;
                                }
                            }

                            return false;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "delete",
                        Enabled = true,
                        Title = "Удалить",
                        Description = "Удалить из очереди",
                        ButtonUse = true,
                        ButtonName = "DeleteButton",
                        MenuUse = true,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var row = FirstGrid.SelectedItem;
                            var index = row.CheckGet("TASK_ID").ToInt();
                            var dialog = new DialogWindow($"Удалить задачу - №{index} из плана?", "Удаление задачи", "",
                                DialogWindowButtons.YesNo);

                            dialog.ShowDialog();

                            if (dialog.DialogResult == true)
                            {
                                DeleteFromPlan(index, FirstGrid, FirstGridItems);
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var row = FirstGrid.SelectedItem;
                            var status = row.CheckGet("PRTS_ID").ToInt();

                            if (status == 4 || status == 8 || status == 3)
                            {
                                return false;
                            }

                            if (row.CheckGet("DOPL_ID").ToInt() > 0)
                            {
                                return false;
                            }

                            if (FirstGrid.Items.Count == 0)
                            {
                                return false;
                            }

                            return true;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "refresh_first_grid",
                        Enabled = true,
                        Title = "Обновить",
                        ButtonUse = true,
                        ButtonName = "RefreshButton",
                        MenuUse = true,
                        Action = () =>
                        {
                            FirstGrid.LoadItems();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "save_start_dttm",
                        Enabled = true,
                        Title = "Корректировка даты начала",
                        ButtonUse = true,
                        ButtonName = "SaveButton",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () => SaveStartDateTime(FirstGrid)
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "show_tech_map",
                        Enabled = true,
                        Title = "Показать ТК",
                        ButtonUse = true,
                        MenuUse = true,
                        ButtonName = "ShowTechMap",
                        Action = () => TechnologicalMapShow(FirstGrid),
                        CheckEnabled = () =>
                        {
                            var row = FirstGrid.SelectedItem.CheckGet("PATHTK");

                            if (!string.IsNullOrEmpty(row))
                            {
                                return true;
                            }

                            return false;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "return_to_plan",
                        Enabled = true,
                        Title = "Вернуться к плану",
                        ButtonUse = true,
                        ButtonName = "ReturnToPlan",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var taskId = FirstGrid.SelectedItem.CheckGet("TASK_ID").ToInt();

                            ReturnTaskToPlan(taskId, 1);
                        },
                        CheckEnabled = () =>
                        {
                            var row = FirstGrid.SelectedItem;
                            var status = row.CheckGet("PRTS_ID").ToInt();

                            if (status == 8 || status == 3)
                            {
                                return true;
                            }

                            return false;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "export_to_excel",
                        Enabled = true,
                        Title = "Экспорт в Excel",
                        MenuUse = true,
                        AccessLevel = Role.AccessMode.ReadOnly,
                        Action = FirstGrid.ItemsExportExcel
                    });
                }
                Commander.SetCurrentGridName("ThreeGrid");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "up2",
                        Enabled = true,
                        Title = "Выше",
                        Description = "Поднять в очереди",
                        ButtonUse = true,
                        ButtonName = "UpButton2",
                        MenuUse = true,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var row = ThreeGrid.SelectedItem;
                            var index = row.CheckGet("TASK_ID").ToInt();
                            ChangeNumPosition(0, ThreeGrid, ThreeGridItems);
                            ThreeGrid.SelectRowByKey(row.CheckGet(PrimaryKey));
                        },
                        CheckEnabled = () =>
                        {
                            var row = ThreeGrid.SelectedItem;
                            var status = row.CheckGet("PRTS_ID").ToInt();
                            if (ThreeGridItems != null)
                            {
                                var index = ThreeGridItems.Items.FindIndex(x =>
                                    x.CheckGet("TASK_ID").ToInt() == row.CheckGet("TASK_ID").ToInt());

                                if (status != 4 && status != 8 && status != 3 && index > 0)
                                {
                                    if (ThreeGridItems.Items[index - 1].CheckGet("PRTS_ID").ToInt() != 4 &&
                                        ThreeGridItems.Items[index - 1].CheckGet("PRTS_ID").ToInt() != 8 &&
                                        ThreeGridItems.Items[index - 1].CheckGet("PRTS_ID").ToInt() != 3)
                                    {
                                        return true;
                                    }
                                }
                            }
                            return false;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "down2",
                        Enabled = true,
                        Title = "Ниже",
                        Description = "Опустить в очереди",
                        ButtonUse = true,
                        ButtonName = "DownButton2",
                        MenuUse = true,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var row = ThreeGrid.SelectedItem;
                            var index = row.CheckGet("TASK_ID").ToInt();
                            ChangeNumPosition(1, ThreeGrid, ThreeGridItems);
                            ThreeGrid.SelectRowByKey(row.CheckGet(PrimaryKey));
                        },
                        CheckEnabled = () =>
                        {
                            var row = ThreeGrid.SelectedItem;
                            var status = row.CheckGet("PRTS_ID").ToInt();

                            if (ThreeGridItems != null)
                            {
                                var index = ThreeGridItems.Items.FindIndex(x =>
                                    x.CheckGet("TASK_ID").ToInt() == row.CheckGet("TASK_ID").ToInt());

                                if (status != 4 && status != 8 && status != 3 && index + 1 < ThreeGridItems.Items.Count)
                                {
                                    return true;
                                }
                            }

                            return false;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "delete2",
                        Enabled = true,
                        Title = "Удалить",
                        Description = "Удалить из очереди",
                        ButtonUse = true,
                        ButtonName = "DeleteButton2",
                        MenuUse = true,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var row = ThreeGrid.SelectedItem;
                            var index = row.CheckGet("TASK_ID").ToInt();
                            var dialog = new DialogWindow($"Удалить задачу - №{index} из плана?", "Удаление задачи", "",
                                DialogWindowButtons.YesNo);

                            dialog.ShowDialog();

                            if (dialog.DialogResult == true)
                            {
                                DeleteFromPlan(index, ThreeGrid, ThreeGridItems);
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var row = ThreeGrid.SelectedItem;
                            var status = row.CheckGet("PRTS_ID").ToInt();

                            if (status == 4 || status == 8 || status == 3)
                            {
                                return false;
                            }

                            if (row.CheckGet("DOPL_ID").ToInt() > 0)
                            {
                                return false;
                            }

                            if (ThreeGrid.Items.Count == 0)
                            {
                                return false;
                            }

                            return true;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "refresh_three_grid",
                        Enabled = true,
                        Title = "Обновить",
                        ButtonUse = true,
                        ButtonName = "RefreshButton2",
                        MenuUse = true,
                        Action = () =>
                        {
                            ThreeGrid.LoadItems();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "save_start_dttm2",
                        Enabled = true,
                        Title = "Корректировка даты начала",
                        ButtonUse = true,
                        ButtonName = "SaveButton2",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () => SaveStartDateTime(ThreeGrid)
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "show_tech_map2",
                        Enabled = true,
                        Title = "Показать ТК",
                        ButtonUse = true,
                        MenuUse = true,
                        ButtonName = "ShowTechMap2",
                        Action = () => TechnologicalMapShow(ThreeGrid),
                        CheckEnabled = () =>
                        {
                            var row = ThreeGrid.SelectedItem.CheckGet("PATHTK");

                            if (!string.IsNullOrEmpty(row))
                            {
                                return true;
                            }

                            return false;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "return_to_plan_2",
                        Enabled = true,
                        Title = "Вернуть в план",
                        ButtonUse = true,
                        ButtonName = "ReturnToPlan2",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var taskId = ThreeGrid.SelectedItem.CheckGet("TASK_ID").ToInt();

                            ReturnTaskToPlan(taskId, 2);
                        },
                        CheckEnabled = () =>
                        {
                            var row = ThreeGrid.SelectedItem;
                            var status = row.CheckGet("PRTS_ID").ToInt();

                            if (status == 8 || status == 3)
                            {
                                return true;
                            }

                            return false;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "export_to_excel_2",
                        Enabled = true,
                        Title = "Экспорт в Excel",
                        MenuUse = true,
                        AccessLevel = Role.AccessMode.ReadOnly,
                        Action = ThreeGrid.ItemsExportExcel
                    });
                }
                Commander.SetCurrentGridName("SecondGrid");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "refresh_second_grid",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить данные",
                        ButtonUse = true,
                        MenuUse = true,
                        ButtonName = "RefreshButtonProductOrder",
                        Action = () =>
                        {
                            SecondGrid.LoadItems();
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "add",
                        Enabled = true,
                        Title = "Добавить на BST[СPH]-1",
                        Description = "Добавить ПЗ на станок",
                        ButtonName = "MoveToPlan",
                        ButtonUse = true,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var row = SecondGrid.SelectedItem;
                            var index = row.CheckGet("TASK_ID").ToInt();
                            if (SecondGrid.Items.Count > 0 && index > 0)
                            {
                                var dialog = new DialogWindow($"Добавить ПЗ - {row.CheckGet("TASK_NUMBER")} в план?", "Добавление ПЗ в план", "",
                                    DialogWindowButtons.YesNo);

                                dialog.ShowDialog();

                                if (dialog.DialogResult == true)
                                {
                                    CheckMachineForAddTaskToPlan(index, row.CheckGet("ID_ST").ToInt(), 311);
                                }
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var row = SecondGrid.SelectedItem.CheckGet("AVAILABLE_311").ToInt();

                            if (SecondGrid.Items.Count == 0)
                            {
                                return false;
                            }

                            if (row == 1)
                            {
                                return true;
                            }

                            return false;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "add2",
                        Enabled = true,
                        Title = "Добавить на AAEI[301P]",
                        Description = "Добавить ПЗ на станок",
                        ButtonName = "MoveToPlan2",
                        ButtonUse = true,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var row = SecondGrid.SelectedItem;
                            var index = row.CheckGet("TASK_ID").ToInt();
                            if (SecondGrid.Items.Count > 0 && index > 0)
                            {
                                var dialog = new DialogWindow($"Добавить ПЗ - {row.CheckGet("TASK_NUMBER")} в план?", "Добавление ПЗ в план", "",
                                    DialogWindowButtons.YesNo);

                                dialog.ShowDialog();

                                if (dialog.DialogResult == true)
                                {
                                    CheckMachineForAddTaskToPlan(index, row.CheckGet("ID_ST").ToInt(), 312);
                                }
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var row = SecondGrid.SelectedItem.CheckGet("AVAILABLE_312").ToInt();

                            if (SecondGrid.Items.Count == 0)
                            {
                                return false;
                            }

                            if (row == 1)
                            {
                                return true;
                            }

                            return false;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "add3",
                        Enabled = true,
                        Title = "Добавить на BST[TBH]-1",
                        Description = "Добавить ПЗ на станок",
                        ButtonName = "MoveToPlan3",
                        ButtonUse = true,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var row = SecondGrid.SelectedItem;
                            var index = row.CheckGet("TASK_ID").ToInt();
                            if (SecondGrid.Items.Count > 0 && index > 0)
                            {
                                var dialog = new DialogWindow($"Добавить ПЗ - {row.CheckGet("TASK_NUMBER")} в план?", "Добавление ПЗ в план", "",
                                    DialogWindowButtons.YesNo);

                                dialog.ShowDialog();

                                if (dialog.DialogResult == true)
                                {
                                    CheckMachineForAddTaskToPlan(index, row.CheckGet("ID_ST").ToInt(), 321);
                                }
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var row = SecondGrid.SelectedItem.CheckGet("AVAILABLE_321").ToInt();

                            if (SecondGrid.Items.Count == 0)
                            {
                                return false;
                            }

                            if (row == 1)
                            {
                                return true;
                            }

                            return false;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "add4",
                        Enabled = true,
                        Title = "Добавить на AAEI[301L]",
                        Description = "Добавить ПЗ на станок",
                        ButtonName = "MoveToPlan4",
                        ButtonUse = true,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var row = SecondGrid.SelectedItem;
                            var index = row.CheckGet("TASK_ID").ToInt();
                            if (SecondGrid.Items.Count > 0 && index > 0)
                            {
                                var dialog = new DialogWindow($"Добавить ПЗ - {row.CheckGet("TASK_NUMBER")} в план?", "Добавление ПЗ в план", "",
                                    DialogWindowButtons.YesNo);

                                dialog.ShowDialog();

                                if (dialog.DialogResult == true)
                                {
                                    CheckMachineForAddTaskToPlan(index, row.CheckGet("ID_ST").ToInt(), 322);
                                }
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var row = SecondGrid.SelectedItem.CheckGet("AVAILABLE_322").ToInt();

                            if (SecondGrid.Items.Count == 0)
                            {
                                return false;
                            }

                            if (row == 1)
                            {
                                return true;
                            }

                            return false;
                        }
                    });
                }
                Commander.Init(this);
            }
        }

        private ListDataSet FirstGridItems;
        private ListDataSet ThreeGridItems;
        private FormHelper Form { get; set; }


        /// <summary>
        /// По умолчанию выбран принтер
        /// </summary>
        private int OrderSelectId { get; set; } = 311;
        private int OrderSelectId2 { get; set; } = 312;
        private int WorkCenterId { get; set; } = 27;

        private void FormInit()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path = "SELECT_MACHINE",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = SelectMachine,
                    ControlType = "SelectBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>(),
                    OnChange = (FormHelperField f, string v) =>
                    {
                        var b = (SelectBox)f.Control;
                        OrderSelectId = b.SelectedItem.Key.ToInt();

                        MoveToFirstMachine.Content = $"Перенести на - {b.SelectedItem.Value}";

                        if (OrderSelectId == 311)
                        {
                            SelectMachine2.SetSelectedItemByKey("312.0");
                            WorkCenterId = 27;
                        }
                        else if (OrderSelectId == 321)
                        {
                            SelectMachine2.SetSelectedItemByKey("322.0");
                            WorkCenterId = 28;
                        }

                        FirstGrid.LoadItems();
                        SecondGrid.LoadItems();
                        ThreeGrid.LoadItems();
                    },
                    QueryLoadItems = new RequestData()
                    {
                        Module = "Preproduction",
                        Object = "PlanningOrderLt",
                        Action = "MachineList",
                        AnswerSectionKey = "ITEMS",
                        OnComplete = (field, ds) =>
                        {
                            var row = new Dictionary<string, string>();

                            ds.ItemsPrepend(row);
                            var list = ds.GetItemsList("ID_ST", "SHORT_NAME");

                            list.Remove("312.0");
                            list.Remove("322.0");

                            var c = (SelectBox)field.Control;
                            if ( c != null)
                            {
                                c.Items = list;
                            }

                            SelectMachine.SetSelectedItemFirst();

                            MoveToFirstMachine.Content = $"Перенести на - {list["311.0"]}";

                        }
                    }
                },
                new FormHelperField()
                {
                    Path = "SELECT_MACHINE2",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = SelectMachine2,
                    ControlType = "SelectBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>(),
                    OnChange = (FormHelperField f, string v) =>
                    {
                        var b = (SelectBox)f.Control;
                        OrderSelectId2 = b.SelectedItem.Key.ToInt();

                        MoveToTwoMachine.Content = $"Перенести на - {b.SelectedItem.Value}";

                        if (OrderSelectId2 == 312)
                        {
                            if (OrderSelectId != 311)
                            {
                                SelectMachine.SetSelectedItemByKey("311.0");
                                WorkCenterId = 27;
                            }
                        }
                        else if (OrderSelectId2 == 322)
                        {
                            if (OrderSelectId != 321)
                            {
                                SelectMachine.SetSelectedItemByKey("321.0");
                                WorkCenterId = 28;
                            }
                        }

                        FirstGrid.LoadItems();
                        ThreeGrid.LoadItems();
                    },
                    QueryLoadItems = new RequestData()
                    {
                        Module = "Preproduction",
                        Object = "PlanningOrderLt",
                        Action = "MachineList",
                        AnswerSectionKey = "ITEMS",
                        OnComplete = (field, ds) =>
                        {
                            var row = new Dictionary<string, string>();

                            ds.ItemsPrepend(row);
                            var list = ds.GetItemsList("ID_ST", "SHORT_NAME");

                            list.Remove("311.0");
                            list.Remove("321.0");

                            var c = (SelectBox)field.Control;
                            if ( c != null)
                            {
                                c.Items = list;
                            }

                            SelectMachine2.SetSelectedItemFirst();

                            MoveToTwoMachine.Content = $"Перенести на - {list["312.0"]}";
                        }
                    }
                }

            };
            Form.SetFields(fields);
            Form.SetDefaults();
        }

        #region Загрузка данных в грид
        private async void LoadItemsForMachine()
        {
            FirstGrid.Toolbar.IsEnabled = false;

            DateTime start_date = DateTime.Now;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PlanningOrderLt");
            q.Request.SetParam("Action", "DynamicList");
            q.Request.SetParam("ID_ST", OrderSelectId.ToString());
            q.Request.SetParam("START_DATE", start_date.ToString("dd.MM.yyyy HH:mm:ss"));
            q.Request.SetParam("END_DATE", start_date.AddDays(1).ToString("dd.MM.yyyy HH:mm:ss"));

            await Task.Run(() => q.DoQuery());

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    {
                        FirstGridItems = ListDataSet.Create(result, "ITEMS");
                        var effectity = ListDataSet.Create(result, "ITEMS_MACHINE");

                        EfficiencyUp.Text = effectity.Items.First().CheckGet("EFFICIENCY_PCT");

                        RefactorTimeStart(FirstGridItems);

                        FirstGrid.UpdateItems(FirstGridItems);
                    }
                }
            }

            FirstGrid.Toolbar.IsEnabled = true;
        }

        private async void LoadItemsForMachine2()
        {
            ThreeGrid.Toolbar.IsEnabled = false;

            DateTime start_date = DateTime.Now;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PlanningOrderLt");
            q.Request.SetParam("Action", "DynamicList");
            q.Request.SetParam("ID_ST", OrderSelectId2.ToString());
            q.Request.SetParam("START_DATE", start_date.ToString("dd.MM.yyyy HH:mm:ss"));
            q.Request.SetParam("END_DATE", start_date.AddDays(1).ToString("dd.MM.yyyy HH:mm:ss"));

            await Task.Run(() => q.DoQuery());

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    {
                        ThreeGridItems = ListDataSet.Create(result, "ITEMS");
                        var effectity = ListDataSet.Create(result, "ITEMS_MACHINE");

                        EfficiencyDown.Text = effectity.Items.First().CheckGet("EFFICIENCY_PCT");

                        RefactorTimeStart(ThreeGridItems);

                        ThreeGrid.UpdateItems(ThreeGridItems);
                    }
                }
            }

            ThreeGrid.Toolbar.IsEnabled = true;
        }

        private async void LoadItemsTaskList()
        {
            SecondGrid.Toolbar.IsEnabled = false;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PlanningOrderLt");
            q.Request.SetParam("Action", "List");
            q.Request.SetParam("WORK_CENTER_ID", WorkCenterId.ToString());

            await Task.Run(() => q.DoQuery());

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                if (result != null)
                {
                    var items = ListDataSet.Create(result, "ITEMS");

                    SecondGrid.UpdateItems(items);
                }
            }

            if (SecondGrid.Items.Count > 0)
            {
                SecondGrid.SelectRowFirst();
            }

            SecondGrid.Toolbar.IsEnabled = true;
        }
        #endregion

        private void RefactorTimeStart(ListDataSet Data, int key = 0)
        {
            var num = 1;
            var timeCount = 0;
            DateTime now = DateTime.Now;
            now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);

            foreach (var item in Data.Items)
            {
                var prodTime = item.CheckGet("PRODUCTION_TIME").ToInt() +
                               item.CheckGet("CHANGE_TIME").ToInt();

                var status = item.CheckGet("PRTS_ID").ToInt();
                if (status == 2 || (item.CheckGet("DOPL_ID").ToInt() > 0 && key == 1))
                {
                    DateTime dttmStart = now.AddMinutes(timeCount);
                    item.CheckAdd("START_DTTM", dttmStart.ToString("dd.MM.yyyy HH:mm:ss"));
                }

                timeCount += prodTime;
                DateTime dttmEnd = now.AddMinutes(timeCount);


                item.CheckAdd("FINISH_DTTM", dttmEnd.ToString("dd.MM.yyyy HH:mm:ss"));
                item.CheckAdd("NUM", num.ToString());

                num++;
            }
        }

        private void SwapItems(int index1, int index2, ListDataSet Data)
        {
            if (index1 < 0 || index2 < 0 || index1 >= Data.Items.Count || index2 >= Data.Items.Count)
            {
                //throw new ArgumentOutOfRangeException("Индексы выходят за границы списка.");
                return;
            }

            (Data.Items[index1], Data.Items[index2]) =
                (Data.Items[index2], Data.Items[index1]);
        }

        /// <summary>
        /// 0 - вверх, 1 - вниз (Перемещение)
        /// </summary>
        /// <param name="mode"></param>
        private void ChangeNumPosition(int mode, GridBox4 Grid, ListDataSet Data)
        {
            Grid.Toolbar.IsEnabled = false;

            if (mode == 0)
            {
                var row = Grid.SelectedItem;
                var ind1 = Data.Items.FindIndex(x => x.CheckGet("TASK_ID") == row.CheckGet("TASK_ID"));
                SwapItems(ind1, ind1 - 1, Data);

                RefactorTimeStart(Data, 1);
                SaveStartDateTime(Grid);

                Grid.UpdateItems(Data);

            }
            else if (mode == 1)
            {
                var row = Grid.SelectedItem;
                var ind1 = Data.Items.FindIndex(x => x.CheckGet("TASK_ID") == row.CheckGet("TASK_ID"));
                SwapItems(ind1, ind1 + 1, Data);

                RefactorTimeStart(Data, 1);
                SaveStartDateTime(Grid);

                Grid.UpdateItems(Data);
            }

            Grid.Toolbar.IsEnabled = true;
        }

        /// <summary>
        /// Удаление ПЗ из плана по идентификатору
        /// </summary>
        /// <param name="taskId">ид задания</param>
        private void DeleteFromPlan(int taskId, GridBox4 Grid, ListDataSet Data)
        {
            Grid.Toolbar.IsEnabled = false;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PlanningOrderLt");
            q.Request.SetParam("Action", "TaskFromPlanDelete");
            q.Request.SetParam("TASK_ID", taskId.ToString());

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var row = Grid.SelectedItem;
                var index = Data.Items.FindIndex(x => x.CheckGet("TASK_ID") == row.CheckGet("TASK_ID"));
                Data.Items.RemoveAt(index);

                RefactorTimeStart(Data, 1);
                SaveStartDateTime(Grid);

                Grid.UpdateItems(Data);
                SecondGrid.LoadItems();
            }

            Grid.Toolbar.IsEnabled = true;
        }

        /// <summary>
        /// Сохранение даты и времени начала планирования по каждому ПЗ по кнопке "Сохранить"
        /// </summary>
        private void SaveStartDateTime(GridBox4 Grid)
        {
            var items = JsonConvert.SerializeObject(Grid.Items.Where(i => i.CheckGet("PRTS_ID").ToInt() == 2 || 
                                                                     i.CheckGet("DOPL_ID").ToInt() > 0));
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PlanningOrderLt");
            q.Request.SetParam("Action", "SaveDateTime");
            q.Request.SetParam("ITEMS", items);

            q.DoQuery();

            if (q.Answer.Status == 0)
            { }
            else
            {
                q.ProcessError();
            }

            Grid.LoadItems();
        }

        /// <summary>
        /// Если изначальное ID в задачи совпадает с выбранным ID о просто происходит добавление ПЗ в план
        /// иначе происходит смена ID машины в таблице TASK и выполнит процедуру по добавлению ПЗ в план
        /// </summary>
        /// <param name="TaskId">Ид задания</param>
        /// <param name="MachineId">Ид машины</param>
        private void CheckMachineForAddTaskToPlan(int TaskId, int crntIdMachine, int MachineId)
        {
            if (crntIdMachine == MachineId)
            {
                AddTaskToPlan(TaskId);
            }
            else
            {
                ChangeIdMachineInTask(TaskId, MachineId);
            }
        }

        /// <summary>
        /// функция для переноса задания между станками
        /// </summary>
        /// <param name="TaskId"></param>
        /// <param name="MachineId"></param>
        /// <param name="IdButton">0 - это верхняя кнопка "Перенести" , 1 - нижняя</param>
        private async void MoveTaskBetweenMachines(int TaskId, int MachineId)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PlanningOrderLt");
            q.Request.SetParam("Action", "MovingTaskBetweenMachine");
            q.Request.SetParam("TASK_ID", TaskId.ToString());
            q.Request.SetParam("MACHINE_ID", MachineId.ToString());

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                FirstGrid.LoadItems();
                ThreeGrid.LoadItems();
            }
        }

        /// <summary>
        /// Функция сделает смену ID машины в таблице TASK и выполнит процедуру по добавлению ПЗ в план
        /// </summary>
        /// <param name="TaskId"></param>
        /// <param name="MachineId"></param>
        private async void ChangeIdMachineInTask(int TaskId, int MachineId)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PlanningOrderLt");
            q.Request.SetParam("Action", "ChangeIdMachineInTask");
            q.Request.SetParam("TASK_ID", TaskId.ToString());
            q.Request.SetParam("MACHINE_ID", MachineId.ToString());

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                FirstGrid.LoadItems();
                SecondGrid.LoadItems();
                ThreeGrid.LoadItems();
            }
            else
            {
                var dialog = new DialogWindow($"{q.Answer.Error.Message}", "Ошибка");
                dialog.SetIcon("alert");
                dialog.ShowDialog();
            }
        }

        /// <summary>
        /// Добавление ПЗ в план
        /// </summary>
        /// <param name="TaskId"></param>
        private async void AddTaskToPlan(int TaskId)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PlanningOrderLt");
            q.Request.SetParam("Action", "MoveTaskToPlan");
            q.Request.SetParam("I_PROT_ID", TaskId.ToString());

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() => q.DoQuery());

            if (q.Answer.Status == 0)
            {
                FirstGrid.LoadItems();
                SecondGrid.LoadItems();
                ThreeGrid.LoadItems();
            }
        }

        /// <summary>
        /// Возвращение заявки в план со статуса 3 и 8
        /// </summary>
        private async void ReturnTaskToPlan(int taskId, int machine)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PlanningOrderLt");
            q.Request.SetParam("Action", "MoveTaskToPlan");
            q.Request.SetParam("I_PROT_ID", taskId.ToString());

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() => q.DoQuery());

            if (q.Answer.Status == 0)
            {
                if (machine == 1)
                {
                    FirstGrid.LoadItems();
                }
                else if (machine == 2)
                {
                    ThreeGrid.LoadItems();
                }
            }
        }

        /// <summary>
        /// Обработчик для кнопки "Показать ТК" для китайского станка
        /// </summary>
        private void TechnologicalMapShow(GridBox4 Grid)
        {
            if (Grid.Items.Count > 0)
            {
                if (Grid.SelectedItem != null)
                {
                    var path = Grid.SelectedItem.CheckGet("PATHTK");
                    if (!string.IsNullOrEmpty(path))
                    {
                        if (File.Exists(path))
                        {
                            Central.OpenFile(path);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Открыть настройки 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsFrames = new Frames.Settings();

            Action refreshGridHandler = () =>
            {
                FirstGrid.LoadItems();
                SecondGrid.LoadItems();
                ThreeGrid.LoadItems();
            };

            settingsFrames.OnSettingsClosed += refreshGridHandler;

            settingsFrames.OnUnload += () =>
            {
                settingsFrames.OnSettingsClosed -= refreshGridHandler;
            };

            settingsFrames.Open();
        }
    }
}