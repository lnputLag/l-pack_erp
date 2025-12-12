using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Production
{

    /// <summary>
    /// просмотрщик журнала
    /// применяется для встроенного режима
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-07-03</released>
    /// <changed>2023-07-03</changed>
    public partial class LogTouch : UserControl
    {
        public LogTouch()
        {
            InitializeComponent();
            SetDefaults();
            Messenger.Default.Register<ItemMessage>(this,ProcessMessages);
        }

        /// <summary>
        /// адрес страницы документации
        /// </summary>
        public string Url { get;set; }


        /// <summary>
        /// деструктор интерфейса
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="Production",
                ReceiverName = "",
                SenderName = "LogTouch",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
           
        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            switch(e.Key)
            {
                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;
            }
        }

        public void SetDefaults()
        {
            FormStatus.Text="";
        }

        public void Init()
        {
            if(!string.IsNullOrEmpty(Url))
            {                
                Show();
            }
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        private void ProcessMessages(ItemMessage obj)
        {
            //Group 
            if (obj.ReceiverGroup.IndexOf("Production") > -1)
            {
                if (obj.ReceiverName.IndexOf("LogTouch") > -1)
                {
                    switch (obj.Action)
                    {
                        case "KeyPressed":
                            LogMsg(obj.Message);               
                            break;
                    }
                }
            }
        }

        public void LogMsg(string msg)
        {
            var today=DateTime.Now.ToString("yyyy-mm-dd_HH:mm:ss");
            Log.Text=$"{Log.Text}\n{today}: {msg}";
        }

        public void SetLogText(string msg)
        {
            Log.Text = msg;
        }

        public void Show()
        {
            //Central.WM.FrameMode=2;
            Central.WM.Show($"LogTouch","Журнал",true,"add",this);
        }

        public void Close()
        {
            Central.WM.Close($"LogTouch");
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

    }
}
