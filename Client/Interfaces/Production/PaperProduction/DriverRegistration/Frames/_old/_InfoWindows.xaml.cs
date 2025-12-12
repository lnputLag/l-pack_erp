using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Input;
using System.Linq;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Вывод информации водителю перед записью в базу
    /// </summary>
    /// <author>Грешных Н.И.</author>
    /// <version>1</version>
    public partial class InfoWindows : UserControl
    {
        public InfoWindows(Dictionary<string, string> v)
        {
            InitializeComponent();
            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            IdSt = v.CheckGet("IdSt").ToInt();
            ReceiverName = v.CheckGet("ReceiverName").ToString();
            Mode = v.CheckGet("Mode").ToInt();
            PhoneDriver = v.CheckGet("PhoneDriver").ToString();
            IdPost = v.CheckGet("IdPost").ToInt();
            PostavshicName = v.CheckGet("PostavshicName").ToString();
            MarkaCar = v.CheckGet("MarkaCar").ToString();
            NumberCar = v.CheckGet("NumberCar").ToString();

            DataStateList = new List<Dictionary<string, string>>();
        }

        private List<Dictionary<string, string>> DataStateList { get; set; }

        /// <summary>
        /// Id поставщика
        /// </summary>
        public int IdPost { get; set; }

        /// <summary>
        /// Название поставщика
        /// </summary>
        public string PostavshicName { get; set; }

        /// <summary>
        /// текущая плащадка 716- БДМ1, 1716-БДМ2
        /// </summary>
        public int IdSt { get; set; }

        /// <summary>
        /// Id машины
        /// </summary>
        public int IdScrap { get; set; }

        /// <summary>
        /// марка машины
        /// </summary>
        public string MarkaCar { get; set; }

        /// <summary>
        /// номер машины (только три цифры)
        /// </summary>
        public string NumberCar { get; set; }

        /// <summary>
        /// сотовый телефон водителя (только цифры)
        /// </summary>
        public string PhoneDriver { get; set; }

        /// <summary>
        /// присвоенный штрих-код при регистрации вод
        /// </summary>
        public string Barcode { get; set; }

        /// <summary>
        /// 0 - привез макулатуру, 1 - приехал за ПЭС
        /// </summary>
        public int Mode { get; set; }

        /// <summary>
        /// Форма
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// интервал автозакрытия формы, сек
        /// </summary>
        public int AutoCloseFormInterval { get; set; }

        /// <summary>
        /// таймер автозакрытия формы
        /// </summary>
        private DispatcherTimer AutoCloseFormTimer { get; set; }

        private DateTime LastClick { get; set; }

        /// <summary>
        /// Имя вкладки, куда происходит возврат при закрытии этой вкладки
        /// </summary>
        public string ReceiverName;

        /// <summary>
        /// Имя этой вкладки
        /// </summary>
        public string TabName;

        /// <summary>
        /// Инициализация формы
        /// </summary>
        public void InitForm()
        {
            Form = new FormHelper();

            AutoCloseFormInterval = 5 * 60;
            LastClick = DateTime.Now;
            ReturnInterface(true);
            FormCloseTimerRun();

            InfoText.Text = $"На номер телефона\n{PhoneDriver}\nпридет вызов с дальнейшими\nинструкциями.\nДослушайте до конца.";
            IdScrap = 0;
            Barcode = "";

        }

        public void Show()
        {
            string title = "Информация";
            TabName = "InfoWindows";
            Central.WM.AddTab(TabName, title, true, "add", this);
            InitForm();
            SetDefaults();
        }

        /// <summary>
        /// деструктор интерфейса
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "DriverRegistration",
                ReceiverName = "",
                SenderName = "InfoWindows",
                Action = "Closed",
            });
            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
            FormCloseTimerStop();

            Central.WM.SetActive(ReceiverName, true);
            ReceiverName = "";
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        private void ProcessMessages(ItemMessage obj)
        {
        }

        /// <summary>
        /// запус таймера автозакрытия формы
        /// </summary>
        private void FormCloseTimerRun()
        {

            if (AutoCloseFormInterval != 0)
            {
                if (AutoCloseFormTimer == null)
                {
                    AutoCloseFormTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0, 0, AutoCloseFormInterval)
                    };

                    AutoCloseFormTimer.Tick += (s, e) =>
                    {
                        ReturnInterface();
                    };
                }

                if (AutoCloseFormTimer.IsEnabled)
                {
                    AutoCloseFormTimer.Stop();
                }
                AutoCloseFormTimer.Start();
            }

        }

        //перезапуск таймера автозакрытия формы
        public void FormCloseTimerReset()
        {
            LastClick = DateTime.Now;
        }

        //останов таймера автозакрытия формы
        private void FormCloseTimerStop()
        {

            if (AutoCloseFormTimer != null)
            {
                if (AutoCloseFormTimer.IsEnabled)
                {
                    AutoCloseFormTimer.Stop();
                }
            }

        }

        /// <summary>
        /// при переключении интерфейса в другое состояние оно отображается
        /// определенное время, затем интерфейс возвращается в исходное состояние
        /// любой клик на элементе сбрабывает таймер возврата:
        /// </summary>
        private void ReturnInterface(bool force = false)
        {

            var today = DateTime.Now;
            var dt = ((TimeSpan)(today - LastClick)).TotalSeconds;

            if (
                dt > AutoCloseFormInterval
            //|| force
            )
            {
                Close();
            }

        }

        private void SetDefaults()
        {

        }

        /// <summary>
        /// нажали кнопку "Домой"
        /// </summary>
        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender, Home);
        }

        /// <summary>
        /// нажали кнопку "OK"
        /// </summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender, Save);
        }

        private void Home()
        {
            ReceiverName = "DriverList";
            Central.WM.RemoveTab($"InfoWindows");
            Central.WM.Close($"EditLastNumPhone");
            Central.WM.Close($"EditSms");
            Central.WM.Close($"EditNumCar");
            Central.WM.Close($"SelectCar");
            Central.WM.Close($"SelectPostavshic");
            Central.WM.Close($"EditPhone");
            Central.WM.Close($"SettingsMode");

            Close();
        }

        private void Close()
        {
            Destroy();
        }

        private void Save()
        {
            // Генерируем новый штрих код, который отличается от предыдущего на 7
            BarcodeGenerate();
            // сохраняем в таблицу scrap_transport
            SaveData();
            // печатаем ярлык
            if (IdScrap != 0)
            {
                var driverList = new DriverList();
                driverList.PrintLabel(2, IdScrap);
            }
            //устанавливаем начальную страницу
            Home();
        }

        /// <summary>
        /// Сохранение данных по зарегистрированной машине
        /// </summary>
        public  void SaveData()
        {
            var p = new Dictionary<string, string>();
            {

                if (Mode == 0)
                {
                    p.CheckAdd("ID_STATUS", "1");
                    p.CheckAdd("CONTAMINATION", "1");
                    //p.CheckAdd("ID_POST", IdPost.ToString());
                    p.CheckAdd("ID_CATEGORY", "2");
                }
                else
                {
                    p.CheckAdd("ID_STATUS", "11");
                    p.CheckAdd("CONTAMINATION", "15");
                    //p.CheckAdd("ID_POST", null);
                    p.CheckAdd("ID_CATEGORY", "41");
                }

                p.CheckAdd("NAME", (MarkaCar + " " + NumberCar).ToString());
                p.CheckAdd("BARCODE", Barcode.ToString());
                p.CheckAdd("PHONE_NUMBER", PhoneDriver.Substring(1, 10).ToString());
                p.CheckAdd("ID_ST", IdSt.ToString());
                p.CheckAdd("ID_POST", IdPost.ToString());

            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "DriverRegistration");
            q.Request.SetParam("Action", "DriverRegistrationSave");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestTimeoutGrid;
            q.Request.Attempts = Central.Parameters.RequestAttemptsGrid;
           
            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(q.Answer.Data);
                IdScrap = result.CheckGet("ID_SCRAP").ToInt();
            }
            else
            {
                q.ProcessError();
            }

        }

        /// <summary>
        /// Генерируем новый ШК для регистрации машины
        /// </summary>
        private void BarcodeGenerate()
        {
            Barcode = "";
            var barcodeOld = "";
            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID_ST", IdSt.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "ProductionPm");
                q.Request.SetParam("Object", "DriverRegistration");
                q.Request.SetParam("Action", "DriverRegistrationBarcode");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestTimeoutGrid;
                q.Request.Attempts = Central.Parameters.RequestAttemptsGrid;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");

                            if (ds.Items.Count > 0)
                            {
                                DataStateList = ds.Items;

                                if (DataStateList.Count > 0)
                                {
                                    var first = DataStateList.First();
                                    if (first != null)
                                    {
                                        barcodeOld = first.CheckGet("BARCODE").ToString();
                                    }
                                }
                            }
                            else
                                barcodeOld = "171600000000";
                        }
                    }

                    Barcode = barcodeOld.Substring(0, 4);
                    var nextVal = (barcodeOld.Substring(4, 8).ToInt() + 7).ToString();
                    Barcode = Barcode + nextVal.PadLeft(8, '0');
                }
                else
                {
                    q.ProcessError();
                }
            }





        }





    }
}
