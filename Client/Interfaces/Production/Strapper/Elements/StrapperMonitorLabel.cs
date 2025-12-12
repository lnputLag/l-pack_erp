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
    /// Заголовок строки обвязчика
    /// </summary>
    partial class StrapperMonitorLabel
    {
        public StrapperMonitorLabel(double gridHeight)
        {
            GridHeight = gridHeight;
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
        /// Имя упаковщика. 
        /// Отображается в всплывающей подсказке.
        /// </summary>
        public string StrapperName { get; set; }

        /// <summary>
        /// Высота ячейки заголовка. 
        /// Записит от строки, для которой создаётся заголовок.
        /// Заполняется через переданный параметр в конструкторе.
        /// </summary>
        private double GridHeight;

        /// <summary>
        /// Ширина ячейки заголовка. 
        /// Независимое значение.
        /// </summary>
        private static int GridWidth = 50;

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
            VisualGrid.Height = GridHeight;

            Label label = new Label();
            label.Content = StrapperName;
            label.Foreground = HColor.BlackFG.ToBrush();
            label.FontSize = 12;
            label.VerticalAlignment = VerticalAlignment.Center;
            label.HorizontalAlignment = HorizontalAlignment.Center;
            VisualGrid.Children.Add(label);

            VisualBorder.Child = VisualGrid;
        }
    }
}
