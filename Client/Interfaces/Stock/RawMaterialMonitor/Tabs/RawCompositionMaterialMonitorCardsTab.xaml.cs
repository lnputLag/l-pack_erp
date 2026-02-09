using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
using Client.Interfaces.Stock.RawMaterialMonitor;
using Newtonsoft.Json;
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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Color = System.Windows.Media.Color;


namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Отстаток по сырьевым композициям на складе
    /// </summary>
    /// <author>kurasov_dp</author>
    public partial class RawCompositionMaterialMonitorCardsTab : ControlBase
    {
        private List<MaterialDataComposition> _compositions;
        private List<MaterialDataComposition> _filteredCompositions;

        // Счетчики для метрик
        private int _totalCompositions = 0;
        private int _criticalRemainsCount = 0;
        private int _lowRemainsCount = 0;
        private int _highRemainsCount = 0;

        // Текущая выбранная категория
        private string _selectedCategory = null;

        // Классификации (в кг)
        private const int CRITICAL_THRESHOLD = 1000000;       
        private const int LOW_THRESHOLD = 2500000;           

        // Константы для категорий
        private const string CATEGORY_CRITICAL = "critical";
        private const string CATEGORY_LOW = "low";
        private const string CATEGORY_HIGH = "high";

        public RawCompositionMaterialMonitorCardsTab()
        {
            InitializeComponent();
            RoleName = "[erp]raw_material_monitor";
            ControlTitle = "Монитор остатков сырья";
            DocumentationUrl = "/doc/l-pack-erp";

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (System.Windows.Input.KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };

            OnLoad = () =>
            {
                SetDefaults();
            };

            OnUnload = () => { };
            OnFocusGot = () => { };
            OnFocusLost = () => { };

            ///<summary>
            /// Система команд (Commander)
            ///</summary>
            {
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "help",
                        Enabled = true,
                        Title = "Справка",
                        Description = "Показать справочную информацию",
                        MenuUse = true,
                        ButtonUse = true,
                        ButtonName = "HelpButton",
                        HotKey = "F1",
                        Action = () =>
                        {
                            Central.ShowHelp(DocumentationUrl);
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "loadAll",
                        Enabled = true,
                        Title = "Загрузить все",
                        Description = "Загрузить все композиции",
                        MenuUse = true,
                        ButtonUse = true,
                        ButtonName = "LoadAllButton",
                        Action = () => LoadAllButton_Click(null, null),
                    });
                }
            }
            Commander.Init(this);
        }

        /// <summary>
        /// Загрузка данных (из БД)
        /// </summary>
        private List<MaterialDataComposition> LoadCompositionsData()
        {
            var compositions = new List<MaterialDataComposition>();

            var p = new Dictionary<string, string>();

            // Выбор из выпадающего списка
            var selectedPlatform = PlatformSelectBox.SelectedItem;
            if (!selectedPlatform.Equals(default(KeyValuePair<string, string>)))
            {
                p.Add("FACTORY_ID", selectedPlatform.Key);
            }
            else
            {
                p.Add("FACTORY_ID", "1"); // Значение по умолчанию
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "RawMaterialResidueMonitor");
            q.Request.SetParam("Action", "RawCompositionList");

            q.Request.SetParams(p);
            q.Request.Timeout = 80000;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    foreach (var item in ds.Items)
                    {
                        int width = item.CheckGet("WIDTH").ToInt();
                        int idc = item.CheckGet("IDC").ToInt();

                        // Поиск существующей композиции по IDC
                        if (compositions.Count(x => x.Idc == idc) > 0)
                        {
                            var comp = compositions.FirstOrDefault(x => x.Idc == idc);

                            string layerNumber = item.CheckGet("LAYER_NUMBER").ToString();
                            string rawGroup = item.CheckGet("RAW_GROUP").ToString();

                            // Поиск существующего слоя в композиции
                            if (comp.Layers.Count(x => x.LayerNumber == layerNumber && x.RawGroup == rawGroup) > 0)
                            {
                                var layer = comp.Layers.FirstOrDefault(x => x.LayerNumber == layerNumber && x.RawGroup == rawGroup);
                                layer.Widths.Add(new MaterialWidthData()
                                {
                                    Width = width,
                                    StockKg = item.CheckGet("STOCK_KG").ToInt()
                                });
                            }
                            else
                            {
                                // Создание нового слоя
                                var newLayer = new MaterialLayerData();
                                newLayer.LayerNumber = layerNumber;
                                newLayer.RawGroup = rawGroup;
                                newLayer.Widths.Add(new MaterialWidthData()
                                {
                                    Width = width,
                                    StockKg = item.CheckGet("STOCK_KG").ToInt()
                                });
                                comp.Layers.Add(newLayer);
                            }
                        }
                        else
                        {
                            // Создание новой композиции
                            var newComp = new MaterialDataComposition();
                            newComp.Idc = idc;
                            newComp.CartonName = item.CheckGet("CARTON_NAME").ToString();

                            // Создание первого слоя
                            var newLayer = new MaterialLayerData();
                            newLayer.LayerNumber = item.CheckGet("LAYER_NUMBER").ToString();
                            newLayer.RawGroup = item.CheckGet("RAW_GROUP").ToString();
                            newLayer.Widths.Add(new MaterialWidthData()
                            {
                                Width = width,
                                StockKg = item.CheckGet("STOCK_KG").ToInt()
                            });

                            newComp.Layers.Add(newLayer);
                            compositions.Add(newComp);
                        }
                    }

                    // Сортировка по ширине внутри каждого слоя
                    foreach (var comp in compositions)
                    {
                        foreach (var layer in comp.Layers)
                        {
                            layer.Widths = layer.Widths.OrderBy(w => w.Width).ToList();
                        }
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            return compositions;
        }

        public void SetDefaults()
        {
            PlatformSelectBox.SetItems(new Dictionary<string, string>()
            {
                {"1",  "Липецк"},
                {"2",  "Кашира"},
            });
            PlatformSelectBox.SelectedItem = PlatformSelectBox.Items.First();
        }

        private void RefreshCompositionData()
        {
            _compositions = LoadCompositionsData();
            CalculateMetrics();
            UpdateMetricsDisplay();
            UpdateSummaryPanels();
            ClearZone2AndZone3();
        }

        /// <summary>
        /// Очистка зон 2 и 3
        /// </summary>
        private void ClearZone2AndZone3()
        {
            CompositionGrid.ItemsSource = null;
            TableTitle.Text = "Выберите категорию";
            DetailsCard.SetValue(null);
            _selectedCategory = null;
        }

        /// <summary>
        /// Создание сводок для Зоны 1
        /// </summary>
        private void UpdateSummaryPanels()
        {
            SummaryPanel.Children.Clear();

            if (_compositions == null || _compositions.Count == 0)
            {
                SummaryPanel.Children.Add(new TextBlock
                {
                    Text = "Нет данных для отображения",
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                });
                return;
            }

            // 1. Критические остатки (до 1 000 000 кг) - от меньшего к большему
            var criticalCompositions = _compositions
                .Where(c => c.TotalStockKg <= CRITICAL_THRESHOLD)
                .OrderBy(c => c.TotalStockKg)
                .Take(10)
                .Select(c => new MaterialCompositionSummaryItem
                {
                    CartonName = c.CartonName,
                    Idc = c.Idc,
                    TotalStockKg = c.TotalStockKg,
                    Category = CATEGORY_CRITICAL
                })
                .ToList();
            CreateCompositionSummaryCategory("КРИТИЧЕСКИЕ ОСТАТКИ (до 1 000 000 кг)", criticalCompositions, CATEGORY_CRITICAL, true);

            // 2. Низкие остатки (1 000 001 - 2 500 000 кг) - от меньшего к большему
            var lowCompositions = _compositions
                .Where(c => c.TotalStockKg > CRITICAL_THRESHOLD && c.TotalStockKg <= LOW_THRESHOLD)
                .OrderBy(c => c.TotalStockKg)
                .Take(10)
                .Select(c => new MaterialCompositionSummaryItem
                {
                    CartonName = c.CartonName,
                    Idc = c.Idc,
                    TotalStockKg = c.TotalStockKg,
                    Category = CATEGORY_LOW
                })
                .ToList();
            CreateCompositionSummaryCategory("НИЗКИЕ ОСТАТКИ (1 000 001 - 2 500 000 кг)", lowCompositions, CATEGORY_LOW, true);

            // 3. Большие остатки (> 2 500 000 кг) - от меньшего к большему (ИНВЕРСИЯ)
            var highCompositions = _compositions
                .Where(c => c.TotalStockKg > LOW_THRESHOLD)
                .OrderBy(c => c.TotalStockKg) 
                .Take(10)
                .Select(c => new MaterialCompositionSummaryItem
                {
                    CartonName = c.CartonName,
                    Idc = c.Idc,
                    TotalStockKg = c.TotalStockKg,
                    Category = CATEGORY_HIGH
                })
                .ToList();
            CreateCompositionSummaryCategory("БОЛЬШИЕ ОСТАТКИ (> 2 500 000 кг)", highCompositions, CATEGORY_HIGH, true);

            // 4. График распределения по форматам 
            CreateFormatDistributionChart();
        }

        /// <summary>
        /// Создание сводки для категории композиций с сеткой
        /// </summary>
        private void CreateCompositionSummaryCategory(string title, List<MaterialCompositionSummaryItem> items, string categoryId, bool showGrid = true)
        {
            Border categoryContainer = new Border
            {
                Style = (Style)FindResource("SummaryCategoryStyle")
            };

            StackPanel categoryPanel = new StackPanel();

            // Заголовок категории
            Border headerBorder = new Border
            {
                Style = (Style)FindResource("SummaryHeaderStyle")
            };

            TextBlock titleText = new TextBlock
            {
                Text = $"{title} (ТОП-10)",
                Style = (Style)FindResource("SummaryTitleStyle")
            };

            headerBorder.Child = titleText;
            categoryPanel.Children.Add(headerBorder);

            if (items.Count > 0)
            {
                // Находим максимальное значение для шкалы
                int maxStock = items.Max(c => c.TotalStockKg);
                if (maxStock == 0) maxStock = 1;

                // Добавляем заголовок сетки
                if (showGrid)
                {
                    Grid headerGrid = new Grid
                    {
                        Style = (Style)FindResource("SummaryItemGridStyle"),
                        Margin = new Thickness(0, 5, 0, 5)
                    };

                    headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                    headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                    headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

                    // Заголовки колонок
                    TextBlock nameHeader = new TextBlock
                    {
                        Text = "Картон",
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.DarkGray
                    };
                    Grid.SetColumn(nameHeader, 0);
                    headerGrid.Children.Add(nameHeader);

                    TextBlock chartHeader = new TextBlock
                    {
                        Text = "Остаток",
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.DarkGray,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    Grid.SetColumn(chartHeader, 1);
                    headerGrid.Children.Add(chartHeader);

                    TextBlock valueHeader = new TextBlock
                    {
                        Text = "Кг",
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.DarkGray,
                        HorizontalAlignment = HorizontalAlignment.Right
                    };
                    Grid.SetColumn(valueHeader, 2);
                    headerGrid.Children.Add(valueHeader);

                    categoryPanel.Children.Add(headerGrid);
                }

                // Линия-разделитель
                Rectangle separator = new Rectangle
                {
                    Height = 1,
                    Fill = Brushes.LightGray,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                categoryPanel.Children.Add(separator);

                foreach (var item in items)
                {
                    Border itemBorder = new Border
                    {
                        Style = (Style)FindResource("SummaryItemStyle"),
                        Tag = new { Category = categoryId, CompositionId = item.Idc }
                    };

                    itemBorder.MouseLeftButtonDown += (s, e) =>
                    {
                        HandleCompositionSummaryItemClick(categoryId, item.Idc);
                    };

                    Grid itemGrid = new Grid
                    {
                        Style = (Style)FindResource("SummaryItemGridStyle")
                    };

                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

                    // Название композиции
                    TextBlock nameText = new TextBlock
                    {
                        Text = item.CartonName,
                        FontSize = 11,
                        FontWeight = FontWeights.SemiBold,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        VerticalAlignment = VerticalAlignment.Center,
                        Foreground = Brushes.Black
                    };
                    Grid.SetColumn(nameText, 0);
                    itemGrid.Children.Add(nameText);

                    // Полоска-индикатор 
                    Border barChart = new Border
                    {
                        Style = (Style)FindResource("BarChartStyle"),
                        Width = Math.Max(50, (item.TotalStockKg / (double)maxStock) * 150),
                        Background = item.CategoryColor,
                        ToolTip = $"{item.FormattedStock} кг",
                        HorizontalAlignment = HorizontalAlignment.Right
                    };
                    Grid.SetColumn(barChart, 1);
                    itemGrid.Children.Add(barChart);

                    // Общий остаток по композиции 
                    TextBlock valueText = new TextBlock
                    {
                        Text = item.FormattedStock + " кг",
                        FontSize = 11,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = item.CategoryColor,
                        VerticalAlignment = VerticalAlignment.Center,
                        MinWidth = 100,
                        TextAlignment = TextAlignment.Right,
                        Margin = new Thickness(5, 0, 0, 0)
                    };
                    Grid.SetColumn(valueText, 2);
                    itemGrid.Children.Add(valueText);

                    itemBorder.Child = itemGrid;
                    categoryPanel.Children.Add(itemBorder);
                }
            }
            else
            {
                categoryPanel.Children.Add(new TextBlock
                {
                    Text = "Нет композиций в этой категории",
                    FontSize = 11,
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 10)
                });
            }

            categoryContainer.Child = categoryPanel;
            SummaryPanel.Children.Add(categoryContainer);
        }

        /// <summary>
        /// Создание графика распределения по форматам
        /// </summary>
        private void CreateFormatDistributionChart()
        {
            if (_compositions == null || _compositions.Count == 0) return;

            // Собираем статистику по форматам
            var formatStats = new Dictionary<int, MaterialFormatDistributionItem>();
            int totalStockAllFormats = 0;

            foreach (var composition in _compositions)
            {
                if (composition?.Layers == null) continue;

                foreach (var layer in composition.Layers)
                {
                    if (layer?.Widths == null) continue;

                    foreach (var width in layer.Widths)
                    {
                        if (!formatStats.ContainsKey(width.Width))
                        {
                            formatStats[width.Width] = new MaterialFormatDistributionItem
                            {
                                Width = width.Width,
                                TotalStockKg = 0,
                                CompositionCount = 0
                            };
                        }

                        formatStats[width.Width].TotalStockKg += width.StockKg;
                        formatStats[width.Width].CompositionCount++;
                        totalStockAllFormats += width.StockKg;
                    }
                }
            }

            if (totalStockAllFormats == 0) return;

            // Рассчитываем проценты
            foreach (var stat in formatStats.Values)
            {
                stat.Percentage = (double)stat.TotalStockKg / totalStockAllFormats * 100;
            }

            // Сортируем по убыванию общего остатка
            var sortedFormats = formatStats.Values
                .OrderByDescending(f => f.TotalStockKg)
                .Take(15)
                .ToList();

            // Контейнер для графика
            Border chartContainer = new Border
            {
                Style = (Style)FindResource("FormatChartContainerStyle")
            };

            StackPanel chartPanel = new StackPanel();

            // Заголовок графика
            Border headerBorder = new Border
            {
                Style = (Style)FindResource("SummaryHeaderStyle")
            };

            TextBlock titleText = new TextBlock
            {
                Text = "РАСПРЕДЕЛЕНИЕ ПО ФОРМАТАМ",
                Style = (Style)FindResource("SummaryTitleStyle")
            };

            headerBorder.Child = titleText;
            chartPanel.Children.Add(headerBorder);

            if (sortedFormats.Count > 0)
            {
                // Находим максимальное значение только среди форматов с ненулевым остатком
                double maxStock = sortedFormats
                    .Where(f => f.TotalStockKg > 0) // Исключаем нулевые значения
                    .Max(f => f.TotalStockKg);

                if (maxStock == 0) maxStock = 1; // Если все форматы нулевые

                // Заголовок сетки
                Grid headerGrid = new Grid
                {
                    Margin = new Thickness(0, 5, 0, 5)
                };

                headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(60) });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(70) });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(100) });

                // Заголовки колонок
                TextBlock widthHeader = new TextBlock
                {
                    Text = "Формат",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.DarkGray
                };
                Grid.SetColumn(widthHeader, 0);
                headerGrid.Children.Add(widthHeader);

                TextBlock chartHeader = new TextBlock
                {
                    Text = "Остаток",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.DarkGray,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetColumn(chartHeader, 1);
                headerGrid.Children.Add(chartHeader);

                TextBlock percentHeader = new TextBlock
                {
                    Text = "Доля",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.DarkGray
                };
                Grid.SetColumn(percentHeader, 2);
                headerGrid.Children.Add(percentHeader);

                TextBlock stockHeader = new TextBlock
                {
                    Text = "Кг",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.DarkGray,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                Grid.SetColumn(stockHeader, 3);
                headerGrid.Children.Add(stockHeader);

                chartPanel.Children.Add(headerGrid);

                foreach (var format in sortedFormats)
                {
                    Grid formatGrid = new Grid();

                    formatGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(60) });
                    formatGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                    formatGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(70) });
                    formatGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(100) });

                    // Ширина формата
                    TextBlock widthText = new TextBlock
                    {
                        Text = format.Width.ToString(),
                        FontSize = 11,
                        FontWeight = FontWeights.SemiBold,
                        VerticalAlignment = VerticalAlignment.Center,
                        Foreground = Brushes.Black
                    };
                    Grid.SetColumn(widthText, 0);
                    formatGrid.Children.Add(widthText);

                    // График - рассчитываем длину полоски пропорционально остатку
                    double barWidth = 0;
                    if (format.TotalStockKg > 0 && maxStock > 0)
                    {
                        barWidth = Math.Max(20, (format.TotalStockKg / maxStock) * 150); // Минимальная ширина 20
                    }
                    else
                    {
                        barWidth = 0; // Для нулевых остатков - пустая полоска
                    }

                    Border chartBar = new Border
                    {
                        Style = (Style)FindResource("FormatBarStyle"),
                        Width = barWidth,
                        Background = format.ChartColor,
                        ToolTip = $"{format.FormattedStock} кг в {format.CompositionCount} композициях",
                        HorizontalAlignment = HorizontalAlignment.Right
                    };
                    Grid.SetColumn(chartBar, 1);
                    formatGrid.Children.Add(chartBar);

                    // Процент
                    TextBlock percentText = new TextBlock
                    {
                        Text = format.FormattedPercentage,
                        FontSize = 11,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(5, 0, 0, 0),
                        Foreground = format.TotalStockKg > 0 ? Brushes.Black : Brushes.Gray // Серый для нулевых
                    };
                    Grid.SetColumn(percentText, 2);
                    formatGrid.Children.Add(percentText);

                    // Количество
                    TextBlock stockText = new TextBlock
                    {
                        Text = format.FormattedStock + " кг",
                        FontSize = 11,
                        FontWeight = format.TotalStockKg > 0 ? FontWeights.SemiBold : FontWeights.Normal,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Foreground = format.TotalStockKg > 0 ? Brushes.Black : Brushes.Gray // Серый для нулевых
                    };
                    Grid.SetColumn(stockText, 3);
                    formatGrid.Children.Add(stockText);

                    chartPanel.Children.Add(formatGrid);
                }

                // Итого
                TextBlock totalText = new TextBlock
                {
                    Text = $"Итого по всем форматам: {FormatNumberWithSpaces(totalStockAllFormats)} кг",
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.DarkSlateBlue,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 0)
                };
                chartPanel.Children.Add(totalText);
            }
            else
            {
                chartPanel.Children.Add(new TextBlock
                {
                    Text = "Нет данных по форматам",
                    FontSize = 11,
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 10)
                });
            }

            chartContainer.Child = chartPanel;
            SummaryPanel.Children.Add(chartContainer);
        }

        /// <summary>
        /// Форматирование числа с пробелами вместо запятых
        /// </summary>
        private string FormatNumberWithSpaces(int number)
        {
            return number.ToString("N0").Replace(",", " ");
        }

        /// <summary>
        /// Обработчик клика на композицию в сводке
        /// </summary>
        private void HandleCompositionSummaryItemClick(string categoryId, int compositionId)
        {
            _selectedCategory = categoryId;

            // Находим композицию по ID
            var targetComposition = _compositions.FirstOrDefault(c => c.Idc == compositionId);

            if (targetComposition == null) return;

            // Обновляем заголовок таблицы
            TableTitle.Text = GetCategoryTitle(categoryId);

            // Фильтруем композиции по категории
            var filteredCompositions = FilterCompositionsByCategory(categoryId);

            // Применяем дополнительные фильтры
            _filteredCompositions = ApplyAdditionalFilters(filteredCompositions);
            CompositionGrid.ItemsSource = _filteredCompositions;

            // Выбираем целевую композицию
            if (targetComposition != null && _filteredCompositions.Contains(targetComposition))
            {
                CompositionGrid.SelectedItem = targetComposition;
                DetailsCard.SetValue(targetComposition);
            }
            else if (_filteredCompositions.Count > 0)
            {
                CompositionGrid.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Получение заголовка категории
        /// </summary>
        private string GetCategoryTitle(string categoryId)
        {
            return categoryId switch
            {
                CATEGORY_CRITICAL => "Критические остатки (до 1 000 000 кг)",
                CATEGORY_LOW => "Низкие остатки (1 000 000 - 2 500 000 кг)",
                CATEGORY_HIGH => "Много в остатке (> 2 500 000 кг)",
                _ => "Все композиции"
            };
        }

        /// <summary>
        /// Фильтрация композиций по категории
        /// </summary>
        private List<MaterialDataComposition> FilterCompositionsByCategory(string categoryId)
        {
            if (_compositions == null) return new List<MaterialDataComposition>();

            return categoryId switch
            {
                CATEGORY_CRITICAL => _compositions
                    .Where(c => c.TotalStockKg <= CRITICAL_THRESHOLD)
                    .OrderBy(c => c.TotalStockKg)
                    .ToList(),

                CATEGORY_LOW => _compositions
                    .Where(c => c.TotalStockKg > CRITICAL_THRESHOLD && c.TotalStockKg <= LOW_THRESHOLD)
                    .OrderBy(c => c.TotalStockKg)
                    .ToList(),

                CATEGORY_HIGH => _compositions
                    .Where(c => c.TotalStockKg > LOW_THRESHOLD)
                    .OrderBy(c => c.TotalStockKg) 
                    .ToList(),

                _ => _compositions
            };
        }

        /// <summary>
        /// Расчет глобальных метрик
        /// </summary>
        private void CalculateMetrics()
        {
            _totalCompositions = _compositions?.Count ?? 0;
            _criticalRemainsCount = 0;
            _lowRemainsCount = 0;
            _highRemainsCount = 0;

            if (_compositions != null)
            {
                foreach (var composition in _compositions)
                {
                    if (composition == null) continue;

                    if (composition.TotalStockKg <= CRITICAL_THRESHOLD)
                        _criticalRemainsCount++;
                    else if (composition.TotalStockKg <= LOW_THRESHOLD)
                        _lowRemainsCount++;
                    else
                        _highRemainsCount++;
                }
            }
        }

        /// <summary>
        /// Обновление отображения метрик
        /// </summary>
        private void UpdateMetricsDisplay()
        {
            if (TotalCompositionsMetric != null)
            {
                TotalCompositionsMetric.Text = $"Всего: {_totalCompositions}";
                CriticalRemainsMetric.Text = $"Критич: {_criticalRemainsCount}";
                LowRemainsMetric.Text = $"Низкий: {_lowRemainsCount}";
                HighRemainsMetric.Text = $"Много: {_highRemainsCount}";
            }
        }

        /// <summary>
        /// Применение фильтров
        /// </summary>
        private void ApplyFilters()
        {
            if (_compositions == null) return;

            // Если выбрана категория в сводках, фильтруем по ней
            if (_selectedCategory != null)
            {
                var filteredCompositions = FilterCompositionsByCategory(_selectedCategory);
                _filteredCompositions = ApplyAdditionalFilters(filteredCompositions);
            }
            else
            {
                // Если нет выбранной категории, показываем все с применением фильтров
                _filteredCompositions = ApplyAdditionalFilters(_compositions);
            }

            CompositionGrid.ItemsSource = _filteredCompositions;

            // Автоматически выбираем первую композицию, если есть
            if (_filteredCompositions.Count > 0)
            {
                CompositionGrid.SelectedIndex = 0;
            }
            else
            {
                DetailsCard.SetValue(null);
            }
        }

        /// <summary>
        /// Применение дополнительных фильтров (только поиск)
        /// </summary>
        private List<MaterialDataComposition> ApplyAdditionalFilters(List<MaterialDataComposition> sourceList)
        {
            if (sourceList == null) return new List<MaterialDataComposition>();

            var filtered = new List<MaterialDataComposition>(sourceList);

            // Фильтр по поиску
            string searchText = SearchTextBox?.Text ?? "";
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                filtered = filtered
                    .Where(c => c.CartonName != null &&
                               c.CartonName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            return filtered;
        }

        /// <summary>
        /// Кнопка "Загрузить все"
        /// </summary>
        private void LoadAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (_compositions == null) return;

            _selectedCategory = null;
            TableTitle.Text = "Все композиции";

            // Применяем фильтры ко всем данным
            _filteredCompositions = ApplyAdditionalFilters(_compositions);
            CompositionGrid.ItemsSource = _filteredCompositions;

            if (_filteredCompositions.Count > 0)
            {
                CompositionGrid.SelectedIndex = 0;
            }
            else
            {
                DetailsCard.SetValue(null);
            }
        }

        // Обработчики событий
        private void CompositionGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = CompositionGrid.SelectedItem as MaterialDataComposition;
            if (selected != null)
            {
                DetailsCard.SetValue(selected);
            }
            else
            {
                DetailsCard.SetValue(null);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void PlatformSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RefreshCompositionData();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshCompositionData();
        }
    }
}




