using Client.Common;
using Client.Interfaces.Main;
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
    /// Управление отгрузками, вкладка "Монитор"
    /// </summary>
    /// <author>balchugov_dv</author>    
    public partial class ShipmentsMonitorTerminal:UserControl
    {
        public ShipmentsMonitorTerminal()
        {
            InitializeComponent();

            AutoUpdateInterval=60*5;
            ItemsAutoUpdate=false;
            FirstLoad=false;

            Profiler=new Profiler();
            FirstTimeLoaded=false;

            ProcessPermissions();
            SetDefaults();
            Init();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, _ProcessMessages);
            Central.Msg.Register(ProcessMessages);
            
            Loaded+=OnLoad;                        
        }

        public int FactoryId = 1;

        private bool ItemsAutoUpdate{get;set;}
        public Profiler Profiler { get;set;}

        /// <summary>
        /// Флаг того, что первичное получение данных интерфейса выполнено
        /// </summary>
        private bool FirstTimeLoaded { get;set;}
        private ShipmentMonitorGrid Monitor { get; set; }
        private List<Dictionary<string,string>> Items { get; set; }
        private bool FirstLoad {get;set;}

        /// <summary>
        /// интервал автообновления грида
        /// 0- автообновление отключено
        /// (по таймеру будет вызвана коллбэк-функция DoLoadItems
        /// </summary>
        public int AutoUpdateInterval { get; set; }
        public DispatcherTimer AutoUpdateTimer { get; set; }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel("[erp]shipment_control");
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

        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="ShipmentControl",
                ReceiverName = "",
                SenderName = "ShipmentsMonitorView",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
            Central.Msg.UnRegister(ProcessMessages);

            //останавливаем таймеры грида
            StopAutoUpdateTimer();
        }

        private void OnLoad(object sender,RoutedEventArgs e)
        {
            UpdateNowMarker();
        }

        public void SetDefaults()
        {
            { 
                var list = new Dictionary<string,string>();
                list.Add("1","Все терминалы");
                list.Add("2","Активные");
                list.Add("3","С отгрузками");
                
                Terminals.Items = list;
                Terminals.SelectedItem = list.FirstOrDefault((x)=>x.Key=="3");     
            }

            {
                var list = new Dictionary<string, string>();
                list.Add("0", "Все типы продукции");
                list.Add("1", "Готовая продукция");
                list.Add("2", "Рулоны");

                TerminalProductTypeSelectBox.Items = list;
                TerminalProductTypeSelectBox.SetSelectedItemByKey("0");
            }

            ForkliftDriverSelectBoxLoadItems();

            TodayDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
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
                    && message.ReceiverName == "ShipmentsControl_MonitorTerminal"
                )
                {
                    switch (message.Action)
                    {
                        case "FocusGot":
                            ItemsAutoUpdate=true;
                            if(!FirstLoad)
                            {
                                LoadItems();
                            }
                            break;

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
        /// <param name="m"></param>
        private void _ProcessMessages(ItemMessage m)
        {
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
                    LoadItems();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// обработчик системы навигации по URL
        /// </summary>
        public void ProcessNavigation()
        {

        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        /// <param name="m"></param>
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F5:
                    LoadItems();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;
            }
        }

        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/shipments/control/monitor");
        }

        private void ForkliftDriverSelectBoxLoadItems(bool setDefaultItem = true)
        {
            ForkliftDriverSelectBox.Items.Clear();

            ForkliftDriverSelectBox.Items.CheckAdd("0", "Все погрузчики");
            FormHelper.ComboBoxInitHelper(ForkliftDriverSelectBox, "Shipments", "ForkliftDriver", "ListMonitor", "ID", "NAME", 
                new Dictionary<string, string>() { { "Today", TodayDate.Text }, { "FACTORY_ID", $"{FactoryId}" }, { "TYPE", "-2" } }, true);

            if (setDefaultItem)
            {
                ForkliftDriverSelectBox.SetSelectedItemByKey("0");
            }
        }

        public void Init()
        {
            LoadItems();
            RunAutoUpdateTimer();
        }

        /// <summary>
        /// получение записей
        /// </summary>
        public async void LoadItems()
        {
            if(ItemsAutoUpdate)
            {
                FirstLoad=true;

                {
                    var tm = Profiler.GetDelta(); 
                    Central.Dbg($"SH:Monitor [{tm}] LoadItems");
                }

                bool resume=true;                

                if (resume)
                {
                    DisableControls();

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module","Shipments");
                    q.Request.SetParam("Object","Shipment");
                    q.Request.SetParam("Action","ListMonitor");
            
                    //FIXME: naming
                    q.Request.SetParam("Today",TodayDate.Text);
                    q.Request.SetParam("FACTORY_ID", $"{FactoryId}");

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

                    await Task.Run(() =>
                    {
                       q.DoQuery();
                    });

                    {
                        var tm = Profiler.GetDelta(); 
                        Central.Dbg($"SH:Monitor [{tm}] List Complete");
                    }

                    if(q.Answer.Status == 0)                
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result!=null)
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            Items=ds.Items;

                            var selctedForkliftDriverId = ForkliftDriverSelectBox.SelectedItem.Key.ToInt();
                            ForkliftDriverSelectBoxLoadItems(false);
                            if (!ForkliftDriverSelectBox.Items.ContainsKey($"{selctedForkliftDriverId}"))
                            {
                                ForkliftDriverSelectBox.SetSelectedItemByKey("0");
                            }
                            else
                            {
                                UpdateItems(Items);
                            }

                            {
                                var tm = Profiler.GetDelta(); 
                                Central.Dbg($"SH:Monitor [{tm}] UpdateItems Complete");
                            }

                            UpdateNowMarker();

                            {
                                var tm = Profiler.GetDelta(); 
                                Central.Dbg($"SH:Monitor [{tm}] UpdateNowMarker Complete");
                            }

                            FirstTimeLoaded = true;
                        }
                  
                    }
                }

                EnableControls();

            }
        }

        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
            Splash.Visibility=Visibility.Visible;        
        }

        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
            Splash.Visibility=Visibility.Collapsed;
        }
        
        public void RunAutoUpdateTimer()
        {
            if(AutoUpdateInterval!=0)
            {
                if(AutoUpdateTimer == null)
                {
                    AutoUpdateTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0,0,AutoUpdateInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", AutoUpdateInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("ShipmentsMonitor_RunAutoUpdateTimer", row);
                    }

                    AutoUpdateTimer.Tick += (s,e) =>
                    {
                        LoadItems();
                    };
                }

                if(AutoUpdateTimer.IsEnabled)
                {
                    AutoUpdateTimer.Stop();
                }
                AutoUpdateTimer.Start();
            }
        }

        public void StopAutoUpdateTimer()
        {
            if(AutoUpdateTimer != null)
            {
                if(AutoUpdateTimer.IsEnabled)
                {
                    AutoUpdateTimer.Stop();
                }
            }
        }

        
        /// <summary>
        /// При обновлении списка строк, производим перерисовку "монитора"
        /// </summary>
        /// <param name="items"></param>
        private void UpdateItems(List<Dictionary<string, string>> items)
        {
            DisableControls();
            
            /*
                Верстка монитора проста и бесхитростна
                горизонтальный таймлайн:
                -- по горизонтали: время, от 7:30 до 8 утра следующего дня
                -- по вертикали терминалы отгрузки

                ---------------------------------------------------------------------------------------------
                | Column1                 | Column2
                ---------------------------------------------------------------------------------------------
                |                         | MonitorTimelineContainer
                ---------------------------------------------------------------------------------------------
                | MonitorHeadersContainer | MonitorDataContainer
                |                         | 
                ---------------------------------------------------------------------------------------------
                |                         | MonitorDataContainerAreaScroll
                ---------------------------------------------------------------------------------------------
                

                Column1,Column2 -- макетные, по ним определяем ширину колонок
                MonitorTimelineContainer
                MonitorHeadersContainer
                MonitorDataContainer

                Содержат скроллеры. Их ширина берется из Column2.
                MonitorHeadersContainer и MonitorTimelineContainer имеют скрытые скроллбары.
                Ширина MonitorDataContainerAreaScroll синхронизирована с MonitorTimelineContainer.
                При прокрутке MonitorDataContainerAreaScroll, офсет прокрутки передается в 
                MonitorTimelineContainer и MonitorDataContainer.

             */

            //значение фильтра: текушая или иная дата
            var today = DateTime.Now;
            if (!string.IsNullOrEmpty(TodayDate.Text))
            {
                today = TodayDate.Text.ToDateTime();
            }
            Central.Dbg($"Today={today.ToString()}");

            // 1 -- Все терминалы
            // 2 -- Активные
            // 3 -- С отгрузками
            var terminalType = 1;
            if (!string.IsNullOrEmpty(Terminals.SelectedItem.Key))
            {
                terminalType = Terminals.SelectedItem.Key.ToInt();
            }

            // 0 -- Все
            // 1 -- Готовая продукция
            // 2 -- Рулоны
            int terminalProductType = 0;
            if (!string.IsNullOrEmpty(TerminalProductTypeSelectBox.SelectedItem.Key))
            {
                terminalProductType = TerminalProductTypeSelectBox.SelectedItem.Key.ToInt();
            }

            // Фильтрация по водителю погрузчика
            // 0 -- Все
            List<Dictionary<string, string>> _items = new List<Dictionary<string, string>>();
            {
                int forkliftDriverId = 0;
                if (!string.IsNullOrEmpty(ForkliftDriverSelectBox.SelectedItem.Key))
                {
                    forkliftDriverId = ForkliftDriverSelectBox.SelectedItem.Key.ToInt();
                }

                if (forkliftDriverId == 0)
                {
                    _items = items;
                }
                else
                {
                    _items = items.Where(x => x.CheckGet("FORKLIFT_DRIVER_ID").ToInt() == forkliftDriverId).ToList();
                }
            }

            //контейнеры
            Monitor = new ShipmentMonitorGrid(today)
            {
                HeadersContainer = MonitorHeadersContainer,
                DataContainer = MonitorDataContainer,
                TimelineContainer = MonitorTimelineContainer,
                ShowAll = true,
                TerminalType = terminalType,
                TerminalProductType = terminalProductType,
            };


            //фильтр: отобразить все терминалы (по умолчанию только те, где есть отгрузки)
            //Monitor.TerminalType=0;

            //загрузка данных и рендер
            Monitor.Clear();
            Monitor.LoadItems(_items);
            //Profiler.AddPoint($"LoadItems complete");

            Monitor.RenderGrid();
            //Profiler.AddPoint($"RenderGrid complete");

            Monitor.RenderData();
            //Profiler.AddPoint($"RenderData complete");

            Central.Dbg($"RowsCount=[{Monitor.RowsCount}]");
            MonitorDataContainer.Height = Monitor.RowsCount * 60;
            MonitorHeadersContainer.Height = Monitor.RowsCount * 60;
            MonitorContainer.Height = Monitor.RowsCount * 60;

            EnableControls();

        }


        private void UpdateNowMarker()
        {
            //прокрутка к текущему времени
            if (Monitor != null)
            {
                if (Monitor.CenterCol != 0)
                {
                    /*
                        положение курсора "текущее время"+90% ширины таблицы
                        курсор окажется у правого края
                    */
                    double c = Column2.ActualWidth;
                    c *= 0.9;

                    /*
                    int offset = Monitor.CenterCol - (int)c;
                    if (offset < 0)
                    {
                        offset = 0;
                    }
                    */
                    int offset=Monitor.GetCenter();
                    offset=offset-(int)c;
                    if (offset < 0)
                    {
                        offset = 0;
                    }
                   
                    Central.Dbg($"Scroll monitor CenterCol=[{Monitor.GetCenter()}] col2=[{c}] offset=[{offset}]");

                    if(offset>120)
                    {
                        //MonitorScrollTo(offset);
                    }
                    
                }
            }
        }

        /// <summary>
        /// Прокрутка к указанной позиции (пикс) блока данных
        /// </summary>
        /// <param name="offset"></param>
        private void MonitorScrollTo(int offset)
        {
            MonitorDataContainerAreaScroll.ScrollToHorizontalOffset(offset);
            MonitorDataContainerScroll.ScrollToHorizontalOffset(offset);
            MonitorTimelineContainerScroll.ScrollToHorizontalOffset(offset);
        }

        /// <summary>
        /// Синхронизация блоков прокрутки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MonitorScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (sender == MonitorDataContainerAreaScroll)
            {
                MonitorDataContainerScroll.ScrollToVerticalOffset(e.VerticalOffset);
                MonitorDataContainerScroll.ScrollToHorizontalOffset(e.HorizontalOffset);

                MonitorTimelineContainerScroll.ScrollToVerticalOffset(e.VerticalOffset);
                MonitorTimelineContainerScroll.ScrollToHorizontalOffset(e.HorizontalOffset);
            }
        }

        private void ResreshButton_Click(object sender,RoutedEventArgs e)
        {
            LoadItems();
        }

        private void Terminals_SelectedItemChanged(DependencyObject d,DependencyPropertyChangedEventArgs e)
        {
            if (FirstTimeLoaded)
            {
                UpdateItems(Items);
            }            
        }

        private void HelpButton_Click(object sender,RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void TerminalProductTypeSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (FirstTimeLoaded)
            {
                UpdateItems(Items);
            }
        }

        private void ForkliftDriverSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (FirstTimeLoaded)
            {
                UpdateItems(Items);
            }
        }
    }
}
