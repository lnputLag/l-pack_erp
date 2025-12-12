using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Логика взаимодействия по образцам от клиентов
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class PatternOrdersList : ControlBase
    {
        public PatternOrdersList()
        {
            InitializeComponent();
            ControlTitle = "Образцы от клиента";
            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/clents_samples";
            RoleName = "[erp]pattern_orders";

            OnLoad = () =>
            {
                SetDefaults();
                LoadRef();
                InitGrid();
                InitPurposeGrid();
            };

            OnUnload = () =>
            {
                Grid.Destruct();
                PurposeGrid.Destruct();
            };

            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverName == ControlName)
                {
                    ProcessMessages(msg);
                }
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

            Commander.SetCurrentGroup("main");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "refresh",
                    Group = "grid_base",
                    Enabled = true,
                    Title = "Показать",
                    Description = "Загрузить данные",
                    ButtonUse = true,
                    ButtonName = "RefreshButton",
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        Grid.LoadItems();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "help",
                    Enabled = true,
                    Title = "Справка",
                    Description = "Показать справочную информацию",
                    ButtonUse = true,
                    ButtonName = "HelpButton",
                    HotKey = "F1",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        Central.ShowHelp(DocumentationUrl);
                    },
                });
            }
            Commander.SetCurrentGridName("Grid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "add",
                    Title = "Добавить",
                    Group = "item",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonName = "AddButton",
                    Description = "Добавить образец от клиента",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var patternOrderForm = new PatternOrder();
                        patternOrderForm.ReceiverName = ControlName;
                        patternOrderForm.Edit(0);
                    },
                    CheckEnabled = () =>
                    {
                        //bool result = true;

                        return EditPatternRight;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "edit",
                    Title = "Изменить",
                    Group = "item",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonName = "EditButton",
                    Description = "Изменение образца от клиента",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            var patternOrderForm = new PatternOrder();
                            patternOrderForm.ReceiverName = ControlName;
                            patternOrderForm.Edit(id);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (row != null)
                        {
                            bool unstored = row.CheckGet("STORAGE_FLAG").ToInt() == 0;
                            result = EditPatternRight && unstored;
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "delete",
                    Title = "Удалить",
                    Group = "item",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonName = "DeleteButton",
                    Description = "Удаление образца от клиента",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            Delete();
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (row != null)
                        {
                            bool unstored = row.CheckGet("STORAGE_FLAG").ToInt() == 0;
                            result = EditPatternRight && unstored;
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "purposeedit",
                    Title = "Изменить данные",
                    Group = "item",
                    MenuUse = false,
                    ButtonUse = true,
                    ButtonName = "PurposeButton",
                    Description = "Изменение данных образца от клиента",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            var purposeEditForm = new PatternOrderPurpose();
                            purposeEditForm.ReceiverName = ControlName;
                            purposeEditForm.Edit(id);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (row != null)
                        {
                            result = true;
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "toexcel",
                    Title = "В Excel",
                    Group = "item",
                    MenuUse = false,
                    ButtonUse = true,
                    ButtonName = "ToExcelButton",
                    Description = "Экспорт строк в Excel",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        Grid.ItemsExportExcel();
                    },
                    CheckEnabled = () =>
                    {
                        var result = true;
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "transferblankprint",
                    Title = "Печать бланка передачи",
                    Group = "operation",
                    MenuUse = true,
                    ButtonUse = false,
                    Description = "Печать бланка передачи",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        var reporter = new PatternOrderReporter();
                        reporter.PatternOrderItem = SelectedItem;
                        reporter.PatternOrderPurposes = PurposeGrid.Items;
                        reporter.Make();
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (row != null)
                        {
                            result = true;
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "tostorage",
                    Title = "Отметить получение на хранение",
                    Group = "operation",
                    MenuUse = true,
                    ButtonUse = false,
                    Description = "Отметить получение на хранение",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        UpdateStorageFlag();
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (row != null)
                        {
                            bool unstored = row.CheckGet("STORAGE_FLAG").ToInt() == 0;
                            result = EditPatternRight && unstored && (row.CheckGet("PURPOSE_COMPLETE").ToInt() == 1);
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "datasended",
                    Title = "Отметить отправку данных клиенту",
                    Group = "operation",
                    MenuUse = true,
                    ButtonUse = false,
                    Description = "Отметить отправку данных клиенту",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        SetDataSended();
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (row != null)
                        {
                            result = EditPatternRight && (row.CheckGet("DATA_CLIENT_SENDED_FLAG").ToInt() == 0);
                        }
                        return result;
                    },
                });

            }
            Commander.SetCurrentGridName("PurposeGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "purposecomplete",
                    Title = "Отметить выполнение",
                    Group = "item",
                    MenuUse = false,
                    ButtonUse = true,
                    ButtonName = "PurposeCompleteButton",
                    Description = "Изменение данные образца от клиента",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = PurposeGrid.GetPrimaryKey();
                        var id = PurposeGrid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            PurposeCompleteClick();
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = PurposeGrid.SelectedItem;
                        if (row != null)
                        {
                            bool canComplete = row.CheckGet("CAN_USER_EDIT").ToBool();
                            // Работы не проводились или отмечены как не выполненные, такие работы можно выполнить
                            bool incompleted = !row.CheckGet("COMPLETION").ToBool();

                            result = incompleted && canComplete;
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "setpurposework",
                    Title = "Взять в работу",
                    Group = "operation",
                    MenuUse = true,
                    ButtonUse = false,
                    Description = "Взять в работу",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        SetPurposeWork();
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = PurposeGrid.SelectedItem;
                        if (row != null)
                        {
                            bool canComplete = row.CheckGet("CAN_USER_EDIT").ToBool();
                            bool workFlag = row.CheckGet("WORK_FLAG").ToBool();
                            bool incompleted = !row.CheckGet("COMPLETION").ToBool();

                            result = incompleted && canComplete && !workFlag;

                            if (!incompleted)
                            {
                                int employeeId = row.CheckGet("EMPLOYEE_ID").ToInt();
                                if (employeeId == Central.User.EmployeeId)
                                {
                                    result = true;
                                }
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "setworkcomplete",
                    Title = "Отметить выполнение работы",
                    Group = "completetion",
                    MenuUse = true,
                    ButtonUse = false,
                    Description = "Отметить выполнение работы",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        SetPurposeComplete(1);
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = PurposeGrid.SelectedItem;
                        if (row != null)
                        {
                            bool canComplete = row.CheckGet("CAN_USER_EDIT").ToBool();
                            bool workFlag = row.CheckGet("WORK_FLAG").ToBool();
                            bool emptyCompletion = true;
                            if (row.ContainsKey("COMPLETION"))
                            {
                                if (!string.IsNullOrEmpty(row["COMPLETION"]))
                                {
                                    emptyCompletion = false;
                                }
                            }

                            result = canComplete && (emptyCompletion || workFlag);
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "setworkincomplete",
                    Title = "Отметить невыполнение работы",
                    Group = "completetion",
                    MenuUse = true,
                    ButtonUse = false,
                    Description = "Отметить невыполнение работы",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        SetPurposeComplete(0);
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = PurposeGrid.SelectedItem;
                        if (row != null)
                        {
                            bool canComplete = row.CheckGet("CAN_USER_EDIT").ToBool();
                            bool workFlag = row.CheckGet("WORK_FLAG").ToBool();
                            bool emptyCompletion = true;
                            if (row.ContainsKey("COMPLETION"))
                            {
                                if (!string.IsNullOrEmpty(row["COMPLETION"]))
                                {
                                    emptyCompletion = false;
                                }
                            }

                            result = canComplete && (emptyCompletion || workFlag);
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "setcompleteclear",
                    Title = "Сбросить информацию о работе",
                    Group = "completetion",
                    MenuUse = true,
                    ButtonUse = false,
                    Description = "Сбросить информацию о работе",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        SetPurposeComplete(-1);
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = PurposeGrid.SelectedItem;
                        if (row != null)
                        {
                            bool canComplete = row.CheckGet("CAN_USER_EDIT").ToBool();
                            bool workFlag = row.CheckGet("WORK_FLAG").ToBool();
                            bool сompletion = !row.CheckGet("COMPLETION").IsNullOrEmpty();

                            result = canComplete && (сompletion || workFlag);
                        }
                        return result;
                    },
                });
            }
            Commander.Init(this);
        }

        /// <summary>
        /// данные для таблицы
        /// </summary>
        public ListDataSet PatternOrdersDS { get; set; }
        /// <summary>
        /// данные для таблицы оценок образца от клиента
        /// </summary>
        public ListDataSet PatternOrdersPurposeDS { get; set; }

        /// <summary>
        /// Список групп, в которые входит пользователь
        /// </summary>
        public List<string> UserGroups { get; set; }

        /// <summary>
        /// Право на редактирование образца от клиента
        /// </summary>
        public bool EditPatternRight;
        /// <summary>
        /// Право на редактирование оценок образца и выставление завершения работ
        /// </summary>
        public bool EditPurposeRight;

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// обработчик системы навигации по URL
        /// </summary>
        public void ProcessNavigation()
        {

        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="obj">сообщение</param>
        private void ProcessMessages(ItemMessage msg)
        {
            string action = msg.Action.ClearCommand();
            if (!action.IsNullOrEmpty())
            {
                switch (action)
                {
                    case "refresh":
                        Grid.LoadItems();
                        break;
                }
            }
        }

        public void SetDefaults()
        {
            FromDate.Text = DateTime.Now.Date.AddDays(-30).ToString("dd.MM.yyyy");
            ToDate.Text = DateTime.Now.Date.ToString("dd.MM.yyyy");

            EditPatternRight = false;
            EditPurposeRight = false;
        }

        /// <summary>
        /// инициализация таблицы с образцами
        /// </summary>
        public void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header=" ",
                    Path="_SELECTED",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    Editable=true,
                    Exportable=false,
                    Width2=4,
                },
                new DataGridHelperColumn()
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn()
                {
                    Header="Дата",
                    Path="PATTERN_DT",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Width2=10,
                    Doc="Дата получения образца",
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn()
                {
                    Header="Данные отправлены",
                    Path="DATA_CLIENT_SENDED_FLAG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    Width2=10,
                },
                new DataGridHelperColumn()
                {
                    Header="Покупатель",
                    Path="CUSTOMER_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn()
                {
                    Header="Конечный потребитель",
                    Path="CLIENT_CUSTOMER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn()
                {
                    Header="Производитель",
                    Path="MANUFACTURER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn()
                {
                    Header="Контактное лицо",
                    Path="CONTACT_PERSON",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn()
                {
                    Header="Наименование",
                    Path="NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn()
                {
                    Header="Менеджер",
                    Path="MANAGER_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn()
                {
                    Header="Объем продаж конкурента",
                    Path="COMPETITOR_SALES",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Double,
                    Width2=6,
                },
                new DataGridHelperColumn()
                {
                    Header="Цена конкурента",
                    Path="COMPETITOR_PRICE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Double,
                    Width2=6,
                },
                new DataGridHelperColumn()
                {
                    Header="Марка конкурента",
                    Path="COMPETITOR_NAME_MARKA",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=4,
                },
                new DataGridHelperColumn()
                {
                    Header="Профиль",
                    Path="PROFIL_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=4,
                },
                new DataGridHelperColumn()
                {
                    Header="Коэффициент гофрирования",
                    Path="CORRUGATED_COEFFICIENT",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn()
                {
                    Header="Толщина",
                    Path="THICKNESS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Double,
                    Width2=4,
                    Doc="Толщина картона",
                },
                new DataGridHelperColumn()
                {
                    Header="ЕСТ",
                    Path="EDGE_CRASH_TEST",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Double,
                    Width2=4,
                    Doc="Показатель торцевого сжатия",
                },
                new DataGridHelperColumn()
                {
                    Header="Марка",
                    Path="MARK_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=4,
                },
                new DataGridHelperColumn()
                {
                    Header="Цвет картона",
                    Path="OUTER_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=6,
                },
                new DataGridHelperColumn()
                {
                    Header="1 слой (внутр)",
                    Group="Сырье",
                    Path="RAW_GROUP_1",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=5,
                },
                new DataGridHelperColumn()
                {
                    Header="2 слой",
                    Group="Сырье",
                    Path="RAW_GROUP_2",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=5,
                },
                new DataGridHelperColumn()
                {
                    Header="3 слой",
                    Group="Сырье",
                    Path="RAW_GROUP_3",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=5,
                },
                new DataGridHelperColumn()
                {
                    Header="4 слой",
                    Group="Сырье",
                    Path="RAW_GROUP_4",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=5,
                },
                new DataGridHelperColumn()
                {
                    Header="5 слой",
                    Group="Сырье",
                    Path="RAW_GROUP_5",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=5,
                },
                new DataGridHelperColumn()
                {
                    Header="Внутренние размеры",
                    Path="SIZE_PATTERN",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn()
                {
                    Header="Габариты развертки",
                    Path="SIZE_BLANK",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn()
                {
                    Header="ВСТ",
                    Path="BURSTING_STRENGTH_TEST",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Double,
                    Width2=4,
                    Doc="Нагрузка штабеля",
                },
                new DataGridHelperColumn()
                {
                    Header="Сопротивление расслаиванию, кН/м",
                    Path="DELAMINATION_RESISTANCE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Double,
                    Width2=4,
                },
                new DataGridHelperColumn()
                {
                    Header="Наличие внутренних трещин",
                    Path="INTERNAL_CRACK_FLAG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    Width2=4,
                },
                new DataGridHelperColumn()
                {
                    Header="1",
                    Group="Цвет",
                    Path="NAME_COLOR_1",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn()
                {
                    Header="2",
                    Group="Цвет",
                    Path="NAME_COLOR_2",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn()
                {
                    Header="3",
                    Group="Цвет",
                    Path="NAME_COLOR_3",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn()
                {
                    Header="4",
                    Group="Цвет",
                    Path="NAME_COLOR_4",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn()
                {
                    Header="5",
                    Group="Цвет",
                    Path="NAME_COLOR_5",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn()
                {
                    Header="Вес",
                    Path="WEIGHT",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Double,
                    Width2=6,
                },
                new DataGridHelperColumn()
                {
                    Header="Примечание",
                    Path="COMMENTS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=10,
                },
                /*
                new DataGridHelperColumn()
                {
                    Header="",
                    Path="STORAGE_FLAG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Hidden=true,
                },
                new DataGridHelperColumn()
                {
                    Header="",
                    Path="PURPOSE_COMPLETE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Hidden=true,
                },
                new DataGridHelperColumn()
                {
                    Header="",
                    Path="PURPOSE_INCOMPLETE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Hidden=true,
                },
                new DataGridHelperColumn()
                {
                    Header="Почта менеджера",
                    Path="MANAGER_EMAIL",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Hidden=true,
                },
                */
            };
            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("ID");
            Grid.SetSorting("ID", ListSortDirection.Descending);
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.Commands = Commander;

            // Раскраска строк
            Grid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                {
                    DataGridHelperColumn.StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";
                        if ((row.CheckGet("PURPOSE_COMPLETE").ToInt() == 1) && (row.CheckGet("STORAGE_FLAG").ToInt() == 0))
                        {
                            color=HColor.BlueFG;
                            result=color.ToBrush();
                        }
                        return result;
                    }
                },
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";
                        if ((row.CheckGet("PURPOSE_COMPLETE").ToInt() == 1) || (row.CheckGet("STORAGE_FLAG").ToInt() == 1))
                        {
                            color=HColor.Green;
                            result=color.ToBrush();
                        }
                        return result;
                    }
                }
            };

            Grid.SearchText = SearchText;
            //данные грида
            Grid.OnLoadItems = LoadItems;
            Grid.OnFilterItems = FilterItems;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    SelectedItem = selectedItem;
                    UpdateActions(selectedItem);
                }
            };
            Grid.OnDblClick = selectedItem =>
            {
                int id = selectedItem.CheckGet("ID").ToInt();
                if (id > 0)
                {
                    OpenEditForm(id);
                }
            };
            Grid.Init();
        }

        /// <summary>
        /// инициализация таблицы с оценками образца
        /// </summary>
        private void InitPurposeGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn()
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=3,
                },
                new DataGridHelperColumn()
                {
                    Header="Цель",
                    Path="NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn()
                {
                    Header="Ответственный",
                    Path="RESPONSIBLE_PERSON",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=7,
                },
                new DataGridHelperColumn()
                {
                    Header="Сотрудник",
                    Path="EMPLOYEE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=11,
                },
                new DataGridHelperColumn()
                {
                    Header="Дт/Вр выполнения",
                    Path="COMPLETION_DTTM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Width2=10,
                    Format="dd.MM.yy HH:mm",
                },
                new DataGridHelperColumn()
                {
                    Header="Выполнено",
                    Path="COMPLETION",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Hidden=true,
                },
                new DataGridHelperColumn()
                {
                    Header="ИД",
                    Path="PTOP_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn()
                {
                    Header="ИД типа цели",
                    Path="PTPU_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn()
                {
                    Header="В работе",
                    Path="WORK_FLAG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Hidden=true,
                },
                new DataGridHelperColumn()
                {
                    Header="Разрешено редактирование",
                    Path="CAN_USER_EDIT",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn()
                {
                    Header="Разрешено редактирование",
                    Path="EMPLOYEE_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };

            PurposeGrid.SetColumns(columns);
            PurposeGrid.SetPrimaryKey("PTOP_ID");
            PurposeGrid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
            PurposeGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            PurposeGrid.Commands = Commander;

            // Раскраска строк
            PurposeGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";
                        // Цель выполнена (зеленый) или не выполнена (красный)
                        string completion = "";
                        if (row.ContainsKey("COMPLETION"))
                        {
                            completion = row.CheckGet("COMPLETION");
                            if (!completion.IsNullOrEmpty())
                            {
                                if (completion.ToInt() == 1)
                                {
                                    color=HColor.Green;
                                }
                                if (completion.ToInt() == 0)
                                {
                                    color=HColor.Red;
                                }
                            }
                        }

                        // Цель в работе
                        if (row.CheckGet("WORK_FLAG").ToInt() == 1)
                        {
                            color=HColor.Yellow;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                }
            };

            PurposeGrid.AutoUpdateInterval = 0;
            //при выборе строки в гриде, обновляются актуальные действия для записи
            PurposeGrid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    PurposeUpdateActions(selectedItem);
                }
            };
            PurposeGrid.Init();
        }

        /// <summary>
        /// Получение списка групп, в которые входит пользователь
        /// </summary>
        private async void LoadRef()
        {
            UserGroups = new List<string>();

            // Активный сотрудник
            string emplId = Central.User.EmployeeId.ToString();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PatternOrder");
            q.Request.SetParam("Action", "GetRef");
            q.Request.SetParam("EMPLOYEE_ID", emplId);

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
                    // менеджеры по работе с клиентами
                    var managersDS = ListDataSet.Create(result, "MANAGERS");
                    var managers = new Dictionary<string, string>()
                    {
                        { "-1", "Все" },
                    };

                    foreach (var item in managersDS.Items)
                    {
                        managers.CheckAdd(item["ID"].ToInt().ToString(), item["FIO"]);
                    }

                    ManagerName.Items = managers;
                    // Если активный пользователь есть в списке, установим его в выбранном значении
                    if (managers.ContainsKey(emplId))
                    {
                        ManagerName.SetSelectedItemByKey(emplId);
                    }
                    else
                    {
                        ManagerName.SetSelectedItemByKey("-1");
                    }

                    //Типы работ
                    var workType = new Dictionary<string, string>()
                    {
                        { "-1", "Все" },
                    };
                    var workTypeDS = ListDataSet.Create(result, "WORK_TYPE");
                    foreach (var item in workTypeDS.Items)
                    {
                        workType.CheckAdd(item["PTPU_ID"].ToInt().ToString(), item["NAME"]);
                    }
                    WorkTypeSelect.Items = workType;
                    WorkTypeSelect.SetSelectedItemByKey("-1");

                    var employeeGroups = ListDataSet.Create(result, "USER_GROUPS");
                    if (employeeGroups.Items.Count > 0)
                    {
                        foreach(var item in employeeGroups.Items)
                        {
                            if (item.CheckGet("ID").ToInt() != 1)
                            {
                                if (item.CheckGet("IN_GROUP").ToBool())
                                {
                                    string groupCode = item.CheckGet("CODE");
                                    if (!string.IsNullOrEmpty(groupCode))
                                    {
                                        UserGroups.Add(groupCode);
                                    }
                                }
                            }
                        }
                    }

                    EditPatternRight = UserGroups.Contains("manager") || UserGroups.Contains("programmer");
                }
            }
        }

        /// <summary>
        /// Загрузка данных из БД в таблицу образцов от клиента
        /// </summary>
        public async void LoadItems()
        {
            GridToolbar.IsEnabled = false;
            Grid.ShowSplash();
            bool resume = true;

            if (resume)
            {
                var f = FromDate.Text.ToDateTime();
                var t = ToDate.Text.ToDateTime();
                if (DateTime.Compare(f, t) > 0)
                {
                    var msg = "Дата начала не должна быть больше даты окончания.";
                    var d = new DialogWindow($"{msg}", "Проверка данных");
                    d.ShowDialog();
                    resume = false;
                }
            }

            if (resume)
            {
                PurposeGrid.ClearItems();

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "PatternOrder");
                q.Request.SetParam("Action", "List");
                q.Request.SetParam("FROM_DATE", FromDate.Text);
                q.Request.SetParam("TO_DATE", ToDate.Text);

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
                        PatternOrdersDS = ListDataSet.Create(result, "PatternOrders");
                        Grid.UpdateItems(PatternOrdersDS);

                        RefreshButton.Style = (Style)RefreshButton.TryFindResource("Button");
                    }
                }
            }

            GridToolbar.IsEnabled = true;
            Grid.HideSplash();
        }

        /// <summary>
        /// Загрузка данных из БД в таблицу целей
        /// </summary>
        public async void LoadPurposeItems()
        {
            if (SelectedItem != null)
            {
                EditPurposeRight = false;
                PurposeToolbar.IsEnabled = false;
                PurposeGrid.ShowSplash();

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "PatternOrder");
                q.Request.SetParam("Action", "PurposeList");
                q.Request.SetParam("ID", SelectedItem["ID"]);
                q.Request.SetParam("EMPLOYEE_ID", Central.User.EmployeeId.ToString());

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

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
                        PatternOrdersPurposeDS = ListDataSet.Create(result, "PurposeList");
                        PurposeGrid.UpdateItems(PatternOrdersPurposeDS);

                        if (UserGroups.Contains("programmer"))
                        {
                            EditPurposeRight = true;
                        }
                        else
                        {
                            foreach (var item in PatternOrdersPurposeDS.Items)
                            {
                                // Если стоит хотя бы одна отметка, разрешающая редактировать оценку, даём доступ к форме оценок
                                if (item.CheckGet("CAN_USER_EDIT").ToInt() == 1)
                                {
                                    EditPurposeRight = true;
                                }
                            }
                        }
                    }
                }

                //Разблокируем кнопку ввода оценок в зависимости от результата загрузки данных
                bool unstored = SelectedItem.CheckGet("STORAGE_FLAG").ToInt() == 0;
                PurposeButton.IsEnabled = EditPurposeRight && unstored;

                PurposeToolbar.IsEnabled = true;
                PurposeGrid.HideSplash();
            }
        }

        /// <summary>
        /// Фильтрация строк таблицы
        /// </summary>
        public void FilterItems()
        {
            if (Grid.Items != null)
            {
                if (Grid.Items.Count > 0)
                {
                    bool incompleteOnly = (bool)IncompletedCheckBox.IsChecked;

                    bool doFilteringByManager = false;
                    var managerId = ManagerName.SelectedItem.Key.ToInt();
                    if (managerId > 0)
                    {
                        doFilteringByManager = true;
                    }

                    bool doFilteringWorkType = false;
                    var workTypeId = WorkTypeSelect.SelectedItem.Key.ToInt();
                    if (workTypeId > 0)
                    {
                        doFilteringWorkType = true;
                    }

                    var items = new List<Dictionary<string, string>>();
                    foreach (Dictionary<string, string> row in Grid.Items)
                    {
                        // Не выполнено
                        bool includeIncomplete = true;
                        if (incompleteOnly)
                        {
                            if (row.CheckGet("PURPOSE_COMPLETE").ToInt() != 0)
                            {
                                includeIncomplete = false;
                            }
                        }

                        // Фильтр по менеджеру
                        bool includeByManager = true;
                        if (doFilteringByManager)
                        {
                            includeByManager = false;
                            if (row.CheckGet("MANAGER_ID").ToInt() == managerId)
                            {
                                includeByManager = true;
                            }
                        }

                        // Фильтр по типу работ
                        bool includeByWorkType = true;
                        if (doFilteringWorkType)
                        {
                            includeByWorkType = false;
                            string workList = row.CheckGet("WORK_LIST");
                            if (!workList.IsNullOrEmpty())
                            {
                                var workType = workList.Split(';');
                                foreach (var wi in workType)
                                {
                                    if (wi.ToInt() == workTypeId)
                                    {
                                        includeByWorkType = true;
                                    }
                                }
                            }
                        }

                        if (includeIncomplete && includeByManager && includeByWorkType)
                        {
                            items.Add(row);
                        }
                    }

                    Grid.Items = items;
                }
            }
        }

        /// <summary>
        /// обновление методов работы с выбранной записью в таблице образцов от клиентов
        /// </summary>
        /// <param name="selectedItem">выбранная запись</param>
        public void UpdateActions(Dictionary<string, string> selectedItem)
        {
            
            // загрузим оценки для выбранного образца
            LoadPurposeItems();
            PurposeCompleteButton.IsEnabled = false;

            /*
            // включение разрешений на действия
            bool unstored = SelectedItem.CheckGet("STORAGE_FLAG").ToInt() == 0;

            AddButton.IsEnabled = EditPatternRight;
            EditButton.IsEnabled = EditPatternRight && unstored;
            DeleteButton.IsEnabled = EditPatternRight && unstored;

            Grid.Menu["ToStorage"].Enabled = EditPatternRight && unstored && (SelectedItem["PURPOSE_COMPLETE"].ToInt() == 1);
            Grid.Menu["DataSended"].Enabled = EditPatternRight && SelectedItem.CheckGet("DATA_CLIENT_SENDED_FLAG").ToInt() == 0;
            */
        }

        /// <summary>
        /// обновление методов работы с выбранной записью в таблице выполнения целей
        /// </summary>
        /// <param name="selectedItem">выбранная запись</param>
        public void PurposeUpdateActions(Dictionary<string, string> selectedItem)
        {
            /*
            int purposeId = selectedItem.CheckGet("PTPU_ID").ToInt();
            // Пока работы не проведены поле COMPLETION остается пустым
            bool emptyCompletion = false;
            // Работы не проводились или отмечены как не выполненные, такие работы можно выполнить
            bool incompleted = true;
            if (selectedItem.ContainsKey("COMPLETION"))
            {
                if (string.IsNullOrEmpty(selectedItem["COMPLETION"]))
                {
                    emptyCompletion = true;
                }
                else
                {
                    incompleted = !selectedItem["COMPLETION"].ToBool();
                }
            }
            else
            {
                emptyCompletion = true;
            }

            // Восможность для пользователя ставить отметки для выбранной цели
            bool canComplete = selectedItem.CheckGet("CAN_USER_EDIT").ToBool();
            var workFlag = selectedItem.CheckGet("WORK_FLAG").ToBool();
            PurposeCompleteButton.IsEnabled = incompleted && canComplete;

            // В работу можно брать, если у пользователя есть право ставить оценку и не в работе
            // В работу можно отправить оценку, по которой не проводились работы, отмеченную как выполненную и как невыполненную
            PurposeGrid.Menu["SetPurposeWork"].Enabled = incompleted && canComplete && !workFlag;
            // Выполненную задачу может отправить в работу только тот, кто поставил выполнение
            if (!emptyCompletion && !incompleted)
            {
                int employeeId = selectedItem.CheckGet("EMPLOYEE_ID").ToInt();
                if (employeeId == Central.User.EmployeeId)
                {
                    PurposeGrid.Menu["SetPurposeWork"].Enabled = true;
                }
            }
            // Отмечать выполнение или невыполнение можно только у незавершенных работ
            PurposeGrid.Menu["SetWorkComplete"].Enabled = canComplete && (emptyCompletion || workFlag);
            PurposeGrid.Menu["SetWorkInComplete"].Enabled = canComplete && (emptyCompletion || workFlag);
            */
        }

        /// <summary>
        /// Обновление признака получения образца на хранение
        /// </summary>
        private async void UpdateStorageFlag()
        {
            var dw = new DialogWindow("Вы действительно хотите отметить получение на хранение?", "Получение на хранение", "", DialogWindowButtons.YesNo);
            if (dw.ShowDialog() == true)
            {
                int ptrnID = SelectedItem["ID"].ToInt();
                if (ptrnID > 0)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "PatternOrder");
                    q.Request.SetParam("Action", "UpdateStorageFlag");
                    q.Request.SetParam("ID", ptrnID.ToString());
                    q.Request.SetParam("STORAGE_FLAG", "1");

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result!= null)
                        {
                            // вернулся не пустой ответ, обновим таблицу
                            Grid.LoadItems();
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
        /// Удаление образца от клиента
        /// </summary>
        private async void Delete()
        {
            int patternId = SelectedItem.CheckGet("ID").ToInt();
            if (patternId > 0)
            {
                var dw = new DialogWindow($"Вы действительно хотите удалить образец {SelectedItem["NAME"]}?", "Удаление образца", "", DialogWindowButtons.NoYes);
                if (dw.ShowDialog() == true)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "PatternOrder");
                    q.Request.SetParam("Action", "Delete");
                    q.Request.SetParam("ID", patternId.ToString());

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            if (result.Count > 0)
                            {
                                // вернулся не пустой ответ, обновим таблицу
                                Grid.LoadItems();
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
        /// Отмечает результат достижения цели
        /// </summary>
        private async void SetPurposeComplete(int completion)
        {
            int ptopId = PurposeGrid.SelectedItem.CheckGet("PTOP_ID").ToInt();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PatternOrder");
            q.Request.SetParam("Action", "PurposeUpdate");
            q.Request.SetParam("PtopId", ptopId.ToString());
            q.Request.SetParam("Completion", completion.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    if (result.Count > 0)
                    {
                        // вернулся не пустой ответ, обновим таблицу
                        LoadPurposeItems();
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Установка флага отправки данных клиенту
        /// </summary>
        private async void SetDataSended()
        {
            int patternId = SelectedItem.CheckGet("ID").ToInt();
            if (patternId > 0)
            {
                var dw = new DialogWindow("Вы действительно хотите отметить отправку данных клиенту?", "Отправка данных клиенту", "", DialogWindowButtons.YesNo);
                if (dw.ShowDialog() == true)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "PatternOrder");
                    q.Request.SetParam("Action", "SetDataSendedFlag");
                    q.Request.SetParam("ID", patternId.ToString());
                    q.Request.SetParam("DATA_SENDED_FLAG", "1");

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
                                // вернулся не пустой ответ, обновим таблицу
                                Grid.LoadItems();
                            }
                        }
                    }
                    else if (q.Answer.Error.Code == 145)
                    {
                        var dwe = new DialogWindow(q.Answer.Error.Message, "Отправка данных клиенту");
                        dwe.ShowDialog();
                    }

                }
            }
        }

        /// <summary>
        /// Ставит флаг "В работе" для выбранной цели
        /// </summary>
        private async void SetPurposeWork()
        {
            var dw = new DialogWindow("Вы действительно хотите отметить начало работы?", "Образцы от клиента", "", DialogWindowButtons.YesNo);
            if (dw.ShowDialog() == true)
            {
                int ptopId = PurposeGrid.SelectedItem.CheckGet("PTOP_ID").ToInt();

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "PatternOrder");
                q.Request.SetParam("Action", "SetPurposeWork");
                q.Request.SetParam("PURPOSE_ID", ptopId.ToString());
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
                            // вернулся не пустой ответ, обновим таблицу
                            LoadPurposeItems();
                        }
                    }
                }
            }
        }

        private void OpenEditForm(int patternId)
        {
            // включение разрешений на действия

            if (EditPatternRight)
            {
                var patternOrderForm = new PatternOrder();
                patternOrderForm.ReceiverName = ControlName;
                patternOrderForm.Edit(patternId);
            }
            if (EditPurposeRight)
            {
                var purposeEditForm = new PatternOrderPurpose();
                purposeEditForm.ReceiverName = ControlName;
                purposeEditForm.Edit(patternId);
            }
        }

        /// <summary>
        /// Обработчик нажатия на кнопку отметки выполнения
        /// </summary>
        private void PurposeCompleteClick()
        {
            var dw = new DialogWindow("Вы выполнили цель?", "Выполнение цели", "", DialogWindowButtons.YesNoCancel);
            if (dw.ShowDialog() == true)
            {
                int completion = 1;
                if (dw.ResultButton == DialogResultButton.No)
                {
                    completion = 0;
                }

                SetPurposeComplete(completion);
            }
        }

        /// <summary>
        /// При сммене дат меняет стиль кнопки обновления данных
        /// </summary>
        private void DateChanged()
        {
            RefreshButton.Style = (Style)RefreshButton.TryFindResource("FButtonPrimary");
        }

        private void IncompletedCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void ToDate_TextChanged(object sender, TextChangedEventArgs e)
        {
            DateChanged();
        }

        private void FromDate_TextChanged(object sender, TextChangedEventArgs e)
        {
            DateChanged();
        }

        private void ManagerName_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void WorkTypeSelect_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }
    }
}
