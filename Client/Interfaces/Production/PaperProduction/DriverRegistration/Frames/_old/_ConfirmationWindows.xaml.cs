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
    /// Вывод информации водителю для проверки правильности введенных данных
    /// </summary>
    /// <author>Грешных Н.И.</author>
    /// <version>1</version>
    public partial class ConfirmationWindows : UserControl
    {
        public ConfirmationWindows(Dictionary<string, string> v)
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
        public int Mode { get; set; }

        /// <summary>
        /// код из полученной СМС (только цифры)
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

            AutoCloseFormInterval = 5 * 60;
            LastClick = DateTime.Now;
            ReturnInterface(true);
            FormCloseTimerRun();
            var s = $"Пожалуйста проверьте данные.\n";

            if (Mode == 0)
            {
                ConfirmationInfoText.Text = $"{s}" +
                $"Я привез макулатуру.\n" +
                $"Мой номер телефона \"{PhoneDriver}\"\n" +
                $"Марка машины \"{MarkaCar} {NumberCar}\"\n" +
                $"Поставщик\n\"{PostavshicName}\".";
            }
            else if (Mode == 1)
            {
                ConfirmationInfoText.Text = $"{s}" +
                $"Я приехал за полиэтиленовой смесью.\n" +
                $"Мой номер телефона \"{PhoneDriver}\"\n" +
                $"Марка машины \"{MarkaCar} {NumberCar}\"";
            }
        }

        public void Show()
        {
            string title = "Информация";
            TabName = "ConfirmationWindows";
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
                SenderName = "ConfirmationWindows",
                Action = "Closed",
            });
            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
            FormCloseTimerStop();

            Central.WM.SetActive("EditNumCar", true);
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
        /// нажали кнопку "Назад"
        /// </summary>
        private void PriorButton_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender, Close);
        }

        /// <summary>
        /// нажали кнопку "Далее"
        /// </summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender, Save);
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
            Central.WM.RemoveTab($"ConfirmationWindows");
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

            var i = new EditSms(p);
            i.Show();
        }
    }
}
