using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
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

namespace Client.Interfaces.Orders
{
    /// <summary>
    /// Позиция заявки на отгрузку макулатуры
    /// </summary>
    public partial class ScrapPaperConsumptionKshOrderPosition : ControlBase
    {
        public ScrapPaperConsumptionKshOrderPosition()
        {
            ControlTitle = "Позиция заявки на отгрузку макулатуры";
            DocumentationUrl = "/doc/l-pack-erp/";
            RoleName = "[erp]order_scrap_paper_ksh";

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
                PositionGridInit();
                SetDefaults();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                PositionGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
            };
        }

        public string ParentFrame { get; set; }

        public int OrderId { get; set; }

        public int BuyerId { get; set; }

        public int OrderPositionId { get; set; }

        public int FactoryId = 2;

        public bool DefaultShipment = true;

        /// <summary>
        /// процессор форм
        /// </summary>
        private FormHelper Form { get; set; }

        private ListDataSet PositionGridDataSet { get; set; }

        private ListDataSet AddressDataSet { get; set; }

        private int DefaultProductCategoryId = 121;

        private int DefaultProductId = 581848;

        private double DefaultPrice = 15.16;

        private double DefaultPriceWithoutVat = 15.16;

        private int DefaultPositionQuantity = 20000;

        private int DefaultAddressId = 2;

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

