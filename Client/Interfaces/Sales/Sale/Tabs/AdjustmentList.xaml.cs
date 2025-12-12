using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Service.Printing;
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
using static Client.Interfaces.Sales.SaleList;

namespace Client.Interfaces.Sales
{
    /// <summary>
    /// Список корректировочных накладных
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class AdjustmentList : ControlBase
    {
        public AdjustmentList()
        {
            ControlTitle = "Список корректировочных накладных";
            RoleName = "[erp]sales_manager";
            DocumentationUrl = "/doc/l-pack-erp/";
            InitializeComponent();

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    switch (m.Action)
                    {
                        case "SetSignotory":
                            SetSignotory(m);
                            break;

                        case "Find":
                            FindAdjustment(m);
                            break;

                        default:
                            Commander.ProcessCommand(m.Action, m);
                            break;
                    }
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
                AdjustmentSaleGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                AdjustmentSaleGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                AdjustmentSaleGrid.ItemsAutoUpdate = true;
                AdjustmentSaleGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                AdjustmentSaleGrid.ItemsAutoUpdate = false;
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
                Commander.Add(new CommandItem()
                {
                    Name = "export_to_excel",
                    Group = "main",
                    Enabled = true,
                    Title = "В Excel",
                    Description = "Выгрузить данные в Excel файл",
                    ButtonUse = true,
                    ButtonName = "ExportToExcelButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        AdjustmentSaleGrid.ItemsExportExcel();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "settings",
                    Group = "main",
                    Enabled = true,
                    Title = "Настройки",
                    Description = "Открыть настройки",
                    ButtonUse = true,
                    ButtonName = "SettingsButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        BurgerMenu.IsOpen = true;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "scan_adjustment",
                    Group = "main",
                    Enabled = true,
                    Title = "Сканирование",
                    Description = "Сканирование документов",
                    ButtonUse = true,
                    ButtonControl = ScanAdjustmentButton,
                    ButtonName = "ScanAdjustmentButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        ScanAdjustment();
                    },
                });
            }

