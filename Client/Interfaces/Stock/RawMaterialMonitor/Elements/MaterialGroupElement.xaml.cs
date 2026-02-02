using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Client.Interfaces.Stock.Elements
{
    /// <summary>
    /// Карточка материала для отображения в группе
    /// </summary>
    public partial class MaterialCard : UserControl
    {
        public MaterialCard()
        {
            InitializeComponent();
        }

        // Событие при клике на карточку (аналогично OperatorProgressItem)
        public delegate void MouseDownDelegate(object sender, MouseEventArgs e);
        public event MouseDownDelegate OnMouseDown;

        /// <summary>
        /// Установка данных карточки
        /// </summary>
        public void SetData(string materialName, List<MaterialFormat> formats, int totalQuantity)
        {
            MaterialNameText.Text = materialName;
            TotalQuantityText.Text = $"Всего: {totalQuantity} шт";

            FormatsPanel.Children.Clear();

            // Находим максимальное количество для расчета ширины прогресс-баров
            int maxQty = 0;
            foreach (var format in formats)
            {
                if (format.QtyStock > maxQty)
                {
                    maxQty = format.QtyStock;
                }
            }

            // Создаем строки для каждого формата
            foreach (var format in formats)
            {
                AddFormatRow(format, maxQty);
            }
        }

        /// <summary>
        /// Добавление строки формата
        /// </summary>
        private void AddFormatRow(MaterialFormat format, int maxQty)
        {
            var grid = new Grid
            {
                Margin = new Thickness(0, 2, 0, 2)
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });

            // Название формата
            var formatText = new TextBlock
            {
                Text = format.Format,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666")),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(formatText, 0);
            grid.Children.Add(formatText);

            // Прогресс-бар
            var progressContainer = new Border
            {
                Height = 6,
                Margin = new Thickness(8, 0, 8, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F0F0")),
                CornerRadius = new CornerRadius(3),
                VerticalAlignment = VerticalAlignment.Center
            };

            // Рассчитываем ширину прогресс-бара
            double barWidth = maxQty > 0 ? (double)format.QtyStock / maxQty * 100 : 0;

            var progressBar = new Border
            {
                Width = barWidth, // В процентах от родительского контейнера
                Height = 6,
                Background = new SolidColorBrush(GetColorByPercentage(barWidth)),
                CornerRadius = new CornerRadius(3),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            progressContainer.Child = progressBar;
            Grid.SetColumn(progressContainer, 1);
            grid.Children.Add(progressContainer);

            // Количество
            var qtyText = new TextBlock
            {
                Text = $"{format.QtyStock} шт",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333")),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(qtyText, 2);
            grid.Children.Add(qtyText);

            FormatsPanel.Children.Add(grid);
        }

        /// <summary>
        /// Получение цвета в зависимости от заполненности
        /// </summary>
        private Color GetColorByPercentage(double percentage)
        {
            if (percentage >= 70)
                return (Color)ColorConverter.ConvertFromString("#4CAF50"); // Зеленый
            else if (percentage >= 40)
                return (Color)ColorConverter.ConvertFromString("#FF9800"); // Оранжевый
            else
                return (Color)ColorConverter.ConvertFromString("#F44336"); // Красный
        }

        // Обработчик клика (аналогично OperatorProgressItem)
        private void MainBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            OnMouseDown?.Invoke(this, e);
        }
    }

    /// <summary>
    /// Класс для хранения данных формата
    /// </summary>
    public class MaterialFormat
    {
        public string Format { get; set; }
        public int QtyStock { get; set; }
    }
}