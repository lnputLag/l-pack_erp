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

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Диаграмма рулонов на ГА
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2021-11-23</released>
    public partial class RollDiagram:UserControl
    {
        public RollDiagram()
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

        public string RoleName = "[erp]roll_registration";

        /// <summary>
        /// датасеты
        /// </summary>
        private ListDataSet RollDs { get;set; }
        private ListDataSet RollActivityDs { get;set; }
        private ListDataSet MachineDs { get;set; }

        /// <summary>
        /// монитор данных
        /// </summary>
        private RollMonitorGrid Monitor { get; set; }

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
            if(m.ReceiverGroup.IndexOf("RollDiagram") > -1)
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
        private void ProcessKeyboard(object sender,System.Windows.Input.KeyEventArgs e)
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
            Central.ShowHelp("/doc/l-pack-erp/production/rolls_diagramm/diagram");
        }

        /// <summary>
        /// деструктор интерфейса
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="RollDiagram",
                ReceiverName = "",
                SenderName = "RollDiagramView",
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
                Today.Text=DateTime.Now.ToString("dd.MM.yyyy");
            }
        }

        /// <summary>
        /// загрузка данных
        /// </summary>
        public async void LoadItems()
        {
            Toolbar.IsEnabled = false;
            Splash.Visibility=System.Windows.Visibility.Visible;

            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();
                { 
                    p.CheckAdd("TODAY", Today.Text);
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module","Production");
                q.Request.SetParam("Object","Roll");
                //FIXME: rename action: NewList -> List*
                q.Request.SetParam("Action","NewList");

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
                        if(result.ContainsKey("ROLL"))
                        {
                            RollDs=(ListDataSet)result["ROLL"];
                            RollDs.Init();                            
                        }

                        if(result.ContainsKey("ROLL_ACTIVITY"))
                        {
                            RollActivityDs=(ListDataSet)result["ROLL_ACTIVITY"];
                            RollActivityDs.Init();                            
                        }

                        if(result.ContainsKey("MACHINE"))
                        {
                            MachineDs=(ListDataSet)result["MACHINE"];
                            MachineDs.Init();                            
                        }
                        
                        UpdateItems();
                    }
                }
            }

            Toolbar.IsEnabled = true;
            Splash.Visibility=System.Windows.Visibility.Collapsed;
        }

        /// <summary>
        /// обновление данных
        /// </summary>
        private void UpdateItems()
        {
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

                ShipmentRollsGrid
                    Rows => List<ShipmentRollsGridRow>
                        Items    => List<ShipmentRollsGridCell>

            */

            var today=Today.Text.ToDateTime();
            
            Monitor=new RollMonitorGrid(today);

            //контейнеры
            Monitor.HeadersContainer    = MonitorHeadersContainer;
            Monitor.DataContainer       = MonitorDataContainer;
            Monitor.TimelineContainer   = MonitorTimelineContainer;

            //очистка данных монитора
            Monitor.Clear();

            //загрузка данных в монитор
            Monitor.LoadTitles(MachineDs);
            Monitor.LoadRolls(RollDs);                        
            Monitor.LoadRollsActivities(RollActivityDs);

            //подготовка строк
            Monitor.PrepareRows();

            //рендеринг грида
            Monitor.RenderGrid();

            //рендеринг данных
            Monitor.RenderRolls();
            Monitor.RenderRollsActivities();
        }

        /// <summary>
        /// будет вызвана, когда грид отрисуется (при создании таба, при получении табом фокуса)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLoad(object sender,RoutedEventArgs e)
        {
            //return;
            Central.Dbg($"ControlLoaded");

            //прокрутка к текущему времени
            if(Monitor!=null)
            {
                if(Monitor.CenterCol != 0)
                {
                    /*
                        положение курсора "текущее время"+90% ширины таблицы
                        курсор окажется у правого края
                    */
                    
                    double c = Column2.ActualWidth;
                    c=c*0.9;

                    int offset = (int)(Monitor.CenterCol*(Monitor.CellWidth))-(int)c;                    
                    if(offset < 0)
                    {
                        offset=0;
                    }

                    //Central.Dbg($"Scroll monitor CenterCol=[{Monitor.CenterCol}] col2=[{c}] offset=[{offset}]");
                    MonitorScrollTo(offset);
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
        private void MonitorScrollChanged(object sender,ScrollChangedEventArgs e)
        {
            if(sender.ToString()==MonitorDataContainerAreaScroll.ToString())
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
