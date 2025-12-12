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
using Client.Interfaces.Production;

namespace Client.Interfaces.Main
{
    /// <summary>
    /// цировая клавиатура
    /// (для интерфейсов с тачскрином)
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-05-26</released>
    /// <changed>2023-05-29</changed>
    public partial class TouchKeyboardNumber : UserControl
    {
        public TouchKeyboardNumber()
        {
            InitializeComponent();

            ReceiverName="";
            ReceiverGroup="";
        }

        public bool PreventSendKeyToCurrentControll = false;
        public delegate void PressKey(string key);
        public event PressKey OnPressKey;

        /// <summary>
        /// </summary>
        public string ReceiverName {get;set;}
        public string ReceiverGroup {get;set;}


        private void KeyboardButtonClick(object sender, RoutedEventArgs e)
        {
            var m = "";

            if (sender != null)
            {
                var button = (Button)sender;
                m = button.Tag.ToString();
            }

            if (!PreventSendKeyToCurrentControll)
            {
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = ReceiverGroup,
                    ReceiverName = ReceiverName,
                    SenderName = "TouchKeyboardNumber",
                    Action = "KeyPressed",
                    Message = m,
                });

                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = ReceiverGroup,
                    ReceiverName = ReceiverName,
                    SenderName = "TouchKeyboardNumber",
                    Action = "key_pressed",
                    Message = m,
                });
            }
            else
            {
                OnPressKey?.Invoke(m);
            }
        }
    }
}
