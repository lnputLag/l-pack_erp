using Client.Assets.HighLighters;
using Client.Common;
using Client.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Client.Interfaces.Production.ProcessingMachines
{
    /// <summary>
    /// Структура данных и рендер грида
    /// (Диаграмма ПЗ на переработке)
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>2</version>
    /// <released>2021-11-23</released>
    public class ProductionTaskMonitorGrid
    {
        /*
            Структура данных со своими внутренними вспомогательными структурами
            и функции рендера данных.
            Входящие данные загружаются во внутренние регистры, дальнейшая работа
            с данними идет с их внутренним представлением.
         */
        public Dictionary<int, ProductionTaskMonitorGridRow> Rows { get; set; }

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
        public Dictionary<string, int> Cols { get; set; }

        /// <summary>
        /// Колонка (ID) в которой стоит маркет текущего времени (метка "сейчас")
        /// </summary>
        public int CenterCol { get; set; }

        /// <summary>
        /// Первая временная метка в гриде. (формат: yyyy-MM-dd_HH:mm )
        /// Если событие началось за гранью монитора слева, его абсцисса будет равна FirstTime.
        /// </summary>
        private string FirstTime { get; set; }
        /// <summary>
        /// Последняя временная метка в гриде (формат: yyyy-MM-dd_HH:mm )
        /// </summary>
        private string LastTime { get; set; }
        /// <summary>
        /// (формат: yyyy-MM-dd_HH:mm )
        /// </summary>
        private string CenterTime { get; set; }
        /// <summary>
        /// Список строк
        /// </summary>
        private Dictionary<int, string> RowTitleList { get; set; }
        private Dictionary<int, int> TaskCounter { get; set; }

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

        /// <summary>
        /// Кнструктор
        /// </summary>
        /// <param name="today"></param>
        public ProductionTaskMonitorGrid(DateTime today)
        {
            //начало отсчета, строка
            TimeStart = "07:30";            
            TimeStartShipment = "08:00";

            CellWidth = 0.20;
            RowHeight = 60;
            XFormat = "yyyy-MM-dd_HH:mm:ss";
            XStep = "01:00";
            
            TodayDT = today;

            //границы монитора
            string monitorLeft = $"{TodayDT:yyyy-MM-dd} 07:30:00";            
            MonitorLeftDT = DateTime.ParseExact(monitorLeft, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            string monitorRight = $"{TodayDT.AddDays(1):yyyy-MM-dd} 08:00:00";            
            MonitorRightDT = DateTime.ParseExact(monitorRight, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            Central.Dbg($"Monitor L-R: [{MonitorLeftDT:yyyy-MM-dd HH:mm:ss}] -> [{MonitorRightDT:yyyy-MM-dd HH:mm:ss}]");

            string monitorStart = $"{TodayDT:yyyy-MM-dd} 07:30:00";            
            MonitorStartDT = DateTime.ParseExact(monitorStart, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            string monitorFinish = $"{TodayDT.AddDays(1):yyyy-MM-dd} 08:00:00";            
            MonitorFinishDT = DateTime.ParseExact(monitorFinish, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            Central.Dbg($"Monitor S-F: [{MonitorStartDT:yyyy-MM-dd HH:mm:ss}] -> [{MonitorFinishDT:yyyy-MM-dd HH:mm:ss}]");

            //sec
            MonitorStep = 5;

            /*
                текущее время мы фиксируем, чтобы все внутрение механизмы опирались
                на одно значение
            */
            TimeHoursNow = DateTime.Now.ToString("HH:mm");
            DateNow = DateTime.Now.ToString("yyyy-MM-dd");

            //кличество точек, одна точка-1 мин.
            TimeTicks = 24 * 60 + 30;
            //интервал сетки по горизонтали, мин
            TimeGridInterval = 60;
            //интервал временных меток на шкале
            TimeLabelInterval = 60;
            TimeLabelOffset = 30;

            CellsCount = 0;
            ColsCount = 0;
            RowsCount = 0;
            WorksCount = 0;
            IdlesCount = 0;
            CounterCount = 0;

            ElementsCount = 0;

            FirstTime = "";
            LastTime = "";
            CenterTime = "";

            CenterCol = 0;

            Rows = new Dictionary<int, ProductionTaskMonitorGridRow>();
            Cols = new Dictionary<string, int>();
            TaskCounter = new Dictionary<int, int>();

            Central.Dbg($"    TimeHoursNow:[{TimeHoursNow}] DateNow:[{DateNow}] ");

            XStepMin = 1;
            XStepSec = 0;
            if (!string.IsNullOrEmpty(XStep))
            {
                string[] t = XStep.Split(':');
                if (!string.IsNullOrEmpty(t[0]))
                {
                    XStepMin = int.Parse(t[0]);
                }
                if (!string.IsNullOrEmpty(t[1]))
                {
                    XStepSec = int.Parse(t[1]);
                }
            }

        }

        /// <summary>
        /// Загрузка заголовков строк: названия машин
        /// </summary>
        /// <param name="items"></param>
        public void LoadTitles(ListDataSet ds)
        {
            var items = ds.Items;
            if (items != null)
            {
                /*
                    Подготовка структуры данных: строки
                 */
                int rowIndex = 0;
                foreach (var r in items)
                {

                    int rowId = r.CheckGet("ID").ToInt();
                    if (rowId != 0)
                    {
                        if (!Rows.ContainsKey(rowId))
                        {
                            var row = new ProductionTaskMonitorGridRow()
                            {
                                Index = rowIndex,
                                Title = $"{r.CheckGet("MACHINE_NAME")}",
                                Row = r
                            };
                            Rows.Add(rowId, row);
                            rowIndex++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Загрузка элементов: производственные задания      
        /// </summary>
        public void LoadWorks(ListDataSet ds)
        {
            /*
                Server.Modules.Production.TaskTime.List

                DataStruct:
                    Rows[MachineId]=>ShipmentMonitorGridCell
                    {
                        { "Id", "s" },      
                        { "MachineId", "s" },      
                        { "MachineName", "s" },      
                        { "TimeBegin", "s" },      
                        { "TimeEnd", "s" },      
                        { "TimeLenght", "s" },      
                        { "ProductionTaskId", "s" }, 
                    }
              
             */

            /*
                минимальная длина сегмента, мин
                если сегмент короче, будет растянут до этого значения
             */
            int minElementLen = 0;

            double startSecond = MonitorStartDT.ConvertToUnixTimestamp();

            var items = ds.Items;
            if (items != null)
            {
                foreach (Dictionary<string,string> r in items)
                {
                    int rowId = r.CheckGet("MACHINE_ID").ToInt();
                    if (rowId != 0)
                    {
                        //флаг отображения сегмента 
                        //(рендер не будет разбираться с данными, а отрисует все, что есть в стеке)
                        bool show = true;
                        //флаг продолжения 
                        bool resume = true;

                        var c = new ProductionTaskMonitorGridCell();
                        c.Id = r.CheckGet("ID").ToString();
                        c.TimeBegin = r.CheckGet("TIME_BEGIN").ToString();
                        c.TimeEnd = r.CheckGet("TIME_END").ToString();
                        c.Row = r;

                        //координаты начала и конца не должны быть пустыми
                        if (resume)
                        {
                            if (string.IsNullOrEmpty(c.TimeBegin))
                            {
                                resume = false;
                                show = false;
                            }

                            if (string.IsNullOrEmpty(c.TimeEnd))
                            {
                                resume = false;
                                show = false;
                            }
                        }

                        if (show)
                        {
                            var timeBeginDT = DateTime.Parse(c.TimeBegin);
                            double timeBeginSecond = timeBeginDT.ConvertToUnixTimestamp();
                            c.Index = (int)(timeBeginSecond - startSecond);

                            var timeEndDT = DateTime.Parse(c.TimeEnd);
                            double timeEndSecond = timeEndDT.ConvertToUnixTimestamp();
                            c.Len = (int)(timeEndSecond - timeBeginSecond);


                            c.X = (int)(Math.Round(c.Index * CellWidth));
                            c.Width = (int)(Math.Round(c.Len * CellWidth)) + 1;
                            c.End = c.X + c.Width;

                            if (Rows.ContainsKey(rowId))
                            {
                                if (!Rows[rowId].Works.ContainsKey(c.Id))
                                {
                                    //(16:14:36) Левин Владимир: нет, в этом случае не нужно совсем выводить эту часть диаграммы
                                    //(16:14:59) Левин Владимир: проверяй на входе, если конец больше начала, то не берем эту строку в рассмотрение                                    
                                    if (c.Len > 0)
                                    {
                                        Rows[rowId].Works.Add(c.Id, c);
                                        WorksCount++;
                                    }

                                }
                            }
                        }
                    }
                }
            }

            Central.Dbg($"LoadWorks cells:{WorksCount}");
        }

        /// <summary>
        /// Загрузка элементов: производственные простои
        /// </summary>
        /// <param name="items"></param>
        public void LoadIdles(ListDataSet ds)
        {
            /*
                Server.Modules.Production.TaskTime.List

                DataStruct:
                    Rows[MachineId]=>ShipmentMonitorGridCell
                    {
                        { "Id", "s" },      
                        { "MachineId", "s" },      
                        { "MachineName", "s" },      
                        { "TimeBegin", "s" },      
                        { "TimeEnd", "s" },      
                        { "ReasonId", "s" },      
                        { "ReasonName", "s" },      
                        { "IdleLength", "s" },  
                    }
              
             */

            /*
                минимальная длина сегмента, мин
                если сегмент короче, будет растянут до этого значения
             */
            int minElementLen = 0;

            double startSecond = MonitorStartDT.ConvertToUnixTimestamp();
            var items = ds.Items;
            
            if (items != null)
            {
                foreach (Dictionary<string,string> r in items)
                {
                    int rowId = r.CheckGet("MACHINE_ID").ToInt();
                    if (rowId != 0)
                    {
                        //флаг отображения сегмента 
                        //(рендер не будет разбираться с данными, а отрисует все, что есть в стеке)
                        bool show = true;
                        //флаг продолжения 
                        bool resume = true;

                        var c = new ProductionTaskMonitorGridCell();
                        c.Id = r.CheckGet("ID").ToString();
                        c.TimeBegin = r.CheckGet("TIME_BEGIN").ToString();
                        c.TimeEnd = r.CheckGet("TIME_END").ToString();
                        c.Row = r;

                        //координаты начала и конца не должны быть пустыми
                        if (resume)
                        {
                            if (string.IsNullOrEmpty(c.TimeBegin))
                            {
                                resume = false;
                                show = false;
                            }

                            if (string.IsNullOrEmpty(c.TimeEnd))
                            {
                                resume = false;
                                show = false;
                            }
                        }

                        if (show)
                        {
                            var timeBeginDT = DateTime.Parse(c.TimeBegin);
                            double timeBeginSecond = timeBeginDT.ConvertToUnixTimestamp();
                            c.Index = (int)(timeBeginSecond - startSecond);

                            var timeEndDT = DateTime.Parse(c.TimeEnd);
                            double timeEndSecond = timeEndDT.ConvertToUnixTimestamp();
                            c.Len = (int)(timeEndSecond - timeBeginSecond);

                            c.X = (int)(Math.Round(c.Index * CellWidth));
                            c.Width = (int)(Math.Round(c.Len * CellWidth)) + 1;
                            c.End = c.X + c.Width;

                            if (Rows.ContainsKey(rowId))
                            {
                                if (!Rows[rowId].Idles.ContainsKey(c.Id))
                                {
                                    if (c.Len > 0)
                                    {
                                        Rows[rowId].Idles.Add(c.Id, c);
                                        IdlesCount++;
                                    }
                                }
                            }
                        }

                    }
                }
            }

            Central.Dbg($"LoadIdles cells:{IdlesCount}");
        }

        /// <summary>
        /// Загрузка элементов: отсчеты каунтера
        /// </summary>
        /// <param name="items"></param>
        public void LoadCounters(ListDataSet ds)
        {
            /*
                Server.Modules.Production.TaskTime.List

                DataStruct:
                    Rows[MachineId]=>ShipmentMonitorGridCell
                    {
                        { "Id", "s" },      
                        { "Time", "s" },  
                    }              
             */

            /*
                минимальная длина сегмента, мин
                если сегмент короче, будет растянут до этого значения
             */
            int minElementLen = 0;

            //текущие даты, формат: yyyy-MM-dd
            //var todayDate = TodayDT.ToString("yyyy-MM-dd");
            //var tomorrowDate = TodayDT.AddDays(1).ToString("yyyy-MM-dd");
            double startSecond = MonitorStartDT.ConvertToUnixTimestamp();

            int itemId = 0;

            var items = ds.Items;
            if (items != null)
            {
                foreach (Dictionary<string,string> r in items)
                {

                    int rowId = r.CheckGet("ID").ToInt();
                    if (rowId != 0)
                    {
                        //флаг отображения сегмента 
                        //(рендер не будет разбираться с данными, а отрисует все, что есть в стеке)
                        bool show = true;
                        //флаг продолжения 
                        bool resume = true;

                        var c = new ProductionTaskMonitorGridCell();
                        c.Id = itemId.ToString();
                        itemId++;
                        c.TimeEnd = r.CheckGet("TIME").ToString();
                        var t = DateTime.Parse(c.TimeEnd);
                        c.TimeBegin = t.AddMinutes(-1).ToString("dd.MM.yyyy HH:mm:ss");
                        c.Row = r;

                        //координаты начала и конца не должны быть пустыми
                        if (resume)
                        {
                            if (string.IsNullOrEmpty(c.TimeBegin))
                            {
                                resume = false;
                                show = false;
                            }

                            if (string.IsNullOrEmpty(c.TimeEnd))
                            {
                                resume = false;
                                show = false;
                            }
                        }

                        if (show)
                        {
                            var timeBeginDT = DateTime.Parse(c.TimeBegin);
                            double timeBeginSecond = timeBeginDT.ConvertToUnixTimestamp();
                            c.Index = (int)(timeBeginSecond - startSecond);

                            var timeEndDT = DateTime.Parse(c.TimeEnd);
                            double timeEndSecond = timeEndDT.ConvertToUnixTimestamp();
                            c.Len = (int)(timeEndSecond - timeBeginSecond);

                            c.X = (int)(Math.Round(c.Index * CellWidth));
                            c.Width = (int)(Math.Round(c.Len * CellWidth)) + 1;
                            c.End = c.X + c.Width;

                            if (Rows.ContainsKey(rowId))
                            {
                                if (!Rows[rowId].Counters.ContainsKey(c.Id))
                                {
                                    Rows[rowId].Counters.Add(c.Id, c);
                                    CounterCount++;
                                }
                            }
                        }

                    }
                }
            }

            Central.Dbg($"LoadCounter cells:{CounterCount}");
        }

        /// <summary>
        /// Подготовка данных строк
        /// </summary>
        public void PrepareRows()
        {
            Central.Dbg($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] PrepareRows");

            /*
                по умолчанию будут показаны строки, где есть текущие отгрузки
                в режиме фильтра (покзать все) будут показаны все терминалы
             */
            if (Rows.Count > 0)
            {
                int j = 0;
                foreach (KeyValuePair<int, ProductionTaskMonitorGridRow> item in Rows)
                {
                    var row = item.Value;

                    bool show = true;

                    if (show)
                    {
                        row.Index = j;
                        row.Show = true;
                        j++;
                    }

                    if (show)
                    {
                        if (row.Idles.Count > 0 && row.Works.Count > 0)
                        {
                            foreach (KeyValuePair<string, ProductionTaskMonitorGridCell> w in row.Works)
                            {
                                //work-work
                                foreach (KeyValuePair<string, ProductionTaskMonitorGridCell> w2 in row.Works)
                                {
                                    if (w.Value.TimeEnd == w2.Value.TimeBegin)
                                    {
                                        w.Value.Width = w2.Value.X - w.Value.X + 1;
                                        w.Value.End = w.Value.X + w.Value.Width;
                                    }
                                }

                                //work-idle
                                foreach (KeyValuePair<string, ProductionTaskMonitorGridCell> i in row.Idles)
                                {
                                    if (w.Value.TimeEnd == i.Value.TimeBegin)
                                    {
                                        w.Value.Width = i.Value.X - w.Value.X + 1;
                                        w.Value.End = w.Value.X + w.Value.Width;
                                    }
                                }
                            }

                            foreach (KeyValuePair<string, ProductionTaskMonitorGridCell> i in row.Idles)
                            {
                                //idle-idle
                                foreach (KeyValuePair<string, ProductionTaskMonitorGridCell> i2 in row.Idles)
                                {
                                    if (i.Value.TimeEnd == i2.Value.TimeBegin)
                                    {
                                        i.Value.Width = i2.Value.X - i.Value.X + 1;
                                        i.Value.End = i.Value.X + i.Value.Width;
                                    }
                                }

                                //idle-work
                                foreach (KeyValuePair<string, ProductionTaskMonitorGridCell> w in row.Works)
                                {
                                    if (i.Value.TimeEnd == w.Value.TimeBegin)
                                    {
                                        i.Value.Width = w.Value.X - i.Value.X + 1;
                                        i.Value.End = i.Value.X + i.Value.Width;
                                    }
                                }
                            }

                        }
                    }
                }
                RowsCount = j;
            }


            if (Central.DebugMode)
            {
                Central.Dbg($"PrepareRows: rows=[{RowsCount}]");
            }
        }

        /// <summary>
        /// Очистка данных
        /// </summary>
        public void Clear()
        {
            if (HeadersContainer != null)
            {
                HeadersContainer.Children.Clear();
                HeadersContainer.RowDefinitions.Clear();
                HeadersContainer.ColumnDefinitions.Clear();
            }


            if (DataContainer != null)
            {
                DataContainer.Children.Clear();
                DataContainer.RowDefinitions.Clear();
                DataContainer.ColumnDefinitions.Clear();
            }


            if (TimelineContainer != null)
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
            double nowSecond = DateTime.Now.ConvertToUnixTimestamp();

            bool showNowMarker = false;
            if (MonitorRightDT == DateTime.Now)
            {
                showNowMarker = true;
            }

            //cols
            if (true)
            {
                bool resume = true;
                DateTime currentDT = MonitorStartDT;
                double startSecond = MonitorStartDT.ConvertToUnixTimestamp();
                double finishSecond = MonitorFinishDT.ConvertToUnixTimestamp();
                int hourSecondsCounter = 1800;
                int colIndex = 0;

                Central.Dbg($">> => {finishSecond}");

                while (resume)
                {
                    //render
                    double currentSecond = currentDT.ConvertToUnixTimestamp();
                    colIndex = (int)(currentSecond - startSecond);

                    //Central.Dbg($" > {currentDT.ToString("HH:mm:ss")} {colIndex}");

                    //начало часа, временные метки
                    if (hourSecondsCounter == 0)
                    {
                        var cell = new Border();
                        cell.Width = 50;

                        var label = new TextBlock();
                        label.Style = (Style)HeadersContainer.FindResource("TTMonTimelineLabel");
                        label.Text = currentDT.ToString("HH:mm");

                        int x = (int)(colIndex * CellWidth);
                        x = x - 16;
                        int y = 0;
                        label.Margin = new Thickness(x, y, 0, 0);

                        TimelineContainer.Children.Add(label);
                    }

                    //вертикальная сетка, каждые N минут, N=TimeInterval
                    //самую первую линию не рисуем, слипается с контейнером
                    //if(gridCounter ==  0 && colIndex != 0)
                    if (hourSecondsCounter == 0)
                    {
                        var vLine = new Border();
                        vLine.Style = (Style)DataContainer.FindResource("TTMonGridVerticalDevider");
                        vLine.Height = RowHeight * RowsCount;
                        vLine.Width = 1;
                        int x = (int)(colIndex * CellWidth);
                        int y = 0;
                        vLine.Margin = new Thickness(x, y, 0, 0);
                        DataContainer.Children.Add(vLine);
                        ElementsCount++;
                    }

                    currentDT = currentDT.AddSeconds(MonitorStep);

                    //счетчик секунд
                    hourSecondsCounter = hourSecondsCounter + MonitorStep;
                    if (hourSecondsCounter >= 3600)
                    {
                        hourSecondsCounter = 0;
                    }

                    if (currentSecond >= finishSecond)
                    {
                        resume = false;
                    }
                }

                ColsCount = colIndex;
            }

            //rows
            if (true)
            {
                if (Rows.Count > 0)
                {
                    foreach (KeyValuePair<int, ProductionTaskMonitorGridRow> item in Rows)
                    {
                        var row = item.Value;
                        //var rowIndex=item.Key;
                        var rowIndex = row.Index;

                        //отрендерим только те строки, где есть данные
                        if (row.Show)
                        {
                            if (true)
                            {
                                //левая колонка,заголовки строк
                                if (HeadersContainer != null)
                                {
                                    var rd = new RowDefinition();
                                    rd.Style = (Style)HeadersContainer.FindResource("SHMonTermGridHeaderRow");
                                    rd.Height = new GridLength(RowHeight);
                                    HeadersContainer.RowDefinitions.Add(rd);

                                    var label = new TextBlock();
                                    label.Text = row.Title;
                                    label.Style = (Style)HeadersContainer.FindResource("SHMonTermGridHeaderTextLabel");

                                    var cell = new Border();
                                    cell.Style = (Style)HeadersContainer.FindResource("SHMonTermGridHeaderCell");

                                    //у последней строки специальный стиль: бордер внизу
                                    if (rowIndex == (RowsCount - 1))
                                    {
                                        cell.Style = (Style)HeadersContainer.FindResource("SHMonTermGridHeaderCellLast");
                                    }
                                    cell.Child = label;

                                    HeadersContainer.Children.Add(cell);
                                    Grid.SetRow(cell, rowIndex);
                                    Grid.SetColumn(cell, 0);
                                    Grid.SetColumnSpan(cell, 1);
                                }
                            }

                            if (true)
                            {
                                //область данных: горизонтальная сетка
                                var hLine = new Border();
                                hLine.Style = (Style)DataContainer.FindResource("TTMonGridVerticalDevider");
                                hLine.Width = CellWidth * ColsCount;
                                hLine.Height = 1;
                                int x = 0;
                                int y = (rowIndex + 1) * RowHeight;
                                hLine.Margin = new Thickness(x, y, 0, 0);
                                DataContainer.Children.Add(hLine);
                                ElementsCount++;
                            }
                        }
                    }
                }
            }

            Central.Dbg($"    Elements: {ElementsCount} ");
        }

        /// <summary>
        /// Отрисовка данных грида: отсчеты каунтера
        /// </summary>
        public void RenderCounters()
        {
            Central.Dbg($"RenderCounter");

            if (Rows.Count > 0)
            {
                if (DataContainer != null)
                {
                    if (CounterCount > 0)
                    {
                        foreach (KeyValuePair<int, ProductionTaskMonitorGridRow> RowPair in Rows)
                        {
                            var row = RowPair.Value;
                            var rowIndex = row.Index;

                            if (row.Show)
                            {

                                /*
                                    2020-05-29
                                    counter.length - показывает сколько заготовок проехали за минуту, 
                                    а stanok.not_idle_qty указывает пороговое значение, если 
                                    проехали меньше порогового значения - то простой

                                    2020-06-01
                                    stanok.not_idle_qty - это не пороговое значение в минуту, 
                                    а пороговое значение с начала задания, то есть пока 
                                    количество пройденных заготовок с начала задания не превысит 
                                    это значение простой, когда превысило - это значение 
                                    никакой роли не играет

                                */
                                int notIdleQuantity = row.Row.CheckGet("NOT_IDLE_QUANTITY").ToInt();
                                string currentCounter = "";
                                foreach (KeyValuePair<string, ProductionTaskMonitorGridCell> CellPair in row.Counters)
                                {
                                    var cell = CellPair.Value;
                                    var colIndex = cell.Index;

                                    bool elementShow = true;

                                    var bgColor = HColor.Gray;

                                    if (notIdleQuantity != 0)
                                    {
                                        int len = cell.Row.CheckGet("LENGTH").ToInt();
                                        int productionTaskId = cell.Row.CheckGet("PRODUCTION_TASK_ID").ToInt();
                                        if (productionTaskId != 0)
                                        {
                                            //TaskCounter

                                            if (!TaskCounter.ContainsKey(productionTaskId))
                                            {
                                                //если в массиве счетчиков нет  счетчика для этого задания -- добавляем
                                                TaskCounter.Add(productionTaskId, len);
                                            }
                                            else
                                            {
                                                //инкрементируем
                                                TaskCounter[productionTaskId] = TaskCounter[productionTaskId] + len;
                                            }

                                            if (len != 0)
                                            {
                                                if (TaskCounter[productionTaskId] < notIdleQuantity)
                                                {
                                                    elementShow = false;
                                                    bgColor = HColor.PinkOrange;
                                                }
                                            }

                                            currentCounter = $"{TaskCounter[productionTaskId]}";
                                        }
                                    }

                                    if (elementShow)
                                    {
                                        var element = new Border();
                                        element.Style = (Style)DataContainer.FindResource("TTMonGridElement");
                                        element.Height = RowHeight + 1;
                                        int w = (int)(Math.Round(cell.Len * CellWidth)) + 1;
                                        element.Width = cell.Width;
                                        element.VerticalAlignment = VerticalAlignment.Top;
                                        int x = cell.X;
                                        int y = rowIndex * RowHeight;
                                        element.Margin = new Thickness(x, y, 0, 0);

                                        var toolTip = new Border();
                                        {
                                            var g = new StackPanel();
                                            g.Orientation = Orientation.Vertical;

                                            {
                                                var label = new TextBlock();
                                                label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                                label.Text = $"Счетчик ({cell.Id})";
                                                g.Children.Add(label);
                                            }

                                            {
                                                var label = new TextBlock();
                                                label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                                label.Text = $"Время: {cell.Row.CheckGet("TIME")}";
                                                g.Children.Add(label);
                                            }

                                            {
                                                var label = new TextBlock();
                                                label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                                label.Text = $"Количество: {cell.Row.CheckGet("LENGTH")} ({currentCounter})";
                                                g.Children.Add(label);
                                            }

                                            {
                                                var label = new TextBlock();
                                                label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                                label.Text = $"ID ПЗ: {cell.Row.CheckGet("PRODUCTION_TASK_ID")}";
                                                g.Children.Add(label);
                                            }

                                            if (Central.DebugMode)
                                            {
                                                {
                                                    var label = new TextBlock();
                                                    label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                                    label.Text = $"* {cell.TimeBegin} -- {cell.TimeEnd}";
                                                    g.Children.Add(label);
                                                }

                                                {
                                                    var label = new TextBlock();
                                                    label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                                    label.Text = $"* {cell.Row.CheckGet("TIME")}";
                                                    g.Children.Add(label);
                                                }
                                            }

                                            if (Central.DebugMode)
                                            {
                                                var label = new TextBlock();
                                                label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                                label.Text = $"DBG: x=[{cell.X}] w=[{cell.Width}] e=[{cell.End}]";
                                                g.Children.Add(label);
                                            }

                                            toolTip.Child = g;
                                        }
                                        element.ToolTip = toolTip;

                                        var bc = new BrushConverter();
                                        var brush = (Brush)bc.ConvertFrom(bgColor);
                                        element.Background = brush;

                                        DataContainer.Children.Add(element);
                                        ElementsCount++;
                                    }
                                }
                            }
                        }

                    }
                }
            }

            Central.Dbg($"    Elements: {ElementsCount} ");
        }

        /// <summary>
        /// Отрисовка данных грида: производственные задания
        /// </summary>
        public void RenderWorks()
        {
            Central.Dbg($"RenderWorks");

            double barHeight = 0.66;

            if (Rows.Count > 0)
            {
                if (DataContainer != null)
                {
                    if (WorksCount > 0)
                    {
                        foreach (KeyValuePair<int, ProductionTaskMonitorGridRow> RowPair in Rows)
                        {
                            var row = RowPair.Value;
                            var rowIndex = row.Index;

                            if (row.Show)
                            {
                                foreach (KeyValuePair<string, ProductionTaskMonitorGridCell> CellPair in row.Works)
                                {
                                    var cell = CellPair.Value;
                                    var colIndex = cell.Index;

                                    var element = new Border();
                                    element.Style = (Style)DataContainer.FindResource("TTMonGridElement");
                                    element.Height = (RowHeight * barHeight) + 1;
                                    int w = (int)(Math.Round(cell.Len * CellWidth));
                                    element.Width = cell.Width;
                                    element.VerticalAlignment = VerticalAlignment.Top;
                                    int x = cell.X;
                                    int y = rowIndex * RowHeight;
                                    element.Margin = new Thickness(x, y + ((RowHeight * (1 - barHeight))), 0, 0);

                                    //название задания покажем на баре
                                    string taskNumber = "";
                                    if (cell.Row.ContainsKey("PRODUCTION_TASK_NUMBER"))
                                    {
                                        taskNumber = cell.Row["PRODUCTION_TASK_NUMBER"];

                                        var label = new TextBlock();
                                        label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                        label.Text = $"{taskNumber}";
                                        label.Margin = new Thickness(2, 0, 0, 0);
                                        label.FontSize = 11;
                                        element.Child = label;
                                    }


                                    var toolTip = new Border();
                                    {
                                        var g = new StackPanel();
                                        g.Orientation = Orientation.Vertical;

                                        {
                                            var label = new TextBlock();
                                            label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                            label.Text = $"Задание: {taskNumber}";
                                            g.Children.Add(label);
                                        }

                                        {
                                            var label = new TextBlock();
                                            label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                            label.Text = $"Время начала:       {cell.TimeBegin}";
                                            g.Children.Add(label);
                                        }

                                        {
                                            var label = new TextBlock();
                                            label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                            label.Text = $"Время окончания: {cell.TimeEnd}";
                                            g.Children.Add(label);
                                        }

                                        {
                                            var label = new TextBlock();
                                            label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                            label.Text = $"ID ПЗ: {cell.Row.CheckGet("PRODUCTION_TASK_ID")}";
                                            g.Children.Add(label);
                                        }


                                        {
                                            var label = new TextBlock();
                                            label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                            label.Text = $"Артикул: {cell.Row.CheckGet("SKU")}";
                                            g.Children.Add(label);
                                        }

                                        {
                                            var label = new TextBlock();
                                            label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                            label.Text = $"Изделие: {cell.Row.CheckGet("PRODUCT_NAME")}";
                                            g.Children.Add(label);
                                        }

                                        {
                                            var v = "нет";
                                            if (cell.Row.CheckGet("PRODUCT_PRINTING").ToInt() == 1)
                                            {
                                                v = "есть";
                                            }

                                            var label = new TextBlock();
                                            label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                            label.Text = $"Печать: {v}";
                                            g.Children.Add(label);
                                        }

                                        {
                                            var v = "нет";
                                            if (cell.Row.CheckGet("PRODUCT_SHTANZ").ToInt() == 1)
                                            {
                                                v = "есть";
                                            }

                                            var label = new TextBlock();
                                            label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                            label.Text = $"Штанцформа: {v}";
                                            g.Children.Add(label);
                                        }

                                        if (Central.DebugMode)
                                        {
                                            var label = new TextBlock();
                                            label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                            label.Text = $"DBG: x=[{cell.X}] w=[{cell.Width}] e=[{cell.End}]";
                                            g.Children.Add(label);
                                        }

                                        toolTip.Child = g;
                                    }
                                    element.ToolTip = toolTip;

                                    var bgColor = HColor.Green;
                                    var bc = new BrushConverter();
                                    var brush = (Brush)bc.ConvertFrom(bgColor);
                                    element.Background = brush;

                                    DataContainer.Children.Add(element);
                                    ElementsCount++;
                                }
                            }
                        }

                    }
                }
            }

            Central.Dbg($"    Elements: {ElementsCount} ");
        }

        /// <summary>
        /// Отрисовка данных грида: простои
        /// </summary>
        public void RenderIdles()
        {
            Central.Dbg($"RenderIdles");

            double barHeight = 0.33;

            if (Rows.Count > 0)
            {
                if (DataContainer != null)
                {
                    if (IdlesCount > 0)
                    {
                        foreach (KeyValuePair<int, ProductionTaskMonitorGridRow> RowPair in Rows)
                        {
                            var row = RowPair.Value;
                            var rowIndex = row.Index;

                            if (row.Show)
                            {
                                foreach (KeyValuePair<string, ProductionTaskMonitorGridCell> CellPair in row.Idles)
                                {
                                    var cell = CellPair.Value;
                                    var colIndex = cell.Index;

                                    var element = new Border();
                                    element.Style = (Style)DataContainer.FindResource("TTMonGridElement");
                                    element.Height = (RowHeight * barHeight) + 1;
                                    int w = (int)(Math.Round(cell.Len * CellWidth));
                                    element.Width = cell.Width;
                                    element.VerticalAlignment = VerticalAlignment.Top;
                                    int x = cell.X;
                                    int y = rowIndex * RowHeight;
                                    element.Margin = new Thickness(x, y + ((RowHeight * (1 - barHeight))), 0, 0);

                                    var toolTip = new Border();
                                    {
                                        var g = new StackPanel();
                                        g.Orientation = Orientation.Vertical;

                                        {
                                            var label = new TextBlock();
                                            label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                            label.Text = $"Простой ({cell.Id})";
                                            g.Children.Add(label);
                                        }

                                        {
                                            var label = new TextBlock();
                                            label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                            label.Text = $"Время начала:       {cell.TimeBegin}";
                                            g.Children.Add(label);
                                        }

                                        {
                                            var label = new TextBlock();
                                            label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                            label.Text = $"Время окончания: {cell.TimeEnd}";
                                            g.Children.Add(label);
                                        }

                                        {
                                            var label = new TextBlock();
                                            label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                            label.Text = $"Длительность: {cell.Row.CheckGet("IDLE_LENGTH")}";
                                            g.Children.Add(label);
                                        }

                                        {
                                            var label = new TextBlock();
                                            label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                            label.Text = $"{cell.Row.CheckGet("REASON_NAME")} {cell.Row.CheckGet("REASON")}";
                                            g.Children.Add(label);
                                        }

                                        if (Central.DebugMode)
                                        {
                                            var label = new TextBlock();
                                            label.Style = (Style)DataContainer.FindResource("TTMonTooltipLabel");
                                            label.Text = $"DBG: x=[{cell.X}] w=[{cell.Width}] e=[{cell.End}]";
                                            g.Children.Add(label);
                                        }

                                        toolTip.Child = g;
                                    }
                                    element.ToolTip = toolTip;

                                    /*

                                        idle_reason
                                        ----------------------------------
                                        27	КЗ
                                        28	ППР                 оливковый
                                        22	Сырье               желто-оранжевый
                                        23	Организационный     фиолетовый
                                        24	Технические         красный
                                        25	Нет заданий         голубой
                                        18	Технологический     желтый
                                        26	Перестройка         оранжевый
                                        29	Комплектация
                                        44	Переполнен буфер
                                     */

                                    int reasonId = cell.Row.CheckGet("REASON_ID").ToInt();
                                    var bgColor = HColor.Gray;
                                    switch (reasonId)
                                    {
                                        case 28:
                                            bgColor = HColor.Olive;
                                            break;

                                        case 23:
                                            bgColor = HColor.Violet;
                                            break;

                                        case 22:
                                            bgColor = HColor.YellowOrange;
                                            break;

                                        case 24:
                                            bgColor = HColor.Red;
                                            break;

                                        case 25:
                                            bgColor = HColor.Blue;
                                            break;

                                        case 18:
                                            bgColor = HColor.Yellow;
                                            break;

                                        case 26:
                                            bgColor = HColor.Orange;
                                            break;

                                        default:
                                            bgColor = HColor.Gray;
                                            break;
                                    }

                                    var bc = new BrushConverter();
                                    var brush = (Brush)bc.ConvertFrom(bgColor);
                                    element.Background = brush;
                                    element.Background = brush;

                                    DataContainer.Children.Add(element);
                                    ElementsCount++;
                                }
                            }
                        }
                    }
                }
            }

            Central.Dbg($"    Elements: {ElementsCount} ");
        }
    }
}