            FrameName = $"{FrameName}";
            if (OrderPositionId > 0)
            {
                Central.WM.Show(FrameName, $"Позиция заявки #{OrderPositionId}", true, "add", this);
            }
            else
            {
                Central.WM.Show(FrameName, $"Новая позиция заявки", true, "add", this);
            }
        }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        private void FormInit()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="ADDRESS_ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=AddressSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="SHIPMENT_ORDER",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ShipOrderTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="NOTE_STOCKMAN",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=NoteStockmanTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NOTE_LOADER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=NoteLoaderTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="QUANTITY_LIMIT_TYPE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=QuantityLimitSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="QUANTITY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=QuantityTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="PRICE_WITHOUT_VAT",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=PriceWithoutVatTextBox,
                    ControlType="TextBox",
                    Format="N8",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitCommaOnly, null },
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="PRICE",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=PriceTextBox,
                    ControlType="TextBox",
                    Format="N6",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitCommaOnly, null },
                    },
                },

                new FormHelperField()
                {
                    Path="FIXED_PRICE",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
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
        /// Установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            Form.SetDefaults();
            PositionGridDataSet = new ListDataSet();
            AddressDataSet = new ListDataSet();

            var quantityLimitSelectBoxItems = new Dictionary<string, string>();
            quantityLimitSelectBoxItems.Add("0", "Без ограничения");
            quantityLimitSelectBoxItems.Add("1", "Не менее");
            quantityLimitSelectBoxItems.Add("2", "Не более");
            quantityLimitSelectBoxItems.Add("3", "Точное количество");
            QuantityLimitSelectBox.SetItems(quantityLimitSelectBoxItems);
            QuantityLimitSelectBox.SetSelectedItemByKey("0");

            if (OrderPositionId > 0)
            {
                if (DefaultShipment)
                {
                    var addressSelectBoxItems = new Dictionary<string, string>();
                    addressSelectBoxItems.Add($"{DefaultAddressId}", "Россия, 398007, Липецкая обл, г Липецк, Промышленный проезд, стр 1А");
                    AddressSelectBox.SetItems(addressSelectBoxItems);
                }
                else
                {
                    AddressSelectBoxLoadItems();
                }

                GetOrderPositionData();
                PositionSearchText.IsEnabled= false;
            }
            else
            {
                if (DefaultShipment)
                {
                    Form.SetValueByPath("SHIPMENT_ORDER", "1");
                    Form.SetValueByPath("QUANTITY", $"{DefaultPositionQuantity}");
                    Form.SetValueByPath("PRICE_WITHOUT_VAT", $"{DefaultPriceWithoutVat}");
                    Form.SetValueByPath("PRICE", $"{DefaultPrice}");

                    var addressSelectBoxItems = new Dictionary<string, string>();
                    addressSelectBoxItems.Add($"{DefaultAddressId}", "Россия, 398007, Липецкая обл, г Липецк, Промышленный проезд, стр 1А");
                    AddressSelectBox.SetItems(addressSelectBoxItems);
                    AddressSelectBox.SetSelectedItemByKey($"{DefaultAddressId}");

                    QuantityLimitSelectBox.SetSelectedItemByKey("0");

                    PositionGridLoadItems($"{DefaultProductId}");
                }
                else
                {
                    AddressSelectBoxLoadItems();
                    PositionGridLoadItems();
                }
            }
        }

        private async void GetOrderPositionData()
        {
            if (OrderPositionId > 0)
            {
                var p = new Dictionary<string, string>();
                p.Add("ORDER_POSITION_ID", $"{OrderPositionId}");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Orders");
                q.Request.SetParam("Object", "ScrapPaper");
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
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                        {
                            Form.SetValues(ds);
                            PositionGridLoadItems(ds.Items[0].CheckGet("PRODUCT_ID"));
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        private void PositionGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид продукции",
                        Description = "Идентификатор продукции",
                        Path="PRODUCT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Description = "Наименование продукции",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=38,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Description = "Артикул продукции",
                        Path="PRODUCT_CODE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=15,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Размеры",
                        Description = "Размеры продукции",
                        Path="PRODUCT_SIZE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество по умолчанию",
                        Description = "Стандартное количество продукции",
                        Path="DEFAULT_QUANTITY",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=20,
                        Format="N0",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ед. изм.",
                        Description = "Единицы измерения",
                        Path="MEASUREMENT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=7,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Ид категории продукции",
                        Description = "Идентификатор категории продукции",
                        Path="PRODUCT_CATEGORY_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Длина",
                        Description = "Длина продукции",
                        Path="PRODUCT_LENGTH",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ширина",
                        Description = "Ширина продукции",
                        Path="PRODUCT_WIDTH",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Высота",
                        Description = "Высота продукции",
                        Path="PRODUCT_HEIGHT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden = true,
                    },
                };
                PositionGrid.SetColumns(columns);
                PositionGrid.SearchText = PositionSearchText;
                PositionGrid.SetPrimaryKey("PRODUCT_ID");
                PositionGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                PositionGrid.AutoUpdateInterval = 0;
                PositionGrid.ItemsAutoUpdate = false;
                PositionGrid.Toolbar = PositionGridToolbar;
                PositionGrid.Commands = Commander;
                PositionGrid.UseProgressSplashAuto = false;
                PositionGrid.Init();
                PositionGrid.Run();
            }
        }

        private async void PositionGridLoadItems(string productId = "")
        {
            var p = new Dictionary<string, string>();
            p.Add("BUYER_ID", $"{BuyerId}");

            if (!string.IsNullOrEmpty(productId))
            {
                p.Add("PRODUCT_ID", productId);
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Orders");
            q.Request.SetParam("Object", "ScrapPaper");
            q.Request.SetParam("Action", "ListProduct");
            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            PositionGridDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    PositionGridDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            else
            {
                q.ProcessError();
            }
            PositionGrid.UpdateItems(PositionGridDataSet);
        }

        private async void AddressSelectBoxLoadItems()
        {
            // FIXME не реализовано

            var p = new Dictionary<string, string>();
            p.Add("BUYER_ID", $"{BuyerId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Orders");
            q.Request.SetParam("Object", "ScrapPaper");
            q.Request.SetParam("Action", "ListAddress");
            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            AddressDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    AddressDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            else
            {
                q.ProcessError();
            }
            AddressSelectBox.SetItems(AddressDataSet);
        }

        private void Save()
        {
            if (PositionGrid.SelectedItem != null && PositionGrid.SelectedItem.Count > 0)
            {
                if (Form.Validate())
                {
                    if (OrderPositionId > 0)
                    {
                        var p = Form.GetValues();
                        p.CheckAdd("ORDER_POSITION_ID", $"{OrderPositionId}");

                        if (AutoPriceCheckBox.IsChecked == true)
                        {
                            p.Remove("PRICE");
                        }

                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Orders");
                        q.Request.SetParam("Object", "ScrapPaper");
                        q.Request.SetParam("Action", "UpdateOrderPosition");
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
                                    if (dataSet.Items[0].CheckGet("ORDER_POSITION_ID").ToInt() > 0)
                                    {
                                        succesfullFlag = true;
                                    }
                                }
                            }

                            if (succesfullFlag)
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    var msg = "Успешное изменение позиции заявки на отгрузку макулатуры.";
                                    var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                                    d.ShowDialog();
                                });

                                // Отправляем сообщение вкладке "Список приходных накладных" обновиться
                                Central.Msg.SendMessage(new ItemMessage()
                                {
                                    ReceiverGroup = "ScrapPaperKsh",
                                    ReceiverName = this.ParentFrame,
                                    SenderName = this.ControlName,
                                    Action = "refresh",
                                });

                                this.Close();
                            }
                            else
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    var msg = "Ошибка изменения позиции заявки на отгрузку макулатуры. Пожалуйста, сообщите о проблеме.";
                                    var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                                    d.ShowDialog();
                                });
                            }
                        }
                        else
                        {
                            q.ProcessError();
                        }
                    }
                    else
                    {
                        var p = Form.GetValues();
                        p.CheckAdd("ORDER_ID", $"{OrderId}");
                        p.CheckAdd("PRODUCT_CATEGORY_ID", PositionGrid.SelectedItem.CheckGet("PRODUCT_CATEGORY_ID"));
                        p.CheckAdd("PRODUCT_ID", PositionGrid.SelectedItem.CheckGet("PRODUCT_ID"));

                        if (AutoPriceCheckBox.IsChecked == true)
                        {
                            p.Remove("PRICE");
                        }

                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Orders");
                        q.Request.SetParam("Object", "ScrapPaper");
                        q.Request.SetParam("Action", "SaveOrderPosition");
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
                                    if (dataSet.Items[0].CheckGet("ORDER_POSITION_ID").ToInt() > 0)
                                    {
                                        succesfullFlag = true;
                                    }
                                }
                            }

                            if (succesfullFlag)
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    var msg = "Успешное создание позиции заявки на отгрузку макулатуры.";
                                    var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                                    d.ShowDialog();
                                });

                                // Отправляем сообщение вкладке "Список приходных накладных" обновиться
                                Central.Msg.SendMessage(new ItemMessage()
                                {
                                    ReceiverGroup = "ScrapPaperKsh",
                                    ReceiverName = this.ParentFrame,
                                    SenderName = this.ControlName,
                                    Action = "refresh",
                                });

                                this.Close();
                            }
                            else
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    var msg = "Ошибка создания позиции заявки на отгрузку макулатуры. Пожалуйста, сообщите о проблеме.";
                                    var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                                    d.ShowDialog();
                                });
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
        /// сокрытие фрейма
        /// </summary>
        private void Close()
        {
            if (!string.IsNullOrEmpty(ParentFrame))
            {
                Central.WM.SetActive(ParentFrame, true);
            }

            Central.WM.Close(FrameName);

            //вся работа по утилизации ресурсов происходит в Destroy
            //он будет вызван при закрытии фрейма
        }

        private void CheckAutoPrice()
        {
            if (AutoPriceCheckBox.IsChecked == true)
            {
                PriceTextBox.IsEnabled = false;
                Form.SetValueByPath("PRICE", "");
            }
            else
            {
                PriceTextBox.IsEnabled = true;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            Central.ShowHelp(DocumentationUrl);
        }

        private void AutoPriceCheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckAutoPrice();
        }
    }
}
