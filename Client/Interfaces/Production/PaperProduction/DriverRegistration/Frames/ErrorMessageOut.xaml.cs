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
    /// Вывод информации водителю об ошибке
    /// </summary>
    /// <author>Грешных Н.И.</author>
    /// <version>1</version>
    public partial class ErrorMessageOut : WizardFrame
    {
        public ErrorMessageOut()
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

        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {

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
                            ErrorInfo.Text = Wizard.Values.CheckGet("ERROR_INFO").ToString();
                            break;

                        //ввод с экранной клавиатуры
                        case "KeyPressed":
                            break;
                    }
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
        /// нажали кнопку "Далее" (для отладки)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NextButtonClick(object sender, RoutedEventArgs e)
        {
            Wizard.Navigate("InformationOutput");
        }
    }
}
