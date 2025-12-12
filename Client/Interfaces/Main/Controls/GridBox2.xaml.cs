using Client.Assets.Converters;
using Client.Common;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
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
    /// грид с данными 2
    /// </summary>
    public partial class GridBox2:UserControl
    {
        public GridBox2()
        {
            InitializeComponent();
            Loaded += GridBox2_Loaded;

            Initialized=false;
            Autosized=false;
            SearchText=new TextBox();
            UseRowHeader=false;
            Name="";
            ColumnWidthMode=GridBox.ColumnWidthModeRef.Compact;
            Columns=new List<DataGridHelperColumn>();
            Items=new List<Dictionary<string, string>>();
            GridItems=new List<Dictionary<string, string>>();
            SelectedItem=new Dictionary<string,string>();
            SelectedItemPrev=new Dictionary<string,string>();
            SelectedItemIndex=0;
            SelectedItemPrevIndex=0;
            Ds=new ListDataSet();
            Menu=new Dictionary<string, DataGridContextMenuItem>();
            ColumnWidth=new Dictionary<string, int>();
            ColumnResizerMouseX=0;
            ColumnResizerMouseY=0;
            ColumnResizerOldWidth=0;
            ColumnResizerInProgress=false;
            ColumnStack=new Dictionary<string, object>();
            OnSelectItem=OnSelectItemAction;
            OnViewItem=null;
            OnDblClick=OnSelectItemAction;
            RowStylers=new Dictionary<StylerTypeRef, StylerDelegate>();
            LineSelectElement=new StackPanel();
            PrimaryKey="ID";
            SortColumn=new DataGridHelperColumn();
            SortColumnPrev=new DataGridHelperColumn();
            SortDirection=ListSortDirection.Ascending;
            resizerWidth=5;            
            sorterWidth=10;
            sorterSymbolAsc="▲";
            sorterSymbolDesc="▼";
            ColumnTotalWidth=0;
            ColumnSymbolWidth=8;
            ColumnAutowidthLog="";
            AutoUpdateInterval=60;
            ItemsAutoUpdate=false;

            ScrollersUpdateTimer=new Timeout(
                1,
                ()=>{
                    ScrollersUpdateSize();        
                }
            );
            ScrollersUpdateTimer.SetIntervalMs(300);


            ColumnResizeTimer =new Timeout(
                1,
                ()=>{
                    ColumnResizerResetStyle();
                    CellUpdateWidthAll();
                }
            );
            ColumnResizeTimer.SetIntervalMs(300);


            AutoUpdateTimer=new Timeout(
                60,
                ()=>{
                    if(ItemsAutoUpdate)
                    {
                        LoadItems();
                    }                    
                },
                true,
                false
            );

            LineWidthAdd=25;
            LineWidth=0;
            VerticalScrollPosition=0;
            LineHeight=0;
            LineFirstVisible=0;
            LineToolTipMode=0;
            Runned=false;
            DoRun=false;
            ConverterFormat = new NumberFormatInfo { NumberDecimalSeparator = "," };
            ConverterCulture = new CultureInfo("ru-RU");
            Profiler=new Profiler();
        }               

        public bool Initialized {get;set;}
        public bool Autosized {get;set;}
        public TextBox SearchText { get; set; } 
        public bool UseRowHeader { get; set; }
        public string Name { get; set; }
        public GridBox.ColumnWidthModeRef ColumnWidthMode {get;set;}
        public List<DataGridHelperColumn> Columns { get; set; }
        public List<Dictionary<string,string>> Items { get; set; }
        public List<Dictionary<string,string>> GridItems { get; set; }
        public Dictionary<string,string> SelectedItem { get; set; }
        public Dictionary<string,string> SelectedItemPrev { get; set; }
        private int SelectedItemIndex { get; set; }
        private int SelectedItemPrevIndex { get; set; }
        public ListDataSet Ds {get;set;}
        public Dictionary<string,DataGridContextMenuItem> Menu { get;set;}
        public delegate void OnLoadItemsDelegate();
        public OnLoadItemsDelegate OnLoadItems;
        public virtual void OnLoadItemsAction(){}
        public delegate void OnFilterItemsDelegate();
        public OnFilterItemsDelegate OnFilterItems;
        public virtual void OnFilterItemsAction(){}
        public Dictionary<string,int> ColumnWidth {get;set;}
        private Dictionary<string,object> ColumnStack {get;set;}
        public delegate void OnSelectItemDelegate(Dictionary<string,string> selectedItem);
        public OnSelectItemDelegate OnSelectItem;
        public virtual void OnSelectItemAction(Dictionary<string,string> selectedItem){ }
        public delegate string OnViewItemDelegate(Dictionary<string,string> row, List<DataGridHelperColumn> columns);
        public OnViewItemDelegate OnViewItem;
        public virtual void OnViewItemAction(Dictionary<string,string> selectedItem){ }
        public delegate void OnDblClickDelegate(Dictionary<string,string> selectedItem);
        public OnDblClickDelegate OnDblClick;
        public virtual void OnDblClickAction(Dictionary<string,string> selectedItem){ }
        public Dictionary<StylerTypeRef,StylerDelegate> RowStylers;
        private StackPanel LineSelectElement {get;set;}
        public string PrimaryKey {get;set;}
        public DataGridHelperColumn SortColumn { get; set; }
        private DataGridHelperColumn SortColumnPrev { get; set; }
        public ListSortDirection SortDirection { get; set; }
        private int resizerWidth {get;set;}
        private int sorterWidth {get;set;}
        private string sorterSymbolAsc {get;set;}
        private string sorterSymbolDesc {get;set;}
        private int ColumnTotalWidth {get;set;}
        private int ColumnSymbolWidth {get;set;}
        private string ColumnAutowidthLog{get;set;}
        public int AutoUpdateInterval{get;set;}
        public bool ItemsAutoUpdate { get; set; }
        private Timeout ScrollersUpdateTimer {get;set;}
        private Timeout ColumnResizeTimer {get;set;}
        private Timeout AutoUpdateTimer {get;set;}
        private int LineWidthAdd {get;set;}
        private int LineWidth {get;set;}
        private int VerticalScrollPosition {get;set;}
        private int LineHeight {get;set;}
        private int LineFirstVisible {get;set;}
        /// <summary>
        /// 1=cell,2=common
        /// </summary>
        private int LineToolTipMode {get;set;}
        private bool Runned {get;set;}
        private bool DoRun {get;set;}
        private NumberFormatInfo ConverterFormat {get;set;}
        private CultureInfo ConverterCulture {get;set;}
        private Profiler Profiler  {get;set;}

        public void Init()
        {
            if(!Initialized)
            {
                GridHeaderContainer.Children.Clear();
                CellMenuConstruct();               
                CellHeaderConstruct();               
                ScrollersBindEvents();
                

                GridContainer.MouseMove += GridContainer_MouseMove;
                GridContainer.MouseUp += GridContainer_MouseUp;

                Central.Msg.Register(ProcessMessage);

                if(AutoUpdateInterval > 0)
                {
                    AutoUpdateTimer.SetInterval(AutoUpdateInterval);
                }

                Initialized=true;
            }

            Run();
        }

        public void ProcessMessage(ItemMessage message)
        {
            if(message!=null)
            {
                if(message.SenderName == "MainWindow")
                {
                    switch (message.Action)
                    {
                        case "Resized":
                            CellHeaderWidthProcess();
                            break;
                    }
                }
            }
        }

        private void GridBox2_Loaded(object sender, RoutedEventArgs e)
        {
        }

        public void CellUpdateWidth(string k, int width)
        {
            if(width > -1)
            {
                CellSetWidth(k,width);

                var lineWidth=0;
                {
                    
                    foreach(object line in GridHeaderContainer.Children)
                    {
                        var b0=(Border)line;
                        var s=(StackPanel)b0.Child;
                        foreach(object cell in s.Children)
                        {
                            var b=(Border)cell;
                            var n=b.Name;
                            var k2=n.CropAfter2("cell_header_");
                            if(k2==k)
                            {
                                b.Width=width;
                            }                    
                            lineWidth=(int)(lineWidth+b.Width);                            
                        }
                        lineWidth=lineWidth+LineWidthAdd;
                        s.Width=lineWidth;
                    }
                    
                    GridHeaderContainer.Width=lineWidth;
                }

                {
                    int j=0;
                    foreach(object line in GridBodyContainer.Children)
                    {
                        j++;
                        var b0=(Border)line;
                        var s=(StackPanel)b0.Child;
                        foreach(object cell in s.Children)
                        {
                            var process=false;

                            if(LineFirstVisible > 0)
                            {
                                if (j >= LineFirstVisible && j <= (LineFirstVisible+100) )
                                {
                                    process=true;
                                }
                            }
                            else
                            {
                                if( j < 100)
                                {
                                    process=true;
                                }
                            }

                            if(process)
                            {
                                var b=(Border)cell;
                                var n=b.Name;
                                var k2=n.CropAfter2("cell_body_");
                                if(k2==k)
                                {
                                    b.Width=width;
                                }                 
                                //lineWidth=(int)(lineWidth+b.Width);
                            }
                        }
                        s.Width=lineWidth;

                        
                    }
                    GridBodyContainer.Width=lineWidth;
                }

                ScrollersUpdate();
            }
        }
       
        public void CellUpdateWidthAll()
        {
            DebugLog("width_2");
            var lineWidth=0;
            {
                foreach(object line in GridHeaderContainer.Children)
                {
                    var b0=(Border)line;
                    var s=(StackPanel)b0.Child;                    
                    foreach(object cell in s.Children)
                    {
                        var b=(Border)cell;
                        var n=b.Name;
                        var k=n.CropAfter2("cell_header_");

                        if(ColumnWidth.ContainsKey(k))
                        {
                            var width=ColumnWidth[k].ToInt();
                            if(width>0)
                            {
                                b.Width=width;
                                lineWidth=(int)(lineWidth+b.Width);                            
                            }                            
                        }
                    }
                    lineWidth=lineWidth+LineWidthAdd;
                    s.Width=lineWidth;
                }
                GridHeaderContainer.Width=lineWidth;
            }

            {
                foreach(object line in GridBodyContainer.Children)
                {
                    var b0=(Border)line;
                    var s=(StackPanel)b0.Child;
                    foreach(object cell in s.Children)
                    {
                        if(cell.GetType() == typeof(Border))
                        {
                            var b=(Border)cell;
                            var n=b.Name;
                            var k=n.CropAfter2("cell_body_");

                            if(ColumnWidth.ContainsKey(k))
                            {
                                var width=ColumnWidth[k].ToInt();
                                if(width>0)
                                {
                                    b.Width=width;
                                    //lineWidth=(int)(lineWidth+b.Width);                            
                                }
                            }
                        }
                    }
                    s.Width=lineWidth;

                    if(LineHeight == 0)
                    {
                        LineHeight=(int)s.ActualHeight;
                    }
                }
                GridBodyContainer.Width=lineWidth;
            }    
               
            LineWidth=lineWidth;
            ScrollersUpdate();
        }

        private void RowStylersProcess()
        {
            if(Initialized)
            {
                if(Ds.Items.Count > 0)
                {
                    try
                    {
                        int j=0;
                        foreach(object line in GridBodyContainer.Children)
                        {
                            j++;
                            var b0=(Border)line;
                            var s=(StackPanel)b0.Child;
                            foreach(object cell in s.Children)
                            {
                                var b=(Border)cell;
                                var p=GetCellInfo(b);
                                var c=ColumnGet(p.CheckGet("PATH"));
                                var row=new Dictionary<string,string>();

                                var index=j-1;
                                if(index >= 0)
                                {
                                    row=Ds.Items.ElementAt(index);
                                }

                                var render=false;
                                if(c.Visible && !c.Hidden)
                                {
                                    render=true;
                                }

                                if(render)
                                {
                                    RowStylersProcessOne(b,p,c,j,row);
                                }
                            }

                            if(j > 1000)
                            {
                                break;
                            }
                        }
                    }
                    catch(Exception e)
                    {
                    }
                }
            }
        }

        private void RowStylersProcessOne(Border b, Dictionary<string,string> p, DataGridHelperColumn c, int index, Dictionary<string,string> row)
        {
            try
            {
                if(RowStylers.Count > 0)
                {
                    foreach(KeyValuePair<StylerTypeRef,StylerDelegate> item in RowStylers)
                    {
                        var type=item.Key;
                        var d=item.Value;
                        if(d != null)
                        {
                            var result=d.Invoke(row);
                            RowStylersProcessOneStyler(type,b,result,c);
                        }
                    }

                    foreach(KeyValuePair<StylerTypeRef,StylerDelegate> item in c.Stylers)
                    {
                        var type=item.Key;
                        var d=item.Value;
                        if(d != null)
                        {
                            var result=d.Invoke(row);
                            RowStylersProcessOneStyler(type,b,result,c);
                        }
                    }
                }
            }
            catch(Exception e)
            {
            }
        }

        private void RowStylersProcessOneStyler(StylerTypeRef type, Border b, object r, DataGridHelperColumn c)
        {
            try
            {
                if(r != DependencyProperty.UnsetValue)
                {
                    switch(type)
                    {
                        case StylerTypeRef.BackgroundColor:
                        {
                            b.Background=(Brush)r;
                        }
                            break;

                        case StylerTypeRef.BorderColor:
                        {
                            b.BorderBrush=(Brush)r;
                        }
                            break;

                        case StylerTypeRef.ForegroundColor:
                        {
                            if(
                                c.ColumnType == ColumnTypeRef.DateTime
                                || c.ColumnType == ColumnTypeRef.Integer
                                || c.ColumnType == ColumnTypeRef.Double
                                || c.ColumnType == ColumnTypeRef.String
                            )
                            {
                                var t=(TextBlock)b.Child;
                                t.Foreground=(Brush)r;
                            }                            
                        }
                            break;

                        case StylerTypeRef.FontWeight:
                        {
                            if(
                                c.ColumnType == ColumnTypeRef.DateTime
                                || c.ColumnType == ColumnTypeRef.Integer
                                || c.ColumnType == ColumnTypeRef.Double
                                || c.ColumnType == ColumnTypeRef.String
                            )
                            {
                                var t=(TextBlock)b.Child;
                                t.FontWeight=(FontWeight)r;
                            }                            
                        }
                            break;
                    }
                }
            }
            catch(Exception e)
            {
            }
        }

        private DataGridHelperColumn ColumnGet(string path)
        {
            var result=new DataGridHelperColumn();

            foreach(DataGridHelperColumn c in Columns)
            {
                if(c.Path == path)
                {
                    result=c;
                }
            }

            return result;
        }

        private Border CellHeaderGet(string path)
        {
            var result=new Border();

            foreach(object line in GridHeaderContainer.Children)
            {
                var b0=(Border)line;
                var s=(StackPanel)b0.Child;
                foreach(object cell in s.Children)
                {
                    var b=(Border)cell;
                    var p=GetCellInfo(b);
                    if(p.CheckGet("PATH") == path)
                    {
                        result=b;
                        break;
                    }                    
                }
            }

            return result;
        }

        private Border CellGet(int index, string path)
        {
            var result=new Border();

            int j=0;
            foreach(object line in GridBodyContainer.Children)
            {
                j++;
                if(j == index)
                {
                    var b0=(Border)line;
                    var s=(StackPanel)b0.Child;
                    foreach(object cell in s.Children)
                    {
                        var b=(Border)cell;
                        var p=GetCellInfo(b);
                        if(p.CheckGet("PATH") == path)
                        {
                            result=b;
                            break;
                        }                    
                    }
                }
            }

            return result;
        }

        private Border LineGet(int index)
        {
            var result=new Border();

            int j=0;
            foreach(object line in GridBodyContainer.Children)
            {
                j++;
                if(j == index)
                {
                    result=(Border)line;
                    break;
                }
            }

            return result;
        }

        public void CellSetWidth(string k, int width)
        {
            if(!ColumnWidth.ContainsKey(k))
            {
                ColumnWidth.Add(k,width);
            }
            ColumnWidth[k]=width;
        }

        public int CellGetWidth(string k)
        {
            int result=0;
            if(ColumnWidth.ContainsKey(k))
            {
                if(ColumnWidth[k] > 0)
                {
                    result=ColumnWidth[k];
                }                
            }
            return result;
        }

        public void CellMenuConstruct()
        {
            if(Central.DebugMode)
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
                                            DebugShowGridInfo();
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
                                            ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
                                            CellHeaderWidthProcess();
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
                                            ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                                            CellHeaderWidthProcess();
                                        }
                                    }
                                },
                                // {
                                //    "UpdateWidth",
                                //    new DataGridContextMenuItem()
                                //    {
                                //        Header ="UpdateWidth",
                                //        Action=() =>
                                //        {
                                //            CellHeaderWidthProcess();
                                //        }
                                //    }
                                //},
                                {
                                    "ShowColumnsConfig",
                                    new DataGridContextMenuItem()
                                    {
                                        Header ="Лог",
                                        Action=() =>
                                        {
                                            DebugShowColumnsInfo();
                                        }
                                    }
                                },
                               
                            }
                        }
                    );
                }
            }
        }

        public void CellMenuShow()
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

        public void CellHeaderConstruct()
        {
            if(Columns.Count > 0)
            {
                var line=new StackPanel();
                line.Orientation=Orientation.Horizontal;   
                line.HorizontalAlignment=HorizontalAlignment.Left;
                
                foreach(DataGridHelperColumn c in Columns)
                {
                    var render=false;
                    var k=c.Path;

                    if(c.Visible && !c.Hidden)
                    {
                        render=true;
                    }

                    if(render)
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

                CellHeaderWidthProcess(true);

                foreach(DataGridHelperColumn c in Columns)
                {
                    var render=false;
                    var k=c.Path;

                    if(c.Visible && !c.Hidden)
                    {
                        render=true;
                    }

                    if(render)
                    {
                        //c.Width=(int)(c.Width2*ColumnSymbolWidth);
                        CellSetWidth(k,c.Width);

                        var b=CellHeaderCreate(c);
                        line.Children.Add(b);
                    }
                }


                var lineBorder=new Border();
                lineBorder.Child=line;
                //lineBorder.Style=(Style)GridContainer.TryFindResource("GridboxLineBorder");                    
                GridHeaderContainer.Children.Add(lineBorder); 
            }
        }

        public void CellHeaderWidthProcess(bool noUpdate=false)
        {
            DebugLog("width_1");
            {
                ColumnAutowidthLog="";
                var gridContainerWidth=(int)GridContainer.ActualWidth;

                if(!Initialized || gridContainerWidth == 0)
                {
                    gridContainerWidth=1000;
                }

                ColumnTotalWidth=0;

                foreach(DataGridHelperColumn c in Columns)
                {
                    var render=false;
                    var k=c.Path;

                    if(c.Visible && !c.Hidden)
                    {
                        render=true;
                    }

                    if(render)
                    {
                        ColumnTotalWidth=ColumnTotalWidth+c.Width2;
                    }
                }
                
                {
                    ColumnAutowidthLog = ColumnAutowidthLog.Append($"грид: {Name}",true);
                    ColumnAutowidthLog = ColumnAutowidthLog.Append($"режим: {ColumnWidthMode}",true);
                    ColumnAutowidthLog = ColumnAutowidthLog.Append($"ширина символа: {ColumnSymbolWidth}",true);
                    ColumnAutowidthLog = ColumnAutowidthLog.Append($"суммарная ширина: {ColumnTotalWidth}",true);
                    ColumnAutowidthLog = ColumnAutowidthLog.Append($"контейнер: w={gridContainerWidth} scroll={VerticalScrollPosition}",true);
                    ColumnAutowidthLog = ColumnAutowidthLog.Append($"строка: w={LineWidth} h={LineHeight}",true);
                }

                var currentMode=ColumnWidthMode;

                if(currentMode == GridBox.ColumnWidthModeRef.Compact)
                {
                    foreach(DataGridHelperColumn c in Columns)
                    {
                        var render=false;
                        var k=c.Path;

                        if(c.Visible && !c.Hidden)
                        {
                            render=true;
                        }

                        if(render)
                        {
                            c.WidthRelative = (int) (((double) (c.Width2) / (double) (ColumnTotalWidth)) * 1000);
                        }
                    }
                }

                foreach(DataGridHelperColumn c in Columns)
                {
                    var render=false;
                    var k=c.Path;

                    if(c.Visible && !c.Hidden)
                    {
                        render=true;
                    }

                    if(render)
                    {
                        var w=0;
                        switch(currentMode)
                        {
                            case GridBox.ColumnWidthModeRef.Compact:
                                var w2=(int)Math.Round(((double)(gridContainerWidth)/(double)1000*(double)c.WidthRelative),0);
                                w=(int)w2-2;
                                break;

                            case GridBox.ColumnWidthModeRef.Full:
                                w=(int)Math.Round((double)(c.Width2*ColumnSymbolWidth),0);
                                break;
                        }

                        c.Width=w;

                        if(noUpdate == false)
                        {
                            CellSetWidth(k,w);
                            //CellUpdateWidth(k,w);
                        }                        
                    }
                }
            }
            
            if(noUpdate == false)
            {
                CellUpdateWidthAll();
            }
        }

        private string CellBodyContentCurrent {get;set;}
        public void CellBodyConstruct()
        {
            if(Columns.Count > 0)
            {                
                {
                    var line=new StackPanel();
                    line.Orientation=Orientation.Horizontal;
                    line.HorizontalAlignment=HorizontalAlignment.Left;
                    line.Style=(Style)GridContainer.TryFindResource("GridboxLineStackPanel");

                    foreach(DataGridHelperColumn c in Columns)
                    {
                        var render=false;
                    
                        if(c.Visible && !c.Hidden)
                        {
                            render=true;
                        }

                        if(render)
                        {
                            var b=CellBodyCreate(c);

                            {
                                if(LineToolTipMode == 1)
                                {
                                    if(!CellBodyContentCurrent.IsNullOrEmpty())
                                    {
                                        b.ToolTip=CellBodyContentCurrent;
                                    }                                    
                                }
                            }

                            line.Children.Add(b);
                        }
                    }

                    if(LineToolTipMode == 2)
                    {
                        var t=OnViewItem.Invoke(RenderingRow,Columns);
                        line.ToolTip=t.ToString();
                    }

                    var lineBorder=new Border();
                    lineBorder.Child=line;
                    lineBorder.Style=(Style)GridContainer.TryFindResource("GridboxLineBorder");                    
                    GridBodyContainer.Children.Add(lineBorder); 
                }

                /*
                {
                    var line=new StackPanel();
                    line.Orientation=Orientation.Horizontal;
                    line.Style=(Style)GridContainer.TryFindResource("GridboxLineStackPanel");

                    GridBodyContainer.Children.Add(line); 
                    LineSelectElement.Margin=new Thickness(10,10,0,0);
                }
                */
                
            }
        }
      
        public Border CellHeaderCreate(DataGridHelperColumn c)
        {
            var k=c.Path;

            var b= new Border();
            b.Name=$"cell_header_{k}";
            b.Tag=$"index={0}|path={k}";

            b.Style=(Style)GridContainer.TryFindResource("GridboxCellHeaderBorder");
            b.Width=CellGetWidth(k);

            var g=new Grid();
            g.HorizontalAlignment=HorizontalAlignment.Stretch;
            {
                var cd=new ColumnDefinition();
                cd.Width=new GridLength(1, GridUnitType.Star);   
                g.ColumnDefinitions.Add(cd);
            }
            {
                var cd=new ColumnDefinition();
                cd.Width=new GridLength(sorterWidth, GridUnitType.Pixel);   
                g.ColumnDefinitions.Add(cd);
            }
            {
                var cd=new ColumnDefinition();
                cd.Width=new GridLength(resizerWidth, GridUnitType.Pixel);   
                g.ColumnDefinitions.Add(cd);
            }
            {
                var rd=new RowDefinition();
                rd.Height=new GridLength(1, GridUnitType.Star);   
                g.RowDefinitions.Add(rd);
            }

            //text
            {
                var r=new Border();                
                r.Name=$"caption_{k}";

                var ts=new TextBlock();
                ts.Text=c.Header;
                //var w=ColumnWidth[k];
                //ts.ToolTip=$"{w}";
                ts.Style=(Style)GridContainer.TryFindResource("GridboxCellHeaderText");

                r.Child=ts;
                
                g.Children.Add(r);
                Grid.SetRow(r, 0);
                Grid.SetColumn(r, 0);
            }

            //sorter
            {
                var r=new Border();                
                r.Width=sorterWidth;
                r.Name=$"sorter_{k}";
                r.Style=(Style)GridContainer.TryFindResource("GridboxCellHeaderSorterBorder");

                var ts=new TextBlock();
                ts.Text="";               
                
                r.Child=ts;

                g.Children.Add(r);
                Grid.SetRow(r, 0);
                Grid.SetColumn(r, 1);
            }

            //resizer
            {
                var r=new Border();
                r.Width=resizerWidth;
                r.Name=$"resizer_{k}";
                r.Style=(Style)GridContainer.TryFindResource("GridboxCellHeaderResizerBorder");

                r.MouseMove += R_MouseMove;
                r.MouseEnter += R_MouseEnter;
                r.MouseLeave += R_MouseLeave;

                g.Children.Add(r);
                Grid.SetRow(r, 0);
                Grid.SetColumn(r, 2);
            }

            b.Child=g;
            
            if(!ColumnStack.ContainsKey(k))
            {
                ColumnStack.Add(k,b);
            }

            {
                b.MouseDown += OnMouseDown;
                b.MouseUp += OnMouseUp;
            }

            return b;
        }

        private bool MouseButtonPressed {get;set;}
        private int MouseButtonType {get;set;}
        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if(!MouseButtonPressed)
            {
                MouseButtonPressed=true;

                if(e.LeftButton == MouseButtonState.Pressed)
                {
                    MouseButtonType=1;
                }else if(e.RightButton == MouseButtonState.Pressed)
                {
                    MouseButtonType=2;
                }
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if(MouseButtonPressed)
            {
                MouseButtonPressed=false;
                if(!ColumnResizerInProgress)
                {
                    CellProcessClick(sender,e);
                }   
                MouseButtonType=0;
            }
        }

        private void R_MouseLeave(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = null;
        }

        private void R_MouseEnter(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.SizeWE;
        }

        private void R_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if(!ColumnResizerInProgress)
                {
                    ColumnResizerInProgress=true;
                    ColumnResizerKey=ObjectGetKey(sender,"resizer_"); 
                    ColumnResizerSetStyle(ColumnResizerKey, "GridboxCellHeaderResizerSelectedBorder");                    
                }
            }
            else
            {
                if(ColumnResizerInProgress)
                {
                    MouseButtonPressed=false;
                    ColumnResizerSetStyle(ColumnResizerKey, "GridboxCellHeaderResizerBorder");
                    ColumnResizerStopResize();                    
                }
            }                
        }

        private void ColumnResizerStopResize()
        {
            if(ColumnResizerInProgress)
            {
                DebugLog("resize_stop");
                ColumnResizerInProgress=false;
                ColumnResizerMouseX=0;
                ColumnResizerMouseY=0;
                ColumnResizerOldWidth=0;
                ColumnResizerKey="";
                ColumnResizerMouseYStart=0;
            
                ColumnResizeTimer.Restart();
            }
        }
        
        private void ColumnSetSortingLabel()
        {
            if(Initialized)
            {
                ColumnSetSortingLabelInner(SortColumnPrev,0);
                ColumnSetSortingLabelInner(SortColumn,1);
            }
        }

        private void ColumnSetSortingLabelInner(DataGridHelperColumn column, int mode=0)
        {
            if(column !=null)
            {
                var c=CellHeaderGet(column.Path);
                if(c != null)
                {
                    switch(mode)
                    {
                        case 1:
                            break;

                        case 0:
                            break;
                    }

                    var g=(Grid)c.Child;
                    if(g!=null)
                    {
                        foreach(object o in g.Children)
                        {
                            var b=(Border)o;
                            var n=b.Name;
                            if(n.IndexOf("sorter_") > -1)
                            {
                                var t=(TextBlock)b.Child; 
                                if(t!=null)
                                {
                                    switch(mode)
                                    {
                                        case 1:
                                        {
                                            switch(SortDirection)
                                            {
                                                case ListSortDirection.Ascending:
                                                    t.Text=sorterSymbolAsc;
                                                    break;

                                                case ListSortDirection.Descending:
                                                    t.Text=sorterSymbolDesc;
                                                    break;
                                            }
                                        }
                                            break;

                                        default:
                                        case 0:
                                        {
                                            t.Text="";
                                        }
                                            break;
                                    }
                                
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ColumnResizerSetStyle(string path, string style)
        {
            {
                var c=CellHeaderGet(path);
                if(c != null)
                {
                    var g=(Grid)c.Child;
                    if(g!=null)
                    {
                        foreach(object o in g.Children)
                        {
                            var b=(Border)o;
                            var n=b.Name;
                            if(n.IndexOf("resizer_") > -1)
                            {
                                if(!style.IsNullOrEmpty())
                                {
                                    b.Style=(Style)GridContainer.TryFindResource(style);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ColumnResizerResetStyle()
        {
            foreach(object line in GridHeaderContainer.Children)
            {
                var b0=(Border)line;
                var s=(StackPanel)b0.Child;
                foreach(object cell in s.Children)
                {
                    var b=(Border)cell;
                    var n=b.Name;
                    var k2=n.CropAfter2("cell_header_");                   
                    ColumnResizerSetStyle(k2, "GridboxCellHeaderResizerBorder");
                }
            }
        }

        private int ColumnResizerMouseX {get;set;}
        private int ColumnResizerMouseY {get;set;}
        private int ColumnResizerMouseYStart {get;set;}
        private int ColumnResizerOldWidth {get;set;}
        private string ColumnResizerKey {get;set;}
        private bool ColumnResizerInProgress {get;set;}

        private string ObjectGetKey(object sender, string key="resizer_")
        {
            var result="";

            if(sender!=null)
            {
                var b=(Border)sender;                        
                var n=b.Name;
                if(!n.IsNullOrEmpty())
                {
                    result=n.CropAfter2(key);
                }
            }

            return result;
        }

        private void GridContainer_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ColumnResizerStopResize();
        }

        private void GridContainer_MouseMove(object sender, MouseEventArgs e)
        {
            if(ColumnResizerInProgress)
            {
                var p=Mouse.GetPosition(this);
                var x=(int)p.X;
                var dx=0;
                var y=(int)p.Y;
                var dy=0;
                var k="";
                var w2=0;

                if(ColumnResizerMouseYStart == 0)
                {
                    ColumnResizerMouseYStart=y;
                }


                if(ColumnResizerMouseX !=0)
                {
                    dx=ColumnResizerMouseX-x;
                }
                if(ColumnResizerMouseY !=0)
                {
                    dy=ColumnResizerMouseY-y;
                }

                ColumnResizerMouseX=x;
                ColumnResizerMouseY=y;
           
                if(dx != 0)
                {
                    if(!ColumnResizerKey.IsNullOrEmpty())
                    {
                        {
                            var w=CellGetWidth(ColumnResizerKey);
                            w2=w+(-1)*dx;                            
                            CellUpdateWidth(ColumnResizerKey,w2);
                        }
                    }
                }

                //SetCursorPos(x,ColumnResizerMouseYStart);

                DebugLog($"M {ColumnResizerInProgress} {x}:{y} {ColumnResizerMouseYStart} {dx} {dy} {k} {w2}");
            }
        }

        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        private Style CellBodyGetDefaultStyle(DataGridHelperColumn c)
        {
            var result= (Style)GridContainer.TryFindResource("GridboxCellBodyBorder");
            switch(c.ColumnType)
            {
               

                case ColumnTypeRef.Integer:
                case ColumnTypeRef.Double:
                    result=(Style)GridContainer.TryFindResource("GridboxCellBodyDigitBorder");
                    break;

                default:
                    result=(Style)GridContainer.TryFindResource("GridboxCellBodyBorder");
                    break;
            }
            return result;
        }

        private Style CellBodyGetSelectedStyle(DataGridHelperColumn c)
        {
            var result= (Style)GridContainer.TryFindResource("GridboxCellBodyBorder");
            switch(c.ColumnType)
            {
                case ColumnTypeRef.Integer:
                case ColumnTypeRef.Double:
                    result=(Style)GridContainer.TryFindResource("GridboxCellBodyDigitSelectedBorder");
                    break;

                default:
                    result=(Style)GridContainer.TryFindResource("GridboxCellBodySelectedBorder");
                    break;
            }
            return result;
        }

        public Border CellBodyCreate(DataGridHelperColumn c)
        {
            CellBodyContentCurrent="";
            var k=c.Path;

            var b= new Border();
            {
                b.Name=$"cell_body_{k}";
                b.Tag=$"index={RenderingRowIndex}|path={k}";
                b.Style=CellBodyGetDefaultStyle(c);                
                b.Width=CellGetWidth(k);
            }

            {
                switch(c.ColumnType)
                {
                    case ColumnTypeRef.Boolean:
                    {
                        var v=CellBodyGetContent(c).ToBool();    
                        var ctl=new CheckBox();
                        ctl.IsChecked=v;
                        ctl.IsEnabled=false;
                        ctl.Style=(Style)GridContainer.TryFindResource("GridboxCellBodyCheckBox");            
                        b.Child=ctl;
                    }
                        break;

                    default:
                    {
                        var v=CellBodyGetContent(c);
                        CellBodyContentCurrent=v;
                        var ctl=new TextBlock();
                        ctl.Text=v;
                        ctl.Style=(Style)GridContainer.TryFindResource("GridboxCellBodyText");            
                        b.Child=ctl;

                    }
                        break;
                }
            }

            {
                //b.MouseUp += CellProcessClick;
                b.MouseDown += OnMouseDown;
                b.MouseUp += OnMouseUp;
            }

            return b;
        }

        private DataGridHelperColumn ColumnGetByPath(string path)
        {
            var  result=new DataGridHelperColumn();

            if(Columns.Count > 0)
            {
                foreach(DataGridHelperColumn c in Columns)
                {
                    var k=c.Path;
                    if(k == path)
                    {
                        result=c;
                        break;
                    }
                }
            }

            return result;
        }

        private Dictionary<string,string> RowItemGetByIndex(int index)
        {
            var result=new Dictionary<string,string>();

            if(index > 0)
            {
                if(GridItems.Count > 0)
                {
                    result=GridItems.ElementAt(index);
                }
            }

            return result;
        }

        

        private void RowSelect(int index, Dictionary<string,string> row)
        {
            SelectedItemPrevIndex=SelectedItemIndex;
            SelectedItemIndex=index;

            SelectedItemPrev=SelectedItem;    
            SelectedItem=row;

            if(OnSelectItem != null)
            {
                OnSelectItem.Invoke(row);
            }
            SetRowSelection(row);
        }

        private void CellProcessClick(object sender, MouseButtonEventArgs e)
        {
            var p=GetCellInfo(sender);
            var k=p.CheckGet("PATH");
            DebugLog($"CLICK {p.GetDumpString()}");
            var index=p.CheckGet("INDEX").ToInt();
            var type=p.CheckGet("TYPE");

            if(!k.IsNullOrEmpty())
            {   
                var c=ColumnGetByPath(k);
                if(c!=null)
                {
                    switch(MouseButtonType)
                    {
                        case 1:
                        {
                            switch(type)
                            {
                                case "HEADER":
                                {
                                    var direction=ListSortDirection.Ascending;
                                    if(SortDirection == direction)
                                    {
                                        direction=ListSortDirection.Descending;
                                    }
                                    SetSorting(k,direction);
                                    UpdateItems();
                                }
                                    break;

                                case "BODY":
                                {
                                    var row=RowItemGetByIndex(index-1);
                                    if(row.Count > 0)
                                    {
                                        RowSelect(index,row);
                                    }

                                    if(c.OnClickAction!=null)
                                    {
                                        var el=new FrameworkElement();
                                        c.OnClickAction.Invoke(row,el);
                                    }
                                }
                                    break;
                            }
                        }
                            break;

                        case 2:
                        {
                            CellMenuShow();
                        }
                            break;
                    }


                    
                }
            }
        }

        public void SelectRowFirst()
        {
            if(GridItems.Count > 0)
            {
                var first=GridItems.First();
                if(first!=null)
                {
                    RowSelect(1,first);
                }
            }
        }

        public void SelectRowByKey(int id,string k = "ID",bool scrollTo=true)
        {
            var ids=id.ToString();
            SelectRowByKey(ids,k,scrollTo);
        }

        public void SelectRowByKey(string id,string k = "ID",bool scrollTo=true)
        {
            if(GridItems.Count > 0)
            {
                int j=0;
                foreach(Dictionary<string,string> row in GridItems)
                {
                    j++;
                    if(row.CheckGet(k) == id)
                    {   
                        RowSelect(j,row);
                        break;
                    }
                }
            }
        }

        private void SetRowSelection(Dictionary<string,string> row)
        {
            {
                var line=LineGet(SelectedItemPrevIndex);
                if(line != null)
                {
                    line.Style=(Style)GridContainer.TryFindResource("GridboxLineBorder");                    
                    //line.Margin=new Thickness(0,0,0,0);
                    
                    {
                        var h=line.ActualHeight;
                        if(h>0)
                        {
                            line.Height=h;
                        }
                    }
                    
                    if(line.Child  != null)
                    {
                        var lineStack=(StackPanel)line.Child;
                        lineStack.Style=(Style)GridContainer.TryFindResource("GridboxLineStackPanel");                    

                        foreach(Border b in lineStack.Children)
                        {
                            var p=GetCellInfo(b);
                            var c=ColumnGet(p.CheckGet("PATH"));
                            b.Style=CellBodyGetDefaultStyle(c);
                        }
                    }                    
                }
            }

            {
                var line=LineGet(SelectedItemIndex);
                if(line != null)
                {
                    line.Style=(Style)GridContainer.TryFindResource("GridboxLineSelectedBorder");                    
                    //line.Margin=new Thickness(0,-2,0,-2);
                    
                    {
                        var h=line.ActualHeight;
                        if(h>0)
                        {
                            line.Height=h;
                        }
                    }

                    if(line.Child  != null)
                    {
                        var lineStack=(StackPanel)line.Child;
                        lineStack.Style=(Style)GridContainer.TryFindResource("GridboxLineSelectedStackPanel");                    

                        foreach(Border b in lineStack.Children)
                        {
                            var p=GetCellInfo(b);
                            var c=ColumnGet(p.CheckGet("PATH"));
                            b.Style=CellBodyGetSelectedStyle(c);
                        }
                    }
                }
            }
        }

        public Dictionary<string,string> GetCellInfo(object sender)
        {
            /*
                NAME
                TAG
                INDEX
                PATH
                TYPE="BODY"|"HEADER"
             */

            var result=new Dictionary<string,string>();

            if(sender!=null)
            {
                var b=(Border)sender;                        
                var n=b.Name;
                result.CheckAdd("TYPE","BODY");
                if(n.IndexOf("cell_header_") > -1)
                {
                    result.CheckAdd("TYPE","HEADER");
                }

                var t=b.Tag.ToString();
                if(!n.IsNullOrEmpty())
                {
                    result.CheckAdd("NAME",n);
                }
                if(!t.IsNullOrEmpty())
                {
                    result.CheckAdd("TAG",t);
                    var list=t.Split('|').ToList();
                    foreach(string block in list)
                    {
                        var token=block.Split('=').ToList();
                        var k="";
                        var v="";
                        if(token[0]!=null){
                            if(!token[0].ToString().IsNullOrEmpty())
                            {
                                k=token[0].ToString();
                            }
                        }
                        if(token[1]!=null){
                            if(!token[1].ToString().IsNullOrEmpty())
                            {
                                v=token[1].ToString();
                            }
                        }
                        result.CheckAdd(k.ToUpper(),v);
                    }
                }
            }

            return result;
        }

        public string CellBodyGetContent(DataGridHelperColumn c)
        {
            string result="";

            if(RenderingRow != null)
            {
                var k=c.Path;
                result=RenderingRow.CheckGet(k);

                if(!result.IsNullOrEmpty())
                {
                    switch(c.ColumnType)
                    {
                        case ColumnTypeRef.Integer:
                        {
                            result=result.ToInt().ToString();
                        }
                            break;

                        case ColumnTypeRef.Double:
                        {
                            var f1="N2";
                            if(!c.Format.IsNullOrEmpty())
                            {
                                f1=c.Format;
                            }
                            var f2="{0:"+f1+"}";

                            double r = 0;
                            var parseResult = double.TryParse(result,NumberStyles.Number,ConverterFormat,out r);
                            if(parseResult)
                            {
                                result = string.Format(ConverterCulture,f2,r);
                            }

                            result = result.Replace(",","");
                            result = result.Replace(".",",");
                        }
                            break;

                        case ColumnTypeRef.DateTime:
                        {
                            var f1="dd.MM.yyyy HH:mm:ss";
                            if(!c.Format.IsNullOrEmpty())
                            {
                                f1=c.Format;
                            }

                            DateTime r=result.ToDateTime();
                            result=r.ToString(f1);
                        }
                            break;
                    }

                    if(!c.Format.IsNullOrEmpty())
                    {
                        
                    }
                }
            }
            return result;
        }

        public void ScrollersBindEvents()
        {
            GridScrollVerticalScroller.ScrollChanged += GridScrollVerticalScroller_ScrollChanged;
            GridScrollHorizontalScroller.ScrollChanged += GridScrollHorizontalScroller_ScrollChanged;

            GridBodyScroller.ScrollChanged += GridBodyScroller_ScrollChanged;
        }

        private void GridBodyScroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if(e != null)
            {
                var x = e.VerticalOffset;
                UpdateScrollPosition(x);
                if(GridScrollVerticalScroller!=null)
                {
                    GridScrollVerticalScroller.ScrollToVerticalOffset(x);
                }
            }
        }

        private void GridScrollHorizontalScroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if(e != null)
            {
                var x = e.HorizontalOffset;
                if(GridBodyScroller!=null)
                {
                    GridBodyScroller.ScrollToHorizontalOffset(x);
                    GridHeaderScroller.ScrollToHorizontalOffset(x);
                }
            }
        }

        private void GridScrollVerticalScroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if(e != null)
            {
                var x = e.VerticalOffset;
                UpdateScrollPosition(x);
                if(GridBodyScroller!=null)
                {
                    GridBodyScroller.ScrollToVerticalOffset(x);
                }
            }
        }

        private void UpdateScrollPosition(double x)
        {
            VerticalScrollPosition=(int)x;
            if(VerticalScrollPosition > 0 && LineHeight > 0)
            {
                LineFirstVisible=(int)((double)VerticalScrollPosition/(double)LineHeight);
            }
        }

        public void ScrollersUpdate()
        {
            //ScrollersUpdateSize();                    
            ScrollersUpdateTimer.Restart();
        }

        public void ScrollersUpdateSize()
        {
            DebugLog("width_3");
            var h=(int)GridBodyContainer.ActualHeight;
            var w=(int)GridHeaderContainer.ActualWidth;
            w=w+100;
            var w2=ColumnTotalWidth;

            GridScrollVerticalContent.Height=h;
            GridScrollHorizontalContent.Width=w;

            var useHorizontalScroll=false;
            var currentMode=ColumnWidthMode;
            var gridContainerWidth=(int)GridContainer.ActualWidth;

            if(currentMode == GridBox.ColumnWidthModeRef.Compact)
            {
	            useHorizontalScroll=false;
            }

            if(currentMode == GridBox.ColumnWidthModeRef.Full)
            {
                useHorizontalScroll=true;
            }

            if(!useHorizontalScroll)
            {
                if( LineWidth > (gridContainerWidth+50) )
                {
                    useHorizontalScroll=true;
                }
            }

            if(useHorizontalScroll)
            {
                GridScrollHorizontalScroller.HorizontalScrollBarVisibility=ScrollBarVisibility.Visible;
            }
            else
            {
                GridScrollHorizontalScroller.HorizontalScrollBarVisibility=ScrollBarVisibility.Hidden;
            }

            ColumnSetSortingLabel();
            //AfterInit();

            HideSplash();
            DebugLog("width_9");
        }

        public void AfterInit()
        {
            if(DoRun)
            {
                if(!Runned)
                {
                    Runned=true;

                    if(AutoUpdateInterval > 0)
                    {
                        AutoUpdateTimer.Restart();
                    }

                    LoadItems();
                }
            }

        }

        public void Run()
        {
            DoRun=true;

            var t=new Timeout(
                1,
                ()=>{
                    AfterInit();
                }              
            );
            t.SetIntervalMs(500);
            t.Run();          
        }

        public void Destruct()
        {
            ScrollersUpdateTimer.Finish();
            ColumnResizeTimer.Finish();
            AutoUpdateTimer.Finish();
        }

        public void ShowSplash()
        {
            Splash.Visibility=Visibility.Visible;
        }

        public void HideSplash()
        {
            Splash.Visibility=Visibility.Collapsed;
        }
        
        public void LoadItems()
        {            
            {
                if(OnLoadItems!=null)
                {
                    OnLoadItems?.Invoke();
                }
            }
        }
        
        public void UpdateItems()
        {
            UpdateItems(null);
        }

        private Dictionary<string, string> RenderingRow {get;set;}
        private int RenderingRowIndex {get;set;}
        public void UpdateItems(ListDataSet ds = null, bool selectFirst=true)
        {
            if(ds != null)
            {
                Ds=ds;
            }

            if(Ds!=null)
            {
                if(Ds.Items.Count > 0)
                {
                    ShowSplash();

                    DebugLog("update_1");

                    GridItems=Ds.Items;

                    if(OnFilterItems != null)
                    {
                        DebugLog("update_2 filter");
                        OnFilterItems.Invoke();
                    }

                    GridBodyContainer.Children.Clear();
                    RenderingRowIndex=0;

                    {
                        DebugLog("update_3 sort");
                        var list=ItemsGetSortedSort();

                        LineToolTipMode=1;
                        if(OnViewItem != null)
                        {
                            LineToolTipMode=2;
                        }

                        DebugLog("update_4 render");
                        foreach(Dictionary<string, string> row in list)
                        {
                            RenderingRowIndex++;
                            RenderingRow=row;
                            CellBodyConstruct();
                        }
                    }                    

                    {
                        DebugLog("update_5 styler");
                        RowStylersProcess();                        
                    }

                    DebugLog("update_9");
                    HideSplash();

                    if(!Autosized)
                    {
                        Autosized=true;

                        var interval=new Timeout(
                            1,
                            ()=>{
                                CellHeaderWidthProcess();
                            }
                        );
                        interval.SetIntervalMs(1000);
                        interval.Run();

                        //if(SelectedItem.Count == 0)
                        //{
                        //    SelectRowFirst();
                        //}
                    }
                }
            }
        }

        private List<Dictionary<string, string>> ItemsGetSortedSort()
        {
            var list=GridItems;
            if(list.Count > 0)
            {
                var comparer = new GridBoxComparer(SortDirection,SortColumn.Path,SortColumn);
                list.Sort(comparer);
            }
            return list;
        }

        public void SetColumns( List<DataGridHelperColumn> columns)
        {
            Columns=columns;
        }

        public void SetSorting(string columnName, ListSortDirection direction = ListSortDirection.Ascending)
        {
            ShowSplash();
            SortColumnPrev=SortColumn;
            SortColumn=ColumnGet(columnName);
            SortDirection=direction;
            ColumnSetSortingLabel();           
        }

        public void SetPrimaryKey(string columnName)
        {
            var c=ColumnGet(columnName);
            PrimaryKey=c.Path;
        }

        public void SetSelectToFirstRow()
        {
        }
        
        public void SetSelectToLastRow()
        {
        }

        public void SetSelectedItemId(int id)
        {
        }

        public void DebugLog(string s)
        {
            //var dt=Profiler.GetDelta();
            //System.Diagnostics.Trace.WriteLine($"({dt}) {s}");
        }

        public void DebugShowGridInfo()
        {

            var s=CollectGridInfo();
            
            var msg=s;
            var d = new LogWindow($"{msg}", "Информация" );
            d.ShowDialog();

          
        }

        public void DebugShowColumnsInfo()
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

                        var h=c.Header;
                        var p=c.Path;
                        var w=0;

                        
                        var l=c.AutoWidthLog;

                        if(!c.Hidden && c.Path!="_")
                        {
                            include=true;
                            if(ColumnWidth.ContainsKey(p))
                            {
                                w=ColumnWidth[p];
                            }
                        }
                        
                        //var a = (int) c.Width2 * ColumnSymbolWidth;
                        //расчетная в пикс
                        var a=(int)c.Width;
                        //обратно пересчитанная
                        var b = (int)((double) w / (double) ColumnSymbolWidth);

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

        private string CollectGridInfo()
        {
            var result="";

            result = result.Append($"GridBox-2",true);
            result = result.Append($"грид: {Name}",true);
            result = result.Append($"режим: {ColumnWidthMode}",true);
            result = result.Append($"первичный ключ: {PrimaryKey}",true);
            result = result.Append($"автообновление: {AutoUpdateInterval}",true);
            result = result.Append($"строк: {Ds.Items.Count} отфильтровано: {GridItems.Count}",true);

            return result;
        }

       

    }
}
