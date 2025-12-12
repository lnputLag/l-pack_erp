using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Service.Printing;
using Client.Interfaces.Stock;
using Client.Interfaces.Stock.ForkliftDrivers.Windows;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using NPOI.HSSF.Record.Chart;
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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Отгрузка литой тары
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class MoldedContainerShipmentList : ControlBase
    {
        public MoldedContainerShipmentList()
        {
            ControlTitle = "Отгрузка ЛТ";
            DocumentationUrl = "/doc/l-pack-erp-new/lt/shipping_lt";
            RoleName = "[erp]molded_contnr_shipment";
            InitializeComponent();

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

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, _ProcessMessages);

            //конструктор, будет вызван, когда объект создается
            //здесь создаются все внутренние структуры
            //впервые этот коллбэк будет вызван, когда данный таб станет активным
            //впервые (до этих пор, никакая работа внутри не происходит, что экономит ресурсы)
            OnLoad = () =>
            {
                FormInit();
                SetDefaults();
                ShipmentGridInit();
                PositionGridInit();
                DriverGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                ShipmentGrid.Destruct();
                PositionGrid.Destruct();
                DriverGrid.Destruct();

                Messenger.Default.Unregister<ItemMessage>(this, _ProcessMessages);
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                ShipmentGrid.ItemsAutoUpdate = true;
                ShipmentGrid.Run();

                DriverGrid.ItemsAutoUpdate = true;
                DriverGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                ShipmentGrid.ItemsAutoUpdate = false;

                DriverGrid.ItemsAutoUpdate = false;
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

            Commander.SetCurrentGridName("ShipmentGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "bind_terminal",
                    Title = "Привязать к терминалу",
                    Description = "Привязать отгрузку к терминалу",
                    Group = "shipment_grid_terminal",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = BindTerminalButton,
                    ButtonName = "BindTerminalButton",
                    Enabled = false,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        this.BindTerminal(0);
                        BindTerminalButton.IsEnabled = false;
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ShipmentGrid != null && ShipmentGrid.SelectedItem != null && ShipmentGrid.SelectedItem.Count > 0)
                        {
                            if (ShipmentGrid.Items.Count(x => x.CheckGet("TERMINAL_FLAG").ToInt() == 1) == 0)
                            {
                                if (DriverGrid != null && DriverGrid.Items != null && DriverGrid.Items.Count > 0)
                                {
                                    if (DriverGrid.Items.Count(x => !string.IsNullOrEmpty(x.CheckGet("TERMINALNUMBER"))) == 0)
                                    {
                                        result = true;
                                    }
                                }
                                else
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
                    Name = "unbind_terminal",
                    Title = "Отвязать от терминала",
                    Description = "Отвязать отгрузку от терминала",
                    Group = "shipment_grid_terminal",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = UnbindTerminalButton,
                    ButtonName = "UnbindTerminalButton",
                    Enabled = false,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        this.UnbindTerminal();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ShipmentGrid != null && ShipmentGrid.SelectedItem != null && ShipmentGrid.SelectedItem.Count > 0)
                        {
                            if (ShipmentGrid.SelectedItem.CheckGet("TERMINAL_FLAG").ToInt() == 1)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "bind_driver",
                    Title = "Привязать водителя",
                    Description = "Привязать водителя к отгрузке",
                    Group = "shipment_grid_driver",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = BindDriverButton,
                    ButtonName = "BindDriverButton",
                    Enabled = false,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        this.BindDriver();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ShipmentGrid != null && ShipmentGrid.SelectedItem != null && ShipmentGrid.SelectedItem.Count > 0)
                        {
                            if (string.IsNullOrEmpty(ShipmentGrid.SelectedItem.CheckGet("DRIVER_ENTRY_DTTM")))
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "unbind_driver",
                    Title = "Отвязать водителя",
                    Description = "Отвязать водителя от отгрузки",
                    Group = "shipment_grid_driver",
                    MenuUse = true,
                    Enabled = false,
                    AccessLevel= Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        this.UnbindDriver();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ShipmentGrid != null && ShipmentGrid.SelectedItem != null && ShipmentGrid.SelectedItem.Count > 0)
                        {
                            if (ShipmentGrid.SelectedItem.CheckGet("DRIVER_ID").ToInt() > 0
                                // самовывоз
                                && ShipmentGrid.SelectedItem.CheckGet("DRIVER_ID").ToInt() != 1095
                                // неизвестно -- тендер
                                && ShipmentGrid.SelectedItem.CheckGet("DRIVER_ID").ToInt() != 2659)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });               
                Commander.Add(new CommandItem()
                {
                    Name = "document_list",
                    Title = "Список документов",
                    Description = "Накладные по отгрузке",
                    Group = "shipment_grid_document",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = DocumentListButton,
                    ButtonName = "DocumentListButton",
                    Enabled= true,
                    Action = () =>
                    {
                        DocumentList();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "show_loading_scheme",
                    Title = "Порядок загрузки",
                    Description = "Показать порядок загрузки",
                    Group = "shipment_grid_document",
                    MenuUse = true,
                    Enabled = true,
                    Action = () =>
                    {
                        ShowLoadingScheme();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "edit_comment",
                    Title = "Изменить комментарий",
                    Description = "Изменить комментарий к отгрузке",
                    Group = "shipment_grid_default",
                    MenuUse = true,
                    Enabled = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        EditComment();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "create_auto_shipment",
                    Title = "Автосоздание отгрузки",
                    Description = "Автосоздание отгрузки по выбранной отгрузке",
                    Group = "shipment_grid_default",
                    MenuUse = true,
                    Enabled = false,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        CreateAutoShipment();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ShipmentGrid != null && ShipmentGrid.SelectedItem != null && ShipmentGrid.SelectedItem.Count > 0)
                        {
                            // Отрузка завершена -- Есть накладная и машина не на терминале
                            if (ShipmentGrid.SelectedItem.CheckGet("INVOICE_FLAG").ToInt() == 1 && ShipmentGrid.SelectedItem.CheckGet("TERMINAL_FLAG").ToInt() == 0
                                // Самовывоз
                                && ShipmentGrid.SelectedItem.CheckGet("SELFSHIP_FLAG").ToInt() == 1)
                            {
                                result = true;
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
                    Name = "refresh_position_grid",
                    Title = "Обновить",
                    Description = "Обновить данные по позициям заявок в отгрузке",
                    Group = "position_grid_default",
                    MenuUse = true,
                    Enabled = true,
                    Action = () =>
                    {
                        PositionGridLoadItems();
                    },
                });
            }

            Commander.SetCurrentGridName("DriverGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "add_driver",
                    Title = "Добавить",
                    Description = "Добавить нового водителя",
                    Group = "driver_grid_default",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = AddDriverButton,
                    ButtonName = "AddDriverButton",
                    Enabled = true,
                    AccessLevel= Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        this.AddDriver();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "edit_driver",
                    Title = "Изменить",
                    Description = "Изменить данные водителя",
                    Group = "driver_grid_default",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = EditDriverButton,
                    ButtonName = "EditDriverButton",
                    Enabled = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        this.EditDriver();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "delete_driver",
                    Title = "Удалить",
                    Description = "Удалить данные о приезде водителя",
                    Group = "driver_grid_default",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = DeleteDriverButton,
                    ButtonName = "DeleteDriverButton",
                    Enabled = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        this.DeleteDriver();
                    },
                });
            }

            Commander.Init(this);
        }

        private ListDataSet ShipmentDataSet { get; set; }

        private ListDataSet PositionDataSet { get; set; }

        private ListDataSet DriverDataSet { get; set; }

        /// <summary>
        /// Терминал для отгрузки литой тары
        /// </summary>
        public int TerminalId { get; set; }

        public string TerminalName { get; set; }

        public string TerminalLabelDefaultText { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        public Dictionary<string, string> ArrivedDriverData { get; set; }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void _ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.IndexOf("ShipmentControl") > -1)
            {
                if (m.ReceiverName.IndexOf("MoldedContainerShipmentListSelectShipmentDateTime") > -1)
                {
                    switch (m.Action)
                    {
                        // регистрация приехавшего водителя
                        case "Save":
                            {
                                var p = new Dictionary<string, string>();
                                if (m.ContextObject != null)
                                {
                                    p = (Dictionary<string, string>)m.ContextObject;
                                }
                                ArrivedDriverData.AddRange(p);
                                SetArrived();
                            }
                            break;
                    }
                }
            }

            if (m.ReceiverName.IndexOf("MoldedContainerShipmentList") > -1)
            {
                switch (m.Action)
                {
                    case "ChoiceForkliftDriver":
                        {
                            BindTerminal(m.Message.ToInt());
                        }
                        break;

                    case "ChoiseTerminal":
                        try
                        {
                            KeyValuePair<string, string> context = (KeyValuePair<string, string>)m.ContextObject;
                            TerminalId = context.Key.ToInt();
                            TerminalName = context.Value;
                            ShowTerminalName();
                        }
                        catch (Exception)
                        {
                        }
                      
                        break;

                    // регистрация приехавшего водителя
                    case "SelectItem":
                        {
                            ArrivedDriverData = new Dictionary<string, string>();
                            if (m.ContextObject != null)
                            {
                                ArrivedDriverData = (Dictionary<string, string>)m.ContextObject;
                            }

                            ChoiseShipmentDateTime();
                        }
                        break;
                }
            }
        }

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
                        Path = "FROM_DATE",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = FromDate,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "FROM_TIME",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = TimeStart,
                        ControlType = "SelectBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },

                    new FormHelperField()
                    {
                        Path = "TO_DATE",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = ToDate,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "TO_TIME",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = TimeEnd,
                        ControlType = "SelectBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                };

                Form.SetFields(fields);
            }
        }

        public void SetDefaults()
        {
            TerminalLabelDefaultText = "Терминал:";
            TerminalId = 30;
            TerminalName = "ЛТ-Т2";

            ShowTerminalName();

            ShipmentDataSet = new ListDataSet();
            PositionDataSet = new ListDataSet();

            var list = new Dictionary<string, string>();
            list.Add("0", "00:00");
            list.Add("1", "01:00");
            list.Add("2", "02:00");
            list.Add("3", "03:00");
            list.Add("4", "04:00");
            list.Add("5", "05:00");
            list.Add("6", "06:00");
            list.Add("7", "07:00");
            list.Add("8", "08:00");
            list.Add("9", "09:00");
            list.Add("10", "10:00");
            list.Add("11", "11:00");
            list.Add("12", "12:00");
            list.Add("13", "13:00");
            list.Add("14", "14:00");
            list.Add("15", "15:00");
            list.Add("16", "16:00");
            list.Add("17", "17:00");
            list.Add("18", "18:00");
            list.Add("19", "19:00");
            list.Add("20", "20:00");
            list.Add("21", "21:00");
            list.Add("22", "22:00");
            list.Add("23", "23:00");
            TimeStart.SetItems(list);
            TimeEnd.SetItems(list);

            OnCurrentShift(null, null);
        }

        public void ShowTerminalName()
        {
            TeminalLabel.Content = $"{TerminalLabelDefaultText} {TerminalName}";
        }

        public void Refresh()
        {
            ShipmentGrid.LoadItems();
            DriverGrid.LoadItems();
        }

        public void ShipmentGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Description="Идентификатор отгрузки",
                        Path="SHIPMENT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата отгрузки",
                        Description="Дата отгрузки",
                        Path="SHIPMENT_DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Приезд",
                        Description="Дата приезда водителя",
                        Path="DRIVER_ARRIVE_DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Выезд",
                        Description="Дата выезда водителя",
                        Path="DRIVER_DEPART_DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер заявки",
                        Description="Номер заявки",
                        Path="ORDER_NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Загруженность, %",
                        Description="Загруженность машины, %",
                        Path="SHIPMENT_TRANSPORT_LOADED",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width2 = 5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Прогресс, %",
                        Description="Процент продукции на складе",
                        Path="PROGRESS",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width2 = 5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="На терминале",
                        Description="Машина строит на терминале",
                        Path="TERMINAL_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2 = 5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Самовывоз",
                        Description="Самовывоз",
                        Path="SELFSHIP_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2 = 5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Покупатель",
                        Description="Наименование покупателя",
                        Path="BUYER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Грузополучатель",
                        Description="Наименование грузополучателя",
                        Path="СONSIGNEE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Адрес",
                        Description="Адрес доставки",
                        Path="SHIPMENT_ADDRESS",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 22,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Машина",
                        Description="Информация о транспортном средстве",
                        Path="CAR",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Прицеп",
                        Description="Наличие прицепа",
                        Path="TRAILER_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2 = 5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Водитель",
                        Description="Наименование водителя",
                        Path="DRIVER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 22,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Телефон",
                        Description="Телефон водителя",
                        Path="DRIVER_PHONE",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Паспорт водителя",
                        Description="Паспорт водителя",
                        Path="DRIVER_PASPORT",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Перевозчик",
                        Description="Наименование перевозчика",
                        Path="CARRIER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Примечание",
                        Description="Примечание логиста",
                        Path="LOGIST_NOTE",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Комментарий",
                        Description="Комментарий к отгрузке",
                        Path="SHIPMENT_NOTE",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Заявок, шт",
                        Description="Количество заявок, шт",
                        Path="ORDER_COUNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Позиций, шт",
                        Description="Количество позиций заявок, шт",
                        Path="ORDER_POSITION_COUNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Накладная",
                        Description="Создана расходная накладная",
                        Path="INVOICE_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2 = 5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Заявки",
                        Description="Список идентификаторов заявок",
                        Path="ORDER_LIST",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Погрузчик",
                        Description="Водитель погрузчика",
                        Path="FORKLIFT_DRIVER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 10,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Ид погрузчика",
                        Description="Ид водителя погрузчика",
                        Path="FORKLIFT_DRIVER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата отгрузки",
                        Description="Дата отгрузки",
                        Path="DRIVER_ENTRY_DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2 = 14,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид водителя",
                        Description="Идентификатор водителя",
                        Path="DRIVER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Разрешена",
                        Description="Отгрузка разрешена менеджером",
                        Path="SHIPMENT_ALLOWED",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Тип доставки",
                        Description="Тип доставки",
                        Path="SHIPMENT_TYPE",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Ид заявки",
                        Description="Идентификатор крайней заявки в отгрузке",
                        Path="ORDER_ID",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Доверенность",
                        Description="Количество заявок в отгрузке, по которым нужна доверенность",
                        Path="ORDER_PROXY_COUNT",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Тендер документы",
                        Description="Флаг завершённого тендера по этой отгрузке со статусом работы с документами = 3",
                        Path="TENDER_DOC_FLAG",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Карта проезда",
                        Description="Наличие файла катры проезда",
                        Path="DRIVEWAY_FILE_FLAG",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Hidden=true,
                    },

                    new DataGridHelperColumn()
                    {
                        Header="#",
                        Path="ID",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Покупатель",
                        Path="BUYER",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Грузополучатель",
                        Path="СONSIGNEE",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Адрес доставки",
                        Path="SHIPMENTADDRESS",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Hidden = true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Статус схемы погрузки",
                        Path="LOADINGSCHEMESTATUS",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="NOORDER",
                        Path="NOORDER",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="PRODUCTION_TYPE_ID",
                        Path="PRODUCTION_TYPE_ID",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Водитель",
                        Path="DRIVER",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Hidden = true,
                    },
                    
                };
                ShipmentGrid.SetColumns(columns);
                ShipmentGrid.SetPrimaryKey("SHIPMENT_ID");
                ShipmentGrid.SearchText = ShipmentSearchBox;
                //данные грида
                ShipmentGrid.OnLoadItems = ShipmentGridLoadItems;
                ShipmentGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                ShipmentGrid.AutoUpdateInterval = 60 * 5;
                ShipmentGrid.Toolbar = ShipmentGridToolbar;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                ShipmentGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem != null)
                    {
                        if (ShipmentGrid != null && ShipmentGrid.Items != null && ShipmentGrid.Items.Count > 0)
                        {
                            if (ShipmentGrid.Items.FirstOrDefault(x => x.CheckGet("SHIPMENT_ID").ToInt() == selectedItem.CheckGet("SHIPMENT_ID").ToInt()) == null)
                            {
                                ShipmentGrid.SelectRowFirst();
                            }
                        }

                        PositionGridLoadItems();
                        UpdateActions();
                    }
                };

                ShipmentGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    // определение цветов фона строк
                    {
                        StylerTypeRef.BackgroundColor,
                        (Dictionary<string, string> row) =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            //отгрузка запрещена
                            if (row.CheckGet("SHIPMENT_ALLOWED").ToInt() == 0)
                            {
                                color = HColor.Blue;
                            }

                            // Отрузка на терминале
                            if (row.CheckGet("TERMINAL_FLAG").ToInt() == 1)
                            {
                                color = HColor.Yellow;
                            }

                            // Отрузка завершена -- Есть накладная и машина не на терминале
                            if (row.CheckGet("INVOICE_FLAG").ToInt() == 1 && row.CheckGet("TERMINAL_FLAG").ToInt() == 0)
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

                ShipmentGrid.OnFilterItems = ShipmentGridFilterItems;

                ShipmentGrid.Commands = Commander;

                ShipmentGrid.Init();
            }
        }

        public void ShipmentGridFilterItems()
        {
            PositionGrid.ClearItems();

            if (ShipmentGrid != null && ShipmentGrid.SelectedItem != null && ShipmentGrid.SelectedItem.Count > 0)
            {
                ShipmentGrid.Commands.Message = new ItemMessage() { Action = "refresh", Message = $"{ShipmentGrid.SelectedItem.CheckGet("SHIPMENT_ID")}" };
            }
        }

        public void UpdateActions()
        {
            PrintProxyDocsButton.IsEnabled = false;
            PrintDriverBootCardButton.IsEnabled = false;
            PrintRouteMapsButton.IsEnabled = false;
            PrintAllButton.IsEnabled = false;

            if (ShipmentGrid != null && ShipmentGrid.Items != null)
            {
                if (ShipmentGrid.SelectedItem != null && ShipmentGrid.SelectedItem.Count > 0)
                {
                    PrintDriverBootCardButton.IsEnabled = true;
                    PrintAllButton.IsEnabled = true;

                    if (ShipmentGrid.SelectedItem.CheckGet("ORDER_PROXY_COUNT").ToInt() > 0
                        || ShipmentGrid.SelectedItem.CheckGet("TENDER_DOC_FLAG").ToInt() > 0)
                    {
                        PrintProxyDocsButton.IsEnabled = true;
                    }

                    if (ShipmentGrid.SelectedItem.CheckGet("DRIVEWAY_FILE_FLAG").ToInt() > 0)
                    {
                        PrintRouteMapsButton.IsEnabled = true;
                    }
                }
            }
        }

        public async void ShipmentGridLoadItems()
        {
            if (Form.Validate())
            {
                string fromDateTime = $"{Form.GetValueByPath("FROM_DATE")} {TimeStart.SelectedItem.Value}:00";
                string toDateTime = $"{Form.GetValueByPath("TO_DATE")} {TimeEnd.SelectedItem.Value}:00";

                var dtFrom = fromDateTime.ToDateTime("dd.MM.yyyy HH:mm:ss");
                var dtTo = toDateTime.ToDateTime("dd.MM.yyyy HH:mm:ss");
                if (DateTime.Compare(dtFrom, dtTo) > 0)
                {
                    const string msg = "Дата начала должна быть меньше даты окончания.";
                    var d = new DialogWindow($"{msg}", "Проверка данных");
                    d.ShowDialog();
                }
                else
                {
                    var p = new Dictionary<string, string>();
                    p.Add("FROM_DATE", fromDateTime);
                    p.Add("TO_DATE", toDateTime);

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Shipments");
                    q.Request.SetParam("Object", "MoldedContainer");
                    q.Request.SetParam("Action", "ListShipment");
                    q.Request.SetParams(p);

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    ShipmentDataSet = new ListDataSet();
                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            ShipmentDataSet = ListDataSet.Create(result, "ITEMS");
                        }
                    }
                    ShipmentGrid.UpdateItems(ShipmentDataSet);
                }
            }
        }

        public void DisableControls()
        {
            ShipmentGrid.SetBusy(true);
            ShipmentGridToolbar.IsEnabled = false;
        }

        public void EnabledControls()
        {
            ShipmentGrid.SetBusy(false);
            ShipmentGridToolbar.IsEnabled = true;
        }

        public void BindTerminal(int forkliftDriverId = 0)
        {
            // Если не отмечено время прибытия водителя, то не даём ставить отгрузку на терминал
            if (string.IsNullOrEmpty(ShipmentGrid.SelectedItem.CheckGet("DRIVER_ARRIVE_DTTM")))
            {
                string msg = "Нельзя привязать к терминалу отгрузку, для которой не отмечен приезд водителя.";
                var d = new DialogWindow($"{msg}", "Привязка к терминалу", "", DialogWindowButtons.OK);
                d.ShowDialog();

                return;
            }

            if (forkliftDriverId == 0)
            {
                var forkliftDriverWindow = new ForkliftDriverList();
                forkliftDriverWindow.ParentFrame = this.ControlName;
                forkliftDriverWindow.Show();
            }
            else
            {
                DisableControls();

                var p = new Dictionary<string, string>();
                p.Add("SHIPMENTID", ShipmentGrid.SelectedItem.CheckGet("SHIPMENT_ID"));
                p.Add("TERMINALID", $"{TerminalId}");
                p.Add("PRODUCTIONTYPE", ShipmentGrid.SelectedItem.CheckGet("SHIPMENT_TYPE"));
                p.Add("APPLICATIONID", ShipmentGrid.SelectedItem.CheckGet("ORDER_ID"));
                p.Add("FORKLIFTDRIVERID", $"{forkliftDriverId}");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "Shipment");
                q.Request.SetParam("Action", "BindTerminal");

                q.Request.SetParams(p);

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
                            if (ds.Items.First().CheckGet("ID").ToInt() > 0)
                            {
                                succesfullFlag = true;
                            }
                        }
                    }

                    if (!succesfullFlag)
                    {
                        string msg = "При выполнении привязки отгрузки к терминалу произошла ошибка. Пожалуйста, сообщите о проблеме.";
                        var d = new DialogWindow($"{msg}", "Привязка к терминалу", "", DialogWindowButtons.OK);
                        d.ShowDialog();
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

                EnabledControls();
            }
        }

        public async void UnbindTerminal()
        {
            bool resume = true;
            
            {
                string underloadMessage = "";

                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("SHIPMENT_ID", ShipmentGrid.SelectedItem.CheckGet("SHIPMENT_ID"));
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "Position");
                q.Request.SetParam("Action", "ListQuantityDeviation");

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
                            foreach (var item in ds.Items)
                            {
                                if (item.CheckGet("QUANTITY_BY_CONSUMPTION").ToInt() < item.CheckGet("QUANTITY_BY_ORDER").ToInt())
                                {
                                    underloadMessage = $"{underloadMessage}{Environment.NewLine}" +
                                        $"Позиция: {item.CheckGet("PRODUCT_NAME")}{Environment.NewLine}" +
                                        $"По заявке: {item.CheckGet("QUANTITY_BY_ORDER").ToInt()} Погружено: {item.CheckGet("QUANTITY_BY_CONSUMPTION").ToInt()}{Environment.NewLine}" +
                                        $"Отклонение: {item.CheckGet("QUANTITY_BY_ORDER").ToInt() - item.CheckGet("QUANTITY_BY_CONSUMPTION").ToInt()}шт. ({item.CheckGet("QUANTITY_PERCENTAGE_DEVIATION").ToDouble()}%){Environment.NewLine}";
                                }
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(underloadMessage))
                {
                    underloadMessage = $"Внимание, остались недогруженные позиции! Вы хотите продолжить?{Environment.NewLine}" +
                        $"{underloadMessage}";
                    var d = new DialogWindow($"{underloadMessage}", "Отвязка от терминала", "", DialogWindowButtons.NoYes);
                    if (d.ShowDialog() != true)
                    {
                        resume = false;
                    }
                }
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID", $"{TerminalId}");
                    p.CheckAdd("IDTS", ShipmentGrid.SelectedItem.CheckGet("SHIPMENT_ID"));
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "Shipment");
                q.Request.SetParam("Action", "UnbindTerminal");

                q.Request.SetParams(p);

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
                            if (ds.Items.First().CheckGet("ID").ToInt() > 0)
                            {
                                succesfullFlag = true;
                            }
                        }
                    }

                    if (!succesfullFlag)
                    {
                        string msg = "При отвязывании отгрузки к терминалу произошла ошибка. Пожалуйста, сообщите о проблеме.";
                        var d = new DialogWindow($"{msg}", "Привязка к терминалу", "", DialogWindowButtons.OK);
                        d.ShowDialog();
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

        public void BindDriver()
        {
            var bindDriver = new BindDriver();
            bindDriver.Id = ShipmentGrid.SelectedItem["SHIPMENT_ID"].ToInt();

            if (DriverGrid != null && DriverGrid.SelectedItem != null && DriverGrid.SelectedItem.Count > 0)
            {
                bindDriver.DriverLogId = DriverGrid.SelectedItem["ID"].ToInt();
                bindDriver.DriverId = DriverGrid.SelectedItem["DRIVERID"].ToInt();
            }

            bindDriver.ShipmentsDS = ShipmentDataSet;
            bindDriver.DriversDS = DriverDataSet;
            bindDriver.Edit();

            Refresh();
        }

        /// <summary>
        /// отвязать водителя
        /// </summary>
        public async void UnbindDriver()
        {
            bool resume = true;
            int shipmentId = 0;

            if (resume)
            {
                if (ShipmentGrid.SelectedItem != null)
                {
                    shipmentId = ShipmentGrid.SelectedItem.CheckGet("ID").ToInt();
                    if (shipmentId == 0)
                    {
                        resume = false;
                    }
                }
            }

            if (resume)
            {
                var msg = "";
                msg = $"{msg}Отвязать водителя от отгрузки?\n";

                if (!string.IsNullOrEmpty(ShipmentGrid.SelectedItem.CheckGet("BUYER")))
                {
                    msg = $"{msg}{ShipmentGrid.SelectedItem.CheckGet("BUYER")}\n";
                }

                if (!string.IsNullOrEmpty(ShipmentGrid.SelectedItem.CheckGet("DRIVER")))
                {
                    msg = $"{msg}{ShipmentGrid.SelectedItem.CheckGet("DRIVER")}\n";
                }

                var d = new DialogWindow($"{msg}", "Отвязка водителя", "", DialogWindowButtons.NoYes);
                if (d.ShowDialog() != true)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("ID", shipmentId.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "Shipment");
                q.Request.SetParam("Action", "UnbindDriver");

                q.Request.SetParams(p);

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
                            if (ds.Items.First().CheckGet("ID").ToInt() > 0)
                            {
                                succesfullFlag = true;
                            }
                        }
                    }

                    if (succesfullFlag)
                    {
                        Refresh();
                    }
                    else
                    {
                        string msg = "При отвязывании водителя от отгрузки произошла ошибка. Пожалуйста, сообщите о проблеме.";
                        var d = new DialogWindow($"{msg}", "Отвязывание водителя", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        public void ShowLoadingScheme()
        {
            if (ShipmentGrid.SelectedItem != null)
            {
                int id = ShipmentGrid.SelectedItem.CheckGet("ID").ToInt();
                var shipment = new Shipment(id);
                shipment.ShowLoadingScheme(ShipmentGrid.SelectedItem);
            }
        }

        public void EditComment()
        {
            if (ShipmentGrid.SelectedItem != null)
            {
                var id = ShipmentGrid.SelectedItem.CheckGet("ID").ToInt();
                var h = new ShipmentComment();
                h.Edit(id);
            }
        }

        public void CreateAutoShipment()
        {
            if (ShipmentGrid.SelectedItem != null)
            {
                var view = new ShipmentAuto
                {
                    IdTs = ShipmentGrid.SelectedItem.CheckGet("ID"),
                };
                view.Edit();

                Refresh();
            }
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
                        Header="ИД заявки",
                        Description="Идентификатор заявки",
                        Path="ORDER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Description="Идентификатор позиции заявки",
                        Path="POSITION_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Продукция",
                        Description="Наименование продукции",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 30,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Description="Артикул продукции",
                        Path="PRODUCT_CODE",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="В заявке, шт",
                        Description="Количество продукции в заявке, шт",
                        Path="POSITION_QUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Поддонов, шт",
                        Description="Рассчитанное количество поддонов для позиции заявки",
                        Path="PALLET_COUNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="На складе, шт",
                        Description="Общее количество продукции на складе",
                        Path="QUANTITY_IN_STOCK",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 11,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Под отгрузку, шт",
                        Description="Количество продукции на складе для позиции заявки",
                        Path="QUANTITY_BY_POSITION",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 13,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Отгружено, шт",
                        Description="Количество отгруженной продукции по позиции заявки",
                        Path="CONSUMPRION_BY_POSITION",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Отклонение, %",
                        Description="Процентное отклонение отгруженного количества к количеству по позиции заявки",
                        Path="QUANTITY_PERCENTAGE_DEVIATION",
                        ColumnType=ColumnTypeRef.Double,
                        Width2 = 10,
                        Format="N2",
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // Если позиция отгружена
                                    if (row.CheckGet("STATUS_ID").ToInt() == 0)
                                    {
                                        // процентное отклонение отгруженного количества к количеству по заявке превышает допустимые значения
                                        if (!PercentageDeviation.CheckPercentageDeviation(row.CheckGet("POSITION_QUANTITY").ToInt(), row["QUANTITY_PERCENTAGE_DEVIATION"].ToDouble()))
                                        {
                                            if (row["QUANTITY_PERCENTAGE_DEVIATION"].ToDouble() >= 0)
                                            {
                                                color = HColor.Red;
                                            }
                                            else
                                            {
                                                color = HColor.Yellow;
                                            }
                                        }
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
                        Header="Статус",
                        Description="Статус позиции заявки",
                        Path="STATUS_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Цена",
                        Description="Цена без НДС",
                        Path="PRICE",
                        ColumnType=ColumnTypeRef.Double,
                        Width2 = 7,
                        Format="N2",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Примечание",
                        Description="Примечание складу",
                        Path="NOTE_STOCK",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 19,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Комментарий",
                        Description="Комментарий по позиции заявки",
                        Path="NOTE",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 19,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Адрес",
                        Description="Адрес доставки",
                        Path="SHIPMENT_ADDRESS",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 58,
                    },

                    new DataGridHelperColumn
                    {
                        Header="ИД отгрузки",
                        Description="Идентификатор отгрузки",
                        Path="SHIPMENT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД продукции",
                        Description="Идентификатор продукции",
                        Path="PRODUCT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД статуса позиции заявки",
                        Description="Идентификатор статуса позиции заявки",
                        Path="STATUS_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                        Hidden=true,
                    },
                };
                PositionGrid.SetColumns(columns);
                PositionGrid.SetPrimaryKey("POSITION_ID");
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

                PositionGrid.Commands = Commander;

                PositionGrid.Init();
            }
        }

        public async void PositionGridLoadItems()
        {
            var p = new Dictionary<string, string>();
            p.Add("SHIPMENT_ID", $"{ShipmentGrid.SelectedItem.CheckGet("SHIPMENT_ID")}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "ListShipmentPosition");
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

        public void DriverGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Description="Идентификатор записи",
                        Path="ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид водителя",
                        Description="Идентификатор водителя",
                        Path="DRIVERID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Приезд",
                        Description="Дата приезда водителя",
                        Path="ARRIVEDATE",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Въезд",
                        Description="Дата въезда водителя под отгрузку",
                        Path="ENTRYDATE",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Водитель",
                        Description="Наименование водителя",
                        Path="DRIVERNAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 30,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Телефон",
                        Description="Телефон водителя",
                        Path="DRIVERPHONE",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Машина",
                        Description="Данные транспортного средства",
                        Path="CAR",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 24,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Покупатель",
                        Description="Наименование покупателя",
                        Path="BUYER",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 30,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Терминал",
                        Description="Номер терминала",
                        Path="TERMINALNUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Паспорт",
                        Description="Паспортные данные водителя",
                        Path="PASSPORT",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 17,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Отгрузка",
                        Description="Дата отгрузки",
                        Path="SHIPMENTDATE",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy",
                        Width2 = 6,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид отгрузки",
                        Description="Идентификатор отгрузки",
                        Path="TRANSPORTID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 6,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Привязан",
                        Description="Привязан к транспорту",
                        Path="TRANSPORTASSIGNED",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 6,
                        Hidden = true,
                    },
                    
                    new DataGridHelperColumn
                    {
                        Header="Марка",
                        Description="Марка автомобиля",
                        Path="CARMARK",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 6,
                        Hidden = true,
                    },                   
                };
                DriverGrid.SetColumns(columns);
                DriverGrid.SetPrimaryKey("DRIVERID");
                DriverGrid.SearchText = DriverSearchBox;
                //данные грида
                DriverGrid.OnLoadItems = DriverGridLoadItems;
                DriverGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                DriverGrid.AutoUpdateInterval = 60 * 5;
                DriverGrid.Toolbar = DriverGridToolbar;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                DriverGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem != null)
                    {
                        if (DriverGrid != null && DriverGrid.Items != null && DriverGrid.Items.Count > 0)
                        {
                            if (DriverGrid.Items.FirstOrDefault(x => x.CheckGet("DRIVERID").ToInt() == selectedItem.CheckGet("DRIVERID").ToInt()) == null)
                            {
                                DriverGrid.SelectRowFirst();
                            }
                        }
                    }
                };

                DriverGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    {
                        StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";


                            //водитель въехал
                            if (row.ContainsKey("ENTRYDATE"))
                            {
                                if(!string.IsNullOrEmpty(row["ENTRYDATE"]))
                                {
                                    color = HColor.Green;
                                }
                            }

                            //отгрузка не привязана
                            if(row.CheckGet("TRANSPORTID").ToInt()==0)
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

                DriverGrid.OnFilterItems = DriverGridFilterItems;

                DriverGrid.Commands = Commander;

                DriverGrid.Init();
            }
        }

        public void DriverGridFilterItems()
        {
            if (DriverGrid != null && DriverGrid.SelectedItem != null && DriverGrid.SelectedItem.Count > 0)
            {
                DriverGrid.Commands.Message = new ItemMessage() { Action = "refresh", Message = $"{DriverGrid.SelectedItem.CheckGet("DRIVERID")}" };
            }
        }

        public async void DriverGridLoadItems()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "ListDriver");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            DriverDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    DriverDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            DriverGrid.UpdateItems(DriverDataSet);
        }

        public void AddDriver()
        {
            ArrivedDriverData = new Dictionary<string, string>();

            var i = new AddDriver();
            Central.WM.SetLayer("add");

            try
            {
                ((DriverListExpected)Central.WM.TabItems.FirstOrDefault(x => x.Key == "AddDriver_ExpectedDrivers").Value.Content).ParentFrameType = DriverListExpected.ParentFrameTypeDefault.MoldedContainerShipmentList;
                ((DriverListExpected)Central.WM.TabItems.FirstOrDefault(x => x.Key == "AddDriver_ExpectedDrivers").Value.Content).Types.SetSelectedItemByKey("7");
                ((DriverListExpected)Central.WM.TabItems.FirstOrDefault(x => x.Key == "AddDriver_ExpectedDrivers").Value.Content).LoadItems();
                ((DriverListAll)Central.WM.TabItems.FirstOrDefault(x => x.Key == "AddDriver_AllDrivers").Value.Content).ParentFrameType = DriverListAll.ParentFrameTypeDefault.MoldedContainerShipmentList;
            }
            catch (Exception)
            {
            }
        }

        public void ChoiseShipmentDateTime()
        {
            if (ArrivedDriverData != null && ArrivedDriverData.Count > 0)
            {
                bool checkDateTime = false;

                if (!string.IsNullOrEmpty(ArrivedDriverData.CheckGet("SHIPMENTID")))
                {
                    if (
                        ArrivedDriverData.CheckGet("UNSHIPPED").ToInt() == 1
                        || ArrivedDriverData.CheckGet("LATE").ToInt() == 1
                    )
                    {
                        checkDateTime = true;
                    }
                }

                if (checkDateTime)
                {
                    var i = new ShipmentDateTime();
                    i.ShipmentType = ArrivedDriverData.CheckGet("SHIPMENTTYPE").ToInt();
                    i.ShipmentId = ArrivedDriverData.CheckGet("SHIPMENTID").ToInt();
                    i.ReceiverName = "MoldedContainerShipmentListSelectShipmentDateTime";
                    i.Edit();
                }
                else
                {
                    SetArrived();
                }
            }
        }

        /// <summary>
        /// отпарвка запроса "отметить водителя как приехавшего"
        /// </summary>
        /// <param name="p"></param>
        public async void SetArrived()
        {
            var resume = false;

            if (ArrivedDriverData != null && ArrivedDriverData.Count > 0)
            {
                resume = true;
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();

                var expected = false;
                var shipmentId = ArrivedDriverData.CheckGet("SHIPMENTID").ToInt();
                if (shipmentId != 0)
                {
                    expected = true;
                    p.CheckAdd("SHIPMENT_DATE", ArrivedDriverData.CheckGet("SHIPMENT_DATE"));
                    p.CheckAdd("SHIPMENT_TIME", ArrivedDriverData.CheckGet("SHIPMENT_TIME"));
                    p.CheckAdd("SET_DATETIME", "1");
                }
                else
                {
                    p.CheckAdd("TYPE_ORDER", "4");
                }

                p.CheckAdd("ID", ArrivedDriverData.CheckGet("ID"));
                p.CheckAdd("EXPECTED", expected.ToInt().ToString());
                p.CheckAdd("SHIPMENT_ID", shipmentId.ToString());
                p.CheckAdd("FACTORY_ID", "1");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "TransportDriver");
                q.Request.SetParam("Action", "SetArrived");

                q.Request.SetParams(p);

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
                            if (ds.Items.First().CheckGet("ID").ToInt() > 0)
                            {
                                succesfullFlag = true;
                            }
                        }
                    }

                    if (succesfullFlag)
                    {
                        Refresh();
                        Central.WM.RemoveTab($"AddDriver");
                    }
                    else
                    {
                        string msg = "При добавления водителя в список приехавших произошла ошибка. Пожалуйста, сообщите о проблеме.";
                        var d = new DialogWindow($"{msg}", "Приезд водителя", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            ArrivedDriverData = new Dictionary<string, string>();
        }

        /// <summary>
        /// Открытие вкладки редактирования данных водителя
        /// </summary>
        public void EditDriver()
        {
            var id = 0;
            var driverLogId = 0;
            if (DriverGrid.SelectedItem != null)
            {
                if (DriverGrid.SelectedItem.ContainsKey("DRIVERID"))
                {
                    id = DriverGrid.SelectedItem["DRIVERID"].ToInt();
                }

                if (DriverGrid.SelectedItem.ContainsKey("ID"))
                {
                    driverLogId = DriverGrid.SelectedItem["ID"].ToInt();
                }
            }

            if (id != 0)
            {
                var driver = new Driver
                {
                    Id = id,
                    DriverLogId = driverLogId
                };
                driver.ReturnTabName = "MoldedContainerShipmentList";
                driver.Edit(id);
            }
        }

        /// <summary>
        /// Удаление водителя из списка приехавших
        /// </summary>
        public async void DeleteDriver()
        {
            var resume = true;

            if (resume)
            {
                var msg = "";
                msg = $"{msg}Удалить водителя из списка приехавших?\n";
                if (DriverGrid.SelectedItem.ContainsKey("DRIVERNAME"))
                {
                    msg = $"{msg}{DriverGrid.SelectedItem["DRIVERNAME"]}\n";
                }

                var d = new DialogWindow($"{msg}", "Приезд водителя", "", DialogWindowButtons.NoYes);
                if (d.ShowDialog() != true)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("ID", DriverGrid.SelectedItem.CheckGet("ID"));
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "TransportDriver");
                q.Request.SetParam("Action", "DeleteArrived");

                q.Request.SetParams(p);

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
                            if (ds.Items.First().CheckGet("ID").ToInt() > 0)
                            {
                                succesfullFlag = true;
                            }
                        }
                    }

                    if (succesfullFlag)
                    {
                        Refresh();
                    }
                    else
                    {
                        string msg = "При удалении водителя из списка приехавших произошла ошибка. Пожалуйста, сообщите о проблеме.";
                        var d = new DialogWindow($"{msg}", "Приезд водителя", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// Вывод печатной формы доверенности
        /// </summary>
        private void PrintProxy()
        {
            if (ShipmentGrid.SelectedItem != null)
            {
                var id = ShipmentGrid.SelectedItem.CheckGet("SHIPMENT_ID").ToInt();
                var reporter = new ShipmentReport(id);
                reporter.PrintProxy();
            }
        }

        /// <summary>
        /// Вывод печатной формы загрузочной карты водителя
        /// </summary>
        private void PrintBootcard()
        {
            if (ShipmentGrid.SelectedItem != null)
            {
                var id = ShipmentGrid.SelectedItem.CheckGet("SHIPMENT_ID").ToInt();
                var reporter = new ShipmentReport(id);
                reporter.PrintBootcard();
            }
        }

        /// <summary>
        /// Вывод печатной формы задания на отгрузку
        /// </summary>
        private void PrintShipmenttask()
        {
            if (ShipmentGrid.SelectedItem != null)
            {
                var id = ShipmentGrid.SelectedItem.CheckGet("SHIPMENT_ID").ToInt();
                var reporter = new ShipmentReport(id);
                reporter.PrintShipmenttask();
            }
        }

        /// <summary>
        /// Вывод печатной формы карты проезда
        /// </summary>
        private void PrintRoutemap()
        {
            if (ShipmentGrid.SelectedItem != null)
            {
                var id = ShipmentGrid.SelectedItem.CheckGet("SHIPMENT_ID").ToInt();
                var reporter = new ShipmentReport(id);
                reporter.PrintRoutemap();
            }
        }

        /// <summary>
        /// печать всех документов
        /// </summary>
        private void PrintAll()
        {
            if (ShipmentGrid.SelectedItem != null)
            {
                var id = ShipmentGrid.SelectedItem.CheckGet("SHIPMENT_ID").ToInt();
                var reporter = new ShipmentReport(id);
                reporter.PrintProxy();
                reporter.PrintBootcard();
                reporter.PrintShipmenttask();
                reporter.PrintRoutemap();
            }
        }

        /// <summary>
        /// Открытие вкладки накладных по выбранной отгрузке
        /// </summary>
        public void DocumentList()
        {
            if (ShipmentGrid.SelectedItem != null && ShipmentGrid.SelectedItem.Count > 0)
            {
                var shipmentDocumentList = new ShipmentDocumentList();
                shipmentDocumentList.RoleName = this.RoleName;
                shipmentDocumentList.TransportId = ShipmentGrid.SelectedItem.CheckGet("SHIPMENT_ID").ToInt();
                shipmentDocumentList.SelectedShipmentItem = ShipmentGrid.SelectedItem;
                shipmentDocumentList.Show();
            }
        }

        /// <summary>
        /// Установка настроек для принтера
        /// </summary>
        public void SetPrintSettings()
        {
            var i = new PrintingInterface();
        }

        public void ChoiseShipmentPlace()
        {
            var terminalWindow = new TerminalList();
            terminalWindow.ParentFrame = this.ControlName;
            terminalWindow.Show();
        }

        private void OnCurrentShift(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now;
            if (date.Hour >= 20 || date.Hour < 8)
            {
                TimeStart.SetSelectedItemByKey("20");
                TimeEnd.SetSelectedItemByKey("8");

                if (date.Hour >= 20)
                {
                    FromDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
                    ToDate.Text = DateTime.Now.AddDays(1).ToString("dd.MM.yyyy");
                }
                else
                {
                    FromDate.Text = DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy");
                    ToDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
                }
            }
            else
            {
                FromDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
                ToDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
                TimeStart.SetSelectedItemByKey("8");
                TimeEnd.SetSelectedItemByKey("20");
            }

            Refresh();
        }

        private void OnPrevShift(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now.AddHours(-12);
            if (date.Hour >= 20 || date.Hour < 8)
            {
                TimeStart.SetSelectedItemByKey("20");
                TimeEnd.SetSelectedItemByKey("8");

                if (date.Hour >= 20)
                {
                    FromDate.Text = date.ToString("dd.MM.yyyy");
                    ToDate.Text = date.AddDays(1).ToString("dd.MM.yyyy");
                }
                else
                {
                    FromDate.Text = date.AddDays(-1).ToString("dd.MM.yyyy");
                    ToDate.Text = date.ToString("dd.MM.yyyy");
                }
            }
            else
            {
                FromDate.Text = date.ToString("dd.MM.yyyy");
                ToDate.Text = date.ToString("dd.MM.yyyy");
                TimeStart.SetSelectedItemByKey("8");
                TimeEnd.SetSelectedItemByKey("20");
            }

            Refresh();
        }

        private void OnCurrentDay(object sender, RoutedEventArgs e)
        {
            FromDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
            ToDate.Text = DateTime.Now.AddDays(1).ToString("dd.MM.yyyy");
            TimeStart.SetSelectedItemByKey("0");
            TimeEnd.SetSelectedItemByKey("0");

            Refresh();
        }

        private void OnPrevDay(object sender, RoutedEventArgs e)
        {
            FromDate.Text = DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy");
            ToDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
            TimeStart.SetSelectedItemByKey("0");
            TimeEnd.SetSelectedItemByKey("0");

            Refresh();
        }

        private void OnCurrentWeek(object sender, RoutedEventArgs e)
        {
            DayOfWeek day = DateTime.Now.DayOfWeek;
            int days = day - DayOfWeek.Monday;
            DateTime date = DateTime.Now.AddDays(-days);
            FromDate.Text = date.Date.ToString("dd.MM.yyyy");
            ToDate.Text = date.Date.AddDays(7).ToString("dd.MM.yyyy");
            TimeStart.SetSelectedItemByKey("0");
            TimeEnd.SetSelectedItemByKey("0");

            Refresh();
        }

        private void OnPrevWeek(object sender, RoutedEventArgs e)
        {
            DayOfWeek day = DateTime.Now.DayOfWeek;
            int days = day - DayOfWeek.Monday;
            DateTime date = DateTime.Now.AddDays(-days).AddDays(-7);
            FromDate.Text = date.Date.ToString("dd.MM.yyyy");
            ToDate.Text = date.Date.AddDays(7).ToString("dd.MM.yyyy");
            TimeStart.SetSelectedItemByKey("0");
            TimeEnd.SetSelectedItemByKey("0");

            Refresh();
        }

        private void OnCurrentMonth(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now;
            FromDate.Text = new DateTime(date.Year, date.Month, 1).ToString("dd.MM.yyyy");
            ToDate.Text = new DateTime(date.Year, date.Month, 1).AddMonths(1).ToString("dd.MM.yyyy");
            TimeStart.SetSelectedItemByKey("0");
            TimeEnd.SetSelectedItemByKey("0");

            Refresh();
        }

        private void OnPrevMonth(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now.AddMonths(-1);
            FromDate.Text = new DateTime(date.Year, date.Month, 1).ToString("dd.MM.yyyy");
            ToDate.Text = new DateTime(date.Year, date.Month, 1).AddMonths(1).ToString("dd.MM.yyyy");
            TimeStart.SetSelectedItemByKey("0");
            TimeEnd.SetSelectedItemByKey("0");

            Refresh();
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }


        /// <summary>
        /// Обработчик нажатия на кнопку печати загрузочной карты водителя
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrintDriverBootCardButton_Click(object sender, RoutedEventArgs e)
        {
            PrintBootcard();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку печати карты проезда
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrintRouteMapsButton_Click(object sender, RoutedEventArgs e)
        {
            PrintRoutemap();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку печати доверенности
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrintProxyDocsButton_Click(object sender, RoutedEventArgs e)
        {
            PrintProxy();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку печати всех документов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrintAllButton_Click(object sender, RoutedEventArgs e)
        {
            PrintAll();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку печати задания на отгрузку
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrintShipmentOrderBootCardButton_Click(object sender, RoutedEventArgs e)
        {
            PrintShipmenttask();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsBurgerMenu.IsOpen = true;
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings();
        }

        private void BurgerChoiseShipmentPlace_Click(object sender, RoutedEventArgs e)
        {
            ChoiseShipmentPlace();
        }
    }
}
