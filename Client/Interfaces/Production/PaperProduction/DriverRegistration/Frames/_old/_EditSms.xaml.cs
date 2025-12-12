using Client.Assets.HighLiters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;


namespace Client.Interfaces.Production
{
    /// <summary>
    /// Ввод кода из СМС
    /// </summary>
    /// <author>Грешных Н.И.</author>
    /// <version>1</version>
    public partial class EditSms : UserControl
    {
        public EditSms(Dictionary<string, string> v)
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

            LastDttm = DateTime.Now;
            LastPhoneNumber = "";
            LastSmsCod = "";

        }

        /// <summary>
        /// Количество оставшихся секунд до отправки новой СМС
        /// </summary>
        public int CountSec { get; set; }

        /// <summary>
        /// интервал автообновления времени, сек
        /// </summary>
        public int AutoUpdateTimeInterval { get; set; }

        /// <summary>
        /// таймер времени повторной отправки смс водителю
        /// </summary>
        private DispatcherTimer TimeCountTimer { get; set; }

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
        /// сотовый телефон водителя (только цифры)
        /// </summary>
        private string LastPhoneNumber { get; set; }

        /// <summary>
        /// время последней отправки СМС водителю
        /// </summary>
        private DateTime LastDttm { get; set; }

        /// <summary>
        /// 0 - привез макулатуру, 1 - приехал за ПЭС
        /// </summary>
        private int Mode { get; set; }

        /// <summary>
        /// код из полученной СМС (только цифры)
        /// </summary>
        public string SmsCod { get; set; }

        /// <summary>
        /// последний отправленный код СМС водителю
        /// </summary>
        private string LastSmsCod { get; set; }

        /// <summary>
        /// признак нужно ли отправлять смс с подтверждением водителю
        /// </summary>
        public int SendSmsFlag { get; set; }

