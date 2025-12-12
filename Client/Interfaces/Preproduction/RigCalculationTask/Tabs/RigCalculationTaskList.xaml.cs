using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Список заданий на расчет новой осастки
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class RigCalculationTaskList : ControlBase
    {
        public RigCalculationTaskList()
        {
            ControlTitle = "Расчет оснастки";
            RoleName = "[erp]rig_calculation_task";
            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/osn_calculation";

            InitializeComponent();

            OnLoad = () =>
            {
                SetDefaults();
                LoadRef();
                InitGrid();
            };

            OnUnload = () =>
            {
                Grid.Destruct();
            };

            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverName == ControlName)
                {
                    ProcessMessage(msg);
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

            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    switch (e.Key)
                    {
                        case Key.F1:
                            Commander.ProcessCommand("help");
                            e.Handled = true;
                            break;
                        case Key.F5:
                            Grid.LoadItems();
                            e.Handled = true;
                            break;

                        case Key.Home:
                            Grid.SelectRowFirst();
                            e.Handled = true;
                            break;

                        case Key.End:
                            Grid.SelectRowLast();
                            e.Handled = true;
                            break;
                    }
                }
            };

            UserAccessMode = Central.Navigator.GetRoleLevel(RoleName);

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
                    Name = "create",
                    Title = "Добавить",
                    Group = "item",
                    MenuUse = true,
                    HotKey = "Insert",
                    ButtonUse = true,
                    ButtonName = "AddButton",
                    Description = "Создание расчета оснастки",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var rigTaskEditForm = new RigCalculationTask();
                        //rigTaskEditForm.RigTaskId = 0;
                        rigTaskEditForm.ReceiverName = ControlName;
                        rigTaskEditForm.Edit(0);
                    },
                    CheckEnabled = () =>
                    {
                        var result = true;
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "edit",
                    Title = "Изменить",
                    Group = "item",
                    MenuUse = true,
                    HotKey = "Return|DoubleCLick",
                    ButtonUse = true,
                    ButtonName = "EditButton",
                    Description = "Изменение значений расчета",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            var rigTaskEditForm = new RigCalculationTask();
                            //rigTaskEditForm.RigTaskId = id;
                            rigTaskEditForm.ReceiverName = ControlName;
                            rigTaskEditForm.Edit(id);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var k = Grid.GetPrimaryKey();
                        var row = Grid.SelectedItem;
                        if (row.CheckGet(k).ToInt() != 0)
                        {
                            result = true;
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
                    Description = "Удаление расчета оснастки",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            DeleteData(id);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var k = Grid.GetPrimaryKey();
                        var row = Grid.SelectedItem;
                        if (row.CheckGet(k).ToInt() != 0)
                        {
                            if (row.CheckGet("CONSTRUCTOR_EMPL_ID").IsNullOrEmpty()
                                && row.CheckGet("DESIGNER_EMPL_ID").IsNullOrEmpty())
                            {
                                result = true;
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "work",
                    Title = "В работу",
                    Group = "operations",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonName = "WorkButton",
                    Description = "Взать расчет в работу",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            SetWork();
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var k = Grid.GetPrimaryKey();
                        var row = Grid.SelectedItem;
                        if (row.CheckGet(k).ToInt() != 0)
                        {
                            if (UserGroups.Count > 0)
                            {
                                if (UserGroups.Contains("constructor") || UserGroups.Contains("designer"))
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
                    Name = "reject",
                    Title = "Отменить",
                    Group = "operations",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonName = "RejectButton",
                    Description = "Отменить расчет",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            ConfirmReject();
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var k = Grid.GetPrimaryKey();
                        var row = Grid.SelectedItem;
                        if (row.CheckGet(k).ToInt() != 0)
                        {
                            if (UserGroups.Count > 0)
                            {
                                if (UserGroups.Contains("constructor") || UserGroups.Contains("designer") || UserGroups.Contains("manager"))
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
                    Name = "open_chat",
                    Title = "Открыть внутренний чат",
                    Group = "operations",
                    MenuUse = true,
                    ButtonUse = false,
                    ButtonName = "",
                    Description = "Отменить расчет",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            OpenChat(1);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var k = Grid.GetPrimaryKey();
                        var row = Grid.SelectedItem;
                        if (row.CheckGet(k).ToInt() != 0)
                        {
                            if (row.CheckGet("CHAT_ID").ToInt() > 0)
                            {
                                result = true;
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "export_to_excel",
                    Title = "В Excel",
                    MenuUse = false,
                    ButtonUse = true,
                    ButtonName = "ToExcelButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        ExportToExcel();
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        if (Grid.Items.Count > 0)
                        {
                            result = true;
                        }
                        return result;
                    },

                });
            }
            Commander.Init(this);
        }

        /// <summary>
        /// Выбранная в гриде строка
        /// </summary>
        private Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// Название группы, к которой относится пользоватль
        /// </summary>
        private List<string> UserGroups;
        /// <summary>
        /// Уровень доступа пользователя к элементом интерфейса
        /// </summary>
        private Role.AccessMode UserAccessMode { get; set; }
        /// <summary>
        /// Заявки, созданные ранее этой даты считаются просроченными
        /// </summary>
        private DateTime LateDate { get; set; }
        /// <summary>
        /// Колонки таблицы для работы с выгрузкой в Excel
        /// </summary>
        private List<DataGridHelperColumn> GridColumns { get; set; }

        /// <summary>
        /// Обработка и выполнение команд
        /// </summary>
        /// <param name="command"></param>
        public void ProcessMessage(ItemMessage m)
        {
            string command = m.Action;
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "refresh":
                        Grid.LoadItems();
                        break;
                    case "setreject":
                        if (m.ContextObject != null)
                        {
                            var v = (Dictionary<string, string>)m.ContextObject;
                            SetReject(v);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            FromDate.Text = DateTime.Now.AddDays(-30).ToString("dd.MM.yyyy");
            ToDate.Text = DateTime.Now.ToString("dd.MM.yyyy");

            UserGroups = new List<string>();
        }

        /// <summary>
        /// Вычисление цвета ячейки по статусу расчета
        /// </summary>
        /// <param name="status">статус расчета</param>
        /// <returns></returns>
        private string GetCellColor(int status)
        {
            string color = "";

            switch (status)
            {
                case 1:
                    // Сделан запрос на расчет
                    color = HColor.Yellow;
                    break;
                case 2:
                    // Выполнен запрос на расчет
                    color = HColor.Green;
                    break;
                case 3:
                    // Отменен запрос на расчет
                    color = HColor.Red;
                    break;
                case 10:
                    // Просрочен запрос на расчет
                    color = HColor.Orange;
                    break;
            }

            return color;
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
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Дата",
                    Path="CREATED_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Format="dd.MM.yyyy HH:mm",
                    Width2=13,
                },
                new DataGridHelperColumn
                {
                    Header="Покупатель",
                    Path="CUSTOMER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=22,
                },
                new DataGridHelperColumn
                {
                    Header="Профиль",
                    Path="PROFILE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Название изделия",
                    Path="PRODUCT_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="FEFCO",
                    Path="FEFCO",
                    ColumnType=ColumnTypeRef.String,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание",
                    Path="NOTE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=30,
                },
                new DataGridHelperColumn
                {
                    Header="Габариты развертки",
                    Path="BLANK_SIZE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, (Dictionary<string, string> row) =>
                            {
                                var result=DependencyProperty.UnsetValue;

                                int status = row.CheckGet("BLANK_STATUS").ToInt();
                                if (status == 1)
                                {
                                    var createdDttm = row.CheckGet("CREATED_DTTM").ToDateTime("dd.MM.yyyy HH:mm:ss").Date;
                                    if (DateTime.Compare(createdDttm, LateDate) < 0)
                                    {
                                        status = 10;
                                    }
                                }

                                var color = GetCellColor(status);

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        }
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Количество на штампе",
                    Path="QTY_ON_STAMP",
                    ColumnType=ColumnTypeRef.Double,
                    Format="N0",
                    Width2=5,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, (Dictionary<string, string> row) =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                int status = row.CheckGet("BLANK_STATUS").ToInt();
                                if (status == 1)
                                {
                                    var createdDttm = row.CheckGet("CREATED_DTTM").ToDateTime("dd.MM.yyyy HH:mm:ss").Date;
                                    if (DateTime.Compare(createdDttm, LateDate) < 0)
                                    {
                                        status = 10;
                                    }
                                }

                                var color = GetCellColor(status);

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        }
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Дата вычисления развертки",
                    Path="BLANK_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=8,
                    Format="dd.MM.yyyy",
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, (Dictionary<string, string> row) =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                int status = row.CheckGet("BLANK_STATUS").ToInt();
                                if (status == 1)
                                {
                                    var createdDttm = row.CheckGet("CREATED_DTTM").ToDateTime("dd.MM.yyyy HH:mm:ss").Date;
                                    if (DateTime.Compare(createdDttm, LateDate) < 0)
                                    {
                                        status = 10;
                                    }
                                }

                                var color = GetCellColor(status);

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        }
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Стоимость штанцформы",
                    Path="STAMP_PRICE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=9,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, (Dictionary<string, string> row) =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                int status = row.CheckGet("STAMP_STATUS").ToInt();
                                if (status == 1)
                                {
                                    var createdDttm = row.CheckGet("CREATED_DTTM").ToDateTime("dd.MM.yyyy HH:mm:ss").Date;
                                    if (DateTime.Compare(createdDttm, LateDate) < 0)
                                    {
                                        status = 10;
                                    }
                                }

                                var color = GetCellColor(status);

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        }
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Дата оценки штанцформы",
                    Path="STAMP_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=8,
                    Format="dd.MM.yyyy",
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, (Dictionary<string, string> row) =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                int status = row.CheckGet("STAMP_STATUS").ToInt();
                                if (status == 1)
                                {
                                    var createdDttm = row.CheckGet("CREATED_DTTM").ToDateTime("dd.MM.yyyy HH:mm:ss").Date;
                                    if (DateTime.Compare(createdDttm, LateDate) < 0)
                                    {
                                        status = 10;
                                    }
                                }

                                var color = GetCellColor(status);

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        }
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Конструктор",
                    Path="CONSTRUCTOR_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, (Dictionary<string, string> row) =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                // Конструктор, который выполнил расчеты
                                if ((row.CheckGet("STAMP_STATUS").ToInt() == 2) || (row.CheckGet("BLANK_STATUS").ToInt() == 2))
                                {
                                    color = HColor.Green;
                                }
                                else if (!string.IsNullOrEmpty(row.CheckGet("CONSTRUCTOR_NAME")))
                                {
                                    color = HColor.Yellow;
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        }
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Чертеж",
                    Path="HAS_DRAWING_FILE",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание конструктора",
                    Path="CONSTRUCTOR_NOTE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Стоимость клише",
                    Path="CLICHE_PRICE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=9,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, (Dictionary<string, string> row) =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                int status = row.CheckGet("CLICHE_STATUS").ToInt();
                                if (status == 1)
                                {
                                    var createdDttm = row.CheckGet("CREATED_DTTM").ToDateTime("dd.MM.yyyy HH:mm:ss").Date;
                                    if (DateTime.Compare(createdDttm, LateDate) < 0)
                                    {
                                        status = 10;
                                    }
                                }

                                var color = GetCellColor(status);

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        }
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Дата оценки клише",
                    Path="CLICHE_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=8,
                    Format="dd.MM.yyyy",
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, (Dictionary<string, string> row) =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                int status = row.CheckGet("CLICHE_STATUS").ToInt();
                                if (status == 1)
                                {
                                    var createdDttm = row.CheckGet("CREATED_DTTM").ToDateTime("dd.MM.yyyy HH:mm:ss").Date;
                                    if (DateTime.Compare(createdDttm, LateDate) < 0)
                                    {
                                        status = 10;
                                    }
                                }

                                var color = GetCellColor(status);

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        }
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Дизайнер",
                    Path="DESIGNER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, (Dictionary<string, string> row) =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                // Расчет цены штанцформы выполнен
                                if (row.CheckGet("CLICHE_STATUS").ToInt() == 2)
                                {
                                    color = HColor.Green;
                                }
                                else if (!string.IsNullOrEmpty(row.CheckGet("DESIGNER_NAME")))
                                {
                                    color = HColor.Yellow;
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        }
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Примечание дизайнера",
                    Path="DESIGNER_NOTE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Сообщений от коллег",
                    Path="UNREAD_MESSAGE_QTY",
                    ColumnType=ColumnTypeRef.Double,
                    Format="N0",
                    Width2=4,
                    Stylers=new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row=>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("UNREAD_MESSAGE_QTY").ToInt() > 0)
                                {
                                    color = HColor.Red;
                                }
                                else if (row.CheckGet("MESSAGE_QTY").ToInt() > 0)
                                {
                                    color = HColor.YellowOrange;
                                }
                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header="Менеджер",
                    Path="MANAGER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn
                {
                    Header="Статус развертки",
                    Path="BLANK_STATUS",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Статус штанцформы",
                    Path="STAMP_STATUS",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Статус клише",
                    Path="CLICHE_STATUS",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ID профиля",
                    Path="PROFILE_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ID конструктора",
                    Path="CONSTRUCTOR_EMPL_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ID дизайнера",
                    Path="DESIGNER_EMPL_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ID менеджера",
                    Path="MANAGER_EMPL_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ID расчета цены",
                    Path="PRICE_CALC_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ID чата с коллегами",
                    Path="CHAT_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Статус расчета цены",
                    Path="PRICE_CALC_STATUS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("ID");
            Grid.SetSorting("CREATED_DTTM", ListSortDirection.Descending);
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.SearchText = SearchText;
            Grid.Commands = Commander;
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

                        int priceCalcStatus = row.CheckGet("PRICE_CALC_STATUS").ToInt();
                        // опубликован
                        if (priceCalcStatus == 3)
                        {
                            color = HColor.Green;
                        }
                        // отменен
                        else if (priceCalcStatus == 2)
                        {
                            color = HColor.Pink;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };

            GridColumns = columns;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0 && UserAccessMode != Role.AccessMode.ReadOnly)
                {
                    UpdateActions(selectedItem);
                }
            };

            //данные грида
            Grid.OnLoadItems = LoadItems;
            Grid.OnFilterItems = FilterItems;
            Grid.Init();
        }

        /// <summary>
        /// Загрузка общей информации
        /// </summary>
        public async void LoadRef()
        {
            // Активный сотрудник
            string emplId = Central.User.EmployeeId.ToString();
            UserGroups.Clear();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "RigCalculationTask");
            q.Request.SetParam("Action", "GetRef");
            q.Request.SetParam("EMPLOYEE_ID", emplId);

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    // менеджеры по работе с клиентами
                    var managersDS = ListDataSet.Create(result, "MANAGERS");
                    var list = new Dictionary<string, string>()
                    {
                        { "-1", "Все" },
                    };

                    var managers = new Dictionary<string, string>();
                    foreach (var item in managersDS.Items)
                    {
                        list.CheckAdd(item["ID"].ToInt().ToString(), item.CheckGet("FIO"));
                    }

                    ManagerName.Items = list;
                    // Если активный пользователь есть в списке, установим его в выбранном значении
                    if (list.ContainsKey(emplId))
                    {
                        ManagerName.SetSelectedItemByKey(emplId);
                    }
                    else
                    {
                        ManagerName.SetSelectedItemByKey("-1");
                    }

                    var employeeGroups = ListDataSet.Create(result, "USER_GROUPS");
                    if (employeeGroups.Items.Count > 0)
                    {
                        foreach (var item in employeeGroups.Items)
                        {
                            if (item.CheckGet("IN_GROUP").ToInt() == 1)
                            {
                                switch (item.CheckGet("CODE"))
                                {
                                    case "manager":
                                        UserGroups.Add("manager");
                                        break;
                                    case "preproduction_design_engineer":
                                        UserGroups.Add("constructor");
                                        break;
                                    case "preproduction_designer":
                                        UserGroups.Add("designer");
                                        break;
                                    case "programmer":
                                        UserGroups.Add("programmer");
                                        break;
                                    case "preproduction_engineer":
                                        UserGroups.Add("engineer");
                                        break;
                                }
                            }
                        }
                    }
                    if (UserGroups.Count == 0)
                    {
                        UserGroups.Add("read-only");
                    }
                }
            }
        }

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        private async void LoadItems()
        {
            GridToolbar.IsEnabled = false;
            // Обновляем дату опоздания при обновлении таблицы
            LateDate = DateTime.Now.Date.AddDays(-2);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "RigCalculationTask");
            q.Request.SetParam("Action", "List");

            var cmpl = (bool)CompletedCheckBox.IsChecked ? "1" : "0";
            q.Request.SetParam("COMPLETED", cmpl);
            q.Request.SetParam("FromDate", FromDate.Text);
            q.Request.SetParam("ToDate", ToDate.Text);

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
                    var ds = ListDataSet.Create(result, "RIG_TASK_LIST");
                    var processedDS = ProcessItems(ds);
                    Grid.UpdateItems(processedDS);
                }
                RefreshButton.Style = (Style)RefreshButton.TryFindResource("Button");
            }

            GridToolbar.IsEnabled = true;
        }

        /// <summary>
        /// Фильтрация строк табщицы
        /// </summary>
        private void FilterItems()
        {
            if (Grid.Items != null)
            {
                if (Grid.Items.Count > 0)
                {
                    bool doFilteringByManager = false;
                    var managerId = ManagerName.SelectedItem.Key.ToInt();
                    if (managerId > 0)
                    {
                        doFilteringByManager = true;
                    }

                    bool doFilterByGroup = false;
                    if (UserGroups.Contains("constructor") || UserGroups.Contains("designer"))
                    {
                        doFilterByGroup = true;
                    }

                    if (doFilteringByManager || doFilterByGroup)
                    {
                        var items = new List<Dictionary<string, string>>();
                        bool showCompleted = (bool)CompletedCheckBox.IsChecked;

                        foreach (Dictionary<string, string> row in Grid.Items)
                        {
                            bool includeByManager = true;
                            if (doFilteringByManager)
                            {
                                includeByManager = false;
                                if (row.CheckGet("MANAGER_EMPL_ID").ToInt() == managerId)
                                {
                                    includeByManager = true;
                                }

                            }

                            // Для конструктора показываем свои строки, для дизайнера - свои
                            bool includeByGroup = true;

                            if (doFilterByGroup)
                            {
                                includeByGroup = false;
                                if (UserGroups.Contains("constructor"))
                                {
                                    // Если есть непрочитанные сообщения, показываем в любом случае
                                    if (row.CheckGet("UNREAD_MESSAGE_QTY").ToInt() > 0)
                                    {
                                        includeByGroup = true;
                                    }
                                    else
                                    {
                                        var blankStatus = row.CheckGet("BLANK_STATUS").ToInt();
                                        var stampStatus = row.CheckGet("STAMP_STATUS").ToInt();
                                        if ((blankStatus > 0) || (stampStatus > 0))
                                        {
                                            if ((blankStatus == 1) || showCompleted)
                                            {
                                                includeByGroup = true;
                                            }

                                            if ((stampStatus == 1) || showCompleted)
                                            {
                                                includeByGroup = true;
                                            }
                                        }
                                    }

                                }

                                if (UserGroups.Contains("designer"))
                                {
                                    // Если есть непрочитанные сообщения, показываем в любом случае
                                    if (row.CheckGet("UNREAD_MESSAGE_QTY").ToInt() > 0)
                                    {
                                        includeByGroup = true;
                                    }
                                    else
                                    {
                                        var clicheStatus = row.CheckGet("CLICHE_STATUS").ToInt();
                                        if (clicheStatus > 0)
                                        {
                                            if ((clicheStatus == 1) || showCompleted)
                                            {
                                                includeByGroup = true;
                                            }
                                        }
                                    }
                                }
                            }

                            if (includeByManager && includeByGroup)
                            {
                                items.Add(row);
                            }
                        }

                        Grid.Items = items;
                    }
                }
            }
        }

        /// <summary>
        /// Обновление действий с записью
        /// </summary>
        /// <param name="selectedItem"></param>
        private void UpdateActions(Dictionary<string, string> selectedItem)
        {
            if (Grid.Items != null)
            {
                if (Grid.Items.Count > 0)
                {
                    SelectedItem = selectedItem;

                }
                else
                {
                    SelectedItem.Clear();
                }
            }
        }
        
        /// <summary>
        /// Удаление записи из БД
        /// </summary>
        /// <param name="id"></param>
        private async void DeleteData(int id)
        {
            bool resume = false;
            var dw = new DialogWindow("Вы действительно хотите удалить расчет оснастки?", "Удаление расчета", "", DialogWindowButtons.NoYes);
            if ((bool)dw.ShowDialog())
            {
                if (dw.ResultButton == DialogResultButton.Yes)
                {
                    resume = true;
                }
            }

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "RigCalculationTask");
                q.Request.SetParam("Action", "Delete");
                q.Request.SetParam("ID", id.ToString());

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
                        if (result.ContainsKey("ITEMS"))
                        {
                            Grid.LoadItems();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Обработка данных перед загрузкой в таблицу
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        private ListDataSet ProcessItems(ListDataSet ds)
        {
            ListDataSet _ds = ds;

            if (_ds.Items != null)
            {
                if (_ds.Items.Count > 0)
                {
                    foreach (var item in _ds.Items)
                    {
                        // Название изделия формируем из размеров и типа изделий
                        var productName = "";
                        var l = item.CheckGet("LENGTH").ToInt();
                        if (l > 0)
                        {
                            productName = $"{l}";
                            var w = item.CheckGet("WIDTH").ToInt();
                            if (w > 0)
                            {
                                productName = $"{productName}х{w}";
                            }
                            var h = item.CheckGet("HEIGHT").ToInt();
                            if (h > 0)
                            {
                                productName = $"{productName}х{h}";
                            }

                            productName = $"{productName} {item.CheckGet("PRODUCT_CLASS_NAME")}";
                        }

                        item.CheckAdd("PRODUCT_NAME", productName);

                        // Размер развертки
                        var blankSize = "";
                        var bl = item.CheckGet("BLANK_LENGTH").ToInt();
                        var bw = item.CheckGet("BLANK_WIDTH").ToInt();
                        if (bl > 0 && bw > 0)
                        {
                            blankSize = $"{bl}х{bw}";
                        }
                        item.CheckAdd("BLANK_SIZE", blankSize);
                    }
                }
            }

            return _ds;
        }

        /// <summary>
        /// Установка исполнителя работ
        /// </summary>
        private async void SetWork()
        {
            if (Grid.Items != null)
            {
                if (Grid.SelectedItem != null)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "RigCalculationTask");
                    q.Request.SetParam("Action", "SetWork");
                    q.Request.SetParam("ID", SelectedItem.CheckGet("ID"));
                    q.Request.SetParam("ROLE", UserGroups[0]);

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
                            if (result.ContainsKey("ITEMS"))
                            {
                                Grid.LoadItems();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Вызов окна для указания причины отмены расчета оснастки
        /// </summary>
        private void ConfirmReject()
        {
            if (Grid.Items != null)
            {
                if (Grid.SelectedItem != null)
                {
                    var p = Grid.SelectedItem;
                    p.CheckAdd("ROLE", UserGroups[0]);
                    // Примечание не передаём, там будет заполняться причина отказа
                    p.CheckAdd("NOTE", "");

                    var noteFrame = new RigCalculationTaskRejectNote();
                    noteFrame.ReceiverName = ControlName;
                    noteFrame.Edit(p);
                }
            }
        }

        /// <summary>
        /// Установка у расчета оснастки статуса Отклонено
        /// </summary>
        private async void SetReject(Dictionary<string, string> p)
        {
            int taskId = p.CheckGet("ID").ToInt();
            if (taskId > 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "RigCalculationTask");
                q.Request.SetParam("Action", "SetReject");
                q.Request.SetParam("ID", p.CheckGet("ID"));
                q.Request.SetParam("ROLE", p.CheckGet("ROLE"));

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
            }
        }

        /// <summary>
        /// Открытие вкладки с чатом по образцу
        /// </summary>
        /// <param name="chatType">Тип чата: 0 - чат с клиентом, 1 - чат с коллегами</param>
        private void OpenChat(int chatType = 0)
        {
            if (SelectedItem != null)
            {
                var chatFrame = new SampleChat();
                chatFrame.ObjectId = SelectedItem.CheckGet("ID").ToInt();
                chatFrame.ChatObject = "PriceCalc";
                chatFrame.ReceiverName = ControlName;
                chatFrame.ChatType = chatType;
                chatFrame.ChatId = SelectedItem.CheckGet("CHAT_ID").ToInt();
                chatFrame.Edit();
            }
        }

        /// <summary>
        /// Экспорт в Excel
        /// </summary>
        private async void ExportToExcel()
        {
            var list = Grid.Items;

            var eg = new ExcelGrid();
            eg.SetColumnsFromGrid(GridColumns);
            eg.Items = list;
            await Task.Run(() =>
            {
                eg.Make();
            });

        }

        /// <summary>
        /// При сммене дат меняет стиль кнопки обновления данных
        /// </summary>
        private void DateChanged()
        {
            RefreshButton.Style = (Style)RefreshButton.TryFindResource("FButtonPrimary");
        }

        private void ManagerName_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void CompletedCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
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
