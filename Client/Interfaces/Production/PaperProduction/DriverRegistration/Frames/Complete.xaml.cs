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
    /// Вывод информации водителю перед записью в базу
    /// </summary>
    /// <author>Грешных Н.И.</author>
    /// <version>1</version>
    public partial class Complete : WizardFrame
    {
        public Complete()
        {
            InitializeComponent();

            if (Central.InDesignMode())
            {
                return;
            }

            CarSaveFlag = false;
            ItemId = 0;
            AutoCloseFormInterval = 10;
            InitForm();
            SetDefaults();
            Central.Msg.Register(ProcessMessage);
        }

        public int ItemId { get; set; }

        private bool CarSaveFlag { get; set; }
        /// <summary>
        /// интервал автозакрытия формы, сек
        /// </summary>
        public int AutoCloseFormInterval { get; set; }

        /// <summary>
        /// таймер автозакрытия формы
        /// </summary>
        private DispatcherTimer AutoCloseFormTimer { get; set; }

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
                    Path="ITEM_ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
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
                            Check();
                            //Print();
                            break;
                    }
                }
            }
        }

        private void Print()
        {
            ItemId = Form.GetValueByPath("ITEM_ID").ToInt();

            // в отладочном режиме отображение на экране
            // в рабочем режиме сразу печать на принтере
            if (Central.DebugMode)
            {
                DriverRegistrationInterface.ProcessLabel(1, ItemId);
            }
            else
            {
                DriverRegistrationInterface.ProcessLabel(2, ItemId);
            }
            FormCloseTimerRun();
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

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", AutoCloseFormInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("Complete_AutoCloseFormTimer", row);
                    }

                    AutoCloseFormTimer.Tick += (s, e) =>
                    {
                        FormCloseManual();
                    };
                }

                if (AutoCloseFormTimer.IsEnabled)
                {
                    AutoCloseFormTimer.Stop();
                }
                AutoCloseFormTimer.Start();
            }

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

        private void FormCloseManual()
        {
            AutoCloseFormTimer.Stop();
            Wizard.Navigate(0);
        }

        private void Check()
        {
            var v = Wizard.Values;
            if (v.CheckGet("CARGO_TYPE").ToInt() == 6)
            {
                Print();
            }
            else
            if (v.CheckGet("CARGO_TYPE").ToInt() >= 4)
            {
                FormCloseTimerRun();
            }
            else
            {
                Print();
            }
        }

    }
}