            Commander.SetCurrentGridName("AdjustmentSaleGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "edit_adjustment",
                    Title = "Открыть",
                    Group = "adjustment_sale_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = EditAdjustmentButton,
                    ButtonName = "EditAdjustmentButton",
                    HotKey = "DoubleCLick",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        EditAdjustment();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (AdjustmentSaleGrid != null && AdjustmentSaleGrid.SelectedItem != null && AdjustmentSaleGrid.SelectedItem.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(AdjustmentSaleGrid.SelectedItem.CheckGet("ADJUSTMENT_ID")))
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "delete_adjustment",
                    Title = "Удалить",
                    Group = "adjustment_sale_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = DeleteAdjustmentButton,
                    ButtonName = "DeleteAdjustmentButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        DeleteAdjustment();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (AdjustmentSaleGrid != null && AdjustmentSaleGrid.SelectedItem != null && AdjustmentSaleGrid.SelectedItem.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(AdjustmentSaleGrid.SelectedItem.CheckGet("ADJUSTMENT_ID")))
                            {
                                if (AdjustmentSaleGrid.SelectedItem.CheckGet("CONSUMPTION_POSITION_COUNT").ToInt() == 0
                                    && AdjustmentSaleGrid.SelectedItem.CheckGet("INCOMING_POSITION_COUNT").ToInt() == 0)
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
                    Name = "UpdateReturnUKDFlag1",
                    Title = "Отметить возврат УКД",
                    Group = "adjustment_sale_grid_return_document",
                    Enabled = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        UpdateReturnDocumentFlag(1, ReturningDocumentType.UniversalAdjustmentDocument);
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (AdjustmentSaleGrid != null && AdjustmentSaleGrid.SelectedItem != null && AdjustmentSaleGrid.SelectedItem.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(AdjustmentSaleGrid.SelectedItem.CheckGet("ADJUSTMENT_ID")))
                            {
                                if (AdjustmentSaleGrid.SelectedItem.CheckGet("RETURN_UAD").ToInt() == 0)
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
                    Name = "UpdateReturnUKDFlag0",
                    Title = "Снять отметку возврата УКД",
                    Group = "adjustment_sale_grid_return_document",
                    Enabled = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        UpdateReturnDocumentFlag(0, ReturningDocumentType.UniversalAdjustmentDocument);
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (AdjustmentSaleGrid != null && AdjustmentSaleGrid.SelectedItem != null && AdjustmentSaleGrid.SelectedItem.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(AdjustmentSaleGrid.SelectedItem.CheckGet("ADJUSTMENT_ID")))
                            {
                                if (AdjustmentSaleGrid.SelectedItem.CheckGet("RETURN_UAD").ToInt() > 0)
                                {
                                    result = true;
                                }
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
        /// Датасет с данными грида корректировочных накладных
        /// </summary>
        public ListDataSet AdjustmentSaleGridDataSet { get; set; }

        /// <summary>
        /// Идентификатор сотрудника - подписанта
        /// </summary>
        public int SignotoryEmployeeId { get; set; }

        /// <summary>
        /// Полное имя сотрудника - подписанта
        /// </summary>
        public string SignotoryEmployeeName { get; set; }

        /// <summary>
        /// Дата, с которой разрешено редактирование движения товаров, продукции и денег
        /// </summary>
        public string ReportingPeriod { get; set; }

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
                        Path="SEARCH",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=SearchText,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path = "ADJUSTMENT_DATE_FROM",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = AdjustmentDateFrom,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "ADJUSTMENT_DATE_TO",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = AdjustmentDateTo,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                };

                Form.SetFields(fields);
            }
        }

        private void AdjustmentSaleGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Description = "Идентификатор корректировочной накладной",
                        Path="ADJUSTMENT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=4,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата",
                        Description = "Дата корректировочной накладной",
                        Path="ADJUSTMENT_DT",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy",
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер",
                        Description = "Номер корректировочной накладной",
                        Path="ADJUSTMENT_NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Покупатель",
                        Description = "Наименование покупателя по накладной, по которой создана корректировка",
                        Path="BUYER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=32,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Всего позиций",
                        Description = "Количество позиций в накладной",
                        Path="ALL_POSITION_COUNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Корректировок позиций",
                        Description = "Количество скорректированных позиий в накладной",
                        Path="DIFFERENT_POSITION_COUNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Сумма корректировок",
                        Description = "Изменение суммарной стоимости относительно исходной накладной",
                        Path="DIFFERENT_SUMMARY",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Виртуальный приход",
                        Description = "Виртуальный приход позиций по корректировочной накладной",
                        Path="INCOMING_VIRTUAL_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="УКД возвращён",
                        Description = "УКД возвращён от покупателя",
                        Path="RETURN_UAD",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид накладной",
                        Description = "Идентификатор исходной накладной, по которой создана корректировка",
                        Path="BASE_INVOICE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                        Group="Исходная накладная",
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ СФ накладной",
                        Description = "Номер СФ исходной накладной, по которой создана корректировка",
                        Path="BASE_INVOICE_NAME_SF",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                        Group="Исходная накладная",
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ ТТН накладной исходной накладной, по которой создана корректировка",
                        Description = "Номер ТТН",
                        Path="BASE_INVOICE_NAME_STH",
                        ColumnType=ColumnTypeRef.String,
                        Width2=11,
                        Group="Исходная накладная",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата накладной",
                        Description = "Дата исходной накладной, по которой создана корректировка",
                        Path="BASE_INVOICE_DT",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy",
                        Width2=9,
                        Group="Исходная накладная",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид расходной накладной",
                        Description = "Идентификатор расходной накладной, созданной по этой корректировке",
                        Path="CONSUMPTION_INVOICE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                        Group="Расходная накладная",
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ СФ расходной накладной",
                        Description = "Номер СФ расходной накладной, созданной по этой корректировке",
                        Path="CONSUMPTION_INVOICE_NAME_SF",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                        Group="Расходная накладная",
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ ТТН расходной накладной",
                        Description = "Номер ТТН расходной накладной, созданной по этой корректировке",
                        Path="CONSUMPTION_INVOICE_NAME_STH",
                        ColumnType=ColumnTypeRef.String,
                        Width2=11,
                        Group="Расходная накладная",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата расходной накладной",
                        Description = "Дата расходной накладной, созданной по этой корректировке",
                        Path="CONSUMPTION_INVOICE_DT",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy",
                        Width2=9,
                        Group="Расходная накладная",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид приходной накладной",
                        Description = "Идентификатор приходной накладной, созданной по этой корректировке",
                        Path="INCOMING_INVOICE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                        Group="Приходная накладная",
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ СФ приходной накладной",
                        Description = "Номер СФ приходной накладной, созданной по этой корректировке",
                        Path="INCOMING_INVOICE_NAMESF",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                        Group="Приходная накладная",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер приходной накладной",
                        Description = "Номер приходной накладной, созданной по этой корректировке",
                        Path="INCOMING_INVOICE_NAME_NAKL",
                        ColumnType=ColumnTypeRef.String,
                        Width2=11,
                        Group="Приходная накладная",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата приходной накладной",
                        Description = "Дата приходной накладной, созданной по этой корректировке",
                        Path="INCOMING_INVOICE_DT",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy",
                        Width2=9,
                        Group="Приходная накладная",
                    },

