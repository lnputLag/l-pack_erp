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
    /// проверка данных (ФИО, номер машины и прицепа, телефона)
    /// </summary>
    /// <author>Грешных Н.И.</author>
    /// <version>1</version>
    public partial class CheckInfo2 : WizardFrame
    {
        public CheckInfo2()
        {
            InitializeComponent();

            if (Central.InDesignMode())
            {
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
                    Path="CARGO_TYPE_DESCRIPTION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CargoTypeDescriptionText,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    First=true,
                },
                new FormHelperField()
                {
                    Path="PHONE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=PhoneText,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SURNAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=FioText,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="MIDDLE_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TRUCK_NUMBER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TruckNumberText,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },

                },
                new FormHelperField()
                {
                    Path="TRAILER_NUMBER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TrailerNumberText,
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
                            SetDefaults();
                            LoadValues();
                            Validate();
                            var fio = Form.GetValueByPath("SURNAME") + " " + Form.GetValueByPath("NAME") + " " + Form.GetValueByPath("MIDDLE_NAME");
                            FioText.Text = fio;
                            //    Form.SetValueByPath("SURNAME", );
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// проверка, можно ли активировать кнопку "далее"
        /// </summary>
        private void Validate()
        {
            NextButtonSet(true);
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
            Wizard.Navigate("TruckNumber");
        }

        /// <summary>
        /// нажали кнопку "Следующий"
        /// </summary>
        private void NextButtonClick(object sender, RoutedEventArgs e)
        {
           
            Wizard.Navigate("ConfirmSms2");
        }
    }
}
