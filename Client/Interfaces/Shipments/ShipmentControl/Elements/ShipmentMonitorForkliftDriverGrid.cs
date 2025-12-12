using Client.Assets.HighLighters;
using Client.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static DevExpress.Data.Filtering.Helpers.SubExprHelper.ThreadHoppingFiltering;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Управление отгрузками, вкладка "Монитор водителей погрузчика"
    /// Вспомогательный класс: Структура данных и рендер грида
    /// </summary>
    /// <author>sviridov_ae</author>       
    public class ShipmentMonitorForkliftDriverGrid
    {
        /*
            Структура данных со своими внутренними вспомогательными структурами
            и функции рендера данных.
            Входящие данные загружаются во внутренние регистры, дальнейшая работа
            с данними идет с их внутренним представлением.
         */
        public Dictionary<int, ShipmentMonitorForkliftDriverGridRow> Rows { get; set; }

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
        /// длина штриха координатной сетки (минут) (30)
        /// </summary>
        public int TimeStep { get; set; }

        /// <summary>
        /// количество отсчетов (25)
        /// </summary>
        public int TimeLabels { get; set; }

        /// <summary>
        /// масштаб X (пикселей в минуте) (1.6)   
        /// </summary>
        public double XScale { get; set; }

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
        /// Дата, от которой рассматриваем отгрузки
        /// Опорное время, сегодня или то, что указано в фильтре
        /// </summary>
        public DateTime ShipmentDate { get; set; }

        /// <summary>
        /// Дата, до которой рассматриваем отгрузки
        /// </summary>
        public DateTime ShipmentDateEnd { get; set; }

        /// <summary>
        /// Текущее время "11:32"
        /// </summary>
        public string TimeHoursNow { get; set; }

        public string DateNow { get; set; }

        /// <summary>
        /// Контейнер "имена погрузчиков"
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
        /// Список погрузчиков
        /// </summary>
        private List<Dictionary<string, string>> ForkliftDriverList { get; set; }

        /// <summary>
        /// Идентификатор площадки
        /// </summary>
        private int FactoryId { get; set; }

        public Dictionary<int, string> Times { get; set; }

        public int MinuteFirst { get; set; }

        /// <summary>
        /// фильтр поргузчиков. 0=без фильтра, 1=все, 2=активные, 3=с отгрузками
        /// </summary>
        public int ForkliftDriverStatus { get; set; }

        /// <summary>
        /// фильтр погрузчиков по типу продукции. 0 -- без фильтра, 1 -- готовая продукция, 2 -- рулоны.
        /// </summary>
        public int ForkliftDriverProductType { get; set; }

        /// <summary>
        /// Кнструктор
        /// </summary>
        /// <param name="shipmentDate"></param>
        public ShipmentMonitorForkliftDriverGrid(DateTime shipmentDate, DateTime shipmentDateEnd, int factoryId = 1)
        {
            //начало отсчета, строка
            TimeStart = shipmentDate.ToString("HH:00");
            TimeStartShipment = shipmentDate.ToString("HH:00");

            ShipmentDate = shipmentDate;
            ShipmentDateEnd = shipmentDateEnd;
            ForkliftDriverStatus = 0;
            ForkliftDriverProductType = 0;
            FactoryId = factoryId;

            /*
                текущее время мы фиксируем, чтобы все внутрение механизмы опирались
                на одно значение
            */
            TimeHoursNow = DateTime.Now.ToString("HH:mm");
            DateNow = DateTime.Now.ToString("yyyy-MM-dd");

            var shipmentPeriodMinutes = Math.Ceiling((ShipmentDateEnd - ShipmentDate).TotalMinutes).ToInt();
            //кличество точек, одна точка-1 мин.
            TimeTicks = shipmentPeriodMinutes + 30;

            TimeStep = 60;
            TimeLabels = shipmentPeriodMinutes / 60 + 1;
            XScale =1.6;

            CellsCount = 0;
            RowsCount = 0;

            FirstTime = "";
            LastTime = "";
            CenterTime = "";

            CenterCol = 0;

            Rows = new Dictionary<int, ShipmentMonitorForkliftDriverGridRow>();
            Cols = new Dictionary<string, int>();

            Central.Dbg($"    TimeHoursNow:[{TimeHoursNow}] DateNow:[{DateNow}] ");

            //подготовка структуры данных
            {
                ForkliftDriverList = GetForkliftDriverList();

                //создание структур и заголовков терминалов
                int rowIndex = 0;
                foreach (var forkliftDriver in ForkliftDriverList)
                {
                    var row = new ShipmentMonitorForkliftDriverGridRow
                    {
                        ForkliftDriverId = forkliftDriver.CheckGet("ID").ToInt(),
                        ForkliftDriverName = $"{forkliftDriver.CheckGet("NAME")}"
                    };

                    if (forkliftDriver.CheckGet("STOCK_ROLL").ToInt() > 0
                        && forkliftDriver.CheckGet("STOCK_PRODUCT").ToInt() > 0)
                    {
                        row.ForkliftDriverType = 3;
                    }
                    else
                    {
                        if (forkliftDriver.CheckGet("STOCK_ROLL").ToInt() > 0)
                        {
                            row.ForkliftDriverType = 2;
                        }
                        else if (forkliftDriver.CheckGet("STOCK_PRODUCT").ToInt() > 0)
                        {
                            row.ForkliftDriverType = 1;
                        }
                    }

                    Rows.Add(rowIndex, row);
                    rowIndex++;
                }
            }

            /*
                просчет координат границ монитора (абсцисса)
                нам нужно знать FirstTime и LastTime до LoadItems
             */
            if (Rows.Count > 0)
            {
                foreach (var item in Rows)
                {
                    var rowIndex = item.Key;

                    if (TimeTicks > 0)
                    {
                        //генератор временных меток
                        var tm = new ShipmentTimelineTime { Hours = 8, Minutes = 0 };

                        var t = TimeStart.Split(':');
                        if (!string.IsNullOrEmpty(t[0]))
                        {
                            tm.Hours = int.Parse(t[0]);
                        }
                        if (!string.IsNullOrEmpty(t[1]))
                        {
                            tm.Minutes = int.Parse(t[1]);
                        }

                        //текущая итерация (id колонки таблицы)
                        int colIndex = 0;
                        //аварийный предохранитель
                        int colMax = 10000;
                        //флажок продолжения итераций
                        bool resume = true;

                        while (resume)
                        {
                            //"HH:MM"
                            string timeHours = tm.OutHoursMinutes();

                            // "YYYY-MM-DD_" + "HH:MM" => YYYY-MM-DD_HH:MM
                            string k="";
                            k = $"{ShipmentDate.AddDays(tm.Days).ToString("yyyy-MM-dd")}_{timeHours}";

                            //на первом проходе 
                            if (rowIndex == 0)
                            {
                                //если первая метка не задана, зададим
                                if (string.IsNullOrEmpty(FirstTime))
                                {
                                    FirstTime = k;
                                }

                                if (timeHours == TimeHoursNow)
                                {
                                    //время маркера
                                    if (string.IsNullOrEmpty(CenterTime))
                                    {
                                        CenterTime = k;
                                    }

                                    //координата маркера
                                    if (CenterCol == 0)
                                    {
                                        CenterCol = colIndex * 3;
                                    }
                                }

                                //забъем хэш данными: время-координата
                                if (!Cols.ContainsKey(k))
                                {
                                    Cols.Add(k, colIndex);
                                }
                            }

                            LastTime = k;

                            tm.IncMinutes(1);

                            colIndex++;
                            if (colIndex > TimeTicks || colIndex > colMax)
                            {
                                resume = false;
                            }

                        }
                    }
                }

                Central.Dbg($"    First:[{FirstTime}] Center:[{CenterTime}] Last:[{LastTime}] CenterCol:[{CenterCol}]");
            }
        }

        public List<Dictionary<string, string>> GetForkliftDriverList()
        {
            List<Dictionary<string, string>> dictionary = new List<Dictionary<string, string>>();

            var p = new Dictionary<string, string>();
            p.Add("FACTORY_ID", $"{FactoryId}");
            p.Add("SHOW_ALL", "1");
            p.Add("SORT_BY_NAME", "1");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "ForkliftDriver");
            q.Request.SetParam("Action", "List");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        dictionary = ds.Items;
                    }
                }
            }
            
            return dictionary;
        }

        /// <summary>
        /// Поиск ID строки по Ид погрузчика. Если не нашёл погрузчика, вернёт -1
        /// </summary>
        /// <param name="forkliftDriverId"></param>
        /// <returns></returns>
        public int FindRowIndexByForkliftDriverId(int forkliftDriverId = 0)
        {
            int rowIndex = -1;
            if (Rows.Count > 0)
            {
                foreach (var rowPair in Rows)
                {
                    var row = rowPair.Value;
                    if (row.ForkliftDriverId == forkliftDriverId)
                    {
                        rowIndex = rowPair.Key;
                    }
                }
            }

            return rowIndex;
        }

        public int GetMinute(DateTime mark)
        {
            int result=0;

            string timeStart=TimeStart;
            var start = ($"{ShipmentDate.ToString("yyyy-MM-dd")} {timeStart}").ToDateTime("yyyy-MM-dd HH:mm");

            var a=mark.ToString("yyyy-MM-dd").ToDateTime("yyyy-MM-dd");
            var b=start.ToString("yyyy-MM-dd").ToDateTime("yyyy-MM-dd");
            var dd = (int)(a - b).TotalDays;
            int minute = (1440*dd) + (mark.Hour * 60) + mark.Minute;           
            if(MinuteFirst==0)
            {
                MinuteFirst=minute;
            }            
            result=minute-MinuteFirst;

            return result;
        }

        public void GenerateTimes()
        {
            if(Times==null)
            {
                //minute=>label  1233=>01.01 09:30
                Times=new Dictionary<int,string>();
            }

            if(Times.Count==0)
            {
                MinuteFirst=0;

                string timeStart=TimeStart;    
                int timeStep=TimeStep;
                var start = ($"{ShipmentDate.ToString("yyyy-MM-dd")} {timeStart}").ToDateTime("yyyy-MM-dd HH:mm");

                var times = from offset in Enumerable.Range(0,TimeLabels) select TimeSpan.FromMinutes(timeStep * offset);

                {
                    int j=0;
                    MinuteFirst=0;
                    foreach(var time in times)
                    {
                        j++;
                        var label = start + time;
                        var minute=GetMinute(label);                       
                        var text = label.ToString("dd.MM HH:mm");
                        if(!Times.ContainsKey(minute))
                        {
                            Times.Add(minute,text);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Отрисовка грида (сетка, маркеры, заголовки, таймлайн)
        /// </summary>
        public void RenderGrid()
        {
            Central.Dbg($"RenderGrid");
            
            //маркер текущего времени
            var showNowMarker = false;
            if (ShipmentDate.ToString("yyyy-MM-dd") == DateNow)
            {
                showNowMarker=true;
            }
            showNowMarker=true;

            //начало шкалы
            string timeStart=TimeStart;
            //интервал сетки
            int timeStep=TimeStep;
            //масштаб X (пикселей в минуте)            
            //double xScale=1.6;
            double xScale=XScale;
            //ширина блока 50 пикс
            int blockWidth=(int)(timeStep*xScale);

            //временная шкала
            if(TimelineContainer != null)
            {                       
                var cd = new ColumnDefinition
                {
                    Style = (Style)TimelineContainer.FindResource("SHMonTermGridTLCol"),
                    Width=GridLength.Auto,
                };
                TimelineContainer.ColumnDefinitions.Add(cd);

                var rd = new RowDefinition
                {
                    Style = (Style)TimelineContainer.FindResource("SHMonTermGridTLRow"),
                };
                TimelineContainer.RowDefinitions.Add(rd);

                GenerateTimes();               

                { 
                    foreach(KeyValuePair<int,string> time in Times)
                    {
                        string timeLabelText=time.Value;
                        int minute=time.Key;
                        int xOffset = (int)(minute*xScale);
                    
                        var cell = new Border
                        {
                            Style = (Style)HeadersContainer.FindResource("SHMonTermGridTLBorder"),
                            Width = blockWidth,
                            Margin = new Thickness(xOffset,0,0,0),
                        };
                        var label = new TextBlock
                        {
                            Text = timeLabelText,
                            Style = (Style)HeadersContainer.FindResource("SHMonTermGridTLLabel"),
                        };
                        cell.Child = label;

                        TimelineContainer.Children.Add(cell);
                        Grid.SetRow(cell,0);
                        Grid.SetColumn(cell,0);
                    }
                }

                if(showNowMarker)
                {
                    var minute=GetMinute(DateTime.Now);
                    var xOffset = (int)(minute*xScale);

                    var cell = new Border
                    {
                        Style = (Style)TimelineContainer.FindResource("SHMonTermGridMKBorder"),
                        Width = blockWidth,
                        Margin = new Thickness(xOffset+1,0,0,0),
                    };                   

                    TimelineContainer.Children.Add(cell);
                    Grid.SetRow(cell,0);
                    Grid.SetColumn(cell,0);
                }
            }

            //координатная сетка            
            {
                if (Rows.Count > 0)
                {
                    foreach (var item in Rows)
                    {
                        var row = item.Value;
                        var rowIndex = row.Index;

                        //отрендерим только те строки, где есть данные
                        if (row.Show)
                        {
                            //область заголовков
                            {
                                if(HeadersContainer != null)
                                {
                                    { 
                                        var cd = new ColumnDefinition
                                        {
                                            Style = (Style)HeadersContainer.FindResource("SHMonTermGridHCCol"),
                                            Width=GridLength.Auto,
                                        };
                                        HeadersContainer.ColumnDefinitions.Add(cd);

                                        var rd = new RowDefinition
                                        {
                                            Style = (Style)HeadersContainer.FindResource("SHMonTermGridHCRow"),
                                        };
                                        HeadersContainer.RowDefinitions.Add(rd);


                                        var cell = new Border
                                        {
                                            Style = (Style)HeadersContainer.FindResource("SHMonTermGridTLBorder"),
                                            Width = blockWidth * 2,
                                            Margin = new Thickness(0,0,0,0),
                                        };
                                        var label = new TextBlock
                                        {
                                            Text = row.ForkliftDriverName,
                                            Style = (Style)HeadersContainer.FindResource("SHMonTermGridTLLabel"),
                                        };
                                        cell.Child = label;

                                        HeadersContainer.Children.Add(cell);
                                        Grid.SetRow(cell, rowIndex);
                                        Grid.SetColumn(cell,0);
                                    }
                                }
                            }

                            //область данных
                            {
                                if(DataContainer != null)
                                {
                                    var cd = new ColumnDefinition
                                    {
                                        Style = (Style)DataContainer.FindResource("SHMonTermGridDCCol"),
                                        Width=GridLength.Auto,
                                    };
                                    DataContainer.ColumnDefinitions.Add(cd);

                                    var rd = new RowDefinition
                                    {
                                        Style = (Style)DataContainer.FindResource("SHMonTermGridDCRow"),
                                    };
                                    DataContainer.RowDefinitions.Add(rd);

                                    foreach(KeyValuePair<int,string> time in Times)
                                    {
                                        //string timeLabelText=time.Value;
                                        int minute=time.Key;
                                        int xOffset = (int)(minute*xScale);

                                        var cell = new Border
                                        {
                                            Style = (Style)HeadersContainer.FindResource("SHMonTermGridTLBorder"),
                                            Width = blockWidth,
                                            Margin = new Thickness(xOffset,0,0,0),
                                        };

                                        DataContainer.Children.Add(cell);
                                        Grid.SetRow(cell,rowIndex);
                                        Grid.SetColumn(cell,0);
                                    }

                                    if(showNowMarker)
                                    {
                                        var minute=GetMinute(DateTime.Now);
                                        var xOffset = (int)(minute*xScale);

                                        var cell = new Border
                                        {
                                            Style = (Style)DataContainer.FindResource("SHMonTermGridMKBorder"),
                                            Width = blockWidth,
                                            Margin = new Thickness(xOffset+1,0,0,0),
                                        };                   

                                        DataContainer.Children.Add(cell);
                                        Grid.SetRow(cell, rowIndex);
                                        Grid.SetColumn(cell,0);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Очистка графических данных
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
        }

        /// <summary>
        /// Отрисовка данных грида
        /// </summary>
        public void RenderData()
        {
            //начало шкалы
            string timeStart = TimeStart;
            double xScale = XScale;

            if (Rows.Count > 0)
            {
                if (DataContainer != null)
                {
                    if (CellsCount > 0)
                    {
                        foreach (var rowPair in Rows)
                        {
                            var row = rowPair.Value;
                            var rowIndex = row.Index;

                            if (row.Show)
                            {
                                foreach (var cellPair in row.Cells)
                                {
                                    var cell = cellPair.Value;
                                    var time = cellPair.Key;

                                    var c = cell;

                                    var blockStart = ($"{c.StartTime}").ToDateTime("dd.MM.yyyy HH:mm");

                                    var minute = GetMinute(blockStart);
                                    var xOffset = (int)(minute * xScale);

                                    var lenX = (int)(c.Len * xScale);

                                    {
                                        var element = new ShipmentsMonitorItem
                                        {
                                            Id = { Text = cell.Id },
                                            BayerName = { Text = cell.BayerName },
                                            DriverName = { Text = cell.DriverName.SurnameInitials() },
                                            ForkliftDriverName = { Text = cell.ForkliftDriverName.SurnameInitials() },
                                            Progress = { Text = $"{cell.Loaded}/{cell.ForLoading}" },
                                            Margin = new Thickness(xOffset, 0, 0, 0),
                                        };

                                        //сегмент
                                        if (cell.Len < 30)
                                        {
                                            element.BayerName.Visibility = Visibility.Collapsed;
                                            element.DriverName.Visibility = Visibility.Collapsed;
                                            element.ForkliftDriverName.Visibility = Visibility.Collapsed;
                                            element.LDriverName.Visibility = Visibility.Collapsed;
                                            element.LForkliftDriverName.Visibility = Visibility.Collapsed;
                                            element.Progress.Visibility = Visibility.Collapsed;
                                        }

                                        //тултип
                                        element.TId.Text = cell.Id;
                                        element.TShipmentStart.Text = cell.StartTime;
                                        element.TShipmentFinish.Text = cell.FinishTime;
                                        element.TBayerName.Text = cell.BayerName;

                                        element.TProductionType.Text = "";
                                        switch (cell.ProductionType.ToInt())
                                        {
                                            default:
                                                element.TProductionType.Text = "гофра";
                                                break;

                                            case 2:
                                                element.TProductionType.Text = "бумага";
                                                break;

                                        }

                                        element.TShipmentType.Text = "";
                                        switch (cell.SelfShipment.ToInt())
                                        {
                                            default:
                                                element.TShipmentType.Text = "доставка";
                                                break;

                                            case 1:
                                                element.TShipmentType.Text = "самовывоз";
                                                break;

                                        }

                                        element.TPackaging.Text = "";
                                        switch (cell.PackagingType.ToInt())
                                        {
                                            case 1:
                                                element.TPackaging.Text = "Паллеты";
                                                element.Packaging.Text = "ПАЛ";
                                                break;

                                            case 2:
                                                element.TPackaging.Text = "Россыпью";
                                                element.Packaging.Text = "РОС";
                                                break;

                                            case 3:
                                                element.TPackaging.Text = "Рулоны";
                                                element.Packaging.Text = "РУЛ";
                                                break;

                                        }

                                        element.TDriver.Text = $"{cell.DriverName}";
                                        element.TTerminal.Text = $"{cell.TerminalTitle}";
                                        element.TForkliftDriver.Text = cell.ForkliftDriverName.SurnameInitials();
                                        element.TProgress.Text = $"{cell.Loaded}/{cell.ForLoading}";

                                        //отладочная информация
                                        if (Central.DebugMode)
                                        {
                                            element.TDebug.Text = "";
                                            element.TDebug.Visibility = Visibility.Visible;
                                        }
                                        else
                                        {
                                            element.TDebug.Visibility = Visibility.Collapsed;
                                        }

                                        element.TShipmentFinish.Visibility = Visibility.Collapsed;
                                        element.LShipmentFinish.Visibility = Visibility.Collapsed;

                                        string bgColor;
                                        switch (cell.Status)
                                        {
                                            default:
                                                bgColor = HColor.White;
                                                element.TStatus.Text = "внешние операции";
                                                break;

                                            case 2:
                                                bgColor = HColor.Blue;
                                                element.TStatus.Text = "отгрузка";
                                                break;

                                            case 3:
                                                bgColor = HColor.Green;
                                                element.TStatus.Text = "отгружено";
                                                element.TShipmentFinish.Visibility = Visibility.Visible;
                                                element.LShipmentFinish.Visibility = Visibility.Visible;
                                                break;
                                        }
                                        var bc = new BrushConverter();
                                        var brush = (Brush)bc.ConvertFrom(bgColor);
                                        element.MonBlock.Background = brush;
                                        element.MonBlock.Width = lenX;

                                        DataContainer.Children.Add(element);
                                        Grid.SetRow(element, rowIndex);
                                        Grid.SetColumn(element, 0);

                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Загрузка строк во внутренние структуры.
        /// Отгрузки будут сразу сгруппированы по погрузчику.
        /// </summary>
        /// <param name="items"></param>
        public void LoadItems(List<Dictionary<string, string>> items)
        {
            /*
                минимальная длина сегмента, мин
                если сегмент короче, будет растянут до этого значения
             */
            int minElementLen = 0;

            {
                foreach (Dictionary<string, string> r in items)
                {
                    int forkliftDriverId = r.CheckGet("FORKLIFT_DRIVER_ID").ToInt();
                    if (forkliftDriverId != 0)
                    {
                        //флаг отображения сегмента 
                        //(рендер не будет разбираться с данными, а отрисует все, что есть в стеке)
                        bool show = true;
                        //флаг продолжения 
                        bool resume = true;

                        var c = new ShipmentMonitorGridCell
                        {
                            Id = r.CheckGet("ID"),
                            StartTime = r.CheckGet("SHIPMENTBEGIN"),
                            FinishTime = r.CheckGet("SHIPMENTEND"),
                            BayerName = r.CheckGet("BAYERNAME"),
                            TerminalId = r.CheckGet("TERMINALID").ToInt(),
                            Status = r.CheckGet("STATUS").ToInt(),
                            DriverName = r.CheckGet("DRIVERNAME"),
                            TerminalTitle = r.CheckGet("TERMINALNUMBER"),
                            SelfShipment = r.CheckGet("SELFSHIPMENT"),
                            PackagingType = r.CheckGet("PACKAGINGTYPE"),
                            ProductionType = r.CheckGet("PRODUCTIONTYPE"),
                            Loaded = r.CheckGet("LOADED"),
                            ForLoading = r.CheckGet("FORLOADING"),
                            ForkliftDriverId = forkliftDriverId,
                            ForkliftDriverName = r.CheckGet("FORKLIFTDRIVERNAME"),
                        };

                        //координаты начала и конца не должны быть пустыми
                        if (string.IsNullOrEmpty(c.StartTime))
                        {
                            resume = false;
                            show = false;
                        }

                        if (string.IsNullOrEmpty(c.FinishTime))
                        {
                            resume = false;
                            show = false;
                        }

                        /*
                            k -- абсцисса положения сегмента в строке

                            c.StartTime     dd.mm.yyyy HH:mm    (из базы данных)
                            c.FinishTime
                            
                            startTimeDT     DateTime
                            finishTimeDT
                            firstTimeDT 

                            FirstTime       yyyy-MM-dd_HH:mm    (в координатах грида)
                            k
                            
                         */

                        /*
                            если координата курсора установлена, и если отгузка
                            имеет статус "отгружается", "притянем" ее к курсору:
                            вместо ее времени окончания, возьмем время курсора
                            (это избавит нас от спонтанных зазоров между концом сегмента и курсором)
                         */

                        var startTimeDT = DateTime.Parse(c.StartTime);
                        var finishTimeDT = DateTime.Parse(c.FinishTime);
                        var firstTimeDT = DateTime.ParseExact(FirstTime, "yyyy-MM-dd_HH:mm", CultureInfo.InvariantCulture);
                        var timeStartShipmentDT = DateTime.Parse($"{ShipmentDate:dd.MM.yyyy} {TimeStartShipment}");

                        if (!string.IsNullOrEmpty(CenterTime))
                        {
                            if (c.Status == 2)
                            {
                                finishTimeDT = DateTime.ParseExact(CenterTime, "yyyy-MM-dd_HH:mm", CultureInfo.InvariantCulture);
                            }
                        }

                        var k = startTimeDT.ToString("yyyy-MM-dd_HH:mm");

                        //если событие началось до левой границы, покажем от левого края монитора                        
                        if (resume)
                        {
                            int result = DateTime.Compare(startTimeDT, firstTimeDT);
                            if (result < 0)
                            {
                                k = FirstTime;
                            }
                        }

                        //если событие закончилось до TimeStartShipment, не показываем его
                        if (resume)
                        {
                            int result = DateTime.Compare(finishTimeDT, timeStartShipmentDT);

                            if (result < 0)
                            {
                                show = false;
                                resume = false;
                            }
                        }

                        /*
                            длина сегмента в минутах
                            если сегмент короче минимально допустимого, удлиним его
                        */
                        if (resume)
                        {
                            var kDT = DateTime.ParseExact(k, "yyyy-MM-dd_HH:mm", CultureInfo.InvariantCulture);
                            var ts = finishTimeDT - kDT;

                            c.Len = (int)ts.TotalMinutes;
                            if (minElementLen != 0)
                            {
                                if (ts.TotalMinutes < minElementLen)
                                {
                                    c.Len = minElementLen;
                                }
                            }

                            if (c.Len < 0)
                            {
                                c.Len = 0;
                            }

                        }

                        if (resume)
                        {
                            if (string.IsNullOrEmpty(k))
                            {
                                show = false;
                            }
                        }

                        //добавляем в стек
                        if (show)
                        {
                            var rowIndex = FindRowIndexByForkliftDriverId(forkliftDriverId);
                            if (rowIndex >= 0)
                            {
                                if (!Rows[rowIndex].Cells.ContainsKey(k))
                                {
                                    Rows[rowIndex].Cells.Add(k, c);
                                    CellsCount++;
                                }
                            }
                        }
                    }
                }
            }

            // фильтр терминалов.
            {
                if (Rows.Count > 0)
                {
                    int j = 0;
                    foreach (var item in Rows)
                    {
                        var row = item.Value;

                        bool show = true;

                        // фильтр погрузчиков по статусу. 0=без фильтра, 1=все, 2=активные, 3=с отгрузками
                        switch (ForkliftDriverStatus)
                        {
                            //1=все
                            case 0:
                            case 1:
                                break;

                            //2=активные
                            case 2:
                                // Если нет ни одной отгрузки у этого водителя
                                if (row.Cells.Count == 0)
                                {
                                    show = false;
                                }
                                else
                                {
                                    // Если нет ни одной отгрузки у этого водителя со статусом -- отгружается
                                    if (row.Cells.Count(x => x.Value.Status == 2) == 0)
                                    {
                                        show = false;
                                    }
                                }
                                break;

                            //3=с отгрузками
                            case 3:
                                // Если нет ни одной отгрузки у этого водителя
                                if (row.Cells.Count == 0)
                                {
                                    show = false;
                                }
                                break;
                        }

                        // фильтр погрузчиков по продукции. 0=без фильтра, 1=ГП, 2=Рулоны
                        switch (ForkliftDriverProductType)
                        {
                            case 1:
                                if (row.ForkliftDriverType == 2)
                                {
                                    show = false;
                                }
                                break;

                            case 2:
                                if (row.ForkliftDriverType == 1)
                                {
                                    show = false;
                                }
                                break;

                            case 0:
                            default:
                                break;
                        }

                        if (show)
                        {
                            row.Index = j;
                            row.Show = true;
                            j++;
                        }
                    }

                    RowsCount = j;
                }
            }

            if (Central.DebugMode)
            {
                Central.Dbg($"LoadItems: items=[{items.Count}] rows=[{RowsCount}] cells=[{CellsCount}]");
            }
        }

        public int GetCenter()
        {
            int offset = 0;

            var start = ($"{ShipmentDate.ToString("yyyy-MM-dd")} {TimeStart}").ToDateTime("yyyy-MM-dd HH:mm");
            var timeLabel = DateTime.Now;
            var dd = (int)(timeLabel - start).TotalDays;
            int minute = (1440 * dd) + (timeLabel.Hour * 60) + timeLabel.Minute;
            offset = (int)(minute * XScale);

            return offset;
        }
    }
}
