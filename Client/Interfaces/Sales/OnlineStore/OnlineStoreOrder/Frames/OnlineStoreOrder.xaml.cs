using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Production;
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
    /// Логика взаимодействия для OnlineStoreOrder.xaml
    /// </summary>
    public partial class OnlineStoreOrder : UserControl
    {
        public OnlineStoreOrder()
        {
            FrameName = "OnlineStoreOrder";

            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Init();
            SetDefaults();

            if (Central.DebugMode)
            {
                BuyerIdTextBox.Visibility = Visibility.Visible;
                CheckAddMolizaButton.IsEnabled = true;
                CheckAddMolizaButton.Visibility = Visibility.Visible;
            }
            else
            {
                BuyerIdTextBox.Visibility = Visibility.Collapsed;
                CheckAddMolizaButton.IsEnabled = false;
                CheckAddMolizaButton.Visibility = Visibility.Collapsed;
            }
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
        /// Идентификатор интернет заказа
        /// ONLINE_STORE_ORDER.ONSR_ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Идентификатор продавца
        /// PRODAVEZ.ID_PROD
        /// (ООО "Торговый Дом Л-ПАК")
        /// </summary>
        public int SallerId { get; set; }

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
                    Path="ORDER_DATE",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = OrderDateTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="ORDER_NUMBER",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = OrderNumberTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="BUYER",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = BuyerTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="DELIVERY_ADDRESS",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = DeliveryAddressTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="PICKUP",
                    FieldType = FormHelperField.FieldTypeRef.Boolean,
                    Control = PickupCheckBox,
                    ControlType = "CheckBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="DELIVERY_PRICE",
                    FieldType = FormHelperField.FieldTypeRef.Double,
                    Control = DeliveryPriceTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.IsNotZero, null },
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
                    Path="SHIPPING_POINT",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = null,
                    ControlType = "void",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.IsNotZero, null },
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

            Form.SetValueByPath("ORDER_DATE", DateTime.Now.ToString("dd.MM.yyyy"));

            ShippingPoint1RadioButton.IsChecked = true;

            //ООО "Торговый Дом Л-ПАК"
            SallerId = 1;
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

            var dt = DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss");

            FrameName = $"{FrameName}_{dt}";

            if (Id > 0)
            {
                GetOrderById();
            }

            if (Id > 0)
            {
                Central.WM.Show(FrameName, $"Заказ #{Id}", true, "add", this);
            }
            else
            {
                Central.WM.Show(FrameName, $"Новый заказ", true, "add", this);
            }
        }

        public void GetOrderById()
        {
            bool succesfulFlag = false;

            var p = new Dictionary<string, string>();
            p.Add("ID", Id.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "OnlineStoreOrder");
            q.Request.SetParam("Action", "GetOrder");
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

                        OrderNumberTextBox.Text = firstDictionary.CheckGet("ORDER_NUM");
                        BuyerTextBox.Text = firstDictionary.CheckGet("BUYER_NAME");
                        DeliveryAddressTextBox.Text = firstDictionary.CheckGet("DELIVERY_ADDRESS");
                        DeliveryPriceTextBox.Text = firstDictionary.CheckGet("DELIVERY_PRICE").ToDouble().ToString();
                        PickupCheckBox.IsChecked = firstDictionary.CheckGet("PICKUP_FLAG").ToBool();

                        string date = firstDictionary.CheckGet("ORDER_DT").ToDateTime().ToString("dd.MM.yyyy");
                        Form.SetValueByPath("ORDER_DATE", date);

                        if (firstDictionary.CheckGet("SHIPPING_POINT").ToInt() == 1)
                        {
                            ShippingPoint1RadioButton.IsChecked = true;
                        }
                        else if (firstDictionary.CheckGet("SHIPPING_POINT").ToInt() == 2)
                        {
                            ShippingPoint2RadioButton.IsChecked = true;
                        }

                        succesfulFlag = true;
                    }
                }

                if (!succesfulFlag)
                {
                    string msg = "Ошибка получения данных по существующему заказу";
                    var d = new DialogWindow($"{msg}", "Заказы интернет-магазина", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        public void Save()
        {
            if(Form.Validate())
            {
                // Если есть ид записи, то обновляем существующую запись
                if (Id > 0)
                {

                }
                // Если нет, то создаём новую
                else
                {
                    bool succesfulFlag = false;

                    var p = new Dictionary<string, string>();
                    p.Add("ID_POK", BuyerIdTextBox.Text);
                    p.Add("CREATED_DTTM", DateTime.Now.ToString());
                    p.Add("DELIVERY_ADDRESS", DeliveryAddressTextBox.Text);
                    p.Add("DELIVERY_PRICE", DeliveryPriceTextBox.Text);
                    p.Add("IDDOG", ContractTextBox.Text);
                    p.Add("ID_PROD", SallerId.ToString());
                    p.Add("ORDER_DT", OrderDateTextBox.Text);
                    p.Add("ORDER_NUM", OrderNumberTextBox.Text);
                    p.Add("PICKUP_FLAG", PickupCheckBox.IsChecked.ToInt().ToString());
                    p.Add("SHIPPING_POINT", Form.GetValueByPath("SHIPPING_POINT"));

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sales");
                    q.Request.SetParam("Object", "OnlineStoreOrder");
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

                            if (dataSet != null && dataSet.Items.Count > 0)
                            {
                                int resultId = dataSet.Items.First().CheckGet("ID").ToInt();

                                if (resultId > 0)
                                {
                                    succesfulFlag = true;
                                }
                            }
                        }

                        if (!succesfulFlag)
                        {
                            string msg = "Ошибка создания заказа";
                            var d = new DialogWindow($"{msg}", "Заказы интернет-магазина", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                        else
                        {
                            // отправляем сообщение гриду заявок обновиться
                            {
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "OnlineStoreOrder",
                                    ReceiverName = "OnlineStoreOrderList",
                                    SenderName = "OnlineStoreOrder",
                                    Action = "Refresh",
                                    Message = "",
                                }
                                );
                            }

                            string msg = "Успешное создание заказа";
                            var d = new DialogWindow($"{msg}", "Заказы интернет-магазина", "", DialogWindowButtons.OK);
                            d.ShowDialog();

                            Close();
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
        /// Получаем договор по покупателю и продавцу.
        /// (iddog) по (id_pok и id_prod)
        /// </summary>
        public void CheckContract()
        {
            bool succesfulFlag = false;
            Form.SetValueByPath("CONTRACT", "");

            var p = new Dictionary<string, string>();
            p.Add("ID_POK", BuyerIdTextBox.Text);
            p.Add("ID_PROD", SallerId.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "OnlineStoreOrder");
            q.Request.SetParam("Action", "GetContract");
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
                    string msg = "Ошибка получения договора.";
                    var d = new DialogWindow($"{msg}", "Заказы интернет-магазина", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                q.ProcessError();
            }

            if (succesfulFlag)
            {
                GetContractButton.IsEnabled = false;
            }
            else
            {
                GetContractButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Выбор покупателя из списка покупателей
        /// </summary>
        public void SelectBuyer()
        {
            var i = new BuyerList("OnlineStoreOrder", "OnlineStoreOrder", FrameName);
            i.Show();
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.IndexOf("OnlineStoreOrder") > -1)
            {
                if (m.ReceiverName.IndexOf("OnlineStoreOrder") > -1)
                {
                    switch (m.Action)
                    {
                        case "SelectItem":
                            if (m.ContextObject != null)
                            {
                                var selectedBuyer = (Dictionary<string, string>)m.ContextObject;
                                BuyerTextBox.Text = $"{selectedBuyer.CheckGet("NAME")} ({selectedBuyer.CheckGet("ID")})";
                                BuyerIdTextBox.Text = selectedBuyer.CheckGet("ID");
                                DeliveryAddressTextBox.Text = selectedBuyer.CheckGet("BUYER_ADRES");
                                UpdatePickup();
                            }
                            break;
                    }
                }
            }
        }

        public void CheckAddMoliza()
        {
            string inn = "";
            bool succesfulFlag = false;

            var i = new ComplectationCMQuantity(inn);
            i.Show("ИНН");

            if (i.OkFlag)
            {
                inn = i.QtyString;
            }

            var p = new Dictionary<string, string>();
            p.Add("INN", inn);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "OnlineStoreOrder");
            q.Request.SetParam("Action", "CheckAddMoliza");
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
                        var molizaName = firstDictionary.CheckGet("NAME");

                        if (!string.IsNullOrEmpty(molizaName))
                        {
                            succesfulFlag = true;

                            string msg = "";
                            msg += $"Код: {firstDictionary.CheckGet("CODE")}{Environment.NewLine}";
                            msg += $"Группа: {firstDictionary.CheckGet("GROUP")}{Environment.NewLine}";
                            msg += $"Наименование: {firstDictionary.CheckGet("NAME")}{Environment.NewLine}";
                            msg += $"Полное наименование: {firstDictionary.CheckGet("FULL_NAME")}{Environment.NewLine}";
                            msg += $"Юр/Физ: {firstDictionary.CheckGet("ENTITY")}{Environment.NewLine}";
                            msg += $"ИНН: {firstDictionary.CheckGet("INN")}{Environment.NewLine}";
                            msg += $"УИД: {firstDictionary.CheckGet("UID")}{Environment.NewLine}";

                            var d = new DialogWindow($"{msg}", "Заказы интернет-магазина", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                }

                if (!succesfulFlag)
                {
                    string msg = $"Ошибка создания контрагента.";
                    var d = new DialogWindow($"{msg}", "Заказы интернет-магазина", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        public void UpdatePickup()
        {
            if ((bool)PickupCheckBox.IsChecked)
            {
                DeliveryPriceTextBox.IsReadOnly = true;
                DeliveryAddressTextBox.IsReadOnly = true;
                Form.RemoveFilter("DELIVERY_PRICE", FormHelperField.FieldFilterRef.Required);
                Form.RemoveFilter("DELIVERY_PRICE", FormHelperField.FieldFilterRef.IsNotZero);
                Form.RemoveFilter("DELIVERY_ADDRESS", FormHelperField.FieldFilterRef.Required);

                DeliveryAddressTextBox.Clear();
                DeliveryPriceTextBox.Clear();
            }
            else
            {
                DeliveryPriceTextBox.IsReadOnly = false;
                DeliveryAddressTextBox.IsReadOnly = false;
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "DELIVERY_PRICE"), FormHelperField.FieldFilterRef.Required);
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "DELIVERY_PRICE"), FormHelperField.FieldFilterRef.IsNotZero);
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "DELIVERY_ADDRESS"), FormHelperField.FieldFilterRef.Required);
            }
        }

        /// <summary>
        /// Выбор точки отгрузки
        /// </summary>
        public void ShippingPointChecked(int pointNumber)
        {
            if (pointNumber == 1)
            {
                if (ShippingPoint2RadioButton != null)
                {
                    ShippingPoint2RadioButton.IsChecked = false;
                    Form.SetValueByPath("SHIPPING_POINT", $"{pointNumber}");

                    if (PickupCheckBox != null)
                    {
                        PickupCheckBox.IsEnabled = true;
                    }
                }
            }
            else if (pointNumber == 2)
            {
                if (ShippingPoint1RadioButton != null)
                {
                    ShippingPoint1RadioButton.IsChecked = false;
                    Form.SetValueByPath("SHIPPING_POINT", $"{pointNumber}");

                    if (PickupCheckBox != null)
                    {
                        PickupCheckBox.IsChecked = true;
                        UpdatePickup();
                        PickupCheckBox.IsEnabled = false;
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
                SenderName = "OnlineStoreOrder",
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
            Central.ShowHelp("/doc/l-pack-erp-new/application/online_shop");
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

        private void SelectDeliveryAddressButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SelectBuyerButton_Click(object sender, RoutedEventArgs e)
        {
            SelectBuyer();
        }

        private void BuyerIdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckContract();
        }

        private void CheckAddMolizaButton_Click(object sender, RoutedEventArgs e)
        {
            CheckAddMoliza();
        }

        private void PickupCheckBox_Click(object sender, RoutedEventArgs e)
        {
            UpdatePickup();
        }

        private void GetContractButton_Click(object sender, RoutedEventArgs e)
        {
            CheckContract();
        }

        private void ShippingPoint1RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            ShippingPointChecked(1);
        }

        private void ShippingPoint2RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            ShippingPointChecked(2);
        }
    }
}
