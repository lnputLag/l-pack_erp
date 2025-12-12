using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// отправляем сообщение погрузчику
    /// </summary>
    /// <author>михеев</author>
    public partial class ForkliftdriverSendMessage : UserControl
    {
        private Window Window { get; set; }

        public List<string> IdDriverList = new List<string>();


        public ForkliftdriverSendMessage()
        {
            InitializeComponent();

            if (!Central.InDesignMode())
            {
            }
        }


        public void Show()
        {
            var title = $"Отправка сообщения водителям";

            var w = 520;
            var h = 300;

            Window = new Window
            {
                Title = title,
                Width = w + 17,
                Height = h + 40,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow,
                Content = new Frame
                {
                    Content = this,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                },
            };

            if (Window != null)
            {
                Window.Topmost = true;
                Window.ShowDialog();
            }


        }

        private void Close()
        {
            Window?.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }


        private void Save()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "ForkliftDriver");
            q.Request.SetParam("Action", "SendMessage");

            q.Request.SetParam("RECEIVER_LIST", JsonConvert.SerializeObject(IdDriverList));
            q.Request.SetParam("MESSAGE_TEXT", MessageBox.Text);
            q.Request.SetParam("MESSAGE_LIFE_TIME", "1");
            q.Request.SetParam("SENDER_NAME", Central.User.Surname + " " + Central.User.Name);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                Messenger.Default.Send(new ItemMessage()
                {
                    ReceiverGroup = "ShipmentControl",
                    ReceiverName = "ForkliftDriverMessage",
                    SenderName = "ForkliftdriverSendMessage",
                    Action = "Refresh",
                    Message = "",
                });
            }
            else
            {
                q.ProcessError();
            }

            Close();
        }
    }
}
