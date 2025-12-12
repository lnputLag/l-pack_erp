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

namespace Client.Interfaces.Sales
{
    /// <summary>
    /// Окно редактирования заявки
    /// </summary>
    public partial class ResponsibleStockOrder : UserControl
    {
        public ResponsibleStockOrder()
        {
            FrameName = "ResponsibleStockOrder";

            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Init();
            SetDefaults();
        }

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
        /// Запрет редактирования примечания логисту для уже созданной заявки
        /// </summary>
        public bool ReadOnlyNoteLogistician { get; set; }

        /// <summary>
        /// naklrashodz.nsthet
        /// Идентификатор заявки на производство
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// online_store_order.onsr_id
        /// Идентификатор заказа интернет-магазина
        /// </summary>
        public int OnlineStoreOrderId { get; set; }

        /// <summary>
        /// Тип заявки:
        /// 1 -- Заявка для поставки на СОХ
        /// 2 -- Заявка для отгрузки заказа ИМ из Липецка
        /// </summary>
        public int OrderType { get; set; }

        /// <summary>
        /// Список позиций заказа интернет-магазина, по которому создаётся заявка на производство
        /// </summary>
        public List<Dictionary<string, string>> OnlineStoreOrderPositionList { get; set; }

        /// <summary>
        /// idpok
        /// Идентификатор покупателя
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// id_ts
        /// </summary>
        private int TransportId { get; set; }

        /// <summary>
        /// ID_PROD
        /// Идентификатор юридической сущности организации
        /// </summary>
        public int OrganizationId { get; set; }

        /// <summary>
        /// iddog
        /// </summary>
        public int ContractId { get; set; }

        /// <summary>
        /// Идентификатор кассы
        /// </summary>
        private int Idk { get; set; }

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
                    Path="DELIVERY_DATE",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = DeliveryDateTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="DELIVERY_TIME",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = DeliveryTimeTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="ORDER_NUMBER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=OrderNumberTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
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
                    Path="SELFSHIP",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=SelfShipCheckBox,
                    ControlType="CheckBox",
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
        public void SetDefaults()
        {
            Form.SetDefaults();

            // Логистика для вас
            CustomerId = 8498;

            OrganizationId = 1;
            Idk = 8764;

            Form.SetValueByPath("NOTE_STOCKMAN", "Грузить строго полный поддон");
            Form.SetValueByPath("NOTE_LOADER", "Грузить строго полный поддон");
            Form.SetValueByPath("NOTE_LOGISTICIAN", "Грузить строго полный поддон");
            Form.SetValueByPath("DELIVERY_DATE", DateTime.Now.ToString("dd.MM.yyyy"));
            DeliveryTimeTextBox.Text = "10:00";

            OnlineStoreOrderPositionList = new List<Dictionary<string, string>>();
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

            if (OrderId > 0)
            {
                FrameName = $"{FrameName}_{OrderId}";
                Central.WM.Show(FrameName, $"Заявка #{OrderId}", true, "add", this);
            }
            else
            {
                FrameName = $"{FrameName}_new";
                Central.WM.Show(FrameName, $"Новая заявка", true, "add", this);
            }
            
            if (OrderId > 0)
            {
                bool succesfulFlag = false;

                var p = new Dictionary<string, string>();
                p.Add("NSTHET", OrderId.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "ResponsibleStock");
                q.Request.SetParam("Action", "GetOrderById");
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

                            TransportId = firstDictionary.CheckGet("ID_TS").ToInt();
                            ContractId = firstDictionary.CheckGet("IDDOG").ToInt();
                            CustomerId = firstDictionary.CheckGet("ID_POK").ToInt();
                            OrganizationId = firstDictionary.CheckGet("ID_PROD").ToInt();

                            if (firstDictionary.CheckGet("TYPE_ORDER").ToInt() == 3)
                            {
                                OrderType = 1;

                                string date = firstDictionary.CheckGet("DT_SUPPLY").ToDateTime().ToString("dd.MM.yyyy");
                                Form.SetValueByPath("DELIVERY_DATE", date);

                                string time = firstDictionary.CheckGet("DT_SUPPLY").ToDateTime().ToString("HH:mm");
                                Form.SetValueByPath("DELIVERY_TIME", time);
                            }
                            else if (firstDictionary.CheckGet("TYPE_ORDER").ToInt() == 1)
                            {
                                OrderType = 2;

                                string date = firstDictionary.CheckGet("DATA").ToDateTime().ToString("dd.MM.yyyy");
                                Form.SetValueByPath("DELIVERY_DATE", date);

                                string time = firstDictionary.CheckGet("DATA").ToDateTime().ToString("HH:mm");
                                Form.SetValueByPath("DELIVERY_TIME", time);
                            }
                      
                            Form.SetValueByPath("ORDER_NUMBER", firstDictionary.CheckGet("NUMBER_ORDER"));
                            Form.SetValueByPath("NOTE_LOADER", firstDictionary.CheckGet("NOTE"));
                            Form.SetValueByPath("NOTE_LOGISTICIAN", firstDictionary.CheckGet("NOTE_LOGISTIC"));
                            Form.SetValueByPath("NOTE_STOCKMAN", firstDictionary.CheckGet("NOTE_GENERAL"));
                            Form.SetValueByPath("SELFSHIP", firstDictionary.CheckGet("SELFSHIP"));

                            if (ReadOnlyNoteLogistician)
                            {
                                NoteLogisticianTextBox.IsReadOnly = true;
                            }

                            if (TransportId > 0)
                            {
                                DeliveryDateTextBox.IsReadOnly = true;
                            }

                            succesfulFlag = true;
                        }
                    }

