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

namespace Client.Interfaces.Production
{

    /// <summary>
    /// форма для просмотра и печати ярлыка
    /// формирует ярлык при регистрации водителя по шаблону
    /// и показывает его или печатает
    /// взято за основу у Балчугова Д.
    /// </summary>
    /// <author>Грешных</author>
    public partial class DriverLabelViewer : UserControl
    {
        public DriverLabelViewer()
        {
            ReceiptPath = "";
            DriverPath = "http://192.168.3.237/repo/l-pack_erp/etc/drv/printer/SAM4SEllixPrinterDriverInstallerV3.0.4.3.exe";
            ReceiptComplete = false;
            ReceiptDocument = null;
            IdScrap = 0;
            ScrapFaultReason = "";
            Printer = new PrintDialog();

            InitializeComponent();

            InitForm();
            SetDefaults();
        }
        public Window Window { get; set; }

        public string ScrapFaultReason { get; set; }

        /// <summary>
        /// Id машины
        /// </summary>
        public int IdScrap { get; set; }

        public FormHelper Form { get; set; }
        private string ReceiptPath { get; set; }
        private string DriverPath { get; set; }
        private PrintDialog Printer { get; set; }
        public bool ReceiptComplete { get; set; }
        public XpsDocument ReceiptDocument { get; set; }

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
                ReceiverGroup = "DriverRegistration",
                ReceiverName = "",
                SenderName = "DriverLabelViewer",
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
            string report = "";

            if (resume)
            {
                report = $"{report}\n Машина:[{IdScrap}]";
                if (IdScrap == 0)
                {
                    report = $"{report}\n Ошибка. ИД машины не задано.";
                    resume = false;
                }
            }

            if (resume)
            {
                var receipt = new ScrapReceipt(IdScrap);
                receipt.ScrapFaultReason = ScrapFaultReason;
                var makeResult = receipt.Make();
                if (makeResult)
                {
                    report = $"{report}{receipt.Report}";
                    ReceiptDocument = receipt.Document;
                    ReceiptViewer.Document = ReceiptDocument.GetFixedDocumentSequence();
                    result = true;
                }
                else
                {
                    report = $"{report}\n Ошибка. Ярлык не сформирован.";
                    report = $"{report}{receipt.Report}";
                    resume = false;
                }
            }

            ReceiptLog.Text = report;
            return result;
        }

        /// <summary>
        /// печать полотна с ярлыком
        /// </summary>
        public bool Print(bool immediate = false)
        {
            bool result = false;
            string report = "";

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
                report = $"{report}\n Не удалось напечатать.";
                report = $"{report}\n Возможно, принтер не настроен.";
                result = false;
            }

            if (!result)
            {
                var t = ReceiptLog.Text;
                t = $"{t}{report}";
                ReceiptLog.Text = t;
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
