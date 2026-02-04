using Client.Interfaces.Stock.RawMaterialMonitor;
using DevExpress.XtraRichEdit.Fields;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        public void SetValue(MaterialData materialData)
        {
            MaterialNameText.Text = materialData.Name;

            int totalQuty = 0;

            foreach (var a in materialData.MaterialDataFormats)
            {
                totalQuty += a.QUTY;

                var grid = new Grid();
                grid.Margin = new Thickness(0, 2, 0, 2); // Отступ между строками

                // колонки: название, цветная полоса, количество
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(50, GridUnitType.Pixel) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(100, GridUnitType.Pixel) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(70, GridUnitType.Pixel) });

                // 1. Название формата
                TextBlock formatNameTextBlock = new TextBlock();
                formatNameTextBlock.Text = a.Name;
                formatNameTextBlock.Foreground = Brushes.DarkGray;
                formatNameTextBlock.FontSize = 12;
                formatNameTextBlock.VerticalAlignment = VerticalAlignment.Center;

                // 2. Цветная полоса
                Border colorBar = new Border();
                colorBar.Height = 16;
                colorBar.Margin = new Thickness(4, 0, 4, 0);
                colorBar.CornerRadius = new CornerRadius(2);
                colorBar.VerticalAlignment = VerticalAlignment.Center;

                // Определение цвета (тестовые значения)
                if (a.QUTY == 0)
                {
                    colorBar.Background = new SolidColorBrush(Color.FromRgb(204, 204, 204)); // Серый
                }
                else if (a.QUTY > 0 && a.QUTY <= 100000)
                {
                    colorBar.Background = new SolidColorBrush(Color.FromRgb(255, 215, 0)); // Желтый (Gold)
                }
                else
                {
                    colorBar.Background = new SolidColorBrush(Color.FromRgb(50, 205, 50)); // Зеленый (LimeGreen)
                }

                // 3. Количество
                TextBlock formatQuantityTextBlock = new TextBlock();
                formatQuantityTextBlock.Text = a.QUTY.ToString("N0"); // Форматирование с разделителями тысяч
                formatQuantityTextBlock.FontSize = 11;
                formatQuantityTextBlock.FontWeight = FontWeights.Bold;
                formatQuantityTextBlock.HorizontalAlignment = HorizontalAlignment.Right;
                formatQuantityTextBlock.VerticalAlignment = VerticalAlignment.Center;

                // Цвет текста в зависимости от количества
                if (a.QUTY == 0)
                {
                    formatQuantityTextBlock.Foreground = Brushes.Gray;
                }
                else if (a.QUTY > 100000)
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

            // Обновление итогового кол-ва
            TotalQuantityText.Text = $"Остаток: {totalQuty:N0} кг";
        }
    }
}