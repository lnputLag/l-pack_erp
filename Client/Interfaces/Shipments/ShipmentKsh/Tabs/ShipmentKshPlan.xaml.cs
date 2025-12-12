using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Main.Controls.Tabs;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Управление отгрузками, вкладка "План" площадки Кашира 
    /// </summary>
    public partial class ShipmentKshPlan : UserControl
    {
        public ShipmentKshPlan()
        {
            InitializeComponent();

            AutoUpdateInterval = (int)(60*2);
            ItemsAutoUpdate=false;
            FirstLoad=false;

            ShipmentsStartTime = 8;
            HideComplete = false;
            Profiler = new Profiler();

            ProcessPermissions();
            SetDefaults();
            Init();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, _ProcessMessages);
            Central.Msg.Register(ProcessMessages);

            Loaded += ShipmentPlan_Loaded;
        }

        /// <summary>
        /// интервал автообновления грида
        /// 0- автообновление отключено
        /// (по таймеру будет вызвана коллбэк-функция DoLoadItems
        /// </summary>
        public int AutoUpdateInterval { get; set; }
        public DispatcherTimer AutoUpdateTimer { get; set; }
        private bool ItemsAutoUpdate {get;set;}

        /// <summary>
        /// профайлер
        /// </summary>
        public Profiler Profiler { get; set; }
        
        public bool HideComplete { get; set; }
        
        /// <summary>
        /// датасеты
        /// </summary>
        public ListDataSet ShipmentsDs { get; set; }

        public ListDataSet TimesDS { get; set; }
        public ListDataSet DriversDs { get; set; }
        public ListDataSet DriversTasksDs { get; set; }
        /// <summary>
        /// время, начало временной шкалы для диаграммы отгрузок
        /// </summary>
        private int ShipmentsStartTime { get; set; }
        private bool FirstLoad {get;set;}
        private bool FirstOnLoaded { get; set; } = true;

        public string RoleName = "[erp]shipment_control_ksh";

        /// <summary>
        /// Идентификатор площадки
        /// </summary>
        private int FactoryId = 2;

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
                    break;
            }

            List<Button> buttons = UIUtil.GetVisualChilds<Button>(this.Content as DependencyObject);
            if (buttons != null && buttons.Count > 0)
            {
                foreach (var button in buttons)
                {
                    var buttonTagList = UIUtil.GetTagList(button);
                    var accessMode = Acl.FindTagAccessMode(buttonTagList);
                    if (accessMode > userAccessMode)
                    {
                        button.IsEnabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о фрейма
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "ShipmentKshControl",
                ReceiverName = "",
                SenderName = "ShipmentKshPlan",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
            Central.Msg.UnRegister(ProcessMessages);

            StopAutoUpdateTimer();
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Today.Text = DateTime.Now.ToString("dd.MM.yyyy");
            HideCompleteCheckbox.IsChecked = false;
            HideInCompleteCheckbox.IsChecked = true;
        }

        private void ShipmentPlan_Loaded(object sender, RoutedEventArgs e)
        {
            // FIXME по каким то причинам FocusGot вызывается не всегда
            // OnLoad вызывается всегда при показн контрола,
            // при этом первый раз мы пропустим 

            if (FirstOnLoaded)
            {
                // контрол только что создан и добавлен, отображать данные не требуется
                FirstOnLoaded = false;
            }
            else
            {
                // контрол стал активным, загружаем данные как по FocusGot
                ItemsAutoUpdate = true;
                if (!FirstLoad)
                {
                    LoadItems();
                }
            }
        }

        /// <summary>
        /// обработка сообщений
        /// </summary>
        /// <param name="message"></param>
        public void ProcessMessages(ItemMessage message)
        {
            if(message!=null)
            {
                if(
                    message.SenderName == "WindowManager"
                    && message.ReceiverName == "ShipmentKshPlan"
                )
                {
                    switch (message.Action)
                    {
                        //case "FocusGot":
                        //    ItemsAutoUpdate = true;
                        //    if (!FirstLoad)
                        //    {
                        //        LoadItems();
                        //    }
                        //    break;

                        case "FocusLost":
                            ItemsAutoUpdate=false;
                            break;
                    }
                }
            }
        }

       

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        private void _ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.IndexOf("ShipmentKshControl") > -1)
            {
                if (m.ReceiverName.IndexOf("ShipmentKshPlan") > -1)
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            ShipmentsLoadItems();
                            DriversLoadItems();
                            break;

                        case "RefreshDrivers":
                            DriversLoadItems();
                            break;

                        case "HideInComplete":
                            HideInCompleteCheckbox.IsChecked = true;
                            ShipmentsUpdateItems();
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e=Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.F5:
                    ShipmentsLoadItems();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// отображение статьи в справочной системе
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/shipments/control/plan");
        }

        /// <summary>
        /// обработка запросов системы навигации
        /// </summary>
        public void ProcessNavigation()
        {
            
        }
        
        /// <summary>
        /// инициализация интерфейса
        /// </summary>
        public void Init()
        {
            Run();
        }
        
        /// <summary>
        /// запуск интерфейса в работу
        /// </summary>
        public void Run()
        {
            RunAutoUpdateTimer();
        }

        public void LoadItems()
        {
            if(ItemsAutoUpdate)
            {
                FirstLoad=true;

                ShipmentsLoadItems();
                DriversLoadItems();
            }
        }

        public void DisableControls()
        {
            ShipmentsToolbar.IsEnabled = false;
            ShipmentsSplash.Visibility = Visibility.Visible;
            Mouse.OverrideCursor = Cursors.Wait;
        }

        public void EnableControls() 
        {
            ShipmentsToolbar.IsEnabled = true;
            ShipmentsSplash.Visibility = Visibility.Collapsed;
            Mouse.OverrideCursor = null;
        }
        
        /// <summary>
        /// получение данных
        /// </summary>
        public async void ShipmentsLoadItems()
        {
            DisableControls();

            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("TODAY", Today.Text);
                    p.Add("FACTORY_ID", $"{FactoryId}");
                }
                
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments/ShipmentKsh");
                q.Request.SetParam("Object", "Shipment");
                q.Request.SetParam("Action", "ListPlan");
                
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if(q.Answer.Status == 0)                
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                    if(result!=null)
                    {
                        ShipmentsDs = ListDataSet.Create(result, "ITEMS");
                        TimesDS = ListDataSet.Create(result, "TIMES");
                        ShipmentsUpdateItems();
                    }
                }
            }

            EnableControls();
        }

        /// <summary>
        /// фильтрация данных
        /// Запрос бьл изменен так что в данных есть данные как за заданную дату так и за следующий день
        /// В фильтре кроме стандартной фильтрации есть выбор данных за заданную дату nextday = 0
        /// И за следующий день nextday = 1
        /// </summary>
        public List<Dictionary<string,string>> ShipmentsFilterItems(int nextday=0)
        {
            var result = new List<Dictionary<string,string>>();
            
            var hideComplete = (bool)HideCompleteCheckbox.IsChecked;

            var hidePackaging = (bool)HidePackagingCheckbox.IsChecked;

            var hidePlacer = (bool)HidePlacerCheckbox.IsChecked;

            if (ShipmentsDs!=null)
            {
                var items=new List<Dictionary<string, string>>();
                bool doFiltering = false;

                if (hideComplete || hidePackaging || hidePlacer)
                {
                    doFiltering=true;
                }

                List<Dictionary<string, string>> ShipmentsDs_Items = new List<Dictionary<string, string>>();

                foreach(var item in ShipmentsDs.Items)
                {
                    if(item.CheckGet("NEXTDAY").ToInt()==nextday)
                    {
                        ShipmentsDs_Items.Add(item);
                    }
                }

                if (doFiltering)
                {
                    foreach (Dictionary<string, string> row in ShipmentsDs_Items)
                    {
                        bool include = true;

                        if (hideComplete)
                        {
                            if (row.CheckGet("COLOR_STATUS")=="green")
                            {
                                include = false;
                            }
                        }

                        if (hidePackaging)
                        {
                            if (row.CheckGet("PACKAGING_TYPE").ToInt() == 1)
                            {
                                include = false;
                            }
                        }

                        if (hidePlacer)
                        {
                            if (row.CheckGet("PACKAGING_TYPE").ToInt() == 2)
                            {
                                include = false;
                            }
                        }

                        if (include)
                        {
                            items.Add(row);
                        }
                    }
                }
                else
                {
                    items = new List<Dictionary<string, string>>(ShipmentsDs_Items);
                }

                result = items;
            }

            return result;
        }
        
        /// <summary>
        /// рендер грида
        /// </summary>
        public void ShipmentsUpdateItems()
        {
            DisableControls();
            
            int lateCnt = 0;
            
            var items=ShipmentsFilterItems();
            ShipmentsClearItems();

            /*
                ---------------------------------------------------------------------
                |           | опоздавшие отгрузки                                   |
                ---------------------------------------------------------------------
                |           | timeline11                                            |
                              latecomerShipments
                ---------------------------------------------------------------------
                |           | отгрузки                                              |
                ---------------------------------------------------------------------
                | timeline0 | timeline21                                            |
                |             goodsShipments                                        |
                ---------------------------------------------------------------------
             */

            var baseDay =Today.Text.ToDateTime().ToString("dd").ToInt();
            var baseDateTime=Today.Text.ToDateTime();
            
            var labelTimeline=new ShipmentTimeline();
            labelTimeline.Container = MonitorTimeLabels;
            labelTimeline.ColumnWidth = 50;
            labelTimeline.StartHour = ShipmentsStartTime;
            labelTimeline.Init();
            
            var latecomerTimeline=new ShipmentTimeline();
            latecomerTimeline.RowStyle = "";
            latecomerTimeline.Container = MonitorContainer11;
            latecomerTimeline.StartHour = ShipmentsStartTime;
            latecomerTimeline.BaseDay = baseDay;
            latecomerTimeline.BaseDateTime=baseDateTime;
            latecomerTimeline.Init();
            
            var goodsTimeline=new ShipmentTimeline();
            goodsTimeline.Container = MonitorContainer21;
            goodsTimeline.StartHour = ShipmentsStartTime;
            goodsTimeline.BaseDay = baseDay;
            goodsTimeline.BaseDateTime=baseDateTime;
            goodsTimeline.Init();
            
            Dictionary<int, int> shipmentBlockByHour = new Dictionary<int, int>();
            
            //даныне отгрузок
            if (items.Count>0)
            {
                foreach (var row in items)
                {
                    /*
                        1 -- с упаковкой
                        2 -- без упаковки
                    */

                    int finished = row.CheckGet("FINISHED").ToInt();
                    int packagingType = row.CheckGet("PACKAGING_TYPE").ToInt();

                    if(finished==1 || HideInCompleteCheckbox.IsChecked==false)
                    {
                        var block = new DiagramShipment(row, this.RoleName);
                        var item = new TimelineItem()
                        {
                            Values = row,
                            Object = block,
                        };

                        if (row.CheckGet("LATE_COMER").ToInt() == 1)
                        {
                            //опоздавшие и перенесенные

                            latecomerTimeline.AddItem(item);
                            lateCnt++;
                        }
                        else
                        {
                            //все остальные
                            int hour = DateTime.Parse(row.CheckGet("SHIPMENT_DATE")).Hour;

                            var dateTimeNow = DateTime.Now.ToString("dd.MM.yyyy HH").ToDateTime("dd.MM.yyyy HH");
                            var shipmentDateTime = row.CheckGet("SHIPMENT_DATE_TIME").ToDateTime("dd.MM.yyyy HH:mm:ss").ToString("dd.MM.yyyy HH").ToDateTime("dd.MM.yyyy HH");
                            // Если машина из будущего времени уже отгружена, то не учитываем её
                            if (!(shipmentDateTime > dateTimeNow && row.CheckGet("COLOR_STATUS") == "green"))
                            {
                                if (shipmentBlockByHour.ContainsKey(hour))
                                {
                                    shipmentBlockByHour[hour]++;
                                }
                                else
                                {
                                    shipmentBlockByHour[hour] = 1;
                                }
                            }

                            goodsTimeline.AddItem(item);
                        }
                    }
                }
            }

            //ось ординат: время
            {
                Dictionary<string, string> times = new Dictionary<string, string>();
                if (TimesDS != null && TimesDS.Items != null && TimesDS.Items.Count > 0)
                {
                    foreach (var item in TimesDS.Items) 
                    {
                        times.Add(item.CheckGet("HOUR"), $"{item.CheckGet("LIMIT").ToInt()}");
                    }
                }

                int t = ShipmentsStartTime;
                for (int i = 0; i <= 24; i++)
                {
                    if (t == 24)
                    {
                        t = 0;
                    }

                    var ts = $"{t}";
                    if (t < 10)
                    {
                        ts = $"0{t}";
                    }
                    
                    var row = new Dictionary<string, string>()
                    {
                        {"LABEL", $"{ts}:00" }, 
                        {"BLOCK", shipmentBlockByHour.ContainsKey(t)  ? shipmentBlockByHour[t].ToString() : "0"  }
                    };

                    t++;

                    var block = new DiagramShipmentLabel(row, this.FactoryId, times);
                    var item = new TimelineItem()
                    {
                        Values = row,
                        Object = block,
                    };

                    labelTimeline.AddItem(item);
                }
            }

            labelTimeline.PrepareItems();
            latecomerTimeline.PrepareItems();
            goodsTimeline.PrepareItems();

            labelTimeline.RenderItems();
            latecomerTimeline.RenderItems();

            goodsTimeline.ColumnIndexMax += 1;
            goodsTimeline.RenderItems();
            
            LateComerPackedHeader.Content = $"Опоздавшие";
            if (lateCnt > 0)
            {
                LateComerPackedHeader.Content = $"{LateComerPackedHeader.Content} ({lateCnt})";
            }
            
            EnableControls();
            
            ShipmentsUpdateItemsForNextDayGrid();
        }

        /// <summary>
        /// рендер грида
        /// </summary>
        public void ShipmentsUpdateItemsForNextDayGrid()
        {
            DisableControls();

            var items = ShipmentsFilterItems(1);
           
            StackPanel panel = new StackPanel();
            panel.Orientation = Orientation.Horizontal;
            TomorrowGrid.Content = panel;

            panel.SizeChanged += Panel_SizeChanged;

            int count = 0;

            //даныне отгрузок
            if (items.Count > 0)
            {
                bool first = true;
                
                foreach (var row in items)
                {
                    var block = new DiagramShipment(row);
                    block.Width = 100;
                    block.Height = 60;

                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        block.Margin = new Thickness(1, 0, 0, 0);
                    }

                    panel.Children.Add(block);
                    count++;

                }
            }

            LabelTomorrow.Content = "Завтрашний день";

            if (count > 0)
            {
                LabelTomorrow.Content = $"{LabelTomorrow.Content} ({count})";
            }

            EnableControls();
        }

        private void Panel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            GridScrollTomorow.Width = (sender as StackPanel).ActualWidth;
        }

        /// <summary>
        /// очистка грида
        /// </summary>
        public void ShipmentsClearItems()
        { 
            MonitorTimeLabels.Children.Clear();
            MonitorContainer11.Children.Clear();
            MonitorContainer21.Children.Clear();

            MonitorContainer11.ColumnDefinitions.Clear();
            MonitorContainer21.ColumnDefinitions.Clear();

            if(TomorrowGrid.Content!=null)
            {
                (TomorrowGrid.Content as StackPanel).Children.Clear();
            }

            GC.Collect();
        }

        /// <summary>
        /// получение данных
        /// </summary>
        public async void DriversLoadItems()
        {
            DriversToolbar.IsEnabled = false;
            DriversSplash.Visibility = Visibility.Visible;

            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("TODAY", Today.Text);
                    p.Add("FACTORY_ID", $"{FactoryId}");
                }
                
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments/ShipmentKsh");
                q.Request.SetParam("Object", "ForkliftDriver");
                q.Request.SetParam("Action", "ListPlan");
                
                q.Request.SetParams(p);
                
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if(q.Answer.Status == 0)                
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                    if(result!=null)
                    {
                        DriversDs = ListDataSet.Create(result, "DRIVER");
                        DriversTasksDs = ListDataSet.Create(result, "TASK");
                        DriversUpdateItems();
                    }
                }
            }

            DriversToolbar.IsEnabled = true;
            DriversSplash.Visibility = Visibility.Collapsed;
        }
        
        /// <summary>
        /// рендер грида
        /// </summary>
        public void DriversUpdateItems()
        {
            DriversSplash.Visibility = Visibility.Visible;
            Mouse.OverrideCursor=Cursors.Wait;
            
            DriversClearItems();

            /*
                ------------------------------------------------- 
                |                        | Занятые погрузчики   |
                ------------------------------------------------- 
                | DriversHeaderContainer | DriversTaskContainer |
                ------------------------------------------------- 
             */


            //список водителей-погрузчиков
            var driversTimeline=new TaskTimeline();
            {
                driversTimeline.Container = DriversHeaderContainer;
                driversTimeline.ColumnWidth = 90;
                driversTimeline.StartHour = ShipmentsStartTime;
                driversTimeline.Init();
            
                var items = DriversDs.Items;
                if (items.Count > 0)
                {
                    driversTimeline.RowIndexMax = items.Count;

                    foreach (var row in items)
                    {
                        var block = new DiagramForkliftdriver(row);
                        var item = new TimelineItem()
                        {
                            Values = row,
                            Object = block,
                        };
                        driversTimeline.AddItem(item);
                    }
                }
            
                driversTimeline.PrepareItems();
                driversTimeline.RenderItems();    
            }

            //список заданий
            var tasksTimeline=new TaskTimeline();
            {
                tasksTimeline.Container = DriversTaskContainer;
                tasksTimeline.StartHour = ShipmentsStartTime;
                tasksTimeline.Init();
                
                var items = DriversTasksDs.Items;
                if (items.Count>0)
                {
                    tasksTimeline.RowIndexMax = driversTimeline.RowIndexMax;
                    tasksTimeline.RowsMap = driversTimeline.RowsMap;
                    
                    foreach (var row in items)
                    {
                        var block = new DiagramTask(row, this.RoleName);
                        var item = new TimelineItem()
                        {
                            Values=row,
                            Object=block,
                        };
                        tasksTimeline.AddItem(item);
                    }
                }
            
                tasksTimeline.PrepareItems();
                tasksTimeline.RenderItems();    
            }
            
            DriversSplash.Visibility = Visibility.Collapsed;
            Mouse.OverrideCursor=null;
        }
        
        /// <summary>
        /// очистка грида
        /// </summary>
        public void DriversClearItems()
        {
            DriversHeaderContainer.Children.Clear();
            DriversTaskContainer.Children.Clear();

            DriversTaskContainer.ColumnDefinitions.Clear();

            GC.Collect();
        }
        
        /// <summary>
        /// запуск механизма автообновления данных
        /// </summary>
        private void RunAutoUpdateTimer()
        {
            if (AutoUpdateInterval != 0)
            {
                if (AutoUpdateTimer == null)
                {
                    AutoUpdateTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0, 0, AutoUpdateInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", AutoUpdateInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("ShipmentPlanKsh_RunAutoUpdateTimer", row);
                    }

                    AutoUpdateTimer.Tick += (s, e) =>
                    {
                        LoadItems();
                    };
                }

                if (AutoUpdateTimer.IsEnabled)
                {
                    AutoUpdateTimer.Stop();
                }
                AutoUpdateTimer.Start();
            }
        }

        /// <summary>
        /// останов механизма автообновления данных
        /// </summary>
        private void StopAutoUpdateTimer()
        {
            if (AutoUpdateTimer != null)
            {
                if (AutoUpdateTimer.IsEnabled)
                {
                    AutoUpdateTimer.Stop();
                }
            }
        }

        /// <summary>
        /// Показать закрытые отгрузки, для которых нужно распечатать документы
        /// </summary>
        private void ShowShipmentToPrint()
        {
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "ShipmentKshControl",
                SenderName = "ShipmentKshPlan",
                ReceiverName = "ShipmentKshList",
                Action = "ShowShipmentToPrint",
                Message = "",
            });
        }

        public void HideInventoryItemCheck()
        {
            DriversUpdateItems();
        }

        private void MonitorScrollChanged(object sender, ScrollChangedEventArgs e)
        {          
            if(sender == TomorrowGridScroll)
            {
                TomorrowGrid.ScrollToVerticalOffset(e.VerticalOffset);
                TomorrowGrid.ScrollToHorizontalOffset(e.HorizontalOffset);
            }

            if (sender == MonitorArea11Scroll)
            {
                if (e.HorizontalChange != 0)
                {
                    MonitorContainer11Scroll.ScrollToHorizontalOffset(e.HorizontalOffset);
                }
            }

            if (sender == MonitorArea21Scroll)
            {
                MonitorContainer21Scroll.ScrollToVerticalOffset(e.VerticalOffset);
                MonitorContainer21Scroll.ScrollToHorizontalOffset(e.HorizontalOffset);
            }

            if (sender == MonitorArea22Scroll)
            {
                MonitorContainer22Scroll.ScrollToVerticalOffset(e.VerticalOffset);
                MonitorContainer22Scroll.ScrollToHorizontalOffset(e.HorizontalOffset);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            ShipmentsLoadItems();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void OnDriverContainerScroll(object sender, ScrollChangedEventArgs e)
        {
            ScrollHeader.ScrollToVerticalOffset(e.VerticalOffset);

            DriverContainerScroll.ScrollToVerticalOffset(e.VerticalOffset);
            DriverContainerScroll.ScrollToHorizontalOffset(e.HorizontalOffset);
        }

        private void ActiveForkliftDrivers_Click(object sender, RoutedEventArgs e)
        {
            var activeForkliftdrivers = new ForkliftDriverActive();
            activeForkliftdrivers.FactoryId = this.FactoryId;
            activeForkliftdrivers.Edit();
        }

        private void ReloadDrivers_Click(object sender, RoutedEventArgs e)
        {
            DriversLoadItems();
        }

        private void HideCompleteCheckbox_Click(object sender, RoutedEventArgs e)
        {
            ShipmentsUpdateItems();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ShipmentsClearItems();
        }

        private void SendMessageButton_Click(object sender, RoutedEventArgs e)
        {
            var form = new ForkliftDriverMessage();
            form.FactoryId = this.FactoryId;
            form.Show();
        }

        private void HidePackagingCheckbox_Click(object sender, RoutedEventArgs e)
        {
            ShipmentsUpdateItems();
        }

        private void HidePlacerCheckbox_Click(object sender, RoutedEventArgs e)
        {
            ShipmentsUpdateItems();
        }

        private void HideInCompleteCheckbox_Click(object sender, RoutedEventArgs e)
        {
            ShipmentsUpdateItems();
        }

        private void ShipmentCountToPrintButton_Click(object sender, RoutedEventArgs e)
        {
            ShowShipmentToPrint();
        }

        private void HideInventoryItemCheckbox_Click(object sender, RoutedEventArgs e)
        {
            HideInventoryItemCheck();
        }
    }
}