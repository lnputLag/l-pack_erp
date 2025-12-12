using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Service.Printing;
using Newtonsoft.Json;
using PdfiumViewer;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// блок "отгрузка"
    /// (план отгрузок)
    /// </summary>
    /// <author>balchugov_dv</author>   
    public partial class DiagramShipment : UserControl
    {
        public DiagramShipment(Dictionary<string, string> values, string roleName = "[erp]shipment_control")
        {
            InitializeComponent();
            Values = values;
            RoleName = roleName;
            Init();
            ProcessPermissions();
        }

        /// <summary>
        /// данные блока
        /// </summary>
        public Dictionary<string, string> Values { get; set; }

        public string RoleName { get; set; }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel(this.RoleName);
            var userAccessMode = mode;
            switch (mode)
            {
                case Role.AccessMode.Special:
                    break;

                case Role.AccessMode.FullAccess:
                    break;

                case Role.AccessMode.ReadOnly:
                default:
                    BindTerminalButton.IsEnabled = false;
                    UnbindTerminalButton.IsEnabled = false;
                    CommentButton.IsEnabled = false;
                    LateReasonButton.IsEnabled = false;
                    break;
            }
        }

        /// <summary>
        /// инициализация блока
        /// </summary>
        public void Init()
        {
            /*
                ShipmentDate    -- дата отгрузки
                ProductionType  -- тип продукции (0 - гофра, 2 - бумага)  
                BayerName       -- покупатель
                SelfShipment    -- самовывоз
                PackagingType   -- тип упаковки (1 - с/уп, 2 - б/уп, 3 - рулоны)
                TerminalNumber  -- номер терминала
                ShipmentInProgress -- отгрузка идет
                DriverName      -- имя водителя
                DriverInPlace   -- водитель приехал
                StatusId -- статус                         

                ShipmentInProgress==1 && TerminalNumber==0 
                    блок окрашиваем с зеленый цвет, 
                    статус не показывается
                    терминал не показывается
                ShipmentInProgress==1 && TerminalNumber!=0 
                    значит блок окрашиваем с синий цвет, 
                    статус не показывается, 
                    терминал показывается.
                ShipmentInProgress == 0 && TerminalNumber==0
                    блок не окрашивается, 
                    статус показывается.

                DriverInPlace == 1 
                    делаем водителя жирным шрифтом

                2020-05-12 UPD:
                    для белых отгрузок нужно показать процент готовности продукции
                    pcg_shipmentplan.f_get_shipment_stock_percent(t.idts)
                    Для этого перенесем логику определения цвета отгрузки в запрос.
                    =>
                    ColorStatus="green"|"blue"|"white"
                    StockPercent=(int) -- процент готовности продукции                                    
                        в правом нижнем углу
             */
                        
            Customer.Text = "";
            Driver.Text = "";
            Status.Text = "";
            Terminal.Text = "";
            TMass.Text = "";

            ForkFilt.Text = "";
            ForkFiltInfo.Text = "";

            Customer.Text = Values.CheckGet("BAYER_NAME");
            Driver.Text = Values.CheckGet("DRIVER_NAME").SurnameInitials();

            var colorStatus = Values.CheckGet("COLOR_STATUS");
            colorStatus = colorStatus.ToLower();
            colorStatus = colorStatus.Trim();
            
            string driverInPlace="";
            bool driverArrived=Values.CheckGet("DRIVER_IN_PLACE").ToBool();
            if (driverArrived)
            {
                driverInPlace = "приехал";
            }
            else
            {
                driverInPlace = "не приехал";
            }

            if (Values.CheckGet("SELF_SHIPMENT").ToInt() == 1)
            {
                Self.Text = "С";
                TSelfShipment.Text = "Да";
            }
            else
            {
                if (Values.CheckGet("PRODUCTION_TYPE").ToInt() == 8)
                {
                    Self.Text = "";
                    TSelfShipment.Text = "";
                }
                else
                {
                    Self.Text = "Д";
                    TSelfShipment.Text = "Нет";
                }
            }

            //tooltip
            TId.Text = Values.CheckGet("ID");
            TShipmentDate.Text = Values.CheckGet("SHIPMENT_DATE");

            /*
                ед. измер "массы":
                гофра:  12.3 тыс. кв. м
                рулон:  12.3 т
             */
            var mu = "";

            var t = "гофра";
            switch (Values.CheckGet("PRODUCTION_TYPE"))
            {
                case "0":
                    t = "изделия";
                    mu = "тыс. кв. м";
                    break;

                case "2":
                    t = "рулоны";
                    mu = "т";
                    break;

                case "8":
                    t = "тмц";
                    mu = "";
                    break;

            }
            TProductionType.Text = t;
            TBayerName.Text = Values.CheckGet("BAYER_NAME");

            var p = "";
            var p2="";
            var packingColor="#ff000000";
            switch (Values.CheckGet("PACKAGING_TYPE").ToInt())
            {
                case 1:
                    p = "с упаковкой";
                    p2="су";                                
                    break;

                case 2:
                    p = "без упаковки";
                    p2="бу";
                    packingColor=HColor.RedFG;
                    break;

                case 3:
                    p = "рулоны";
                    p2="рл";
                    break;

                case 0:
                    if (Values.CheckGet("PRODUCTION_TYPE").ToInt() == 8)
                    {
                        p = "тмц";
                        p2 = "т";
                    }
                    break;
            }
            TPackagingType.Text   = p;
            Packing.Text=p2;

            {
                var bc2 = new BrushConverter();
                var b2 = (Brush)bc2.ConvertFrom(packingColor);
                Packing.Foreground=b2;
            }
            
            if (Values.CheckGet("TERMINAL_NUMBER") != "0")
            {
                TTerminal.Text = Values.CheckGet("TERMINAL_NUMBER");
                TTerminal.Visibility = Visibility.Visible;
                LTerminal.Visibility = Visibility.Visible;
            }
            else
            {
                TTerminal.Visibility = Visibility.Collapsed;
                LTerminal.Visibility = Visibility.Collapsed;
            }

            if (!string.IsNullOrEmpty(Status.Text))
            {
                TStatus.Text = Values.CheckGet("STATUS");
                TStatus.Visibility = Visibility.Visible;
                LStatus.Visibility = Visibility.Visible;
            }
            else
            {
                TStatus.Visibility = Visibility.Collapsed;
                LStatus.Visibility = Visibility.Collapsed;
            }

            if (Central.DebugMode)
            {
                TStatus.Text = $"{Values.CheckGet("STATUS")} ({Values.CheckGet("STATUS_ID")})";
            }

            if (!string.IsNullOrEmpty(Values.CheckGet("DRIVER_NAME")))
            {
                TDriver.Text = $"{Values.CheckGet("DRIVER_NAME")}";
                TDriverArrive.Text=$"{Values.CheckGet("DRIVER_ARRIVE_DATETIME")}";
                
                TDriver.Visibility = Visibility.Visible;
                LDriver.Visibility = Visibility.Visible;
                TDriverArrive.Visibility = Visibility.Visible;
                LDriverArrive.Visibility = Visibility.Visible;
            }
            else
            {
                TDriver.Visibility = Visibility.Collapsed;
                LDriver.Visibility = Visibility.Collapsed;
                TDriverArrive.Visibility = Visibility.Collapsed;
                LDriverArrive.Visibility = Visibility.Collapsed;
            }

            
            int stockPercent = Values.CheckGet("STOCK_PERCENT").ToInt();
            if (stockPercent != 0)
            {
                TStockPercent.Visibility = Visibility.Visible;
                LStockPercent.Visibility = Visibility.Visible;
                TStockPercent.Text = $"{stockPercent} %";
            }
            else
            {
                TStockPercent.Visibility = Visibility.Collapsed;
                LStockPercent.Visibility = Visibility.Collapsed;
            }

            //если отгрузка готова к отгрузке, подкрасим строку статуса
            bool ready=false;
            if (Values.CheckGet("STATUS_ID").ToString().ToInt() == 5 && stockPercent>0 )
            {
                ready=true;
            }

            StatusContainer.Visibility=Visibility.Collapsed;
            TerminalContainer.Visibility=Visibility.Collapsed;

            //отгрузка готова и водитель приехал
            if(colorStatus != "orange")
            {
                if(ready && driverArrived)
                {
                    colorStatus="violet";
                }
            }
            
            //запрещена
            if(Values.CheckGet("FORBIDDEN").ToInt()==1)
            {
                colorStatus="gray";
            }

            //машина на терминале -> идет отгрузка
            if(Values.CheckGet("TERMINAL_NUMBER").ToInt()!=0)
            {
                colorStatus="blue";
            }

            // зеленый -- отгрузка совершена. Используется только для отгрузок на СОХ. У отгрузок на СОХ нет naklrashod, поэтому обычная проверка на отгрузку тут не сработает.
            // nrz.status = 4 => responsible_stock_status = 1 => зеленый(отгрузка совершена)
            if (Values.CheckGet("RESPONSIBLE_STOCK_STATUS").ToInt() == 1)
            {
                colorStatus = "green";
            }

            var bgColor = HColor.White;
            switch (colorStatus)
            {
                //отгружен
                case "green":
                    bgColor = HColor.Green;

                    DateTime date1 = new DateTime();

                    if(DateTime.TryParse(Values.CheckGet("SHIPMENT_BEGIN"), out date1))
                    {
                        Status.Text = date1.Day + "." + date1.Month.ToString("00") + " " + date1.Hour.ToString("00") + ":" + date1.Minute.ToString("00");
                        StatusContainer.Visibility = Visibility.Visible;
                    }

                    if (DateTime.TryParse(Values.CheckGet("SHIPMENT_END"), out date1))
                    {
                        Status.Text += "-" + date1.Hour.ToString("00") + ":" + date1.Minute.ToString("00");
                        StatusContainer.Visibility = Visibility.Visible;
                    }
                    break;

                //опоздавшая/перенесенная
                case "orange":
                    bgColor = HColor.Orange;

                    Status.Text = Values.CheckGet("STATUS");
                    StatusContainer.Visibility=Visibility.Visible;

                    Terminal.Text = $"{stockPercent}%";
                    Terminal.FontWeight = FontWeights.Bold;
                    TerminalContainer.Visibility=Visibility.Visible;

                    LShipmentDate.Visibility=Visibility.Collapsed;
                    TShipmentDate.Visibility=Visibility.Collapsed;
                    break;

                //опоздавшая/перенесенная
                case "yellow":
                    bgColor = HColor.Yellow;
                    
                    Status.Text = Values.CheckGet("STATUS");
                    StatusContainer.Visibility=Visibility.Visible;

                    Terminal.Text = $"{stockPercent}%";
                    Terminal.FontWeight = FontWeights.Bold;
                    TerminalContainer.Visibility=Visibility.Visible;

                    LShipmentDate.Visibility=Visibility.Collapsed;
                    TShipmentDate.Visibility=Visibility.Collapsed;
                    break;

                //отгружается
                case "blue":
                    bgColor = HColor.Blue;
                    Terminal.Text = $"{Values.CheckGet("STOCK_PERCENT")}% ";
                    Terminal.FontWeight = FontWeights.Bold;
                    
                    if (
                        Values.CheckGet("PACKAGING_TYPE") == "1"
                        || Values.CheckGet("PACKAGING_TYPE") == "2"
                    )
                    {
                        //для продукции
                        //для рулонов не применяем, там может быть номер вагона
                        ForkFilt.Text = Values.CheckGet("FORKLIFT_DRIVER_NAME").SurnameInitials();
                    }
                    else
                    {
                        ForkFilt.Text = Values.CheckGet("FORKLIFT_DRIVER_NAME");
                    }
                    
                    TForkliftDriver.Text=Values.CheckGet("FORKLIFT_DRIVER_NAME");
                    TForkliftDriverPhone.Text=Values.CheckGet("FORKLIFT_DRIVER_PHONE").CellPhone();
                    ForkFiltInfo.Text = "П";
                    

                    Status.Text = "Терминал";
                    StatusContainer.Visibility=Visibility.Visible;

                    Terminal.Text = $"{Values.CheckGet("TERMINAL_NUMBER")}";
                    Terminal.FontWeight = FontWeights.Bold;
                    TerminalContainer.Visibility=Visibility.Visible;

                    break;
                
                //запрещенная
                case "gray":
                    bgColor = HColor.Gray;

                    Status.Text = Values.CheckGet("STATUS");
                    StatusContainer.Visibility = Visibility.Visible;

                    Terminal.Text = $"{stockPercent}%";
                    Terminal.FontWeight = FontWeights.Bold;
                    TerminalContainer.Visibility = Visibility.Visible;
                    break;

                //готова к постановке на терминал
                case "violet":
                    bgColor = HColor.VioletPink;

                    Status.Text = Values.CheckGet("STATUS");
                    StatusContainer.Visibility=Visibility.Visible;

                    Terminal.Text = $"{stockPercent}%";
                    Terminal.FontWeight = FontWeights.Bold;
                    TerminalContainer.Visibility=Visibility.Visible;
                    break;

                //ждет своей очереди
                case "white":
                default:                                
                    Status.Text = Values.CheckGet("STATUS");
                    StatusContainer.Visibility=Visibility.Visible;

                    Terminal.Text = $"{stockPercent}%";
                    Terminal.FontWeight = FontWeights.Bold;
                    TerminalContainer.Visibility=Visibility.Visible;

                    break;
            }

            //дополнительные цветовые маркеры
            if(colorStatus!="blue" && colorStatus!="green")
            {
                //статус синий: уточните у мастера
                if (Values.CheckGet("STATUS_ID").ToString().ToInt() == 10)
                {
                    var bc1 = new BrushConverter();
                    var brush1 = (Brush)bc1.ConvertFrom(HColor.BlueFG);
                    Status.Foreground=brush1;
                    Terminal.Foreground=brush1;
                }

                //статус: фиолетовый
                //технологическая готовность к отгрузке
                if(ready)
                {
                    var bc1 = new BrushConverter();
                    var brush1 = (Brush)bc1.ConvertFrom(HColor.VioletPink);
                    StatusContainer.Background=brush1;
                    TerminalContainer.Background=brush1;
                }
            }

            if (Status.Text == "ПЗ")
            {
                Status.FontWeight = FontWeights.Black;
            }
            else
            {
                Status.FontWeight = FontWeights.Normal;
            }

            // Отгрузки, до срыва которых остался час подсвечиваем красным
            // Если это не отгрузка в рулонах, не опоздавшая и не перенесённая отгрузка и покупатель не ВАЙЛДБЕРРИЗ
            if (Values.CheckGet("PACKAGING_TYPE").ToInt() != 3 
                && Values.CheckGet("LATE_COMER").ToInt() != 1 
                && Values.CheckGet("NEXTDAY").ToInt() != 1
                && !Values.CheckGet("BAYER_NAME").Contains("ВАЙЛДБЕРРИЗ"))
            {
                // отгрузка не завершена, водитель на месте и водитель не опоздал
                if (colorStatus != "green" 
                    && Values.CheckGet("DRIVER_IN_PLACE").ToBool() 
                    && Values.CheckGet("SHIPMENT_DATE_TIME").ToDateTime("dd.MM.yyyy HH:mm:ss") >= Values.CheckGet("DRIVER_ARRIVE_DATETIME_FULL2").ToDateTime("dd.MM.yyyy HH:mm:ss"))
                {
                    // Если с начала отгрузки прошло 2 часа
                    if ((DateTime.Now - Values.CheckGet("SHIPMENT_DATE_TIME").ToDateTime("dd.MM.yyyy HH:mm:ss")).TotalHours >= 2)
                    {
                        // добавляем выделение красным цветом

                        var bc1 = new BrushConverter();
                        var brush1 = (Brush)bc1.ConvertFrom("#ff5050");
                        StatusContainer.Background = brush1;
                        TerminalContainer.Background = brush1;
                    }
                }
            }

            {
                //водитель: серый
                //водитель-перевозчик приехал
                if (!Driver.Text.IsNullOrEmpty())
                {
                    if (driverArrived)
                    {
                        var bc2 = new BrushConverter();
                        var b2 = (Brush)bc2.ConvertFrom("#FF777777");
                        Driver.Background = b2;

                        b2 = (Brush)bc2.ConvertFrom("#ffffffff");
                        Driver.Foreground = b2;
                    }
                }
            }

            // Флаг регистрации водителя через сайт
            {
                if (Values.CheckGet("REMOTE_REGISTRATION_FLAG").ToInt() > 0)
                {
                    RemoteRegistration.Text = "С";
                    RemoteRegistration.Background = "#FF9400D3".ToBrush();
                    RemoteRegistration.Foreground = "#ffffffff".ToBrush();
                    TRemoteRegistration.Text = "Да";

                    RemoteRegistration.Visibility = Visibility.Visible;
                }
                else
                {
                    RemoteRegistration.Text = "";
                    RemoteRegistration.Background = "#00ffffff".ToBrush();
                    TRemoteRegistration.Text = "";
                    
                    RemoteRegistration.Visibility = Visibility.Collapsed;
                }
            }

            if (Values.CheckGet("MASS").ToInt()>0)
            {
                TMass.Text=Values.CheckGet("MASS").ToInt()+" кв.м.";
            }

            {
                var bc2 = new BrushConverter();
                CarSideLoad.Text="";
                TSideLoad.Text="";
                CarSideLoad.Background=(Brush)bc2.ConvertFrom("#00ffffff");
                if( Values.CheckGet("SIDE_LOADING").ToBool() )
                {
                    CarSideLoad.Text="Б";
                    TSideLoad.Text="Да";
                    CarSideLoad.Background=(Brush)bc2.ConvertFrom("#FF777777");
                    CarSideLoad.Foreground=(Brush)bc2.ConvertFrom("#FFffffff");
                }
            }

            TComment.Text = Values.CheckGet("COMMENTS");
            
            Margin = new Thickness(0, 1, 0, 0);

            var bc = new BrushConverter();
            var brush = (Brush)bc.ConvertFrom(bgColor);
            Background = brush;

            // Добавил поле STOCK_ZONE
            // возвращает У, П или ''(пусто)
            if (Values.CheckGet("STOCK_ZONE") !=string.Empty)
            {
                Terminal.Text += Values.CheckGet("STOCK_ZONE");
                // не помещается, чуть сдвинем
                Terminal.Margin = new Thickness(-10, 0, 0, 0);
            }

            if(ForkFilt.Text == string.Empty)
            {
                DateTime date1 = new DateTime();

                if (DateTime.TryParse(Values.CheckGet("DTTM_PZ_END"), out date1))
                {
                    ForkFilt.Text = date1.Day + "." + date1.Month.ToString("00") + " " + date1.Hour.ToString("00") + ":" + date1.Minute.ToString("00");
                }

                if(ForkFilt.Text != string.Empty )
                {
                    ForkFilt.Text = "ПЗ " + ForkFilt.Text;
                    ForkFilt.Margin = new Thickness(-12, 0, 0, 0);
                }
            }
        }

        /// <summary>
        /// Порядок загрузки
        /// </summary>
        private void LoadingOrderNew()
        {
            var id = Values.CheckGet("ID").ToInt();
            if (id != 0)
            {
               var shipment=new Shipment(id);
                shipment.ShowLoadingScheme(Values);
            }
        }

        //доверенность
        private void PrintProxy(bool print = false)
        {
            var id = Values.CheckGet("ID").ToInt();
            if (id != 0)
            {
                var reporter = new ShipmentReport(id);
                reporter.PrintProxy(print);
            }
        }

        //загрузочная карта водителя
        private void PrintBootcard(bool print = false)
        {
            var id = Values.CheckGet("ID").ToInt();
            if (id != 0)
            {
                var reporter = new ShipmentReport(id);
                reporter.PrintBootcard(print);
            }
        }

        //задание на отгрузку
        private void PrintShipmenttask(bool print = false)
        {
            var id = Values.CheckGet("ID").ToInt();
            if (id != 0)
            {
                var reporter = new ShipmentReport(id);
                reporter.PrintShipmenttask(print);
            }
        }

        //карта склада
        private void PrintStockmap(bool print = false)
        {
            var id = Values.CheckGet("ID").ToInt();
            if (id != 0)
            {
                var reporter = new ShipmentReport(id);
                reporter.PrintStockmap(print);
            }
        }

        //карта проезда
        private void PrintRoutemap(bool print = false)
        {
            var id = Values.CheckGet("ID").ToInt();
            if (id != 0)
            {
                var reporter = new ShipmentReport(id);
                reporter.PrintRoutemap(print);
            }
        }


        private void MonBlock_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //проверка разрешений для отображения пунктов контекстного меню

            BindTerminalButton.IsEnabled = false;
            UnbindTerminalButton.IsEnabled = false;
            InfoOrderButton.IsEnabled = false;
            LoadingOrderButton.IsEnabled = false;
            PrintMenuButton.IsEnabled = false;

            //не запрещена и не на терминале
            if (
                (
                    Values["COLOR_STATUS"] == "white"
                    || Values["COLOR_STATUS"] == "orange"
                    || Values["COLOR_STATUS"] == "yellow"
                )
                && Values.CheckGet("FORBIDDEN").ToInt() != 1
                && Values.CheckGet("TerminalNumber").ToInt() == 0
                && !string.IsNullOrEmpty(Values.CheckGet("DRIVER_ARRIVE_DATETIME"))
            )
            {
                BindTerminalButton.IsEnabled = true;
            }

            if (Values.CheckGet("TERMINAL_NUMBER").ToInt() != 0)
            {
                UnbindTerminalButton.IsEnabled = true;
            }

            if (!(Values.CheckGet("PRODUCTION_TYPE").ToInt() == 8))
            {
                InfoOrderButton.IsEnabled = true;
                LoadingOrderButton.IsEnabled = true;
                PrintMenuButton.IsEnabled = true;
            }

            ProcessPermissions();
        }

        private void ContextMenu_BindTerminal_Click(object sender, RoutedEventArgs e)
        {
            var item = (MenuItem)sender;

            var bindTerminal = new BindTerminal();
            bindTerminal.ShipmentId = Values["ID"].ToInt();
            bindTerminal.ShipmentType = Values.CheckGet("PRODUCTION_TYPE").ToInt();
            bindTerminal.Edit();
        }

        public void SetComment()
        {
            var id=Values["ID"].ToInt();
            var h=new ShipmentComment();
            h.Edit(id);
        }
        
        public void SetLateReason()
        {
            var id=Values["ID"].ToInt();
            var h=new ShipmentReasonOfLateness();
            h.Edit(id);
        }
        
        private void ContextMenu_UnbindTerminal_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var h=new BindTerminal();

            if (!Values.ContainsKey("TRANSPORT_ID"))
            {
                Values.Add("TRANSPORT_ID", Values.CheckGet("ID"));
            }

            h.Unbind(Values);
        }

        private void ContextMenu_LoadingOrder_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //LoadingOrder();
            LoadingOrderNew();
        }

        private void PrintProxyDocsButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            PrintProxy();
        }

        private void PrintDriverBootCardButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            PrintBootcard();
        }

        private void PrintShipmentOrderBootCardButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            PrintShipmenttask();
        }

        private void PrintMapButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            PrintStockmap();
        }

        private void PrintRouteMapsButton_Click(object sender, RoutedEventArgs e)
        {
            PrintRoutemap();
        }

        private void ShowAll(bool print = false)
        {
            PrintProxy(print);
            PrintBootcard(print);
            PrintShipmenttask(print);
            PrintStockmap(print);
            PrintRoutemap(print);
        }

        private void PrintAllButton_Click(object sender, RoutedEventArgs e)
        {
            ShowAll(true);
        }
        
        private void ContextMenu_Comment_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetComment();
        }
        
        private void ContextMenu_LateReason_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetLateReason();
        }

        private void InfoOrderButton_Click(object sender, RoutedEventArgs e)
        {
            var h = new ShipmentInformation();
            h.Id = Values["ID"].ToInt();
            h.Init();
            h.Open();
        }

        private async void PrintShipInfo_Click(object sender, RoutedEventArgs e)
        {
            var p = new Dictionary<string, string>();

            p.Add("SHIPMENT_ID", Values["ID"]);
            p.Add("FORMAT", "pdf");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Position");
            q.Request.SetParam("Action", "GetShipmentReport");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                if (q.Answer.Type == LPackClientAnswer.AnswerTypeRef.File)
                {
                    var printHelper = new PrintHelper();
                    printHelper.PrintingProfile = PrintingSettings.DocumentPrinter.ProfileName;
                    printHelper.PrintingCopies = 1;
                    printHelper.PrintingLandscape = true;
                    printHelper.Init();
                    var printingResult = printHelper.StartPrinting(q.Answer.DownloadFilePath);
                    printHelper.Dispose();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        private void ShowAllButton_Click(object sender, RoutedEventArgs e)
        {
            ShowAll(false);
        }
    }
}
