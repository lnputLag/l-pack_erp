using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Service.Printing;
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

namespace Client.Interfaces.Sales
{
    /// <summary>
    /// Список накладных расхода
    /// </summary>
    public partial class SaleList : UserControl
    {
        public SaleList()
        {
            InitializeComponent();
            FrameName = "SaleList";

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            InitForm();
            SetDefaults();
            InitGrid();

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
        /// Дата, с которой разрешено редактирование движения товаров, продукции и денег
        /// </summary>
        public string ReportingPeriod { get; set; }

        /// <summary>
        /// Идентификатор сотрудника - подписанта
        /// </summary>
        public int SignotoryEmployeeId { get; set; }

        /// <summary>
        /// Полное имя сотрудника - подписанта
        /// </summary>
        public string SignotoryEmployeeName { get; set; }

        public string RoleName = "[erp]sales_manager";

        /// <summary>
        /// 1 = УПД, 2 = Транспортная накладная, 3 = Накладная на возвратную тару, 4 = Транспортная накладная для СОХ, 5 = УКД
        /// </summary>
        public enum ReturningDocumentType
        {
            UniversalTransferDocument = 1,
            WaybillDocument = 2,
            ResponsibleStockWaybillDocument = 4,
            UniversalAdjustmentDocument = 5
        }

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
                        Header="*",
                        Path="_SELECTED",
                        ColumnType=ColumnTypeRef.Boolean,
                        Editable=true,
                        Width=40,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Path="NSTHET",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=54,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата создания",
                        Path="DATA",
                        ColumnType=ColumnTypeRef.String,
                        Width=63,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ СФ",
                        Path="NAME_SF",
                        ColumnType=ColumnTypeRef.String,
                        Width=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата СФ",
                        Path="DATASTH",
                        ColumnType=ColumnTypeRef.String,
                        Width=63,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ Счёта",
                        Path="NAME_TOVCHEK",
                        ColumnType=ColumnTypeRef.String,
                        Width=47,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата счёта",
                        Path="DATAOPRSTH",
                        ColumnType=ColumnTypeRef.String,
                        Width=63,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ ТТН",
                        Path="NAME_STH",
                        ColumnType=ColumnTypeRef.String,
                        Width=55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ ТН",
                        Path="NAME_PRIH",
                        ColumnType=ColumnTypeRef.String,
                        Width=48,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Покупатель",
                        Path="BUYER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width=250,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Сумма",
                        Path="SUM_PRICE",
                        ColumnType=ColumnTypeRef.Double,
                        Width=65,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Позиций номенклатуры",
                        Path="COUNT_CONSUMPTION",
                        Doc="Количество позиций по артикулу и цене продажи",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=38,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Непроведённых единиц",
                        Path="COUNT_UNCOMPLETED_CONSUMPTION",
                        Doc="Количество не проведённых записей расхода",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=45,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата печати документов",
                        Path="DTTM_PRINTING",
                        ColumnType=ColumnTypeRef.String,
                        Width=63,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // Если документы нужно выгружать в веб и документы ещё не выгружены в веб и печатные документы уже распечатаны
                                    if(row.CheckGet("WEB_DOC_UPLOAD_FLAG").ToInt() == 0 
                                        && !string.IsNullOrEmpty(row.CheckGet("DTTM_PRINTING"))
                                        && (row.CheckGet("WEB_UNIVERSAL_TRANSFER_DOC").ToInt() > 0
                                        || row.CheckGet("WEB_QUALITY_CERTIFICATE").ToInt() > 0
                                        || row.CheckGet("WEB_RECEIPT").ToInt() > 0
                                        || row.CheckGet("WEB_CONSIGNMENT_NOTE").ToInt() > 0
                                        || row.CheckGet("WEB_CMR").ToInt() > 0
                                        || row.CheckGet("WEB_WAYBILL").ToInt() > 0
                                        || row.CheckGet("WEB_SPECIFICATION_ON_PAPER").ToInt() > 0)
                                        )
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
                        Header="Списание в брак",
                        Path="DEFECTIVE_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width=44,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Веб{Environment.NewLine}Документы выгружены в веб",
                        Path="WEB_DOC_UPLOAD_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width=40,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Полный комплект документов",
                        Path="PRINT_KOMPLECT",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width=45,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Цена 0",
                        Path="PRINT_ZERO",
                        Doc="Печатать с ценой 0",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width=45,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Документы возвращены от покупателя",
                        Path="RETURN_DOCUMENTS",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width=45,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ТН возвращена от покупателя",
                        Path="RETURN_TN",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width=45,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Создана корректировка",
                        Path="ADJUSTMENT_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width=44,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Грузополучатель",
                        Path="CONSIGNEE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width=100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Покупатель по договору",
                        Path="CONTRACT_BUYER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width=100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата отгрузки",
                        Path="SHIPMENT_DATA",
                        ColumnType=ColumnTypeRef.String,
                        Width=63,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид отгрузки",
                        Path="SHIPMENT_ID",
                        ColumnType=ColumnTypeRef.String,
                        Width=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Договор",
                        Path="CONTRACT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width=65,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Примечание",
                        Path="COMMENTS",
                        ColumnType=ColumnTypeRef.String,
                        Width=106,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Продавец",
                        Path="SELLER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width=75,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Пользователь",
                        Path="FAKT_FIO",
                        ColumnType=ColumnTypeRef.String,
                        Width=60,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Ид корректировки",
                        Path="ADJUSTMENT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=55,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Тип договора",
                        Path="CONTRACT_TYPE",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=55,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="На терминале",
                        Path="TERMINAL_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=55,
                        MaxWidth=55,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид пользователя",
                        Path="IDFIO",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=55,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид покупателя",
                        Path="BUYER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=55,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид продавца",
                        Path="SELLER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=55,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид смены",
                        Path="ID_TIMES",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=55,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид договора",
                        Path="CONTRACT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=55,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид покупателя по договору",
                        Path="CONTRACT_BUYER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=55,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид грузополучателя",
                        Path="CONSIGNEE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=55,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид покупателя, который оплатил счёт",
                        Path="PAY_DEBT_BUYER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=55,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид перевозчика",
                        Path="TRANSPORTER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=55,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="OPTROZN",
                        Path="OPTROZN",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=55,
                        Hidden=true,
                    }, 
                    new DataGridHelperColumn
                    {
                        Header="Способ оплаты",
                        Path="NALBESNAL",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=55,
                        MaxWidth=55,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Суммарная оплата от покупателя",
                        Path="PAY_DEBT_SUM",
                        ColumnType=ColumnTypeRef.Double,
                        Width=65,
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
                        Header="Формат выгрузки в веб документа УПД",
                        Description="(1 - xls, 2 - jpg)",
                        Path="WEB_UNIVERSAL_TRANSFER_DOC",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Формат выгрузки в веб документа Удостоверение качества",
                        Description="(1 - xls, 2 - jpg)",
                        Path="WEB_QUALITY_CERTIFICATE",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Формат выгрузки в веб документа Счёт",
                        Description="(1 - xls, 2 - jpg)",
                        Path="WEB_RECEIPT",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Формат выгрузки в веб документа Товарно-Транспортная накладная",
                        Description="(1 - xls, 2 - jpg)",
                        Path="WEB_CONSIGNMENT_NOTE",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Формат выгрузки в веб документа CMR",
                        Description="(1 - xls, 2 - jpg)",
                        Path="WEB_CMR",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Формат выгрузки в веб документа Транспортная накладная",
                        Description="(1 - xls, 2 - jpg)",
                        Path="WEB_WAYBILL",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Формат выгрузки в веб документа Спецификация на бумагу",
                        Description="(1 - xls, 2 - jpg)",
                        Path="WEB_SPECIFICATION_ON_PAPER",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },

