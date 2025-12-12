using AutoUpdaterDotNET;
using Client.Assets.Converters;
using Client.Common;
using Gu.Wpf.DataGrid2D;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static Client.Interfaces.Main.FormDialog;

namespace Client.Interfaces.Main
{
    /// <summary>
    /// грид с данными
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-11-10</released>
    /// <changed>2023-11-10</changed>
    public partial class GridBox3:UserControl
    {
        public GridBox3()
        {
            InitializeComponent();

            CellClickDurationTheshhold = 150;
            CellDoubleClickInterval = 1000;

            Initialized =false;
            Autosized=false;
            SearchText=new TextBox();
            UseRowHeader=false;
            Name="";
            Descriription="";
            ColumnWidthMode=GridBox.ColumnWidthModeRef.Compact;
            Columns=new List<DataGridHelperColumn>();
            ColumnsCount=0;
            Items=new List<Dictionary<string, string>>();
            GridItems=new List<Dictionary<string, string>>();
            GridItemsSorted=new List<Dictionary<string, string>>();
            SelectedItem=new Dictionary<string,string>();
            SelectedItemPrev=new Dictionary<string,string>();
            SelectedItemIndex=0;
            SelectedItemPrevIndex=0;
            SelectedColumn = null;
            SelectedColumnPrev = null;
            SelectedCell = null;
            SelectedCellPrev = null;
            Ds =new ListDataSet();
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
            UseSorting=true;
            resizerWidth=5;            
            sorterWidth=10;
            sorterSymbolAsc="▲";
            sorterSymbolDesc="▼";
            ColumnTotalWidth=0;
            ColumnSymbolWidth=8;
            ColumnWidthMin=20;
            ColumnWidthOffset = 0;
            ColumnHeightMin =20;
            ColumnAutowidthLog="";
            AutoUpdateInterval=60;
            //300
            Interval1 = 300;
            //300
            Interval2 = 300;
            //300
            Interval3 = 300;

            ScrollersUpdateTimeout =new Common.Timeout(
                1,
                ()=>{
                    ScrollersUpdateSize();        
                }
            );
            ScrollersUpdateTimeout.SetIntervalMs(Interval2);


            ColumnResizeTimeout =new Common.Timeout(
                1,
                ()=>{
                    ColumnResizerResetStyle();
                    CellUpdateWidthAll();
                }
            );
            ColumnResizeTimeout.SetIntervalMs(Interval3);


            AutoUpdateTimeout=new Common.Timeout(
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

            SearchTimeout=new Common.Timeout(
                1,
                ()=>{
                    SearchCheckText();
                },
                true,
                false
            );
            SearchTimeout.SetIntervalMs(1500);

            MouseReleaseTimeout=new Common.Timeout(
                1,
                ()=>{
                    MouseCursorRelease();
                },
                true,
                false
            );
            MouseReleaseTimeout.SetIntervalMs(500);


            SorterReleaseTimeout=new Common.Timeout(
                1,
                ()=>{
                    GridSorterBlocked=false;
                },
                true,
                false
            );
            SorterReleaseTimeout.SetIntervalMs(1000);
            


            LineWidthAdd=40;
            LineWidth=0;
            VerticalScrollPosition=0;
            VerticalScrollPositionOld=0;
            VerticalScrollDirection=0;
            VerticalScrollDelta=0;
            LineHeight=0;
            LineFirstVisible=0;
            LineToolTipMode=0;
            Runned=false;
            DoRun=false;
            ConverterFormat = new NumberFormatInfo { NumberDecimalSeparator = "," };
            ConverterCulture = new CultureInfo("ru-RU");
            Profiler=new Profiler();
            Styles=new Dictionary<string, Style>();
            UseDynamicRendering=true;
            SearchingInProgress=false;
            RenderingInProgress=false;
            SearchingComplete=false;

            CellClickCount=0;
            CellClickTimeout=new Common.Timeout(
                1,
                ()=>{
                    CellDoubleClickCheck();
                },
                true,
                false
            );
            CellClickTimeout.SetIntervalMs(CellDoubleClickInterval);

            CellList=new Dictionary<int, List<Border>>();
            IndexRenderStep=30;
            MouseOverArea=0;
            GridSorterBlocked=false;
            DescriptionMenuItem=false;
            QueryLoadItems=null;
            SortDirection = ListSortDirection.Ascending;
            SortDirectionPrev = ListSortDirection.Ascending;
            UseTotals = false;

            CellControllClicked = false;
            CellClickDuration = 0;
            CellClickProfiler = new Profiler();
           
            SortingEnabled = false;
        }               

        public bool Initialized {get;set;}
        public bool Autosized {get;set;}
        public TextBox SearchText { get; set; } 
        public bool UseRowHeader { get; set; }
        public string Name { get; set; }
        public string Descriription { get; set; }
        public GridBox.ColumnWidthModeRef ColumnWidthMode {get;set;}
        public List<DataGridHelperColumn> Columns { get; set; }
        private int ColumnsCount {get;set;}
        public List<Dictionary<string,string>> Items { get; set; }
        public List<Dictionary<string,string>> GridItems { get; set; }
        public List<Dictionary<string,string>> GridItemsSorted { get; set; }
        public Dictionary<string,string> SelectedItem { get; set; }
        public Dictionary<string,string> SelectedItemPrev { get; set; }
        public DataGridHelperColumn SelectedColumn { get; set; }
        public DataGridHelperColumn SelectedColumnPrev { get; set; }
        public Border SelectedCell { get; set; }
        public Border SelectedCellPrev { get; set; }
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
        public ListSortDirection SortDirectionPrev { get; set; }
        /// <summary>
        /// разрешение сортировать столбцы кликом по заголовку
        /// </summary>
        public bool UseSorting { get; set; }
        private int resizerWidth {get;set;}
        private int sorterWidth {get;set;}
        private string sorterSymbolAsc {get;set;}
        private string sorterSymbolDesc {get;set;}
        private int ColumnTotalWidth {get;set;}
        private int ColumnSymbolWidth {get;set;}
        private int ColumnWidthMin {get;set;}
        private int ColumnWidthOffset { get; set; }        
        private int ColumnHeightMin {get;set;}
        private string ColumnAutowidthLog{get;set;}
        public int AutoUpdateInterval{get;set;}
        public bool ItemsAutoUpdate{get;set;}
        private Common.Timeout ScrollersUpdateTimeout {get;set;}
        private Common.Timeout ColumnResizeTimeout {get;set;}
        private Common.Timeout AutoUpdateTimeout {get;set;}
        private Common.Timeout SearchTimeout {get;set;}
        private Common.Timeout MouseReleaseTimeout {get;set;}
        private Common.Timeout SorterReleaseTimeout {get;set;}
        private int LineWidthAdd {get;set;}
        private int LineWidth {get;set;}
        private int VerticalScrollPosition {get;set;}
        private int VerticalScrollPositionOld {get;set;}
        private int VerticalScrollDirection {get;set;}
        private int VerticalScrollDelta {get;set;}
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
        private Dictionary<string,Style> Styles{get;set;}
        private bool UseDynamicRendering {get;set;}
        private int CellClickCount {get;set;}
        private Common.Timeout CellClickTimeout {get;set;}
        private int CellDoubleClickInterval {get;set;}
        private bool DescriptionMenuItem {get;set;}
        public RequestData QueryLoadItems {get;set;}
        public StackPanel Toolbar {get;set;}
        /// <summary>
        /// запуск просчета ширины колонок после рендера
        /// </summary>
        private int Interval1 { get; set; }
        /// <summary>
        /// обновление скроллеров
        /// </summary>
        private int Interval2 { get; set; }
        /// <summary>
        /// задержка после ручного ресайза колонки
        /// </summary>
        private int Interval3 { get; set; }
        private bool UseTotals { get; set; }
        private int CellClickDurationTheshhold { get; set; }
        private bool SortingEnabled { get; set; }

        public void Init()
        {
            if(!Initialized)
            {
                DebugLogInit("Init");

                GridHeaderContainer.Children.Clear();
                GridBodyContainer.Children.Clear();
                GridTotalsContainer.Children.Clear();

                LoadStyles();

                DebugLogInit("CellHeaderConstruct");
                CellHeaderConstruct();              
                
                DebugLogInit("EventsBind");
                EventsBind();
                
                GridContainer.MouseMove += GridOnMouseMove;
                GridContainer.MouseUp += GridContainer_MouseUp;
                GridContainer.PreviewKeyDown += GridContainer_PreviewKeyDown;
                Central.Msg.Register(ProcessMessage);

                if(AutoUpdateInterval > 0)
                {
                    AutoUpdateTimeout.SetInterval(AutoUpdateInterval);
                }

                if (SearchText != null)
                {
                    SearchText.KeyUp += SearchText_KeyUp;
                }

                DebugLogInit("MenuConstruct");
                CellMenuConstruct();               

                if(SortingEnabled)
                {
                    if(SortColumn.Path.IsNullOrEmpty())
                    {
                        SetSorting(PrimaryKey, ListSortDirection.Ascending);
                    }
                }

                Initialized=true;
            }

            Run();
        }

        private void SearchText_KeyUp(object sender,System.Windows.Input.KeyEventArgs e)
        {
            if(Initialized)

            {
                if(GridItems.Count > 0)
                {
                    if(!RenderingInProgress && !SearchingInProgress)
                    {
                        SearchTimeout.Restart();
                    }            
                }
            }            
        }

        private void SearchCheckText()
        {
            SearchTimeout.Finish();
            bool doSearch=false;

            if(!doSearch){
                if(SearchText != null)
                {
                    var t=SearchText.Text;
                    if(!t.IsNullOrEmpty())
                    {
                        if(t.Length >= 3)
                        {
                            doSearch=true;
                            SearchingComplete=true;
                        }                        
                    }
                    else
                    {
                        if(SearchingComplete)
                        {
                            doSearch=true;
                            SearchingComplete=false;
                        }
                    }
                }
            }

            if(doSearch)
            {
                SearchingInProgress=true;
                UpdateItems();
            }
        }

        private void GridContainer_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            ProcessKeyboardEvents(e);
        }

        private void LoadStyles()
        {
            if(!Styles.ContainsKey("GridboxCellHeaderBorder"))
            {
                Styles.Add("GridboxCellHeaderBorder",(Style)GridContainer.TryFindResource("GridboxCellHeaderBorder"));
                Styles.Add("GridboxCellHeaderText",(Style)GridContainer.TryFindResource("GridboxCellHeaderText"));
                Styles.Add("GridboxCellHeaderSorterBorder",(Style)GridContainer.TryFindResource("GridboxCellHeaderSorterBorder"));
                Styles.Add("GridboxCellHeaderResizerBorder",(Style)GridContainer.TryFindResource("GridboxCellHeaderResizerBorder"));
                Styles.Add("GridboxCellBodyBorder",(Style)GridContainer.TryFindResource("GridboxCellBodyBorder"));
                Styles.Add("GridboxCellBodyDigitBorder",(Style)GridContainer.TryFindResource("GridboxCellBodyDigitBorder"));
                Styles.Add("GridboxCellBodySelectedBorder",(Style)GridContainer.TryFindResource("GridboxCellBodySelectedBorder"));
                Styles.Add("GridboxCellBodyDigitSelectedBorder",(Style)GridContainer.TryFindResource("GridboxCellBodyDigitSelectedBorder"));
                Styles.Add("GridboxCellBodyCheckBox",(Style)GridContainer.TryFindResource("GridboxCellBodyCheckBox"));
                Styles.Add("GridboxCellBodyText",(Style)GridContainer.TryFindResource("GridboxCellBodyText"));
                Styles.Add("GridboxLineBorder",(Style)GridContainer.TryFindResource("GridboxLineBorder"));
                Styles.Add("GridboxLineStackPanel",(Style)GridContainer.TryFindResource("GridboxLineStackPanel"));
                Styles.Add("GridboxLineSelectedBorder",(Style)GridContainer.TryFindResource("GridboxLineSelectedBorder"));
                Styles.Add("GridboxLineSelectedStackPanel",(Style)GridContainer.TryFindResource("GridboxLineSelectedStackPanel"));
                Styles.Add("GridboxCellHeaderTextBorder", (Style)GridContainer.TryFindResource("GridboxCellHeaderTextBorder"));
                Styles.Add("GridboxCellBodyTextBox", (Style)GridContainer.TryFindResource("GridboxCellBodyTextBox"));
                Styles.Add("GridboxCellHeaderStaticBorder", (Style)GridContainer.TryFindResource("GridboxCellHeaderStaticBorder"));
                Styles.Add("CheckBoxStyle", (Style)GridContainer.TryFindResource("CheckBoxStyle"));
            }            
        }

