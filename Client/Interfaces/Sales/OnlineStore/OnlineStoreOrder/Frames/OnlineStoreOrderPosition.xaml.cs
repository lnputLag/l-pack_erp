using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
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
    /// Список продукции для работы с позициями заказа интернет-магазина
    /// </summary>
    /// <author>sviridov_ae</author>
    public partial class OnlineStoreOrderPosition : ControlBase
    {
        /// <summary>
        /// Конструктор класса таба
        /// Список продукции для работы с позициями заказа интернет-магазина
        /// </summary>
        public OnlineStoreOrderPosition()
        {
            ControlTitle = "Позиция заказа";

            OnMessage = (ItemMessage message) => {

                DebugLog($"message=[{message.Message}]");

                if (message.ReceiverName == ControlName)
                {
                    ProcessCommand(message.Action);
                }
            };

            //конструктор, будет вызван, когда объект создается
            //здесь создаются все внутренние структуры
            //впервые этот коллбэк будет вызван, когда данный таб станет активным
            //впервые (до этих пор, никакая работа внутри не происходит, что экономит ресурсы)
            OnLoad = () =>
            {
                InitializeComponent();

                Init();
                SetDefaults();
                GridInit();

                if (PositionId > 0)
                {
                    PriceTextBox.IsReadOnly = false;
                }
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                ProductGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                ProductGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {

            };
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Выбранная запись в гриде продукций
        /// </summary>
        public Dictionary<string, string> ProductGridSelectedItem { get; set; }

        /// <summary>
        /// Данные для гида продукций
        /// </summary>
        public ListDataSet ProductGridDataSet { get; set; }

        /// <summary>
        /// Идентификатор заказа интернет магазина
        /// ONLINE_STORE_ORDER.ONSR_ID
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Идентификатор позиции заказа
        /// ONLINE_STORE_ORDER_ITEM.OSOI_ID
        /// </summary>
        public int PositionId { get; set; }

        /// <summary>
        /// Список уже добавленных в заказ позиций
        /// </summary>
        public List<string> SelectedCodeList { get; set; }

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
                    Path="QUANTITY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=QuantityTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="QUANTITY_PALLET",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=QuantityPalletTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="PRICE",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=PriceTextBox,
                    ControlType="TextBox",
                    Format="N2",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="PRICE_VAT",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=PriceVatTextBox,
                    ControlType="TextBox",
                    Format="N2",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="PRICE_DELIVERY",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=PriceDeliveryTextBox,
                    ControlType="TextBox",
                    Format="N2",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{

                    },
                },
                new FormHelperField()
                {
                    Path="PRICE_VAT_DELIVERY",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=PriceVatDeliveryTextBox,
                    ControlType="TextBox",
                    Format="N2",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{

                    },
                },

                new FormHelperField()
                 {
                    Path="SEARCH",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=SearchText,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
            };

            Form.SetFields(fields);
            Form.ToolbarControl = GridToolbar;
        }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void GridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Path="ID2",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="PRODUCT_FULL_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 330,
                        MaxWidth = 540,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Кол-во на СОХ",
                        Path="QUANTITY_IN_STOCK",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth = 55,
                        MaxWidth = 100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Path="CODE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 115,
                        MaxWidth = 115,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Оптовая цена без НДС",
                        Path="PRICE",
                        ColumnType=ColumnTypeRef.Double,
                        MinWidth = 95,
                        MaxWidth = 95,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Оптовая цена с НДС",
                        Path="PRICE_VAT",
                        ColumnType=ColumnTypeRef.Double,
                        MinWidth = 105,
                        MaxWidth = 105,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Розничная цена без НДС",
                        Path="RETAIL_PRICE",
                        ColumnType=ColumnTypeRef.Double,
                        MinWidth = 105,
                        MaxWidth = 105,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Розничная цена с НДС",
                        Path="RETAIL_PRICE_VAT",
                        ColumnType=ColumnTypeRef.Double,
                        MinWidth = 115,
                        MaxWidth = 115,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Оптовое количество поддонов",
                        Path="WHOLESALE_PALLET_QTY",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth = 30,
                        MaxWidth = 65,
                    },
                    new DataGridHelperColumn
                    {
                        Header="На поддоне, шт",
                        Path="COUNT_ON_PALLET",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth = 75,
                        MaxWidth = 85,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Минимальное кол-во для заказа",
                        Path="MIN_ORDER_QTY",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth = 75,
                        MaxWidth = 95,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Шаг изменения количества",
                        Path="STEP_QTY",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth = 75,
                        MaxWidth = 105,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Внутреннее наименование",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 185,
                        MaxWidth = 370,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Добавлен в заявку",
                        Path="ALREADY_SELECTED",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=55,
                        MaxWidth=80,
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
                ProductGrid.SetColumns(columns);

                // раскраска строк
                ProductGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    // определение цветов фона строк
                    {
                        StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            if(row.CheckGet("ALREADY_SELECTED").ToInt() == 1)
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

                ProductGrid.SearchText = SearchText;
                ProductGrid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                ProductGrid.OnSelectItem = selectedItem =>
                {
                    ProductGridSelectedItem = selectedItem;

                    UpdatePalletQuantity();
                    UpdatePrice();
                };

                //данные грида
                ProductGrid.OnLoadItems = ProductGridLoadItems;
                //ProductGrid.Run();

            }
        }

        public async void ProductGridLoadItems()
        {
            DisableControls();

            // Если редактируем существующую позицию, то показываем информацию только по ней
            if (PositionId > 0)
            {
                var p = new Dictionary<string, string>();
                p.Add("POSITION_ID", PositionId.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "OnlineStoreOrder");
                q.Request.SetParam("Action", "GetOrderPosition");
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
                        ProductGridDataSet = dataSet;
                        if (ProductGridDataSet != null && ProductGridDataSet.Items.Count > 0)
                        {
                            foreach (var item in ProductGridDataSet.Items)
                            {
                                double price = item.CheckGet("PRICE").ToDouble();
                                double retailMarginPct = item.CheckGet("RETAIL_MARGIN_PCT").ToDouble();
                                double retailPrice = Math.Round(price + ((price / 100) * retailMarginPct), 2);
                                item.CheckAdd("RETAIL_PRICE", retailPrice.ToString());

                                double retailPriceVat = Math.Round(retailPrice * 0.2 + retailPrice, 2);
                                item.CheckAdd("RETAIL_PRICE_VAT", retailPriceVat.ToString());
                            }
                        }
                        ProductGrid.UpdateItems(ProductGridDataSet);

                        var positionDataSet = ListDataSet.Create(result, "POSITION");
                        if (positionDataSet != null && positionDataSet.Items != null && positionDataSet.Items.Count > 0)
                        {
                            var positionData = positionDataSet.Items.First();
                            Form.SetValueByPath("QUANTITY", positionData.CheckGet("QUANTITY"));
                            Form.SetValueByPath("PRICE", positionData.CheckGet("PRICE"));
                            Form.SetValueByPath("PRICE_VAT", positionData.CheckGet("PRICE_VAT"));
                            Form.SetValueByPath("PRICE_DELIVERY", positionData.CheckGet("PRICE_DELIVERY"));
                            Form.SetValueByPath("PRICE_VAT_DELIVERY", positionData.CheckGet("PRICE_VAT_DELIVERY"));
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
            // Если создаём новую позицию, то показываем весь список продукции
            else
            {
                var p = new Dictionary<string, string>();

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "OnlineStoreOrder");
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
                        ProductGridDataSet = dataSet;
                        if (ProductGridDataSet != null && ProductGridDataSet.Items.Count > 0)
                        {
                            foreach (var item in ProductGridDataSet.Items)
                            {
                                double price = item.CheckGet("PRICE").ToDouble();
                                double retailMarginPct = item.CheckGet("RETAIL_MARGIN_PCT").ToDouble();
                                double retailPrice = Math.Round(price + ((price / 100) * retailMarginPct), 2);
                                item.CheckAdd("RETAIL_PRICE", retailPrice.ToString());

                                double retailPriceVat = Math.Round(retailPrice * 0.2 + retailPrice, 2);
                                item.CheckAdd("RETAIL_PRICE_VAT", retailPriceVat.ToString());

                                if (SelectedCodeList.Contains(item.CheckGet("CODE")))
                                {
                                    item.CheckAdd("ALREADY_SELECTED", "1");
                                }
                            }
                        }
                        ProductGrid.UpdateItems(ProductGridDataSet);
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            EnableControls();
        }

        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Show()
        {
            Central.WM.FrameMode = 1;

            if (PositionId > 0)
            {
                ControlName = $"{ControlName}_{PositionId}";
                Central.WM.Show(ControlName, $"Позиция #{PositionId} заказа #{OrderId}", true, "add", this);
            }
            else
            {
                var dt = DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss");
                ControlName = $"{ControlName}_new_{dt}";
                Central.WM.Show(ControlName, $"Новая позиция заказа #{OrderId}", true, "add", this);
            }
        }

        /// <summary>
        /// Обновляем количество поддонов в зависимости от количества картона и количества на поддоне для выбранной записи
        /// </summary>
        public void UpdatePalletQuantity()
        {
            if (ProductGridSelectedItem != null && ProductGridSelectedItem.Count > 0)
            {
                if (QuantityTextBox.Text.ToInt() > 0)
                {
                    int quantityInPallet = ProductGridSelectedItem.CheckGet("COUNT_ON_PALLET").ToInt();
                    int quantityCardboard = QuantityTextBox.Text.ToInt();

                    int quantityPallet = Math.Ceiling((double)quantityCardboard / (double)quantityInPallet).ToInt();
                    QuantityPalletTextBox.Text = quantityPallet.ToString();
                }
            }
        }

        public void UpdatePrice()
        {
            if (Form != null)
            {
                // Если редактируем существующий заказ, то даём редактировать цену без НДС. Цена с НДС будет расчитываться автоматически.
                if (PositionId > 0)
                {
                    double price = Form.GetValueByPath("PRICE").ToDouble();
                    double priceVat = Math.Round(price * 0.2 + price, 2);
                    Form.SetValueByPath("PRICE_VAT", priceVat.ToString());
                    Form.SetValueByPath("PRICE_DELIVERY", Form.GetValueByPath("PRICE"));
                    Form.SetValueByPath("PRICE_VAT_DELIVERY", priceVat.ToString());
                }
                // Если новая позиция заказа, то не даём редактировать цену. Цена изменяется взависимости от количества продукции в заказа
                else
                {
                    if (ProductGridSelectedItem != null && ProductGridSelectedItem.Count > 0)
                    {
                        if (QuantityPalletTextBox.Text.ToInt() > 0 && QuantityTextBox.Text.ToInt() > 0 && ProductGridSelectedItem.CheckGet("WHOLESALE_PALLET_QTY").ToInt() > 0)
                        {
                            //  Если количество поддонов больше или равно оптовому количеству, то берём оптовую цену
                            if (QuantityPalletTextBox.Text.ToInt() >= ProductGridSelectedItem.CheckGet("WHOLESALE_PALLET_QTY").ToInt())
                            {
                                Form.SetValueByPath("PRICE", ProductGridSelectedItem.CheckGet("PRICE").ToDouble().ToString());
                                Form.SetValueByPath("PRICE_VAT", ProductGridSelectedItem.CheckGet("PRICE_VAT").ToDouble().ToString());
                                Form.SetValueByPath("PRICE_DELIVERY", ProductGridSelectedItem.CheckGet("PRICE").ToDouble().ToString());
                                Form.SetValueByPath("PRICE_VAT_DELIVERY", ProductGridSelectedItem.CheckGet("PRICE_VAT").ToDouble().ToString());
                            }
                            // Иначе берём розничную цену
                            else
                            {
                                Form.SetValueByPath("PRICE", ProductGridSelectedItem.CheckGet("RETAIL_PRICE").ToDouble().ToString());
                                Form.SetValueByPath("PRICE_VAT", ProductGridSelectedItem.CheckGet("RETAIL_PRICE_VAT").ToDouble().ToString());
                                Form.SetValueByPath("PRICE_DELIVERY", ProductGridSelectedItem.CheckGet("RETAIL_PRICE").ToDouble().ToString());
                                Form.SetValueByPath("PRICE_VAT_DELIVERY", ProductGridSelectedItem.CheckGet("RETAIL_PRICE_VAT").ToDouble().ToString());
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Сохраняем данные по позиции заявки
        /// </summary>
        public void Save()
        {
            if (Form.Validate())
            {
                // Обновление существующей позиции
                if (PositionId > 0)
                {
                    // Проверяем, соответствует ли указанное количество продукции полным поддонам
                    var percentageOfDivision = QuantityTextBox.Text.ToInt() % ProductGridSelectedItem.CheckGet("COUNT_ON_PALLET").ToInt();
                    if (percentageOfDivision > 0)
                    {
                        int palletCount = QuantityTextBox.Text.ToInt() / ProductGridSelectedItem.CheckGet("COUNT_ON_PALLET").ToInt();
                        palletCount += 1;
                        int quantityWithoutRemainder = palletCount * ProductGridSelectedItem.CheckGet("COUNT_ON_PALLET").ToInt();

                        var message = $"Выбранное количество продукции не кратно поддону.{Environment.NewLine}" +
                            $"Для {palletCount} полных поддонов нужно выбрать {quantityWithoutRemainder} продукции.{Environment.NewLine}" +
                            $"Вы хотите продолжить c неполным поддоном?";

                        if (DialogWindow.ShowDialog(message, "Заказы интернет-магазина", "", DialogWindowButtons.NoYes) != true)
                        {
                            return;
                        }
                    }

                    var p = new Dictionary<string, string>();
                    p.AddRange(Form.GetValues());
                    p.CheckAdd("POSITION_ID", PositionId.ToString());

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sales");
                    q.Request.SetParam("Object", "OnlineStoreOrder");
                    q.Request.SetParam("Action", "UpdateOrderPosition");
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
                                // Отправляем сообщение табу заявок интернет-магазина о необходимости обновить грид
                                {
                                    Central.Msg.SendMessage(new ItemMessage()
                                    {
                                        ReceiverGroup = "OnlineStoreOrder",
                                        ReceiverName = "OnlineStoreOrderList",
                                        SenderName = "OnlineStoreOrderPosition",
                                        Action = "Refresh",
                                    });
                                }

                                Close();
                            }
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
                // Создание новой позиции
                else
                {
                    if (ProductGridSelectedItem.CheckGet("QUANTITY_IN_STOCK").ToInt() < QuantityTextBox.Text.ToInt())
                    {
                        var message = $"На складе недостаточно продукции.{Environment.NewLine}" +
                            $"Вы хотите продолжить?";

                        if (DialogWindow.ShowDialog(message, "Заказы интернет-магазина", "", DialogWindowButtons.NoYes) != true)
                        {
                            return;
                        }
                    }

                    // Проверяем, соответствует ли указанное количество продукции полным поддонам
                    var percentageOfDivision = QuantityTextBox.Text.ToInt() % ProductGridSelectedItem.CheckGet("COUNT_ON_PALLET").ToInt();
                    if (percentageOfDivision > 0)
                    {
                        int palletCount = QuantityTextBox.Text.ToInt() / ProductGridSelectedItem.CheckGet("COUNT_ON_PALLET").ToInt();
                        palletCount += 1;
                        int quantityWithoutRemainder = palletCount * ProductGridSelectedItem.CheckGet("COUNT_ON_PALLET").ToInt();

                        var message = $"Выбранное количество продукции не кратно поддону.{Environment.NewLine}" +
                            $"Для {palletCount} полных поддонов нужно выбрать {quantityWithoutRemainder} продукции.{Environment.NewLine}" +
                            $"Вы хотите продолжить c неполным поддоном?";

                        if (DialogWindow.ShowDialog(message, "Заказы интернет-магазина", "", DialogWindowButtons.NoYes) != true)
                        {
                            return;
                        }
                    }

                    var p = new Dictionary<string, string>();
                    p.AddRange(Form.GetValues());
                    p.CheckAdd("ORDER_ID", OrderId.ToString());
                    p.CheckAdd("PRODUCT_ID", ProductGridSelectedItem.CheckGet("ID2"));

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sales");
                    q.Request.SetParam("Object", "OnlineStoreOrder");
                    q.Request.SetParam("Action", "SaveOrderPosition");
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
                                // Отправляем сообщение табу заявок интернет-магазина о необходимости обновить грид
                                {
                                    Central.Msg.SendMessage(new ItemMessage()
                                    {
                                        ReceiverGroup = "OnlineStoreOrder",
                                        ReceiverName = "OnlineStoreOrderList",
                                        SenderName = "OnlineStoreOrderPosition",
                                        Action = "Refresh",
                                    });
                                }

                                Close();
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

        public void ProcessCommand(string command)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "help":
                        {
                            //FIXME: Нужно сделать документацию
                            Central.ShowHelp("/doc/l-pack-erp-new/application/online_shop");
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();
            ProductGridSelectedItem = new Dictionary<string, string>();
            ProductGridDataSet = new ListDataSet();

            if (SelectedCodeList == null)
            {
                SelectedCodeList = new List<string>();
            }
        }

        /// <summary>
        /// Деактивация контроллов
        /// </summary>
        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
            ProductGrid.IsEnabled = false;
        }

        /// <summary>
        /// Активация контроллов
        /// </summary>
        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
            ProductGrid.IsEnabled = true;
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            Central.WM.Close(ControlName);
        }

        public void Refresh()
        {
            SetDefaults();
            ProductGridLoadItems();
        }

        private void QuantityTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdatePalletQuantity();
            UpdatePrice();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ResreshButton_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessCommand("help");
        }

        private void PriceTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdatePrice();
        }
    }
}