                    if (!succesfulFlag)
                    {
                        string msg = "Ошибка получения данных по существующей заявке. Пожалуйста, сообщите о проблеме";
                        var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }

                SelfShipCheckBox.IsEnabled = false;
            }
            else
            {
                if (OrderType == 2)
                {
                    Form.SetValueByPath("SELFSHIP", "1");
                    Form.SetValueByPath("DELIVERY_TIME", "08:00");

                    if (ContractId == 0)
                    {
                        bool succesfulFlag = false;

                        var p = new Dictionary<string, string>();
                        p.Add("ID_POK", CustomerId.ToString());
                        p.Add("ID_PROD", OrganizationId.ToString());
                        p.Add("TYPE", "5");

                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Sales");
                        q.Request.SetParam("Object", "ResponsibleStock");
                        q.Request.SetParam("Action", "GetContractId");
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
                                    ContractId = dataSet.Items.First().CheckGet("CONTRACT_ID").ToInt();
                                    succesfulFlag = true;
                                }
                            }

                            if (!succesfulFlag)
                            {
                                string msg = "Ошибка получения данных для заявки. Пожалуйста, сообщите о проблеме";
                                var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
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
                    bool succesfulFlag = false;

                    var p = new Dictionary<string, string>();
                    p.Add("ID_POK", CustomerId.ToString());
                    p.Add("ID_PROD", OrganizationId.ToString());
                    p.Add("TYPE", "1");

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sales");
                    q.Request.SetParam("Object", "ResponsibleStock");
                    q.Request.SetParam("Action", "GetContractId");
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
                                ContractId = dataSet.Items.First().CheckGet("CONTRACT_ID").ToInt();
                                succesfulFlag = true;
                            }
                        }

