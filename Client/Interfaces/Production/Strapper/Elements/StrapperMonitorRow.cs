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
    /// Строка обвязчик
    /// </summary>
    public class StrapperMonitorRow
    {
        public StrapperMonitorRow(int pixelPerMinute, DateTime startDateTime, DateTime endDateTime)
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
        /// Главный грид строки
        /// </summary>
        public Grid VisualGrid { get; set; }

        /// <summary>
        /// Номер упаковщика
        /// </summary>
        public int StrapperNumber { get; set; }

        /// <summary>
        /// Высота ячейки строки обвязчика.
        /// Независимое значение.
        /// </summary>
        private static int DefaultGridHeight = 50;

        /// <summary>
        /// Ширина ячейки строки обвязчика.
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
        /// Грид операций
        /// </summary>
        public Grid VisualGridOperation { get; set; }

        /// <summary>
        /// Список операций.
        /// Заполняется по номеру упаковщика.
        /// </summary>
        public List<Dictionary<string, string>> OperationList { get; set; }

        /// <summary>
        /// Длительность одной операции, сек.
        /// Независимое значение.
        /// </summary>
        private static int DefaultOperationDuration = 5;

        /// <summary>
        /// Цвет выделения операции.
        /// Независимое значение.
        /// </summary>
        private static string DefaultOperationColor = HColor.BlackFG;

        /// <summary>
        /// Грид координатной сетки
        /// </summary>
        public Grid VisualGridCoordinate { get; set; }

        /// <summary>
        /// Шаг координатной сетки, сек.
        /// Независимое значение.
        /// </summary>
        private static int DefaultCoordinateStep = 60;

        /// <summary>
        /// Грид интервалов между операциями
        /// </summary>
        public Grid VisualGridInterval { get; set; }

        /// <summary>
        /// Список интервалов между операциями.
        /// Заполняется по номеру упаковщика.
        /// </summary>
        public List<Dictionary<string, string>> IntervalList { get; set; }

        /// <summary>
        /// Максимальная длительность интервала для отображения в гриде, мин.
        /// Независимое значение.
        /// </summary>
        public static int MaxIntervalDurationForShow = 5;

        /// <summary>
        /// Цвет выделения интервала между операциями.
        /// Независимое значение.
        /// </summary>
        private static string DefaultIntervalColor = HColor.Green;

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

            VisualGrid = new Grid();
            VisualGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            VisualGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            VisualGrid.Width = GridWidth;
            VisualGrid.Height = DefaultGridHeight;

            VisualGridCoordinate = new Grid();
            VisualGridCoordinate.Width = VisualGrid.Width;
            VisualGridCoordinate.Height = VisualGrid.Height;
            Panel.SetZIndex(VisualGridCoordinate, -1);
            VisualGrid.Children.Add(VisualGridCoordinate);

            VisualGridInterval = new Grid();
            VisualGridInterval.Width = VisualGrid.Width;
            VisualGridInterval.Height = VisualGrid.Height;
            Panel.SetZIndex(VisualGridInterval, 0);
            VisualGrid.Children.Add(VisualGridInterval);

            VisualGridOperation = new Grid();
            VisualGridOperation.Width = VisualGrid.Width;
            VisualGridOperation.Height = VisualGrid.Height;
            Panel.SetZIndex(VisualGridOperation, 1);
            VisualGrid.Children.Add(VisualGridOperation);

            VisualBorder.Child = VisualGrid;
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
                }
            }
        }

        public void CreateVisualOperation()
        {
            VisualGridOperation.Children.Clear();

            if (OperationList != null && OperationList.Count > 0)
            {
                foreach (var operation in OperationList)
                {
                    Border border = new Border();
                    border.Width = ((double)PixelPerMinute / 60) * DefaultOperationDuration;
                    border.HorizontalAlignment = HorizontalAlignment.Left;
                    border.BorderThickness = new Thickness(0);
                    border.Background = DefaultOperationColor.ToBrush();
                    var totalMinutes = (operation.CheckGet("CREATED_DTTM").ToDateTime() - StartDateTime).TotalMinutes;
                    double marginLeft = totalMinutes * PixelPerMinute;
                    border.Margin = new Thickness(marginLeft, 0, 0, 0);
                    border.Padding = new Thickness(0);
                    border.ToolTip = $"Дата: {operation.CheckGet("CREATED_DTTM")}" +
                        $"{Environment.NewLine}Поддон: {operation.CheckGet("PALLET_FULL_NUMBER")}";

                    VisualGridOperation.Children.Add(border);
                }
            }
        }

        public void CreateVisualInterval()
        {
            VisualGridInterval.Children.Clear();

            if (IntervalList != null && IntervalList.Count > 0)
            {
                foreach (var interval in IntervalList)
                {
                    if (interval.CheckGet("INTERVAL_IN_SECONDS").ToDouble() <= 60 * MaxIntervalDurationForShow)
                    {
                        Border border = new Border();
                        border.Width = (interval.CheckGet("INTERVAL_IN_SECONDS").ToDouble() / 60) * PixelPerMinute;
                        border.HorizontalAlignment = HorizontalAlignment.Left;
                        border.BorderThickness = new Thickness(0, 1, 0, 1);
                        border.BorderBrush = DefaultOperationColor.ToBrush();
                        border.Background = DefaultIntervalColor.ToBrush();
                        double marginLeft = (interval.CheckGet("PREVIOUS_DTTM").ToDateTime() - StartDateTime).TotalMinutes * PixelPerMinute;
                        border.Margin = new Thickness(marginLeft, 0, 0, 0);
                        border.Padding = new Thickness(0);
                        border.ToolTip = $"Интервал: {interval.CheckGet("INTERVAL")}" +
                            $"{Environment.NewLine}" +
                            $"{Environment.NewLine}Предыдущая операция: {interval.CheckGet("PREVIOUS_DTTM")}" +
                            $"{Environment.NewLine}Предыдущий поддон: {interval.CheckGet("PREVIOUS_PALLET_FULL_NUMBER")}" +
                            $"{Environment.NewLine}" +
                            $"{Environment.NewLine}Текущая операция: {interval.CheckGet("CURRENT_DTTM")}" +
                            $"{Environment.NewLine}Текущий поддон: {interval.CheckGet("CURRENT_PALLET_FULL_NUMBER")}";

                        VisualGridInterval.Children.Add(border);
                    }
                }
            }
        }
    }
}
