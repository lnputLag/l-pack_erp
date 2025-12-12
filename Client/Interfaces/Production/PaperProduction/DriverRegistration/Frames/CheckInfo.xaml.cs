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
    /// проверка данных
    /// </summary>
    /// <author>Грешных Н.И.</author>
    /// <version>1</version>
    public partial class CheckInfo : WizardFrame
    {
        public CheckInfo()
        {
            InitializeComponent();

            if (Central.InDesignMode())
            {
                return;
            }

            CargoTypeDescriptionTextHidden = false;
            PhoneNumberTextHidden = false;

            InitForm();
            SetDefaults();
            Central.Msg.Register(ProcessMessage);
        }

        private bool CargoTypeDescriptionTextHidden { get; set; }
        private bool PhoneNumberTextHidden { get; set; }

        private void Check()
        {
            var v = Wizard.Values;
            
            PhoneNumberTextHidden = false;
            switch (v.CheckGet("CARGO_TYPE").ToInt())
            {
                //typeDescription="Я привез макулатуру";
                case 1:
                    {
                        CargoTypeDescriptionTextHidden = false;
                    }
                    break;

                //typeDescription="Я приехал за полиэтиленовой смесью";
                case 2:
                    {
                        CargoTypeDescriptionTextHidden = true;
                    }
                    break;
                //typeDescription="Я привез химию";
                case 3:
                    {
                        CargoTypeDescriptionTextHidden = true;
                    }
                    break;
                //typeDescription="Я привез ТМЦ";
                case 4:
                    {
                        PhoneNumberTextHidden = true;
                    }
                    break;
                //typeDescription="Я привез рулоны";
                case 5:
                    {
                        PhoneNumberTextHidden = true;
                    }
                    break;

            }

            if ((CargoTypeDescriptionTextHidden) || (PhoneNumberTextHidden))
            {
                VendorNameLabel.Visibility = Visibility.Collapsed;
                VendorNameControl.Visibility = Visibility.Collapsed;
            }
            else
            {
                VendorNameLabel.Visibility = Visibility.Visible;
                VendorNameControl.Visibility = Visibility.Visible;
            }

            if (PhoneNumberTextHidden)
            {
                PhoneNumberText.Visibility = Visibility.Collapsed;
                PhoneNameLabel.Visibility = Visibility.Collapsed;
            }
            else
            {
                PhoneNumberText.Visibility = Visibility.Visible;
                PhoneNameLabel.Visibility = Visibility.Visible;
            }

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
                    Enabled=CargoTypeDescriptionTextHidden,
                },
                new FormHelperField()
                {
                    Path="PHONE_NUMBER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=PhoneNumberText,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    First=true,
                },
                new FormHelperField()
                {
                    Path="CAR_MODEL_DESCRIPTION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CarModelDescriptionText,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    First=true,
                },
                new FormHelperField()
                {
                    Path="CAR_NUMBER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CarNumberText,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    First=true,
                },
                new FormHelperField()
                {
                    Path="VENDOR_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=VendorNameText,
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
                            Check();
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
            Wizard.Navigate(-1);
        }

        /// <summary>
        /// нажали кнопку "Следующий"
        /// </summary>
        private void NextButtonClick(object sender, RoutedEventArgs e)
        {
            Wizard.Navigate(1);
        }
    }
}
