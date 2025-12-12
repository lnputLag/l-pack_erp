using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Shipments;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Список заявок для подтверждения даты отделом ОПП
    /// </summary>
    /// <author>sviridov_ae</author>
    public partial class PreproductionConfirmOrderList : ControlBase
    {
        /// <summary>
        /// Конструктор класса таба 
        /// Список заявок для подтверждения даты отделом ОПП
        /// </summary>
        public PreproductionConfirmOrderList()
        {
            ControlTitle = "Подтверждение заявок";
            RoleName = "[erp]preproduction_confirm_order";
            DocumentationUrl = "/doc/l-pack-erp-new/planing/confirm_application/list_application";
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
                FormInit();
                SetDefaults();
                OrderGridInit();
                PositionGridInit();
                HistoryGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                OrderGrid.Destruct();
                PositionGrid.Destruct();
                HistoryGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                OrderGrid.ItemsAutoUpdate = true;
                OrderGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                OrderGrid.ItemsAutoUpdate = false;
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
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        if (OrderGrid != null && OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0)
                        {
                            OrderGrid.Commands.Message = new ItemMessage() { Action = "refresh" , Message = $"{OrderGrid.SelectedItem.CheckGet("ORDER_ID")}"};
                        }
                        Refresh();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "show_workcenter_workload",
                    Group = "main",
                    Enabled = true,
                    Title = "Загруженность",
                    Description = "Загруженность станков по принятым заявкам",
                    ButtonUse = true,
                    ButtonControl = ShowWorkcenterWorkloadButton,
                    ButtonName = "ShowWorkcenterWorkloadButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        ShowWorkcenterWorkload();
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
                    ButtonName = "HelpButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    HotKey = "F1",
                    Action = () =>
                    {
                        Central.ShowHelp(DocumentationUrl);
                    },
                });
            }

            Commander.SetCurrentGridName("OrderGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "show_in_composition_list",
                    Title = "Отчёт по композициям",
                    Description = "Показать в отчёте по композициям",
                    Group = "order_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = ShowInCompositionListButton,
                    ButtonName = "ShowInCompositionListButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        ShowInCompositionList();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (OrderGrid != null && OrderGrid.Items != null && OrderGrid.Items.Count > 0)
                        {
                            if (OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "show_chat",
                    Title = "Чат",
                    Description = "Открыть чат",
                    Group = "order_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = ShowChatButton,
                    ButtonName = "ShowChatButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        ShowChat();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (OrderGrid != null && OrderGrid.Items != null && OrderGrid.Items.Count > 0)
                        {
                            if (OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "show_loading_scheme",
                    Title = "Показать схему погрузки",
                    Group = "order_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        ShowLoadingScheme();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (OrderGrid != null && OrderGrid.Items != null && OrderGrid.Items.Count > 0)
                        {
                            if (OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "confirm",
                    Title = "Утвердить дату / Посмотреть загрузку",
                    Group = "order_grid_confirm",
                    MenuUse = true,
                    Enabled = false,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        Confirm();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (OrderGrid != null && OrderGrid.Items != null && OrderGrid.Items.Count > 0)
                        {
                            if (OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "change_factory",
                    Title = "Изменить площадку",
                    Group = "order_grid_confirm",
                    MenuUse = true,
                    Enabled = false,
                    AccessLevel = Role.AccessMode.FullAccess,
                    ButtonUse = true,
                    ButtonControl = ChangeFactoryButton,
                    ButtonName = "ChangeFactoryButton",
                    Action = () =>
                    {
                        ChangeFactory();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (OrderGrid != null && OrderGrid.Items != null && OrderGrid.Items.Count > 0)
                        {
                            if (OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0)
                            {
                                if (
                                    (
                                        OrderGrid.SelectedItem.CheckGet("ORDER_TYPE").ToInt() == 1
                                        && (OrderGrid.SelectedItem.CheckGet("ORDER_STATUS_ID").ToInt() == 6 
                                        || OrderGrid.SelectedItem.CheckGet("ORDER_STATUS_ID").ToInt() == 16)
                                        && OrderGrid.SelectedItem.CheckGet("SHIPMENT_ID").ToInt() == 0
                                    )
                                    || OrderGrid.Items.Sum(x => x.CheckGet("CHECKED_FLAG").ToInt()) > 0)
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
                    Name = "send_to_rack",
                    Title = "На стеллажный склад",
                    Group = "order_grid_rack",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = SendToRackButton,
                    ButtonName = "SendToRackButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        SendToRack(1);
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0)
                        {
                            if (PositionGrid != null && PositionGrid.Items != null && PositionGrid.Items.Count > 0)
                            {
                                if (PositionGrid.Items.First().CheckGet("ORDER_ID").ToInt() == OrderGrid.SelectedItem.CheckGet("ORDER_ID").ToInt())
                                {
                                    // Если есть хотя бы одна позиция с признаком "На обычный склад"
                                    if (PositionGrid.Items.FirstOrDefault(x => x.CheckGet("PLACED_IN_RACK_FLAG").ToInt() == 0) != null)
                                    {
                                        result = true;
                                    }
                                }
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "non_send_to_rack",
                    Title = "На обычный склад",
                    Group = "order_grid_rack",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = NonSendToRackButton,
                    ButtonName = "NonSendToRackButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        SendToRack(0);
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0)
                        {
                            if (PositionGrid != null && PositionGrid.Items != null && PositionGrid.Items.Count > 0)
                            {
                                if (PositionGrid.Items.First().CheckGet("ORDER_ID").ToInt() == OrderGrid.SelectedItem.CheckGet("ORDER_ID").ToInt())
                                {
                                    // Если есть хотя бы одна позиция с признаком "На стеллажный склад"
                                    if (PositionGrid.Items.FirstOrDefault(x => x.CheckGet("PLACED_IN_RACK_FLAG").ToInt() == 1) != null)
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

            Commander.SetCurrentGridName("PositionGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "show_technological_map",
                    Title = "Открыть техкарту",
                    Group = "position_grid_default",
                    Enabled = true,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = ShowTechnologicalMapButton,
                    ButtonName = "ShowTechnologicalMapButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        ShowTechnologicalMap();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "edit_scheme",
                    Title = "Изменить схему производства",
                    Group = "position_grid_default",
                    Enabled = true,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = EditSchemeButton,
                    ButtonName = "EditSchemeButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        EditScheme();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (PositionGrid != null && PositionGrid.Items != null && PositionGrid.Items.Count > 0)
                        {
                            if (PositionGrid.SelectedItem != null && PositionGrid.SelectedItem.Count > 0)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "show_technological_map",
                    Title = "Открыть техкарту",
                    Group = "position_grid_default",
                    Enabled = true,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = ShowTechnologicalMapButton,
                    ButtonName = "ShowTechnologicalMapButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        ShowTechnologicalMap();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (PositionGrid != null && PositionGrid.Items != null && PositionGrid.Items.Count > 0)
                        {
                            if (PositionGrid.SelectedItem != null && PositionGrid.SelectedItem.Count > 0)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
            }

            Commander.Init(this);
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Основной датасет с данными по заявкам на гофропроизводство
        /// </summary>
        public ListDataSet OrderGridDataSet { get; set; }

        /// <summary>
        /// Основной датасет с данными по позициям заявки на гофропроизводство
        /// </summary>
        public ListDataSet PositionGridDataSet { get; set; }

        /// <summary>
        /// Основной датасет с данными по изменениям заявки на гофропроизводство
        /// </summary>
        public ListDataSet HistoryGridDataSet { get; set; }

        /// <summary>
        /// Выбранная запись в гриде изменений по завке на гофропроизводство
        /// </summary>
        public Dictionary<string, string> HistoryGridSelectedItem { get; set; }

        /// <summary>
        /// Флаг возможности установить флаг высокодоходной заявки
        /// </summary>
        public bool CanSetSpecialOrderFlag { get; set; }

        public void FormInit()
        {
            //инициализация формы
            {
                Form = new FormHelper();

                //колонки формы
                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="SEARCH",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=SearchText,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                };

                Form.SetFields(fields);
            }
        }

        public void SetDefaults()
        {
            OrderGridDataSet = new ListDataSet();
            OrderGrid.SelectedItem = new Dictionary<string, string>();
            PositionGridDataSet = new ListDataSet();
            HistoryGridDataSet = new ListDataSet();
            HistoryGridSelectedItem = new Dictionary<string, string>();

            var factorySelectBoxItems = new Dictionary<string, string>();
            factorySelectBoxItems.Add("1", "Л-ПАК ЛИПЕЦК");
            factorySelectBoxItems.Add("2", "Л-ПАК КАШИРА");
            FactorySelectBox.SetItems(factorySelectBoxItems);
            FactorySelectBox.SetSelectedItemByKey("1");

            var fullnessOrderSelectBoxItems = new Dictionary<string, string>();
            fullnessOrderSelectBoxItems.Add("0", "Все заявки");
            fullnessOrderSelectBoxItems.Add("1", "Полные");
            fullnessOrderSelectBoxItems.Add("2", "Неполные");
            FullnessOrderSelectBox.SetItems(fullnessOrderSelectBoxItems);
            FullnessOrderSelectBox.SetSelectedItemByKey("0");

            GetManagerListForSelectBox();

            var statusSelectBoxItems = new Dictionary<string, string>();
            statusSelectBoxItems.Add("-1", "Все статусы");
            statusSelectBoxItems.Add("1", "Согласование ОРК"); // statusSelectBoxItems.Add("3", "Согласование ОРК"); statusSelectBoxItems.Add("13", "Частичное согласование ОРК");
            statusSelectBoxItems.Add("2", "Подтверждение ОРК"); // statusSelectBoxItems.Add("23", "Подтверждение ОРК"); statusSelectBoxItems.Add("33", "Частичное подтверждение ОРК");
            statusSelectBoxItems.Add("3", "Согласование ПДС"); // statusSelectBoxItems.Add("6", "Согласование ПДС"); statusSelectBoxItems.Add("16", "Частичное согласование ПДС");

            //statusSelectBoxItems.Add("1", "Новая");
            //statusSelectBoxItems.Add("3", "Согласование ОРК");
            //statusSelectBoxItems.Add("13", "Частичное согласование ОРК");
            //statusSelectBoxItems.Add("23", "Подтверждение ОРК");
            //statusSelectBoxItems.Add("33", "Частичное подтверждение ОРК");
            //statusSelectBoxItems.Add("6", "Согласование ПДС");
            //statusSelectBoxItems.Add("16", "Частичное согласование ПДС");

            StatusSelectBox.SetItems(statusSelectBoxItems);
            StatusSelectBox.SetSelectedItemByKey("3");
        }

        public async void GetManagerListForSelectBox()
        {
            var p = new Dictionary<string, string>();
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "ConfirmOrder");
            q.Request.SetParam("Action", "ListOrderManager");
            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() => 
            {
                q.DoQuery();
            });

            Dictionary<string, string> managerSelectBoxItems = new Dictionary<string, string>();
            managerSelectBoxItems.Add("0", "Все менеджеры");

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        foreach (var item in ds.Items)
                        {
                            managerSelectBoxItems.Add(item.CheckGet("ID"), item.CheckGet("NAME"));
                        }
                    }
                }
            }

            ManagerSelectBox.SetItems(managerSelectBoxItems);
            ManagerSelectBox.SetSelectedItemByKey("0");
        }

        /// <summary>
        /// Инициализация грида списка заявок на гофропроизводство
        /// </summary>
        public void OrderGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header = "*",
                        Path = "CHECKED_FLAG",
                        ColumnType = ColumnTypeRef.Boolean,
                        Width2=3,
                        Editable = true,
                        OnClickAction = (row, el) =>
                        {
                            if (row.CheckGet("ORDER_STATUS_ID").ToInt() == 6 || row.CheckGet("ORDER_STATUS_ID").ToInt() == 16)
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид заявки",
                        Path="ORDER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата отгрузки",
                        Description="Дата отгрузки в производственных сутках",
                        Path="SHIPMENT_DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy",
                        Width2=9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата доставки",
                        Path="DELIVERY_DATE",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Утверждённая дата",
                        Path="PROPOSED_DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy",
                        Width2=9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус",
                        Path="ORDER_STATUS",
                        ColumnType=ColumnTypeRef.String,
                        Width2=12,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // Желтый -- Частичное согласование ОРК, Частичное согласование ПДС, Частичное подтверждение ОРК
                                    if (row.CheckGet("ORDER_STATUS_ID").ToInt() == 13
                                        || row.CheckGet("ORDER_STATUS_ID").ToInt() == 16
                                        || row.CheckGet("ORDER_STATUS_ID").ToInt() == 33)
                                    {
                                        color = HColor.Yellow;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Высокодоходная",
                        Path="SPECIAL_ORDER_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=6,
                        Editable = CanSetSpecialOrderFlag,
                        OnClickAction = (row, el) =>
                        {
                            try
                            {
                                CheckSpecialOrderFlag((!row.CheckGet("SPECIAL_ORDER_FLAG").ToBool()).ToInt(), row);
                            }
                            catch (Exception ex)
                            {

                            }

                            return true;
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Покупатель",
                        Path="CUSTOMER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=29,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // Оранжевый -- Покупатель работает по предоплате и мы ожидаем оплату предоплаты
                                    if (row.CheckGet("PREPAYMENT_FLAG").ToInt() == 1 && row.CheckGet("PREPAY_CONFIRM").ToInt() == 1)
                                    {
                                        color = HColor.Orange;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Грузополучатель",
                        Path="CONSIGNEE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=27,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер заявки",
                        Path="ORDER_NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Чат",
                        Path="CHAT_MESSAGE_COUNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Липецк",
                        Description="Может производиться в Липецке",
                        Path="PRODUCTION_LIPETSK_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Кашира",
                        Description="Может производиться в Кашире",
                        Path="PRODUCTION_KASHIRA_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Самовывоз",
                        Path="PICKUP_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Неполная заявка",
                        Path="PARTIAL_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=5,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // 
                                    if (row.CheckGet("PARTIAL_FLAG").ToInt() > 0)
                                    {
                                        color = HColor.Red;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Процент загрузки",
                        Path="LOAD_PERCENT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // Красный -- Процент загрузки > 120
                                    if (row.CheckGet("LOAD_PERCENT").ToInt() > 120)
                                    {
                                        color = HColor.Red;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Счёт на предоплату",
                        Path="PREPAID_NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=11,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Примечание клиента",
                        Path="CUSTOMER_NOTE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=19,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Мен-ер по заявкам",
                        Path="ORDER_MANAGER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Мен-ер по продажам",
                        Path="SALES_MANAGER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата создания",
                        Path="ORDER_CREATE_DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид отгрузки",
                        Path="SHIPMENT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Тип покупателя",
                        Path="CUSTOMER_TYPE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=11,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Статус",
                        Path="ORDER_STATUS_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Тип",
                        Path="ORDER_TYPE",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид менеджера по заявке",
                        Path="ORDER_MANAGER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид счёта на предоплату",
                        Path="PREPAID_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Email покупателя",
                        Path="CUSTOMER_CONTACT_EMAIL",
                        ColumnType=ColumnTypeRef.String,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Работа по предоплате",
                        Path="PREPAYMENT_FLAG",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ожидаем оплату предоплаты",
                        Path="PREPAY_CONFIRM",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид площадки",
                        Path="FACTORY_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус схемы загрузки",
                        Path="SHIPMENT_SCHEME_STATUS",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Имя файла схемы погрузки от покупателя",
                        Path="SHIPMENT_SCHEME_FILE",
                        ColumnType=ColumnTypeRef.String,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество позиций в заявке, которые можно производить в Липецке",
                        Path="PRODUCTION_LIPETSK_COUNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество позиций в заявке, которые можно производить в Кашире",
                        Path="PRODUCTION_KASHIRA_COUNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество позиий в заявке",
                        Path="POSITION_COUNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                };
                OrderGrid.SetColumns(columns);
                OrderGrid.SearchText = SearchText;
                OrderGrid.OnLoadItems = OrderGridLoadItems;
                OrderGrid.SetPrimaryKey("ORDER_ID");
                OrderGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                OrderGrid.AutoUpdateInterval = 60 * 5;

                OrderGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    {
                        StylerTypeRef.BackgroundColor,
                        (Dictionary<string, string> row) =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            // Синий -- статус заявки = 3 - Согласование ОРК, 13 - Частичное согласование ОРК
                            if (row.CheckGet("ORDER_STATUS_ID").ToInt() == 3 
                                || row.CheckGet("ORDER_STATUS_ID").ToInt() == 13)
                            {
                                color = HColor.Blue;
                            }

                            // Зелёный -- статус заявки = 23 - подтверждение ОРК
                            if (row.CheckGet("ORDER_STATUS_ID").ToInt() == 23)
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

                OrderGrid.OnFilterItems = () =>
                {
                    if (OrderGrid.Items != null && OrderGrid.Items.Count > 0)
                    {
                        // Фильтрация по площадке
                        if (FactorySelectBox.SelectedItem.Key != null)
                        {
                            var key = FactorySelectBox.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            items.AddRange(OrderGrid.Items.Where(x => x.CheckGet("FACTORY_ID").ToInt() == key));

                            OrderGrid.Items = items;
                        }

                        // Фильтрация заявок по флагу неполных заявок
                        // 0 -- Все заявки
                        // 1 -- Только полные заявки
                        // 2 -- Только неполные заявки
                        if (FullnessOrderSelectBox.SelectedItem.Key != null)
                        {
                            var key = FullnessOrderSelectBox.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            switch (key)
                            {
                                // Все заявки
                                case 0:
                                    items = OrderGrid.Items;
                                    break;

                                // Только полные заявки
                                case 1:
                                    items.AddRange(OrderGrid.Items.Where(x => x.CheckGet("PARTIAL_FLAG").ToInt() == 0));
                                    break;

                                // Только неполные заявки
                                case 2:
                                    items.AddRange(OrderGrid.Items.Where(x => x.CheckGet("PARTIAL_FLAG").ToInt() == 1));
                                    break;

                                default:
                                    items = OrderGrid.Items;
                                    break;
                            }

                            OrderGrid.Items = items;
                        }

                        // Фильтрация заявок по менеджеру по заявкам
                        // 0 -- Все менеджеры
                        if (ManagerSelectBox.SelectedItem.Key != null)
                        {
                            var key = ManagerSelectBox.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            switch (key)
                            {
                                // Все менеджеры
                                case 0:
                                    items = OrderGrid.Items;
                                    break;

                                default:
                                    items.AddRange(OrderGrid.Items.Where(x => x.CheckGet("ORDER_MANAGER_ID").ToInt() == key));
                                    break;
                            }

                            OrderGrid.Items = items;
                        }

                        // Фильтрация заявок по статусу
                        // -1 -- Все статусы
                        if (StatusSelectBox.SelectedItem.Key != null)
                        {
                            var key = StatusSelectBox.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            switch (key)
                            {
                                // Все статусы
                                case -1:
                                    items = OrderGrid.Items;
                                    break;

                                // Согласование ОРК (3, 13)
                                case 1:
                                    items.AddRange(OrderGrid.Items.Where(x => x.CheckGet("ORDER_STATUS_ID").ToInt() == 3 || x.CheckGet("ORDER_STATUS_ID").ToInt() == 13));
                                    break;

                                // Подтверждение ОРК (23, 33)
                                case 2:
                                    items.AddRange(OrderGrid.Items.Where(x => x.CheckGet("ORDER_STATUS_ID").ToInt() == 23 || x.CheckGet("ORDER_STATUS_ID").ToInt() == 33));
                                    break;

                                // Согласование ПДС (6, 16)
                                case 3:
                                    items.AddRange(OrderGrid.Items.Where(x => x.CheckGet("ORDER_STATUS_ID").ToInt() == 6 || x.CheckGet("ORDER_STATUS_ID").ToInt() == 16));
                                    break;

                                default:
                                    items = OrderGrid.Items;
                                    break;
                            }

                            OrderGrid.Items = items;
                        }
                    }
                };

                //при выборе строки в гриде, обновляются актуальные действия для записи
                OrderGrid.OnSelectItem = selectedItem =>
                {
                    ClearDependendGrids();

                    if (selectedItem != null && selectedItem.Count > 0 && OrderGrid.Items.FirstOrDefault(x => x.CheckGet("ORDER_ID").ToInt() == selectedItem.CheckGet("ORDER_ID").ToInt()) != null)
                    {
                        PositionGridLoadItems();
                        HistoryGridLoadItems();
                    }
                    else
                    {
                        OrderGrid.SelectRowFirst();
                    }

                    UpdateAction();
                };

                OrderGrid.Commands = Commander;
                OrderGrid.UseProgressSplashAuto = false;
                OrderGrid.Init();
            }
        }

        public void ClearDependendGrids()
        {
            if (PositionGrid != null)
            {
                PositionGrid.ClearItems();
            }

            if (HistoryGrid != null)
            {
                HistoryGrid.ClearItems();
            }
        }

        public async void OrderGridLoadItems()
        {
            DisableControls();

            ClearDependendGrids();

            var p = new Dictionary<string, string>();
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "ConfirmOrder");
            q.Request.SetParam("Action", "List");
            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            OrderGridDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    OrderGridDataSet = ListDataSet.Create(result, "ITEMS");
                    if (OrderGrid.Items != null)
                    {
                        foreach (var item in OrderGridDataSet.Items)
                        {
                            var row = OrderGrid.Items.FirstOrDefault(x => x.CheckGet("ORDER_ID").ToInt() == item.CheckGet("ORDER_ID").ToInt());
                            if (row != null)
                            {
                                item.CheckAdd("CHECKED_FLAG", row.CheckGet("CHECKED_FLAG"));
                            }
                            else
                            {
                                item.CheckAdd("CHECKED_FLAG", "0");
                            }
                        }
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
            OrderGrid.UpdateItems(OrderGridDataSet);

            EnableControls();
        }

        /// <summary>
        /// Инициализация грида позиций выбранной заявки на гофропроизводство
        /// </summary>
        public void PositionGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn()
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=3,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Path="POSITION_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Path="PRODUCT_CODE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Продукция",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=23,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // Желтый -- изделия, где 3 и более цвета печати.
                                    if (row.CheckGet("COLOR_COUNT").ToInt() >= 3)
                                    {
                                        color = HColor.Yellow;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Картон",
                        Path="CARDBOARD_DESCRIPTION",
                        ColumnType=ColumnTypeRef.String,
                        Width2=20,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ограничение",
                        Path="QUANTITY_LIMIT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Path="QUANTITY_IN_ORDER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // Красный -- Есть в схеме производства есть станок ФОЛД(Скл многоточ) и в заявке менее 5000 шт. (меньше минимальной партии для станка ФОЛД(Скл многоточ))
                                    if (row.CheckGet("FOLD_FLAG").ToInt() == 1 && row.CheckGet("QUANTITY_IN_ORDER").ToInt() < 5000)
                                    {
                                        color = HColor.Red;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Цена",
                        Path="PRICE",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Поддонов",
                        Path="PALLET_QUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="На последнем поддоне",
                        Path="LAST_PALLET_QUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Транспортный пакет",
                        Path="TRANSPORT_PACKAGE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Поддон",
                        Path="NAME_PALLE_TYPE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                        Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    if (row.CheckGet("IS_STANDART").ToInt() == 0)
                                    {
                                        color = HColor.Red;
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
                        Header="Укладка",
                        Path="NAME_LAYING",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Заготовка",
                        Description="Размер заготовки (Длина х Ширина)",
                        Path="BLANK_SIZE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="На складе",
                        Path="QUANTITY_ON_STOCK",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Площадь",
                        Path="SQUARE",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=7,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    if (row.CheckGet("ORDER_NOTE").IsNullOrEmpty()
                                        &&
                                        (
                                            (
                                                row.CheckGet("SQUARE").ToInt() < 1500 && row.CheckGet("COMPOSITION_TYPE").ToInt() == 2
                                            )
                                            ||
                                            (
                                                row.CheckGet("SQUARE").ToInt() < 3000
                                                &&
                                                (
                                                    row.CheckGet("COMPOSITION_TYPE").ToInt() == 3 || row.CheckGet("COMPOSITION_TYPE").ToInt() == 4 || row.CheckGet("COMPOSITION_TYPE").ToInt() == 5
                                                )
                                            )
                                            ||
                                            (
                                                row.CheckGet("COMPONENT").ToInt() == 0 && row.CheckGet("SQUARE").ToInt() < 300 && row.CheckGet("COMPOSITION_TYPE").ToInt() == 1
                                            )
                                            ||
                                            (
                                                row.CheckGet("COMPONENT").ToInt() == 1 && row.CheckGet("SQUARE").ToInt() < 70 && row.CheckGet("COMPOSITION_TYPE").ToInt() == 1
                                            )
                                        )
                                    )
                                    {
                                        color = HColor.Red;
                                    }
                                    else if ( !string.IsNullOrEmpty(row.CheckGet("ORDER_NOTE"))
                                        &&
                                        (
                                            (
                                                row.CheckGet("SQUARE").ToInt() < 300 && row.CheckGet("COMPOSITION_TYPE").ToInt() == 1
                                            )
                                            ||
                                            (
                                                row.CheckGet("SQUARE").ToInt() < 1500 && row.CheckGet("COMPOSITION_TYPE").ToInt() == 2
                                            )
                                            ||
                                            (
                                                row.CheckGet("SQUARE").ToInt() < 3000
                                                &&
                                                (
                                                    row.CheckGet("COMPOSITION_TYPE").ToInt() == 3 || row.CheckGet("COMPOSITION_TYPE").ToInt() == 4 || row.CheckGet("COMPOSITION_TYPE").ToInt() == 5
                                                )
                                            )
                                        )
                                    )
                                    {
                                        color = HColor.Yellow;
                                    }

                                    // Оранжевый -- Выставлен флаг не кроить
                                    if (row.CheckGet("NONCUTTING_FLAG").ToInt() == 1)
                                    {
                                        color = HColor.Orange;
                                    }

                                    // Красный -- Есть в схеме производства есть станок ФОЛД(Скл многоточ) и в заявке менее 2500 м2. (меньше минимальной партии для станка ФОЛД(Скл многоточ))
                                    if (row.CheckGet("FOLD_FLAG").ToInt() == 1 && row.CheckGet("SQUARE").ToInt() < 2500)
                                    {
                                        color = HColor.Red;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Не кроить",
                        Path="NONCUTTING_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Особая дата и номер",
                        Path="SPECIAL_DT_AND_NUMBER_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Стеллажный склад",
                        Path="PLACED_IN_RACK_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Отсутствует эталонный образец",
                        Path="REFERENCE_SAMPLE_NOAVAILABLE",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Липецк",
                        Description="Может производиться в Липецке",
                        Path="PRODUCTION_LIPETSK_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Кашира",
                        Description="Может производиться в Кашире",
                        Path="PRODUCTION_KASHIRA_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Cхема",
                        Path="TYPESCHEME_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=11,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    //// Красный -- Схемы производства Md
                                    //if (row.CheckGet("MD_FLAG").ToInt() == 1)
                                    //{
                                    //    color = HColor.Red;
                                    //}

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Клише Липецк",
                        Path="CLICHE_STATUS_LIPETSK",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // Красный -- Есть статус клише
                                    if (!string.IsNullOrEmpty(row.CheckGet("CLICHE_STATUS_LIPETSK")))
                                    {
                                        color = HColor.Red;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Штанц. Липецк",
                        Path="SHTANZ_STATUS_LIPETSK",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // Красный -- Есть статус штанцформы
                                    if (!string.IsNullOrEmpty(row.CheckGet("SHTANZ_STATUS_LIPETSK")))
                                    {
                                        color = HColor.Red;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Клише Кашира",
                        Path="CLICHE_STATUS_KASHIRA",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // Красный -- Есть статус клише
                                    if (!string.IsNullOrEmpty(row.CheckGet("CLICHE_STATUS_KASHIRA")))
                                    {
                                        color = HColor.Red;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Штанц. Кашира",
                        Path="SHTANZ_STATUS_KASHIRA",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // Красный -- Есть статус штанцформы
                                    if (!string.IsNullOrEmpty(row.CheckGet("SHTANZ_STATUS_KASHIRA")))
                                    {
                                        color = HColor.Red;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Время отгрузки",
                        Path="SHIPMENT_TIME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Адрес доставки",
                        Path="DELIVERY_ADDRESS",
                        ColumnType=ColumnTypeRef.String,
                        Width2=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Примечание к заявке",
                        Path="ORDER_NOTE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=28,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Недельная отгрузка",
                        Path="WEEKLY_SHIPMENT",
                        ColumnType=ColumnTypeRef.String,
                        Width2=16,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата последней отгрузки",
                        Path="LAST_SHIPMENT_DT",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy",
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата тех карты",
                        Path="TK_CREATED_DATE",
                        Description="Дата создания тех карты",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy",
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="УПД возврат",
                        Path="RETURN_UPD_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ТН возврат",
                        Path="RETURN_TN_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=7,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Номер ограничения",
                        Path="QUANTITY_LIMIT",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Путь к тех карте",
                        Path="PRODUCT_PATHTK",
                        ColumnType=ColumnTypeRef.String,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Г-Md",
                        Path="MD_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="FOLD_FLAG",
                        Path="FOLD_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="COMPOSITION_TYPE",
                        Path="COMPOSITION_TYPE",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="COMPONENT",
                        Path="COMPONENT",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид схемы производства",
                        Path="SCHEME_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество цветов в ТК",
                        Path="COLOR_COUNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид заявки",
                        Path="ORDER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="IS_STANDART",
                        Path="IS_STANDART",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                };
                PositionGrid.SetColumns(columns);
                PositionGrid.SetPrimaryKey("POSITION_ID");
                PositionGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                PositionGrid.AutoUpdateInterval = 0;              

                //при выборе строки в гриде, обновляются актуальные действия для записи
                PositionGrid.OnSelectItem = selectedItem =>
                {
                };

                PositionGrid.Commands = Commander;
                PositionGrid.UseProgressSplashAuto = false;
                PositionGrid.Init();
            }
        }

        public async void PositionGridLoadItems()
        {
            DisableControls();

            if (OrderGrid.SelectedItem != null && !string.IsNullOrEmpty(OrderGrid.SelectedItem.CheckGet("ORDER_ID")))
            {
                var p = new Dictionary<string, string>();
                p.Add("ORDER_ID", OrderGrid.SelectedItem.CheckGet("ORDER_ID"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "ConfirmOrder");
                q.Request.SetParam("Action", "ListPosition");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                PositionGridDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        PositionGridDataSet = ListDataSet.Create(result, "ITEMS");
                    }
                }
                else
                {
                    q.ProcessError();
                }
                PositionGrid.SelectedItem = new Dictionary<string, string>();
                PositionGrid.UpdateItems(PositionGridDataSet);

                OrderGrid.Commands.ProcessSelectItem(OrderGrid.SelectedItem);
            }

            EnableControls();
        }

        /// <summary>
        /// Инициализация грида истории изменений по выбранной заявке на гофропроизводство
        /// </summary>
        public void HistoryGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn()
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=4,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата изменения",
                        Path="CHANGE_DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2=14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Пользователь",
                        Path="USER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=15,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Path="PRODUCT_CODE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=15,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Продукция",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=40,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Path="PRODUCT_QUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },      
                    new DataGridHelperColumn
                    {
                        Header="Адрес доставки",
                        Path="DELIVERY_ADDRESS",
                        ColumnType=ColumnTypeRef.String,
                        Width2=124,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Операция",
                        Path="CHANGE_OPERATION_TYPE",
                        ColumnType=ColumnTypeRef.String,
                        Hidden=true,
                    },
                };
                HistoryGrid.SetColumns(columns);
                HistoryGrid.SetPrimaryKey("_ROWNUMBER");
                HistoryGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                HistoryGrid.AutoUpdateInterval = 0;

                HistoryGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    {
                        StylerTypeRef.BackgroundColor,
                        (Dictionary<string, string> row) =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            // Зелёный -- Тип операции изменения = insert
                            if (row.CheckGet("CHANGE_OPERATION_TYPE") == "I")
                            {
                                color = HColor.Green;
                            }

                            // Желтый -- Тип операции изменения = update
                            if (row.CheckGet("CHANGE_OPERATION_TYPE") == "U")
                            {
                                color = HColor.Yellow;
                            }
                            
                            // Красный -- Тип операции изменения = delete
                            if (row.CheckGet("CHANGE_OPERATION_TYPE") == "D")
                            {
                                color = HColor.Red;
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },
                };

                //при выборе строки в гриде, обновляются актуальные действия для записи
                HistoryGrid.OnSelectItem = selectedItem =>
                {
                    HistoryGridSelectedItem = selectedItem;
                };

                HistoryGrid.UseProgressSplashAuto = false;
                HistoryGrid.Init();
            }
        }

        public async void HistoryGridLoadItems()
        {
            DisableControls();

            if (OrderGrid.SelectedItem != null && !string.IsNullOrEmpty(OrderGrid.SelectedItem.CheckGet("ORDER_ID")))
            {
                var p = new Dictionary<string, string>();
                p.Add("ORDER_ID", OrderGrid.SelectedItem.CheckGet("ORDER_ID"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "ConfirmOrder");
                q.Request.SetParam("Action", "ListChange");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;
                q.DoQuery();

                HistoryGridDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        HistoryGridDataSet = ListDataSet.Create(result, "ITEMS");
                    }
                }
                else
                {
                    q.ProcessError();
                }
                HistoryGrid.UpdateItems(HistoryGridDataSet);

                OrderGrid.Focus();
                OrderGrid.GridView.Focus();
            }

            EnableControls();
        }

        /// <summary>
        /// Открытие эксель файла тех карты по выбранной продукции
        /// </summary>
        public void ShowTechnologicalMap()
        {
            string pathTk = PositionGrid.SelectedItem.CheckGet("PRODUCT_PATHTK");
            if (!string.IsNullOrEmpty(pathTk))
            {
                if (System.IO.File.Exists(pathTk))
                {
                    Central.OpenFile(pathTk);
                }
                else
                {
                    var msg = $"Файл {pathTk} не найден по указанному пути";
                    var d = new DialogWindow($"{msg}", "Подтверждение заявок на гофропроизводство", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                var msg = "Не найден путь к Excel файлу тех карты";
                var d = new DialogWindow($"{msg}", "Подтверждение заявок на гофропроизводство", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Изменить схему производства
        /// </summary>
        public void EditScheme()
        {
            if (PositionGrid.SelectedItem != null && PositionGrid.SelectedItem.Count > 0)
            {
                var window = new PreproductionConfirmOrderEditScheme();
                window.PositionId = PositionGrid.SelectedItem.CheckGet("POSITION_ID").ToInt();
                window.ProductName = PositionGrid.SelectedItem.CheckGet("PRODUCT_NAME");
                window.CurrentSchemeName = PositionGrid.SelectedItem.CheckGet("TYPESCHEME_NAME");
                window.CurrentSchemeId = PositionGrid.SelectedItem.CheckGet("SCHEME_ID").ToInt();
                window.ShipmentDt = OrderGrid.SelectedItem.CheckGet("SHIPMENT_DTTM");
                window.Show();
            }
            else
            {
                var msg = "Не выбрана позиция заявки для изменения схемы.";
                var d = new DialogWindow($"{msg}", "Подтверждение заявок на гофропроизводство", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        public void SendToRack(int rackFlag = 1)
        {
            if (OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0)
            {
                if (PositionGrid.Items != null && PositionGrid.Items.Count > 0)
                {
                    var p = new Dictionary<string, string>();
                    p.Add("ORDER_ID", OrderGrid.SelectedItem.CheckGet("ORDER_ID"));
                    p.Add("SEND_TO_RACK_FLAG", $"{rackFlag}");

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "ConfirmOrder");
                    q.Request.SetParam("Action", "SendToRack");
                    q.Request.SetParams(p);

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {
                        bool succesfullFlag = false;

                        var queryResult = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (queryResult != null)
                        {
                            var dataSet = ListDataSet.Create(queryResult, "ITEMS");
                            if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                            {
                                if (!string.IsNullOrEmpty(dataSet.Items.First().CheckGet("ORDER_ID")))
                                {
                                    succesfullFlag = true;
                                }
                            }
                        }

                        PositionGridLoadItems();

                        if (!succesfullFlag)
                        {
                            string msg = $"При обновлении признака размещения позиций на стеллажном складе произошла ошибка. Пожалуйста, сообщите о проблеме.";
                            var d = new DialogWindow($"{msg}", "Подтверждение заявок на гофропроизводство", "", DialogWindowButtons.OK);
                            d.ShowDialog();
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
        /// Проверяем, что в позициях выбранной заявки есть редкие артикулы(профили)
        /// </summary>
        /// <param name="orderIdList"></param>
        /// <returns></returns>
        public bool CheckRareProductCode(List<string> orderIdList)
        {
            bool functionResult = false;

            var p = new Dictionary<string, string>();
            p.Add("ORDER_ID_LIST", JsonConvert.SerializeObject(orderIdList));

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "ConfirmOrder");
            q.Request.SetParam("Action", "CheckRareProductCode");
            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;
            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var queryResult = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (queryResult != null)
                {
                    var dataSet = ListDataSet.Create(queryResult, "ITEMS");
                    if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                    {
                        bool rareProductCodeFlag = dataSet.Items.First().CheckGet("RARE_PRODUCT_CODE_FLAG").ToBool();
                        if (rareProductCodeFlag)
                        {
                            string msg = $"Внимание!{Environment.NewLine}В заявке присутствуют позиции с редким профилем «ЕС» или «ЕЕ».{Environment.NewLine}Вы хотите продолжить?";
                            var d = new DialogWindow($"{msg}", "Подтверждение заявок на гофропроизводство", "", DialogWindowButtons.NoYes);
                            if (d.ShowDialog() == true)
                            {
                                functionResult = true;
                            }
                        }
                        else
                        {
                            functionResult = true;
                        }
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            return functionResult;
        }

        public void ChangeFactory()
        {
            if (OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0)
            {
                DisableControls();

                // Если переносим больше одной завки
                if (OrderGrid.Items.Sum(x => x.CheckGet("CHECKED_FLAG").ToInt()) > 1)
                {
                    List<Dictionary<string, string>> checkedRowList = OrderGrid.Items.Where(x => x.CheckGet("CHECKED_FLAG").ToInt() == 1).ToList();

                    bool resume = false;
                    {
                        var msg = $"Перенести {checkedRowList.Count} заявок на другую площадку?";
                        var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.YesNo);
                        if (d.ShowDialog() == true)
                        {
                            resume = true;
                        }
                    }

                    if (resume)
                    {
                        bool errorFlag = false;
                        string customErrorMessage = "";

                        foreach (var checkedRow in checkedRowList)
                        {
                            if (checkedRow.CheckGet("ORDER_TYPE").ToInt() == 1
                                && (checkedRow.CheckGet("ORDER_STATUS_ID").ToInt() == 6 || checkedRow.CheckGet("ORDER_STATUS_ID").ToInt() == 16)
                                && checkedRow.CheckGet("SHIPMENT_ID").ToInt() == 0)
                            {
                                var p = new Dictionary<string, string>();
                                p.Add("ORDER_ID", checkedRow.CheckGet("ORDER_ID"));

                                var q = new LPackClientQuery();
                                q.Request.SetParam("Module", "Preproduction");
                                q.Request.SetParam("Object", "ConfirmOrder");
                                q.Request.SetParam("Action", "ChangeFactory");
                                q.Request.SetParams(p);

                                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                                q.DoQuery();

                                bool succesfullFlag = false;
                                if (q.Answer.Status == 0)
                                {
                                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                                    if (result != null)
                                    {
                                        var dataSet = ListDataSet.Create(result, "ITEMS");
                                        if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                                        {
                                            if (dataSet.Items[0].CheckGet("ORDER_ID").ToInt() > 0)
                                            {
                                                succesfullFlag = true;
                                            }
                                        }
                                    }
                                }
                                else if (q.Answer.Error.Code == 145)
                                {
                                    customErrorMessage = $"{customErrorMessage}Заявка #{checkedRow.CheckGet("ORDER_ID").ToInt()} {q.Answer.Error.Message}{Environment.NewLine}{Environment.NewLine}";
                                    q.SilentErrorProcess = true;
                                    q.ProcessError();
                                }
                                else
                                {
                                    q.SilentErrorProcess = true;
                                    q.ProcessError();
                                }

                                if (!succesfullFlag)
                                {
                                    errorFlag = true;
                                }
                            }
                        }

                        OrderGrid.LoadItems();

                        if (!errorFlag)
                        {
                            var msg = "Успешное перемещение заявок на другую площадку.";
                            var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(customErrorMessage))
                            {
                                var msg = $"При перемещении заявок на другую площадку произошла ошибка.{Environment.NewLine}{Environment.NewLine}{customErrorMessage}";
                                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                                d.ShowDialog();
                            }
                            else
                            {
                                var msg = "При перемещении заявок на другую площадку произошла ошибка. Пожалуйста, сообщите о проблеме.";
                                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                                d.ShowDialog();
                            }
                        }
                    }
                }
                else
                {
                    if (OrderGrid.Items.Sum(x => x.CheckGet("CHECKED_FLAG").ToInt()) == 1)
                    {
                        OrderGrid.SelectRowByKey(OrderGrid.Items.FirstOrDefault(x => x.CheckGet("CHECKED_FLAG").ToInt() == 1).CheckGet("ORDER_ID"));
                    }

                    if (OrderGrid.SelectedItem.CheckGet("ORDER_TYPE").ToInt() == 1
                        && (OrderGrid.SelectedItem.CheckGet("ORDER_STATUS_ID").ToInt() == 6 || OrderGrid.SelectedItem.CheckGet("ORDER_STATUS_ID").ToInt() == 16)
                        && OrderGrid.SelectedItem.CheckGet("SHIPMENT_ID").ToInt() == 0)
                    {
                        bool resume = false;
                        {
                            var msg = $"Перенести заявку #{OrderGrid.SelectedItem.CheckGet("ORDER_ID").ToInt()} на другую площадку?";
                            var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.YesNo);
                            if (d.ShowDialog() == true)
                            {
                                resume = true;
                            }
                        }

                        if (resume)
                        {
                            var p = new Dictionary<string, string>();
                            p.Add("ORDER_ID", OrderGrid.SelectedItem.CheckGet("ORDER_ID"));

                            var q = new LPackClientQuery();
                            q.Request.SetParam("Module", "Preproduction");
                            q.Request.SetParam("Object", "ConfirmOrder");
                            q.Request.SetParam("Action", "ChangeFactory");
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
                                    var dataSet = ListDataSet.Create(result, "ITEMS");
                                    if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                                    {
                                        if (dataSet.Items[0].CheckGet("ORDER_ID").ToInt() > 0)
                                        {
                                            succesfullFlag = true;
                                        }
                                    }
                                }

                                if (succesfullFlag)
                                {
                                    OrderGrid.LoadItems();

                                    var msg = "Успешное перемещение заявки на другую площадку.";
                                    var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                                    d.ShowDialog();
                                }
                                else
                                {
                                    var msg = "При перемещении заявки на другую площадку произошла ошибка. Пожалуйста, сообщите о проблеме.";
                                    var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                                    d.ShowDialog();
                                }
                            }
                            else
                            {
                                q.ProcessError();
                            }
                        }
                    }
                    else
                    {
                        var msg = "Заявку с текущим статусом нельзя переместить на другую площадку.";
                        var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }

                EnableControls();
            }
        }

        public void Confirm()
        {
            if (OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0)
            {
                // Если утверждаем больше одной завки
                if (OrderGrid.Items.Sum(x => x.CheckGet("CHECKED_FLAG").ToInt()) > 1)
                {
                    DisableControls();

                    List<Dictionary<string, string>> checkedRowList = OrderGrid.Items.Where(x => x.CheckGet("CHECKED_FLAG").ToInt() == 1).ToList();

                    if (CheckRareProductCode(checkedRowList.Select(x => x.CheckGet("ORDER_ID")).ToList()))
                    {
                        foreach (var checkedRow in checkedRowList)
                        {
                            var p = new Dictionary<string, string>();
                            p.Add("ORDER_STATUS_ID", checkedRow.CheckGet("ORDER_STATUS_ID"));
                            p.Add("ORDER_ID", checkedRow.CheckGet("ORDER_ID"));
                            p.Add("ORDER_TYPE", checkedRow.CheckGet("ORDER_TYPE"));
                            p.Add("CONFIRM_DTTM", checkedRow.CheckGet("SHIPMENT_DTTM"));
                            p.Add("NEW_DTTM_FLAG", "0");

                            var q = new LPackClientQuery();
                            q.Request.SetParam("Module", "Preproduction");
                            q.Request.SetParam("Object", "ConfirmOrder");
                            q.Request.SetParam("Action", "Confirm");
                            q.Request.SetParams(p);
                            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;
                            q.DoQuery();

                            if (q.Answer.Status != 0)
                            {
                                q.ProcessError();
                            }
                        }

                        OrderGrid.LoadItems();
                    }

                    EnableControls();
                }
                // Если утверждаем одну заявку
                else
                {
                    if (OrderGrid.Items.Sum(x => x.CheckGet("CHECKED_FLAG").ToInt()) == 1)
                    {
                        OrderGrid.SelectRowByKey(OrderGrid.Items.FirstOrDefault(x => x.CheckGet("CHECKED_FLAG").ToInt() == 1).CheckGet("ORDER_ID"));
                    }

                    if (OrderGrid.SelectedItem.CheckGet("ORDER_STATUS_ID").ToInt() == 6 || OrderGrid.SelectedItem.CheckGet("ORDER_STATUS_ID").ToInt() == 16)
                    {
                        DisableControls();

                        if (CheckRareProductCode(new List<string> { OrderGrid.SelectedItem.CheckGet("ORDER_ID") }))
                        {
                            var p = new Dictionary<string, string>();
                            p.Add("ORDER_ID", OrderGrid.SelectedItem.CheckGet("ORDER_ID"));

                            var q = new LPackClientQuery();
                            q.Request.SetParam("Module", "Preproduction");
                            q.Request.SetParam("Object", "ConfirmOrder");
                            q.Request.SetParam("Action", "CheckPositionStatus");
                            q.Request.SetParams(p);

                            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                            q.DoQuery();

                            if (q.Answer.Status == 0)
                            {
                                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                                if (result != null)
                                {
                                    var dataSet = ListDataSet.Create(result, "ITEMS");
                                    if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                                    {
                                        var firstItem = dataSet.Items.First();
                                        int noClicheFlag = firstItem.CheckGet("NO_CLICHE_FLAG").ToInt();
                                        int noShtanzFlag = firstItem.CheckGet("NO_SHTANZ_FLAG").ToInt();
                                        int noReferenceSamplePlaceFlag = firstItem.CheckGet("NO_REFERENCE_SAMPLE_PLACE_FLAG").ToInt();

                                        // Если один из флагов поднят, сообщаем пользователю
                                        {
                                            string msg = "";

                                            if (noClicheFlag > 0)
                                            {
                                                msg = $"{msg}В заявке присутствуют позиции без клише!{Environment.NewLine}";
                                            }

                                            if (noShtanzFlag > 0)
                                            {
                                                msg = $"{msg}В заявке присутствуют позиции без штанцформы!{Environment.NewLine}";
                                            }

                                            if (noReferenceSamplePlaceFlag > 0)
                                            {
                                                msg = $"{msg}В заявке присутствуют позиции с дополнительным требованием - Эталонный образец, но место и номер не заполнены!{Environment.NewLine}";
                                            }

                                            if (!string.IsNullOrEmpty(msg))
                                            {
                                                var d = new DialogWindow($"{msg}", "Подтверждение заявок на гофропроизводство", "", DialogWindowButtons.OK);
                                                d.ShowDialog();
                                            }
                                        }

                                        var window = new PreproductionConfirmOrder();
                                        window.OrderId = OrderGrid.SelectedItem.CheckGet("ORDER_ID").ToInt();
                                        window.FactoryId = OrderGrid.SelectedItem.CheckGet("FACTORY_ID").ToInt();
                                        window.DefaultDate = OrderGrid.SelectedItem.CheckGet("SHIPMENT_DTTM");
                                        window.OrderType = OrderGrid.SelectedItem.CheckGet("ORDER_TYPE").ToInt();
                                        window.OrderStatus = OrderGrid.SelectedItem.CheckGet("ORDER_STATUS_ID").ToInt();
                                        window.Editable = true;
                                        window.Show();
                                    }
                                }
                            }
                            else
                            {
                                q.ProcessError();
                            }
                        }

                        EnableControls();
                    }
                    else
                    {
                        var window = new PreproductionConfirmOrder();
                        window.OrderId = OrderGrid.SelectedItem.CheckGet("ORDER_ID").ToInt();
                        window.FactoryId = OrderGrid.SelectedItem.CheckGet("FACTORY_ID").ToInt();
                        if (!string.IsNullOrEmpty(OrderGrid.SelectedItem.CheckGet("SHIPMENT_DTTM")))
                        {
                            window.DefaultDate = OrderGrid.SelectedItem.CheckGet("SHIPMENT_DTTM");
                        }
                        else
                        {
                            window.DefaultDate = DateTime.Now.ToString("dd.MM.yyyy");
                        }
                        window.Editable = false;
                        window.Show();
                    }
                }
            }
        }

        /// <summary>
        /// Показать загруженность станков. (Без учёта выбранной заявки. Только принятые заявки на конкретную дату.)
        /// </summary>
        public void ShowWorkcenterWorkload()
        {
            var window = new PreproductionConfirmOrder();
            window.DefaultDate = DateTime.Now.ToString("dd.MM.yyyy");
            window.FactoryId = FactorySelectBox.SelectedItem.Key.ToInt();
            window.Editable = false;
            window.Show();
        }

        /// <summary>
        /// Показать отчёт по композициям с композициями, которые используются в этой заявке
        /// </summary>
        public void ShowInCompositionList()
        {
            if (OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0)
            {
                string factoryId = OrderGrid.SelectedItem.CheckGet("FACTORY_ID").ToInt().ToString();

                var p = new Dictionary<string, string>();
                p.Add("ORDER_ID", OrderGrid.SelectedItem.CheckGet("ORDER_ID"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "ConfirmOrder");
                q.Request.SetParam("Action", "ListCardboardByOrder");
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
                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                        {
                            List<int> currentOrderCardboard = ds.Items.Select(x => x.CheckGet("CARDBOARD_ID").ToInt()).ToList();
                            if (currentOrderCardboard != null && currentOrderCardboard.Count > 0)
                            {
                                var preproductionConfirmOrderCompositionList = Central.WM.CheckAddTab<PreproductionConfirmOrderCompositionList>("PreproductionConfirmOrderCompositionList", "Отчёт по композициям", true, "", "bottom");
                                preproductionConfirmOrderCompositionList.CurrentOrderCardboard = currentOrderCardboard;
                                preproductionConfirmOrderCompositionList.CurrentOrderDt = OrderGrid.SelectedItem.CheckGet("SHIPMENT_DTTM");
                                preproductionConfirmOrderCompositionList.FactorySelectBox.SetSelectedItemByKey(factoryId);
                                preproductionConfirmOrderCompositionList.Refresh();
                                Central.WM.SetActive("PreproductionConfirmOrderCompositionList");
                            }
                        }
                        else
                        {
                            var msg = "По выбранной заявке не найдены используемые композиции.";
                            var d = new DialogWindow($"{msg}", "Подтверждение заявок на гофропроизводство", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                }
            }
            else
            {
                var msg = "Не выбрана заявка для просмотра в отчёте по композициям.";
                var d = new DialogWindow($"{msg}", "Подтверждение заявок на гофропроизводство", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Открыть чат с клиентом
        /// </summary>
        public void ShowChat()
        {
            if (OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0)
            {
                var window = new PreproductionConfirmOrderChat();
                window.OrderId = OrderGrid.SelectedItem.CheckGet("ORDER_ID").ToInt();
                window.OrderNumber = OrderGrid.SelectedItem.CheckGet("ORDER_NUMBER");
                window.CustomerEmail = OrderGrid.SelectedItem.CheckGet("CUSTOMER_CONTACT_EMAIL");
                window.Show();
            }
            else
            {
                var msg = "Не выбрана заявка для просмотра чата";
                var d = new DialogWindow($"{msg}", "Подтверждение заявок на гофропроизводство", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        public async void ShowLoadingScheme()
        {
            if (OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0)
            {
                // Если грузим по схеме от клиента
                if (OrderGrid.SelectedItem.CheckGet("SHIPMENT_SCHEME_STATUS").ToInt() == 2)
                {
                    if (!string.IsNullOrEmpty(OrderGrid.SelectedItem.CheckGet("SHIPMENT_SCHEME_FILE")))
                    {
                        string schemeFolder = Central.GetStorageNetworkPathByCode("shipment_loadingscheme");
                        if (!string.IsNullOrEmpty(schemeFolder))
                        {
                            string schemeFullPath = System.IO.Path.Combine(schemeFolder, OrderGrid.SelectedItem.CheckGet("SHIPMENT_SCHEME_FILE"));
                            if (System.IO.File.Exists(schemeFullPath))
                            {
                                Central.OpenFile(schemeFullPath);
                            }
                            else
                            {
                                var msg = $"Не найден файл схемы погрузки от клиента. {Environment.NewLine}{schemeFullPath}";
                                var d = new DialogWindow($"{msg}", "Подтверждение заявок на гофропроизводство", "", DialogWindowButtons.OK);
                                d.ShowDialog();
                            }
                        }
                        else
                        {
                            var msg = "Не задан путь к папке со схемами погрузки от клиента";
                            var d = new DialogWindow($"{msg}", "Подтверждение заявок на гофропроизводство", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                    else
                    {
                        var msg = "Не задан файл схемы погрузки от клиента";
                        var d = new DialogWindow($"{msg}", "Подтверждение заявок на гофропроизводство", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    // Если для заявки создана отгрузка
                    if (!string.IsNullOrEmpty(OrderGrid.SelectedItem.CheckGet("SHIPMENT_ID")))
                    {
                        var p = new Dictionary<string, string>();
                        {
                            p.CheckAdd("ID", OrderGrid.SelectedItem.CheckGet("SHIPMENT_ID"));
                            p.CheckAdd("DEMO", "0");
                        }

                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Shipments");
                        q.Request.SetParam("Object", "Loading");
                        q.Request.SetParam("Action", "GetMap");

                        q.Request.Timeout = 15000;
                        q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                        q.Request.SetParams(p);

                        await Task.Run(() =>
                        {
                            q.DoQuery();
                        });

                        if (q.Answer.Status == 0)
                        {
                            Central.OpenFile(q.Answer.DownloadFilePath);
                        }
                        else
                        {
                            q.ProcessError();
                        }
                    }
                    else
                    {
                        var p = new Dictionary<string, string>();
                        {
                            p.CheckAdd("ID", OrderGrid.SelectedItem.CheckGet("ORDER_ID"));
                        }

                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Shipments");
                        q.Request.SetParam("Object", "Loading");
                        q.Request.SetParam("Action", "GetMapByNsthet");

                        q.Request.Timeout = 15000;
                        q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                        q.Request.SetParams(p);

                        await Task.Run(() =>
                        {
                            q.DoQuery();
                        });

                        if (q.Answer.Status == 0)
                        {
                            Central.OpenFile(q.Answer.DownloadFilePath);
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
                var msg = "Не выбрана заявка для просмотра схемы погрузки";
                var d = new DialogWindow($"{msg}", "Подтверждение заявок на гофропроизводство", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Установить значение флага высокодоходной заявки (особая заявка)
        /// </summary>
        public void CheckSpecialOrderFlag(int flag, Dictionary<string, string> selectedRow)
        {
            if (CanSetSpecialOrderFlag)
            {
                DisableControls();

                var p = new Dictionary<string, string>();
                p.Add("FLAG", flag.ToString());
                p.Add("ORDER_ID", selectedRow.CheckGet("ORDER_ID"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "ConfirmOrder");
                q.Request.SetParam("Action", "CheckSpecialOrderFlag");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;
                q.DoQuery();

                if (q.Answer.Status != 0)
                {
                    q.ProcessError();
                    OrderGrid.LoadItems();
                }

                EnableControls();
            }
        }

        public void Refresh()
        {
            //OrderGrid.SelectedItem = null;

            ClearDependendGrids();

            OrderGrid.LoadItems();
        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel(this.RoleName);
            switch (mode)
            {
                // Если уровень доступа -- "Спецправа",
                case Role.AccessMode.Special:
                    CanSetSpecialOrderFlag = true;
                    break;

                case Role.AccessMode.FullAccess:
                    CanSetSpecialOrderFlag = true;
                    break;

                // Если уровень доступа -- "Только чтение",
                case Role.AccessMode.ReadOnly:
                    ConfirmButton.IsEnabled = false;
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Обновляем доступные действия
        /// </summary>
        public void UpdateAction()
        {
            ConfirmButton.IsEnabled = false;

            if (OrderGrid != null && OrderGrid.Items != null && OrderGrid.Items.Count > 0)
            {
                if (OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0)
                {
                    if (OrderGrid.SelectedItem.CheckGet("ORDER_STATUS_ID").ToInt() == 6 || OrderGrid.SelectedItem.CheckGet("ORDER_STATUS_ID").ToInt() == 16)
                    {
                        ConfirmButton.IsEnabled = true;
                    }
                }

                if (OrderGrid.Items.Sum(x => x.CheckGet("CHECKED_FLAG").ToInt()) > 0)
                {
                    ConfirmButton.IsEnabled = true;
                }
            }

            ProcessPermissions();
        }

        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
            //OrderGrid.ShowSplash();
            //PositionGrid.ShowSplash();
            //HistoryGrid.ShowSplash();
        }

        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
            //OrderGrid.HideSplash();
            //PositionGrid.HideSplash();
            //HistoryGrid.HideSplash();
        }

        public void ExportToExcel()
        {
            if (OrderGrid != null && OrderGrid.Items != null)
            {
                OrderGrid.ItemsExportExcel();
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            Confirm();
        }

        private void ManagerVerifiedStatusSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ClearDependendGrids();
            if (OrderGrid != null && OrderGrid.Items != null)
            {
                OrderGrid.UpdateItems();
            }
        }

        private void EditSchemeButton_Click(object sender, RoutedEventArgs e)
        {
            EditScheme();
        }

        private void ManagerSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ClearDependendGrids();
            if (OrderGrid != null && OrderGrid.Items != null)
            {
                OrderGrid.UpdateItems();
            }
        }

        private void FullnessOrderSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ClearDependendGrids();
            if (OrderGrid != null && OrderGrid.Items != null)
            {
                OrderGrid.UpdateItems();
            }
        }

        private void StatusSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ClearDependendGrids();
            if (OrderGrid != null && OrderGrid.Items != null)
            {
                OrderGrid.UpdateItems();
            }
        }

        private void ExcelButton_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcel();
        }

        private void FactorySelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ClearDependendGrids();
            if (OrderGrid != null && OrderGrid.Items != null)
            {
                OrderGrid.UpdateItems();
            }
        }
    }
}
