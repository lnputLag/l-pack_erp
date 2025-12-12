using Client.Interfaces.Service.Printing;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using System.Drawing.Printing;
using System.Printing;
using System;
using System.Windows;
using System.Windows.Controls;
using static DevExpress.XtraPrinting.Native.ExportOptionsPropertiesNames;
using System.Windows.Documents;
using Client.Common;

namespace Client.Interfaces.Main
{
    /// <summary>
    /// Interaction logic for PrintPreview.xaml
    /// </summary>
    public partial class PrintPreview:Window
    {
        public PrintPreview()
        {
            ContentWidth=850;
            ContentHeight=800;
            Init();
        }

        public PrintPreview(int width,int height)
        {
            ContentWidth=width;
            ContentHeight=height;
            Init();
        }

        public PrintPreview(bool landscape)
        {
            ContentWidth=850;
            ContentHeight=800;
            _landscape = landscape;
            if(landscape)
            {
                ContentWidth=1200;
            }
            Init();
        }

        public int ContentWidth { get; set; }
        public int ContentHeight { get; set; }

        private bool _landscape;

        /// <summary>
        /// Настройки из профиля печати
        /// </summary>
        public PrintingSettings PrintingSettings { get; set; }

        private Button printbtn;
        public Button PrintButton
        {
            get
            {
                return printbtn;
            }
        }

        public void Init()
        {
            InitializeComponent();
            documentViewer.Width=ContentWidth;
            documentViewer.Height=ContentHeight;
        }

        private void PrintButtonClick(object sender,RoutedEventArgs e)
        {
            PrintDocument();
            e.Handled=true;
        }

        private void SaveButtonClick(object sender, RoutedEventArgs e)
        {
            SaveDocument();
            e.Handled = true;
        }

        public void SaveDocument()
        {
            var printHelper = new PrintHelper();
            printHelper.PrintingProfile = "default";
            printHelper.PrintingDuplex = System.Drawing.Printing.Duplex.Simplex;
            printHelper.Init();
            var printingResult = printHelper.SaveDocument(documentViewer.Document.DocumentPaginator);
            printHelper.Dispose();
        }
        
        public void PrintDocument()
        {
            try
            {
                if (PrintingSettings != null)
                {
                    PrintDialog PrintDialog = new PrintDialog();
                    PrintServer printServer = new PrintServer(!string.IsNullOrEmpty(PrintingSettings.PrinterServerName) ? PrintingSettings.PrinterServerName : null);
                    var pq = printServer.GetPrintQueue(PrintingSettings.PrinterQueueName);
                    PrintDialog.PrintQueue = pq; //FIXME передача других параметров печати

                    if (PrintingSettings.Landscape != null)
                    {
                        if ((bool)PrintingSettings.Landscape)
                        {
                            PrintDialog.PrintTicket.PageOrientation = PageOrientation.Landscape;
                        }
                        else
                        {
                            PrintDialog.PrintTicket.PageOrientation = PageOrientation.Portrait;
                        }
                    }

                    switch (PrintingSettings.Duplex)
                    {
                        case Duplex.Default:
                        default:
                            PrintDialog.PrintTicket.Duplexing = Duplexing.Unknown;
                            break;

                        case Duplex.Simplex:
                            PrintDialog.PrintTicket.Duplexing = Duplexing.OneSided;
                            break;

                        case Duplex.Horizontal:
                            PrintDialog.PrintTicket.Duplexing = Duplexing.TwoSidedShortEdge;
                            break;

                        case Duplex.Vertical:
                            PrintDialog.PrintTicket.Duplexing = Duplexing.TwoSidedLongEdge;
                            break;
                    }

                    PrintDialog.PrintDocument(documentViewer.Document.DocumentPaginator, "");
                }
                else
                {
                    PrintDialog PrintDialog = new PrintDialog();

                    if (_landscape)
                    {
                        PrintDialog.PrintTicket.PageOrientation = PageOrientation.Landscape;
                    }

                    PrintDialog.PrintDocument(documentViewer.Document.DocumentPaginator, "");
                }
            }
            catch (Exception ex)
            {
                SendErrorReport(ex.Message);
            }           
        }

        /// <summary>
        /// Сохраняем репорт о клиентской ошибке
        /// </summary>
        public void SendErrorReport(string msg)
        {
            var q = new LPackClientQuery();
            // Отключаем стандартное всплывающее сообщение с ошибкой и отправляем репорт
            q.Answer.Error.Message = msg;
            q.SilentErrorProcess = true;
            q.Answer.Status = 145;
            q.ProcessError();
        }
    }
}
