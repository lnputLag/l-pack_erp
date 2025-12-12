using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
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

namespace Client.Interfaces.Orders
{
    /// <summary>
    /// Интерфейс заявок на отгрузку макулатуры КШ
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class OrderScrapPaperConsumptionKshList : ControlBase
    {
        public OrderScrapPaperConsumptionKshList()
        {
            ControlTitle = "Заявки на отгрузку макулатуры КШ";
            RoleName = "[erp]order_scrap_paper_ksh";
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
                FormInit();
                SetDefaults();
                OrderGridInit();
                PositionGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                OrderGrid.Destruct();
                PositionGrid.Destruct();
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
                    ButtonControl = ResreshButton,
                    ButtonName = "ResreshButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
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
                    Name = "add_order",
                    Title = "Добавить",
                    Group = "order_grid_default",
                    Enabled = true,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = NewOrderButton,
                    ButtonName = "NewOrderButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        AddOrder();
                    }
                });
                Commander.Add(new CommandItem()
                {
                    Name = "edit_order",
                    Title = "Изменить",
                    Group = "order_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = EditOrderButton,
                    ButtonName = "EditOrderButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        EditOrder();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (OrderGrid != null && OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(OrderGrid.SelectedItem.CheckGet("ORDER_ID")))
                            {
                                if (string.IsNullOrEmpty(OrderGrid.SelectedItem.CheckGet("SHIPMENT_ID")))
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
                    Name = "delete_order",
                    Title = "Удалить",
                    Group = "order_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = DeleteOrderButton,
                    ButtonName = "DeleteOrderButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        DeleteOrder();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (OrderGrid != null && OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(OrderGrid.SelectedItem.CheckGet("ORDER_ID")))
                            {
                                if (string.IsNullOrEmpty(OrderGrid.SelectedItem.CheckGet("SHIPMENT_ID")))
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
                    Name = "create_shipment",
                    Title = "Создать отгрузку",
                    Group = "order_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = CreateShipmentButton,
                    ButtonName = "CreateShipmentButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        CreateShipment();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (OrderGrid != null && OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(OrderGrid.SelectedItem.CheckGet("ORDER_ID")))
                            {
                                if (string.IsNullOrEmpty(OrderGrid.SelectedItem.CheckGet("SHIPMENT_ID")))
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
                    Name = "order_export_to_excel",
                    Group = "order_grid_excel",
                    Enabled = true,
                    Title = "В Excel",
                    Description = "Выгрузить данные в Excel файл",
                    ButtonUse = true,
                    ButtonName = "OrderExportToExcelButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        OrderGrid.ItemsExportExcel();
                    },
                });
            }

            Commander.SetCurrentGridName("PositionGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "add_position",
                    Title = "Добавить",
                    Group = "position_grid_default",
                    Enabled = true,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = NewPositionButton,
                    ButtonName = "NewPositionButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        AddPosition();
                    }, 
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0)
                        {
                            if (string.IsNullOrEmpty(OrderGrid.SelectedItem.CheckGet("SHIPMENT_ID")))
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "edit_position",
                    Title = "Изменить",
                    Group = "position_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = EditPositionButton,
                    ButtonName = "EditPositionButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        EditPosition();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (PositionGrid != null && PositionGrid.SelectedItem != null && PositionGrid.SelectedItem.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(PositionGrid.SelectedItem.CheckGet("ORDER_POSITION_ID")))
                            {
                                if (OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0)
                                {
                                    if (string.IsNullOrEmpty(OrderGrid.SelectedItem.CheckGet("SHIPMENT_ID")))
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
                    Name = "delete_position",
                    Title = "Удалить",
                    Group = "position_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = DeletePositionButton,
                    ButtonName = "DeletePositionButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        DeletePosition();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (PositionGrid != null && PositionGrid.SelectedItem != null && PositionGrid.SelectedItem.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(PositionGrid.SelectedItem.CheckGet("ORDER_POSITION_ID")))
                            {
                                if (OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0)
                                {
                                    if (string.IsNullOrEmpty(OrderGrid.SelectedItem.CheckGet("SHIPMENT_ID")))
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
                    Name = "position_export_to_excel",
                    Group = "position_grid_excel",
                    Enabled = true,
                    Title = "В Excel",
                    Description = "Выгрузить данные в Excel файл",
                    ButtonUse = true,
                    ButtonName = "PositionExportToExcelButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        PositionGrid.ItemsExportExcel();
                    },
                });
            }

            Commander.Init(this);
        }

        public int FactoryId = 2;

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Датасет с данными грида заявок
        /// </summary>
        public ListDataSet OrderGridDataSet { get; set; }

        /// <summary>
        /// Датасет с данными грида позиций заявки
        /// </summary>
        public ListDataSet PositionGridDataSet { get; set; }

        public void Refresh()
        {
            OrderGrid.LoadItems();
        }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        private void FormInit()
        {
            //инициализация формы
            {
                Form = new FormHelper();

                //колонки формы
                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="ORDER_SEARCH",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=OrderSearchText,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path = "ORDER_DATE_FROM",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = OrderDateFrom,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "ORDER_DATE_TO",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = OrderDateTo,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path="POSITION_SEARCH",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=PositionSearchText,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                };

                Form.SetFields(fields);
            }
        }

        private void SetDefaults()
        {
            OrderGridDataSet = new ListDataSet();
            PositionGridDataSet = new ListDataSet();
            Form.SetDefaults();

            Form.SetValueByPath("ORDER_DATE_FROM", DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy"));
            Form.SetValueByPath("ORDER_DATE_TO", DateTime.Now.ToString("dd.MM.yyyy"));
        }

        private void OrderGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид заявки",
                        Description = "Идентификатор заявки",
                        Path="ORDER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер заявки",
                        Description = "Номер заявки",
                        Path="ORDER_NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата создания",
                        Description = "Дата создания заявки",
                        Path="CREATED_DATE",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2=8,
                        Format="dd.MM.yyyy HH:mm:ss",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата отгрузки",
                        Description = "Дата отгрузки",
                        Path="SHIPPING_DATE",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2=10,
                        Format="dd.MM.yyyy HH",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус заявки",
                        Description = "Статус заявки",
                        Path="ORDER_STATUS_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Покупатель",
                        Description = "Наименование покупателя",
                        Path="CUSTOMER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=16,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Позиций",
                        Description = "Количество позиций в заявке",
                        Path="ORDER_POSITION_CNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус позиций",
                        Description = "Статус позиций по заявке",
                        Path="ORDER_POSITION_STATUS_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Грузополучатель",
                        Description = "Наименование грузополучателя",
                        Path="CONSIGNEE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=16,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Продавец",
                        Description = "Наименование продавци",
                        Path="SELLER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=16,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Грузоотправитель",
                        Description = "Наименование грузоотправителя",
                        Path="SHIPPER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=16,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Самовывоз",
                        Description = "Признак отгрузки с самовывозом",
                        Path="SELFSHIP_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Разрешена",
                        Description = "Признак того, что отгрузка разрешена менеджером",
                        Path="ALLOWED_SHIPMENT",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Доверенность",
                        Description = "Признак наличия доверенности",
                        Path="ORDER_PROXY_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Тендер",
                        Description = "Признак того, что отгрузка учавствует в тендере",
                        Path="TENDER_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус тендера",
                        Description = "Статус тендера под отгрузку",
                        Path="TENDER_STATUS_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Перевозчик",
                        Description = "Наименование перевозчика",
                        Path="CARRIER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=25,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Водитель",
                        Description = "Наименование водителя",
                        Path="DRIVER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=25,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Телефон водителя",
                        Description = "Номер телефона водителя",
                        Path="DRIVER_PHONE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Транспорт",
                        Description = "Данные транспортного средства",
                        Path="CAR",
                        ColumnType=ColumnTypeRef.String,
                        Width2=19,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Примечание погрузчика",
                        Description = "Примечание погрузчика",
                        Path="NOTE_LOADER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=19,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Примечание кладовщика",
                        Description = "Примечание кладовщика",
                        Path="NOTE_STOCKMAN",
                        ColumnType=ColumnTypeRef.String,
                        Width2=19,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Примечание логиста",
                        Description = "Примечание логиста",
                        Path="NOTE_LOGISTICIAN",
                        ColumnType=ColumnTypeRef.String,
                        Width2=19,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид расходной накладной",
                        Description = "Идентификатор расходной накладной",
                        Path="INVOICE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид отгрузки",
                        Description = "Идентификатор отгрузки",
                        Path="SHIPMENT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Ид статуса заявки",
                        Description = "Идентификатор статуса заявки",
                        Path="ORDER_STATUS_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Тип заявки",
                        Description = "Тип заявки",
                        Path="ORDER_TYPE",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Файл доверенности",
                        Description = "Файл доверенности по заявке",
                        Path="ORDER_PROXY_FILE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид площадки",
                        Description = "Идентификатор площадки",
                        Path="ORDER_FACTORY_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид статуса позиций",
                        Description = "Идентификатор статуса позиций по заявке",
                        Path="ORDER_POSITION_STATUS_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид статуса тендера",
                        Description = "Идентификатор статуса тендера",
                        Path="TENDER_STATUS_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид договора",
                        Description = "Идентификатор договора",
                        Path="CONTRACT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид продавца",
                        Description = "Идентификатор продавца",
                        Path="SELLER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид грузоотправителя",
                        Description = "Идентификатор грузоотправителя",
                        Path="SHIPPER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид покупателя",
                        Description = "Идентификатор покупателя",
                        Path="CUSTOMER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид грузополучателя",
                        Description = "Идентификатор грузополучателя",
                        Path="CONSIGNEE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата доставки",
                        Description = "Дата доставки",
                        Path="DELIVERY_DATE",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2=10,
                        Format="dd.MM.yyyy HH",
                        Visible=false
                    },
                }; 
                OrderGrid.SetColumns(columns);
                OrderGrid.SearchText = OrderSearchText;
                OrderGrid.OnLoadItems = OrderGridLoadItems;
                OrderGrid.SetPrimaryKey("ORDER_ID");
                OrderGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                OrderGrid.AutoUpdateInterval = 5 * 60;
                OrderGrid.Toolbar = OrderGridToolbar;
                OrderGrid.Commands = Commander;
                OrderGrid.UseProgressSplashAuto = false;
                OrderGrid.OnSelectItem = (Dictionary<string, string> selectedItem) =>
                {
                    if (PositionGrid != null)
                    {
                        PositionGrid.ClearItems();
                    }

                    if (selectedItem != null && selectedItem.Count > 0 && OrderGrid.Items.FirstOrDefault(x => x.CheckGet("ORDER_ID").ToInt() == selectedItem.CheckGet("ORDER_ID").ToInt()) != null)
                    {
                        PositionGridLoadItems();
                    }
                    else
                    {
                        OrderGrid.SelectRowFirst();
                    }
                };
                OrderGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    // определение цветов фона строк
                    {
                        StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            if (row.CheckGet("ORDER_STATUS_ID").ToInt() == 1)
                            {
                                color = HColor.Blue;
                            }
                            else if (row.CheckGet("ORDER_STATUS_ID").ToInt() == 2)
                            {
                                 color = HColor.Red;
                            }
                            else if (row.CheckGet("ORDER_STATUS_ID").ToInt() == 4
                                || (!string.IsNullOrEmpty(row.CheckGet("INVOICE_ID")) && row.CheckGet("ORDER_POSITION_STATUS_ID").ToInt() == 0))
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
                OrderGrid.Init();
            }
        }

        private async void OrderGridLoadItems()
        {
            bool resume = true;

            var f = Form.GetValueByPath("ORDER_DATE_FROM").ToDateTime();
            var t = Form.GetValueByPath("ORDER_DATE_TO").ToDateTime();

            if (DateTime.Compare(f, t) > 0)
            {
                const string msg = "Дата начала должна быть меньше даты окончания.";
                var d = new DialogWindow($"{msg}", this.ControlTitle);
                d.ShowDialog();
                resume = false;
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();
                p.Add("ORDER_DATE_FROM", Form.GetValueByPath("ORDER_DATE_FROM"));
                p.Add("ORDER_DATE_TO", Form.GetValueByPath("ORDER_DATE_TO"));
                p.Add("FACTORY_ID", $"{FactoryId}");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Orders");
                q.Request.SetParam("Object", "ScrapPaper");
                q.Request.SetParam("Action", "ListOrder");
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
                    }
                }
                else
                {
                    q.ProcessError();
                }
                OrderGrid.UpdateItems(OrderGridDataSet);
            }
        }

        private void PositionGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Description = "Идентификатор позиции заявки",
                        Path="ORDER_POSITION_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Продукция",
                        Description = "Наименование продукции по позиции заявки",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=32,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус",
                        Description = "Статус позиции заявки",
                        Path="ORDER_POSITION_STATUS_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Description = "Количество продукции по позиции заявки",
                        Path="ORDER_POSITION_QUANTITY",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=6,
                        Format="N0",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Отгружено",
                        Description = "Количество отгруженной продукции, шт.",
                        Path="COUNT_ALREADY_SHIPPED",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=6,
                        Format="N0",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Цена",
                        Description = "Фактическая цена",
                        Path="PRICE",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=8,
                        Format="N6",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Цена без НДС",
                        Description = "Цена без НДС",
                        Path="PRICE_WITHOUT_VAT",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=9,
                        Format="N8",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Description = "Артикул продукции по позиции заявки",
                        Path="PRODUCT_CODE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=15,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Description = "Отгрузочное наименование продукции",
                        Path="PRODUCT_SHIPMENT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=97,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Примечание погрузчика",
                        Description = "Примечание погрузчика",
                        Path="NOTE_LOADER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=19,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Примечание кладовщика",
                        Description = "Примечание кладовщика",
                        Path="NOTE_STOCKMAN",
                        ColumnType=ColumnTypeRef.String,
                        Width2=19,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид продукции",
                        Description = "Идентификатор продукции по позиции заявки",
                        Path="PRODUCT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Ид статуса",
                        Description = "Идентификатор статуса позиции заявки",
                        Path="ORDER_POSITION_STATUS_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                };
                PositionGrid.SetColumns(columns);
                PositionGrid.SearchText = PositionSearchText;
                PositionGrid.OnLoadItems = PositionGridLoadItems;
                PositionGrid.SetPrimaryKey("ORDER_POSITION_ID");
                PositionGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                PositionGrid.AutoUpdateInterval = 0;
                PositionGrid.ItemsAutoUpdate = false;
                PositionGrid.Toolbar = PositionGridToolbar;
                PositionGrid.Commands = Commander;
                PositionGrid.UseProgressSplashAuto = false;
                PositionGrid.Init();
            }
        }

        private async void PositionGridLoadItems()
        {
            bool resume = false;

            if (OrderGrid != null && OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0)
            {
                resume = true;
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();
                p.Add("ORDER_ID", OrderGrid.SelectedItem.CheckGet("ORDER_ID"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Orders");
                q.Request.SetParam("Object", "ScrapPaper");
                q.Request.SetParam("Action", "ListPosition");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

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
                PositionGrid.UpdateItems(PositionGridDataSet);
            }
            else
            {
                PositionGrid.UpdateItems(new ListDataSet());
            }
        }

        private void AddOrder()
        {
            var i = new ScrapPaperConsumptionKshOrder();
            i.RoleName = this.RoleName;
            i.FactoryId = this.FactoryId;
            i.ParentFrame = this.FrameName;
            i.Show();
        }

        private void EditOrder()
        {
            if (OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0)
            {
                var i = new ScrapPaperConsumptionKshOrder();
                i.RoleName = this.RoleName;
                i.FactoryId = this.FactoryId;
                i.ParentFrame = this.FrameName;
                i.OrderId = OrderGrid.SelectedItem.CheckGet("ORDER_ID").ToInt();
                i.Show();
            }
            else
            {
                var msg = "Не выбрана заявка для изменения";
                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        private void DeleteOrder()
        {
            if (OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0)
            {
                var msg = $"Вы действительно хотите удалить заявку {OrderGrid.SelectedItem.CheckGet("ORDER_ID")} {OrderGrid.SelectedItem.CheckGet("ORDER_NUMBER")}?";
                if (DialogWindow.ShowDialog($"{msg}", this.ControlTitle, "", DialogWindowButtons.YesNo) == true)
                {
                    var p = Form.GetValues();
                    p.CheckAdd("ORDER_ID", OrderGrid.SelectedItem.CheckGet("ORDER_ID"));
                        
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Orders");
                    q.Request.SetParam("Object", "ScrapPaper");
                    q.Request.SetParam("Action", "DeleteOrder");
                    q.Request.SetParams(p);

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {
                        bool succesfullFlag = false;
                        int orderId = 0;

                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            var dataSet = ListDataSet.Create(result, "ITEMS");
                            if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                            {
                                orderId = dataSet.Items[0].CheckGet("ORDER_ID").ToInt();
                                if (orderId > 0)
                                {
                                    succesfullFlag = true;
                                }
                            }
                        }

                        if (succesfullFlag)
                        {
                            msg = "Успешное удаление заявки на отгрузку макулатуры.";
                            var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                            d.ShowDialog();

                            Refresh();
                        }
                        else
                        {
                            msg = "Ошибка удаления заявки на отгрузку макулатуры. Пожалуйста, сообщите о проблеме.";
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
                var msg = "Не выбрана заявка для удаления";
                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        private void CreateShipment()
        {
            if (OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0)
            {
                var p = new Dictionary<string, string>();
                p.CheckAdd("ORDER_ID", OrderGrid.SelectedItem.CheckGet("ORDER_ID"));
                p.CheckAdd("SELFSHIP_FLAG", OrderGrid.SelectedItem.CheckGet("SELFSHIP_FLAG"));
                p.CheckAdd("SELLER_ID", OrderGrid.SelectedItem.CheckGet("SELLER_ID"));
                p.CheckAdd("SHIPPING_DATE", OrderGrid.SelectedItem.CheckGet("SHIPPING_DATE"));
                p.CheckAdd("FACTORY_ID", $"{this.FactoryId}");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Orders");
                q.Request.SetParam("Object", "ScrapPaper");
                q.Request.SetParam("Action", "SaveShipment");
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
                            if (dataSet.Items[0].CheckGet("SHIPMENT_ID").ToInt() > 0)
                            {
                                succesfullFlag = true;
                            }
                        }
                    }

                    if (succesfullFlag)
                    {
                        var msg = "Успешное создание отгрузки";
                        var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                        d.ShowDialog();

                        Refresh();
                    }
                    else
                    {
                        var msg = "Ошибка создания отгрузки. Пожалуйста, сообщите о проблеме.";
                        var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
            else
            {
                var msg = "Не выбрана заявка для создания отгрузки";
                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        private void AddPosition()
        {
            if (OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0)
            {
                var i = new ScrapPaperConsumptionKshOrderPosition();
                i.RoleName = this.RoleName;
                i.FactoryId = this.FactoryId;
                i.ParentFrame = this.FrameName;
                i.OrderId = OrderGrid.SelectedItem.CheckGet("ORDER_ID").ToInt();
                i.BuyerId = OrderGrid.SelectedItem.CheckGet("CONSIGNEE_ID").ToInt();
                i.Show();
            }
            else
            {
                var msg = "Не выбрана заявка для создания позиции";
                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        private void EditPosition()
        {
            if (PositionGrid.SelectedItem != null && PositionGrid.SelectedItem.Count > 0)
            {
                var i = new ScrapPaperConsumptionKshOrderPosition();
                i.RoleName = this.RoleName;
                i.FactoryId = this.FactoryId;
                i.ParentFrame = this.FrameName;
                i.OrderId = OrderGrid.SelectedItem.CheckGet("ORDER_ID").ToInt();
                i.OrderPositionId = PositionGrid.SelectedItem.CheckGet("ORDER_POSITION_ID").ToInt();
                i.BuyerId = OrderGrid.SelectedItem.CheckGet("CONSIGNEE_ID").ToInt();
                i.Show();
            }
            else
            {
                var msg = "Не выбрана позиции заявки для изменения";
                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        private void DeletePosition()
        {
            if (PositionGrid.SelectedItem != null && PositionGrid.SelectedItem.Count > 0)
            {
                var msg = $"Вы действительно хотите удалить позицию заявки {PositionGrid.SelectedItem.CheckGet("ORDER_POSITION_ID")} {PositionGrid.SelectedItem.CheckGet("PRODUCT_NAME")}?";
                if (DialogWindow.ShowDialog($"{msg}", this.ControlTitle, "", DialogWindowButtons.YesNo) == true)
                {
                    var p = Form.GetValues();
                    p.CheckAdd("ORDER_POSITION_ID", PositionGrid.SelectedItem.CheckGet("ORDER_POSITION_ID"));

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Orders");
                    q.Request.SetParam("Object", "ScrapPaper");
                    q.Request.SetParam("Action", "DeleteOrderPosition");
                    q.Request.SetParams(p);

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {
                        bool succesfullFlag = false;
                        int orderId = 0;

                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            var dataSet = ListDataSet.Create(result, "ITEMS");
                            if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                            {
                                orderId = dataSet.Items[0].CheckGet("ORDER_POSITION_ID").ToInt();
                                if (orderId > 0)
                                {
                                    succesfullFlag = true;
                                }
                            }
                        }

                        if (succesfullFlag)
                        {
                            msg = "Успешное удаление позиции заявки на отгрузку макулатуры.";
                            var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                            d.ShowDialog();

                            Refresh();
                        }
                        else
                        {
                            msg = "Ошибка удаления позиции заявки на отгрузку макулатуры. Пожалуйста, сообщите о проблеме.";
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
                var msg = "Не выбрана позиция заявки для удаления";
                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }
    }
}
