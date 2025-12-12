using Client.Assets.HighLighters;
using Client.Common;
using DevExpress.ClipboardSource.SpreadsheetML;
using Microsoft.Office.Interop.Excel;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Логика взаимодействия для PieChart.xaml
    /// </summary>
    public partial class PieChart : UserControl
    {
        public PieChart()
        {
            InitializeComponent();

            SetDefault();
        }

        private double Radius { get; set; }

        private double DefaultLabelBorderSize { get; set; }

        private const string DefaultColor = HColor.Gray;

        private static List<Dictionary<string, string>> DefaultData = new List<Dictionary<string, string>>()
        {
            new Dictionary<string, string>()
            {
                { "VALUE", "360" },
                // Добавляем очень маленькое значение к сумме, из-за бага отображение одного элемента на всю диаграмму
                { "ANGLE", $"{360 * 2.0 * Math.PI / (360 + (360 / 100 * 0.1))}" },
                { "COLOR", DefaultColor }
            }
        };

        public void SetDefault()
        {
            Radius = Math.Floor((MainContainer.Height - 2) / 2);
            DefaultLabelBorderSize = 10;
            SetValues();
        }

        public void SetValues(List<Dictionary<string, string>> data = null)
        {
            if (data != null && data.Count > 0)
            {
                double sum = data.Sum(x => x.CheckGet("VALUE").ToDouble());
                if (sum > 0)
                {
                    if (data.Count(x => x.CheckGet("VALUE").ToDouble() > 0) == 1)
                    {
                        // Добавляем очень маленькое значение к сумме, из-за бага отображение одного элемента на всю диаграмму
                        sum = sum + (sum / 100 * 0.1);
                    }

                    foreach (var item in data)
                    {
                        item.CheckAdd("ANGLE", $"{item.CheckGet("VALUE").ToDouble() * 2.0 * Math.PI / sum}");
                    }
                }
                else
                {
                    data = DefaultData;
                }
            }
            else
            {
                data = DefaultData;
            }

            double startAngle = 0.0;

            System.Windows.Point centerPoint = new System.Windows.Point(Radius, Radius);
            Size xyradius = new Size(Radius, Radius);

            // FIXME не работает, если в массиве только одно значение
            MainContainer.Children.Clear();
            LabelContainer.Children.Clear();
            foreach (var item in data)
            {
                double angle = item.CheckGet("ANGLE").ToDouble();

                double endAngle = startAngle + angle;

                System.Windows.Point startPoint = centerPoint;
                startPoint.Offset(Radius * Math.Cos(startAngle), Radius * Math.Sin(startAngle));

                System.Windows.Point endPoint = centerPoint;
                endPoint.Offset(Radius * Math.Cos(endAngle), Radius * Math.Sin(endAngle));

                double angleDeg = angle * 180.0 / Math.PI;

                string color = DefaultColor;
                if (!string.IsNullOrEmpty(item.CheckGet("COLOR")))
                {
                    color = item.CheckGet("COLOR");
                }

                Path p = new Path()
                {
                    Stroke = Brushes.Black,                    
                    Fill = color.ToBrush(),
                    ToolTip = item.CheckGet("DESCRIPTION"),
                    Data = new PathGeometry(
                        new PathFigure[]
                        {
                            new PathFigure(
                                centerPoint,
                                new PathSegment[]
                                {
                                    new LineSegment(startPoint, isStroked: true),
                                    new ArcSegment(endPoint, xyradius,
                                                   angleDeg, angleDeg > 180,
                                                   SweepDirection.Clockwise, isStroked: true)
                                },
                                closed: true)
                        })
                };

                MainContainer.Children.Add(p);

                // Создаём легенду для добавленного кусочка круговой диаграммы
                {
                    StackPanel stackPanel = new StackPanel();
                    stackPanel.Orientation = Orientation.Horizontal;

                    System.Windows.Controls.Border border = new System.Windows.Controls.Border();
                    border.Width = DefaultLabelBorderSize;
                    border.Height = DefaultLabelBorderSize;
                    border.Background = color.ToBrush();
                    border.Margin = new Thickness(0, 0, 2, 0);
                    border.BorderBrush = HColor.BlackFG.ToBrush();
                    border.BorderThickness = new Thickness(1);
                    border.ToolTip = item.CheckGet("DESCRIPTION");
                    stackPanel.Children.Add(border);

                    TextBlock textBlock = new TextBlock();
                    textBlock.VerticalAlignment = VerticalAlignment.Center;
                    textBlock.HorizontalAlignment = HorizontalAlignment.Left;
                    textBlock.TextWrapping = TextWrapping.Wrap;
                    textBlock.Text = item.CheckGet("NAME");
                    textBlock.ToolTip = item.CheckGet("DESCRIPTION");
                    stackPanel.Children.Add(textBlock);

                    LabelContainer.Children.Add(stackPanel);
                }

                startAngle = endAngle;
            }
        }
    }
}
