using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Client.Interfaces.Production.ProcessingMachines
{
    /// <summary>
    /// Логика взаимодействия для TechnologicalMapExcelInformationWindow.xaml
    /// </summary>
    public partial class TechnologicalMapExcelInformationWindow : UserControl
    {
        public TechnologicalMapExcelInformationWindow(string infoText)
        {
            InitializeComponent();
            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            FrameName = "TechnologicalMapExcelInformationWindow";

            InformationText = infoText;
        }
        public string FrameName { get; set; }

        /// <summary>
        /// Информация, которая будет отображаться на окне
        /// </summary>
        public string InformationText { get; set; }

        public DispatcherTimer AutoCloseTimer { get; set; }


        /// <summary>
        /// Отображение и автоматическое закрытие через заданное количество секунд
        /// </summary>
        /// <param name="seconds"></param>
        public void ShowAndAutoClose(int seconds = 1, bool sendReport = true)
        {
            if (!string.IsNullOrEmpty(InformationText))
            {
                TechnologicalMapExcelInformationWindowLabel.Content = InformationText;

                // Отправляем отчёт об ошибке
                if (sendReport)
                {
                    var q = new LPackClientQuery();
                    q.SilentErrorProcess = true;

                    var error = new Error();
                    error.Code = 146;
                    error.Message = InformationText;
                    error.Description = "";

                    Central.ProcError(error, "", true, q);
                }

                // режим отображения новых фреймов
                //     0=по умолчанию
                //     1=новая вкладка
                //     2=новое окно
                Central.WM.FrameMode = 2;
                AutoClose(seconds);

                Dictionary<string, string> windowParametrs = new Dictionary<string, string>();
                windowParametrs.Add("no_resize", "1");
                windowParametrs.Add("center_screen", "1");
                Central.WM.Show(FrameName, "Техкарта", true, "add", this, "top", windowParametrs);
            }
        }

        /// <summary>
        /// автоматически скрывает окно через заданное время
        /// P.S. Для срабатывания этой функции вместе с функцией Central.WM.Show нужно вызывать AutoClose() перед Show().
        /// </summary>
        /// <param name="seconds"></param>
        public void AutoClose(int seconds = 1)
        {
            if (AutoCloseTimer == null)
            {
                AutoCloseTimer = new DispatcherTimer
                {
                    Interval = new TimeSpan(0, 0, seconds)
                };

                {
                    var row = new Dictionary<string, string>();
                    row.CheckAdd("TIMEOUT", seconds.ToString());
                    row.CheckAdd("DESCRIPTION", "");
                    Central.Stat.TimerAdd("TechnologicalMapExcelInformationWindow_AutoClose", row);
                }

                AutoCloseTimer.Tick += (s, e) =>
                {
                    AutoCloseTimer.Stop();
                    Close();
                };
            }
            else
            {
                AutoCloseTimer.Stop();
            }
            AutoCloseTimer.Start();
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            Central.WM.Close(FrameName);

            //вся работа по утилизации ресурсов происходит в Destroy
            //он будет вызван при закрытии фрейма
        }

        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "TechnologicalMapExcel",
                ReceiverName = "",
                SenderName = "TechnologicalMapExcelInformationWindow",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {

        }
    }
}
