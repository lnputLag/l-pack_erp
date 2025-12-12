using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Client.Interfaces.Production
{
    public partial class TabControl:UserControl
    {
        /*
            Loaded
            Showed
            Closed
         
         */

        public TabControl()
        {
            if(Central.InDesignMode()){
                return;
            }

            ControlName=this.GetType().Name;
            ControlTitle=ControlName;
            ControlId=Cryptor.MakeRandom().ToString();
            GroupName="";

            Loaded += OnLoad;
        }

        public string ControlName {get;set;}
        public string ControlTitle {get;set;}
        public string ControlId {get;set;}
        public string GroupName {get;set;}

        private string TabName {get;set;}

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            Central.WM.SetActive(TabName, true);

            // отправка сообщения о загрузке окна
            Central.Msg.SendMessage(new ItemMessage()
            {
                ReceiverGroup="",
                ReceiverName = "",
                SenderName = ControlName,
                Action = "Loaded",
            });
        }

        public void Show(string tabName="")
        {
            TabName=$"{ControlName}_{ControlId}";
            var tabTitle=ControlTitle;

            if(tabName.IsNullOrEmpty())
            {
                tabName=TabName;
            }
            Central.WM.AddTab(tabName, tabTitle, true, "add", this);

            Central.Msg.SendMessage(new ItemMessage()
            {
                ReceiverGroup="",
                ReceiverName = ControlName,
                SenderName = ControlName,
                Action = "Showed",
            });
        }

        public void Close()
        {
            Central.WM.RemoveTab(TabName);
        }

        /// <summary>
        /// проверка, является ли данный таб активным в данный момент
        /// </summary>
        /// <returns></returns>
        public bool IsActive()
        {
            bool result=false;
            if(!ControlName.IsNullOrEmpty())
            {
                if(Central.WM.TabSelected1 == TabName)
                {
                    result=true;
                }
            }
            return result;
        }

        public void Destroy()
        {
            // отправка сообщения о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="",
                ReceiverName = "",
                SenderName = ControlName,
                Action = "Closed",
            });

            Central.Msg.SendMessage(new ItemMessage()
            {
                ReceiverGroup="",
                ReceiverName = "",
                SenderName = ControlName,
                Action = "Closed",
            });

            // отключение обработчика сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }
    }
}
