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
    /// Ввод телефона водителя
    /// </summary>
    /// <author>Грешных Н.И.</author>
    /// <version>1</version>
    public partial class EditPhone0 : UserControl
    {
        public EditPhone0(Dictionary<string, string> v)
        {
            InitializeComponent();

            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            InitForm();
            SetDefaults();

            ReceiverName = v.CheckGet("ReceiverName").ToString();
            Mode = v.CheckGet("Mode").ToInt();
        }

        /// <summary>
        /// сотовый телефон водителя (только цифры)
        /// </summary>
        private string PhoneDriver { get; set; }

        /// <summary>
        /// 0 - привез макулатуру, 1 - приехал за ПЭС
        /// </summary>
        private int Mode { get; set; }

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
                    Path="PhoneEdit",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=PhoneEdit,
                    Default="8",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },                    
                },
            };

            Form.SetFields(fields);

            AutoCloseFormInterval = 5 * 60;
            LastClick = DateTime.Now;
            ReturnInterface(true);
            FormCloseTimerRun();
        }

        private void SetDefaults()
        {
            Form.SetDefaults();

            var s=Form.GetFieldByPath("PhoneEdit").ToString();
            if(s.Length > 0)
            {
                PhoneEdit.CaretIndex=s.Length;
            }

            NextButtonSet(false);
        }

        /// <summary>
        /// проверка, можно ли активировать кнопку "далее"
        /// </summary>
        private void NextButtonCheck()
        {
                //если в поле "номер телефона" блок текста длиной 10 символов, можно продолжать
                var s = Form.GetValueByPath("PhoneEdit");
                if(s.Length == 10)
                {
                    NextButtonSet(true);
                }
                else
                {
                    NextButtonSet(false);
                }
        }

        /// <summary>
        /// активация/деактивация кнопки "далее"
        /// </summary>
        /// <param name="mode"></param>
        private void NextButtonSet(bool mode=true)
        {
            if(mode)
            {
                NextButton.IsEnabled=true;
                NextButton.Opacity=1.0;
                NextButton.Style=(Style)NextButton.TryFindResource("TouchFormButtonPrimaryBig");
            }
            else
            {
                NextButton.IsEnabled=false;
                NextButton.Opacity=0.5;
                NextButton.Style=(Style)NextButton.TryFindResource("TouchFormButtonBig");
            }
        }

        /// <summary>
        /// Показ формы
        /// </summary>
        public void Show()
        {
            string title = "Ввод телефона";
            TabName = "EditPhone";
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
                SenderName = "EditPhone",
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
        /// обработчик сообщений
        /// </summary>
        private void ProcessMessages(ItemMessage obj)
        {
            if (obj.ReceiverName.IndexOf("CellPhone") > -1)
            {
                switch (obj.Action)
                {
                    case "KeyPressed":
                        ChangeValue(obj.Message);                        
                        break;
                }
            }

            ////Group 
            //if (obj.ReceiverGroup.IndexOf("DriverRegistration") > -1)
            //{
            //    if (obj.ReceiverName.IndexOf(TabName) > -1)
            //    {

            //        if (obj.Action == "PostavshicSelected")
            //        {
            //            if (obj.ContextObject != null)
            //            {
            //                var v = (Dictionary<string, string>)obj.ContextObject;
            //                //               SetSelectPostavsicButton(v);
            //            }
            //        }
            //    }
            //}

            //if (obj.Action == "Closed")
            //{

            //}
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
            )
            {
                Close();
            }

        }

        private void ChangeValue(string symbol)
        {
            if (!string.IsNullOrEmpty(symbol))
            {
                var s = Form.GetValueByPath("PhoneEdit");
                switch (symbol)
                {
                    case "BACK_SPACE":
                        if (s.Length > 1)
                        {
                            s = s.Substring(0, (s.Length - 1));
                        }
                        break;

                    default:
                        if (s.Length < 10)
                        {
                            s = s + symbol;
                        }
                        break;
                }
                Form.SetValueByPath("PhoneEdit", s);
            }

            NextButtonCheck();
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
        private void PriorButton_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender, Close);
            //Close();
        }

        /// <summary>
        /// нажали кнопку "Следующий"
        /// </summary>
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender, Save);
            //Save();
        }

        private void Home()
        {
            ReceiverName = "DriverList";
            Central.WM.Close($"SettingsMode");

            Close();
        }

        private void Close()
        {
            Central.WM.RemoveTab($"EditPhone");
            Destroy();
        }

        private void Save()
        {
            //привез макулатуру
            if (Mode == 0)
            {
                var p = new Dictionary<string, string>();

                /*
                p.CheckAdd("IdSt", Form.CheckGet("IdSt").ToString());
                p.CheckAdd("ReceiverName", TabName.ToString());
                p.CheckAdd("Mode", Form.CheckGet("Mode").ToString());
                p.CheckAdd("PhoneDriver", PhoneDriver.ToString());
                p.CheckAdd("IdPost", Form.CheckGet("IdPost").ToString());
                p.CheckAdd("PostavshicName", Form.CheckGet("PostavshicName").ToString());
                p.CheckAdd("MarkaCar", Form.CheckGet("MarkaCar").ToString());
                p.CheckAdd("NumberCar", Form.CheckGet("NumberCar").ToString());
                //выбор поставщика
                p.CheckAdd("TypeGrid", "0");
                */

                var i = new SelectPostavshic(p);
                i.Show();

            }
            else if (Mode == 1)
            {
                var p = new Dictionary<string, string>();

                // приехал за ПЭС  
                /*
                var p = new Dictionary<string, string>();
                p.CheckAdd("IdSt", Form.CheckGet("IdSt").ToString());
                p.CheckAdd("ReceiverName", TabName.ToString());
                p.CheckAdd("Mode", Form.CheckGet("Mode").ToString());
                p.CheckAdd("PhoneDriver", PhoneDriver.ToString());
                p.CheckAdd("IdPost", Form.CheckGet("IdPost").ToString());
                p.CheckAdd("PostavshicName", Form.CheckGet("PostavshicName").ToString());
                p.CheckAdd("MarkaCar", Form.CheckGet("MarkaCar").ToString());
                p.CheckAdd("NumberCar", Form.CheckGet("NumberCar").ToString());
                //выбор машины
                p.CheckAdd("TypeGrid", "1");
                */

                var i = new SelectPostavshic(p);
                i.Show();
            }
        }

        
    }
}
