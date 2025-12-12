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
    public partial class TouchKeyboardAlpha : UserControl
    {
        /// <summary>
        /// символьная клавиатура
        /// (для интерфейсов с тачскрином)
        /// </summary>
        /// <author>balchugov_dv</author>
        /// <version>1</version>
        /// <released>2023-05-29</released>
        /// <changed>2023-05-29</changed>
        public TouchKeyboardAlpha()
        {
            InitializeComponent();

            ReceiverName="";
            ReceiverGroup="";
        }

        /// <summary>
        /// </summary>
        public string ReceiverName {get;set;}
        public string ReceiverGroup {get;set;}

        private void KeyboardButtonClick(object sender, RoutedEventArgs e)
        {
            var m="";

            if(sender!=null)
            {
                var button=(Button)sender;
                m=button.Tag.ToString();
            }

            Central.Msg.SendMessage(new ItemMessage()
            {
                ReceiverGroup=ReceiverGroup,
                ReceiverName = ReceiverName,
                SenderName = "TouchKeyboardAlpha",
                Action = "KeyPressed",
                Message=m,
            });
        }
    }
}
