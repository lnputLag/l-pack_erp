using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Service.Printing;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
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

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Интерфейс поиска информации по перемещению поддона
    /// </summary>
    public partial class PalletSearcher : UserControl
    {
        public PalletSearcher()
        {
            InitializeComponent();

            Loaded += (object sender, RoutedEventArgs e) =>
            {
                Central.WM.SelectedTab = "Pallet_Searcher";
            };

            if (Central.DebugMode)
            {
                ViewHtmlLabelButton.Visibility = Visibility.Visible;
                ViewTemplateButton.Visibility = Visibility.Visible;
                ViewHtmlTemplateButton.Visibility = Visibility.Visible;
            }
            else
            {
                ViewHtmlLabelButton.Visibility = Visibility.Collapsed;
                ViewTemplateButton.Visibility = Visibility.Collapsed;
                ViewHtmlTemplateButton.Visibility = Visibility.Collapsed;
            }

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Init();
            SetDefaults();
            PalletGridInit();

            ProcessPermissions();
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Датасет с данными для грида поддонов
        /// </summary>
        public ListDataSet PalletGridDataSet { get; set; }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void Init()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="SEARCH_BARCODE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=SearchBarcodeTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                         { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="SEARCH_PRODUCTION_TASK_NUMBER",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=SearchProductionTaskNumberTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                         { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="SEARCH_PALLET_NUMBER",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=SearchPalletNumberTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                         { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="SEARCH_PRODUCT_NAME",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = SearchProductNameTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="SEARCH_PRODUCT_CODE",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = SearchProductCodeTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="SEARCH_CUSTOMER_NAME",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = SearchCustomerNameTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="SEARCH_ORDER_NUMBER",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = SearchOrderNumberTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="SEARCH_ORDER_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=SearchOrderIdTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                         { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="SEARCH_ORDER_POSITION_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=SearchOrderPositionIdTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                         { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="SEARCH_SHIPMENT_DATETIME",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control=SearchShipmentDateTime,
                    ControlType="TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="SEARCH_FACTORY_ID",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control=FactorySelectBox,
                    ControlType="SelectBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="SEARCH_IN_BALANCE_FLAG",
                    FieldType = FormHelperField.FieldTypeRef.Boolean,
                    Control=InBalanceCheckBox,
                    ControlType="CheckBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },

                new FormHelperField()
                {
                    Path="MOVING_HISTORY",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = MovingHistoryTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },

                new FormHelperField()
                {
                    Path="QUANTITY_ON_PALLET",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = QunatityOnPalletTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="DEF_QUANTITY_ON_PALLET",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = DefaultQunatityOnPalletTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="COUNT_CARDBOARD_BY_TASK",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = QuantityByTaskTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="COUNT_PALLET_BY_TASK",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = CountPalletByTaskTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="SKLAD",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = SkladTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="NUM_PLACE",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = NumPlaceTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="SHIPPING_DATETIME",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = ShipingDateTimeTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },

                new FormHelperField()
                {
                    Path="PRODUCT_NAME",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = ProductNameTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="CODE",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = CodeTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="DIMENSIONS",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = ProductDimensionsTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_CATEGORY",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = ProductCategoryTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="DEF_PALLET",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = DefaultPalletTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="DEF_LAYING_SCHEME",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = LayingTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="ORDER_NAME",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = OrderNameTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },

                new FormHelperField()
                {
                    Path="ORDER_NUMBER",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = null,
                    ControlType = "void",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="ORDER_ID",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = null,
                    ControlType = "void",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="INCOMING_ID",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = null,
                    ControlType = "void",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCTION_TASK_ID",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = null,
                    ControlType = "void",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="PALLET_NUM",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = null,
                    ControlType = "void",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_IDK1",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = null,
                    ControlType = "void",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
            };

            Form.SetFields(fields);
        }

        /// <summary>
        /// инициализация грида найденных поддонов
        /// </summary>
        public void PalletGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Path="PALLET_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Создан",
                        Path="PALLET_CREATE_DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm",
                        Width=95,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ПЗ",
                        Path="PRODUCTION_TASK_NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Width=75,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер поддона",
                        Path="PALLET_NUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=40,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Path="PRODUCT_CODE",
                        ColumnType=ColumnTypeRef.String,
                        Width=120,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Продукция",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width=320,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер заявки",
                        Path="ORDER_NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Width=95,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата отгрузки",
                        Path="SHIPMENT_DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH",
                        Width=80,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид ПЗ",
                        Path="PRODUCTION_TASK_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=55,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Перестил",
                        Path="SUBPRODUCT_FLAG",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=30,
                        MaxWidth=55,
                        Hidden=true,
                    },

                    new DataGridHelperColumn
                    {
                        Header=" ",
                        Path="_",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=5,
                        MaxWidth=2000,
                    },
                };
                PalletGrid.SetColumns(columns);

                PalletGrid.PrimaryKey = "PALLET_ID";
                PalletGrid.SearchText = PalletGridSearchBox;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                PalletGrid.OnSelectItem = selectedItem =>
                {
                    GetPalletData();
                    ProcessPermissions();
                };

                PalletGrid.Init();
                PalletGrid.Run();
            }
        }

        /// <summary>
        /// Обработчик для сканера
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;

            switch (e.Key)
            {
                case Key.Down:
                case Key.Enter:
                    var code = Central.WM.GetScannerInput();

                    if (!string.IsNullOrEmpty(code))
                    {
                        SearchBarcodeTextBox.Text = code;
                    }

                    break;
            }
        }

        /// <summary>
        /// Проверяем, что заполнена информация для поиска
        /// </summary>
        public bool CheckFieldsForSearch()
        {
            bool resultValidateFlag = false;

            if (Form != null)
            {
                if (Form.Validate())
                {
                    bool localValide = false;

                    Dictionary<string, string> formValues = Form.GetValues();
                    if (formValues != null && formValues.Count > 0)
                    {
                        if (
                            (!string.IsNullOrEmpty(formValues.CheckGet("SEARCH_BARCODE")) && formValues.CheckGet("SEARCH_BARCODE").Length >= 8)
                            || (!string.IsNullOrEmpty(formValues.CheckGet("SEARCH_PRODUCTION_TASK_NUMBER")) && formValues.CheckGet("SEARCH_PRODUCTION_TASK_NUMBER").Length == 4)
                            || !string.IsNullOrEmpty(formValues.CheckGet("SEARCH_PRODUCT_NAME"))
                            || (!string.IsNullOrEmpty(formValues.CheckGet("SEARCH_PRODUCT_CODE")) && formValues.CheckGet("SEARCH_PRODUCT_CODE").Length >= 7)
                            || !string.IsNullOrEmpty(formValues.CheckGet("SEARCH_ORDER_ID"))
                            || !string.IsNullOrEmpty(formValues.CheckGet("SEARCH_ORDER_POSITION_ID"))
                            || !string.IsNullOrEmpty(formValues.CheckGet("SEARCH_ORDER_NUMBER"))
                            || !string.IsNullOrEmpty(formValues.CheckGet("SEARCH_SHIPMENT_DATETIME"))
                            || !string.IsNullOrEmpty(formValues.CheckGet("SEARCH_CUSTOMER_NAME"))
                            )
                        {
                            localValide = true;
                        }                        
                    }

                    if (!localValide)
                    {
                        Form.Valid = false;

                        // Красный
                        var color = "#ffee0000";
                        var bc = new BrushConverter();
                        var brush = (Brush)bc.ConvertFrom(color);

                        SearchProductionTaskNumberTextBox.BorderBrush = brush;
                        SearchProductNameTextBox.BorderBrush = brush;
                        SearchProductCodeTextBox.BorderBrush = brush;
                        SearchBarcodeTextBox.BorderBrush = brush;
                        SearchOrderIdTextBox.BorderBrush = brush;
                        SearchCustomerNameTextBox.BorderBrush = brush;
                        SearchOrderNumberTextBox.BorderBrush = brush;
                        SearchOrderPositionIdTextBox.BorderBrush = brush;
                        SearchShipmentDateTime.BorderBrush = brush;
                    }
                    else
                    {
                        // Серый (дефолтный)
                        var color = "#ffcccccc";
                        var bc = new BrushConverter();
                        var brush = (Brush)bc.ConvertFrom(color);

                        SearchProductionTaskNumberTextBox.BorderBrush = brush;
                        SearchProductNameTextBox.BorderBrush = brush;
                        SearchProductCodeTextBox.BorderBrush = brush;
                        SearchBarcodeTextBox.BorderBrush = brush;
                        SearchOrderIdTextBox.BorderBrush = brush;
                        SearchCustomerNameTextBox.BorderBrush = brush;
                        SearchOrderNumberTextBox.BorderBrush = brush;
                        SearchOrderPositionIdTextBox.BorderBrush = brush;
                        SearchShipmentDateTime.BorderBrush = brush;
                    }
                }
            }

            resultValidateFlag = Form.Valid;
            return resultValidateFlag;
        }

        /// <summary>
        /// Очищаем поля с получаемыми данными выбранного поддона
        /// </summary>
        public void ClearDataFields()
        {
            Form.SetValueByPath("ORDER_NAME", "");
            Form.SetValueByPath("DEF_LAYING_SCHEME", "");
            Form.SetValueByPath("DEF_PALLET", "");
            Form.SetValueByPath("PRODUCT_CATEGORY", "");
            Form.SetValueByPath("DIMENSIONS", "");
            Form.SetValueByPath("CODE", "");
            Form.SetValueByPath("PRODUCT_NAME", "");
            Form.SetValueByPath("SHIPPING_DATETIME", "");
            Form.SetValueByPath("NUM_PLACE", "");
            Form.SetValueByPath("SKLAD", "");
            Form.SetValueByPath("COUNT_PALLET_BY_TASK", "");
            Form.SetValueByPath("COUNT_CARDBOARD_BY_TASK", "");
            Form.SetValueByPath("DEF_QUANTITY_ON_PALLET", "");
            Form.SetValueByPath("QUANTITY_ON_PALLET", "");
            Form.SetValueByPath("MOVING_HISTORY", "");

            Form.SetValueByPath("ORDER_NUMBER", null);
            Form.SetValueByPath("ORDER_ID", null);
            Form.SetValueByPath("INCOMING_ID", null);
            Form.SetValueByPath("PRODUCTION_TASK_ID", null);
            Form.SetValueByPath("PALLET_NUM", null);
            Form.SetValueByPath("PRODUCT_IDK1", null);
        }

        /// <summary>
        /// Поиск данных по поддону
        /// </summary>
        public async void Search()
        {
            DisableControls();
            SplashControl.Visible = true;

            ClearDataFields();

            if (CheckFieldsForSearch())
            {
                Dictionary<string, string> p = Form.GetValues();
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Stock");
                q.Request.SetParam("Object", "Pallet");
                q.Request.SetParam("Action", "SearchPallet");
                q.Request.SetParams(p);
                q.Request.Timeout = 90000;
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
                        PalletGridDataSet = ListDataSet.Create(result, "ITEMS");
                        PalletGrid.UpdateItems(PalletGridDataSet);
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
        /// Получаем данные по выбранному поддону
        /// </summary>
        public async void GetPalletData()
        {
            if (PalletGrid != null && PalletGrid.SelectedItem != null)
            {
                ClearDataFields();

                Dictionary<string, string> p = new Dictionary<string, string>();
                p.CheckAdd("BARCODE", PalletGrid.SelectedItem.CheckGet("PALLET_ID"));
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Stock");
                q.Request.SetParam("Object", "Pallet");
                q.Request.SetParam("Action", "GetHistory");
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
                        var historyDataSet = ListDataSet.Create(result, "HISTORY");
                        if (historyDataSet != null && historyDataSet.Items != null && historyDataSet.Items.Count > 0)
                        {
                            string movingHistoryString = "";
                            foreach (var item in historyDataSet.Items)
                            {
                                movingHistoryString = $"{movingHistoryString}{item.CheckGet("PER_MOV")}{Environment.NewLine}";
                            }

                            MovingHistoryTextBox.Text = movingHistoryString;
                        }

                        var itemDataSet = ListDataSet.Create(result, "ITEMS");
                        Form.SetValues(itemDataSet);
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
        }

        /// <summary>
        /// Установка значений по умолчанию.
        /// Очищает все поля.
        /// </summary>
        public void SetDefaults()
        {
            PalletGridDataSet = new ListDataSet();
            Form.SetDefaults();

            // Серый (дефолтный)
            var color = "#ffcccccc";
            var bc = new BrushConverter();
            var brush = (Brush)bc.ConvertFrom(color);
            SearchShipmentDateTime.BorderBrush = brush;

            var factorySelectBoxItems = new Dictionary<string, string>();
            factorySelectBoxItems.Add("1", "Л-ПАК ЛИПЕЦК");
            factorySelectBoxItems.Add("2", "Л-ПАК КАШИРА");
            FactorySelectBox.SetItems(factorySelectBoxItems);
            FactorySelectBox.SetSelectedItemByKey("1");

            if (PalletGrid != null)
            {
                PalletGrid.ClearItems();
            }
        }

        public void ClearSearchShipmentDateTime()
        {
            SearchShipmentDateTime.Text = "";
        }

        /// <summary>
        /// Деактивация контроллов
        /// </summary>
        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
            MainGrid.IsEnabled = false;
        }

        /// <summary>
        /// Активация контроллов
        /// </summary>
        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
            MainGrid.IsEnabled = true;
        }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Stock",
                ReceiverName = "",
                SenderName = "Pallet_Searcher",
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
            Central.ShowHelp("/doc/l-pack-erp/warehouse/pallet_searcher");
        }

        /// <summary>
        /// Установка настроек для принтера
        /// </summary>
        public void SetPrintSettings()
        {
            LabelReport2.SetPrintingProfile();
        }

        /// <summary>
        /// Вывод на печать ярылыка по выбранному поддону
        /// </summary>
        public void PrintLabel()
        {
            if (PalletGrid != null && PalletGrid.SelectedItem != null && PalletGrid.SelectedItem.CheckGet("PALLET_ID").ToInt() > 0)
            {
                LabelReport2 report = new LabelReport2(true);
                report.PrintLabel(PalletGrid.SelectedItem.CheckGet("PALLET_ID"));
            }
        }

        public void ProcessCommand(string command, ItemMessage m = null)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "label_print_pdf":
                        {
                            PrintLabel();
                        }
                        break;

                    case "label_view_pdf":
                        {
                            if (PalletGrid != null && PalletGrid.SelectedItem != null && PalletGrid.SelectedItem.CheckGet("PALLET_ID").ToInt() > 0)
                            {
                                LabelReport2 report = new LabelReport2(true);
                                report.ShowLabelPdf(PalletGrid.SelectedItem.CheckGet("PALLET_ID"), 0);
                            }
                        }
                        break;

                    case "label_view_html":
                        {
                            if (PalletGrid != null && PalletGrid.SelectedItem != null && PalletGrid.SelectedItem.CheckGet("PALLET_ID").ToInt() > 0)
                            {
                                LabelReport2 report = new LabelReport2(true);
                                report.ShowLabelHtml(PalletGrid.SelectedItem.CheckGet("PALLET_ID"), 0);
                            }
                        }
                        break;

                    case "template_view_pdf":
                        {
                            if (PalletGrid != null && PalletGrid.SelectedItem != null && PalletGrid.SelectedItem.CheckGet("PALLET_ID").ToInt() > 0)
                            {
                                LabelReport2 report = new LabelReport2(true);
                                report.ShowLabelPdf(PalletGrid.SelectedItem.CheckGet("PALLET_ID"), 1);
                            }
                        }
                        break;

                    case "template_view_html":
                        {
                            if (PalletGrid != null && PalletGrid.SelectedItem != null && PalletGrid.SelectedItem.CheckGet("PALLET_ID").ToInt() > 0)
                            {
                                LabelReport2 report = new LabelReport2(true);
                                report.ShowLabelHtml(PalletGrid.SelectedItem.CheckGet("PALLET_ID"), 1);
                            }
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel("[erp]pallet_search");
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

            if (PalletGrid != null && PalletGrid.Menu != null && PalletGrid.Menu.Count > 0)
            {
                foreach (var manuItem in PalletGrid.Menu)
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

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            SetDefaults();
        }

        private void ButtonOnClick(object sender, RoutedEventArgs e)
        {
            var b = (Button)sender;
            if(b != null)
            {
                var t = b.Tag.ToString();
                ProcessCommand(t);
            }
        }

        private void ClearSearchShipmentDateTimeButton_Click(object sender, RoutedEventArgs e)
        {
            ClearSearchShipmentDateTime();
        }
    }
}
