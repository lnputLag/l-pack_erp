using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Client.Interfaces.Orders.MoldedContainer
{
    /// <summary>
    /// Форма редактирования заявки на литую тару
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class MoldedContainerOrder : ControlBase
    {
        public MoldedContainerOrder()
        {
            DocumentationUrl = "/doc/l-pack-erp/orders/molded_container_order";
            InitializeComponent();

            InitForm();

            Commander.SetCurrentGroup("main");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "save",
                    Group = "main_form",
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
                    Group = "main_form",
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
                    Group = "main_form",
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

        /// <summary>
        /// Имя вкладки, окуда вызвана форма редактирования
        /// </summary>
        public string ReceiverName;
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
                    Path="ID_POK2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Consignee,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
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
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Orders");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "GetOrder");
            q.Request.SetParam("ORDER_ID", OrderId.ToString());
            q.Request.SetParam("BUYER_ID", BuyerId.ToString());

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
                    // Грузополучатель
                    var consigneeDS = ListDataSet.Create(result, "CONSIGNEE");
                    Consignee.Items = consigneeDS.GetItemsList("ID", "NAME");
                    if (consigneeDS.Items.Count > 0)
                    {
                        var first = consigneeDS.Items[0];
                        Consignee.SetSelectedItemByKey(first["ID"]);
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
                    Show();
                }
            }
        }

        /// <summary>
        /// Получение связанных справочников для выбранного продавца
        /// </summary>
        private async void SetSellerData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Orders");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "GetSellerData");
            q.Request.SetParam("SELLER_ID", Seller.SelectedItem.Key);
            q.Request.SetParam("BUYER_ID", BuyerId.ToString());

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
            ControlName = $"Заявка_{OrderId}";
            GetData();
        }

        /// <summary>
        /// Показывает вкладку
        /// </summary>
        public void Show()
        {
            Central.WM.Show(ControlName, ControlName, true, "add", this);
        }

        /// <summary>
        /// Закрытие вкладки с формой
        /// </summary>
        public void Close()
        {
            Central.WM.Close(ControlName);
            Central.WM.SetActive(ReceiverName);
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
            q.Request.SetParam("Module", "Orders");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "SaveOrder");
            q.Request.SetParam("BUYER_ID", BuyerId.ToString());
            q.Request.SetParam("ORDER_ID", OrderId.ToString());
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
                    if (result.ContainsKey("ITEMS"))
                    {
                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverGroup = "Orders",
                            ReceiverName = ReceiverName,
                            SenderName = ControlName,
                            Action = "RefreshOrders",
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
