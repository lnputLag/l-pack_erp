using Client.Assets.Converters;
using Client.Assets.HighLighters;
using Client.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Main
{
    /// <summary>
    /// Отображение данных в виде графика
    /// Прокрутка есть только по горизонтали
    /// Вертикального скролла нет
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2022-10-31</released>
    /// <changed>2022-10-31</changed>
    public partial class GraphBox : UserControl
    {
        public GraphBox()
        {
            Loaded += GraphBox_Loaded;

            XAxis = new Axis();
            YAxis = new Axis();
            Data = new List<Dictionary<string, string>>();
            Columns = new List<DataGridHelperColumn>();
            DataSet = new ListDataSet();
            ColumnsList = new List<string>();
            XValues = new Dictionary<double, string>();

            GridColor = "#ffcccccc";
            AxisColor = "#ff333333";
            DashColor = HColor.BlackFG;
            LabelColor = "#ffaaaaaa";

            PrimaryKey = "";
            PrimaryLabel = "";

            Busy = false;
            Initialized = false;
            AutoUpdateInterval = 300;
            ScrollToEnd = false;
            RenderTimes = 0;
            AutoRender = true;
            InvertY = true;

            CellWidth = 20;
            CellHeight = 20;
            XOffset = 25;
            YOffset = 20;
            DashSize = 4;
            LabelYPosition = 0;
            XAxisLabelStep = 2;

            TypeOX = 1;
            IsStepChart = false;

            AxisThickness = 1.5;
            GridThickness = 0.5;
            DashThickness = 1.5;

            YAxis.Step = 50;
            XAxis.Step = 50;

            Profiler = new Profiler();
            Log = "";
            DebugMode = false;

            InitializeComponent();
        }

        /// <summary> 
        /// Даннные для отрисовки
        /// </summary>
        public List<Dictionary<string, string>> Data { get; set; }

        /// <summary> 
        /// Надписи на оси X 
        /// <para> координата надписи ~ сама надпись </para>
        /// </summary>
        public Dictionary<double, string> XValues { get; set; }

        /// <summary> Ось X </summary>
        public Axis XAxis { get; set; }
        /// <summary> Ось Y </summary>
        public Axis YAxis { get; set; }

        /// <summary> Цвет линий сетки </summary>
        public string GridColor { get; set; }
        /// <summary> Цвет осей </summary>
        public string AxisColor { get; set; }
        /// <summary> Толщина осей </summary>
        public double AxisThickness { get; set; }
        /// <summary> Толщина линий сетки </summary>
        public double GridThickness { get; set; }
        /// <summary> Толщина засечек </summary>
        public double DashThickness { get; set; }
        /// <summary> Длина засечек на осях </summary>
        public double DashSize { get; set; }
        /// <summary> Цвет засечек на осях </summary>
        public string DashColor { get; set; }
        /// <summary> Цвет надписей на осях </summary>
        public string LabelColor { get; set; }

        /// <summary>
        /// Цвет для подписи точки, если не задан берется цвет линии
        /// </summary>
        public Brush PointBrush { get; set; }

        /// <summary>
        /// <para>Если используются координаты графика - координата Y растёт снизу вверх</para>
        /// <para>Если используются координаты холста  - координата Y растёт сверху вниз</para>
        /// </summary>
        public bool InvertY { get; set; }
        /// <summary>
        /// Положение надписей к графикам
        /// <para> 0 - none </para>
        /// <para> 1 - top </para>
        /// <para> 2 - bottom </para>
        /// </summary>
        public double LabelYPosition { get; set; }
        /// <summary> 
        /// Шаг между подписями на оси OX
        /// </summary>
        public int XAxisLabelStep { get; set; }

        /// <summary>
        /// Тип оси OX
        /// <para> 1 - линейный </para>
        /// <para> 2 - логарифмический </para>
        /// </summary>
        public int TypeOX { get; set; }
        /// <summary>
        /// Ступенчатый график
        /// </summary>
        public bool IsStepChart { get; set; }

        /// <summary> Смещение графика вправо по оси X </summary>
        public double XOffset { get; set; }
        /// <summary> Смещение графика вверх по оси Y </summary>
        public double YOffset { get; set; }

        /// <summary> Ширина холста </summary>
        public double CanvasWidth { get; set; }
        /// <summary> Высота холста </summary>
        public double CanvasHeight { get; set; }
        /// <summary> Ширина ячейки </summary>
        public double CellWidth { get; set; }
        /// <summary> Высота ячейки </summary>
        public double CellHeight { get; set; }

        /// <summary> Название колонки с данными для оси Y </summary>
        public string PrimaryKey { get; set; }
        /// <summary> Название колонки с подписями на оси X </summary>
        public string PrimaryLabel { get; set; }

        /// <summary>
        /// прокрутка к концу графика
        /// </summary>
        public bool ScrollToEnd { get; set; }
        /// <summary> Счётчик количества рендеров </summary>
        private int RenderTimes { get; set; }
        /// <summary>
        /// автовызов функции Render после UpdateItems
        /// </summary>
        public bool AutoRender { get; set; }

        /// <summary>
        /// Примитивный объект-профайлер.
        /// Отслеживает прошедшее время между двумя событиями.
        /// </summary>
        private Profiler Profiler { get; set; }
        private String Log { get; set; }
        public bool DebugMode { get; set; }

        public void Init()
        {
            LogMsg("Init");
            LogMsg($"    PrimaryKey=[{PrimaryKey}]");
            LogMsg($"    PrimaryLabel=[{PrimaryLabel}]");

            if (Columns?.Count > 0)
            {
                foreach (DataGridHelperColumn c in Columns)
                {
                    if (!PrimaryKey.IsNullOrEmpty()
                        && c.Path == PrimaryKey)
                    {
                        XAxis.Key = c.Path;
                        XAxis.KeyCaption = c.Header;
                        YAxis.KeyCaption = c.Header;
                    }

                    if (!PrimaryLabel.IsNullOrEmpty()
                        && c.Path == PrimaryLabel)
                    {
                        XAxis.KeyCaption = c.Header;
                        YAxis.KeyCaption = c.Header;
                    }
                }
            }

            if (DebugMode)
            {
                DebugButton.Visibility = Visibility.Visible;
                DebugButtonSeparator.Visibility = Visibility.Visible;
            }
            else
            {
                DebugButton.Visibility = Visibility.Collapsed;
                DebugButtonSeparator.Visibility = Visibility.Collapsed;
            }

            Initialized = true;
        }

        public void Destruct()
        {
            AutoUpdateTimerStop();
        }

        public void Render()
        {
            LogMsg("Render");
            Busy = true;

            if (XValues.Count > 0
                && CanvasWidth != 0
                && CanvasHeight != 0)
            {
                RenderPrepare();
                RenderGrid();
                RenderAxis();
                RenderAxisLabels();
                RenderAxisDashes();
                RenderData();
                RenderScrolls();

                RenderTimes++;
            }
            Busy = false;
        }

        public void RenderPrepare()
        {
            //Для оси OX рассчёт идёт на основе шага, заданного извне
            XAxis.Factor = CellWidth / XAxis.Step;
            //Для оси OY рассчёт идёт на основе высоты холста, которая статична и не прокручивается
            YAxis.Factor = (CanvasHeight - CellHeight * 2) / (YAxis.Max - YAxis.Min);

            if (XValues?.Count > 0)
            {
                double xValueMax = 0;
                foreach (KeyValuePair<double, string> item in XValues)
                {
                    var positionX = item.Key;
                    xValueMax = Math.Max(xValueMax, positionX);
                }
                XAxis.Max = xValueMax;

                CanvasWidth = (XAxis.Max - XAxis.Min) * XAxis.Factor + (XOffset * 2);
            }

            Canvas4.Width = XOffset;

            Canvas.Width = CanvasWidth;
            Canvas2.Width = CanvasWidth;
            GraphHorizontalScrollSpacer.Width = CanvasWidth;
            GraphHorizontalScrollSpacer.Height = 1;
            Canvas5.Width = CanvasWidth;

            Canvas.Height = CanvasHeight;
            Canvas2.Height = CanvasHeight;

            LogMsg("RenderPrepare");
            LogMsg($"   Canvas=[{CanvasWidth}]x[{CanvasHeight}]");
            LogMsg($"   Cell=[{CellWidth}]x[{CellHeight}]");
            LogMsg($"   XAxis");
            LogMsg($"       Factor=[{XAxis.Factor}]");
            LogMsg($"       Min=[{XAxis.Min}]");
            LogMsg($"       Max=[{XAxis.Max}]");
            LogMsg($"       Step=[{XAxis.Step}]");
            LogMsg($"   YAxis");
            LogMsg($"       Factor=[{YAxis.Factor}]");
            LogMsg($"       Min=[{YAxis.Min}]");
            LogMsg($"       Max=[{YAxis.Max}]");
            LogMsg($"       Step=[{YAxis.Step}]");
        }

        public void RenderGrid()
        {
            LogMsg("RenderGrid");

            var gridBrush = GridColor.ToBrush();

            // OX
            if (XValues.Count > 0)
            {
                foreach (KeyValuePair<double, string> item in XValues)
                {
                    double x0 = item.Key;
                    double y0 = YAxis.Min;
                    double x = x0;
                    double y = YAxis.Max;
                    var line = MakeLine(x, y, x0, y0, gridBrush, GridThickness);
                    Canvas2.Children.Add(line);
                }
            }

            // OY
            for (double y = YAxis.Min; y <= YAxis.Max; y += YAxis.Step)
            {
                double x0 = XAxis.Min;
                double x = XAxis.Max;
                var line = MakeLine(x, y, x0, y, gridBrush, GridThickness);
                Canvas2.Children.Add(line);
            }
        }

        public void RenderAxis()
        {
            LogMsg("RenderAxis");
            var axisBrush = AxisColor.ToBrush();

            // OX
            {
                InvertY = false;

                double x0 = XAxis.Min;
                double y0 = (0.5 * AxisThickness - YOffset) / YAxis.Factor;
                double x = XAxis.Max;
                double y = y0;
                var line = MakeLine(x, y, x0, y0, axisBrush, AxisThickness);
                Canvas5.Children.Add(line);

                InvertY = true;
            }

            // OY
            {
                double x0 = -0.5 * AxisThickness / XAxis.Factor;
                double y0 = YAxis.Min;
                double x = x0;
                double y = YAxis.Max;
                var line = MakeLine(x, y, x0, y0, axisBrush, AxisThickness);
                Canvas4.Children.Add(line);
            }
        }

        public void RenderAxisLabels()
        {
            LogMsg("RenderAxisLabels");
            var labelBrush = LabelColor.ToBrush();

            // OX
            if (XValues.Count > 0)
            {
                InvertY = false;

                var i = 0;
                foreach (KeyValuePair<double, string> item in XValues)
                {
                    i++;
                    var positionX = item.Key;

                    if (XAxisLabelStep != 0
                        && i % XAxisLabelStep == 0
                        )
                    {
                        var s = $"{item.Value}";
                        double x = positionX;
                        double y = 0;
                        var label = MakeLabel(s, x, y, -YOffset, labelBrush, "center");
                        if (DebugMode)
                        {
                            var tooltip = new InformationTable();
                            tooltip.AddRow("value", item.Key.ToString());
                            tooltip.AddRow("caption", item.Value.ToString());
                            label.ToolTip = tooltip.GetObject();
                        }
                        Canvas5.Children.Add(label);
                    }
                }
                InvertY = true;
            }

            // OY
            for (double y = YAxis.Min; y <= YAxis.Max; y += YAxis.Step)
            {
                if (y != YAxis.Min)
                {
                    int x = 0;
                    var s = y.ToString();

                    var label = MakeLabel(s, x, y, 0, labelBrush, "right", "center");
                    if (DebugMode)
                    {
                        var tooltip = new InformationTable();

                        //tooltip.AddRow("step", i.ToString());
                        tooltip.AddRow("value", y.ToString());
                        label.ToolTip = tooltip.GetObject();
                    }
                    Canvas4.Children.Add(label);
                }
            }
        }

        public void RenderAxisDashes()
        {
            LogMsg("RenderAxisDashes");
            LogMsg($"    XOffset=[{XOffset}]");
            LogMsg($"    YOffset=[{YOffset}]");

            var dashBrush = DashColor.ToBrush();

            // OX
            if (XValues.Count > 0)
            {
                InvertY = false;

                foreach (KeyValuePair<double, string> item in XValues)
                {
                    double x0 = item.Key;
                    double y0 = (-YOffset - DashSize / 2) / YAxis.Factor;
                    double x = x0;
                    double y = (-YOffset + DashSize / 2) / YAxis.Factor;
                    var line = MakeLine(x, y, x0, y0, dashBrush, DashThickness);
                    Canvas5.Children.Add(line);
                }
                InvertY = true;
            }

            // OY
            for (double y = YAxis.Min; y <= YAxis.Max; y += YAxis.Step)
            {
                double x0 = (-DashSize / 2) / XAxis.Factor;
                double x = (DashSize / 2) / XAxis.Factor;
                var line = MakeLine(x, y, x0, y, dashBrush, DashThickness);
                Canvas4.Children.Add(line);
            }
        }

        public void RenderData()
        {
            LogMsg("RenderData");
            foreach (DataGridHelperColumn column in Columns)
            {
                if (column.Path != PrimaryKey
                    && column.Path != PrimaryLabel
                    )
                {
                    YAxis.KeyCaption = column.Header;
                    YAxis.Key = column.Path;

                    double pointXOffset = 0;
                    double labelYOffset = 25;
                    int labelStep = 2;
                    double pointDiameter = 6;
                    double lineThickness = 0.6;
                    bool isStepChart = false;

                    if (column.Params != null)
                    {
                        if (column.Params.ContainsKey("PointXOffset"))
                        {
                            pointXOffset = column.Params.CheckGet("PointXOffset").ToDouble();
                        }

                        if (column.Params.ContainsKey("LabelYPosition"))
                        {
                            LabelYPosition = column.Params.CheckGet("LabelYPosition").ToInt();
                        }
                        if (LabelYPosition == 1)
                        {
                            labelYOffset = -22;
                        }
                        if (LabelYPosition == 2)
                        {
                            labelYOffset = 8;
                        }

                        if (column.Params.ContainsKey("LabelYOffset"))
                        {
                            labelYOffset = column.Params.CheckGet("LabelYOffset").ToDouble();
                        }

                        if (column.Params.ContainsKey("LabelStep"))
                        {
                            labelStep = column.Params.CheckGet("LabelStep").ToInt();
                        }

                        if (column.Params.ContainsKey("PointDiameter"))
                        {
                            pointDiameter = column.Params.CheckGet("PointDiameter").ToDouble();
                        }

                        if (column.Params.ContainsKey("LineThickness"))
                        {
                            lineThickness = column.Params.CheckGet("LineThickness").ToDouble();
                        }
                        
                        if (column.Params.ContainsKey("IsStepChart"))
                        {
                            isStepChart = column.Params.CheckGet("IsStepChart").ToBool();
                        }
                    }

                    RenderGraph(column, pointXOffset, labelYOffset, labelStep, pointDiameter, lineThickness, isStepChart);
                }
            }
        }

        public void RenderGraph(DataGridHelperColumn column, double pointXOffset, double labelYOffset, int labelStep, double pointDiameter, double lineThickness, bool isStepChart)
        {
            double x0 = 0;
            double y0 = 0;
            double x = 0;
            double y = 0;

            //ключ массива, подпись X
            var xCaptionKey = PrimaryKey;
            if (!PrimaryLabel.IsNullOrEmpty())
            {
                xCaptionKey = PrimaryLabel;
            }

            LogMsg("RenderGraph");
            LogMsg($"    XValueKey=[{XAxis.Key}]  ");
            LogMsg($"    XCaptionKey=[{xCaptionKey}] ");
            LogMsg($"    YValueKey=[{YAxis.Key}] ");
            LogMsg($"    LabelYPosition=[{LabelYPosition}] (1=top,2=bottom) ");
            LogMsg($"    LabelYOffset=[{labelYOffset}] ");

            int i = 0;
            foreach (Dictionary<string, string> row in Data)
            {
                i++;

                if (row.ContainsKey(XAxis.Key)
                    && row.ContainsKey(YAxis.Key))
                {
                    string xStr = row.CheckGet(XAxis.Key);
                    string yStr = row.CheckGet(YAxis.Key);

                    if (xStr != "null"
                        && yStr != "null")
                    {
                        x = xStr.ToDouble();
                        y = yStr.ToDouble();

                        x += pointXOffset;

                        var xLabel = row.CheckGet(xCaptionKey);

                        // если график ступенчатый и точки на разной высоте, тогда
                        if (isStepChart
                            && y != y0
                            && i != 1)
                        {
                            // сначала рендерится точка со старой y и новым x
                            RenderNode(column, x, y0, x0, y0, xLabel, i, pointXOffset, labelYOffset, labelStep, pointDiameter, lineThickness);

                            // затем рендерится точка с новой y и новым x
                            RenderNode(column, x, y, x, y0, xLabel, i, pointXOffset, labelYOffset, labelStep, pointDiameter, lineThickness);
                        }
                        // иначе просто рисуем новую точку с линией от старой
                        else
                        {
                            RenderNode(column, x, y, x0, y0, xLabel, i, pointXOffset, labelYOffset, labelStep, pointDiameter, lineThickness);
                        }

                        x0 = x;
                        y0 = y;
                    }
                }
            }
        }

        //FIXTIME: подумать над тем, чтобы не передавать столько параметров
        public void RenderNode(DataGridHelperColumn column, double x, double y, double x0, double y0, string xLabel, int i, double pointXOffset, double labelYOffset, int labelStep, double pointDiameter, double lineThickness)
        {
            var intervals = ProcessStyler(column, y, y0);
            var brush = HColor.Gray.ToBrush();
            if (intervals.Count > 0)
            {
                brush = intervals.LastOrDefault().Item1;
            }

            // точка
            if (pointDiameter > 0)
            {
                var b = MakePoint(x, y, brush, pointDiameter);
                b.ToolTip = MakeTooltip(xLabel, y.ToString());
                Canvas.Children.Add(b);
            }

            // линия от предыдущей точки
            if (i > 1
                && lineThickness > 0
                )
            {
                // линия может быть разделена на участки разного цвета
                foreach (var interval in intervals)
                {
                    Brush brushInterval = interval.Item1;
                    double yInterval = interval.Item2;

                    double dy = Math.Abs(y - yInterval);
                    double dy0 = Math.Abs(y0 - yInterval);

                    double ratio = 1;
                    if (dy0 + dy != 0)
                    {
                        ratio = dy0 / (dy0 + dy);
                    }

                    double dx0 = Math.Abs(x - x0) * ratio;
                    double xInterval = x0 + dx0;

                    Line line = MakeLine(xInterval, yInterval, x0, y0, brushInterval, lineThickness);
                    Canvas.Children.Add(line);

                    x0 = xInterval;
                    y0 = yInterval;
                }
            }

            // надпись
            if (labelStep != 0
                && i % labelStep == 0
                && LabelYPosition != 0
                )
            {
                var yOffset = labelYOffset;


                var b = MakeLabel(y.ToString(), x, y, labelYOffset, PointBrush == null ?  brush : PointBrush, "center");
                Canvas.Children.Add(b);
            }
        }


        public List<(Brush, double)> ProcessStyler(DataGridHelperColumn column, double y, double? yPrevious = null)
        {
            // если линия - цвет интервала, координата Y конца интервала
            // если точка - цвет точки, координата Y точки
            var segments = new List<(Brush, double)>();
            var row = new Dictionary<string, string>();
            row.CheckAdd("VALUE", y.ToString());
            row.CheckAdd("PREVIOUS_VALUE", yPrevious.ToString());

            if (column.Stylers.Count > 0)
            {
                foreach (KeyValuePair<StylerTypeRef, StylerDelegate> styler in column.Stylers)
                {
                    if (styler.Key == StylerTypeRef.ForegroundColor)
                    {
                        var d = (StylerDelegate)styler.Value;
                        if (d != null)
                        {
                            segments = (List<(Brush, double)>)d.Invoke(row);
                        }
                        break;
                    }
                }
            }

            return segments;
        }

        private void GraphBox_Loaded(object sender, RoutedEventArgs e)
        {
            //Render();
        }

        /// <summary>
        /// Конвертация координат на графике в координаты холста
        /// </summary>
        public double TrimX(double x)
        {
            if (TypeOX == 2
                && x >= 0
                )
            {
                x = Math.Log(x + 1) * (XAxis.Max - XAxis.Min) / Math.Log(XAxis.Max + 1);
            }
            x *= XAxis.Factor;
            x += XOffset;
            return x;
        }

        /// <summary>
        /// Конвертация координат на графике в координаты холста
        /// </summary>
        public double TrimY(double y)
        {
            y *= YAxis.Factor;
            y += YOffset;
            if (InvertY)
            {
                y = (CanvasHeight - y);
            }
            return y;
        }

        public Border MakePoint(double xGraph, double yGraph, Brush brush, double size = 5)
        {
            double xCanvas = TrimX(xGraph);
            double yCanvas = TrimY(yGraph);
            xCanvas -= size / 2;
            yCanvas -= size / 2;

            var b = new Border();
            b.Height = size;
            b.Width = size;
            b.Margin = new Thickness(xCanvas, yCanvas, 0, 0);
            b.HorizontalAlignment = HorizontalAlignment.Left;
            b.VerticalAlignment = VerticalAlignment.Top;
            b.BorderThickness = new Thickness(1, 1, 1, 1);
            b.CornerRadius = new CornerRadius(size / 2);
            b.BorderBrush = brush;
            b.Background = brush;
            return b;
        }

        public Line MakeLine(double xGraph, double yGraph, double x0Graph, double y0Graph, Brush brush, double thickness = 0.6)
        {
            var line = new Line();
            line.Visibility = System.Windows.Visibility.Visible;
            line.StrokeThickness = thickness;
            line.Stroke = brush;
            line.StrokeStartLineCap = PenLineCap.Round;
            line.StrokeEndLineCap = PenLineCap.Round;
            line.StrokeDashCap = PenLineCap.Round;

            line.X1 = TrimX(x0Graph);
            line.Y1 = TrimY(y0Graph);
            line.X2 = TrimX(xGraph);
            line.Y2 = TrimY(yGraph);

            return line;
        }

        public Border MakeLabel(string text, double xGraph, double yGraph, double labelYOffset, Brush brush, string horizontalAlignment = "left", string verticalAlignment = "bottom")
        {
            var textBlock = new TextBlock();
            textBlock.Style = (Style)Canvas.FindResource("GraphBoxGraphPointCaption");
            textBlock.Foreground = brush;
            textBlock.Text = text;
            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            textBlock.Arrange(new Rect(textBlock.DesiredSize));

            var height = textBlock.ActualHeight;
            var width = textBlock.ActualWidth;

            double xCanvas = TrimX(xGraph);
            double yCanvas = TrimY(yGraph);
            yCanvas += labelYOffset;
            // если идёт отрисовка надписи для точки на графике
            // и надпись заезжает на ось OX
            // тогда поднимаем надпись ровно на ось OX
            if (InvertY
                && Math.Abs(CanvasHeight - yCanvas) < YOffset + height)
            {
                yCanvas = CanvasHeight - (YOffset + height);
            }

            switch (horizontalAlignment)
            {
                case "left":
                    break;

                case "center":
                    xCanvas -= width / 2;
                    break;

                // пока данный кейс только для рендера надписей слева от OY
                // сдвиг влево, чтобы надпись не наползала на ось и черточки
                case "right":
                    xCanvas -= width + AxisThickness + DashSize / 2;
                    break;
            }

            switch (verticalAlignment)
            {
                case "bottom":
                    break;

                case "center":
                    yCanvas -= height / 2;
                    break;

                case "top":
                    yCanvas -= height;
                    break;
            }

            var b = new Border();
            b.Height = height;
            b.Width = width;
            b.Margin = new Thickness(xCanvas, yCanvas, 0, 0);
            b.HorizontalAlignment = HorizontalAlignment.Left;
            b.VerticalAlignment = VerticalAlignment.Top;
            b.BorderThickness = new Thickness(0, 0, 0, 0);
            b.Child = textBlock;

            return b;
        }

        public Border MakeTooltip(string keyCaption, string valueCaption)
        {
            var result = new Border();

            var xKeyCaption = "";
            var yKeyCaption = "";

            if (!XAxis.KeyCaption.IsNullOrEmpty())
            {
                xKeyCaption = XAxis.KeyCaption;
            }
            else if (!XAxis.Key.IsNullOrEmpty())
            {
                xKeyCaption = XAxis.Key;
            }

            if (!YAxis.KeyCaption.IsNullOrEmpty())
            {
                yKeyCaption = YAxis.KeyCaption;
            }
            else if (!YAxis.Key.IsNullOrEmpty())
            {
                yKeyCaption = YAxis.Key;
            }

            var tooltip = new InformationTable();
            tooltip.AddRow(xKeyCaption, keyCaption);
            tooltip.AddRow(yKeyCaption, valueCaption);
            result = tooltip.GetObject();

            return result;
        }

        public void ClearData()
        {
            LogMsg("ClearData");
            Canvas.Children.Clear();
            Canvas2.Children.Clear();
            Canvas3.Children.Clear();
            Canvas4.Children.Clear();
            Canvas5.Children.Clear();
            //GC.Collect();
        }

        public void RenderScrolls()
        {
            LogMsg("RenderScrolls");
            if (ScrollToEnd && RenderTimes == 0)
            {
                GraphHorizontalScroll.ScrollToRightEnd();
                Canvas5Scroll.ScrollToRightEnd();
                Canvas3Scroll.ScrollToRightEnd();
                Canvas2Scroll.ScrollToRightEnd();
                CanvasScroll.ScrollToRightEnd();
            }
        }


        private DispatcherTimer AutoUpdateTimer { get; set; }
        /// <summary>
        /// Интервал обновления для таймера, секунды
        /// </summary>
        public int AutoUpdateInterval { get; set; }
        private void AutoUpdateTimerRun()
        {
            Central.Dbg($"UpdateTimerRun");
            if (AutoUpdateInterval != 0)
            {
                if (AutoUpdateTimer == null)
                {
                    AutoUpdateTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0, 0, AutoUpdateInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", AutoUpdateInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("GraphBox_AutoUpdateTimerRun", row);
                    }

                    AutoUpdateTimer.Tick += (s, e) =>
                    {
                        Central.Dbg($"UpdateTimer");
                        if (OnLoadItems != null)
                        {
                            Busy = true;
                            OnLoadItems?.Invoke();
                            Busy = false;
                        }
                    };
                }

                if (AutoUpdateTimer.IsEnabled)
                {
                    AutoUpdateTimer.Stop();
                }
                AutoUpdateTimer.Start();
            }
        }

        private void AutoUpdateTimerStop()
        {
            Central.Dbg($"UpdateTimerStop");

            if (AutoUpdateTimer != null)
            {
                if (AutoUpdateTimer.IsEnabled)
                {
                    AutoUpdateTimer.Stop();
                }
            }
        }


        public List<DataGridHelperColumn> Columns { get; set; }
        public void SetColumns(List<DataGridHelperColumn> columns)
        {
            if (columns.Count > 0)
            {
                Columns = columns;
                InitColumns();
            }
        }

        protected void InitColumns()
        {
            if (Canvas != null)
            {
                if (Columns.Count > 0)
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
                    ColumnsList = new List<string>();
                    int columnIndex = 0;
                    foreach (DataGridHelperColumn c in Columns)
                    {
                        //назначим индекс, как порядковый номер по списку
                        //индекс используется для сортировки строк грида
                        c.Index = columnIndex;

                        var p = "";
                        if (!string.IsNullOrEmpty(c.Path))
                        {
                            p = $"{c.Path.Trim()}";
                        }

                        var n = $"{p}";
                        if (!string.IsNullOrEmpty(c.Name))
                        {
                            n = c.Name.Trim();
                        }
                        else
                        {
                            c.Name = n;
                        }

                        var h = $"{p}";
                        if (!string.IsNullOrEmpty(c.Header))
                        {
                            h = c.Header;
                        }
                        c.Header = h;

                        if (!ColumnsList.Contains(c.Name))
                        {
                            ColumnsList.Add(c.Name);
                            c.Enabled = true;
                        }
                        else
                        {
                            c.Enabled = false;
                        }

                        columnIndex++;
                    }
                }
            }
        }

        public delegate void OnLoadItemsDelegate();
        /// <summary>
        /// коллбэк: получение данных
        /// </summary>
        public OnLoadItemsDelegate OnLoadItems;

        public delegate void OnCalculateXValuesDelegate(GraphBox ctl);
        /// <summary>
        /// коллбэк: рассчёт надписей и их координат для оси OX
        /// </summary>
        public OnCalculateXValuesDelegate OnCalculateXValues;

        public delegate void OnFilterItemsDelegate();
        /// <summary>
        /// коллбэк: фильтрация данных
        /// </summary>
        public OnFilterItemsDelegate OnFilterItems;

        public delegate void EnableControlsDelegate();
        public EnableControlsDelegate EnableControls;

        public delegate void DisableControlsDelegate();
        public DisableControlsDelegate DisableControls;


        /// <summary>
        /// получение данных, запуск механизма автообновления
        /// </summary>
        public void Run()
        {
            //внешний фильтр
            //внешние фильтры берут данные из GridItems и возвращают их туда же
            if (OnFilterItems != null)
            {
                OnFilterItems?.Invoke();
            }

            LoadItems();
            AutoUpdateTimerRun();
        }

        /// <summary>
        /// запуск получения данных грида
        /// </summary>
        public void LoadItems()
        {
            if (Initialized)
            {
                if (OnLoadItems != null)
                {
                    Busy = true;
                    OnLoadItems?.Invoke();
                    Busy = false;
                }
            }
        }

        public void ExportDS()
        {

        }

        private bool Busy { get; set; }
        private bool Initialized { get; set; }

        public void ShowSplash()
        {
            Splash.Visibility = Visibility.Visible;
        }

        public void HideSplash()
        {
            Splash.Visibility = Visibility.Collapsed;
        }

        public List<Dictionary<string, string>> GridItems { get; set; }
        private ListDataSet DataSet { get; set; }
        public void UpdateItems(ListDataSet ds = null, bool selectFirst = false)
        {
            if (Initialized)
            {
                ShowSplash();
                DisableControls?.Invoke();

                if (ds != null)
                {
                    DataSet = ds;

                    Busy = true;

                    if (DataSet.Items.Count > 0)
                    {
                        //загружаем в рабочую структуру весь датасет
                        //фильтры будут последовательно просеивать рабочую структуру
                        //остаток будет отправлен в грид
                        GridItems = DataSet.Items;

                        //внешний фильтр
                        //внешние фильтры берут данные из GridItems и возвращают их туда же
                        if (OnFilterItems != null)
                        {
                            OnFilterItems?.Invoke();
                        }

                        Data = GridItems;
                    }
                    else
                    {
                        //внешний фильтр
                        //внешние фильтры берут данные из GridItems и возвращают их туда же
                        if (OnFilterItems != null)
                        {
                            OnFilterItems?.Invoke();
                        }

                        ClearItems();
                    }

                    ClearData();

                    if (OnCalculateXValues != null)
                    {
                        OnCalculateXValues?.Invoke(this);
                    }

                    if (AutoRender)
                    {
                        Render();
                    }

                    Busy = false;
                }

                //StopAutoUpdateTimer();
                //RunAutoUpdateTimer();

                EnableControls?.Invoke();
                HideSplash();
            }
        }

        public string GetLog()
        {
            string result = "";
            result = Log;
            return result;
        }

        public void LogMsg(string msg)
        {
            var d = (int)Profiler.GetDelta();

            var s = "";
            s = $"{s} [{d}]";
            s = $"{s} {msg}";

            Log = Log.AddCR();
            Log = Log.Append(s);
        }

        public void ShowLog()
        {
            var s = Log;
            var e = new DialogWindow("", "GraphBox Log", s);
            e.ShowDialog();
        }


        public void ClearItems()
        {
            Data = new List<Dictionary<string, string>>();
            DataSet.Items = new List<Dictionary<string, string>>();
            GridItems = new List<Dictionary<string, string>>();
        }

        public List<string> ColumnsList { get; set; }

        private void GraphHorizontalScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (sender.ToString() == GraphHorizontalScroll.ToString())
            {
                var v = e.VerticalOffset;
                var h = e.HorizontalOffset;

                //Canvas3Scroll.ScrollToVerticalOffset(v);
                //Canvas3Scroll.ScrollToHorizontalOffset(h);
                Canvas5Scroll.ScrollToVerticalOffset(v);
                Canvas5Scroll.ScrollToHorizontalOffset(h);

                Canvas2Scroll.ScrollToVerticalOffset(v);
                Canvas2Scroll.ScrollToHorizontalOffset(h);

                CanvasScroll.ScrollToVerticalOffset(v);
                CanvasScroll.ScrollToHorizontalOffset(h);
            }
        }
        private void Grid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (!Busy)
                {
                    ClearData();
                    if (e.Delta < 0)
                    {
                        XAxis.Step *= 2;
                        XAxisLabelStep *= 2;
                        Render();
                    }
                    else
                    {
                        XAxis.Step /= 2;
                        XAxisLabelStep /= 2;
                        Render();
                    }
                }
            }
            else
            {
                var d = GraphHorizontalScroll.HorizontalOffset - e.Delta;
                GraphHorizontalScroll.ScrollToHorizontalOffset(d);
            }
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!Busy)
            {
                CanvasWidth = e.NewSize.Width;
                CanvasHeight = e.NewSize.Height;

                ClearData();
                Render();
            }
        }
        private void Render_Click(object sender, RoutedEventArgs e)
        {
            Render();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearData();
        }

        private void RenderContainerButton_Click(object sender, RoutedEventArgs e)
        {
            RenderPrepare();
        }

        private void RenderAxisButton_Click(object sender, RoutedEventArgs e)
        {
            RenderAxis();
        }

        private void RenderDataButton_Click(object sender, RoutedEventArgs e)
        {
            RenderData();
        }

        private void RenderScrollsButton_Click(object sender, RoutedEventArgs e)
        {
            RenderScrolls();
        }

        private void LogsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowLog();
        }

        private void RenderGridButton_Click(object sender, RoutedEventArgs e)
        {
            RenderGrid();
        }

        private void ClearButton_Click_1(object sender, RoutedEventArgs e)
        {
            ClearData();
        }

        private void Logs2Button_Click(object sender, RoutedEventArgs e)
        {
            Log = "";
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadItems();
        }

        private void ExportDSButton_Click(object sender, RoutedEventArgs e)
        {
            ExportDS();
        }
    }

    /// <summary>
    /// Ось
    /// </summary>
    public class Axis
    {
        public Axis()
        {
            Min = 0;
            Max = 100;
            Factor = 1;
            Step = 10;
            Key = "";
            KeyCaption = "";
        }

        public double Min { get; set; }
        public double Max { get; set; }
        /// <summary> 
        /// Коэффициент сжатия координаты на экране относительно координаты на графике
        /// </summary>
        public double Factor { get; set; }
        /// <summary> 
        /// Шаг сетки грида, шаг между значениями/насечками на оси
        /// </summary>
        public double Step { get; set; }
        /// <summary>
        /// Ключ для массива с данными
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// Ключ для массива с подписями 
        /// (если подписи - это не просто значения X, а, например, время)
        /// </summary>
        public string KeyCaption { get; set; }
    }

}