                    new DataGridHelperColumn
                    {
                        Header=" ",
                        Path="_",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=2,
                        MaxWidth=2000,
                    },
                };
                Grid.SetColumns(columns);

                Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    // определение цветов фона строк
                    {
                        StylerTypeRef.BackgroundColor,
                        (Dictionary<string, string> row) =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            // Суммарная стоимость позиций в накладной не сходится с суммарной оплатой от клиента
                            if (row.CheckGet("SUM_PRICE").ToDouble() != row.CheckGet("PAY_DEBT_SUM").ToDouble())
                            {
                                color = HColor.Red;
                            }

                            // Нет позиций номенклатуры
                            if (row.CheckGet("COUNT_CONSUMPTION").ToInt() == 0)
                            {
                                color = HColor.Blue;
                            }

                            // Есть не проведённые позиции расхода
                            if (row.CheckGet("COUNT_UNCOMPLETED_CONSUMPTION").ToInt() > 0)
                            {
                                 color = HColor.Yellow;
                            }

                            // Если покупатель, который оплатил счёт это не покупатель, который числится в накладной расхода
                            if (row.CheckGet("PAY_DEBT_BUYER_ID").ToInt() > 0 && row.CheckGet("PAY_DEBT_BUYER_ID").ToInt() != row.CheckGet("BUYER_ID").ToInt())
                            {
                                color = HColor.YellowOrange;
                            }

                            // Если покупател по договору это не покупатель, который числится в накладной расхода
                            if (row.CheckGet("CONTRACT_BUYER_ID").ToInt() > 0 && row.CheckGet("CONTRACT_BUYER_ID").ToInt() != row.CheckGet("BUYER_ID").ToInt())
                            {
                                color = HColor.Orange;
                            }

                            // Если тип договора 2 - договор с поставщиком
                            if (row.CheckGet("CONTRACT_TYPE").ToInt() == 2)
                            {
                                color = HColor.Olive;
                            }

                            // Если привязан терминал
                            if (row.CheckGet("TERMINAL_FLAG").ToInt() > 0)
                            {
                                color = HColor.VioletPink;
                            }

                            if (string.IsNullOrEmpty(row.CheckGet("DTTM_PRINTING"))
                                && row.CheckGet("DATA").ToDateTime("dd.MM.yyyy") >= DateTime.Now.AddDays(-1)     
                                && !string.IsNullOrEmpty(row.CheckGet("SHIPMENT_DATA"))
                                && row.CheckGet("TERMINAL_FLAG").ToInt() == 0
                            )
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

                    // определение цветов шрифта строк
                    {
                        StylerTypeRef.ForegroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            // Есть не проведённые позиции расхода
                            if (row.CheckGet("COUNT_UNCOMPLETED_CONSUMPTION").ToInt() > 0)
                            {
                                color = HColor.RedFG;
                            }

                            // Если покупатель, который оплатил счёт это не покупатель, который числится в накладной расхода
                            if (row.CheckGet("PAY_DEBT_BUYER_ID").ToInt() > 0 && row.CheckGet("PAY_DEBT_BUYER_ID").ToInt() != row.CheckGet("BUYER_ID").ToInt())
                            {
                                color = HColor.BlueFg;
                            }

                            // Если покупател по договору это не покупатель, который числится в накладной расхода
                            if (row.CheckGet("CONTRACT_BUYER_ID").ToInt() > 0 && row.CheckGet("CONTRACT_BUYER_ID").ToInt() != row.CheckGet("BUYER_ID").ToInt())
                            {
                                color = HColor.BlueFG;
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },
                };
                Grid.AutoUpdateInterval = 0;
                Grid.SearchText = SearchText;
                Grid.OnLoadItems = LoadItems;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                Grid.OnSelectItem = selectedItem =>
                {
                    GridSelectedItem = selectedItem;
                    SetActionEnabled();
                };

                //двойной клик на строке откроет форму редактирования
                Grid.OnDblClick = selectedItem =>
                {
                    EditDocument();
                };

                Grid.OnFilterItems = () =>
                {
                    if (Grid.GridItems != null)
                    {
                        if (Grid.GridItems.Count > 0)
                        {
                            // Фильтрация по площадке
                            if (FactorySelectBox.SelectedItem.Key != null)
                            {
                                var key = FactorySelectBox.SelectedItem.Key.ToInt();
                                var items = new List<Dictionary<string, string>>();

                                items.AddRange(Grid.GridItems.Where(x => x.CheckGet("FACTORY_ID").ToInt() == key));

                                Grid.GridItems = items;
                            }

                            if (SellerSelectBox.SelectedItem.Key != null)
                            {
                                var customerId = SellerSelectBox.SelectedItem.Key.ToInt();
                                var items = new List<Dictionary<string, string>>();

                                switch (customerId)
                                {
                                    case -1:
                                        items = Grid.GridItems;
                                        break;

                                    default:
                                        items.AddRange(Grid.GridItems.Where(row => row.CheckGet("SELLER_ID").ToInt() == customerId));
                                        break;
                                }

                                Grid.GridItems = items;
                            }

                            if (ContractTypeSelectBox.SelectedItem.Key != null)
                            {
                                var contractType = ContractTypeSelectBox.SelectedItem.Key.ToInt();
                                var items = new List<Dictionary<string, string>>();

                                switch (contractType)
                                {
                                    case 0:
                                        items = Grid.GridItems;
                                        break;

                                    default:
                                        items.AddRange(Grid.GridItems.Where(row => row.CheckGet("CONTRACT_TYPE").ToInt() == contractType));
                                        break;
                                }

                                Grid.GridItems = items;
                            }

                            if (WithAdjustmentCheckBox != null)
                            {
                                var items = new List<Dictionary<string, string>>();

                                if (WithAdjustmentCheckBox.IsChecked == true)
                                {
                                    items.AddRange(Grid.GridItems.Where(row => row.CheckGet("ADJUSTMENT_ID").ToInt() > 0));
                                }
                                else
                                {
                                    items = Grid.GridItems;
                                }

                                Grid.GridItems = items;
                            }

                            if (NeedWebDocCheckBox != null)
                            {
                                var items = new List<Dictionary<string, string>>();

                                if (NeedWebDocCheckBox.IsChecked == true)
                                {
                                    items.AddRange(Grid.GridItems.Where(row =>
                                        row.CheckGet("WEB_DOC_UPLOAD_FLAG").ToInt() == 0
                                        && !string.IsNullOrEmpty(row.CheckGet("DTTM_PRINTING"))
                                        && (row.CheckGet("WEB_UNIVERSAL_TRANSFER_DOC").ToInt() > 0
                                        || row.CheckGet("WEB_QUALITY_CERTIFICATE").ToInt() > 0
                                        || row.CheckGet("WEB_RECEIPT").ToInt() > 0
                                        || row.CheckGet("WEB_CONSIGNMENT_NOTE").ToInt() > 0
                                        || row.CheckGet("WEB_CMR").ToInt() > 0
                                        || row.CheckGet("WEB_WAYBILL").ToInt() > 0
                                        || row.CheckGet("WEB_SPECIFICATION_ON_PAPER").ToInt() > 0)
                                    ));
                                }
                                else
                                {
                                    items = Grid.GridItems;
                                }

                                Grid.GridItems = items;
                            }
                        }
                    }
                };

                // контекстное меню
                Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    //{
                    //    "UploadWebDocument",
                    //    new DataGridContextMenuItem()
                    //    {
                    //        Header="Выгрузить все документы в веб",
                    //        Action=()=>
                    //        {
                    //            UploadWebDocument();
                    //        }
                    //    }
                    //},
                    {
                        "EditDocument",
                        new DataGridContextMenuItem()
                        {
                            Header="Открыть",
                            Action=()=>
                            {
                                EditDocument();
                            }
                        }
                    },
                    { "s0", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    {
                        "DeleteDocument",
                        new DataGridContextMenuItem()
                        {
                            Header="Удалить",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                DeleteDocument();
                            }
                        }
                    },
                    { "s1", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    {
                        "UpdateReturnUPDFlag1",
                        new DataGridContextMenuItem()
                        {
                            Header="Отметить возврат УПД",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                UpdateReturnDocumentFlag(1, ReturningDocumentType.UniversalTransferDocument);
                            }
                        }
                    },
                    {
                        "UpdateReturnUPDFlag0",
                        new DataGridContextMenuItem()
                        {
                            Header="Снять отметку возврата УПД",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                UpdateReturnDocumentFlag(0, ReturningDocumentType.UniversalTransferDocument);
                            }
                        }
                    },
                    { "s2", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    {
                        "UpdateReturnTNFlag1",
                        new DataGridContextMenuItem()
                        {
                            Header="Отметить возврат ТН",
                            Action=()=>
                            {
                                UpdateReturnDocumentFlag(1, ReturningDocumentType.WaybillDocument);
                            }
                        }
                    },
                    {
                        "UpdateReturnTNFlag0",
                        new DataGridContextMenuItem()
                        {
                            Header="Снять отметку возврата ТН",
                            Action=()=>
                            {
                                UpdateReturnDocumentFlag(0, ReturningDocumentType.WaybillDocument);
                            }
                        }
                    },
                };

