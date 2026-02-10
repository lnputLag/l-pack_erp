using Client.Interfaces.Stock.RawMaterialMonitor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Карточка сырьевой группы
    /// </summary>
    /// <author>kurasov_dp</author>
    public partial class MaterialGroupElement : UserControl
    {
        public MaterialGroupElement()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Получение данных из таба и заполнение карточки
        /// </summary>
        /// <param name="materialData">Данные материала</param>
        /// <param name="selectedFormat">Выбранный формат (null если все)</param>
        public void SetValue(MaterialData materialData, string selectedFormat = null, bool isCritical = false)
        {
            MaterialNameText.Text = materialData.Name ?? "Без названия";
            FormatContainer.Children.Clear();

            int totalQuty = 0;

            // Если выбран конкретный формат
            if (!string.IsNullOrEmpty(selectedFormat) && selectedFormat != "Все форматы")
            {
                var formatData = materialData.MaterialDataFormats
                    .FirstOrDefault(f => f.Name == selectedFormat);

                if (formatData != null)
                {
                    totalQuty = formatData.QUTY;
                    AddFormatRow(formatData.Name, formatData.QUTY);
                }
            }
            else
            {
                // Если не выбран конкретный формат - показываем все форматы
                var sortedFormats = materialData.MaterialDataFormats
                    .OrderBy(f =>
                    {
                        if (int.TryParse(f.Name, out int num))
                            return num;
                        return int.MaxValue;
                    })
                    .ThenBy(f => f.Name)
                    .ToList();

                foreach (var format in sortedFormats)
                {
                    AddFormatRow(format.Name, format.QUTY);
                    totalQuty += format.QUTY;
                }
            }

            // Обновление итогового кол-ва
            TotalQuantityText.Text = $"Остаток: {totalQuty:N0} кг";
        }

        /// <summary>
        /// Добавление строки с форматом
        /// </summary>
        private void AddFormatRow(string formatName, int qty)
        {
            var grid = new Grid();
            grid.Margin = new Thickness(0, 2, 0, 2);

            // Колонки: название, цветная полоса, количество
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(50, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(100, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(70, GridUnitType.Pixel) });

            // 1. Название формата
            TextBlock formatNameTextBlock = new TextBlock
            {
                Text = formatName,
                Foreground = Brushes.DarkGray,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            };

            // 2. Цветная полоса
            Border colorBar = new Border
            {
                Height = 12,
                Margin = new Thickness(4, 0, 4, 0),
                CornerRadius = new CornerRadius(2),
                VerticalAlignment = VerticalAlignment.Center
            };

            // Определение цвета по количеству
            if (qty == 0)
            {
                colorBar.Background = new SolidColorBrush(Color.FromRgb(204, 204, 204)); // Серый
            }
            else if (qty > 0 && qty <= 100000)
            {
                colorBar.Background = new SolidColorBrush(Color.FromRgb(255, 215, 0)); // Желтый
            }
            else
            {
                colorBar.Background = new SolidColorBrush(Color.FromRgb(50, 205, 50)); // Зеленый
            }

            // 3. Количество
            TextBlock formatQuantityTextBlock = new TextBlock
            {
                Text = qty.ToString("N0"),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Цвет текста в зависимости от количества
            if (qty == 0)
            {
                formatQuantityTextBlock.Foreground = Brushes.Gray;
            }
            else if (qty > 100000)
            {
                formatQuantityTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(39, 174, 96)); // Зеленый
            }
            else
            {
                formatQuantityTextBlock.Foreground = Brushes.Black;
            }

            // Добавление элементов в сетку
            grid.Children.Add(formatNameTextBlock);
            grid.Children.Add(colorBar);
            grid.Children.Add(formatQuantityTextBlock);

            // Установка позиции в колонках
            Grid.SetColumn(formatNameTextBlock, 0);
            Grid.SetColumn(colorBar, 1);
            Grid.SetColumn(formatQuantityTextBlock, 2);

            FormatContainer.Children.Add(grid);
        }
    }
}