        private List<Dictionary<string, string>> DataStateList { get; set; }

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

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="SmsEdit",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=SmsEdit,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },

                },
            };

            Form.SetFields(fields);
            CountSec = 60;
            AutoUpdateTimeInterval = 1;
            AutoCloseFormInterval = 5 * 60;
            LastClick = DateTime.Now;
            ReturnInterface(true);
            FormCloseTimerRun();
            TextBlockCountSec.Text = $"Отправить повторно СМС";
            TimerCountRun();
            SendSmsButton.IsEnabled = false;
            NextButton.Visibility = Visibility.Hidden;

        }

        public void Show()
        {
            string title = "Ввод кода СМС";
            TabName = "EditSms";
            Central.WM.AddTab(TabName, title, true, "add", this);
            InitForm();
            SetDefaults();
            //проверяем необходимость отправки СМС
            GetFlagCheck();
            if (SendSmsFlag == 1)
            {
                SendSms();
            }
            else
            {
                // вызываем сразу окно подтверждения
                Save();
            }
        }

        /// <summary>
        /// деструктор интерфейса
        /// </summary>
        public void Destroy()
        {
            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
            FormCloseTimerStop();
            //останавливаем таймеры времени
            TimerCountStop();

            //устанавливаем активное окно
            if (!string.IsNullOrEmpty(ReceiverName))
            {
                Central.WM.SetActive(ReceiverName, true);
                ReceiverName = "";
            }
        }

        /// <summary>
        /// запус таймера анимации времени
        /// </summary>
        private void TimerCountRun()
        {

            if (AutoUpdateTimeInterval != 0)
            {
                if (TimeCountTimer == null)
                {
                    TimeCountTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0, 0, AutoUpdateTimeInterval)
                    };

                    TimeCountTimer.Tick += (s, e) =>
                    {
                        CountSec--;
                        TextBlockCountSec.Text = $"Через {CountSec} сек. можно будет повторно отправить СМС";
                        if (CountSec == 0)
                        {
                            TimeCountTimer.Stop();
                            TextBlockCountSec.Text = $"Отправить повторно СМС";
                            SendSmsButton.IsEnabled = true;
                        }

                    };
                }

                if (TimeCountTimer.IsEnabled)
                {
                    TimeCountTimer.Stop();
                }
                TimeCountTimer.Start();
            }

        }

        //останов таймера анимации времени
        private void TimerCountStop()
        {

            if (TimeCountTimer != null)
            {
                if (TimeCountTimer.IsEnabled)
                {
                    TimeCountTimer.Stop();
                }
            }

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

        private void KeyboardButton0_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("0");
        }

        private void KeyboardButton1_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("1");
        }

        private void KeyboardButton2_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("2");
        }

        private void KeyboardButton3_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("3");
        }

        private void KeyboardButton4_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("4");
        }

        private void KeyboardButton5_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("5");
        }

        private void KeyboardButton6_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("6");
        }

        private void KeyboardButton7_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("7");
        }

        private void KeyboardButton8_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("8");
        }

        private void KeyboardButton9_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("9");
        }

        private void KeyboardButtonBackspace_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("<");
        }

        private void ChangeValue(string symbol)
        {
            if (!string.IsNullOrEmpty(symbol))
            {
                var s = Form.GetValueByPath("SmsEdit");

                switch (symbol)
                {
                    case "<":
                        if (!string.IsNullOrEmpty(s))
                        {
                            s = s.Substring(0, (s.Length - 1));
                        }
                        break;

                    default:
                        if (s.Length <= 2)
                        {
                            s = s + symbol;
                        }

                        break;
                }

                if (s.Length <= 2)
                {
                    NextButton.Visibility = Visibility.Hidden;
                    KeyboardButton0.IsEnabled = true;
                    KeyboardButton1.IsEnabled = true;
                    KeyboardButton2.IsEnabled = true;
                    KeyboardButton3.IsEnabled = true;
                    KeyboardButton4.IsEnabled = true;
                    KeyboardButton5.IsEnabled = true;
                    KeyboardButton6.IsEnabled = true;
                    KeyboardButton7.IsEnabled = true;
                    KeyboardButton8.IsEnabled = true;
                    KeyboardButton9.IsEnabled = true;
                    NumberCar = "";
                }
                else
                {
                    NextButton.Visibility = Visibility.Visible;
                    KeyboardButton0.IsEnabled = false;
                    KeyboardButton1.IsEnabled = false;
                    KeyboardButton2.IsEnabled = false;
                    KeyboardButton3.IsEnabled = false;
                    KeyboardButton4.IsEnabled = false;
                    KeyboardButton5.IsEnabled = false;
                    KeyboardButton6.IsEnabled = false;
                    KeyboardButton7.IsEnabled = false;
                    KeyboardButton8.IsEnabled = false;
                    KeyboardButton9.IsEnabled = false;
                    NumberCar = s;
                }

                Form.SetValueByPath("SmsEdit", s);

            }

            FormCloseTimerReset();
        }

        /// <summary>
        /// нажали кнопку "Домой"
        /// </summary>
        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender, Home);
        }

        /// <summary>
        /// нажали кнопку "Предыдущий"
        /// </summary>
        private void PriorButton_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender, Close);
        }

        /// <summary>
        /// нажали кнопку "Следующий"
        /// </summary>
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender, CheckCodSms);
        }

        private void Home()
        {
            ReceiverName = "DriverList";
            Central.WM.Close($"EditNumCar");
            Central.WM.Close($"SelectCar");
            Central.WM.Close($"SelectPostavshic");
            Central.WM.Close($"EditPhone");
            Central.WM.Close($"SettingsMode");

            Close();
        }

        private void Close()
        {
            Central.WM.RemoveTab($"EditSms");
            Destroy();
        }

        /// <summary>
        /// проверка кода из СМС с введенным значением
        /// </summary>
        private void CheckCodSms()
        {
            var s = Form.GetValueByPath("SmsEdit");

            if (SmsCod == s)
            {
                Save();
            }
            else
            {
                NextButton.Visibility = Visibility.Hidden;
                var t = "Внимание";
                var m = "Неправильный код из смс. Введите снова";
                var i = new ErrorTouch();
                i.Show(t, m);
            }
        }

        private void Save()
        {
            var p = new Dictionary<string, string>();
            p.CheckAdd("IdSt", IdSt.ToString());
            p.CheckAdd("ReceiverName", TabName.ToString());
            p.CheckAdd("Mode", Mode.ToString());
            p.CheckAdd("PhoneDriver", PhoneDriver.ToString());
            p.CheckAdd("IdPost", IdPost.ToString());
            p.CheckAdd("PostavshicName", PostavshicName.ToString());
            p.CheckAdd("MarkaCar", MarkaCar.ToString());
            p.CheckAdd("NumberCar", NumberCar.ToString());

            var i = new InfoWindows(p);
            i.Show();
        }

        /// <summary>
        /// нажали кнопку "Повторная смс"
        /// </summary>
        private void SendSmsButton_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender, SendSms);
            SendSmsButton.IsEnabled = false;
            CountSec = 60;
            TimerCountRun();
        }

        /// <summary>
        /// отправляем смс с проверочным кодом на телефон
        /// </summary>
        private async void SendSms()
        {
            bool resume = true;
                   
            var rez = DateTime.Now - LastDttm;
            
            // Если один и тот же водитель отправляет себе смс несколько раз, отправляем один и тот же код
            if ((rez.Minutes < 10)
            && (LastPhoneNumber == PhoneDriver))
            {
                SmsCod = LastSmsCod;
            }
            else
            {
                // Создание объекта для генерации чисел
                Random rnd = new Random();

                //Получаем случайное число (в диапазоне от 0 до 900)
                int value = rnd.Next(0, 900);
                // 3 - х значный код
                SmsCod = (100 + value).ToString();
                LastDttm = DateTime.Now;
                LastSmsCod = SmsCod;
                LastPhoneNumber = PhoneDriver;
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("PHONE_NUMBER", PhoneDriver.Substring(1, 10).ToString());
                    p.CheckAdd("CODE", SmsCod.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "ProductionPm");
                q.Request.SetParam("Object", "DriverRegistration");
                q.Request.SetParam("Action", "DriverRegistrationSendSms");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestTimeoutGrid;
                q.Request.Attempts = Central.Parameters.RequestAttemptsGrid;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                }
                else
                {
                    q.ProcessError();
                }
            }


        }

        private void EditNumPhoneButton_Click(object sender, RoutedEventArgs e)
        {
            FormCloseTimerStop();
            Helper.ButtonClickAnimation(sender, EditNumPhone);
        }

        /// <summary>
        /// вызываем форму ввода 4-х последних цифр телефона
        /// </summary>
        private void EditNumPhone()
        {
            var p = new Dictionary<string, string>();
            p.CheckAdd("IdSt", IdSt.ToString());
            p.CheckAdd("ReceiverName", TabName.ToString());
            p.CheckAdd("Mode", Mode.ToString());
            p.CheckAdd("PhoneDriver", PhoneDriver.ToString());
            p.CheckAdd("IdPost", IdPost.ToString());
            p.CheckAdd("PostavshicName", PostavshicName.ToString());
            p.CheckAdd("MarkaCar", MarkaCar.ToString());
            p.CheckAdd("NumberCar", NumberCar.ToString());

            var i = new EditLastNumPhone(p);
            i.Show();
        }

        /// <summary>
        /// проверяем , нужно ли отправлять смс с подтверждением
        /// </summary>
        private void GetFlagCheck()
        {
            bool resume = true;
            SendSmsFlag = 0;

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("PARAM_NAME", "REGISTRATION_DRIVER");

                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "ProductionPm");
                q.Request.SetParam("Object", "DriverRegistration");
                q.Request.SetParam("Action", "GetData");
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
                                        SendSmsFlag = first.CheckGet("PARAM_VALUE").ToInt();
                                    }
                                }
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
