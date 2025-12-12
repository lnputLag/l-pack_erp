using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// документация для интерфейса
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2022-09-30</released>
    /// <changed>2022-09-30</changed>
    public partial class RollControlHelp : UserControl
    {
        public RollControlHelp()
        {
            InitializeComponent();

            CurrentAssembly = Assembly.GetExecutingAssembly();

            SetDefaults();
        }

        public Assembly CurrentAssembly { get; set;}

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
                SenderName = "RollControlHelp",
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
            try
            {
                var src="Client.Interfaces.Production.CorrugatingMachines.RollControl.Elements.1.png";
                var stream = CurrentAssembly.GetManifestResourceStream(src);
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = stream;
                image.EndInit();
                HelpImage.Source=image;
            }
            catch(Exception e)
            {

            }   

            Show();
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
