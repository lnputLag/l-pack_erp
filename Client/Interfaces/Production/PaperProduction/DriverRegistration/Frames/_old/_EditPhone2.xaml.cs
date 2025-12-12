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
    public partial class EditPhone2 : WizardFrame
    {
        public EditPhone2()
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
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            //установка значений по умолчанию
            Form.SetDefaults();

            //установка курсора в конец строки
            //(по умолчанию там цифра 8, нужно курсор поставить после 8)
            var s=Form.GetFieldByPath("PhoneEdit").ToString();
            if(s.Length > 0)
            {
                PhoneEdit.CaretIndex=s.Length;
            }

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
                switch (message.Action)
                {
                    //фрейм загружен 
                    case "Loaded":
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

        /// <summary>
        /// ввод в поле
        /// </summary>
        /// <param name="symbol"></param>
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

            Validate();
        }

        /// <summary>
        /// проверка, можно ли активировать кнопку "далее"
        /// </summary>
        private void Validate()
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
        /// нажали кнопку "Домой"
        /// </summary>
        private void HomeButtonClick(object sender, RoutedEventArgs e)
        {
            Navigate(0);
        }

        /// <summary>
        /// нажали кнопку "Предыдущий"
        /// </summary>
        private void PriorButtonClick(object sender, RoutedEventArgs e)
        {
            Navigate(-1);
        }

        /// <summary>
        /// нажали кнопку "Следующий"
        /// </summary>
        private void NextButtonClick(object sender, RoutedEventArgs e)
        {
            Navigate(1);
        }
    }
}
