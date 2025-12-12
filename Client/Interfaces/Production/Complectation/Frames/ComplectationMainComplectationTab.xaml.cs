using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Stock;
using GalaSoft.MvvmLight.Messaging;
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

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Логика взаимодействия для ComplectationMainComplectationTab.xaml
    /// </summary>
    public partial class ComplectationMainComplectationTab : UserControl
    {
        public ComplectationMainComplectationTab(string parentFrame, Dictionary<string, string> selectedProductItem, List<Dictionary<string, string>> oldPalletList)
        {
            FrameName = "ComplectationMainComplectationTab";
            SelectedProductItem = selectedProductItem;
            ProductId = selectedProductItem.CheckGet("ID2").ToInt();
            OrderId = selectedProductItem.CheckGet("IDORDERDATES").ToInt();
            _ParentFrame = parentFrame;

            InitializeComponent();
            Loaded += (object sender, RoutedEventArgs e) =>
            {
                Central.WM.SetActive(FrameName);
                Central.WM.SelectedTab = FrameName;
            };

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            SetComplectationType();
            ProcessPermissions();

            InitForm();
            SetDefaults();

            OldPalletList = oldPalletList;

            OldPalletGridInit();
            NewPalletGridInit();
        }

        /// <summary>
        /// Наименование таба, который вызвал этот таб
        /// </summary>
        private string _ParentFrame { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Техническое имя фрейма
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// Заголовок таба
        /// </summary>
        public string FrameTitle { get; set; }

        /// <summary>
        /// Идентификатор продукции (t.id2)
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Идентификатор заявки по выбранной продукции (orderdates.idorderdates)
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Выбранная запись в гриде списываемых поддонов
        /// </summary>
        public Dictionary<string, string> OldPalletGridSelectedItem { get; set; }

        /// <summary>
        /// Выбранная запись в гриде новых поддонов
        /// </summary>
        public Dictionary<string, string> NewPalletGridSelectedItem { get; set; }

        /// <summary>
        /// Датасет с данными по выбранной продукции
        /// </summary>
        public ListDataSet ProductDataSet { get; set; }

        /// <summary>
        /// Список выбранных поддонов, из которых будут комплектоваться новые
        /// </summary>
        public List<Dictionary<string, string>> OldPalletList { get; set; }

        /// <summary>
        /// Датасет создаваемых в процессе компелктации поддонов
        /// </summary>
        public ListDataSet NewPalletDataSet { get; set; }

        /// <summary>
        /// Флаг того, что есть права мастера
        /// (используется для комплектации из воздуха)
        /// </summary>
        public bool MasterFlag { get; set; }

        /// <summary>
        /// Данные по выбранной для комплектации продукции
        /// </summary>
        public Dictionary<string, string> SelectedProductItem { get; set; }

        /// <summary>
        /// Тип комплектации
        /// </summary>
        public enum ComplectationTypeRef
        {
            /// <summary>
            /// ГА
            /// </summary>
            CorrugatingMachine = 1,
            /// <summary>
            /// ПР
            /// </summary>
            ProcessingMachine = 2,
            /// <summary>
            /// СГП
            /// </summary>
            Stock = 3,
            /// <summary>
            /// ЛТ
            /// </summary>
            MoldedContainer = 4,
            /// <summary>
            /// ГА Кашира
            /// </summary>
            CorrugatingMachineKsh = 5,
            /// <summary>
            /// ПР Кашира
            /// </summary>
            ProcessingMachineKsh = 6, 
        }

        /// <summary>
        /// Выбранный тип комплектации
        /// </summary>
        public ComplectationTypeRef ComplectationType { get; set; }

        /// <summary>
        /// Инициализация грида списываемых поддонов
        /// </summary>
        public void OldPalletGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "Поддон",
                    Path = "PALLET",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 90,
                    MaxWidth = 90,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество, шт",
                    Path = "KOL",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 127,
                    MaxWidth = 165,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество на остатке",
                    Doc = "По этому приходу",
                    Path = "QUANTITY_BY_CONSIGNMENT",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 55,
                    MaxWidth = 55,
                    Hidden=true,
                },

                new DataGridHelperColumn
                {
                    Header = "Начало кондиционирования",
                    Path = "CONDITION_DTTM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Hidden = true,
                },

                new DataGridHelperColumn
                {
                    Header = " ",
                    Path = "_",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 5,
                    MaxWidth = 800,
                },
            };
            OldPalletGrid.SetColumns(columns);
            OldPalletGrid.SetSorting("PALLET");

            OldPalletGrid.SetMode(1);
            OldPalletGrid.Grid.RowHeight = 35;
            OldPalletGrid.Grid.FontSize = 20;

            OldPalletGrid.Menu = new Dictionary<string, DataGridContextMenuItem>();

            OldPalletGrid.OnSelectItem = selectedItem =>
            {
                OldPalletGridSelectedItem = selectedItem;
                UpdateButtons();
            };

            OldPalletGrid.OnLoadItems = OldPalletGridLoadItems;

            OldPalletGrid.Init();
            OldPalletGrid.Run();
            OldPalletGrid.Focus();
        }

        /// <summary>
        /// Получение данных для грида списываемых поддонов
        /// </summary>
        public void OldPalletGridLoadItems()
        {
            OldPalletGrid.Items = OldPalletList;
            if (OldPalletGrid.Items != null && OldPalletGrid.Items.Count > 0)
            {
                OldPalletGrid.SetSelectToFirstRow();
            }

            UpdateConsumptionQuantity();
            UpdateButtons();
        }

        /// <summary>
        /// Обновляем значение текстбокса расхода
        /// </summary>
        public void UpdateConsumptionQuantity()
        {
            if (OldPalletGrid != null && OldPalletGrid.Items != null && OldPalletGrid.Items.Count > 0)
            {
                int consumptionQuantity = OldPalletGrid.Items.Sum(x => x.CheckGet("KOL").ToInt());
                ConsumptionQuantityTextBox.Text = consumptionQuantity.ToString();
            }
            else
            {
                ConsumptionQuantityTextBox.Text = "0";
            }
        }

        /// <summary>
        /// Инициализация грида новых поддонов
        /// </summary>
        public void NewPalletGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "Поддон",
                    Path = "PODDON_NUMBER",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 90,
                    MaxWidth = 90,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество, шт",
                    Path = "QTY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 127,
                    MaxWidth = 165,
                },

                new DataGridHelperColumn
                {
                    Header = " ",
                    Path = "_",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 5,
                    MaxWidth = 800,
                },
            };
            NewPalletGrid.SetColumns(columns);
            NewPalletGrid.SetSorting("PODDON_NUMBER");

            NewPalletGrid.SetMode(1);
            NewPalletGrid.Grid.RowHeight = 35;
            NewPalletGrid.Grid.FontSize = 20;

            NewPalletGrid.Menu = new Dictionary<string, DataGridContextMenuItem>();

            NewPalletGrid.OnSelectItem = selectedItem =>
            {
                NewPalletGridSelectedItem = selectedItem;
                UpdateButtons();
            };

            NewPalletGrid.OnLoadItems = NewPalletGridLoadItems;

            NewPalletGrid.Init();
            NewPalletGrid.Run();
        }

        /// <summary>
        /// Получение данных для грида новых поддонов
        /// </summary>
        public void NewPalletGridLoadItems()
        {
            NewPalletGrid.UpdateItems(NewPalletDataSet);
            UpdateIncomingQuantity();
            UpdateButtons();
        }

        /// <summary>
        /// Обновление значения текстбокса прихода
        /// </summary>
        public void UpdateIncomingQuantity()
        {
            if (NewPalletGrid != null && NewPalletGrid.Items != null && NewPalletGrid.Items.Count > 0)
            {
                int incomingQuantity = NewPalletGrid.Items.Sum(x => x.CheckGet("QTY").ToInt());
                IncomingQuantityTextBox.Text = incomingQuantity.ToString();
            }
            else
            {
                IncomingQuantityTextBox.Text = "0";
            }
        }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void InitForm()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="PRODUCT_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductNameTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TOTAL_QUANTITY_ON_PALLET",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TotalQuantityOnPalletsTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="QUANTITY_ON_PALLET",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=QuantityOnPalletTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="QUANTITY_BY_ORDER",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=QuantityByOrderTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="QUANTITY_CREATED_PRODUCT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=QuantityCreatedProductTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="COUNT_PRODUCTION_TASK",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CountProductionTaskTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PALLET_COUNT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CountPalletsTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SHIPPING_DATETIME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ShippingDateTimeTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NEXT_PRODUCTION_TASK",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=NextProductionTaskTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                new FormHelperField()
                {
                    Path="QUANTITY_IN_PACK",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=QuantityInPackTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PALLET_HEIGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PalletHeigthTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PALLET",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=DefaultPalletTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="LAYING_SCHEME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=DefaultLayingSchemeTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="BRAND",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CardboardBrandTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PROFIL",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CardboardProfilTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_ID2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ProductId2TextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_CATEGORY",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductCategoryTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DIMENSIONS",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductDimensionsTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CARDBOARD_THIKNES",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CardboardThiknesTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                new FormHelperField()
                {
                    Path="PRODUCT_IDK1",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PATHTK",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            // Колбек стандартной валидации
            Form.OnValidate = (valid, message) =>
            {
                //CheckBlankLength();
            };

            Form.SetFields(fields);
            //Form.ToolbarControl = FormToolbar;
        }

        /// <summary>
        /// Получаем данные по выбранной продукции
        /// </summary>
        public void ProductLoadItems()
        {
            var p = new Dictionary<string, string>();
            p.Add("PRODUCT_ID", ProductId.ToString());
            p.Add("ORDER_ID", OrderId.ToString());

            // ячейка
            string stock = "";
            // место
            string placeNumber = "";

            stock = SelectedProductItem.CheckGet("SKLAD");
            placeNumber = "0";

            p.Add("SKLAD", stock);
            p.Add("NUM_PLACE", placeNumber);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Complectation");
            q.Request.SetParam("Object", "Product");
            q.Request.SetParam("Action", "GetById2");

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
                    ProductDataSet = ds;

                    if (OldPalletList != null && OldPalletList.Count > 0 && ProductDataSet != null && ProductDataSet.Items != null && ProductDataSet.Items.Count > 0)
                    {
                        if (OldPalletList.First().CheckGet("THIKNES").ToDouble() > 0)
                        {
                            ProductDataSet.Items.First().CheckAdd("CARDBOARD_THIKNES", OldPalletList.First().CheckGet("THIKNES"));
                        }
                    }

                    Form.SetValues(ProductDataSet);
                    UpdateButtons();
                }
            }
            else 
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            OldPalletGridSelectedItem = new Dictionary<string, string>();
            NewPalletGridSelectedItem = new Dictionary<string, string>();
            ProductDataSet = new ListDataSet();
            OldPalletList = new List<Dictionary<string, string>>();
            NewPalletDataSet = new ListDataSet();

            SplashControl.Visible = false;

            UpdateButtons();
        }

        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Show()
        {
            ProductLoadItems();
            UpdateVisual();

            string title = "Комплектация";
            switch (ComplectationType)
            {
                case ComplectationTypeRef.CorrugatingMachine:
                    title = $"{title} ГА";
                    break;

                case ComplectationTypeRef.ProcessingMachine:
                    title = $"{title} ПР";
                    break;

                case ComplectationTypeRef.Stock:
                    title = $"{title} СГП";
                    break;

                case ComplectationTypeRef.MoldedContainer:
                    title = $"{title} ЛТ";
                    break;

                case ComplectationTypeRef.CorrugatingMachineKsh:
                    title = $"{title} ГА КШ";
                    break;

                case ComplectationTypeRef.ProcessingMachineKsh:
                    title = $"{title} ПР КШ";
                    break;

                default:
                    break;
            }

            this.FrameTitle = title;

            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 1;

            Central.WM.Show(FrameName, title, true, "add", this);
        }

        /// <summary>
        /// Устанавливаем значение выбранной комплектации в зависимости от интерфейса, откыда был вызван этот интерфейс
        /// </summary>
        public void SetComplectationType()
        {
            switch (_ParentFrame)
            {
                // Комплектация ГА
                case "ProductionComplectationCM":
                    ComplectationType = ComplectationTypeRef.CorrugatingMachine;
                    break;

                // Комплектация ПР
                case "ProductionPMConversion":
                    ComplectationType = ComplectationTypeRef.ProcessingMachine;
                    break;

                // Комплектация СГП
                case "ProductionComplectationStock":
                    ComplectationType = ComplectationTypeRef.Stock;
                    break;

                // Комплектация ЛТ
                case "ProductionComplectationMoldedContainer":
                    ComplectationType = ComplectationTypeRef.MoldedContainer;
                    break;

                // Комплектация ГА КШ
                case "ComplectationCorrugatorKsh":
                    ComplectationType = ComplectationTypeRef.CorrugatingMachineKsh;
                    break;

                // Комплектация ПР КШ
                case "ComplectationProcessingKsh":
                    ComplectationType = ComplectationTypeRef.ProcessingMachineKsh;
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// В зависимости от типа комплектации меняем визуальную часть интерфейса (видимость кнопок)
        /// </summary>
        public void UpdateVisual()
        {
            switch (ComplectationType)
            {
                case ComplectationTypeRef.CorrugatingMachine:
                    OpenTechnologicalMapButton.Visibility = Visibility.Collapsed;
                    OpenTechnologicalMapBorder.Visibility = Visibility.Collapsed;

                    GetOrderDataButton.Visibility = Visibility.Visible;
                    GetOrderDataBorder.Visibility = Visibility.Visible;

                    CountProductionTaskLabel.Visibility = Visibility.Visible;
                    CountProductionTaskTextBox.Visibility = Visibility.Visible;
                    NextProductionTaskLabel.Visibility = Visibility.Visible;
                    NextProductionTaskTextBox.Visibility = Visibility.Visible;
                    CardboardThiknesLabel.Visibility = Visibility.Visible;
                    CardboardThiknesTextBox.Visibility = Visibility.Visible;
                    break;

                case ComplectationTypeRef.ProcessingMachine:
                    OpenTechnologicalMapButton.Visibility = Visibility.Visible;
                    OpenTechnologicalMapBorder.Visibility = Visibility.Visible;

                    GetOrderDataButton.Visibility = Visibility.Collapsed;
                    GetOrderDataBorder.Visibility = Visibility.Collapsed;

                    CountProductionTaskLabel.Visibility = Visibility.Collapsed;
                    CountProductionTaskTextBox.Visibility = Visibility.Collapsed;
                    NextProductionTaskLabel.Visibility = Visibility.Collapsed;
                    NextProductionTaskTextBox.Visibility = Visibility.Collapsed;
                    CardboardThiknesLabel.Visibility = Visibility.Collapsed;
                    CardboardThiknesTextBox.Visibility = Visibility.Collapsed;
                    break;

                case ComplectationTypeRef.Stock:
                    OpenTechnologicalMapButton.Visibility = Visibility.Collapsed;
                    OpenTechnologicalMapBorder.Visibility = Visibility.Collapsed;

                    GetOrderDataButton.Visibility = Visibility.Collapsed;
                    GetOrderDataBorder.Visibility = Visibility.Collapsed;

                    CountProductionTaskLabel.Visibility = Visibility.Collapsed;
                    CountProductionTaskTextBox.Visibility = Visibility.Collapsed;
                    NextProductionTaskLabel.Visibility = Visibility.Collapsed;
                    NextProductionTaskTextBox.Visibility = Visibility.Collapsed;
                    CardboardThiknesLabel.Visibility = Visibility.Collapsed;
                    CardboardThiknesTextBox.Visibility = Visibility.Collapsed;
                    break;

                case ComplectationTypeRef.MoldedContainer:
                    OpenTechnologicalMapButton.Visibility = Visibility.Visible;
                    OpenTechnologicalMapBorder.Visibility = Visibility.Visible;

                    GetOrderDataButton.Visibility = Visibility.Collapsed;
                    GetOrderDataBorder.Visibility = Visibility.Collapsed;

                    CountProductionTaskLabel.Visibility = Visibility.Collapsed;
                    CountProductionTaskTextBox.Visibility = Visibility.Collapsed;
                    NextProductionTaskLabel.Visibility = Visibility.Collapsed;
                    NextProductionTaskTextBox.Visibility = Visibility.Collapsed;
                    CardboardThiknesLabel.Visibility = Visibility.Collapsed;
                    CardboardThiknesTextBox.Visibility = Visibility.Collapsed;
                    break;

                case ComplectationTypeRef.CorrugatingMachineKsh:
                    OpenTechnologicalMapButton.Visibility = Visibility.Collapsed;
                    OpenTechnologicalMapBorder.Visibility = Visibility.Collapsed;

                    GetOrderDataButton.Visibility = Visibility.Visible;
                    GetOrderDataBorder.Visibility = Visibility.Visible;

                    CountProductionTaskLabel.Visibility = Visibility.Visible;
                    CountProductionTaskTextBox.Visibility = Visibility.Visible;
                    NextProductionTaskLabel.Visibility = Visibility.Visible;
                    NextProductionTaskTextBox.Visibility = Visibility.Visible;
                    CardboardThiknesLabel.Visibility = Visibility.Visible;
                    CardboardThiknesTextBox.Visibility = Visibility.Visible;
                    break;

                case ComplectationTypeRef.ProcessingMachineKsh:
                    OpenTechnologicalMapButton.Visibility = Visibility.Visible;
                    OpenTechnologicalMapBorder.Visibility = Visibility.Visible;

                    GetOrderDataButton.Visibility = Visibility.Collapsed;
                    GetOrderDataBorder.Visibility = Visibility.Collapsed;

                    CountProductionTaskLabel.Visibility = Visibility.Collapsed;
                    CountProductionTaskTextBox.Visibility = Visibility.Collapsed;
                    NextProductionTaskLabel.Visibility = Visibility.Collapsed;
                    NextProductionTaskTextBox.Visibility = Visibility.Collapsed;
                    CardboardThiknesLabel.Visibility = Visibility.Collapsed;
                    CardboardThiknesTextBox.Visibility = Visibility.Collapsed;
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            Role.AccessMode mode = Role.AccessMode.None;

            switch (ComplectationType)
            {
                // Комплектация ГА
                case ComplectationTypeRef.CorrugatingMachine:
                    mode = Central.Navigator.GetRoleLevel("[erp]complectation_cm");
                    break;

                // Комплектация ПР
                case ComplectationTypeRef.ProcessingMachine:
                    mode = Central.Navigator.GetRoleLevel("[erp]complectation_pm");
                    break;

                // Комплектация СГП
                case ComplectationTypeRef.Stock:
                    mode = Central.Navigator.GetRoleLevel("[erp]complectation_stock");
                    break;

                // Комплектация ЛТЗГ
                case ComplectationTypeRef.MoldedContainer:
                    mode = Central.Navigator.GetRoleLevel("[erp]compl_molded_contnr");
                    break;

                // Комплектация ГА КШ
                case ComplectationTypeRef.CorrugatingMachineKsh:
                    mode = Central.Navigator.GetRoleLevel("[erp]complectation_cm_ksh");
                    break;

                // Комплектация ПР КШ
                case ComplectationTypeRef.ProcessingMachineKsh:
                    mode = Central.Navigator.GetRoleLevel("[erp]complectation_pm_ksh");
                    break;

                default:
                    mode = Role.AccessMode.None;
                    break;
            }

            switch (mode)
            {
                case Role.AccessMode.Special:
                    MasterFlag = true;
                    break;

                default:
                    MasterFlag = false;
                    break;
            }
        }

        /// <summary>
        /// Обновление активности кнопок
        /// </summary>
        public void UpdateButtons()
        {
            ComplectationButton.IsEnabled = false;

            NewPalletAddButton.IsEnabled = false;
            NewPalletEditButton.IsEnabled = false;
            NewPalletDeleteButton.IsEnabled = false;

            OpenTechnologicalMapButton.IsEnabled = false;

            int summaryQuantityOnOldPallet = 0;
            int summaryQuantityOnNewPallet = 0;

            if (OldPalletGrid != null && OldPalletGrid.Items != null && OldPalletGrid.Items.Count > 0)
            {
                summaryQuantityOnOldPallet = OldPalletGrid.Items.Sum(x => x.CheckGet("KOL").ToInt());
            }

            if (NewPalletGrid != null && NewPalletGrid.Items != null && NewPalletGrid.Items.Count > 0)
            {
                summaryQuantityOnNewPallet = NewPalletGrid.Items.Sum(x => x.CheckGet("QTY").ToInt());
            }

            // Управление активностью кнопки комплектации
            {
                // Если есть старые поддоны, то это списание или комплектация - можем комплектовать
                if (OldPalletGrid != null && OldPalletGrid.Items != null && OldPalletGrid.Items.Count > 0)
                {
                    // Если есть новые поддоны, то это обычная комплектация
                    if (NewPalletGrid != null && NewPalletGrid.Items != null && NewPalletGrid.Items.Count > 0)
                    {
                        // Проверяем количество списываемой и создаваемой продукции
                        // если количество создаваемой продукции больше, чем списываемой, то должны быть права мастера
                        if (summaryQuantityOnNewPallet > summaryQuantityOnOldPallet)
                        {
                            // Проверяем, что есть права мастера. Если есть права, то можем комплектовать
                            if (MasterFlag)
                            {
                                ComplectationButton.IsEnabled = true;
                            }
                        }
                        // Если количество создаваемой продукции меньше или равно количеству списываемой, то можем комплектовать
                        else
                        {
                            ComplectationButton.IsEnabled = true;
                        }
                    }
                    // Если новых поддонов нет, то это списание
                    else
                    {
                        ComplectationButton.IsEnabled = true;
                    }
                }
                // Если старых поддонов нет, то это комплектация из воздуха. Предварительно проверяем, что есть новые поддоны
                else if (NewPalletGrid != null && NewPalletGrid.Items != null && NewPalletGrid.Items.Count > 0)
                {
                    // Проверяем, что есть права мастера. Если есть права, то можем комплектовать
                    if (MasterFlag)
                    {
                        ComplectationButton.IsEnabled = true;
                    }
                }
            }
            
            // Управление активностью кнопок действий с новыми поддонами
            {
                if (NewPalletGrid != null)
                {
                    NewPalletAddButton.IsEnabled = true;

                    if (NewPalletGridSelectedItem != null && NewPalletGridSelectedItem.Count > 0)
                    {
                        NewPalletEditButton.IsEnabled = true;
                        NewPalletDeleteButton.IsEnabled = true;
                    }
                }
            }

            // Управление текстом на кнопке комплектации
            {
                if (summaryQuantityOnNewPallet > 0)
                {
                    ComplectationButton.Content = "Скомплектовать";
                }
                else
                {
                    ComplectationButton.Content = "Списать в брак";
                }
            }

            // Управление активностью специальных для ПР кнопок
            {
                if (!string.IsNullOrEmpty(Form.GetValueByPath("PATHTK")))
                {
                    OpenTechnologicalMapButton.IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m">Сообщение</param>
        private void ProcessMessages(ItemMessage m)
        {

        }

        /// <summary>
        /// Делает неактивными все тулбары вкладки
        /// </summary>
        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
            NewPalletToolbar.IsEnabled = false;
        }

        /// <summary>
        /// Делает активными все тулбары вкладки
        /// Вызывает метод установки активности кнопок
        /// </summary>
        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
            NewPalletToolbar.IsEnabled = true;

            UpdateButtons();
        }

        /// <summary>
        /// Расчёт и обновление значения текстбокса Брак
        /// </summary>
        public void UpdateDefectiveQuantity()
        {
            if (ConsumptionQuantityTextBox != null && IncomingQuantityTextBox != null)
            {
                int defectiveQuantity = ConsumptionQuantityTextBox.Text.ToInt() - IncomingQuantityTextBox.Text.ToInt();
                DefectiveQuantityTextBox.Text = defectiveQuantity.ToString();

                // Установка заднего фона для поля Брак
                {
                    if (DefectiveQuantityTextBox.Text.ToInt() == 0)
                    {
                        var color = HColor.Yellow;
                        var bc = new BrushConverter();
                        var brush = (Brush)bc.ConvertFrom(color);

                        DefectiveQuantityTextBoxBorder.Background = brush;
                        DefectiveQuantityLabelBorder.Background = brush;
                        DefectiveQuantityTextBox.Background = brush;
                    }
                    else if (DefectiveQuantityTextBox.Text.ToInt() < 0)
                    {
                        var color = HColor.Red;
                        var bc = new BrushConverter();
                        var brush = (Brush)bc.ConvertFrom(color);

                        DefectiveQuantityTextBoxBorder.Background = brush;
                        DefectiveQuantityLabelBorder.Background = brush;
                        DefectiveQuantityTextBox.Background = brush;
                    }
                    else
                    {
                        DefectiveQuantityTextBoxBorder.Background = null;
                        DefectiveQuantityLabelBorder.Background = null;
                        DefectiveQuantityTextBox.Background = null;
                    }
                }

                // Установка заднего фона для поля Расход
                {
                    if (ConsumptionQuantityTextBox.Text.ToInt() == 0)
                    {
                        var color = HColor.Yellow;
                        var bc = new BrushConverter();
                        var brush = (Brush)bc.ConvertFrom(color);

                        ConsumptionQuantityTextBoxBorder.Background = brush;
                        ConsumptionQuantityLabelBorder.Background = brush;
                        ConsumptionQuantityTextBox.Background = brush;
                    }
                    else if (ConsumptionQuantityTextBox.Text.ToInt() < 0)
                    {
                        var color = HColor.Red;
                        var bc = new BrushConverter();
                        var brush = (Brush)bc.ConvertFrom(color);

                        ConsumptionQuantityTextBoxBorder.Background = brush;
                        ConsumptionQuantityLabelBorder.Background = brush;
                        ConsumptionQuantityTextBox.Background = brush;
                    }
                    else
                    {
                        ConsumptionQuantityTextBoxBorder.Background = null;
                        ConsumptionQuantityLabelBorder.Background = null;
                        ConsumptionQuantityTextBox.Background = null;
                    }
                }

                // Установка заднего фона для поля Приход
                {
                    if (IncomingQuantityTextBox.Text.ToInt() == 0)
                    {
                        var color = HColor.Yellow;
                        var bc = new BrushConverter();
                        var brush = (Brush)bc.ConvertFrom(color);

                        IncomingQuantityTextBoxBorder.Background = brush;
                        IncomingQuantityLabelBorder.Background = brush;
                        IncomingQuantityTextBox.Background = brush;
                    }
                    else if (IncomingQuantityTextBox.Text.ToInt() < 0)
                    {
                        var color = HColor.Red;
                        var bc = new BrushConverter();
                        var brush = (Brush)bc.ConvertFrom(color);

                        IncomingQuantityTextBoxBorder.Background = brush;
                        IncomingQuantityLabelBorder.Background = brush;
                        IncomingQuantityTextBox.Background = brush;
                    }
                    else
                    {
                        IncomingQuantityTextBoxBorder.Background = null;
                        IncomingQuantityLabelBorder.Background = null;
                        IncomingQuantityTextBox.Background = null;
                    }
                }
            }
        }

        /// <summary>
        /// Основная функция комплектации
        /// </summary>
        public void Complectation()
        {
            switch (ComplectationType)
            {
                case ComplectationTypeRef.CorrugatingMachine:
                    ComplectationCorrugatingMachine();
                    break;

                case ComplectationTypeRef.ProcessingMachine:
                    ComplectationProcessingMachine();
                    break;

                case ComplectationTypeRef.Stock:
                    ComplectationStock();
                    break;

                case ComplectationTypeRef.MoldedContainer:
                    ComplectationMoldedContainer();
                    break;

                case ComplectationTypeRef.CorrugatingMachineKsh:
                    ComplectationCorrugatingMachineKsh();
                    break;

                case ComplectationTypeRef.ProcessingMachineKsh:
                    ComplectationProcessingMachineKsh();
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            if (!string.IsNullOrEmpty(_ParentFrame))
            {
                Central.WM.SetActive(_ParentFrame, true);
                Central.WM.SelectedTab = _ParentFrame;
            }

            Central.WM.Close(FrameName);

            //вся работа по утилизации ресурсов происходит в Destroy
            //он будет вызван при закрытии фрейма
        }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Production",
                ReceiverName = "",
                SenderName = "ComplectationMainComplectationTab",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// отображение справочной статьи
        /// (относительный путь)
        /// </summary>
        public void ShowHelp()
        {
            //FIXME: Нужно сделать документацию
            Central.ShowHelp("/doc/l-pack-erp/");
        }

        /// <summary>
        /// Обработчик нажатий клавиатуры
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

                case Key.Enter:
                    NewPalletAdd();
                    e.Handled = true;
                    break;

                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// Добавляем поддон в грид новых поддонов (справа)
        /// </summary>
        public void NewPalletAdd()
        {
            int quantityOnPallet = 0;
            var i = new ComplectationPalletAdd(Form.GetValueByPath("QUANTITY_ON_PALLET").ToInt());

            if (ComplectationType == ComplectationTypeRef.CorrugatingMachine)
            {
                i.Show(true,
                    OldPalletGridSelectedItem.CheckGet("THIKNES").ToDouble(),
                    OldPalletGridSelectedItem.CheckGet("KOL_PACK").ToInt(),
                    OldPalletGridSelectedItem.CheckGet("ID_PZ").ToInt(),
                    OldPalletGridSelectedItem.CheckGet("ID2").ToInt()
                    );
            }
            else
            {
                i.Show();
            }

            if (i.OkFlag)
            {
                quantityOnPallet = i.QuantityOnPallet;
            }

            if (quantityOnPallet > 0)
            {
                var palletNumber = NewPalletDataSet.Items.Count + 1;

                var item = new Dictionary<string, string>
                {
                    ["PODDON_NUMBER"] = palletNumber.ToString(),
                    ["QTY"] = quantityOnPallet.ToString()
                };

                NewPalletDataSet.Items.Add(item);
                NewPalletGridLoadItems();
            }
        }

        /// <summary>
        /// Редактируем выбранный поддон из грида новых поддонов (справа)
        /// </summary>
        public void NewPalletEdit()
        {
            if (NewPalletGridSelectedItem != null && NewPalletGridSelectedItem.Count > 0)
            {
                var currentPalletNumber = NewPalletGridSelectedItem["PODDON_NUMBER"];
                var currentQuantityOnPallet = NewPalletGridSelectedItem["QTY"];

                int quantityOnPallet = 0;
                var i = new ComplectationPalletAdd(Form.GetValueByPath("QUANTITY_ON_PALLET").ToInt(), currentQuantityOnPallet.ToInt());

                if (ComplectationType == ComplectationTypeRef.CorrugatingMachine)
                {
                    i.Show(true,
                        OldPalletGridSelectedItem.CheckGet("THIKNES").ToDouble(),
                        OldPalletGridSelectedItem.CheckGet("KOL_PACK").ToInt(),
                        OldPalletGridSelectedItem.CheckGet("ID_PZ").ToInt(),
                        OldPalletGridSelectedItem.CheckGet("ID2").ToInt()
                        );
                }
                else
                {
                    i.Show();
                }

                if (i.OkFlag)
                {
                    quantityOnPallet = i.QuantityOnPallet;
                }

                if (quantityOnPallet > 0)
                {
                    NewPalletGridSelectedItem["QTY"] = quantityOnPallet.ToString();
                    NewPalletGridLoadItems();
                    NewPalletGrid.SelectRowByKey(currentPalletNumber.ToInt(), "PODDON_NUMBER");
                }
            }
        }

        /// <summary>
        /// Удаляем выбранный поддон из грида новых поддонов (справа)
        /// </summary>
        public void NewPalletDelete()
        {
            if (NewPalletGridSelectedItem != null && NewPalletGridSelectedItem.Count > 0)
            {
                NewPalletDataSet.Items.Remove(NewPalletGridSelectedItem);
                NewPalletGridSelectedItem = null;

                var palletNumber = 1;

                foreach (var item in NewPalletDataSet.Items)
                {
                    item["PODDON_NUMBER"] = palletNumber.ToString();
                    palletNumber++;
                }

                NewPalletGridLoadItems();
            }
        }

        /// <summary>
        /// Открывает окно ввода количества продукции, которое необходимо списать с выбранного поддона. Остаток запишется в новую строку грида новых поддонов
        /// </summary>
        public void WriteOff()
        {
            int remainQuantityOnPallet = 0;
            var i = new ComplectationWriteOffQuantity(Form.GetValueByPath("QUANTITY_ON_PALLET").ToInt(), OldPalletGridSelectedItem.CheckGet("KOL").ToInt(), OldPalletGridSelectedItem.CheckGet("PALLET"));
            i.Show();

            if (i.OkFlag)
            {
                remainQuantityOnPallet = i.RemainQuantityOnPallet;
            }

            if (remainQuantityOnPallet > 0)
            {
                var palletNumber = NewPalletDataSet.Items.Count + 1;

                var item = new Dictionary<string, string>
                {
                    ["PODDON_NUMBER"] = palletNumber.ToString(),
                    ["QTY"] = remainQuantityOnPallet.ToString()
                };

                NewPalletDataSet.Items.Add(item);
                NewPalletGridLoadItems();
            }
        }

        /// <summary>
        /// Комплектация ГА
        /// </summary>
        public async void ComplectationCorrugatingMachine()
        {
            var resume = true;
            DisableControls();

            // Проверяем что не пытаются сделать товара больше чем есть
            int summaryQuantityOnOldPallet = OldPalletGrid.Items.Sum(x => x.CheckGet("KOL").ToInt());
            int summaryQuantityOnNewPallet = 0;
            if (NewPalletGrid.Items != null)
            {
                summaryQuantityOnNewPallet = NewPalletGrid.Items.Sum(x => x.CheckGet("QTY").ToInt());
            }

            // Количество списываемых поддонов
            int oldPalletCount = OldPalletGrid.Items.Count;

            // Количество новых поддонов
            int newPalletCount = 0;
            if (NewPalletGrid.Items != null)
            {
                newPalletCount = NewPalletGrid.Items.Count;
            }

            if (summaryQuantityOnOldPallet < summaryQuantityOnNewPallet)
            {
                DialogWindow.ShowDialog("Нельзя скомплектовать больше товара чем было до комплектации. Уменьшите суммарное количество получаемого товара", "Комплектация ГА");
                resume = false;
            }

            // Запрашиваем подтверждение операции
            if (resume)
            {
                var message = 
                            $"Будет списано {Environment.NewLine}" +
                            $"{ConsumptionQuantityTextBox.Text} товара на {oldPalletCount} поддонах{Environment.NewLine}";

                if (summaryQuantityOnNewPallet > 0)
                {
                    message += 
                            $"Будет скомплектовано {Environment.NewLine}" +
                            $"{summaryQuantityOnNewPallet} товара на {newPalletCount} поддонах.{Environment.NewLine}";
                }
                else
                {
                    message += $"в брак.{Environment.NewLine}";
                }

                message += "Продолжить?";

                if (DialogWindow.ShowDialog(message, "Комплектация ГА", "", DialogWindowButtons.YesNo) != true)
                {
                    resume = false;
                }
            }

            // Предупреждение о комплектации не полных поддонов для ГП
            if (resume)
            {
                // Если новых поддонов больше 1 и это Готовая Продукция
                if (newPalletCount > 1 
                    && (Form.GetValueByPath("PRODUCT_IDK1").ToInt() == 5 || Form.GetValueByPath("PRODUCT_IDK1").ToInt() == 6))
                {
                    int nonfullPalletCount = NewPalletGrid.Items.Count(x => x.CheckGet("QTY").ToInt() != Form.GetValueByPath("QUANTITY_ON_PALLET").ToInt());
                    if (nonfullPalletCount > 1)
                    {
                        var message =
                            $"Внимание! Вы создаёте {nonfullPalletCount} неполных поддона с готовой продукцией.{Environment.NewLine}" +
                            $"Рекомендуется вернуться на предыдущий шаг и скомплектовать готовую продукцию до полного поддона.{Environment.NewLine}" +
                            $"Продолжить с {nonfullPalletCount} неполными поддонами?";
                        if (DialogWindow.ShowDialog(message, "Комплектация ГА", "", DialogWindowButtons.NoYes) != true)
                        {
                            resume = false;
                        }
                    }
                }
            }

            // Флаг того, что это комплектация без созданиянового поддона (со списанием количества картона с поддона и перемещением в К -1)
            bool complectationWithoutNewPallet = false;
            if (oldPalletCount == 1 && newPalletCount == 1)
            {
                if (OldPalletGrid.Items.First().CheckGet("RETURN_PALLET_FLAG").ToInt() == 0 && OldPalletGrid.Items.First().CheckGet("PRODUCTION_TASK_MACHINE_ID").ToInt() != 1800)
                {
                    complectationWithoutNewPallet = true;
                }
            }

            // Ид причины комплектации/списания
            var reasonId = "0";
            // Описание причины комплектации/списания
            var reasonMessage = "";

            // Запрашиваем причину комплектации/списания
            if (resume)
            {
                // Если обычныя комплектация, то выводим список причин комплектации
                if (summaryQuantityOnNewPallet > 0 && !complectationWithoutNewPallet)
                {
                    var view = new ComplectationReasonsEdit();
                    view.CorrugatorFlag = 1;
                    view.Show();

                    if (view.OkFlag)
                    {
                        reasonId = view.SelectedReason.Key;
                        reasonMessage = view.ReasonMessage;
                    }
                }
                // Если списание, то выводим список причин списания
                else
                {
                    var view = new ComplectationWriteOffReasonsEdit();
                    view.CorrugatorFlag = 1;
                    view.Show();

                    if (view.OkFlag)
                    {
                        reasonId = view.SelectedReason.Key;
                    }
                }

                if (reasonId.ToInt() > 0)
                {
                    resume = true;
                }
                else
                {
                    resume = false;
                }
            }

            // Комплектуем
            if (resume)
            {
                SplashControl.Visible = true;

                if (oldPalletCount > 0)
                {        
                    if (Form.GetValueByPath("PRODUCT_ID2") == OldPalletGrid.Items.First().CheckGet("ID2")) 
                    {
                        if (!complectationWithoutNewPallet)
                        {
                            List<Dictionary<string, string>> newPalletGridItems = new List<Dictionary<string, string>>();
                            if (NewPalletGrid.Items != null)
                            {
                                newPalletGridItems = NewPalletGrid.Items;
                            }

                            var p = new Dictionary<string, string>
                            {
                                ["Product"] = JsonConvert.SerializeObject(SelectedProductItem),
                                ["OldPalletList"] = JsonConvert.SerializeObject(OldPalletGrid.Items),
                                ["NewPalletList"] = JsonConvert.SerializeObject(newPalletGridItems),

                                ["idorderdates"] = OrderId.ToString(),

                                ["StanokId"] = ComplectationPlace.CorrugatingMachines,
                                ["ReasonId"] = reasonId,
                                ["ReasonMessage"] = reasonMessage
                            };

                            var q = new LPackClientQuery();
                            q.Request.SetParam("Module", "Complectation");
                            q.Request.SetParam("Object", "Pallet");
                            q.Request.SetParam("Action", "CreateCMNew");

                            q.Request.SetParams(p);

                            await Task.Run(() => { q.DoQuery(); });

                            if (q.Answer.Status == 0)
                            {
                                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                                if (result != null)
                                {
                                    var ds = ListDataSet.Create(result, "ITEMS");
                                    if (ds.Items.Count > 0)
                                    {
                                        var idpz = ds.Items.First().CheckGet("idpz");

                                        if (summaryQuantityOnNewPallet > 0 || idpz != "0")
                                        {
                                            // отправить сообщение
                                            Messenger.Default.Send(new ItemMessage
                                            {
                                                ReceiverGroup = "Complectation",
                                                ReceiverName = "Stock",
                                                SenderName = "ComplectationMainComplectationTab",
                                                Action = "Refresh",
                                            });

                                            // отправить сообщение
                                            Messenger.Default.Send(new ItemMessage
                                            {
                                                ReceiverGroup = "Complectation",
                                                ReceiverName = "CM",
                                                SenderName = "ComplectationMainComplectationTab",
                                                Action = "Refresh",
                                            });

                                            // печать ярлыков
                                            for (var i = 1; i <= newPalletCount; i++)
                                            {
                                                LabelReport2 report = new LabelReport2(true);
                                                report.PrintLabel(idpz, i.ToString(), Form.GetValueByPath("PRODUCT_IDK1"));
                                            }
                                        }
                                        else
                                        {
                                            Messenger.Default.Send(new ItemMessage
                                            {
                                                ReceiverGroup = "Complectation",
                                                ReceiverName = "WriteOff",
                                                SenderName = "ComplectationMainComplectationTab",
                                                Action = "Refresh",
                                            });

                                            // отправить сообщение
                                            Messenger.Default.Send(new ItemMessage
                                            {
                                                ReceiverGroup = "Complectation",
                                                ReceiverName = "CM",
                                                SenderName = "ComplectationMainComplectationTab",
                                                Action = "Refresh",
                                            });

                                            DialogWindow.ShowDialog("Списание выполнено");
                                        }

                                        if (Form.GetValueByPath("PRODUCT_IDK1").ToInt() != 6)
                                        {
                                            CheckRejectPercentage(OldPalletGrid.Items);
                                        }

                                        Close();
                                    }
                                    else
                                    {
                                        var msg = "На сервере произошла ошибка. Пожалуйста сообщите о проблеме.";
                                        var d = new DialogWindow($"{msg}", "Комплектация ГА", "", DialogWindowButtons.OK);
                                        d.ShowDialog();
                                    }
                                }
                                else
                                {
                                    var msg = "На сервере произошла ошибка. Пожалуйста сообщите о проблеме.";
                                    var d = new DialogWindow($"{msg}", "Комплектация ГА", "", DialogWindowButtons.OK);
                                    d.ShowDialog();
                                }
                            }
                            else
                            {
                                q.ProcessError();
                            }
                        }
                        // Комплектация без создания нового поддона (со списанием количества картона с поддона и перемещением в К -1)
                        else
                        {
                            var p = new Dictionary<string, string>
                            {
                                ["Product"] = JsonConvert.SerializeObject(SelectedProductItem),
                                ["OldPalletList"] = JsonConvert.SerializeObject(OldPalletGrid.Items),
                                ["NewPalletList"] = JsonConvert.SerializeObject(NewPalletGrid.Items),

                                ["idorderdates"] = OrderId.ToString(),

                                ["StanokId"] = ComplectationPlace.CorrugatingMachines,
                                ["ReasonId"] = reasonId,
                                ["ReasonMessage"] = reasonMessage
                            };

                            var q = new LPackClientQuery();
                            q.Request.SetParam("Module", "Complectation");
                            q.Request.SetParam("Object", "Pallet");
                            q.Request.SetParam("Action", "CreateCMWithoutNewPallet");

                            q.Request.SetParams(p);

                            await Task.Run(() => { q.DoQuery(); });

                            if (q.Answer.Status == 0)
                            {
                                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                                if (result != null)
                                {
                                    var ds = ListDataSet.Create(result, "ITEMS");
                                    if (ds.Items.Count > 0)
                                    {
                                        Messenger.Default.Send(new ItemMessage
                                        {
                                            ReceiverGroup = "Complectation",
                                            ReceiverName = "WriteOff",
                                            SenderName = "ComplectationMainComplectationTab",
                                            Action = "Refresh",
                                        });

                                        // отправить сообщение
                                        Messenger.Default.Send(new ItemMessage
                                        {
                                            ReceiverGroup = "Complectation",
                                            ReceiverName = "CM",
                                            SenderName = "ComplectationMainComplectationTab",
                                            Action = "Refresh",
                                        });

                                        int idMoving = ds.Items.First().CheckGet("IDM").ToInt();

                                        LabelReport2 report = new LabelReport2(true);
                                        report.PrintLabel(OldPalletGrid.Items.First().CheckGet("ID_PZ"), OldPalletGrid.Items.First().CheckGet("NUM"), Form.GetValueByPath("PRODUCT_IDK1"), OldPalletGrid.Items.First().CheckGet("IDP").ToInt());

                                        if (Form.GetValueByPath("PRODUCT_IDK1").ToInt() != 6)
                                        {
                                            CheckRejectPercentage(OldPalletGrid.Items);
                                        }

                                        Close();
                                    }
                                    else
                                    {
                                        var msg = "На сервере произошла ошибка. Пожалуйста сообщите о проблеме.";
                                        var d = new DialogWindow($"{msg}", "Комплектация ГА", "", DialogWindowButtons.OK);
                                        d.ShowDialog();
                                    }
                                }
                                else
                                {
                                    var msg = "На сервере произошла ошибка. Пожалуйста сообщите о проблеме.";
                                    var d = new DialogWindow($"{msg}", "Комплектация ГА", "", DialogWindowButtons.OK);
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
                        var msg = $"Продукция на поддонах не совпадает с выбранной. Пожалуйста, нажмите кнопку \"Отмена\" и повторите операцию.";
                        var d = new DialogWindow($"{msg}", "Комплектация ГА", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    var msg = $"Необходимо выбрать хотя бы один поддон из которого будем комплектовать.";
                    var d = new DialogWindow($"{msg}", "Комплектация ГА", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }

            SplashControl.Visible = false;
            EnableControls();
        }

        public async void GetOrderData()
        {
            if (OrderId > 0)
            {
                var p = new Dictionary<string, string>();
                p.Add("ORDER_POSITION_ID", OrderId.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Complectation");
                q.Request.SetParam("Object", "Operation");
                q.Request.SetParam("Action", "ListOrderData");

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
                        var productionTaskDs = ListDataSet.Create(result, "PRODUCTION_TASK");
                        var orderDs = ListDataSet.Create(result, "ORDER");
                        var complectationDs = ListDataSet.Create(result, "COMPLECTATION");

                        string msg = "";

                        if (orderDs != null && orderDs.Items != null && orderDs.Items.Count > 0)
                        {
                            msg = $"{msg}---[Данные по заявке]---{Environment.NewLine}";

                            var orderDsFirstItem = orderDs.Items[0];
                            msg = $"{msg}Заявка: {orderDsFirstItem.CheckGet("ORDER_ID").ToInt()}{Environment.NewLine}";
                            msg = $"{msg}Продукция: {orderDsFirstItem.CheckGet("PRODUCT_NAME")}{Environment.NewLine}";
                            msg = $"{msg}Необходимое количество по заявке: {orderDsFirstItem.CheckGet("ORDER_QUANTITY").ToInt()}{Environment.NewLine}";
                            msg = $"{msg}Текущее количество по заявке: {orderDsFirstItem.CheckGet("ACTUAL_QUANTITY").ToInt()}{Environment.NewLine}";
                            msg = $"{msg}Заданий на ГА: {orderDsFirstItem.CheckGet("RPODUCTION_TASK_COUNT").ToInt()}{Environment.NewLine}";
                            msg = $"{msg}Выполненных задание на ГА: {orderDsFirstItem.CheckGet("PRODUCTION_TASK_DONE_COUNT").ToInt()}{Environment.NewLine}";
                            msg = $"{msg}Количество в заданиях на ГА: {orderDsFirstItem.CheckGet("PRODUCTION_TASK_QUANTITY").ToInt()}{Environment.NewLine}";
                            msg = $"{msg}Отсканированно в заданиях на ГА: {orderDsFirstItem.CheckGet("PRODUCTION_TASK_DONE_QUANTITY").ToInt()}{Environment.NewLine}";

                            msg = $"{msg}{Environment.NewLine}";
                        }

                        if (productionTaskDs != null && productionTaskDs.Items != null && productionTaskDs.Items.Count > 0)
                        {
                            msg = $"{msg}---[Данные по ПЗ]---{Environment.NewLine}";

                            bool first = true;

                            foreach (var productionTaskDsItem in productionTaskDs.Items)
                            {
                                if (!first)
                                {
                                    msg = $"{msg}---{Environment.NewLine}";
                                }
                                else
                                {
                                    first = false;
                                }

                                msg = $"{msg}Задание: {productionTaskDsItem.CheckGet("PRODUCTION_TASK_NUM")}{Environment.NewLine}";
                                msg = $"{msg}По заданию: {productionTaskDsItem.CheckGet("PRODUCTION_TASK_QUANTITY").ToInt()}{Environment.NewLine}";
                                msg = $"{msg}Отсканированно: {productionTaskDsItem.CheckGet("SCANED_QUANTITY").ToInt()}{Environment.NewLine}";
                                msg = $"{msg}Текущее количество: {productionTaskDsItem.CheckGet("ACTUAL_QUANTITY").ToInt()}{Environment.NewLine}";
                                msg = $"{msg}Выпустил ГА: {productionTaskDsItem.CheckGet("CORRUGATOR_MACHINE_QUANTITY").ToInt()}{Environment.NewLine}";
                            }

                            msg = $"{msg}{Environment.NewLine}";
                        }

                        if (complectationDs != null && complectationDs.Items != null && complectationDs.Items.Count > 0)
                        {
                            msg = $"{msg}---[Данные по комплектациям ГА]---{Environment.NewLine}";

                            bool first = true;

                            foreach (var complectationDsItem in complectationDs.Items)
                            {
                                if (!first)
                                {
                                    msg = $"{msg}---{Environment.NewLine}";
                                }
                                else
                                {
                                    first = false;
                                }

                                msg = $"{msg}Комплектация: {complectationDsItem.CheckGet("PRODUCTION_TASK_NUM")}{Environment.NewLine}";
                                msg = $"{msg}По заданию: {complectationDsItem.CheckGet("PRODUCTION_TASK_QUANTITY").ToInt()}{Environment.NewLine}";
                                msg = $"{msg}Отсканированно: {complectationDsItem.CheckGet("SCANED_QUANTITY").ToInt()}{Environment.NewLine}";
                                msg = $"{msg}Текущее количество: {complectationDsItem.CheckGet("ACTUAL_QUANTITY").ToInt()}{Environment.NewLine}";
                            }
                        }

                        var d = new DialogWindow($"{msg}", "Комплектация ГА", "", DialogWindowButtons.OK);
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
        /// Проверяем процент забракованной продукции от общего количества по этому производственному заданию.
        /// Работает только для расхода без прихода (списание полное или частичное)
        /// </summary>
        public void CheckRejectPercentage(List<Dictionary<string, string>> palletList)
        {
            List<Dictionary<string, string>> prductionTaskList = new List<Dictionary<string, string>>();
            foreach (var item in palletList)
            {
                if (item.CheckGet("ID_PZ").ToInt() > 0 && item.CheckGet("ID2").ToInt() > 0)
                {
                    var productionTask = prductionTaskList.FirstOrDefault(x => x.CheckGet("ID_PZ").ToInt() == item.CheckGet("ID_PZ").ToInt()
                                            && x.CheckGet("ID2").ToInt() == item.CheckGet("ID2").ToInt());
                    if (productionTask == null)
                    {
                        prductionTaskList.Add(new Dictionary<string, string>() {
                        { "ID_PZ", item.CheckGet("ID_PZ") },
                        { "ID2", item.CheckGet("ID2") },
                        { "PRODUCTION_TASK_NUM", item.CheckGet("PRODUCTION_TASK_NUM") }
                    });
                    }
                }
            }

            foreach (var productionTask in prductionTaskList)
            {
                int productionTaskId = productionTask.CheckGet("ID_PZ").ToInt();
                string productionTaskNumber = productionTask.CheckGet("PRODUCTION_TASK_NUM");
                int productId = productionTask.CheckGet("ID2").ToInt();
                bool succesfullCalculateFlag = false;
                
                if (productionTaskId > 0)
                {
                    var p = new Dictionary<string, string>();
                    p.Add("ID_PZ", productionTaskId.ToString());
                    p.Add("ID2", productId.ToString());

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Complectation");
                    q.Request.SetParam("Object", "Product");
                    q.Request.SetParam("Action", "GetConsumptionQuantity");

                    q.Request.SetParams(p);
                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            ListDataSet ds = ListDataSet.Create(result, "ITEMS");
                            if (ds.Items.Count > 0)
                            {
                                Dictionary<string, string> firstDictionary = ds.Items.First();

                                int productionTaskQuantity = firstDictionary.CheckGet("INCOMING_QUANTITY").ToInt();
                                int orderQuantity = firstDictionary.CheckGet("ORDER_QUANTITY").ToInt();
                                int consumptionQuantityProductionTask = firstDictionary.CheckGet("CONSUMPTION_QUANTITY_PZ").ToInt();
                                int consumptionQuantityOrder = firstDictionary.CheckGet("CONSUMPTION_QUANTITY_OD").ToInt();

                                if (orderQuantity > 0)
                                {
                                    double onePercent = (double)orderQuantity / 100.0;
                                    double fivePercent = onePercent * 5;
                                    int intFivePercent = fivePercent.ToInt();

                                    // Если суммарный расход по заданию больше, чем 5% от количества по заявки, то сообщаем об этом
                                    if (consumptionQuantityProductionTask > intFivePercent)
                                    {
                                        var msg = $"По производственному заданию {productionTaskNumber}" +
                                            $"{Environment.NewLine}количество забракованной продукции превышает 5%!" +
                                            $"{Environment.NewLine}По заявке = {orderQuantity} шт." +
                                            $"{Environment.NewLine}Отбраковано по заявке = {consumptionQuantityOrder} шт." +
                                            $"{Environment.NewLine}По заданию = {productionTaskQuantity} шт." +
                                            $"{Environment.NewLine}Отбраковано по заданию = {consumptionQuantityProductionTask} шт.";
                                        var d = new DialogWindow($"{msg}", "Комплектация ГА", "", DialogWindowButtons.OK);
                                        d.ShowDialog();
                                    }

                                    succesfullCalculateFlag = true;
                                }
                            }
                        }

                        if (!succesfullCalculateFlag)
                        {
                            var msg = $"Ошибка расчёта процента брака для производственного задания {productionTaskNumber}." +
                                $"{Environment.NewLine}Пожалуйста, сообщите о проблеме.";
                            var d = new DialogWindow($"{msg}", "Комплектация ГА", "", DialogWindowButtons.OK);
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
        /// Комплектация ГА Кашира
        /// </summary>
        public async void ComplectationCorrugatingMachineKsh()
        {
            var resume = true;
            DisableControls();

            // Проверяем что не пытаются сделать товара больше чем есть
            int summaryQuantityOnOldPallet = OldPalletGrid.Items.Sum(x => x.CheckGet("KOL").ToInt());
            int summaryQuantityOnNewPallet = 0;
            if (NewPalletGrid.Items != null)
            {
                summaryQuantityOnNewPallet = NewPalletGrid.Items.Sum(x => x.CheckGet("QTY").ToInt());
            }

            // Количество списываемых поддонов
            int oldPalletCount = OldPalletGrid.Items.Count;

            // Количество новых поддонов
            int newPalletCount = 0;
            if (NewPalletGrid.Items != null)
            {
                newPalletCount = NewPalletGrid.Items.Count;
            }

            if (summaryQuantityOnOldPallet < summaryQuantityOnNewPallet)
            {
                DialogWindow.ShowDialog("Нельзя скомплектовать больше товара чем было до комплектации. Уменьшите суммарное количество получаемого товара", "Комплектация ГА КШ");
                resume = false;
            }

            // Запрашиваем подтверждение операции
            if (resume)
            {
                var message =
                            $"Будет списано {Environment.NewLine}" +
                            $"{ConsumptionQuantityTextBox.Text} товара на {oldPalletCount} поддонах{Environment.NewLine}";

                if (summaryQuantityOnNewPallet > 0)
                {
                    message +=
                            $"Будет скомплектовано {Environment.NewLine}" +
                            $"{summaryQuantityOnNewPallet} товара на {newPalletCount} поддонах.{Environment.NewLine}";
                }
                else
                {
                    message += $"в брак.{Environment.NewLine}";
                }

                message += "Продолжить?";

                if (DialogWindow.ShowDialog(message, "Комплектация ГА КШ", "", DialogWindowButtons.YesNo) != true)
                {
                    resume = false;
                }
            }

            // Предупреждение о комплектации не полных поддонов для ГП
            if (resume)
            {
                // Если новых поддонов больше 1 и это Готовая Продукция
                if (newPalletCount > 1
                    && (Form.GetValueByPath("PRODUCT_IDK1").ToInt() == 5 || Form.GetValueByPath("PRODUCT_IDK1").ToInt() == 6))
                {
                    int nonfullPalletCount = NewPalletGrid.Items.Count(x => x.CheckGet("QTY").ToInt() != Form.GetValueByPath("QUANTITY_ON_PALLET").ToInt());
                    if (nonfullPalletCount > 1)
                    {
                        var message =
                            $"Внимание! Вы создаёте {nonfullPalletCount} неполных поддона с готовой продукцией.{Environment.NewLine}" +
                            $"Рекомендуется вернуться на предыдущий шаг и скомплектовать готовую продукцию до полного поддона.{Environment.NewLine}" +
                            $"Продолжить с {nonfullPalletCount} неполными поддонами?";
                        if (DialogWindow.ShowDialog(message, "Комплектация ГА", "", DialogWindowButtons.NoYes) != true)
                        {
                            resume = false;
                        }
                    }
                }
            }

            // Флаг того, что это комплектация без созданиянового поддона (со списанием количества картона с поддона и перемещением в К -1)
            bool complectationWithoutNewPallet = false;
            if (oldPalletCount == 1 && newPalletCount == 1)
            {
                if (OldPalletGrid.Items.First().CheckGet("RETURN_PALLET_FLAG").ToInt() == 0 && OldPalletGrid.Items.First().CheckGet("PRODUCTION_TASK_MACHINE_ID").ToInt() != 1810)
                {
                    complectationWithoutNewPallet = true;
                }
            }

            // Ид причины комплектации/списания
            var reasonId = "0";
            // Описание причины комплектации/списания
            var reasonMessage = "";

            // Запрашиваем причину комплектации/списания
            if (resume)
            {
                // Если обычныя комплектация, то выводим список причин комплектации
                if (summaryQuantityOnNewPallet > 0 && !complectationWithoutNewPallet)
                {
                    var view = new ComplectationReasonsEdit();
                    view.CorrugatorFlag = 1;
                    view.Show();

                    if (view.OkFlag)
                    {
                        reasonId = view.SelectedReason.Key;
                        reasonMessage = view.ReasonMessage;
                    }
                }
                // Если списание, то выводим список причин списания
                else
                {
                    var view = new ComplectationWriteOffReasonsEdit();
                    view.CorrugatorFlag = 1;
                    view.Show();

                    if (view.OkFlag)
                    {
                        reasonId = view.SelectedReason.Key;
                    }
                }

                if (reasonId.ToInt() > 0)
                {
                    resume = true;
                }
                else
                {
                    resume = false;
                }
            }

            // Комплектуем
            if (resume)
            {
                SplashControl.Visible = true;

                if (oldPalletCount > 0)
                {
                    if (Form.GetValueByPath("PRODUCT_ID2") == OldPalletGrid.Items.First().CheckGet("ID2"))
                    {
                        if (!complectationWithoutNewPallet)
                        {
                            List<Dictionary<string, string>> newPalletGridItems = new List<Dictionary<string, string>>();
                            if (NewPalletGrid.Items != null)
                            {
                                newPalletGridItems = NewPalletGrid.Items;
                            }

                            var p = new Dictionary<string, string>
                            {
                                ["Product"] = JsonConvert.SerializeObject(SelectedProductItem),
                                ["OldPalletList"] = JsonConvert.SerializeObject(OldPalletGrid.Items),
                                ["NewPalletList"] = JsonConvert.SerializeObject(newPalletGridItems),

                                ["idorderdates"] = OrderId.ToString(),

                                ["StanokId"] = ComplectationPlace.CorrugatingMachinesKsh,
                                ["ReasonId"] = reasonId,
                                ["ReasonMessage"] = reasonMessage
                            };

                            var q = new LPackClientQuery();
                            q.Request.SetParam("Module", "Complectation");
                            q.Request.SetParam("Object", "Pallet");
                            q.Request.SetParam("Action", "CreateCorrugatingMachineKsh");

                            q.Request.SetParams(p);

                            await Task.Run(() => { q.DoQuery(); });

                            if (q.Answer.Status == 0)
                            {
                                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                                if (result != null)
                                {
                                    var ds = ListDataSet.Create(result, "ITEMS");
                                    if (ds.Items.Count > 0)
                                    {
                                        var idpz = ds.Items.First().CheckGet("idpz");

                                        if (summaryQuantityOnNewPallet > 0 || idpz != "0")
                                        {
                                            // отправить сообщение
                                            Messenger.Default.Send(new ItemMessage
                                            {
                                                ReceiverGroup = "ComplectationKsh",
                                                ReceiverName = "ComplectationListKsh",
                                                SenderName = "ComplectationMainComplectationTab",
                                                Action = "Refresh",
                                            });

                                            // отправить сообщение
                                            Messenger.Default.Send(new ItemMessage
                                            {
                                                ReceiverGroup = "ComplectationKsh",
                                                ReceiverName = _ParentFrame,
                                                SenderName = "ComplectationMainComplectationTab",
                                                Action = "Refresh",
                                            });

                                            // печать ярлыков
                                            for (var i = 1; i <= newPalletCount; i++)
                                            {
                                                LabelReport2 report = new LabelReport2(true);
                                                report.PrintLabel(idpz, i.ToString(), Form.GetValueByPath("PRODUCT_IDK1"));
                                            }
                                        }
                                        else
                                        {
                                            Messenger.Default.Send(new ItemMessage
                                            {
                                                ReceiverGroup = "ComplectationKsh",
                                                ReceiverName = "ComplectationWriteOffListKsh",
                                                SenderName = "ComplectationMainComplectationTab",
                                                Action = "Refresh",
                                            });

                                            // отправить сообщение
                                            Messenger.Default.Send(new ItemMessage
                                            {
                                                ReceiverGroup = "ComplectationKsh",
                                                ReceiverName = _ParentFrame,
                                                SenderName = "ComplectationMainComplectationTab",
                                                Action = "Refresh",
                                            });

                                            DialogWindow.ShowDialog("Списание выполнено");
                                        }

                                        if (Form.GetValueByPath("PRODUCT_IDK1").ToInt() != 6)
                                        {
                                            CheckRejectPercentage(OldPalletGrid.Items);
                                        }

                                        Close();
                                    }
                                    else
                                    {
                                        var msg = "На сервере произошла ошибка. Пожалуйста сообщите о проблеме.";
                                        var d = new DialogWindow($"{msg}", "Комплектация ГА КШ", "", DialogWindowButtons.OK);
                                        d.ShowDialog();
                                    }
                                }
                                else
                                {
                                    var msg = "На сервере произошла ошибка. Пожалуйста сообщите о проблеме.";
                                    var d = new DialogWindow($"{msg}", "Комплектация ГА КШ", "", DialogWindowButtons.OK);
                                    d.ShowDialog();
                                }
                            }
                            else
                            {
                                q.ProcessError();
                            }
                        }
                        // Комплектация без создания нового поддона (со списанием количества картона с поддона и перемещением в К -1)
                        else
                        {
                            var p = new Dictionary<string, string>
                            {
                                ["Product"] = JsonConvert.SerializeObject(SelectedProductItem),
                                ["OldPalletList"] = JsonConvert.SerializeObject(OldPalletGrid.Items),
                                ["NewPalletList"] = JsonConvert.SerializeObject(NewPalletGrid.Items),

                                ["idorderdates"] = OrderId.ToString(),

                                ["StanokId"] = ComplectationPlace.CorrugatingMachinesKsh,
                                ["ReasonId"] = reasonId,
                                ["ReasonMessage"] = reasonMessage
                            };

                            var q = new LPackClientQuery();
                            q.Request.SetParam("Module", "Complectation");
                            q.Request.SetParam("Object", "Pallet");
                            q.Request.SetParam("Action", "CreateCorrugatingMachineKshWithoutNewPallet");

                            q.Request.SetParams(p);

                            await Task.Run(() => { q.DoQuery(); });

                            if (q.Answer.Status == 0)
                            {
                                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                                if (result != null)
                                {
                                    var ds = ListDataSet.Create(result, "ITEMS");
                                    if (ds.Items.Count > 0)
                                    {
                                        Messenger.Default.Send(new ItemMessage
                                        {
                                            ReceiverGroup = "ComplectationKsh",
                                            ReceiverName = "ComplectationWriteOffListKsh",
                                            SenderName = "ComplectationMainComplectationTab",
                                            Action = "Refresh",
                                        });

                                        // отправить сообщение
                                        Messenger.Default.Send(new ItemMessage
                                        {
                                            ReceiverGroup = "ComplectationKsh",
                                            ReceiverName = _ParentFrame,
                                            SenderName = "ComplectationMainComplectationTab",
                                            Action = "Refresh",
                                        });

                                        int idMoving = ds.Items.First().CheckGet("IDM").ToInt();

                                        LabelReport2 report = new LabelReport2(true);
                                        report.PrintLabel(OldPalletGrid.Items.First().CheckGet("ID_PZ"), OldPalletGrid.Items.First().CheckGet("NUM"), Form.GetValueByPath("PRODUCT_IDK1"), OldPalletGrid.Items.First().CheckGet("IDP").ToInt());

                                        if (Form.GetValueByPath("PRODUCT_IDK1").ToInt() != 6)
                                        {
                                            CheckRejectPercentage(OldPalletGrid.Items);
                                        }

                                        Close();
                                    }
                                    else
                                    {
                                        var msg = "На сервере произошла ошибка. Пожалуйста сообщите о проблеме.";
                                        var d = new DialogWindow($"{msg}", "Комплектация ГА КШ", "", DialogWindowButtons.OK);
                                        d.ShowDialog();
                                    }
                                }
                                else
                                {
                                    var msg = "На сервере произошла ошибка. Пожалуйста сообщите о проблеме.";
                                    var d = new DialogWindow($"{msg}", "Комплектация ГА КШ", "", DialogWindowButtons.OK);
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
                        var msg = $"Продукция на поддонах не совпадает с выбранной. Пожалуйста, нажмите кнопку \"Отмена\" и повторите операцию.";
                        var d = new DialogWindow($"{msg}", "Комплектация ГА КШ", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    var msg = $"Необходимо выбрать хотя бы один поддон из которого будем комплектовать.";
                    var d = new DialogWindow($"{msg}", "Комплектация ГА КШ", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }

            SplashControl.Visible = false;
            EnableControls();
        }

        /// <summary>
        /// Комплектация ПР
        /// </summary>
        public async void ComplectationProcessingMachine()
        {
            var resume = true;
            DisableControls();

            resume = CheckBalance();

            // Суммарное количество продукции на списываемых поддонах
            int summaryQuantityOnOldPallet = 0;
            // Суммарное количество продукции на создаваемых поддонах
            int summaryQuantityOnNewPallet = 0;

            // проверяем что не пытаются сделать товара больше чем есть
            if (resume)
            {
                if (OldPalletGrid.Items != null)
                {
                    summaryQuantityOnOldPallet = OldPalletGrid.Items.Sum(x => x.CheckGet("KOL").ToInt());
                }

                if (NewPalletGrid.Items != null)
                {
                    summaryQuantityOnNewPallet = NewPalletGrid.Items.Sum(x => x.CheckGet("QTY").ToInt());
                }

                if (summaryQuantityOnOldPallet < summaryQuantityOnNewPallet)
                {
                    if (MasterFlag)
                    {
                        resume = DialogWindow.ShowDialog("Вы мастер Переработки. Внимание вы комплектуете товара больше чем было до комплектации. Продолжить?",
                                                       "Комплектация ПР", "", DialogWindowButtons.NoYes) == true;
                    }
                    else
                    {
                        DialogWindow.ShowDialog("Нельзя скомплектовать больше товара чем было до комплектации. Уменьшите суммарное количество получаемого товара", "Комплектация ПР");
                        resume = false;
                    }
                }
            }

            // Запрашиваем подтверждение операции
            if (resume)
            {
                var message =
                            $"Будет списано {Environment.NewLine}" +
                            $"{ConsumptionQuantityTextBox.Text} товара на {OldPalletGrid.Items.Count} поддонах{Environment.NewLine}";

                if (summaryQuantityOnNewPallet > 0)
                {
                    message +=
                            $"Будет скомплектовано {Environment.NewLine}" +
                            $"{summaryQuantityOnNewPallet} товара на {NewPalletGrid.Items.Count} поддонах.{Environment.NewLine}";
                }
                else
                {
                    message += $"в брак.{Environment.NewLine}";
                }

                message += "Продолжить?";

                if (DialogWindow.ShowDialog(message, "Комплектация ПР", "", DialogWindowButtons.YesNo) != true)
                {
                    resume = false;
                }
            }

            // Ид причины комплектации
            var reasonId = "0";
            // Описание причины комплектации
            var reasonMessage = "";

            // Запрашиваем причину комплектации
            if (resume)
            {
                // Если есть новые поддоны, то есть это комплектация, то вводим причину комплектации
                if (summaryQuantityOnNewPallet > 0)
                {
                    var view = new ComplectationReasonsEdit();
                    view.ConvertingFlag = 1;
                    view.Show();

                    if (view.OkFlag)
                    {
                        reasonId = view.SelectedReason.Key;
                        reasonMessage = view.ReasonMessage;
                    }
                    else
                    {
                        resume = false;
                    }
                }
                // Если нет, то указываем причину списания
                else
                {
                    var view = new ComplectationWriteOffReasonsEdit();
                    view.ConvertingFlag = 1;
                    view.Show();

                    if (view.OkFlag)
                    {
                        reasonId = view.SelectedReason.Key;
                    }
                    else
                    {
                        resume = false;
                    }
                }
            }

            // Комплектуем
            if (resume)
            {
                SplashControl.Visible = true;

                int newPalletCount = 0;
                List<Dictionary<string, string>> newPalletGridItems = new List<Dictionary<string, string>>();
                if (NewPalletGrid.Items != null)
                {
                    newPalletCount = NewPalletGrid.Items.Count;
                    newPalletGridItems = NewPalletGrid.Items;
                }

                var p = new Dictionary<string, string>
                {
                    ["Product"] = JsonConvert.SerializeObject(SelectedProductItem),
                    ["OldPalletList"] = JsonConvert.SerializeObject(OldPalletGrid.Items),
                    ["NewPalletList"] = JsonConvert.SerializeObject(newPalletGridItems),
                    ["idorderdates"] = OrderId.ToString(),
                    ["StanokId"] = ComplectationPlace.ProcessingMachines,
                    ["ReasonId"] = reasonId,
                    ["ReasonMessage"] = reasonMessage
                };

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Complectation");
                q.Request.SetParam("Object", "Pallet");
                q.Request.SetParam("Action", "CreateConversion");

                q.Request.SetParams(p);

                await Task.Run(() => { q.DoQuery(); });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                        {
                            var idpz = ds.Items.First().CheckGet("idpz");
                            if (summaryQuantityOnNewPallet > 0 || idpz != "0")
                            {
                                // отправить сообщение
                                Messenger.Default.Send(new ItemMessage
                                {
                                    ReceiverGroup = "Complectation",
                                    ReceiverName = "Stock",
                                    SenderName = "ComplectationMainComplectationTab",
                                    Action = "Refresh",
                                });

                                // печать ярлыков
                                for (var i = 1; i <= newPalletCount; i++)
                                {
                                    LabelReport2 report = new LabelReport2(true);
                                    report.PrintLabel(idpz, i.ToString(), SelectedProductItem.CheckGet("IDK1"));
                                }
                            }
                            else
                            {
                                Messenger.Default.Send(new ItemMessage
                                {
                                    ReceiverGroup = "Complectation",
                                    ReceiverName = "WriteOff",
                                    SenderName = "ComplectationMainComplectationTab",
                                    Action = "Refresh",
                                });

                                DialogWindow.ShowDialog("Списание выполнено");
                            }

                            // отправить сообщение
                            Messenger.Default.Send(new ItemMessage
                            {
                                ReceiverGroup = "Complectation",
                                ReceiverName = "PM",
                                SenderName = "ComplectationMainComplectationTab",
                                Action = "Refresh",
                            });

                            Close();
                        }
                        else
                        {
                            var msg = "На сервере произошла ошибка. Пожалуйста сообщите о проблеме.";
                            var d = new DialogWindow($"{msg}", "Комплектация ПР", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                    else
                    {
                        var msg = "На сервере произошла ошибка. Пожалуйста сообщите о проблеме.";
                        var d = new DialogWindow($"{msg}", "Комплектация ПР", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            SplashControl.Visible = false;
            EnableControls();
        }

        /// <summary>
        /// Комплектация ПР КШ
        /// </summary>
        public async void ComplectationProcessingMachineKsh()
        {
            var resume = true;
            DisableControls();

            resume = CheckBalance();

            // Суммарное количество продукции на списываемых поддонах
            int summaryQuantityOnOldPallet = 0;
            // Суммарное количество продукции на создаваемых поддонах
            int summaryQuantityOnNewPallet = 0;

            // проверяем что не пытаются сделать товара больше чем есть
            if (resume)
            {
                if (OldPalletGrid.Items != null)
                {
                    summaryQuantityOnOldPallet = OldPalletGrid.Items.Sum(x => x.CheckGet("KOL").ToInt());
                }

                if (NewPalletGrid.Items != null)
                {
                    summaryQuantityOnNewPallet = NewPalletGrid.Items.Sum(x => x.CheckGet("QTY").ToInt());
                }

                if (summaryQuantityOnOldPallet < summaryQuantityOnNewPallet)
                {
                    if (MasterFlag)
                    {
                        resume = DialogWindow.ShowDialog("Вы мастер Переработки. Внимание вы комплектуете товара больше чем было до комплектации. Продолжить?",
                                                       "Комплектация ПР КШ", "", DialogWindowButtons.NoYes) == true;
                    }
                    else
                    {
                        DialogWindow.ShowDialog("Нельзя скомплектовать больше товара чем было до комплектации. Уменьшите суммарное количество получаемого товара", "Комплектация ПР КШ");
                        resume = false;
                    }
                }
            }

            // Запрашиваем подтверждение операции
            if (resume)
            {
                var message =
                            $"Будет списано {Environment.NewLine}" +
                            $"{ConsumptionQuantityTextBox.Text} товара на {OldPalletGrid.Items.Count} поддонах{Environment.NewLine}";

                if (summaryQuantityOnNewPallet > 0)
                {
                    message +=
                            $"Будет скомплектовано {Environment.NewLine}" +
                            $"{summaryQuantityOnNewPallet} товара на {NewPalletGrid.Items.Count} поддонах.{Environment.NewLine}";
                }
                else
                {
                    message += $"в брак.{Environment.NewLine}";
                }

                message += "Продолжить?";

                if (DialogWindow.ShowDialog(message, "Комплектация ПР КШ", "", DialogWindowButtons.YesNo) != true)
                {
                    resume = false;
                }
            }

            // Ид причины комплектации
            var reasonId = "0";
            // Описание причины комплектации
            var reasonMessage = "";

            // Запрашиваем причину комплектации
            if (resume)
            {
                // Если есть новые поддоны, то есть это комплектация, то вводим причину комплектации
                if (summaryQuantityOnNewPallet > 0)
                {
                    var view = new ComplectationReasonsEdit();
                    view.ConvertingFlag = 1;
                    view.Show();

                    if (view.OkFlag)
                    {
                        reasonId = view.SelectedReason.Key;
                        reasonMessage = view.ReasonMessage;
                    }
                    else
                    {
                        resume = false;
                    }
                }
                // Если нет, то указываем причину списания
                else
                {
                    var view = new ComplectationWriteOffReasonsEdit();
                    view.ConvertingFlag = 1;
                    view.Show();

                    if (view.OkFlag)
                    {
                        reasonId = view.SelectedReason.Key;
                    }
                    else
                    {
                        resume = false;
                    }
                }
            }

            // Комплектуем
            if (resume)
            {
                SplashControl.Visible = true;

                int newPalletCount = 0;
                List<Dictionary<string, string>> newPalletGridItems = new List<Dictionary<string, string>>();
                if (NewPalletGrid.Items != null)
                {
                    newPalletCount = NewPalletGrid.Items.Count;
                    newPalletGridItems = NewPalletGrid.Items;
                }

                var p = new Dictionary<string, string>
                {
                    ["Product"] = JsonConvert.SerializeObject(SelectedProductItem),
                    ["OldPalletList"] = JsonConvert.SerializeObject(OldPalletGrid.Items),
                    ["NewPalletList"] = JsonConvert.SerializeObject(newPalletGridItems),
                    ["idorderdates"] = OrderId.ToString(),
                    ["StanokId"] = ComplectationPlace.ProcessingMachinesKsh,
                    ["ReasonId"] = reasonId,
                    ["ReasonMessage"] = reasonMessage
                };

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Complectation");
                q.Request.SetParam("Object", "Pallet");
                q.Request.SetParam("Action", "CreateProcessingMachineKsh");

                q.Request.SetParams(p);

                await Task.Run(() => { q.DoQuery(); });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                        {
                            var idpz = ds.Items.First().CheckGet("idpz");
                            if (summaryQuantityOnNewPallet > 0 || idpz != "0")
                            {
                                // отправить сообщение
                                Messenger.Default.Send(new ItemMessage
                                {
                                    ReceiverGroup = "ComplectationKsh",
                                    ReceiverName = "ComplectationListKsh",
                                    SenderName = "ComplectationMainComplectationTab",
                                    Action = "Refresh",
                                });

                                // отправить сообщение
                                Messenger.Default.Send(new ItemMessage
                                {
                                    ReceiverGroup = "ComplectationKsh",
                                    ReceiverName = _ParentFrame,
                                    SenderName = "ComplectationMainComplectationTab",
                                    Action = "Refresh",
                                });

                                // печать ярлыков
                                for (var i = 1; i <= newPalletCount; i++)
                                {
                                    LabelReport2 report = new LabelReport2(true);
                                    report.PrintLabel(idpz, i.ToString(), SelectedProductItem.CheckGet("IDK1"));
                                }
                            }
                            else
                            {
                                Messenger.Default.Send(new ItemMessage
                                {
                                    ReceiverGroup = "ComplectationKsh",
                                    ReceiverName = "ComplectationWriteOffListKsh",
                                    SenderName = "ComplectationMainComplectationTab",
                                    Action = "Refresh",
                                });

                                // отправить сообщение
                                Messenger.Default.Send(new ItemMessage
                                {
                                    ReceiverGroup = "ComplectationKsh",
                                    ReceiverName = _ParentFrame,
                                    SenderName = "ComplectationMainComplectationTab",
                                    Action = "Refresh",
                                });

                                DialogWindow.ShowDialog("Списание выполнено");
                            }

                            Close();
                        }
                        else
                        {
                            var msg = "На сервере произошла ошибка. Пожалуйста сообщите о проблеме.";
                            var d = new DialogWindow($"{msg}", "Комплектация ПР КШ", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                    else
                    {
                        var msg = "На сервере произошла ошибка. Пожалуйста сообщите о проблеме.";
                        var d = new DialogWindow($"{msg}", "Комплектация ПР КШ", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            SplashControl.Visible = false;
            EnableControls();
        }

        /// <summary>
        /// Проверка продукции на остатках
        /// (чтобы избежать ошибок в процедуре SETPROD)
        /// </summary>
        public bool CheckBalance()
        {
            bool result = true;

            if (OldPalletGrid != null && OldPalletGrid.Items != null && OldPalletGrid.Items.Count > 0)
            {
                string msg = "";

                // 1 -- Проверяем по tovar_in_otdel
                {
                    // Поддоны, сгруппированные по отделам (id1), в которых они числятся
                    var palletOfDepartment = OldPalletGrid.Items.GroupBy(x => x.CheckGet("ID1").ToInt()).Select(group => new { Department = group.Key, Pallets = group.ToList() }).ToList();
                    if (palletOfDepartment != null && palletOfDepartment.Count > 0)
                    {
                        foreach (var item in palletOfDepartment)
                        {
                            int counsumptionQuantityOfDepartment = item.Pallets.Sum(x => x.CheckGet("KOL").ToInt());
                            int balanceQuantityOfDepartment = 0;
                            string departmentName = "";

                            var p = new Dictionary<string, string>();
                            p.Add("PRODUCT_ID", item.Pallets.First().CheckGet("ID2"));
                            p.Add("DEPARTMENT_ID", item.Department.ToString());

                            var q = new LPackClientQuery();
                            q.Request.SetParam("Module", "Complectation");
                            q.Request.SetParam("Object", "Product");
                            q.Request.SetParam("Action", "GetBalanceInDepartment");
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
                                        balanceQuantityOfDepartment = dataSet.Items.First().CheckGet("QUANTITY_IN_DEPARTMENT").ToInt();
                                        departmentName = dataSet.Items.First().CheckGet("DEPARTMENT_NAME");
                                    }
                                }
                            }
                            else
                            {
                                msg = "Во время проверки данных перед комплектацией произошла ошибка. Пожалуйста, повторите операцию.";
                                var d = new DialogWindow($"{msg}", "Комплектация", "", DialogWindowButtons.OK);
                                d.ShowDialog();

                                return false;
                            }

                            if (balanceQuantityOfDepartment < counsumptionQuantityOfDepartment)
                            {
                                result = false;

                                string palletList = "";
                                foreach (var pallet in item.Pallets)
                                {
                                    palletList = $"{palletList} [{pallet.CheckGet("PALLET")}]";
                                }

                                msg = $"{msg}Отдел: {departmentName}.{Environment.NewLine}" +
                                    $"Поддоны в отделе: {palletList}.{Environment.NewLine}" +
                                    $"Количество на списываемых поддонах: {counsumptionQuantityOfDepartment}.{Environment.NewLine}" +
                                    $"Количество в отделе: {balanceQuantityOfDepartment}.{Environment.NewLine}" +
                                    $"---{Environment.NewLine}";
                            }
                        }

                        if (!result)
                        {
                            msg = $"Внимание! Невозможно продолжить комплектацию{Environment.NewLine}" +
                                $"Несоответствие количества списываемой продукции и остатков этой продукции в отделе.{Environment.NewLine}" +
                                $"---{Environment.NewLine}" +
                                $"{msg}";
                            var d = new DialogWindow($"{msg}", "Комплектация", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Открытие excel файла текарты по выбранной продукции
        /// </summary>
        public void OpenTechnologicalMap()
        {
            if (SelectedProductItem != null)
            {
                string pathTk = SelectedProductItem.CheckGet("PATHTK");

                if (!string.IsNullOrEmpty(pathTk))
                {
                    if (System.IO.File.Exists(pathTk))
                    {
                        Central.OpenFile(pathTk);
                    }
                    else
                    {
                        var msg = $"Файл {pathTk} не найден по указанному пути";
                        var d = new DialogWindow($"{msg}", "Комплектация Переработка", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    var msg = "Не найден путь к Excel файлу тех карты";
                    var d = new DialogWindow($"{msg}", "Комплектация Переработка", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                var msg = "Не выбран товар, для которого нужно найти тех карту";
                var d = new DialogWindow($"{msg}", "Комплектация Переработка", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Комплектация СГП
        /// </summary>
        public async void ComplectationStock()
        {
            var resume = true;
            DisableControls();

            // Количество списываемых поддонов
            int oldPalletCount = OldPalletGrid.Items.Count;
            // Суммарное количество картона на списываемых поддонах
            int summaryQuantityOnOldPallet = OldPalletGrid.Items.Sum(x => x.CheckGet("KOL").ToInt());
            // Количество новых поддонов
            int newPalletCount = NewPalletGrid.Items.Count;
            // Суммарное количество картона на новых поддонах
            int summaryQuantityOnNewPallet = NewPalletGrid.Items.Sum(x => x.CheckGet("QTY").ToInt());
            // Ид причины комплектации/списания
            var reasonId = "0";
            // Описание причины комплектации/списания
            var reasonMessage = "";

            // проверяем что не пытаются сделать товара больше чем есть
            if (summaryQuantityOnNewPallet > summaryQuantityOnOldPallet)
            {
                if (MasterFlag)
                {
                    resume = DialogWindow.ShowDialog(
                                                   "Вы мастер СГП. Внимание вы комплектуете товара больше чем было до комплектации. Продолжить?",
                                                   "Предупреждение", "", DialogWindowButtons.NoYes) == true;
                }
                else
                {
                    DialogWindow.ShowDialog(
                                            "Нельзя скомплектовать больше товара чем было до комплектации. Уменьшите суммарное количество получаемого товара",
                                            "Ошибка");
                    resume = false;
                }
            }

            // Отграничение на комплектацию одного поддона с тем же количеством, что было до комплектации
            if (resume)
            {
                if (oldPalletCount == 1 && newPalletCount == 1)
                {
                    if (summaryQuantityOnOldPallet == summaryQuantityOnNewPallet)
                    {
                        DialogWindow.ShowDialog(
                            $"Комплектация одного поддона без изменения количества запрещена. {Environment.NewLine}Измените количество поддонов или товара.",
                            "Ошибка");
                        resume = false;
                    }
                }
            }

            // указываем причину комплектации/списания
            if (resume)
            {
                // Если есть новые поддоны, то указываем причину комплектации
                if (newPalletCount > 0)
                {
                    var сomplectationReasonsEdit = new ComplectationReasonsEdit();
                    сomplectationReasonsEdit.StockFlag = 1;
                    сomplectationReasonsEdit.Show();

                    if (сomplectationReasonsEdit.OkFlag)
                    {
                        reasonId = сomplectationReasonsEdit.SelectedReason.Key;
                        reasonMessage = сomplectationReasonsEdit.ReasonMessage;
                    }
                    else
                    {
                        resume = false;
                    }
                }
                // Если нет, то указываем причину списания
                else
                {
                    var сomplectationWriteOffReasonsEdit = new ComplectationWriteOffReasonsEdit();
                    сomplectationWriteOffReasonsEdit.StockFlag = 1;
                    сomplectationWriteOffReasonsEdit.Show();

                    if (сomplectationWriteOffReasonsEdit.OkFlag)
                    {
                        reasonId = сomplectationWriteOffReasonsEdit.SelectedReason.Key;
                    }
                    else
                    {
                        resume = false;
                    }
                }
            }

            // Запрашиваем финальное подтверждение
            if (resume)
            {
                var message =
                            $"Будет списано {Environment.NewLine}" +
                            $"{ConsumptionQuantityTextBox.Text} товара на {OldPalletGrid.Items.Count} поддонах{Environment.NewLine}";

                if (summaryQuantityOnNewPallet > 0)
                {
                    message +=
                            $"Будет скомплектовано {Environment.NewLine}" +
                            $"{summaryQuantityOnNewPallet} товара на {NewPalletGrid.Items.Count} поддонах.{Environment.NewLine}";
                }
                else
                {
                    message += $"в брак.{Environment.NewLine}";
                }

                message += "Продолжить?";

                if (DialogWindow.ShowDialog(message, "Предупреждение", "", DialogWindowButtons.YesNo) != true)
                {
                    resume = false;
                }
            }

            // Проверка заявок
            var view = new ComplectationStockParamsEdit { ProductId = Form.GetValueByPath("PRODUCT_ID2") };
            if (resume)
            {
                view.Show();

                if (!view.OkFlag)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                SplashControl.Visible = true;

                var p = new Dictionary<string, string>
                {
                    ["id2_current"] = Form.GetValueByPath("PRODUCT_ID2"),
                    ["OldPalletList"] = JsonConvert.SerializeObject(OldPalletGrid.Items),
                    ["NewPalletList"] = JsonConvert.SerializeObject(NewPalletGrid.Items),
                    ["idorderdates"] = view.SelectedValue?["IDORDERDATES"],
                    ["ReasonId"] = reasonId,
                    ["ReasonMessage"] = reasonMessage
                };

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Complectation");
                q.Request.SetParam("Object", "Pallet");
                q.Request.SetParam("Action", "CreateStkNew");

                q.Request.SetParams(p);

                await Task.Run(() => { q.DoQuery(); });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds.Items.Count > 0)
                        {
                            var idpz = ds.Items.First().CheckGet("idpz");
                            var idk1 = ds.Items.First().CheckGet("IDK1");

                            if (summaryQuantityOnNewPallet > 0 || idpz != "0")
                            {
                                // отправить сообщение списку комплектаций обновиться текущей датой
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "Complectation",
                                    ReceiverName = "Stock",
                                    SenderName = "ComplectationMainComplectationTab",
                                    Action = "Refresh",
                                });

                                // печать ярлыков
                                for (var i = 1; i <= newPalletCount; i++)
                                {
                                    LabelReport2 report = new LabelReport2(true);
                                    report.PrintLabel(idpz, i.ToString(), idk1);
                                }
                            }
                            else
                            {
                                Messenger.Default.Send(new ItemMessage
                                {
                                    ReceiverGroup = "Complectation",
                                    ReceiverName = "WriteOff",
                                    SenderName = "ComplectationMainComplectationTab",
                                    Action = "Refresh",
                                });

                                DialogWindow.ShowDialog("Списание выполнено");
                            }

                            // отправить сообщение
                            Messenger.Default.Send(new ItemMessage
                            {
                                ReceiverGroup = "Complectation",
                                ReceiverName = "Stock",
                                SenderName = "ComplectationMainComplectationTab",
                                Action = "Refresh",
                            });

                            Close();
                        }
                        else
                        {
                            var msg = "На сервере произошла ошибка. Пожалуйста сообщите о проблеме.";
                            var d = new DialogWindow($"{msg}", "Комплектация СГП", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                    else
                    {
                        var msg = "На сервере произошла ошибка. Пожалуйста сообщите о проблеме.";
                        var d = new DialogWindow($"{msg}", "Комплектация СГП", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            SplashControl.Visible = false;
            EnableControls();
        }

        /// <summary>
        /// Комплектация ЛТ
        /// </summary>
        public async void ComplectationMoldedContainer()
        {
            var resume = true;
            DisableControls();

            // Проверяем что не пытаются сделать товара больше чем есть
            int summaryQuantityOnOldPallet = OldPalletGrid.Items.Sum(x => x.CheckGet("KOL").ToInt());
            int summaryQuantityOnNewPallet = 0;
            if (NewPalletGrid.Items != null)
            {
                summaryQuantityOnNewPallet = NewPalletGrid.Items.Sum(x => x.CheckGet("QTY").ToInt());
            }

            // Количество списываемых поддонов
            int oldPalletCount = OldPalletGrid.Items.Count;

            // Количество новых поддонов
            int newPalletCount = 0;
            if (NewPalletGrid.Items != null)
            {
                newPalletCount = NewPalletGrid.Items.Count;
            }

            if (summaryQuantityOnOldPallet < summaryQuantityOnNewPallet)
            {
                DialogWindow.ShowDialog("Нельзя скомплектовать больше товара чем было до комплектации. Уменьшите суммарное количество получаемого товара", this.FrameTitle);
                resume = false;
            }

            // Запрашиваем подтверждение операции
            if (resume)
            {
                var message =
                            $"Будет списано {Environment.NewLine}" +
                            $"{ConsumptionQuantityTextBox.Text} товара на {oldPalletCount} поддонах{Environment.NewLine}";

                if (summaryQuantityOnNewPallet > 0)
                {
                    message +=
                            $"Будет скомплектовано {Environment.NewLine}" +
                            $"{summaryQuantityOnNewPallet} товара на {newPalletCount} поддонах.{Environment.NewLine}";
                }
                else
                {
                    message += $"в брак.{Environment.NewLine}";
                }

                message += "Продолжить?";

                if (DialogWindow.ShowDialog(message, this.FrameTitle, "", DialogWindowButtons.YesNo) != true)
                {
                    resume = false;
                }
            }

            // Ид причины комплектации/списания
            var reasonId = "0";
            // Описание причины комплектации/списания
            var reasonMessage = "";

            // Запрашиваем причину комплектации/списания
            if (resume)
            {
                // Если обычныя комплектация, то выводим список причин комплектации
                if (summaryQuantityOnNewPallet > 0)
                {
                    var сomplectationReasonsEdit = new ComplectationReasonsEdit();
                    сomplectationReasonsEdit.MoldedContainerFlag = 1;
                    сomplectationReasonsEdit.Show();

                    if (сomplectationReasonsEdit.OkFlag)
                    {
                        reasonId = сomplectationReasonsEdit.SelectedReason.Key;
                        reasonMessage = сomplectationReasonsEdit.ReasonMessage;
                    }
                }
                // Если списание, то выводим список причин списания
                else
                {
                    var view = new ComplectationWriteOffReasonsEdit();
                    view.MoldedContainerFlag = 1;
                    view.Show();

                    if (view.OkFlag)
                    {
                        reasonId = view.SelectedReason.Key;
                    }
                }

                if (reasonId.ToInt() > 0)
                {
                    resume = true;
                }
                else
                {
                    resume = false;
                }
            }

            // Комплектуем
            if (resume)
            {
                SplashControl.Visible = true;

                if (oldPalletCount > 0)
                {
                    if (Form.GetValueByPath("PRODUCT_ID2") == OldPalletGrid.Items.First().CheckGet("ID2"))
                    {
                        List<Dictionary<string, string>> newPalletGridItems = new List<Dictionary<string, string>>();
                        if (NewPalletGrid.Items != null)
                        {
                            newPalletGridItems = NewPalletGrid.Items;
                        }

                        var p = new Dictionary<string, string>
                        {
                            ["Product"] = JsonConvert.SerializeObject(SelectedProductItem),
                            ["OldPalletList"] = JsonConvert.SerializeObject(OldPalletGrid.Items),
                            ["NewPalletList"] = JsonConvert.SerializeObject(newPalletGridItems),

                            ["idorderdates"] = OrderId.ToString(),

                            ["StanokId"] = ComplectationPlace.MoldedContainer,
                            ["ReasonId"] = reasonId,
                            ["ReasonMessage"] = reasonMessage
                        };

                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Complectation");
                        q.Request.SetParam("Object", "Pallet");
                        q.Request.SetParam("Action", "CreateMoldedContainer");

                        q.Request.SetParams(p);

                        await Task.Run(() => { q.DoQuery(); });

                        if (q.Answer.Status == 0)
                        {
                            var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                            if (result != null)
                            {
                                var ds = ListDataSet.Create(result, "ITEMS");
                                if (ds.Items.Count > 0)
                                {
                                    var idpz = ds.Items.First().CheckGet("idpz");
                                    if (summaryQuantityOnNewPallet > 0 || idpz != "0")
                                    {
                                        // отправить сообщение
                                        Messenger.Default.Send(new ItemMessage
                                        {
                                            ReceiverGroup = "Complectation",
                                            ReceiverName = "Stock",
                                            SenderName = "ComplectationMainComplectationTab",
                                            Action = "Refresh",
                                        });

                                        // отправить сообщение
                                        Messenger.Default.Send(new ItemMessage
                                        {
                                            ReceiverGroup = "Complectation",
                                            ReceiverName = "ProductionComplectationMoldedContainer",
                                            SenderName = "ComplectationMainComplectationTab",
                                            Action = "Refresh",
                                        });

                                        // печать ярлыков
                                        for (var i = 1; i <= newPalletCount; i++)
                                        {
                                            LabelReport2 report = new LabelReport2(true);
                                            report.PrintLabel(idpz, i.ToString(), Form.GetValueByPath("PRODUCT_IDK1"));
                                        }
                                    }

                                    Close();
                                }
                                else
                                {
                                    var msg = "На сервере произошла ошибка. Пожалуйста сообщите о проблеме.";
                                    var d = new DialogWindow($"{msg}", this.FrameTitle, "", DialogWindowButtons.OK);
                                    d.ShowDialog();
                                }
                            }
                            else
                            {
                                var msg = "На сервере произошла ошибка. Пожалуйста сообщите о проблеме.";
                                var d = new DialogWindow($"{msg}", this.FrameTitle, "", DialogWindowButtons.OK);
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
                        var msg = $"Продукция на поддонах не совпадает с выбранной. Пожалуйста, нажмите кнопку \"Отмена\" и повторите операцию.";
                        var d = new DialogWindow($"{msg}", this.FrameTitle, "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    var msg = $"Необходимо выбрать хотя бы один поддон из которого будем комплектовать.";
                    var d = new DialogWindow($"{msg}", this.FrameTitle, "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }

            SplashControl.Visible = false;
            EnableControls();
        }

        /// <summary>
        /// Установка настроек для принтера
        /// </summary>
        public void SetPrintSettings()
        {
            LabelReport2.SetPrintingProfile();
        }

        private void NewPalletAddButton_Click(object sender, RoutedEventArgs e)
        {
            NewPalletAdd();
        }

        private void NewPalletEditButton_Click(object sender, RoutedEventArgs e)
        {
            NewPalletEdit();
        }

        private void NewPalletDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            NewPalletDelete();
        }

        private void ConsumptionQuantityTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateDefectiveQuantity();
        }

        private void IncomingQuantityTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateDefectiveQuantity();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ComplectationButton_Click(object sender, RoutedEventArgs e)
        {
            Complectation();
        }

        private void OpenTechnologicalMapButton_Click(object sender, RoutedEventArgs e)
        {
            OpenTechnologicalMap();
        }

        private void WriteOffButton_Click(object sender, RoutedEventArgs e)
        {
            WriteOff();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings();
        }

        private void GetOrderDataButton_Click(object sender, RoutedEventArgs e)
        {
            GetOrderData();
        }
    }
}