                    new DataGridHelperColumn
                    {
                        Header="Ид покупателя",
                        Description = "Идентификатор покупателя по накладной, по которой создана корректировка",
                        Path="BUYER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид площадки",
                        Description = "Идентификатор площадки реализации исходной накладной",
                        Path="FACTORY_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Расходов",
                        Description = "Количество проведённых расходов по позициям корректировки",
                        Path="CONSUMPTION_POSITION_COUNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Приходов",
                        Description = "Количество проведённых приходов по позициям корерктировки",
                        Path="INCOMING_POSITION_COUNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                        Visible=false,
                    },
                };
                AdjustmentSaleGrid.SetColumns(columns);
                AdjustmentSaleGrid.SearchText = SearchText;
                AdjustmentSaleGrid.OnLoadItems = AdjustmentSaleGridLoadItems;
                AdjustmentSaleGrid.SetPrimaryKey("ADJUSTMENT_ID");
                AdjustmentSaleGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                AdjustmentSaleGrid.AutoUpdateInterval = 5 * 60;
                AdjustmentSaleGrid.Toolbar = GridToolbar;

                AdjustmentSaleGrid.OnFilterItems = () =>
                {
                    if (AdjustmentSaleGrid.Items != null && AdjustmentSaleGrid.Items.Count > 0)
                    {
                        // Фильтрация по площадке
                        if (FactorySelectBox.SelectedItem.Key != null)
                        {
                            var key = FactorySelectBox.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            items.AddRange(AdjustmentSaleGrid.Items.Where(x => x.CheckGet("FACTORY_ID").ToInt() == key));

                            AdjustmentSaleGrid.Items = items;
                        }
                    }
                };

                AdjustmentSaleGrid.Commands = Commander;
                AdjustmentSaleGrid.UseProgressSplashAuto = false;
                AdjustmentSaleGrid.Init();
            }
        }

        public async void AdjustmentSaleGridLoadItems()
        {
            DisableControls();

            bool resume = true;

            var f = Form.GetValueByPath("ADJUSTMENT_DATE_FROM").ToDateTime();
            var t = Form.GetValueByPath("ADJUSTMENT_DATE_TO").ToDateTime();

            if (DateTime.Compare(f, t) > 0)
            {
                string msg = "Дата начала должна быть меньше даты окончания.";
                var d = new DialogWindow($"{msg}", this.ControlTitle);
                d.ShowDialog();
                resume = false;
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();
                p.Add("ADJUSTMENT_DATE_FROM", Form.GetValueByPath("ADJUSTMENT_DATE_FROM"));
                p.Add("ADJUSTMENT_DATE_TO", Form.GetValueByPath("ADJUSTMENT_DATE_TO"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "Adjustment");
                q.Request.SetParam("Action", "List");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                AdjustmentSaleGridDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        AdjustmentSaleGridDataSet = ListDataSet.Create(result, "ITEMS");

                        var periodDataSet = ListDataSet.Create(result, "REPORTING_PERIOD");
                        if (periodDataSet != null && periodDataSet.Items != null && periodDataSet.Items.Count > 0)
                        {
                            ReportingPeriod = periodDataSet.Items.First().CheckGet("PARAM_VALUE").ToDateTime().ToString("dd.MM.yyyy");
                            ReportingPeriodLabel.Content = $"Отчётный период с: {ReportingPeriod}";
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
                AdjustmentSaleGrid.UpdateItems(AdjustmentSaleGridDataSet);
            }

            EnableControls();
        }

        private void SetDefaults()
        {
            AdjustmentSaleGridDataSet = new ListDataSet();

            Form.SetDefaults();

            Form.SetValueByPath("ADJUSTMENT_DATE_FROM", DateTime.Now.AddDays(-DateTime.Now.Day + 1).ToString("dd.MM.yyyy"));
            Form.SetValueByPath("ADJUSTMENT_DATE_TO", DateTime.Now.ToString("dd.MM.yyyy"));

            var factorySelectBoxItems = new Dictionary<string, string>();
            factorySelectBoxItems.Add("1", "Л-ПАК ЛИПЕЦК");
            factorySelectBoxItems.Add("2", "Л-ПАК КАШИРА");
            FactorySelectBox.SetItems(factorySelectBoxItems);
            FactorySelectBox.SetSelectedItemByKey("1");

            GetDefaultSignotory();
        }

        private void SetSignotory(ItemMessage m)
        {
            if (m.ContextObject != null)
            {
                try
                {
                    KeyValuePair<string, string> context = (KeyValuePair<string, string>)m.ContextObject;
                    SignotoryEmployeeId = context.Key.ToInt();
                    SignotoryEmployeeName = context.Value;
                    SignotoryLabel.Content = $"Подписант: {SignotoryEmployeeName}";

                    var signotoryEmployeeIdParameter = Central.User.UserParameterList.FirstOrDefault(x => x.Interface == this.GetType().Name && x.Name == "SignotoryEmployeeId");
                    var signotoryEmployeeNameParameter = Central.User.UserParameterList.FirstOrDefault(x => x.Interface == this.GetType().Name && x.Name == "SignotoryEmployeeName");
                    if (signotoryEmployeeIdParameter != null)
                    {
                        signotoryEmployeeIdParameter.Value = $"{SignotoryEmployeeId}";
                    }
                    else
                    {
                        signotoryEmployeeIdParameter = new UserParameter(this.GetType().Name, "SignotoryEmployeeId", $"{SignotoryEmployeeId}", "Идентификатор сотрудника подписанта");
                        Central.User.UserParameterList.Add(signotoryEmployeeIdParameter);
                    }

                    if (signotoryEmployeeNameParameter != null)
                    {
                        signotoryEmployeeNameParameter.Value = SignotoryEmployeeName;
                    }
                    else
                    {
                        signotoryEmployeeNameParameter = new UserParameter(this.GetType().Name, "SignotoryEmployeeName", SignotoryEmployeeName, "ФИО сотрудника подписанта");
                        Central.User.UserParameterList.Add(signotoryEmployeeNameParameter);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private void FindAdjustment(ItemMessage m)
        {
            if (m.ContextObject != null)
            {
                try
                {
                    Dictionary<string, string> context = (Dictionary<string, string>)m.ContextObject;
                    if (context != null && context.Count > 0)
                    {
                        string invoiceNumber = context.CheckGet("ADJUSTMENT_ID");
                        string invoiceDate = context.CheckGet("ADJUSTMENT_DATE");
                        if (!string.IsNullOrEmpty(invoiceNumber) && !string.IsNullOrEmpty(invoiceDate))
                        {
                            SearchText.Text = invoiceNumber;
                            Form.SetValueByPath("ADJUSTMENT_DATE_FROM", invoiceDate);
                            Form.SetValueByPath("ADJUSTMENT_DATE_TO", invoiceDate);

                            Refresh();
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public void GetDefaultSignotory()
        {
            var signotoryEmployeeIdParameter = Central.User.UserParameterList.FirstOrDefault(x => x.Interface == this.GetType().Name && x.Name == "SignotoryEmployeeId");
            var signotoryEmployeeNameParameter = Central.User.UserParameterList.FirstOrDefault(x => x.Interface == this.GetType().Name && x.Name == "SignotoryEmployeeName");
            if (signotoryEmployeeIdParameter != null && signotoryEmployeeNameParameter != null)
            {
                SignotoryEmployeeId = signotoryEmployeeIdParameter.Value.ToInt();
                SignotoryEmployeeName = signotoryEmployeeNameParameter.Value;
                SignotoryLabel.Content = $"Подписант: {SignotoryEmployeeName}";
            }
            else
            {
                SignotoryEmployeeId = Central.User.EmployeeId;
                SignotoryEmployeeName = Central.User.Name;
                SignotoryLabel.Content = $"Подписант: {SignotoryEmployeeName}";

                if (signotoryEmployeeIdParameter != null)
                {
                    signotoryEmployeeIdParameter.Value = $"{SignotoryEmployeeId}";
                }
                else
                {
                    signotoryEmployeeIdParameter = new UserParameter(this.GetType().Name, "SignotoryEmployeeId", $"{SignotoryEmployeeId}", "Идентификатор сотрудника подписанта");
                    Central.User.UserParameterList.Add(signotoryEmployeeIdParameter);
                }

                if (signotoryEmployeeNameParameter != null)
                {
                    signotoryEmployeeNameParameter.Value = SignotoryEmployeeName;
                }
                else
                {
                    signotoryEmployeeNameParameter = new UserParameter(this.GetType().Name, "SignotoryEmployeeName", SignotoryEmployeeName, "ФИО сотрудника подписанта");
                    Central.User.UserParameterList.Add(signotoryEmployeeNameParameter);
                }
            }
        }

        public void Refresh()
        {
            AdjustmentSaleGrid.LoadItems();
        }

        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
        }

        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
        }

        public void ScanAdjustment()
        {
            var w = new ScanDocument();
            w.ParentFrame = FrameName;
            w.Show();
        }

        public void EditAdjustment()
        {
            var window = new Adjustment();
            window.InvoiceId = AdjustmentSaleGrid.SelectedItem.CheckGet("BASE_INVOICE_ID").ToInt();
            window.FactoryId = AdjustmentSaleGrid.SelectedItem.CheckGet("FACTORY_ID").ToInt();
            window.ReportingPeriod = ReportingPeriod;
            window.SignotoryEmployeeId = SignotoryEmployeeId;
            window.SignotoryEmployeeName = SignotoryEmployeeName;
            window.AdjusmentId = AdjustmentSaleGrid.SelectedItem.CheckGet("ADJUSTMENT_ID").ToInt();
            window.Show();
        }

        public void DeleteAdjustment()
        {
            var d = new DialogWindow($"Удалить корректировку {AdjustmentSaleGrid.SelectedItem.CheckGet("ADJUSTMENT_NUMBER")} от {AdjustmentSaleGrid.SelectedItem.CheckGet("ADJUSTMENT_DT")}", this.ControlTitle, "", DialogWindowButtons.NoYes);
            if (d.ShowDialog() == true)
            {
                DisableControls();

                var p = new Dictionary<string, string>();
                p.Add("ADJUSTMENT_ID", AdjustmentSaleGrid.SelectedItem.CheckGet("ADJUSTMENT_ID"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "Adjustment");
                q.Request.SetParam("Action", "Delete");
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
                            if (ds.Items[0].CheckGet("ADJUSTMENT_ID").ToInt() > 0)
                            {
                                succesfullFlag = true;
                            }
                        }
                    }

                    if (succesfullFlag)
                    {
                        Refresh();

                        d = new DialogWindow($"Успешное удаление корректировки", this.ControlTitle);
                        d.ShowDialog();
                    }
                    else
                    {
                        d = new DialogWindow($"Ошибка удаления корректировки. Пожалуйста, сообщите о проблеме.", this.ControlTitle);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }

                EnableControls();
            }
        }

        public void UpdateReturnDocumentFlag(int returnFlag, ReturningDocumentType documentType)
        {
            DisableControls();

            var p = new Dictionary<string, string>();
            p.Add("INVOICE_ID", AdjustmentSaleGrid.SelectedItem.CheckGet("BASE_INVOICE_ID"));
            p.Add("RETURN_FLAG", $"{returnFlag}");
            p.Add("DOCUMENT_TYPE", $"{(int)documentType}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "Sale");
            q.Request.SetParam("Action", "UpdateReturnDocumentFlag");
            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            AdjustmentSaleGridDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                bool succesfullFlag = false;

                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        if (ds.Items[0].CheckGet("INVOICE_ID").ToInt() > 0)
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
                    string msg = $"При изменении отметки возврата документа по накладной произошла ошибка. Пожалуйста, сообщите о проблеме.";
                    var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                q.ProcessError();
            }

            EnableControls();
        }

        private void SetPrintSettings()
        {
            var i = new PrintingInterface();
        }

        public void ChoiseSignotory()
        {
            var w = new ChoiseSignotory();
            w.ParentFrame = FrameName;
            w.Show();
        }

        public void SetFormDateInReportingPeriod()
        {
            if (!string.IsNullOrEmpty(ReportingPeriod))
            {
                Form.SetValueByPath("ADJUSTMENT_DATE_FROM", ReportingPeriod);
                Form.SetValueByPath("ADJUSTMENT_DATE_TO", DateTime.Now.ToString("dd.MM.yyyy"));

                Refresh();
            }
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings();
        }

        private void BurgerChoiseSignotory_Click(object sender, RoutedEventArgs e)
        {
            ChoiseSignotory();
        }

        private void FactorySelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AdjustmentSaleGrid.UpdateItems();
        }

        private void ReportingPeriodMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SetFormDateInReportingPeriod();
        }
    }
}
