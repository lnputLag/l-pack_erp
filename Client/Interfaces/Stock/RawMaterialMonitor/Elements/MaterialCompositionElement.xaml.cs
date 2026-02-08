using Client.Interfaces.Stock.RawMaterialMonitor;
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

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Карточка сырьевой композиции
    /// </summary>
    /// <author>kurasov_dp</author>
    public partial class MaterialCompositionElement : UserControl
    {
        public MaterialCompositionElement()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Заполнение карточки данными композиции
        /// </summary>
        public void SetValue(MaterialDataComposition compositionData)
        {
            if (compositionData == null)
            {
                // Показываем пустую карточку
                CartonNameText.Text = "Нет данных";
                LayersContainer.Children.Clear();
                TotalStockText.Text = "Всего: 0 кг";
                return;
            }

            CartonNameText.Text = compositionData.CartonName ?? "Без названия";
            LayersContainer.Children.Clear();

            // Группировка слоям (layer_number) 
            var groupedLayers = compositionData.Layers
                .GroupBy(l => l.LayerNumber)
                .Select(g => new
                {
                    LayerNumber = g.Key,
                    RawGroup = g.First().RawGroup,
                    Widths = g.SelectMany(x => x.Widths)
                               .GroupBy(w => w.Width)
                               .Select(wg => new MaterialWidthData
                               {
                                   Width = wg.Key,
                                   StockKg = wg.Sum(w => w.StockKg)
                               })
                               .OrderBy(w => w.Width)
                               .ToList()
                })
                .OrderBy(l => l.LayerNumber == "1 внеш" ? 0 :
                             int.TryParse(l.LayerNumber, out int num) ? num : 999)
                .ToList();

            // Создание блока для каждого слоя
            foreach (var layer in groupedLayers)
            {
                AddLayerBlock(layer.LayerNumber, layer.RawGroup, layer.Widths);
            }

            // Обновление итогового кол-ва
            int totalStock = groupedLayers.Sum(l => l.Widths.Sum(w => w.StockKg));
            TotalStockText.Text = $"Всего: {totalStock:N0} кг";
        }

        /// <summary>
        /// Добавление блока для одного слоя
        /// </summary>
        private void AddLayerBlock(string layerNumber, string rawGroup, List<MaterialWidthData> widths)
        {
            int layerTotal = widths.Sum(w => w.StockKg);

            // Главный контейнер
            Border layerContainer = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(0, 0, 0, 8),
                Background = Brushes.White
            };

            Grid mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // Заголовок
            mainGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // Контент (скрывается)

            // 1. Заголовок (всегда видимый)
            Border headerBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                CornerRadius = new CornerRadius(5, 5, 0, 0),
                Padding = new Thickness(10)
            };

            Grid headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            // Левая часть: номер слоя + сырьевая группа
            StackPanel leftPanel = new StackPanel { Orientation = Orientation.Horizontal };

            TextBlock layerNumberText = new TextBlock
            {
                Text = layerNumber,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.DarkSlateGray,
                Margin = new Thickness(0, 0, 8, 0)
            };

            TextBlock rawGroupText = new TextBlock
            {
                Text = rawGroup,
                FontSize = 11,
                Foreground = Brushes.DimGray,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            leftPanel.Children.Add(layerNumberText);
            leftPanel.Children.Add(rawGroupText);

            // Центр: общий вес слоя
            TextBlock totalText = new TextBlock
            {
                Text = $"{layerTotal:N0} кг",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = layerTotal > 0 ? Brushes.DarkGreen : Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(15, 0, 15, 0)
            };

            // Правая часть: кнопка развернуть/свернуть
            Button toggleButton = new Button
            {
                Content = "▼",
                FontSize = 10,
                Width = 24,
                Height = 24,
                Padding = new Thickness(2),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = Brushes.Gray,
                Cursor = Cursors.Hand,
                Tag = false // флаг(свернуто - false/развернуто - true)
            };

            Grid.SetColumn(leftPanel, 0);
            Grid.SetColumn(totalText, 1);
            Grid.SetColumn(toggleButton, 2);

            headerGrid.Children.Add(leftPanel);
            headerGrid.Children.Add(totalText);
            headerGrid.Children.Add(toggleButton);
            headerBorder.Child = headerGrid;

            Grid.SetRow(headerBorder, 0);
            mainGrid.Children.Add(headerBorder);

            // 2. Контент слоя (изначально скрыт)
            Border contentBorder = new Border
            {
                Padding = new Thickness(10, 5, 10, 10),
                Visibility = Visibility.Collapsed
            };

            StackPanel contentPanel = new StackPanel();

            // Добавление ширины 
            Grid widthsGrid = new Grid();
            widthsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(45) });
            widthsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            widthsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(80) });

            int rowIndex = 0;
            var sortedWidths = widths.OrderBy(w => w.Width).ToList();

            foreach (var widthData in sortedWidths)
            {
                widthsGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

                    // 1. Ширина
                    TextBlock widthText = new TextBlock
                    {
                        Text = widthData.Width.ToString(),
                        FontSize = 10,
                        Foreground = Brushes.Gray,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(widthText, 0);
                    Grid.SetRow(widthText, rowIndex);
                    widthsGrid.Children.Add(widthText);

                    // 2. Цветная полоска
                    Border colorBar = new Border
                    {
                        Height = 12,
                        Margin = new Thickness(5, 0, 5, 0),
                        CornerRadius = new CornerRadius(2),
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    // Определяем цвет в зависимости от количества кг (тестовые значения)
                    if (widthData.StockKg == 0)
                    {
                        colorBar.Background = new SolidColorBrush(Color.FromRgb(204, 204, 204)); // Серый
                    }
                    else if (widthData.StockKg > 0 && widthData.StockKg <= 50000)
                    {
                        colorBar.Background = new SolidColorBrush(Color.FromRgb(255, 215, 0)); // Желтый
                    }
                    else
                    {
                        colorBar.Background = new SolidColorBrush(Color.FromRgb(50, 205, 50)); // Зеленый
                    }

                    Grid.SetColumn(colorBar, 1);
                    Grid.SetRow(colorBar, rowIndex);
                    widthsGrid.Children.Add(colorBar);

                    // 3. Количество (кг)
                    TextBlock stockText = new TextBlock
                    {
                        Text = widthData.StockKg.ToString("N0") + " кг",
                        FontSize = 11,
                        FontWeight = FontWeights.SemiBold,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    // Цвет текста
                    if (widthData.StockKg == 0)
                    {
                        stockText.Foreground = Brushes.Gray;
                    }
                    else if (widthData.StockKg > 100000)
                    {
                        stockText.Foreground = new SolidColorBrush(Color.FromRgb(39, 174, 96)); // Зеленый
                    }
                    else
                    {
                        stockText.Foreground = Brushes.Black;
                    }

                    Grid.SetColumn(stockText, 2);
                    Grid.SetRow(stockText, rowIndex);
                    widthsGrid.Children.Add(stockText);

                    rowIndex++;
                }

            contentPanel.Children.Add(widthsGrid);

            // Итог по слою 
            if (widths.Count > 0)
            {
                TextBlock layerTotalCompactText = new TextBlock
                {
                    Text = $"Итого: {layerTotal:N0} кг",
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.DarkSlateBlue,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 8, 0, 0)
                };
                contentPanel.Children.Add(layerTotalCompactText);
            }

            contentBorder.Child = contentPanel;
            Grid.SetRow(contentBorder, 1);
            mainGrid.Children.Add(contentBorder);

            // Обработчик клика по кнопке
            toggleButton.Click += (s, e) =>
            {
                bool isExpanded = (bool)toggleButton.Tag;
                toggleButton.Tag = !isExpanded;

                if (!isExpanded)
                {
                    contentBorder.Visibility = Visibility.Visible;
                    toggleButton.Content = "▲";
                    layerContainer.Background = new SolidColorBrush(Color.FromRgb(250, 250, 250));
                }
                else
                {
                    contentBorder.Visibility = Visibility.Collapsed;
                    toggleButton.Content = "▼";
                    layerContainer.Background = Brushes.White;
                }
            };

            layerContainer.Child = mainGrid;
            LayersContainer.Children.Add(layerContainer);
        }
    }
}
