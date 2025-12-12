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
    /// Выбор вида регистрации машины
    /// </summary>
    /// <author>Грешных Н.И.</author>
    /// <version>1</version>
    public partial class SettingsMode : UserControl
    {
        public SettingsMode(Dictionary<string, string> v)
        {
            InitializeComponent();
            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            ParametrsForm = new Dictionary<string, string>(v);
            ReceiverName = v.CheckGet("ReceiverName").ToString();
        }
                
        private Dictionary<string, string> ParametrsForm { get; set; }

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
        /// 0 - привез макулатуру, 1 - приехал за ПЭС
        /// </summary>
        private int Mode { get; set; }

        /// <summary>
        /// текущая плащадка 716- БДМ1, 1716-БДМ2
        /// </summary>
        private int IdSt { get; set; }


        /// <summary>
        /// Показ формы
        /// </summary>
        public void Show()
        {
            InitForm();
            SetDefaults();

            string title = "Выбор вида регистрации";
            TabName = "SettingsMode";
            Central.WM.AddTab(TabName, title, true, "add", this);
        }

        /// <summary>
        /// Инициализация формы
        /// </summary>
        public void InitForm()
        {
            Form = new FormHelper();

            if (IdSt == 716)
                PolyetilenButton.IsEnabled = false;
            else
                PolyetilenButton.IsEnabled = true;

            LastClick = DateTime.Now;
            AutoCloseFormInterval = 5 * 60;
            ReturnInterface(true);
            FormCloseTimerRun();

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
                ReceiverName = "DriverList",
                SenderName = "SettingsMode",
                Action = "Closed",
            });

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
        /// при переключении интерфейса в другое сосотояение оно отображается
        /// определенное время, затем интерфейс возвращается в исходное состояние
        /// любой клик на элементе сбрабывает таймер возврата:
        /// </summary>
        private void ReturnInterface(bool force = false)
        {

            var today = DateTime.Now;
            var dt = ((TimeSpan)(today - LastClick)).TotalSeconds;

            if (
                dt > AutoCloseFormInterval
                || force
            )
            {
                Close();
            }
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        private void ProcessMessages(ItemMessage obj)
        {

        }

        private void SetDefaults()
        {

        }

        /// <summary>
        /// нажали кнопку "Предыдущий"
        /// </summary>
        private void PriorButton_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender, Close);
        }

        /// <summary>
        /// нажали кнопку привез макулатуру
        /// </summary>
        private void ScrapButton_Click(object sender, RoutedEventArgs e)
        {
            Mode = 0;
            Helper.ButtonClickAnimation(sender, NextWindows);
        }

        /// <summary>
        /// нажали кнопку приехал за ПЭС
        /// </summary>
        private void PolyetilenButton_Click(object sender, RoutedEventArgs e)
        {
            Mode = 1;
            Helper.ButtonClickAnimation(sender, NextWindows);
        }

        /// <summary>
        /// нажали кнопку "Следующий"
        /// </summary>
        private void NextWindows()
        {
            var p = new Dictionary<string, string>();

            p.CheckAdd("IdSt", ParametrsForm.CheckGet("IdSt").ToString());
            p.CheckAdd("ReceiverName", TabName.ToString());
            p.CheckAdd("Mode", Mode.ToString());
            p.CheckAdd("PhoneDriver", ParametrsForm.CheckGet("PhoneDriver").ToString());
            p.CheckAdd("IdPost", ParametrsForm.CheckGet("IdPost").ToString());
            p.CheckAdd("PostavshicName", ParametrsForm.CheckGet("PostavshicName").ToString());
            p.CheckAdd("MarkaCar", ParametrsForm.CheckGet("MarkaCar").ToString());
            p.CheckAdd("NumberCar", ParametrsForm.CheckGet("NumberCar").ToString());

            //var i = new EditPhone(p);
            //i.Show();
        }

        public void Close()
        {
            Central.WM.Close($"SettingsMode");
        }

    }
}
