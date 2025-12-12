using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Список новых образцов на подтверждение
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleConfirmationList : ControlBase
    {
        public SampleConfirmationList()
        {
            InitializeComponent();
            ControlTitle = "Новые образцы";
            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/samples_new/account_samples/new_samples1";
            RoleName = "[erp]sample_accounting";

            OnLoad = () =>
            {
                InitGrid();
                LoadRef();
                SetDefaults();
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
                    MenuUse = false,
                    ButtonUse = true,
                    ButtonName = "CreateButton",
                    Description = "Создание образца",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        AddMenu.IsOpen = true;
                    },
                    CheckEnabled = () =>
                    {
                        var result = true;
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "newplotter",
                    Title = "Добавить на плоттер",
                    Group = "item",
                    MenuUse = true,
                    ButtonUse = false,
                    ButtonName = "",
                    Description = "Добавить на плоттер",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        Edit(0, 0);
                    },
                    CheckEnabled = () =>
                    {
                        var result = true;
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "newproduct",
                    Title = "Добавить на линию",
                    Group = "item",
                    MenuUse = true,
                    ButtonUse = false,
                    ButtonName = "",
                    Description = "Добавить на линию",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        Edit(0, 1);
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
                    Description = "Изменение изделия/заготовки",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            Edit(id);
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
                    Name = "customizelabel",
                    Title = "Настроить ярлык",
                    Group = "item",
                    MenuUse = true,
                    ButtonUse = false,
                    ButtonName = "",
                    Description = "Открыть форму настройки ярлыка образца",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            var listIds = new List<int>() { id };
                            var labelCistomizing = new SampleLabelCustomizing();
                            labelCistomizing.SampleIdList = listIds;
                            labelCistomizing.Show();
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = true;
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "accept",
                    Title = "Подтвердить",
                    Group = "operations",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonName = "AcceptButton",
                    Description = "Принять образец в работу",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            GetConfirmationReason();
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var k = Grid.GetPrimaryKey();
                        var row = Grid.SelectedItem;
                        if (row != null)
                        {
                            if (row.CheckGet("CONFIRMATION").ToInt() < 2)
                            {
                                result = true;
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "reject",
                    Title = "Отклонить",
                    Group = "operations",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonName = "RejectButton",
                    Description = "Отклонить образец",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            var dw = new DialogWindow("Вы точно хотите отклонить образец?", "Отклонить образец", "", DialogWindowButtons.NoYes);
                            if ((bool)dw.ShowDialog())
                            {
                                UpdateStatus(2);
                            }
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = true;
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "sameboxes",
                    Title = "Показать похожие изделия",
                    Group = "forms",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonName = "ShowSameButton",
                    Description = "Показать похожие изделия",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            var profuctDuplicated = new SampleProductDuplicated();
                            profuctDuplicated.ReceiverName = ControlName;
                            profuctDuplicated.Edit(SelectedItem.CheckGet("ID").ToInt());
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var k = Grid.GetPrimaryKey();
                        var row = Grid.SelectedItem;
                        if (row != null)
                        {
                            if (row.CheckGet(k).ToInt() != 0)
                            {
                                result = row.CheckGet("DUPLICATED").ToInt() == 1;
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "attachments",
                    Title = "Прикрепленные файлы",
                    Group = "forms",
                    MenuUse = true,
                    ButtonUse = false,
                    Description = "Открыть список прикрепленных файлов",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            var sampleFiles = new SampleFiles();
                            sampleFiles.SampleId = id;
                            sampleFiles.ReturnTabName = ControlName;
                            sampleFiles.Show();
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
                    Name = "samplechat",
                    Title = "Открыть чат по образцу",
                    Group = "forms",
                    MenuUse = true,
                    ButtonUse = false,
                    Description = "Открыть чат по образцу",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            OpenChat(0);
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
                    Name = "innerchat",
                    Title = "Открыть внутренний чат",
                    Group = "forms",
                    MenuUse = true,
                    ButtonUse = false,
                    Description = "Открыть внутренний чат",
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
                            result = true;
                        }
                        return result;
                    },
                });
            }
            Commander.Init(this);
        }

        /// <summary>
        /// Признак фильтрации по менеджеру: false - по списку из сессии, true - по выбранному в выпадающем списке
        /// </summary>
        private bool SingleManager;

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        public void ProcessNavigation()
        {

        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="obj">сообщение</param>
        private void ProcessMessage(ItemMessage obj)
        {
            string command = obj.Action;
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "refresh":
                        Grid.LoadItems();
                        break;

                    case "setconfirmation":
                        var v = (Dictionary<string, string>)obj.ContextObject;
                        SetConfirmation(v);
                        break;
                }
            }
        }

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>()
            {
                new DataGridHelperColumn
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Веб-заявка",
                    Path="WEB_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Дата создания",
                    Path="DT_CREATED",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Дата изготовления",
                    Path="DT_COMPLETED",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Тип изготовления",
                    Path="PRODUCTION_TYPE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Статус",
                    Path="STATUS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Покупатель",
                    Path="CUSTOMER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=25,
                },
                new DataGridHelperColumn
                {
                    Header="Номер",
                    Path="SAMPLE_NUM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Образец",
                    Path="SAMPLE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=25,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.ForegroundColor,
                            row=>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("DUPLICATED").ToInt() == 1)
                                {
                                    color = HColor.BlueFG;
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
                    Header="Примечание",
                    Path="SAMPLE_COMMENT",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn
                {
                    Header="Комментарий",
                    Path="SAMPLE_NOTE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn
                {
                    Header="Картон",
                    Path="CARDBOARD_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.ForegroundColor,
                            row=>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("ANY_CARTON_FLAG").ToInt() == 1)
                                {
                                    color = HColor.BlueFG;
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
                    Header="Картон для образца",
                    Path="SAMPLE_CARDBOARD",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn
                {
                    Header="Количество",
                    Path="QTY_INFO",
                    ColumnType=ColumnTypeRef.String,
                    Width2=4,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row=>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("CONFIRMATION").ToInt() < 2)
                                {
                                    // Ограничение по количеству, требуется подтверждение
                                    if (row.CheckGet("QTY").ToInt() > row.CheckGet("LIMIT_QTY").ToInt())
                                    {
                                        color = HColor.Red;
                                    }
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
                    Header="Ограничение",
                    Path="LIMIT_REASON",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Обоснование",
                    Path="CONFIRMATION_REASON",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Файлы",
                    Path="FILE_IS",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Сообщений от клиента",
                    Path="MANAGER_UNREAD_MSG",
                    ColumnType=ColumnTypeRef.Double,
                    Format="N0",
                    Width2=4,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row=>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("MANAGER_UNREAD_MSG").ToInt() > 0)
                                {
                                    color = HColor.Red;
                                }
                                else if (row.CheckGet("CHAT_MSG").ToInt() > 0)
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
                    Header="Сообщений от коллег",
                    Path="UNREAD_MESSAGE_QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Double,
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
                    Header="Доставка",
                    Path="DELIVERY",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
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
                    Header="Подтверждение",
                    Path="CONFIRMATION",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Без марки картона",
                    Path="ANY_CARTON_FLAG",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Количество",
                    Path="QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Разрешенное количество",
                    Path="LIMIT_QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ИД ограничения",
                    Path="SALR_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ИД менеджера",
                    Path="MANAGER_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Дублирование изделий",
                    Path="DUPLICATED",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Тип образца",
                    Path="PRODUCTION_TYPE_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ID внутреннего чата",
                    Path="CHAT_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ID статуса",
                    Path="STATUS_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };

            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("ID");
            Grid.SetSorting("DT_CREATED", ListSortDirection.Descending);
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.Commands = Commander;
            Grid.SearchText = SearchText;

            // Раскраска строк
            Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                // Цвета фона строк
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        if (row.CheckGet("CONFIRMATION").ToInt() == 2)
                        {
                            // Количество подтверждено
                            color = HColor.Green;
                        }
                        else
                        {
                            if ((row.CheckGet("QTY").ToInt() > row.CheckGet("LIMIT_QTY").ToInt()) || (row.CheckGet("DUPLICATED").ToInt() == 1))
                            {
                                // Есть ограничения по количеству, нужно подтверждение
                                color = HColor.Yellow;
                            }
                            else
                            {
                                // Подтверждение не требуется
                                color = HColor.Green;
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
            Grid.OnFilterItems = FilterItems;
            Grid.Init();
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            SingleManager = false;
        }

        /// <summary>
        /// Загрузка общей информации
        /// </summary>
        public async void LoadRef()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "ListRef");

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    // менеджеры по работе с клиентами и продажам или пользователи определенной группы, куда входит авторизованный пользователь
                    string managerKey = "MANAGERS";
                    if (result.ContainsKey("USER_GROUP"))
                    {
                        managerKey = "USER_GROUP";
                    }
                    var managersDS = ListDataSet.Create(result, managerKey);
                    var list = new Dictionary<string, string>()
                    {
                        { "-1", "Все" },
                    };

                    var managers = new Dictionary<string, string>();
                    foreach (var item in managersDS.Items)
                    {
                        list.CheckAdd(item["ID"].ToInt().ToString(), item["FIO"]);
                    }

                    ManagerName.Items = list;

                    // Если активный пользователь есть в списке, установим его в выбранном значении
                    string emplId = Central.User.EmployeeId.ToString();
                    if (list.ContainsKey(emplId))
                    {
                        ManagerName.SetSelectedItemByKey(emplId);
                    }
                    else
                    {
                        ManagerName.SetSelectedItemByKey("-1");
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
            Grid.ShowSplash();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "ListAccepted");

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
                    var sampleDS = ListDataSet.Create(result, "SAMPLES");
                    Grid.UpdateItems(sampleDS);
                }
            }

            GridToolbar.IsEnabled = true;
            Grid.HideSplash();
        }

        /// <summary>
        /// обновление методов работы с выбранной записью
        /// </summary>
        /// <param name="selectedItem">выбранная запись</param>
        private void UpdateActions(Dictionary<string, string> selectedItem)
        {
            SelectedItem = selectedItem;
        }

        /// <summary>
        /// Обработка и фильтрация строк
        /// </summary>
        private void FilterItems()
        {
            if (Grid.Items != null)
            {
                if (Grid.Items.Count > 0)
                {

                    bool doFilteringByManager = false;
                    var managerIds = new List<int>();
                    if (SingleManager)
                    {
                        if (ManagerName.SelectedItem.Key != null)
                        {
                            var managerId = ManagerName.SelectedItem.Key.ToInt();
                            if (managerId > 0)
                            {
                                managerIds.Add(managerId);
                                doFilteringByManager = true;
                            }
                        }
                    }
                    else
                    {
                        var ids = Central.SessionValues["ManagersConfig"]["ListActive"];
                        if (!string.IsNullOrEmpty(ids))
                        {
                            var arr = ids.Split(',');
                            foreach (var item in arr)
                            {
                                managerIds.Add(item.ToInt());
                            }
                            doFilteringByManager = true;
                        }
                    }

                    if (doFilteringByManager)
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in Grid.Items)
                        {
                            bool includeByManager = false;

                            if (doFilteringByManager)
                            {
                                var l = managerIds.ToArray();
                                if (row.CheckGet("MANAGER_ID").ToInt().ContainsIn(l))
                                {
                                    includeByManager = true;
                                }
                            }
                            // если выбраны все, показываем только образцы из своей группы
                            else
                            {
                                int emplId = row.CheckGet("MANAGER_ID").ToInt();
                                if (ManagerName.Items.ContainsKey(emplId.ToString()))
                                {
                                    includeByManager = true;
                                }
                            }

                            if (includeByManager)
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
        /// Созранение данных о подтверждении образца
        /// </summary>
        /// <param name="v"></param>
        private async void SetConfirmation(Dictionary<string, string>  v)
        {
            int sampleId = v.CheckGet("ID").ToInt();
            if (sampleId > 0)
            {
                GridToolbar.IsEnabled = false;
                Grid.ShowSplash();

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "Samples");
                q.Request.SetParam("Action", "SetConfirmation");
                q.Request.SetParams(v);

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
                        Grid.LoadItems();
                    }
                }

                GridToolbar.IsEnabled = true;
                Grid.HideSplash();
            }
        }

        /// <summary>
        /// Открытие вкладки с чатом по образцу
        /// </summary>
        private void OpenChat(int chatType = 0)
        {
            if (Grid.SelectedItem != null)
            {
                var chatFrame = new SampleChat();
                chatFrame.ChatType = chatType;
                chatFrame.ChatId = Grid.SelectedItem.CheckGet("CHAT_ID").ToInt();
                chatFrame.ObjectId = Grid.SelectedItem.CheckGet("ID").ToInt();
                chatFrame.ReceiverName = ControlName;
                chatFrame.Recipient = 32;
                chatFrame.RawMissingFlag = 0;
                chatFrame.UnReadCheckBox.IsChecked = true;
                chatFrame.Edit();
            }
        }

        /// <summary>
        /// Открывает форму редактирования образца
        /// </summary>
        /// <param name="sampleId"></param>
        private void Edit(int sampleId, int productionType = 0)
        {
            int confirmation = 0;
            if (sampleId > 0)
            {
                productionType = Grid.SelectedItem.CheckGet("PRODUCTION_TYPE_ID").ToInt();
                confirmation = Grid.SelectedItem.CheckGet("CONFIRMATION").ToInt();
            }

            if (productionType == 0)
            {
                var sampleForm = new Sample();
                sampleForm.ReceiverName = ControlName;
                sampleForm.Confirmation = confirmation;
                sampleForm.Edit(sampleId);
            }
            else if (productionType == 1)
            {
                var sampleProductForm = new SampleProduction();
                sampleProductForm.ReceiverName = ControlName;
                sampleProductForm.Edit(sampleId);
            }
            else
            {

            }
        }

        /// <summary>
        /// Обновление статуса образца
        /// </summary>
        /// <param name="newStatus"></param>
        private async void UpdateStatus(int newStatus)
        {
            if (Grid.SelectedItem != null)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "Samples");
                q.Request.SetParam("Action", "UpdateStatus");
                q.Request.SetParam("SAMPLE_ID", Grid.SelectedItem.CheckGet("ID"));
                q.Request.SetParam("STATUS", newStatus.ToString());

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
                        if (result.Count > 0)
                        {
                            Grid.LoadItems();
                        }
                    }
                }

            }
        }

        /// <summary>
        /// Получение данных для подтверждения образца
        /// </summary>
        private void GetConfirmationReason()
        {
            var p = new Dictionary<string, string>()
            {
                { "ID", SelectedItem.CheckGet("ID") },
                { "LIMIT_QTY_ID", SelectedItem.CheckGet("SALR_ID") },
                { "REASON", "0" },
                { "STATUS", SelectedItem.CheckGet("STATUS_ID") },
                { "DT_COMPLETED", SelectedItem.CheckGet("DT_COMPLETED") },
            };


            if (SelectedItem.CheckGet("QTY").ToInt() <= SelectedItem.CheckGet("LIMIT_QTY").ToInt())
            {
                // Если ограничений нет, то при нажатии на кнопку подтверждения сразу подтверждаем
                SetConfirmation(p);
            }
            else
            {
                var confirmationForm = new SampleSetConfirmation();
                confirmationForm.ReceiverName = ControlName;
                confirmationForm.Edit(p);
            }
        }

        private void ManagerName_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SingleManager = true;
            Grid.UpdateItems();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            Edit(0, 0);
        }

        private void NewProduct_Click(object sender, RoutedEventArgs e)
        {
            Edit(0, 1);
        }

        private void SelectManagerButton_Click(object sender, RoutedEventArgs e)
        {
            SingleManager = false;
            var selectManager = new SampleSelectManager();
            selectManager.ReceiverName = ControlName;
            selectManager.Show();
        }
    }
}
