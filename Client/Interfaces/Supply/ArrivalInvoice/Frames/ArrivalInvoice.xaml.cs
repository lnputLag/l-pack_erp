using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
using Client.Interfaces.Service.Printing;
using Client.Interfaces.Stock;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
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

namespace Client.Interfaces.Supply
{
    /// <summary>
    /// Форма редактирования приходной накладной
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class ArrivalInvoice : ControlBase
    {
        public ArrivalInvoice()
        {
            ControlTitle = "Приходная накладная";
            DocumentationUrl = "/doc/l-pack-erp/";
            RoleName = "[erp]arrival_invoice";
            InitializeComponent();

            if (Central.DebugMode)
            {
                SellerIdTextBox.Visibility = Visibility.Visible;
            }

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (System.Windows.Input.KeyEventArgs e) =>
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
                InitForm();
                SetDefaults();
                IncomingGridInit();
                LoadInvoiceData();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                IncomingGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                IncomingGrid.ItemsAutoUpdate = true;
                IncomingGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                IncomingGrid.ItemsAutoUpdate = false;
            };

            {
                Commander.Add(new CommandItem()
                {
                    Name = "save",
                    Group = "main",
                    Enabled = false,
                    Title = "Сохранить",
                    Description = "Сохранить данные",
                    ButtonUse = true,
                    ButtonControl = SaveButton,
                    ButtonName = "SaveButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        Save();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = CheckDateInReportingPeriod();

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "cancel",
                    Group = "main",
                    Enabled = true,
                    Title = "Отмена",
                    Description = "Отмена",
                    ButtonUse = true,
                    ButtonControl = CancelButton,
                    ButtonName = "CancelButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        Close();
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

                Commander.Add(new CommandItem()
                {
                    Name = "select_seller",
                    Group = "main",
                    Enabled = true,
                    Title = "",
                    Description = "Выбрать поставщика",
                    ButtonUse = true,
                    ButtonControl = SelectSellerButton,
                    ButtonName = "SelectSellerButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        SelectSeller();
                    },
                });
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
                        IncomingGridLoadItems();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "auto_update_price",
                    Group = "main",
                    Enabled = false,
                    Title = "Подгрузить цены",
                    Description = "Автоматически подгрузить цены на все позиции накладной",
                    ButtonUse = true,
                    ButtonControl = AutoUpdatePriceButton,
                    ButtonName = "AutoUpdatePriceButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        AutoUpdatePrice();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (InvoiceId > 0 && CheckDateInReportingPeriod())
                        {
                            result = true;                            
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "update_pay_debt_supplier",
                    Group = "main",
                    Enabled = true,
                    Title = "Пересчитать оплату",
                    Description = "Пересчитать оплату поставщику",
                    ButtonUse = true,
                    ButtonControl = UpdatePayDebtSupplierButton,
                    ButtonName = "UpdatePayDebtSupplierButton",
                    AccessLevel = Role.AccessMode.Special,
                    Action = () =>
                    {
                        UpdatePayDebtSupplier();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (InvoiceId > 0 && CheckDateInReportingPeriod())
                        {
                            result = true;
                        }

                        return result;
                    },
                });
            }

            Commander.SetCurrentGridName("IncomingGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "new_item",
                    Title = "Добавить",
                    Description = "Добавить новую позицию",
                    Group = "incoming_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = NewItemButton,
                    ButtonName = "NewItemButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        AddItem();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (InvoiceId > 0 && CheckDateInReportingPeriod())
                        {
                            result = true;
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "edit_item",
                    Title = "Изменить",
                    Description = "Изменить выбранную позицию",
                    Group = "incoming_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = EditItemButton,
                    ButtonName = "EditItemButton",
                    HotKey = "Return|DoubleCLick",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        EditItem();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (InvoiceId > 0 && CheckDateInReportingPeriod())
                        {
                            if (IncomingGrid != null && IncomingGrid.SelectedItem != null && IncomingGrid.SelectedItem.Count > 0)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "delete_item",
                    Title = "Удалить",
                    Description = "Удалить выбранную позицию",
                    Group = "incoming_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = DeleteItemButton,
                    ButtonName = "DeleteItemButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        DeleteItem();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (InvoiceId > 0 && CheckDateInReportingPeriod())
                        {
                            if (IncomingGrid != null && IncomingGrid.SelectedItem != null && IncomingGrid.SelectedItem.Count > 0)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });

                Commander.Add(new CommandItem()
                {
                    Name = "confirm_incoming",
                    Title = "Провести",
                    Description = "Провести одну выбранную позицию",
                    Group = "incoming_grid_default2",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = false,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        ConfirmOneIncoming(IncomingGrid.SelectedItem);
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (InvoiceId > 0 && CheckDateInReportingPeriod())
                        {
                            if (IncomingGrid != null && IncomingGrid.SelectedItem != null && IncomingGrid.SelectedItem.Count > 0)
                            {
                                if (IncomingGrid.SelectedItem.CheckGet("COMPLETED_FLAG").ToInt() == 0)
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
                    Name = "unconfirm_incoming",
                    Title = "Отменить проведение",
                    Description = "Отменить проведение одной выбранной позиции",
                    Group = "incoming_grid_default2",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = false,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        UnconfirmOneIncoming(IncomingGrid.SelectedItem);
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (InvoiceId > 0 && CheckDateInReportingPeriod())
                        {
                            if (IncomingGrid != null && IncomingGrid.SelectedItem != null && IncomingGrid.SelectedItem.Count > 0)
                            {
                                if (IncomingGrid.SelectedItem.CheckGet("COMPLETED_FLAG").ToInt() > 0)
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
                    Name = "confirm_selected_incoming",
                    Title = "Провести выбранные",
                    Description = "Провести выбранные позиции",
                    Group = "incoming_grid_default3",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = ConfirmSelectedIncomingButton,
                    ButtonName = "ConfirmSelectedIncomingButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        ConfirmSelectedIncoming();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (InvoiceId > 0 && CheckDateInReportingPeriod())
                        {
                            if (IncomingGrid != null && IncomingGrid.Items != null && IncomingGrid.Items.Count > 0)
                            {
                                if (IncomingGrid.Items.Count(x => x.CheckGet("CHECKED_FLAG").ToInt() > 0 && x.CheckGet("COMPLETED_FLAG").ToInt() == 0) > 0)
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
                    Name = "unconfirm_selected_incoming",
                    Title = "Отменить проведение выбранных",
                    Description = "Отменить проведение выбранных позиций",
                    Group = "incoming_grid_default3",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = UnconfirmSelectedIncomingButton,
                    ButtonName = "UnconfirmSelectedIncomingButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        UnconfirmSelectedIncoming();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (InvoiceId > 0 && CheckDateInReportingPeriod())
                        {
                            if (IncomingGrid != null && IncomingGrid.Items != null && IncomingGrid.Items.Count > 0)
                            {
                                if (IncomingGrid.Items.Count(x => x.CheckGet("CHECKED_FLAG").ToInt() > 0 && x.CheckGet("COMPLETED_FLAG").ToInt() > 0) > 0)
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
                    Name = "confirm_all_incoming",
                    Title = "Провести все",
                    Description = "Провести все позиции по накладной",
                    Group = "incoming_grid_default4",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = false,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        ConfirmAllIncoming();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (InvoiceId > 0 && CheckDateInReportingPeriod())
                        {
                            if (IncomingGrid != null && IncomingGrid.Items != null && IncomingGrid.Items.Count > 0)
                            {
                                if (IncomingGrid.Items.Count(x => x.CheckGet("COMPLETED_FLAG").ToInt() == 0) > 0)
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
                    Name = "unconfirm_all_incoming",
                    Title = "Отменить проведение всех",
                    Description = "Отменить проведение всех позиций по накладной",
                    Group = "incoming_grid_default4",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = false,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        UnconfirmAllIncoming();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (InvoiceId > 0 && CheckDateInReportingPeriod())
                        {
                            if (IncomingGrid != null && IncomingGrid.Items != null && IncomingGrid.Items.Count > 0)
                            {
                                if (IncomingGrid.Items.Count(x => x.CheckGet("COMPLETED_FLAG").ToInt() > 0) > 0)
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
                    Name = "print_label",
                    Title = "Печать ярлыка",
                    Description = "Печать ярлыка для одной выбранной позиции",
                    Group = "incoming_grid_default5",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = false,
                    Action = () =>
                    {
                        PrintOneLabel(IncomingGrid.SelectedItem);
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (IncomingGrid != null && IncomingGrid.SelectedItem != null && IncomingGrid.SelectedItem.Count > 0)
                        {
                            if (IncomingGrid.SelectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 2
                                || IncomingGrid.SelectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 3
                                || IncomingGrid.SelectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 17)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "print_selected_label",
                    Title = "Печать выбранных ярлыков",
                    Description = "Печать ярлыка для выбранных позиций",
                    Group = "incoming_grid_default5",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = PrintSelectedLabelButton,
                    ButtonName = "PrintSelectedLabelButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        PrintSelectedLabel();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (IncomingGrid != null && IncomingGrid.Items != null && IncomingGrid.Items.Count > 0)
                        {
                            if (IncomingGrid.Items.Count(x => 
                                    x.CheckGet("CHECKED_FLAG").ToInt() > 0 
                                    && (x.CheckGet("PRODUCT_IDK1").ToInt() == 2 || x.CheckGet("PRODUCT_IDK1").ToInt() == 3 || x.CheckGet("PRODUCT_IDK1").ToInt() == 17)
                                ) > 0)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "print_all_label",
                    Title = "Печать всех ярлыков",
                    Description = "Печать ярлыка для всех позиций по накладной",
                    Group = "incoming_grid_default5",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = false,
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        PrintAllLabel();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (IncomingGrid != null && IncomingGrid.Items != null && IncomingGrid.Items.Count > 0)
                        {
                            if (IncomingGrid.Items.Count(x => 
                                    x.CheckGet("PRODUCT_IDK1").ToInt() == 2 || x.CheckGet("PRODUCT_IDK1").ToInt() == 3 || x.CheckGet("PRODUCT_IDK1").ToInt() == 17
                                ) > 0)
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
        /// Датасет с данными грида позиций прихода
        /// </summary>
        private ListDataSet IncomingGridDataSet { get; set; }

        /// <summary>
        /// Идентификатор приходной накладной
        /// </summary>
        public int InvoiceId { get; set; }

        /// <summary>
        /// Наименование таба, который вызвал этот таб
        /// </summary>
        public string ParentFrame { get; set; }

        /// <summary>
        /// Дата, с которой разрешено редактирование движения товаров, продукции и денег
        /// </summary>
        public string ReportingPeriod { get; set; }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void InitForm()
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

                    new FormHelperField()
                    {
                        Path = "INVOICE_DATE",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = InvoiceDateTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "DATASF",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = DatasfTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "DATAPSF",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = DatapsfTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "DATAOPRSF",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = DataoprsfTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },

                    new FormHelperField()
                    {
                        Path = "NAME_NAKL",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = NameNaklTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "NAMESF",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = NamesfTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "TRANSPORT_NUMBER",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = TransportNumberTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "SHIPMENT_NUMBER",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = ShipmentNumberTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },

                    new FormHelperField()
                    {
                        Path = "CUSTOMER_ID",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = CustomerSelectBox,
                        ControlType = "SelectBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                               { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "SELLER_ID",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = SellerIdTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                               { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "SELLER_NAME",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = SellerNameTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "CONTRACT_ID",
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = ContractSelectBox,
                        ControlType = "SelectBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "DELIVERY_NUM",
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = DeliverySelectBox,
                        ControlType = "SelectBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },

                    new FormHelperField()
                    {
                        Path = "NOTE",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = NoteTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "WEIGHT_NETTO_DOC",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = WeightNettoDocTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "FACTORY_ID",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = FactorySelectBox,
                        ControlType = "SelectBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },

                    new FormHelperField()
                    {
                        Path = "INVOICE_ID",
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = null,
                        ControlType = "void",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                };

                Form.SetFields(fields);

                Form.BeforeSet = (Dictionary<string, string> values) =>
                {
                    if (!string.IsNullOrEmpty(values.CheckGet("FACTORY_ID")))
                    {
                        values.CheckAdd("FACTORY_ID", values.CheckGet("FACTORY_ID").ToInt().ToString());
                    }
                };
            }
        }

        public async void LoadInvoiceData()
        {
            if (InvoiceId > 0)
            {
                FactorySelectBox.IsEnabled = false;

                var p = new Dictionary<string, string>();
                p.Add("INVOICE_ID", InvoiceId.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Supply");
                q.Request.SetParam("Object", "ArrivalInvoice");
                q.Request.SetParam("Action", "Get");
                q.Request.SetParams(p);

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
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                        {
                            Form.SetValues(ds);
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        public void SetDefaults()
        {
            IncomingGridDataSet = new ListDataSet();

            Form.SetDefaults();

            var factorySelectBoxItems = new Dictionary<string, string>();
            factorySelectBoxItems.Add("1", "Л-ПАК ЛИПЕЦК");
            factorySelectBoxItems.Add("2", "Л-ПАК КАШИРА");
            FactorySelectBox.SetItems(factorySelectBoxItems);

            FillCustomerSelectBox();
            FillDeliverySelectBox();

            if (InvoiceId == 0)
            {
                Form.SetValueByPath("INVOICE_DATE", DateTime.Now.ToString("dd.MM.yyyy"));
                Form.SetValueByPath("DATASF", DateTime.Now.ToString("dd.MM.yyyy"));
                Form.SetValueByPath("DATAPSF", DateTime.Now.ToString("dd.MM.yyyy"));
                Form.SetValueByPath("DATAOPRSF", DateTime.Now.ToString("dd.MM.yyyy"));
                Form.SetValueByPath("DELIVERY_NUM", "3");

                Commander.ProcessSelectItem(null);
            }
            else
            {
                FactorySelectBox.IsEnabled = false;
            }
        }

        public void FillCustomerSelectBox()
        {
            var сustomerSelectBoxItems = new Dictionary<string, string>();
            сustomerSelectBoxItems.Add("1", "ТД Л-Пак");
            сustomerSelectBoxItems.Add("2", "Л-ПАК");
            сustomerSelectBoxItems.Add("23", "БумПак");
            сustomerSelectBoxItems.Add("427", "Л-ПАК Кашира");
            CustomerSelectBox.SetItems(сustomerSelectBoxItems);
        }

        public void FillDeliverySelectBox()
        {
            var deliverySelectBoxItems = new Dictionary<string, string>();
            deliverySelectBoxItems.Add("1", "Доставка");
            deliverySelectBoxItems.Add("2", "Компенсация транспорта");
            deliverySelectBoxItems.Add("3", "Самовывоз");
            DeliverySelectBox.SetItems(deliverySelectBoxItems);
        }

        public void IncomingGridInit()
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
                        ColumnType = DataGridHelperColumn.ColumnTypeRef.Boolean,
                        Width2=5,
                        Editable = true,
                    },
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
                        Description = "Идентификатор прихода",
                        Path="INCOMING_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Арикул",
                        Path="PRODUCT_CODE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Код товара",
                        Path="PRODUCT_KOD",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование продукции",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=32,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Внешний номер",
                        Path="ROLL_NAME_OUTER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Внутренний номер",
                        Path="ROLL_NAME_INNER2",
                        ColumnType=ColumnTypeRef.String,
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Path="QUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                        TotalsType = TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Единица измерения",
                        Path="IZM_FIRST_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Цена",
                        Path="PRICE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Сумма",
                        Path="SUMMARY_PRICE",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width2=10,
                        TotalsType = TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество 2",
                        Path="QUANTITY2",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                        TotalsType = TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Единица измерения 2",
                        Path="IZM_SECOND_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Площадь изделия",
                        Path="PRODUCT_SQUARE",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Проведен",
                        Path="COMPLETED_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Производитель",
                        Path="MANUFACTURER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=13,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата оприходования",
                        Path="CREATED_DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format = "dd.MM.yyyy HH:mm:ss",
                        Width2=14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Склад",
                        Description = "Текущее положение",
                        Path="SKLAD",
                        ColumnType=ColumnTypeRef.String,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ячейка",
                        Description = "Текущее положение",
                        Path="NUM_PLACE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата постановки",
                        Description = "Дата перемещения в текущую ячейку",
                        Path="PLACED_DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format = "dd.MM.yyyy HH:mm:ss",
                        Width2=14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дефект макулатуры",
                        Path="SCRAP_DEFECT_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид продукции",
                        Path="PRODUCT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Ид категории продукции",
                        Path="PRODUCT_IDK1",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Внутренний номер рулона",
                        Description = "Без обработки для читаемости",
                        Path="ROLL_NAME_INNER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид отдела",
                        Path="DEPARTMENT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                };
                IncomingGrid.SetColumns(columns);

                IncomingGrid.SearchText = SearchText;
                IncomingGrid.OnLoadItems = IncomingGridLoadItems;
                IncomingGrid.SetPrimaryKey("INCOMING_ID");
                IncomingGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                IncomingGrid.AutoUpdateInterval = 60;
                IncomingGrid.Toolbar = IncomingGridToolbar;

                IncomingGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    // определение цветов фона строк
                    {
                        StylerTypeRef.BackgroundColor,
                        (Dictionary<string, string> row) =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            // Позиция проведена
                            if (row.CheckGet("COMPLETED_FLAG").ToInt() == 0)
                            {
                                color = HColor.Blue;
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },
                };

                IncomingGrid.OnColumnConstructed = SortColumnsByUserParameterList;
                IncomingGrid.OnGridControlColumnsCollectionChanged = Columns_CollectionChanged;

                IncomingGrid.Commands = Commander;
                IncomingGrid.UseProgressSplashAuto = false;
                IncomingGrid.Init();
            }
        }

        private void Columns_CollectionChanged(DevExpress.Xpf.Grid.GridColumnCollection sender)
        {
            UserParameter.SaveGridBox4ColumnsByUserParameterList(sender, this, "IncomingGridColumnPositionList");
        }

        private void SortColumnsByUserParameterList()
        {
            UserParameter.SortGridBox4ColumnsByUserParameterList(IncomingGrid, this, "IncomingGridColumnPositionList");
        }

        public async void IncomingGridLoadItems()
        {
            if (InvoiceId > 0)
            {
                var p = new Dictionary<string, string>();
                p.Add("INVOICE_ID", InvoiceId.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Supply");
                q.Request.SetParam("Object", "ArrivalInvoice");
                q.Request.SetParam("Action", "ListIncoming");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                IncomingGridDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        IncomingGridDataSet = ListDataSet.Create(result, "ITEMS");
                        if (IncomingGrid.Items != null)
                        {
                            foreach (var item in IncomingGridDataSet.Items)
                            {
                                var row = IncomingGrid.Items.FirstOrDefault(x => x.CheckGet("INCOMING_ID").ToInt() == item.CheckGet("INCOMING_ID").ToInt());
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
                IncomingGrid.UpdateItems(IncomingGridDataSet);
            }
        }

        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Show()
        {
            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 1;
            FrameName = $"{FrameName}_{InvoiceId}";

            if (InvoiceId > 0)
            {
                Central.WM.Show(FrameName, $"Детализация накладной {InvoiceId}", true, "add", this);
            }
            else
            {
                Central.WM.Show(FrameName, $"Детализация новой накладной", true, "add", this);
            }
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            if (!string.IsNullOrEmpty(ParentFrame))
            {
                Central.WM.SetActive(ParentFrame, true);
            }

            Central.WM.Close(FrameName);

            //вся работа по утилизации ресурсов происходит в Destroy
            //он будет вызван при закрытии фрейма
        }

        public void GetContractList()
        {
            if (!string.IsNullOrEmpty(Form.GetValueByPath("CUSTOMER_ID"))
                && !string.IsNullOrEmpty(Form.GetValueByPath("SELLER_ID")))
            {
                var p = new Dictionary<string, string>();
                p.Add("SELLER_ID", Form.GetValueByPath("SELLER_ID"));
                p.Add("CUSTOMER_ID", Form.GetValueByPath("CUSTOMER_ID"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Supply");
                q.Request.SetParam("Object", "ArrivalInvoice");
                q.Request.SetParam("Action", "ListContract");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                ContractSelectBox.DropDownListBox.Items.Clear();
                ContractSelectBox.Items.Clear();
                ContractSelectBox.DropDownListBox.SelectedItem = null;
                ContractSelectBox.ValueTextBox.Text = "";
                ContractSelectBox.IsEnabled = false;
                int currentContract = Form.GetValueByPath("CONTRACT_ID").ToInt();
                Form.SetValueByPath("CONTRACT_ID", "0");

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                        {
                            ContractSelectBox.IsEnabled = true;
                            ContractSelectBox.SetItems(ds, FormHelperField.FieldTypeRef.Integer, "CONTRACT_ID", "CONTRACT_FULL_NAME");
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }

                if (ContractSelectBox.Items.ContainsKey(currentContract.ToString()))
                {
                    ContractSelectBox.SetSelectedItemByKey(currentContract.ToString());
                }
            }
        }

        public void AddItem()
        {
            var window = new IncomingEdit();
            window.InvoiceId = InvoiceId;
            window.ParentFrame = this.FrameName;
            window.Show();
        }

        public void EditItem()
        {
            if (IncomingGrid.SelectedItem != null && IncomingGrid.SelectedItem.Count > 0)
            {
                if (IncomingGrid.SelectedItem.CheckGet("COMPLETED_FLAG").ToInt() > 0)
                {
                    var msg = $"Эти данные из накладной уже прошли обработку. Всё равно изменить позицию прихода {IncomingGrid.SelectedItem.CheckGet("INCOMING_ID")}?";
                    var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.NoYes);
                    if (d.ShowDialog() != true)
                    {
                        return;
                    }
                }

                var window = new IncomingEdit();
                window.IncomingId = IncomingGrid.SelectedItem.CheckGet("INCOMING_ID").ToInt();
                window.IncomingData = IncomingGrid.SelectedItem;
                window.ParentFrame = this.FrameName;
                window.Show();
            }
            else
            {
                var msg = "Не выбрана позиция для редактирования.";
                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        public void DeleteItem()
        {
            if (IncomingGrid.SelectedItem != null && IncomingGrid.SelectedItem.Count > 0)
            {
                if (IncomingGrid.SelectedItem.CheckGet("COMPLETED_FLAG").ToInt() > 0)
                {
                    var msg = $"Эти данные из накладной уже прошли обработку. Всё равно удалить позицию прихода {IncomingGrid.SelectedItem.CheckGet("INCOMING_ID")}?";
                    var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.NoYes);
                    if (d.ShowDialog() != true)
                    {
                        return;
                    }
                }
                else
                {
                    var msg = $"Удалить позицию прихода {IncomingGrid.SelectedItem.CheckGet("INCOMING_ID")}?";
                    var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.NoYes);
                    if (d.ShowDialog() != true)
                    {
                        return;
                    }
                }

                var p = new Dictionary<string, string>();
                p.CheckAdd("INVOICE_ID", InvoiceId.ToString());
                p.CheckAdd("INCOMING_ID", IncomingGrid.SelectedItem.CheckGet("INCOMING_ID"));
                p.CheckAdd("SELLER_ID", Form.GetValueByPath("SELLER_ID"));
                p.CheckAdd("COMPLETED_FLAG", IncomingGrid.SelectedItem.CheckGet("COMPLETED_FLAG").ToInt().ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Supply");
                q.Request.SetParam("Object", "ArrivalInvoice");
                q.Request.SetParam("Action", "DeleteIncoming");
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
                            if (dataSet.Items.First().CheckGet("INCOMING_ID").ToInt() > 0)
                            {
                                succesfullFlag = true;
                            }
                        }
                    }

                    if (succesfullFlag)
                    {
                        // Отправляем сообщение вкладке "Список приходных накладных" обновиться
                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverGroup = "Supply",
                            ReceiverName = "ArrivalInvoiceList",
                            SenderName = this.ControlName,
                            Action = "Refresh",
                            Message = $"{InvoiceId}",
                        });

                        IncomingGridLoadItems();

                        var msg = "Успешное удаление позиции прихода.";
                        var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                    else
                    {
                        var msg = "Ошибка удаления позиции прихода. Пожалуйста, сообщите о проблеме.";
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
                var msg = "Не выбрана позиция для удаления";
                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Проведение всех позиций прихода по накладной
        /// </summary>
        public void ConfirmAllIncoming()
        {
            if (IncomingGrid != null && IncomingGrid.Items != null && IncomingGrid.Items.Count > 0)
            {
                string msg = "";
                foreach (var selectedItem in IncomingGrid.Items)
                {
                    if (selectedItem != null && selectedItem.Count > 0 && selectedItem.CheckGet("COMPLETED_FLAG").ToInt() == 0)
                    {
                        if (!ConfirmIncoming(selectedItem))
                        {
                            msg = "Ошибка проведения позиций прихода. Пожалуйста, сообщите о проблеме.";
                        }
                    }
                }

                if (string.IsNullOrEmpty(msg))
                {
                    msg = "Успешное проведение всех позиций прихода по накладной";
                }

                // Отправляем сообщение вкладке "Список приходных накладных" обновиться
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "Supply",
                    ReceiverName = "ArrivalInvoiceList",
                    SenderName = this.ControlName,
                    Action = "Refresh",
                    Message = $"{InvoiceId}",
                });

                IncomingGridLoadItems();

                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        public void ConfirmSelectedIncoming()
        {
            if (IncomingGrid != null && IncomingGrid.Items != null && IncomingGrid.Items.Count > 0)
            {
                if (IncomingGrid.Items.Count(x => x.CheckGet("CHECKED_FLAG").ToInt() > 0) > 0)
                {
                    string msg = "";
                    var selectedItems = IncomingGrid.Items.Where(x => x.CheckGet("CHECKED_FLAG").ToInt() > 0).ToList();
                    foreach (var selectedItem in selectedItems)
                    {
                        if (selectedItem != null && selectedItem.Count > 0 && selectedItem.CheckGet("COMPLETED_FLAG").ToInt() == 0)
                        {
                            if (ConfirmIncoming(selectedItem))
                            {
                                selectedItem.CheckAdd("CHECKED_FLAG", "0");
                            }
                            else
                            {
                                msg = "Ошибка проведения позиций прихода. Пожалуйста, сообщите о проблеме.";
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(msg))
                    {
                        msg = "Успешное проведение выбранных позиций прихода по накладной";
                    }

                    // Отправляем сообщение вкладке "Список приходных накладных" обновиться
                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverGroup = "Supply",
                        ReceiverName = "ArrivalInvoiceList",
                        SenderName = this.ControlName,
                        Action = "Refresh",
                        Message = $"{InvoiceId}",
                    });

                    IncomingGridLoadItems();

                    var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
        }

        /// <summary>
        /// Проведение одной позиции прихода
        /// </summary>
        /// <param name="selectedItem"></param>
        /// <param name="showLogMessage"></param>
        public void ConfirmOneIncoming(Dictionary<string, string> selectedItem)
        {
            if (selectedItem != null && selectedItem.Count > 0)
            {
                string msg = "";
                if (selectedItem.CheckGet("COMPLETED_FLAG").ToInt() > 0)
                {
                    msg = "Выбранная позиция уже проведена";
                    var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                    d.ShowDialog();

                    return;
                }

                {
                    msg = "";
                    if (ConfirmIncoming(selectedItem))
                    {
                        msg = "Успешное проведение позиции прихода.";
                    }
                    else
                    {
                        msg = "Ошибка проведения позиции прихода. Пожалуйста, сообщите о проблеме.";
                    }

                    // Отправляем сообщение вкладке "Список приходных накладных" обновиться
                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverGroup = "Supply",
                        ReceiverName = "ArrivalInvoiceList",
                        SenderName = this.ControlName,
                        Action = "Refresh",
                        Message = $"{InvoiceId}",
                    });

                    IncomingGridLoadItems();

                    var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                var msg = "Не выбрана позиция для проведения";
                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        public bool ConfirmIncoming(Dictionary<string, string> selectedItem)
        {
            bool confirmResult = false;

            if (selectedItem != null && selectedItem.Count > 0)
            {
                var p = new Dictionary<string, string>();
                p.CheckAdd("INVOICE_ID", InvoiceId.ToString());
                p.CheckAdd("INCOMING_ID", selectedItem.CheckGet("INCOMING_ID"));
                p.CheckAdd("SELLER_ID", Form.GetValueByPath("SELLER_ID"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Supply");
                q.Request.SetParam("Object", "ArrivalInvoice");
                q.Request.SetParam("Action", "ConfirmIncoming");
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
                            if (dataSet.Items.First().CheckGet("INCOMING_ID").ToInt() > 0)
                            {
                                confirmResult = true;
                            }
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            return confirmResult;
        }

        /// <summary>
        /// Отмена проведения всех позиций прихода по накладной
        /// </summary>
        public void UnconfirmAllIncoming()
        {
            if (IncomingGrid != null && IncomingGrid.Items != null && IncomingGrid.Items.Count > 0)
            {
                string msg = "";
                foreach (var selectedItem in IncomingGrid.Items)
                {
                    if (selectedItem != null && selectedItem.Count > 0 && selectedItem.CheckGet("COMPLETED_FLAG").ToInt() > 0)
                    {
                        if (!UnconfirmIncoming(selectedItem))
                        {
                            msg = "Ошибка отмены проведения позиций прихода. Пожалуйста, сообщите о проблеме.";
                        }
                    }
                }

                if (string.IsNullOrEmpty(msg))
                {
                    msg = "Успешная отмена проведения всех позиций прихода по накладной";
                }

                // Отправляем сообщение вкладке "Список приходных накладных" обновиться
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "Supply",
                    ReceiverName = "ArrivalInvoiceList",
                    SenderName = this.ControlName,
                    Action = "Refresh",
                    Message = $"{InvoiceId}",
                });

                IncomingGridLoadItems();

                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        public void UnconfirmSelectedIncoming()
        {
            if (IncomingGrid != null && IncomingGrid.Items != null && IncomingGrid.Items.Count > 0)
            {
                if (IncomingGrid.Items.Count(x => x.CheckGet("CHECKED_FLAG").ToInt() > 0) > 0)
                {
                    string msg = "";
                    var selectedItems = IncomingGrid.Items.Where(x => x.CheckGet("CHECKED_FLAG").ToInt() > 0).ToList();
                    foreach (var selectedItem in selectedItems)
                    {
                        if (selectedItem != null && selectedItem.Count > 0 && selectedItem.CheckGet("COMPLETED_FLAG").ToInt() > 0)
                        {
                            if (UnconfirmIncoming(selectedItem))
                            {
                                selectedItem.CheckAdd("CHECKED_FLAG", "0");
                            }
                            else
                            {
                                msg = "Ошибка отмены проведения позиций прихода. Пожалуйста, сообщите о проблеме.";
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(msg))
                    {
                        msg = "Успешная отмена проведения выбранных позиций прихода по накладной";
                    }

                    // Отправляем сообщение вкладке "Список приходных накладных" обновиться
                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverGroup = "Supply",
                        ReceiverName = "ArrivalInvoiceList",
                        SenderName = this.ControlName,
                        Action = "Refresh",
                        Message = $"{InvoiceId}",
                    });

                    IncomingGridLoadItems();

                    var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
        }

        /// <summary>
        /// Отмена проведения одной позиции прихода
        /// </summary>
        /// <param name="selectedItem"></param>
        public void UnconfirmOneIncoming(Dictionary<string, string> selectedItem)
        {
            if (selectedItem != null && selectedItem.Count > 0)
            {
                string msg = "";
                if (selectedItem.CheckGet("COMPLETED_FLAG").ToInt() == 0)
                {
                    msg = "Выбранная позиция ещё не проведена";
                    var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                    d.ShowDialog();

                    return;
                }

                {
                    msg = "";
                    if (UnconfirmIncoming(selectedItem))
                    {
                        msg = "Успешная отмена проведения позиции прихода.";
                    }
                    else
                    {
                        msg = "Ошибка отмены проведения позиции прихода. Пожалуйста, сообщите о проблеме.";
                    }

                    // Отправляем сообщение вкладке "Список приходных накладных" обновиться
                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverGroup = "Supply",
                        ReceiverName = "ArrivalInvoiceList",
                        SenderName = this.ControlName,
                        Action = "Refresh",
                        Message = $"{InvoiceId}",
                    });

                    IncomingGridLoadItems();

                    var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                var msg = "Не выбрана позиция для отмены проведения";
                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        public bool UnconfirmIncoming(Dictionary<string, string> selectedItem)
        {
            bool unconfirmResult = false;

            if (selectedItem != null && selectedItem.Count > 0)
            {
                var p = new Dictionary<string, string>();
                p.CheckAdd("INCOMING_ID", selectedItem.CheckGet("INCOMING_ID"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Supply");
                q.Request.SetParam("Object", "ArrivalInvoice");
                q.Request.SetParam("Action", "UnconfirmIncoming");
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
                            if (dataSet.Items.First().CheckGet("INCOMING_ID").ToInt() > 0)
                            {
                                unconfirmResult = true;
                            }
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            return unconfirmResult;
        }

        public void PrintLabel(string incomingId)
        {
            if (!string.IsNullOrEmpty(incomingId))
            {
                var rawMaterialLabelReport = new RawMaterialLabelReport();
                rawMaterialLabelReport.PrintLabel(incomingId);
            }
            else
            {
                var msg = "Не выбрана позиция для печати ярлыка";
                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        public void PrintOneLabel(Dictionary<string, string> selectedItem)
        {
            if (selectedItem != null && selectedItem.Count > 0)
            {
                if (selectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 2
                    || selectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 3
                    || IncomingGrid.SelectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 17)
                {
                    PrintLabel(selectedItem.CheckGet("INCOMING_ID"));
                }
            }
            else
            {
                var msg = "Не выбрана позиция для печати ярлыка";
                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        public void PrintSelectedLabel()
        {
            if (IncomingGrid != null && IncomingGrid.Items != null && IncomingGrid.Items.Count > 0)
            {
                if (IncomingGrid.Items.Count(x => x.CheckGet("CHECKED_FLAG").ToInt() > 0) > 0)
                {
                    var selectedItems = IncomingGrid.Items.Where(x => x.CheckGet("CHECKED_FLAG").ToInt() > 0).ToList();
                    foreach (var item in selectedItems)
                    {
                        if (item.CheckGet("PRODUCT_IDK1").ToInt() == 2
                            || item.CheckGet("PRODUCT_IDK1").ToInt() == 3
                            || IncomingGrid.SelectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 17)
                        {
                            PrintLabel(item.CheckGet("INCOMING_ID"));

                            item.CheckAdd("CHECKED_FLAG", "0");
                        }
                    }

                    IncomingGrid.UpdateItems();
                }
            }
        }

        public void PrintAllLabel()
        {
            if (IncomingGrid != null && IncomingGrid.Items != null && IncomingGrid.Items.Count > 0)
            {
                foreach (var item in IncomingGrid.Items)
                {
                    if (item.CheckGet("PRODUCT_IDK1").ToInt() == 2
                        || item.CheckGet("PRODUCT_IDK1").ToInt() == 3
                        || IncomingGrid.SelectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 17)
                    {
                        PrintLabel(item.CheckGet("INCOMING_ID"));
                    }
                }
            }
        }

        public void SelectSeller()
        {
            var i = new SupplierList();
            i.ParentFrame = this.FrameName;
            i.OnSave = OnSelectSeller;
            i.Show();
        }

        private async void AutoUpdatePrice()
        {
            if (!string.IsNullOrEmpty(Form.GetValueByPath("NAME_NAKL")))
            {
                var p = new Dictionary<string, string>();
                p.CheckAdd("INVOICE_ID", $"{InvoiceId}");
                p.CheckAdd("SELLER_ID", Form.GetValueByPath("SELLER_ID"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Supply");
                q.Request.SetParam("Object", "ArrivalInvoice");
                q.Request.SetParam("Action", "AutoUpdatePrice");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    bool succesFullFlag = false;

                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var dataSet = ListDataSet.Create(result, "ITEMS");
                        if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                        {
                            if (dataSet.Items.First().CheckGet("INVOICE_ID").ToInt() > 0)
                            {
                                succesFullFlag = true;
                            }
                        }
                    }

                    if (succesFullFlag)
                    {
                        IncomingGridLoadItems();

                        var msg = "Успешное заполнение цен для позиций по накладной";
                        var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                    else
                    {
                        var msg = "Ошибка заполнения цен для позиций по накладной. Пожалуйста, сообщите о проблеме.";
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
                var msg = "Не заполнен внешний номер накладной.";
                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        public async void UpdatePayDebtSupplier()
        {
            var p = new Dictionary<string, string>();
            p.CheckAdd("INVOICE_ID", $"{InvoiceId}");
            p.CheckAdd("SELLER_ID", Form.GetValueByPath("SELLER_ID"));

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Supply");
            q.Request.SetParam("Object", "ArrivalInvoice");
            q.Request.SetParam("Action", "UpdatePayDebtSupplier");
            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                bool succesFullFlag = false;

                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var dataSet = ListDataSet.Create(result, "ITEMS");
                    if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                    {
                        if (dataSet.Items.First().CheckGet("INVOICE_ID").ToInt() > 0)
                        {
                            succesFullFlag = true;
                        }
                    }
                }

                if (succesFullFlag)
                {
                    var msg = "Успешная корректировка данных по оплате поставщику";
                    var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
                else
                {
                    var msg = "Ошибка корректировки данных по оплате поставщику. Пожалуйста, сообщите о проблеме.";
                    var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        public void OnSelectSeller(Dictionary<string, string> selectedSupplier)
        {
            Form.SetValueByPath("SELLER_NAME", $"{selectedSupplier.CheckGet("SUPPLIER_NAME")} ({selectedSupplier.CheckGet("ID")})");
            Form.SetValueByPath("SELLER_ID", selectedSupplier.CheckGet("ID"));
        }

        /// <summary>
        /// Проверяем, что дата текущей накладной в отчётном периоде
        /// </summary>
        /// <returns></returns>
        private bool CheckDateInReportingPeriod()
        {
            bool result = false;

            if (Form != null && !string.IsNullOrEmpty(Form.GetValueByPath("INVOICE_DATE")))
            {
                if (Form.GetValueByPath("INVOICE_DATE").ToDateTime() >= ReportingPeriod.ToDateTime())
                {
                    result = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Дополнительная валидация.
        /// Запрещаем выбирать дату, которая находится вне текущего отчётного периода.
        /// </summary>
        public bool ValidateReportingPeriod()
        {
            bool valid = true;
            var reportingPeriod = ReportingPeriod.ToDateTime();
            var invoiceDate = Form.GetValueByPath("INVOICE_DATE").ToDateTime();
            var datasf = Form.GetValueByPath("DATASF").ToDateTime();
            var datapsf = Form.GetValueByPath("DATAPSF").ToDateTime();
            var dataoprsf = Form.GetValueByPath("DATAOPRSF").ToDateTime();

            if (invoiceDate < reportingPeriod)
            {
                valid = false;
            }

            if (datasf < reportingPeriod)
            {
                valid = false;
            }

            if (datapsf < reportingPeriod)
            {
                valid = false;
            }

            if (dataoprsf < reportingPeriod)
            {
                valid = false;
            }

            if (!valid)
            {
                var msg = "Дата не в отчётном периоде.";
                var d = new DialogWindow($"{msg}", "Редактирование документа", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }

            return valid;
        }

        public void Save()
        {
            if (Form.Validate())
            {
                if (ValidateReportingPeriod())
                {
                    if (InvoiceId > 0)
                    {
                        var p = Form.GetValues();
                        p.CheckAdd("INVOICE_ID", $"{InvoiceId}");

                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Supply");
                        q.Request.SetParam("Object", "ArrivalInvoice");
                        q.Request.SetParam("Action", "Update");
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
                                    if (dataSet.Items.First().CheckGet("INVOICE_ID").ToInt() > 0)
                                    {
                                        succesfullFlag = true;
                                    }
                                }
                            }

                            if (succesfullFlag)
                            {
                                LoadInvoiceData();

                                var msg = "Успешное обновление накладной прихода.";
                                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                                d.ShowDialog();

                                // Отправляем сообщение вкладке "Список приходных накладных" обновиться
                                Central.Msg.SendMessage(new ItemMessage()
                                {
                                    ReceiverGroup = "Supply",
                                    ReceiverName = "ArrivalInvoiceList",
                                    SenderName = this.ControlName,
                                    Action = "Refresh",
                                    Message = $"{InvoiceId}",
                                });
                            }
                            else
                            {
                                var msg = "Ошибка обновления накладной прихода. Пожалуйста, сообщите о проблеме.";
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
                        // Если номер счёт-фактуры не соответствует номеру накладной, //и в накладной есть гильзовый картон, то выводим предупреждение
                        if (Form.GetValueByPath("NAME_NAKL") != Form.GetValueByPath("NAMESF")
                            //&& IncomingGrid.Items.Count(x => x.CheckGet("PRODUCT_IDK1").ToInt() == 17) > 0
                            )
                        {
                            var msg = $"Номера накладной и счёт-фактуры не совпадают. Хотите продолжить ?";
                            var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.NoYes);
                            if (d.ShowDialog() != true)
                            {
                                return;
                            }
                        }

                        var p = Form.GetValues();

                        {
                            if (string.IsNullOrEmpty(p.CheckGet("FACTORY_ID")))
                            {
                                // Если покупатель -- Л-Пак Кашира
                                if (p.CheckGet("CUSTOMER_ID").ToInt() == 427)
                                {
                                    p.CheckAdd("FACTORY_ID", "2");
                                }
                                else
                                {
                                    p.CheckAdd("FACTORY_ID", "1");
                                }
                            }
                        }

                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Supply");
                        q.Request.SetParam("Object", "ArrivalInvoice");
                        q.Request.SetParam("Action", "Save"); 
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
                                    InvoiceId = dataSet.Items.First().CheckGet("INVOICE_ID").ToInt();
                                }
                            }

                            if (InvoiceId > 0)
                            {
                                LoadInvoiceData();

                                Commander.ProcessSelectItem(null);

                                var msg = "Успешное создание накладной прихода.";
                                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                                d.ShowDialog();

                                // Отправляем сообщение вкладке "Список приходных накладных" обновиться
                                Central.Msg.SendMessage(new ItemMessage()
                                {
                                    ReceiverGroup = "Supply",
                                    ReceiverName = "ArrivalInvoiceList",
                                    SenderName = this.ControlName,
                                    Action = "Refresh",
                                    Message = $"{InvoiceId}",
                                });
                            }
                            else
                            {
                                var msg = "Ошибка создания наладной прихода. Пожалуйста, сообщите о проблеме.";
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
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            var i = new PrintingInterface();
        }

        private void CustomerSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GetContractList();
        }

        private void SellerIdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            GetContractList();
        }
    }
}
