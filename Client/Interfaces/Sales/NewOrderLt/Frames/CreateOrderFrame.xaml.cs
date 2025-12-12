using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Client.Interfaces.Sales.NewOrderLt.Frames
{
    public partial class CreateOrderFrame : ControlBase
    {
        public CreateOrderFrame()
        {
            InitializeComponent();
            
            InitForm();

            FrameMode = 0;
            OnGetFrameTitle = () =>
            {
                string result;
                var id = OrderId;

                if (id == 0)
                {
                    result = "Создание заявки на ЛТ";
                }
                else
                {
                    result = $"Изменение заявки на ЛТ - {OrderId}";
                }

                return result;
            };

            {
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "save",
                        Enabled = true,
                        Title = "Сохранить",
                        Description = "Сохранение данных",
                        ButtonUse = true,
                        ButtonName = "SaveButton",
                        MenuUse = false,
                        Action = () =>
                        {
                            Save();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "close",
                        Enabled = true,
                        Title = "Отмена",
                        Description = "Закрыть форму без сохранения",
                        ButtonUse = true,
                        ButtonName = "CancelButton",
                        MenuUse = false,
                        Action = () =>
                        {
                            Close();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "help",
                        Enabled = true,
                        Description = "Справка",
                        ButtonUse = true,
                        ButtonName = "HelpButton",
                        MenuUse = false,
                        Action = () =>
                        {
                            Central.ShowHelp(DocumentationUrl);
                        },
                    });
                }
                Commander.Init(this);
            }
        }
        
        /// <summary>
        /// Форма редактирования техкарты
        /// </summary>
        FormHelper Form { get; set; }
        /// <summary>
        /// ID заявки
        /// </summary>
        public int OrderId;
        /// <summary>
        /// ID покупателя
        /// </summary>
        public int BuyerId;

        private int ContractId;
        private int BankAccountId;

        private DateTime ShipmentOldDate;
        private DateTime SupplyOldDate;

        /// <summary>
        /// Инициализация формы
        /// </summary>
        private void InitForm()
        {
            Form = new FormHelper();

            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                  Path="ID_BUYER",
                  FieldType=FormHelperField.FieldTypeRef.Integer,
                  Control=Buyer,
                  ControlType="SelectBox",
                  Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                      { FormHelperField.FieldFilterRef.Required, null },
                  },
                  OnChange = (field, s) =>
                  {
                      var f = (SelectBox)field.Control;
                      BuyerId = f.SelectedItem.Key.ToInt();
                      GetData();
                  },
                  QueryLoadItems = new RequestData()
                  {
                      Module = "NewOrderLt",
                      Object = "CreateOrder",
                      Action = "BuyerList",
                      AnswerSectionKey = "ITEMS",
                      OnComplete = (FormHelperField f, ListDataSet ds) =>
                      {
                          var row = new Dictionary<string, string>();
                          ds.ItemsPrepend(row);
                          var list = ds.GetItemsList("ID", "NAME");
                          var c = (SelectBox)f.Control;
                          if ( c != null)
                          {
                              c.Items = list;
                          }

                          Buyer.SetSelectedItemFirst();
                          BuyerId = Buyer.SelectedItem.Key.ToInt();
                      }
                  }
                },
                new FormHelperField()
                {
                    Path="ID_POK2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Consignee,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.IsNotZero, null}
                    },
                },
                new FormHelperField()
                {
                    Path="ID_PROD",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Seller,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="IDDOG",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Contract,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="IDK",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=BankAccount,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SELFSHIP",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=SelfDelivery,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NUMBER_ORDER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=OrderNumber,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DATA",
                    FieldType=FormHelperField.FieldTypeRef.Date,
                    Control=ShipmentDate,
                    ControlType="DateEdit",
                    Format="dd.MM.yyyy",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DT_SUPPLY",
                    FieldType=FormHelperField.FieldTypeRef.DateTime,
                    Control=SupplyDate,
                    ControlType="DateEdit",
                    Format="dd.MM.yyyy HH:mm:ss",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DT_CONFIRM",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=DateConfirmation,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PREPAY_CONFIRM",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=PrepayConfirmation,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NOTE_GENERAL",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=NoteStorekeeper,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=NoteLoader,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NOTE_LOGISTIC",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=NoteLogist,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                // -- Блок полей печати документов --
                new FormHelperField()
                {
                    Path = "PRINT_ACCOUNT",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = ReceiptTextBox,
                    ControlType = "TextBox",
                    Options="zeronoempty",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path = "PRINT_INVOICE",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = InvoiceTextBox,
                    ControlType = "TextBox",
                    Options="zeronoempty",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path = "PRINT_WAYBILL",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = PackingListTextBox,
                    ControlType = "TextBox",
                    Options="zeronoempty",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path = "PRINT_WAYBILL_TR",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = WaybillTextBox,
                    ControlType = "TextBox",
                    Options="zeronoempty",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path = "PRINT_WAYBILL_TR_INNER",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = InnerWaybillTextBox,
                    ControlType = "TextBox",
                    Options="zeronoempty",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path = "PRINT_WAYBILL_TR_CLIENT",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = ClientWaybillTextBox,
                    ControlType = "TextBox",
                    Options="zeronoempty",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path = "PRINT_WAYBILL_TOV_TR",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = ConsignmentNoteTextBox,
                    ControlType = "TextBox",
                    Options="zeronoempty",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path = "PRINT_WAYBILL_TOV_TR_INNER",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = InnerConsignmentNoteTextBox,
                    ControlType = "TextBox",
                    Options="zeronoempty",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path = "PRINT_WAYBILL_INNER",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = ClientConsignmentNoteTextBox,
                    ControlType = "TextBox",
                    Options="zeronoempty",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path = "PRINT_UK",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = QualityCertificateTextBox,
                    ControlType = "TextBox",
                    Options="zeronoempty",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path = "PRINT_PAPER_UK",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = QualityCertificateOnPaperTextBox,
                    ControlType = "TextBox",
                    Options="zeronoempty",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path = "PRINT_PASSPORT_QUALITY",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = QualityPassportTextBox,
                    ControlType = "TextBox",
                    Options="zeronoempty",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path = "PRINT_CERTIFICATE_GOST",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = CertificateGostTextBox,
                    ControlType = "TextBox",
                    Options="zeronoempty",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path = "PRINT_CERTIFICATE_TU",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = CertificateTuTextBox,
                    ControlType = "TextBox",
                    Options="zeronoempty",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path = "PRINT_PAPER_SPECIFICATION",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = SpecificationOnPaperTextBox,
                    ControlType = "TextBox",
                    Options="zeronoempty",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path = "PRINT_UPD",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = UniversalTransferDocTextBox,
                    ControlType = "TextBox",
                    Options="zeronoempty",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path = "PRINT_TORG12_INNER",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = InnerUniversalTransferDocTextBox,
                    ControlType = "TextBox",
                    Options="zeronoempty",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path = "PRINT_CMR",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = CmrTextBox,
                    ControlType = "TextBox",
                    Options="zeronoempty",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path = "PRINT_CMR_INNER",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = InnerCmrTextBox,
                    ControlType = "TextBox",
                    Options="zeronoempty",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
            };

            Form.SetFields(fields);
            Form.StatusControl = FormStatus;

            // Блокируем кнопку сохранения, пока не выполнена загрузка данных
            SaveButton.IsEnabled = false;
        }
        
        /// <summary>
        /// Получение данных для формы
        /// </summary>
        private async void GetData()
        {
            var f = Form.GetValues();
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "NewOrderLt");
            q.Request.SetParam("Object", "CreateOrder");
            q.Request.SetParam("Action", "GetData");
            q.Request.SetParam("ORDER_ID", OrderId.ToString());
            q.Request.SetParam("BUYER_ID", f.CheckGet("ID_BUYER"));

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestGridAttempts;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    // Грузополучатель
                    var consigneeDS = ListDataSet.Create(result, "CONSIGNEE");
                    Consignee.Items = consigneeDS.GetItemsList("ID", "NAME");
                    if (consigneeDS.Items.Count > 0)
                    {
                        var first = consigneeDS.Items[0];
                        Consignee.SetSelectedItemByKey(first["ID"]);
                        Consignee.IsReadOnly = false;
                    }

                    if (consigneeDS.Items.Count == 0)
                    {
                        var d = new Dictionary<string, string>()
                        {
                            {"0", "Отсутствует"}
                        };

                        Consignee.Items = d;
                        Consignee.SetSelectedItemByKey("0");
                        Consignee.IsReadOnly = true;
                    }

                    // Продавец
                    var sellerDS = ListDataSet.Create(result, "SELLERS");
                    Seller.Items = sellerDS.GetItemsList("ID", "NAME");

                    var orderDS = ListDataSet.Create(result, "ORDER");
                    var supplyDateTime = orderDS.Items[0].CheckGet("DT_SUPPLY");
                    ContractId = orderDS.Items[0].CheckGet("IDDOG").ToInt();
                    BankAccountId = orderDS.Items[0].CheckGet("IDK").ToInt();
                    
                    //Примечание логисту по умолчанию
                    if (OrderId == 0)
                    {
                        orderDS.Items[0].CheckAdd("NOTE_LOGISTIC", "Габариты транспорта должны быть в длину не менее 13,5 м");
                    }

                    Form.SetValues(orderDS);

                    // Если продавец не выбран, по умолчанию ставим продавца ТД Л-ПАК
                    if (Seller.SelectedItem.Key.ToInt() == 0)
                    {
                        Seller.SetSelectedItemByKey("1");
                    }

                    SaveButton.IsEnabled = true;

                    //Если в заявке есть позиции, флаг самовывоза должен быть недоступен
                    bool hasPosition = orderDS.Items[0].CheckGet("HAS_POSITION").ToBool();
                    SelfDelivery.IsEnabled = !hasPosition;

                    // Проверяем, создана ли отгрузка. Если да, то даты менять нельзя
                    bool hasShipment = orderDS.Items[0].CheckGet("ID_TS").ToInt() > 0;
                    ShipmentOldDate = orderDS.Items[0].CheckGet("DATA").ToDateTime();
                    SupplyOldDate = orderDS.Items[0].CheckGet("DT_SUPPLY").ToDateTime();
                    SetDatePickersAvailable(hasShipment);

                    if (OrderId != 0)
                    {
                        Buyer.SetSelectedItemByKey(BuyerId.ToString());
                        Buyer.IsReadOnly = true;
                    }
                }
            }
        }

        /// <summary>
        /// Получение связанных справочников для выбранного продавца
        /// </summary>
        private async void SetSellerData()
        {
            var f = Form.GetValues();
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "NewOrderLt");
            q.Request.SetParam("Object", "CreateOrder");
            q.Request.SetParam("Action", "GetSellerData");
            q.Request.SetParam("SELLER_ID", Seller.SelectedItem.Key);
            q.Request.SetParam("BUYER_ID", f.CheckGet("ID_BUYER"));

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestGridAttempts;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var accountsDS = ListDataSet.Create(result, "ACCOUNTS");
                    BankAccount.Items = accountsDS.GetItemsList("ID", "NAME");
                    if (BankAccountId > 0)
                    {
                        BankAccount.SetSelectedItemByKey(BankAccountId.ToString());
                        BankAccountId = 0;
                    }
                    else
                    {
                        BankAccount.SetSelectedItemFirst();
                    }
                    
                    // Справочник договоров покупателя и продавца
                    // Выведем только договоры по литой таре (type=6)
                    var contractDS = ListDataSet.Create(result, "CONTRACTS");

                    var dict = new Dictionary<string, string>();
                    foreach (var item in contractDS.Items)
                    {
                        if (item.CheckGet("CONTRACT_TYPE").ToInt() == 6)
                        {
                            dict.Add(item["CONTRACT_ID"].ToInt().ToString(), item["CONTRACT_FULL_NAME"]);
                        }
                    }
                    Contract.Items = dict;
                    if (ContractId > 0)
                    {
                        Contract.SetSelectedItemByKey(ContractId.ToString());
                        ContractId = 0;
                    }
                    else
                    {
                        Contract.SetSelectedItemFirst();
                    }

                    SetSellerData();
                }
            }
        }

        /// <summary>
        /// Установка доступности полей и кнопок выбора дат
        /// </summary>
        private void SetDatePickersAvailable(bool exclude=false)
        {
            if (exclude)
            {
                SupplyDate.IsEnabled = false;
                ShipmentDate.IsEnabled = false;
            }
            else
            {
                bool selfDeliveryChecked = (bool)SelfDelivery.IsChecked;

                SupplyDate.IsEnabled = !selfDeliveryChecked;
                ShipmentDate.IsEnabled = selfDeliveryChecked;
            }
        }

        /// <summary>
        /// Вход в работу с заявкой на литую тару
        /// </summary>
        /// <param name="orderId"></param>
        public void Edit(int orderId = 0)
        {
            OrderId = orderId;
            GetData();
            Show();
        }
        
        /// <summary>
        /// Проверки перед записью данных в БД
        /// </summary>
        private void Save()
        {
            if (Form.Validate())
            {
                bool resume = true;
                var v = Form.GetValues();
                string errorMsg = "Не все поля заполнены верно";

                if (resume)
                {
                    // Проверяем корректность даты и времени доставки, если не стоит флаг самовывоз
                    if (!(bool)SelfDelivery.IsChecked)
                    {

                        if (SupplyDate.Text.IsNullOrEmpty())
                        {
                            errorMsg = "Неверно задано дата или время доставки";
                            resume = false;
                        }
                    }
                }

                if (resume)
                {
                    //Если пользователь ввёл минуты или секунды - обнуляем
                    string supplyDate = v.CheckGet("DT_SUPPLY");
                    if (!supplyDate.IsNullOrEmpty())
                    {
                        supplyDate = supplyDate.Substring(0, 13);
                        v["DT_SUPPLY"] = $"{supplyDate}:00:00";
                    }
                    v.CheckAdd("OLD_DATE", ShipmentOldDate.ToString("dd.MM.yyyy"));

                    SaveData(v);
                }
                else
                {
                    Form.SetStatus(errorMsg, 1);
                }
            }
        }

        /// <summary>
        /// Запись данных в БД
        /// </summary>
        /// <param name="p"></param>
        private async void SaveData(Dictionary<string, string> p)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "NewOrderLt");
            q.Request.SetParam("Object", "CreateOrder");
            q.Request.SetParam("Action", "Save");
            q.Request.SetParam("BUYER_ID", BuyerId.ToString());
            q.Request.SetParam("ORDER_ID", OrderId.ToString());
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestGridAttempts;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    if (result.ContainsKey("ITEMS"))
                    {
                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverName = "OrderGrid",
                            Action = "refresh_order_grid",
                            Message = $"{OrderId}"
                        });

                        Close();
                    }
                }
            }
            else if (q.Answer.Error.Code == 145)
            {
                Form.SetStatus(q.Answer.Error.Message, 1);
            }
        }

        private void Seller_SelectedItemChanged(System.Windows.DependencyObject d, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (Seller.SelectedItem.Key.ToInt() > 0)
            {
                SetSellerData();
            }
        }

        private void SelfDelivery_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if ((bool)SelfDelivery.IsChecked)
            {
                SupplyDate.Text = string.Empty;
            }
            else
            {
                ShipmentDate.Text = string.Empty;
            }
            
            SetDatePickersAvailable();
        }
    }
}