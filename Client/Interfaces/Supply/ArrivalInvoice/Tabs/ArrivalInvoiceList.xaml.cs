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

namespace Client.Interfaces.Supply
{
    /// <summary>
    /// Список приходных накладных
    /// </summary>
    public partial class ArrivalInvoiceList : ControlBase
    {
        public ArrivalInvoiceList()
        {
            ControlTitle = "Приходные накладные";
            RoleName = "[erp]arrival_invoice";
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
                ArrivalInvoiceGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                ArrivalInvoiceGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                ArrivalInvoiceGrid.ItemsAutoUpdate = true;
                ArrivalInvoiceGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                ArrivalInvoiceGrid.ItemsAutoUpdate = false;
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
                        ArrivalInvoiceGrid.ItemsExportExcel();
                    },
                });
            }

            Commander.SetCurrentGridName("ArrivalInvoiceGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "add_arrival_invoice",
                    Title = "Добавить",
                    Group = "arrival_invoice_grid_default",
                    Enabled = true,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = NewDocumentButton,
                    ButtonName = "NewDocumentButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        AddArrivalInvoice();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "edit_arrival_invoice",
                    Title = "Открыть",
                    Group = "arrival_invoice_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = EditDocumentButton,
                    ButtonName = "EditDocumentButton",
                    HotKey = "Return|DoubleCLick",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        EditArrivalInvoice();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ArrivalInvoiceGrid != null && ArrivalInvoiceGrid.SelectedItem != null && ArrivalInvoiceGrid.SelectedItem.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(ArrivalInvoiceGrid.SelectedItem.CheckGet("INVOICE_ID")))
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "delete_arrival_invoice",
                    Title = "Удалить",
                    Group = "arrival_invoice_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = DeleteDocumentButton,
                    ButtonName = "DeleteDocumentButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        DeleteArrivalInvoice();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ArrivalInvoiceGrid != null && ArrivalInvoiceGrid.SelectedItem != null && ArrivalInvoiceGrid.SelectedItem.Count > 0)
                        {
                            // Если накладная в отчётном периоде
                            if (ArrivalInvoiceGrid.SelectedItem.CheckGet("INVOICE_DATE").ToDateTime() >= ReportingPeriod.ToDateTime())
                            {
                                // Если нет записей в таблицах, которые ссылаются на эту накладную прихода
                                if (string.IsNullOrEmpty(ArrivalInvoiceGrid.SelectedItem.CheckGet("INPA_ID"))
                                    && string.IsNullOrEmpty(ArrivalInvoiceGrid.SelectedItem.CheckGet("PADP_ID"))
                                    && string.IsNullOrEmpty(ArrivalInvoiceGrid.SelectedItem.CheckGet("ID_SCRAP"))
                                    && string.IsNullOrEmpty(ArrivalInvoiceGrid.SelectedItem.CheckGet("ID_SCRAP_PZ"))
                                    && ArrivalInvoiceGrid.SelectedItem.CheckGet("INCOMING_COUNT").ToInt() == 0)
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
        /// Датасет с данными грида приходных накладных
        /// </summary>
        public ListDataSet ArrivalInvoiceGridDataSet { get; set; }

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
                        Path = "INVOICE_DATE_FROM",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = InvoiceDateFrom,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "INVOICE_DATE_TO",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = InvoiceDateTo,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                };

                Form.SetFields(fields);
            }
        }

        private void SetDefaults()
        {
            ArrivalInvoiceGridDataSet = new ListDataSet();

            Form.SetDefaults();

            Form.SetValueByPath("INVOICE_DATE_FROM", DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy"));
            Form.SetValueByPath("INVOICE_DATE_TO", DateTime.Now.ToString("dd.MM.yyyy"));

            var factorySelectBoxItems = new Dictionary<string, string>();
            factorySelectBoxItems.Add("1", "Л-ПАК ЛИПЕЦК");
            factorySelectBoxItems.Add("2", "Л-ПАК КАШИРА");
            FactorySelectBox.SetItems(factorySelectBoxItems);
            FactorySelectBox.SetSelectedItemByKey("1");

            var сustomerSelectBoxItems = new Dictionary<string, string>();
            сustomerSelectBoxItems.Add("-1", "Все покупатели");
            сustomerSelectBoxItems.Add("1", "ТД Л-Пак");
            сustomerSelectBoxItems.Add("2", "Л-ПАК");
            сustomerSelectBoxItems.Add("23", "БумПак");
            сustomerSelectBoxItems.Add("427", "Л-ПАК Кашира");
            CustomerSelectBox.SetItems(сustomerSelectBoxItems);
            CustomerSelectBox.SetSelectedItemByKey("-1");

            var productCategorySelectBox = new Dictionary<string, string>();
            productCategorySelectBox.Add("-1", "Все типы продукции");
            productCategorySelectBox.Add("1", "Сырьё");
            productCategorySelectBox.Add("2", "Готовая продукция");
            productCategorySelectBox.Add("3", "Оснастка");
            ProductCategorySelectBox.SetItems(productCategorySelectBox);
            ProductCategorySelectBox.SetSelectedItemByKey("-1");
        }

        public void SetFormDateInReportingPeriod()
        {
            if (!string.IsNullOrEmpty(ReportingPeriod))
            {
                Form.SetValueByPath("INVOICE_DATE_FROM", ReportingPeriod);
                Form.SetValueByPath("INVOICE_DATE_TO", DateTime.Now.ToString("dd.MM.yyyy"));

                Refresh();
            }
        }

        private void ArrivalInvoiceGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид накладной",
                        Description = "Идентификатор приходной накладной",
                        Path="INVOICE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата оприходования СФ",
                        Description = "Дата оприходования счет-фактуры",
                        Path="DATAOPRSF",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2=9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер накладной",
                        Description = "Внешний номер накладной (с внешнего документа)",
                        Path="NAME_NAKL",
                        ColumnType=ColumnTypeRef.String,
                        Width2=14,
                    },                   
                    new DataGridHelperColumn
                    {
                        Header="Поставщик",
                        Description = "Имя поставщика  (если оприходуется сырьё и прочие материалы)",
                        Path="SELLER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=28,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Позиций, шт.",
                        Description = "Количество позиций в приходе, шт.",
                        Path="INCOMING_COUNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество, шт.",
                        Description = "Суммарное количество продукции в приходе",
                        Path="INCOMING_QUANTITY",
                        ColumnType=ColumnTypeRef.Double,
                        Format = "N0",
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вес, кг.",
                        Description = "Суммарный вес продукции в приходе",
                        Path="INCOMING_WEIGHT",
                        ColumnType=ColumnTypeRef.Double,
                        Format = "N0",
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вес по накладной, кг.",
                        Description = "Вес (всех рулонов бумаги) по внешней приходной накладной, кг.",
                        Path="WEIGHT_NETTO_DOC",
                        ColumnType=ColumnTypeRef.Double,
                        Format = "N0",
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Условие поставки",
                        Description = "Условие поставки сырья и пр. материалов",
                        Path="DELIVERY_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ транспорта",
                        Description = "№ машины или вагона в котором привезли сырье или макулатуру",
                        Path="TRANSPORT_NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ отгрузки",
                        Description = "Номер отгрузки",
                        Path="SHIPMENT_NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Примечание",
                        Description = "Примечание",
                        Path="NOTE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=23,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Покупатель",
                        Description = "Имя юридической сущности компании куда оприходуется данная продукция",
                        Path="CUSTOMER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=11,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Договор",
                        Description = "Договор по накладной",
                        Path="CONTRACT_FULL_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=18,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИНН поставщика",
                        Description = "ИНН\\КПП поставщика",
                        Path="SELLER_INN",
                        ColumnType=ColumnTypeRef.String,
                        Width2=14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер СФ",
                        Description = "Номер счет - фактуры с внешнего документа",
                        Path="NAMESF",
                        ColumnType=ColumnTypeRef.String,
                        Width2=9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата СФ",
                        Description = "Дата счёт-фактуры",
                        Path="DATASF",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2=9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата накладной",
                        Description = "Дата накладной прихода",
                        Path="INVOICE_DATE",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2=9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата поступления СФ",
                        Description = "Дата поступления счет-фактуры",
                        Path="DATAPSF",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2=9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Продукция по накладной",
                        Description = "Список наименований продукции по накладной",
                        Path="PRODUCT_NAME_LIST",
                        ColumnType=ColumnTypeRef.String,
                        Width2=16,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Тип договора",
                        Description = "Тип договора по накладной",
                        Path="CONTRACT_TYPE",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=27,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование типа договора",
                        Description = "Наименование типа договора по накладной",
                        Path="CONTRACT_TYPE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=27,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Категории продукции",
                        Description = "Список категорий продукции по накладной",
                        Doc="1 -- Сырьё; 2 -- ГП; 3 -- Оснастка.",
                        Path="CATEGORY_LIST",
                        ColumnType=ColumnTypeRef.String,
                        Width2=18,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид поставщика",
                        Description = "Идентификатор поставщика (если оприходуется сырьё и прочие материалы)",
                        Path="SELLER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=12,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид покупателя",
                        Description = "Идентификатор юридической сущности компании куда оприходуется данная продукция",
                        Path="CUSTOMER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=12,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид договора",
                        Description = "Идентификатор договора с контрагентом (поставщиком)",
                        Path="CONTRACT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=12,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="IDMO",
                        Path="IDMO",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=12,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="IDMO1",
                        Path="IDMO1",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=12,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="IDMO2",
                        Path="IDMO2",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=12,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="IDMO3",
                        Path="IDMO3",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=12,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Условие поставки сырья и пр. материалов",
                        Description = "Условие поставки сырья и пр. материалов: 1 - доставка, 2 - компенсация транспорта, 3 - самовывоз.",
                        Path="DELIVERY_NUM",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=12,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="INPA_ID",
                        Path="INPA_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=12,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="PADP_ID",
                        Path="PADP_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=12,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ID_SCRAP",
                        Path="ID_SCRAP",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=12,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ID_SCRAP_PZ",
                        Path="ID_SCRAP_PZ",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=12,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид площадки",
                        Path="FACTORY_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                };
                ArrivalInvoiceGrid.SetColumns(columns);
                ArrivalInvoiceGrid.SearchText = SearchText;
                ArrivalInvoiceGrid.OnLoadItems = ArrivalInvoiceGridLoadItems;
                ArrivalInvoiceGrid.SetPrimaryKey("INVOICE_ID");
                ArrivalInvoiceGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                ArrivalInvoiceGrid.AutoUpdateInterval = 5 * 60;
                ArrivalInvoiceGrid.Toolbar = GridToolbar;

                ArrivalInvoiceGrid.OnFilterItems = () =>
                {
                    if (ArrivalInvoiceGrid.Items != null && ArrivalInvoiceGrid.Items.Count > 0)
                    {
                        // Фильтрация по площадке
                        if (FactorySelectBox.SelectedItem.Key != null)
                        {
                            var key = FactorySelectBox.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            items.AddRange(ArrivalInvoiceGrid.Items.Where(x => x.CheckGet("FACTORY_ID").ToInt() == key));

                            ArrivalInvoiceGrid.Items = items;
                        }

                        // Фильтрация накладных по покупателю
                        // -1 -- Все покупатели
                        if (CustomerSelectBox.SelectedItem.Key != null)
                        {
                            var key = CustomerSelectBox.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            switch (key)
                            {
                                // Все покупатели
                                case -1:
                                    items = ArrivalInvoiceGrid.Items;
                                    break;

                                default:
                                    items.AddRange(ArrivalInvoiceGrid.Items.Where(x => x.CheckGet("CUSTOMER_ID").ToInt() == key));
                                    break;
                            }

                            ArrivalInvoiceGrid.Items = items;
                        }

                        if (ProductCategorySelectBox.SelectedItem.Key != null)
                        {
                            var key = ProductCategorySelectBox.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            switch (key)
                            {
                                // Сырьё
                                case 1:
                                    items.AddRange(ArrivalInvoiceGrid.Items.Where(x => 
                                           x.CheckGet("CATEGORY_LIST").Contains("[2]")
                                        || x.CheckGet("CATEGORY_LIST").Contains("[3]")
                                        || x.CheckGet("CATEGORY_LIST").Contains("[17]")
                                        || x.CheckGet("CATEGORY_LIST").Contains("[121]")
                                    ));
                                    break;

                                // Готовая продукция
                                case 2:
                                    items.AddRange(ArrivalInvoiceGrid.Items.Where(x =>
                                           x.CheckGet("CATEGORY_LIST").Contains("[5]")
                                        || x.CheckGet("CATEGORY_LIST").Contains("[6]")
                                    ));
                                    break;

                                // Оснастка
                                case 3:
                                    items.AddRange(ArrivalInvoiceGrid.Items.Where(x =>
                                           x.CheckGet("CATEGORY_LIST").Contains("[14]")
                                        || x.CheckGet("CATEGORY_LIST").Contains("[15]")
                                        || x.CheckGet("CATEGORY_LIST").Contains("[12]")
                                        || x.CheckGet("CATEGORY_LIST").Contains("[10]")
                                    ));
                                    break;

                                // Все виды продукции
                                case -1:
                                default:
                                    items = ArrivalInvoiceGrid.Items;
                                    break;
                            }

                            ArrivalInvoiceGrid.Items = items;
                        }
                    }
                };

                ArrivalInvoiceGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    // определение цветов фона строк
                    {
                        StylerTypeRef.BackgroundColor,
                        (Dictionary<string, string> row) =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            // Нет позиций номенклатуры
                            if (row.CheckGet("INCOMING_COUNT").ToInt() == 0)
                            {
                                color = HColor.Blue;
                            }

                            // Есть не проведённые позиции прихода
                            if (row.CheckGet("MIN_PROVEDENO").ToInt() == 0 && row.CheckGet("INCOMING_COUNT").ToInt() > 0)
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
                };
               
                ArrivalInvoiceGrid.Commands = Commander;
                ArrivalInvoiceGrid.UseProgressSplashAuto = false;
                ArrivalInvoiceGrid.Init();
            }
        }

        public async void ArrivalInvoiceGridLoadItems()
        {
            bool resume = true;

            var f = Form.GetValueByPath("INVOICE_DATE_FROM").ToDateTime();
            var t = Form.GetValueByPath("INVOICE_DATE_TO").ToDateTime();

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
                p.Add("INVOICE_DATE_FROM", Form.GetValueByPath("INVOICE_DATE_FROM"));
                p.Add("INVOICE_DATE_TO", Form.GetValueByPath("INVOICE_DATE_TO"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Supply");
                q.Request.SetParam("Object", "ArrivalInvoice");
                q.Request.SetParam("Action", "List");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                ArrivalInvoiceGridDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        ArrivalInvoiceGridDataSet = ListDataSet.Create(result, "ITEMS");

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
                ArrivalInvoiceGrid.UpdateItems(ArrivalInvoiceGridDataSet);
            }
        }

        private void SetPrintSettings()
        {
            var i = new PrintingInterface();
        }

        public void Refresh()
        {
            ArrivalInvoiceGrid.LoadItems();
        }

        public void AddArrivalInvoice()
        {
            var i = new ArrivalInvoice();
            i.ReportingPeriod = this.ReportingPeriod;
            i.ParentFrame = this.FrameName;
            i.Show();
        }

        public void EditArrivalInvoice() 
        {
            if (ArrivalInvoiceGrid.SelectedItem != null && ArrivalInvoiceGrid.SelectedItem.CheckGet("INVOICE_ID").ToInt() > 0)
            {
                var i = new ArrivalInvoice();
                i.ReportingPeriod = this.ReportingPeriod;
                i.ParentFrame = this.FrameName;
                i.InvoiceId = ArrivalInvoiceGrid.SelectedItem.CheckGet("INVOICE_ID").ToInt();
                i.Show();
            }
            else
            {
                string msg = $"Не выбрана накладная для просмотра.";
                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        public void DeleteArrivalInvoice()
        {
            if (ArrivalInvoiceGrid.SelectedItem != null && ArrivalInvoiceGrid.SelectedItem.CheckGet("INVOICE_ID").ToInt() > 0)
            {
                {
                    string msg = $"Удалить накладную прихода {ArrivalInvoiceGrid.SelectedItem.CheckGet("INVOICE_ID").ToInt()} № {ArrivalInvoiceGrid.SelectedItem.CheckGet("NAME_NAKL")} ?";
                    var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.NoYes);
                    if (d.ShowDialog() == false)
                    {
                        return;
                    }
                }

                var p = new Dictionary<string, string>();
                p.Add("INVOICE_ID", ArrivalInvoiceGrid.SelectedItem.CheckGet("INVOICE_ID"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Supply");
                q.Request.SetParam("Object", "ArrivalInvoice");
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
                            if (!string.IsNullOrEmpty(ds.Items[0].CheckGet("INVOICE_ID")))
                            {
                                succesfullFlag = true;
                            }
                        }
                    }

                    if (succesfullFlag)
                    {
                        Refresh();

                        string msg = $"Успешное удаление накладной.";
                        var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                    else
                    {
                        string msg = $"Ошибка удаления накладной. Пожалуйста, сообщите о проблеме.";
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
                string msg = $"Не выбрана накладная для удаления.";
                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings();
        }

        private void CustomerSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ArrivalInvoiceGrid.UpdateItems();
        }

        private void ProductCategorySelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ArrivalInvoiceGrid.UpdateItems();
        }

        private void ReportingPeriodMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SetFormDateInReportingPeriod();
        }

        private void FactorySelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ArrivalInvoiceGrid.UpdateItems();
        }
    }
}
