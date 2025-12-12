using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Client.Common
{
    /// <summary>
    /// многоуровневые заголовки колонок грида
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    public class GridMultiRowHeader
    {
        public ArrayList HeaderLevels { get; set; }
        public DataGrid DataGrid { get; set; }
        public Grid HeaderGrid { get; set; }
        public Grid FooterGrid { get; set; }
        public Border ContainerHeader { get; set; }
        public Border ContainerFooter { get; set; }
        public int LevelsCount { get; set; }
        public bool HideLast { get; set; }
        public int HeaderLineHeight { get; set; }
        public int ColumnsCount { get; set; }
        public Dictionary<string, string> FooterColumns { get; set; }
        public bool UseZeroColumn { get; set; }
        public bool ZeroColumnCreated { get; set; }
        public bool ZeroColumnInitialized { get; set; }
        public DataGridTextColumn ZeroColumn { get; set; }
        public Dictionary<string,string> GridItemsTotals { get;set; }
        public Dictionary<string,TextBlock> GridCtlsTotals { get;set; }

        public GridMultiRowHeader()
        {
            HeaderLevels = new ArrayList();
            LevelsCount = 0;
            //отладочный флажок, на продакшне =true
            HideLast = true;
            //высота строки заголовка
            HeaderLineHeight = 22;
            ColumnsCount = 0;
            
            /*
                Временное решение проблемы с плавающими мультизаголовками
                При инициализации грида у него добавляется пустая колонка 
                для выравнивания столбцов.
                Когда в грид придут данные, он создаст мастер-колонку, которая подопрет
                остальные. В этот момент мы удаляем нашу временную подпорочную колонку.
                Факт поступления данных мы отслеживаем по событию Grid.LoadingRow

             */
            UseZeroColumn=true;
            
            ZeroColumnCreated=false;   
            ZeroColumnInitialized=false;
            GridItemsTotals=new Dictionary<string, string>();
            GridCtlsTotals=new Dictionary<string, TextBlock>();
        }

        private void DataGridLoadingRow(object sender,DataGridRowEventArgs e)
        {
            if( UseZeroColumn )
            {
                if( ZeroColumnCreated )
                {
                    if( DataGrid.Items.Count > 0 )
                    {
                        //DataGrid.Columns.RemoveAt(0);
                        //ZeroColumn=DataGrid.Columns[0];
                        if( ZeroColumn != null )
                        {
                            DataGrid.Columns.Remove(ZeroColumn);
                        }                       
                        DataGrid.LoadingRow -= DataGridLoadingRow;
                        ZeroColumnCreated=false;
                    }
                }
            }
        }

        public void AddHeaderLevel(ArrayList columnsStruct)
        {
            HeaderLevels.Add(columnsStruct);
            LevelsCount++;
        }

        public void AddFooterLevel(Dictionary<string, string> cols)
        {
            FooterColumns = cols;
            //LevelsCount++;
        }

        public void SetFooterLevel(Dictionary<string, string> cols)
        {
            FooterColumns = cols;
        }


        public void UpdateHeader()
        {
            bool cont = true;

            //проверки
            if (cont)
            {
                if (DataGrid == null)
                {
                    cont = false;
                }
            }

            if (cont)
            {
                if (LevelsCount == 0)
                {
                    cont = false;
                }

                ColumnsCount = DataGrid.Columns.Count;
                if (ColumnsCount == 0)
                {
                    cont = false;
                }
            }

            //построение структуры заголовка
            if (cont)
            {
                HeaderGrid = new Grid();
                              

                ContainerHeader.Child = null;
                ContainerHeader.Height = 0;
                //ContainerHeader.Style = (Style)ContainerHeader.FindResource("MainGridMultiColumnContainer");

                for (int i = (LevelsCount - 1); i >= 0; i--)
                {
                    RenderHeaderRow(i);
                }
                RenderHeaderRow(-1);

                HeaderGrid.ShowGridLines = false;
                ContainerHeader.Child = HeaderGrid;

                if (HideLast)
                {
                    ContainerHeader.Height = LevelsCount * HeaderLineHeight;
                }
                else
                {
                    ContainerHeader.Height = (LevelsCount + 1) * HeaderLineHeight;
                }
            }


            if( !ZeroColumnInitialized )
            {
                if( UseZeroColumn )
                {
                    if( !ZeroColumnCreated )
                    {
                        var doAction=false;

                        if( DataGrid.Items != null )
                        {
                            if( DataGrid.Items.Count == 0 )
                            {
                                doAction=true;                       
                            }
                        }
                        else
                        {
                            doAction=true;
                        }
                    
                        if( doAction )
                        {
                            ZeroColumn = new DataGridTextColumn();             
                            ZeroColumn.Header = ""; 
                            ZeroColumn.DisplayIndex=0;          
                            ZeroColumn.Width=new DataGridLength(25);
                            ZeroColumn.MinWidth=25;            
                            DataGrid.Columns.Add(ZeroColumn);

                            ZeroColumnCreated=true;
                        }
                    }

                    DataGrid.LoadingRow += DataGridLoadingRow;
                    
                }
                ZeroColumnInitialized=true;

            }


            if(HideLast==false)
            {
                //GridMultilineHeaderArea.Height=50;
            }

            
        }

        public string[] GroupsOrder => _groupsOrder;

        private string[] _columnsOrder;
        private string[] _groupsOrder;
        private int[] _groupsSpans;
        private int[] _groupsWidth;
        private ArrayList _columnsStruct;
        public void PrepareHeadersData(int level)
        {
            //level=1,0

            // список имен полей, как они идут в гриде
            // columnsOrder[<DisplayIndex>]=<ColumnName>
            _columnsOrder = new string[ColumnsCount+1];
            _groupsOrder = new string[ColumnsCount+1];
            _groupsSpans = new int[ColumnsCount+1];
            _groupsWidth = new int[ColumnsCount+1];

            _columnsStruct = (ArrayList)HeaderLevels[level];

            bool cont = true;


            if (cont)
            {
                //заполняем список полей, они выстраиваются в массиве по ключу:
                //по возрастанию DisplayIndex

                var j=1;
                foreach (DataGridColumn column in DataGrid.Columns)
                {
                    var n = DataGridUtil.GetName(column);
                    if (!string.IsNullOrEmpty(n))
                    {
                        _columnsOrder[j]=n;
                        _groupsOrder[j]="";

                        var group=FindGroupByColumn(_columnsStruct, n);
                        if (group!=null)
                        {
                            _groupsOrder[j]=$"{group.Header}";                        
                        }                                    
                    }
                    
                    j++;
                }
            }

            if (cont)
            {
                string[] groupsOrder2;
                groupsOrder2 = new string[ColumnsCount+1];

                int j = 0;
                int mark = 0;
                foreach (string g in _groupsOrder)
                {
                    groupsOrder2[j] = _groupsOrder[j];

                    //если следующая группа имеет такое же имя, сливаем                        
                    if (j > 0)
                    {
                        if (_groupsOrder[j] == _groupsOrder[j - 1] && !string.IsNullOrEmpty(_groupsOrder[j]))
                        {
                            groupsOrder2[j] = "";
                        }
                        else
                        {
                            groupsOrder2[j] = _groupsOrder[j];
                            mark = j;
                        }
                    }

                    if (_groupsSpans[mark] == 0)
                    {
                        _groupsSpans[mark] = 1;
                    }
                    else
                    {
                        _groupsSpans[mark] = _groupsSpans[mark] + 1;
                    }

                    j++;
                }
                _groupsOrder = groupsOrder2;
            }
        }

        public void RenderHeaderRow(int level)
        {
            
            var l = level;
            //для последнего уровня все равно рендерим первый
            //(он виртуальный, не отображается)
            if (l < 0)
            {
                l = 0;
            }
            PrepareHeadersData(l);

            //колонки
            foreach (string c in _columnsOrder)
            {
                var newColumn = new ColumnDefinition();
                newColumn.Width = GridLength.Auto;                                
                HeaderGrid.ColumnDefinitions.Add(newColumn);
            }

            var row = LevelsCount - level - 1;

            if (level > -1)
            {
                //level=3.2.1.0
                //верхние строки заголовков

                var newRow = new RowDefinition();
                newRow.Height = new GridLength(HeaderLineHeight, GridUnitType.Pixel);
                HeaderGrid.RowDefinitions.Add(newRow);

                int j = 0;
                int jc=0;
                foreach (string c in _columnsOrder)
                {
                    var g=new MultilineHeaderGroup();
                    if(c!=null)
                    {
                        g=FindColumnInStruct(_columnsStruct,c);
                    }
                    

                    if (_groupsSpans[j] > 0)
                    {
                        DataGridColumn currentColumn = null;
                        string columnName = "";

                        foreach (DataGridColumn column in DataGrid.Columns)
                        {
                            if (currentColumn == null)
                            {
                                columnName = DataGridUtil.GetName(column);
                                if (!string.IsNullOrEmpty(columnName))
                                {
                                    if (columnName == c)
                                    {
                                        currentColumn = column;
                                    }
                                }
                            }
                        }


                        if (currentColumn != null)
                        {
                            var cellText = new TextBlock();
                            cellText.Text = $"{_groupsOrder[j]}";
                            cellText.Style = (Style)ContainerHeader.FindResource("MainGridMultiColumnHeaderText");

                            /*
                            if(!string.IsNullOrEmpty(g.Description))
                            {
                                cellText.ToolTip=g.Description;
                            }
                            */
                            

                            var cellBorder = new Border();
                            cellBorder.Style = (Style)ContainerHeader.FindResource("MainGridMultiColumnHeader");                            
                            cellBorder.Child = cellText;
                            
                            if (!string.IsNullOrEmpty(_groupsOrder[j]))
                            {
                                cellBorder.ToolTip = $"{_groupsOrder[j]}";
                            }

                            //если одинарный с пустым заголовком, то сливается с нижним
                            var mergeDown = false;
                            if (_groupsSpans[j] == 1)
                            {
                                if (string.IsNullOrEmpty(_groupsOrder[j]))
                                {
                                    mergeDown = true;
                                }
                            }
                            if (mergeDown)
                            {
                                cellBorder.BorderThickness = new Thickness(0, 0, 1, 0);
                            }
                            else
                            {
                                //magick
                                if (level == 1)
                                {
                                    //cellBorder.BorderThickness = new Thickness(0, 0, 1, 1);
                                    cellBorder.BorderThickness = new Thickness(0, 0, 1, 0);
                                }
                                else
                                {
                                    //cellBorder.BorderThickness = new Thickness(0, 0, 1, 2);
                                    cellBorder.BorderThickness = new Thickness(0, 0, 1, 0);
                                }
                            }


                            var cellGrid = new Grid();
                            //cellGrid.Style = (Style)ContainerHeader.FindResource("MainGridMultiColumnHeaderCell");
                            System.Windows.Controls.Grid.SetRow(cellGrid, row);
                            System.Windows.Controls.Grid.SetColumn(cellGrid, (j-1));
                            System.Windows.Controls.Grid.SetColumnSpan(cellGrid, _groupsSpans[j]);
                            cellGrid.Children.Add(cellBorder);

                            HeaderGrid.Children.Add(cellGrid);
                            jc++;
                        }
                    }

                    j++;
                }
            }
            else
            {
                //level=-1
                //последняя строка заголовков (скрытая)

                var newRow = new RowDefinition();
                if (HideLast)
                {
                    newRow.Height = new GridLength(0.0, GridUnitType.Pixel);
                }
                else
                {
                    newRow.Height = new GridLength(HeaderLineHeight, GridUnitType.Pixel);
                }
                HeaderGrid.RowDefinitions.Add(newRow);

                int j = 0;
                foreach (string c in _columnsOrder)
                {
                    DataGridColumn currentColumn = null;
                    string columnName = "";

                    foreach (DataGridColumn column in DataGrid.Columns)
                    {
                        if (currentColumn == null)
                        {
                            columnName = DataGridUtil.GetName(column);
                            if (!string.IsNullOrEmpty(columnName))
                            {
                                if (columnName == c)
                                {
                                    currentColumn = column;
                                }
                            }
                        }
                    }

                    if (currentColumn != null)
                    {
                        var cellText = new TextBlock();
                        if( currentColumn.Header!=null )
                        {
                            cellText.Text = $"{currentColumn.Header.ToString()}";
                        }

                        var cellBorder = new Border();
                        cellBorder.Style = (Style)ContainerHeader.FindResource("MainGridMultiColumnHeader");
                        cellBorder.Child = cellText;

                        var cellGrid = new Grid();
                        cellGrid.MinWidth = 15;
                        cellGrid.Margin = new Thickness(0);
                        System.Windows.Controls.Grid.SetRow(cellGrid, row);
                        System.Windows.Controls.Grid.SetColumn(cellGrid, j);
                        cellGrid.Children.Add(cellBorder);

                        Binding widthBinding = new Binding();
                        widthBinding.Source = currentColumn;
                        widthBinding.Path = new PropertyPath("ActualWidth");
                        widthBinding.UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged;
                        cellGrid.SetBinding(System.Windows.Controls.Grid.WidthProperty, widthBinding);

                        HeaderGrid.Children.Add(cellGrid);
                        j++;
                    }
                }
            }
        }

        public MultilineHeaderGroup FindColumnInStruct(ArrayList _columnsStruct, string n)
        {
            var result=new MultilineHeaderGroup();

            if(_columnsStruct!=null)
            {
                foreach(MultilineHeaderGroup g in _columnsStruct)
                {
                    if(g.Name==n)
                    {
                        result=g;
                        return result;
                    }
                }
            }

            return result;
        }

        public Dictionary<string,int> GetColumnsWidth()
        {
            var result=new Dictionary<string,int>();

            DataGridColumn currentColumn = null;
            string columnName = "";

            foreach (DataGridColumn column in DataGrid.Columns)
            {
                if (column!=null)
                {
                    columnName = DataGridUtil.GetName(column);
                    if (!string.IsNullOrEmpty(columnName))
                    {
                        
                        var w=(int)column.ActualWidth;
                        if(!result.ContainsKey(columnName))
                        {
                            result.Add(columnName,0);
                        }
                        result[columnName]=w;
                    }
                }
            }

            return result;
        }

        public void UpdateFooter()
        {
            bool cont = true;

            //проверки
            if (cont)
            {
                if (DataGrid == null)
                {
                    cont = false;
                }
            }

            
            var b=false;
            if(b) 
            { 
                LevelsCount=1;
            }
            
            if (cont)
            {
                if (LevelsCount == 0)
                {
                    //cont = false;
                }

                ColumnsCount = DataGrid.Columns.Count;
                if(b) 
                { 
                    ColumnsCount=6;
                }

                if (ColumnsCount == 0)
                {
                    cont = false;
                }
            }

            //построение структуры заголовка
            if (cont)
            {
                FooterGrid = new Grid();

                ContainerFooter.Child = null;
                //ContainerFooter.Height = 0;
                //ContainerFooter.Style = (Style)ContainerFooter.FindResource("MainGridMultiColumnContainer");


                _columnsOrder = new string[ColumnsCount];

                int j=0;
                foreach (DataGridColumn column in DataGrid.Columns)
                {

                    var k=j;
                    j++;
                    //var k = (int)column.DisplayIndex;
                    //if (k > -1)
                    {
                        var v = (string)DataGridUtil.GetName(column);
                        if (!string.IsNullOrEmpty(v))
                        {
                            _columnsOrder[k] = v;
                        }
                    }
                }

                RenderFooterRow();


                FooterGrid.ShowGridLines = false;
                ContainerFooter.Child = FooterGrid;
            }

        }


        public void RenderFooterRow()
        {

            //колонки
            foreach (string c in _columnsOrder)
            {
                var newColumn = new ColumnDefinition();
                newColumn.Width = GridLength.Auto;
                FooterGrid.ColumnDefinitions.Add(newColumn);
            }

            var row = 0;

            var newRow = new RowDefinition();
            FooterGrid.RowDefinitions.Add(newRow);

            int j = 0;
            foreach (string c in _columnsOrder)
            {
                DataGridColumn currentColumn = null;
                string columnName = "";

                foreach (DataGridColumn column in DataGrid.Columns)
                {
                    if (currentColumn == null)
                    {
                        columnName = DataGridUtil.GetName(column);
                        if (!string.IsNullOrEmpty(columnName))
                        {
                            if (columnName == c)
                            {
                                currentColumn = column;
                            }
                        }
                    }
                }

                if (currentColumn != null)
                {
                    var cellText = new TextBlock();
                    cellText.Text = $"";

                    if(!GridCtlsTotals.ContainsKey(columnName))
                    {
                        GridCtlsTotals.Add(columnName,cellText);
                    }

                    //var p=columnName;
                    //Binding textBinding = new Binding();
                    //textBinding.Source = GridItemsTotals;
                    //textBinding.Path = new PropertyPath($"[{p}]");
                    //cellText.SetBinding(System.Windows.Controls.TextBlock.TextProperty, textBinding);

                    /*
                    if (FooterColumns != null)
                    {
                        if (FooterColumns.ContainsKey(columnName))
                        {
                            cellText.Text = $"{FooterColumns[columnName]}";
                        }
                    }
                    */

                    

                    /*
                    if (FooterColumns != null)
                    {
                        if (FooterColumns.ContainsKey(columnName))
                        {
                            Binding valueBinding = new Binding();
                            valueBinding.Source = FooterColumns[columnName].ToString();
                            //valueBinding.Path = new PropertyPath("ActualWidth");
                            cellText.SetBinding(System.Windows.Controls.TextBlock.TextProperty, valueBinding);
                        }
                    }
                    */


                    var cellBorder = new Border();
                    cellBorder.Style = (Style)ContainerHeader.FindResource("MainGridTotalsColumnHeader");
                    cellBorder.Child = cellText;

                    var cellGrid = new Grid();
                    cellGrid.MinWidth = 15;
                    cellGrid.Margin = new Thickness(0);
                    System.Windows.Controls.Grid.SetRow(cellGrid, row);
                    System.Windows.Controls.Grid.SetColumn(cellGrid, j);
                    cellGrid.Children.Add(cellBorder);

                    Binding widthBinding = new Binding();
                    widthBinding.Source = currentColumn;
                    widthBinding.Path = new PropertyPath("ActualWidth");
                    cellGrid.SetBinding(System.Windows.Controls.Grid.WidthProperty, widthBinding);

                    FooterGrid.Children.Add(cellGrid);
                    j++;
                }
            }

        }

        public void UpdateFooterValues(Dictionary<string,string> items)
        {
            GridItemsTotals=items;

            if(FooterGrid!=null)
            {
                foreach(KeyValuePair<string,string> c in GridItemsTotals )
                {
                    if(GridCtlsTotals.ContainsKey(c.Key))
                    {
                        GridCtlsTotals[c.Key].Text=c.Value;
                    }
                }               
               
            }
            
        }

        public MultilineHeaderGroup FindGroupByColumn(ArrayList columnsStruct, string columnName)
        {
            MultilineHeaderGroup result = null;

            foreach (MultilineHeaderGroup ig in columnsStruct)
            {
                if (ig.Columns != null)
                {
                    foreach (string ic in ig.Columns)
                    {
                        if (result == null)
                        {
                            if (ic == columnName)
                            {
                                result = ig;
                            }
                        }
                    }
                }
            }

            return result;
        }
    }


    public class MultilineHeaderGroup
    {
        public string Name { get; set; }
        public string Header { get; set; }        
        public string Description { get; set; }        
        public ArrayList Columns { get; set; }

        public MultilineHeaderGroup()
        {
            Name = "";
            Header = "";
            Description="";
            Columns = new ArrayList();
        }
    }
}
