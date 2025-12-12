using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;


namespace Client.Interfaces.Production
{
    /// <summary>
    /// подтверждение кода из смс для Cargo_type = 6
    /// </summary>
    /// <author>Грешных Н.И.</author>
    /// <version>1</version>
    public partial class ConfirmSms4 : WizardFrame
    {
        public ConfirmSms4()
        {
            InitializeComponent();

            if (Central.InDesignMode())
            {
                return;
            }

            SentCode = "";
            SentCodeDate = "";
            CheckTimerInterval = 1;
            CheckLostTime = false;

            InitForm();
            SetDefaults();
            Central.Msg.Register(ProcessMessage);
        }


        /// <summary>
        /// Видимость СМС
        /// </summary>
        private int SmsVisibleIs { get; set; }

        /// <summary>
        /// отправленный в СМС код
        /// заполняется после успешной отправки
        /// </summary>
        private string SentCode { get; set; }
        /// <summary>
        /// дата последней отправки кода
        /// </summary>
        private string SentCodeDate { get; set; }

        private bool CheckLostTime { get; set; }
        private List<Dictionary<string, string>> DataStateList { get; set; }

        /// <summary>
        /// количество отправленных смс водителю 
        /// </summary>
        private int CountSendSms { get; set; }

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
                    Path="PHONE3",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    First=true,
                },
                new FormHelperField()
                {
                    Path="_CODE_SMS",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CodeText,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    First=true,
                },
            };

            Form.SetFields(fields);
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            CountSendSms = 0;
            CheckLostTime = true;
            SentCodeDate = "";
            CheckTimerStop();

            //установка значений по умолчанию
            Form.SetDefaults();

            NextButtonSet(false);
        }

        /// <summary>
        /// </summary>
        private void SetPhoneNumber()
        {
            PhoneNumber.Text = $"";
            var n = Form.GetValueByPath("PHONE3");
            var c = "";

            //if(Central.DebugMode)
            //{
            //    c=SentCode;
            //}

            //var dataList = new List<Dictionary<string, string>>();
            //var row2 = new Dictionary<string, string>();
            //row2.CheckAdd("PARAM_NAME", "BDM_SMS_VISIBLE");
            //dataList.Add(row2);
            //GetData2(dataList);
            //if (SmsVisibleIs == 1)
            
            if (CountSendSms == 2)
            {
                c = SentCode;
                CodeText.Text = c;
                Validate();
            }

            if (!n.IsNullOrEmpty())
            {
                PhoneNumber.Text = $"На ваш телефон {n} {c}";
            }
        }

        /// <summary>
        /// обработка сообщений
        /// </summary>
        /// <param name="message"></param>
        public void ProcessMessage(ItemMessage message)
        {
            if (message != null)
            {
                if (message.ReceiverName == ControlName)
                {
                    switch (message.Action)
                    {
                        //фрейм загружен 
                        case "Showed":
                            var v = Wizard.Values;
                            v.CheckAdd("_CODE_SMS", "");
                            SetDefaults();
                            LoadValues();
                            SetPhoneNumber();
                            SendCodeSMS();
                            break;

                        //ввод с экранной клавиатуры
                        case "KeyPressed":
                            ChangeValue(message.Message);
                            break;

                        //фрейм закрылся
                        case "Closed":
                            CheckTimerStop();
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// ввод в поле из виртуальной клавиатуры
        /// </summary>
        /// <param name="symbol"></param>
        private void ChangeValue(string symbol)
        {
            if (IsActive() && !string.IsNullOrEmpty(symbol))
            {
                var s = Form.GetValueByPath("_CODE_SMS");
                switch (symbol)
                {
                    case "BACK_SPACE":
                        if (s.Length > 0)
                        {
                            s = s.Substring(0, (s.Length - 1));
                        }
                        break;

                    default:
                        if (s.Length < 3)
                        {
                            s = s + symbol;
                        }
                        break;
                }
                Form.SetValueByPath("_CODE_SMS", s);
            }

            Validate();
        }

        /// <summary>
        /// проверка, можно ли активировать кнопку "далее"
        /// </summary>
        private void Validate()
        {
            //CheckLostTime=false;
            var s = Form.GetValueByPath("_CODE_SMS");
            if (s == SentCode)
            {
                NextButtonSet(true);
                SaveValues();
            }
            else
            {
                NextButtonSet(false);
            }

            //CheckLostTime=true;
        }

        /// <summary>
        /// активация/деактивация кнопки "далее"
        /// </summary>
        /// <param name="mode"></param>
        private void NextButtonSet(bool mode = true)
        {
            if (NextButton != null)
            {
                if (mode)
                {
                    //CheckTimerStop();

                    NextButton.IsEnabled = true;
                    NextButton.Opacity = 1.0;
                    NextButton.Style = (Style)NextButton.TryFindResource("TouchFormButtonPrimaryBig");
                }
                else
                {
                    NextButton.IsEnabled = false;
                    NextButton.Opacity = 0.5;
                    NextButton.Style = (Style)NextButton.TryFindResource("TouchFormButtonBig");
                }
            }
        }

        private void SendButtonSet(bool mode = true)
        {
            if (NextButton != null)
            {
                if (mode)
                {
                    SendCodeButton.Visibility = Visibility.Visible;
                    SendCodeNote.Visibility = Visibility.Collapsed;
                }
                else
                {
                    SendCodeButton.Visibility = Visibility.Collapsed;
                    SendCodeNote.Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// проверка возможности повторной отправки
        /// </summary>
        private void CheckSendCodeSMS()
        {
            bool resume = true;
            bool doSend = false;

            if (resume)
            {
                if (SentCodeDate.IsNullOrEmpty())
                {
                    resume = false;
                    doSend = true;
                }
            }

            var dt = 0;
            if (resume)
            {
                if (!SentCodeDate.IsNullOrEmpty())
                {
                    var t0 = SentCodeDate.ToDateTime();
                    var t1 = DateTime.Now;
                    dt = ((TimeSpan)(t1 - t0)).TotalSeconds.ToInt();

                    if (dt > 180)
                    {
                        resume = false;
                        doSend = true;
                    }
                }
            }

            if (doSend)
            {
                SendButtonSet(true);
                CheckTimerStop();
            }
            else
            {
                SendButtonSet(false);

                var dd = 180 - dt;
                var time = TimeSpan.FromSeconds(dd);
                string s = time.ToString(@"mm\:ss");
                SendCodeNote.Text = $"Повторная отправка через {s}";
            }
        }

        /// <summary>
        /// таймер проверки
        /// </summary>
        private DispatcherTimer CheckTimer { get; set; }
        /// <summary>
        /// интервал проверки, сек
        /// </summary>
        private int CheckTimerInterval { get; set; }
        /// <summary>
        /// запусr таймера получения данных
        /// </summary>
        private void CheckTimerRun()
        {
            if (CheckTimerInterval != 0)
            {
                if (CheckTimer == null)
                {
                    CheckTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0, 0, CheckTimerInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", CheckTimerInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("ConfirmSms_CheckTimerRun", row);
                    }

                    CheckTimer.Tick += (s, e) =>
                    {
                        CheckSendCodeSMS();
                    };
                }

                if (CheckTimer.IsEnabled)
                {
                    CheckTimer.Stop();
                }
                CheckTimer.Start();
            }

            CheckSendCodeSMS();
        }

        /// <summary>
        /// останов таймера  
        /// </summary>
        private void CheckTimerStop()
        {
            if (CheckTimer != null)
            {
                if (CheckTimer.IsEnabled)
                {
                    CheckTimer.Stop();
                }
            }

            //CheckLostTime=false;
            //SendCodeButton.Visibility=Visibility.Collapsed;
            //SendCodeNote.Visibility=Visibility.Collapsed;
            //CallCodeButton.Visibility=Visibility.Collapsed;
        }

        /// <summary>
        /// отправка кода в смс
        /// </summary>
        private async void SendCodeSMS()
        {
            bool resume = true;

            var phoneNumber = "";
            var code = "";

            if (resume)
            {
                phoneNumber = Form.GetValueByPath("PHONE3");
                if (phoneNumber.IsNullOrEmpty())
                {
                    resume = false;
                }
                else
                {
                    // 89202446677
                    //  0987654321
                    //+79191646870
                    if (phoneNumber.Length == 12)
                    {
                        phoneNumber = phoneNumber.Substring(2, 10);
                    }
                }
            }

            if (resume)
            {
                code = Cryptor.MakeRandom(100, 999).ToString();
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("PHONE_NUMBER", phoneNumber.ToString());
                    p.CheckAdd("CODE", code.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "PaperProduction");
                q.Request.SetParam("Object", "TransportDriver");
                q.Request.SetParam("Action", "SendSms");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    CountSendSms++;
                    SentCode = code;
                    SentCodeDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                    CheckLostTime = true;
                    CheckTimerRun();
                    SetPhoneNumber();
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// нажали кнопку "Домой"
        /// </summary>
        private void HomeButtonClick(object sender, RoutedEventArgs e)
        {
            Wizard.Navigate(0);
        }

        /// <summary>
        /// нажали кнопку "Предыдущий"
        /// </summary>
        private void PriorButtonClick(object sender, RoutedEventArgs e)
        {
            Wizard.Navigate("PhoneNumber4");
        }

        /// <summary>
        /// нажали кнопку "Следующий"
        /// </summary>
        private void NextButtonClick(object sender, RoutedEventArgs e)
        {
            Wizard.Navigate("Info");
        }

        private void SendCodeButton_Click(object sender, RoutedEventArgs e)
        {
            //CheckSendCodeSMS();
            CountSendSms = 2;
            SetPhoneNumber();

        }

        /// <summary>
        /// запрос на получение данных из CONFIGURATION_OPTIONS
        /// </summary>
        private void GetData2(List<Dictionary<string, string>> list)
        {
            SmsVisibleIs = 0;

            var listString = JsonConvert.SerializeObject(list);

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("DATA_LIST", listString);
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "PMFire");
            q.Request.SetParam("Action", "GetData");
            q.Request.SetParams(p);

            q.Request.Timeout = 10000;
            q.Request.Attempts = 1;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
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
                                SmsVisibleIs = first.CheckGet("PARAM_VALUE").ToInt();
                            }
                        }
                    }
                }
            }

        }


    }
}
