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
    /// подтверждение кода через телефонный звонок
    /// </summary>
    /// <author>Грешных Н.И.</author>
    /// <version>1</version>
    public partial class ConfirmCall : WizardFrame
    {
        public ConfirmCall()
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
        /// отправленный в СМС код
        /// заполняется после успешной отправки
        /// </summary>
        private string SentCode {get;set;}
        /// <summary>
        /// дата последней отпарвки кода
        /// </summary>
        private string SentCodeDate {get;set;}

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
                    Path="PHONE_NUMBER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },          
                    First=true,
                },
                new FormHelperField()
                {
                    Path="_CODE_CALL",
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
            
            
            //установка значений по умолчанию
            Form.SetDefaults();

            NextButtonSet(false);
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
                            var v = Wizard.Values;
                            v.CheckAdd("_CODE_CALL","");
                            Check();
                            SetDefaults();
                            LoadValues();
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
            if (IsActive() && !string.IsNullOrEmpty(symbol))
            {
                var s = Form.GetValueByPath("_CODE_CALL");
                switch (symbol)
                {
                    case "BACK_SPACE":
                        if (s.Length > 0)
                        {
                            s = s.Substring(0, (s.Length - 1));
                        }
                        break;

                    default:
                        if (s.Length < 4)
                        {
                            s = s + symbol;
                        }
                        break;
                }
                Form.SetValueByPath("_CODE_CALL", s);
            }

            Validate();
        }

        /// <summary>
        /// проверка, можно ли активировать кнопку "далее"
        /// </summary>
        private void Validate()
        {
            // 89202446789
            // 01234567890
            //        ^

            var result=false;

            var v=Wizard.Values;    
            var s1 = Form.GetValueByPath("_CODE_CALL");
            

            if(s1.Length == 4)
            {
                var s2=v.CheckGet("PHONE_NUMBER");
                if(s2.Length==11)
                {
                    var s3=s2.Substring(7,4);
                    if(s3==s1)
                    {
                        result=true;
                    }
                }
            }                

            if(result)
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


        private void SendButtonSet(bool mode=true)
        {
            if(NextButton!=null)
            {
                if(mode)
                {
                    SendCodeButton.IsEnabled=true;
                    SendCodeButton.Opacity=1.0;
                    SendCodeButton.Style=(Style)NextButton.TryFindResource("TouchFormButtonPrimaryBig");
                }
                else
                {
                    SendCodeButton.IsEnabled=false;
                    SendCodeButton.Opacity=0.5;
                    SendCodeButton.Style=(Style)NextButton.TryFindResource("TouchFormButtonBig");
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
            Wizard.Navigate("ConfirmSms");
        }

        /// <summary>
        /// нажали кнопку "Следующий"
        /// </summary>
        private void NextButtonClick(object sender, RoutedEventArgs e)
        {
            Wizard.Navigate("Info");
        }

        private void CallCodeButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SendCodeButton_Click(object sender, RoutedEventArgs e)
        {
            Wizard.Navigate("ConfirmSms");
        }

        private void Check()
        {
            //если ТМЦ пропускаем шаг
            var v = Wizard.Values;
            switch (v.CheckGet("CARGO_TYPE").ToInt())
            {
                //typeDescription="Я привез ТМЦ";
                case 4:
                    {
                        Wizard.Navigate(1);
                    }
                    break;
                //typeDescription="Я привез рулоны";
                case 5:
                    {
                        Wizard.Navigate(1);
                    }
                    break;
            }
        }
    }
}
