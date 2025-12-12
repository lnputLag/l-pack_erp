using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Production.ProcessingMachines
{
    /// <summary>
    /// Диаграмма ПЗ на переработке
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2021-11-23</released>
    public partial class ProductionTaskDiagram:UserControl
    {
        public ProductionTaskDiagram()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this,ProcessMessages);

            //регистрация обработчика клавиатуры
            PreviewKeyDown+=ProcessKeyboard;

            //при отрисовке грида (при загрузке, переключении таба)
            Loaded+=OnLoad;

            SetDefaults();
            Init();

            ProcessPermissions();
        }

        public string RoleName = "[erp]production_task_pr_diagram";

        /// <summary>
        /// датасеты
        /// </summary>
        private ListDataSet MachineDs { get;set; }
        private ListDataSet WorkDs { get;set; }
        private ListDataSet IdleDs { get;set; }
        private ListDataSet CounterDs { get;set; }

        /// <summary>
        /// монитор данных
        /// </summary>
        private ProductionTaskMonitorGrid Monitor { get; set; }

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
        /// обработчик сообщений
        /// </summary>
        private void ProcessMessages(ItemMessage m)
        {
            //Group ProductionTask
            if(m.ReceiverGroup.IndexOf("ProductionTaskDiagram") > -1)
            {
                switch(m.Action)
                {
                    case "Refresh":
                        LoadItems();
                        break;
                }
            }
        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        private void ProcessKeyboard(object sender,KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.F5:
                    LoadItems();
                    e.Handled=true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled=true;
                    break;

                case Key.Home:
                    e.Handled=true;
                    break;

                case Key.End:                    
                    e.Handled=true;
                    break;
            }
        }

        /// <summary>
        /// отображение статьи в справочной системе
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/production/pt_processing_diagramm");
        }

        /// <summary>
        /// деструктор интерфейса
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="ProductionTaskDiagram",
                ReceiverName = "",
                SenderName = "ProductionTaskDiagramView",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //монитор данных
            if(Monitor!=null)
            {
                Monitor.Clear();
            }

        }

        /// <summary>
        /// инициализация грида
        /// </summary>
        public void Init()
        {
            LoadItems();
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            //значения полей по умолчанию
            {
                Today.Text=DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy");
            }
        }

        /// <summary>
        /// загрузка данных
        /// </summary>
        public async void LoadItems()
        {
            Toolbar.IsEnabled = false;
            Splash.Visibility=Visibility.Visible;

            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();
                { 
                    p.CheckAdd("TODAY", Today.Text);
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module","Production");
                q.Request.SetParam("Object","ProductionTask");
                q.Request.SetParam("Action","ListDiagram");
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if(q.Answer.Status == 0)                
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                    if(result!=null)
                    {
                        if(result.ContainsKey("MACHINE"))
                        {
                            MachineDs=(ListDataSet)result["MACHINE"];
                            MachineDs.Init();                            
                        }
                        
                        if(result.ContainsKey("WORK"))
                        {
                            WorkDs=(ListDataSet)result["WORK"];
                            WorkDs.Init();                            
                        }
                        
                        if(result.ContainsKey("IDLE"))
                        {
                            IdleDs=(ListDataSet)result["IDLE"];
                            IdleDs.Init();                            
                        }
                        
                        if(result.ContainsKey("COUNTER"))
                        {
                            CounterDs=(ListDataSet)result["COUNTER"];
                            CounterDs.Init();                            
                        }
                        
                        UpdateItems();
                    }
                }                
            }

            Toolbar.IsEnabled = true;
            Splash.Visibility=Visibility.Collapsed;
        }

        /// <summary>
        /// обновление данных
        /// </summary>
        private void UpdateItems()
        {
            /*
                Верстка монитора проста и бесхитростна)
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

                ShipmentMonitorGrid
                    Rows => List<ShipmentMonitorGridRow>
                        Works    => List<ShipmentMonitorGridCell>
                        Idles    => List<ShipmentMonitorGridCell>
                        Counters => List<ShipmentMonitorGridCell>

             */
            
            var today=Today.Text.ToDateTime();
            
            Monitor = new ProductionTaskMonitorGrid(today);
            
            //контейнеры
            Monitor.HeadersContainer = MonitorHeadersContainer;
            Monitor.DataContainer = MonitorDataContainer;
            Monitor.TimelineContainer = MonitorTimelineContainer;
            
            //очистка данных монитора
            Monitor.Clear();
            
            //загрузка данных
            Monitor.LoadTitles(MachineDs);
            Monitor.LoadWorks(WorkDs);
            Monitor.LoadIdles(IdleDs);
            Monitor.LoadCounters(CounterDs);
            
            //подготовка строк
            Monitor.PrepareRows();

            //рендеринг грида
            Monitor.RenderGrid();

            //рендеринг данных
            Monitor.RenderCounters();
            Monitor.RenderWorks();
            Monitor.RenderIdles();
        }

        /// <summary>
        /// будет вызвана, когда грид отрисуется (при создании таба, при получении табом фокуса)
        /// </summary>
        private void OnLoad(object sender,RoutedEventArgs e)
        {
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
       
        private void HelpButton_Click_1(object sender,RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void RefreshButton_Click(object sender,RoutedEventArgs e)
        {
            LoadItems();
        }
    }
}
