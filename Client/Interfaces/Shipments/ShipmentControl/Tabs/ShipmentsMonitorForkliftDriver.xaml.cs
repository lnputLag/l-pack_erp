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
    /// Управление отгрузками, вкладка "Монитор водителей погрузчика"
    /// </summary>
    /// <author>sviridov_ae</author>    
    public partial class ShipmentsMonitorForkliftDriver : UserControl
    {
        public ShipmentsMonitorForkliftDriver()
        {
            InitializeComponent();

            AutoUpdateInterval = 60 * 5;
            ItemsAutoUpdate = false;
            FirstLoad = false;

            FirstTimeLoaded = false;

            ProcessPermissions();
            SetDefaults();
            Init();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, _ProcessMessages);
            Central.Msg.Register(ProcessMessages);

            Loaded += OnLoad;
        }

        public int FactoryId = 1;

        private bool ItemsAutoUpdate{get;set;}

        /// <summary>
        /// Флаг того, что первичное получение данных интерфейса выполнено
        /// </summary>
        private bool FirstTimeLoaded { get;set;}
        private ShipmentMonitorForkliftDriverGrid Monitor { get; set; }
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
                SenderName = "ShipmentsMonitorForkliftDriver",
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
                var list = new Dictionary<string, string>();
                list.Add("1", "Все погрузчики");
                list.Add("2", "Активные");
                list.Add("3", "С отгрузками");

                ForkliftDriverStatusSelectBox.Items = list;
                ForkliftDriverStatusSelectBox.SelectedItem = list.FirstOrDefault((x) => x.Key == "3");
            }

            {
                var list = new Dictionary<string, string>();
                list.Add("0", "Все типы продукции");
                list.Add("1", "Готовая продукция");
                list.Add("2", "Рулоны");

                ForkliftDriverProductTypeSelectBox.Items = list;
                ForkliftDriverProductTypeSelectBox.SetSelectedItemByKey("0");
            }

            var date = DateTime.Now;
            if (date.Hour >= 20 || date.Hour < 8)
            {
                if (date.Hour >= 20)
                {
                    FromDateTime.Text = $"{DateTime.Now.ToString("dd.MM.yyyy")} 20:00:00";
                    ToDateTime.Text = $"{DateTime.Now.AddDays(1).ToString("dd.MM.yyyy")} 08:00:00";
                }
                else
                {
                    FromDateTime.Text = $"{DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy")} 20:00:00";
                    ToDateTime.Text = $"{DateTime.Now.ToString("dd.MM.yyyy")} 08:00:00";
                }
            }
            else
            {
                FromDateTime.Text = $"{DateTime.Now.ToString("dd.MM.yyyy")} 08:00:00";
                ToDateTime.Text = $"{DateTime.Now.ToString("dd.MM.yyyy")} 20:00:00";
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
                    && message.ReceiverName == "ShipmentsControl_MonitorForkliftDriver"
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
            var e = Central.WM.KeyboardEventsArgs;
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
            Central.ShowHelp("/doc/l-pack-erp/shipments/control/");
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
            if (ItemsAutoUpdate)
            {
                FirstLoad = true;

                DisableControls();

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "Shipment");
                q.Request.SetParam("Action", "ListMonitorForkliftDriver");

                q.Request.SetParam("FROM_DATETIME", FromDateTime.Text);
                q.Request.SetParam("TO_DATETIME", ToDateTime.Text);
                q.Request.SetParam("FACTORY_ID", $"{FactoryId}");

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
                        Items = ds.Items;

                        UpdateItems(Items);

                        UpdateNowMarker();

                        FirstTimeLoaded = true;
                    }
                }

                EnableControls();
            }
        }

        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
            Splash.Visibility = Visibility.Visible;        
        }

        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
            Splash.Visibility = Visibility.Collapsed;
        }
        
        public void RunAutoUpdateTimer()
        {
            if(AutoUpdateInterval != 0)
            {
                if(AutoUpdateTimer == null)
                {
                    AutoUpdateTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0, 0, AutoUpdateInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", AutoUpdateInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("ShipmentsMonitorForkliftDriver_RunAutoUpdateTimer", row);
                    }

                    AutoUpdateTimer.Tick += (s, e) =>
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
            DateTime fromDttm = DateTime.Now;
            DateTime toDttm = DateTime.Now.AddDays(1);
            if (!string.IsNullOrEmpty(FromDateTime.Text))
            {
                fromDttm = FromDateTime.Text.ToDateTime();
            }
            if (!string.IsNullOrEmpty(ToDateTime.Text))
            {
                toDttm = ToDateTime.Text.ToDateTime();
            }
            Central.Dbg($"fromDttm={fromDttm.ToString()}");

            // 1 -- Все погрузчики
            // 2 -- Активные
            // 3 -- С отгрузками
            var forkliftDriverStatus = 1;
            if (!string.IsNullOrEmpty(ForkliftDriverStatusSelectBox.SelectedItem.Key))
            {
                forkliftDriverStatus = ForkliftDriverStatusSelectBox.SelectedItem.Key.ToInt();
            }

            // 0 -- Все
            // 1 -- Готовая продукция
            // 2 -- Рулоны
            int forkliftDriverProductType = 0;
            if (!string.IsNullOrEmpty(ForkliftDriverProductTypeSelectBox.SelectedItem.Key))
            {
                forkliftDriverProductType = ForkliftDriverProductTypeSelectBox.SelectedItem.Key.ToInt();
            }

            //контейнеры
            Monitor = new ShipmentMonitorForkliftDriverGrid(fromDttm, toDttm)
            {
                HeadersContainer = MonitorHeadersContainer,
                DataContainer = MonitorDataContainer,
                TimelineContainer = MonitorTimelineContainer,
                ForkliftDriverStatus = forkliftDriverStatus,
                ForkliftDriverProductType = forkliftDriverProductType,
            };

            //загрузка данных и рендер
            Monitor.Clear();
            Monitor.LoadItems(items);

            Monitor.RenderGrid();

            Monitor.RenderData();

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

                    int offset=Monitor.GetCenter();
                    offset=offset-(int)c;
                    if (offset < 0)
                    {
                        offset = 0;
                    }
                   
                    Central.Dbg($"Scroll monitor CenterCol=[{Monitor.GetCenter()}] col2=[{c}] offset=[{offset}]");
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

        private void HelpButton_Click(object sender,RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadItems();
        }

        private void ForkliftDriverStatusSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (FirstTimeLoaded)
            {
                UpdateItems(Items);
            }
        }

        private void ForkliftDriverProductTypeSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (FirstTimeLoaded)
            {
                UpdateItems(Items);
            }
        }

        private void OnCurrentShift(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now;
            if (date.Hour >= 20 || date.Hour < 8)
            {
                if (date.Hour >= 20)
                {
                    FromDateTime.Text = $"{DateTime.Now.ToString("dd.MM.yyyy")} 20:00:00";
                    ToDateTime.Text = $"{DateTime.Now.AddDays(1).ToString("dd.MM.yyyy")} 08:00:00";
                }
                else
                {
                    FromDateTime.Text = $"{DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy")} 20:00:00";
                    ToDateTime.Text = $"{DateTime.Now.ToString("dd.MM.yyyy")} 08:00:00";
                }
            }
            else
            {
                FromDateTime.Text = $"{DateTime.Now.ToString("dd.MM.yyyy")} 08:00:00";
                ToDateTime.Text = $"{DateTime.Now.ToString("dd.MM.yyyy")} 20:00:00";
            }

            LoadItems();
        }

        private void OnPrevShift(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now.AddHours(-12);
            if (date.Hour >= 20 || date.Hour < 8)
            {
                if (date.Hour >= 20)
                {
                    FromDateTime.Text = $"{date.ToString("dd.MM.yyyy")} 20:00:00";
                    ToDateTime.Text = $"{date.AddDays(1).ToString("dd.MM.yyyy")} 08:00:00";
                }
                else
                {
                    FromDateTime.Text = $"{date.AddDays(-1).ToString("dd.MM.yyyy")} 20:00:00";
                    ToDateTime.Text = $"{date.ToString("dd.MM.yyyy")} 08:00:00";
                }
            }
            else
            {
                FromDateTime.Text = $"{date.ToString("dd.MM.yyyy")} 08:00:00";
                ToDateTime.Text = $"{date.ToString("dd.MM.yyyy")} 20:00:00";
            }

            LoadItems();
        }

        private void OnCurrentDay(object sender, RoutedEventArgs e)
        {
            FromDateTime.Text = $"{DateTime.Now.ToString("dd.MM.yyyy")} 00:00:00";
            ToDateTime.Text = $"{DateTime.Now.AddDays(1).ToString("dd.MM.yyyy")} 00:00:00";

            LoadItems();
        }

        private void OnPrevDay(object sender, RoutedEventArgs e)
        {
            FromDateTime.Text = $"{DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy")} 00:00:00";
            ToDateTime.Text = $"{DateTime.Now.ToString("dd.MM.yyyy")} 00:00:00";

            LoadItems();
        }

        private void OnCurrentWeek(object sender, RoutedEventArgs e)
        {
            DayOfWeek day = DateTime.Now.DayOfWeek;
            int days = day - DayOfWeek.Monday;
            DateTime date = DateTime.Now.AddDays(-days);

            FromDateTime.Text = $"{date.Date.ToString("dd.MM.yyyy")} 00:00:00";
            ToDateTime.Text = $"{date.Date.AddDays(7).ToString("dd.MM.yyyy")} 00:00:00";

            LoadItems();
        }

        private void OnPrevWeek(object sender, RoutedEventArgs e)
        {
            DayOfWeek day = DateTime.Now.DayOfWeek;
            int days = day - DayOfWeek.Monday;
            DateTime date = DateTime.Now.AddDays(-days).AddDays(-7);

            FromDateTime.Text = $"{date.Date.ToString("dd.MM.yyyy")} 00:00:00";
            ToDateTime.Text = $"{date.Date.AddDays(7).ToString("dd.MM.yyyy")} 00:00:00";

            LoadItems();
        }

        private void OnCurrentMonth(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now;

            FromDateTime.Text = $"{new DateTime(date.Year, date.Month, 1).ToString("dd.MM.yyyy")} 00:00:00";
            ToDateTime.Text = $"{new DateTime(date.Year, date.Month, 1).AddMonths(1).ToString("dd.MM.yyyy")} 00:00:00";

            LoadItems();
        }

        private void OnPrevMonth(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now.AddMonths(-1);

            FromDateTime.Text = $"{new DateTime(date.Year, date.Month, 1).ToString("dd.MM.yyyy")} 00:00:00";
            ToDateTime.Text = $"{new DateTime(date.Year, date.Month, 1).AddMonths(1).ToString("dd.MM.yyyy")} 00:00:00";

            LoadItems();
        }
    }
}
