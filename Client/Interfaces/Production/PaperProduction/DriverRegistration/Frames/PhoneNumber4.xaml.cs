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
using NPOI.SS.Formula.Functions;
using DevExpress.Data.Camera;


namespace Client.Interfaces.Production
{
    /// <summary>
    /// Проверка и ввод телефона водителем по коду брони
    /// </summary>
    /// <author>Грешных Н.И.</author>
    /// <version>1</version>
    public partial class PhoneNumber4 : WizardFrame
    {
        public PhoneNumber4()
        {
            InitializeComponent();
            
            if(Central.InDesignMode()){
                return;
            }

            InitForm();
            SetDefaults();
            Central.Msg.Register(ProcessMessage);
        }

        /// <summary>
        /// Ид слота
        /// </summary>
        private string PhoneOld { get; set; }

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
                    Control=PhoneNumberText,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },          
                    First=true,
                },
                new FormHelperField()
                {
                    Path="ID_TS",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="RMBU_ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="WMTS_ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ID_D",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            Form.SetFields(fields);
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            //установка значений по умолчанию
            Form.SetDefaults();
            NextButtonSet(false);
        }

        /// <summary>
        /// специальная обработка
        /// </summary>
        private void SetPhoneNumber()
        {
            /*
                 если поле пустое, подставляется первая цифра "+7",
                 курсор ставится в конец поля
             */

            PhoneOld = Form.GetValueByPath("PHONE3");

            if (Form.GetValueByPath("PHONE3").IsNullOrEmpty())
            {
                Form.SetValueByPath("PHONE3", "+7");
                PhoneNumberText.CaretIndex=2;
            }
            else
            {
                Validate();
            }
        }

        /// <summary>
        /// обработка сообщений
        /// </summary>
        /// <param name="message"></param>
        public void ProcessMessage(ItemMessage message)
        {
            if(message!=null)
            {
                if(message.ReceiverName==ControlName)
                {
                    switch (message.Action)
                    {
                        //фрейм загружен 
                        case "Showed":
                            SetDefaults();
                            LoadValues();
                            SetPhoneNumber();
                            break;

                        //ввод с экранной клавиатуры
                        case "KeyPressed":
                            ChangeValue(message.Message);                        
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
            var r0=IsActive();
            if (IsActive() && !string.IsNullOrEmpty(symbol))
            {
                var s = Form.GetValueByPath("PHONE3");
                switch (symbol)
                {
                    case "BACK_SPACE":
                        if (s.Length > 2)
                        {
                            s = s.Substring(0, (s.Length - 1));
                        }
                        break;

                    default:
                        if (s.Length < 12)
                        {
                            s = s + symbol;
                        }
                        break;
                }
                Form.SetValueByPath("PHONE3", s);
            }

            Validate();
        }

        /// <summary>
        /// проверка, можно ли активировать кнопку "далее"
        /// </summary>
        private void Validate()
        {
                //если в поле "номер телефона" блок текста длиной 12 символов, можно продолжать
                var s = Form.GetValueByPath("PHONE3");
                if(s.Length == 12)
                {
                    NextButtonSet(true);
                    SaveValues();
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
            if(NextButton!=null)
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
            Wizard.Navigate("BookingCode");
        }

        /// <summary>
        /// нажали кнопку "Следующий"
        /// </summary>
        private void NextButtonClick(object sender, RoutedEventArgs e)
        {
            var v = Wizard.Values;
            var id_d = v.CheckGet("ID_D").ToInt().ToString();
            var id_ts = v.CheckGet("ID_TS").ToInt().ToString();
            var rmbu_id = v.CheckGet("RMBU_ID").ToInt().ToString();
            var phone_new = v.CheckGet("PHONE3").ToString();

            var vid = "";
            var id = "";

            // если введенный номер изменен, то обновляем телефон водителя на новый 
            if (PhoneOld != phone_new)
            {
                if (phone_new.Length > 11)
                {
                    phone_new = phone_new.Substring(1, 11);
                }

                // это самовывоз
                if (id_ts.ToInt() > 0)
                {
                    vid = "0";
                    id = id_d;
                }
                else // это доставка
                if (rmbu_id.ToInt() > 0)
                {
                    vid = "1";
                    id = rmbu_id;
                
                }

                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("PHONE", phone_new);
                    p.CheckAdd("VID", vid);
                    p.CheckAdd("ID", id);
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "PaperProduction");
                q.Request.SetParam("Object", "TransportDriver");
                q.Request.SetParam("Action", "SavePhoneDriver");
                q.Request.SetParams(p);

                q.Request.Timeout = 3000;
                q.Request.Attempts = 1;

                q.DoQuery();
                
                if (q.Answer.Status != 0)
                {
                    q.ProcessError();
                }
                else
                {
                    Wizard.Navigate("InformationOutput");
                }
            }
            else
                Wizard.Navigate("InformationOutput"); 
        }

    }
}
