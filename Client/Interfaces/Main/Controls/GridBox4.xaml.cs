using AutoUpdaterDotNET;
using Client.Assets.Converters;
using Client.Common;
using DevExpress.Data;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using DevExpress.Utils.Controls;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using DevExpress.Xpo.DB;
using Gu.Wpf.DataGrid2D;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.XSSF.Streaming.Values;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security.RightsManagement;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using DevExpress.Utils;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static Client.Interfaces.Main.FormDialog;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Client.Interfaces.Main
{
    /// <summary>
    /// грид с данными
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-11-10</released>
    /// <changed>2023-11-10</changed>
    /// <changed>2024-03-05</changed> 
    public partial class GridBox4 : System.Windows.Controls.UserControl
    {
        public GridBox4()
        {
            InitializeComponent();

            Initialized = false;
            GridBox4Localizer.EnableLocalize = true;
            ControlName = this.GetType().Name;
            DebugName="";
            Columns = new List<DataGridHelperColumn>();
            ColumnsDx = new Dictionary<string, DevExpress.Xpf.Grid.GridColumn>();
            DataTable = new System.Data.DataTable();
            DataSet = new ListDataSet();
            SortingEnabled = false;
            SortColumn = new DataGridHelperColumn();
            SortDirection = ListSortDirection.Ascending;
            PrimaryKey = "";
            ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            SelectedItem = new Dictionary<string, string>();
            SelectedItemIndex = 0;
            SelectedItemKey = "";
            SelectedItemValue = "";
            SelectedItemContent = "";
            SelectedColumn = null;
            OnSelectItem = null;
            Menu = new Dictionary<string, DataGridContextMenuItem>();
            Commands = null;
            OnDblClick = null;
            MouseClickType = MouseClickTypeRef.Undefined;
            ColumnSymbolWidth = 8;
            //2 символа в заголовке
            ColumnWidthMin = 35;
            RowHeight = 20;
            SearchText = new System.Windows.Controls.TextBox();
            Items = new List<Dictionary<string, string>>();
            OnFilterItems = null;
            OnLoadItems = null;
            Toolbar = new StackPanel();
            QueryLoadItems = null;
            ItemsAutoUpdate = true;
            AutoUpdateInterval = 60;
            SearchTimerTimeout = 1000;
            GridContainerWidthOffset = 45;
            GridContainerWidthOffsetActual = 0;
            RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>();
            Descriription = "";
            PerformanceLog = "";
            ReformanceProfiler = new Profiler();
            RowSelectProfiler = new Profiler();
            ControlStyles = new Dictionary<string, Style>();
            Resized = false;
            Constructed = false;
            Loaded = false;
            Updated = false;
            Stage = 0;
            RunTimeout = null;
            Runned = false;
            Sorted = false;
            UpdateItemsFirstTime = true;
            CellTplCache = new Dictionary<string, DataTemplate>();
            LastKeyboardEvent = DateTime.Now;
            UseProgressSplashAuto = false;
            UseProgressBar = false;
            ProgressBarInterval = Central.Parameters.ProgressGridDelay;
            ProgressBarIntervalTick = 200;
            ProgressBarTimeout = null;
            ProgressBarAbortTimeout = null;
            ProgressNote = "Загрузка";

            {
                ProgressBarTimeout = new Common.Timeout(
                    1,
                    () =>
                    {
                        ProgressBarTimeoutProcess();
                    },
                    true,
                    false
                );
                ProgressBarTimeout.SetIntervalMs(ProgressBarIntervalTick);
                ProgressBarTimeoutProcess(-1);
            }

            {
                ProgressBarAbortTimeout = new Common.Timeout(
                    1,
                    () =>
                    {
                        ProgressBarTimeoutProcess(9);
                    },
                    false,
                    false
                );
                ProgressBarAbortTimeout.SetIntervalMs(45000);
            }
        }

        /// <summary>
        /// имя контрола
        /// заполняется конструктором
        /// </summary>
        public string ControlName { get; set; }       
        public string DebugName { get; set; }       
        /// <summary>
        /// режим отображения колонок грида
        /// Compact=Все колонки меют динамическую ширину (режим 1)
        /// Full=Все колонки имеют фиксированную ширину (режим 2)
        /// </summary>
        public GridBox.ColumnWidthModeRef ColumnWidthMode { get; set; }
        /// <summary>
        /// массив с данными выбранной строки
        /// </summary>
        public Dictionary<string, string> SelectedItem { get; set; }
        /// <summary>
        /// включение фильтрации (DXW Grid)
        /// </summary>
        public bool EnableFiltering { get; set; } = false;
        /// <summary>
        /// Флаг для включения (true) отключения (false) сортировки при нажатии на шапку колонки
        /// </summary>
        public bool EnableSortingGrid { get; set; } = true;
        /// <summary>
        /// контент выбранной ячейки грида
        /// </summary>
        public string SelectedItemContent { get; set; }
        public DataGridHelperColumn SelectedColumn { get; set; }
        public delegate void OnSelectItemDelegate(Dictionary<string, string> selectedItem);
        /// <summary>
        /// коллбэк, вызывается при выборе строки
        /// кликом пользователя по строке или программно
        /// </summary>
        public OnSelectItemDelegate OnSelectItem;
        /// <summary>
        /// контекстное меню строки грида
        /// </summary>
        public Dictionary<string, DataGridContextMenuItem> Menu { get; set; }
        /// <summary>
        /// процессор команд
        /// </summary>
        public CommandController Commands { get; set; }

        public delegate void OnDblClickDelegate(Dictionary<string, string> selectedItem);
        /// <summary>
        /// коллбэк, вызывается двойном клике на строке
        /// </summary>
        public OnDblClickDelegate OnDblClick;

        public delegate void OnGridControlColumnsCollectionChangedDelegate(DevExpress.Xpf.Grid.GridColumnCollection columns);
        /// <summary>
        /// Коллбек, вызывается при изменении коллекции колонок после инициализации грида (Initialized)
        /// </summary>
        public OnGridControlColumnsCollectionChangedDelegate OnGridControlColumnsCollectionChanged;

        public delegate void OnColumnConstructedDelegate();
        /// <summary>
        /// Коллбек, вызывается после создания колонок 
        /// </summary>
        public OnColumnConstructedDelegate OnColumnConstructed;

        public enum MouseClickTypeRef
        {
            Undefined,
            LeftButtonClick,
            RightButtonClick,
            DoubleClick,
        }
        public MouseClickTypeRef MouseClickType { get; set; }
        
        /// <summary>
        /// контрол для поиска текста
        /// если пользователь вводит текст в контрол, производится фильтрация
        /// данных грида: покажутся только строки, содержащие введенный текст
        /// </summary>
        public System.Windows.Controls.TextBox SearchText { get; set; }
        
        public List<Dictionary<string, string>> Items { get; set; }
        public delegate void OnFilterItemsDelegate();
        /// <summary>
        /// коллбэк, будет вызван при необходимости программной фильтрации
        /// данных грида, всю работу по программной фильтрации производит
        /// этот коллбэк
        /// </summary>
        public OnFilterItemsDelegate OnFilterItems;
        public delegate void OnLoadItemsDelegate();
        /// <summary>
        /// коллбэк, будет вызван при необходимости обновить данные грида
        /// этот коллбэк должен получить и подготовить данные для грида
        /// </summary>
        public OnLoadItemsDelegate OnLoadItems;
        /// <summary>
        /// контрол, содержащий элементы управления грида
        /// на время обновления данных, все контролы, находящиеся в этом контейнере 
        /// блокируются
        /// </summary>
        public StackPanel Toolbar { get; set; }
        /// <summary>
        /// хелпер для простых запросов получения данных
        /// </summary>
        public RequestData QueryLoadItems { get; set; }
        
        /// <summary>
        /// режим автообновление, 
        /// true=грид будет обновлять данные в указанный интервал (AutoUpdateInterval)
        /// для подчиненных гридов установить false
        /// </summary>
        public bool ItemsAutoUpdate { get; set; }
        /// <summary>
        /// интервал автоматического обновления данных, сек
        /// </summary>
        public int AutoUpdateInterval { get; set; }
        /// <summary>
        /// стайлеры строки
        /// </summary>
        public Dictionary<StylerTypeRef, StylerDelegate> RowStylers;
        public string Descriription { get; set; }
        
        public Dictionary<string, DataTemplate> CellTplCache { get; set; }
        
        /// <summary>
        /// показать сплеш-скрин на время загрузки данных автоматически
        /// </summary>
        public bool UseProgressSplashAuto { get; set; }
        /// <summary>
        /// показать блок с прогресс-баром на сплеш-скрине
        /// </summary>
        public bool UseProgressBar { get; set; }
        /// <summary>
        /// задержка перед отображением прогресс-бара, мс (5000)
        /// </summary>
        public int ProgressBarInterval { get; set; }
        public int ProgressBarIntervalTick { get; set; }
        public bool Initialized { get; private set; }


        private Common.Timeout ProgressBarTimeout { get; set; }
        private Common.Timeout ProgressBarAbortTimeout { get; set; }
        private DateTime LastKeyboardEvent { get; set; }
        private string PerformanceLog { get; set; }
        private Profiler ReformanceProfiler { get; set; }
        private Profiler RowSelectProfiler { get; set; }
        private Dictionary<string, Style> ControlStyles { get; set; }
        private int GridContainerWidthOffset { get; set; }
        private int GridContainerWidthOffsetActual { get; set; }
        private bool Resized { get; set; }
        private bool Constructed { get; set; }
        private bool Loaded { get; set; }
        private bool Updated { get; set; }
        private bool Runned { get; set; }
        private bool Sorted { get; set; }
        private int Stage { get; set; }
        private Common.Timeout RunTimeout { get; set; }
        private bool UpdateItemsFirstTime { get; set; }
        private Common.Timeout AutoUpdateTimeout { get; set; }
        private Common.Timeout SearchTimeout { get; set; }
        private int SearchTimerTimeout { get; set; }
        private bool SearchingComplete { get; set; }
        private bool SearchingInProgress { get; set; }
        private int ColumnSymbolWidth { get; set; }
        public int ColumnWidthMin { get; set; }
        private int RowHeight { get; set; }
        private int SelectedItemIndex { get; set; }
        private string SelectedItemKey { get; set; }
        private string SelectedItemValue { get; set; }
        private Dictionary<string, GridControlBand> Bands { get; set; }
        private List<DataGridHelperColumn> Columns { get; set; }
        private Dictionary<string, DevExpress.Xpf.Grid.GridColumn> ColumnsDx { get; set; }
        private System.Data.DataTable DataTable { get; set; }
        private ListDataSet DataSet { get; set; }
        private bool SortingEnabled { get; set; }
        private DataGridHelperColumn SortColumn { get; set; }
        private ListSortDirection SortDirection { get; set; }
        private string PrimaryKey { get; set; }
        private bool InitBaseComplete {get;set;}=false;
        

        /// <summary>
        /// инициализация грида, запускает внутренние механизмы работы грида
        /// должен вызываться после всех настроек грида
        /// </summary>
        public void Init()
        {
            if(!InitBaseComplete)
            {
                InitBaseComplete=true;
                LoadStyles();
                BindEvents();
                InitSearch();
                InitGridControl();
                Central.Msg.Register(ProcessMessage);
            }
            StylesConstruct();
            Run();

            /*
                AutoWidth="False"
                PreviewMouseDown ="grid_cust_Click"
                ShowCheckBoxSelectorColumn="True"
                ShowCheckBoxSelectorInGroupRow="True"
                AllowFixedColumnMenu="True"
                ShowTotalSummary="True" ShowFixedTotalSummary="True"

                filtering
                //<dxg:GridControl FilterString="([OrderDate] &lt; #1/1/1995# AND [UnitPrice] &lt; 10) OR ([OrderDate] &gt;= #1/1/1996# AND [UnitPrice] &gt;= 100)" />

                styles concepts
                https://supportcenter.devexpress.com/ticket/details/t186017/gridcontrol-rowstyle

                row concepts
                https://docs.devexpress.com/WPF/DevExpress.Xpf.Grid.TableView.RowStyle

                template
                https://js.devexpress.com/React/Documentation/Guide/React_Components/Component_Configuration_Syntax/#Markup_Customization/Using_the_Template_Component
                https://docs.devexpress.com/WPF/6152/controls-and-libraries/data-grid/appearance-customization

                optimized and inplace edit
                https://docs.devexpress.com/WPF/17112/controls-and-libraries/data-grid/performance-improvement/optimized-mode

                bindings
                https://supportcenter.devexpress.com/ticket/details/t925592/build-binding-paths-in-wpf-data-grid-rows

                create xaml as xml
                https://stackoverflow.com/questions/3773154/building-a-datatemplate-in-c-sharp

                context menus
                https://docs.devexpress.com/WPF/6587/controls-and-libraries/data-grid/miscellaneous/context-menus

                selecton
                https://docs.devexpress.com/WPF/7359/controls-and-libraries/data-grid/focus-navigation-selection/multiple-row-selection 
             */
        }

        /// <summary>
        /// очистка конфигурации грида
        /// вызывается перед переинициализацией
        /// </summary>
        public void Clear()
        {
            try
            {
                RowStylers.Clear();

                DataSet=new ListDataSet();
                SelectedItem=new Dictionary<string, string>();            
                Items=new List<Dictionary<string, string>>();       
            
                DataTable.Columns.Clear();
                GridControl.Columns.Clear();
                GridControl.CustomColumnSort -= SortItems;
                ColumnsDx.Clear();

                GridControl.TotalSummary.Clear();
                GridControl.CustomSummary -= OnTotalProcess;
                GridView.ShowTotalSummary = false;                

                Constructed=false;
                Initialized=false;
                Stage=1;
                Runned=false;
            }
            catch(Exception e)
            {
            }
        }

        public Style GetStyle(string style)
        {
            var result = new Style();
            if(ControlStyles.ContainsKey(style))
            {
                result = ControlStyles[style];
            }
            return result;
        }
        
        public void LoadStyles()
        {
            ControlStyles.Add("GridBox4CellContainer", (Style)GridContainer.TryFindResource("GridBox4CellContainer"));
            ControlStyles.Add("GridBox4CellContainerBorder", (Style)GridContainer.TryFindResource("GridBox4CellContainerBorder"));
            ControlStyles.Add("GridBox4CellCheckBox", (Style)GridContainer.TryFindResource("GridBox4CellCheckBox"));
            ControlStyles.Add("GridBox4CellCheckBoxRo", (Style)GridContainer.TryFindResource("GridBox4CellCheckBoxRo"));
            ControlStyles.Add("GridBox4CellTextBox", (Style)GridContainer.TryFindResource("GridBox4CellTextBox"));
            ControlStyles.Add("GridBox4CellTextBlock", (Style)GridContainer.TryFindResource("GridBox4CellTextBlock"));
            ControlStyles.Add("GridBox4Row", (Style)GridContainer.TryFindResource("GridBox4Row"));
            ControlStyles.Add("GridBox4TotalCellStyle", (Style)GridContainer.TryFindResource("GridBox4TotalCellStyle"));
        }

        public void InitGridControl()
        {
            GridView.UseLightweightTemplates = DevExpress.Xpf.Grid.UseLightweightTemplates.None;
            GridControl.ItemsSource = DataTable;
            GridControl.IsFilterEnabled = EnableFiltering;
            GridView.IsColumnMenuEnabled = EnableFiltering;
            GridControl.AllowInitiallyFocusedRow = true;
            GridView.ShowIndicator = false;
            GridView.RowStyle = GetStyle("GridBox4Row");
            GridView.Tag = Name;
        }

        void View_CanSelectCell(object sender, CanSelectCellEventArgs e)
        {
            e.CanSelectCell = true;
        }

        private int RunInnerCounter { get; set; } = 0;
        private void RunInner()
        {
            /*
                0
                1   resized
                2   constructed
                3   loaded
                4   updated
             */
            var retry = true;
            var j = RunInnerCounter;
            {
                j++;

                Central.Dbg($"RunInner ({RunInnerCounter.ToString().SPadLeft(5)}) Name=[{Name.ToString().SPadLeft(20)}] DebugName=[{DebugName.ToString().SPadLeft(20)}] stage=[{Stage}]");

                //ширина контейнера определена
                if(Stage == 0)
                {
                    if(!Resized)
                    {
                        var i = GetGridContainerWidth();
                        DebugPerformanceLogAdd($"stage=[{Stage}] i=[{i}]");
                        if(i > 0)
                        {
                            Resized = true;
                            Stage = 1;
                        }
                    }
                }

                if(Stage == 1)
                {
                    if(!Constructed)
                    {
                        ColumnWidthSetDefaults();
                        ColumnConstruct();
                        TotalsConstruct();
                        DebugPerformanceLogAdd($"stage=[{Stage}]");
                        Constructed = true;
                        Stage = 2;
                    }
                }

                if(Stage == 2)
                {
                    if(!Loaded)
                    {                        
                        if(AutoUpdateInterval > 0)
                        {
                            LoadItems();
                            DebugPerformanceLogAdd($"stage=[{Stage}] run LoadItems");
                            Stage = 3;
                        }
                        else
                        {
                            LoadItems();
                            DebugPerformanceLogAdd($"stage=[{Stage}] no autoload");
                            Stage = 3;
                        }
                    }
                    else
                    {
                        Stage = 3;
                    }
                }

                if(Stage == 3)
                {
                    if(Updated)
                    {
                        DebugPerformanceLogAdd($"stage=[{Stage}] Updated");
                        Stage = 4;
                    }

                    if(j > 10)
                    {
                        DebugPerformanceLogAdd($"stage=[{Stage}] Timeout");
                        Stage = 4;
                    }
                }

                if(Stage == 4)
                {
                    if(SortingEnabled)
                    {
                        var d = DevExpress.Data.ColumnSortOrder.Ascending;
                        var p = SortColumn.Path;
                        if(SortDirection == ListSortDirection.Ascending)
                        {
                            d = DevExpress.Data.ColumnSortOrder.Ascending;
                        }
                        else
                        {
                            d = DevExpress.Data.ColumnSortOrder.Descending;
                        }
                        if(!p.IsNullOrEmpty())
                        {
                            GridControl.Columns[SortColumn.Path].SortOrder = d;
                        }
                    }

                    RunTimeout.Finish();
                    RunAutoupdateTimer();
                    DebugPerformanceLogAdd($"stage=[{Stage}]");
                    Stage = 5;
                }

                DebugPerformanceLogAdd($"stage=[{Stage}]");
                if(Stage >= 5)
                {
                    Initialized = true;
                    Central.Dbg($"Gridbox4 initialized name=[{Name}]");
                }
            }

            RunInnerCounter = j;
        }

        private void RunAutoupdateTimer()
        {
            if(AutoUpdateInterval > 0)
            {
                AutoUpdateTimeout = new Common.Timeout(
                    AutoUpdateInterval,
                    () =>
                    {
                        if(ItemsAutoUpdate)
                        {
                            LoadItems();
                        }
                    },
                    true,
                    false
                );
                {
                    AutoUpdateTimeout.Restart();
                }
            }
        }

        public void Run()
        {
            if(Stage < 5)
            {
                if(!Runned)
                {
                    Runned = true;
                    RunTimeout = new Common.Timeout(
                        1,
                        () =>
                        {
                            RunInner();
                        },
                        true,
                        false
                    );
                    RunTimeout.SetIntervalMs(100);
                    RunTimeout.Run();
                }
            }
        }

        public void InitSearch()
        {
            if(SearchTimerTimeout > 0)
            {
                SearchTimeout = new Common.Timeout(
                   1,
                   () => {
                       SearchCheckText();
                   },
                   true,
                   false
                );
                SearchTimeout.SetIntervalMs(SearchTimerTimeout);
            }

            SearchingComplete = false;
            SearchingInProgress = false;
        }

        private void GridControl_CustomUnboundColumnData(object sender, DevExpress.Xpf.Grid.GridColumnDataEventArgs e)
        {
            var r0 = GridControl;
        }

        private void GridView_CellValueChanging(object sender, DevExpress.Xpf.Grid.CellValueChangedEventArgs e)
        {
            var r0 = GridControl;
        }

        private void GridView_CustomCellAppearance(object sender, DevExpress.Xpf.Grid.CustomCellAppearanceEventArgs e)
        {
            var r0 = GridControl;
        }

        /// <summary>
        /// установка списка колонок грида
        /// </summary>
        /// <param name="columns"></param>
        public void SetColumns(List<DataGridHelperColumn> columns)
        {
            Columns = columns;
        }

        /// <summary>
        /// установка первичного ключа грида
        /// если первичный ключ установлен, по нему выполняется сортировка по умолчанию)
        /// </summary>
        /// <param name="path"></param>
        public void SetPrimaryKey(string path)
        {
            PrimaryKey = path;
        }

        public void SetCommander(CommandController cmd)
        {
            Commands = cmd;
            cmd.AddGrid(this);
        }

        public string GetPrimaryKey()
        {
            return PrimaryKey;
        }

        public int GetPrimaryIndex()
        {
            if(!string.IsNullOrEmpty(PrimaryKey))
            {
                var column = GridControl.Columns[PrimaryKey];
                if(column != null)
                {
                    return GridControl.Columns.IndexOf(column);
                }

            }

            return 0;
        }

        /// <summary>
        /// установка сортировки грида
        /// </summary>
        /// <param name="path"></param>
        /// <param name="direction"></param>
        public void SetSorting(string path, ListSortDirection direction = ListSortDirection.Ascending)
        {
            SortingEnabled = true;
            SortColumn = ColumnGet(path);
            SortDirection = direction;

            DebugPerformanceLogAdd($"SetSorting {path} {SortDirection}");
        }

        public async void UpdateItems2(ListDataSet ds = null)
        {
            if(ds != null)
            {
                DataSet = ds;
            }

            await Task.Run(() =>
            {
                while(!Initialized)
                {
                    System.Threading.Thread.Sleep(1000);
                }
                if(Initialized)
                {
                    UpdateItems();
                }
            });
        }

        public async void UpdateItems()
        {
            UpdateItems(null);
        }

        public async void UpdateItems(ListDataSet ds = null)
        {
            DebugPerformanceLogAdd("UpdateItems");

            if(!Initialized)
            {
                await Task.Run(() =>
                {
                    while(!Initialized)
                    {
                        System.Threading.Thread.Sleep(1000);
                    }
                    if(Initialized)
                    {
                        var rr = 0;
                        UpdateItems();
                    }
                });
            }

            if(ds != null)
            {
                DataSet = ds;
            }

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                DoUpdateItems();
            });

            ProcessToolbar(1, "UpdateItems", 1);
        }

        private bool _ChainUpdateSelect { get; set; } = false;
        private bool _SelectAll { get; set; } = false;
        public void DoUpdateItems()
        {
            // 0=clear, 1=first, 2=custom
            var selectMode = 0;
            var selectId = "";

            {
                Items = DataSet.Items;
                Items = ItemsSearch(Items);
                if(OnFilterItems != null)
                {
                    OnFilterItems.Invoke();
                }
            }

            {
                //if(!ItemsAutoUpdate)
                //{
                //    RowManuallySelected = false;
                //}
                
                RowsConstruct();
                //SelectedItem.Clear();
                if (Items.Count == 0)
                {
                    SelectedItem = new Dictionary<string, string>();
                }

                if(Name == "OrderGrid")
                {
                    var r8 = 0;
                }

                {
                    DevExpress.Xpf.Grid.GridColumn sortColumn = null;
                    if(ColumnsDx.ContainsKey(SortColumn.Path))
                    {
                        sortColumn = ColumnsDx[SortColumn.Path];
                    }
                    if(sortColumn != null)
                    {
                        if(SortDirection == ListSortDirection.Ascending)
                        {
                            GridControl.SortBy(sortColumn, ColumnSortOrder.Ascending);
                        }
                        else
                        {
                            GridControl.SortBy(sortColumn, ColumnSortOrder.Descending);
                        }
                    }
                }
            }

            var procCommands = false;
            if(Commands != null)
            {
                if(Commands.Message != null)
                {
                    var complete = false;
                    if(!RowToSelect.IsNullOrEmpty())
                    {
                        selectMode = 2;
                        complete = true;
                        selectId = RowToSelect;
                        RowToSelect = "";                        
                    }
                    else
                    {
                        var action = Commands.Message.Action.ToString();
                        action = action.ToLower();
                        if(action.IndexOf("refresh") > -1)
                        {
                            var id = Commands.Message.Message.ToString();
                            if(!id.IsNullOrEmpty())
                            {
                                selectMode = 2;
                                complete = true;
                                selectId = id;
                            }
                        }

                        //if(action.IndexOf("FocusGot") > -1)
                        //{
                        //    selectMode = 1;
                        //    complete = true;
                        //}
                    }

                    if(complete)
                    {
                        procCommands = true;
                    }
                }
            }

            if(!procCommands)
            {
                var rr = SelectedItem;
                var rn = Name;
                var id = "";
                if(SelectedItem.Count > 0)
                {
                    if(!PrimaryKey.IsNullOrEmpty())
                    {
                        id = (SelectedItem.CheckGet(PrimaryKey)).ToInt().ToString();
                    }
                }


                if(Items.Count > 0 && Items[0].Count != 0)
                {
                    if(!id.IsNullOrEmpty())
                    {
                        if(CheckRowInItems(SelectedItem))
                        {
                            selectMode = 2;
                            selectId = id;
                        }
                        else
                        {
                            selectMode = 1;
                        }
                    }
                    else
                    {
                        selectMode = 1;
                    }
                }
                else
                {
                    selectMode = 0;
                }
            }

            RowToSelect = "";

            if(selectMode == 2)
            {
                if(!RowManuallySelected)
                {
                    selectMode = 1;
                }
            }

            //if(Name== "EmployeeGrid")
            if(Name == "GroupGrid")
            {
                var r8 = 0;
                var r81 = selectMode;
                var r82 = selectId;
                var r83 = PrimaryKey;
                var r84 = SelectedItem;
            }

            if(_SelectAll)
            {
                _SelectAll = false;
                selectMode = 2;
            }

            switch(selectMode)
            {
                case 0:
                    {
                        RowManuallySelected = false;
                        ProcessRowSelection(0);
                    }
                    break;

                case 1:
                    {
                        SelectRowFirst();
                    }
                    break;

                case 2:
                    {
                        SelectRowByKey(selectId);
                    }
                    break;
            }

            DebugPerformanceLogAdd("RenderItems");
        }

        /// <summary>
        /// содержится ли указанная строка в коллекции строк
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private bool CheckRowInItems(Dictionary<string, string> row)
        {
            var result = false;
            if(Items.Count > 0)
            {
                if(!PrimaryKey.IsNullOrEmpty())
                {
                    var v = row.CheckGet(PrimaryKey);
                    if(!v.IsNullOrEmpty())
                    {
                        foreach(var r in Items)
                        {
                            if(r.CheckGet(PrimaryKey) == v)
                            {
                                result = true;
                            }
                        }
                    }
                }
            }
            return result;
        }

        void SortItems(object sender, CustomColumnSortEventArgs e)
        {
            DataGridHelperColumn dataGridHelperColumn = Columns.FirstOrDefault(x => x.Path == e.Column.FieldName);
            if (dataGridHelperColumn != null)
            {
                switch (dataGridHelperColumn.ColumnType)
                {
                    case ColumnTypeRef.String:
                        break;
                    case ColumnTypeRef.Integer:
                        break;
                    case ColumnTypeRef.Double:
                        {
                            var v1 = e.Value1.ToString().ToDouble();
                            var v2 = e.Value2.ToString().ToDouble();
                            var r = v1.CompareTo(v2);
                            e.Result = r;
                            e.Handled = true;
                        }
                        break;

                    case ColumnTypeRef.DateTime:
                        {
                            var v1 = e.Value1.ToString().ToDateTime();
                            var v2 = e.Value2.ToString().ToDateTime();
                            var r = v1.CompareTo(v2);
                            e.Result = r;
                            e.Handled = true;
                        }
                        break;

                    case ColumnTypeRef.Boolean:
                        break;
                    case ColumnTypeRef.Image:
                        break;
                    case ColumnTypeRef.SelectBox:
                        break;
                    default:
                        break;
                }
            }
        }

        public void ClearItems()
        {
            DataTable.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mode">0=занято/заблокировано, 1=свободно/разблокировано</param>
        /// <param name="from">источник вызова, произвольная строка для отладки</param>
        /// <param name="source">0=вызов вручную, 1=вызов автоматически</param>
        private void ProcessToolbar(int mode = 0, string from = "", int source = 0)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                ProcessToolbar2(mode, from, source);
            });
        }

        /// <summary>
        /// 0=вручную, 1=автоматически
        /// </summary>
        private int ProgressMode { get; set; }
        /// <summary>
        /// 0=disabled, 1=await show, 2=show, 3=shown
        /// </summary>
        private int ProgressStage { get; set; }
        private int ProgressTimeCurrent { get; set; }
        private DateTime ProgressStart { get; set; }

        private int ProgressDelay { get; set; }
        private int ProgressCurrent { get; set; }
        private string ProgressNote { get; set; }
        /// <summary>
        /// работа с прогресс-баром
        /// </summary>
        /// <param name="mode"></param>
        private void ProgressBarTimeoutProcess(int mode = 0)
        {
            switch(mode)
            {
                //tick
                default:
                    {
                        var today = DateTime.Now;
                        var dt = (TimeSpan)(today - ProgressStart);
                        ProgressTimeCurrent = dt.TotalMilliseconds.ToInt();

                        if(ProgressDelay > 0)
                        {
                            if(ProgressTimeCurrent > ProgressDelay)
                            {
                                ProgressDelay = ProgressTimeCurrent;
                                Splash2ProgressBar.IsIndeterminate = true;
                            }
                        }

                        Splash2Dbg.Text = "";
                        Splash2Timer.Text = "";
                        if(Central.DebugMode)
                        {
                            var t = (ProgressTimeCurrent / 1000).ToString();
                            Splash2Timer.Text = $"ожидание: {t}";

                            //Splash2Dbg.Text = $"{Splash2ProgressBar.Minimum}-{Splash2ProgressBar.Maximum} {ProgressCurrent} ({ProgressStep})";
                        }

                        if(ProgressDelay > 0)
                        {
                            ProgressCurrent = ProgressTimeCurrent;
                            Splash2ProgressBar.Value = ProgressCurrent;
                        }

                        //ожидание
                        if(ProgressStage == 1)
                        {
                            if(ProgressTimeCurrent > ProgressBarInterval)
                            {
                                ProgressStage = 2;
                            }
                        }

                        //отображение
                        if(ProgressStage == 2)
                        {
                            Splash2.Visibility = Visibility.Visible;
                            Splash2.SetZIndex(30);

                            if(UseProgressBar)
                            {
                                if(ProgressDelay > 0)
                                {
                                    Splash2ProgressBar.IsIndeterminate = false;
                                    Splash2ProgressBar.Minimum = 0;
                                    Splash2ProgressBar.Maximum = ProgressDelay;
                                }
                                else
                                {
                                    Splash2ProgressBar.IsIndeterminate = true;
                                }

                                if(!ProgressNote.IsNullOrEmpty())
                                {
                                    Splash2Note.Text = ProgressNote;
                                }
                                Splash2ProgressBarContainer.Visibility = Visibility.Visible;
                            }

                            ProgressStage = 3;
                        }
                    }
                    break;

                //init
                case -1:
                    {
                        ProgressMode = 0;
                        ProgressStage = 0;
                        ProgressTimeCurrent = 0;
                        ProgressStart = DateTime.Now;
                        ProgressCurrent = 0;
                        ProgressDelay = 0;
                        Splash2.Visibility = Visibility.Collapsed;
                        Splash2ProgressBarContainer.Visibility = Visibility.Collapsed;
                    }
                    break;

                //start
                case 1:
                    {
                        ProgressTimeCurrent = 0;
                        if(ProgressMode == 1 && UseProgressSplashAuto)
                        {
                            //auto
                            ProgressStage = 1;
                        }
                        else
                        {
                            //manual
                            ProgressStage = 2;
                        }
                        ProgressStart = DateTime.Now;
                        ProgressCurrent = 0;
                        ProgressBarTimeout.Restart();


                        if(UseProgressSplashAuto)
                        {
                            ProgressBarAbortTimeout.Restart();
                        }
                    }
                    break;

                //finish
                case 9:
                    {
                        ProgressDelay = ProgressTimeCurrent;
                        ProgressTimeCurrent = 0;
                        ProgressCurrent = 0;
                        ProgressBarTimeout.Finish();
                        Splash2.Visibility = Visibility.Collapsed;
                        Splash2ProgressBarContainer.Visibility = Visibility.Collapsed;
                    }
                    break;
            }
        }


        public void ProcessToolbar2(int mode = 0, string from = "", int source = 0)
        {
            switch(mode)
            {
                // отображается/заблокировано
                case 0:
                    {
                        if(UseProgressSplashAuto)
                        {
                            if(Toolbar != null)
                            {
                                Toolbar.IsEnabled = false;
                            }
                        }

                        if(source == 1)
                        {
                            ProgressMode = 1;
                        }
                        ProgressBarTimeoutProcess(1);
                    }
                    break;

                // скрыто/разблокировано
                case 1:
                    {
                        if(Toolbar != null)
                        {
                            Toolbar.IsEnabled = true;
                        }

                        ProgressBarTimeoutProcess(9);
                    }
                    break;
            }

            Central.Dbg($"ProcessToolbar mode=[{mode}] from=[{from}]");
        }

        /// <summary>
        /// получение данных гридом
        /// вызывается коллбэк: OnLoadItems или QueryLoadItems
        /// </summary>
        public void LoadItems()
        {
            if(Constructed)
            {
                DebugPerformanceLogAdd("");

                if(OnLoadItems != null)
                {
                    ProcessToolbar(0, "LoadItems", 1);

                    Loaded = true;
                    DoLoadItems();
                }
                else
                {
                    if(QueryLoadItems != null)
                    {
                        LoadItemsInner();
                    }
                }
                DebugPerformanceLogAdd("");
            }
        }

        private async void DoLoadItems()
        {
            OnLoadItems.Invoke();
        }

        private async void LoadItemsInner()
        {
            bool resume = true;

            if(Toolbar != null)
            {
                Toolbar.IsEnabled = false;
            }

            if(resume)
            {
                var q = new LPackClientQuery();

                QueryLoadItems.Params = new Dictionary<string, string>();

                if(QueryLoadItems.BeforeRequest != null)
                {
                    QueryLoadItems.BeforeRequest.Invoke(QueryLoadItems);
                }

                q.Request.SetParam("Module", QueryLoadItems.Module);
                q.Request.SetParam("Object", QueryLoadItems.Object);
                q.Request.SetParam("Action", QueryLoadItems.Action);

                if(QueryLoadItems.Params != null)
                {
                    q.Request.SetParams(QueryLoadItems.Params);
                }

                q.Request.Timeout = QueryLoadItems.Timeout;
                q.Request.Attempts = QueryLoadItems.Attempts;

                q.DoQuery();

                //await Task.Run(() =>
                //{
                //    q.DoQuery();
                //});

                if(q.Answer.Status == 0)
                {
                    var answerData = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if(answerData != null)
                    {
                        Loaded = true;
                        QueryLoadItems.AnswerData=answerData;
                        if(!QueryLoadItems.AnswerSectionKey.IsNullOrEmpty())
                        {
                            var ds = ListDataSet.Create(answerData, QueryLoadItems.AnswerSectionKey);
                            if(QueryLoadItems.AfterRequest != null)
                            {
                                ds = QueryLoadItems.AfterRequest.Invoke(QueryLoadItems, ds);
                            }
                            UpdateItems(ds);

                            if(QueryLoadItems.AfterUpdate != null)
                            {
                                QueryLoadItems.AfterUpdate.Invoke(QueryLoadItems, ds);
                            }
                            
                            QueryLoadItems.OnCompleteGrid?.Invoke(ds);
                        }
                    }
                }
            }

            if(Toolbar != null)
            {
                Toolbar.IsEnabled = true;
            }
        }

        public void CopyCellValue()
        {
            try
            {
                if(SelectedItem.Count > 0)
                {
                    if(SelectedColumn != null)
                    {
                        var k = SelectedColumn.Path;
                        var r = SelectedItem.CheckGet(k).ToString();
                        if(!r.IsNullOrEmpty())
                        {
                            System.Windows.Clipboard.SetText(r);
                        }
                    }
                }
            }
            catch(Exception e)
            {

            }
        }

        /// <summary>
        /// возвращает список выбранных строк грида
        /// проверка факта выбранности строки идет по колонке _SELECTED
        /// </summary>
        /// <returns></returns>
        public List<Dictionary<string, string>> GetItemsSelected()
        {
            var result = new List<Dictionary<string, string>>();
            RefreshGridItems();
            foreach(Dictionary<string, string> row in Items)
            {
                if(row.CheckGet("_SELECTED").ToBool())
                {
                    result.Add(row);
                }
            }
            return result;
        }

        /// <summary>
        /// возвращает список строк грида
        /// </summary>
        /// <returns></returns>
        public List<Dictionary<string, string>> GetItems()
        {
            var result = new List<Dictionary<string, string>>();
            RefreshGridItems();
            result = Items;
            return result;
        }

        /// <summary>
        /// возврат строки (массива колонок) из исходного датасета
        /// </summary>
        /// <param name="primaryValue"></param>
        /// <returns></returns>
        public Dictionary<string, string> GetRow(GridBox4CellData cell)
        {
            var result = new Dictionary<string, string>();
            var primaryValue = cell.Row.CheckGet(PrimaryKey);
            {
                //foreach(Dictionary<string, string> row in DataSet.Items)
                foreach(Dictionary<string, string> row in Items)
                {
                    var v = row.CheckGet(PrimaryKey).ToString();
                    if(v == primaryValue)
                    {
                        result = row;
                        break;
                    }
                }
            }
            return result;
        }

        public void RefreshGridItem(Dictionary<string, string> item)
        {
            if(Items.Count > 0)
            {
                var currentRow = new Dictionary<string, string>();
                foreach(Dictionary<string, string> row in Items)
                {
                    var v = item.CheckGet(PrimaryKey);
                    if(row.CheckGet(PrimaryKey) == v)
                    {
                        currentRow = row;
                        break;
                    }
                }
                currentRow = item;
            }
        }

        private void RefreshGridItems()
        {
            var items = new List<Dictionary<string, string>>();

            DataRow[] foundRows = DataTable.Select();
            foreach(DataRow dr in foundRows)
            {
                var row = new Dictionary<string, string>();
                foreach(DataColumn dc in DataTable.Columns)
                {
                    var k = dc.ColumnName;
                    var v = dr[k].ToString();
                    row.CheckAdd(k, v);
                }
                items.Add(row);

                if(!SelectedItemKey.IsNullOrEmpty() && !SelectedItemValue.IsNullOrEmpty())
                {
                    var v = row.CheckGet(SelectedItemKey);
                    if(v == SelectedItemValue)
                    {
                        SelectedItem = row;
                    }
                }
            }

            if(items.Count > 0)
            {
                Items = items;
            }
        }

        private void SearchCheckText()
        {
            SearchTimeout.Finish();
            bool doSearch = false;

            if(!doSearch)
            {
                if(SearchText != null)
                {
                    var t = SearchText.Text;
                    if(!t.IsNullOrEmpty())
                    {
                        if(t.Length >= 1)
                        {
                            doSearch = true;
                            SearchingComplete = true;
                        }
                    }
                    else
                    {
                        if(SearchingComplete)
                        {
                            doSearch = true;
                            SearchingComplete = false;
                        }
                    }
                }
            }

            if(doSearch)
            {
                SearchingInProgress = true;
                UpdateItems();
            }
        }

        private bool _SearchComplete { get; set; }
        private List<Dictionary<string, string>> ItemsSearch(List<Dictionary<string, string>> list)
        {
            _SearchComplete = false;
            if(list.Count > 0)
            {
                bool doFiltering = false;
                string s = "";

                if(SearchText != null)
                {
                    var t = SearchText.Text;
                    if(!t.IsNullOrEmpty())
                    {
                        doFiltering = true;
                        s = SearchText.Text.Trim().ToLower();
                    }
                }

                if(doFiltering)
                {
                    var sList = new List<string>();
                    if(s.IndexOf(",") > -1)
                    {
                        sList = s.Split(',').ToList();
                    }
                    else
                    {
                        sList.Add(s);
                    }

                    var items = new List<Dictionary<string, string>>();
                    foreach(Dictionary<string, string> row in list)
                    {
                        bool include = false;

                        foreach(string sw in sList)
                        {
                            foreach(KeyValuePair<string, string> cell in row)
                            {
                                if(!string.IsNullOrEmpty(cell.Value))
                                {
                                    //Ищем колонку для ячейки датасета
                                    foreach (var c in Columns)
                                    {
                                        if (c.Path == cell.Key)
                                        {
                                            if ((c.Visible && !c.Hidden) || c.Searchable)
                                            {
                                                if (cell.Value.ToLower().IndexOf(sw) > -1)
                                                {
                                                    include = true;
                                                }
                                            }
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        if(include)
                        {
                            items.Add(row);
                        }
                    }

                    if(items.Count != list.Count)
                    {
                        _SearchComplete = true;
                    }
                    list = items;
                }
            }
            return list;
        }

        private bool CheckColumn(DataGridHelperColumn c)
        {
            //var result = false;

            //if(
            //    c.Visible
            //    && !c.Hidden
            //    && c.Path!="_"
            //)
            //{
            //    result = true;
            //}

            //return result;

            return true;
        }

        private void ItemSelectedSetByPrimaryValue(string value, string key = "")
        {
            var complete = false;
            if(key.IsNullOrEmpty())
            {
                key = PrimaryKey;
            }
            var j = 0;

            var list = GetListFromDataTable();
            foreach(Dictionary<string, string> row in list)
            //foreach(var row in DataSet.Items)
            {
                var v = row.CheckGet(key);
                if(v == value)
                {
                    SelectedItemKey = key;
                    SelectedItemValue = v;
                    //SelectedItem = row;
                    SelectedItemIndex = j;
                    complete = true;
                    break;
                }
                j++;
            }

            if(complete)
            {
                var primaryColumn = ColumnGet(key);
                var primaryColumnType = primaryColumn.ColumnType;

                foreach(var row in DataSet.Items)
                {
                    string v = "";
                    switch(primaryColumnType)
                    {
                        case ColumnTypeRef.Integer:
                        v = row.CheckGet(key).ToInt().ToString();
                        break;

                        case ColumnTypeRef.Boolean:
                        v = row.CheckGet(key).ToBool().ToString();
                        break;

                        default:
                        v = row.CheckGet(key);
                        break;
                    }

                    if(v == value)
                    {
                        SelectedItem = row;
                        break;
                    }
                }
            }
        }

        public void ProcessCellClick(RoutedEventArgs e, bool doClickInvoke = true)
        {
            /*
                SelectedColumn
                SelectedItem
             */

            var cellTag = "";
            var cellPath = "";
            var cellInfo = new Dictionary<string, string>();
            var row = new Dictionary<string, string>();
            var cellKeys = new List<string>();
            var cellVals = new List<string>();
            var sourceName = "";

            if (e.Source != null)
            {
                try
                {
                    //var tv = (TableView)e.Source;
                    if (e.Source is TableView tv)
                    {
                        sourceName = tv.Tag.ToString();
                    }
                }
                catch (Exception ex)
                {

                }
            }

            var h = GridView.CalcHitInfo((DependencyObject)e.OriginalSource);
            if (h != null)
            {
                if (h.Column != null)
                {
                    cellTag = h.Column.Tag.ToString();
                    if (!cellTag.IsNullOrEmpty())
                    {
                        cellInfo = GetCellInfo(cellTag);
                        cellPath = cellInfo.CheckGet("PATH");
                        SelectedColumn = ColumnGet(cellPath);
                    }
                }
            }

            var c = (System.Data.DataRowView)GridControl.CurrentItem;
            if (c != null)
            {
                foreach (var o in c.DataView.Table.Columns)
                {
                    cellKeys.Add(o.ToString());
                }

                foreach (var o in c.Row.ItemArray)
                {
                    cellVals.Add(o.ToString());
                }

                if (cellKeys.Count > 0)
                {
                    int j = 0;
                    foreach (var x in cellKeys)
                    {
                        var k = cellKeys[j].ToString();
                        var v = cellVals[j].ToString();
                        row.Add(k, v);
                        j++;
                    }
                }
            }

            if (row.Count > 0)
            {
                if (!PrimaryKey.IsNullOrEmpty())
                {
                    var v = row.CheckGet(PrimaryKey);
                    ItemSelectedSetByPrimaryValue(v, PrimaryKey);
                    SelectedItemIndex = GridView.FocusedRowHandle;
                }
            }

            if (SelectedItem.Count > 0)
            {
                if (MouseClickType == MouseClickTypeRef.RightButtonClick)
                {
                    //Central.Dbg($"CellMenuShow ({Name}) {SelectedItem.CheckGet("ID").ToString()}");

                    var timeout = new Common.Timeout(
                        10,
                        () => {
                            CellMenuShow();
                        }
                    );
                    timeout.SetIntervalMs(100);
                    timeout.Run();
                    //CellMenuShow();
                }

                if (
                    MouseClickType == MouseClickTypeRef.LeftButtonClick
                    || MouseClickType == MouseClickTypeRef.RightButtonClick
                )
                {
                    if (SelectedColumn != null)
                    {
                        if (doClickInvoke)
                        {
                            if (SelectedColumn.OnClickAction != null)
                            {
                                SelectedColumn.OnClickAction.Invoke(SelectedItem, null);
                            }
                        }
                    }

                    ProcessRowSelection(2);
                }

                if (MouseClickType == MouseClickTypeRef.DoubleClick)
                {
                    if (Commands != null)
                    {
                        Commands.ProcessDoubleClick(sourceName);
                    }

                    if (OnDblClick != null)
                    {
                        OnDblClick.Invoke(SelectedItem);
                    }
                }
            }
        }

        public void ProcessCellClick(MouseButtonEventArgs e, bool doClickInvoke = true)
        {
            /*
                SelectedColumn
                SelectedItem
             */

            var cellTag = "";
            var cellPath = "";
            var cellInfo = new Dictionary<string, string>();
            var row = new Dictionary<string, string>();
            var cellKeys = new List<string>();
            var cellVals = new List<string>();
            var sourceName = "";

            if(e.Source != null)
            {
                try
                {
                    //var tv = (TableView)e.Source;
                    if(e.Source is TableView tv)
                    {
                        sourceName = tv.Tag.ToString();
                    }
                }
                catch(Exception ex)
                {

                }
            }

            var h = GridView.CalcHitInfo((DependencyObject)e.OriginalSource);
            if(h != null)
            {
                if(h.Column != null)
                {
                    cellTag = h.Column.Tag.ToString();
                    if(!cellTag.IsNullOrEmpty())
                    {
                        cellInfo = GetCellInfo(cellTag);
                        cellPath = cellInfo.CheckGet("PATH");
                        SelectedColumn = ColumnGet(cellPath);
                    }
                }
            }

            var c = (System.Data.DataRowView)GridControl.CurrentItem;
            if(c != null)
            {
                foreach(var o in c.DataView.Table.Columns)
                {
                    cellKeys.Add(o.ToString());
                }

                foreach(var o in c.Row.ItemArray)
                {
                    cellVals.Add(o.ToString());
                }

                if(cellKeys.Count > 0)
                {
                    int j = 0;
                    foreach(var x in cellKeys)
                    {
                        var k = cellKeys[j].ToString();
                        var v = cellVals[j].ToString();
                        row.Add(k, v);
                        j++;
                    }
                }
            }

            if(row.Count > 0)
            {
                if(!PrimaryKey.IsNullOrEmpty())
                {
                    var v = row.CheckGet(PrimaryKey);
                    ItemSelectedSetByPrimaryValue(v, PrimaryKey);
                    SelectedItemIndex = GridView.FocusedRowHandle;
                }
            }

            if(SelectedItem.Count > 0)
            {
                if(MouseClickType == MouseClickTypeRef.RightButtonClick)
                {
                    //Central.Dbg($"CellMenuShow ({Name}) {SelectedItem.CheckGet("ID").ToString()}");

                    var timeout = new Common.Timeout(
                        10,
                        () => {
                            CellMenuShow();
                        }
                    );
                    timeout.SetIntervalMs(100);
                    timeout.Run();
                    //CellMenuShow();
                }

                if(
                    MouseClickType == MouseClickTypeRef.LeftButtonClick
                    || MouseClickType == MouseClickTypeRef.RightButtonClick
                )
                {
                    if(SelectedColumn != null)
                    {
                        if(doClickInvoke)
                        {
                            if(SelectedColumn.OnClickAction != null)
                            {
                                SelectedColumn.OnClickAction.Invoke(SelectedItem, null);
                            }
                        }
                    }

                    ProcessRowSelection(2);
                }

                if(MouseClickType == MouseClickTypeRef.DoubleClick)
                {
                    if(Commands != null)
                    {
                        Commands.ProcessDoubleClick(sourceName);
                    }

                    if(OnDblClick != null)
                    {
                        OnDblClick.Invoke(SelectedItem);
                    }
                }
            }
        }

        private DataGridHelperColumn ColumnGet(string path)
        {
            var result = new DataGridHelperColumn();
            foreach(var c in Columns)
            {
                if(CheckColumn(c))
                {
                    if(c.Path == path)
                    {
                        result = c;
                        break;
                    }
                }
            }
            return result;
        }

        private void ColumnWidthSetDefaults()
        {
            var totalWidth = 0;
            foreach(var c in Columns)
            {
                if(CheckColumn(c))
                {
                    if(c.Width2 == 0)
                    {
                        switch(c.ColumnType)
                        {
                            case ColumnTypeRef.Boolean:
                                {
                                    c.Width2 = 2;
                                }
                                break;

                            case ColumnTypeRef.DateTime:
                                {
                                    //12345678901234567890
                                    //10.02.2023 12:45:45
                                    //10.02.2023
                                    c.Width2 = 20;
                                }
                                break;

                            case ColumnTypeRef.Integer:
                            case ColumnTypeRef.Double:
                                {
                                    //1234567890
                                    //10000
                                    c.Width2 = 5;
                                }
                                break;

                            case ColumnTypeRef.String:
                            default:
                                {
                                    c.Width2 = 16;
                                }
                                break;
                        }
                    }
                    totalWidth = totalWidth + c.Width2;
                }
            }

            if(ColumnWidthMode == GridBox.ColumnWidthModeRef.Compact)
            {
                foreach(var c in Columns)
                {
                    if (
                        c.Visible
                        && !c.Hidden
                        && c.Path != "_"
                    )
                    {
                        c.WidthRelative = (int)((double)c.Width2 / (double)totalWidth * (double)1000);
                    }
                }
            }
        }

        private int GetGridContainerWidth()
        {
            var result = 0;
            result = (int)GridContainer.ActualWidth;
            var offset = GridContainerWidthOffset;
            if(result > 1024)
            {
                offset = (int)((double)offset * 1.5);
            }
            GridContainerWidthOffsetActual = offset;
            result = result - offset;
            if(result < 0)
            {
                result = 0;
            }
            return result;
        }

        private int ColumnGetWidth(DataGridHelperColumn c)
        {
            var w = 0;
            var containerWidth = GetGridContainerWidth();
            switch(ColumnWidthMode)
            {
                case GridBox.ColumnWidthModeRef.Compact:
                    {
                        // c.WidthRelative -- доля от общей ширины, [1-1000]
                        w = (int)((double)c.WidthRelative / (double)1000 * (double)containerWidth);
                    }
                    break;

                case GridBox.ColumnWidthModeRef.Full:
                    {
                        // c.Width2 -- число символов
                        w = c.Width2 * ColumnSymbolWidth;
                    }
                    break;
            }

            if(w < ColumnWidthMin)
            {
                //w = ColumnWidthMin;
            }

            return w;
        }

        private int ColumnGetWidthActual(DataGridHelperColumn c)
        {
            var w = 0;

            foreach(var column in GridControl.Columns)
            {
                if(c.Path == column.Name)
                {
                    w = column.Width.ToInt();
                    break;
                }
            }
            return w;
        }

        private void ColumnUpdateWidth()
        {
            if(Constructed)
            {
                if(GridControl.Columns.Count > 0)
                {
                    var containerWidth = GetGridContainerWidth();

                    foreach(var column in GridControl.Columns)
                    {
                        var path = column.Name;
                        var c = ColumnGet(path);
                        column.Width = ColumnGetWidth(c);
                    }
                }
            }
        }

        private void ColumnConstruct()
        {
            var sw = "";

            GridControl.CustomColumnSort += SortItems;

            var containerWidth = GetGridContainerWidth();
            sw = sw.Append($" {containerWidth}");
            sw = sw.Append($" {GridBox.ColumnWidthModeRef.Compact}");

            var useMultiHeaders = false;
            foreach(var c in Columns)
            {
                if(CheckColumn(c))
                {
                    if(!c.Group.IsNullOrEmpty())
                    {
                        useMultiHeaders = true;
                        break;
                    }
                }
            }

            var groupPrev = "";
            var groupList = new Dictionary<string, GridControlBand>();
            foreach(DataGridHelperColumn c in Columns)
            {
                if(CheckColumn(c))
                {
                    var column = new DevExpress.Xpf.Grid.GridColumn();

                    var binding = new System.Windows.Data.Binding();
                    binding.Path = new PropertyPath($"[{c.Path}]");

                    var h = c.Header;
                    {
                        if(c.Name.IsNullOrEmpty())
                        {
                            c.Name = c.Path;
                        }

                        h.Replace("\n", "");
                        h.Replace("\r", "");
                        column.Header = h;

                        var t = $"{h}";
                        if(!c.Description.IsNullOrEmpty())
                        {
                            t = $"{t}\n{c.Description}";
                        }
                        column.HeaderToolTip = t;
                    }

                    column.Name = c.Path;
                    column.FieldName = c.Path;
                    column.Tag = $"path={c.Path}";
                    column.Width = ColumnGetWidth(c);
                    sw = sw.Append($" [{c.Path}]=[{column.Width}](w2={c.Width2})(wr={c.WidthRelative})");
                    column.MinWidth = ColumnWidthMin;
                    column.ReadOnly = true;
                    
                    
                    
                    
                    column.AllowSorting = EnableSortingGrid ? DevExpress.Utils.DefaultBoolean.True : DefaultBoolean.False;
                    column.AllowGrouping = DevExpress.Utils.DefaultBoolean.False;
                    column.AllowColumnFiltering = DevExpress.Utils.DefaultBoolean.False;
                    
                    if (EnableFiltering)
                    {
                        column.AllowColumnFiltering = DevExpress.Utils.DefaultBoolean.True;
                        column.FilterPopupMode = FilterPopupMode.ExcelSmart;
                    }

                    switch(c.ColumnType)
                    {
                        case ColumnTypeRef.Boolean:
                            {
                                c.CellControlStyle = (Style)GridContainer.TryFindResource("CheckBoxStyle");
                                c.CellControlStyle2 = (Style)GridContainer.TryFindResource("CheckboxCheckedIcon");
                                c.CellControlStyle3 = (Style)GridContainer.TryFindResource("CheckboxUncheckedIcon");
                                DataTable.Columns.Add(c.Path, typeof(bool));
                            }
                            break;

                        case ColumnTypeRef.Integer:
                            {
                                c.CellControlStyle = GetStyle("GridBox4CellTextBlock");
                                DataTable.Columns.Add(c.Path, typeof(int));
                            }
                            break;

                        case ColumnTypeRef.Double:
                            {
                                c.CellControlStyle = GetStyle("GridBox4CellTextBlock");
                                DataTable.Columns.Add(c.Path, Type.GetType("System.String"));
                                c.Converter = new GridBox4DataConverter();
                                c.Converter.Type = c.ColumnType;
                                c.Converter.Format = c.Format;
                                c.Converter.FormatterRaw = c.FormatterRaw;
                                c.Converter.Init();

                                column.SortMode = DevExpress.XtraGrid.ColumnSortMode.Custom;
                            }
                            break;

                        case ColumnTypeRef.String:
                            {
                                c.CellControlStyle = GetStyle("GridBox4CellTextBlock");
                                DataTable.Columns.Add(c.Path, Type.GetType("System.String"));
                                c.Converter = new GridBox4DataConverter();
                                c.Converter.Type = c.ColumnType;
                                c.Converter.Format = c.Format;
                                c.Converter.FormatterRaw = c.FormatterRaw;
                                c.Converter.Init();
                            }
                            break;

                        case ColumnTypeRef.DateTime:
                            {
                                c.CellControlStyle = GetStyle("GridBox4CellTextBlock");
                                DataTable.Columns.Add(c.Path, Type.GetType("System.String"));
                                c.Converter = new GridBox4DataConverter();
                                c.Converter.Type = c.ColumnType;
                                c.Converter.Format = c.Format;
                                c.Converter.FormatterRaw = c.FormatterRaw;
                                c.Converter.Init();

                                column.SortMode = DevExpress.XtraGrid.ColumnSortMode.Custom;
                            }
                            break;
                    }

                    {
                        var templateSelector = new GridBox4CellTemplateSelector();
                        templateSelector.Column = c;
                        templateSelector.GridBox = this;
                        templateSelector.CellContainerStyle = GetStyle("GridBox4CellContainer");
                        column.CellTemplateSelector = templateSelector;
                    }

                    {
                        column.Binding = binding;
                    }

                    if(useMultiHeaders)
                    {
                        {
                            var b = new GridControlBand();
                            {
                                var n = c.Name;
                                if(!c.Group.IsNullOrEmpty())
                                {
                                    n = c.Group;
                                    n = n.MakeSafeName();
                                    b.Header = c.Group;
                                    b.HorizontalHeaderContentAlignment = System.Windows.HorizontalAlignment.Center;
                                }

                                b.Name = n;
                                if(!groupList.ContainsKey(n))
                                {
                                    groupList.Add(n, b);
                                    GridControl.Bands.Add(b);
                                }

                                if(groupList.ContainsKey(n))
                                {
                                    groupList[n].Columns.Add(column);
                                }
                            }
                        }
                    }

                    GridControl.Columns.Add(column);
                    ColumnsDx.Add(c.Path, column);
                }
            }

            DebugPerformanceLogAdd(sw);

            //system
            {
                var path = "_ROW_BACKGROUND";

                var column = new DevExpress.Xpf.Grid.GridColumn();
                column.Header = path;
                column.Name = path;
                column.FieldName = path;
                column.Tag = $"path={path}";
                column.Width = 0;
                column.ReadOnly = true;

                DataTable.Columns.Add(path, Type.GetType("System.String"));
            }

            //labels
            {
                var path = "_ROW_LABELS";

                var column = new DevExpress.Xpf.Grid.GridColumn();
                column.Header = path;
                column.Name = path;
                column.FieldName = path;
                column.Tag = $"path={path}";
                column.Width = 0;
                column.ReadOnly = true;

                DataTable.Columns.Add(path, Type.GetType("System.String"));
            }

            foreach(var c in Columns)
            {
                var column = GridControl.Columns[c.Path];

                if(
                    c.Visible
                    && !c.Hidden
                    && c.Path != "_"
                )
                {
                }
                else
                {
                    if(column != null)
                    {
                        column.Visible = false;
                    }
                }

                if(column != null)
                {
                    if (EnableSortingGrid)
                    {
                        if(c.DxEnableColumnSorting)
                        {
                            column.AllowSorting = DefaultBoolean.True;
                        }
                        else
                        {
                            column.AllowSorting = DefaultBoolean.False;

                        }
                    }

                    if(c.DxEnableColumnFiltering)
                    {
                        column.AllowColumnFiltering = DevExpress.Utils.DefaultBoolean.True;
                        column.FilterPopupMode = DevExpress.Xpf.Grid.FilterPopupMode.ExcelSmart;
                    }

                    if(!string.IsNullOrEmpty(c.DxHeaderToolTip))
                    {
                        column.HeaderToolTip = c.DxHeaderToolTip;
                    }
                }
            }

            
            if (groupList.Count > 0)
            {
                Bands = groupList;
                GridColumnIsVisible(0);
            }
            
            OnColumnConstructed?.Invoke();
        }

        public void SetGridColumnVisible(string columnPath, bool visible)
        {
            var column = Columns.FirstOrDefault(x => x.Path == columnPath);
            if (column != null)
            {
                column.Visible = visible;
            }
        }

        public void GridColumnIsVisible(int status)
        {
            if (status == 1)
            {
                if (Columns != null)
                {
                    foreach (var column in Columns)
                    {
                        var gridColumn = GridControl.Columns[column.Path];
                        if (gridColumn != null)
                        {
                            gridColumn.Visible = true;
                        }
                    }
                }
            }

            if (status == 0)
            {
                if (Columns != null)
                {
                    foreach (var column in Columns)
                    {
                        var gridColumn = GridControl.Columns[column.Path];
                        if (gridColumn != null)
                        {
                            if (column.Visible && !column.Hidden)
                            {
                                gridColumn.Visible = true;
                            }
                            else
                            {
                                gridColumn.Visible = false;
                            }
                        }
                    }
                }
            }

            if (Bands != null)
            {
                foreach (var group in Bands)
                {
                    var hasVisibleColumns = group.Value.Columns.Any(col => col.Visible);
                    if (!hasVisibleColumns)
                    {
                        group.Value.Visible = false;
                    }
                    else
                    {
                        group.Value.Visible = true;
                    }
                }
            }
        }

        /// <summary>
        /// Получаем результат работы агрегатной функции для указанной по имени колонки
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string GetTotalValueByColumnPath(string path)
        {
            string value = "";

            var totalSummaryList = this.GridControl.TotalSummary;
            foreach (GridSummaryItem gridSummaryItem in totalSummaryList)
            {
                if (gridSummaryItem.FieldName == path)
                {
                    foreach (DevExpress.Xpf.Grid.GridColumn column in GridControl.Columns)
                    {
                        if (column.FieldName == gridSummaryItem.FieldName)
                        {
                            value = column.TotalSummaryText;

                            break;
                        }
                    }

                    break;
                }
            }

            return value;
        }

        /// <summary>
        /// Получаем список результатов всех агрегатных функций в гриде в формате
        /// Path/Result
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> GetTotals()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            var totalSummaryList = this.GridControl.TotalSummary;
            foreach (GridSummaryItem gridSummaryItem in totalSummaryList)
            {
                foreach (DevExpress.Xpf.Grid.GridColumn column in GridControl.Columns)
                {
                    if (column.FieldName == gridSummaryItem.FieldName)
                    {
                        result.CheckAdd(column.FieldName, column.TotalSummaryText);

                        continue;
                    }
                }
            }

            return result;
        }

        public void TotalsConstruct()
        {
            var useTotals = false;
            foreach(var c in Columns)
            {
                if(CheckColumn(c))
                {
                    if(
                        c.Totals != null
                        || c.TotalsType != TotalsTypeRef.None
                    )
                    {
                        useTotals = true;
                        break;
                    }
                }
            }

            if(useTotals)
            {
                var totalList = new List<GridSummaryItem>();

                foreach(var c in Columns)
                {
                    if(CheckColumn(c))
                    {
                        if(
                            c.Totals != null
                            || c.TotalsType != TotalsTypeRef.None
                        )
                        {
                            var t = new GridSummaryItem()
                            {
                                FieldName = c.Path,
                                //Tag=c.Path,
                                //Alignment=GridSummaryItemAlignment.Left,
                                SummaryType = DevExpress.Data.SummaryItemType.Custom,
                            };
                            totalList.Add(t);
                        }
                    }
                }

                GridControl.TotalSummary.AddRange(totalList);
                GridControl.CustomSummary += OnTotalProcess;
                GridView.ShowTotalSummary = true;                
            }
        }

        private void OnTotalProcess(object sender, DevExpress.Data.CustomSummaryEventArgs e)
        {
            var path = ((GridSummaryItem)e.Item).FieldName.ToString();
            //var path = ((GridSummaryItem)e.Item).Tag.ToString();
            if(!path.IsNullOrEmpty())
            {
                if(e.SummaryProcess == CustomSummaryProcess.Finalize)
                {
                    e.TotalValue = TotalsProcess(path);
                }
            }
        }

        private string TotalsProcess(string path)
        {
            var result = "";
            var c = ColumnGet(path);
            if(c != null)
            {
                var totalsProcessed = false;

                if(!totalsProcessed)
                {
                    if(c.TotalsType != TotalsTypeRef.None)
                    {
                        var r = ProcessTotalsForColumn(c);

                        if(c.Converter != null)
                        {
                            result = c.Converter.DoConvert(r);
                        }
                        else
                        {
                            result = r.ToString();
                        }
                        totalsProcessed = true;
                    }
                }

                if(!totalsProcessed)
                {
                    if(c.Totals != null)
                    {
                        var list = GetListFromDataTable();
                        var r = c.Totals.Invoke(list);

                        if(c.Converter != null)
                        {
                            result = c.Converter.DoConvert(r);
                        }
                        else
                        {
                            result = r.ToString();
                        }
                        totalsProcessed = true;
                    }
                }

            }
            return result;
        }

        private string ProcessTotalsForColumn(DataGridHelperColumn c)
        {
            var result = "";

            var list = GetListFromDataTable();

            switch(c.TotalsType)
            {
                case TotalsTypeRef.Summ:
                    {
                        result = ProcessTotalsSummColumn(c, list);
                    }
                    break;

                case TotalsTypeRef.Count:
                    {
                        var count = list.Count;
                        result = count.ToString();
                    }
                    break;
            }
            return result;
        }

        private string ProcessTotalsSummColumn(DataGridHelperColumn c, List<Dictionary<string, string>> list)
        {
            var result = "";

            switch(c.ColumnType)
            {
                case ColumnTypeRef.Integer:
                    {
                        var summ = 0;
                        foreach(Dictionary<string, string> row in list)
                        {
                            var v = row.CheckGet(c.Path).ToInt();
                            summ = summ + v;
                        }
                        result = summ.ToString();
                    }
                    break;

                case ColumnTypeRef.Double:
                    {
                        var summ = 0.0;
                        foreach(Dictionary<string, string> row in list)
                        {
                            var v = row.CheckGet(c.Path).ToDouble();
                            summ = summ + v;
                        }
                        result = summ.ToString();
                    }
                    break;
            }
            return result;
        }

        public void SetRowStylers(Dictionary<StylerTypeRef, StylerDelegate> rowStylers)
        {
            RowStylers = rowStylers;
        }

        private void StylesConstruct()
        {
            var stylers = new List<GridBox4StylerElement>();

            // => RowConstruct
            if(RowStylers.Count > 0)
            {
                foreach(KeyValuePair<StylerTypeRef, StylerDelegate> item in RowStylers)
                {
                    var s = new GridBox4StylerElement();
                    s.Type = item.Key;
                    s.Callback = item.Value;
                    s.Scope = "row";
                    s.Path = "";
                    stylers.Add(s);
                }
            }

            // => GridBox4Styler
            foreach(var c in Columns)
            {
                if(CheckColumn(c))
                {
                    if(c.Stylers.Count > 0)
                    {
                        foreach(KeyValuePair<StylerTypeRef, StylerDelegate> item in c.Stylers)
                        {
                            var s = new GridBox4StylerElement();
                            s.Type = item.Key;
                            s.Callback = item.Value;
                            s.Scope = "cell";
                            s.Path = c.Path;
                            stylers.Add(s);
                        }
                    }
                }
            }

            if(stylers.Count > 0)
            {
                var style = new Style(typeof(DevExpress.Xpf.Grid.CellContentPresenter));

                {
                    var multiBinding = new MultiBinding();
                    {
                        var bind = new System.Windows.Data.Binding();
                        bind.Path = new PropertyPath("Row.Row");
                        multiBinding.Bindings.Add(bind);
                    }
                    {
                        var bind = new System.Windows.Data.Binding();
                        bind.Path = new PropertyPath("Column.FieldName");
                        multiBinding.Bindings.Add(bind);
                    }
                    multiBinding.Converter = new GridBox4Styler(stylers, this);
                    multiBinding.ConverterParameter = DataGridHelperColumn.StylerTypeRef.BackgroundColor;
                    style.Setters.Add(new Setter(DevExpress.Xpf.Grid.GridRowContent.BackgroundProperty, multiBinding));
                }

                {
                    var multiBinding = new MultiBinding();
                    {
                        var bind = new System.Windows.Data.Binding();
                        bind.Path = new PropertyPath("Row.Row");
                        multiBinding.Bindings.Add(bind);
                    }
                    {
                        var bind = new System.Windows.Data.Binding();
                        bind.Path = new PropertyPath("Column.FieldName");
                        multiBinding.Bindings.Add(bind);
                    }
                    multiBinding.Converter = new GridBox4Styler(stylers, this);
                    multiBinding.ConverterParameter = DataGridHelperColumn.StylerTypeRef.ForegroundColor;
                    style.Setters.Add(new Setter(DevExpress.Xpf.Grid.GridRowContent.ForegroundProperty, multiBinding));
                }

                {
                    var multiBinding = new MultiBinding();
                    {
                        var bind = new System.Windows.Data.Binding();
                        bind.Path = new PropertyPath("Row.Row");
                        multiBinding.Bindings.Add(bind);
                    }
                    {
                        var bind = new System.Windows.Data.Binding();
                        bind.Path = new PropertyPath("Column.FieldName");
                        multiBinding.Bindings.Add(bind);
                    }
                    multiBinding.Converter = new GridBox4Styler(stylers, this);
                    multiBinding.ConverterParameter = DataGridHelperColumn.StylerTypeRef.BorderColor;
                    style.Setters.Add(new Setter(DevExpress.Xpf.Grid.GridRowContent.BorderBrushProperty, multiBinding));
                }

                {
                    var multiBinding = new MultiBinding();
                    {
                        var bind = new System.Windows.Data.Binding();
                        bind.Path = new PropertyPath("Row.Row");
                        multiBinding.Bindings.Add(bind);
                    }
                    {
                        var bind = new System.Windows.Data.Binding();
                        bind.Path = new PropertyPath("Column.FieldName");
                        multiBinding.Bindings.Add(bind);
                    }
                    multiBinding.Converter = new GridBox4Styler(stylers, this);
                    multiBinding.ConverterParameter = DataGridHelperColumn.StylerTypeRef.FontWeight;
                    style.Setters.Add(new Setter(DevExpress.Xpf.Grid.GridRowContent.FontWeightProperty, multiBinding));
                }

                /*
                    https://github.com/DevExpress-Examples/how-to-build-binding-paths-in-gridcontrol-cells
                 
                    Value - access the current cell value;
                    Column - access the current column;
                    RowData.Row.[YourPropertyName] - access a property of an object from the ItemsSource collection;
                    Data.[FieldName] - access column values in Server Mode, access unbound column values;
                    View.DataContext.[YourPropertyName] - access a property in the grid's ViewModel.

                 */

                GridView.CellStyle = style;
            }

            {
                /*
                  {
                      string myCellTemplate = "<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
                      "xmlns:dxe=\"http://schemas.devexpress.com/winfx/2008/xaml/editors\">" +
                      "<dxe:TextEdit Name=\"PART_Editor\" HorizontalContentAlignment=\"Center\" " +
                      "FontSize=\"{Binding RowData.Row.NameFontSize}\"/></DataTemplate>";
                      DataTemplate dataTemplate = XamlReader.Load(GetStreamFromString(myCellTemplate)) as DataTemplate;
                      GridView.CellTemplate = dataTemplate;
                  }
                  */

                //var dataTemplate=new DataTemplate();
                //dataTemplate.Text
                //GridView.CellTemplate = new GridBox4TemplateSelector();

            }

            //GridControl.Columns["ID"].CellTemplateSelector = new GridBox4TemplateSelector();

            /*
             * 
             *  {
                    var bind = new System.Windows.Data.Binding();
                    bind.Converter = new GridBox4Styler(stylers);
                    bind.ConverterParameter = DataGridHelperColumn.StylerTypeRef.FontWeight;
                    bind.Path = new PropertyPath("Row");
                    style.Setters.Add(new Setter(DevExpress.Xpf.Grid.GridRowContent.FontWeightProperty, bind));
                }
              
            //bind.Path = new PropertyPath("RowData");

            if(RowStylers.Count > 0)
            {
                foreach(KeyValuePair<StylerTypeRef, StylerDelegate> item in RowStylers)
                {
                    var k=item.Key;
                    var s = item.Value;

                    

                    switch(k)
                    {
                        case StylerTypeRef.BackgroundColor:
                            {
                                //newStyle.Setters.Add(new Setter(DevExpress.Xpf.Grid.GridRowContent.BackgroundProperty, bind));
                            }
                            break;

                        case StylerTypeRef.BorderColor:
                            {

                            }
                            break;

                        case StylerTypeRef.ForegroundColor:
                            {

                            }
                            break;

                        case StylerTypeRef.FontWeight:
                            {

                            }
                            break;
                    }
                }
            }


            foreach(var c in Columns)
            {
                if(CheckColumn(c))
                {
                    if(c.Stylers.Count>0)
                    {
                        foreach(KeyValuePair<StylerTypeRef, StylerDelegate> item in c.Stylers)
                        {
                            var k = item.Key;
                            var s = item.Value;

                            var newStyle = new Style(typeof(DevExpress.Xpf.Grid.CellContentPresenter));
                            var bind = new System.Windows.Data.Binding();
                            bind.Converter = new GridBox4StylerCell();
                            bind.ConverterParameter = s;
                            bind.Path = new PropertyPath("Row");

                            switch(k)
                            {
                                case StylerTypeRef.BackgroundColor:
                                    {
                                        newStyle.Setters.Add(new Setter(DevExpress.Xpf.Grid.GridRowContent.BackgroundProperty, bind));
                                    }
                                    break;

                                case StylerTypeRef.BorderColor:
                                    {

                                    }
                                    break;

                                case StylerTypeRef.ForegroundColor:
                                    {

                                    }
                                    break;

                                case StylerTypeRef.FontWeight:
                                    {

                                    }
                                    break;
                            }

                            //GridView.CellStyle = newStyle;
                        }
                    }
                }
            }
            */

            //var column = GridControl.Columns[0];
            //Style oldStyle = column.CellStyle;
            //Style newStyle = new Style(typeof(DevExpress.Xpf.Grid.CellContentPresenter), oldStyle);
            //Style newStyle = new Style(typeof(DevExpress.Xpf.Grid.CellContentPresenter));

            //newStyle.Setters.Add(new Setter(DevExpress.Xpf.Grid.GridRowContent.BackgroundProperty, "#ff0C79CA".ToBrush()) );
            /*
            var bind = new System.Windows.Data.Binding();
            bind.Converter = new GridBox4TestStyle();
            bind.ConverterParameter = "";
            bind.Path = new PropertyPath("Row");
            newStyle.Setters.Add(new Setter(DevExpress.Xpf.Grid.GridRowContent.BackgroundProperty, bind));
            GridView.CellStyle = newStyle;
            */

            /*
            //not working
            //oBind.Path = new PropertyPath("DataContext");
            //oBind.Path = new PropertyPath("DataContext.NAME");
            //oBind.Path = new PropertyPath("CellData[0].Value");
            //oBind.Path = new PropertyPath("Row");
            //oBind.Path = new PropertyPath("DataContext.[NAME]");

            //oBind.Path = new PropertyPath("DataContext[ID]");
            //oBind.Path = new PropertyPath("RowData.Row.Cells[ID]");
            //!
            //oBind.Path = new PropertyPath("Row[ID]");
            //oBind.Path = new PropertyPath("Row");
            //oBind.Source = GridControl.View;

            newStyle.Setters.Add(new Setter(DevExpress.Xpf.Grid.GridRowContent.BackgroundProperty, oBind));
            GridView.CellStyle = newStyle;
            */

            //column.CellStyle = newStyle;

            /*
                background color
                foreground color
                font style (bold)

             */

            /*

             var style = new Style(typeof(LightweightCellEditor));
            style.Setters.Add(new Setter {
                Property = LightweightCellEditor.BackgroundProperty,
                Value = new SolidColorBrush(Colors.Red)
            });

             ItemsSource="{Binding Appointments, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"  

             */

            /*
            System.Windows.Data.Binding oBind = new System.Windows.Data.Binding();
            oBind.Converter = new GridBox4TestStyle();
            oBind.Path = new PropertyPath("DataContext.NAME");
            oBind.Source = GridControl.View;
            newStyle.Setters.Add(new Setter(DevExpress.Xpf.Grid.GridRowContent.BackgroundProperty, oBind));
            */

            /*
            DataTrigger trg = new DataTrigger();
            trg.Binding = new System.Windows.Data.Binding("CellData[0].Value");
            trg.Value = 5;
            trg.Setters.Add(new Setter(DevExpress.Xpf.Grid.CellContentPresenter.BackgroundProperty, Brushes.Yellow));
            newStyle.Triggers.Add(trg);

            column.CellStyle = newStyle;
            //column.LightweightCellEditor = newStyle;
            */

            /*
            Style oStyle = new Style(typeof(DevExpress.Xpf.Grid.GridRowContent), GridView.RowStyle);
            System.Windows.Data.Binding oBind = new System.Windows.Data.Binding();
            oBind.Converter = new GridBox4TestStyle();
            oBind.Path = new PropertyPath("DataContext.NAME");
            oBind.Source = GridControl.View;
            oStyle.Setters.Add(new Setter(DevExpress.Xpf.Grid.GridRowContent.BackgroundProperty, oBind));
            GridView.RowStyle = oStyle;
            */
        }

        public void ProcessMessage(ItemMessage message)
        {
            if(message != null)
            {
                if(message.SenderName == "MainWindow")
                {
                    switch(message.Action)
                    {
                        case "Resized":
                        DoResize();
                        break;
                    }
                }
            }
        }

        public void DoResize()
        {
            ColumnUpdateWidth();
        }

        public static Stream GetStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public void BindEvents()
        {
            GridView.MouseDown += GridControlOnMouseDown;
            GridView.MouseDoubleClick += GridControlOnMouseDoubleClick;
            if(SearchText != null)
            {
                SearchText.KeyUp += SearchTextOnChange;
            }
            GridView.KeyDown += GridControlOnKeyDown;
            GridControl.Columns.CollectionChanged += Columns_CollectionChanged;
        }

        /// <summary>
        /// Вызывается при изменении коллекции колонок devexpress
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Columns_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (Initialized)
            {
                OnGridControlColumnsCollectionChanged?.Invoke((DevExpress.Xpf.Grid.GridColumnCollection)sender);
            }
        }

        /// <summary>
        /// Вызывается при смене текущей позиции в гриде
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GridControl_SelectedItemChanged(object sender, SelectedItemChangedEventArgs e)
        {
            if(e.NewItem is DataRowView rowv)
            {
                if(rowv != null)
                {
                    if(rowv.Row != null)
                    {
                        SelectedItem = rowv.Row.ToDictionary();
                        OnSelectItem?.Invoke(SelectedItem);
                    }
                }
            }
        }

        private void GridControlOnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            ProcessKeyboard(e);
        }

        private void SearchTextOnChange(object sender, System.Windows.Input.KeyEventArgs e)
        {
            {
                if(DataSet.Items.Count > 0)
                {
                    //if(!RenderingInProgress && !SearchingInProgress)
                    {
                        SearchTimeout.Restart();
                    }
                }
            }
        }

        private void GridControlOnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MouseClickType = MouseClickTypeRef.DoubleClick;
            Central.Dbg($"ProcessCellClick1");
            ProcessCellClick(e);
        }

        private void GridControlOnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
            {
                MouseClickType = MouseClickTypeRef.LeftButtonClick;
            }

            if(e.RightButton == MouseButtonState.Pressed)
            {
                MouseClickType = MouseClickTypeRef.RightButtonClick;
            }

            if (e.ClickCount > 1)
            {
                return;
            }

            Central.Dbg($"ProcessCellClick2");
            ProcessCellClick(e);
        }

        public Dictionary<string, string> GetCellInfo(string t)
        {
            var r = new Dictionary<string, string>();
            if(!t.IsNullOrEmpty())
            {
                var list = t.Split('|').ToList();
                foreach(string block in list)
                {
                    var token = block.Split('=').ToList();
                    var k = "";
                    var v = "";
                    if(token[0] != null)
                    {
                        if(!token[0].ToString().IsNullOrEmpty())
                        {
                            k = token[0].ToString();
                        }
                    }
                    if(token[1] != null)
                    {
                        if(!token[1].ToString().IsNullOrEmpty())
                        {
                            v = token[1].ToString();
                        }
                    }
                    r.CheckAdd(k.ToUpper(), v);
                }
            }
            return r;
        }

        public DataRow RowCreate(Dictionary<string, string> item)
        {
            var row = DataTable.NewRow();

            foreach(var c in Columns)
            {
                if(CheckColumn(c))
                {
                    if(c.Converter != null)
                    {
                        var v = item.CheckGet(c.Path);
                        row[c.Path] = c.Converter.DoConvert(v);
                    }
                    else
                    {
                        var v = item.CheckGet(c.Path);
                        switch(c.ColumnType)
                        {
                            case ColumnTypeRef.Boolean:
                                {
                                    row[c.Path] = v.ToBool();
                                }
                                break;

                            case ColumnTypeRef.Integer:
                                {
                                    row[c.Path] = v.ToInt();
                                }
                                break;
                            case ColumnTypeRef.Double:
                                {
                                    row[c.Path] = v.ToString();
                                }
                                break;
                            case ColumnTypeRef.String:
                            default:
                                {
                                    row[c.Path] = v.ToString();
                                }
                                break;
                        }
                    }

                    if(c.Converter != null)
                    {
                        row[c.Path] = c.Converter.DoFormat(row[c.Path].ToString(), item);
                    }
                }
            }

            {
                var result = "#ffffffff".ToBrush();

                if(RowStylers.Count > 0)
                {
                    foreach(KeyValuePair<StylerTypeRef, StylerDelegate> rowStyler in RowStylers)
                    {
                        if(rowStyler.Value != null)
                        {
                            if(rowStyler.Key == StylerTypeRef.BackgroundColor)
                            {
                                var stylerResult = rowStyler.Value.Invoke(item);
                                if(stylerResult != DependencyProperty.UnsetValue)
                                {
                                    result = (System.Windows.Media.Brush)stylerResult;
                                }
                            }
                        }
                    }
                }
                row["_ROW_BACKGROUND"] = result;
            }

            {
                row["_ROW_LABELS"] = "";
            }

            return row;
        }

        public void RowsConstruct()
        {
            DataTable.Clear();
            if (Items.Count > 0 && Items[0].Count != 0)
            {
                foreach (var item in Items)
                {
                    RowConstruct(item);
                }
            }
            Updated = true;
        }

        public void RowConstruct(Dictionary<string, string> item)
        {
            //var row = DataTable.NewRow();
            var row = RowCreate(item);
            DataTable.Rows.Add(row);
        }

        public void RowColumnUpdate(string selectedRowKey, string cellKey, string cellValue)
        {
            var currentRow1 = new Dictionary<string, string>();
            var currentRow2 = DataTable.NewRow();
            int j = 0;
            {
                foreach(DataRow dr in DataTable.Rows)
                {
                    var value = dr[PrimaryKey].ToString();
                    if(value == selectedRowKey)
                    {
                        dr[cellKey] = cellValue.ToBool();
                        //currentRow2 = dr;
                        //foreach(DataColumn dc in DataTable.Columns)
                        //{
                        //    var k = dc.ColumnName;
                        //    var v = dr[k].ToString();
                        //    currentRow1.CheckAdd(k, v);
                        //}
                        break;
                    }
                    j++;
                }
            }
        }

        public void RowUpdate(string selectedRowKey)
        {
            var currentRow1 = new Dictionary<string, string>();
            var currentRow2 = DataTable.NewRow();
            int j = 0;
            {
                foreach(DataRow dr in DataTable.Rows)
                {
                    var value = dr[PrimaryKey].ToString();
                    if(value == selectedRowKey)
                    {
                        currentRow2 = dr;
                        foreach(DataColumn dc in DataTable.Columns)
                        {
                            var k = dc.ColumnName;
                            var v = dr[k].ToString();
                            currentRow1.CheckAdd(k, v);
                        }
                        break;
                    }
                    j++;
                }
            }

            if(currentRow1.Count > 0)
            {
                var currentRow = new Dictionary<string, string>();
                foreach(Dictionary<string, string> row in Items)
                {
                    if(row.CheckGet(PrimaryKey) == selectedRowKey)
                    {
                        foreach(KeyValuePair<string, string> item in currentRow1)
                        {
                            row.CheckAdd(item.Key, item.Value);
                        }
                        break;
                    }
                }
            }

            if(currentRow1.Count > 0)
            {
                {
                    var cur = j;
                    var max = DataTable.Rows.Count;

                    DataTable.Rows.Remove(currentRow2);
                    var r = RowCreate(currentRow1);
                    DataTable.Rows.InsertAt(r, j);
                    var r0 = GridView.FocusedRowHandle;

                    if((cur + 1) == max)
                    {
                        RowSelectedSetByIndex(cur);
                    }
                    else
                    {
                        RowSelectedSetByIndex(r0 - 1);
                    }


                }
            }
        }

        private void RowSelectedSetByIndex(int rowIndex)
        {
            GridView.FocusedRowHandle = rowIndex;
            GridView.SelectRow(rowIndex);
        }

        public void RowReconstruct(Dictionary<string, string> item)
        {
            if(DataTable.Rows.Count > 0)
            {                
                var r2 = PrimaryKey;

                var v = item.CheckGet(PrimaryKey);

                {
                    var currentRow = DataTable.NewRow();
                    foreach(DataRow row in DataTable.Rows)
                    {
                        if(row[PrimaryKey].ToString() == v)
                        {
                            currentRow = row;
                            break;
                        }
                    }
                }

                GridControl.UpdateLayout();
                {
                    var currentRow = new Dictionary<string, string>();
                    foreach(Dictionary<string, string> row in Items)
                    {
                        if(row.CheckGet(PrimaryKey) == v)
                        {
                            currentRow = row;
                            break;
                        }
                    }
                    currentRow = item;
                }

            }
        }

        public void ConstructMenu()
        {
            // рабочая часть меню
            if(Commands != null)
            {
                Menu = Commands.RenderMenu(Name);
            }

            // общие функции
            ConstructMenuCommon();

            // отладочные функции
            ConstructMenuDebug();
        }

        public void ConstructMenuCommon()
        {
            var menu = new Dictionary<string, DataGridContextMenuItem>(Menu);
            Menu = new Dictionary<string, DataGridContextMenuItem>();

            {
                var mi = new DataGridContextMenuItem()
                {
                    Header = "Копировать текст в буфер обмена",
                    Enabled = true,
                    Action = () =>
                    {
                        CopyCellValue();
                    },
                };
                Menu.Add("copy", mi);
            }

            if(menu != null && menu.Count > 0)
            {
                {
                    var k = $"separator-{0}";
                    var mi = new DataGridContextMenuItem()
                    {
                        Header = "-",
                    };
                    Menu.Add(k, mi);
                }

                foreach(KeyValuePair<string, DataGridContextMenuItem> item in menu)
                {
                    if(item.Value.GroupHeader != "" && item.Value.GroupHeaderName != "")
                    {
                        if (!Menu.ContainsKey(item.Value.GroupHeader))
                        {
                            Menu.Add(
                                item.Value.GroupHeader,
                                new DataGridContextMenuItem()
                                {
                                    Header = item.Value.GroupHeaderName,
                                    Action = () =>
                                    {
                                    },
                                    Items = new Dictionary<string, DataGridContextMenuItem>() { }
                                }
                            );
                        }
                        Menu.CheckGet(item.Value.GroupHeader).Items.Add(item.Key, item.Value);
                        
                    }
                    else
                    {
                        Menu.Add(item.Key, item.Value);
                    }
                }
            }
        }

        public void ConstructMenuDebug()
        {
            if(Central.DebugMode)
            {
                if(!Menu.ContainsKey("Debug"))
                {
                    Menu.Add(
                        "Debug",
                        new DataGridContextMenuItem()
                        {
                            Header = "(Отладка)",
                            Action = () =>
                            {
                            },
                            Items = new Dictionary<string, DataGridContextMenuItem>()
                            {
                                {
                                    "GridBox4",
                                    new DataGridContextMenuItem()
                                    {
                                        Header ="GridBox4",
                                        Action=() =>
                                        {

                                        }
                                    }
                                },
                                {
                                    "Update",
                                    new DataGridContextMenuItem()
                                    {
                                        Header ="Обновить",
                                        Action=() =>
                                        {
                                            LoadItems();
                                        }
                                    }
                                },
                                {
                                    "ShowLog",
                                    new DataGridContextMenuItem()
                                    {
                                        Header ="Лог",
                                        Action=() =>
                                        {
                                            DebugShowLog();
                                        }
                                    }
                                },
                                {
                                    "Test",
                                    new DataGridContextMenuItem()
                                    {
                                        Header ="Тест",
                                        Action=() =>
                                        {
                                        },
                                        Items = new Dictionary<string, DataGridContextMenuItem>()
                                        {
                                            {
                                                "TestSelectFirst",
                                                new DataGridContextMenuItem()
                                                {
                                                    Header ="Выделить первую",
                                                    Action=() =>
                                                    {
                                                        SelectRowX("first");
                                                    }
                                                }
                                            },
                                            {
                                                "TestSelectPrev",
                                                new DataGridContextMenuItem()
                                                {
                                                    Header ="Выделить предыдущую",
                                                    Action=() =>
                                                    {
                                                        SelectRowX("prev");
                                                    }
                                                }
                                            },
                                            {
                                                "TestSelectNext",
                                                new DataGridContextMenuItem()
                                                {
                                                    Header ="Выделить следующую",
                                                    Action=() =>
                                                    {
                                                        SelectRowX("next");
                                                    }
                                                }
                                            },
                                            {
                                                "TestSelectLast",
                                                new DataGridContextMenuItem()
                                                {
                                                    Header ="Выделить последнюю",
                                                    Action=() =>
                                                    {
                                                        SelectRowX("last");
                                                    }
                                                }
                                            },


                                        }
                                    }
                                },
                            }
                        }
                    );
                }
            }
        }

        private System.Windows.Controls.ContextMenu cm { get; set; }
        public void CellMenuShow()
        {
            //System.Windows.Controls.ContextMenu cm = new System.Windows.Controls.ContextMenu();
            cm = new System.Windows.Controls.ContextMenu();

            foreach(KeyValuePair<string, DataGridContextMenuItem> menuItem in Menu)
            {
                var m = menuItem.Value;
                if(m.Visible)
                {

                    if(m.Header == "-")
                    {
                        var ms = new Separator();
                        cm.Items.Add(ms);
                    }
                    else
                    {
                        var mi = new System.Windows.Controls.MenuItem { Header = m.Header, IsEnabled = m.Enabled, ToolTip = m.ToolTip };
                        if(m.Action != null)
                        {
                            mi.Click += (object sender, RoutedEventArgs e) =>
                            {
                                m.Action();
                            };
                        }
                        {
                            if(m.Items.Count > 0)
                            {
                                foreach(KeyValuePair<string, DataGridContextMenuItem> menuItem2 in m.Items)
                                {
                                    var m2 = menuItem2.Value;
                                    if(m2.Visible)
                                    {
                                        if(m2.Header == "-")
                                        {
                                            var ms = new Separator();
                                            mi.Items.Add(ms);
                                        }
                                        else
                                        {
                                            var mi2 = new System.Windows.Controls.MenuItem { Header = m2.Header, IsEnabled = m2.Enabled, ToolTip = m2.ToolTip };
                                            if(m2.Action != null)
                                            {
                                                mi2.Click += (object sender, RoutedEventArgs e) =>
                                                {
                                                    m2.Action();
                                                };
                                            }
                                            {
                                                if(m2.Items.Count > 0)
                                                {
                                                    foreach(KeyValuePair<string, DataGridContextMenuItem> menuItem3 in m2.Items)
                                                    {
                                                        var m3 = menuItem3.Value;
                                                        if(m3.Visible)
                                                        {
                                                            if(m3.Header == "-")
                                                            {
                                                                var ms = new Separator();
                                                                mi.Items.Add(ms);
                                                            }
                                                            else
                                                            {
                                                                var mi3 = new System.Windows.Controls.MenuItem { Header = m3.Header, IsEnabled = m3.Enabled, ToolTip = m3.ToolTip };
                                                                if(m3.Action != null)
                                                                {
                                                                    mi3.Click += (object sender, RoutedEventArgs e) =>
                                                                    {
                                                                        m3.Action();
                                                                    };
                                                                }


                                                                mi2.Items.Add(mi3);
                                                            }

                                                        }
                                                    }
                                                }
                                            }

                                            mi.Items.Add(mi2);
                                        }

                                    }
                                }
                            }
                        }
                        cm.Items.Add(mi);
                    }
                }
            }

            cm.IsOpen = true;
        }

        public void DebugShowColWidth()
        {
            var d = new LogWindow("", "GridBox4");
            d.AutoUpdateInterval = 500;
            d.Show();
            d.SetOnUpdate(() =>
            {
                var s = "";
                s = $"{s}GIRD_BOX-4 Ширина колонок";

                {
                    s = $"{s}\n ";
                    s = $"{s}ColumnWidthMode=[{ColumnWidthMode}]\n ";

                    if(Columns.Count > 0)
                    {

                        var containerWidth = GetGridContainerWidth();
                        s = $"{s}containerWidth=[{containerWidth}] GridContainerWidthOffsetActual=[{GridContainerWidthOffsetActual}]\n ";

                        {
                            s = $"{s}\n";
                            s = $"{s} {"#".ToString().SPadLeft(2)} | ";
                            s = $"{s} {"PATH".ToString().SPadLeft(15)} | ";
                            s = $"{s} {"HEADER".ToString().SPadLeft(15)} | ";
                            s = $"{s} {"W2".ToString().SPadLeft(3)} | ";
                            s = $"{s} {"WR".ToString().SPadLeft(3)} | ";
                            s = $"{s} {"WA".ToString().SPadLeft(3)} | ";
                        }

                        int j = 0;
                        foreach(var c in Columns)
                        {
                            if(CheckColumn(c))
                            {
                                j++;
                                {
                                    s = $"{s}\n";
                                    s = $"{s} {j.ToString().SPadLeft(2)} | ";
                                    s = $"{s} {c.Path.ToString().SPadLeft(15)} | ";
                                    s = $"{s} {c.Header.ToString().SPadLeft(15)} | ";
                                    s = $"{s} {c.Width2.ToString().SPadLeft(3)} | ";
                                    s = $"{s} {c.WidthRelative.ToString().SPadLeft(3)} | ";
                                    var wa = ColumnGetWidth(c);
                                    s = $"{s} {wa.ToString().SPadLeft(3)} | ";
                                }
                            }
                        }
                    }
                }

                {
                    s = $"{s}\n ";
                    s = $"{s}{PerformanceLog}";
                }

                return s;
            });
        }

        public void DebugShowLog()
        {
            var d = new LogWindow("", "GridBox4");
            d.AutoUpdateInterval = 1000;
            d.Show();
            d.SetOnUpdate(() =>
            {
                var today = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                var s = "";
                s = $"{s}GIRD_BOX-4 {today}";

                {
                    s = $"{s}\n ";
                    {
                        s = $"{s}ColumnWidthMode=[{ColumnWidthMode}] ";
                        s = $"{s}MouseClickType=[{MouseClickType}] ";
                        s = $"{s}WidthOffset=[{GridContainerWidthOffset}] ";
                        s = $"{s}\n ";
                    }

                    {
                        s = $"{s}PrimaryKey=[{PrimaryKey}] ";
                        s = $"{s}SelectedItemIndex=[{SelectedItemIndex}] ";
                        s = $"{s}SelectedItemKey=[{SelectedItemKey}] ";
                        s = $"{s}SelectedItemValue=[{SelectedItemValue}] ";
                        var rr = SelectedItem.CheckGet(PrimaryKey);
                        s = $"{s}row=[{rr}] ";
                        s = $"{s}\n ";
                    }

                    if(Columns.Count > 0)
                    {
                        var containerWidth = GetGridContainerWidth();
                        s = $"{s}containerWidth=[{containerWidth}] GridContainerWidthOffsetActual=[{GridContainerWidthOffsetActual}]\n ";

                        {
                            s = $"{s}\n";
                            s = $"{s} {"#".ToString().SPadLeft(2)} | ";
                            s = $"{s} {"PATH".ToString().SPadLeft(15)} | ";
                            s = $"{s} {"HEADER".ToString().SPadLeft(15)} | ";
                            s = $"{s} {"W2".ToString().SPadLeft(3)} | ";
                            s = $"{s} {"WR".ToString().SPadLeft(3)} | ";
                            //s = $"{s} {"WA".ToString().SPadLeft(3)} | ";
                            s = $"{s} {"WA".ToString().SPadLeft(3)} | ";
                            s = $"{s} {"WS".ToString().SPadLeft(3)} | ";
                        }

                        int j = 0;
                        foreach(var c in Columns)
                        {
                            if(CheckColumn(c))
                            {
                                j++;
                                {
                                    var h = c.Header;
                                    h = h.Replace("\n", "");
                                    h = h.Replace("\r", "");


                                    s = $"{s}\n";
                                    s = $"{s} {j.ToString().SPadLeft(2)} | ";
                                    s = $"{s} {c.Path.ToString().SPadLeft(15)} | ";
                                    s = $"{s} {h.ToString().SPadLeft(15)} | ";
                                    s = $"{s} {c.Width2.ToString().SPadLeft(3)} | ";
                                    s = $"{s} {c.WidthRelative.ToString().SPadLeft(3)} | ";
                                    //var wa = ColumnGetWidth(c);
                                    //s = $"{s} {wa.ToString().SPadLeft(3)} | ";
                                    var wa = ColumnGetWidthActual(c);
                                    s = $"{s} {wa.ToString().SPadLeft(3)} | ";
                                    var was = Math.Round((double)wa / (double)ColumnSymbolWidth, 0).ToInt();
                                    s = $"{s} {was.ToString().SPadLeft(3)} | ";



                                }
                            }
                        }
                    }
                }

                {
                    s = $"{s}\n ";
                    s = $"{s}{PerformanceLog}";
                }

                return s;
            });
        }

        private void DebugPerformanceLogAdd(string eventName)
        {
            ReformanceProfiler.GetDelta();
            var total = ((int)ReformanceProfiler.Total).ToString();
            var delta = ((int)ReformanceProfiler.Delta).ToString();

            if(!eventName.IsNullOrEmpty())
            {
                var s = $"{total.SPadLeft(6)} +{delta.SPadLeft(6)} {eventName}";
                PerformanceLog = PerformanceLog.Append(s, true);
                PerformanceLog = PerformanceLog.Truncate(1000);
            }
        }

        public void Destruct()
        {

        }

        public void SelectRowByKey(string id)
        {
            ItemSelectedSetByPrimaryValue(id);
            RowSelectedSetByIndex(SelectedItemIndex);
            ProcessRowSelection(3);
        }

        private string RowToSelect { get; set; } = "";
        public void SetSelectedRowAfterUpdate(string id)
        {
            RowToSelect = id;
        }

        public void SelectRowFirst()
        {
            SelectRowX("first");
        }

        public void SelectRowLast()
        {
            SelectRowX("last");
        }

        public void SelectRowNext()
        {
            SelectRowX("next");
        }

        public void SelectRowPrev()
        {
            SelectRowX("prev");
        }

        public void SelectRowAll(bool select = false)
        {
            var items = GetItems();
            if(items.Count > 0)
            {
                foreach(var row in items)
                {
                    if(row.ContainsKey("_SELECTED"))
                    {
                        if(select)
                        {
                            row.CheckAdd("_SELECTED", "1");
                        }
                        else
                        {
                            row.CheckAdd("_SELECTED", "0");
                        }                        
                    }
                }
            }
            var ds = ListDataSet.Create2(items);
            _SelectAll = true;
            UpdateItems(ds);
        }

        public void SelectAllRows(bool select=false)
        {
            SelectRowAll(select);
        }

        private Dictionary<string, string> GetRowFromDatatable(DataRow dr)
        {
            var row = new Dictionary<string, string>();
            foreach(DataColumn dc in DataTable.Columns)
            {
                var k = dc.ColumnName;
                var v = dr[k].ToString();
                row.CheckAdd(k, v);
            }
            return row;
        }

        private Dictionary<string, string> GetRowFromRowView(DataRowView rowView)
        {
            //var result = new Dictionary<string, string>();

            var dataRow = (DataRow)rowView.Row;
            //var rowItems = dataRow.ItemArray;

            //int j = 0;
            //foreach(var c in Columns)
            //{
            //    if(!c.Hidden)
            //    {
            //        var k = c.Path;
            //        var v = "";
            //        {
            //            v = rowItems[j].ToString();
            //        }
            //        result.Add(k, v);
            //        j++;
            //    }
            //}
            //return result;

            return dataRow.ToDictionary();
        }

        private List<Dictionary<string, string>> GetListFromDataTable()
        {
            var result = new List<Dictionary<string, string>>();

            var count = GridControl.VisibleRowCount;
            if(count > 0)
            {
                for(int i = 0; i < count; i++)
                {
                    if (GridControl.GetRow(i) is DataRowView)
                    {
                        var drv = (DataRowView)GridControl.GetRow(i);
                        var row = GetRowFromRowView(drv);
                        result.Add(row);
                    }
                }
            }

            return result;
        }


        private void SelectRowX(string mode="first")
        {
            var selectionComplete = false;
            var valuePrimary = "";
            var index = 0;
            var nextSelected = false;

            {
                var list = GetListFromDataTable();
                if(list.Count > 0)
                {
                    switch(mode)
                    {
                        case "first":
                            {
                                var selectedRow = list.First();
                                valuePrimary = selectedRow.CheckGet(PrimaryKey);
                                SelectedItemIndex = 0;
                                selectionComplete = true;

                            }
                            break;

                        case "last":
                            {
                                var selectedRow = list.Last();
                                valuePrimary = selectedRow.CheckGet(PrimaryKey);
                                SelectedItemIndex = (list.Count - 1);
                                selectionComplete = true;
                            }
                            break;

                        case "next":
                            {
                                var selectedRow = list.First();
                                var selectNext = false;
                                var j = 0;
                                foreach(Dictionary<string, string> row in list)
                                {
                                    if(selectNext)
                                    {
                                        selectedRow = row;
                                        break;
                                    }

                                    if(!selectNext)
                                    {
                                        var v1 = row.CheckGet(PrimaryKey);
                                        if(v1 == SelectedItemValue)
                                        {
                                            selectNext = true;
                                        }
                                    }

                                    j++;
                                }

                                if(selectedRow.Count > 0)
                                {
                                    SelectedItemIndex = j;
                                    selectionComplete = true;

                                    if(SelectedItemIndex > (list.Count-1))
                                    {
                                        SelectedItemIndex = (list.Count - 1);
                                    }
                                    else
                                    {
                                        valuePrimary = selectedRow.CheckGet(PrimaryKey);
                                    }
                                }
                            }
                            break;

                        case "prev":
                            {
                                var selectedRow = list.First();
                                var selectPrev = false;
                                var j = 0;
                                foreach(Dictionary<string, string> row in list)
                                {
                                    {
                                        var v1 = row.CheckGet(PrimaryKey);
                                        if(v1 == SelectedItemValue)
                                        {
                                            selectPrev = true;
                                            break;
                                        }
                                    }

                                    if(!selectPrev)
                                    {
                                        selectedRow = row;
                                        j++;
                                    }
                                }

                                if(selectedRow.Count > 0)
                                {
                                    SelectedItemIndex = j-1;
                                    selectionComplete = true;

                                    if(SelectedItemIndex < 0)
                                    {
                                        SelectedItemIndex = 0;
                                    }
                                    else
                                    {
                                        valuePrimary = selectedRow.CheckGet(PrimaryKey);
                                    }
                                }
                            }
                            break;
                    }
                }
            }

            if(selectionComplete)
            {                
                ItemSelectedSetByPrimaryValue(valuePrimary);
                RowSelectedSetByIndex(SelectedItemIndex);                
                ProcessRowSelection(4);
            }
        }

        /// <summary>
        /// подчиненный режим
        /// в подчиненном режиме автообновление отключено (ItemsAutoUpdate=false)
        /// </summary>
        //public bool SlaveMode { get; set; } = false;
        private bool RowManuallySelected { get; set; } = false;
        private void ProcessRowSelection(int source)
        {
            /*
                source:
                    0=clear_selection
                    1=autoupdate
                    2=mouse_click
                    3=SelectRowByKey      ] -- messages
                    4=ProcessRowSelection ]
             */

            Central.Dbg($"ProcessRowSelection({source}) name=[{Name}] id=[{SelectedItem.CheckGet("ID").ToString()}]");

            OnSelectItem?.Invoke(SelectedItem);

            if(Commands != null)
            {
                var d = (int)RowSelectProfiler.GetDelta();
                if(d > 100)
                {                    
                    if(source == 2 || UpdateItemsFirstTime || _ChainUpdateSelect)
                    {
                        Commands.ProcessSelectItem(SelectedItem);
                        ConstructMenu();

                        if(_ChainUpdateSelect)
                        {
                            _ChainUpdateSelect = false;
                        }
                    }

                    if(source == 0)
                    {
                        Commands.ProcessSelectItem(SelectedItem);
                        ConstructMenu();
                    }
                }
            }

            if(source == 2 || source == 3)
            {
                RowManuallySelected = true;
            }

            //if(source != 4)
            //{
            //    if(source == 2)
            //    {
            //        Commands.SetGridContextMenu(Name);
            //    }
            //    else
            //    {
            //        Commands.DoGridContextMenu(Name);
            //    }
            //}
        }

        /// <summary>
        /// экспорт данных в Excel
        /// </summary>
        public async void ItemsExportExcel()
        {
            var list = new List<Dictionary<string, string>>();

            if (list.Count == 0)
            {
                var items = GetItemsSelected();
                if (items.Count > 0)
                {
                    list = items;
                }
            }

            if (list.Count == 0)
            {
                list = GetItems();
            }

            var eg = new ExcelGrid();
            var cols = Columns;
            eg.SetColumnsFromGrid(cols);
            eg.Items = list;
            await Task.Run(() =>
            {
                eg.Make();
            });
        }

        public async void ItemsExportExcel(string gridTitle)
        {
            var list= new List<Dictionary<string, string>>();

            if(list.Count == 0)
            {
                var items = GetItemsSelected();
                if(items.Count > 0)
                {
                    list = items;
                }
            }

            if(list.Count == 0)
            {
                list = GetItems();
            }

            var eg = new ExcelGrid();
            var cols = Columns;
            eg.SetColumnsFromGrid(cols);
            eg.Items = list;

            if (!string.IsNullOrEmpty(gridTitle))
            {
                eg.GridTitle = gridTitle;
            }

            await Task.Run(() =>
            {
                eg.Make();
            });
        }

        [Obsolete]
        public void ExportItemsExcel()
        {
            ItemsExportExcel();
        }

        /// <summary>
        /// обработка горячих клавиш  грида
        /// общие вспомогательные горячие клавиши
        /// up|down|home|end|ctrl+f|refresh
        /// </summary>
        /// <param name="e"></param>
        public void ProcessKeyboard(System.Windows.Input.KeyEventArgs e)
        {
            //if(Commands != null)
            //{
            //    Commands.ProcessKeyboard(e);
            //}
            
            // общие вспомогательные горячие клавиши грида
            // up|down|home|end|ctrl+f|refresh

            var dt = (int)((TimeSpan)(DateTime.Now - LastKeyboardEvent)).TotalMilliseconds;
            LastKeyboardEvent = DateTime.Now;

            if(!e.Handled)
            {
                switch(e.Key)
                {
                    case Key.Up:
                        {
                            e.Handled = true;
                            SelectRowPrev();
                        }
                        break;

                    case Key.Down:
                        {
                            e.Handled = true;
                            SelectRowNext();
                        }                        
                        break;

                    case Key.Home:
                        {
                            e.Handled = true;
                            SelectRowFirst();
                        }                        
                        break;

                    case Key.End:
                        {
                            e.Handled = true;
                            SelectRowLast();
                        }
                        break;

                    case Key.F5:
                        {
                            e.Handled = true;
                            LoadItems();
                        }
                        break;

                    // <Ctrl>+<R>
                    case Key.R:
                        {
                            if(
                                Keyboard.IsKeyDown(Key.LeftCtrl)
                                || Keyboard.IsKeyDown(Key.RightCtrl)
                            )
                            {
                                e.Handled = true;
                                LoadItems();
                            }
                        }
                        break;

                    // <Ctrl>+<F>
                    case Key.F:
                        {
                            if(
                                Keyboard.IsKeyDown(Key.LeftCtrl)
                                || Keyboard.IsKeyDown(Key.RightCtrl)
                            )
                            {
                                e.Handled = true;
                                if(SearchText != null)
                                {
                                    SearchText.Focus();
                                }
                            }
                        }
                        break;

                    // <Ctrl>+<C>
                    case Key.C:
                        {
                            if(
                                Keyboard.IsKeyDown(Key.LeftCtrl)
                                || Keyboard.IsKeyDown(Key.RightCtrl)
                            )
                            {
                                if(e.OriginalSource != null)
                                {
                                    var ce = (DevExpress.Xpf.Grid.CellEditor)e.OriginalSource;
                                    if(ce != null)
                                    {
                                        var dc = (DevExpress.Xpf.Grid.EditGridCellData)ce.DataContext;
                                        if(dc != null)
                                        {
                                            var cd = GridBox4Assets.GetCellData(dc);
                                            if(cd != null)
                                            {
                                                var r = cd.Value.ToString();
                                                if(!r.IsNullOrEmpty())
                                                {
                                                    System.Windows.Clipboard.SetText(r);
                                                    e.Handled = true;
                                                }                                                
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;

                    // <Esc>,<Esc>
                    case Key.Escape:
                        {
                            if(dt < 500)
                            {
                                e.Handled = true;
                                if(SearchText != null)
                                {
                                    SearchText.Text="";
                                }
                            }
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// установить режим "грид занят одижанием данных"
        /// блокируется блок с инструментами (тулбар), блок данных затеняется
        /// </summary>
        public void HideSplash()
        {
            ProcessToolbar(1, "HideSplash");
        }

        /// <summary>
        /// установить режим "грид занят одижанием данных"
        /// блокируется блок с инструментами (тулбар), блок данных затеняется
        /// </summary>
        public void ShowSplash(string note = "")
        {
            if(!note.IsNullOrEmpty())
            {
                ProgressNote=note;
            }
            ProcessToolbar(0, "ShowSplash");
        }

        /// <summary>
        /// установить режим "грид занят ожиданием данных"
        /// блокируется блок с инструментами (тулбар), блок данных затеняется
        /// </summary>
        /// <param name="busy">true=занят, false=свободен</param>
        public void SetBusy(bool busy=true, string note = "")
        {
            if(!note.IsNullOrEmpty())
            {
                ProgressNote = note;
            }

            if(busy)
            {
                ProcessToolbar(0, "SetBusy");
            }
            else
            {
                ProcessToolbar(1, "SetBusy");
            }
        }

        /// <summary>
        /// удаляет строки из грида по ключу key
        /// со значением value
        /// </summary>
        /// <returns></returns>
        public void DeleteItemsByKey(string key, string value)
        {
            var ds2 = new List<Dictionary<string, string>>(Items);
            foreach (var item in ds2)
            {
                if (item.CheckGet(key) == value)
                {
                    Items.Remove(item);
                }
            }

            UpdateItems();
        }
    }
}