        private Style StyleGet(string styleName)
        {
            var result=new Style();
            if(Styles.ContainsKey(styleName))
            {
                result=Styles[styleName];
            }
            return result;
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
                            DoResize();                            
                            break;
                    }
                }
            }
        }

        public bool ColumnCheckVisible(DataGridHelperColumn c)
        {
            bool result = false;
            if(c.Visible && !c.Hidden)
            {
                result = true;                
            }

            //if(ColumnWidthMode == GridBox.ColumnWidthModeRef.Compact)
            //{
            //    if(c.Path == "_")
            //    {
            //        result = false; 
            //    }
            //}

            return result;
        }

        public int CellSpacerGetWidth()
        {
            int result = 0;

            var gridContainerWidth = GetGridContainderWidth();
            if(ColumnWidthMode == GridBox.ColumnWidthModeRef.Full)
            {
                if(LineWidth < gridContainerWidth)
                {
                    result = gridContainerWidth - LineWidth;
                }
            }

            if(ColumnWidthMode == GridBox.ColumnWidthModeRef.Compact)
            {
                if(LineWidth < gridContainerWidth)
                {
                    result = gridContainerWidth - LineWidth;
                }
            }

            if(result < 0)
            {
                result = 0;
            }

            return result;
        }

        public int GetLineWidth()
        {
            var lineWidth = 0;
            var spacerWidth = 0;
            var gridContainerWidth = GetGridContainderWidth();

            {
                foreach(DataGridHelperColumn c in Columns)
                {
                    if(ColumnCheckVisible(c))
                    {
                        if(c.Path != "_")
                        {
                            var w = CellGetWidth(c.Path);
                            lineWidth = (int)(lineWidth + w);
                        }
                    }
                }

                //spacerWidth = gridContainerWidth - lineWidth;

                //if(ColumnWidthMode == GridBox.ColumnWidthModeRef.Compact)
                {
                    if(lineWidth < gridContainerWidth)
                    {
                        spacerWidth = gridContainerWidth - lineWidth;
                    }
                }

                CellSetWidth("_", spacerWidth);

                LineWidth = lineWidth + spacerWidth;
            }
            //GridHeaderContainer.Width = lineWidth;
            //LineWidth = lineWidth;

            return LineWidth;
        }

        public void CellUpdateWidth(string k, int width)
        {
            if(width > -1 && width >= ColumnWidthMin)
            {
                CellSetWidth(k,width);                

                /*
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
                */

                var lineWidth = GetLineWidth();
                /*
                {
                    foreach(DataGridHelperColumn c in Columns)
                    {
                        if(ColumnCheckVisible(c))
                        {
                            if(c.Path != "_")
                            {
                                var w = CellGetWidth(c.Path);
                                lineWidth = (int)(lineWidth + w);
                            }
                        }
                    }
                }
                //GridHeaderContainer.Width = lineWidth;
                LineWidth = lineWidth;
                */

                var column = ColumnGet(k);
                {
                    //var column = ColumnGet(k);
                    int j = 0;
                    foreach(ColumnDefinition cd in GridHeaderContainer.ColumnDefinitions)
                    {
                        if(column.Index == j)
                        {
                            cd.Width = new GridLength(width, GridUnitType.Pixel);
                        }
                        j++;
                    }
                }

                {
                    //var column=ColumnGet(k);
                    int j=0;
                    foreach (ColumnDefinition cd in GridBodyContainer.ColumnDefinitions)
                    {
                        if(column.Index == j)
                        {
                            cd.Width=new GridLength(width,GridUnitType.Pixel);
                        }
                        j++;
                    }
                }

                {
                    //var column=ColumnGet(k);
                    int j = 0;
                    foreach(ColumnDefinition cd in GridTotalsContainer.ColumnDefinitions)
                    {
                        if(column.Index == j)
                        {
                            cd.Width = new GridLength(width, GridUnitType.Pixel);
                        }
                        j++;
                    }
                }

                {
                    foreach(DataGridHelperColumn c in Columns)
                    {
                        if(ColumnCheckVisible(c))
                        {
                            try
                            {
                                var cellBorder = CellHeaderGet(c.Path);
                                var cellGrid = (Grid)cellBorder.Child;
                                var cellGridElementKey = $"caption_{k}";
                                foreach(object cellGridElement in cellGrid.Children)
                                {
                                    var cellGridElementBorder = (Border)cellGridElement;
                                    if(cellGridElementBorder.Name == cellGridElementKey)
                                    {
                                        if(width > ColumnWidthMin)
                                        {
                                            var n = 10;
                                            cellBorder.Width = width;
                                            var w = width - (resizerWidth + sorterWidth + n);
                                            if(w > 0)
                                            {
                                                cellGridElementBorder.Width = w;
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception e) 
                            { 
                            }
                        }
                    }
                }

                //ScrollersUpdate();
                //CellUpdateWidthAll();
            }
        }
       
        public void CellUpdateWidthAll()
        {
            DebugLog("width_2");

            /*
            var lineWidth=0;
            {
                foreach(object line in GridHeaderContainer.Children)
                {
                    var b0=(Border)line;
                    var s=(Grid)b0.Child;                    
                    foreach(object cell in s.Children)
                    {
                        var b=(Border)cell;
                        var n=b.Name;
                        var k=n.CropAfter2("cell_header_");

                        if(ColumnWidth.ContainsKey(k))
                        {
                            //var width=ColumnWidth[k].ToInt();
                            var width = CellGetWidth(k);
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
            */

            var lineWidth = GetLineWidth();
            /*
            var lineWidth = 0;
            {
                foreach(DataGridHelperColumn c in Columns)
                {
                    if(ColumnCheckVisible(c))
                    {
                        var w = CellGetWidth(c.Path);
                        lineWidth = (int)(lineWidth + w);
                    }
                }
            }
            //GridHeaderContainer.Width = lineWidth;
            LineWidth = lineWidth;
            */

            {
                int j = 0;
                foreach(ColumnDefinition cd in GridHeaderContainer.ColumnDefinitions)
                {
                    var column = ColumnGetByIndex(j);
                    var width = CellGetWidth(column.Path);
                    cd.Width = new GridLength(width, GridUnitType.Pixel);
                    j++;
                }
            }

            {
                int j=0;
                foreach (ColumnDefinition cd in GridBodyContainer.ColumnDefinitions)
                {
                    var column=ColumnGetByIndex(j);
                    var width=CellGetWidth(column.Path);
                    cd.Width=new GridLength(width,GridUnitType.Pixel);
                    j++;
                }
            }

            {
                int j = 0;
                foreach(ColumnDefinition cd in GridTotalsContainer.ColumnDefinitions)
                {
                    var column = ColumnGetByIndex(j);
                    var width = CellGetWidth(column.Path);
                    cd.Width = new GridLength(width, GridUnitType.Pixel);
                    j++;
                }
            }

            if(Initialized)
            {
                foreach(DataGridHelperColumn c in Columns)
                {
                    if(ColumnCheckVisible(c))
                    {
                        var width = CellGetWidth(c.Path);
                        var k = c.Path;

                        try
                        {
                            var cellBorder = CellHeaderGet(c.Path);
                            var cellGrid = (Grid)cellBorder.Child;
                            var cellGridElementKey = $"caption_{k}";
                            foreach(object cellGridElement in cellGrid.Children)
                            {
                                var cellGridElementBorder = (System.Windows.Controls.Border)cellGridElement;
                                if(cellGridElementBorder.Name == cellGridElementKey)
                                {
                                    if(width > ColumnWidthMin)
                                    {
                                        var n = 10;
                                        cellBorder.Width = width;
                                        var w = width - (resizerWidth + sorterWidth + n);
                                        if(w > 0)
                                        {
                                            cellGridElementBorder.Width = w;
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {

                        }
                    }
                }
            }

            ScrollersUpdate();
        }

        private void RowStylersProcess(List<Border> list)
        {
            try
            {
                foreach(object cell in list)
                {
                    var b=(Border)cell;
                    var p=GetCellInfo(b);
                    var c=ColumnGet(p.CheckGet("PATH"));
                    var index=RenderingRowIndex-1;
                    var row=RenderingRow;

                    var render=false;
                    if(ColumnCheckVisible(c))
                    {
                        render=true;
                    }

                    if(render)
                    {
                        RowStylersProcessOne(b,p,c,index,row);
                    }
                }
            }
            catch(Exception e)
            {
            }           
        }

        private void RowStylersProcessOne(Border b, Dictionary<string,string> p, DataGridHelperColumn c, int index, Dictionary<string,string> row)
        {
            try
            {
                if (RowStylers.Count > 0)
                {
                    foreach (KeyValuePair<StylerTypeRef, StylerDelegate> item in RowStylers)
                    {
                        var type = item.Key;
                        var d = item.Value;
                        if (d != null)
                        {
                            var result = d.Invoke(row);
                            RowStylersProcessOneStyler(type, b, result, c);
                        }
                    }
                }

                var mode=0;

                if(mode==0)
                {
                    if (c.Stylers2.Count > 0)
                    {
                        mode=2;
                    }
                }
                
                if(mode==0)
                {
                    if (c.Stylers.Count > 0)
                    {
                        mode=1;
                    }
                }

                if (mode == 1)
                {
                    foreach (KeyValuePair<StylerTypeRef, StylerDelegate> item in c.Stylers)
                    {
                        var type = item.Key;
                        var d = item.Value;
                        if (d != null)
                        {
                            var result = d.Invoke(row);
                            RowStylersProcessOneStyler(type, b, result, c);
                        }
                    }
                }

                if (mode == 2)
                {
                    foreach (StylerProcessor processor in c.Stylers2)
                    {
                        var type = processor.StylerType;
                        var d = processor.Processor;
                        if (d != null)
                        {
                            var result = d.Invoke(row);
                            RowStylersProcessOneStyler(type, b, result, c);
                        }
                    }
                }
            }
            catch(Exception e)
            {
            }
        }

        public void ShowDescription()
        {
            var h=new GridDescription();
            h.GridBox=this;
            h.Init();
            h.Open();
        }

        private object ColumnHeaderMakeTooltip(DataGridHelperColumn c)
        {
            var result=new Border();

            var toolTipContent=$"{c.Header}";
            {
                var s=CellHeaderGetTooltip(c);
                toolTipContent=toolTipContent.Append(s,true);
            }
            
            if(!toolTipContent.IsNullOrEmpty())            
            {
                var block=new StackPanel();
                block.Orientation= Orientation.Vertical;

                {
                    var elementText=new TextBlock();
                    elementText.Text=toolTipContent;
                    //elementText.Style=(Style)Description.TryFindResource("ColumnDefinitionContainerText");
                    block.Children.Add(elementText);
                }

                //{
                //    var elementText=new TextBlock();
                //    elementText.Text="Описание";
                //    elementText.Style=(Style)GridHeaderContainer.TryFindResource("ColumnDefinitionContainerText");
                //    elementText.Padding=new Thickness(0);
                //    block.Children.Add(elementText);
                //}

                var elementBlock=new Border();                        
                elementBlock.Child=block;
                elementBlock.Tag=$"{c.Path}";
                //elementBlock.ToolTip=c.Header;
                //elementBlock.MouseUp += ColumnOnMouseClick;
                //elementBlock.Style=(Style)Description.TryFindResource("ColumnDefinitionContainerItem");

                result=elementBlock;
            }


            return result;
        }

        private string RowStylersGetDescription(DataGridHelperColumn c)
        {
            var result="";
             
            if (c.Stylers2.Count > 0)
            {
                var row=new Dictionary<string, string>();
                foreach (StylerProcessor processor in c.Stylers2)
                {
                    var type = processor.StylerType;
                    var d = processor.Processor;
                    if (d != null)
                    {
                        
                        var stylerDescription=(Dictionary<string, string>)d.Invoke(row, 1);


                        if(stylerDescription.Count > 0)
                        {
                            var stylerTypeTitle="";
                            switch(type)
                            {
                                case StylerTypeRef.BackgroundColor:
                                    stylerTypeTitle="Цвет фона";
                                    break;

                                case StylerTypeRef.BorderColor:
                                    stylerTypeTitle="Цвет границы";
                                    break;

                                case StylerTypeRef.FontWeight:
                                    stylerTypeTitle="Шрифт";
                                    break;

                                case StylerTypeRef.ForegroundColor:
                                    stylerTypeTitle="Цвет текста";
                                    break;
                            }

                            result=result.Append(stylerTypeTitle,true);

                            //result=result.Append(stylerDescription,true,1);
                            foreach(KeyValuePair<string, string> item in stylerDescription)
                            {
                                result=result.Append($"{item.Key}{item.Value}",true,1);
                            }
                        }
                    }
                }
            }

            return result;
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
            return ColumnGetByPath(path);
        }

        private DataGridHelperColumn ColumnGetByPath(string path)
        {
            var  result=new DataGridHelperColumn();

            if(Columns.Count > 0)
            {
                foreach(DataGridHelperColumn c in Columns)
                {
                    if(c.Path == path)
                    {
                        result=c;
                        break;
                    }
                }
            }

            return result;
        }

        private DataGridHelperColumn ColumnGetByIndex(int index)
        {
            var  result=new DataGridHelperColumn();

            if(Columns.Count > 0)
            {
                foreach(DataGridHelperColumn c in Columns)
                {
                    if(c.Index == index)
                    {
                        result=c;
                        break;
                    }
                }
            }

            return result;
        }

        private Border CellHeaderGet(string path)
        {
            var result=new Border();

            foreach(Border cell in GridHeaderContainer.Children)
            {
                var p = GetCellInfo(cell);
                if(p.CheckGet("PATH") == path)
                {
                    result= cell;
                    break;
                }   
            }

            //foreach(object line in GridHeaderContainer.Children)
            //{
            //    var b0=(Border)line;
            //    var s=(Grid)b0.Child;
            //    foreach(object cell in s.Children)
            //    {
            //        var b=(Border)cell;
            //        var p=GetCellInfo(b);
            //        if(p.CheckGet("PATH") == path)
            //        {
            //            result=b;
            //            break;
            //        }                    
            //    }
            //}

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
                    result = ColumnWidth[k];
                }
            }

            //if(k != "_")
            //{
            //    if(ColumnWidth.ContainsKey(k))
            //    {
            //        if(ColumnWidth[k] > 0)
            //        {
            //            result=ColumnWidth[k];
            //        }                
            //    }
            //}
            //else
            //{
            //    result = CellSpacerGetWidth();
            //}

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
                            Header="(Отладка)",
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
                                        Header ="Режим 1 (Compact)",
                                        Action=() =>
                                        {
                                            ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
                                            //CellHeaderWidthProcess(true);
                                            Autosized=false;
                                            UpdateItems();
                                        }
                                    }
                                },
                                {
                                    "ColumnsAutoResize2",
                                    new DataGridContextMenuItem()
                                    {
                                        Header ="Режим 2 (Full)",
                                        Action=() =>
                                        {
                                            ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                                            //CellHeaderWidthProcess(true);
                                            Autosized=false;
                                            UpdateItems();
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
                                {
                                    "Reinit",
                                    new DataGridContextMenuItem()
                                    {
                                        Header ="Переинициализация",
                                        Action=() =>
                                        {
                                            Initialized=false;
                                            Init();
                                        }
                                    }
                                },
                                {
                                    "Render",
                                    new DataGridContextMenuItem()
                                    {
                                        Header ="Рендер",
                                        Action=() =>
                                        {
                                            UpdateRenderedRows(0);
                                        }
                                    }
                                },
                                {
                                    "ScrollersUpdateSize",
                                    new DataGridContextMenuItem()
                                    {
                                        Header ="ScrollersUpdateSize",
                                        Action=() =>
                                        {
                                            ScrollersUpdateSize();
                                        }
                                    }
                                },
                                {
                                    "RenderClear",
                                    new DataGridContextMenuItem()
                                    {
                                        Header ="RenderClear",
                                        Action=() =>
                                        {
                                            RenderClear();
                                        }
                                    }
                                },
                                {
                                    "CellUpdateWidthAll",
                                    new DataGridContextMenuItem()
                                    {
                                        Header ="CellUpdateWidthAll",
                                        Action=() =>
                                        {
                                            CellUpdateWidthAll();
                                        }
                                    }
                                },
                                {
                                    "CellHeaderWidthProcess0",
                                    new DataGridContextMenuItem()
                                    {
                                        Header ="CellHeaderWidthProcess(0)",
                                        Action=() =>
                                        {
                                            CellHeaderWidthProcess();
                                        }
                                    }
                                },
                                {
                                    "CellHeaderWidthProcess1",
                                    new DataGridContextMenuItem()
                                    {
                                        Header ="CellHeaderWidthProcess(1)",
                                        Action=() =>
                                        {
                                            CellHeaderWidthProcess(true);
                                        }
                                    }
                                },

                            }
                        }
                    );
                }
            }

            if (Central.DebugMode)
            {
                if (DescriptionMenuItem)
                {
                    if (!Menu.ContainsKey("Description"))
                    {
                        Menu.Add(
                            "Description",
                            new DataGridContextMenuItem()
                            {
                                Header = "(Описание)",
                                Action = () =>
                                {
                                    ShowDescription();
                                }
                            }
                        );
                    }
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

        private string CellBodyContentCurrent {get;set;}
        private int _RowCounter {get;set;}

        private void CellRowConstruct()
        {
            var rd=new RowDefinition();
            //rd.Height=new GridLength(20,GridUnitType.Pixel);
            rd.Height=new GridLength(0,GridUnitType.Auto);
            GridBodyContainer.RowDefinitions.Add(rd); 
            _RowCounter++;
        }
        

        public void CellHeaderConstruct()
        {
            GridHeaderContainer.ColumnDefinitions.Clear();
            GridHeaderContainer.RowDefinitions.Clear();

            GridBodyContainer.ColumnDefinitions.Clear();
            GridBodyContainer.RowDefinitions.Clear();

            GridTotalsContainer.ColumnDefinitions.Clear();
            GridTotalsContainer.RowDefinitions.Clear();

            if(Columns.Count > 0)
            {
                CellHeaderWidthInit();
                CellHeaderWidthProcess(true);

                {
                    var rd = new RowDefinition();
                    rd.Height = new GridLength(0, GridUnitType.Auto);
                    GridHeaderContainer.RowDefinitions.Add(rd);
                }

                {
                    var rd = new RowDefinition();
                    rd.Height = new GridLength(0, GridUnitType.Auto);
                    GridTotalsContainer.RowDefinitions.Add(rd);
                }

                //var line=new StackPanel();
                //line.Orientation=Orientation.Horizontal;   
                //line.HorizontalAlignment=HorizontalAlignment.Left;                

                int j = 0;
                foreach(DataGridHelperColumn c in Columns)
                {
                    var render=false;
                    var k=c.Path;

                    if(ColumnCheckVisible(c))
                    {
                        render=true;
                    }

                    if(render)
                    {
                        CellSetWidth(k, c.Width);

                        var w = CellGetWidth(k);
                        {
                            var cd=new ColumnDefinition();
                            cd.Width=new GridLength(w,GridUnitType.Pixel);
                            cd.MinWidth=20;
                            GridHeaderContainer.ColumnDefinitions.Add(cd);
                        }

                        {
                            var cd = new ColumnDefinition();
                            cd.Width = new GridLength(w, GridUnitType.Pixel);
                            cd.MinWidth = 20;
                            GridBodyContainer.ColumnDefinitions.Add(cd);
                        }

                        {
                            var cd = new ColumnDefinition();
                            cd.Width = new GridLength(w, GridUnitType.Pixel);
                            cd.MinWidth = 20;
                            GridTotalsContainer.ColumnDefinitions.Add(cd);
                        }

                        {
                            if(!c.Doc.IsNullOrEmpty())
                            {
                                DescriptionMenuItem=true;
                            }
                        }

                        {
                            if(!UseTotals)
                            {
                                if(c.Totals != null)
                                {
                                    UseTotals = true;
                                }
                            }
                        }
                    }
                                        
                    if(render)
                    {
                        var b = CellHeaderCreate(c);
                        //line.Children.Add(b);

                        GridHeaderContainer.Children.Add(b);
                        Grid.SetRow(b, 0);
                        //Grid.SetColumn(b, c.ColumnIndex);
                        Grid.SetColumn(b, j);
                        j++;
                    }
                }

                //var lineBorder=new Border();
                //lineBorder.Child=line;
                //lineBorder.BorderBrush = "#ff900000".ToBrush();
                //if(true)
                //{
                //    lineBorder.BorderThickness = new Thickness(1);
                //    lineBorder.Background = "#ffF0F0F0".ToBrush();
                //}
                //lineBorder.HorizontalAlignment = HorizontalAlignment.Stretch;
                //GridHeaderContainer.Children.Add(lineBorder);
               



            }
        }

        private StackPanel RowSelector {get;set;}
        private void CellBodySelectorConstruct()
        {
            RowSelector=new StackPanel();
            RowSelector.Style=(Style)GridBodyContainer.TryFindResource("GridboxCellBodySelectorBorder");
            RowSelector.Visibility=Visibility.Collapsed;
            //RowSelector.Width=LineWidth;

            {
                {
                    var b=new Border();
                    b.Style=(Style)GridBodyContainer.TryFindResource("GridboxCellBodySelectorBorder1");
                    RowSelector.Children.Add(b);
                }
                {
                    var b=new Border();
                    b.Style=(Style)GridBodyContainer.TryFindResource("GridboxCellBodySelectorBorder2");
                    RowSelector.Children.Add(b);
                }
            }
            
            GridBodyContainer.Children.Add(RowSelector);

            Grid.SetRow(RowSelector,1);
            Grid.SetColumn(RowSelector,0);
            Grid.SetColumnSpan(RowSelector,ColumnsCount);
        }

        private void CellBodySelectorUpdate()
        {
            RowSelector.Visibility=Visibility.Visible;
            Grid.SetRow(RowSelector,SelectedItemIndex-1);
        }
        
        private int RowsRendered {get;set;}
        public void CellBodyConstruct()
        {
            if(Columns.Count > 0)
            {                
                {
                    var s3="";

                    var list=new List<Border>();
                    var j=0;
                    foreach(DataGridHelperColumn c in Columns)
                    {
                        var render=false;

                        if(ColumnCheckVisible(c))
                        {
                            render=true;
                        }

                        if( CellCheck(RenderingRowIndex, c.Path) )
                        {
                            render=false;
                        }

                        if(RenderFirst)
                        {
                            if(j == 0)
                            {
                                render=true;
                            }
                            else
                            {
                                render=false;
                            }
                        }
                        
                        if(render)
                        {
                            {
                                //строка в гриде
                                c.RowIndex=RenderingRowIndex-1;
                                //порядковый номер строки
                                c.RowNumber=RenderingRowIndex;
                                c.Index=j;
                                c.ColumnIndex=c.Index;
                                j++;

                                Border b=null;
                                b=CellBodyCreate(c);
                                list.Add(b);

                                {
                                    if(LineToolTipMode == 1)
                                    {
                                        if(!CellBodyContentCurrent.IsNullOrEmpty())
                                        {
                                            b.ToolTip=CellBodyContentCurrent;
                                        }                                    
                                    }

                                    if(LineToolTipMode == 2)
                                    {
                                        var t=OnViewItem.Invoke(RenderingRow,Columns).ToString();
                                        if(!t.IsNullOrEmpty())
                                        {
                                            b.ToolTip=t;
                                        }                                    
                                    }
                                }

                                {
                                    GridBodyContainer.Children.Add(b);
                                    Grid.SetRow(b, c.RowIndex);
                                    Grid.SetColumn(b, c.ColumnIndex);

                                    if(!RenderFirst)
                                    {
                                        CellAddToCache(b,c.RowNumber);
                                    }
                                    
                                }
                             
                                if(c.OnRender != null)
                                {
                                    var elementList=c.OnRender.Invoke(RenderingRow,b);
                                    if(elementList != null)
                                    {
                                        foreach (Border b2 in elementList)
                                        {
                                            b2.Tag = b.Tag;
                                            {
                                                b2.MouseDown += CellOnMouseDown;
                                                b2.MouseUp += CellOnMouseUp;
                                            }

                                            GridBodyContainer.Children.Add(b2);
                                            Grid.SetRow(b2, c.RowIndex);
                                            Grid.SetColumn(b2, c.ColumnIndex);                                            
                                        }
                                    }
                                }

                            }

                        }
                    }
                    RowStylersProcess(list);  
                }
            }
        }

        private Dictionary<int,List<Border>> CellList {get;set;}
        private void CellAddToCache(Border b, int rowIndex)
        {
            if(!CellList.ContainsKey(rowIndex))
            {
                var row=new List<Border>();
                CellList.Add(rowIndex,row);
            }
            CellList[rowIndex].Add(b);

        }

        private bool CellCheck(int rowIndex, string path)
        {
            var result=false;
            
            var k=$"index={rowIndex}|path={path}";

            if(CellList.ContainsKey(rowIndex))
            {
                var row=CellList[rowIndex];
                foreach(Border b in row)
                {
                    var tag=b.Tag.ToString();
                    if(tag == k)
                    {
                        result=true;
                        break;
                    }
                }
            }
               
            return result;
        }

        public void CellBodyDestruct()
        {
            if(RenderingRowIndex > 0)
            {
                if(CellList.ContainsKey(RenderingRowIndex))
                {
                    var row=CellList[RenderingRowIndex];
                    CellList.Remove(RenderingRowIndex);

                    int j=0;
                    foreach(Border b in row)
                    {
                        GridBodyContainer.Children.Remove(b);
                    }
                }
            }
        }

        private int IndexViewFirst{get;set;}
        private int IndexViewLast{get;set;}
        private int IndexRenderFirst{get;set;}
        private int IndexRenderLast{get;set;}

        /// <summary>
        /// число строк во вьюпорте (физически в окне грида)
        /// </summary>
        private int IndexRenderStep{get;set;}

        /// <summary>
        /// отступ до начала блока фоновой обработки
        /// </summary>
        private int IndexRenderStep1{get;set;}

        /// <summary>
        /// длина блока фоновой обработки
        /// (смещение вперед от конца вьюпорта)
        /// </summary>
        private int IndexRenderStep2{get;set;}

        private void UpdateRenderedRows(double x)
        {
            if(Initialized)
            {
                //if(UseDynamicRendering)
                {
                    

                    Rendered = false;
                    RenderCheck(1);

                    /*
                    if (Rendered)
                    {
                        //down
                        if (VerticalScrollDirection > 0)
                        {
                            if ((IndexViewLast + IndexRenderStep1) > ViewFrameLast)
                            {
                                Rendered = false;
                                RenderCheck(1);
                            }
                        }

                        //up
                        if (VerticalScrollDirection < 0)
                        {
                            if ((IndexViewFirst - IndexRenderStep1) < ViewFrameFirst)
                            {
                                Rendered = false;
                                RenderCheck(-1);
                            }
                        }
                    }
                    */
                }

                
            }
        }

        private int IndexClearFirst {get;set;}
        private int IndexClearLast {get;set;}
        private int ViewFrameFirst {get;set;}
        private int ViewFrameLast {get;set;}

        private void RenderClear()
        {
            //IndexRenderStep=100;
            //IndexRenderStep2=50;

            //IndexRenderStep=30;

            IndexRenderStep1=10;
            //IndexRenderStep2=20;

            GridBodyContainer.Children.Clear();
            CellList.Clear();
            _RowCounter=0;
            ViewFrameFirst=0;
            ViewFrameLast=0;
            IndexViewFirst=0;
            IndexViewLast=IndexRenderStep;
            IndexRenderFirst=0;
            IndexRenderLast=IndexRenderStep;
            IndexClearFirst=0;
            IndexClearLast=0;
            LogRender="";
            Rendered=false;
            RenderFirst=true;
        }

        private async void RenderCheck(int i=0)
        {
            //RenderSetFrame(i);
            //RenderBlock();    
            
            {
                var lineHeight=ColumnHeightMin;
                {
                    if(VerticalScrollPosition>0)
                    {
                        IndexViewFirst=(int)((double)VerticalScrollPosition/(double)lineHeight);
                    }
                    IndexViewLast=IndexViewFirst+IndexRenderStep;
                }
            }

            RenderBlock2();

            MouseCursorRelease();
        }

        private async void RenderCheck01(object state)
        {
            RenderSetFrame(1);
            RenderBlock();
        }

        private async void RenderCheck0(int i=0)
        {

            Task.Run(() =>
	        {   
                //DebugLogRender($"r1");
                RenderSetFrame(1);                
	        }).ContinueWith(_ =>
	        {
                //DebugLogRender($"r2");
                RenderBlock();
	        }, TaskScheduler.FromCurrentSynchronizationContext()); 

            //   SynchronizationContext uiContext = SynchronizationContext.Current;
	        //Thread thread = new Thread(RenderCheck01);
	        //thread.Start(uiContext);

            //Thread t = new Thread(() => RenderCheck(i));
            //t.Start();

            //Task.Run(() =>
            //{
            //    RenderCheck();   
                
            //}).ContinueWith().Tas;
            //TaskScheduler.FromCurrentSynchronizationContext();        

            

            /*
             Task.Run(() => {
                RenderCheck(i);
            }).ContinueWith(() => {
                RenderCheck9();
            }).TaskScheduler.FromCurrentSynchronizationContext());
             */
        }

        private void RenderCheck9(Task t)
        {
        }

        private void RenderSetFrame(int i=0)
        {
            if(UseDynamicRendering)
            {
                int a=2;    

                //scroll down
                if(i > 0)
                {
                    IndexClearLast=IndexViewFirst-IndexRenderStep1;
                    IndexClearFirst=IndexClearLast-IndexRenderStep2;

                    IndexRenderFirst=IndexViewLast+IndexRenderStep1-a;
                    IndexRenderLast=IndexRenderLast+IndexRenderStep2+a;

                    ViewFrameFirst=IndexClearLast;
                    ViewFrameLast=IndexRenderLast;
                }

                //scroll up
                if(i < 0)
                {
                    IndexClearFirst=IndexViewLast+IndexRenderStep1;
                    IndexClearLast=IndexClearFirst+IndexRenderStep2;

                    IndexRenderLast=IndexViewFirst-IndexRenderStep1+a;
                    IndexRenderFirst=IndexRenderLast-IndexRenderStep2-a;

                    ViewFrameFirst=IndexRenderFirst;
                    ViewFrameLast=IndexClearLast;
                }

                if(i == 0)
                {
                    IndexRenderLast=IndexRenderLast+IndexRenderStep2;
                }
               
                {
                    if(IndexClearLast < 0)
                    {
                        IndexClearLast=0;
                    }

                    if(IndexClearFirst < 0)
                    {
                        IndexClearFirst=0;
                    }

                    if(ViewFrameFirst < 1)
                    {
                        ViewFrameFirst=1;
                    }
                }
            }
            else
            {
                var rows=GridItemsSorted.Count;
                IndexRenderLast=rows;
            }

            //DebugLogRender($"render_set [{i}] [{IndexRenderFirst}]-[{IndexRenderLast}]");
            var d=" ";
            if(i > 0)
            {
                d="v";
                DebugLogRender($"render_set [{d}] C[{IndexClearFirst}]-[{IndexClearLast}]  V[{IndexViewFirst}]-[{IndexViewLast}]  R[{IndexRenderFirst}]-[{IndexRenderLast}]");
            }
            if(i < 0)
            {
                d="^";
                DebugLogRender($"render_set [{d}] R[{IndexRenderFirst}]-[{IndexRenderLast}]  V[{IndexViewFirst}]-[{IndexViewLast}]  C[{IndexClearFirst}]-[{IndexClearLast}]");
            }
        }

        private bool Rendered {get;set;}
        private bool RenderFirst {get;set;}
        private void RenderBlock2()
        {
            var s="";
            RowsRendered=0;

            if(RenderFirst)
            {
                RenderingRowIndex=0;
                foreach(Dictionary<string, string> row in GridItemsSorted)
                {
                    RenderingRowIndex++;
                    RenderingRow=row;

                     CellRowConstruct();
                     CellBodyConstruct();
                }
                RenderFirst=false;
            }

            //clear
            {
                var first=0;
                var last=IndexViewFirst-IndexRenderStep1;

                RenderingRowIndex=0;
                foreach(Dictionary<string, string> row in GridItemsSorted)
                {
                    RenderingRowIndex++;
                    RenderingRow=row;

                        if(RenderingRowIndex >= first && RenderingRowIndex <= last)
                        {
                             CellBodyDestruct();
                        }
                    
                }

                s=$"{s} c1[{first}]-[{last}]";
            }

            //clear
            {
                var first=IndexViewLast+IndexRenderStep1;
                var last=_RowCounter;

                RenderingRowIndex=0;
                foreach(Dictionary<string, string> row in GridItemsSorted)
                {
                    RenderingRowIndex++;
                    RenderingRow=row;

                        if(RenderingRowIndex >= first && RenderingRowIndex <= last)
                        {
                             CellBodyDestruct();
                        }
                    
                }
                s=$"{s} c2[{first}]-[{last}]";
            }

            //render
            {
                var first=IndexViewFirst-IndexRenderStep1;
                var last=IndexViewLast+IndexRenderStep1;

                if(first < 1)
                {
                    first = 1;// first+IndexRenderStep1;
                    last=last+IndexRenderStep1;
                }

                ViewFrameFirst=first;
                ViewFrameLast=last;

                {
                    if(
                        IndexViewFirst < 100 
                        && VerticalScrollDirection < 0
                        && VerticalScrollDelta > IndexRenderStep
                        && first < 100 
                        && first > 0
                    )
                    {
                        first=1;
                    }
                }

                RenderingRowIndex=0;
                foreach(Dictionary<string, string> row in GridItemsSorted)
                {
                    RenderingRowIndex++;
                    RenderingRow=row;

                        if(RenderingRowIndex >= first && RenderingRowIndex <= last)
                        {
                              CellBodyConstruct();
                              RowsRendered++;
                        }
                    
                }
                s=$"{s} r[{first}]-[{last}]";
            }

            Rendered=true;

            DebugLogRender($"render2 {s}");
        }

        private void RenderBlock()
        {
            //номер строки, нумерация с 1
            RenderingRowIndex=0;
            RowsRendered=0;
            foreach(Dictionary<string, string> row in GridItemsSorted)
            {
                RenderingRowIndex++;
                RenderingRow=row;

                //mode 1
                if(false)
                {
                    {
                        if(RenderingRowIndex >= IndexRenderFirst && RenderingRowIndex <= IndexRenderLast)
                        {
                            //CellRowConstruct();
                            CellBodyConstruct();
                            RowsRendered++;
                        }
                    }
                }

                //mode2
                if(false)                
                {
                    if(RenderFirst)
                    {
                            CellRowConstruct();
                            //CellBodyConstruct(RenderFirst);
                    }

                    //{
                    //    if(RenderingRowIndex >= IndexViewFirst && RenderingRowIndex <= IndexViewLast)
                    //    {
                    //        //CellRowConstruct();
                    //        CellBodyConstruct();
                    //        RowsRendered++;
                    //    }
                    //}

                    {
                        if(RenderingRowIndex >= IndexRenderFirst && RenderingRowIndex <= IndexRenderLast)
                        {
                            //CellRowConstruct();
                            CellBodyConstruct();
                            RowsRendered++;
                        }
                    }

                    {
                        if(RenderingRowIndex >= IndexClearFirst && RenderingRowIndex <= IndexClearLast)
                        //if(RenderingRowIndex > 0  && RenderingRowIndex <= IndexClearLast)
                        {
                            CellBodyDestruct();
                        }
                    }
                }
              
            }

            Rendered=true;
            RenderFirst=false;
        }

        public void RenderTotals()
        {
            if(UseTotals)
            {
                GridTotalsContainer.Children.Clear();

                int j = 0;
                foreach(DataGridHelperColumn c in Columns)
                {
                    var render = false;
                    var k = c.Path;

                    if(ColumnCheckVisible(c))
                    {
                        render = true;
                    }

                    if(render)
                    {
                        var b = CellTotalCreate(c);

                        GridTotalsContainer.Children.Add(b);
                        Grid.SetRow(b, 0);
                        Grid.SetColumn(b, j);
                        j++;
                    }
                }

                GridTotalsBlock.Visibility=Visibility.Visible; 
            }
            else
            {
                GridTotalsBlock.Visibility = Visibility.Collapsed;
            }
        }

        public void DebugShowColumnsInfo()
        {
            var d = new LogWindow("", "Конфигурация вкладок");
            d.AutoUpdateInterval = 500;
            d.Show();
            d.SetOnUpdate(() =>
            {
                var s = "";
                s = $"{s}GIRD_BOX-3";

                if (Initialized)
                {
                    var dir=" ";
                    {
                        if(VerticalScrollDirection > 0)
                        {
                            dir="v";
                        }
                        if(VerticalScrollDirection < 0)
                        {
                            dir="^";
                        }
                    }

                    {
                        //s = $"{s}\n init=[{Initialized}]  ";
                        s = $"{s}\n ";
                        s = $"{s}\n block  frame_h=[{GridBodyScroller.ActualHeight}] canvas_h=[{GridScrollVerticalContent.Height}]";
                        //s = $"{s}\n row    row_h=[{ColumnHeightMin}] ";
                        //s = $"{s}\n dir    pos0=[{VerticalScrollPositionOld}] pos=[{VerticalScrollPosition}] dir=[{dir}] lines=[{VerticalScrollDelta}]";                        
                        s = $"{s}\n dir    scroll=[{VerticalScrollPosition}] pos=[{IndexViewFirst}] dir=[{dir}] lines=[{VerticalScrollDelta}]";                        
                        s = $"{s}\n items  ds=[{GridItems.Count}] filtered=[{GridItemsSorted.Count}] ord_field=[{SortColumn.Path}] ord_dir=[{SortDirection.ToString()}]";
                        s = $"{s}\n render s0=[{IndexRenderStep}] s1=[{IndexRenderStep1}] total_rows=[{_RowCounter}] rendered=[{RowsRendered}] cached=[{CellList.Count}]";
                        //s = $"{s}\n render s0=[{IndexRenderStep}] s1=[{IndexRenderStep1}] s2=[{IndexRenderStep2}] rendered=[{RowsRendered}]  _RowCounter=[{_RowCounter}] cached=[{CellList.Count}]";
                        //s = $"{s}\n        C[{IndexClearFirst}]-[{IndexClearLast}] V[{IndexViewFirst}]-[{IndexViewLast}] R[{IndexRenderFirst}]-[{IndexRenderLast}]";
                        //s = $"{s}\n frame  [{ViewFrameFirst}]-[{ViewFrameLast}]";
                        
                    }

                    

                    {
                        s = $"{s}\n RENDER";
                        s = $"{s}\n {LogRender}";
                    }

                    if (Columns.Count > 0)
                    {
                        {
                            s = s.Append($"{ColumnAutowidthLog}", true);
                            s = s.Append($" ", true);

                            {
                                s = $"{s}\n";
                                s = $"{s} {"#".ToString().SPadLeft(2)} | ";
                                s = $"{s} {"PATH".ToString().SPadLeft(15)} | ";
                                s = $"{s} {"HEADER".ToString().SPadLeft(15)} | ";
                                s = $"{s} {"w1".ToString().SPadLeft(2)} | ";
                                s = $"{s} {"w2".SPadLeft(3)} | ";
                                s = $"{s} {"wr".ToString().SPadLeft(3)} | ";
                                s = $"{s} {"wс".ToString().SPadLeft(3)} | ";
                                s = $"{s} {"wa".ToString().SPadLeft(3)} | ";
                                s = $"{s} {"".ToString().SPadLeft(20)}";
                            }

                            var j = 0;
                            var at=0;
                            var gridContainerWidth = GetGridContainderWidth();
                            foreach (DataGridHelperColumn c in Columns)
                            {
                                j++;
                                var include = false;

                                var h = c.Header;
                                var p = c.Path;
                                //фактическая, пикс
                                var w = 0;
                                var l = c.AutoWidthLog;

                                if(ColumnCheckVisible(c))
                                {
                                    include = true;
                                    w= CellGetWidth(c.Path);
                                }

                                //расчетная в пикс
                                var a = (int)c.Width;
                                at=at+w;
                                //обратно пересчитанная
                                var b = (int)((double)w / (double)ColumnSymbolWidth);

                                if (include)
                                {
                                    s = $"{s}\n";
                                    s = $"{s} {j.ToString().SPadLeft(2)} | ";
                                    s = $"{s} {p.ToString().SPadLeft(15)} | ";
                                    s = $"{s} {h.ToString().SPadLeft(15)} | ";
                                    s = $"{s} {c.Width2.ToString().SPadLeft(2)} | ";
                                    s = $"{s} {b.ToString().SPadLeft(3)} | ";
                                    s = $"{s} {c.WidthRelative.ToString().SPadLeft(3)} | ";
                                    s = $"{s} {a.ToString().SPadLeft(3)} | ";
                                    s = $"{s} {w.ToString().SPadLeft(3)} | ";
                                    s = $"{s} {l.ToString().SPadLeft(20)}";
                                }
                            }

                            {
                                var t = "";
                                t = t.Append($" ", true);
                                t = t.Append($" w1 -- заданная ширина, симв. ", true);
                                t = t.Append($" w2 -- фактическая ширина, симв.", true);
                                t = t.Append($" wr -- относительная ширина, доля [1-1000]", true);
                                t = t.Append($" wc -- заданная ширина, пикс.", true);
                                t = t.Append($" wa -- фактическая ширина, пикс.", true);
                                s = s.Append($"{t}");
                            }

                            {
                                //fact=[{at}]
                                var vsp = GridScrollVerticalScroller.VerticalOffset;
                                s = $"{s}\n grid [{gridContainerWidth}]x[{GridBodyContainer.ActualHeight}] line=[{LineWidth}] vscroll=[{vsp}]";
                                s = $"{s}\n sorting [{SortColumn.Path}]:[{SortDirection}]";
                                s = $"{s}\n selection index=[{SelectedItemIndex}] column=[{SelectedColumn.Path}] cell=[{SelectedCell.Tag}] [{SelectedItem.CheckGet("_ROWNUMBER")}]";

                                

                            }
                        }

                        //var msg=s;
                        //var d = new LogWindow($"{msg}", "Конфигурация колонок" );
                        //d.ShowDialog();
                    }

                    {
                        s = $"{s}\n INIT";
                        s = $"{s}\n {LogInit}";
                    }

                    {
                        s = $"{s}\n WORK";
                        s = $"{s}\n {Log}";
                    }
                }

                return s;
            });
        }

        private string LogRender {get;set;}
        private void DebugLogRender(string message)
        {
            //DebugLog($"    {message}");
            var today=DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss_ffffff");
            LogRender=LogRender.Append($"{today} {message}",true);
            LogRender=LogRender.Crop(800);
        }

        private string LogInit {get;set;}
        private void DebugLogInit(string message)
        {
            //DebugLog($"    {message}");
            var today=DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss_ffffff");
            LogInit=LogInit.Append($"{today} {message}",true);
            LogInit=LogInit.Crop(800);
        }

        private void CellHeaderWidthInit()
        {
            int colIndex=0;
            foreach(DataGridHelperColumn c in Columns)
            {
                c.Index=colIndex;
                //c.ColumnIndex=colIndex;
                colIndex++;

                var render=false;
                var k=c.Path;

                if(ColumnCheckVisible(c))
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
        }

        public int GetGridContainderWidth()
        {
            var gridContainerWidth=(int)GridContainer.ActualWidth;

            //if(!Initialized || gridContainerWidth == 0)
            if(gridContainerWidth == 0)
            {
                gridContainerWidth=1200;
            }
            return gridContainerWidth;
        }

        public void CellHeaderWidthProcess(bool noUpdate=false)
        {
            DebugLog("width_1");
            var s1="";
            var s2="";
            
            {
                ColumnAutowidthLog="";
                var gridContainerWidth=GetGridContainderWidth();
                
                DebugLogInit($"CellHeaderWidthProcess noUpdate=[{noUpdate}] gridContainerWidth=[{gridContainerWidth}]");

                ColumnTotalWidth=0;
                foreach(DataGridHelperColumn c in Columns)
                {
                    var render=false;
                    var k=c.Path;

                    if(ColumnCheckVisible(c))
                    {
                        render=true;
                        ColumnsCount++;
                    }

                    if(render)
                    {
                        ColumnTotalWidth=ColumnTotalWidth+c.Width2;
                    }
                }
                
                {
                    ColumnAutowidthLog = ColumnAutowidthLog.Append($"грид: {Name}",true);
                    ColumnAutowidthLog = ColumnAutowidthLog.Append($"режим: {ColumnWidthMode} ширина символа: {ColumnSymbolWidth} суммарная ширина: {ColumnTotalWidth}",true);
                    ColumnAutowidthLog = ColumnAutowidthLog.Append($"контейнер: w={gridContainerWidth} scroll={VerticalScrollPosition}",true);                    
                }

                var currentMode=ColumnWidthMode;
                if(currentMode == GridBox.ColumnWidthModeRef.Compact)
                {
                    foreach(DataGridHelperColumn c in Columns)
                    {
                        var render=false;
                        var k=c.Path;

                        if(ColumnCheckVisible(c))
                        {
                            render=true;
                        }

                        if(render)
                        {
                            c.WidthRelative = (int) (((double) (c.Width2) / (double) (ColumnTotalWidth)) * 1000);
                            s1=s1.Append($" {c.WidthRelative}");
                        }
                    }
                }

                foreach(DataGridHelperColumn c in Columns)
                {
                    var render=false;
                    var k=c.Path;

                    if(ColumnCheckVisible(c))
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
                                //w=(int)w2-2;
                                w=(int)w2;
                                //w = w + ColumnWidthOffset;
                                //w = w + 2;
                                break;

                            case GridBox.ColumnWidthModeRef.Full:
                                w=(int)Math.Round((double)(c.Width2*ColumnSymbolWidth),0);
                                break;
                        }

                        c.Width=w;
                        c.MinWidth=w;
                        c.MaxWidth = 2000;
                        
                        s2=s2.Append($" {w}");

                        if(noUpdate == false)
                        {
                            CellSetWidth(k,w);
                            //CellUpdateWidth(k,w);
                        }                        
                    }
                }
            }
            
            GridFrameUpdate();

            DebugLogInit($"    {s1}");
            DebugLogInit($"    {s2}");                       

            //if(noUpdate == false)
            {                
                CellUpdateWidthAll();
                //{
                //    var interval = new Common.Timeout(
                //        1,
                //        () => {
                //            CellUpdateWidthAll();
                //        }
                //    );
                //    interval.SetIntervalMs(500);
                //    interval.Run();
                //}
            }
        }

        private void GridFrameUpdate()
        {
            if(GridItemsSorted.Count > 0)
            {
                IndexRenderStep=(int)Math.Round(GridBodyScroller.ActualHeight/ColumnHeightMin);
            }
            else
            {
                IndexRenderStep=30;
            }
            
            DebugLog($"GridFrameUpdate frame rows=[{IndexRenderStep}]");
        }
      
        public Border CellHeaderCreate(DataGridHelperColumn c)
        {
            var columnSpacer = false;
            var k=c.Path;
            var tag=$"index={0}|path={k}";

            if(k == "_")
            {
                columnSpacer = true;
            }

            var b= new Border();
            b.Name=$"cell_header_{k}";
            b.Tag=tag;

            if(!columnSpacer)
            {
                b.Style = StyleGet("GridboxCellHeaderBorder");
            }
            else
            {
                b.Style = StyleGet("GridboxCellHeaderStaticBorder");
            }
                
            b.Width=CellGetWidth(k);

            var g=new Grid();
            g.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            {
                var cd=new ColumnDefinition();
                cd.Width=new GridLength(1, GridUnitType.Star);   
                g.ColumnDefinitions.Add(cd);
            }

            if(!columnSpacer)
            {
                {
                    var cd = new ColumnDefinition();
                    cd.Width = new GridLength(sorterWidth, GridUnitType.Pixel);
                    g.ColumnDefinitions.Add(cd);
                }
                {
                    var cd = new ColumnDefinition();
                    cd.Width = new GridLength(resizerWidth, GridUnitType.Pixel);
                    g.ColumnDefinitions.Add(cd);
                }
                {
                    var rd = new RowDefinition();
                    rd.Height = new GridLength(1, GridUnitType.Star);
                    g.RowDefinitions.Add(rd);
                }
            }
           
            //title
            {
                var r=new Border();                
                r.Name=$"caption_{k}";
                r.Style= StyleGet("GridboxCellHeaderTextBorder");

                var ts=new TextBlock();
                ts.Text=c.Header;              
                ts.ToolTip=ColumnHeaderMakeTooltip(c);               
                ts.Style=StyleGet("GridboxCellHeaderText");

                r.Child=ts;
                r.MouseDown += ColumnHeaderOnClick;
                r.Tag=tag;


                g.Children.Add(r);
                Grid.SetRow(r, 0);
                Grid.SetColumn(r, 0);
            }

            if(!columnSpacer)
            {
                //sorter            
                {
                    var r = new Border();
                    r.Width = sorterWidth;
                    r.Name = $"sorter_{k}";
                    r.Style = StyleGet("GridboxCellHeaderSorterBorder");

                    r.MouseDown += ColumnSorterOnClick;
                    r.Tag = tag;

                    var ts = new TextBlock();
                    ts.Text = "";

                    r.Child = ts;                    

                    g.Children.Add(r);
                    Grid.SetRow(r, 0);
                    Grid.SetColumn(r, 1);
                }

                //resizer
                {
                    var r = new Border();
                    r.Width = resizerWidth;
                    r.Name = $"resizer_{k}";
                    r.Style = StyleGet("GridboxCellHeaderResizerBorder");

                    r.MouseMove += ColumnResizerOnMouseMove;
                    r.MouseEnter += ColumnHeaderOnMouseOver;
                    r.MouseLeave += ColumnHeaderOnMouseOut;

                    g.Children.Add(r);
                    Grid.SetRow(r, 0);
                    Grid.SetColumn(r, 2);
                }
            }

            b.Child=g;
            
            if(!ColumnStack.ContainsKey(k))
            {
                ColumnStack.Add(k,b);
            }

            {
                //b.MouseDown += CellOnMouseDown;
                //b.MouseUp += CellOnMouseUp;
            }

            return b;
        }

       

        private string CellHeaderGetTooltip(DataGridHelperColumn c)
        {
            string result="";

            if(!c.Doc.IsNullOrEmpty())
            {
                result=$"{c.Doc}";
            }

            return result;
        }

        /// <summary>
        /// 0=none,1=header,2=body
        /// </summary>
        private int MouseOverArea {get;set;}

        private bool MouseButtonPressed {get;set;}
        /// <summary>
        /// 1=left,2=right
        /// </summary>
        private int MouseButtonType {get;set;}
        private void CellOnMouseDown(object sender, MouseButtonEventArgs e)
        {
            DebugLog($"CLICK B_DN [{MouseButtonType}] [{CellClickCount}]");
            //if(!MouseButtonPressed)
            {
                MouseButtonPressed=true;
                if(e.LeftButton == MouseButtonState.Pressed)
                {
                    MouseButtonType=1;
                }
                else if(e.RightButton == MouseButtonState.Pressed)
                {
                    MouseButtonType=2;
                }
                if(!ColumnResizerInProgress)
                {
                    CellBodyOnClick(sender, e);
                }
            }
            e.Handled = true;
        }

        private void CellOnMouseUp(object sender, MouseButtonEventArgs e)
        {
            //DebugLog($"CLICK B_UP [{MouseButtonType}] [{CellClickCount}]");
            if(MouseButtonPressed)
            {
                if(!ColumnResizerInProgress)
                {
                    //CellOnClick(sender, e);
                }
                MouseButtonType = 0;
                MouseButtonPressed = false;
            }
            //e.Handled = true;
        }

        private void ColumnHeaderOnMouseOut(object sender, MouseEventArgs e)
        {
            MouseOverArea=0;
            if(!ColumnResizerInProgress)
            {                
                MouseCursorRelease();
            }
        }

        private bool GridSorterBlocked {get;set;}
        private void GridSorterRelease()
        {
            //GridSorterBlocked=false;
            SorterReleaseTimeout.Restart();
        }
        private void GridSorterBlock()
        {
            GridSorterBlocked=true;
        }


        private void ColumnHeaderOnMouseOver(object sender, MouseEventArgs e)
        {
            MouseOverArea=1;
            Mouse.OverrideCursor = Cursors.SizeWE;
            ColumnResizerInProgress = true;
            //GridSorterBlock();
        }

        private void MouseCursorRelease()
        {
            Mouse.OverrideCursor = null;
            //GridSorterRelease();
        }

        private void ColumnResizerOnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if(!ColumnResizerInProgress)
                {
                    ColumnResizerInProgress=true;
                    ColumnResizerKey=ObjectGetKey(sender,"resizer_"); 
                    ColumnResizerSetStyle(ColumnResizerKey, "GridboxCellHeaderResizerSelectedBorder");   
                    Mouse.OverrideCursor=Cursors.SizeWE;
                }
            }
            else
            {
                if(ColumnResizerInProgress)
                {
                    MouseButtonPressed=false;
                    ColumnResizerSetStyle(ColumnResizerKey, "GridboxCellHeaderResizerBorder");
                    ColumnResizerStopResize();                    
                    MouseCursorRelease();
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
            
                ColumnResizeTimeout.Restart();
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

                    try
                    {
                        var g = (Grid)c.Child;
                        if(g != null)
                        {
                            foreach(object o in g.Children)
                            {
                                var b = (Border)o;
                                var n = b.Name;
                                if(n.IndexOf("sorter_") > -1)
                                {
                                    var t = (TextBlock)b.Child;
                                    if(t != null)
                                    {
                                        switch(mode)
                                        {
                                            case 1:
                                                {
                                                    switch(SortDirection)
                                                    {
                                                        case ListSortDirection.Ascending:
                                                        t.Text = sorterSymbolAsc;
                                                        break;

                                                        case ListSortDirection.Descending:
                                                        t.Text = sorterSymbolDesc;
                                                        break;
                                                    }
                                                }
                                                break;

                                            default:
                                            case 0:
                                                {
                                                    t.Text = "";
                                                }
                                                break;
                                        }

                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {

                    }

                    
                }
            }
        }

        private void ColumnResizerSetStyle(string path, string style)
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
                                b.Style=StyleGet(style);
                            }
                        }
                    }
                }
            }
        }

        private void ColumnResizerResetStyle()
        {
            foreach(DataGridHelperColumn c in Columns)
            {
                ColumnResizerSetStyle(c.Path, "GridboxCellHeaderResizerBorder");
            }

            /*
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
            */
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

        private void GridOnMouseMove(object sender, MouseEventArgs e)
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
            else
            {
                if(MouseOverArea != 1)
                {
                    MouseCursorRelease();
                }                
            }
        }

        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        private Style CellBodyGetDefaultStyle(DataGridHelperColumn c)
        {
            var result= StyleGet("GridboxCellBodyBorder");
            switch(c.ColumnType)
            {
                case ColumnTypeRef.Integer:
                case ColumnTypeRef.Double:
                    result=StyleGet("GridboxCellBodyDigitBorder");
                    break;

                default:
                    result=StyleGet("GridboxCellBodyBorder");
                    break;
            }
            return result;
        }

        private Style CellBodyGetSelectedStyle(DataGridHelperColumn c)
        {
            var result= StyleGet("GridboxCellBodyBorder");
            switch(c.ColumnType)
            {
                case ColumnTypeRef.Integer:
                case ColumnTypeRef.Double:
                    result=StyleGet("GridboxCellBodyDigitSelectedBorder");
                    break;

                default:
                    result=StyleGet("GridboxCellBodySelectedBorder");
                    break;
            }
            return result;
        }

        public Border CellBodyCreate(DataGridHelperColumn column, int mode=0)
        {
            CellBodyContentCurrent="";
            var k=column.Path;

            /*
                mode
                    0-normal, readonly
                    1-selected, can copy
              
                grid
                    border (row,col)  ->CellProcessClick
                        CheckBox
                            ->onclick
                        TextBlock
             */

            var b= new Border();
            {
                b.Name=$"cell_body_{k}";
                b.Tag=$"index={RenderingRowIndex}|path={k}";
                b.Style=CellBodyGetDefaultStyle(column);
                b.Height = 18;
            }

            {
                var ctl = CellBodyControlCreate(column, mode, RenderingRowIndex, RenderingRow);
                if(ctl!=null)
                {
                    b.Child = ctl;
                }
            }

            {
                b.MouseDown += CellOnMouseDown;
                b.MouseUp += CellOnMouseUp;
            }

            return b;
        }

        private UIElement CellBodyControlCreate(DataGridHelperColumn c, int mode = 0, int rowIndex=0, Dictionary<string, string> row=null)
        {
            //RenderingRowIndex -> rowIndex
            //RenderingRow -> row
            UIElement result = null;
            var k = c.Path;
            var tag = $"index={rowIndex}|path={k}";

            switch(c.ColumnType)
            {
                case ColumnTypeRef.Boolean:
                    {
                        var v = CellBodyGetContent(c, row).ToBool();
                        var ctl = new CheckBox();
                        ctl.IsChecked = v;
                        ctl.IsEnabled = false;
                        ctl.Style = StyleGet("CheckBoxStyle");

                        if(c.Editable)
                        {
                            ctl.Name = $"cell_body_{k}_control";
                            ctl.Tag = tag;
                            ctl.IsEnabled = true;
                            ctl.Click += CheckOnClick;
                        }
                        result = ctl;
                    }
                    break;

                default:
                    {
                        var v = CellBodyGetContent(c, row);
                        CellBodyContentCurrent = v;

                        var v0 = "";

                        {
                            if(RenderFirst)
                            {
                                v = "";
                            }
                        }

                        if(mode == 0)
                        {
                            var ctl = new TextBlock();
                            ctl.Text = $"{v0}{v}";
                            ctl.Style = StyleGet("GridboxCellBodyText");
                            result = ctl;
                        }

                        if(mode == 1)
                        {
                            var ctl = new TextBox();
                            ctl.Text = $"{v0}{v}";
                            ctl.Style = StyleGet("GridboxCellBodyTextBox");

                            ctl.Name = $"cell_body_{k}_control";
                            ctl.Tag = tag;

                            ctl.PreviewMouseDown += Ctl_PreviewMouseDown;
                            ctl.MouseDown += Ctl_MouseDown;
                            ctl.MouseUp += Ctl_MouseUp;
                            //ctl.MouseDoubleClick += Ctl_MouseDoubleClick;
                            
                            result = ctl;
                        }
                    }
                    break;
            }
            
            return result;
        }

        private void Ctl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var cellInfo = GetCellInfo(sender,1);
            var cellPath = cellInfo.CheckGet("PATH");
            var cellIndex = cellInfo.CheckGet("INDEX").ToInt();
            var cellType = cellInfo.CheckGet("TYPE");

            var s = $"cellPath=[{cellPath}] cellIndex=[{cellIndex}] cellType=[{cellType}]";
            DebugLog($"Ctl_PreviewMouseDown {s}");

            //var selected = false;
            //{
            //    var cellInfo = GetCellInfo(sender, 1);
            //    var cellIndex = cellInfo.CheckGet("INDEX").ToInt();
            //    if(SelectedItemIndex == cellIndex)
            //    {
            //        selected = true;
            //    }
            //}

            //CellClickDuration = CellClickDuration+(int)CellClickProfiler.GetDelta();
            //DebugLog($"CLICK C_DN0 [{MouseButtonType}] [{CellClickCount}] [{CellClickDuration}]");
            //if(CellClickDuration > CellClickDurationTheshhold)
            ////if(!CellControllClicked)
            //{
            //    CellControllClicked = true;
            //    CellProcessClickDouble();
            //}

            CellProcessClickDouble();
            e.Handled = false;
        }

        private void Ctl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var cellInfo = GetCellInfo(sender,1);
            var cellPath = cellInfo.CheckGet("PATH");
            var cellIndex = cellInfo.CheckGet("INDEX").ToInt();
            var cellType = cellInfo.CheckGet("TYPE");

            var s = $"cellPath=[{cellPath}] cellIndex=[{cellIndex}] cellType=[{cellType}]";
            DebugLog($"Ctl_MouseDown {s}");

            //var selected = false;
            //{
            //    var cellInfo = GetCellInfo(sender, 1);
            //    var cellIndex = cellInfo.CheckGet("INDEX").ToInt();
            //    if(SelectedItemIndex == cellIndex)
            //    {
            //        selected = true;
            //    }
            //}

            //CellClickDuration = CellClickDuration+(int)CellClickProfiler.GetDelta();
            //DebugLog($"CLICK C_DN [{MouseButtonType}] [{CellClickCount}] [{CellClickDuration}]");
            //if(CellClickDuration > CellClickDurationTheshhold)
            ////if(!CellControllClicked)
            //{
            //    CellControllClicked = true;
            //    CellProcessClickDouble();
            //}

            CellProcessClickDouble();
            e.Handled = false;
        }

        private void Ctl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var cellInfo = GetCellInfo(sender,1);
            var cellPath = cellInfo.CheckGet("PATH");
            var cellIndex = cellInfo.CheckGet("INDEX").ToInt();
            var cellType = cellInfo.CheckGet("TYPE");

            var s = $"cellPath=[{cellPath}] cellIndex=[{cellIndex}] cellType=[{cellType}]";
            DebugLog($"Ctl_MouseUp {s}");

            //var selected = false;
            //{
            //    var cellInfo = GetCellInfo(sender, 1);
            //    var cellIndex = cellInfo.CheckGet("INDEX").ToInt();
            //    if(SelectedItemIndex == cellIndex)
            //    {
            //        selected = true;
            //    }
            //}


            //CellClickDuration = CellClickDuration+(int)CellClickProfiler.GetDelta();
            //DebugLog($"CLICK C_UP [{MouseButtonType}] [{CellClickCount}] [{CellClickDuration}]");
            //if(CellClickDuration > CellClickDurationTheshhold)
            ////if(!CellControllClicked)
            //{
            //    CellControllClicked = true;
            //    CellProcessClickDouble();
            //}

            CellProcessClickDouble();
            e.Handled = false;
        }

        private void Ctl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DebugLog($"CLICK C_DBL [{MouseButtonType}] [{CellClickCount}] ");
            e.Handled = false;
        }

        public Border CellTotalCreate(DataGridHelperColumn column, int mode = 0)
        {           
            var k = column.Path;

            /*
                mode
                    0-normal, readonly
                    1-selected, can copy
              
                grid
                    border (row,col)  ->CellProcessClick
                        CheckBox
                            ->onclick
                        TextBlock
                        
             
             */

            var b = new Border();
            {
                b.Name = $"cell_total_{k}";
                b.Tag = $"index={RenderingRowIndex}|path={k}";
                b.Style = CellBodyGetDefaultStyle(column);
            }

            {
                var ctl = CellTotalControlCreate(column, mode);
                if(ctl != null)
                {
                    b.Child = ctl;
                }
            }

            return b;
        }

        private UIElement CellTotalControlCreate(DataGridHelperColumn c, int mode = 0, int rowIndex = 0, Dictionary<string, string> row = null)
        {
            //RenderingRowIndex -> rowIndex
            //RenderingRow -> row
            UIElement result = null;
            var k = c.Path;
            switch(c.ColumnType)
            {
                default:
                    {
                        var v = CellTotalGetContent(c, row);
                        var v0 = "";

                        if(mode == 0)
                        {
                            var ctl = new TextBlock();
                            ctl.Text = $"{v0}{v}";
                            ctl.Style = StyleGet("GridboxCellBodyText");
                            result = ctl;
                        }

                        if(mode == 1)
                        {
                            var ctl = new TextBox();
                            ctl.Text = $"{v0}{v}";
                            ctl.Style = StyleGet("GridboxCellBodyTextBox");
                            result = ctl;
                        }
                    }
                    break;
            }

            return result;
        }

        private void CheckOnClick(object sender, RoutedEventArgs e)
        {
            var checkCtl=(CheckBox)sender;
            var v="0";
            var t=checkCtl.Tag.ToString();
            var n=checkCtl.Name;

            if((bool)checkCtl.IsChecked)
            {
                v="1";
            }
            SetItemValue(n,t,v);

            {
                 var p=GetCellInfo(n,t);
                 var index=p.CheckGet("INDEX").ToInt();
                 var path=p.CheckGet("PATH").ToString();
                 var c=ColumnGetByPath(path);
                 if(c != null)
                 {
                     if(c.OnClickAction!=null)
                     {
                        var el=new FrameworkElement();
                        el = (FrameworkElement)sender;
                        var row=RowGetByIndex(index);
                        c.OnClickAction.Invoke(row,el);
                     }
                 }
            }
        }

        private void SetItemValue(string name, string tag, string value)
        {
            var p=GetCellInfo(name,tag);
            var index=p.CheckGet("INDEX").ToInt();
            var path=p.CheckGet("PATH").ToString();

            if(index > 0 && !path.IsNullOrEmpty())
            {
                var row=RowGetByIndex(index);
                row.CheckAdd(path,value);
                //int j=0;
                //if(GridItems.Count > 0)
                //{
                //    foreach(Dictionary<string,string> row in GridItems)
                //    {
                //        j++;
                //        if(j==index)
                //        {
                //            row.CheckAdd(path,value);
                //            break;
                //        }
                //    }
                //}
            }
        }

        public Dictionary<string,string> RowGetByIndex(int index)
        {
            var result=new Dictionary<string,string>();

            int j=0;
            if(GridItems.Count > 0)
            {
                foreach(Dictionary<string,string> row in GridItemsSorted)
                {
                    j++;
                    if(j==index)
                    {
                        result=row;
                        break;
                    }
                }
            }

            return result;
        }

        private Dictionary<string,string> RowItemGetByIndex(int index)
        {
            var result=new Dictionary<string,string>();

            if(index >= 0)
            {
                if(GridItemsSorted.Count > 0)
                {
                    result=GridItemsSorted.ElementAt(index);
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
            //SetRowSelection(row);
            CellBodySelectorUpdate();
        }

        public void ProcessKeyboardEvents(KeyEventArgs e)
        {
            if(!e.Handled)
            {
                switch (e.Key)
                {
                    case Key.F5:
                        LoadItems();
                        e.Handled = true;
                        break;

                    case Key.Home:
                        SelectRowFirst();
                        e.Handled = true;
                        break;

                    case Key.End:
                        SelectRowLast();
                        e.Handled = true;
                        break;

                    case Key.Up:
                        SelectRowPrev();
                        e.Handled = true;
                        break;

                     case Key.Down:
                        SelectRowNext();
                        e.Handled = true;
                        break;

                    case Key.F8:
                        DebugShowColumnsInfo();
                        e.Handled = true;
                        break;

                    //case Key.F1:
                    //{
                    //    if(
                    //        Keyboard.IsKeyDown(Key.LeftShift)
                    //        || Keyboard.IsKeyDown(Key.Right)
                    //    )
                    //    {
                    //        ShowDescription();
                    //        e.Handled = true; 
                    //    }
                    //}
                    //    break;
                }
            }            
        }

        private bool CellControllClicked { get; set; }
        private int CellClickDuration { get; set; }
        private Profiler CellClickProfiler { get; set; }

        private void CellDoubleClickCheck()
        {
            if(CellClickCount > 0)
            {
                CellClickCount=0;
                CellControllClicked = false;
                CellClickDuration = 0;
                CellClickProfiler.GetDelta();
                CellClickTimeout.Finish();
            }
        }

        private void CellProcessClickDouble()
        {
            return;

            //->CellDoubleClickCheck
            //CellClickTimeout.Restart();
            CellClickTimeout.Restart();

            if(MouseButtonType == 1)
            {
                if(CellClickCount == 0)
                {
                    //CellDoubleClickCheck();
                    CellClickDuration = 0;
                    CellClickProfiler.GetDelta();
                    CellClickCount++;
                    return;
                }

                if(CellClickCount == 1)
                {
                    CellClickDuration = CellClickDuration + (int)CellClickProfiler.GetDelta();
                    DebugLog($"    CLICK DBL t[{MouseButtonType}] c[{CellClickCount}] d[{CellClickDuration}]");

                    if(CellClickDuration > CellClickDurationTheshhold)
                    {
                        CellClickCount++;
                    }
                }
            }

            if(CellClickCount >= 2)
            {
                CellClickDuration = CellClickDuration + (int)CellClickProfiler.GetDelta();
                DebugLog($"    CLICK 222 t[{MouseButtonType}] c[{CellClickCount}] d[{CellClickDuration}]");

                //CellClickDuration=(int)CellClickProfiler.GetDelta();
                //DebugLog($"CLICK DBL [{CellClickDuration}]");
                CellDoubleClickCheck();
                ///CellProcessDoubleClick();

                //if(CellClickDuration > CellClickDurationTheshhold)
                //{
                //    CellProcessDoubleClick();
                //}
                //else
                //{
                //    CellClickCount--;
                //}
            }
        }

        private void CellProcessDoubleClick()
        {
            OnDblClick?.Invoke(SelectedItem);
        }


        private void ColumnHeaderOnClick(object sender, MouseButtonEventArgs e)
        {
            var cellInfo = GetCellInfo(sender);
            var cellPath = cellInfo.CheckGet("PATH");
            var cellIndex = cellInfo.CheckGet("INDEX").ToInt();
            var cellType = cellInfo.CheckGet("TYPE");
            var s = $"cellPath=[{cellPath}] cellIndex=[{cellIndex}] cellType=[{cellType}]";
            DebugLog($"ColumnHeaderOnClick {s}");
            DoSort(cellPath);
        }

        private void ColumnSorterOnClick(object sender, MouseButtonEventArgs e)
        {
            var cellInfo = GetCellInfo(sender);
            var cellPath = cellInfo.CheckGet("PATH");
            var cellIndex = cellInfo.CheckGet("INDEX").ToInt();
            var cellType = cellInfo.CheckGet("TYPE");
            var s = $"cellPath=[{cellPath}] cellIndex=[{cellIndex}] cellType=[{cellType}]";
            DebugLog($"ColumnSorterOnClick {s}");
            DoSort(cellPath);
        }


        private void CellBodyOnClick(object sender, MouseButtonEventArgs e)
        {
            var cellInfo = GetCellInfo(sender);
            var cellPath = cellInfo.CheckGet("PATH");
            var cellIndex = cellInfo.CheckGet("INDEX").ToInt();
            var cellType = cellInfo.CheckGet("TYPE");

            var s = $"cellPath=[{cellPath}] cellIndex=[{cellIndex}] cellType=[{cellType}]";
            DebugLog($"CellBodyOnClick {s}");

            //if(cellType == "HEADER")
            //{
            //    //left
            //    if(MouseButtonType == 1)
            //    {
            //        //DoSort(cellPath);
            //    }
            //}

            if(cellType == "BODY")
            {
                if(sender != null)
                {
                    var b = (Border)sender;
                    if(b != null)
                    {
                        SelectedCellPrev = SelectedCell;
                        SelectedCell = b;
                    }
                }

                if(cellIndex > 0)
                {
                    SelectedItemPrevIndex = SelectedItemIndex;
                    SelectedItemIndex = cellIndex;
                }

                if(cellIndex > 0)
                {
                    var row = RowItemGetByIndex(cellIndex - 1);
                    if(row.Count > 0)
                    {
                        SelectedItemPrev = SelectedItem;
                        SelectedItem = row;
                    }
                }

                if(!cellPath.IsNullOrEmpty())
                {
                    var column = ColumnGetByPath(cellPath);
                    if(column != null)
                    {
                        SelectedColumnPrev = SelectedColumn;
                        SelectedColumn = column;
                    }
                }

                //left
                if(MouseButtonType == 1)
                {
                    CellProcessControl();
                    CellProcessClickDouble();
                }

                //right
                if(MouseButtonType == 2)
                {
                    CellMenuShow();
                }

                //all
                {
                    RowSelect(SelectedItemIndex, SelectedItem);
                    if(SelectedColumn.OnClickAction != null)
                    {
                        var el = new FrameworkElement();
                        SelectedColumn.OnClickAction.Invoke(SelectedItem, el);
                    }
                }
            }
        }

        private void CellOnClick(object sender, MouseButtonEventArgs e)
        {
            var cellInfo=GetCellInfo(sender);
            var cellPath=cellInfo.CheckGet("PATH");            
            var cellIndex=cellInfo.CheckGet("INDEX").ToInt();
            var cellType=cellInfo.CheckGet("TYPE");

            /*
            if(!columnPath.IsNullOrEmpty())
            {
                var column = ColumnGetByPath(columnPath);
                if(column != null)
                {
                    SelectedColumn = column;
                }
            }
            */

            var cellBodyClick =false;            
            var cellContextMenu = false;

            /*
                header left(sort)
                body   left(select)    right(select)(menu)
                
             */

            if(cellType == "HEADER")
            {
                //left
                if(MouseButtonType == 1)
                {
                    DoSort(cellPath);
                }
            }

            if(cellType == "BODY")
            {
                //RowSelect(SelectedItemIndex, SelectedItem);

                //if(SelectedColumn.OnClickAction != null)
                //{
                //    var el = new FrameworkElement();
                //    SelectedColumn.OnClickAction.Invoke(SelectedItem, el);
                //}
                //CellProcessClickDouble();


                //right
                if(MouseButtonType == 2)
                {
                   // CellMenuShow();
                }

                //cellBodyClick = true;
            }


            //if(cellBodyClick)
            //{
            //    //var row = RowItemGetByIndex(columnIndex - 1);
            //    //if(row.Count > 0)
            //    {
            //        RowSelect(SelectedItemIndex, SelectedItem);
            //    }

            //    CellDoubleClick();
            //    //CellProcessControl();

            //    if(SelectedColumn.OnClickAction != null)
            //    {
            //        var el = new FrameworkElement();
            //        SelectedColumn.OnClickAction.Invoke(SelectedItem, el);
            //    }
            //}

            //if(cellContextMenu)
            //{
            //    CellMenuShow();
            //}

            //if(SelectedColumn != null)
            //{
                //switch(MouseButtonType)
                //{
                //    // left
                //    case 1:
                //        {
                //            //if(!columnPath.IsNullOrEmpty())
                //            {
                //                //column=ColumnGetByPath(columnPath);
                //                //if(column!=null)
                //                {
                //                    switch(columnType)
                //                    {
                //                        case "HEADER":
                //                            {
                //                                DoSort(columnPath);
                //                            }
                //                            break;

                //                        case "BODY":
                //                            {
                //                                cellBodyClick = true;

                //                            }
                //                            break;
                //                    }
                //                }
                //            }
                //        }
                //        break;

                //    // right
                //    case 2:
                //        {
                //            //if(!columnPath.IsNullOrEmpty())
                //            {
                //                //column=ColumnGetByPath(columnPath);
                //                //if(column!=null)
                //                {
                //                    switch(columnType)
                //                    {
                //                        case "BODY":
                //                            {
                //                                cellBodyClick = true;

                //                            }
                //                            break;
                //                    }
                //                }
                //            }

                //            cellContextMenu = true;
                //        }
                //        break;
                //}

                //if(column!=null)
                //{
                //    //SelectedColumn = column;                

                    
                //}
            //}           
        }

        public void CellProcessControl()
        {
            if(SelectedCellPrev !=null)
            {
                var cellBorder = (Border)SelectedCellPrev;
                //RenderingRowIndex = SelectedItemPrevIndex;
                //RenderingRow = SelectedItemPrev;
                var ctl = CellBodyControlCreate(SelectedColumnPrev, 0, SelectedItemPrevIndex, SelectedItemPrev);
                cellBorder.Child = ctl;
                
            }

            if(SelectedCell != null)
            {
                var cellBorder = (Border)SelectedCell;
                //RenderingRowIndex = SelectedItemIndex;
                //RenderingRow = SelectedItem;
                var ctl = CellBodyControlCreate(SelectedColumn, 1, SelectedItemIndex, SelectedItem);
                cellBorder.Child = ctl;
            }
        }

        public void DoResize()
        {
            //CellHeaderWidthProcess();
            //UpdateItems();
            if(ColumnWidthMode == GridBox.ColumnWidthModeRef.Compact)
            {
                CellHeaderWidthProcess();
            }
            CellUpdateWidthAll();
        }

        public void DoSort(string columnPath)
        {
            if(UseSorting)
            {
                if(!GridSorterBlocked)
                {
                    var direction = SortDirection;

                    //направление меняется, если мы щелкнули по тому же заголовку
                    //более одного раза
                    {
                        var sortingColumn = ColumnGet(columnPath);
                        if(SortColumn == sortingColumn)
                        {
                            //if(SortDirectionPrev == direction)
                            {
                                if(direction == ListSortDirection.Descending)
                                {
                                    direction = ListSortDirection.Ascending;
                                }
                                else
                                {
                                    direction = ListSortDirection.Descending;
                                }
                            }
                        }
                    }

                    SetSorting(columnPath,direction);
                    UpdateRenderedRows(0);
                    UpdateItems();
                    //CellHeaderWidthProcess();
                    //UpdateItems();
                    //ColumnResizeTimeout.Restart();
                }
            }
        }

        public void SelectRowFirst()
        {
            if(GridItems.Count > 0)
            {
                var row= GridItemsSorted.First();
                if(row!=null)
                {
                    RowSelect(1,row);
                }
            }
        }

        public void SelectRowLast()
        {
            if(GridItemsSorted.Count > 0)
            {
                var row= GridItemsSorted.Last();
                if(row!=null)
                {
                    RowSelect(GridItemsSorted.Count,row);
                }
            }
        }

        public void SelectRowNext()
        {
            if(SelectedItem.Count > 0)
            {
                if((SelectedItemIndex+1) <= GridItemsSorted.Count)
                {
                    SelectedItemIndex++;
                    var id=SelectedItem.CheckGet(PrimaryKey);
                    var runSelecting=false;
                    foreach(Dictionary<string,string> row in GridItemsSorted)
                    {
                        if(runSelecting)
                        {
                            SelectedItem=row;
                            id=SelectedItem.CheckGet(PrimaryKey);
                            break;
                        }

                        if(row.CheckGet(PrimaryKey)==id)
                        {
                            runSelecting=true;
                        }
                    }
                    SelectRowByKey(id,PrimaryKey,true);
                }
            }
        }

        public void SelectRowPrev()
        {
            if(SelectedItem.Count > 0)
            {
                if((SelectedItemIndex-1) > 0)
                {
                    SelectedItemIndex--;
                    var id=SelectedItem.CheckGet(PrimaryKey);
                    var selectedItemOld=SelectedItem;
                    var runSelecting=true;
                    foreach(Dictionary<string,string> row in GridItemsSorted)
                    {
                        if(row.CheckGet(PrimaryKey)==id)
                        {
                            runSelecting=false;
                            id=selectedItemOld.CheckGet(PrimaryKey);
                            break;
                        }

                        if(runSelecting)
                        {
                            selectedItemOld=row;
                        }
                    }
                    SelectRowByKey(id,PrimaryKey,true);
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
            if(GridItemsSorted.Count > 0)
            {
                int j=0;
                bool selected = false;
                foreach(Dictionary<string,string> row in GridItemsSorted)
                {
                    j++;
                    if(row.CheckGet(k) == id)
                    {   
                        RowSelect(j,row);
                        selected = true;
                        break;
                    }
                }

                if (!selected)
                {
                    var firstRow = GridItemsSorted.First();
                    RowSelect(1, firstRow);
                }
            }
        }

        public void SelectRowByKey(string id)
        {
            var k=PrimaryKey;
            SelectRowByKey(id,k,true);
        }

        private void SetRowSelection(Dictionary<string,string> row)
        {               
            return;
            {
                var line=LineGet(SelectedItemPrevIndex);
                if(line != null)
                {
                    line.Style=StyleGet("GridboxLineBorder");                    
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
                        lineStack.Style=StyleGet("GridboxLineStackPanel");                    

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
                    line.Style=StyleGet("GridboxLineSelectedBorder");                    
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
                        lineStack.Style=StyleGet("GridboxLineSelectedStackPanel");                    

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

        public Dictionary<string,string> GetCellInfo(object sender, int type=0)
        {
            /*
                type
                    0=Border
                    1=TextBox

                NAME
                TAG
                INDEX
                PATH
                TYPE="BODY"|"HEADER"
             */

            var result=new Dictionary<string,string>();

            if(sender!=null)
            {
                switch(type)
                {
                    case 1:
                        {
                            var b = (TextBox)sender;
                            if(b != null)
                            {
                                if(b.Tag != null)
                                {
                                    var t = b.Tag.ToString();
                                    var n = b.Name;
                                    result = GetCellInfo(n, t);
                                }
                            }
                        }
                        break;

                    default:
                        {
                            var b = (Border)sender;
                            if(b != null)
                            {
                                if(b.Tag != null)
                                {
                                    var t = b.Tag.ToString();
                                    var n = b.Name;
                                    result = GetCellInfo(n, t);
                                }
                            }
                        }
                        break;
                }

                //var b=(Border)sender;                        
                //var n=b.Name;
                //if(b.Tag != null)
                //{
                //    var t = b.Tag.ToString();
                //    result = GetCellInfo(n, t);
                //}
            }

            return result;
        }

        public Dictionary<string,string> GetCellInfo(string name, string tag)
        {
            /*
                NAME
                TAG
                INDEX
                PATH
                TYPE="BODY"|"HEADER"
             */

            var result=new Dictionary<string,string>();

            result.CheckAdd("TYPE","BODY");
            if(name.IndexOf("cell_header_") > -1)
            {
                result.CheckAdd("TYPE","HEADER");
            }
                
            if(!name.IsNullOrEmpty())
            {
                result.CheckAdd("NAME",name);
            }
            if(!tag.IsNullOrEmpty())
            {
                result.CheckAdd("TAG",tag);
                var list=tag.Split('|').ToList();
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

            return result;
        }

        public string CellTotalGetContent(DataGridHelperColumn c, Dictionary<string, string> row)
        {
            string result = "";

            if(c.Totals != null)
            {
                var r = c.Totals.Invoke(GridItemsSorted);
                result = r.ToString();
            }

            return result;
        }

        public string CellBodyGetContent(DataGridHelperColumn c, Dictionary<string,string> row)
        {
            //RenderingRow -> row
            string result ="";

            if(row != null)
            {
                var k=c.Path;
                result= row.CheckGet(k);

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

                        case ColumnTypeRef.String:
                        {
                            result=result.Replace("\n","");
                            result=result.Replace("\r","");
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

        public void EventsBind()
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
                UpdateRenderedRows(x);
                Central.Dbg($"x=[{x}]");
                if(GridScrollVerticalScroller!=null)
                {
                    var a=MouseButtonType;
                    var b=MouseButtonPressed;
                    if(x>=0)
                    {
                        if(!MouseButtonPressed)
                        {
                            GridScrollVerticalScroller.ScrollToVerticalOffset(x);
                        }
                        
                    }
                }

                e.Handled = true;
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
                    GridTotalsScroller.ScrollToHorizontalOffset(x);
                }
                e.Handled = true;
            }
        }

        private void GridScrollVerticalScroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if(e != null)
            {
                var x = e.VerticalOffset;               
                if(GridBodyScroller!=null)
                {
                    GridBodyScroller.ScrollToVerticalOffset(x);
                }
                e.Handled = true;
            }
        }

        private void UpdateScrollPosition(double x)
        {
            VerticalScrollPositionOld=VerticalScrollPosition;
            VerticalScrollPosition=(int)x;

            
            if(VerticalScrollPosition > VerticalScrollPositionOld)
            {
                //down
                VerticalScrollDirection=1;
            }
            if(VerticalScrollPosition < VerticalScrollPositionOld)
            {
                //up    
                VerticalScrollDirection=-1;
            }

            var d=Math.Abs(VerticalScrollPositionOld-VerticalScrollPosition);
            if(d > 0)
            {
                VerticalScrollDelta=(int)Math.Round((double)d/(double)ColumnHeightMin);
            }
                        
            if(VerticalScrollPosition > 0 && LineHeight > 0)
            {
                LineFirstVisible=(int)((double)VerticalScrollPosition/(double)LineHeight);
            }
        }

       

        public void ScrollersUpdate()
        {
            //->ScrollersUpdateSize();
            ScrollersUpdateTimeout.Restart();
        }

        public void ScrollersUpdateSize()
        {
            var gridContainerWidth = GetGridContainderWidth();
            var gridContentWidth = LineWidth;
            var gridContentHeight = (int)GridBodyContainer.ActualHeight;
            var oversize = false;
            var useHorizontalScroll = false;
            var currentMode = ColumnWidthMode;

            if(gridContentWidth < gridContainerWidth)
            {
                gridContentWidth = gridContainerWidth;
            }

            if(gridContentWidth > (gridContainerWidth + 50))
            {
                oversize = true;
            }

            //var h=(int)GridBodyContainer.ActualHeight;
            //var w=(int)GridHeaderContainer.ActualWidth;
            //var w = gridContainerWidth;

            //w = w - 20;
            //GridBodyContainer.Width = w;
            //GridHeaderContainer.Width = w;  

            if(ColumnWidthMode == GridBox.ColumnWidthModeRef.Full)
            {
                //gridContentWidth = gridContentWidth + 20;
            }
            
            GridBodyContainer.Width = gridContentWidth;
            GridHeaderContainer.Width = gridContentWidth;
            GridTotalsContainer.Width = gridContentWidth;

            //if(GridScrollVerticalScroller.VerticalScrollBarVisibility == ScrollBarVisibility.Visible)
            //{
            //    //w = w - 20;
            //}

            gridContentWidth = gridContentWidth - 20;

            //w=w+100;
            //var w2=ColumnTotalWidth;

            if(UseDynamicRendering)
            {
                //h=15000;
                gridContentHeight = GridItemsSorted.Count*ColumnHeightMin;                
            }

            GridScrollVerticalContent.Height= gridContentHeight;
            GridScrollHorizontalContent.Width= gridContentWidth;

            
            //var gridContainerWidth=(int)GridContainer.ActualWidth;

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
                //if(gridContentWidth > (gridContainerWidth+50) )
                //{
                //    useHorizontalScroll=true;
                //}
                if(oversize)
                {
                    useHorizontalScroll = true;
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


            DebugLog($"width_3 [{gridContentWidth}]x[{gridContentHeight}] oversize=[{oversize}] content=[{gridContentWidth}] block=[{gridContainerWidth}]");

            ColumnSetSortingLabel();
            _HideSplash();
            DebugLog("width_9");
        }

        public void AfterInit()
        {
            if(DoRun)
            {                         
                if(!Runned)
                {
                    Autosized = false;
                    if(ItemsAutoUpdate)
                    {
                        DebugLogInit("AfterInit");
                        Runned=true;

                        if(AutoUpdateInterval > 0)
                        {
                            AutoUpdateTimeout.Restart();
                        }

                        //if(ItemsAutoUpdate)
                        {
                            Autosized=false;
                            LoadItems();
                        }
                    }
                    //CellHeaderWidthProcess();

                }
            }


            //if(DoRun)
            //{
            //    if(!Runned)
            //    {
            //        Runned=true;

            //        if(AutoUpdateInterval > 0)
            //        {
            //            AutoUpdateTimeout.Restart();
            //        }

            //        if(ItemsAutoUpdate)
            //        {
            //            LoadItems();
            //        }
            //    }
            //}

        }

        public void Run()
        {
            DoRun=true;

            var t=new Common.Timeout(
                1,
                ()=>{
                    AfterInit();
                }              
            );
            t.SetIntervalMs(100);
            t.Run();

            GridBodyContainer.Focus();

            /*
            LoadItems();

            if(AutoUpdateInterval > 0)
            {
                AutoUpdateTimeout.Restart();
            }
            */
        }

        public void Destruct()
        {
        }

        public void ShowSplash()
        {
            Splash.Visibility = Visibility.Visible;
            this.Cursor = Cursors.Wait;
            Splash.Cursor = Cursors.Wait;
        }

        public void HideSplash()
        {
            Splash.Visibility = Visibility.Collapsed;
            this.Cursor = null;
            Splash.Cursor = null;
        }

        private void _ShowSplash()
        {
            //Splash.Visibility=Visibility.Visible;
        }

        private void _HideSplash()
        {
            //Splash.Visibility=Visibility.Collapsed;
        }
        
        public void LoadItems()
        {
            if(OnLoadItems!=null)
            {
                if(Toolbar!=null){
                    Toolbar.IsEnabled = false;
                }

                OnLoadItems?.Invoke();

                if(Toolbar!=null){
                    Toolbar.IsEnabled = true;
                }
            }
            else
            {
                if(QueryLoadItems != null)
                {
                    LoadItemsInner();
                }
            }
        }

        public async void LoadItemsInner()
        {
            bool resume = true;
            
            if(Toolbar!=null){
                Toolbar.IsEnabled = false;
            }

            if (resume)
            {
                var q = new LPackClientQuery();

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
                q.Request.Attempts= QueryLoadItems.Attempts;

                q.DoQuery();

                //await Task.Run(() =>
                //{
                //    q.DoQuery();
                //});

                if (q.Answer.Status == 0)
                {
                    var answerData = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (answerData != null)
                    {
                        if(!QueryLoadItems.AnswerSectionKey.IsNullOrEmpty())
                        {
                            var ds = ListDataSet.Create(answerData, QueryLoadItems.AnswerSectionKey);
                            UpdateItems(ds);
                        }
                    }
                }
            }

            if(Toolbar!=null){
                Toolbar.IsEnabled = true;
            }
            
        }
        
        public void UpdateItems()
        {
            UpdateItems(null);
        }

        private Dictionary<string, string> RenderingRow {get;set;}
        private int RenderingRowIndex {get;set;}
        private bool RenderingInProgress {get;set;}
        private bool SearchingInProgress {get;set;}
        private bool SearchingComplete {get;set;}
        
        public void UpdateItems(ListDataSet ds = null, bool selectFirst=true)
        {
            Log="";
            DebugLog("update_0");

            //CellHeaderWidthProcess();

            if(ds != null)
            {
                Ds=ds;
            }

            if(Ds!=null)
            {
                //if(Ds.Items.Count > 0)
                {
                    _ShowSplash();
                    RenderingInProgress=true;
                    
                    DebugLog("update_1");

                    GridItems=Ds.Items;

                    if(OnFilterItems != null)
                    {
                        DebugLog("update_2 filter");
                        OnFilterItems.Invoke();
                    }

                    GridItemsSorted=GridItems;

                    {
                        DebugLog("update_3 search");
                        GridItemsSorted=ItemsSearch(GridItemsSorted);
                    }

                    if(SortingEnabled)
                    {
                        DebugLog("update_3 sort");
                        GridItemsSorted = ItemsGetSortedSort(GridItemsSorted);
                    }

                    {
                        LineToolTipMode=1;
                        if(OnViewItem != null)
                        {
                            LineToolTipMode=2;
                        }

                        DebugLog("update_4 render");
                        RenderClear();
                        ScrollersUpdateSize();

                        DebugLogRender($"update=[{GridItems.Count}]");
                        RenderCheck();
                        RenderTotals();

                        CellBodySelectorConstruct();
                    }                                       

                    DebugLog("update_8");
                    _HideSplash();

                    if(!Autosized)
                    {
                        Autosized=true;

                        var interval=new Common.Timeout(
                            1,
                            ()=>{
                                CellHeaderWidthProcess();
                                //CellUpdateWidthAll();
                            }
                        );
                        interval.SetIntervalMs(Interval1);
                        interval.Run();

                        if(SelectedItem.Count == 0)
                        {
                            SelectRowFirst();
                        }
                    }

                    //CellUpdateWidthAll();
                    /*
                    {
                        var interval = new Common.Timeout(
                            1,
                            () => {
                                CellUpdateWidthAll();
                            }
                        );
                        interval.SetIntervalMs(300);
                        interval.Run();
                    }
                    */


                    if(SelectedItem.Count > 0)
                    {
                        var id=SelectedItem.CheckGet(PrimaryKey);
                        SelectRowByKey(id,PrimaryKey,true);
                        //int i = 0;
                    }

                    DebugLog("update_9");
                    RenderingInProgress=false;

                    if(SearchingInProgress)
                    {
                        SearchingInProgress=false;
                        SearchText.Focus();
                    }
                }
            }
        }

        public async void ExportItemsExcel()
        {
            if (GridItemsSorted.Count > 0)
            {
                var eg = new ExcelGrid();
                var cols = Columns;
                eg.SetColumnsFromGrid(cols);
                eg.Items = GridItemsSorted;
                await Task.Run(() =>
                {
                    eg.Make();
                });
            }
        }

        private List<Dictionary<string, string>> ItemsSearch(List<Dictionary<string, string>> list)
        {
            if(list.Count > 0)
            {
                bool doFiltering = false;
                string s="";

                if(SearchText != null)
                {
                    var t=SearchText.Text;
                    if(!t.IsNullOrEmpty())
                    {
                        doFiltering=true;
                        s=SearchText.Text.Trim().ToLower();
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
                    foreach(Dictionary<string,string> row in list)
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
                    list=items;
                }
            }
            return list;
        }

        private List<Dictionary<string, string>> ItemsGetSortedSort(List<Dictionary<string, string>> list)
        {
            //var list=GridItems;
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
            
            var c=new DataGridHelperColumn
            {
                Header="",
                Path="_",
                Doc="",
                ColumnType=ColumnTypeRef.String,
                Width2=1,
            };
            Columns.Add(c);
            
        }

        public void SetSorting(string columnName, ListSortDirection direction = ListSortDirection.Ascending)
        {
            SortingEnabled = true;

            //_ShowSplash();
            SortColumnPrev =SortColumn;
            SortColumn=ColumnGet(columnName);
            SortDirectionPrev = SortDirection;
            SortDirection =direction;
            //ColumnSetSortingLabel();           
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

        public string Log {get;set;}
        public void DebugLog(string s)
        {
            var dt=Profiler.GetDelta();
            Log=Log.Append($"({dt.ToString().SPadLeft(5)}) {s}",true);
            Log=Log.Crop(1000);
            System.Diagnostics.Trace.WriteLine($"({dt}) {s}");
        }

        public void DebugShowGridInfo()
        {

            var s=CollectGridInfo();
            
            var msg=s;
            var d = new LogWindow($"{msg}", "Информация" );
            d.ShowDialog();

          
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
