using Client.Assets.HighLighters;
using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace Client.Interfaces.Production.Strapper
{
    /// <summary>
    /// Строка с временными метками
    /// </summary>
    public class StrapperMonitorTimeLine
    {
        public StrapperMonitorTimeLine(int pixelPerMinute, DateTime startDateTime, DateTime endDateTime)
        {
            PixelPerMinute = pixelPerMinute;
            StartDateTime = startDateTime;
            EndDateTime = endDateTime;
            TotalMinutes = (EndDateTime - StartDateTime).TotalMinutes;
            GridWidth = (int)Math.Ceiling(TotalMinutes * PixelPerMinute);
        }

        /// <summary>
        /// Главный бордер строки
        /// </summary>
        public Border VisualBorder { get; set; }

        /// <summary>
        /// Грид координатной сетки
        /// </summary>
        public Grid VisualGridCoordinate { get; set; }

        /// <summary>
        /// Высота ячейки временной метки.
        /// Независимое значение.
        /// </summary>
        private static int DefaultGridHeight = 25;

        /// <summary>
        /// Ширина ячейки временной метки.
        /// Рассчитывается автоматически в конструкторе.
        /// </summary>
        private int GridWidth;

        /// <summary>
        /// Дата начала строки с временными метками.
        /// Заполнчется через параметр в конструкторе.
        /// </summary>
        private DateTime StartDateTime { get; set; }

        /// <summary>
        /// Дата окончания строки с временными метками.
        /// Заполнчется через параметр в конструкторе.
        /// </summary>
        private DateTime EndDateTime { get; set; }

        /// <summary>
        /// Суммарное количество минут в строке с временными метками.
        /// Рассчитывается автоматически в конструкторе.
        /// </summary>
        private double TotalMinutes { get; set; }

        /// <summary>
        /// Количество пикселей для отображения одной минуты.
        /// Заполнчется через параметр в конструкторе.
        /// </summary>
        private int PixelPerMinute;

        /// <summary>
        /// Шаг координатной сетки, сек.
        /// Независимое значение.
        /// </summary>
        private static int DefaultCoordinateStep = 300;

        /// <summary>
        /// Цвет стандартной ячейки.
        /// Независимое значение.
        /// </summary>
        private static string DefaultBorderColor = HColor.Gray;

        public void CreateVisual()
        {
            VisualBorder = new Border();
            VisualBorder.BorderBrush = DefaultBorderColor.ToBrush();
            VisualBorder.BorderThickness = new Thickness(1);

            VisualGridCoordinate = new Grid();
            VisualGridCoordinate.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            VisualGridCoordinate.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            VisualGridCoordinate.Width = GridWidth;
            VisualGridCoordinate.Height = DefaultGridHeight;

            VisualBorder.Child = VisualGridCoordinate;
        }

        public void CreateVisualCoordinate()
        {
            VisualGridCoordinate.Children.Clear();

            if (TotalMinutes > 0)
            {
                int coordinateStepCount = (int)Math.Ceiling((TotalMinutes * 60) / DefaultCoordinateStep);
                double coordinateStepPixel = ((double)PixelPerMinute / 60) * DefaultCoordinateStep;

                for (int i = 0; i < coordinateStepCount; i++)
                {
                    Border border = new Border();
                    border.Width = 1;
                    border.HorizontalAlignment = HorizontalAlignment.Left;
                    border.BorderThickness = new Thickness(0);
                    border.Background = DefaultBorderColor.ToBrush();
                    double marginLeft = coordinateStepPixel * i;
                    border.Margin = new Thickness(marginLeft, 0, 0, 0);
                    VisualGridCoordinate.Children.Add(border);

                    Label label = new Label();
                    string coordinateStepTime = StartDateTime.AddSeconds(DefaultCoordinateStep * i).ToString("dd.MM HH:mm");
                    label.Content = coordinateStepTime;
                    label.Margin = new Thickness(marginLeft + 5, 0, 0, 0);
                    label.Foreground = HColor.BlackFG.ToBrush();
                    label.FontSize = 12;
                    label.VerticalAlignment = VerticalAlignment.Center;
                    label.HorizontalAlignment = HorizontalAlignment.Left;
                    VisualGridCoordinate.Children.Add(label);
                }
            }
        }
    }
}
