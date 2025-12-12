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
    /// формирует ярлык рулона по шаблону
    /// и показывает его или печатает
    /// </summary>
    /// <author>balchugov_dv</author>
    public partial class RollReceiptViewer : UserControl
    {
        public RollReceiptViewer()
        {
            ReceiptPath="";
            DriverPath="http://192.168.3.237/repo/l-pack_erp/etc/drv/printer/SAM4SEllixPrinterDriverInstallerV3.0.4.3.exe";
            ReceiptComplete=false;
            ReceiptDocument=null;
            RollId=0;
            RollFaultReason="";
            Printer = new PrintDialog();  

            InitializeComponent();

            InitForm();
            SetDefaults();
        }
        public Window Window { get; set; }
        
        /// <summary>
        /// id рулона, приходит снаружи
        /// </summary>
        public int RollId { get; set; }
        public string RollFaultReason { get;set; }

        public FormHelper Form { get; set; }
        private string ReceiptPath { get;set;}
        private string DriverPath { get;set;}
        private PrintDialog Printer { get;set;}
        public bool ReceiptComplete { get;set;}
        public XpsDocument ReceiptDocument { get; set; }

        /// <summary>
        /// Инициализация формы
        /// </summary>
        public void InitForm()
        {
            Form=new FormHelper();
            //список колонок формы
            var fields=new List<FormHelperField>()
            {                
            };

            Form.SetFields(fields);
            Form.StatusControl=FormStatus;
        }

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
                SenderName = "RollReceiptViewer",
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
            switch(e.Key)
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
            bool result=false;
            bool resume=true;
            string report="";

            if(resume)
            {
                report=$"{report}\n Рулон:[{RollId}]";
                if(RollId==0)
                {
                    report=$"{report}\n Ошибка. ИД рулона не задано.";
                    resume=false;
                }
            }

            if(resume)
            {
                var receipt = new RollReceipt(RollId);
                receipt.RollFaultReason=RollFaultReason;
                var makeResult=receipt.Make();
                if(makeResult)
                {
                    report=$"{report}{receipt.Report}";
                    ReceiptDocument=receipt.Document;
                    ReceiptViewer.Document=ReceiptDocument.GetFixedDocumentSequence();
                    result=true;
                }
                else
                {
                    report=$"{report}\n Ошибка. Ярлык не сформирован.";
                    report=$"{report}{receipt.Report}";
                    resume=false;
                }
            }

            ReceiptLog.Text=report;
            return result;
        }

        /// <summary>
        /// Диалог печати
        /// </summary>
        /// <param name="rollId"></param>
        public void _Edit(int rollId)
        {
            RollId = rollId;
            _GetData(1);
        }

        /// <summary>
        /// печать
        /// </summary>
        /// <param name="rollId"></param>
        /// <returns></returns>
        public bool _Print(int rollId)
        {
            bool result=false; 

            RollId = rollId;
            _GetData(2);
            result=ReceiptComplete;
            return result;
        }

        /// <summary>
        /// получение данных
        /// method:1=открыть диалог,2=напечатать
        /// </summary>
        private async void _GetData(int method=0)
        {
            ReceiptLog.Text="";
            ReceiptPath="";
            ReceiptComplete=false;

            bool complete=false;
            string report="";

            var p=new Dictionary<string,string>();
            {
                p.CheckAdd("ID",RollId.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "Roll");
            q.Request.SetParam("Action", "GetReceipt");
            q.Request.SetParams(p);

            q.Request.RequiredAnswerType=LPackClientAnswer.AnswerTypeRef.File;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                {
                    bool resume=true;

                    if(resume) { 
                        if(!string.IsNullOrEmpty(q.Answer.DownloadFilePath))
                        {
                            ReceiptPath=q.Answer.DownloadFilePath;
                        }
                        else
                        {
                            resume=false;
                            report=$"{report}\nФайл не получен";
                        }
                    }

                    if(resume) { 
                        try
                        {
                            BitmapImage image = new BitmapImage();
                            image.BeginInit();
                            image.UriSource = new Uri(ReceiptPath);
                            image.EndInit();          
                            
                            //ReceiptImage.Source=image;
                            
                            complete=true;
                        }
                        catch(Exception e)
                        {
                            resume=false;
                            report=$"{report}\nФормат не распознан";
                        }
                    }

                    if(complete)
                    { 
                        report=$"{report}\nЯрлык создан";
                        ReceiptComplete=true;
                    }
                    else
                    {
                        report=$"{report}\nОшибка при создании ярлыка";
                    }
                }

                ReceiptLog.Text=report;
                switch(method)
                {
                    case 1:
                        Show();
                        break;

                    case 2:

                        if(ReceiptComplete)
                        {
                            Splash.Visibility=Visibility.Visible;
                            Show();                            
                            Print();
                            Close();
                            Splash.Visibility=Visibility.Collapsed;
                        }
                        break;
                }

            }
            else
            {
                report=$"{report}\nОшибка создания ярлыка";
            }
            

            if(!string.IsNullOrEmpty(ReceiptPath))
            {
                OpenButton.IsEnabled=true;
            }
            else
            {
                OpenButton.IsEnabled=false;
            }
        }

        /// <summary>
        /// печать полотна с ярлыком
        /// </summary>
        public bool Print(bool immediate=false)
        {
            bool result=false;
            string report="";

            try
            {
                if(Printer==null)
                {
                    PrintDialog printDlg = new PrintDialog();  
                    Printer=printDlg;
                }

                if(immediate)
                {
                    Show();            
                    Printer.PrintDocument(ReceiptViewer.Document.DocumentPaginator,"");
                    Splash.Visibility=Visibility.Visible;
                    Close();                
                    Splash.Visibility=Visibility.Hidden;
                }
                else
                {
                    Printer.PrintDocument(ReceiptViewer.Document.DocumentPaginator,"");
                }

                result=true;
            }
            catch(Exception e)
            {
                report=$"{report}\n Не удалось напечатать.";
                report=$"{report}\n Возможно, принтер не настроен.";
                result=false;
            }

            if(!result)
            {
                var t=ReceiptLog.Text;
                t=$"{t}{report}";
                ReceiptLog.Text=t;
                Splash.Visibility=Visibility.Hidden;
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
            Printer=printDlg;
            Printer.ShowDialog();   
        }

        /// <summary>
        /// установка драйвера принтера
        /// </summary>
        public void Install()
        {
            if(!string.IsNullOrEmpty(DriverPath))
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
            Central.WM.Show($"ReceiptPrint","Печать",true,"add",this);
            Central.WM.SetActive($"ReceiptPrint",true);
        }
       
        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            Central.WM.Close($"ReceiptPrint");
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }


        private void PrintButton_Click(object sender,RoutedEventArgs e)
        {
            Print();
        }

        private void SaveButton_Click(object sender,RoutedEventArgs e)
        {

        }

        private void CloseButton_Click(object sender,RoutedEventArgs e)
        {
            Close();
        }

        private void OpenButton_Click(object sender,RoutedEventArgs e)
        {
            if(!string.IsNullOrEmpty(ReceiptPath))
            {
                Central.OpenFile(ReceiptPath);
            }
        }

        private void SetupButton_Click(object sender,RoutedEventArgs e)
        {
            Setup();
        }

        private void InstallButton_Click(object sender,RoutedEventArgs e)
        {
            Install();
        }

        private void RefreshButton_Click(object sender,RoutedEventArgs e)
        {
            Init();
        }
    }
}
