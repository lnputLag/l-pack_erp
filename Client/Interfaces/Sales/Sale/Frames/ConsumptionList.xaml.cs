using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Production;
using Client.Interfaces.Stock;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
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
using static NPOI.SS.Formula.PTG.ArrayPtg;

namespace Client.Interfaces.Sales
{
    /// <summary>
    /// Список позиций расхода для выбранной накладной
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class ConsumptionList : UserControl
    {
        /// <summary>
        /// Обязательные к заполнению переменные:
        /// InvoiceId;
        /// ReportingPeriod.
        /// Не обязательные к заполнению переменные:
        /// ParentFrame.
        /// SignotoryEmployeeId
        /// SignotoryEmployeeName
        /// </summary>
        public ConsumptionList()
        {
            InitializeComponent();
            FrameName = "ConsumptionList";

            if (Central.DebugMode)
            {
                BuyerIdTextBox.Visibility = Visibility.Visible;
                ConsigneeIdTextBox.Visibility = Visibility.Visible;
            }

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            InitForm();
            InitGrid();
            SetDefaults();
            ProcessPermissions();
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Основной датасет с данными по позициям
        /// </summary>
        public ListDataSet GridDataSet { get; set; }

        /// <summary>
        /// Выбранная запись в гриде
        /// </summary>
        public Dictionary<string, string> GridSelectedItem { get; set; }

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// Идентификатор накладной расхода
        /// naklrashod.nsthet
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
        /// Данные по выбранному покупателю
        /// </summary>
        public Dictionary<string, string> SelectedBuyerData { get; set; }

        /// <summary>
        /// Идентификатор сотрудника - подписанта
        /// </summary>
        public int SignotoryEmployeeId { get; set; }

        /// <summary>
        /// Полное имя сотрудника - подписанта
        /// </summary>
        public string SignotoryEmployeeName { get; set; }

        public string RoleName = "[erp]sales_manager";

        public ListDataSet ContractDataSet { get; set; }

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
                        Path = "INVOICE_ID",
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = null,
                        ControlType = "void",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "BUYER_ID",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = BuyerIdTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "ADJUSTMENT_ID",
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = null,
                        ControlType = "void",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "INVOICE_TYPE",
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = null,
                        ControlType = "void",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },

                    new FormHelperField()
                    {
                        Path = "SELLER_ID",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = SellerSelectBox,
                        ControlType = "SelectBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "SHIPPER_ID",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = ShipperSelectBox,
                        ControlType = "SelectBox",
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
                        Path = "RECEIPT_DATE",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = ReceiptDateTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
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
                        Path = "SHIPPING_DATE",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = ShippingDateTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },

                    new FormHelperField()
                    {
                        Path = "INVOICE",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = InvoiceTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "WAYBILL",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = WaybillTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "CONSIGNMENT_NOTE",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = ConsignmentNoteTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "DEFECT",
                        FieldType = FormHelperField.FieldTypeRef.Boolean,
                        Control = DefectCheckBox,
                        ControlType = "CheckBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "INVOICE_COMMENT",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = CommentTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                            { FormHelperField.FieldFilterRef.MaxLen, 100 },
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "SHIFT_ID",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = ShiftSelectBox,
                        ControlType = "SelectBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "BUYER_NAME",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = BuyerNameTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "CONSIGNEE_NAME",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = ConsigneeNameTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "CONSIGNEE_ID",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = ConsigneeIdTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "CONTRACT_ID",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = ContractSelectBox,
                        ControlType = "SelectBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },

                    new FormHelperField()
                    {
                        Path = "ADJUSTMENT_NUMBER",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = AdjustmentNumberTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "ADJUSTMENT_DATE",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = AdjustmentDateTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                };

                Form.SetFields(fields);

                // Колбек стандартной валидации
                // Также вызывается при изменении данных в TextBox
                Form.OnValidate = (valid, message) =>
                {
                    ValidateContract();
                };

                Form.BeforeSet = (Dictionary<string, string> values) => 
                {
                    if (!string.IsNullOrEmpty(values.CheckGet("SHIPPER_ID")))
                    {
                        values.CheckAdd("SHIPPER_ID", values.CheckGet("SHIPPER_ID").ToInt().ToString());
                    }

                    if (!string.IsNullOrEmpty(values.CheckGet("FACTORY_ID")))
                    {
                        values.CheckAdd("FACTORY_ID", values.CheckGet("FACTORY_ID").ToInt().ToString());
                    }
                };
            }
        }