                Grid.Init();
                Grid.Run();
            }
        }

        public async void LoadItems()
        {
            DisableControls();

            bool resume = true;

            var f = Form.GetValueByPath("INVOICE_DATE_FROM").ToDateTime();
            var t = Form.GetValueByPath("INVOICE_DATE_TO").ToDateTime();

            if (DateTime.Compare(f, t) > 0)
            {
                const string msg = "Дата начала должна быть меньше даты окончания.";
                var d = new DialogWindow($"{msg}", "Список продаж");
                d.ShowDialog();
                resume = false;
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();
                p.Add("INVOICE_DATE_FROM", Form.GetValueByPath("INVOICE_DATE_FROM"));
                p.Add("INVOICE_DATE_TO", Form.GetValueByPath("INVOICE_DATE_TO"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "Sale");
                q.Request.SetParam("Action", "List");
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

                        if (Grid.Items != null && GridDataSet.Items != null && Grid.Items.Count > 0 && GridDataSet.Items.Count > 0)
                        {
                            foreach (var item in GridDataSet.Items)
                            {
                                var row = Grid.Items.FirstOrDefault(x => x.CheckGet("NSTHET").ToInt() == item.CheckGet("NSTHET").ToInt());
                                if (row != null)
                                {
                                    item.CheckAdd("_SELECTED", row.CheckGet("_SELECTED"));
                                }
                                else
                                {
                                    item.CheckAdd("_SELECTED", "0");
                                }
                            }
                        }

                        Grid.UpdateItems(GridDataSet);

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
            }

            EnableControls();
        }

        public void SetDefaults()
        {
            GridSelectedItem = new Dictionary<string, string>();
            GridDataSet = new ListDataSet();
            Form.SetDefaults();

            Form.SetValueByPath("INVOICE_DATE_FROM", DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy"));
            Form.SetValueByPath("INVOICE_DATE_TO", DateTime.Now.ToString("dd.MM.yyyy"));

            var factorySelectBoxItems = new Dictionary<string, string>();
            factorySelectBoxItems.Add("1", "Л-ПАК ЛИПЕЦК");
            factorySelectBoxItems.Add("2", "Л-ПАК КАШИРА");
            FactorySelectBox.SetItems(factorySelectBoxItems);
            FactorySelectBox.SetSelectedItemByKey("1");

            var sellerSelectBoxItems = new Dictionary<string, string>();
            sellerSelectBoxItems.Add("-1", "Все продавцы");
            sellerSelectBoxItems.Add("1", "ООО \"ТД Л-ПАК\"");
            sellerSelectBoxItems.Add("2", "ООО \"Л-ПАК\"");
            sellerSelectBoxItems.Add("427", "ООО \"Л-ПАК Кашира\"");
            sellerSelectBoxItems.Add("0", "ООО \"Л-Пак\"");
            SellerSelectBox.SetItems(sellerSelectBoxItems);
            SellerSelectBox.SetSelectedItemByKey("-1");

            var contractTypeSelectBoxItems = new Dictionary<string, string>();
            contractTypeSelectBoxItems.Add("0", "Все типы договора");
            contractTypeSelectBoxItems.Add("1", "Основной договор");
            contractTypeSelectBoxItems.Add("3", "Договор поставки БК");
            contractTypeSelectBoxItems.Add("6", "Договор поставки ЛТ");
            contractTypeSelectBoxItems.Add("7", "Договор поставки макулатуры");
            contractTypeSelectBoxItems.Add("5", "Договор ИМ");
            contractTypeSelectBoxItems.Add("2", "Договор с поставщиком");
            contractTypeSelectBoxItems.Add("4", "Прочие продажи");
            ContractTypeSelectBox.SetItems(contractTypeSelectBoxItems);
            ContractTypeSelectBox.SetSelectedItemByKey("0");

            GetDefaultSignotory(); 
        }

        public void SetFormDateInReportingPeriod()
        {
            if (!string.IsNullOrEmpty(ReportingPeriod))
            {
                Form.SetValueByPath("INVOICE_DATE_FROM", ReportingPeriod);
                Form.SetValueByPath("INVOICE_DATE_TO", DateTime.Now.ToString("dd.MM.yyyy"));

                LoadItems();
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
                    BurgerChoiseSignotory.IsEnabled = true;
                    break;

                case Role.AccessMode.FullAccess:
                    BurgerChoiseSignotory.IsEnabled = true;
                    break;

                case Role.AccessMode.ReadOnly:
                default:
                    BurgerChoiseSignotory.IsEnabled = false;
                    break;
            }

            //UIUtil.SetFrameworkElementEnabledByTagAccessMode(this.Content as DependencyObject, Acl.AccessMode.ReadOnly);

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
        /// Открывает форму просмотра позиций расхода по выбранной накладной
        /// </summary>
        public void EditDocument()
        {
            if (GridSelectedItem != null)
            {
                var i = new ConsumptionList();
                i.InvoiceId = GridSelectedItem.CheckGet("NSTHET").ToInt();
                i.ReportingPeriod = ReportingPeriod;
                i.ParentFrame = FrameName;
                i.SignotoryEmployeeName = SignotoryEmployeeName;
                i.SignotoryEmployeeId = SignotoryEmployeeId;
                i.Show();
            }
        }

        /// <summary>
        /// Создание новой накладной
        /// </summary>
        public void NewDocument()
        {
            var i = new ConsumptionList();
            i.ReportingPeriod = ReportingPeriod;
            i.ParentFrame = FrameName;
            i.SignotoryEmployeeId = SignotoryEmployeeId;
            i.SignotoryEmployeeName = SignotoryEmployeeName;
            i.Show();
        }

        /// <summary>
        /// Удаление выбранной накладной
        /// </summary>
        public void DeleteDocument()
        {
            {
                string msg = $"Удалить накладную расхода {GridSelectedItem.CheckGet("NSTHET").ToInt()} № {GridSelectedItem.CheckGet("NAME_SF")} ?";
                var d = new DialogWindow($"{msg}", "Список продаж", "", DialogWindowButtons.NoYes);
                if (d.ShowDialog() == false)
                {
                    return;
                }
            }

            DisableControls();

            var p = new Dictionary<string, string>();
            p.Add("INVOICE_ID", GridSelectedItem.CheckGet("NSTHET"));

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "Sale");
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
                    LoadItems();
                }
                else
                {
                    string msg = $"При удалении накладной произошла ошибка. Пожалуйста, сообщите о проблеме.";
                    var d = new DialogWindow($"{msg}", "Список продаж", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                q.ProcessError();
            }

            EnableControls();
        }

        public void UpdateReturnDocumentFlag(int returnFlag, ReturningDocumentType documentType)
        {
            if (GridSelectedItem != null && GridSelectedItem.Count > 0)
            {
                DisableControls();

                var p = new Dictionary<string, string>();
                p.Add("INVOICE_ID", GridSelectedItem.CheckGet("NSTHET"));
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

                if (q.Answer.Status == 0)
                {
                    bool succesfullFlag = false;

                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var dataSet = ListDataSet.Create(result, "ITEMS");
                        if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                        {
                            if (dataSet.Items[0].CheckGet("INVOICE_ID").ToInt() > 0)
                            {
                                succesfullFlag = true;
                            }
                        }
                    }

                    if (succesfullFlag)
                    {
                        LoadItems();
                    }
                    else
                    {
                        string msg = $"При изменении отметки возврата документа по накладной произошла ошибка. Пожалуйста, сообщите о проблеме.";
                        var d = new DialogWindow($"{msg}", "Список продаж", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }

                EnableControls();
            }
            else
            {
                string msg = $"Не выбрана накладная для изменения отметки возврата документа.";
                var d = new DialogWindow($"{msg}", "Список продаж", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Печать утренних документов
        /// </summary>
        public async void PrintMorningDocument()
        {
            DisableControls();

            bool resume = true;

            var f = Form.GetValueByPath("INVOICE_DATE_FROM").ToDateTime();
            var t = Form.GetValueByPath("INVOICE_DATE_TO").ToDateTime();

            if (DateTime.Compare(f, t) > 0)
            {
                string msg = "Дата начала должна быть меньше даты окончания.";
                var d = new DialogWindow($"{msg}", "Список продаж");
                d.ShowDialog();
                resume = false;
            }

            if (resume)
            {
                string msg = $"Распечатать утренние документы для накладных с {Form.GetValueByPath("INVOICE_DATE_FROM")} по {Form.GetValueByPath("INVOICE_DATE_TO")}?";
                var d = new DialogWindow($"{msg}", "Список продаж", "", DialogWindowButtons.YesNo);
                if (d.ShowDialog() == false)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                SplashControl.Message = $"Пожалуйста, подождите.{Environment.NewLine}Идёт печать документов.";
                SplashControl.Visible = true;

                List<Dictionary<string, string>> gridItems = new List<Dictionary<string, string>>();
                {
                    var p = new Dictionary<string, string>();
                    p.Add("INVOICE_DATE_FROM", Form.GetValueByPath("INVOICE_DATE_FROM"));
                    p.Add("INVOICE_DATE_TO", Form.GetValueByPath("INVOICE_DATE_TO"));

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sales");
                    q.Request.SetParam("Object", "Sale");
                    q.Request.SetParam("Action", "List");
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
                            var dataSet = ListDataSet.Create(result, "ITEMS");
                            if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                            {
                                gridItems = dataSet.Items;
                            }
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }

                if (gridItems != null && gridItems.Count > 0)
                {
                    // 1 -- Дата накладной в выбранном диапазоне
                    // 2 -- Продацец = "ТД Л-Пак"
                    // 3 -- Пустой номер счёта
                    // 4 -- Номер счёт-фактуры не пустой и не "0"
                    // 5 -- Это не интернет-реализация
                    gridItems = gridItems.Where(x =>
                        x.CheckGet("DATAOPRSTH").ToDateTime() >= f
                        && x.CheckGet("DATAOPRSTH").ToDateTime() <= t
                        && x.CheckGet("SELLER_ID").ToInt() == 1
                        && string.IsNullOrEmpty(x.CheckGet("NAME_TOVCHEK"))
                        && !string.IsNullOrEmpty(x.CheckGet("NAME_SF"))
                        && x.CheckGet("NAME_SF") != "0"
                        && x.CheckGet("CONTRACT_TYPE").ToInt() != 5
                        )?.ToList();

                    foreach (var item in gridItems)
                    {
                        var p = new Dictionary<string, string>();
                        p.Add("BUYER_ID", item.CheckGet("BUYER_ID"));

                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Sales");
                        q.Request.SetParam("Object", "Sale");
                        q.Request.SetParam("Action", "ListMorningDocument");
                        q.Request.SetParams(p);
                        q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                        q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                        await Task.Run(() =>
                        {
                            q.DoQuery();
                        });

                        Dictionary<string, string> documentCountList = new Dictionary<string, string>();
                        if (q.Answer.Status == 0)
                        {
                            var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                            if (result != null)
                            {
                                var dataSet = ListDataSet.Create(result, "ITEMS");
                                if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                                {
                                    documentCountList = dataSet.Items.First();
                                }
                            }
                        }

                        if (documentCountList != null && documentCountList.Count > 0 && documentCountList.Count(x => x.Value.ToInt() > 0) > 0)
                        {
                            DocumentPrintManager documentPrintManager = new DocumentPrintManager();
                            documentPrintManager.RoleName = this.RoleName;
                            documentPrintManager.HiddenInterface = true;
                            documentPrintManager.SetSignotory(SignotoryEmployeeId, SignotoryEmployeeName);
                            documentPrintManager.FormInit();
                            documentPrintManager.SetDefaults();
                            documentPrintManager.Form.SetValues(documentCountList);
                            documentPrintManager.InvoiceId = item.CheckGet("NSTHET").ToInt();
                            documentPrintManager.LoadInvoiceData();
                            documentPrintManager.PrintDocument();
                            //documentPrintManager.HtmlDocument();
                        }
                    }
                }

                SplashControl.Message = "";
                SplashControl.Visible = false;
            }

            EnableControls();
        }

        public async void UploadWebDocument()
        {
            DisableControls();

            bool resume = true;

            var f = Form.GetValueByPath("INVOICE_DATE_FROM").ToDateTime();
            var t = Form.GetValueByPath("INVOICE_DATE_TO").ToDateTime();

            if (DateTime.Compare(f, t) > 0)
            {
                string msg = "Дата начала должна быть меньше даты окончания.";
                var d = new DialogWindow($"{msg}", "Список продаж");
                d.ShowDialog();
                resume = false;
            }

            if (resume)
            {
                string msg = $"Выгрузить веб документы для накладных с {Form.GetValueByPath("INVOICE_DATE_FROM")} по {Form.GetValueByPath("INVOICE_DATE_TO")}?";
                var d = new DialogWindow($"{msg}", "Список продаж", "", DialogWindowButtons.YesNo);
                if (d.ShowDialog() == false)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                List<Dictionary<string, string>> gridItems = new List<Dictionary<string, string>>();
                {
                    var p = new Dictionary<string, string>();
                    p.Add("INVOICE_DATE_FROM", Form.GetValueByPath("INVOICE_DATE_FROM"));
                    p.Add("INVOICE_DATE_TO", Form.GetValueByPath("INVOICE_DATE_TO"));

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sales");
                    q.Request.SetParam("Object", "Sale");
                    q.Request.SetParam("Action", "List");
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
                                gridItems = dataSet.Items;
                            }
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }

                if (gridItems != null && gridItems.Count > 0)
                {
                    // 1 -- Дата накладной в выбранном диапазоне
                    // 2 -- Продацец = "ТД Л-Пак"
                    // 3 -- Пустой номер счёта
                    // 4 -- Номер счёт-фактуры не пустой и не "0"
                    // 5 -- Это не интернет-реализация
                    gridItems = gridItems.Where(x =>
                        x.CheckGet("DATAOPRSTH").ToDateTime() >= f
                        && x.CheckGet("DATAOPRSTH").ToDateTime() <= t
                        && x.CheckGet("SELLER_ID").ToInt() == 1
                        && !string.IsNullOrEmpty(x.CheckGet("NAME_SF"))
                        && x.CheckGet("NAME_SF") != "0"
                        && x.CheckGet("CONTRACT_TYPE").ToInt() != 5
                        )?.ToList();

                    foreach (var item in gridItems)
                    {
                        DocumentPrintManager documentPrintManager = new DocumentPrintManager();
                        documentPrintManager.RoleName = this.RoleName;
                        documentPrintManager.SetSignotory(SignotoryEmployeeId, SignotoryEmployeeName);
                        documentPrintManager.FormInit();
                        documentPrintManager.SetDefaults();
                        documentPrintManager.InvoiceId = item.CheckGet("NSTHET").ToInt();
                        documentPrintManager.LoadInvoiceData();
                        documentPrintManager.UploadWebDocumentList();
                    }
                }
            }

            EnableControls();
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
                SenderName = "SalesList",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            Grid.Destruct();
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.IndexOf("Preproduction") > -1)
            {
                if (m.ReceiverName.IndexOf("SaleList") > -1)
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            LoadItems();
                            break;

                        case "SetSignotory":
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
                            break;

                        case "Find":
                            if (m.ContextObject != null)
                            {
                                try
                                {
                                    Dictionary<string, string> context = (Dictionary<string, string>)m.ContextObject;
                                    if (context != null && context.Count > 0)
                                    {
                                        string invoiceNumber = context.CheckGet("INVOICE_ID");
                                        string invoiceDate = context.CheckGet("INVOICE_DATE");
                                        if (!string.IsNullOrEmpty(invoiceNumber) && !string.IsNullOrEmpty(invoiceDate))
                                        {
                                            SearchText.Text = invoiceNumber;
                                            Form.SetValueByPath("INVOICE_DATE_FROM", invoiceDate);
                                            Form.SetValueByPath("INVOICE_DATE_TO", invoiceDate);
                                            SellerSelectBox.SetSelectedItemFirst();
                                            ContractTypeSelectBox.SetSelectedItemFirst();
                                            WithAdjustmentCheckBox.IsChecked = false;

                                            LoadItems();
                                        }
                                    }
                                }
                                catch (Exception)
                                {
                                }
                            }
                            break;
                    }
                }
            }
        }

        public void SetActionEnabled()
        {
            Grid.Menu["DeleteDocument"].Enabled = false;
            DeleteDocumentButton.IsEnabled = false;
            Grid.Menu["UpdateReturnUPDFlag1"].Enabled = false;
            Grid.Menu["UpdateReturnUPDFlag0"].Enabled = false;
            Grid.Menu["UpdateReturnTNFlag1"].Enabled = false;
            Grid.Menu["UpdateReturnTNFlag0"].Enabled = false;

            if (!string.IsNullOrEmpty(ReportingPeriod))
            {
                if (GridSelectedItem.CheckGet("DATA").ToDateTime() >= ReportingPeriod.ToDateTime())
                {
                    if (GridSelectedItem.CheckGet("COUNT_CONSUMPTION").ToInt() == 0)
                    {
                        Grid.Menu["DeleteDocument"].Enabled = true;
                        DeleteDocumentButton.IsEnabled = true;
                    }
                }
            }

            if (GridSelectedItem.CheckGet("RETURN_DOCUMENTS").ToInt() > 0)
            {
                Grid.Menu["UpdateReturnUPDFlag0"].Enabled = true;
            }
            else
            {
                Grid.Menu["UpdateReturnUPDFlag1"].Enabled = true;
            }

            if (GridSelectedItem.CheckGet("RETURN_TN").ToInt() > 0)
            {
                Grid.Menu["UpdateReturnTNFlag0"].Enabled = true;
            }
            else
            {
                Grid.Menu["UpdateReturnTNFlag1"].Enabled = true;
            }

            ProcessPermissions();
        }

        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
        }

        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
        }

        /// <summary>
        /// Установка настроек для принтера
        /// </summary>
        public void SetPrintSettings()
        {
            var i = new PrintingInterface();
        }

        public void ChoiseSignotory()
        {
            var w = new ChoiseSignotory();
            w.ParentFrame = FrameName;
            w.Show();
        }

        public void ScanDocument()
        {
            var w = new ScanDocument();
            w.ParentFrame = FrameName;
            w.Show();
        }

        public void PrintSelectedDocument()
        {
            if (Grid.GetSelectedItems().Count > 0)
            {
                var documentListChoise = new DocumentListChoise();
                documentListChoise.SignotoryEmployeeId = this.SignotoryEmployeeId;
                documentListChoise.SignotoryEmployeeName = this.SignotoryEmployeeName;
                documentListChoise.RoleName = this.RoleName;
                documentListChoise.OnSaveVoid = PrintSelectedDocument2;
                documentListChoise.Show();
            }
            else
            {
                string msg = "Не выбраны накладные для печати документов.";
                var d = new DialogWindow($"{msg}", "Список продаж");
                d.ShowDialog();
            }
        }

        public void PrintSelectedDocument2(Dictionary<string, string> documentCountList)
        {
            var selectedItemList = Grid.GetSelectedItems();
            if (selectedItemList.Count > 0)
            {
                string msg = $"Распечатать указанный пакет документов для {selectedItemList.Count} отмеченных накладных?";
                var d = new DialogWindow($"{msg}", "Список продаж", "", DialogWindowButtons.YesNo);
                if (d.ShowDialog() == true)
                {
                    foreach (var item in selectedItemList)
                    {
                        DocumentPrintManager documentPrintManager = new DocumentPrintManager();
                        documentPrintManager.RoleName = this.RoleName;
                        documentPrintManager.HiddenInterface = true;
                        documentPrintManager.SetSignotory(SignotoryEmployeeId, SignotoryEmployeeName);
                        documentPrintManager.FormInit();
                        documentPrintManager.SetDefaults();
                        documentPrintManager.Form.SetValues(documentCountList);
                        documentPrintManager.InvoiceId = item.CheckGet("NSTHET").ToInt();
                        documentPrintManager.LoadInvoiceData();
                        documentPrintManager.PrintDocument();
                        //documentPrintManager.HtmlDocument();

                        item.CheckAdd("_SELECTED", "0");
                    }

                    Grid.UpdateItems();
                }
            }
            else
            {
                string msg = "Не выбраны накладные для печати документов.";
                var d = new DialogWindow($"{msg}", "Список продаж");
                d.ShowDialog();
            }
        }

        /// <summary>
        /// отображение справочной статьи
        /// (относительный путь)
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp-new/Sales/list_sales");
            //Central.ShowHelp("/doc/l-pack-erp/sales/sale_list");
        }

        private void ResreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadItems();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void SellerSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void ContractTypeSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void EditDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            EditDocument();
        }

        private void NewDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            NewDocument();
        }

        private void DeleteDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteDocument();
        }

        private void WithAdjustmentCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void WithAdjustmentCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void PrintMorningDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            PrintMorningDocument();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings();
        }

        private void BurgerChoiseSignotory_Click(object sender, RoutedEventArgs e)
        {
            ChoiseSignotory();
        }

        private void ScanDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            ScanDocument();
        }

        private void ReportingPeriodMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SetFormDateInReportingPeriod();
        }

        private void FactorySelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void PrintSelectedDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            PrintSelectedDocument();
        }

        private void NeedWebDocCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void NeedWebDocCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Grid.UpdateItems();
        }
    }
}