                        if (!succesfulFlag)
                        {
                            string msg = "Ошибка получения данных для заявки. Пожалуйста, сообщите о проблеме";
                            var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
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

        public void Save()
        {
            if (Form.Validate())
            {
                // Если редактирование существующей
                if (OrderId > 0)
                {
                    bool succesfulFlag = false;

                    var p = new Dictionary<string, string>();
                    p.Add("NSTHET", OrderId.ToString());
                    p.Add("ID_PROD", OrganizationId.ToString());
                    p.Add("ID_POK", CustomerId.ToString());
                    p.Add("IDK", Idk.ToString());
                    p.Add("IDDOG", ContractId.ToString());
                    p.Add("NUMBER_ORDER", OrderNumberTextBox.Text);
                    p.Add("NOTE", NoteLoaderTextBox.Text);
                    p.Add("NOTE_GENERAL", NoteStockmanTextBox.Text);
                    p.Add("NOTE_LOGISTIC", NoteLogisticianTextBox.Text);

                    p.Add("SELFSHIP", $"{SelfShipCheckBox.IsChecked.ToInt()}");

                    // Поставка на сох
                    if (OrderType == 1)
                    {
                        if (DeliveryTimeTextBox.Text == "00:00" || DeliveryTimeTextBox.Text == "__:__")
                        {
                            DeliveryTimeTextBox.Text = "10:00";
                        }

                        p.Add("DT_SUPPLY", $"{DeliveryDateTextBox.Text} {DeliveryTimeTextBox.Text}:00");
                        p.Add("TYPE_ORDER", "3");
                    }
                    // Отгрузка заказа ИМ из липецка
                    else if (OrderType == 2)
                    {
                        if (DeliveryTimeTextBox.Text == "00:00" || DeliveryTimeTextBox.Text == "__:__")
                        {
                            DeliveryTimeTextBox.Text = "08:00";
                        }

                        p.Add("DATA", $"{DeliveryDateTextBox.Text} {DeliveryTimeTextBox.Text}:00");
                        p.Add("TYPE_ORDER", "1");
                    }

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sales");
                    q.Request.SetParam("Object", "ResponsibleStock");
                    q.Request.SetParam("Action", "UpdateOrder");
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
                                int resultId = dataSet.Items.First().CheckGet("NSTHET").ToInt();

                                if (resultId > 0)
                                {
                                    succesfulFlag = true;
                                }
                            }
                        }

                        if (!succesfulFlag)
                        {
                            string msg = "Ошибка обновления заявки";
                            var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                        else
                        {
                            // отправляем сообщение гриду заявок обновиться
                            {
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "ResponsibleStock",
                                    ReceiverName = "ResponsibleStockList",
                                    SenderName = "ResponsibleStockOrder",
                                    Action = "Refresh",
                                    Message = "",
                                }
                                );
                            }

                            string msg = "Успешное обновление заявки";
                            var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                            d.ShowDialog();

                            Close();
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
                // Если создание новой
                else
                {
                    bool succesfulFlag = false;

                    var p = new Dictionary<string, string>();
                    p.Add("ID_PROD", OrganizationId.ToString());
                    p.Add("ID_POK", CustomerId.ToString());
                    p.Add("IDK", Idk.ToString());
                    p.Add("IDDOG", ContractId.ToString());
                    p.Add("NUMBER_ORDER", OrderNumberTextBox.Text);
                    p.Add("NOTE", NoteLoaderTextBox.Text);
                    p.Add("NOTE_GENERAL", NoteStockmanTextBox.Text);
                    p.Add("NOTE_LOGISTIC", NoteLogisticianTextBox.Text);
                    p.Add("SELFSHIP", $"{SelfShipCheckBox.IsChecked.ToInt()}");

                    // Поставка на сох
                    if (OrderType == 1)
                    {
                        if (DeliveryTimeTextBox.Text == "00:00" || DeliveryTimeTextBox.Text == "__:__")
                        {
                            DeliveryTimeTextBox.Text = "10:00";
                        }

                        p.Add("DT_SUPPLY", $"{DeliveryDateTextBox.Text} {DeliveryTimeTextBox.Text}:00");
                        p.Add("TYPE_ORDER", "3");
                    }
                    // Отгрузка заказа ИМ из липецка
                    else if(OrderType == 2)
                    {
                        if (DeliveryTimeTextBox.Text == "00:00" || DeliveryTimeTextBox.Text == "__:__")
                        {
                            DeliveryTimeTextBox.Text = "08:00";
                        }

                        p.Add("DATA", $"{DeliveryDateTextBox.Text} {DeliveryTimeTextBox.Text}:00");
                        p.Add("TYPE_ORDER", "1");
                    }

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sales");
                    q.Request.SetParam("Object", "ResponsibleStock");
                    q.Request.SetParam("Action", "SaveOrder");
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
                                OrderId = dataSet.Items.First().CheckGet("NSTHET").ToInt();
                                if (OrderId > 0)
                                {
                                    succesfulFlag = true;
                                }
                            }
                        }

                        if (!succesfulFlag)
                        {
                            string msg = "Ошибка создания заявки";
                            var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                        else
                        {
                            if (OrderType == 2)
                            {
                                SetOrderIdIntoOnlineStoreOrder();
                                SaveOrderPositionsByOnlineStoreOrder();
                            }

                            // отправляем сообщение гриду заявок обновиться
                            {
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "ResponsibleStock",
                                    ReceiverName = "ResponsibleStockList",
                                    SenderName = "ResponsibleStockOrder",
                                    Action = "Refresh",
                                    Message = "",
                                }
                                );
                            }

                            Close();
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
                var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        public void SetOrderIdIntoOnlineStoreOrder()
        {
            var p = new Dictionary<string, string>();

            p.Add("ID", OnlineStoreOrderId.ToString());
            p.Add("ORDER_ID", OrderId.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "OnlineStoreOrder");
            q.Request.SetParam("Action", "UpdateOrderId");
            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;
            q.DoQuery();

            if (q.Answer.Status != 0)
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Создаём позиции заявки на производство по позициям заказа интернет-магазина
        /// </summary>
        public void SaveOrderPositionsByOnlineStoreOrder()
        {
            if (OnlineStoreOrderPositionList != null && OnlineStoreOrderPositionList.Count > 0)
            {
                foreach (var onlineStoreOrderPosition in OnlineStoreOrderPositionList)
                {
                    var p = new Dictionary<string, string>();

                    p.Add("NSTHET", OrderId.ToString());
                    p.Add("QUANTITY", onlineStoreOrderPosition.CheckGet("QUNATITY"));
                    p.Add("ID2", onlineStoreOrderPosition.CheckGet("PRODUCT_ID"));
                    p.Add("IDK1", onlineStoreOrderPosition.CheckGet("PRODUCT_CATEGORY_ID"));
                    p.Add("PRICE_VAT_EXCLUDED", onlineStoreOrderPosition.CheckGet("PRICE"));
                    p.Add("NOTE_LOADER", "Точное количество изделий на поддон");
                    p.Add("NOTE_STOCKMAN", "Точное количество изделий на поддон");
                    p.Add("ID_TS", "");
                    p.Add("ID_ADDRESS", "1");
                    p.Add("ID_DSD", null);
                    p.Add("SELFSHIP", "1");

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sales");
                    q.Request.SetParam("Object", "ResponsibleStock");
                    q.Request.SetParam("Action", "SaveOrderPosition");
                    q.Request.SetParams(p);
                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;
                    q.DoQuery();

                    if (q.Answer.Status != 0)
                    {
                        q.ProcessError();
                    }
                }
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
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
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
                SenderName = "ResponsibleStockOrder",
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
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
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
            ShowHelp();
        }
    }
}
