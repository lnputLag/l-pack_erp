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
    /// <author>ledovskikh_dv</author>
    public partial class UnitWeightHelp : UserControl
    {
        public UnitWeightHelp()
        {
            InitializeComponent();

            CurrentAssembly = Assembly.GetExecutingAssembly();

            SetDefaults();
        }

        public Assembly CurrentAssembly { get; set; }

        /// <summary>
        /// деструктор интерфейса
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Production",
                ReceiverName = "",
                SenderName = "UnitWeightHelp",
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
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            FormStatus.Text = "";
        }

        /// <summary>
        // инициализация компонентов таблицы
        /// </summary>
        public void Init()
        {
            try
            {
                var src = "Client.Interfaces.Production.IndustrialWaste.Elements.Doc.png";
                var stream = CurrentAssembly.GetManifestResourceStream(src);
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = stream;
                image.EndInit();
                HelpImage.Source = image;
            }
            catch (Exception e)
            {

            }

            Show();
        }

        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Show()
        {
            //Central.WM.FrameMode=2;
            Central.WM.Show($"DocTouch", "Документация", true, "add", this);
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            Central.WM.Close($"DocTouch");
        }

        /// <summary>
        /// обработчик нажатия на кнопку отмена
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

    }
}
