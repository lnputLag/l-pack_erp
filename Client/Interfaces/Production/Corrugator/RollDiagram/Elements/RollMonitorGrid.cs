using Client.Assets.HighLighters;
using Client.Common;
using Client.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Структура данных и рендер грида
    /// (диаграмма рулонов на ГА)
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>2</version>
    /// <released>2021-11-23</released>
    public class RollMonitorGrid
    {
        /*
            Структура данных со своими внутренними вспомогательными структурами
            и функции рендера данных.
            Входящие данные загружаются во внутренние регистры, дальнейшая работа
            с данними идет с их внутренним представлением.
         */
        public Dictionary<int,RollMonitorGridRow> Rows { get; set; }

        /// <summary>
        /// Время начала "экрана" "07:30"
        /// </summary>
        public string TimeStart { get; set; }
        /// <summary>
        /// Время начала событий, "08:00" (будут показаны события, заканчивающиеся после этого времени)
        /// </summary>
        public string TimeStartShipment { get; set; }
        /// <summary>
        /// Количество отсчетов линейки времени
        /// </summary>
        public int TimeTicks { get; set; }
        /// <summary>
        /// Интервал сетки по горизонтали
        /// </summary>
        public int TimeGridInterval { get; set; }
        /// <summary>
        /// Интервал отсчетов линейки времени
        /// </summary>
        public int TimeLabelInterval { get; set; }
        /// <summary>
        /// Смещение линейки времени, (0, 30, 60)
        /// </summary>
        public int TimeLabelOffset { get; set; }
        /// <summary>
        /// Опорное время, сегодня или то, что указано в фильтре
        /// </summary>
        public DateTime TodayDT { get; set; }

        public DateTime MonitorLeftDT { get; set; }
        public DateTime MonitorRightDT { get; set; }
        public DateTime MonitorStartDT { get; set; }
        public DateTime MonitorFinishDT { get; set; }
        public int MonitorStep { get; set; }

        /// <summary>
        /// Текущее время "11:32"
        /// </summary>
        public string TimeHoursNow { get; set; }

        public string DateNow { get; set; }

        /// <summary>
        /// Контейнер "имена терминалов"
        /// </summary>
        public Grid HeadersContainer { get; set; }
        /// <summary>
        /// Контйнер отгрузок
        /// </summary>
        public Grid DataContainer { get; set; }
        /// <summary>
        /// Контейнер линейки времени
        /// </summary>
        public Grid TimelineContainer { get; set; }

        /// <summary>
        /// Общее число отгрузок на экране
        /// </summary>
        public int CellsCount { get; set; }

        public int ColsCount { get; set; }

        /// <summary>
        /// Общее число видимых строк на экране
        /// </summary>
        public int RowsCount { get; set; }

        /// <summary>
        /// Хеш координат: время-колонка, "yyyy-MM-dd_HH:mm"=>int
        /// </summary>
        public Dictionary<string,int> Cols { get; set; }

        /// <summary>
        /// Колонка (ID) в которой стоит маркет текущего времени (метка "сейчас")
        /// </summary>
        public int CenterCol { get; set; }

        /// <summary>
        /// Первая временная метка в гриде. (формат: yyyy-MM-dd_HH:mm )
        /// Если событие началось за гранью монитора слева, его абсцисса будет равна FirstTime.
        /// </summary>
        public string FirstTime { get; set; }
        /// <summary>
        /// Последняя временная метка в гриде (формат: yyyy-MM-dd_HH:mm )
        /// </summary>
        public string LastTime { get; set; }
        /// <summary>
        /// (формат: yyyy-MM-dd_HH:mm )
        /// </summary>
        public string CenterTime { get; set; }
        /// <summary>
        /// Список строк
        /// </summary>
        public Dictionary<int,string> RowTitleList { get; set; }

        public Dictionary<int,int> TaskCounter { get; set; }
        

        /// <summary>
        /// Счетчик количества заданий
        /// </summary>
        public int WorksCount { get; set; }
        /// <summary>
        /// Счетчик количества простоев
        /// </summary>
        public int IdlesCount { get; set; }
        /// <summary>
        /// Счетчик количества отсчетов каунтера
        /// </summary>
        public int CounterCount { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int ElementsCount { get; set; }

        /// <summary>
        /// Единица ширины ячейки (влияет на масштаб всех элементов)
        /// </summary>
        public double CellWidth { get; set; }
        /// <summary>
        /// Высота строки
        /// </summary>
        public int RowHeight { get; set; }

        public string XFormat { get; set; }
        public string XStep { get; set; }
        public int XStepMin { get; set; }
        public int XStepSec { get; set; }

        public int MaxDataHeight { get; set; }

        /// <summary>
        /// Кнструктор
        /// </summary>
        /// <param name="today"></param>
        public RollMonitorGrid(DateTime today)
        {
            //начало отсчета, строка            
            TimeStart="07:30";            
            TimeStartShipment="08:00";

            CellWidth=0.2;            
            RowHeight=20;
            XFormat="yyyy-MM-dd_HH:mm:ss";
            XStep="01:00";
            MaxDataHeight=0;
            
            TodayDT=today;


            //границы монитора
            string monitorLeft=$"{TodayDT.ToString("yyyy-MM-dd")} 07:30:00";            
            MonitorLeftDT=DateTime.ParseExact(monitorLeft,"yyyy-MM-dd HH:mm:ss",CultureInfo.InvariantCulture);

            string monitorRight=$"{TodayDT.AddDays(1).ToString("yyyy-MM-dd")} 08:00:00";            
            MonitorRightDT=DateTime.ParseExact(monitorRight,"yyyy-MM-dd HH:mm:ss",CultureInfo.InvariantCulture);

            //Central.Dbg($"Monitor L-R: [{MonitorLeftDT.ToString("yyyy-MM-dd HH:mm:ss")}] -> [{MonitorRightDT.ToString("yyyy-MM-dd HH:mm:ss")}]");

            string monitorStart=$"{TodayDT.ToString("yyyy-MM-dd")} 07:30:00";            
            MonitorStartDT=DateTime.ParseExact(monitorStart,"yyyy-MM-dd HH:mm:ss",CultureInfo.InvariantCulture);

            string monitorFinish=$"{TodayDT.AddDays(1).ToString("yyyy-MM-dd")} 08:00:00";            
            MonitorFinishDT=DateTime.ParseExact(monitorFinish,"yyyy-MM-dd HH:mm:ss",CultureInfo.InvariantCulture);

            //Central.Dbg($"Monitor S-F: [{MonitorStartDT.ToString("yyyy-MM-dd HH:mm:ss")}] -> [{MonitorFinishDT.ToString("yyyy-MM-dd HH:mm:ss")}]");

            //sec
            MonitorStep=5;

            /*
                текущее время мы фиксируем, чтобы все внутрение механизмы опирались
                на одно значение
            */
            TimeHoursNow = DateTime.Now.ToString("HH:mm");
            DateNow = DateTime.Now.ToString("yyyy-MM-dd");

            //кличество точек, одна точка-1 мин.
            TimeTicks=24*60+30;
            //интервал сетки по горизонтали, мин
            TimeGridInterval=60;
            //интервал временных меток на шкале
            TimeLabelInterval=60;
            TimeLabelOffset=30;

            CellsCount=0;
            ColsCount=0;
            RowsCount=0;
            WorksCount=0;
            IdlesCount=0;
            CounterCount=0;

            ElementsCount=0;

            FirstTime="";
            LastTime="";
            CenterTime="";

            CenterCol=0;

            Rows=new Dictionary<int,RollMonitorGridRow>();
            Cols=new Dictionary<string,int>();
            TaskCounter=new Dictionary<int, int>();

            //Central.Dbg($"    TimeHoursNow:[{TimeHoursNow}] DateNow:[{DateNow}] ");

            XStepMin=1;
            XStepSec=0;
            if( !string.IsNullOrEmpty( XStep ) )
            {
                string[] t = XStep.Split(':');
                if(!string.IsNullOrEmpty(t[0]))
                {
                    XStepMin=int.Parse(t[0]);
                }
                if(!string.IsNullOrEmpty(t[1]))
                {
                    XStepSec=int.Parse(t[1]);
                }
            }

        }

        /// <summary>
        /// Загрузка заголовков строк: названия машин
        /// </summary>
        public void LoadTitles(ListDataSet ds)
        {
            var items=ds.Items;
            if(items!=null)
            {
                /*
                    Подготовка структуры данных: строки
                        row:
                            Index
                            Title
                            Row (etc)
                 */
                int rowIndex = 0;
                foreach(Dictionary<string,string> row in items)
                {

                    int rowId = row.CheckGet("ID").ToInt();
                    if(rowId != 0)
                    {
                        if(!Rows.ContainsKey(rowId))
                        {
                            var gridRow = new RollMonitorGridRow();
                            gridRow.Index=rowIndex;
                            gridRow.Title=$"{row.CheckGet("MACHINE_NAME")}";
                            gridRow.Row=row;
                            Rows.Add(rowId,gridRow);
                            rowIndex++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Загрузка элементов: рулоны      
        /// </summary>
        public void LoadRolls(ListDataSet ds)
        {
            /*
                Server.Modules.Production.TaskTime.List

                Source:

                    [Id] => 176
                    [MachineId] => 1
                    [MachineName] => ГА-1
                    [DebitId] => 4507039
                    [ReelId] => 3
                    [ReelSide] => 0
                    [TimeBegin] => 27.08.2020 13:20:08
                    [TimeEnd] => 27.08.2020 14:01:12
                    [SourceName] => Бумага Б1 70/2100
                    [RollReelId] => 176

                Cell:
                    Id
                    TimeBegin
                    TimeEnd
                    Row (etc)
              
             */

            /*
                минимальная длина сегмента, мин
                если сегмент короче, будет растянут до этого значения
             */
            int minElementLen = 0;

            //текущие даты, формат: yyyy-MM-dd
            double startSecond  = MonitorStartDT.ConvertToUnixTimestamp();

            var items=ds.Items;

            if(items != null)
            {
                foreach(Dictionary<string,string> r in items)
                {
                    int rowId = 0;

                    /*
                         Id=110
                            |||
                            ||- Side
                            |-- ReelId
                            --- Machine
                     */
                    var rowStr=$"{r.CheckGet("MACHINE_ID").ToInt().ToString()}{r.CheckGet("REEL_ID").ToInt().ToString()}{r.CheckGet("REEL_SIDE").ToInt().ToString()}";
                    rowId=rowStr.ToInt();

                    if(rowId != 0)
                    {
                        //флаг отображения сегмента 
                        //(рендер не будет разбираться с данными, а отрисует все, что есть в стеке)
                        bool show = true;
                        //флаг продолжения 
                        bool resume = true;

                        var c = new RollMonitorGridCell();
                        c.Id        = r.CheckGet("ID").ToString();
                        c.TimeBegin = r.CheckGet("TIME_BEGIN").ToString();
                        c.TimeEnd   = r.CheckGet("TIME_END").ToString();
                        c.Row=r;

                        //координаты начала и конца не должны быть пустыми
                        if(resume)
                        {
                            if(string.IsNullOrEmpty(c.TimeBegin))
                            {
                                resume=false;
                                show=false;
                            }

                            if(string.IsNullOrEmpty(c.TimeEnd))
                            {
                                resume=false;
                                show=false;
                            }
                        }
                        
                        if(show)
                        {
                            var timeBeginDT = DateTime.Parse(c.TimeBegin);
                            double timeBeginSecond=timeBeginDT.ConvertToUnixTimestamp();
                            
                            // индекс первой ячейки
                            c.Index=(int)(timeBeginSecond-startSecond);
                            
                            var timeEndDT = DateTime.Parse(c.TimeEnd);
                            double timeEndSecond=timeEndDT.ConvertToUnixTimestamp();

                            // индекс последней ячейки
                            var lastCell=(int)(timeEndSecond - startSecond);

                            // начальная координата
                            c.X=(int)(c.Index * CellWidth);

                            // конечная координата
                            c.End = (int)(lastCell * CellWidth);
                                
                            c.Len=(int)(timeEndSecond-timeBeginSecond);

                            c.Width=(int)(c.End - c.X) + 1;

                            if(Rows.ContainsKey(rowId))
                            {
                                if(!Rows[rowId].Rolls.ContainsKey(c.Id))
                                {
                                    Rows[rowId].Rolls.Add(c.Id,c);
                                    WorksCount++;
                                }
                            }
                        }
                       
                    }
                }
            }  
            
            var r0=Rows;

            //Central.Dbg($"LoadRolls cells:{WorksCount}");
            
        }


         /// <summary>
        /// Загрузка элементов: рулоны      
        /// </summary>
        public void LoadRollsActivities(ListDataSet ds)
        {
            /*
                Server.Modules.Production.TaskTime.List

                Source:

                    [Id] => 13
                    [RollReelId] => 123
                    [TimeBegin] => 27.08.2020 11:48:14
                    [TimeEnd] => 27.08.2020 12:08:35
                    [MachineId] => 1
                    [ReelId] => 1
                    [ReelSide] => 1

                Cell:
                    Id
                    TimeBegin
                    TimeEnd
                    Row (etc)
              
             */

            /*
                минимальная длина сегмента, мин
                если сегмент короче, будет растянут до этого значения
             */
            int minElementLen = 0;

            //текущие даты, формат: yyyy-MM-dd
            double startSecond  = MonitorStartDT.ConvertToUnixTimestamp();

            var items=ds.Items;

            if(items != null)
            {
                foreach(Dictionary<string,string> r in items)
                {
                    int rowId = 0;
                   
                   

                    /*
                         Id=110
                            |||
                            ||- Side
                            |-- ReelId
                            --- Machine
                     */
                    var rowStr=$"{r.CheckGet("MACHINE_ID").ToInt().ToString()}{r.CheckGet("REEL_ID").ToInt().ToString()}{r.CheckGet("REEL_SIDE").ToInt().ToString()}";
                    rowId=rowStr.ToInt();

                    if(rowId != 0)
                    {
                        //флаг отображения сегмента 
                        //(рендер не будет разбираться с данными, а отрисует все, что есть в стеке)
                        bool show = true;
                        //флаг продолжения 
                        bool resume = true;

                        var c = new RollMonitorGridCell();
                        c.Id        = r.CheckGet("ID").ToString();
                        c.TimeBegin = r.CheckGet("TIME_BEGIN").ToString();
                        c.TimeEnd   = r.CheckGet("TIME_END").ToString();
                        c.Row=r;

                        //координаты начала и конца не должны быть пустыми
                        if(resume)
                        {
                            if(string.IsNullOrEmpty(c.TimeBegin))
                            {
                                resume=false;
                                show=false;
                            }

                            if(string.IsNullOrEmpty(c.TimeEnd))
                            {
                                resume=false;
                                show=false;
                            }
                        }
                        
                        if(show)
                        {
                            var timeBeginDT = DateTime.Parse(c.TimeBegin);
                            double timeBeginSecond = timeBeginDT.ConvertToUnixTimestamp();

                            // индекс первой ячейки
                            c.Index = (int)(timeBeginSecond - startSecond);

                            var timeEndDT = DateTime.Parse(c.TimeEnd);
                            double timeEndSecond = timeEndDT.ConvertToUnixTimestamp();

                            // индекс последней ячейки
                            var lastCell = (int)(timeEndSecond - startSecond);

                            // начальная координата
                            c.X = (int)(c.Index * CellWidth);

                            // конечная координата
                            c.End = (int)(lastCell * CellWidth);

                            c.Len = (int)(timeEndSecond - timeBeginSecond);

                            c.Width = (int)(c.End - c.X) + 1;

                            if (Rows.ContainsKey(rowId))
                            {
                                if(!Rows[rowId].RollsActivities.ContainsKey(c.Id))
                                {
                                    Rows[rowId].RollsActivities.Add(c.Id,c);
                                    IdlesCount++;
                                }
                            }
                        }
                       
                    }
                }
            }  
            
            var r0=Rows;

            //Central.Dbg($"LoadRollsActivities cells:{IdlesCount}");
            
        }

        
        /// <summary>
        /// Подготовка данных строк
        /// </summary>
        public void PrepareRows()
        {
            //Central.Dbg($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] PrepareRows");

            /*
                по умолчанию будут показаны строки, где есть текущие отгрузки
                в режиме фильтра (покзать все) будут показаны все терминалы
             */
            if(Rows.Count > 0)
            {
                int j = 0;
                foreach(KeyValuePair<int,RollMonitorGridRow> item in Rows)
                {
                    var row = item.Value;

                    bool show = true;

                    if(show)
                    {
                        row.Index=j;
                        row.Show=true;
                        j++;
                    }


                    if(show)
                    {
                        if( row.RollsActivities.Count>0 && row.Rolls.Count>0 )
                        {
                            foreach( KeyValuePair<string, RollMonitorGridCell> w in row.Rolls )
                            {

                                //work-work
                                foreach( KeyValuePair<string, RollMonitorGridCell> w2 in row.Rolls )
                                {                                    
                                    if( w.Value.TimeEnd == w2.Value.TimeBegin )
                                    {
                                        w.Value.Width=w2.Value.X-w.Value.X+1;                                        
                                        w.Value.End=w.Value.X+w.Value.Width;
                                    }
                                }

                                //work-idle
                                foreach( KeyValuePair<string, RollMonitorGridCell> i in row.RollsActivities )
                                {                                    
                                    if( w.Value.TimeEnd == i.Value.TimeBegin )
                                    {
                                        w.Value.Width=i.Value.X-w.Value.X+1;                                        
                                        w.Value.End=w.Value.X+w.Value.Width;
                                    }
                                }

                            }

                            foreach( KeyValuePair<string, RollMonitorGridCell> i in row.RollsActivities )
                            {
                                //idle-idle
                                foreach( KeyValuePair<string, RollMonitorGridCell> i2 in row.RollsActivities )
                                {                                    
                                    if( i.Value.TimeEnd == i2.Value.TimeBegin )
                                    {
                                        i.Value.Width=i2.Value.X-i.Value.X+1;                                        
                                        i.Value.End=i.Value.X+i.Value.Width;
                                    }
                                }

                                //idle-work
                                foreach( KeyValuePair<string, RollMonitorGridCell> w in row.Rolls )
                                {                                    
                                    if( i.Value.TimeEnd == w.Value.TimeBegin )
                                    {
                                        i.Value.Width=w.Value.X-i.Value.X+1;                                        
                                        i.Value.End=i.Value.X+i.Value.Width;
                                    }
                                }
                            }

                        }
                    }

                }
                RowsCount=j;
            }

            var r0=Rows;
            var rr=0;

            if(Central.DebugMode)
            {
                Central.Dbg($"PrepareRows: rows=[{RowsCount}]");                
            }
        }

        /// <summary>
        /// Очистка данных
        /// </summary>
        public void Clear()
        {

            if(HeadersContainer != null)
            {
                HeadersContainer.Children.Clear();
                HeadersContainer.RowDefinitions.Clear();
                HeadersContainer.ColumnDefinitions.Clear();
            }

            
            if(DataContainer != null)
            {
                DataContainer.Children.Clear();
                DataContainer.RowDefinitions.Clear();
                DataContainer.ColumnDefinitions.Clear();
            }
            

            if(TimelineContainer != null)
            {
                TimelineContainer.Children.Clear();
                TimelineContainer.RowDefinitions.Clear();
                TimelineContainer.ColumnDefinitions.Clear();
            }

            Rows.Clear();

        }
        
        /// <summary>
        /// Отрисовка грида (сетка, маркеры, заголовки, таймлайн)
        /// </summary>
        public void RenderGrid()
        {
            
            /*
            Central.Dbg($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] RenderGrid");

            //YYYY-MM-DD
            */

            if( HeadersContainer == null || DataContainer == null )
            {
                return;
            }


           

            /*
                флажок отображения маркера текущего времени
                если мы смотрим данные за текущий день, маркер будет показан
                текущий день мы определяем по левому краю данных
                    данные у нас показаны от 8 утра сегодня до 8 утра завтра (относительно выбранной даты)
                маркер -- вертикальная красная линия
             */
            bool showNowMarker = false;
            if( MonitorLeftDT.ToString("yyyy-MM-dd") == DateTime.Now.ToString("yyyy-MM-dd") )
            {
                showNowMarker=true;
            }


            //прогон строк: вычисление высоты
            //сумма высот блоков (фактически: текущая координата Y)
            int PositionCounterY = 0;
            if(Rows.Count > 0)
            {
                foreach(KeyValuePair<int,RollMonitorGridRow> item in Rows)
                {
                    var row = item.Value;
                    //var rowIndex=item.Key;
                    var rowIndex = row.Index;

                    //кастомная высота строки
                    var height = RowHeight;
                    if(row.Row.CheckGet("HEIGHT").ToInt()>0)
                    {
                        height=row.Row.CheckGet("HEIGHT").ToInt();
                    }                    
                    row.Height=height;

                    //записываем координату Y для каждой строки, 
                    //в дальнейшем рендерить блок будем с этой ординатой
                    row.PositionY=PositionCounterY;
                    PositionCounterY=PositionCounterY+height;
                }

                MaxDataHeight=PositionCounterY;
            }


            //cols
            if(true)
            {
                bool resume=true;
                DateTime currentDT=MonitorStartDT;
                double startSecond  = MonitorStartDT.ConvertToUnixTimestamp();                
                double finishSecond = MonitorFinishDT.ConvertToUnixTimestamp();                                
                int hourSecondsCounter=1800;
                int colIndex=0;      
                                            
                //Central.Dbg($">> => {finishSecond}");

                while(resume)
                {
                    //render
                    double currentSecond=currentDT.ConvertToUnixTimestamp();
                    colIndex=(int)(currentSecond-startSecond);

                    //Central.Dbg($"    {currentSecond}  {nowSecond}");
                    //Central.Dbg($" > {currentDT.ToString("HH:mm:ss")} {colIndex}");

                    //начало часа, временные метки
                    if( hourSecondsCounter==0 )
                    {
                        var cell = new Border();
                        cell.Width=50;
                            
                        var label = new TextBlock();
                        label.Style = (Style)HeadersContainer.FindResource("TTMonTimelineLabel");
                        label.Text=currentDT.ToString("HH:mm");
                                
                        int x=(int)(colIndex*CellWidth);
                        x=x-16;
                        int y=0;
                        label.Margin=new Thickness(x,y,0,0);

                        TimelineContainer.Children.Add(label);
                    }

                    //вертикальная сетка, каждые N минут, N=TimeInterval
                    //самую первую линию не рисуем, слипается с контейнером
                    //if(gridCounter ==  0 && colIndex != 0)                    
                    if( hourSecondsCounter==0 )
                    {
                        var vLine = new Border();
                        vLine.Style = (Style)DataContainer.FindResource("TTMonGridVerticalDevider");
                        //vLine.Height= RowHeight*RowsCount;
                        vLine.Height= PositionCounterY;
                        vLine.Width=1;
                        int x=(int)(colIndex*CellWidth);
                        int y=0;
                        vLine.Margin=new Thickness(x,y,0,0);
                        DataContainer.Children.Add(vLine);
                        ElementsCount++;
                    }

                    
                    
                    

                    currentDT=currentDT.AddSeconds(MonitorStep);                    
                    
                    //счетчик секунд
                    hourSecondsCounter=hourSecondsCounter+MonitorStep;                    
                    if( hourSecondsCounter >= 3600 )
                    {
                        hourSecondsCounter=0;
                    }

                    if( currentSecond >= finishSecond )
                    {
                        resume=false;
                    }
                }

                ColsCount=colIndex;
            }

            //rows
            if(true)
            { 
                if(Rows.Count > 0)
                {
                    
                    foreach(KeyValuePair<int,RollMonitorGridRow> item in Rows)
                    {
                        var row = item.Value;
                        //var rowIndex=item.Key;
                        var rowIndex = row.Index;

                        //отрендерим только те строки, где есть данные                        
                        if(row.Show)
                        {

                            /*
                                каждая ячейка -- прямоугольник
                                применяется абсолютное позиционирование внутри контенера путем
                                установки отступа слева и сверху
                                "координаты" рассчитываются в предварительном прогоне 
                                в начале функции
                             */
                                
                            var cell = new Border();
                            cell.Style = (Style)HeadersContainer.FindResource("SHMonTermGridHeaderCell");
                                
                            cell.HorizontalAlignment=HorizontalAlignment.Stretch;
                            cell.VerticalAlignment=VerticalAlignment.Top;

                            cell.BorderThickness=new Thickness(0,0,0,0);

                            var borderHeight=row.Height;
                            var borderBottom=1;
                            if(row.Row.CheckGet("SIDE").ToInt()==1)
                            {
                                //у левого раскате нет бордера снизу
                                borderBottom=0;
                            }
                            else
                            {
                                //компенсация высоты
                                borderHeight++;
                            }
                                
                            if(row.Row.CheckGet("TYPE").ToString().ToLower()=="reel")
                            {
                                cell.BorderThickness=new Thickness(1,1,0,borderBottom);
                            }

                            cell.Height=borderHeight;
                            
                            int x=0;                                
                            int y=row.PositionY;    
                            cell.Margin=new Thickness(x,y,0,0);


                            //правая колонка, область данных
                            DataContainer.Children.Add(cell);
                            ElementsCount++;


                            //левая колонка, заголовки строк
                            var cellLeft= UIUtil.DeepCopy(cell);
                                
                            var label = new TextBlock();
                            label.Text=row.Title;
                            label.Style = (Style)HeadersContainer.FindResource("SHMonTermGridHeaderTextLabel");
                            cellLeft.Child=label;
                            HeadersContainer.Children.Add(cellLeft);
                        }
                    }
                }
            }


            //маркер текущего времени
            if( showNowMarker )
            {

                double nowSecond=DateTime.Now.ConvertToUnixTimestamp();                        
                double startSecond  = MonitorStartDT.ConvertToUnixTimestamp();          
                var colIndex=(int)(nowSecond-startSecond);
                CenterCol=colIndex;
                                
                {
                    var vLine = new Border();
                    vLine.Style = (Style)DataContainer.FindResource("TTMonGridVerticalMarker");
                    //vLine.Height= RowHeight*RowsCount;
                    vLine.Height= PositionCounterY;
                    vLine.Width=1;
                    int x=(int)(colIndex*CellWidth);
                    int y=0;
                    vLine.Margin=new Thickness(x,y,0,0);
                    DataContainer.Children.Add(vLine);
                    ElementsCount++;
                }
            }

            //Central.Dbg($"    Elements: {ElementsCount} ");
        }
        
            
        /// <summary>
        /// Отрисовка данных грида: рулоны
        /// </summary>
        public void RenderRolls()
        {
            //Central.Dbg($"RenderReels");

            //double barHeight=0.66;
            double barHeight=1.0;

            if(Rows.Count > 0)
            {
                if(DataContainer != null)
                {
                    if(WorksCount > 0)
                    {
                        foreach(KeyValuePair<int,RollMonitorGridRow> RowPair in Rows)
                        {
                            var row = RowPair.Value;
                            var rowIndex = row.Index;

                            if(row.Show)
                            {
                                foreach(KeyValuePair<string,RollMonitorGridCell> CellPair in row.Rolls)
                                {
                                    var cell = CellPair.Value;
                                    var colIndex = cell.Index;
                                    
                                    var element = new Border();
                                    element.Style = (Style)DataContainer.FindResource("TTMonGridElement");
                                    element.Height=(RowHeight*barHeight)+1;
                                    //int w=(int)(Math.Round(cell.Len*CellWidth))+1;
                                    int w=(int)(Math.Round(cell.Len*CellWidth));
                                    //element.Width=w;                                    
                                    element.Width=cell.Width;
                                    element.VerticalAlignment=VerticalAlignment.Top;
                                    //int x=(int)(Math.Round(colIndex*CellWidth));
                                    int x=cell.X;
                                    //int y=rowIndex*RowHeight;
                                    int y=row.PositionY;


                                    element.Margin=new Thickness(x,y+((RowHeight*(1-barHeight))),0,0);

                                   
                                    

                                    /*
                                    //название задания покажем на баре
                                    string taskNumber="";
                                    if( cell.Row.Values.ContainsKey("ProductionTaskNumber") )
                                    {
                                        taskNumber=cell.Row.Values["ProductionTaskNumber"];

                                        var label = new TextBlock();
                                        label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                        label.Text=$"{taskNumber}";
                                        label.Margin=new Thickness(2,0,0,0);
                                        label.FontSize=11;
                                        element.Child = label;
                                    }
                                    */                                   

                                    
                                    var toolTip=new Border();
                                    {
                                        var g=new StackPanel();
                                        g.Orientation=Orientation.Vertical;

                                        {
                                            var label = new TextBlock();
                                            label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                            label.Text=$"Бумага: {cell.Row.CheckGet("SOURCE_NAME")}";
                                            g.Children.Add(label);
                                        }

                                        {
                                            var label = new TextBlock();
                                            label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                            label.Text=$"ID рулона: {cell.Row.CheckGet("DEBIT_ID").ToInt()}";
                                            g.Children.Add(label);
                                        }

                                        {
                                            var label = new TextBlock();
                                            label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                            label.Text=$"PCRO_ID: {cell.Row.CheckGet("ID").ToInt()}";
                                            g.Children.Add(label);
                                        }

                                        {
                                            var label = new TextBlock();
                                            label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                            label.Text=$"Время начала:       {cell.Row.CheckGet("TIME_BEGIN")}";
                                            g.Children.Add(label);
                                        }

                                        {
                                            var label = new TextBlock();
                                            label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                            label.Text=$"Время окончания: {cell.Row.CheckGet("TIME_END")}";
                                            g.Children.Add(label);
                                        }


                                        /*
                                        if( Central.DebugMode )
                                        {
                                            {
                                                var label = new TextBlock();
                                                label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                                label.Text=$"M:{cell.Row.CheckGet("MACHINE_ID")} R:{cell.Row.CheckGet("REEL_ID")} S:{cell.Row.CheckGet("REEL_SIDE")} pcro_id:{cell.Row.CheckGet("ID")}";
                                                g.Children.Add(label);
                                            }
                                        }
                                        */

                                        
                                        toolTip.Child=g;
                                    }
                                    element.ToolTip=toolTip;

                                    

                                    var bgColor = HColor.Gray;
                                    var bc = new BrushConverter();
                                    var brush = (Brush)bc.ConvertFrom(bgColor);
                                    element.Background=brush;
                                    
                                    DataContainer.Children.Add(element);
                                    ElementsCount++;

                                }
                            }
                        }

                    }
                }
            }

            //Central.Dbg($"    Elements: {ElementsCount} ");
        }


        /// <summary>
        /// Отрисовка данных грида: рулоны
        /// </summary>
        public void RenderRollsActivities()
        {
            //Central.Dbg($"RenderRollsActivities");

            //double barHeight=0.66;
            double barHeight=0.66;

            if(Rows.Count > 0)
            {
                if(DataContainer != null)
                {
                    if(WorksCount > 0)
                    {
                        foreach(KeyValuePair<int,RollMonitorGridRow> RowPair in Rows)
                        {
                            var row = RowPair.Value;
                            var rowIndex = row.Index;

                            if(row.Show)
                            {
                                foreach(KeyValuePair<string,RollMonitorGridCell> CellPair in row.RollsActivities)
                                {
                                    var cell = CellPair.Value;
                                    var colIndex = cell.Index;
                                    
                                    var element = new Border();
                                    element.Style = (Style)DataContainer.FindResource("TTMonGridElement");
                                    element.Height=(RowHeight*barHeight)+1;
                                    //int w=(int)(Math.Round(cell.Len*CellWidth))+1;
                                    int w=(int)(Math.Round(cell.Len*CellWidth));
                                    //element.Width=w;    
                                    if(cell.Width<0)
                                    {
                                        cell.Width=0;
                                    }
                                    element.Width=cell.Width;
                                    element.VerticalAlignment=VerticalAlignment.Top;
                                    //int x=(int)(Math.Round(colIndex*CellWidth));
                                    int x=cell.X;
                                    //int y=rowIndex*RowHeight;
                                    int y=row.PositionY;
                                    element.Margin=new Thickness(x,y+((RowHeight*(1-barHeight))),0,0);


                                     /*
                                        1 -- левый  -- вниз
                                        2 -- правый -- вверх
                                     */
                                    if(row.Row.CheckGet("SIDE").ToInt()==1)
                                    {
                                        element.Margin=new Thickness(x,y+((RowHeight*(1-barHeight))),0,0);    
                                    }

                                    if(row.Row.CheckGet("SIDE").ToInt()==2)
                                    {
                                        element.Margin=new Thickness(x,y,0,0);    
                                    }


                                    /*
                                    //название задания покажем на баре
                                    string taskNumber="";
                                    if( cell.Row.Values.ContainsKey("ProductionTaskNumber") )
                                    {
                                        taskNumber=cell.Row.Values["ProductionTaskNumber"];

                                        var label = new TextBlock();
                                        label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                        label.Text=$"{taskNumber}";
                                        label.Margin=new Thickness(2,0,0,0);
                                        label.FontSize=11;
                                        element.Child = label;
                                    }
                                    */                                   

                                    
                                    var toolTip=new Border();
                                    {
                                        var g=new StackPanel();
                                        g.Orientation=Orientation.Vertical;

                                        


                                        {
                                            var label = new TextBlock();
                                            label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                            label.Text=$"Время начала:       {cell.Row.CheckGet("TIME_BEGIN")}";
                                            g.Children.Add(label);
                                        }

                                        {
                                            var label = new TextBlock();
                                            label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                            label.Text=$"Время окончания: {cell.Row.CheckGet("TIME_END")}";
                                            g.Children.Add(label);
                                        }

                                        {
                                            var label = new TextBlock();
                                            label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                            label.Text=$"PCRO_ID:       {cell.Row.CheckGet("ROLL_REEL_ID").ToInt()}";
                                            g.Children.Add(label);
                                        }

                                        {
                                            var label = new TextBlock();
                                            label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                            label.Text=$"PCRA_ID:       {cell.Row.CheckGet("ID").ToInt()}";
                                            g.Children.Add(label);
                                        }

                                        {
                                            var label = new TextBlock();
                                            label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                            label.Text=$"LENGTH:       {cell.Row.CheckGet("LENGTH").ToInt()}";
                                            g.Children.Add(label);
                                        }

                                        /*
                                        if( Central.DebugMode )
                                        {
                                            {
                                                var label = new TextBlock();
                                                label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                                label.Text=$"M:{cell.Row.CheckGet("MACHINE_ID")} R:{cell.Row.CheckGet("REEL_ID")} S:{cell.Row.CheckGet("REEL_SIDE")} RollReelId:{cell.Row.CheckGet("ROLL_REEL_ID")}";
                                                g.Children.Add(label);
                                            }
                                        }
                                        */

                                        
                                        toolTip.Child=g;
                                    }
                                    element.ToolTip=toolTip;

                                    

                                    var bgColor = HColor.Green;
                                    var bc = new BrushConverter();
                                    var brush = (Brush)bc.ConvertFrom(bgColor);
                                    element.Background=brush;
                                    
                                    DataContainer.Children.Add(element);
                                    ElementsCount++;

                                }
                            }
                        }

                    }
                }
            }

            //Central.Dbg($"    Elements: {ElementsCount} ");
        }
    }
}
