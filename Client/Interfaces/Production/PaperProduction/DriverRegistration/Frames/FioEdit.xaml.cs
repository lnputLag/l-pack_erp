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
using static Client.Interfaces.Main.DataGridHelperColumn;
using Client.Interfaces.Main;
using System.Windows.Media;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Ввод ФИО водителя
    /// </summary>
    /// <author>Грешных Н.И.</author>
    /// <version>1</version>
    public partial class FioEdit : WizardFrame
    {
        /// <summary>
        ///  на каком поле находится курсор
        /// </summary>
        private int CurTextNum = 0;

        public FioEdit()
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
                    Path="SURNAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=SurnameText,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    First=true,
                },
                new FormHelperField()
                {
                    Path="NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=NameText,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                                                { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="MIDDLE_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=MiddleNameText,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                                                { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="PHONE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=PhoneText,
                      Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        { FormHelperField.FieldFilterRef.Required, null },
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

                            //Form.SetValueByPath("SURNAME", "");
                            //Form.SetValueByPath("NAME", "");
                            //Form.SetValueByPath("MIDDLE_NAME", "");
                            //Form.SetValueByPath("PHONE_NUMBER_7", "+7");
                            Validate();
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
                if (CurTextNum == 0)
                {
                    var s = Form.GetValueByPath("SURNAME");
                    switch (symbol)
                    {
                        case "BACK_SPACE":
                            if (s.Length > 0)
                            {
                                s = s.Substring(0, (s.Length - 1));
                            }
                            break;

                        default:
                            if (s.Length == 0)
                            {
                                s = symbol.ToUpper();
                            }
                            else if (s.Length < 64)
                            {
                                s = s + symbol;
                            }
                            break;
                    }
                    Form.SetValueByPath("SURNAME", s);
                }

                if (CurTextNum == 1)
                {
                    var s = Form.GetValueByPath("NAME");
                    switch (symbol)
                    {
                        case "BACK_SPACE":
                            if (s.Length > 0)
                            {
                                s = s.Substring(0, (s.Length - 1));
                            }
                            break;

                        default:
                            if (s.Length == 0)
                            {
                                s = symbol.ToUpper();
                            }
                            else if (s.Length < 64)
                            {
                                s = s + symbol;
                            }
                            break;
                    }
                    Form.SetValueByPath("NAME", s);
                }

                if (CurTextNum == 2)
                {
                    var s = Form.GetValueByPath("MIDDLE_NAME");
                    switch (symbol)
                    {
                        case "BACK_SPACE":
                            if (s.Length > 0)
                            {
                                s = s.Substring(0, (s.Length - 1));
                            }
                            break;

                        default:
                            if (s.Length == 0)
                            {
                                s = symbol.ToUpper();
                            }
                            else if (s.Length < 64)
                            {
                                s = s + symbol;
                            }
                            break;
                    }
                    Form.SetValueByPath("MIDDLE_NAME", s);
                }

                if (CurTextNum == 3)
                {
                    var s = Form.GetValueByPath("PHONE");
                    switch (symbol)
                    {
                        case "BACK_SPACE":
                            if (s.Length > 2)
                            {
                                s = s.Substring(0, (s.Length - 1));
                            }
                            break;

                        case "0":
                        case "1":
                        case "2":
                        case "3":
                        case "4":
                        case "5":
                        case "6":
                        case "7":
                        case "8":
                        case "9":
                            if (s.Length < 12)
                            {
                                s = s + symbol;
                            }
                            break;
                    }
                    Form.SetValueByPath("PHONE", s);
                }
                Validate();
            }
        }

        /// <summary>
        /// проверка, можно ли активировать кнопку "далее"
        /// </summary>
        private void Validate()
        {
            var s = Form.GetValueByPath("PHONE");
            if (s.Length == 12)
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
            var v = Wizard.Values;

            if ((v.CheckGet("CARGO_TYPE").ToInt() == 4) || ((v.CheckGet("CARGO_TYPE").ToInt() == 5)))
            {
                Wizard.Navigate("CargoType");
            }
            else
                Wizard.Navigate(-1);
        }

        /// <summary>
        /// нажали кнопку "Следующий"
        /// </summary>
        private void NextButtonClick(object sender, RoutedEventArgs e)
        {
            SaveValues();
            Wizard.Navigate("CarModel");
        }

        private void SurnameText_GotFocus(object sender, RoutedEventArgs e)
        {
            CurTextNum = 0;
        }

        private void NameText_GotFocus(object sender, RoutedEventArgs e)
        {
            CurTextNum = 1;
        }

        private void MiddleNameText_GotFocus(object sender, RoutedEventArgs e)
        {
            CurTextNum = 2;
        }

        private void PhoneNumber7Text_GotFocus(object sender, RoutedEventArgs e)
        {
            CurTextNum = 3;
        }
    }
}
