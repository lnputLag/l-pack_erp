using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Xps.Packaging;

namespace Client.Interfaces.Main
{
    /// <summary>
    /// просмотрщик ярлыков
    /// (используется в интерфейсах с тачскрином)
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-05-30</released>
    /// <changed>2023-05-30</changed>
    public partial class LabelViewer : UserControl
    {
        public LabelViewer()
        {
            InitializeComponent();    

            ReceiptPath = "";
            DriverPath = "http://192.168.3.237/repo/l-pack_erp/etc/drv/printer/SAM4SEllixPrinterDriverInstallerV3.0.4.3.exe";
            ReceiptComplete = false;
            ReceiptDocument = null;
            Printer = new PrintDialog();
            Log="";

            InitForm();
            SetDefaults();
        }
        public Window Window { get; set; }
        public FormHelper Form { get; set; }
        private string ReceiptPath { get; set; }
        private string DriverPath { get; set; }
        private PrintDialog Printer { get; set; }
        public bool ReceiptComplete { get; set; }
        public XpsDocument ReceiptDocument { get; set; }
        public string Log {get;set;}

        /// <summary>
        /// Инициализация формы
        /// </summary>
        public void InitForm()
        {
            Form = new FormHelper();
            //список колонок формы
            var fields = new List<FormHelperField>()
            {
            };

            Form.SetFields(fields);
            Form.StatusControl = FormStatus;
        }

        /// <summary>
        /// деструктор интерфейса
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "",
                ReceiverName = "",
                SenderName = "LabelViewer",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        private void SetDefaults()
        {
            Form.SetDefaults();
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
        /// инициализация вьювера
        /// 
        /// </summary>
        public bool Init()
        {
            bool result = false;
            bool resume = true;
            //string report = "";

            LogMsg($"Init");

            ReceiptViewer.Document = ReceiptDocument.GetFixedDocumentSequence();
            LabelLog.Text = Log;
            return result;
        }

        /// <summary>
        /// печать полотна с ярлыком
        /// </summary>
        public bool Print(bool immediate = false)
        {
            bool result = false;
            //string report = "";
            LogMsg($"Print");

            try
            {
                if (Printer == null)
                {
                    PrintDialog printDlg = new PrintDialog();
                    Printer = printDlg;
                }

                if (immediate)
                {
                    Show();
                    Printer.PrintDocument(ReceiptViewer.Document.DocumentPaginator, "");
                    Splash.Visibility = Visibility.Visible;
                    Close();
                    Splash.Visibility = Visibility.Hidden;
                }
                else
                {
                    Printer.PrintDocument(ReceiptViewer.Document.DocumentPaginator, "");
                }

                result = true;
            }
            catch (Exception e)
            {
                LogMsg($"Не удалось напечатать ярлык.");
                LogMsg($"Возможно, принтер не настроен.");
                result = false;
            }

            LabelLog.Text = Log;

            if (!result)
            {
                //var t = LabelLog.Text;
                //t = $"{t}{report}";
                //LabelLog.Text = t;
                Splash.Visibility = Visibility.Hidden;
                Show();
            }

            return result;
        }

        /// <summary>
        /// вызов системного диалога печати
        /// </summary>
        public void Setup()
        {
            PrintDialog printDlg = new PrintDialog();
            Printer = printDlg;
            Printer.ShowDialog();
        }

        /// <summary>
        /// установка драйвера принтера
        /// </summary>
        public void Install()
        {
            if (!string.IsNullOrEmpty(DriverPath))
            {
                Central.OpenFile(DriverPath);
            }
        }

        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Show()
        {
            //Central.WM.FrameMode=2;
            Central.WM.Show($"DriverLabelViewer", "Печать", true, "add", this);
            Central.WM.SetActive($"DriverLabelViewer", true);
        }

        /// <summary>
        /// формирование и запись строки во внутренний журнал
        /// </summary>
        /// <param name="text"></param>
        /// <param name="addCr"></param>
        /// <param name="offset"></param>
        public void LogMsg(string text, bool addCr = false, int offset = 0)
        {
            if (addCr)
            {
                var o = "";
                if (offset > 0)
                {
                    for (int i = 0; i <= offset; i++)
                    {
                        o = $"{o}    ";
                    }
                }

                if (text.IndexOf("\n") > -1)
                {
                    if (!o.IsNullOrEmpty())
                    {
                        text = text.Replace("\n", $"\n{o}");
                    }
                }

                text = $"{o}{text}";
            }

            if (!text.IsNullOrEmpty())
            {
                Log = Log.Append(text, addCr);
            }
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            Central.WM.Close($"DriverLabelViewer");
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }


        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            Print();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ReceiptPath))
            {
                Central.OpenFile(ReceiptPath);
            }
        }

        private void SetupButton_Click(object sender, RoutedEventArgs e)
        {
            Setup();
        }

        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            Install();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Init();
        }
    }
}
