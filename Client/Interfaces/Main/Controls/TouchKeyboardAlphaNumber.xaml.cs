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
    public partial class TouchKeyboardAlphaNumber : UserControl
    {
        /// <summary>
        /// символьная клавиатура + цифры
        /// (для интерфейсов с тачскрином)
        /// </summary>
        /// <author>greshnyh_ni</author>
        /// <version>1</version>
        /// <released>2025-02-26</released>
        /// <changed>2025-00-26</changed>
        public TouchKeyboardAlphaNumber()
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
                SenderName = "TouchKeyboardAlphaNumber",
                Action = "KeyPressed",
                Message=m,
            });
        }
    }
}
