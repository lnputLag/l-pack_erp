using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Service.Printing;
using Client.Interfaces.Stock;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Форма ручной печати ярлыков по выбранному производственному заданию.
    /// Отображает информацию по уже созданным поддонам по выбранному ПЗ.
    /// </summary>
    /// <author>sviridov_ae</author>
    public partial class StackerManuallyPrint : UserControl
    {
        /// <summary>
        /// Обязательные к заполнению переменные:
        /// MachineId;
        /// ProductionTaskIdTextBox.Text;
        /// ProductionTaskNumberTextBox.Text.
        /// Не обязательные к заполнению переменные:
        /// ParentFrame.
        /// ParentContainer
        /// </summary>
        public StackerManuallyPrint()
        {
            FrameName = "StackerManuallyPrint";
            RoleName = "[erp]corrugator_stacker";
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Init();
            SetDefaults();
            PalletGridInit();
        }

        /// <summary>
        /// Наименование таба, который вызвал этот таб
        /// </summary>
        public string ParentFrame { get; set; }

        public string ParentContainer { get; set; }

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }
        
        /// <summary>
        /// Список продукции по выбранному производственному заданию
        /// </summary>
        public List<Dictionary<string, string>> ProductList { get; set; }

        /// <summary>
        /// Датасет с данными по выбранному производственному заданию
        /// </summary>
        public ListDataSet ProductionTaskDataDataSet { get; set; }

        /// <summary>
        /// Выбранная в выпадающем списке продукция
        /// </summary>
        public KeyValuePair<string, string> SelectedProduct { get; set; }

        /// <summary>
        /// Датасет с данными для грида
        /// </summary>
        public ListDataSet GridDataSet { get; set; }

        /// <summary>
        /// Идентификатор станка
        /// </summary>
        public int MachineId { get; set; }

        /// <summary>
        /// Выбранная запись в гриде существующих поддонов по выбранному производственному заданию
        /// </summary>
        public Dictionary<string, string> PalletGridSelectedItem { get; set; }

        /// <summary>
        /// Принтер, на котором будут печататься ярлыки:
        /// 0 -- Принтер первого стекера;
        /// 1 -- Принтер второго стекера;
        /// 2 -- Принтер по умолчанию (Принтер выбирается в зависимости от стекера, на котором ехало задание).
        /// </summary>
        public int PrinterType { get; set; }

        public string RoleName { get; set; }

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
                    Path="PRODUCTION_TASK_NUMBER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductionTaskNumberTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCTION_TASK_ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductionTaskIdTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACKER_NUMBER",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=StackerNumberTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },



                new FormHelperField()
                {
                    Path="CUSTOMER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CustomerTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
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
                    Path="PRODUCTION_SHIFT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductionShiftTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CODE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductCodeTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ORDER_ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=OrderIdTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCTION_DATE_TIME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=DateTimeTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },



                new FormHelperField()
                {
                    Path="COUNT_PALLET_BY_TASK",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CountPalletByTaskTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="COUNT_CREATED_PALLET",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CountCreatedPalletTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                }, 
                new FormHelperField()
                {
                    Path="COUNT_SCANNED_PALLET",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CountScannedPalletTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="COUNT_CARDBOARD_BY_TASK",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CountCardboardByTaskTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="COUNT_CREATED_CARDBOARD",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CountCreatedCardboardTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="COUNT_SCANNED_CARDBOARD",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CountScannedCardboardTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },


                new FormHelperField()
                {
                    Path="COUNT_ON_PALLET_BY_TASK",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CountOnPalletByTaskTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="COUNT_ON_PALLET",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CountOnPalletTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.DigitOnly, null },
                            { FormHelperField.FieldFilterRef.IsNotZero, null }
                    },
                },

                new FormHelperField()
                {
                    Path="BLOCKED_LAST_LABEL_PRINT_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=BlockedLastLabelPrintFlagCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                new FormHelperField()
                {
                    Path="PALLET_NUMBER",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
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
            };

            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
        }

        /// <summary>
        /// Инициализация грида
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
                        Header = "*",
                        Path = "SELECTED_FLAG",
                        ColumnType = DataGridHelperColumn.ColumnTypeRef.Boolean,
                        MinWidth = 45,
                        MaxWidth = 45,
                        Editable = true,
                        OnClickAction = (row, el) =>
                        {
                            UpdateAction();
                            return null;
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=60,
                        MaxWidth=60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Поддон",
                        Path="NUM",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=60,
                        MaxWidth=60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер",
                        Path="PALLET_NUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество на поддоне",
                        Path="QUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=82,
                        MaxWidth=82,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Отсканирован",
                        Path="SCANED_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=55,
                        MaxWidth=55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="В К0",
                        Path="FAULT_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=55,
                        MaxWidth=55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Заблокирован",
                        Path="BLOCKED_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=55,
                        MaxWidth=55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Удалён",
                        Path="DELETED_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=55,
                        MaxWidth=55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Создан",
                        Path="PALLET_CREATE_DTTM",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=110,
                        MaxWidth=110,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Создатель",
                        Path="PALLET_CREATE_USER",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=180,
                        MaxWidth=180,
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
                        MinWidth=280,
                        MaxWidth=5000,
                    },
                };
                PalletGrid.SetColumns(columns);
                PalletGrid.SetSorting("PALLET_NUMBER", ListSortDirection.Descending);

                // раскраска строк
                PalletGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    // определение цветов фона строк
                    {
                        StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";
                            
                            // Не отсканирован
                            if(row.CheckGet("SCANED_FLAG").ToInt() == 0)
                            {
                                color = HColor.Blue;
                            }

                            if (row.CheckGet("BLOCKED_FLAG").ToInt() > 0)
                            {
                                color = HColor.Yellow;
                            }

                            if (row.CheckGet("DELETED_FLAG").ToInt() > 0)
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

                PalletGrid.Init();

                // контекстное меню
                PalletGrid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    {
                        "ReprintLabel",
                        new DataGridContextMenuItem()
                        {
                            Header="Повторная печать ярлыка",
                            Action=()=>
                            {
                                ReprintLabel();
                            }
                        }
                    },
                    { "s0", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    {
                        "EditPallet",
                        new DataGridContextMenuItem()
                        {
                            Header="Редактировать поддон",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                EditPallet(PalletGridSelectedItem);
                            }
                        }
                    },
                    {
                        "DeletePallet",
                        new DataGridContextMenuItem()
                        {
                            Header="Удалить поддон",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                DeleteOnePallet(PalletGridSelectedItem);
                            }
                        }
                    },
                    { "s1", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    {
                        "MoveToComplectation",
                        new DataGridContextMenuItem()
                        {
                            Header="В К0",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                MoveToComplectation();
                            }
                        }
                    },
                };

                //при выборе строки в гриде, обновляются актуальные действия для записи
                PalletGrid.OnSelectItem = selectedItem =>
                {
                    PalletGridSelectedItem = selectedItem;
                    UpdateAction();
                };

                PalletGrid.OnFilterItems = () =>
                {
                    if (PalletGrid.GridItems != null)
                    {
                        if (PalletGrid.GridItems.Count > 0)
                        {
                            if (ProductSelectBox.SelectedItem.Key != null)
                            {
                                int productId = ProductSelectBox.SelectedItem.Key.ToInt();
                                var items = new List<Dictionary<string, string>>();

                                if (productId > 0)
                                {
                                    items.AddRange(PalletGrid.GridItems.Where(row => row.CheckGet("PRODUCT_ID").ToInt() == productId));
                                }
                                else
                                {
                                    items = PalletGrid.GridItems;
                                }

                                PalletGrid.GridItems = items;
                            }
                        }
                    }
                };

                PalletGrid.Run();
            }
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
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            SelectedProduct = new KeyValuePair<string, string>();
            ProductList = new List<Dictionary<string, string>>();
            ProductionTaskDataDataSet = new ListDataSet();
            GridDataSet = new ListDataSet();
            PalletGridSelectedItem = new Dictionary<string, string>();

            var printerTypeParameter = Central.User.UserParameterList.FirstOrDefault(x => x.Interface == this.GetType().Name && x.Name == "PrinterType");
            if (printerTypeParameter != null)
            {
                PrinterType = printerTypeParameter.Value.ToInt();
            }
            PrinterRadioButtonCheck(PrinterType);

            //Form.SetDefaults();
        }

        /// <summary>
        /// Получаем данные для выпадающего списка продукций по выбранному ПЗ
        /// </summary>
        public async void LoadProductList()
        {
            if (!string.IsNullOrEmpty(ProductionTaskIdTextBox.Text))
            {
                var p = new Dictionary<string, string>();
                p.Add("ID_PZ", ProductionTaskIdTextBox.Text);

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "ManuallyPrint");
                q.Request.SetParam("Action", "ListProduct");
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
                        ProductList = dataSet.Items;

                        ProductSelectBox.SetItems(dataSet, "ID", "NAME_CODE");
                        ProductSelectBox.SetSelectedItemFirst();

                        if(ProductSelectBox.Items != null && ProductSelectBox.Items.Count > 1)
                        {
                            // Оранжевый, если более одной продукции по заданию
                            var color = "#FFCA51";
                            var bc = new BrushConverter();
                            var brush = (Brush)bc.ConvertFrom(color);
                            ProductSelectBox.BorderBrush = brush;
                        }
                        else
                        {
                            // Чёрный, если одна продукция по заданию
                            var color = "#ffcccccc";
                            var bc = new BrushConverter();
                            var brush = (Brush)bc.ConvertFrom(color);
                            ProductSelectBox.BorderBrush = brush;
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// Получаем данные по производственному заданию по его ИД и Продукции
        /// </summary>
        public void LoadDataByProductionTaskAndProduct()
        {
            ClearFormValues();

            if (!string.IsNullOrEmpty(ProductionTaskIdTextBox.Text) && !string.IsNullOrEmpty(SelectedProduct.Key))
            {
                var p = new Dictionary<string, string>();
                p.Add("ID_PZ", ProductionTaskIdTextBox.Text);
                p.Add("ID2", SelectedProduct.Key);

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "ManuallyPrint");
                q.Request.SetParam("Action", "GetProductionTaskData");
                q.Request.SetParams(p);

                q.Request.Timeout = 15000;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        // Заполняем данные формы
                        var dataSet = ListDataSet.Create(result, "ITEMS");
                        ProductionTaskDataDataSet = dataSet;
                        Form.SetValues(ProductionTaskDataDataSet);

                        // Заполняем данные грида созданных поддонов
                        var gridDataSet = ListDataSet.Create(result, "GRID_ITEMS");
                        GridDataSet = gridDataSet;
                        PalletGrid.UpdateItems(GridDataSet);

                        // Расчитываем номер следующего поддона
                        int maxPalletNumber = 0;
                        if (GridDataSet != null && GridDataSet.Items != null && GridDataSet.Items.Count > 0)
                        {
                            maxPalletNumber = GridDataSet.Items.Max(x => x.CheckGet("PALLET_NUMBER").ToInt());
                        }
                        Form.SetValueByPath("PALLET_NUMBER", $"{maxPalletNumber + 1}");

                        // Если количество поддонов по заданию не заполнено, то расчитываем это значение
                        if (!(CountPalletByTaskTextBox.Text.ToInt() > 0))
                        {
                            int countPalletByTask = Math.Ceiling(CountCardboardByTaskTextBox.Text.ToDouble() / CountOnPalletByTaskTextBox.Text.ToDouble()).ToInt();
                            CountPalletByTaskTextBox.Text = countPalletByTask.ToString();
                        }

                        if (CountCreatedCardboardTextBox.Text.ToInt() > CountCardboardByTaskTextBox.Text.ToInt())
                        {
                            var color = "#ffee0000";
                            var bc = new BrushConverter();
                            var brush = (Brush)bc.ConvertFrom(color);
                            CountCreatedCardboardTextBox.BorderBrush = brush;
                            CountCreatedCardboardTextBox.ToolTip = "Картона больше, чем нужно по заявке";

                            Form.Valid = false;
                        }
                        else
                        {
                            var color = "#ffcccccc";
                            var bc = new BrushConverter();
                            var brush = (Brush)bc.ConvertFrom(color);
                            CountCreatedCardboardTextBox.BorderBrush = brush;
                            CountCreatedCardboardTextBox.ToolTip = "";
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// Печать нескольких новых ярлыков
        /// </summary>
        public void PrintFew()
        {
            if (MachineId > 0)
            {
                if (Form.Validate())
                {
                    DisableControls();

                    var window = new StackerManuallyPrintLabelCreator(2);
                    window.Show();

                    int quantityPallet = 0;
                    if (window.SuccessFlag)
                    {
                        quantityPallet = window.ResultValue;
                    }

                    if (quantityPallet > 0)
                    {
                        bool resume = true;

                        string message = "";
                        // Если собираются распечатать последний ярлык
                        if (Form.GetValueByPath("BLOCKED_LAST_LABEL_PRINT_FLAG").ToInt() > 0
                            && Form.GetValueByPath("COUNT_PALLET_BY_TASK").ToInt() - (Form.GetValueByPath("COUNT_CREATED_PALLET").ToInt() + quantityPallet) <= 0)
                        {
                            if (Form.GetValueByPath("COUNT_ON_PALLET_BY_TASK").ToInt() == Form.GetValueByPath("COUNT_ON_PALLET").ToInt())
                            {
                                message = $"По выбранному заданию оператором заблокирована печать последнего ярлыка." +
                                $"{Environment.NewLine}Для печати последнего ярлыка уменьшите количество на поддоне или увеличьте количество отбракованной вручную продукции.";

                                DialogWindow.ShowDialog(message, "Предупреждение", "", DialogWindowButtons.OK);
                                resume = false;
                            }
                            else
                            {
                                message = $"Внимание! По выбранному заданию оператором была заблокирована печать последнего ярлыка.{Environment.NewLine}" +
                                $"Распечатать новые ярлыки для задания: {Form.GetValueByPath("PRODUCTION_TASK_NUMBER")}?{Environment.NewLine}" +
                                $"Продукция: {Form.GetValueByPath("PRODUCT_NAME")}.{Environment.NewLine}" +
                                $"Количество новых ярлыков: {quantityPallet}.{Environment.NewLine}" +
                                $"Количество на одном поддоне: {Form.GetValueByPath("COUNT_ON_PALLET")}.";

                                if (DialogWindow.ShowDialog(message, "Предупреждение", "", DialogWindowButtons.YesNo) != true)
                                {
                                    resume = false;
                                }
                            }
                        }
                        else
                        {
                            message = $"Распечатать новые ярлыки для задания: {Form.GetValueByPath("PRODUCTION_TASK_NUMBER")}?{Environment.NewLine}" +
                            $"Продукция: {Form.GetValueByPath("PRODUCT_NAME")}.{Environment.NewLine}" +
                            $"Количество новых ярлыков: {quantityPallet}.{Environment.NewLine}" +
                            $"Количество на одном поддоне: {Form.GetValueByPath("COUNT_ON_PALLET")}.";

                            if (DialogWindow.ShowDialog(message, "Предупреждение", "", DialogWindowButtons.YesNo) != true)
                            {
                                resume = false;
                            }
                        }

                        if (resume)
                        {
                            bool localResume = true;

                            for (int i = 0; i < quantityPallet; i++)
                            {
                                if (localResume)
                                {
                                    localResume = false;

                                    var p = new Dictionary<string, string>();
                                    p.Add("ID_PZ", Form.GetValueByPath("PRODUCTION_TASK_ID"));
                                    p.Add("KOL", Form.GetValueByPath("COUNT_ON_PALLET"));
                                    p.Add("ID2", SelectedProduct.Key);
                                    p.Add("ID_ST", MachineId.ToString());

                                    var q = new LPackClientQuery();
                                    q.Request.SetParam("Module", "Production");
                                    q.Request.SetParam("Object", "CorrugatingLabel");
                                    q.Request.SetParam("Action", "CreatePallet");
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
                                            if (dataSet != null && dataSet.Items.Count > 0)
                                            {
                                                var firstDictionary = dataSet.Items.First();
                                                string productionIdk1 = Form.GetValueByPath("PRODUCT_IDK1");
                                                int tovarSubFlag = firstDictionary.CheckGet("TOVAR_SUB_FLAG").ToInt();
                                                // Если продукция на поддоне - Перестил, то должны печатать ярлык заготовки
                                                if (tovarSubFlag > 0)
                                                {
                                                    productionIdk1 = "4";
                                                }

                                                LabelReport2 report = new LabelReport2(true);
                                                if (PrinterType == 1 || (PrinterType == 2 && Form.GetValueByPath("STACKER_NUMBER").ToInt() == 2))
                                                {
                                                    report.PrintingProfileLabel = $"{report.PrintingProfileLabel}2";
                                                }
                                                report.PrintLabel(firstDictionary.CheckGet("ID_PZ"), firstDictionary.CheckGet("NUM"), productionIdk1);

                                                localResume = true;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (q.Answer.Error.Code == 145)
                                        {
                                            var d = new DialogWindow($"{q.Answer.Error.Message}", "Ручная печать ярлыков", "", DialogWindowButtons.OK);
                                            d.ShowDialog();
                                        }
                                        else
                                        {
                                            q.ProcessError();
                                        }
                                    }
                                }
                            }

                            LoadDataByProductionTaskAndProduct();
                        }
                    }

                    EnableControls();
                }
            }
            else
            {
                var msg = $"Не указан станок.";
                var d = new DialogWindow($"{msg}", "Ручная печать ярлыков", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Печать одного нового ярлыка
        /// </summary>
        public void PrintOne()
        {
            if (MachineId > 0)
            {
                if (Form.Validate())
                {
                    // Если по заданию осталось создать один поддон и указанное количество на создаваемом поддоне равно количеству на поддоне по умолчанию
                    if (Form.GetValueByPath("BLOCKED_LAST_LABEL_PRINT_FLAG").ToInt() > 0
                        && Form.GetValueByPath("COUNT_PALLET_BY_TASK").ToInt() - Form.GetValueByPath("COUNT_CREATED_PALLET").ToInt() == 1
                        && Form.GetValueByPath("COUNT_ON_PALLET_BY_TASK").ToInt() == Form.GetValueByPath("COUNT_ON_PALLET").ToInt())
                    {
                        string message = $"По выбранному заданию оператором заблокирована печать последнего ярлыка." +
                            $"{Environment.NewLine}Для печати последнего ярлыка уменьшите количество на поддоне или увеличьте количество отбракованной вручную продукции.";
                        DialogWindow.ShowDialog(message, "Предупреждение", "", DialogWindowButtons.OK);
                    }
                    else
                    {
                        DisableControls();

                        var window = new StackerManuallyPrintLabelCreator(1);
                        if (!string.IsNullOrEmpty(Form.GetValueByPath("PALLET_NUMBER")))
                        {
                            window.PalletNumberTextBox.Text = Form.GetValueByPath("PALLET_NUMBER");
                        }

                        window.Show();

                        int palletNumber = 0;
                        if (window.SuccessFlag)
                        {
                            palletNumber = window.ResultValue;
                        }

                        if (palletNumber > 0)
                        {
                            bool resume = true;

                            string message = "";
                            if (Form.GetValueByPath("BLOCKED_LAST_LABEL_PRINT_FLAG").ToInt() > 0
                                && Form.GetValueByPath("COUNT_PALLET_BY_TASK").ToInt() - Form.GetValueByPath("COUNT_CREATED_PALLET").ToInt() == 1)
                            {
                                message = $"Внимание! По выбранному заданию оператором была заблокирована печать последнего ярлыка.{Environment.NewLine}" +
                                $"Распечатать новый ярлык для задания: {Form.GetValueByPath("PRODUCTION_TASK_NUMBER")}?{Environment.NewLine}" +
                                $"Продукция: {Form.GetValueByPath("PRODUCT_NAME")}.{Environment.NewLine}" +
                                $"Порядковый номер поддона: {palletNumber}.{Environment.NewLine}" +
                                $"Количество на поддоне: {Form.GetValueByPath("COUNT_ON_PALLET")}.";
                            }
                            else
                            {
                                message = $"Распечатать новый ярлык для задания: {Form.GetValueByPath("PRODUCTION_TASK_NUMBER")}?{Environment.NewLine}" +
                                $"Продукция: {Form.GetValueByPath("PRODUCT_NAME")}.{Environment.NewLine}" +
                                $"Порядковый номер поддона: {palletNumber}.{Environment.NewLine}" +
                                $"Количество на поддоне: {Form.GetValueByPath("COUNT_ON_PALLET")}.";
                            }

                            if (DialogWindow.ShowDialog(message, "Предупреждение", "", DialogWindowButtons.YesNo) != true)
                            {
                                resume = false;
                            }

                            if (resume)
                            {
                                var p = new Dictionary<string, string>();
                                p.Add("ID_PZ", Form.GetValueByPath("PRODUCTION_TASK_ID"));
                                p.Add("KOL", Form.GetValueByPath("COUNT_ON_PALLET"));
                                p.Add("ID2", SelectedProduct.Key);
                                p.Add("ID_ST", MachineId.ToString());
                                p.Add("NUM_PALLET", palletNumber.ToString());

                                var q = new LPackClientQuery();
                                q.Request.SetParam("Module", "Production");
                                q.Request.SetParam("Object", "CorrugatingLabel");
                                q.Request.SetParam("Action", "CreatePallet");
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
                                        if (dataSet != null && dataSet.Items.Count > 0)
                                        {
                                            var firstDictionary = dataSet.Items.First();
                                            string productionIdk1 = Form.GetValueByPath("PRODUCT_IDK1");
                                            int tovarSubFlag = firstDictionary.CheckGet("TOVAR_SUB_FLAG").ToInt();
                                            // Если продукция на поддоне - Перестил, то должны печатать ярлык заготовки
                                            if (tovarSubFlag > 0)
                                            {
                                                productionIdk1 = "4";
                                            }

                                            LabelReport2 report = new LabelReport2(true);
                                            if (PrinterType == 1 || (PrinterType == 2 && Form.GetValueByPath("STACKER_NUMBER").ToInt() == 2))
                                            {
                                                report.PrintingProfileLabel = $"{report.PrintingProfileLabel}2";
                                            }
                                            report.PrintLabel(firstDictionary.CheckGet("ID_PZ"), firstDictionary.CheckGet("NUM"), productionIdk1);

                                            LoadDataByProductionTaskAndProduct();
                                        }
                                    }
                                }
                                else
                                {
                                    if (q.Answer.Error.Code == 145)
                                    {
                                        var d = new DialogWindow($"{q.Answer.Error.Message}", "Ручная печать ярлыков", "", DialogWindowButtons.OK);
                                        d.ShowDialog();
                                    }
                                    else
                                    {
                                        q.ProcessError();
                                    }
                                }
                            }
                        }

                        EnableControls();
                    }
                }
            }
            else
            {
                var msg = $"Не указан станок.";
                var d = new DialogWindow($"{msg}", "Ручная печать ярлыков", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Повторная печать ярлыка
        /// </summary>
        public void ReprintLabel()
        {
            if (Form != null)
            {
                if (PalletGridSelectedItem != null && PalletGridSelectedItem.Count > 0)
                {
                    string productIdk1 = "";
                    // Если продукция на поддоне - Перестил, то должны печатать ярлык заготовки
                    if (PalletGridSelectedItem.CheckGet("SUBPRODUCT_FLAG").ToInt() > 0)
                    {
                        productIdk1 = "4";
                    }
                    else
                    {
                        productIdk1 = Form.GetValueByPath("PRODUCT_IDK1");
                    }

                    LabelReport2 report = new LabelReport2(true);
                    if (PrinterType == 1 || (PrinterType == 2 && Form.GetValueByPath("STACKER_NUMBER").ToInt() == 2))
                    {
                        report.PrintingProfileLabel = PrintingSettings.LabelPrinter2.ProfileName;
                    }
                    report.PrintLabel(Form.GetValueByPath("PRODUCTION_TASK_ID"), PalletGridSelectedItem.CheckGet("PALLET_NUMBER"), productIdk1);
                }
            }
        }

        public void ReprintFewLabel()
        {
            if (Form != null)
            {
                if (PalletGrid != null && PalletGrid.Items != null && PalletGrid.Items.Count > 0)
                {
                    // Получаем список отмеченных поддонов 
                    List<Dictionary<string, string>> selectedPallets = PalletGrid.Items.Where(x => x.CheckGet("SELECTED_FLAG").ToInt() == 1).ToList();
                    if (selectedPallets != null && selectedPallets.Count > 0)
                    {
                        foreach (var selectedPallet in selectedPallets)
                        {
                            string productIdk1 = "";
                            // Если продукция на поддоне - Перестил, то должны печатать ярлык заготовки
                            if (PalletGridSelectedItem.CheckGet("SUBPRODUCT_FLAG").ToInt() > 0)
                            {
                                productIdk1 = "4";
                            }
                            else
                            {
                                productIdk1 = Form.GetValueByPath("PRODUCT_IDK1");
                            }

                            LabelReport2 report = new LabelReport2(true);
                            if (PrinterType == 1 || (PrinterType == 2 && Form.GetValueByPath("STACKER_NUMBER").ToInt() == 2))
                            {
                                report.PrintingProfileLabel = $"{report.PrintingProfileLabel}2";
                            }
                            report.PrintLabel(Form.GetValueByPath("PRODUCTION_TASK_ID"), selectedPallet.CheckGet("PALLET_NUMBER"), productIdk1);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Обновляет доступные действия
        /// </summary>
        public void UpdateAction()
        {
            DeletePalletButton.IsEnabled = false;
            ReprintPalletButton.IsEnabled = false;

            if (PalletGrid != null && PalletGrid.Items != null && PalletGrid.Items.Count > 0)
            {
                if (PalletGrid.Items.Count(x => x.CheckGet("SELECTED_FLAG").ToInt() == 1) > 0)
                {
                    ReprintPalletButton.IsEnabled = true;
                }

                if (PalletGrid.Items.Count(x => x.CheckGet("SELECTED_FLAG").ToInt() == 1 && x.CheckGet("SCANED_FLAG").ToInt() == 0) > 0)
                {
                    DeletePalletButton.IsEnabled = true;
                }

                if (PalletGridSelectedItem != null && PalletGridSelectedItem.Count > 0)
                {
                    if (PalletGridSelectedItem.CheckGet("SCANED_FLAG").ToInt() > 0)
                    {
                        PalletGrid.Menu["DeletePallet"].Enabled = false;
                    }
                    else
                    {
                        PalletGrid.Menu["DeletePallet"].Enabled = true;
                    }
                }
            }

            ProcessPermissions();
        }

        public void DeleteFewPallet()
        {
            if (PalletGrid != null && PalletGrid.Items != null && PalletGrid.Items.Count > 0)
            {
                // Получаем список отмеченных поддонов исключая из него те поддоны, которые были отсканированы
                List<Dictionary<string, string>> selectedPallets = PalletGrid.Items.Where(x => x.CheckGet("SELECTED_FLAG").ToInt() == 1 && x.CheckGet("SCANED_FLAG").ToInt() == 0).ToList();
                if (selectedPallets != null && selectedPallets.Count > 0)
                {
                    bool succesfullFlag = true;
                    foreach (var selectedPallet in selectedPallets)
                    {
                        if (succesfullFlag)
                        {
                            succesfullFlag = false;

                            var p = new Dictionary<string, string>();
                            p.Add("PALLET_ID", selectedPallet.CheckGet("ID"));

                            var q = new LPackClientQuery();
                            q.Request.SetParam("Module", "Production");
                            q.Request.SetParam("Object", "ManuallyPrint");
                            q.Request.SetParam("Action", "DeletePallete");
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
                                        if (dataSet.Items.First().CheckGet("PALLET_ID").ToInt() > 0)
                                        {
                                            succesfullFlag = true;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                q.ProcessError();
                            }
                        }
                    }

                    if (succesfullFlag)
                    {
                        LoadDataByProductionTaskAndProduct();
                    }
                }
            }
        }

        public void EditPallet(Dictionary<string, string> selectedPallet)
        {
            if (selectedPallet.CheckGet("SCANED_FLAG").ToInt() > 0)
            {
                string msg = $"Поддон уже отсканирован. Такой поддон нельзя редактировать.";
                var d = new DialogWindow($"{msg}", "Ручная печать ярлыков", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
            else
            {
                int quantityOnPallet = selectedPallet.CheckGet("QUANTITY").ToInt();
                var i = new ComplectationCMQuantity(quantityOnPallet);
                i.Show("Количество на поддоне");
                if (i.OkFlag)
                {
                    quantityOnPallet = i.QtyInt;
                    if (quantityOnPallet > 0)
                    {
                        var p = new Dictionary<string, string>();
                        p.Add("PALLET_ID", selectedPallet.CheckGet("ID"));
                        p.Add("QUANTITY_ON_PALLET", quantityOnPallet.ToString());

                        // Если поддон был заблокирован и мы уменьшили количество на поддоне, то снимаем блокировку с поддона
                        if (selectedPallet.CheckGet("BLOCKED_FLAG").ToInt() > 0 && selectedPallet.CheckGet("QUANTITY").ToInt() > quantityOnPallet)
                        {
                            p.Add("BLOCKED_FLAG", "0");
                        }

                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Production");
                        q.Request.SetParam("Object", "CorrugatingLabel");
                        q.Request.SetParam("Action", "EditPallet");
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
                                    if (dataSet.Items.First().CheckGet("PALLET_ID").ToInt() > 0)
                                    {
                                        string productIdk1 = "";
                                        // Если продукция на поддоне - Перестил, то должны печатать ярлык заготовки
                                        if (selectedPallet.CheckGet("SUBPRODUCT_FLAG").ToInt() > 0)
                                        {
                                            productIdk1 = "4";
                                        }
                                        else
                                        {
                                            productIdk1 = Form.GetValueByPath("PRODUCT_IDK1");
                                        }

                                        LabelReport2 report = new LabelReport2(true);
                                        if (PrinterType == 1 || (PrinterType == 2 && Form.GetValueByPath("STACKER_NUMBER").ToInt() == 2))
                                        {
                                            report.PrintingProfileLabel = $"{report.PrintingProfileLabel}2";
                                        }
                                        report.PrintLabel(Form.GetValueByPath("PRODUCTION_TASK_ID"), selectedPallet.CheckGet("PALLET_NUMBER"), productIdk1);

                                        LoadDataByProductionTaskAndProduct();
                                    }
                                }
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

        /// <summary>
        /// Удаление выбранного поддона
        /// </summary>
        public void DeleteOnePallet(Dictionary<string, string> selectedPallet)
        {
            if (selectedPallet.CheckGet("SCANED_FLAG").ToInt() > 0)
            {
                string msg = $"Ярлык уже отсканирован. Такой ярлык удалять нельзя.";
                var d = new DialogWindow($"{msg}", "Ручная печать ярлыков", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
            else
            {
                var message = $"Вы действительно хотите удалить поддон {selectedPallet.CheckGet("NUM")}?";
                if (DialogWindow.ShowDialog(message, "Ручная печать ярлыков", "", DialogWindowButtons.YesNo) == true)
                {
                    var p = new Dictionary<string, string>();
                    p.Add("PALLET_ID", selectedPallet.CheckGet("ID"));

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Production");
                    q.Request.SetParam("Object", "ManuallyPrint");
                    q.Request.SetParam("Action", "DeletePallete");
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
                                if (dataSet.Items.First().CheckGet("PALLET_ID").ToInt() > 0)
                                {
                                    LoadDataByProductionTaskAndProduct();
                                }
                            }
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
        /// Создание задания для выдителя погрузчика на перемещение выбранного поддона на комплектацию Га
        /// </summary>
        public void MoveToComplectation()
        {
            if (PalletGridSelectedItem != null && PalletGridSelectedItem.Count > 0)
            {
                // Если поддон уже отсканирован и это готовая продукция, то можем продолжать
                if (PalletGridSelectedItem.CheckGet("SCANED_FLAG").ToInt() > 0)
                {
                    var window = new StackerManuallyPrintFault();
                    window.PalletId = PalletGridSelectedItem.CheckGet("ID").ToInt();
                    window.PalletFullName = PalletGridSelectedItem.CheckGet("NUM");

                    if (Form.GetValueByPath("PRODUCT_IDK1").ToInt() == 5 || Form.GetValueByPath("PRODUCT_IDK1").ToInt() == 6)
                    {
                        window.FaultStage = 4;
                    }
                    else if (Form.GetValueByPath("PRODUCT_IDK1").ToInt() == 4)
                    {
                        window.FaultStage = 5;
                    }

                    window.Show();
                    LoadDataByProductionTaskAndProduct();
                }
                else
                {
                    string msg = $"Нельзя отбраковать не отсканированный поддон";
                    var d = new DialogWindow($"{msg}", "Ручная печать ярлыков", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                string msg = $"Не выбран поддон, который нужно отбракоать.";
                var d = new DialogWindow($"{msg}", "Ручная печать ярлыков", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Очищает значения текстбоксов с информацией по заданию и грида
        /// </summary>
        public void ClearFormValues()
        {
            Form.SetValueByPath("CUSTOMER", "");
            Form.SetValueByPath("PRODUCT_NAME", "");
            Form.SetValueByPath("PRODUCTION_SHIFT", "");
            Form.SetValueByPath("CODE", "");
            Form.SetValueByPath("ORDER_ID", "");
            Form.SetValueByPath("PRODUCTION_DATE_TIME", "");
            Form.SetValueByPath("COUNT_PALLET_BY_TASK", "");
            Form.SetValueByPath("COUNT_CREATED_PALLET", "");
            Form.SetValueByPath("COUNT_SCANNED_PALLET", "");
            Form.SetValueByPath("COUNT_CARDBOARD_BY_TASK", "");
            Form.SetValueByPath("COUNT_CREATED_CARDBOARD", "");
            Form.SetValueByPath("COUNT_SCANNED_CARDBOARD", "");
            Form.SetValueByPath("COUNT_ON_PALLET_BY_TASK", "");
            Form.SetValueByPath("PALLET_NUMBER", "");

            PalletGrid.ClearItems();
        }

        public void ClearAllValues()
        {
            Form.SetValueByPath("CUSTOMER", "");
            Form.SetValueByPath("PRODUCT_NAME", "");
            Form.SetValueByPath("PRODUCTION_SHIFT", "");
            Form.SetValueByPath("CODE", "");
            Form.SetValueByPath("ORDER_ID", "");
            Form.SetValueByPath("PRODUCTION_DATE_TIME", "");
            Form.SetValueByPath("COUNT_PALLET_BY_TASK", "");
            Form.SetValueByPath("COUNT_CREATED_PALLET", "");
            Form.SetValueByPath("COUNT_SCANNED_PALLET", "");
            Form.SetValueByPath("COUNT_CARDBOARD_BY_TASK", "");
            Form.SetValueByPath("COUNT_CREATED_CARDBOARD", "");
            Form.SetValueByPath("COUNT_SCANNED_CARDBOARD", "");
            Form.SetValueByPath("COUNT_ON_PALLET_BY_TASK", "");
            Form.SetValueByPath("PALLET_NUMBER", "");

            PalletGrid.ClearItems();

            Form.SetValueByPath("PRODUCTION_TASK_NUMBER", "");
            Form.SetValueByPath("PRODUCTION_TASK_ID", "");

            var emptyDic = new Dictionary<string, string>();
            emptyDic.Add("", "");
            ProductSelectBox.SetItems(emptyDic);

            SetDefaults();
        }

        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Show()
        {
            this.ProcessPermissions();

            FrameName = $"{FrameName}_{Form.GetValueByPath("PRODUCTION_TASK_ID").ToInt()}";

            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 1;
            if (Central.WM.TabItems.ContainsKey(ParentContainer))
            {
                Central.WM.Show(FrameName, "Ручная печать", true, ParentContainer, this);
            }
            else
            {
                Central.WM.Show(FrameName, "Ручная печать ярлыков", true, "main", this);
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
                SenderName = "StackerManuallyPrint",
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
            Central.ShowHelp("/doc/l-pack-erp-new/gofroproduction/label_printing/task_list/manual_printing");
            //Central.ShowHelp("/doc/l-pack-erp/production/stacker_cm/stacker_manually_print/stacker_manually_print");
        }

        /// <summary>
        /// Установка настроек для принтера
        /// </summary>
        public void SetPrintSettings()
        {
            LabelReport2.SetPrintingProfile();
        }

        public void PrinterRadioButtonCheck(int printerType)
        {
            PrinterType = printerType;

            switch (printerType)
            {
                case 0:
                    FirstPrinterRadioButton.IsChecked = true;
                    SecondPrinterRadioButton.IsChecked = false;
                    DefaultPrinterRadioButton.IsChecked = false;
                    break;

                case 1:
                    FirstPrinterRadioButton.IsChecked = false;
                    SecondPrinterRadioButton.IsChecked = true;
                    DefaultPrinterRadioButton.IsChecked = false;
                    break;

                case 2:
                    FirstPrinterRadioButton.IsChecked = false;
                    SecondPrinterRadioButton.IsChecked = false;
                    DefaultPrinterRadioButton.IsChecked = true;
                    break;

                default:
                    break;
            }

            var printerTypeParameter = Central.User.UserParameterList.FirstOrDefault(x => x.Interface == this.GetType().Name && x.Name == "PrinterType");
            if (printerTypeParameter != null)
            {
                printerTypeParameter.Value = $"{PrinterType}";
            }
            else
            {
                printerTypeParameter = new UserParameter(this.GetType().Name, "PrinterType", $"{PrinterType}", "Принтер для ручной печати ярлыков на выходе ГА. " +
                    "0 -- Принтер первого стекера; 1 -- Принтер второго стекера; 2 -- Принтер по умолчанию (Принтер выбирается в зависимости от стекера, на котором ехало задание).");
                Central.User.UserParameterList.Add(printerTypeParameter);
            }
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.IndexOf("StackerManuallyPrint") > -1)
            {
                if (m.ReceiverName.IndexOf("StackerManuallyPrint") > -1)
                {
                    switch (m.Action)
                    {
                        case "SelectItem":
                            if (m.ContextObject != null)
                            {
                                var selectedProductionTask = (Dictionary<string, string>)m.ContextObject;
                                ProductionTaskNumberTextBox.Text = selectedProductionTask.CheckGet("NUM");
                                ProductionTaskIdTextBox.Text = selectedProductionTask.CheckGet("ID");
                            }
                            break;
                    }
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
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

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadDataByProductionTaskAndProduct();
        }

        private void ProductSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SelectedProduct = ProductSelectBox.SelectedItem;
            LoadDataByProductionTaskAndProduct();
        }

        private void ProductionTaskIdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadProductList();
        }

        private void PrintFewButton_Click(object sender, RoutedEventArgs e)
        {
            PrintFew();
        }

        private void PrintOneButton_Click(object sender, RoutedEventArgs e)
        {
            PrintOne();
        }

        private void DeletePalletButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteFewPallet();
        }

        private void ReprintPalletButton_Click(object sender, RoutedEventArgs e)
        {
            ReprintFewLabel();
        }

        private void BlockedLastLabelPrintFlagCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (BlockedLastLabelPrintFlagLabel != null)
            {
                BlockedLastLabelPrintFlagLabel.Foreground = HColor.RedFG.ToBrush();
            }
        }

        private void BlockedLastLabelPrintFlagCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (BlockedLastLabelPrintFlagLabel != null)
            {
                BlockedLastLabelPrintFlagLabel.Foreground = "#000000".ToBrush();
            }
        }

        private void SecondPrinterRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            PrinterRadioButtonCheck(1);
        }

        private void FirstPrinterRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            PrinterRadioButtonCheck(0);
        }

        private void DefaultPrinterRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            PrinterRadioButtonCheck(2);
        }
    }
}
