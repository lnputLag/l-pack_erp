using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Client.Interfaces.Main
{
    public partial class NotificationItemView:UserControl
    {
        public NotificationItemView()
        {
            InitializeComponent();
            Code="";
            Class="";
            Link="";
            Type=0;
        }

        /// <summary>
        /// уникальный код сообщения
        /// </summary>
        public string Code { get;set; }
        /// <summary>
        /// класс сообщения
        /// сообщения с одном классом на клиенте буду заменять друг друга
        /// (если пришло новое сообщение оно  заменит существующее в боксе с таким же классом)
        /// </summary>
        public string Class { get;set; }
        /// <summary>
        /// индекс строки, в которой размещается уведомление в боксе уведомлений
        /// </summary>
        public int RowNumber { get;set; }
        /// <summary>
        /// ссылка на интерфейс L-PACK ERP для перехода        /// 
        /// </summary>
        public string Link { get;set; }
        /// <summary>
        /// тип уведомления:
        /// 1 -- (важное) призыв к действию
        /// 2 -- (обычное) информационное сообщение
        /// </summary>
        public int Type { get; set; }

        private void ItemRemoveButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
             var o=new  Dictionary<string,string>(){
                { "link" , Link },
                { "code" , Code },
            };
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="Main",
                ReceiverName = "Notifications",
                SenderName = "NotificationItems",
                Action = "Close",
                Message=Code,
                ContextObject=o,
            });
        }

        private void Link_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            var o=new  Dictionary<string,string>(){
                { "link" , Link },
                { "code" , Code },
            };
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="Main",
                ReceiverName = "Notifications",
                SenderName = "NotificationItems",
                Action = "GotoLink",
                Message=Link,
                ContextObject=o,
            });
        }

        private void Title_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            var o=new  Dictionary<string,string>(){
                { "link" , Link },
                { "code" , Code },
            };
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="Main",
                ReceiverName = "Notifications",
                SenderName = "NotificationItems",
                Action = "GotoLink",
                Message=Link,
                ContextObject=o,
            });
        }
    }
}
