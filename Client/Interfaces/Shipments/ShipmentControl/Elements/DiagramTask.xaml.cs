using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Client.Assets.HighLighters;
using System;
using NPOI.SS.Formula.Functions;
using Newtonsoft.Json;
using System.Linq;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// блок "задание погрузчика"
    /// (план отгрузок)
    /// </summary>
    /// <author>balchugov_dv</author>   
    public partial class DiagramTask : UserControl
    {
        /// <summary>
        /// блок "задание погрузчика"
        /// </summary>
        /// <param name="values"></param>
        public DiagramTask(Dictionary<string, string> values, string roleName = "[erp]shipment_control")
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

        /// <summary>
        /// время в минутах, по истечении которого блок с прогрессом загрузки 
        /// подкрашивается красным, если ни один поддон так и не отгрузили
        /// </summary>
        private int DownTime { get; set; }
        /// <summary>
        /// время в минутах, по истечении которого блок с прогрессом загрузки 
        /// подкрашивается красным, если не загружали поддоны за это время
        /// </summary>
        private int LastPalletTime { get; set; }

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
                    ShowAllPalletFlagButton.IsEnabled = false;
                    ShowAllPalletFlagButton.Visibility = Visibility.Collapsed;
                    ShowIncompletePalletFlagButton.IsEnabled = false;
                    ShowIncompletePalletFlagButton.Visibility = Visibility.Collapsed;
                    break;

                case Role.AccessMode.ReadOnly:
                default:
                    UnbindTerminalButton.IsEnabled = false;
                    ReassignLoaderButton.IsEnabled = false;
                    ShowAllPalletFlagButton.IsEnabled = false;
                    ShowAllPalletFlagButton.Visibility = Visibility.Collapsed;
                    ShowIncompletePalletFlagButton.IsEnabled = false;
                    ShowIncompletePalletFlagButton.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        /// <summary>
        /// инициализация блока
        /// </summary>
        public void Init()
        {
            DownTime = 10;
            LastPalletTime=20;

            //блок
            BayerName.Text = Values.CheckGet("BAYER_NAME");
            DriverName.Text = Values.CheckGet("DRIVER_NAME").SurnameInitials();
            Progress.Text = $"{Values.CheckGet("LOADED").ToInt()}/{Values.CheckGet("FOR_LOADING").ToInt()}";
            TerminalNumber.Text = Values.CheckGet("TERMINAL_NUMBER");

            //тултип
            TId.Text = Values.CheckGet("ID");
            TShipmentStart.Text = Values.CheckGet("SHIPMENT_BEGIN");
            TShipmentFinish.Text = Values.CheckGet("SHIPMENT_END");
            TLastPallet.Text = Values.CheckGet("LAST_PALLET_LOADED");
            TBayerName.Text = Values.CheckGet("BAYER_NAME");
            TTerminal.Text = Values.CheckGet("TERMINAL_NUMBER");

            TProductionType.Text = "";
            switch (Values.CheckGet("PRODUCTION_TYPE").ToInt())
            {
                default:
                    TProductionType.Text = "изделия";
                    break;

                case 2:
                    TProductionType.Text = "рулоны";
                    break;

                case 8:
                    TProductionType.Text = "тмц";
                    break;

            }

            TShipmentType.Text = "";
            switch (Values.CheckGet("SELF_SHIPMENT").ToInt())
            {
                case 0:
                default:
                    if (Values.CheckGet("PRODUCTION_TYPE").ToInt() == 8)
                    {
                        TShipmentType.Text = "";
                    }
                    else
                    {
                        TShipmentType.Text = "нет";
                    }
                    break;

                case 1:
                    TShipmentType.Text = "да";
                    break;

            }

            TPackaging.Text = "";
            switch (Values.CheckGet("PACKAGING_TYPE").ToInt())
            {
                case 1:
                    TPackaging.Text = "изделия, паллеты";
                    Packaging.Text = "ПАЛ";
                    break;

                case 2:
                    TPackaging.Text = "изделия, россыпью";
                    Packaging.Text = "РОС";
                    break;

                case 3:
                    TPackaging.Text = "рулоны";
                    Packaging.Text = "РУЛ";
                    break;

                case 0:
                    if (Values.CheckGet("PRODUCTION_TYPE").ToInt() == 8)
                    {
                        TPackaging.Text = "тмц";
                        Packaging.Text = "ТМЦ";
                    }
                    break;
            }

            TDriver.Text = $"{Values.CheckGet("DRIVER_NAME")}";
            TProgress.Text = $"{Values.CheckGet("LOADED").ToInt()}/{Values.CheckGet("FOR_LOADING").ToInt()}";

            string tShowAllPalletFlag = "Нет";
            string showAllPalletFlag = "";
            if (Values.CheckGet("SHOW_ALL_PALLET_FLAG").ToInt() == 1)
            {
                tShowAllPalletFlag = "Да";
                showAllPalletFlag = "Все";
            }
            else if (Values.CheckGet("SHOW_ALL_PALLET_FLAG").ToInt() == 2)
            {
                tShowAllPalletFlag = "Неполные";
                showAllPalletFlag = "Непол";
            }
            TShowAllPalletFlag.Text = $"{tShowAllPalletFlag}";
            ShowAllPalletFlagTextBox.Text = showAllPalletFlag;

            string tShipmentBloskedFlag = "Нет";
            if (Values.CheckGet("SHPMENT_BLOCKED_FLAG").ToInt() > 0)
            {
                tShipmentBloskedFlag = "Да";
            }
            TShipmentBloskedFlag.Text = tShipmentBloskedFlag;

            //отладочная информация
            if (Central.DebugMode)
            {
                TDebug.Text = "";
                TDebug.Visibility = Visibility.Visible;
            }
            else
            {
                TDebug.Visibility = Visibility.Collapsed;
            }

            TShipmentFinish.Visibility = Visibility.Collapsed;
            LShipmentFinish.Visibility = Visibility.Collapsed;

            {
                string bgColor;
                if (Values.CheckGet("SHPMENT_BLOCKED_FLAG").ToInt() > 0)
                {
                    bgColor = HColor.Red;
                }
                else
                {
                    switch (Values.CheckGet("STATUS").ToInt())
                    {
                        default:
                            bgColor = HColor.White;
                            TStatus.Text = "внешние операции";
                            break;

                        case 2:
                            bgColor = HColor.Blue;
                            TStatus.Text = "отгрузка";

                            SetAdditionalBackgroundCollor();

                            break;

                        case 3:
                            bgColor = HColor.Green;
                            TStatus.Text = "отгружено";
                            TShipmentFinish.Visibility = Visibility.Visible;
                            LShipmentFinish.Visibility = Visibility.Visible;
                            break;
                    }
                }

                var bc = new BrushConverter();
                var brush = (Brush)bc.ConvertFrom(bgColor);
                Background = brush;
            }
            
            UpdateActions();
        }

        /// <summary>
        /// Дополнительные цветовые маркеры
        /// </summary>
        public void SetAdditionalBackgroundCollor()
        {
            var timeNow = System.DateTime.Now;

            // Отгрузки, до срыва которых остался час подсвечиваем красным
            {
                // Если это не отгрузка в рулонах и покупатель не ВАЙЛДБЕРРИЗ
                if (Values.CheckGet("PACKAGING_TYPE").ToInt() != 3
                    && !Values.CheckGet("BAYER_NAME").Contains("ВАЙЛДБЕРРИЗ"))
                {
                    // Не проверяем факт приезда водителя, так как если погрузчик взял  задачу на эту отгрузку, то машина уже должна быть на месте

                    // Если водитель не опоздал
                    if (Values.CheckGet("SHIPMENT_DATE_TIME").ToDateTime("dd.MM.yyyy HH:mm:ss") >= Values.CheckGet("DRIVER_ARRIVE_DATETIME_FULL2").ToDateTime("dd.MM.yyyy HH:mm:ss"))
                    {
                        // Если с начала отгрузки прошло 2 часа
                        if ((DateTime.Now - Values.CheckGet("SHIPMENT_DATE_TIME").ToDateTime("dd.MM.yyyy HH:mm:ss")).TotalHours >= 2)
                        {
                            // добавляем выделение красным цветом

                            var bc1 = new BrushConverter();
                            Packaging.Background = (Brush)bc1.ConvertFrom("#ff5050");
                        }
                    }
                }                
            }

            // подкраска блока "прогресс отгузки"
            {
                var makeRed = false;

                /// если с того момента, как началась отгрузка прошло более лимита
                /// а количество отгруженных поддонов так и не стало больше 0, 
                /// подкрасим блок
                {
                    var shipmentBegin = Values.CheckGet("SHIPMENT_BEGIN").ToDateTime();
                    if (
                        (timeNow - shipmentBegin).TotalMinutes >= DownTime
                        && Values.CheckGet("LOADED").ToInt() == 0
                    )
                    {
                        makeRed = true;
                    }
                }

                // если не завозили поддоны дольше лимита, подкрасим блок
                {
                    //время последнего поддона задано
                    if (!Values.CheckGet("LAST_PALLET_LOADED").IsNullOrEmpty())
                    {
                        var lastPallet = Values.CheckGet("LAST_PALLET_LOADED").ToDateTime();
                        if ((timeNow - lastPallet).TotalMinutes >= LastPalletTime)
                        {
                            makeRed = true;
                        }
                    }
                }

                if (makeRed)
                {
                    var bc1 = new BrushConverter();
                    Progress.Background = (Brush)bc1.ConvertFrom(HColor.Red);
                }
            }
        }

        public void UpdateActions()
        {
            // пункты контекстного меню
            UnbindTerminalButton.IsEnabled = true;
            ReassignLoaderButton.IsEnabled = true;

            ShowAllPalletFlagButton.IsEnabled = false;
            if (Values.CheckGet("SHOW_ALL_PALLET_FLAG").ToInt() != 1)
            {
                ShowAllPalletFlagButton.IsEnabled = true;
            }

            ShowIncompletePalletFlagButton.IsEnabled = false;
            if (Values.CheckGet("SHOW_ALL_PALLET_FLAG").ToInt() != 2)
            {
                ShowIncompletePalletFlagButton.IsEnabled = true;
            }

            RemoveShipmentBlockFlagButton.IsEnabled = false;
            if (Values.CheckGet("SHPMENT_BLOCKED_FLAG").ToInt() > 0)
            {
                RemoveShipmentBlockFlagButton.IsEnabled = true;
            }

            ProcessPermissions();
        }
        
        private void AssignForkliftdriver()
        {
            var resume = true;

            if(Values.CheckGet("ID").ToInt()==0)
            {
                resume=false;
            }
            
            if(resume)
            {
                var bindForklift=new AssignForkliftdriver();
                bindForklift.ShipmentId=Values.CheckGet("ID").ToInt();
                bindForklift.Edit();
            }
        }

        private async void SetShowPalletFlag(int type)
        {
            var p = new Dictionary<string, string>();
            {
                p.Add("TERMINAL_ID", Values.CheckGet("TERMINAL_ID"));
                p.Add("SHOW_ALL_PALLET_FLAG", $"{type}");
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Terminal");
            q.Request.SetParam("Action", "UpdateShowAllPalletFlag");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        if (ds.Items.First().CheckGet("TERMINAL_ID").ToInt() > 0)
                        {
                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "ShipmentControl",
                                SenderName = "DiagramTask",
                                ReceiverName = "DriverList",
                                Action = "Refresh",
                                Message = "",
                            });

                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "ShipmentKshControl",
                                ReceiverName = "ShipmentKshPlan",
                                SenderName = "DiagramTask",
                                Action = "RefreshDrivers",
                                Message = "",
                            });
                        }
                    }
                }
            }
        }

        private async void RemoveShipmentBlockFlag()
        {
            var p = new Dictionary<string, string>();
            {
                p.Add("TERMINAL_ID", Values.CheckGet("TERMINAL_ID"));
                p.Add("SHPMENT_BLOCKED_FLAG", "0");
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Terminal");
            q.Request.SetParam("Action", "SetShpmentBlockedFlag");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        if (ds.Items.First().CheckGet("TERMINAL_ID").ToInt() > 0)
                        {
                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "ShipmentControl",
                                SenderName = "DiagramTask",
                                ReceiverName = "DriverList",
                                Action = "Refresh",
                                Message = "",
                            });

                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "ShipmentKshControl",
                                ReceiverName = "ShipmentKshPlan",
                                SenderName = "DiagramTask",
                                Action = "RefreshDrivers",
                                Message = "",
                            });
                        }
                    }
                }
            }
        }

        private void ContextMenu_UnbindTerminal_Click(object sender, RoutedEventArgs e)
        {
            var h=new BindTerminal();

            if (!Values.ContainsKey("TRANSPORT_ID"))
            {
                Values.Add("TRANSPORT_ID", Values.CheckGet("ID"));
            }

            h.Unbind(Values);
        }
        
        private void ContextMenu_ReassignLoader_Click(object sender,RoutedEventArgs e)
        {
            AssignForkliftdriver();
        }

        private void ShowAllPalletFlagButton_Click(object sender, RoutedEventArgs e)
        {
            SetShowPalletFlag(1);
        }

        private void ShowIncompletePalletFlagButton_Click(object sender, RoutedEventArgs e)
        {
            SetShowPalletFlag(2);
        }

        private void RemoveShipmentBlockFlagButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveShipmentBlockFlag();
        }
    }
}
