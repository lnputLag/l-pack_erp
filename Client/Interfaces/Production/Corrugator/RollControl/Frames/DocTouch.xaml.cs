using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Production
{

    /// <summary>
    /// просмотрщик документации
    /// применяется для встроенного режима
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2022-05-30</released>
    /// <changed>2022-05-30</changed>
    public partial class DocTouch : UserControl
    {
        public DocTouch()
        {
            InitializeComponent();

            SetDefaults();
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
                SenderName = "DocTouch",
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
                Browser.Navigate(Url);
                Show();
            }
        }

        public void Show()
        {
            //Central.WM.FrameMode=2;
            Central.WM.Show($"DocTouch","Документация",true,"add",this);
        }

        public void Close()
        {
            Central.WM.Close($"DocTouch");
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

    }
}
