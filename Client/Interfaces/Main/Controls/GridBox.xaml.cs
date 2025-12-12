using Client.Assets.Converters;
using Client.Common;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Main
{
    /// <summary>
    /// грид с данными
    /// </summary>
    public partial class GridBox:UserControl
    {
        public GridBox()
        {
            InitializeComponent();

            Name="";
            DataContext = this;
            Columns=new List<DataGridHelperColumn>();
            GridItemsTotals=new Dictionary<string, string>();
            _sortedColumnIndex=-1;            
            _sortedColumnDirection=ListSortDirection.Descending;
            InputTimeoutInterval=1;
            InputTimeoutDelegate InputTimeout;
            InputTimeout=OnInputTimeout;
            AutoUpdateInterval=300;
            ItemsAutoUpdate=true;
            _useGroups=false;
            _useTotals=false;
            ContainerHeader=GridMultilineHeader;
            ContainerFooter=GridTotals;
            HeaderScroll=GridMultilineHeaderScroll;
            Busy=false;
            SelectedItem=new Dictionary<string,string>();
            OnSelectItem=OnSelectItemAction;
            OnDblClick=OnSelectItemAction;
            OnScroll=OnScrollAction;
            Menu=new Dictionary<string, DataGridContextMenuItem>();
            Initialized=false;
            Autosized=false;
            EventProcessorRegistered = false;
            UseRowHeader=true;
            UseSorting=true;
            UseSelecting=true;
            UseHit=true;
            UseRowDragDrop=false;
            PrimaryKey="ID";
            Mode=0;
            SelectItemMode=1;
            OnChangeEventEnabled=false;
            Label="";
            ComboBoxOldValues=new Dictionary<string, Dictionary<string, string>>();
            SearchTextExternal="";
            ColumnSymbolWidth=5;
            ColumnMinSymbols=2;
            ColumnMaxWidth=900;
            ColumnMinWidth=35;
            ColumnWidthMode=ColumnWidthModeRef.None;
            ColumnAutowidthLog = "";
        }
        
        /// <summary>
        /// имя грида (технологическое)
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// коллекция колонок
        /// </summary>
        public List<DataGridHelperColumn> Columns { get; set; }
        public List<string> ColumnsList { get; set; }
        private List<Dictionary<string,string>> _items { get; set; }
        /// <summary>
        /// коллекция записей грида
        /// </summary>
        public List<Dictionary<string,string>> Items
        {
            get
            {
                return _items;
            }
            set
            {
                _items=value;
                Grid.ItemsSource=Items;
                UpdateGrid();
            }
        }

        public Dictionary<string,string> GridItemsTotals { get;set; }

        public int _sortedColumnIndex { get; set; }
        public ListSortDirection _sortedColumnDirection { get; set; }
        public DataGridHelperColumn SortColumn { get; set; }
        public ListSortDirection SortDirection { get; set; }
        private bool _useGroups { get; set; }
        private bool _useTotals { get; set; }

        public GridMultiRowHeader GridHeader { get; set; }
        public Dictionary<string,string> FooterTotals { get; set; }
        public Border ContainerHeader { get; set; }
        public Border ContainerFooter { get; set; }
        public List<string> GroupsList { get; set; }
        /// <summary>
        /// контрол для поиска по слову
        /// </summary>
        public TextBox SearchText { get; set; }
        public string SearchTextExternal { get; set; }
        /// <summary>
        /// датасет записей (пришедший с сервера)
        /// </summary>
        public ListDataSet DataSet { get; set; }
        /// <summary>
        /// коллекция записей, используется для внутренних механизмов фильтрации
        /// </summary>
        public List<Dictionary<string,string>> GridItems { get; set; }
        /// <summary>
        /// использовать заголовок для строк
        /// по умолчанию=true
        /// </summary>
        public bool UseRowHeader { get; set; }
        /// <summary>
        /// использовать сортировку грида по колонке щелчком на заголовке
        /// </summary>
        public bool UseSorting { get; set; }
        /// <summary>
        /// Давать возможность выбрать строку.
        /// (И программно тоже)
        /// Если нужно заблокировать выделение строк, установите UseHit=false
        /// </summary>
        public bool UseSelecting { get;set;}
        /// <summary>
        /// интерактивность, если false, не обрабатываются события мыши
        /// </summary>
        public bool UseHit { get;set;}
        /// <summary>
        /// использовать механизм перетаскивания для строк
        /// </summary>
        public bool UseRowDragDrop {get;set;}
        /// <summary>
        /// ключ строки 
        /// (из датасета, по умолчанию ID)
        /// </summary>
        public string PrimaryKey { get;set;}

        /// <summary>
        /// режим отображения
        /// 0=нормальный режим,1=упрощенный для тачскринов
        /// </summary>
        private int Mode { get;set;}

        public int SelectItemMode {get;set;}
        
        /// <summary>
        /// флаг, разрешающий генерацию события OnChangeAction для колонок
        /// (флаг снимается во время перегрузки данных гридом)
        /// </summary>
        private bool OnChangeEventEnabled {get;set;}

        private Dictionary<string,Dictionary<string,string>> ComboBoxOldValues {get;set;}

        public String Label {get;set;}

        // координата нажатия мыши, используется для проверки drag&drop
        private Point? mouseDownPoint { get; set; }


        private bool _busy { get; set; }
        public bool Busy
        {
            get
            {
                return _busy;
            }
            set
            {
                _busy = value;

                if(_busy)
                {
                    //Mouse.OverrideCursor = Cursors.Wait;
                    //Grid.Opacity=0.7;
                    //Splash.Visibility=Visibility.Visible;
                }
                else
                {
                    //Mouse.OverrideCursor = null;
                    //Grid.Opacity=1.0;
                    //Splash.Visibility=Visibility.Collapsed;
                }
            }
        }

        public ScrollViewer HeaderScroll { get; set; }

        /// <summary>
        /// выделенная запись
        /// </summary>
        public Dictionary<string,string> SelectedItem { get; set; }
        public object SelectedItemRaw { get; set; }

        public delegate void OnScrollDelegate(object sender,ScrollChangedEventArgs e);
        public OnScrollDelegate OnScroll;
        public virtual void OnScrollAction(object sender,ScrollChangedEventArgs e)
        {

        }

        public delegate void OnSelectItemDelegate(Dictionary<string,string> selectedItem);
        /// <summary>
        /// коллбэк: выбор строки грида
        /// </summary>
        public OnSelectItemDelegate OnSelectItem;
        public virtual void OnSelectItemAction(Dictionary<string,string> selectedItem)
        {

        }

        public delegate void OnDblClickDelegate(Dictionary<string,string> selectedItem);
        /// <summary>
        /// коллбэк: двойной клик по строке грида
        /// </summary>
        public OnDblClickDelegate OnDblClick;
        public virtual void OnDblClickAction(Dictionary<string,string> selectedItem)
        {

        }


        public delegate void EnableControlsDelegate();
        public EnableControlsDelegate EnableControls;
        public virtual void EnableControlsAction()
        {

        }
        public delegate void DisableControlsDelegate();
        public DisableControlsDelegate DisableControls;
        public virtual void DisableControlsAction()
        {

        }

        /// <summary>
        /// интервал автообновления грида, сек
        /// 0- автообновление отключено
        /// (по таймеру будет вызвана коллбэк-функция DoLoadItems
        /// </summary>
        public int AutoUpdateInterval { get; set; }
        public bool ItemsAutoUpdate { get; set; }
        public DispatcherTimer AutoUpdateTimer { get; set; }

        //public int AfterUpdateInterval { get; set; }
        //public DispatcherTimer AfterUpdateTimer { get; set; }

        public delegate void OnLoadItemsDelegate();
        /// <summary>
        /// коллбэк: OnLoadItems
        /// </summary>
        public OnLoadItemsDelegate OnLoadItems;
        public virtual void OnLoadItemsAction()
        {

        }

        public delegate void OnFilterItemsDelegate();
        /// <summary>
        /// коллбэк: OnFilterItems
        /// </summary>
        public OnFilterItemsDelegate OnFilterItems;
        public virtual void OnFilterItemsAction()
        {

        }

        public delegate void OnItemDropDelegate(string sourceName,Dictionary<string,string> row);
        /// <summary>
        /// коллбэк: OnItemDrop
        /// </summary>
        public OnItemDropDelegate OnItemDrop;
        public virtual void OnItemDropAction(string sourceName,Dictionary<string,string> row)
        {

        }

        /// <summary>
        /// набор стайлеров для строк грида
        /// </summary>
        public Dictionary<StylerTypeRef,StylerDelegate> RowStylers;

        public Dictionary<string,DataGridContextMenuItem> Menu { get;set;}

        public bool Initialized { get;set;}
        public bool Autosized { get;set;}
        public bool EventProcessorRegistered { get;set;}

        /// <summary>
        /// ширина символа, пикс
        /// </summary>
        private int ColumnSymbolWidth {get;set;}
        /// <summary>
        ///минимальная ширина колонки, количество символов
        /// </summary>
        private int ColumnMinSymbols {get;set;}
        /// <summary>
        /// максимальная ширина колонки, пикс.
        /// </summary>
        private int ColumnMaxWidth {get;set;}
        /// <summary>
        /// минимальная ширина колонки, пикс.
        /// </summary>
        private int ColumnMinWidth {get;set;}

        /// <summary>
        /// режим работы алгоритма автоподбора ширины колонок
        /// </summary>
        public ColumnWidthModeRef ColumnWidthMode {get;set;}

        /// <summary>
        /// справочник режимов работы алгоритма автоподбора ширины колонок
        /// </summary>
        public enum ColumnWidthModeRef
        {
            /// <summary>
            /// алгоритм автопросчета ширины не используется
            /// </summary>
            None = 0,
            /// <summary>
            /// Все колонки меют динамическую ширину (режим 1)
            /// Ширина колонок подстраивается под ширину блока грида.
            /// Если суммарная ширина колонок грида меньше ширины блока
            /// грида, справа будет пустое пространство.
            /// </summary>
            Compact = 1,
            /// <summary>
            /// Все колонки имеют фиксированную ширину (режим 2)
            /// При отображении показываются все колонки, если суммарная ширина
            /// всех колонок шире блока грида, появится прокрутка по горизонтали.
            /// </summary>
            Full = 2,
        }

        public string ColumnAutowidthLog {get; set; }

        public void ClearItems()
        {
            if(Items!=null)
            {
                Items=new List<Dictionary<string,string>>();
            }      
            if(DataSet!=null)
            {
                DataSet.Items=new List<Dictionary<string,string>>();                
            } 
            if(GridItems!=null)
            {
                GridItems=new List<Dictionary<string,string>>();                
            } 
        }

        public void SetColumns(List<DataGridHelperColumn> columns)
        {
            if(columns.Count > 0)
            {
                Columns=columns;
                InitColumns();
            }
        }

        public void SetRowStylers(Dictionary<StylerTypeRef,StylerDelegate> rowStylers)
        {
            RowStylers=rowStylers;
        }

        protected void InitColumns()
        {
            if(Grid != null)
            {
                if(Columns.Count > 0)
                {
                    /*
                        Это кэш имен колонок.
                        Если имя колонки не задано, то в качестве имени будет использоваться переменная Path
                        Иногда несколько колонок используют один и тот же Path
                        в этом случае у них получатся одинаковые имена. 
                        Чтобы исключить такие ситуации, мы записываем в кэш все созданные колонки.
                        Далее при создании колонки, мы проверяем, есть ли ее имя в кэше.
                        Если есть, то не даем создать вторую колонку с таким же именем.
                     */
                    ColumnsList=new List<string>();

                    GroupsList=new List<string>();

                    int columnIndex = 0;

                    foreach(DataGridHelperColumn c in Columns)
                    {
                        //назначим индекс, как порядковый номер по списку
                        //индекс используется для сортировки строк грида
                        c.Index=columnIndex;


                        var p = "";
                        if(!string.IsNullOrEmpty(c.Path))
                        {
                            p=$"{c.Path.Trim()}";
                        }

                        var n = $"{p}";
                        if(!string.IsNullOrEmpty(c.Name))
                        {
                            n=c.Name.Trim();
                        }
                        else
                        {
                            c.Name=n;
                        }

                        var h = $"{p}";
                        if(!string.IsNullOrEmpty(c.Header))
                        {
                            h=c.Header;
                        }                       
                        c.Header=h;


                        if(!ColumnsList.Contains(c.Name))
                        {
                            ColumnsList.Add(c.Name);
                            c.Enabled=true;
                        }
                        else
                        {
                            c.Enabled=false;
                        }


                        //если хотя бы для одной колонки задана группа,
                        //активируем режим групп
                        if(!string.IsNullOrEmpty(c.Group))
                        {
                            _useGroups=true;

                            if(!GroupsList.Contains(c.Group))
                            {
                                GroupsList.Add(c.Group);
                            }
                        }

                        if(c.Totals!=null)
                        {
                            _useTotals=true;
                        }

                        columnIndex++;
                    }



                }
            }

        }

        /// <summary>
        /// установка режима объединенных колонок
        /// </summary>
        /// <param name="h"></param>
        /// <param name="f"></param>
        public void SetMultiHeaders(Border h,Border f)
        {
            ContainerHeader=h;
            ContainerFooter=f;
        }

        private void InitHeaders()
        {
            if(ContainerHeader == null)
            {
                _useGroups=false;
            }

            if(_useGroups)
            {
                bool userGroups=false;
                if(Columns.Count>0)
                {

                    foreach(DataGridHelperColumn c in Columns)
                    {
                        if(c.Enabled)
                        {
                            if(!string.IsNullOrEmpty(c.Group))
                            {
                                var k = c.Group;
                                if(!string.IsNullOrEmpty(k))
                                {
                                    userGroups=true;
                                }
                            }
                        }
                    }
                }
                if(!userGroups)
                {
                    _useGroups=false;
                }
            }
            

            if(ContainerFooter == null)
            {
                _useGroups=false;
            }



            //_useGroups=true;

            if(_useGroups || _useTotals)
            {
                //управляющая структура
                GridHeader = new GridMultiRowHeader
                {
                    DataGrid = Grid,
                    ContainerHeader = ContainerHeader,
                    ContainerFooter = ContainerFooter,
                };

            }


            //колонки с группами существуют
            if(_useGroups)
            {
                //стек колонок
                var headers = new ArrayList();


                /*
                    в два прохода:
                    -найдем все колонки, имеющие одинаковые группы, идущие последовательно
                    -проинициализируем те, у которых определены группы
                    
                    name        group       groups          columnsSkip
                    ----        -----       ---             ---
                    id                      
                    title       
                    width       dimension   .add(width)     1
                    height      dimension   .add(height)    2
                    note
                    cardboard   src         .add(cardboard) 1
                 */

                var groups = new Dictionary<string,ArrayList>();
                var columnsSkip = new Dictionary<string,int>();

                foreach(DataGridHelperColumn c in Columns)
                {
                    if(c.Enabled && !c.Hidden)
                    {
                        if(!string.IsNullOrEmpty(c.Group))
                        {
                            var k = c.Group;

                            if(!groups.ContainsKey(k))
                            {
                                groups.Add(k,new ArrayList());
                            }
                            groups[k].Add(c.Name);


                            if(!columnsSkip.ContainsKey(k))
                            {
                                columnsSkip.Add(k,0);
                            }
                            columnsSkip[k]++;
                        }
                    }
                }


                foreach(DataGridHelperColumn c in Columns)
                {
                    if(c.Enabled && !c.Hidden)
                    {

                        if(string.IsNullOrEmpty(c.Group))
                        {
                            //no group
                            var g = new MultilineHeaderGroup()
                            {
                                Name = c.Name,
                                Header = "",
                                Description="",
                                Columns = new ArrayList { c.Name },
                            };
                            headers.Add(g);
                        }
                        else
                        {
                            //group
                            var k = c.Group;

                            if(columnsSkip.ContainsKey(k))
                            {
                                if(columnsSkip[k]>0)
                                {
                                    var g = new MultilineHeaderGroup()
                                    {
                                        Name = c.Name,
                                        Header = c.Group,
                                        Description=c.Description,
                                        Columns = groups[k],
                                    };
                                    headers.Add(g);
                                    columnsSkip[k]=0;
                                }
                            }
                        }



                    }
                }

                /*
                foreach( DataGridHelperColumn c in Columns)
                { 
                    if(c.Enabled)
                    {
                        //if(!string.IsNullOrEmpty(c.Group))
                        {
                            
                            var header="";
                            if(!string.IsNullOrEmpty(c.Group))
                            {
                                header=c.Group;
                            }

                            
                            var g = new MultilineHeaderGroup()
                            {
                                Name = c.Name,
                                Header = header,
                                Columns = new ArrayList { c.Name }
                            };
                            headers.Add(g);
                        }
                    }
                }
                */

                /*
                if( GroupsList.Count > 0 )
                {
                    foreach(string groupName in GroupsList)
                    {
                        var includedColumns=new ArrayList();

                        foreach( DataGridHelperColumn c in Columns)
                        {
                            if( c.Enabled)
                            {
                                if(!string.IsNullOrEmpty(c.Group))
                                {
                                    if(c.Group == groupName)
                                    {
                                        includedColumns.Add(c.Name);
                                    }
                                }
                            }                            
                        }

                        var g = new MultilineHeaderGroup()
                        {
                            Name = groupName,
                            Header = groupName,
                            Columns = includedColumns,
                        };
                        headers.Add(g);
                    }
                }
                */
                /*
                foreach( DataGridHelperColumn c in Columns)
                { 
                    if(!string.IsNullOrEmpty(c.Group))
                    {
                        var g = new MultilineHeaderGroup()
                        {
                            Name = c.Name,
                            Header = c.Header,
                            Columns = new ArrayList { c.Name }
                        };
                        headers.Add(g);
                    }
                }
                */
                GridHeader.AddHeaderLevel(headers);

                //коллбэки событий
                Grid.ColumnReordered += Grid_ColumnReordered;

                //инициализация механизма
                GridMultilineHeaderArea.Visibility=Visibility.Visible;
                UpdateHeaders();

            }


            //колонки с итогами существуют
            if(_useTotals)
            {
                
                
                var cols=new Dictionary<string,string>();

                foreach(DataGridHelperColumn c in Columns)
                {
                    if(c.Enabled && !c.Hidden)
                    {
                        cols.Add(c.Name,c.Name);
                        GridItemsTotals.CheckAdd(c.Name,"");
                    }
                }
                
                GridHeader.AddFooterLevel(cols);
                GridHeader.UpdateFooter();

                Grid.ColumnReordered += Grid_ColumnReordered;
                
                GridTotalsArea.Visibility=Visibility.Visible;
                UpdateFooters();                
            }
        }

        /// <summary>
        /// процесс обновления заголовков
        /// происходит после ресайза колонок грида или после перестановки колонок
        /// </summary>
        private void UpdateHeaders()
        {
            if(GridHeader != null)
            {
                if(_useGroups)
                {
                    GridHeader.UpdateHeader();
                }
            }
        }

        private void UpdateFooters()
        {
            if(GridHeader != null)
            {
                if(_useTotals)
                {
                    GridHeader.UpdateFooterValues(GridItemsTotals);
                }
            }           
        }

        public void DisableRowHeader()
        {
            Grid.RowHeaderWidth=0;
            MainGridMultiColumnHeaderCorner.Width=0;
        }

        private void Grid_ColumnReordered(object sender,DataGridColumnEventArgs e)
        {
            UpdateHeaders();
            UpdateFooters();
        }

        /// <summary>
        /// установка режима отображения        
        /// 0=нормальный режим,1=упрощенный для тачскринов
        /// </summary>
        /// <param name="mode">0=нормальный режим,1=упрощенный для тачскринов</param>
        public void SetMode(int mode=0)
        {
            Mode=mode;
        }

        /// <summary>
        /// производит генерацию колонок грида из структуры Columns
        /// и инициализирует вспомогательные механизмы (сортировка)
        /// </summary>
        public void Init()
        {
            if(Grid != null)
            {
                /*
                    ячейкам будут программно назначены стили:
                    (в зависимости от типа данных)
                        DataGridColumn
                        DataGridColumnDigit
                        DataGridColumnBool
                 */
                var styles=new Dictionary<string,string>();

                switch(Mode)
                {
                    //упрощенный для тачскринов
                    case 1:
                        Grid.Style=(Style)Grid.TryFindResource("DataGridTouch");

                        styles.CheckAdd("base",      "TouchDataGridColumn");
                        styles.CheckAdd("digit",     "TouchDataGridColumnDigit");
                        styles.CheckAdd("bool",      "TouchDataGridColumnBool");
                        styles.CheckAdd("selectbox", "");

                        UseRowHeader=false;

                        break;

                    //нормальный режим
                    case 0:
                    default:
                        Grid.Style=(Style)Grid.TryFindResource("DataGridMain");

                        styles.CheckAdd("base",      "DataGridColumn");
                        styles.CheckAdd("digit",     "DataGridColumnDigit");
                        styles.CheckAdd("bool",      "DataGridColumnBool");                    
                        styles.CheckAdd("selectbox", "DataGridColumnSelectBox");
                
                        break;
                }

                if(Grid.Columns.Count>0)
                {
                    Grid.Columns.Clear();
                }

                if(Columns.Count > 0)
                {
                    if(
                        ColumnWidthMode == ColumnWidthModeRef.Compact
                        || ColumnWidthMode == ColumnWidthModeRef.Full
                    )
                    {
                        var spacerExists=false;
                        foreach(DataGridHelperColumn c in Columns)
                        {
                            if(c.Path == "_")
                            {
                                spacerExists=true;
                            }
                        }

                        if(!spacerExists)
                        {
                            var columnSpacer = new DataGridHelperColumn
                            {
                                Header=" ",
                                Path="_",
                                ColumnType=ColumnTypeRef.String,
                                MinWidth=5,
                                MaxWidth=2000,
                            };
                            Columns.Add(columnSpacer);
                        }
                    }


                    int columnCounter = 0;

                    foreach(DataGridHelperColumn c in Columns)
                    {
                        if(c.Enabled && !c.Hidden && c.Visible)
                        {

                            var h = $"column{columnCounter}";
                            if(!string.IsNullOrEmpty(c.Header))
                            {
                                h=c.Header.Trim();
                            }

                            var p = "";
                            if(!string.IsNullOrEmpty(c.Path))
                            {
                                p=$"{c.Path.Trim()}";
                            }

                            var n = $"{p}";
                            if(!string.IsNullOrEmpty(c.Name))
                            {
                                n=c.Name.Trim();
                            }

                            

                            switch(c.ColumnType)
                            {
                                case DataGridHelperColumn.ColumnTypeRef.String:
                                {
                                    var col = new DataGridTextColumn();                                    

                                    var b = new System.Windows.Data.Binding();

                                    if(c.FormatterRaw!=null)
                                    {
                                        b.Path=new PropertyPath($"{Items}");
                                        b.Converter=new ProxyRawConverter();
                                        b.ConverterParameter=c.FormatterRaw;
                                    }
                                    else if(c.Formatter!=null)
                                    {
                                        b.Path=new PropertyPath($"[{p}]");
                                        b.Converter=new ProxyConverter();
                                        b.ConverterParameter=c.Formatter;
                                    }
                                    else
                                    {
                                        b.Path=new PropertyPath($"[{p}]");
                                    }

                                    col.Binding=b;

                                    col.CellStyle=(Style)Grid.TryFindResource(styles.CheckGet("base"));

                                    col.Header=h;                                        

                                    if(c.MinWidth > 0)
                                    {
                                        col.MinWidth=c.MinWidth;
                                    }
                                    else
                                    {
                                        if(c.Width > 0)
                                        {
                                            col.Width=c.Width;
                                        }
                                    }
                                    if(c.MaxWidth > 0)
                                    {
                                        col.MaxWidth=c.MaxWidth;
                                    }


                                    if(!string.IsNullOrEmpty(c.Style))
                                    {
                                        col.CellStyle=(Style)Grid.TryFindResource($"{c.Style}");
                                    }

                                    {                                        
                                        col.CellStyle=ProcessStylers(c.Stylers);
                                    }

                                   

                                    Grid.Columns.Add(col);
                                }
                                break;

                                
                                
                                
                                case DataGridHelperColumn.ColumnTypeRef.Integer:
                                {

                                    if (c.Editable)
                                    {
                                         //var col = new DataGridTextColumn();
                                        var col = new DataGridTemplateColumn();

                                        var b = new System.Windows.Data.Binding();
                                        b.Path = new PropertyPath($"[{p}]");

                                        if (c.FormatterRaw != null)
                                        {
                                            b.Path = new PropertyPath($"{Items}");
                                            b.Converter = new ProxyRawConverter();
                                            b.ConverterParameter = c.FormatterRaw;
                                        }
                                        else if (c.Formatter != null)
                                        {
                                            b.Path = new PropertyPath($"[{p}]");
                                            b.Converter = new ProxyConverter();
                                            b.ConverterParameter = c.Formatter;
                                        }
                                        else
                                        {
                                            b.Path = new PropertyPath($"[{p}]");
                                            b.Converter=new DataGridHelperInteger();
                                        }

                                        if (!string.IsNullOrEmpty(c.Format))
                                        {
                                            b.ConverterParameter = c.Format;
                                        }

                                        var factory = new FrameworkElementFactory(typeof(TextBox));

                                        //if (c.Editable)
                                        {
                                            b.Mode = BindingMode.TwoWay;
                                            b.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                                            //factory.SetValue(TextBox.IsEnabledProperty,true);
                                            factory.SetValue(TextBox.IsReadOnlyProperty, false);
                                        }
                                        //else
                                        //{
                                        //    b.Mode = BindingMode.OneWay;
                                        //    factory.SetValue(TextBox.IsReadOnlyProperty, true);
                                        //    //factory.SetValue(TextBox.IsEnabledProperty,false);
                                        //}

                                        factory.SetBinding(TextBox.TextProperty, b);
                                        /*
                                        factory.AddHandler(
                                            TextBox.ClickEvent,
                                            new RoutedEventHandler((o, e) =>
                                            {                                            
                                                if(o!=null)
                                                {
                                                    var el=(FrameworkElement)o;
                                                    if(el.DataContext!=null)
                                                    {
                                                        var p=(Dictionary<string,string>)el.DataContext;
                                                        if(c.OnClickAction!=null)
                                                        {
                                                            c.OnClickAction.Invoke(p,el);
                                                        }                                                    
                                                    }
                                                }                                           
                                            })
                                        );
                                        */


                                        var tpl = new DataTemplate();
                                        tpl.VisualTree = factory;

                                        col.CellTemplate = tpl;
                                        col.CellEditingTemplate = tpl;

                                        /*

                                        col.Binding=b;
                                        col.CellStyle=(Style)Grid.TryFindResource("DataGridColumnDigit");
                                        
                                        */
                                        //col.CellStyle = (Style) Grid.TryFindResource("DataGridColumnTextBox");
                                        // factory.SetValue(TextBox.StyleProperty,(Style)Grid.TryFindResource("DataGridColumnDigit"));

                                        col.Header = h;
                                        if (c.MinWidth > 0)
                                        {
                                            col.MinWidth = c.MinWidth;
                                        }
                                        else
                                        {
                                            if (c.Width > 0)
                                            {
                                                col.Width = c.Width;
                                            }
                                        }

                                        if (c.MaxWidth > 0)
                                        {
                                            col.MaxWidth = c.MaxWidth;
                                        }

                                        if (!string.IsNullOrEmpty(c.Style))
                                        {
                                            col.CellStyle = (Style) Grid.TryFindResource($"{c.Style}");
                                        }

                                        {
                                            col.CellStyle = ProcessStylers(c.Stylers);
                                            
                                            //var style=ProcessStylers(c.Stylers,1,c.ColumnType);
                                            //col.CellStyle=style;
                                        }

                                        Grid.Columns.Add(col);

                                    }
                                    else
                                    {
                                        var col = new DataGridTextColumn();

                                        var b = new System.Windows.Data.Binding();

                                        if(c.FormatterRaw!=null)
                                        {
                                            b.Path=new PropertyPath($"{Items}");
                                            b.Converter=new ProxyRawConverter();
                                            b.ConverterParameter=c.FormatterRaw;
                                        }
                                        else if(c.Formatter!=null)
                                        {
                                            b.Path=new PropertyPath($"[{p}]");
                                            b.Converter=new ProxyConverter();
                                            b.ConverterParameter=c.Formatter;
                                        }
                                        else
                                        {
                                            b.Path=new PropertyPath($"[{p}]");
                                            b.Converter=new Float0();
                                        }

                                        col.Binding=b;

                                        

                                        col.CellStyle=(Style)Grid.TryFindResource(styles.CheckGet("digit"));
                                        
                                        col.Header=h;

                                        if(c.MinWidth > 0)
                                        {
                                            col.MinWidth=c.MinWidth;
                                        }
                                        else
                                        {
                                            if(c.Width > 0)
                                            {
                                                col.Width=c.Width;
                                            }
                                        }
                                        if(c.MaxWidth > 0)
                                        {
                                            col.MaxWidth=c.MaxWidth;
                                        }

                                        if(!string.IsNullOrEmpty(c.Style))
                                        {
                                            col.CellStyle=(Style)Grid.TryFindResource($"{c.Style}");
                                        }

                                        {
                                            var style=ProcessStylers(c.Stylers,1,c.ColumnType);
                                            col.CellStyle=style;
                                        }
                                        
                                        Grid.Columns.Add(col);
                                    }
                                    
                                }
                                break;

                                case DataGridHelperColumn.ColumnTypeRef.Double:
                                {
                                    var col = new DataGridTextColumn();

                                    var b = new System.Windows.Data.Binding();
                                    
                                    if(c.FormatterRaw!=null)
                                    {
                                        b.Path=new PropertyPath($"{Items}");
                                        b.Converter=new ProxyRawConverter();
                                        b.ConverterParameter=c.FormatterRaw;
                                    }
                                    else if(c.Formatter!=null)
                                    {
                                        b.Path=new PropertyPath($"[{p}]");
                                        b.Converter=new ProxyConverter();
                                        b.ConverterParameter=c.Formatter;
                                    }
                                    else
                                    {
                                        b.Path=new PropertyPath($"[{p}]");
                                        b.Converter=new DataGridHelperDouble();
                                    }

                                    if(!string.IsNullOrEmpty(c.Format))
                                    {
                                        b.ConverterParameter=c.Format;
                                    }


                                    col.Binding=b;

                                    col.CellStyle=(Style)Grid.TryFindResource(styles.CheckGet("digit"));

                                    col.Header=h;

                                    if(c.MinWidth > 0)
                                    {
                                        col.MinWidth=c.MinWidth;
                                    }
                                    else
                                    {
                                        if(c.Width > 0)
                                        {
                                            col.Width=c.Width;
                                        }
                                    }
                                    if(c.MaxWidth > 0)
                                    {
                                        col.MaxWidth=c.MaxWidth;
                                    }

                                    if(!string.IsNullOrEmpty(c.Style))
                                    {
                                        col.CellStyle=(Style)Grid.TryFindResource($"{c.Style}");
                                    }

                                    {
                                        //col.CellStyle=ProcessStylers(c.Stylers);

                                        var style=ProcessStylers(c.Stylers,1,c.ColumnType);
                                        col.CellStyle=style;

                                        if(Name=="production_task_list2")
                                        {
                                            var r0=0;
                                        }

                                    }

                                    Grid.Columns.Add(col);
                                }
                                break;

                                case DataGridHelperColumn.ColumnTypeRef.DateTime:
                                {
                                    var col = new DataGridTextColumn();

                                    var b = new System.Windows.Data.Binding();

                                    if(c.FormatterRaw!=null)
                                    {
                                        b.Path=new PropertyPath($"{Items}");
                                        b.Converter=new ProxyRawConverter();
                                        b.ConverterParameter=c.FormatterRaw;
                                    }
                                    else if(c.Formatter!=null)
                                    {
                                        b.Path=new PropertyPath($"[{p}]");
                                        b.Converter=new ProxyConverter();
                                        b.ConverterParameter=c.Formatter;
                                    }
                                    else
                                    {
                                        b.Path=new PropertyPath($"[{p}]");
                                        b.Converter=new DataGridHelperDateTime();                                        
                                    }

                                    if(!string.IsNullOrEmpty(c.Format))
                                    {
                                        b.ConverterParameter=c.Format;
                                    }

                                    //b.Path=new PropertyPath($"[{p}]");
                                    col.Binding=b;

                                    col.CellStyle=(Style)Grid.TryFindResource(styles.CheckGet("base"));

                                    col.Header=h;

                                    if(c.MinWidth > 0)
                                    {
                                        col.MinWidth=c.MinWidth;
                                    }
                                    else
                                    {
                                        if(c.Width > 0)
                                        {
                                            col.Width=c.Width;
                                        }
                                    }
                                    if(c.MaxWidth > 0)
                                    {
                                        col.MaxWidth=c.MaxWidth;
                                    }

                                    if(!string.IsNullOrEmpty(c.Style))
                                    {
                                        col.CellStyle=(Style)Grid.TryFindResource($"{c.Style}");
                                    }

                                    {
                                        col.CellStyle=ProcessStylers(c.Stylers);
                                    }

                                    Grid.Columns.Add(col);
                                }
                                break;

                                case DataGridHelperColumn.ColumnTypeRef.Boolean:
                                {
                                    var col = new DataGridTemplateColumn();                                         

                                    var b = new System.Windows.Data.Binding();
                                    b.Path=new PropertyPath($"[{p}]");
                                    
                                    if(c.FormatterRaw!=null)
                                    {
                                        b.Path=new PropertyPath($"{Items}");
                                        b.Converter=new ProxyRawConverter();
                                        b.ConverterParameter=c.FormatterRaw;
                                    }
                                    else if(c.Formatter!=null)
                                    {
                                        b.Path=new PropertyPath($"[{p}]");
                                        b.Converter=new ProxyConverter();
                                        b.ConverterParameter=c.Formatter;
                                    }
                                    else
                                    {
                                        b.Path=new PropertyPath($"[{p}]");
                                        b.Converter=new ToBool();
                                    }

                                    if(!string.IsNullOrEmpty(c.Format))
                                    {
                                        b.ConverterParameter=c.Format;
                                    }
                                    
                                    /*
                                    //b.Converter=new ToBool();
                                    if(c.Formatter!=null)
                                    {
                                        b.Converter=new ProxyConverter();
                                        b.ConverterParameter=c.Formatter;
                                    }
                                    else
                                    {
                                        b.Converter=new ToBool();
                                    }
                                    */

                                    // CheckBox
                                    var factory = new FrameworkElementFactory(typeof(CheckBox));

                                    if(c.Editable)
                                    {
                                        b.Mode = BindingMode.TwoWay;
                                        b.UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged;
                                        factory.SetValue(CheckBox.IsEnabledProperty,true);
                                    }
                                    else
                                    {
                                        b.Mode = BindingMode.OneWay;
                                        factory.SetValue(CheckBox.IsEnabledProperty,false);
                                    }

                                    factory.SetBinding(CheckBox.IsCheckedProperty,b);
                                    factory.AddHandler(
                                        CheckBox.ClickEvent,
                                        new RoutedEventHandler((o, e) =>
                                        {                                            
                                            if(o!=null)
                                            {
                                                var el=(FrameworkElement)o;
                                                if(el.DataContext!=null)
                                                {
                                                    // не всегда el.DataContext is Dictionary<string, string>
                                                    // добавил проверку
                                                    if (el.DataContext is Dictionary<string, string> p)
                                                    {
                                                        if (c.OnClickAction != null)
                                                        {
                                                            c.OnClickAction.Invoke(p, el);
                                                            //col.CellStyle = ProcessStylers(c.Stylers, 1, DataGridHelperColumn.ColumnTypeRef.Boolean);
                                                        }
                                                    }
                                                }
                                            }                                           
                                        })
                                    );                            
                                    
                                    var tpl = new DataTemplate();                                    
                                    tpl.VisualTree = factory;

                                    col.CellTemplate = tpl;
                                    col.CellEditingTemplate=tpl;
                                    
                                    col.CellStyle=(Style)Grid.TryFindResource(styles.CheckGet("bool"));

                                    col.Header=h;

                                    if(c.MinWidth > 0)
                                    {
                                        col.MinWidth=c.MinWidth;
                                    }
                                    else
                                    {
                                        if(c.Width > 0)
                                        {
                                            col.Width=c.Width;
                                        }
                                    }
                                    if(c.MaxWidth > 0)
                                    {
                                        col.MaxWidth=c.MaxWidth;
                                    }

                                    if(!string.IsNullOrEmpty(c.Style))
                                    {
                                        col.CellStyle=(Style)Grid.TryFindResource($"{c.Style}");
                                    }

                                    {
                                        //col.CellStyle = ProcessStylers(c.Stylers);
                                    }

                                    Grid.Columns.Add(col);

                                }
                                break;

                                case DataGridHelperColumn.ColumnTypeRef.Image:
                                {
                                    var col = new DataGridTemplateColumn();        
                                    
                                    /*
                                    FrameworkElementFactory factory1 = new FrameworkElementFactory(typeof(Image));
                                    Binding b1 = new Binding("Picture");
                                    b1.Mode = BindingMode.TwoWay;
                                    factory1.SetValue(Image.SourceProperty, b1);
                                    DataTemplate cellTemplate1 = new DataTemplate();
                                    cellTemplate1.VisualTree = factory1;
                                    col.CellTemplate = cellTemplate1;
                                    */

                                    

                                    var b = new System.Windows.Data.Binding();
                                    b.Path=new PropertyPath($"{Items}");

                                    if(c.FormatterRaw!=null)
                                    {
                                        b.Converter=new ProxyRawConverter();
                                        b.ConverterParameter=c.FormatterRaw;
                                    }

                                    //var img = new Image();

                                    FrameworkElementFactory factory1 = new FrameworkElementFactory(typeof(Image));
                                    factory1.SetValue(Image.SourceProperty, b);                                    
                                    DataTemplate cellTemplate1 = new DataTemplate();
                                    cellTemplate1.VisualTree = factory1;
                                    col.CellTemplate = cellTemplate1;


                                    //col.CellStyle=(Style)Grid.TryFindResource("DataGridColumn");

                                    col.Header=h;                                    

                                    if(c.MinWidth > 0)
                                    {
                                        col.MinWidth=c.MinWidth;
                                    }
                                    else
                                    {
                                        if(c.Width > 0)
                                        {
                                            col.Width=c.Width;
                                        }
                                    }
                                    if(c.MaxWidth > 0)
                                    {
                                        col.MaxWidth=c.MaxWidth;
                                    }


                                    if(!string.IsNullOrEmpty(c.Style))
                                    {
                                        col.CellStyle=(Style)Grid.TryFindResource($"{c.Style}");
                                    }

                                    if(c.Stylers!=null)
                                    {
                                        col.CellStyle=ProcessStylers(c.Stylers);
                                    }

                                    Grid.Columns.Add(col);
                                }
                                break;


                                case DataGridHelperColumn.ColumnTypeRef.SelectBox:
                                {

                                    var col = new DataGridTemplateColumn();        
                                    
                                    var b = new System.Windows.Data.Binding();
                                    b.Path=new PropertyPath($"[{p}]");
                                    
                                    if(c.FormatterRaw!=null)
                                    {
                                        b.Path=new PropertyPath($"{Items}");
                                        b.Converter=new ProxyRawConverter();
                                        b.ConverterParameter=c.FormatterRaw;
                                    }
                                    else if(c.Formatter!=null)
                                    {
                                        b.Path=new PropertyPath($"[{p}]");
                                        b.Converter=new ProxyConverter();
                                        b.ConverterParameter=c.Formatter;
                                    }
                                    else
                                    {
                                        b.Path=new PropertyPath($"[{p}]");
                                        b.Converter=new DataGridHelperSelect(c.Items);
                                    }
                                                                                
                                    FrameworkElementFactory factory = new FrameworkElementFactory(typeof(ComboBox));

                                    if(c.Items != null)
                                    {
                                        //factory.SetValue(ComboBox.ItemsSourceProperty, c.Items);

                                        var v=new List<string>();
                                        {
                                            if(c.Items.Count>0)
                                            {
                                                foreach(KeyValuePair<string,string> i in c.Items)
                                                {
                                                    v.Add(i.Value);
                                                }
                                            }
                                        }
                                        factory.SetValue(ComboBox.ItemsSourceProperty, v);
                                    }
                                       

                                    if(c.Editable)
                                    {
                                        b.Mode = BindingMode.TwoWay;
                                        b.UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged;
                                    }
                                    else
                                    {
                                        b.Mode = BindingMode.OneWay;
                                    }
                                    factory.SetBinding(ComboBox.SelectedItemProperty,b);
                                    
                                    
                                        

                                    factory.AddHandler(
                                        ComboBox.SelectionChangedEvent,
                                        new SelectionChangedEventHandler((o, e) =>
                                        {                                            
                                            if(o!=null)
                                            {
                                                var el=(FrameworkElement)o;
                                                if(el.DataContext!=null)
                                                {
                                                    var k=c.Path;
                                                    var p=(Dictionary<string,string>)el.DataContext;
                                                    var v=p.CheckGet(k);
                                                    var i=p.CheckGet("ID");

                                                    if(!ComboBoxOldValues.ContainsKey(k))
                                                    {
                                                        ComboBoxOldValues.Add(k,new Dictionary<string, string>());
                                                    }
                                                    var storage=ComboBoxOldValues[k];
                                                    var old=storage.CheckGet(i);
                                                    

                                                    if(OnChangeEventEnabled)
                                                    {
                                                        if(c.OnChangeAction!=null)
                                                        {
                                                            if(!old.IsNullOrEmpty() && old!=v)
                                                            {
                                                                //Central.Dbg($"{Label}: (1) OnChangeAction raised");
                                                                c.OnChangeAction.Invoke(p,v,old,el);
                                                            }                                                            
                                                        }                                                    
                                                    } 
                                                    
                                                    storage.CheckAdd(i,v);
                                                }
                                            }                                                  
                                        })
                                    );
                                        

                                    factory.SetValue(ComboBox.StyleProperty, (Style)Grid.TryFindResource("DataGridColumnSelectBoxCtl"));
                                        
                                    var tpl = new DataTemplate();                                    
                                    tpl.VisualTree = factory;

                                    col.CellTemplate = tpl;
                                    //col.CellEditingTemplate=tpl;
                                        
                                    col.CellStyle=(Style)Grid.TryFindResource(styles.CheckGet("selectbox"));

                                    col.Header=h;     
                                        
                                    if(c.MinWidth > 0)
                                    {
                                        col.MinWidth=c.MinWidth;
                                    }
                                    else
                                    {
                                        if(c.Width > 0)
                                        {
                                            col.Width=c.Width;
                                        }
                                    }
                                    if(c.MaxWidth > 0)
                                    {
                                        col.MaxWidth=c.MaxWidth;
                                    }

                                    /*
                                    if(!string.IsNullOrEmpty(c.Style))
                                    {
                                        col.CellStyle=(Style)Grid.TryFindResource($"{c.Style}");
                                    }

                                    if(c.Stylers!=null)
                                    {
                                        col.CellStyle=ProcessStylers(c.Stylers);
                                    }
                                    */

                                    Grid.Columns.Add(col);

                                }
                                break;

                            }
                            columnCounter++;
                        }
                    }


                    int columnCounter2 = 0;

                    foreach(DataGridHelperColumn c in Columns)
                    {
                        if(c.Enabled && !c.Hidden && c.Visible)
                        {
                            if(Grid.Columns[columnCounter2] != null)
                            {
                                var column = Grid.Columns[columnCounter2];

                                DataGridUtil.SetName(column,c.Name);
                            }
                            columnCounter2++;
                        }
                    }
                }


                //row styles
                if(RowStylers!=null)
                {
                    Grid.RowStyle=ProcessStylers(RowStylers,2);                    
                }

                ClearItems();
                Grid.Sorting +=  Grid_Sorting;
                //Central.Dbg($"CB Sort INIT ");


                InitHeaders();

                //поле поиска
                if(SearchText != null)
                {
                    SearchText.KeyUp+=SearchText_KeyUp;
                }

                //отработка курсора занятости
                Grid.MouseEnter+=Grid_MouseEnter;
                Grid.MouseLeave+=Grid_MouseLeave;
                Splash.MouseEnter+=Grid_MouseEnter;
                Splash.MouseLeave+=Grid_MouseLeave;
                Grid.PreviewMouseDown+=Grid_MouseDown;
                Grid.SelectionChanged+=Grid_SelectionChanged;
                Grid.SizeChanged+= Grid_OnResize;
                //Grid.PreviewMouseMove += Grid_PreviewMouseMove;
                Grid.MouseMove += Grid_PreviewMouseMove;

                if (!UseRowHeader)
                {
                    Grid.RowHeaderWidth=0;
                }               

                Grid.CanUserSortColumns=UseSorting;

                if(!UseHit)
                {
                    Grid.IsHitTestVisible=false;
                }

                if(UseRowDragDrop)
                {
                    Grid.AllowDrop=true;
                    Grid.Drop += Grid_Drop;
                }

                EnableEvents();
                
                /*
                if(
                    ColumnAutowidthMode == ColumnAutoWidthModeRef.Compact
                    || ColumnAutowidthMode == ColumnAutoWidthModeRef.Full
                )
                {
                    if (!EventProcessorRegistered)
                    {
                        Central.Msg.Register(ProcessMessage);
                        EventProcessorRegistered = true;
                    }
                }
                */
                
                
            }
            Initialized=true;
        }

        /// <summary>
        /// регистрация грида в реестре гридов
        /// (далее с этими гридами будут проводиться различные
        /// операции автоматически: утилизация ресурсов, активация автообновления)
        /// </summary>
        /// <param name="tabName"></param>
        public void Register(string tabName="")
        {
            //if(!tabName.IsNullOrEmpty())
            //{
            //    if(!Central.ResourcesGridBox.ContainsKey(tabName))
            //    {
            //        var a=new Dictionary<string, GridBox>();
            //        Central.ResourcesGridBox.Add(tabName,a);
            //    }

            //    var t=Central.ResourcesGridBox["tabName"];       
            //    var n="";
            //    var controlName=this.GetType().Name;
            //    n=$"{tabName}_{controlName}";

            //    if(!n.IsNullOrEmpty())
            //    {
            //        if(!t.ContainsKey(n))
            //        {
            //            t.Add(n,this);
            //        }    
            //    }
            //}
        }

        private void Grid_OnResize(object sender, SizeChangedEventArgs e)
        {
            /*
            if (Initialized)
            {
                if(
                    ColumnAutowidthMode == ColumnAutoWidthModeRef.Compact
                    || ColumnAutowidthMode == ColumnAutoWidthModeRef.Full
                )
                {
                    ColumnsAutoResize();
                }   
            }
            */
            
            /*
            Central.Msg.SendMessage(new ItemMessage()
            {
                ReceiverGroup="All",
                ReceiverName = "",
                SenderName = "MainWindow",
                Action = "Resized",
            });
            */
            /*
            if(
                ColumnAutowidthMode == ColumnAutoWidthModeRef.Compact
                || ColumnAutowidthMode == ColumnAutoWidthModeRef.Full
            )
            {
                ColumnsAutoResize();
            }
            */
        }

        public void ProcessMessage(ItemMessage message)
        {
            if(message!=null)
            {
                if (message.ReceiverGroup == "All")
                {
                    switch (message.Action)
                    {
                        case "Resized":
                            if(
                                ColumnWidthMode == ColumnWidthModeRef.Compact
                                || ColumnWidthMode == ColumnWidthModeRef.Full
                            )
                            {
                                //ColumnsAutoResize0();
                            }
                            break;
                    }
                }
            }
        }

        
        
        private void Grid_Drop(object sender, DragEventArgs e)
        {
            if(e.Data!=null)
            {
                var movingObject=(MovingObject)e.Data.GetData(typeof(MovingObject));
                if(movingObject!=null)
                {
                    var data = (Dictionary<string,string>)movingObject.Data;
                    var sorceName=(string)movingObject.SourceName;
                    if(OnItemDrop!=null)
                    {
                        var n="";
                        OnItemDrop?.Invoke(sorceName,data);
                    }
                }

            }
        }

        private Dictionary<StylerTypeRef,StylerDelegate> PrepareStylers( Dictionary<StylerTypeRef,StylerDelegate> stylers )
        {
            /*
                подготовка стайлеров
                если есть хотя бы один стайлер для бэкграунда -- все хорошо
                если нет, добавим дефолтный
             */

            bool backgroundColorAddDefault=false;
            bool foregroundColorAddDefault=false;

            if(stylers.Count==0)
            {
                backgroundColorAddDefault=true;
                foregroundColorAddDefault=true;
            }
            else
            {
                int backgroundColorCount=0;
                int foregroundColorCount=0;

                foreach(KeyValuePair<StylerTypeRef,StylerDelegate> s in stylers )
                {
                    if(s.Key==StylerTypeRef.BackgroundColor)
                    {
                        backgroundColorCount++;
                    }
                    if(s.Key==StylerTypeRef.ForegroundColor)
                    {
                        foregroundColorCount++;
                    }
                }

                if(backgroundColorCount==0)
                {
                    backgroundColorAddDefault=true;
                }

                if(foregroundColorCount==0)
                {
                    foregroundColorAddDefault=true;
                }
            }

            if(backgroundColorAddDefault)
            {
                stylers.Add(
                    StylerTypeRef.BackgroundColor,
                    (Dictionary<string, string> row) =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        return result;
                    }
                );
                /*
                stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                {
                    {
                        StylerTypeRef.BackgroundColor,
                        (Dictionary<string, string> row) =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            return result;
                        }
                    },
                };
                */
            }

            if(foregroundColorAddDefault)
            {
                
                /*
                stylers.Add(
                    StylerTypeRef.ForegroundColor,
                    (Dictionary<string, string> row) =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        
                        
                        var color = "";
                        color=HColor.GreenFG;

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }
                        

                        return result;
                    }
                );
                */
                
                /*
                stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                {
                    {
                        StylerTypeRef.ForegroundColor,
                        (Dictionary<string, string> row) =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            return result;
                        }
                    },
                };
                */
            }

            return stylers;
        }

        public void ShowSplash()
        {
            Splash.Visibility=Visibility.Visible;
            this.Cursor=Cursors.Wait;
            Splash.Cursor=Cursors.Wait;
            Busy=true;
            DisableEvents();
        }

        public void HideSplash()
        {
            Splash.Visibility=Visibility.Collapsed;
            this.Cursor=null;
            Splash.Cursor=null;
            Busy=false;
            EnableEvents();
        }


        private void Grid_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (UseRowDragDrop)
            {
                // No drag operation
                if (mouseDownPoint == null)
                    return;

                var dg = sender as DataGrid;
                if (dg == null) return;
                // Get the current mouse position
                Point mousePos = e.GetPosition(null);
                Vector diff = mouseDownPoint.Value - mousePos;
                // test for the minimum displacement to begin the drag
                if (e.LeftButton == MouseButtonState.Pressed &&
                    (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
                {

                    //select row
                    DependencyObject dep = (DependencyObject)e.OriginalSource;
                    while ((dep != null) && !(dep is DataGridCell))
                    {
                        dep = VisualTreeHelper.GetParent(dep);
                    }
                    if (dep == null) return;

                    int colIndex = -1;
                    var rowItem = new Dictionary<string, string>();
                    if (dep is DataGridCell)
                    {
                        DataGridCell cell = dep as DataGridCell;
                        colIndex = cell.Column.DisplayIndex;
                        cell.Focus();

                        while ((dep != null) && !(dep is DataGridRow))
                        {
                            dep = VisualTreeHelper.GetParent(dep);
                        }

                        DataGridRow row = dep as DataGridRow;

                        if (row.DataContext != null)
                        {
                            Grid.SelectedItem = row.DataContext;
                            rowItem = (Dictionary<string, string>)row.DataContext;

                            var movingObject = new MovingObject();
                            movingObject.Data = rowItem;
                            movingObject.SourceName = Name;

                            var dragSource = row;
                            var dataObj = new DataObject(movingObject);
                            dataObj.SetData("DragSource", dragSource);
                            DragDrop.DoDragDrop(dragSource, dataObj, DragDropEffects.Copy);
                        }
                    }
                }
            }
        }

        private void Grid_MouseDown(object sender,MouseButtonEventArgs e)
        {
            if(e.ChangedButton == MouseButton.Left)
            {
                mouseDownPoint = e.GetPosition(null);

                //select row
                DependencyObject dep = (DependencyObject)e.OriginalSource;
                while((dep != null) && !(dep is DataGridCell))
                {
                    dep = VisualTreeHelper.GetParent(dep);
                }
                if(dep == null) return;

                int colIndex=-1;
                var rowItem=new Dictionary<string,string>();
                if(dep is DataGridCell)
                {
                    DataGridCell cell = dep as DataGridCell;
                    colIndex=cell.Column.DisplayIndex;
                    cell.Focus();

                    while((dep != null) && !(dep is DataGridRow))
                    {
                        dep = VisualTreeHelper.GetParent(dep);
                    }

                    DataGridRow row = dep as DataGridRow;
                    if(UseSelecting)
                    {
                       
                        //var disconnectedItem = typeof(System.Windows.Data.BindingExpressionBase).GetField("DisconnectedItem", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);

                        if(row.DataContext!=null)
                        {
                            Grid.SelectedItem = row.DataContext;
                            rowItem=row.DataContext as Dictionary<string, string>;
                            if (rowItem != null)
                            {
                                SetSelectedItem(rowItem);
                            }
                        }
                    }                    
                }

                if(colIndex>-1)
                {
                    if(Columns.Count>0)
                    {
                        var c=Columns[colIndex];
                        if(c.ColumnType != DataGridHelperColumn.ColumnTypeRef.Boolean)
                        {
                            c.OnClickAction?.Invoke(rowItem, null);
                        }
                    }
                }
            }

            if(e.ChangedButton == MouseButton.Right)
            {
                //select row
                DependencyObject dep = (DependencyObject)e.OriginalSource;
                while((dep != null) && !(dep is DataGridCell))
                {
                    dep = VisualTreeHelper.GetParent(dep);
                }
                if(dep == null) return;

                if(dep is DataGridCell)
                {
                    DataGridCell cell = dep as DataGridCell;
                    cell.Focus();

                    while((dep != null) && !(dep is DataGridRow))
                    {
                        dep = VisualTreeHelper.GetParent(dep);
                    }
                    DataGridRow row = dep as DataGridRow;
                    if(UseSelecting)
                    {
                        Grid.SelectedItem = row.DataContext;

                        var item2=(Dictionary<string,string>)row.DataContext;
                        SetSelectedItem(item2);
                    }                    
                }

                var show=false;
                var action="debug";
                {
                    var result=false;

                    // Ключ - название операции, значение - список ролей, которым эта операция доступна
                    Dictionary<string, List<string>> Permissions = new Dictionary<string, List<string>>
                    {
                        {
                            "debug", new List<string>() {
                                "[f]admin",
                            }
                        },
                    };

                    var rolesList = new List<string>();
                    if (Permissions.ContainsKey(action))
                    {
                        rolesList = Permissions[action];
                    }

                    if ((rolesList.Count > 0) && (Central.User.Roles.Count > 0))
                    {
                        if (rolesList[0] == "*")
                        {
                            result = true;
                        }
                        else
                        {
                            foreach (KeyValuePair<string, Role> ur in Central.User.Roles)
                            {
                                string userRole = ur.Value.Code;
                                userRole = userRole.Trim();
                                userRole = userRole.ToLower();
                                if (rolesList.Contains(userRole))
                                {
                                    result = true;
                                    break;
                                }
                            }
                        }
                    }

                    if(result)
                    {
                        show=true;
                    }
                }
                    

                    if(
                        Central.DebugMode
                        || show
                    )
                    {
                        if(!Menu.ContainsKey("Debug"))
                        {
                            Menu.Add(
                                "Debug",
                                new DataGridContextMenuItem()
                                {
                                    Header="Отладка",
                                    Action=() =>
                                    {
                                    },
                                    Items=new Dictionary<string, DataGridContextMenuItem>()
                                    { 
                                        {
                                            "Update",
                                            new DataGridContextMenuItem()
                                            {
                                                Header ="Обновить",
                                                Action=() =>
                                                {
                                                    UpdateItems();
                                                }
                                            }
                                        },
                                        {
                                            "ShowInfo",
                                            new DataGridContextMenuItem()
                                            {
                                                Header ="Информация",
                                                Action=() =>
                                                {
                                                    ShowGridInfo();
                                                }
                                            }
                                        },
                                        {
                                            "ColumnsAutoResize1",
                                            new DataGridContextMenuItem()
                                            {
                                                Header ="Режим 1",
                                                Action=() =>
                                                {
                                                    ColumnWidthMode = ColumnWidthModeRef.Compact;
                                                    //ColumnsAutoResize0();
                                                    ColumnsUpdateSize();
                                                }
                                            }
                                        },
                                        {
                                            "ColumnsAutoResize2",
                                            new DataGridContextMenuItem()
                                            {
                                                Header ="Режим 2",
                                                Action=() =>
                                                {
                                                    ColumnWidthMode = ColumnWidthModeRef.Full;
                                                    //ColumnsAutoResize0();
                                                    ColumnsUpdateSize();
                                                }
                                            }
                                        },
                                        {
                                            "ShowColumnsConfig",
                                            new DataGridContextMenuItem()
                                            {
                                                Header ="Лог",
                                                Action=() =>
                                                {
                                                    ShowColumnsConfig();
                                                }
                                            }
                                        },
                                        /*
                                        {
                                            "ColumnsSetResizeable",
                                            new DataGridContextMenuItem()
                                            {
                                                Header ="Разблокировать ширину",
                                                Action=() =>
                                                {
                                                    ColumnsSetResizeable();
                                                }
                                            }
                                        },
                                        */
                                        {
                                            "Separator1",
                                            new DataGridContextMenuItem()
                                            {
                                                Header ="-",
                                                Action=() =>
                                                {
                                                }
                                            }
                                        },
                                        {
                                            "ShowColumnsList",
                                            new DataGridContextMenuItem()
                                            {
                                                Header ="Список колонок",
                                                Action=() =>
                                                {
                                                    ShowColumnsList();
                                                }
                                            }
                                        },
                                        {
                                            "ShowColumnsDoc",
                                            new DataGridContextMenuItem()
                                            {
                                                Header ="Описание колонок",
                                                Action=() =>
                                                {
                                                    ShowColumnsDoc();
                                                }
                                            }
                                        },

                                        
                               
                                    }
                                }
                            );
                        }
                    }

                if(Menu.Count>0)
                {

                    ContextMenu cm = new ContextMenu();

                    foreach(KeyValuePair<string,DataGridContextMenuItem> menuItem in Menu)
                    {
                        var m=menuItem.Value;
                        if(m.Visible)
                        {
                            
                            if (m.Header=="-")
                            {
                                var ms = new Separator();
                                cm.Items.Add(ms);
                            }
                            else
                            {
                                var mi = new MenuItem {Header = m.Header, IsEnabled = m.Enabled};
                                if(m.Action!=null)
                                {
                                    mi.Click+=(object sender,RoutedEventArgs e)=>
                                    {
                                        m.Action();
                                    };
                                }  
                                {
                                    if(m.Items.Count>0)
                                    {
                                        foreach(KeyValuePair<string,DataGridContextMenuItem> menuItem2 in m.Items)
                                        {
                                            var m2=menuItem2.Value;
                                            if(m2.Visible)
                                            {
                                                if (m2.Header=="-")
                                                {
                                                    var ms = new Separator();
                                                    mi.Items.Add(ms);
                                                }
                                                else
                                                {
                                                    var mi2 = new MenuItem {Header = m2.Header, IsEnabled = m2.Enabled};
                                                    if(m2.Action!=null)
                                                    {
                                                        mi2.Click+=(object sender,RoutedEventArgs e)=>
                                                        {
                                                            m2.Action();
                                                        };
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

                    cm.IsOpen=true;
                }

                e.Handled = true;
            }

        }

       
       
        /// <summary>
        /// обработка стилей
        /// </summary>
        /// <param name="stylers">массив стилей</param>
        /// <param name="type">1=ячейка,2=строка</param>
        /// <returns></returns>
        private Style ProcessStylers(Dictionary<StylerTypeRef,StylerDelegate> stylers, int type = 1, DataGridHelperColumn.ColumnTypeRef columnType=DataGridHelperColumn.ColumnTypeRef.String)
        {
            if(type==1)
            {
                stylers=PrepareStylers(stylers);
            }

            //DataGridHelperColumn.ColumnTypeRef.Integer
            
            string s = "";
            Binding binding = new Binding();
            binding.Source = stylers;

            var style = new Style();
            if(type==1)
            {
                //cell
                style=new Style(typeof(DataGridCell));

            }
            else if(type==2)
            {
                //row
                style=new Style(typeof(DataGridRow));
            }


            if(type==1)
            {
                
                var styleName="";
                switch(Mode)
                {
                    //упрощенный для тачскринов
                    case 1:
                        styleName="TouchDataGridColumn";
                        break;

                    //нормальный режим
                    case 0:
                    default:

                        switch(columnType)
                        {
                            case ColumnTypeRef.Integer:
                            case ColumnTypeRef.Double:
                                styleName="DataGridColumnDigit";
                                break;

                            default:
                            case ColumnTypeRef.String:
                                styleName="DataGridColumn";
                                break;
                        }

                        break;
                }
                
                style.BasedOn=(Style)Grid.TryFindResource(styleName);
            }


            foreach(KeyValuePair<StylerTypeRef,StylerDelegate> styler in stylers)
            {
                switch(styler.Key)
                {
                    case StylerTypeRef.BackgroundColor:
                    {
                        var setter = new Setter(
                            ContentControl.BackgroundProperty,
                            new Binding(s)
                            {
                                Converter = new ProxyHightlighter(),
                                ConverterParameter=styler.Value,
                            }
                        );
                        style.Setters.Add(setter);

                        var setter2 = new Setter(
                            ContentControl.BorderBrushProperty,
                            new Binding(s)
                            {
                                Converter = new ProxyHightlighter(),
                                ConverterParameter=styler.Value,
                            }
                        );
                        style.Setters.Add(setter2);
                    }
                    break;

                    case StylerTypeRef.BorderColor:
                    {
                        var setter2 = new Setter(
                            ContentControl.BorderBrushProperty,
                            new Binding(s)
                            {
                                Converter = new ProxyHightlighter(),
                                ConverterParameter=styler.Value,
                            }
                        );
                        style.Setters.Add(setter2);
                    }
                    break;

                    case StylerTypeRef.ForegroundColor:
                    {
                        var setter2 = new Setter(
                            ContentControl.ForegroundProperty,
                            new Binding(s)
                            {
                                Converter = new ProxyHightlighter(),
                                ConverterParameter=styler.Value,
                            }
                        );
                        style.Setters.Add(setter2);
                    }
                    break;


                    case StylerTypeRef.FontWeight:
                    {
                        var setter2 = new Setter(
                            ContentControl.FontWeightProperty,
                            new Binding(s)
                            {
                                Converter = new ProxyFontWight(),
                                ConverterParameter=styler.Value,
                            }
                        );
                        style.Setters.Add(setter2);
                    }
                    break;
                }
            }

            //{
            //    var v=DependencyProperty.UnsetValue;
            //    v=TextAlignment.Right;

            //    var setter2 = new Setter(
            //        ContentControl.HorizontalAlignmentProperty,
            //        new Binding(s)
            //        {
            //            //Converter = new ProxyFontWight(),
            //            ConverterParameter=v,
            //        }
            //    );
            //    style.Setters.Add(setter2);
            //}

            return style;
        }


        private void ProcessTotals(List<Dictionary<string,string>> items)
        {
            
            if(Grid != null)
            {
                if(Columns.Count > 0)
                {
                    if(_useTotals)
                    {
                        foreach(DataGridHelperColumn c in Columns)
                        {
                            if(c.Enabled && !c.Hidden)
                            {
                                if(c.Totals!=null)
                                {
                                    var v=c.Totals.Invoke(items).ToString();
                                    GridItemsTotals.CheckAdd(c.Name,v);                                    
                                }
                            }
                        }
                        
                    }
                }
            }
            
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
                        Central.Stat.TimerAdd("GridBox_RunAutoUpdateTimer", row);
                    }

                    AutoUpdateTimer.Tick += (s,e) =>
                    {
                        if(OnLoadItems!=null)
                        {
                            if(ItemsAutoUpdate)
                            {
                                Busy=true;
                                OnLoadItems?.Invoke();
                                Busy=false;
                            }
                        }
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

        public void Destruct()
        {
            StopAutoUpdateTimer();
            if (EventProcessorRegistered)
            {
                Central.Msg.Register(ProcessMessage);
                EventProcessorRegistered = false;
            }
        }

        /// <summary>
        /// получение данных, запуск механизма автообновления
        /// </summary>
        public void Run()
        {
            //внешний фильтр
            //внешние фильтры берут данные из GridItems и возвращают их туда же
            if(OnFilterItems!=null)
            {
                OnFilterItems?.Invoke();
            }

            LoadItems();
            RunAutoUpdateTimer();
        }

        /// <summary>
        /// получение данных
        /// </summary>
        public void LoadItems()
        {
            if(Initialized)
            {
                if(OnLoadItems!=null)
                {
                    Busy=true;                    
                    OnLoadItems?.Invoke();
                    Busy=false;
                }
            }
        }

        /// <summary>
        /// установка интервала автообновления грида
        /// </summary>
        /// <param name="i"></param>
        public void SetAutoUpdateInterval(int i = 0)
        {
            StopAutoUpdateTimer();
            AutoUpdateTimer=null;
            AutoUpdateInterval=i;
            RunAutoUpdateTimer();
        }

        private void SearchText_KeyUp(object sender,System.Windows.Input.KeyEventArgs e)
        {
            RunInputTimeoutTimer();
        }


        private void Grid_MouseEnter(object sender,System.Windows.Input.MouseEventArgs e)
        {
            /*
                мы отображаем курсор занятости системы, только
                если мышь проходит над этим гридом
             */
            if(Busy)
            {
                Mouse.OverrideCursor = Cursors.Wait;
            }
            else
            {
                Mouse.OverrideCursor = null;
            }

        }

        private void Grid_MouseLeave(object sender,MouseEventArgs e)
        {
            Mouse.OverrideCursor = null;
        }

        public void Scroller_ScrollChanged(object sender,ScrollChangedEventArgs e)
        {
            if(e != null)
            {
                var hs = e.HorizontalOffset;
                if(HeaderScroll!=null)
                {
                    //HeaderScroll.ScrollToHorizontalOffset(hs);
                    GridMultilineHeaderScroll.ScrollToHorizontalOffset(hs);
                }

                if(OnScroll!=null)
                {
                    OnScroll?.Invoke(sender,e);
                }
            }
        }

        /// <summary>
        /// запуск механизма поиска в гриде
        /// </summary>
        /// <param name="word"></param>
        public void SearchItems(string word)
        {
            SearchTextExternal=word;
            UpdateItems(null);
        }

        /// <summary>
        /// обновление значения в колонке k выбранной строки
        /// (той строки, где сейчас стоит курсор выделения грида)
        /// </summary>
        /// <param name="k"></param>
        /// <param name="v"></param>
        public void UpdateRowColumn(string k, string v)
        {
            if(Initialized)
            {
                /*
                if(DataSet != null)
                {
                    if(DataSet.Items.Count>0)
                    {
                        var rowId=SelectedItem.CheckGet("_ROWNUMBER").ToInt();
                        if(rowId > 0)
                        {
                            foreach(Dictionary<string,string> row in DataSet.Items)
                            {
                                //var v2=modeList.CheckGet(v.ToString());
                                row.CheckAdd(k,v);
                            }
                        }
                        UpdateItems(DataSet);
                    }
                }
                */
                
                if(Items.Count > 0)
                {
                    var rowId=SelectedItem.CheckGet("_ROWNUMBER").ToInt();
                    if(rowId > 0)
                    {
                        foreach(Dictionary<string,string> row in Items)
                        {
                            if(row.CheckGet("_ROWNUMBER").ToInt() == rowId)
                            {
                                //var v2=modeList.CheckGet(v.ToString());
                                row.CheckAdd(k,v);
                            }
                        }
                    }
                    var ds=ListDataSet.Create(Items);
                    UpdateItems(ds);
                }
                
            }
        }



        /// <summary>
        /// запуск фильтрации данных грида
        /// обновляет коллекцию записей грида, источник -- указанный датасет
        /// проводит простейший поиск по всем колонкам и обновляет коллекцию данных грида
        /// </summary>
        public void UpdateItems()
        {
            UpdateItems(null);
        }

        public void UpdateItems(ListDataSet ds = null, bool selectFirst=true)
        {
            if(Initialized)
            {
                ShowSplash();
                //DisableControls?.Invoke();

                if(ds!=null)
                {
                    DataSet=ds;
                }

                if(DataSet != null)
                {
                    Busy=true;

                    if(DataSet.Items.Count>0)
                    {
                        //загружаем в рабочую структуру весь датасет
                        //фильтры будут последовательно просеивать рабочую структуру
                        //остаток будет отправлен в грид
                        GridItems=DataSet.Items;

                        //поиск по слову (встроенный контрол "поиск")
                        DoSearchItems();

                        //внешний фильтр
                        //внешние фильтры берут данные из GridItems и возвращают их туда же
                        if(OnFilterItems!=null)
                        {
                            OnFilterItems?.Invoke();
                        }

                        //итоги
                        ProcessTotals(GridItems);

                        Items=GridItems;
                    }
                    else
                    {
                        //внешний фильтр
                        //внешние фильтры берут данные из GridItems и возвращают их туда же
                        if(OnFilterItems!=null)
                        {
                            OnFilterItems?.Invoke();
                        }

                        //итоги
                        var t=new List<Dictionary<string,string>>();
                        ProcessTotals(t);

                        ClearItems();
                    }

                    if(
                        ColumnWidthMode == ColumnWidthModeRef.Compact
                        || ColumnWidthMode == ColumnWidthModeRef.Full
                    )
                    {
                        if(!Autosized)
                        {
                            //ColumnsAutoResize0();
                            ColumnsUpdateSize();
                            Autosized=true;
                        }
                    }

                    Busy=false;
                }


                if(SelectedItem.Count > 0)
                {
                    string id = "";
                    string k=PrimaryKey;
                    if(!string.IsNullOrEmpty(k))
                    {
                        if(SelectedItem.ContainsKey(k))
                        {
                            id=SelectedItem[k].ToString();
                        }

                        SelectRowByKey(id,k);
                    }
                    
                }
                else
                {
                    if(selectFirst)
                    {
                        SetSelectToFirstRow();
                    }                    
                }
                
                StopAutoUpdateTimer();
                RunAutoUpdateTimer();
                HideSplash();
            }   
        }

        /// <summary>
        /// парсинг ответа, извлечение секции и обновление данных грида
        /// </summary>
        /// <param name="answer"></param>
        /// <param name="section"></param>
        public void UpdateItemsAnswer(LPackClientAnswer answer, string section="ITEMS")
        {
            if (answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(answer.Data);
                if (result != null)
                {
                    {
                        var ds = ListDataSet.Create(result, section);
                        UpdateItems(ds);
                    }
                }
            }
        }
        
        /// <summary>
        /// парсинг ответа, извлечение секции и обновление данных грида
        /// в набор данных добавляются элементы в начало
        /// </summary>
        /// <param name="answer"></param>
        /// <param name="section"></param>
        public void UpdateItemsAnswerPrepend(LPackClientAnswer answer, string section="ITEMS", List<Dictionary<string,string>> items=null)
        {
            if (answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(answer.Data);
                if (result != null)
                {
                    {
                        var ds = ListDataSet.Create(result, section);
                        if (items!=null)
                        {
                            if (items.Count > 0)
                            {
                                var items0 = new List<Dictionary<string, string>>();
                                foreach (Dictionary<string,string> row in items)
                                {
                                    items0.Add(row);
                                }    
                                items0.AddRange(ds.Items);
                                ds.Items = items0;
                            }
                        }
                        UpdateItems(ds);
                    }
                }
            }
        }

        /// <summary>
        /// остановка обработки эвентов
        /// (это те евенты, которые генерирует пользователь через UI)
        /// </summary>
        public void DisableEvents()
        {
            OnChangeEventEnabled=false;
            //Central.Dbg($"{Label}: (0) DisableEvents");
        }

        /// <summary>
        /// запуск обработки эвентов
        /// (это те евенты, которые генерирует пользователь через UI)
        /// </summary>
        public void EnableEvents()
        {
            OnChangeEventEnabled=true;
            //Central.Dbg($"{Label}: (9) EnableEvents");
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            //Central.Dbg($"{Label}: (8) Grid_Loaded");
        }

        public ListDataSet GetItems()
        {
            var result=new ListDataSet();

            if(DataSet != null)
            {
                var items=(List<Dictionary<string,string>>)Grid.ItemsSource;
                DataSet.Items=items;
                result=DataSet;
            }

            return result;
        }

        public DataGridHelperColumn FindColumnByName(string name="")
        {
            var result=new DataGridHelperColumn();
            if(!string.IsNullOrEmpty(name))
            {
                if(Columns.Count > 0)
                {
                    var found=false;
                    foreach(DataGridHelperColumn c in Columns)
                    {
                        if(!found)
                        {
                            if(c.Name==name)
                            {
                                found=true;
                                result=c;
                            }
                        }
                    }
                }
            }
            return result;
        }

        public Dictionary<string,string> SetSelectedItemId(int id, string key="ID")
        {
            var item=new Dictionary<string,string>();
            if(UseSelecting)
            {
                if(id!=0)
                {
                    if(!string.IsNullOrEmpty(key))
                    {                    
                        item.Add(key,id.ToString());
                        SelectedItem=item;
                        OnSelectItem?.Invoke(SelectedItem);
                    }
                }
            }
            return item;
        }

        public Dictionary<string,string> SetSelectedItemId(string id, string key="ID")
        {
            var item=new Dictionary<string,string>();
            if(UseSelecting)
            {
                if(!id.IsNullOrEmpty())
                {
                    if(!string.IsNullOrEmpty(key))
                    {                    
                        item.Add(key,id);
                        SelectedItem=item;
                        OnSelectItem?.Invoke(SelectedItem);
                    }
                }
            }
            return item;
        }

        public List<Dictionary<string, string>> GetSelectedItems()
        {
            var result= new List<Dictionary<string, string>>();

            if (Items.Count > 0)
            {
                foreach (Dictionary<string, string> row in Items)
                {
                    if (row.CheckGet("_SELECTED").ToBool() == true)
                    {
                        result.Add(row);
                    }  
                }
            }

            return result;
        }

        /// <summary>
        /// список строк для групповых операций
        /// </summary>
        /// <returns></returns>
        public List<Dictionary<string, string>> GetListItems(string key="", string keySorting="", string keySortingDirection="")
        {
            var list= new List<Dictionary<string, string>>();
            var complete = false;

            if (key.IsNullOrEmpty())
            {
                key = PrimaryKey;
            }

            {
                if(keySorting.IsNullOrEmpty())
                {
                    keySorting=SortColumn.Path;
                }

                if(keySortingDirection.IsNullOrEmpty())
                {
                    if(SortDirection == ListSortDirection.Ascending)
                    {
                        keySortingDirection="asc";
                    }
                    else
                    {
                        keySortingDirection="desc";
                    }
                }
            }

            // список помеченных строк
            if (!complete)
            {
                list=GetSelectedItems();
                if(list.Count > 0)
                {
                    complete = true;
                }    
            }
                    
            // текущая строка, на которой стоит курсор
            if (!complete)
            {
                var row = SelectedItem;
                if (row.CheckGet(key).ToInt() != 0)
                {
                    list.Add(row);
                    complete = true;
                }  
            }

            // сортировка в том же порядке, что и в гриде
            if(
                !keySorting.IsNullOrEmpty()
                && !keySortingDirection.IsNullOrEmpty()
            )
            {
                var list2=new List<string>();
                foreach(Dictionary<string,string> row in list)
                {
                    var vv=row.CheckGet(keySorting);
                    var vk=row.CheckGet(PrimaryKey);
                    list2.Add($"{vv}_{vk}");
                }

                //var c=new DataGridHelperSorter(ListSortDirection.Ascending,keySorting,SortColumn);
                //var c= new DataGridHelperSorterString();
                if(keySortingDirection.ClearCommand()=="asc")
                {
                    list2=list2.OrderBy(
                        (q) => 
                        {
                            return q;
                        }                        
                    ).ToList();
                }
                else
                {
                    list2=list2.OrderByDescending(q => q).ToList();
                }

                var list1=new List<Dictionary<string,string>>();
                
                foreach(string k in list2)
                {
                    foreach(Dictionary<string,string> row in list)
                    {
                        var vv=row.CheckGet(keySorting);
                        var vk=row.CheckGet(PrimaryKey);
                        var s1=$"{vv}_{vk}";
                        
                        if(s1 == k)
                        {
                            list1.Add(row);
                        }
                    }
                }
                
                list=list1;
            }
            
            return list;
        }



        /// <summary>
        /// поиск по слову из встроенного поля поиска
        /// может быть использована как заготовка для внешней функции поиска
        /// </summary>
        public void DoSearchItems()
        {
            if(GridItems!=null)
            {
                if(GridItems.Count>0)
                {
                    bool doFiltering = false;
                    var s = "";

                    if(SearchText != null)
                    {
                        if(!string.IsNullOrEmpty(SearchText.Text))
                        {
                            doFiltering=true;
                            s=SearchText.Text.Trim().ToLower();
                        }
                    }
                    else
                    {
                        if(!SearchTextExternal.IsNullOrEmpty())
                        {
                            doFiltering=true;
                            s=SearchTextExternal.Trim().ToLower();
                        }
                    }

                    if(doFiltering)
                    {
                        var sList=new List<string>();
                        if(s.IndexOf(",")>-1)
                        {
                            sList=s.Split(',').ToList();
                        }
                        else
                        {
                            sList.Add(s);
                        }

                        var items = new List<Dictionary<string,string>>();
                        foreach(Dictionary<string,string> row in GridItems)
                        {
                            bool include = false;

                            foreach(string sw in sList)
                            {
                                foreach(KeyValuePair<string,string> cell in row)
                                {
                                    if(!string.IsNullOrEmpty(cell.Value))
                                    {
                                        if(cell.Value.ToLower().IndexOf(sw) > -1)
                                        {
                                            include=true;
                                        }
                                    }
                                }
                            }

                            if(include)
                            {
                                items.Add(row);
                            }
                        }
                        GridItems=items;
                    }
                }
            }
        }

        public List<Dictionary<string,string>> GetSelectedItems(string key="_SELECTED")
        {
            var result=new List<Dictionary<string,string>>();

            if(Items.Count > 0)
            {
                result = Items.Where(x => x.ContainsKey(key) && x[key] == "1").ToList();
            }

            return result;
        }

        public void SetSelectedItem(Dictionary<string,string> item)
        {
            //FIXME: в связанных гридах вызывает stackoverflow
            if(SelectItemMode==1)
            {
                OnSelectItem?.Invoke(item);
            }            
        }

        public void SelectRowByKey(int id,string k = "ID",bool scrollTo=true)
        {
            bool selected = false;
            if(Grid != null)
            {
                if(UseSelecting)
                {
                    if(id != 0)
                    {
                        if(Grid.Items.Count > 0)
                        {
                            for(int i = 0;i < Grid.Items.Count;i++)
                            {
                                var item = Grid.Items[i];
                                var item2 = item as Dictionary<string,string>;
                                if(item2.ContainsKey(k))
                                {
                                    if(item2[k].ToInt() == id)
                                    {
                                        Grid.SelectedItem = item;
                                        SetSelectedItem(item2);
                                        Grid.UpdateLayout();
                                        if(scrollTo)
                                        {
                                            Grid.ScrollIntoView(item);
                                        }
                                        selected = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    // если не нашли строку с указанным ключом, выделяем первую строку
                    if (!selected)
                    {
                        SetSelectToFirstRow();
                    }
                }
            }
        }

        public void SelectRowByKey(string id,string k = "ID",bool scrollTo=true)
        {
            bool selected = false;
            if(Grid != null)
            {
                if(UseSelecting)
                {
                    if(!id.IsNullOrEmpty())
                    {
                        if(Grid.Items.Count > 0)
                        {
                            for(int i = 0;i < Grid.Items.Count;i++)
                            {
                                var item = Grid.Items[i];
                                var item2 = item as Dictionary<string,string>;
                                if(item2.ContainsKey(k))
                                {
                                    if(item2[k].ToString() == id)
                                    {
                                        Grid.SelectedItem = item;
                                        SetSelectedItem(item2);
                                        Grid.UpdateLayout();
                                        if(scrollTo)
                                        {
                                            Grid.ScrollIntoView(item);
                                        }
                                        selected = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    // если не нашли строку с указанным ключом, выделяем первую строку
                    if (!selected)
                    {
                        SetSelectToFirstRow();
                    }
                }
            }
        }

        public Dictionary<string,string> GetRowByKey(string id,string k = "ID")
        {
            var result=new  Dictionary<string,string>();
            if(Grid != null)
            {
                {
                    if(!id.IsNullOrEmpty())
                    {
                        if(Items.Count > 0)
                        {
                            for(int i = 0; i < Items.Count; i++)
                            {
                                var item = Items[i];
                                var item2 = item as Dictionary<string,string>;
                                if(item2.ContainsKey(k))
                                {
                                    if(item2[k].ToString() == id)
                                    {
                                         result = item2;        
                                         break;
                                    }
                                }
                            }
                        }
                    }                   
                }
            }
            return result;
        }

        public Dictionary<string,string> GetRowByIndex(int index)
        {
            var result=new  Dictionary<string,string>();
            if(Grid != null)
            {
                if(Items.Count > 0)
                {
                    int j=0;
                    for(int i = 0; i < Items.Count; i++)
                    {
                        if(j == index)
                        {
                            result = Items[i];        
                            break;
                        }
                        j++;
                    }
                }
            }
            return result;
        }


        public void SetSelectToFirstRow()
        {
            if(Grid != null)
            {
                if(UseSelecting)
                {
                    if(Grid.Items.Count > 0)
                    {
                        var item = Grid.Items[0];
                        var item2 = item as Dictionary<string,string>;

                        Grid.SelectedItem = item;
                        SetSelectedItem(item2);
                        Grid.ScrollIntoView(item);

                        SelectedItem=item2;
                    }
                }
            }
        }

        public void SetSelectToLastRow()
        {
            if(Grid != null)
            {
                if(UseSelecting)
                {
                    if(Grid.Items.Count > 0)
                    {
                        var item = Grid.Items[(Grid.Items.Count-1)];
                        var item2 = item as Dictionary<string,string>;

                        Grid.SelectedItem = item;
                        SetSelectedItem(item2);
                        Grid.ScrollIntoView(item);

                        SelectedItem=item2;
                    }
                }
                
            }
        }

        public int GetSelectedRowIndex()
        {
            var result=0;
            if(Grid != null)
            {
                if(UseSelecting)
                {
                    if(Grid.Items.Count > 0)
                    {
                        if(!PrimaryKey.IsNullOrEmpty())
                        {
                            if(SelectedItem != null)
                            {
                                var v=SelectedItem.CheckGet(PrimaryKey).ToString();
                                if(!v.IsNullOrEmpty())
                                {
                                    for(int i = 0; i < Grid.Items.Count; i++)
                                    {
                                        var item = Grid.Items[i];
                                        var item2 = item as Dictionary<string,string>;
                                        var v0=item2.CheckGet(PrimaryKey);

                                        if(!v0.IsNullOrEmpty())
                                        {
                                            if(v0 == v)
                                            {
                                                result=i;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }

        public void SetSelectByRowIndex(int i)
        {
            var scrollTo=true;
            if(Grid != null)
            {
                if(UseSelecting)
                {
                    if(Grid.Items.Count > 0)
                    {
                        if(!PrimaryKey.IsNullOrEmpty())
                        {
                            {
                                var item = Grid.Items[i];
                                var item2 = item as Dictionary<string,string>;
                                {
                                    {
                                        Grid.SelectedItem = item;
                                        SetSelectedItem(item2);
                                        Grid.UpdateLayout();
                                        if(scrollTo)
                                        {
                                            Grid.ScrollIntoView(item);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void SetSelectToNextRow()
        {
            if(Grid != null)
            {
                if(UseSelecting)
                {
                    if(Grid.Items.Count > 0)
                    {
                        if(!PrimaryKey.IsNullOrEmpty())
                        {
                            if(SelectedItem != null)
                            {
                                var i=GetSelectedRowIndex();
                                i++;
                                if(i < Grid.Items.Count)
                                {
                                    SetSelectByRowIndex(i);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void SetSelectToPrevRow()
        {
            if(Grid != null)
            {
                if(UseSelecting)
                {
                    if(Grid.Items.Count > 0)
                    {
                        if(!PrimaryKey.IsNullOrEmpty())
                        {
                            if(SelectedItem != null)
                            {
                                var i=GetSelectedRowIndex();
                                i--;
                                if(i > -1)
                                {
                                    SetSelectByRowIndex(i);
                                }
                            }
                        }
                    }
                }
            }
        }

        public string GetSelectedRowKey(string key="ID")
        {
            string result = "";
            
            if (SelectedItem != null)
            {
                result = SelectedItem.CheckGet(key);
            }
            
            return result;
        }
        
        public int GetSelectedRowId(string key="ID")
        {
            int result = 0;

            var k = GetSelectedRowKey(key);
            result = k.ToInt();
            
            return result;
        }

        public void UpdateGrid()
        {
            //DisableEvents();
        
            DoSortGrid(_sortedColumnIndex,_sortedColumnDirection);
            UpdateHeaders();
            UpdateFooters();

            //EnableEvents();
        }

        /// <summary>
        /// установка сортировки по умолчанию
        /// </summary>
        /// <param name="columnName">имя колонки (name)</param>
        /// <param name="direction">ListSortDirection.Ascending</param>
        public void SetSorting(string columnName,ListSortDirection direction = ListSortDirection.Ascending)
        {
            _sortedColumnIndex=GetColumnIndex(columnName);
            _sortedColumnDirection=direction;
        }

        public void Sort(string columnName,ListSortDirection direction = ListSortDirection.Ascending)
        {
            _sortedColumnIndex=GetColumnIndex(columnName);
            _sortedColumnDirection=direction;
            //Central.Dbg($"CB Sort C2");
            DoSortGrid(_sortedColumnIndex,_sortedColumnDirection);
        }

        private void Grid_Sorting(object sender,DataGridSortingEventArgs e)
        {
            var column = e.Column;
            e.Handled = true;

            _sortedColumnIndex = column.DisplayIndex;

            if(column.SortDirection != ListSortDirection.Ascending)
            {
                _sortedColumnDirection=ListSortDirection.Ascending;
            }
            else
            {
                _sortedColumnDirection=ListSortDirection.Descending;
            }
            //Central.Dbg($"CB Sort C1");
            DoSortGrid(_sortedColumnIndex,_sortedColumnDirection);
        }

        private void DoSortGrid(int columnIndex,ListSortDirection direction)
        {
            bool resume = false;

            if(Grid!=null)
            {
                if(columnIndex!=-1)
                {
                    if(Columns.Count > 0)
                    {
                        if(Grid.Columns[columnIndex] != null)
                        {
                            resume=true;
                        }
                    }
                }                
            }

            if(resume)
            {
                var column = Grid.Columns[columnIndex];
                var gridColumn = GetColumn(columnIndex);

                column.SortDirection = direction;
                if(Grid.ItemsSource!=null)
                {
                    var lcv = (ListCollectionView)CollectionViewSource.GetDefaultView(Grid.ItemsSource);
                    string path = column.SortMemberPath;
                    IComparer comparer = new DataGridHelperSorter(direction,path,gridColumn);
                    lcv.CustomSort = comparer;
                }
                var sortedColumnIndex = column.DisplayIndex;                
                var sortedColumnDirection = direction;
                //Central.Dbg($"gridbox sort name=[{Name}] col=[{sortedColumnIndex}] ord=[{sortedColumnDirection.ToString()}] type=[{gridColumn.ColumnType}] ");

                if (Name=="shipment_list_driver")
                {
                    var r0 = 0;
                }
                
                SortColumn=gridColumn;
                SortDirection=sortedColumnDirection;
                
            }


        }

        public DataGridHelperColumn.ColumnTypeRef GetColumnType(int columnIndex)
        {
            var result = DataGridHelperColumn.ColumnTypeRef.String;

            if(Grid!=null)
            {
                if(Columns.Count > 0)
                {
                    foreach(DataGridHelperColumn c in Columns)
                    {
                        if(columnIndex == c.Index)
                        {
                            result=c.ColumnType;
                        }
                    }
                }
            }

            return result;
        }

        public DataGridHelperColumn GetColumn(int columnIndex)
        {
            var result = new DataGridHelperColumn();

            if(Grid!=null)
            {
                if(Columns.Count > 0)
                {
                    foreach(DataGridHelperColumn c in Columns)
                    {
                        if(columnIndex == c.Index)
                        {
                            result=c;
                            break;
                        }
                    }
                }
            }

            return result;
        }

        public int GetColumnIndex(string columnName)
        {
            var result = -1;

            if(Grid!=null)
            {
                if(Columns.Count > 0)
                {
                    foreach(DataGridHelperColumn c in Columns)
                    {
                        if(columnName == c.Name)
                        {
                            result=c.Index;
                        }
                    }
                }
            }

            return result;
        }

        public static ScrollViewer GetScrollViewer(UIElement element)
        {
            if(element == null) return null;

            ScrollViewer retour = null;
            for(int i = 0;i < VisualTreeHelper.GetChildrenCount(element) && retour == null;i++)
            {
                if(VisualTreeHelper.GetChild(element,i) is ScrollViewer)
                {
                    retour = (ScrollViewer)(VisualTreeHelper.GetChild(element,i));
                }
                else
                {
                    retour = GetScrollViewer(VisualTreeHelper.GetChild(element,i) as UIElement);
                }
            }
            return retour;
        }





        /*
           Фильтрация данных происходит через небольшой интервал
           после окончания ввода пользователя, чтобы не производить
           фильтрацию, пока еще пользователь вводит данные.
        */
        public DispatcherTimer InputTimeoutTimer;
        public int InputTimeoutInterval { get; set; }
        public delegate void InputTimeoutDelegate();
        public InputTimeoutDelegate InputTimeout;
        public virtual void OnInputTimeout()
        {

        }

        public void RunInputTimeoutTimer()
        {
            RunInputTimeoutTimer(null);
        }
        public void RunInputTimeoutTimer(InputTimeoutDelegate o = null)
        {
            if(InputTimeoutInterval != 0)
            {
                if(InputTimeoutTimer == null)
                {
                    InputTimeoutTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0,0,InputTimeoutInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", InputTimeoutInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("GridBox_RunInputTimeoutTimer", row);
                    }

                    InputTimeoutTimer.Tick += (s,e) =>
                    {
                        if(o!=null)
                        {
                            o?.Invoke();
                        }
                        else
                        {
                            UpdateItems();
                        }

                        StopInputTimeoutTimer();
                    };

                }

                if(InputTimeoutTimer.IsEnabled)
                {
                    InputTimeoutTimer.Stop();
                }
                InputTimeoutTimer.Start();
            }
        }

        public void StopInputTimeoutTimer()
        {
            if(InputTimeoutTimer != null)
            {
                if(InputTimeoutTimer.IsEnabled)
                {
                    InputTimeoutTimer.Stop();
                }
            }
        }

        private void Grid_MouseDoubleClick(object sender,MouseButtonEventArgs e)
        {
            if(UseSelecting)
            {
                 if(sender != null)
                {
                    var dg = sender as DataGrid;
                    if(dg != null)
                    {
                        if(dg.SelectedItem != null)
                        {
                            SelectedItemRaw=dg.SelectedItem;
                            SelectedItem=dg.SelectedItem as Dictionary<string,string>;
                            OnSelectItem?.Invoke(SelectedItem);
                            OnDblClick?.Invoke(SelectedItem);
                        }
                    }

                }
            }
        }

        private void Grid_SelectionChanged(object sender,SelectionChangedEventArgs e)
        {
            if(UseSelecting)
            {
                if(sender != null)
                {
                    var dg = sender as DataGrid;
                    if(dg != null)
                    {
                        if(dg.SelectedItem != null)
                        {
                            SelectedItemRaw=dg.SelectedItem;
                            SelectedItem=dg.SelectedItem as Dictionary<string,string>;
                            OnSelectItem?.Invoke(SelectedItem);

                        }
                    }

                }
            }
        }

        public void Focus()
        {
            Grid.Focus();
            Grid.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }

        public void ShowGridInfo()
        {
            if (Initialized)
            {
                if (Columns.Count > 0)
                {

                    var s = "";

                    var n = "без имени";
                    if (!Name.IsNullOrEmpty())
                    {
                        n = Name;
                    }

                    s = s.Append($"Грид [{n}]", true);
                    s = s.Append($"Режим отображения: {ColumnWidthMode}", true);

                    var j = 0;
                    if (Items.Count > 0)
                    {
                        j = Items.Count;
                    }

                    s = s.Append($"Строк: {j}", true);

                    s = s.Append($"Первичный ключ: {PrimaryKey}", true);
                    s = s.Append($"Автообновление данных: {AutoUpdateInterval}", true);

                    var msg = s;
                    var d = new DialogWindow($"{msg}", "Информация о гриде", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
        }

        public void ShowColumnsConfig()
        {
            if(Initialized)
            {
                if(Columns.Count>0)
                {
                    var s = "";
                    s=s.Append($"{ColumnAutowidthLog}",true);
                    s=s.Append($" ",true);    

                    {
                        s=$"{s}\n";                  
                        s=$"{s} {"#".ToString().SPadLeft(2)} | ";
                        s=$"{s} {"PATH".ToString().SPadLeft(15)} | ";
                        s=$"{s} {"HEADER".ToString().SPadLeft(15)} | ";
                        s=$"{s} {"w1".ToString().SPadLeft(2)} | ";
                        s=$"{s} {"w2".SPadLeft(3)} | ";
                        s=$"{s} {"wr".ToString().SPadLeft(3)} | ";
                        s=$"{s} {"wс".ToString().SPadLeft(3)} | ";
                        s=$"{s} {"wa".ToString().SPadLeft(3)} | ";
                        s=$"{s} {"".ToString().SPadLeft(20)}";
                    }

                    var j = 0;
                    foreach(DataGridHelperColumn c in Columns)
                    {
                        j++;
                        var include=false;

                        var gridColumn=FindColumn(c);
                        var h=c.Header;
                        var p=c.Path;
                        var w=0;
                        var l=c.AutoWidthLog;
                        if(gridColumn != null)
                        {
                            w=gridColumn.ActualWidth.ToInt();
                        }

                        if(!c.Hidden && c.Path!="_")
                        {
                            include=true;
                        }
                        
                        var a = (int) c.Width2 * symbolWidth;
                        var b = (int)((double) w / (double) symbolWidth);

                        if(include)
                        {
                            s=$"{s}\n";                  
                            s=$"{s} {j.ToString().SPadLeft(2)} | ";
                            s=$"{s} {p.ToString().SPadLeft(15)} | ";
                            s=$"{s} {h.ToString().SPadLeft(15)} | ";
                            s=$"{s} {c.Width2.ToString().SPadLeft(2)} | ";
                            s=$"{s} {b.ToString().SPadLeft(3)} | ";
                            s=$"{s} {c.WidthRelative.ToString().SPadLeft(3)} | ";
                            s=$"{s} {a.ToString().SPadLeft(3)} | ";
                            s=$"{s} {w.ToString().SPadLeft(3)} | ";
                            s=$"{s} {l.ToString().SPadLeft(20)}";
                        }                    
                    }
                    
                    {
                        var t = "";
                        t = t.Append($" ", true);
                        t = t.Append($" w1 -- заданная ширина, симв. ",true);
                        t = t.Append($" w2 -- фактическая ширина, симв.",true);
                        t = t.Append($" wr -- относительная ширина, доля [1-1000]",true);
                        t = t.Append($" wc -- заданная ширина, пикс.",true);
                        t = t.Append($" wa -- фактическая ширина, пикс.",true);
                        s=s.Append($"{t}");    
                    }

                    var msg=s;
                    var d = new LogWindow($"{msg}", "Конфигурация колонок" );
                    d.ShowDialog();
                }
            }
        }

        public void ColumnsSetResizeable()
        {
            if(Initialized)
            {
                if(Columns.Count>0)
                {
                    foreach(DataGridHelperColumn c in Columns)
                    {
                        var gridColumn=FindColumn(c);
                        if(gridColumn != null)
                        {
                            gridColumn.MinWidth=45;
                            gridColumn.MaxWidth=900;
                        }
                    }
                }
            }
        }

        private int CalculateColumnWidth(int stringLength=2)
        {
            int widthAddon=25;

            

            //if (ColumnAutowidthMode == ColumnAutoWidthModeRef.Full)
            //{
            //    widthAddon=30;                
            //}

            var result=(stringLength*ColumnSymbolWidth)+widthAddon;

            //int m = ColumnSymbolWidth * 7;
            if (result < ColumnMinWidth )
            {
                result = ColumnMinWidth;
            }
            
            return result;
        }

        public int symbolWidth { get; set; }
        public void ColumnsUpdateSize()
        {
            ColumnAutowidthLog = "";
            symbolWidth = 8;

            if (Initialized)
            {
                if (Columns.Count > 0)
                {
                    ColumnAutowidthLog = ColumnAutowidthLog.Append($"грид: {Name}",true);
                    ColumnAutowidthLog = ColumnAutowidthLog.Append($"режим: {ColumnWidthMode}",true);
                    ColumnAutowidthLog = ColumnAutowidthLog.Append($"ширина символа: {symbolWidth}",true);
                    
                    // defaults
                    foreach (DataGridHelperColumn c in Columns)
                    {
                        if (!c.Hidden && c.Path!="_")
                        {
                            if (c.Width2 == 0)
                            {
                                switch (c.ColumnType)
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
                        }
                    }
                    
                    
                    // summ
                    int widthTotal = 0;
                    foreach (DataGridHelperColumn c in Columns)
                    {
                        if (!c.Hidden && c.Path!="_")
                        {
                            widthTotal = widthTotal + c.Width2;    
                        }
                    }
                    ColumnAutowidthLog = ColumnAutowidthLog.Append($"суммарная ширина: {widthTotal}",true);
                    
                    int blockWidth = (int)Grid.ActualWidth;
                    ColumnAutowidthLog = ColumnAutowidthLog.Append($"ширина блока: {blockWidth}",true);
                    
                    
                    //relative width
                    foreach (DataGridHelperColumn c in Columns)
                    {
                        if (!c.Hidden && c.Path!="_")
                        {
                            if (ColumnWidthMode == ColumnWidthModeRef.Compact)
                            {
                                c.WidthRelative = (int) (((double) (c.Width2) / (double) (widthTotal)) * 1000);
                            }
                            
                            if (ColumnWidthMode == ColumnWidthModeRef.Full)
                            {
                                //widthTotal = 5000;
                                //var w = (int)(widthTotal * symbolWidth);
                                //c.WidthRelative = (int) (((double) (c.Width2) / (double) (w)) * 10000);
                                c.WidthRelative = (int) (((double) (c.Width2) / (double) (widthTotal)) * 1000);
                            }
                        }
                    }
                    
                    //assign
                    foreach (DataGridHelperColumn c in Columns)
                    {
                        if (!c.Hidden && c.Path!="_")
                        {
                            var gridColumn=FindColumn(c);
                            if (gridColumn != null)
                            {

                                if (ColumnWidthMode == ColumnWidthModeRef.Compact)
                                {
                                    gridColumn.MinWidth = 35;
                                    gridColumn.MaxWidth = 2000;
                                    gridColumn.Width = new DataGridLength(c.WidthRelative, DataGridLengthUnitType.Star);    
                                }
                                
                                if (ColumnWidthMode == ColumnWidthModeRef.Full)
                                {
                                    var w = (int) c.Width2 * symbolWidth;

                                    /*
                                    {
                                        gridColumn.MinWidth = 35;
                                        gridColumn.MaxWidth = 2000;
                                        gridColumn.Width = new DataGridLength(c.WidthRelative, DataGridLengthUnitType.Star);    
                                    }
                                    */

                                    
                                    {
                                        gridColumn.MinWidth = w;
                                        gridColumn.MaxWidth = 2000;
                                        gridColumn.Width = new DataGridLength(w, DataGridLengthUnitType.Pixel);
                                    }
                                    

                                    /*
                                    {
                                        var ww = 2880;
                                        var r =  (int) (((double) (c.Width2) / (double) (ww)) * 1000);
                                        
                                        Grid.Width = ww;
                                        gridColumn.MinWidth = 35;
                                        gridColumn.MaxWidth = 2000;
                                        gridColumn.Width = new DataGridLength(r, DataGridLengthUnitType.Star); 
                                    }
                                    */
                                    
                                    switch (c.ColumnType)
                                    {
                                        case ColumnTypeRef.Boolean:
                                        {
                                            gridColumn.MinWidth = 35;
                                            
                                        }
                                            break;

                                        case ColumnTypeRef.DateTime:
                                        {
                                        }
                                            break;

                                        case ColumnTypeRef.Integer:
                                        case ColumnTypeRef.Double:
                                        {
                                        }
                                            break;

                                        case ColumnTypeRef.String:
                                        default:
                                        {
                                        }
                                            break;
                                    }
                                }
                                
                                
                                
                            }
                        }

                        if (c.Path == "_")
                        {
                            var gridColumn=FindColumn(c);
                            if (gridColumn != null)
                            {
                                if (ColumnWidthMode == ColumnWidthModeRef.Compact)
                                {
                                    gridColumn.MinWidth = 0;
                                    gridColumn.MaxWidth = 0;
                                    gridColumn.Width = 0;
                                }
                                
                                if (ColumnWidthMode == ColumnWidthModeRef.Full)
                                {
                                    gridColumn.MinWidth = 35;
                                    gridColumn.MaxWidth = 2000;
                                    gridColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                                    //gridColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                                }
                            }
                        }
                    }

                }
            }


        }
        
        
        public void ColumnsAutoResize0()
        {
            int columnStringLengthDynamicLimit=32;
            int columnStringLengthHeaderLimit=65;
            double columnStringLengthHeaderTolerance=0.45;

            /* 
                (1) подрезка строки
                если ширина текстовой колонки оказалась больше лимита: columnStringLengthDynamicLimit
                ширина колонки будет установлена равной лимиту

                (2) расширение колонки
                если ширина колонки меньше лимита: columnStringLengthHeaderLimit
                заголовок не помещается в колонку
                если колонку можно расширить на долю: columnStringLengthHeaderTolerance
                так, чтобы заголовок помещался, 
                тогда колонка будет расширена
             */
            
            ColumnAutowidthLog = "";
            
            if(Initialized)
            {
                if(Columns.Count>0)
                {
                    //выбор режима
                    {
                        switch (ColumnWidthMode)
                        {
                            case ColumnWidthModeRef.Compact:
                            {
                                columnStringLengthDynamicLimit=20;
                                columnStringLengthHeaderLimit=0;
                            }
                            break;

                            case ColumnWidthModeRef.Full:
                            {
                                columnStringLengthDynamicLimit=120;
                                columnStringLengthHeaderLimit=65;
                            }
                            break;
                        }
                    }

                    foreach (DataGridHelperColumn c in Columns)
                    {
                        c.AutoWidthLog = "";
                        c.AutoWidthLog=c.AutoWidthLog.Append($" (1)");
                        c.ContentLength=ColumnMinSymbols;
                        c.AutoWidth=0;

                        c.MinWidth=ColumnMinWidth;
                        c.MaxWidth=ColumnMaxWidth;

                        c.AutoWidthLog=c.AutoWidthLog.Append($" def[{c.AutoWidth}]");
                    }

                    int iteration = 0;
                    bool doIterations = true;
                    var prf=new Profiler();

                    //while (doIterations)
                    {
                        iteration++;
                        
                        ProcColumnsWidth(columnStringLengthDynamicLimit, columnStringLengthHeaderLimit, columnStringLengthHeaderTolerance);

                        var totalWidth = 0;
                        var spacerWidth = 0;
                        {
                            foreach (DataGridHelperColumn c in Columns)
                            {
                                var gridColumn = FindColumn(c);
                                if (gridColumn != null)
                                {
                                    totalWidth = totalWidth + (int)gridColumn.ActualWidth;
                                    if (c.Path == "_")
                                    {
                                        spacerWidth = (int) gridColumn.ActualWidth;
                                    }
                                }
                            }
                        }

                        var dt = (int)prf.GetDelta();
                        ColumnAutowidthLog = ColumnAutowidthLog.Append($"iteration=[{iteration}] dt=[{dt}]",true);
                        ColumnAutowidthLog = ColumnAutowidthLog.Append($"    ColumnAutowidthMode=[{ColumnWidthMode}] totalWidth=[{totalWidth}] spacerWidth=[{spacerWidth}]",true);
                        ColumnAutowidthLog = ColumnAutowidthLog.Append($"    columnStringLengthDynamicLimit=[{columnStringLengthDynamicLimit}] columnStringLengthHeaderLimit=[{columnStringLengthHeaderLimit}] columnStringLengthHeaderTolerance=[{columnStringLengthHeaderTolerance}]",true);

                        /*
                        if (spacerWidth > 150)
                        {
                            columnStringLengthDynamicLimit=columnStringLengthDynamicLimit+(int)(columnStringLengthDynamicLimit*0.5);
                        }
                        else
                        {
                            doIterations = false;
                        }

                        if (ColumnAutowidthMode == ColumnAutoWidthModeRef.Compact)
                        {
                            if (iteration >= 3)
                            {
                                doIterations = false;
                            }
                        }
                        
                        if (ColumnAutowidthMode == ColumnAutoWidthModeRef.Full)
                        {
                            doIterations = false;
                        }
                        */
                    }
                    
                }
            }
        }

        public void ProcColumnsWidth(int columnStringLengthDynamicLimit, int columnStringLengthHeaderLimit, double columnStringLengthHeaderTolerance)
        {
            
            foreach(DataGridHelperColumn c in Columns)
            {
                c.AutoWidthLog=c.AutoWidthLog.Append($" (2)");

                {
                    // autowidth calculate (ContentWidth AutoWidth)
                    {
                        if(Items != null)
                        {
                            if(Items.Count > 0)
                            {
                                foreach(Dictionary<string,string> row in Items)
                                {
                                    var s=row.CheckGet(c.Path);
                                    if(!s.IsNullOrEmpty())
                                    {
                                        var l=s.Length;

                                        // если в строке > 75% капса, длину увеличим на 25%
                                        {
                                            int count = 0;
                                            for (int i = 0; i < s.Length; i++)
                                            {
                                                if (char.IsUpper(s[i])) count++;
                                            }

                                            if (count > (l-l*0.75))
                                            {
                                                l = l + (int)(l * 0.25);
                                            }
                                        }
                                        
                                        
                                        if(l > c.ContentLength)
                                        {
                                            c.ContentLength=l;
                                        }
                                    }                                    
                                }

                                c.AutoWidthLog=c.AutoWidthLog.Append($" cnt_len=[{c.ContentLength}]");

                                c.ContentLength2 = c.ContentLength;
                                {
                                    //если строка длиннее определенного лимита, подрезка                                        
                                    if(columnStringLengthDynamicLimit > 0)
                                    {
                                        if(c.ContentLength > columnStringLengthDynamicLimit)
                                        {
                                            c.ContentLength=columnStringLengthDynamicLimit;
                                            c.AutoWidthLog=c.AutoWidthLog.Append($" limit[{c.ContentLength}]");
                                        }
                                    }
                                }

                                if(c.ContentLength > 0)
                                {
                                    c.AutoWidth=CalculateColumnWidth(c.ContentLength);                                    
                                }     
                            }
                        }
                    }
                    
                    
                    // autowidth fine tuning 
                    {
                        //если заголовок можно показать полностью, нужно немного расширить
                        if(columnStringLengthHeaderLimit > 0 )
                        {
                            var headerLength=c.Header.Length;
                            if(headerLength > 0)
                            {
                                var headerWidth=CalculateColumnWidth(headerLength);

                                if(c.AutoWidth <= columnStringLengthHeaderLimit)
                                {
                                    if(c.AutoWidth < headerWidth)
                                    {
                                        var a=(int)(c.AutoWidth+(int)(c.AutoWidth*columnStringLengthHeaderTolerance));
                                        if(a >= headerWidth)
                                        {
                                            c.AutoWidth=a;
                                            c.AutoWidthLog=c.AutoWidthLog.Append($" expand[{c.AutoWidth}]");
                                        }
                                    }
                                }
                            }                                                
                        }
                    }
                }


                // minwidth, maxwidth
                {
                    c.Width = 0;
                    
                    {
                        c.MaxWidth = CalculateColumnWidth(c.ContentLength2);

                        if (ColumnWidthMode == ColumnWidthModeRef.Full)
                        {
                            if (c.Header.Length > c.ContentLength2)
                            {
                                //небольшое увеличение, т.к. часть будет скрыта под символами сортировки колонки
                                var m = c.Header.Length + 4;
                                c.MaxWidth = CalculateColumnWidth(m);
                                c.AutoWidthLog = c.AutoWidthLog.Append($" expand_hdr[{c.MaxWidth}]");
                            }
                        }
                    }
                    
                    switch(c.ColumnType)
                    {
                        case ColumnTypeRef.Boolean:
                        {
                            if (ColumnWidthMode == ColumnWidthModeRef.Compact)
                            {
                                c.MinWidth = ColumnMinWidth;
                                c.Width = ColumnMinWidth;
                            }

                            if (ColumnWidthMode == ColumnWidthModeRef.Full)
                            {
                                c.MinWidth = c.AutoWidth;
                            }
                        }
                            break;
                        
                        case ColumnTypeRef.DateTime:
                        {
                            if (ColumnWidthMode == ColumnWidthModeRef.Compact)
                            {
                                c.MinWidth = 80;
                                
                                // если задан формат даты, вычислим длину строки от формата
                                if (!c.Format.IsNullOrEmpty())
                                {
                                    var l = c.Format.Length;
                                    var m = CalculateColumnWidth(l);
                                    c.MinWidth = m-10;
                                    c.AutoWidthLog = c.AutoWidthLog.Append($" expand_min_fromat[{c.MinWidth}]");
                                    
                                    c.MaxWidth = c.MinWidth;
                                }
                                
                                c.Width = c.MinWidth;
                                
                            }

                            if (ColumnWidthMode == ColumnWidthModeRef.Full)
                            {
                                c.MinWidth = c.AutoWidth;
                                c.MaxWidth = CalculateColumnWidth(c.ContentLength);
                            }
                        }
                            break;

                        case ColumnTypeRef.Integer:
                        case ColumnTypeRef.Double:
                        {
                            if (ColumnWidthMode == ColumnWidthModeRef.Compact)
                            {
                            }

                            if (ColumnWidthMode == ColumnWidthModeRef.Full)
                            {
                                c.MinWidth = c.AutoWidth;
                            }
                        }
                            break;

                        case ColumnTypeRef.String:
                        default:
                        {
                            if (ColumnWidthMode == ColumnWidthModeRef.Compact)
                            {
                                c.Width = c.AutoWidth;
                            }

                            if (ColumnWidthMode == ColumnWidthModeRef.Full)
                            {
                                c.MinWidth = c.AutoWidth;
                                c.Width = c.AutoWidth;
                            }
                        }
                            break;
                    }

                    if ( c.MinWidth > c.MaxWidth )
                    {
                        c.MaxWidth = c.MinWidth;
                        c.AutoWidthLog=c.AutoWidthLog.Append($" max_replace[{c.MaxWidth}]");
                    }
                }

                //колонка-распорка
                {
                    if(c.Path == "_")
                    {
                        c.Width=0;
                        c.MinWidth=1;
                        c.MaxWidth=2000;
                        
                        if (ColumnWidthMode == ColumnWidthModeRef.Full)
                        {
                            c.MinWidth=1;
                        }   
                    }
                }
                
                c.AutoWidthLog=c.AutoWidthLog.Append($" auto[{c.AutoWidth}]");
            }
          
          
            // pass to render
            int j = 0;
            foreach(DataGridHelperColumn c in Columns)
            {
                {
                    j++;
                    var gridColumn=FindColumn(c);
                    if(gridColumn != null)
                    {
                        c.AutoWidthLog=c.AutoWidthLog.Append($" (9)");
                        c.AutoWidthLog=c.AutoWidthLog.Append($" [{c.MinWidth}]-[{c.Width}]-[{c.MaxWidth}]");
                        
                        if (c.MinWidth > 0)
                        {
                            gridColumn.MinWidth=c.MinWidth;    
                        }

                        /*
                        if (j==1)
                        {
                            gridColumn.Width = new DataGridLength(0, DataGridLengthUnitType.Auto);
                        }
                        else
                        {
                            gridColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                        }
                        */
                        
                        //gridColumn.Width = (DataGridLength)DependencyProperty.UnsetValue;
                        //gridColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
                        gridColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                        
                        if (c.Width > 0)
                        {
                            //gridColumn.Width=c.Width;
                            //gridColumn.Width = new DataGridLength(c.Width, DataGridLengthUnitType.Pixel);
                        }

                        if (c.MaxWidth > 0)
                        {
                            gridColumn.MaxWidth=c.MaxWidth;    
                        }
                        
                        if(c.Path == "_")
                        {
                            if (ColumnWidthMode == ColumnWidthModeRef.Compact)
                            {
                                //gridColumn.Width = new DataGridLength(0.01, DataGridLengthUnitType.Star);
                                gridColumn.MinWidth = 0;
                                gridColumn.MaxWidth = 2000;
                            } 
                            
                            if (ColumnWidthMode == ColumnWidthModeRef.Full)
                            {
                                //gridColumn.Width = new DataGridLength(0.01, DataGridLengthUnitType.Star);
                                gridColumn.MinWidth = 0;
                                gridColumn.MaxWidth = 2000;
                            }   
                        }
                    }
                }
            }
        }

        public DataGridColumn FindColumn(DataGridHelperColumn column)
        {
            DataGridColumn result=null;

            if(Initialized)
            {
                if(Grid.Columns.Count > 0)
                {
                    foreach (DataGridColumn c in Grid.Columns)
                    {
                        var n=DataGridUtil.GetName(c).ToString();
                        if(n == column.Name )
                        {
                            result=c;
                            break;
                        }
                    }
                }
            }

            return result;
        }

        public void ShowColumnsList()
        {
            if(Columns.Count>0)
            {
                var s = "";
                foreach(DataGridHelperColumn c in Columns)
                {
                    s=$"{s}\n {c.Header.ToString()}";                  
                }

                var msg = s;
                var d = new DialogWindow($"{msg}","Колонки грида","",DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        public void ShowColumnsDoc()
        {
            if(Columns.Count>0)
            {
                var table="";
                foreach(DataGridHelperColumn c in Columns)
                {
                    var row="";
                    { 
                        row=$"{row}    <td>{c.Header.ToString()}</td>\n";
                    }
                    { 
                        row=$"{row}    <td>{c.Doc.ToString()}</td>\n";
                    }
                    { 
                        row=$"{row}    <td></td>\n";
                    }

                    table=$"{table}<tr>\n{row}<tr>\n";
                }

                table=$"<table class='brd'>{table}</table>";
                
                var msg=table;
                var d = new DialogWindow($"{msg}", "Колонки грида", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
            
        }
        public void ProcessKeyboardEvents(KeyEventArgs e)
        {

        }

        public async void ExportItemsExcel()
        {
            if(GridItems.Count > 0)
            {
                var eg = new ExcelGrid();
                var cols = Columns;
                eg.SetColumnsFromGrid(cols);
                eg.Items = GridItems;
                await System.Threading.Tasks.Task.Run(() =>
                {
                    eg.Make();
                });
            }
        }

    }


    public class ProxyHightlighter:IValueConverter
    {
        public object Convert(object value,Type targetType,object parameter,CultureInfo culture)
        {
            if(value!=null)
            {
                var v = (Dictionary<string,string>)value;
                if(parameter!=null)
                {
                    var d = (StylerDelegate)parameter;
                    if(d!=null)
                    {
                        var mybrush = d.Invoke(v);
                        if(mybrush!=null)
                        {
                            return mybrush;
                        }
                    }
                }
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)
        {
            return null;
        }

    }

     public class ProxyFontWight:IValueConverter
    {
        public object Convert(object value,Type targetType,object parameter,CultureInfo culture)
        {
            if(value!=null)
            {
                var v = (Dictionary<string,string>)value;
                if(parameter!=null)
                {
                    var d = (StylerDelegate)parameter;
                    if(d!=null)
                    {
                        var fontWeight = d.Invoke(v);
                        if(fontWeight!=null)
                        {
                            return fontWeight;
                        }
                    }
                }
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)
        {
            return null;
        }

    }

    public class ProxyConverter:IValueConverter
    {
        public object Convert(object value,Type targetType,object parameter,CultureInfo culture)
        {
            if(value!=null)
            {
                var v = value.ToString();
                if(parameter!=null)
                {
                    var d = (FormatterDelegate)parameter;
                    if(d!=null)
                    {
                        var myvar = d.Invoke(v);
                        if(myvar!=null)
                        {
                            return myvar;
                        }
                    }
                }
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)
        {
            return null;
        }

    }

    public class ProxyRawConverter:IValueConverter
    {
        public object Convert(object value,Type targetType,object parameter,CultureInfo culture)
        {
            if(value!=null)
            {
                var v = (Dictionary<string,string>)value;
                if(parameter!=null)
                {
                    var d = (FormatterRawDelegate)parameter;
                    if(d!=null)
                    {
                        var myvar = d.Invoke(v);
                        if(myvar!=null)
                        {
                            return myvar;
                        }
                    }
                }
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)
        {
            return null;
        }

    }


    public class DataGridHelperColumn
    {
        public DataGridHelperColumn()
        {
            Visible=true;
            Hidden=false;
            Editable=false;
            Enabled=true;
            Exportable=true;
            Searchable=false;
            Index=0;
            RowIndex=0;
            RowNumber=0;
            ColumnIndex=0;
            Name="";
            Description="";
            Header="";
            Doc="";
            Path="";
            Style="";
            CellControlStyle = null;
            CellControlStyle2 = null;
            ColumnType =ColumnTypeRef.String;
            Format="";
            FormatInput="";
            Group="";
            Width=20;
            MinWidth=0;
            MaxWidth=400;
            Formatter=null;
            FormatterRaw=null;
            Options = "";
            Items=new Dictionary<string, string>();

            if(Hidden)
            {
                Enabled=false;
            }

            OnClickAction=null;
            ContentLength=0;
            ContentLength2 = 0;
            AutoWidth=0;
            AutoWidthLog="";
            WidthRelative = 0;
            Width2 = 0;
            TooltipDoc="";
            Converter=null;
            TotalsType = TotalsTypeRef.None;
            Labels = new List<DataGridHelperColumnLabel>();


            DxEnableColumnFiltering = false;
            DxEnableColumnSorting = true;
        }

        public DataGridHelperColumn(string header, string path, ColumnTypeRef columnType, int minWidth, int maxWidth, string format = "")
        {
            Header = header;
            Path = path;
            ColumnType = columnType;
            MinWidth = minWidth;
            MaxWidth = maxWidth;
            Format = format;
            Params=new Dictionary<string, string>();
        }


        public bool Enabled { get; set; }
        public bool Hidden { get; set; }
        public bool Visible { get; set; }=true;
        public bool Exportable { get; set; }
        public bool Editable { get; set; } = false;
        /// <summary>
        /// Флаг использования при поиске для скрытой колонки. Для видимых колонок игнорируется
        /// </summary>
        public bool Searchable { get; set; } = false;
        public int Index { get; set; }
        public int RowIndex { get; set; }
        public int RowNumber { get; set; }        
        public int ColumnIndex { get; set; }
        public string Name { get; set; }
        /// <summary>
        /// примечание
        /// (всплывающая подсказка при наведении мыши на заголовок)
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// заголовок колонки
        /// </summary>
        public string Header { get; set; }
        /// <summary>
        /// подробное описание колонки 
        /// (для системы автодокументации)
        /// </summary>
        public string Doc { get; set; }="";
        /// <summary>
        /// имя колонки
        /// (уникальный идентификатор в датасете)
        /// </summary>
        public string Path { get; set; }
        public string Style { get; set; }
        public Style CellControlStyle { get; set; }
        public Style CellControlStyle2 { get; set; }
        public Style CellControlStyle3 { get; set; }
        public enum ColumnTypeRef
        {
            String = 1,
            Integer = 2,
            Double = 3,
            DateTime = 4,
            Boolean = 5,
            Image = 6,
            SelectBox = 7,
        }
        /// <summary>
        /// тип данных колонки
        /// </summary>
        public ColumnTypeRef ColumnType { get; set; }
        /// <summary>
        /// дополнительные параметры отображения
        /// zeroempty--показать пустое поле вместо 0 для int
        /// </summary>
        public string Options { get; set; }="";

        /*
           Используются формула для форматирования дат и чисел:
           DateTime    dd.MM.yyyy...
           Integer
           Double      N2,N3...

           В соотв. со следующими принципами

                           C#                          Oracle DB
           --------------  --------------------------  -------------------
           0--num
                           N2
                           N3
                           N0
           2--date         dd.MM.yyyy                  dd.mm.yy
           3--datetime     dd.MM.yyyy HH:mm:ss         dd.mm.yy hh24:mi:ss
           4--datetimehm   dd.MM.yyyy HH:mm            dd.mm.yy hh24:mi
           5--dateshorthm  dd.MM HH:mm                 dd.mm hh24:mi
           6--dateshort    dd.MM                       dd.mm 

           string.Format(culture, "{0:N2}", double.Parse(s));
           => N2
           C / c -- Задает формат денежной единицы, указывает количество десятичных разрядов после запятой
           D / d -- Целочисленный формат, указывает минимальное количество цифр
           E / e -- Экспоненциальное представление числа, указывает количество десятичных разрядов после запятой
           F / f -- Формат дробных чисел с фиксированной точкой, указывает количество десятичных разрядов после запятой
           G / g -- Задает более короткий из двух форматов: F или E
           N / n -- Также задает формат дробных чисел с фиксированной точкой, определяет количество разрядов после запятой
           P / p -- Задает отображения знака процентов рядом с число, указывает количество десятичных разрядов после запятой
           X / x -- Шестнадцатеричный формат числа
       */
        /// <summary>
        /// для дат: dd.MM.yyyy HH:mm:ss для чисел: N2
        /// </summary>
        public string Format { get; set; }
        public string FormatInput { get; set; }="dd.MM.yyyy HH:mm:ss";
        public string Group { get; set; }
        public int Width { get; set; }
        public int MinWidth { get; set; }
        public int MaxWidth { get; set; }
        public Dictionary<string,string> Items { get; set; }

        public delegate object StylerDelegate(Dictionary<string,string> row);
        public delegate object StylerDelegate2(Dictionary<string,string> row, int mode=0);
        public enum StylerTypeRef
        {
            BackgroundColor = 1,
            BorderColor = 2,
            ForegroundColor = 3,
            FontWeight = 4,
        }
        public Dictionary<StylerTypeRef,StylerDelegate> Stylers=new Dictionary<StylerTypeRef, StylerDelegate>();
        public List<StylerProcessor> Stylers2=new List<StylerProcessor>();
        public delegate object FormatterDelegate(string value);
        public delegate object FormatterRawDelegate(Dictionary<string,string> value);
        public FormatterDelegate Formatter { get;set;}=null;
        public FormatterRawDelegate FormatterRaw { get;set;}=null; 
        
        public delegate object OnClickDelegate (Dictionary<string,string> value,FrameworkElement element);
        public OnClickDelegate OnClickAction { get;set;}

        public delegate object OnAfterClickDelegate(Dictionary<string, string> value, FrameworkElement element);
        public OnAfterClickDelegate OnAfterClickAction { get; set; }

        public delegate object OnChangeDelegate (Dictionary<string,string> row, string value, string oldValue, FrameworkElement element);
        public OnChangeDelegate OnChangeAction { get;set;}
        
        public delegate object TotalsDelegate(List<Dictionary<string,string>> rows);
        public TotalsDelegate Totals { get;set;}=null;

        public enum TotalsTypeRef
        {
            None = 0,
            Summ = 1,
            Count = 2,            
        }
        public TotalsTypeRef TotalsType { get; set; } = TotalsTypeRef.None;

        public delegate List<Border> OnRenderDelegate(Dictionary<string,string> row, Border element);
        public OnRenderDelegate OnRender { get;set;}

        public delegate List<FrameworkElementFactory> OnRender4Delegate(Dictionary<string, string> row, FrameworkElementFactory ctl);
        public OnRender4Delegate OnRender4 { get; set; }

        public Dictionary<string,string> Params { get;set;}
        public List<DataGridHelperColumnLabel> Labels{ get; set; }

        /// <summary>
        /// длина контента в ячейке, число символов
        /// </summary>
        public int ContentLength  { get;set;}=0;
        public int ContentLength2  { get;set;}=0;
        /// <summary>
        /// автоматически рассчитанная ширина колонки
        /// </summary>
        public int AutoWidth  { get;set;}=0;       
        /// <summary>
        /// отчет по расчеты ширины колонки
        /// </summary>
        public string AutoWidthLog  { get;set;}
        
        /// <summary>
        /// относительная ширина колонки, доля от общей ширины [1-100]
        /// </summary>
        public int WidthRelative  { get;set;}
        /// <summary>
        /// заданная ширина в символах, число
        /// </summary>
        public int Width2  { get;set;}

        public string TooltipDoc  { get;set;}
        public GridBox4DataConverter Converter { get;set;}

        // добавление фильтров для GridBox4
        public bool DxEnableColumnSorting { get; set; }
        public bool DxEnableColumnFiltering { get; set; }

        public string DxHeaderToolTip { get;set;}
    }

    public class StylerProcessor
    {
        public StylerProcessor(StylerTypeRef type, StylerDelegate2 callback, string description="")
        {
            StylerType=type;
            Processor=callback; 
            Description=description;
        }
        public StylerTypeRef StylerType {get;set;}
        public StylerDelegate2 Processor {get;set;} 
        public string Description {get;set;} 
    }

    public class DataGridHelperDateTime:IValueConverter
    {
        public object Convert(object value,Type targetType,object parameter,CultureInfo culture)
        {
            string result = "";

            if(value != null)
            {
                var s = value.ToString();
                
                
                var p = "";
                if(parameter!=null)
                {
                    p=parameter.ToString();
                }

                if(string.IsNullOrEmpty(p))
                {
                    p="dd.MM.yyyy HH:mm:ss";
                }

                if(!string.IsNullOrEmpty(s))
                {
                    var d=s.ToDateTime();
                    s=d.ToString(p);
                    result = s;
                }
            }

            return result;
        }

        public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)
        {
            return null;
        }

    }

    public class DataGridHelperDouble:IValueConverter
    {
        public object Convert(object value,Type targetType,object parameter,CultureInfo culture)
        {
            string result = "";

            if(value != null)
            {
                var s = value.ToString();

                var p = "N2";
                if(parameter != null)
                {
                    string ps = parameter.ToString();
                    if(!string.IsNullOrEmpty(ps))
                    {
                        p=ps;
                    }
                }

                //"{0:N2}"
                string f = "{0:"+p+"}";

                if(!string.IsNullOrEmpty(s))
                {
                    IFormatProvider formatter = new NumberFormatInfo { NumberDecimalSeparator = "," };

                    if(s.IndexOf(".") > -1)
                    {
                        s=s.Replace(".",",");
                    }

                    double r = 0;
                    var parseResult = double.TryParse(s,NumberStyles.Number,formatter,out r);
                    if(parseResult)
                    {
                        s = string.Format(culture,f,r);
                    }

                    s = s.Replace(",","");
                    s = s.Replace(".",",");
                    s = $"{s}";
                    result = s;
                }
            }

            return result;
        }

        public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)
        {
            return null;
        }

    }
    
    public class DataGridHelperInteger:IValueConverter
    {
        public object Convert(object value,Type targetType,object parameter,CultureInfo culture)
        {
            string result = "";

            if(value != null)
            {
                var s = value.ToString();

                var p = "N0";
                if(parameter != null)
                {
                    string ps = parameter.ToString();
                    if(!string.IsNullOrEmpty(ps))
                    {
                        p=ps;
                    }
                }

                //"{0:N2}"
                string f = "{0:"+p+"}";

                if(!string.IsNullOrEmpty(s))
                {
                    IFormatProvider formatter = new NumberFormatInfo { NumberDecimalSeparator = "," };

                    if(s.IndexOf(".") > -1)
                    {
                        s=s.Replace(".",",");
                    }

                    double r = 0;
                    var parseResult = double.TryParse(s,NumberStyles.Number,formatter,out r);
                    if(parseResult)
                    {
                        s = string.Format(culture,f,r);
                    }

                    s = s.Replace(",","");
                    s = s.Replace(".",",");
                    result = s;
                }
            }
            
            /*
            string result = "";

            if(value != null)
            {
                var s = value.ToString();

                if(!string.IsNullOrEmpty(s))
                {
                    IFormatProvider formatter = new NumberFormatInfo { NumberDecimalSeparator = "," };

                    int r = 0;
                    var parseResult = int.TryParse(s,NumberStyles.Number,formatter,out r);
                    if(parseResult)
                    {
                        s = r.ToString();
                    }

                    result = s;
                }
            }

            */
            return result;
        }

        public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)
        {
            var s = value.ToString();
            return s;
        }

    }

    public class DataGridHelperSelect:IValueConverter
    {
        public DataGridHelperSelect(Dictionary<string, string> items=null)
        {
            Items=new Dictionary<string, string>();
            if(items!=null)
            {
                Items=items;
            }
        }

        public Dictionary<string, string> Items {get;set;}
    
        public object Convert(object value,Type targetType,object parameter,CultureInfo culture)
        {
            string result = "";

            if(value != null)
            {
                var s = value.ToString();
                result=FindValueByKey(s);               
            }

            return result;
        }

        public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)
        {
            string result = "";

            if(value != null)
            {
                var s = value.ToString();
                result=FindKeyByValue(s);               
            }

            return result;
        }

        public string FindValueByKey(string key)
        {
            string result = "";

            if(!string.IsNullOrEmpty(key))
            {
                
                key=key.ToInt().ToString();    

                if(Items!=null)
                {
                    if(Items.Count>0)
                    {
                        foreach(KeyValuePair<string,string> i in Items)
                        {
                            if(i.Key == key)
                            {
                               result=i.Value;
                            }
                        }
                    }
                }
            }

            return result;
        }

        public string FindKeyByValue(string value)
        {
            string result = "";

            if(!string.IsNullOrEmpty(value))
            {
                if(Items!=null)
                {
                    if(Items.Count>0)
                    {
                        foreach(KeyValuePair<string,string> i in Items)
                        {
                            if(i.Value == value)
                            {
                               result=i.Key;
                            }
                        }
                    }
                }
            }

            return result;
        }

      


    }

    public class DataGridHelperSorter:IComparer
    {
        readonly ListSortDirection _direction;
        readonly string _path;
        readonly DataGridHelperColumn.ColumnTypeRef _type;
        readonly DataGridHelperColumn _column;

        /*
                            C#                          Oracle DB
            --------------  --------------------------  -------------------
            0--num, 
            1--string,                                
            2--date         dd.MM.yyyy                  dd.mm.yy
            3--datetime     dd.MM.yyyy HH:mm:ss         dd.mm.yy hh24:mi:ss
            4--datetimehm   dd.MM.yyyy HH:mm            dd.mm.yy hh24:mi
            5--dateshorthm  dd.MM HH:mm                 dd.mm hh24:mi
            6--dateshort    dd.MM                       dd.mm 
        */
        public DataGridHelperSorter(ListSortDirection direction,string path,DataGridHelperColumn column)
        {
            _direction = direction;
            _path = path;
            _column=column;
            _type=column.ColumnType;
        }

        public int Compare(object x = null,object y = null)
        {
            int result = 0;
            string dbg = ".";
            var provider = CultureInfo.InvariantCulture;

            if(x!=null && y!=null)
            {

                bool resume = true;

                var x1 = x as Dictionary<string,string>;
                var y1 = y as Dictionary<string,string>;

                if(resume)
                {
                    if(x1==null || y1==null)
                    {
                        resume=false;
                        dbg=$"{dbg} b0";
                    }
                }


                var p = _path;
                p = p.Replace("[","").Replace("]","");

                var x2 = "";
                var y2 = "";

                if(resume)
                {
                    if(x1.ContainsKey(p))
                    {
                        if(!string.IsNullOrEmpty(x1[p]))
                        {
                            x2=x1[p].ToString();
                        }

                    }

                    if(y1.ContainsKey(p))
                    {
                        if(!string.IsNullOrEmpty(y1[p]))
                        {
                            y2=y1[p].ToString();
                        }
                    }
                }


                bool x0 = false;
                bool y0 = false;

                if(resume)
                {
                    /*
                        Если из базы вернется null
                        то здесь образуется пустая строка
                        в этом случае нужно приравнять аргумент значению по умолчанию.
                        Для каждого типа данных это будет свое значение.
                        Если оба значения нулевые, то сравнивать их нечего.
                     */
                    if(string.IsNullOrEmpty(x2) || string.IsNullOrEmpty(y2))
                    {
                        if(string.IsNullOrEmpty(x2))
                        {
                            x0=true;
                        }

                        if(string.IsNullOrEmpty(y2))
                        {
                            y0=true;
                        }

                        if(x0 && y0)
                        {
                            dbg=$"{dbg} b2";
                        }
                    }
                }
                else
                {
                    dbg=$"{dbg} b3";
                }



                if(resume)
                {
                    // меняем направление x1 <-> y1

                    dbg=$"{dbg}.";


                    if(_direction == ListSortDirection.Descending)
                    {
                        var t = x2;
                        x2 = y2;
                        y2 = t;

                        var t0 = x0;
                        x0 = y0;
                        y0 = t0;
                    }


                    dbg=$"{dbg} type=[{_column.ColumnType}] p=[{p}]";


                    switch(_column.ColumnType)
                    {
                        case DataGridHelperColumn.ColumnTypeRef.String:

                            if(x0)
                            {
                                x2="";
                            }

                            if(y0)
                            {
                                y2="";
                            }

                            result = string.CompareOrdinal(x2,y2);
                            break;

                        case DataGridHelperColumn.ColumnTypeRef.Integer:
                        {
                            int xi = 0;
                            int yi = 0;

                            if(!x0)
                            {
                                xi=x2.ToInt();
                            }

                            if(!y0)
                            {
                                yi=y2.ToInt();
                            }

                            dbg=$"{dbg} x=[{xi}] y=[{yi}]";

                            result = xi.CompareTo(yi);
                        }
                        break;

                        case DataGridHelperColumn.ColumnTypeRef.Double:
                        {
                            double xd = 0;
                            double yd = 0;

                            if(!x0)
                            {
                                xd=x2.ToDouble();
                            }

                            if(!y0)
                            {
                                yd=y2.ToDouble();
                            }

                            dbg=$"{dbg} x=[{xd}] y=[{yd}]";

                            result = xd.CompareTo(yd);
                        }
                        break;

                        case DataGridHelperColumn.ColumnTypeRef.DateTime:
                        {
                            //string format="dd.MM.yyyy";
                            string format = "";
                            if(!string.IsNullOrEmpty(_column.FormatInput))
                            {
                                format=_column.FormatInput;
                            }

                            DateTime xd = DateTime.MinValue;
                            DateTime yd = DateTime.MinValue;

                            if(!x0)
                            {
                                xd = x2.ToDateTime(format);
                            }

                            if(!y0)
                            {
                                yd = y2.ToDateTime(format);
                            }

                            dbg=$"{dbg} x=[{xd}] y=[{yd}]  x0=[{x0}] y0=[{y0}] f=[{format}] cf=[{_column.Format}]";

                            result =  DateTime.Compare(xd,yd);
                        }
                        break;

                        case DataGridHelperColumn.ColumnTypeRef.Boolean:
                        {

                            bool xb = false;
                            bool yb = false;

                            if(!x0)
                            {
                                xb = x2.ToBool();
                            }

                            if(!y0)
                            {
                                yb = y2.ToBool();
                            }

                            dbg=$"{dbg} x=[{xb}] y=[{yb}]";

                            result = xb.CompareTo(yb);
                        }
                        break;

                    }
                }
            }
            else
            {
                dbg=$"{dbg} bx";
            }

            //Central.Dbg($"           {dbg}");

            return result;
        }

       
    }

    class DataGridHelperSorterString : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == "" || y == "")
            {
                return 0;
            }
          
            return string.CompareOrdinal(x,y);
          
        }
    }


    /// <summary>
    /// Перечисления для выбора стороны отрисовки лейблов
    /// </summary>
    public enum LabelPosition
    {
        Left,
        Right
    }

    public class DataGridHelperColumnLabel
    {
        public DataGridHelperColumnLabel()
        {
            Path = "";
        }

        public static FrameworkElementFactory MakeElement(string text = "", string background = "#FFFFFF99", string foreground = "#FF000000", int w = 16, int h = 16, double fontSize = 12, string toolTip = "")
        {
            var block = new FrameworkElementFactory(typeof(Border));
            {
                System.Windows.Media.Brush bg = background.ToBrush();
                var horizontalAlignment = System.Windows.HorizontalAlignment.Right;
                var cr = new CornerRadius(2);

                block.SetValue(Border.WidthProperty, (double)w);
                block.SetValue(Border.HeightProperty, (double)h);
                block.SetValue(Border.BackgroundProperty, bg);
                block.SetValue(Border.CornerRadiusProperty, cr);
                block.SetValue(Border.HorizontalAlignmentProperty, horizontalAlignment);

                if (!toolTip.IsNullOrEmpty())
                {
                    block.SetValue(Border.ToolTipProperty, toolTip);
                }
            }

            {
                var t = new FrameworkElementFactory(typeof(TextBlock));
                var horizontalAlignment = System.Windows.HorizontalAlignment.Center;
                var verticalAlignment = System.Windows.VerticalAlignment.Center;
                System.Windows.Media.Brush fg = foreground.ToBrush();

                t.SetValue(TextBlock.TextProperty, text);
                t.SetValue(TextBlock.HorizontalAlignmentProperty, horizontalAlignment);
                t.SetValue(TextBlock.VerticalAlignmentProperty, verticalAlignment);
                t.SetValue(TextBlock.ForegroundProperty, fg);
                t.SetValue(TextBlock.FontSizeProperty, fontSize);
                block.AppendChild(t);
            }

            return block;
        }

        public string Path { get; set; }
        public delegate FrameworkElementFactory DataGridHelperColumnLabelConstructDelegate();
        public DataGridHelperColumnLabelConstructDelegate Construct;
        public delegate object DataGridHelperColumnLabelUpdateDelegate(Dictionary<string, string> row);
        public DataGridHelperColumnLabelUpdateDelegate Update;
        
        public LabelPosition Position { get; set; } = LabelPosition.Right;
    }

    public class DataGridContextMenuItem
    {
        public DataGridContextMenuItem()
        {
            Header="";
            ToolTip = "";
            Enabled =true;
            Visible=true;
            Action=null;
            Items=new Dictionary<string, DataGridContextMenuItem>();
            Tag = "";
            GroupHeader = "";
            GroupHeaderName = "";
        }
        public delegate void OnContextMenuItemSelectedDelegate();
        public OnContextMenuItemSelectedDelegate Action;
        public string Header { get; set; }
        public string ToolTip { get; set; }
        public bool Enabled { get; set; }
        public bool Visible { get; set; }
        public string Tag { get; set; }
        public string GroupHeader { get; set; }
        public string GroupHeaderName { get; set; }
        public Dictionary<string, DataGridContextMenuItem> Items { get; set; }

        public static List<string> GetTagList(DataGridContextMenuItem dataGridContextMenuItem)
        {
            List<string> tagList = new List<string>();

            if (dataGridContextMenuItem != null && dataGridContextMenuItem.Tag != null)
            {
                string frameworkElementTag = dataGridContextMenuItem.Tag.ToString();
                if (!string.IsNullOrEmpty(frameworkElementTag))
                {
                    tagList = frameworkElementTag.Split(UIUtil.TagSeparator).ToList();
                }
            }

            return tagList;
        }
    }

    public class MovingObject
    {
        public MovingObject()
        {
            Data=new Dictionary<string, string>();
            SourceName="";
        }
        public Dictionary<string,string> Data {get;set;}
        public string SourceName {get;set;}
    }
}
