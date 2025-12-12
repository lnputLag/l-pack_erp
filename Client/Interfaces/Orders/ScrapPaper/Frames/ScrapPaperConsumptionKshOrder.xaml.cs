using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Sales;
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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace Client.Interfaces.Orders
{
    /// <summary>
    /// Заявка на отгрузку макулатуры
    /// </summary>
    public partial class ScrapPaperConsumptionKshOrder : ControlBase
    {
        public ScrapPaperConsumptionKshOrder()
        {
            ControlTitle = "Заявка на отгрузку макулатуры";
            DocumentationUrl = "/doc/l-pack-erp/";

            RoleName = "[erp]order_scrap_paper_ksh";

            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

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
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                //отключаем обработчик сообщений
                Messenger.Default.Unregister<ItemMessage>(this);
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

        public int FactoryId = 2;

        /// <summary>
        /// процессор форм
        /// </summary>
        private FormHelper Form { get; set; }

        /// <summary>
        /// Продавец
        /// 427 ООО "Л-ПАК Кашира"
        /// </summary>
        private int SellerId = 427;

        /// <summary>
        /// Грузоотправитель
        /// 427 ООО "Л-ПАК Кашира"
        /// </summary>
        private int ShipperId = 427;

        /// <summary>
        /// Идентификатор банка
        /// 21165 Л-ПАК Кашира ВТБ
        /// </summary>
        private int BankId = 21165;

        /// <summary>
        /// Тип заявки
        /// 5 Макулатура
        /// </summary>
        private int OrderType = 5;

        /// <summary>
        /// Покупатель по умолчанию
        /// 7456 ООО "Л-ПАК"
        /// </summary>
        private int DefaultBuyerId = 7456;

        /// <summary>
        /// Покупатель по умолчанию
        /// 7456 ООО "Л-ПАК"
        /// </summary>
        private string DefaultBuyerName = "ООО \"Л-ПАК\"";

        /// <summary>
        /// Флаг самовывоза по умолчанию
        /// </summary>
        private int DefaultSelfShipFlag = 1;

        private string DefaultOrderNumber = "Мак Кш";

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
            if (OrderId > 0)
            {
                Central.WM.Show(FrameName, $"Заявка #{OrderId}", true, "add", this);
            }
            else
            {
                Central.WM.Show(FrameName, $"Новая заявка", true, "add", this);
            }
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverName.IndexOf(this.FrameName) > -1)
            {
                switch (m.Action)
                {
                    case "SelectItem":
                        int type = m.Message.ToInt();
                        var selectedBuyerData = (Dictionary<string, string>)m.ContextObject;
                        if (type == 2)
                        {
                            Form.SetValueByPath("CONSIGNEE_NAME", $"{selectedBuyerData.CheckGet("BUYER_NAME")} ({selectedBuyerData.CheckGet("ID")})");
                            Form.SetValueByPath("CONSIGNEE_ID", selectedBuyerData.CheckGet("ID"));
                        }
                        else
                        {
                            Form.SetValueByPath("BUYER_NAME", $"{selectedBuyerData.CheckGet("BUYER_NAME")} ({selectedBuyerData.CheckGet("ID")})");
                            Form.SetValueByPath("BUYER_ID", selectedBuyerData.CheckGet("ID"));
                        }
                        break;
                }
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
                    Path="ORDER_NUMBER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=OrderNumberTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="BUYER_NAME",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = BuyerTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="CONSIGNEE_NAME",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = ConsigneeTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="CONTRACT",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = ContractTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.IsNotZero, null },
                    },
                },
                new FormHelperField()
                {
                    Path="SHIPMENT_DATE_TIME",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = ShipmentDateTimeTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="DELIVERY_DATE_TIME",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = DeliveryDateTimeTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="SELFSHIP",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=SelfShipCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
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
                    Path="NOTE_LOGISTICIAN",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=NoteLogisticianTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                new FormHelperField()
                {
                    Path="BUYER_ID",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = BuyerIdTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="CONSIGNEE_ID",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = ConsigneeIdTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
            };

            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
            Form.OnValidate = (valid, message) =>
            {
                if (!valid)
                {
                    if (string.IsNullOrEmpty(Form.GetValueByPath("SHIPMENT_DATE_TIME")))
                    {
                        var color = "#ffee0000";
                        var bc = new BrushConverter();
                        var brush = (Brush)bc.ConvertFrom(color);
                        ShipmentDateTimeTextBox.BorderBrush = brush;
                    }
                    else
                    {
                        var color = "#ffcccccc";
                        var bc = new BrushConverter();
                        var brush = (Brush)bc.ConvertFrom(color);
                        ShipmentDateTimeTextBox.BorderBrush = brush;
                    }
                }
                else
                {
                    var color = "#ffcccccc";
                    var bc = new BrushConverter();
                    var brush = (Brush)bc.ConvertFrom(color);
                    ShipmentDateTimeTextBox.BorderBrush = brush;
                }
            };
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            Form.SetDefaults();

            if (OrderId > 0)
            {
                GetOrderData();
            }
            else
            {
                Form.SetValueByPath("BUYER_NAME", $"{DefaultBuyerName} ({DefaultBuyerId})");
                Form.SetValueByPath("BUYER_ID", $"{DefaultBuyerId}");

                Form.SetValueByPath("CONSIGNEE_NAME", $"{DefaultBuyerName} ({DefaultBuyerId})");
                Form.SetValueByPath("CONSIGNEE_ID", $"{DefaultBuyerId}");

                Form.SetValueByPath("SELFSHIP", $"{DefaultSelfShipFlag}");
                Form.SetValueByPath("ORDER_NUMBER", $"{DefaultOrderNumber} {DateTime.Now.ToString("dd")}/{DateTime.Now.ToString("MM")}/{DateTime.Now.ToString("yyyy")}");
            }
        }

        private async void GetOrderData()
        {
            if (OrderId > 0)
            {
                var p = new Dictionary<string, string>();
                p.Add("ORDER_ID", $"{OrderId}");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Orders");
                q.Request.SetParam("Object", "ScrapPaper");
                q.Request.SetParam("Action", "GetOrder");
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
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        private async void Save()
        {
            if (Form.Validate())
            {
                if (OrderId > 0)
                {
                    var p = Form.GetValues();
                    p.CheckAdd("ORDER_ID", $"{OrderId}");

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Orders");
                    q.Request.SetParam("Object", "ScrapPaper");
                    q.Request.SetParam("Action", "UpdateOrder");
                    q.Request.SetParams(p);

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    if (q.Answer.Status == 0)
                    {
                        bool succesfullFlag = false;
                        int orderId = 0;

                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            var dataSet = ListDataSet.Create(result, "ITEMS");
                            if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                            {
                                orderId = dataSet.Items[0].CheckGet("ORDER_ID").ToInt();
                                if (orderId > 0)
                                {
                                    succesfullFlag = true;
                                }
                            }
                        }

                        if (succesfullFlag)
                        {
                            var msg = "Успешное изменение заявки на отгрузку макулатуры.";
                            var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                            d.ShowDialog();

                            // Отправляем сообщение вкладке "Список приходных накладных" обновиться
                            Central.Msg.SendMessage(new ItemMessage()
                            {
                                ReceiverGroup = "ScrapPaperKsh",
                                ReceiverName = this.ParentFrame,
                                SenderName = this.ControlName,
                                Action = "refresh",
                            });

                            Close();
                        }
                        else
                        {
                            var msg = "Ошибка изменения заявки на отгрузку макулатуры. Пожалуйста, сообщите о проблеме.";
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
                    var p = Form.GetValues();

                    if (string.IsNullOrEmpty(p.CheckGet("CONSIGNEE_ID")))
                    {
                        p.CheckAdd("CONSIGNEE_ID", p.CheckGet("BUYER_ID"));
                    }

                    p.CheckAdd("SELLER_ID", $"{SellerId}");
                    p.CheckAdd("SHIPPER_ID", $"{ShipperId}");
                    p.CheckAdd("BANK_ID", $"{BankId}");
                    p.CheckAdd("ORDER_TYPE", $"{OrderType}");
                    p.CheckAdd("FACTORY_ID", $"{FactoryId}");

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Orders");
                    q.Request.SetParam("Object", "ScrapPaper");
                    q.Request.SetParam("Action", "SaveOrder");
                    q.Request.SetParams(p);

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    if (q.Answer.Status == 0)
                    {
                        bool succesfullFlag = false;
                        int orderId = 0;

                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            var dataSet = ListDataSet.Create(result, "ITEMS");
                            if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                            {
                                orderId = dataSet.Items[0].CheckGet("ORDER_ID").ToInt();
                                if (orderId > 0)
                                {
                                    succesfullFlag = true;
                                }
                            }
                        }

                        if (succesfullFlag)
                        {
                            var msg = "Успешное создание заявки на отгрузку макулатуры.";
                            var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                            d.ShowDialog();

                            // Отправляем сообщение вкладке "Список приходных накладных" обновиться
                            Central.Msg.SendMessage(new ItemMessage()
                            {
                                ReceiverGroup = "ScrapPaperKsh",
                                ReceiverName = this.ParentFrame,
                                SenderName = this.ControlName,
                                Action = "refresh",
                            });

                            await Task.Run(() =>
                            {
                                SaveDefaultPosition(orderId);
                            });

                            Close();
                        }
                        else
                        {
                            var msg = "Ошибка создания заявки на отгрузку макулатуры. Пожалуйста, сообщите о проблеме.";
                            var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
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
                string msg = "Не все обязательные поля заполнены";
                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        private void SaveDefaultPosition(int orderId)
        {
            var p = new Dictionary<string, string>();
            p.CheckAdd("PRODUCT_CATEGORY_ID", $"{DefaultProductCategoryId}");
            p.CheckAdd("PRODUCT_ID", $"{DefaultProductId}");
            p.CheckAdd("PRICE", $"{DefaultPrice}");
            p.CheckAdd("PRICE_WITHOUT_VAT", $"{DefaultPriceWithoutVat}");
            p.CheckAdd("QUANTITY", $"{DefaultPositionQuantity}");
            p.CheckAdd("ADDRESS_ID", $"{DefaultAddressId}");
            p.CheckAdd("ORDER_ID", $"{orderId}");

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

        private void CheckGetContract()
        {
            Form.SetValueByPath("CONTRACT", "");

            var p = new Dictionary<string, string>();
            p.Add("BUYER_ID", BuyerIdTextBox.Text);
            p.Add("SELLER_ID", $"{SellerId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Orders");
            q.Request.SetParam("Object", "ScrapPaper");
            q.Request.SetParam("Action", "GetContract");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                bool succesfulFlag = false;
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var dataSet = ListDataSet.Create(result, "ITEMS");
                    if (dataSet != null && dataSet.Items.Count > 0)
                    {
                        int resultId = dataSet.Items.First().CheckGet("CONTRACT_ID").ToInt();
                        if (resultId > 0)
                        {
                            Form.SetValueByPath("CONTRACT", resultId.ToString());
                            succesfulFlag = true;
                        }
                    }
                }

                if (!succesfulFlag)
                {
                    string msg = "Ошибка получения договора. Пожалуйста, сообщите о проблеме.";
                    var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Выбор покупателя из списка покупателей
        /// </summary>
        private void SelectBuyer(int type)
        {
            var i = new BuyerList("ScrapPaperKsh", this.FrameName, FrameName);
            i.Type = type;
            i.Show();
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

        private void SelectBuyerButton_Click(object sender, RoutedEventArgs e)
        {
            SelectBuyer(1);
        }

        private void BuyerIdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckGetContract();
        }

        private void SelectConsigneeButton_Click(object sender, RoutedEventArgs e)
        {
            SelectBuyer(2);
        }

        private void ConsigneeIdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

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
    }
}