        public void SetDefaults()
        {
            GridSelectedItem = new Dictionary<string, string>();
            GridDataSet = new ListDataSet();
            SelectedBuyerData = new Dictionary<string, string>();
            ContractDataSet = new ListDataSet();

            Form.SetDefaults();

            var factorySelectBoxItems = new Dictionary<string, string>();
            factorySelectBoxItems.Add("1", "Л-ПАК ЛИПЕЦК");
            factorySelectBoxItems.Add("2", "Л-ПАК КАШИРА");
            FactorySelectBox.SetItems(factorySelectBoxItems);

            var sellerSelectBoxItems = new Dictionary<string, string>();
            sellerSelectBoxItems.Add("1", "ООО \"Торговый Дом Л-ПАК\"");
            sellerSelectBoxItems.Add("2", "ООО \"Л-ПАК\"");
            sellerSelectBoxItems.Add("427", "ООО \"Л-ПАК Кашира\"");
            sellerSelectBoxItems.Add("0", "ООО \"Л-Пак\"");
            SellerSelectBox.SetItems(sellerSelectBoxItems);

            var shipperSelectBoxItems = new Dictionary<string, string>();
            shipperSelectBoxItems.Add("434", "ОП ООО \"Торговый Дом Л-ПАК \"Л\"\"");
            shipperSelectBoxItems.Add("2", "ООО \"Л-ПАК\"");
            shipperSelectBoxItems.Add("427", "ООО \"Л-ПАК Кашира\"");
            shipperSelectBoxItems.Add("435", "ООО \"ЛОГИСТИКА ДЛЯ ВАС\"");
            shipperSelectBoxItems.Add("0", "ООО \"Л-Пак\"");
            ShipperSelectBox.SetItems(shipperSelectBoxItems);
        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel(this.RoleName);
            var userAccessMode = mode;
            switch (mode)
            {
                case Role.AccessMode.Special:
                    break;

                case Role.AccessMode.FullAccess:
                    break;

                case Role.AccessMode.ReadOnly:
                default:
                    break;
            }

            List<Button> buttons = UIUtil.GetVisualChilds<Button>(this.Content as DependencyObject);
            if (buttons != null && buttons.Count > 0)
            {
                foreach (var button in buttons)
                {
                    var buttonTagList = UIUtil.GetTagList(button);
                    var accessMode = Acl.FindTagAccessMode(buttonTagList);
                    if (accessMode > userAccessMode)
                    {
                        button.IsEnabled = false;
                    }
                }
            }

            if (Grid != null && Grid.Menu != null && Grid.Menu.Count > 0)
            {
                foreach (var manuItem in Grid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void InitGrid()
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
                        Width2 = 2,
                        Editable = true,
                        OnClickAction = (row, el) =>
                        {
                            UpdateActions();

                            return true;
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=2,
                        TotalsType = TotalsTypeRef.Count,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Path="CONSUMPTION_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
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
                        Header="Наименование продукции",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=34,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ поддона",
                        Path="PALLET_FULL_NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ рулона",
                        Path="ROLL_NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Path="QUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                        TotalsType = TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Единица измерения",
                        Path="IZM_FIRST_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=2,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Цена",
                        Path="PRICE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Сумма",
                        Path="SUMMARY_PRICE",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=10,
                        Format="N2",
                        TotalsType = TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="НДС",
                        Path="CUSTOMER_VAT",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=2,
                        Format="N2",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Площадь на поддоне",
                        Path="SQUARE",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=8,
                        TotalsType = TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Площадь изделия",
                        Path="PRODUCT_SQUARE",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Диаметр рулона",
                        Path="ROLL_DIAMETER",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Проведен",
                        Path="COMPLETED_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=2,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Потребитель",
                        Path="CUSTOMER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=30,
                    },                 
                    new DataGridHelperColumn
                    {
                        Header="Артикул потребителя",
                        Path="PRODUCT_CUSTOMER_CODE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид прихода",
                        Path="INCOMING_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид продукции",
                        Path="PRODUCT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Код товара",
                        Path="PRODUCT_KOD",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=59,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ задания",
                        Path="PRODUCTION_TASK_NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=40,
                        MaxWidth=80,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ поддона",
                        Path="PALLET_NUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=40,
                        MaxWidth=85,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид категории продукции",
                        Path="PRODUCT_IDK1",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=40,
                        MaxWidth=42,
                        Hidden=true,
                    }, 
                    new DataGridHelperColumn
                    {
                        Header="Ид покупателя",
                        Path="CUSTOMER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=40,
                        MaxWidth=42,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид ПЗ",
                        Path="PRODUCTION_TASK_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=40,
                        MaxWidth=42,
                        Hidden=true,
                    },
                };
                Grid.SetColumns(columns);
                Grid.SearchText = SearchText;
                Grid.SetPrimaryKey("CONSUMPTION_ID");
                Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

                Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
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

                //при выборе строки в гриде, обновляются актуальные действия для записи
                Grid.OnSelectItem = selectedItem =>
                {
                    GridSelectedItem = selectedItem;
                    UpdateActions();
                };

                // контекстное меню
                Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    {
                        "AddConsumption",
                        new DataGridContextMenuItem()
                        {
                            Header="Добавить позицию",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                AddConsumption();
                            }
                        }
                    },
                    {
                        "EditConsumption",
                        new DataGridContextMenuItem()
                        {
                            Header="Изменить выбранную позицию",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                EditConsumption();
                            }
                        }
                    },
                    {
                        "DeleteConsumption",
                        new DataGridContextMenuItem()
                        {
                            Header="Удалить выбранную позицию",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                DeleteOneConsumption(GridSelectedItem);
                            }
                        }
                    },
                    { "s0", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    {
                        "ConfirmConsumption",
                        new DataGridContextMenuItem()
                        {
                            Header="Провести выбранную позицию",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                ConfirmOneConsumption(GridSelectedItem);
                            }
                        }
                    },
                    {
                        "UnconfirmConsumption",
                        new DataGridContextMenuItem()
                        {
                            Header="Отменить проведение выбранной позиции",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                UnconfirmOneConsumption(GridSelectedItem);
                            }
                        }
                    },
                    { "s1", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    {
                        "DividingPallet",
                        new DataGridContextMenuItem()
                        {
                            Header="Разделить поддон на 2",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                DividingPallet();
                            }
                        }
                    },
                    {
                        "MoveToOtherDocument",
                        new DataGridContextMenuItem()
                        {
                            Header="Перенести в другую накладную выбранные позиции",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                PrepareToMoveToOtherDocument();
                            }
                        }
                    },
                    { "s2", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    {
                        "Confirm",
                        new DataGridContextMenuItem()
                        {
                            Header="Провести все позиции по накладной",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                Confirm();
                            }
                        }
                    },
                    {
                        "Unconfirm",
                        new DataGridContextMenuItem()
                        {
                            Header="Отменить проведение всех позиций по накладной",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                Unconfirm();
                            }
                        }
                    },
                    { "s3", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    {
                        "OpenAdjustment",
                        new DataGridContextMenuItem()
                        {
                            Header="Корректировка",
                            Action=()=>
                            {
                                OpenAdjustment();
                            }
                        }
                    },
                    { "s4", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    {
                        "PrintLabel",
                        new DataGridContextMenuItem()
                        {
                            Header="Ярлык",
                            Action=()=>
                            {
                                PrintOneLabel(GridSelectedItem);
                            }
                        }
                    },
                };

                Grid.Init();
                Grid.Run();
            }
        }

        /// <summary>
        /// Получение данных для заполнения грида
        /// </summary>
        public async void LoadConsumptionItems()
        {
            if (InvoiceId > 0)
            {
                GridDisableControls();

                var p = new Dictionary<string, string>();
                p.Add("INVOICE_ID", InvoiceId.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "Sale");
                q.Request.SetParam("Action", "ListConsumption");
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
                        GridDataSet = ListDataSet.Create(result, "ITEMS");
                        if (GridDataSet != null && GridDataSet.Items!= null && GridDataSet.Items.Count > 0)
                        {
                            var decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

                            foreach (var item in GridDataSet.Items)
                            {
                                try
                                {
                                    if (!string.IsNullOrEmpty(item.CheckGet("PRICE")))
                                    {
                                        item.CheckAdd("PRICE", item.CheckGet("PRICE").Replace(".", ","));

                                        decimal priceByOne = decimal.Parse(item.CheckGet("PRICE").Replace(",", decimalSeparator));
                                        int quantityProduct = item.CheckGet("QUANTITY").ToInt();

                                        decimal summaryPrice = priceByOne * quantityProduct;
                                        summaryPrice = Math.Round(summaryPrice * 100, MidpointRounding.AwayFromZero) / 100;
                                        item.CheckAdd("SUMMARY_PRICE", summaryPrice.ToString());
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // msg
                                }

                                // Сохранение статуса CHECKED_FLAG у отмеченных строк при обновлении грида
                                if (Grid.Items != null)
                                {
                                    var row = Grid.Items.FirstOrDefault(x => x.CheckGet("CONSUMPTION_ID").ToInt() == item.CheckGet("CONSUMPTION_ID").ToInt());
                                    if (row != null)
                                    {
                                        item.CheckAdd("CHECKED_FLAG", row.CheckGet("CHECKED_FLAG"));
                                    }
                                }
                            }
                        }
                        Grid.UpdateItems(GridDataSet);
                    }
                }
                else
                {
                    q.ProcessError();
                }

                GridEnableControls();
            }
        }

        public async void LoadInvoiceData()
        {
            if (InvoiceId > 0)
            {
                FormDisableControls();

                var p = new Dictionary<string, string>();
                p.Add("INVOICE_ID", InvoiceId.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "Sale");
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
                            if (Form.GetValueByPath("INVOICE_TYPE").ToInt() == 3)
                            {
                                Form.SetValueByPath("DEFECT", "1");
                            }
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }

                FormEnableControls();
            }
        }

        public void LoadData()
        {
            LoadInvoiceData();
            LoadConsumptionItems();
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
                //InvoiceTextBox.IsReadOnly = false;
                //WaybillTextBox.IsReadOnly = false;

                LoadData();
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "CONSIGNMENT_NOTE"), FormHelperField.FieldFilterRef.Required);

                Central.WM.Show(FrameName, $"Детализация накладной {InvoiceId}", true, "add", this);
            }
            else
            {
                //InvoiceTextBox.IsReadOnly = true;
                //WaybillTextBox.IsReadOnly = true;

                Form.SetValueByPath("RECEIPT_DATE", DateTime.Now.ToString("dd.MM.yyyy"));

                Central.WM.Show(FrameName, $"Детализация новой накладной", true, "add", this);
            }
        }

        /// <summary>
        /// Обновляем доступные действия
        /// </summary>
        public void UpdateActions()
        {
            SaveButton.IsEnabled = false;

            //ReceiptDateDatePicker.IsEnabled = false;
            ReceiptDateTextBox.IsEnabled = false;
            //InvoiceDateDatePicker.IsEnabled = false;
            InvoiceDateTextBox.IsEnabled = false;
            //ShippingDateDatePicker.IsEnabled = false;
            ShippingDateTextBox.IsEnabled = false;

            AddConsumptionButton.IsEnabled = false;
            EditConsumptionButton.IsEnabled = false;
            DeleteConsumptionButton.IsEnabled = false;
            AdjustmentButton.IsEnabled = false;
            ConfirmSelectedConsumptionButton.IsEnabled = false;
            UnconfirmSelectedConsumptionButton.IsEnabled = false;
            UpdatePayDebtBuyerButton.IsEnabled = false;
            PrintDocumentButton.IsEnabled = false;
            UploadWebDocumentButton.IsEnabled = false;
            PrintLabelButton.IsEnabled = false;

            FactorySelectBox.IsEnabled = false;

            Grid.Menu["AddConsumption"].Enabled = false;
            Grid.Menu["EditConsumption"].Enabled = false;
            Grid.Menu["DeleteConsumption"].Enabled = false;
            Grid.Menu["ConfirmConsumption"].Enabled = false;
            Grid.Menu["UnconfirmConsumption"].Enabled = false;
            Grid.Menu["Confirm"].Enabled = false;
            Grid.Menu["Unconfirm"].Enabled = false;
            Grid.Menu["DividingPallet"].Enabled = false;
            Grid.Menu["MoveToOtherDocument"].Enabled = false;
            Grid.Menu["OpenAdjustment"].Enabled = false;
            Grid.Menu["PrintLabel"].Enabled = false;

            if (!string.IsNullOrEmpty(ReportingPeriod))
            {
                bool inReportingPeriod = Form.GetValueByPath("SHIPPING_DATE").ToDateTime() >= ReportingPeriod.ToDateTime();

                if (InvoiceId > 0)
                {
                    PrintDocumentButton.IsEnabled = true;
                    UploadWebDocumentButton.IsEnabled = true;

                    AdjustmentButton.IsEnabled = true;
                    Grid.Menu["OpenAdjustment"].Enabled = true;

                    // Если дата в отчётном периоде
                    if (inReportingPeriod)
                    {
                        AddConsumptionButton.IsEnabled = true;
                        Grid.Menu["AddConsumption"].Enabled = true;
                        Grid.Menu["Confirm"].Enabled = true;
                        Grid.Menu["Unconfirm"].Enabled = true;

                        UpdatePayDebtBuyerButton.IsEnabled = true;

                        if (GridSelectedItem != null && GridSelectedItem.Count > 0)
                        {
                            Grid.Menu["EditConsumption"].Enabled = true;
                            Grid.Menu["DeleteConsumption"].Enabled = true;
                            EditConsumptionButton.IsEnabled = true;
                            DeleteConsumptionButton.IsEnabled = true;

                            Grid.Menu["MoveToOtherDocument"].Enabled = true;

                            ConfirmSelectedConsumptionButton.IsEnabled = true;
                            UnconfirmSelectedConsumptionButton.IsEnabled = true;

                            if (GridSelectedItem.CheckGet("COMPLETED_FLAG").ToInt() > 0)
                            {
                                Grid.Menu["UnconfirmConsumption"].Enabled = true;
                            }
                            else
                            {
                                Grid.Menu["ConfirmConsumption"].Enabled = true;
                                Grid.Menu["DividingPallet"].Enabled = true;
                            }
                        }
                    }

                    if (GridSelectedItem != null && GridSelectedItem.Count > 0)
                    {
                        PrintLabelButton.IsEnabled = true;

                        if (GridSelectedItem.CheckGet("PRODUCT_IDK1").ToInt().ContainsIn(4, 5, 6))
                        {
                            Grid.Menu["PrintLabel"].Enabled = true;
                        }
                    }
                }
                else
                {
                    // Если дата в отчётном периоде
                    if (inReportingPeriod)
                    {
                        FactorySelectBox.IsEnabled = true;
                    }
                }

                // Если дата в отчётном периоде
                if (inReportingPeriod)
                {
                    SaveButton.IsEnabled = true;

                    //ReceiptDateDatePicker.IsEnabled = true;
                    ReceiptDateTextBox.IsEnabled = true;
                    //InvoiceDateDatePicker.IsEnabled = true;
                    InvoiceDateTextBox.IsEnabled = true;
                    //ShippingDateDatePicker.IsEnabled = true;
                    ShippingDateTextBox.IsEnabled = true;
                }
            }

            ProcessPermissions();
        }

        /// <summary>
        /// Добавление позиции расхода
        /// </summary>
        public void AddConsumption()
        {
            var window = new ConsumptionEdit();
            window.InvoiceId = InvoiceId;
            window.CustomerId = Form.GetValueByPath("BUYER_ID").ToInt();
            window.Show();
        }

        /// <summary>
        /// Добавляем виртуальный поддон в расход 
        /// (Производим и списываем в эту отгрузку новый поддон)
        /// </summary>
        public void AddVirtualConsumption()
        {
            var window = new ConsumptionEdit();
            window.InvoiceId = InvoiceId;
            window.CustomerId = Form.GetValueByPath("BUYER_ID").ToInt();
            window.VirtualFlag = true;
            window.Show();
        }

        /// <summary>
        /// Редактирование выбранной позиции расхода
        /// </summary>
        public void EditConsumption()
        {
            if (GridSelectedItem != null && GridSelectedItem.Count > 0)
            {
                var window = new ConsumptionEdit();
                window.InvoiceId = InvoiceId;
                window.ConsumptionId = GridSelectedItem.CheckGet("CONSUMPTION_ID").ToInt();
                window.CompletedFlag = GridSelectedItem.CheckGet("COMPLETED_FLAG").ToBool();
                window.CustomerId = GridSelectedItem.CheckGet("CUSTOMER_ID").ToInt();
                window.ConsumptionData = GridSelectedItem;
                window.Show();
            }
            else
            {
                var msg = "Не выбрана позиция для редактирования.";
                var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Удаление выбранных позиции расхода
        /// </summary>
        public void DeleteConsumption()
        {
            var selectedConsumptionList = Grid.Items.Where(x => x.CheckGet("CHECKED_FLAG").ToInt() > 0).ToList();
            if (selectedConsumptionList != null && selectedConsumptionList.Count > 0)
            {
                if (selectedConsumptionList.Count(x => x.CheckGet("COMPLETED_FLAG").ToInt() == 1) > 0)
                {
                    var msg = $"Эти данные из счёта уже прошли обработку. Всё равно удалить {selectedConsumptionList.Count} позиций расхода?";
                    var d = new DialogWindow($"{msg}", "Удаление позиции расхода", "", DialogWindowButtons.NoYes);
                    if (d.ShowDialog() != true)
                    {
                        return;
                    }
                }
                else
                {
                    var msg = $"Удалить {selectedConsumptionList.Count} позиций расхода?";
                    var d = new DialogWindow($"{msg}", "Удаление позиции расхода", "", DialogWindowButtons.NoYes);
                    if (d.ShowDialog() != true)
                    {
                        return;
                    }
                }

                bool succesfullFlag = true;
                foreach (var selectedItem in selectedConsumptionList)
                {
                    if (!DeleteOneConsumption(selectedItem, false, false))
                    {
                        succesfullFlag = false;
                    }
                }

                // Отправляем сообщение вкладке "Список продаж" обновиться
                Messenger.Default.Send(new ItemMessage()
                {
                    ReceiverGroup = "Preproduction",
                    ReceiverName = "SaleList",
                    SenderName = "ConsumptionList",
                    Action = "Refresh",
                    Message = "",
                }
                );

                LoadConsumptionItems();

                if (succesfullFlag)
                {
                    var msg = "Успешное удаление позиций расхода.";
                    var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else if (GridSelectedItem != null && GridSelectedItem.Count > 0)
            {
                if (GridSelectedItem.CheckGet("COMPLETED_FLAG").ToInt() == 1)
                {
                    var msg = "Эти данные из счёта уже прошли обработку. Всё равно удалить позицию расхода?";
                    var d = new DialogWindow($"{msg}", "Удаление позиции расхода", "", DialogWindowButtons.NoYes);
                    if (d.ShowDialog() != true)
                    {
                        return;
                    }
                }
                else
                {
                    var msg = "Удалить позицию расхода?";
                    var d = new DialogWindow($"{msg}", "Удаление позиции расхода", "", DialogWindowButtons.NoYes);
                    if (d.ShowDialog() != true)
                    {
                        return;
                    }
                }

                DeleteOneConsumption(GridSelectedItem);
            }
            else
            {
                var msg = "Не выбрана позиция для удаления.";
                var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Удаление одной позиции расхода
        /// </summary>
        /// <param name="selectedItem"></param>
        /// <param name="updateGridItems"></param>
        private bool DeleteOneConsumption(Dictionary<string, string> selectedItem, bool updateGridItems = true, bool showSuccesfullMessage = true)
        {
            bool succesfullFlag = false;

            if (selectedItem != null && selectedItem.Count > 0)
            {
                var p = new Dictionary<string, string>();
                p.Add("INVOICE_ID", InvoiceId.ToString());
                p.Add("CONSUMPTION_ID", selectedItem.CheckGet("CONSUMPTION_ID"));
                p.Add("INCOMING_ID", selectedItem.CheckGet("INCOMING_ID"));
                p.Add("CUSTOMER_ID", selectedItem.CheckGet("CUSTOMER_ID"));
                p.Add("COMPLETED_FLAG", selectedItem.CheckGet("COMPLETED_FLAG").ToInt().ToString());
                if (string.IsNullOrEmpty(selectedItem.CheckGet("PRODUCTION_TASK_NUMBER")))
                {
                    p.Add("EMPTY_NUMBER_FLAG", "1");
                }
                else
                {
                    p.Add("EMPTY_NUMBER_FLAG", "0");
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "Sale");
                q.Request.SetParam("Action", "DeleteConsumption");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                GridDisableControls();
                q.DoQuery();
                GridEnableControls();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var dataSet = ListDataSet.Create(result, "ITEMS");
                        if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                        {
                            if (dataSet.Items.First().CheckGet("CONSUMPTION_ID").ToInt() > 0)
                            {
                                succesfullFlag = true;
                            }
                        }
                    }

                    if (!succesfullFlag)
                    {
                        var msg = "Ошибка удаления позиции расхода. Пожалуйста, сообщите о проблеме.";
                        var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }

                if (updateGridItems)
                {
                    // Отправляем сообщение вкладке "Список продаж" обновиться
                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = "Preproduction",
                        ReceiverName = "SaleList",
                        SenderName = "ConsumptionList",
                        Action = "Refresh",
                        Message = "",
                    }
                    );

                    LoadConsumptionItems();
                }

                if (succesfullFlag && showSuccesfullMessage)
                {
                    var msg = "Успешное удаление позиции расхода.";
                    var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                var msg = "Не выбрана позиция для удаления.";
                var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }

            return succesfullFlag;
        }

        /// <summary>
        /// Провести позиции накладной
        /// </summary>
        public void ConfirmConsumption()
        {
            var selectedConsumptionList = Grid.Items.Where(x => x.CheckGet("CHECKED_FLAG").ToInt() > 0).ToList();
            if (selectedConsumptionList != null && selectedConsumptionList.Count > 0)
            {
                {
                    var msg = $"Провести {selectedConsumptionList.Count} позиций расхода?";
                    var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.NoYes);
                    if (d.ShowDialog() != true)
                    {
                        return;
                    }
                }

                bool succesfullFlag = true;
                foreach (var selectedConsumption in selectedConsumptionList)
                {
                    if (ConfirmOneConsumption(selectedConsumption, false, false))
                    {
                        selectedConsumption.CheckAdd("CHECKED_FLAG", "0");
                    }
                    else
                    {
                        succesfullFlag = false;
                    }
                }

                // Отправляем сообщение вкладке "Список продаж" обновиться
                Messenger.Default.Send(new ItemMessage()
                {
                    ReceiverGroup = "Preproduction",
                    ReceiverName = "SaleList",
                    SenderName = "ConsumptionList",
                    Action = "Refresh",
                    Message = "",
                }
                );

                LoadConsumptionItems();

                if (succesfullFlag)
                {
                    var msg = "Успешное проведение позиций расхода.";
                    var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else if (GridSelectedItem != null && GridSelectedItem.Count > 0)
            {
                ConfirmOneConsumption(GridSelectedItem);
            }
            else
            {
                var msg = "Не выбрана позиция для проведения.";
                var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Провести одну позицию
        /// </summary>
        public bool ConfirmOneConsumption(Dictionary<string, string> selectedItem, bool updateGridItems = true, bool showSuccesfullMessage = true)
        {
            bool succesfullFlag = false;

            if (selectedItem != null && selectedItem.Count > 0)
            {
                if (selectedItem.CheckGet("COMPLETED_FLAG").ToInt() == 0)
                {
                    var p = new Dictionary<string, string>();
                    p.Add("INVOICE_TYPE", Form.GetValueByPath("INVOICE_TYPE"));
                    p.Add("CONSUMPTION_ID", selectedItem.CheckGet("CONSUMPTION_ID"));

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sales");
                    q.Request.SetParam("Object", "Sale");
                    q.Request.SetParam("Action", "ConfirmConsumption");
                    q.Request.SetParams(p);
                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    GridDisableControls();
                    q.DoQuery();
                    GridEnableControls();

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            var dataSet = ListDataSet.Create(result, "ITEMS");
                            if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                            {
                                if (dataSet.Items.First().CheckGet("CONSUMPTION_ID").ToInt() > 0)
                                {
                                    succesfullFlag = true;
                                }
                            }
                        }

                        if (!succesfullFlag)
                        {
                            var msg = "Ошибка проведения позиции расхода. Пожалуйста, сообщите о проблеме.";
                            var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }

                    if (updateGridItems)
                    {
                        // Отправляем сообщение вкладке "Список продаж" обновиться
                        Messenger.Default.Send(new ItemMessage()
                        {
                            ReceiverGroup = "Preproduction",
                            ReceiverName = "SaleList",
                            SenderName = "ConsumptionList",
                            Action = "Refresh",
                            Message = "",
                        }
                        );

                        LoadConsumptionItems();
                    }

                    if (succesfullFlag && showSuccesfullMessage)
                    {
                        var msg = "Успешное проведение позиции расхода.";
                        var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
            }
            else
            {
                var msg = "Не выбрана позиция для проведения.";
                var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }

            return succesfullFlag;
        }

        /// <summary>
        /// Отменить проведение позиции
        /// </summary>
        public void UnconfirmConsumption()
        {
            var selectedConsumptionList = Grid.Items.Where(x => x.CheckGet("CHECKED_FLAG").ToInt() > 0).ToList();
            if (selectedConsumptionList != null && selectedConsumptionList.Count > 0)
            {
                {
                    var msg = $"Отменить проведение {selectedConsumptionList.Count} позиций расхода?";
                    var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.NoYes);
                    if (d.ShowDialog() != true)
                    {
                        return;
                    }
                }

                bool succesfullFlag = true;
                foreach (var selectedConsumption in selectedConsumptionList)
                {
                    if (UnconfirmOneConsumption(selectedConsumption, false, false))
                    {
                        selectedConsumption.CheckAdd("CHECKED_FLAG", "0");
                    }
                    else
                    {
                        succesfullFlag = false; 
                    }
                }

                // Отправляем сообщение вкладке "Список продаж" обновиться
                Messenger.Default.Send(new ItemMessage()
                {
                    ReceiverGroup = "Preproduction",
                    ReceiverName = "SaleList",
                    SenderName = "ConsumptionList",
                    Action = "Refresh",
                    Message = "",
                }
                );

                LoadConsumptionItems();

                if (succesfullFlag)
                {
                    var msg = "Успешная отмена проведения позиций расхода.";
                    var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else if (GridSelectedItem != null && GridSelectedItem.Count > 0)
            {
                UnconfirmOneConsumption(GridSelectedItem);
            }
            else
            {
                var msg = "Не выбрана позиция для отмены проведения.";
                var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Отменить проведение одной позиции
        /// </summary>
        public bool UnconfirmOneConsumption(Dictionary<string, string> selectedItem, bool updateGridItems = true, bool showSuccesfullMessage = true)
        {
            bool succesfullFlag = false;

            if (selectedItem != null && selectedItem.Count > 0)
            {
                if (selectedItem.CheckGet("COMPLETED_FLAG").ToInt() > 0)
                {
                    var p = new Dictionary<string, string>();
                    p.Add("CONSUMPTION_ID", selectedItem.CheckGet("CONSUMPTION_ID"));

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sales");
                    q.Request.SetParam("Object", "Sale");
                    q.Request.SetParam("Action", "UnconfirmConsumption");
                    q.Request.SetParams(p);
                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    GridDisableControls();
                    q.DoQuery();
                    GridEnableControls();

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            var dataSet = ListDataSet.Create(result, "ITEMS");
                            if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                            {
                                if (dataSet.Items.First().CheckGet("CONSUMPTION_ID").ToInt() > 0)
                                {
                                    succesfullFlag = true;
                                }
                            }
                        }

                        if (!succesfullFlag)
                        {
                            var msg = "Ошибка отмены проведения позиции расхода. Пожалуйста, сообщите о проблеме";
                            var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }

                    if (updateGridItems)
                    {
                        // Отправляем сообщение вкладке "Список продаж" обновиться
                        Messenger.Default.Send(new ItemMessage()
                        {
                            ReceiverGroup = "Preproduction",
                            ReceiverName = "SaleList",
                            SenderName = "ConsumptionList",
                            Action = "Refresh",
                            Message = "",
                        }
                        );

                        LoadConsumptionItems();
                    }

                    if (succesfullFlag && showSuccesfullMessage)
                    {
                        var msg = "Успешная отмена проведения позиции расхода.";
                        var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
            }
            else
            {
                var msg = "Не выбрана позиция для отмены проведения.";
                var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }

            return succesfullFlag;
        }

        /// <summary>
        /// Провести все позиции накладной
        /// </summary>
        public void Confirm()
        {
            var p = new Dictionary<string, string>();
            p.Add("INVOICE_ID", InvoiceId.ToString());
            p.Add("INVOICE_TYPE", Form.GetValueByPath("INVOICE_TYPE"));

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "Sale");
            q.Request.SetParam("Action", "Confirm");
            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            GridDisableControls();
            q.DoQuery();
            GridEnableControls();

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
                    LoadConsumptionItems();
                }
                else
                {
                    var msg = "Ошибка проведения всех позиций накладной. Пожалуйста, сообщите о проблеме";
                    var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Отменить проведение всех позиций по накладной
        /// </summary>
        public void Unconfirm()
        {
            var p = new Dictionary<string, string>();
            p.Add("INVOICE_ID", InvoiceId.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "Sale");
            q.Request.SetParam("Action", "Unconfirm");
            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            GridDisableControls();
            q.DoQuery();
            GridEnableControls();

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
                    LoadConsumptionItems();
                }
                else
                {
                    var msg = "Ошибка отмены проведения всех позиций по накладной. Пожалуйста, сообщите о проблеме";
                    var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Разделение поддона на 2
        /// </summary>
        public void DividingPallet()
        {
            if (GridSelectedItem != null && GridSelectedItem.Count > 0)
            {
                // Количество на старом поддоне
                int oldQuantity = GridSelectedItem.CheckGet("QUANTITY").ToInt();
                // Количество на новом поддоне
                int newQuantity = 0;

                var i = new ComplectationCMQuantity(oldQuantity);
                i.Show("Количество на новом поддоне");
                if (i.OkFlag)
                {
                    newQuantity = i.QtyInt;
                }

                if (newQuantity > 0)
                {
                    if (newQuantity >= oldQuantity)
                    {
                        var msg = $"Количество на новом поддоне должно быть меньше, чем количество на выбранном поддоне.{Environment.NewLine}Пожалуйста, уменьшите количество на новом поддоне.";
                        var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                    else
                    {
                        var p = new Dictionary<string, string>();
                        p.Add("CONSUMPTION_ID", GridSelectedItem.CheckGet("CONSUMPTION_ID"));
                        p.Add("QUANTITY_ON_NEW_PALLET", $"{newQuantity}");

                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Sales");
                        q.Request.SetParam("Object", "Sale");
                        q.Request.SetParam("Action", "DividingPallet");
                        q.Request.SetParams(p);
                        q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                        q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                        GridDisableControls();
                        q.DoQuery();
                        GridEnableControls();

                        if (q.Answer.Status == 0)
                        {
                            bool succesfullFlag = false;

                            var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                            if (result != null)
                            {
                                var dataSet = ListDataSet.Create(result, "ITEMS");
                                if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                                {
                                    if (dataSet.Items.First().CheckGet("CONSUMPTION_ID").ToInt() > 0)
                                    {
                                        succesfullFlag = true;
                                    }
                                }
                            }

                            if (succesfullFlag)
                            {
                                LoadConsumptionItems();

                                var msg = "Успешное разделение поддона на 2.";
                                var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                                d.ShowDialog();
                            }
                            else
                            {
                                var msg = "Ошибка разделения поддона на 2. Пожалуйста, сообщите о проблеме.";
                                var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
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
            else
            {
                var msg = "Не выбрана позиция для разделения.";
                var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        public void PrepareToMoveToOtherDocument()
        {
            bool succesfullFlag = false;
            var msg = "Не выбрана позиция для перемещения в другую накладную.";

            var selectedConsumptionList = Grid.Items.Where(x => x.CheckGet("CHECKED_FLAG").ToInt() > 0).ToList();
            if (selectedConsumptionList != null && selectedConsumptionList.Count > 0)
            {
                if (selectedConsumptionList.Count(x => x.CheckGet("COMPLETED_FLAG").ToInt() == 0) > 0)
                {
                    succesfullFlag = true;
                }
                else
                {
                    msg = "Нельзя переместить проведённые позиции. Пожалуйста, отмените проведение и повторите операцию.";
                }
            }
            else if (GridSelectedItem != null && GridSelectedItem.Count > 0)
            {
                if (GridSelectedItem.CheckGet("COMPLETED_FLAG").ToInt() == 0)
                {
                    succesfullFlag = true;
                }
                else
                {
                    msg = "Нельзя переместить проведённую позицию. Пожалуйста, отмените проведение и повторите операцию.";
                }
            }

            if (succesfullFlag)
            {
                var window = new SaleListInReportingPeriod();
                window.ParentFrame = FrameName;
                window.FactoryId = Form.GetValueByPath("FACTORY_ID").ToInt();
                window.Show();
            }
            else
            {
                var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Перенести позицию в другую накладную
        /// </summary>
        public void MoveToOtherDocument(Dictionary<string, string> movingPositionData)
        {
            var selectedConsumptionList = Grid.Items.Where(x => x.CheckGet("CHECKED_FLAG").ToInt() > 0).ToList();
            if (selectedConsumptionList != null && selectedConsumptionList.Count > 0)
            {
                {
                    var msg = $"Перенести {selectedConsumptionList.Count} позиций в выбранную накладную?" +
                        $"{Environment.NewLine}№ счёт-фактуры: {movingPositionData.CheckGet("NAME_SF")}" +
                        $"{Environment.NewLine}№ ТТН: {movingPositionData.CheckGet("NAME_STH")}" +
                        $"{Environment.NewLine}Покупатель: {movingPositionData.CheckGet("BUYER_NAME")}";

                    var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.NoYes);
                    if (d.ShowDialog() != true)
                    {
                        return;
                    }
                }

                bool succesfullFlag = true;
                foreach (var selectedConsumption in selectedConsumptionList)
                {
                    if (MoveOneToOtherDocument(selectedConsumption, movingPositionData, false, false))
                    {
                        selectedConsumption.CheckAdd("CHECKED_FLAG", "0");
                    }
                    else
                    {
                        succesfullFlag = false;
                    }
                }

                // Отправляем сообщение вкладке "Список продаж" обновиться
                Messenger.Default.Send(new ItemMessage()
                {
                    ReceiverGroup = "Preproduction",
                    ReceiverName = "SaleList",
                    SenderName = "ConsumptionList",
                    Action = "Refresh",
                    Message = "",
                }
                );

                LoadConsumptionItems();

                if (succesfullFlag)
                {
                    var msg = "Успешное перемещение позиций расхода в другую накладную.";
                    var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else if (GridSelectedItem != null && GridSelectedItem.Count > 0)
            {
                {
                    var msg = $"Перенести поддон {GridSelectedItem.CheckGet("PALLET_FULL_NUMBER")} в выбранную накладную?" +
                        $"{Environment.NewLine}№ счёт-фактуры: {movingPositionData.CheckGet("NAME_SF")}" +
                        $"{Environment.NewLine}№ ТТН: {movingPositionData.CheckGet("NAME_STH")}" +
                        $"{Environment.NewLine}Покупатель: {movingPositionData.CheckGet("BUYER_NAME")}";

                    var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.NoYes);
                    if (d.ShowDialog() != true)
                    {
                        return;
                    }
                }

                MoveOneToOtherDocument(GridSelectedItem, movingPositionData);
            }
            else
            {
                var msg = "Не выбрана позиция для перемещения в другую накладную.";
                var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        public bool MoveOneToOtherDocument(Dictionary<string, string> selectedItem, Dictionary<string, string> movingPositionData, bool updateGridItems = true, bool showSuccesfullMessage = true)
        {
            bool succesfullFlag = false;
            if (selectedItem != null && selectedItem.Count > 0)
            {
                if (selectedItem.CheckGet("COMPLETED_FLAG").ToInt() == 0)
                {
                    var p = new Dictionary<string, string>();
                    p.Add("CONSUMPTION_ID", selectedItem.CheckGet("CONSUMPTION_ID"));
                    p.Add("OLD_CUSTOMER_ID", selectedItem.CheckGet("CUSTOMER_ID"));
                    p.Add("OLD_INVOICE_ID", InvoiceId.ToString());
                    p.Add("NEW_INVOICE_ID", movingPositionData.CheckGet("NEW_INVOICE_ID"));
                    p.Add("NEW_CUSTOMER_ID", movingPositionData.CheckGet("NEW_CUSTOMER_ID"));

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sales");
                    q.Request.SetParam("Object", "Sale");
                    q.Request.SetParam("Action", "MoveToOtherDocument");
                    q.Request.SetParams(p);
                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    GridDisableControls();
                    q.DoQuery();
                    GridEnableControls();

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            var dataSet = ListDataSet.Create(result, "ITEMS");
                            if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                            {
                                if (dataSet.Items.First().CheckGet("CONSUMPTION_ID").ToInt() > 0)
                                {
                                    succesfullFlag = true;
                                }
                            }
                        }

                        if (!succesfullFlag)
                        {
                            var msg = "Ошибка перемещения позиции расхода в другую накладную. Пожалуйста, сообщите о проблеме";
                            var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }

                    if (updateGridItems)
                    {
                        // Отправляем сообщение вкладке "Список продаж" обновиться
                        Messenger.Default.Send(new ItemMessage()
                        {
                            ReceiverGroup = "Preproduction",
                            ReceiverName = "SaleList",
                            SenderName = "ConsumptionList",
                            Action = "Refresh",
                            Message = "",
                        }
                        );

                        LoadConsumptionItems();
                    }

                    if (succesfullFlag && showSuccesfullMessage)
                    {
                        var msg = "Успешное перемещение позиции расхода в другую накладную.";
                        var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
            }
            else
            {
                var msg = "Не выбрана позиция для перемещения в другую накладную.";
                var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }

            return succesfullFlag;
        }

        /// <summary>
        /// Открытие окна корректировки по документу
        /// </summary>
        public void OpenAdjustment()
        {
            var window = new Adjustment();
            window.InvoiceId = InvoiceId;
            window.FactoryId = Form.GetValueByPath("FACTORY_ID").ToInt();
            window.ReportingPeriod = ReportingPeriod;
            window.SignotoryEmployeeId = SignotoryEmployeeId;
            window.SignotoryEmployeeName = SignotoryEmployeeName;
            window.AdjusmentId = Form.GetValueByPath("ADJUSTMENT_ID").ToInt();
            window.Show();
        }

        /// <summary>
        /// Печать документов
        /// </summary>
        public void PrintDocument()
        {
            var documentPrintManager = new DocumentPrintManager();
            documentPrintManager.RoleName = this.RoleName;
            documentPrintManager.SetSignotory(SignotoryEmployeeId, SignotoryEmployeeName);
            documentPrintManager.InvoiceId = InvoiceId;
            documentPrintManager.AdjustmentId = Form.GetValueByPath("ADJUSTMENT_ID").ToInt();
            documentPrintManager.Show();
        }

        public void PrintLabel()
        {
            var selectedConsumptionList = Grid.Items.Where(x => x.CheckGet("CHECKED_FLAG").ToInt() > 0).ToList();
            if (selectedConsumptionList != null && selectedConsumptionList.Count > 0)
            {
                foreach (var selectedItem in selectedConsumptionList)
                {
                    if (selectedItem.CheckGet("PRODUCT_IDK1").ToInt().ContainsIn(4, 5, 6))
                    {
                        PrintOneLabel(selectedItem);
                        selectedItem.CheckAdd("CHECKED_FLAG", "0");
                    }
                }

                Grid.UpdateItems();
            }
            else if (GridSelectedItem != null && GridSelectedItem.Count > 0)
            {
                if (!GridSelectedItem.CheckGet("PRODUCT_IDK1").ToInt().ContainsIn(4, 5, 6))
                {
                    var msg = "Нельзя распечатать ярлык для данного вида продукции.";
                    var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }

                PrintOneLabel(GridSelectedItem);
            }
            else
            {
                var msg = "Не выбрана позиция для печати ярлыка.";
                var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        public void PrintOneLabel(Dictionary<string, string> selectedItem)
        {
            if (selectedItem != null && selectedItem.Count > 0)
            {
                LabelReport2 labelReport2 = new LabelReport2(true);
                labelReport2.ShowLabel(selectedItem.CheckGet("PRODUCTION_TASK_ID"), selectedItem.CheckGet("PALLET_NUMBER"), selectedItem.CheckGet("PRODUCT_IDK1"));
            }
            else
            {
                var msg = "Не выбрана позиция для печати ярлыка.";
                var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.IndexOf("Preproduction") > -1)
            {
                if (m.ReceiverName.IndexOf("ConsumptionList") > -1)
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            LoadConsumptionItems();
                            break;

                        case "RefreshAll":
                            LoadData();
                            break;

                        case "SetAdjustmentId":
                            Form.SetValueByPath("ADJUSTMENT_ID", m.Message.ToInt().ToString());
                            Dictionary<string, string> messageData = (Dictionary<string, string>)m.ContextObject;
                            Form.SetValueByPath("ADJUSTMENT_DATE", messageData.CheckGet("ADJUSTMENT_DATE"));
                            Form.SetValueByPath("ADJUSTMENT_NUMBER", messageData.CheckGet("ADJUSTMENT_NUMBER"));
                            break;

                        case "MoveToOtherDocument":
                            if (m.ContextObject != null)
                            {
                                MoveToOtherDocument((Dictionary<string, string>)m.ContextObject);
                            }
                            break;

                        case "UploadWebDocumentList":
                            this.UploadWebDocumentList();
                            break;

                        case "SelectItem":
                            int type = m.Message.ToInt();
                            SelectedBuyerData = (Dictionary<string, string>)m.ContextObject;
                            if (type == 2)
                            {
                                Form.SetValueByPath("CONSIGNEE_NAME", $"{SelectedBuyerData.CheckGet("BUYER_NAME")} ({SelectedBuyerData.CheckGet("ID")})");
                                Form.SetValueByPath("CONSIGNEE_ID", SelectedBuyerData.CheckGet("ID"));
                            }
                            else
                            {
                                Form.SetValueByPath("BUYER_NAME", $"{SelectedBuyerData.CheckGet("BUYER_NAME")} ({SelectedBuyerData.CheckGet("ID")})");
                                Form.SetValueByPath("BUYER_ID", SelectedBuyerData.CheckGet("ID"));
                            }
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Sales",
                ReceiverName = "",
                SenderName = "ConsumptionList",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            Grid.Destruct();
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

        public void FormDisableControls()
        {
            FormToolbar.IsEnabled = false;
            MainForm.IsEnabled = false;
        }

        public void FormEnableControls()
        {
            FormToolbar.IsEnabled = true;
            MainForm.IsEnabled = true;
        }

        public void GridDisableControls()
        {
            GridToolbar.IsEnabled = false;
            Grid.IsEnabled = false;
        }

        public void GridEnableControls()
        {
            GridToolbar.IsEnabled = true;
            Grid.IsEnabled = true;
        }

        /// <summary>
        /// отображение справочной статьи
        /// (относительный путь)
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp-new/Sales/list_sales/edit_tn");
            //Central.ShowHelp("/doc/l-pack-erp/sales/sale_list/consumption_list");
        }

        /// <summary>
        /// При изменении даты Счёта менеяем на соответствующую дату даты фактуры и отгрузки.
        /// Заполняем выпадающий список смен
        /// </summary>
        public void ReceiptDateTextChanged()
        {
            if (Form != null)
            {
                if (!string.IsNullOrEmpty(Form.GetValueByPath("RECEIPT_DATE")))
                {
                    Form.SetValueByPath("INVOICE_DATE", Form.GetValueByPath("RECEIPT_DATE"));
                    Form.SetValueByPath("SHIPPING_DATE", Form.GetValueByPath("RECEIPT_DATE"));
                }

                GetDataForShiftSelectBox(Form.GetValueByPath("RECEIPT_DATE"));
            }
        }

        public void ShippingDateTextChanged()
        {
            if (Form != null)
            {
                UpdateActions();
            }
        }

        /// <summary>
        /// Заполнение выпадающего списка смен
        /// </summary>
        /// <param name="date"></param>
        public void GetDataForShiftSelectBox(string date)
        {
            var p = new Dictionary<string, string>();
            p.Add("DATE", date);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "Sale");
            q.Request.SetParam("Action", "GetShiftByDate");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            FormDisableControls();
            q.DoQuery();
            FormEnableControls();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    ShiftSelectBox.SetItems(ds, "ID", "DTTM");
                    ShiftSelectBox.SetSelectedItemFirst();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        public void DefectCheckBoxChecked()
        {
            Form.SetValueByPath("SELLER_ID", "0");
            Form.SetValueByPath("SHIPPER_ID", "0");
            Form.SetValueByPath("BUYER_ID", "0");
            Form.SetValueByPath("BUYER_NAME", "(0)");
            Form.SetValueByPath("INVOICE_TYPE", "3");
        }

        public void DefectCheckBoxUnchecked()
        {
            Form.SetValueByPath("INVOICE_TYPE", "0");
        }

        /// <summary>
        /// Дополнительная валидация.
        /// Запрещаем выбирать дату, которая находится вне текущего отчётного периода.
        /// </summary>
        public bool ValidateReportingPeriod()
        {
            bool valid = true;
            var reportingPeriod = ReportingPeriod.ToDateTime();
            var receiptDate = Form.GetValueByPath("RECEIPT_DATE").ToDateTime();
            var invoiceDate = Form.GetValueByPath("INVOICE_DATE").ToDateTime();
            var shippingDate = Form.GetValueByPath("SHIPPING_DATE").ToDateTime();

            if (receiptDate < reportingPeriod)
            {
                valid = false;
            }

            if (invoiceDate < reportingPeriod)
            {
                valid = false;
            }

            if (shippingDate < reportingPeriod)
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

        private void CheckActiveContract()
        {
            if (ContractSelectBox.SelectedItem.Key != null && ContractDataSet.Items.Count > 0)
            {
                var contractItem = ContractDataSet.Items.FirstOrDefault(x => x.CheckGet("CONTRACT_ID").ToInt() == ContractSelectBox.SelectedItem.Key.ToInt());
                if (contractItem != null)
                {
                    if (contractItem.CheckGet("ACTIVE_FLAG").ToInt() == 0)
                    {
                        StatusTextBox.Text = "Неактивный договор";
                    }
                    else
                    {
                        StatusTextBox.Text = "";
                    }
                }
            }
        }

        private void ValidateContract()
        {
            if (ContractDataSet.Items.Count > 0)
            {
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "CONTRACT_ID"), FormHelperField.FieldFilterRef.Required);
            }
            else
            {
                Form.RemoveFilter("CONTRACT_ID", FormHelperField.FieldFilterRef.Required);
            }
        }

        public void Save()
        {
            if (Form.Validate())
            {
                if (ValidateReportingPeriod())
                {
                    if (Form.GetValueByPath("DEFECT").ToInt() > 0)
                    {
                        if (!string.IsNullOrEmpty(Form.GetValueByPath("CONSIGNMENT_NOTE"))
                            && !Form.GetValueByPath("CONSIGNMENT_NOTE").Contains("Б"))
                        {
                            string msg = $"Отмечен флажок брака, но номер ТТН не содержит букву \"Б\"." +
                                $"{Environment.NewLine}Добавить букву \"Б\" в номер ТТН автоматически?";
                            var d = new DialogWindow($"{msg}", "Редактирование документа", "", DialogWindowButtons.YesNo);
                            if (d.ShowDialog() == true)
                            {
                                Form.SetValueByPath("CONSIGNMENT_NOTE", $"{Form.GetValueByPath("CONSIGNMENT_NOTE")} Б");
                            }
                        }
                    }

                    if (InvoiceId > 0)
                    {
                        if (Form.GetValueByPath("DEFECT").ToInt() == 0)
                        {
                            if (string.IsNullOrEmpty(Form.GetValueByPath("BUYER_ID")))
                            {
                                var msg = "Выберите покупателя.";
                                var d = new DialogWindow($"{msg}", "Редактирование документа", "", DialogWindowButtons.OK);
                                d.ShowDialog();

                                return;
                            }
                        }

                        CheckAddShipperFilling(false);

                        var p = Form.GetValues();

                        if (string.IsNullOrEmpty(Form.GetValueByPath("CONSIGNEE_ID")))
                        {
                            p.Remove("CONSIGNEE_ID");
                        }

                        if (ContractDataSet.Items.Count == 0)
                        {
                            p.Remove("CONTRACT_ID");
                        }

                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Sales");
                        q.Request.SetParam("Object", "Sale");
                        q.Request.SetParam("Action", "Update");
                        q.Request.SetParams(p);

                        q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                        q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                        FormDisableControls();
                        q.DoQuery();
                        FormEnableControls();

                        if (q.Answer.Status == 0)
                        {
                            bool succesfullFlag = false;

                            var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                            if (result != null)
                            {
                                var ds = ListDataSet.Create(result, "ITEMS");
                                if (ds != null && ds.Items != null && ds.Items.Count > 0)
                                {
                                    if (ds.Items.First().CheckGet("INVOICE_ID").ToInt() > 0)
                                    {
                                        succesfullFlag = true;
                                    }
                                }
                            }

                            if (succesfullFlag)
                            {
                                // Отправляем сообщение вкладке "Список продаж" обновиться
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "Preproduction",
                                    ReceiverName = "SaleList",
                                    SenderName = "ConsumptionList",
                                    Action = "Refresh",
                                    Message = "",
                                }
                                );

                                LoadInvoiceData();

                                var msg = "Успешное обновление накладной.";
                                var d = new DialogWindow($"{msg}", "Редактирование документа", "", DialogWindowButtons.OK);
                                d.ShowDialog();
                            }
                            else
                            {
                                var msg = "При обновлении накладной произошла ошибка. Пожалуйста, сообщите о проблеме.";
                                var d = new DialogWindow($"{msg}", "Редактирование документа", "", DialogWindowButtons.OK);
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
                        if (Form.GetValueByPath("DEFECT").ToInt() == 0)
                        {
                            if (string.IsNullOrEmpty(Form.GetValueByPath("BUYER_ID")))
                            {
                                var msg = "Выберите покупателя.";
                                var d = new DialogWindow($"{msg}", "Редактирование документа", "", DialogWindowButtons.OK);
                                d.ShowDialog();

                                return;
                            }
                        }

                        if (Form.GetValueByPath("SELLER_ID").ToInt() == 0 && Form.GetValueByPath("DEFECT").ToInt() == 0)
                        {
                            var msg = $"Внимание!" +
                                $"{Environment.NewLine}Выбран продавец ООО \"Л-Пак\" Банк \"Возрождение\"" +
                                $"{Environment.NewLine}Продолжить?";
                            var d = new DialogWindow($"{msg}", "Редактирование документа", "", DialogWindowButtons.NoYes);
                            if (d.ShowDialog() != true)
                            {
                                return;
                            }
                        }

                        CheckAddShipperFilling(false);

                        var p = Form.GetValues();

                        if (string.IsNullOrEmpty(Form.GetValueByPath("CONSIGNEE_ID")))
                        {
                            p.Remove("CONSIGNEE_ID");
                        }

                        if (ContractDataSet.Items.Count == 0)
                        {
                            p.Remove("CONTRACT_ID");
                        }

                        if (string.IsNullOrEmpty(p.CheckGet("FACTORY_ID")))
                        {
                            // Если грузоотправитель Л-Пак Кашира
                            if (p.CheckGet("SHIPPER_ID").ToInt() == 427)
                            {
                                p.CheckAdd("FACTORY_ID", "2");
                            }
                            else
                            {
                                p.CheckAdd("FACTORY_ID", "1");
                            }
                        }

                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Sales");
                        q.Request.SetParam("Object", "Sale");
                        q.Request.SetParam("Action", "Save");
                        q.Request.SetParams(p);

                        q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                        q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                        FormDisableControls();
                        q.DoQuery();
                        FormEnableControls();

                        if (q.Answer.Status == 0)
                        {
                            bool succesfullFlag = false;

                            var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                            if (result != null)
                            {
                                var ds = ListDataSet.Create(result, "ITEMS");
                                if (ds != null && ds.Items != null && ds.Items.Count > 0)
                                {
                                    if (ds.Items.First().CheckGet("INVOICE_ID").ToInt() > 0)
                                    {
                                        InvoiceId = ds.Items.First().CheckGet("INVOICE_ID").ToInt();
                                        Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "CONSIGNMENT_NOTE"), FormHelperField.FieldFilterRef.Required);
                                        succesfullFlag = true;
                                    }
                                }
                            }

                            if (succesfullFlag)
                            {
                                // Отправляем сообщение вкладке "Список продаж" обновиться
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "Preproduction",
                                    ReceiverName = "SaleList",
                                    SenderName = "ConsumptionList",
                                    Action = "Refresh",
                                    Message = "",
                                }
                                );

                                LoadInvoiceData();

                                var msg = "Успешное создание накладной.";
                                var d = new DialogWindow($"{msg}", "Редактирование документа", "", DialogWindowButtons.OK);
                                d.ShowDialog();
                            }
                            else
                            {
                                var msg = "При создании накладной произошла ошибка. Пожалуйста, сообщите о проблеме.";
                                var d = new DialogWindow($"{msg}", "Редактирование документа", "", DialogWindowButtons.OK);
                                d.ShowDialog();
                            }
                        }
                        else
                        {
                            q.ProcessError();
                        }
                    }

                    UpdateActions();
                }
            }
        }


        /// <summary>
        /// Получаем список договоров по выбранному покупателю и продавцу
        /// </summary>
        public void GetContractList()
        {
            if (!string.IsNullOrEmpty(Form.GetValueByPath("SELLER_ID")))
            {
                if (Form.GetValueByPath("DEFECT").ToInt() > 0 || !string.IsNullOrEmpty(Form.GetValueByPath("BUYER_ID")))
                {
                    var p = new Dictionary<string, string>();
                    p.Add("SELLER_ID", Form.GetValueByPath("SELLER_ID"));

                    if (Form.GetValueByPath("DEFECT").ToInt() > 0)
                    {
                        p.Add("BUYER_ID", "0");
                    }
                    else
                    {
                        p.Add("BUYER_ID", Form.GetValueByPath("BUYER_ID"));
                    }

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sales");
                    q.Request.SetParam("Object", "Sale");
                    q.Request.SetParam("Action", "ListContract");
                    q.Request.SetParams(p);

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    FormDisableControls();
                    q.DoQuery();
                    FormEnableControls();

                    ContractSelectBox.DropDownListBox.Items.Clear();
                    ContractSelectBox.Items.Clear();
                    ContractSelectBox.DropDownListBox.SelectedItem = null;
                    ContractSelectBox.ValueTextBox.Text = "";
                    ContractSelectBox.IsEnabled = false;
                    Form.SetValueByPath("CONTRACT_ID", "-1");

                    ContractDataSet = new ListDataSet();
                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            ContractDataSet = ListDataSet.Create(result, "ITEMS");
                            if (ContractDataSet != null && ContractDataSet.Items != null && ContractDataSet.Items.Count > 0)
                            {
                                ContractSelectBox.IsEnabled = true;
                            }
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                    ContractSelectBox.SetItems(ContractDataSet, "CONTRACT_ID", "CONTRACT_FULL_NAME");
                }
            }
        }

        /// <summary>
        /// Первичное заполнение поля грузоотправитель по Продавцу, если поле грузоотправитель не было заполнено и если мы только создаём документ
        /// </summary>
        public void CheckAddShipperFilling(bool initializedCheck = true)
        {
            if (initializedCheck)
            {
                if (!(InvoiceId > 0))
                {
                    if (ShipperSelectBox.SelectedItem.Key.IsNullOrEmpty())
                    {
                        if (!Form.GetValueByPath("SELLER_ID").IsNullOrEmpty())
                        {
                            var sellerId = Form.GetValueByPath("SELLER_ID").ToInt();
                            // Если продавец ООО "Торговый Дом Л-ПАК" и грузоотправитель не заполнен,
                            // то по умолчанию выбираем грузоотправителя ОП ООО "Торговый Дом Л-ПАК "Л""
                            if (sellerId == 1)
                            {
                                Form.SetValueByPath("SHIPPER_ID", "434");
                            }
                            // Если продавец Л-Пак Кашира, 
                            // то по умолчанию выбираем грузоотправителя Л-Пак Кашира
                            else if (sellerId == 427)
                            {
                                Form.SetValueByPath("SHIPPER_ID", "427");
                            }
                            else
                            {
                                Form.SetValueByPath("SHIPPER_ID", $"{sellerId}");
                            }
                        }

                    }
                }
            }
            else
            {
                if (ShipperSelectBox.SelectedItem.Key.IsNullOrEmpty())
                {
                    if (!Form.GetValueByPath("SELLER_ID").IsNullOrEmpty())
                    {
                        var sellerId = Form.GetValueByPath("SELLER_ID").ToInt();
                        // Если продавец ООО "Торговый Дом Л-ПАК" и грузоотправитель не заполнен,
                        // то по умолчанию выбираем грузоотправителя ОП ООО "Торговый Дом Л-ПАК "Л""
                        if (sellerId == 1)
                        {
                            Form.SetValueByPath("SHIPPER_ID", "434");
                        }
                        // Если продавец Л-Пак Кашира, 
                        // то по умолчанию выбираем грузоотправителя Л-Пак Кашира
                        else if (sellerId == 427)
                        {
                            Form.SetValueByPath("SHIPPER_ID", "427");
                        }
                        else
                        {
                            Form.SetValueByPath("SHIPPER_ID", $"{sellerId}");
                        }
                    }
                }
            }
        }

        public void SelectBuyer(int type = 1)
        {
            var i = new BuyerList("Preproduction", "ConsumptionList", FrameName);
            i.Type = type;
            i.Show();
        }

        public void BuyerIdTextChanged()
        {
            if (!string.IsNullOrEmpty(Form.GetValueByPath("BUYER_ID")))
            {
                Form.SetValueByPath("CONSIGNEE_ID", "");
                Form.SetValueByPath("CONSIGNEE_NAME", "");

                GetContractList();
            }
        }

        public async void UploadWebDocumentList()
        {
            UploadWebDocumentButton.IsEnabled = false;

            DocumentPrintManager documentPrintManager = new DocumentPrintManager();
            documentPrintManager.RoleName = this.RoleName;
            documentPrintManager.SetSignotory(SignotoryEmployeeId, SignotoryEmployeeName);
            documentPrintManager.InvoiceId = InvoiceId;
            documentPrintManager.FormInit();
            documentPrintManager.SetDefaults();
            documentPrintManager.LoadInvoiceData();
            documentPrintManager.UploadWebDocumentList();

            UploadWebDocumentButton.IsEnabled = true;
        }

        public async void UpdatePayDebtBuyer()
        {
            var p = new Dictionary<string, string>();
            p.Add("INVOICE_ID", InvoiceId.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "Sale");
            q.Request.SetParam("Action", "UpdatePayDebtPokupatel");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            FormDisableControls();
            q.DoQuery();
            FormEnableControls();

            if (q.Answer.Status == 0)
            {
                bool succesFullFlag = false;

                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        if (ds.Items.First().CheckGet("INVOICE_ID").ToInt() > 0)
                        {
                            succesFullFlag = true;
                        }
                    }
                }

                if (succesFullFlag)
                {
                    var msg = "Успешная корректировка данных по оплате покупателем";
                    var d = new DialogWindow($"{msg}", "Редактирование документа", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
                else
                {
                    var msg = "Ошибка корректировки данных по оплате покупателем. Пожалуйста, сообщите о проблеме.";
                    var d = new DialogWindow($"{msg}", "Редактирование документа", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadConsumptionItems();
        }

        private void AddConsumptionButton_Click(object sender, RoutedEventArgs e)
        {
            AddConsumption();
        }

        private void PrintDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            PrintDocument();
        }

        private void ConfirmSelectedConsumptionButton_Click(object sender, RoutedEventArgs e)
        {
            ConfirmConsumption();
        }

        private void UnconfirmSelectedConsumptionButton_Click(object sender, RoutedEventArgs e)
        {
            UnconfirmConsumption();
        }

        private void AdjustmentButton_Click(object sender, RoutedEventArgs e)
        {
            OpenAdjustment();
        }

        private void EditConsumptionButton_Click(object sender, RoutedEventArgs e)
        {
            EditConsumption();
        }

        private void DeleteConsumptionButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteConsumption();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void SellerSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GetContractList();

            CheckAddShipperFilling();
        }

        private void DefectCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            DefectCheckBoxChecked();
        }

        private void ReceiptDateTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ReceiptDateTextChanged();
        }

        private void ShippingDateTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ShippingDateTextChanged();
        }

        private void DefectCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            DefectCheckBoxUnchecked();
        }

        private void SelectBuyerButton_Click(object sender, RoutedEventArgs e)
        {
            SelectBuyer(1);
        }

        private void BuyerIdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            BuyerIdTextChanged();
        }

        private void SelectConsigneeButton_Click(object sender, RoutedEventArgs e)
        {
            SelectBuyer(2);
        }

        private void UploadWebDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            UploadWebDocumentList();
        }

        private void UpdatePayDebtBuyerButton_Click(object sender, RoutedEventArgs e)
        {
            UpdatePayDebtBuyer();
        }

        private void ContractSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CheckActiveContract();
        }

        private void MoveToOtherDocumentSelectedConsumptionButton_Click(object sender, RoutedEventArgs e)
        {
            PrepareToMoveToOtherDocument();
        }

        private void AddVirtualConsumptionButton_Click(object sender, RoutedEventArgs e)
        {
            AddVirtualConsumption();
        }

        private void PrintLabelButton_Click(object sender, RoutedEventArgs e)
        {
            PrintLabel();
        }
    }
}
