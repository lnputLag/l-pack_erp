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


namespace Client.Interfaces.Production
{
    /// <summary>
    /// Ввод последних 4-х цифр телефона
    /// </summary>
    /// <author>Грешных Н.И.</author>
    /// <version>1</version>
    public partial class EditLastNumPhone : UserControl
    {
        public EditLastNumPhone(Dictionary<string, string> v)
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
            LastNumPhone = PhoneDriver.Substring(7, 4);
        }

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
        /// 0 - привез макулатуру, 1 - приехал за ПЭС
        /// </summary>
        private int Mode { get; set; }

        /// <summary>
        /// последние 4-х цифры телефона
        /// </summary>
        public string LastNumPhone { get; set; }

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
                    Path="LastNumPhoneEdit",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=LastNumPhoneEdit,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },

                },
            };

            Form.SetFields(fields);

            AutoCloseFormInterval = 5 * 60;
            LastClick = DateTime.Now;
            ReturnInterface(true);
            FormCloseTimerRun();
            NextButton.Visibility = Visibility.Hidden;

        }

        public void Show()
        {
            string title = "Ввод цифр телефона";
            TabName = "EditLastNumPhone";
            Central.WM.AddTab(TabName, title, true, "add", this);
            InitForm();
            SetDefaults();
        }

        /// <summary>
        /// деструктор интерфейса
        /// </summary>
        public void Destroy()
        {
            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
            FormCloseTimerStop();

            //устанавливаем активное окно
            if (!string.IsNullOrEmpty(ReceiverName))
            {
                Central.WM.SetActive(ReceiverName, true);
                ReceiverName = "";
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
                var s = Form.GetValueByPath("LastNumPhoneEdit");

                switch (symbol)
                {
                    case "<":
                        if (!string.IsNullOrEmpty(s))
                        {
                            s = s.Substring(0, (s.Length - 1));
                        }
                        break;

                    default:
                        if (s.Length <= 3)
                        {
                            s = s + symbol;
                        }

                        break;
                }

                if (s.Length <= 3)
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
                }

                Form.SetValueByPath("LastNumPhoneEdit", s);

            }

            FormCloseTimerReset();
        }

        /// <summary>
        /// нажали кнопку "Домой"
        /// </summary>
        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender, Home);
            //Home();
        }

        /// <summary>
        /// нажали кнопку "Предыдущий"
        /// </summary>
        /*
        private void PriorButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        */
        /// <summary>
        /// нажали кнопку "Следующий"
        /// </summary>
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender, CheckLastNumPhone);

        }

        private void Home()
        {
            ReceiverName = "DriverList";
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
            Central.WM.RemoveTab($"EditLastNumPhone");
            Destroy();
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
        /// проверка последних 4-х цифр телефона с введенным значением
        /// </summary>
        private void CheckLastNumPhone()
        {
            var s = Form.GetValueByPath("LastNumPhoneEdit");

            if (LastNumPhone == s)
            {
                Save();
            }
            else
            {
                NextButton.Visibility = Visibility.Hidden;
                var t = "Внимание";
                var m = "Неправильно введены ЧЕТЫРЕ последние цифры телефона. Введите снова";
                var i = new ErrorTouch();
                i.Show(t, m);
            }
        }
    }
}
