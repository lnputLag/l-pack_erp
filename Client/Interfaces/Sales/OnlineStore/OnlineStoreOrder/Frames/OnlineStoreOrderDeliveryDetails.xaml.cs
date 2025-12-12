using Client.Common;
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
    /// Логика взаимодействия для OnlineStoreOrderDeliveryDetails.xaml
    /// </summary>
    public partial class OnlineStoreOrderDeliveryDetails : UserControl
    {
        public OnlineStoreOrderDeliveryDetails(int orderId, int pickupFlag, string parentFrame)
        {
            FrameName = "OnlineStoreOrderDeliveryDetails";
            OrderId = orderId;
            PickupFlag = pickupFlag;
            ParentFrame = parentFrame;

            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Init();
            SetDefaults();

            if (PickupFlag == 0)
            {
                CommentTextBox.IsReadOnly = false;
                PhoneTextBox.IsReadOnly = false;
            }
            else
            {
                CommentTextBox.IsReadOnly = true;
                PhoneTextBox.IsReadOnly = true;
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
        /// Идентификатор заказа интернет магазина
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Выбранная дата доставки
        /// </summary>
        public string SelectedDeliveryDate { get; set; }

        /// <summary>
        /// Имя фрейма, который вызвал этот фрейм.
        /// После закрытия этого фрейма будет вызван ParentFrame.
        /// </summary>
        public string ParentFrame { get; set; }

        /// <summary>
        /// Флаг самовывоза
        /// </summary>
        public int PickupFlag { get; set; }

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
                    Path="COMMENT",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = CommentTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="PHONE",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = PhoneTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="ORDER_ID",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = OrderIdTextBox,
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
                    },
                },
                new FormHelperField()
                {
                    Path="DELIVERY_ADDRESS",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = DeliveryAddressTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="WEIGHT",
                    FieldType = FormHelperField.FieldTypeRef.Double,
                    Control = WeightTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="PALLET_COUNT",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = PalletCountTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
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
            Form.SetValueByPath("DELIVERY_DATE", DateTime.Now.AddDays(1).ToString("dd.MM.yyyy"));
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
            Central.WM.FrameMode = 2;

            var dt = DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss");
            FrameName = $"{FrameName}_{dt}";

            LoadData();
            Central.WM.Show(FrameName, $"Доставка для заказа #{OrderId}", true, "add", this);
        }

        /// <summary>
        /// Получение данных для заполнения полей формы
        /// </summary>
        public async void LoadData()
        {
            DisableControls();

            var p = new Dictionary<string, string>();
            p.Add("ORDER_ID", OrderId.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "OnlineStoreOrder");
            q.Request.SetParam("Action", "GetDeliveryDetails");
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
                    if (dataSet != null && dataSet.Items.Count > 0)
                    {
                        var firstDictionary = dataSet.Items.First();

                        OrderIdTextBox.Text = firstDictionary.CheckGet("ORDER_ID").ToInt().ToString();
                        BuyerTextBox.Text = $"{firstDictionary.CheckGet("BUYER_NAME")} {firstDictionary.CheckGet("BUYER_INN")}";
                        DeliveryAddressTextBox.Text = firstDictionary.CheckGet("DELIVERY_ADDRESS");
                        PalletCountTextBox.Text = firstDictionary.CheckGet("PALLET_COUNT").ToInt().ToString();
                        WeightTextBox.Text = Math.Round(firstDictionary.CheckGet("WEIGHT_GROSS").ToDouble(), 2).ToString(); //(брутто)
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            EnableControls();
        }

        public async void Save()
        {
            DisableControls();

            if (Form.Validate())
            {
                var p = Form.GetValues();

                if (PickupFlag == 0)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sales");
                    q.Request.SetParam("Object", "OnlineStoreOrder");
                    q.Request.SetParam("Action", "CreateDeliveryExcelReport");
                    q.Request.SetParams(p);

                    q.Request.Timeout = 25000;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    if (q.Answer.Status == 0)
                    {
                        Central.SaveFile(q.Answer.DownloadFilePath, true);

                        SelectedDeliveryDate = p.CheckGet("DELIVERY_DATE");
                        Close();
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
                else
                {
                    SelectedDeliveryDate = p.CheckGet("DELIVERY_DATE");
                    Close();
                }
            }

            EnableControls();
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            
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
            Central.WM.SetActive(ParentFrame, true);
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
                SenderName = "OnlineStoreOrderDeliveryDetails",
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
    }
}
