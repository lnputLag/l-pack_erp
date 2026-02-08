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
    /// Остаток по сырьевым композициям на складе
    /// в карточном виде с пагинацией и фильтрацией
    /// </summary>
    /// <author>kurasov_dp</author>
    //public partial class RawCompositionMaterialMonitorCardsTab : ControlBase
    //{
    //    private List<MaterialDataComposition> _compositions;
    //    private List<MaterialDataComposition> _filteredCompositions;

    //    // Поле для хранения выбранного формата
    //    private int _selectedWidthFilter = 0;

    //    // Добавляем счетчики для метрик
    //    private int _totalCompositions = 0;
    //    private int _lowRemainsCount = 0;
    //    private int _criticalRemainsCount = 0;
    //    private int _highRemainsCount = 0;

    //    // Текущая выбранная категория
    //    private string _selectedCategory = null;
    //    private MaterialDataComposition _selectedComposition = null;

    //    // Пороги для классификации остатков
    //    public const int LOW_REMAINS_THRESHOLD = 50000;    // Меньше 50 кг
    //    public const int CRITICAL_THRESHOLD = 10000;       // Меньше 10 кг
    //    public const int HIGH_REMAINS_THRESHOLD = 100000;  // Больше 100 кг

    //    // Константы для категорий
    //    private const string CATEGORY_CRITICAL = "critical";
    //    private const string CATEGORY_LOW = "low";
    //    private const string CATEGORY_HIGH = "high";
    //    private const string CATEGORY_ZERO = "zero";

    //    public RawCompositionMaterialMonitorCardsTab()
    //    {
    //        InitializeComponent();
    //        RoleName = "[erp]raw_material_monitor";
    //        ControlTitle = "Монитор остатков сырья";
    //        DocumentationUrl = "/doc/l-pack-erp";

    //        OnMessage = (ItemMessage m) =>
    //        {
    //            if (m.ReceiverName == ControlName)
    //            {
    //                Commander.ProcessCommand(m.Action, m);
    //            }
    //        };

    //        OnKeyPressed = (System.Windows.Input.KeyEventArgs e) =>
    //        {
    //            if (!e.Handled)
    //            {
    //                Commander.ProcessKeyboard(e);
    //            }
    //        };

    //        OnLoad = () =>
    //        {

    //            SetDefaults();
    //        };

    //        OnUnload = () =>
    //        {

    //        };

    //        OnFocusGot = () =>
    //        {

    //        };

    //        OnFocusLost = () =>
    //        {

    //        };

    //        ///<summary>
    //        /// Система команд (Commander)
    //        ///</summary>
    //        {
    //            Commander.SetCurrentGroup("main");
    //            {
    //                Commander.Add(new CommandItem()
    //                {
    //                    Name = "help",
    //                    Enabled = true,
    //                    Title = "Справка",
    //                    Description = "Показать справочную информацию",
    //                    MenuUse = true,
    //                    ButtonUse = true,
    //                    ButtonName = "HelpButton",
    //                    HotKey = "F1",
    //                    Action = () =>
    //                    {
    //                        Central.ShowHelp(DocumentationUrl);
    //                    },
    //                });
    //                Commander.Add(new CommandItem()
    //                {
    //                    Name = "loadAll",
    //                    Enabled = true,
    //                    Title = "Загрузить все",
    //                    Description = "Загрузить все композиции",
    //                    MenuUse = true,
    //                    ButtonUse = true,
    //                    ButtonName = "LoadAllButton",
    //                    Action = () => LoadAllButton_Click(null, null),
    //                });
    //            }
    //        }
    //        Commander.Init(this);

    //    }

    //    /// <summary>
    //    /// Загрузка данных (из БД)
    //    /// В модель карточек
    //    /// </summary>
    //    private List<MaterialDataComposition> LoadCompositionsData()
    //    {
    //        var compositions = new List<MaterialDataComposition>();

    //        var p = new Dictionary<string, string>();

    //        // Выбор из выпадающего списка
    //        var selectedPlatform = PlatformSelectBox.SelectedItem;
    //        if (!selectedPlatform.Equals(default(KeyValuePair<string, string>)))
    //        {
    //            p.Add("FACTORY_ID", selectedPlatform.Key);
    //        }
    //        else
    //        {
    //            p.Add("FACTORY_ID", "1"); // Значение по умолчанию
    //        }

    //        var q = new LPackClientQuery();
    //        q.Request.SetParam("Module", "Stock");
    //        q.Request.SetParam("Object", "RawMaterialResidueMonitor");
    //        q.Request.SetParam("Action", "RawCompositionList");

    //        q.Request.SetParams(p);
    //        q.Request.Timeout = 80000;
    //        q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

    //        q.DoQuery();

    //        if (q.Answer.Status == 0)
    //        {
    //            var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
    //            if (result != null)
    //            {
    //                var ds = ListDataSet.Create(result, "ITEMS");
    //                foreach (var item in ds.Items)
    //                {
    //                    int width = item.CheckGet("WIDTH").ToInt();

    //                    // ФИЛЬТРАЦИЯ: ИСКЛЮЧАЕМ WIDTH = 1600
    //                    if (width == 1600)
    //                    {
    //                        continue; // Пропускаем эту запись
    //                    }

    //                    int idc = item.CheckGet("IDC").ToInt();

    //                    // Поиск существующей композиции по IDC
    //                    if (compositions.Count(x => x.Idc == idc) > 0)
    //                    {
    //                        var comp = compositions.FirstOrDefault(x => x.Idc == idc);

    //                        string layerNumber = item.CheckGet("LAYER_NUMBER").ToString();
    //                        string rawGroup = item.CheckGet("RAW_GROUP").ToString();

    //                        // Поиск существующего слоя в композиции
    //                        if (comp.Layers.Count(x => x.LayerNumber == layerNumber && x.RawGroup == rawGroup) > 0)
    //                        {
    //                            var layer = comp.Layers.FirstOrDefault(x => x.LayerNumber == layerNumber && x.RawGroup == rawGroup);
    //                            layer.Widths.Add(new MaterialWidthData()
    //                            {
    //                                Width = item.CheckGet("WIDTH").ToInt(),
    //                                StockKg = item.CheckGet("STOCK_KG").ToInt()
    //                            });
    //                        }
    //                        else
    //                        {
    //                            // Создание нового слоя
    //                            var newLayer = new MaterialLayerData();
    //                            newLayer.LayerNumber = layerNumber;
    //                            newLayer.RawGroup = rawGroup;
    //                            newLayer.Widths.Add(new MaterialWidthData()
    //                            {
    //                                Width = item.CheckGet("WIDTH").ToInt(),
    //                                StockKg = item.CheckGet("STOCK_KG").ToInt()
    //                            });
    //                            comp.Layers.Add(newLayer);
    //                        }
    //                    }
    //                    else
    //                    {
    //                        // Создание новой композиции
    //                        var newComp = new MaterialDataComposition();
    //                        newComp.Idc = idc;
    //                        newComp.CartonName = item.CheckGet("CARTON_NAME").ToString();

    //                        // Создание первого слоя
    //                        var newLayer = new MaterialLayerData();
    //                        newLayer.LayerNumber = item.CheckGet("LAYER_NUMBER").ToString();
    //                        newLayer.RawGroup = item.CheckGet("RAW_GROUP").ToString();
    //                        newLayer.Widths.Add(new MaterialWidthData()
    //                        {
    //                            Width = item.CheckGet("WIDTH").ToInt(),
    //                            StockKg = item.CheckGet("STOCK_KG").ToInt()
    //                        });

    //                        newComp.Layers.Add(newLayer);
    //                        compositions.Add(newComp);
    //                    }
    //                }

    //                // Сортировка по ширине внутри каждого слоя
    //                foreach (var comp in compositions)
    //                {
    //                    foreach (var layer in comp.Layers)
    //                    {
    //                        layer.Widths = layer.Widths.OrderBy(w => w.Width).ToList();
    //                    }
    //                }
    //            }
    //        }
    //        else
    //        {
    //            q.ProcessError();
    //        }

    //        return compositions;
    //    }

    //    public void SetDefaults()
    //    {
    //        PlatformSelectBox.SetItems(new Dictionary<string, string>()
    //        {
    //            {"1",  "Липецк"},
    //            {"2",  "Кашира"},
    //        });
    //        PlatformSelectBox.SelectedItem = PlatformSelectBox.Items.First();

    //    }

    //    private void RefreshCompositionData()
    //    {
    //        _compositions = LoadCompositionsData();
    //        CalculateMetrics(); // Рассчитываем метрики
    //        UpdateMetricsDisplay(); // Обновляем отображение
    //        UpdateWidthFilterList(); // Обновляем список форматов
    //        UpdateSummaryPanels(); // Новый метод для обновления сводок
    //        ClearZone2AndZone3(); // Очищаем зоны 2 и 3 при обновлении данных
    //        //ApplyFilters(); // Применяем текущие фильтры
    //    }

    //    /// <summary>
    //    /// Метод для заполнения списка форматов
    //    /// </summary>
    //    private void UpdateWidthFilterList()
    //    {
    //        if (_compositions == null) return;

    //        var allWidths = new HashSet<int>();

    //        foreach (var composition in _compositions)
    //        {
    //            if (composition?.Layers == null) continue;

    //            foreach (var layer in composition.Layers)
    //            {
    //                if (layer?.Widths == null) continue;

    //                foreach (var widthData in layer.Widths)
    //                {
    //                    // Исключаем ширину 1600
    //                    if (widthData.Width != 1600)
    //                    {
    //                        allWidths.Add(widthData.Width);
    //                    }
    //                }
    //            }
    //        }

    //        // Сортируем форматы
    //        var sortedWidths = allWidths.OrderBy(w => w).ToList();

    //        // Добавляем "Все форматы" первым элементом
    //        var widthItems = new List<string> { "Все форматы" };
    //        widthItems.AddRange(sortedWidths.Select(w => w.ToString()));

    //        WidthFilterComboBox.ItemsSource = widthItems;
    //        WidthFilterComboBox.SelectedIndex = 0; // Выбираем "Все форматы"
    //    }

    //    /// <summary>
    //    /// Очистка зон 2 и 3
    //    /// </summary>
    //    private void ClearZone2AndZone3()
    //    {
    //        CompositionGrid.ItemsSource = null;
    //        TableTitle.Text = "Выберите категорию";
    //        DetailsCard.SetValue(null);
    //        _selectedCategory = null;
    //        _selectedComposition = null;
    //    }

    //    /// <summary>
    //    /// Создание сводок для Зоны 1 (на основе форматов)
    //    /// </summary>
    //    private void UpdateSummaryPanels()
    //    {
    //        SummaryPanel.Children.Clear();

    //        if (_compositions == null || _compositions.Count == 0)
    //        {
    //            SummaryPanel.Children.Add(new TextBlock
    //            {
    //                Text = "Нет данных для отображения",
    //                Foreground = Brushes.Gray,
    //                FontStyle = FontStyles.Italic,
    //                TextAlignment = TextAlignment.Center,
    //                Margin = new Thickness(0, 20, 0, 0)
    //            });
    //            return;
    //        }

    //        // Анализируем все композиции и группируем их по категориям
    //        var compositionAnalysis = AnalyzeCompositionsByWidths();

    //        // 1. Нулевые форматы
    //        var zeroCompositions = compositionAnalysis
    //            .Where(c => c.Category == "zero" && c.ProblemWidthsCount > 0)
    //            .OrderByDescending(c => c.ProblemWidthsCount)
    //            .Take(10)
    //            .ToList();
    //        CreateCompositionSummaryCategory("❌ КОМПОЗИЦИИ С НУЛЕВЫМИ ФОРМАТАМИ", zeroCompositions, "zero");

    //        // 2. Критические форматы
    //        var criticalCompositions = compositionAnalysis
    //            .Where(c => c.Category == "critical" && c.ProblemWidthsCount > 0)
    //            .OrderByDescending(c => c.ProblemWidthsCount)
    //            .Take(10)
    //            .ToList();
    //        CreateCompositionSummaryCategory("🔴 КОМПОЗИЦИИ С КРИТИЧЕСКИМИ ФОРМАТАМИ", criticalCompositions, "critical");

    //        // 3. Низкие форматы
    //        var lowCompositions = compositionAnalysis
    //            .Where(c => c.Category == "low" && c.ProblemWidthsCount > 0)
    //            .OrderByDescending(c => c.ProblemWidthsCount)
    //            .Take(10)
    //            .ToList();
    //        CreateCompositionSummaryCategory("🟠 КОМПОЗИЦИИ С НИЗКИМИ ФОРМАТАМИ", lowCompositions, "low");

    //        // 4. Большие форматы
    //        var highCompositions = compositionAnalysis
    //            .Where(c => c.Category == "high" && c.ProblemWidthsCount > 0)
    //            .OrderByDescending(c => c.ProblemWidthsCount)
    //            .Take(10)
    //            .ToList();
    //        CreateCompositionSummaryCategory("🟢 КОМПОЗИЦИИ С БОЛЬШИМИ ФОРМАТАМИ", highCompositions, "high");
    //    }

    //    ///<summary>
    //    ///
    //    ///</summary>
    //    private void CreateCompositionSummaryCategory(string title, List<MaterialCompositionSummaryItem> items, string categoryId)
    //    {
    //        Border categoryContainer = new Border
    //        {
    //            Style = (Style)FindResource("SummaryCategoryStyle")
    //        };

    //        StackPanel categoryPanel = new StackPanel();

    //        // Заголовок категории
    //        Border headerBorder = new Border
    //        {
    //            Style = (Style)FindResource("SummaryHeaderStyle")
    //        };

    //        TextBlock titleText = new TextBlock
    //        {
    //            Text = $"{title} (Всего: {items.Count})",
    //            Style = (Style)FindResource("SummaryTitleStyle")
    //        };

    //        headerBorder.Child = titleText;
    //        categoryPanel.Children.Add(headerBorder);

    //        if (items.Count > 0)
    //        {
    //            // Находим максимальное количество проблемных форматов для шкалы
    //            int maxProblemCount = items.Max(c => c.ProblemWidthsCount);
    //            if (maxProblemCount == 0) maxProblemCount = 1;

    //            foreach (var item in items)
    //            {
    //                Border itemBorder = new Border
    //                {
    //                    Style = (Style)FindResource("SummaryItemStyle"),
    //                    Tag = new { Category = categoryId, CompositionId = item.Idc }
    //                };

    //                itemBorder.MouseLeftButtonDown += (s, e) =>
    //                {
    //                    HandleCompositionSummaryItemClick(categoryId, item.Idc);
    //                };

    //                Grid itemGrid = new Grid();
    //                itemGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
    //                itemGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
    //                itemGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

    //                // Название композиции
    //                StackPanel infoPanel = new StackPanel
    //                {
    //                    Orientation = Orientation.Vertical
    //                };

    //                TextBlock nameText = new TextBlock
    //                {
    //                    Text = item.CartonName,
    //                    FontSize = 11,
    //                    FontWeight = FontWeights.SemiBold,
    //                    TextTrimming = TextTrimming.CharacterEllipsis
    //                };

    //                TextBlock detailsText = new TextBlock
    //                {
    //                    Text = $"Всего: {item.TotalStockKg:N0} кг | Проблемных форматов: {item.ProblemWidthsCount}",
    //                    FontSize = 9,
    //                    Foreground = Brushes.Gray,
    //                    TextTrimming = TextTrimming.CharacterEllipsis
    //                };

    //                infoPanel.Children.Add(nameText);
    //                infoPanel.Children.Add(detailsText);

    //                Grid.SetColumn(infoPanel, 0);
    //                itemGrid.Children.Add(infoPanel);

    //                // Полоска-индикатор (показывает количество проблемных форматов)
    //                Border barChart = new Border
    //                {
    //                    Style = (Style)FindResource("BarChartStyle"),
    //                    Width = Math.Max(30, (item.ProblemWidthsCount / (double)maxProblemCount) * 120),
    //                    Background = item.CategoryColor,
    //                    ToolTip = $"{item.ProblemWidthsCount} проблемных форматов"
    //                };
    //                Grid.SetColumn(barChart, 1);
    //                itemGrid.Children.Add(barChart);

    //                // Общий остаток по композиции
    //                TextBlock valueText = new TextBlock
    //                {
    //                    Text = $"{item.TotalStockKg:N0} кг",
    //                    FontSize = 10,
    //                    FontWeight = FontWeights.SemiBold,
    //                    Foreground = item.CategoryColor,
    //                    VerticalAlignment = VerticalAlignment.Center,
    //                    MinWidth = 60,
    //                    TextAlignment = TextAlignment.Right
    //                };
    //                Grid.SetColumn(valueText, 2);
    //                itemGrid.Children.Add(valueText);

    //                itemBorder.Child = itemGrid;
    //                categoryPanel.Children.Add(itemBorder);
    //            }
    //        }
    //        else
    //        {
    //            categoryPanel.Children.Add(new TextBlock
    //            {
    //                Text = "Нет композиций",
    //                FontSize = 11,
    //                Foreground = Brushes.Gray,
    //                FontStyle = FontStyles.Italic,
    //                HorizontalAlignment = HorizontalAlignment.Center,
    //                Margin = new Thickness(0, 10, 0, 10)
    //            });
    //        }

    //        categoryContainer.Child = categoryPanel;
    //        SummaryPanel.Children.Add(categoryContainer);
    //    }

    //    private void HandleCompositionSummaryItemClick(string categoryId, int compositionId)
    //    {
    //        _selectedCategory = categoryId;

    //        // Находим композицию по ID
    //        var targetComposition = _compositions.FirstOrDefault(c => c.Idc == compositionId);

    //        if (targetComposition == null) return;

    //        // Обновляем заголовок таблицы
    //        TableTitle.Text = GetCompositionCategoryTitle(categoryId);

    //        // Фильтруем композиции, которые имеют форматы выбранной категории
    //        List<MaterialDataComposition> filteredCompositions = new List<MaterialDataComposition>();

    //        if (_compositions != null)
    //        {
    //            foreach (var composition in _compositions)
    //            {
    //                bool hasMatchingWidths = false;

    //                switch (categoryId)
    //                {
    //                    case "zero":
    //                        hasMatchingWidths = composition.Layers
    //                            .Any(l => l.Widths.Any(w => w.StockKg == 0));
    //                        break;

    //                    case "critical":
    //                        hasMatchingWidths = composition.Layers
    //                            .Any(l => l.Widths.Any(w => w.StockKg > 0 && w.StockKg <= 10000));
    //                        break;

    //                    case "low":
    //                        hasMatchingWidths = composition.Layers
    //                            .Any(l => l.Widths.Any(w => w.StockKg > 10000 && w.StockKg <= 50000));
    //                        break;

    //                    case "high":
    //                        hasMatchingWidths = composition.Layers
    //                            .Any(l => l.Widths.Any(w => w.StockKg > 50000));
    //                        break;
    //                }

    //                if (hasMatchingWidths)
    //                {
    //                    filteredCompositions.Add(composition);
    //                }
    //            }
    //        }

    //        // Применяем дополнительные фильтры
    //        _filteredCompositions = ApplyAdditionalFilters(filteredCompositions);
    //        CompositionGrid.ItemsSource = _filteredCompositions;

    //        // Выбираем целевую композицию
    //        if (targetComposition != null)
    //        {
    //            CompositionGrid.SelectedItem = targetComposition;
    //            DetailsCard.SetValue(targetComposition);
    //        }
    //        else if (_filteredCompositions.Count > 0)
    //        {
    //            CompositionGrid.SelectedIndex = 0;
    //        }
    //    }


    //    /// <summary>
    //    /// Метод анализа композиций
    //    /// </summary>
    //    private List<MaterialCompositionSummaryItem> AnalyzeCompositionsByWidths()
    //    {
    //        var result = new List<MaterialCompositionSummaryItem>();

    //        if (_compositions == null) return result;

    //        foreach (var composition in _compositions)
    //        {
    //            if (composition == null) continue;

    //            // Считаем проблемные форматы в каждой категории
    //            int zeroCount = 0;
    //            int criticalCount = 0;
    //            int lowCount = 0;
    //            int highCount = 0;

    //            foreach (var layer in composition.Layers)
    //            {
    //                if (layer?.Widths == null) continue;

    //                foreach (var width in layer.Widths)
    //                {
    //                    if (width.Width == 1600) continue; // Пропускаем ширину 1600

    //                    if (width.StockKg == 0)
    //                        zeroCount++;
    //                    else if (width.StockKg <= 10000)
    //                        criticalCount++;
    //                    else if (width.StockKg <= 50000)
    //                        lowCount++;
    //                    else
    //                        highCount++;
    //                }
    //            }

    //            // Определяем основную категорию композиции (с максимальным количеством проблем)
    //            string mainCategory = "high";
    //            int maxCount = highCount;

    //            if (zeroCount > maxCount) { mainCategory = "zero"; maxCount = zeroCount; }
    //            if (criticalCount > maxCount) { mainCategory = "critical"; maxCount = criticalCount; }
    //            if (lowCount > maxCount) { mainCategory = "low"; maxCount = lowCount; }

    //            result.Add(new MaterialCompositionSummaryItem
    //            {
    //                CartonName = composition.CartonName,
    //                Idc = composition.Idc,
    //                TotalStockKg = composition.TotalStockKg,
    //                Category = mainCategory,
    //                ProblemWidthsCount = maxCount
    //            });
    //        }

    //        return result;
    //    }

    //    /// <summary>
    //    /// Создание сводки для категории форматов
    //    /// </summary>
    //    private void CreateWidthSummaryCategory(string title, List<MaterialWidthAnalysisItem> items, string categoryId)
    //    {
    //        // Контейнер категории
    //        Border categoryContainer = new Border
    //        {
    //            Style = (Style)FindResource("SummaryCategoryStyle")
    //        };

    //        StackPanel categoryPanel = new StackPanel();

    //        // Заголовок категории
    //        Border headerBorder = new Border
    //        {
    //            Style = (Style)FindResource("SummaryHeaderStyle")
    //        };

    //        TextBlock titleText = new TextBlock
    //        {
    //            Text = $"{title} (Всего: {items.Count})",
    //            Style = (Style)FindResource("SummaryTitleStyle")
    //        };

    //        headerBorder.Child = titleText;
    //        categoryPanel.Children.Add(headerBorder);

    //        // Элементы списка
    //        if (items.Count > 0)
    //        {
    //            // Находим максимальное значение для шкалы
    //            int maxValue = items.Max(c => c.StockKg);
    //            if (maxValue == 0) maxValue = 1; // Защита от деления на 0

    //            foreach (var item in items)
    //            {
    //                Border itemBorder = new Border
    //                {
    //                    Style = (Style)FindResource("SummaryItemStyle"),
    //                    Tag = new { Category = categoryId, WidthItem = item }
    //                };

    //                itemBorder.MouseLeftButtonDown += (s, e) =>
    //                {
    //                    HandleWidthSummaryItemClick(categoryId, item);
    //                };

    //                Grid itemGrid = new Grid();
    //                itemGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
    //                itemGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
    //                itemGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

    //                // Название композиции + формат + слой
    //                StackPanel infoPanel = new StackPanel
    //                {
    //                    Orientation = Orientation.Vertical
    //                };

    //                TextBlock nameText = new TextBlock
    //                {
    //                    Text = $"{item.CartonName}",
    //                    FontSize = 10,
    //                    FontWeight = FontWeights.SemiBold,
    //                    TextTrimming = TextTrimming.CharacterEllipsis
    //                };

    //                TextBlock detailsText = new TextBlock
    //                {
    //                    Text = $"Формат: {item.Width} | Слой: {item.LayerNumber} | {item.RawGroup}",
    //                    FontSize = 9,
    //                    Foreground = Brushes.Gray,
    //                    TextTrimming = TextTrimming.CharacterEllipsis
    //                };

    //                infoPanel.Children.Add(nameText);
    //                infoPanel.Children.Add(detailsText);

    //                Grid.SetColumn(infoPanel, 0);
    //                itemGrid.Children.Add(infoPanel);

    //                // Полоска-индикатор (барчарт)
    //                Border barChart = new Border
    //                {
    //                    Style = (Style)FindResource("BarChartStyle"),
    //                    Width = Math.Max(30, (item.StockKg / (double)maxValue) * 120),
    //                    Background = item.CategoryColor,
    //                    ToolTip = $"{item.StockKg:N0} кг"
    //                };
    //                Grid.SetColumn(barChart, 1);
    //                itemGrid.Children.Add(barChart);

    //                // Количество
    //                TextBlock valueText = new TextBlock
    //                {
    //                    Text = $"{item.StockKg:N0} кг",
    //                    FontSize = 10,
    //                    FontWeight = FontWeights.SemiBold,
    //                    Foreground = item.CategoryColor,
    //                    VerticalAlignment = VerticalAlignment.Center,
    //                    MinWidth = 60,
    //                    TextAlignment = TextAlignment.Right
    //                };
    //                Grid.SetColumn(valueText, 2);
    //                itemGrid.Children.Add(valueText);

    //                itemBorder.Child = itemGrid;
    //                categoryPanel.Children.Add(itemBorder);
    //            }
    //        }
    //        else
    //        {
    //            categoryPanel.Children.Add(new TextBlock
    //            {
    //                Text = "Нет позиций",
    //                FontSize = 11,
    //                Foreground = Brushes.Gray,
    //                FontStyle = FontStyles.Italic,
    //                HorizontalAlignment = HorizontalAlignment.Center,
    //                Margin = new Thickness(0, 10, 0, 10)
    //            });
    //        }

    //        categoryContainer.Child = categoryPanel;
    //        SummaryPanel.Children.Add(categoryContainer);
    //    }

    //    /// <summary>
    //    /// Обработка клика на элемент сводки форматов
    //    /// </summary>
    //    private void HandleWidthSummaryItemClick(string categoryId, MaterialWidthAnalysisItem widthItem)
    //    {
    //        _selectedCategory = categoryId;

    //        // Обновляем заголовок таблицы
    //        TableTitle.Text = GetWidthCategoryTitle(categoryId);

    //        // Фильтруем композиции, которые имеют форматы выбранной категории
    //        List<MaterialDataComposition> filteredCompositions = new List<MaterialDataComposition>();

    //        if (_compositions != null)
    //        {
    //            foreach (var composition in _compositions)
    //            {
    //                // Проверяем, есть ли в композиции форматы выбранной категории
    //                bool hasMatchingWidths = false;

    //                switch (categoryId)
    //                {
    //                    case "zero":
    //                        hasMatchingWidths = composition.Layers
    //                            .Any(l => l.Widths.Any(w => w.StockKg == 0));
    //                        break;

    //                    case "critical":
    //                        hasMatchingWidths = composition.Layers
    //                            .Any(l => l.Widths.Any(w => w.StockKg > 0 && w.StockKg <= 10000));
    //                        break;

    //                    case "low":
    //                        hasMatchingWidths = composition.Layers
    //                            .Any(l => l.Widths.Any(w => w.StockKg > 10000 && w.StockKg <= 50000));
    //                        break;

    //                    case "high":
    //                        hasMatchingWidths = composition.Layers
    //                            .Any(l => l.Widths.Any(w => w.StockKg > 50000));
    //                        break;
    //                }

    //                if (hasMatchingWidths)
    //                {
    //                    filteredCompositions.Add(composition);
    //                }
    //            }
    //        }

    //        // Применяем дополнительные фильтры
    //        _filteredCompositions = ApplyAdditionalFilters(filteredCompositions);
    //        CompositionGrid.ItemsSource = _filteredCompositions;

    //        // Находим и выбираем композицию, в которой находится выбранный формат
    //        var targetComposition = _filteredCompositions
    //            .FirstOrDefault(c => c.Idc == widthItem.Idc);

    //        if (targetComposition != null)
    //        {
    //            CompositionGrid.SelectedItem = targetComposition;
    //            DetailsCard.SetValue(targetComposition);
    //        }
    //        else if (_filteredCompositions.Count > 0)
    //        {
    //            CompositionGrid.SelectedIndex = 0;
    //        }
    //    }

    //    /// <summary>
    //    /// Получение заголовка категории для форматов
    //    /// </summary>
    //    private string GetWidthCategoryTitle(string categoryId)
    //    {
    //        return categoryId switch
    //        {
    //            "zero" => "Композиции с нулевыми остатками по форматам",
    //            "critical" => "Композиции с критическими остатками по форматам (1-10 000 кг)",
    //            "low" => "Композиции с низкими остатками по форматам (10 001-50 000 кг)",
    //            "high" => "Композиции с большими остатками по форматам (>50 000 кг)",
    //            _ => "Все композиции"
    //        };
    //    }

    //    /// <summary>
    //    /// Создание одной категории сводки
    //    /// </summary>
    //    private void CreateSummaryCategory(string title, List<MaterialDataComposition> items, string categoryId, Color color)
    //    {
    //        // Контейнер категории
    //        Border categoryContainer = new Border
    //        {
    //            Style = (Style)FindResource("SummaryCategoryStyle")
    //        };

    //        StackPanel categoryPanel = new StackPanel();

    //        // Заголовок категории
    //        Border headerBorder = new Border
    //        {
    //            Style = (Style)FindResource("SummaryHeaderStyle")
    //        };

    //        TextBlock titleText = new TextBlock
    //        {
    //            Text = $"{title} (Всего: {items.Count})",
    //            Style = (Style)FindResource("SummaryTitleStyle")
    //        };

    //        headerBorder.Child = titleText;
    //        categoryPanel.Children.Add(headerBorder);

    //        // Элементы списка
    //        if (items.Count > 0)
    //        {
    //            // Находим максимальное значение для шкалы
    //            int maxValue = items.Max(c => c.TotalStockKg);
    //            if (maxValue == 0) maxValue = 1; // Защита от деления на 0

    //            foreach (var item in items)
    //            {
    //                Border itemBorder = new Border
    //                {
    //                    Style = (Style)FindResource("SummaryItemStyle"),
    //                    Tag = new { Category = categoryId, Composition = item }
    //                };

    //                itemBorder.MouseLeftButtonDown += (s, e) =>
    //                {
    //                    HandleSummaryItemClick(categoryId, item);
    //                };

    //                Grid itemGrid = new Grid();
    //                itemGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
    //                itemGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
    //                itemGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

    //                // Название композиции
    //                TextBlock nameText = new TextBlock
    //                {
    //                    Text = item.CartonName,
    //                    FontSize = 11,
    //                    TextTrimming = TextTrimming.CharacterEllipsis,
    //                    VerticalAlignment = VerticalAlignment.Center
    //                };
    //                Grid.SetColumn(nameText, 0);

    //                // Полоска-индикатор (барчарт)
    //                Border barChart = new Border
    //                {
    //                    Style = (Style)FindResource("BarChartStyle"),
    //                    Width = Math.Max(50, (item.TotalStockKg / (double)maxValue) * 150),
    //                    Background = new SolidColorBrush(color)
    //                };
    //                Grid.SetColumn(barChart, 1);

    //                // Количество
    //                TextBlock valueText = new TextBlock
    //                {
    //                    Text = $"{item.TotalStockKg:N0} кг",
    //                    FontSize = 11,
    //                    FontWeight = FontWeights.SemiBold,
    //                    Foreground = new SolidColorBrush(color),
    //                    VerticalAlignment = VerticalAlignment.Center,
    //                    MinWidth = 60,
    //                    TextAlignment = TextAlignment.Right
    //                };
    //                Grid.SetColumn(valueText, 2);

    //                itemGrid.Children.Add(nameText);
    //                itemGrid.Children.Add(barChart);
    //                itemGrid.Children.Add(valueText);
    //                itemBorder.Child = itemGrid;
    //                categoryPanel.Children.Add(itemBorder);
    //            }
    //        }
    //        else
    //        {
    //            categoryPanel.Children.Add(new TextBlock
    //            {
    //                Text = "Нет позиций",
    //                FontSize = 11,
    //                Foreground = Brushes.Gray,
    //                FontStyle = FontStyles.Italic,
    //                HorizontalAlignment = HorizontalAlignment.Center,
    //                Margin = new Thickness(0, 10, 0, 10)
    //            });
    //        }

    //        categoryContainer.Child = categoryPanel;
    //        SummaryPanel.Children.Add(categoryContainer);
    //    }

    //    /// <summary>
    //    /// Обработка клика на элемент сводки
    //    /// </summary>
    //    private void HandleSummaryItemClick(string categoryId, MaterialDataComposition composition)
    //    {
    //        _selectedCategory = categoryId;
    //        _selectedComposition = composition;

    //        // Обновляем заголовок таблицы
    //        TableTitle.Text = GetCategoryTitle(categoryId);

    //        // Фильтруем и отображаем данные в таблице
    //        List<MaterialDataComposition> filteredItems;

    //        switch (categoryId)
    //        {
    //            case CATEGORY_ZERO:
    //                filteredItems = _compositions
    //                    .Where(c => c.TotalStockKg == 0)
    //                    .OrderBy(c => c.CartonName)
    //                    .ToList();
    //                break;

    //            case CATEGORY_CRITICAL:
    //                filteredItems = _compositions
    //                    .Where(c => c.TotalStockKg > 0 && c.TotalStockKg <= CRITICAL_THRESHOLD)
    //                    .OrderBy(c => c.TotalStockKg)
    //                    .ToList();
    //                break;

    //            case CATEGORY_LOW:
    //                filteredItems = _compositions
    //                    .Where(c => c.TotalStockKg > CRITICAL_THRESHOLD && c.TotalStockKg <= LOW_REMAINS_THRESHOLD)
    //                    .OrderBy(c => c.TotalStockKg)
    //                    .ToList();
    //                break;

    //            case CATEGORY_HIGH:
    //                filteredItems = _compositions
    //                    .Where(c => c.TotalStockKg > LOW_REMAINS_THRESHOLD)
    //                    .OrderByDescending(c => c.TotalStockKg)
    //                    .ToList();
    //                break;

    //            default:
    //                filteredItems = new List<MaterialDataComposition>();
    //                break;
    //        }

    //        // Применяем дополнительные фильтры (поиск и чекбокс)
    //        _filteredCompositions = ApplyAdditionalFilters(filteredItems);
    //        CompositionGrid.ItemsSource = _filteredCompositions;

    //        // Выбираем кликнутую композицию
    //        if (_filteredCompositions.Contains(composition))
    //        {
    //            CompositionGrid.SelectedItem = composition;
    //            DetailsCard.SetValue(composition);
    //        }
    //        else if (_filteredCompositions.Count > 0)
    //        {
    //            CompositionGrid.SelectedIndex = 0;
    //        }
    //    }

    //    /// <summary>
    //    /// Получение заголовка категории
    //    /// </summary>
    //    private string GetCategoryTitle(string categoryId)
    //    {
    //        return categoryId switch
    //        {
    //            CATEGORY_ZERO => "Нулевые остатки",
    //            CATEGORY_CRITICAL => "Критические остатки (0 – 10 000 кг)",
    //            CATEGORY_LOW => "Низкие остатки (10 000 – 50 000 кг)",
    //            CATEGORY_HIGH => "Большие остатки (> 50 000 кг)",
    //            _ => "Все композиции"
    //        };
    //    }



    //    private void ApplyFilters()
    //    {

    //        if (_compositions == null) return;

    //        // Если выбрана категория в сводках, фильтруем по ней
    //        if (_selectedCategory != null)
    //        {
    //            // Перефильтровываем текущую выбранную категорию
    //            var filteredCompositions = FilterCompositionsByCategory(_selectedCategory);
    //            _filteredCompositions = ApplyAdditionalFilters(filteredCompositions);
    //        }
    //        else
    //        {
    //            // Если нет выбранной категории, показываем все с применением фильтров
    //            _filteredCompositions = ApplyAdditionalFilters(_compositions);
    //        }

    //        CompositionGrid.ItemsSource = _filteredCompositions;

    //        // Автоматически выбираем первую композицию, если есть
    //        if (_filteredCompositions.Count > 0)
    //        {
    //            CompositionGrid.SelectedIndex = 0;
    //        }
    //        else
    //        {
    //            DetailsCard.SetValue(null);
    //        }
    //    }

    //    private List<MaterialDataComposition> FilterCompositionsByCategory(string categoryId)
    //    {
    //        if (_compositions == null) return new List<MaterialDataComposition>();

    //        var filtered = new List<MaterialDataComposition>();

    //        foreach (var composition in _compositions)
    //        {
    //            bool hasMatchingWidths = false;

    //            switch (categoryId)
    //            {
    //                case "zero":
    //                    hasMatchingWidths = composition.Layers?
    //                        .Any(l => l.Widths?.Any(w => w.StockKg == 0) == true) == true;
    //                    break;

    //                case "critical":
    //                    hasMatchingWidths = composition.Layers?
    //                        .Any(l => l.Widths?.Any(w => w.StockKg > 0 && w.StockKg <= 10000) == true) == true;
    //                    break;

    //                case "low":
    //                    hasMatchingWidths = composition.Layers?
    //                        .Any(l => l.Widths?.Any(w => w.StockKg > 10000 && w.StockKg <= 50000) == true) == true;
    //                    break;

    //                case "high":
    //                    hasMatchingWidths = composition.Layers?
    //                        .Any(l => l.Widths?.Any(w => w.StockKg > 50000) == true) == true;
    //                    break;

    //                default:
    //                    hasMatchingWidths = true;
    //                    break;
    //            }

    //            if (hasMatchingWidths)
    //            {
    //                filtered.Add(composition);
    //            }
    //        }

    //        return filtered;
    //    }

    //    /// <summary>
    //    /// Расчет глобальных метрик для всех данных
    //    /// </summary>
    //    private void CalculateMetrics()
    //    {
    //        _totalCompositions = _compositions?.Count ?? 0;
    //        _lowRemainsCount = 0;
    //        _criticalRemainsCount = 0;
    //        _highRemainsCount = 0;

    //        if (_compositions != null)
    //        {
    //            // Считаем композиции, которые имеют хоть один формат в категории
    //            foreach (var composition in _compositions)
    //            {
    //                if (composition == null || composition.Layers == null) continue;

    //                bool hasCritical = false;
    //                bool hasLow = false;
    //                bool hasHigh = false;

    //                foreach (var layer in composition.Layers)
    //                {
    //                    if (layer.Widths == null) continue;

    //                    foreach (var width in layer.Widths)
    //                    {
    //                        if (width.StockKg > 0 && width.StockKg <= 10000)
    //                            hasCritical = true;
    //                        else if (width.StockKg > 10000 && width.StockKg <= 50000)
    //                            hasLow = true;
    //                        else if (width.StockKg > 50000)
    //                            hasHigh = true;
    //                    }
    //                }

    //                if (hasCritical) _criticalRemainsCount++;
    //                if (hasLow) _lowRemainsCount++;
    //                if (hasHigh) _highRemainsCount++;
    //            }
    //        }
    //    }

    //    /// <summary>
    //    /// Обновление отображения метрик
    //    /// </summary>
    //    private void UpdateMetricsDisplay()
    //    {
    //        if (TotalCompositionsMetric != null)
    //        {
    //            TotalCompositionsMetric.Text = $"Всего: {_totalCompositions}";

    //            // Меняем текстовые метки для ясности
    //            LowRemainsMetric.Text = $"Низкий: {_lowRemainsCount}";
    //            CriticalRemainsMetric.Text = $"Критич: {_criticalRemainsCount}";
    //            HighRemainsMetric.Text = $"Много: {_highRemainsCount}";
    //        }
    //    }

    //    /// <summary>
    //    /// Применение дополнительных фильтров (поиск и чекбокс)
    //    /// </summary>
    //    private List<MaterialDataComposition> ApplyAdditionalFilters(List<MaterialDataComposition> sourceList)
    //    {
    //        if (sourceList == null) return new List<MaterialDataComposition>();

    //        var filtered = new List<MaterialDataComposition>(sourceList);

    //        // Фильтр по поиску
    //        string searchText = SearchTextBox?.Text ?? "";
    //        if (!string.IsNullOrWhiteSpace(searchText))
    //        {
    //            filtered = filtered
    //                .Where(c => c.CartonName != null &&
    //                           c.CartonName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
    //                .ToList();
    //        }

    //        //// Фильтр по выбранному формату
    //        //if (_selectedWidthFilter > 0)
    //        //{
    //        //    filtered = filtered
    //        //        .Where(c => c.Layers != null && c.Layers.Any(l =>
    //        //            l.Widths != null && l.Widths.Any(w => w.Width == _selectedWidthFilter)))
    //        //        .ToList();
    //        //}

    //        return filtered;
    //    }

    //    /// <summary>
    //    /// Кнопка "Загрузить все"
    //    /// </summary>
    //    private void LoadAllButton_Click(object sender, RoutedEventArgs e)
    //    {
    //        if (_compositions == null) return;

    //        _selectedCategory = null;
    //        TableTitle.Text = "Все композиции";

    //        // Применяем все фильтры ко всем данным
    //        _filteredCompositions = ApplyAdditionalFilters(_compositions);
    //        CompositionGrid.ItemsSource = _filteredCompositions;

    //        if (_filteredCompositions.Count > 0)
    //        {
    //            CompositionGrid.SelectedIndex = 0;
    //        }
    //        else
    //        {
    //            DetailsCard.SetValue(null);
    //        }
    //    }

    //    /// <summary>
    //    /// Анализ всех форматов (WIDTH) во всех композициях
    //    /// </summary>
    //    private List<MaterialWidthAnalysisItem> AnalyzeAllWidths()
    //    {
    //        var widthItems = new List<MaterialWidthAnalysisItem>();

    //        if (_compositions == null) return widthItems;

    //        foreach (var composition in _compositions)
    //        {
    //            if (composition?.Layers == null) continue;

    //            foreach (var layer in composition.Layers)
    //            {
    //                if (layer?.Widths == null) continue;

    //                foreach (var widthData in layer.Widths)
    //                {
    //                    widthItems.Add(new MaterialWidthAnalysisItem
    //                    {
    //                        CartonName = composition.CartonName,
    //                        Idc = composition.Idc,
    //                        LayerNumber = layer.LayerNumber,
    //                        RawGroup = layer.RawGroup,
    //                        Width = widthData.Width,
    //                        StockKg = widthData.StockKg
    //                    });
    //                }
    //            }
    //        }

    //        return widthItems;
    //    }

    //    // Новый обработчик выбора в DataGrid
    //    private void CompositionGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    //    {
    //        var selected = CompositionGrid.SelectedItem as MaterialDataComposition;
    //        if (selected != null)
    //        {
    //            DetailsCard.SetValue(selected);
    //        }
    //        else
    //        {
    //            DetailsCard.SetValue(null);
    //        }
    //    }

    //    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    //    {
    //        ApplyFilters();
    //    }

    //    private void PlatformSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    //    {
    //        RefreshCompositionData();
    //    }

    //    private void Refresh_Click(object sender, RoutedEventArgs e)
    //    {
    //        RefreshCompositionData();
    //    }

    //    // Обработчик изменения выбора формата
    //    private void WidthFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    //    {
    //        if (WidthFilterComboBox.SelectedItem == null) return;

    //        var selectedItem = WidthFilterComboBox.SelectedItem.ToString();

    //        if (selectedItem == "Все форматы")
    //        {
    //            _selectedWidthFilter = 0;
    //        }
    //        else if (int.TryParse(selectedItem, out int width))
    //        {
    //            _selectedWidthFilter = width;
    //        }

    //        ApplyFilters();
    //    }
    //}

    //public partial class RawCompositionMaterialMonitorCardsTab : ControlBase
    //{
    //    private List<MaterialDataComposition> _compositions;
    //    private List<MaterialDataComposition> _filteredCompositions;

    //    // Поле для хранения выбранного формата
    //    private int _selectedWidthFilter = 0;

    //    // Добавляем счетчики для метрик
    //    private int _totalCompositions = 0;
    //    private int _lowRemainsCount = 0;
    //    private int _criticalRemainsCount = 0;
    //    private int _highRemainsCount = 0;

    //    // Текущая выбранная категория
    //    private string _selectedCategory = null;

    //    // Константы для категорий
    //    private const string CATEGORY_CRITICAL = "critical";
    //    private const string CATEGORY_LOW = "low";
    //    private const string CATEGORY_HIGH = "high";
    //    private const string CATEGORY_ZERO = "zero";

    //    public RawCompositionMaterialMonitorCardsTab()
    //    {
    //        InitializeComponent();
    //        RoleName = "[erp]raw_material_monitor";
    //        ControlTitle = "Монитор остатков сырья";
    //        DocumentationUrl = "/doc/l-pack-erp";

    //        OnMessage = (ItemMessage m) =>
    //        {
    //            if (m.ReceiverName == ControlName)
    //            {
    //                Commander.ProcessCommand(m.Action, m);
    //            }
    //        };

    //        OnKeyPressed = (System.Windows.Input.KeyEventArgs e) =>
    //        {
    //            if (!e.Handled)
    //            {
    //                Commander.ProcessKeyboard(e);
    //            }
    //        };

    //        OnLoad = () =>
    //        {
    //            SetDefaults();
    //        };

    //        OnUnload = () => { };
    //        OnFocusGot = () => { };
    //        OnFocusLost = () => { };

    //        ///<summary>
    //        /// Система команд (Commander)
    //        ///</summary>
    //        {
    //            Commander.SetCurrentGroup("main");
    //            {
    //                Commander.Add(new CommandItem()
    //                {
    //                    Name = "help",
    //                    Enabled = true,
    //                    Title = "Справка",
    //                    Description = "Показать справочную информацию",
    //                    MenuUse = true,
    //                    ButtonUse = true,
    //                    ButtonName = "HelpButton",
    //                    HotKey = "F1",
    //                    Action = () =>
    //                    {
    //                        Central.ShowHelp(DocumentationUrl);
    //                    },
    //                });
    //                Commander.Add(new CommandItem()
    //                {
    //                    Name = "loadAll",
    //                    Enabled = true,
    //                    Title = "Загрузить все",
    //                    Description = "Загрузить все композиции",
    //                    MenuUse = true,
    //                    ButtonUse = true,
    //                    ButtonName = "LoadAllButton",
    //                    Action = () => LoadAllButton_Click(null, null),
    //                });
    //            }
    //        }
    //        Commander.Init(this);
    //    }

    //    /// <summary>
    //    /// Загрузка данных (из БД) с фильтрацией WIDTH = 1600
    //    /// </summary>
    //    private List<MaterialDataComposition> LoadCompositionsData()
    //    {
    //        var compositions = new List<MaterialDataComposition>();

    //        var p = new Dictionary<string, string>();

    //        // Выбор из выпадающего списка
    //        var selectedPlatform = PlatformSelectBox.SelectedItem;
    //        if (!selectedPlatform.Equals(default(KeyValuePair<string, string>)))
    //        {
    //            p.Add("FACTORY_ID", selectedPlatform.Key);
    //        }
    //        else
    //        {
    //            p.Add("FACTORY_ID", "1"); // Значение по умолчанию
    //        }

    //        var q = new LPackClientQuery();
    //        q.Request.SetParam("Module", "Stock");
    //        q.Request.SetParam("Object", "RawMaterialResidueMonitor");
    //        q.Request.SetParam("Action", "RawCompositionList");

    //        q.Request.SetParams(p);
    //        q.Request.Timeout = 80000;
    //        q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

    //        q.DoQuery();

    //        if (q.Answer.Status == 0)
    //        {
    //            var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
    //            if (result != null)
    //            {
    //                var ds = ListDataSet.Create(result, "ITEMS");
    //                foreach (var item in ds.Items)
    //                {
    //                    int width = item.CheckGet("WIDTH").ToInt();

    //                    // ФИЛЬТРАЦИЯ: ИСКЛЮЧАЕМ WIDTH = 1600
    //                    if (width == 1600)
    //                    {
    //                        continue;
    //                    }

    //                    int idc = item.CheckGet("IDC").ToInt();

    //                    // Поиск существующей композиции по IDC
    //                    if (compositions.Count(x => x.Idc == idc) > 0)
    //                    {
    //                        var comp = compositions.FirstOrDefault(x => x.Idc == idc);

    //                        string layerNumber = item.CheckGet("LAYER_NUMBER").ToString();
    //                        string rawGroup = item.CheckGet("RAW_GROUP").ToString();

    //                        // Поиск существующего слоя в композиции
    //                        if (comp.Layers.Count(x => x.LayerNumber == layerNumber && x.RawGroup == rawGroup) > 0)
    //                        {
    //                            var layer = comp.Layers.FirstOrDefault(x => x.LayerNumber == layerNumber && x.RawGroup == rawGroup);
    //                            layer.Widths.Add(new MaterialWidthData()
    //                            {
    //                                Width = width,
    //                                StockKg = item.CheckGet("STOCK_KG").ToInt()
    //                            });
    //                        }
    //                        else
    //                        {
    //                            // Создание нового слоя
    //                            var newLayer = new MaterialLayerData();
    //                            newLayer.LayerNumber = layerNumber;
    //                            newLayer.RawGroup = rawGroup;
    //                            newLayer.Widths.Add(new MaterialWidthData()
    //                            {
    //                                Width = width,
    //                                StockKg = item.CheckGet("STOCK_KG").ToInt()
    //                            });
    //                            comp.Layers.Add(newLayer);
    //                        }
    //                    }
    //                    else
    //                    {
    //                        // Создание новой композиции
    //                        var newComp = new MaterialDataComposition();
    //                        newComp.Idc = idc;
    //                        newComp.CartonName = item.CheckGet("CARTON_NAME").ToString();

    //                        // Создание первого слоя
    //                        var newLayer = new MaterialLayerData();
    //                        newLayer.LayerNumber = item.CheckGet("LAYER_NUMBER").ToString();
    //                        newLayer.RawGroup = item.CheckGet("RAW_GROUP").ToString();
    //                        newLayer.Widths.Add(new MaterialWidthData()
    //                        {
    //                            Width = width,
    //                            StockKg = item.CheckGet("STOCK_KG").ToInt()
    //                        });

    //                        newComp.Layers.Add(newLayer);
    //                        compositions.Add(newComp);
    //                    }
    //                }

    //                // Сортировка по ширине внутри каждого слоя
    //                foreach (var comp in compositions)
    //                {
    //                    foreach (var layer in comp.Layers)
    //                    {
    //                        layer.Widths = layer.Widths.OrderBy(w => w.Width).ToList();
    //                    }
    //                }
    //            }
    //        }
    //        else
    //        {
    //            q.ProcessError();
    //        }

    //        return compositions;
    //    }

    //    public void SetDefaults()
    //    {
    //        PlatformSelectBox.SetItems(new Dictionary<string, string>()
    //        {
    //            {"1",  "Липецк"},
    //            {"2",  "Кашира"},
    //        });
    //        PlatformSelectBox.SelectedItem = PlatformSelectBox.Items.First();
    //    }

    //    private void RefreshCompositionData()
    //    {
    //        _compositions = LoadCompositionsData();
    //        CalculateMetrics();
    //        UpdateMetricsDisplay();
    //        UpdateWidthFilterList();
    //        UpdateSummaryPanels();
    //        ClearZone2AndZone3();
    //    }

    //    /// <summary>
    //    /// Метод для заполнения списка форматов
    //    /// </summary>
    //    private void UpdateWidthFilterList()
    //    {
    //        if (_compositions == null) return;

    //        var allWidths = new HashSet<int>();

    //        foreach (var composition in _compositions)
    //        {
    //            if (composition?.Layers == null) continue;

    //            foreach (var layer in composition.Layers)
    //            {
    //                if (layer?.Widths == null) continue;

    //                foreach (var widthData in layer.Widths)
    //                {
    //                    // Исключаем ширину 1600
    //                    if (widthData.Width != 1600)
    //                    {
    //                        allWidths.Add(widthData.Width);
    //                    }
    //                }
    //            }
    //        }

    //        // Сортируем форматы
    //        var sortedWidths = allWidths.OrderBy(w => w).ToList();

    //        // Добавляем "Все форматы" первым элементом
    //        var widthItems = new List<string> { "Все форматы" };
    //        widthItems.AddRange(sortedWidths.Select(w => w.ToString()));

    //        WidthFilterComboBox.ItemsSource = widthItems;
    //        WidthFilterComboBox.SelectedIndex = 0;
    //    }

    //    /// <summary>
    //    /// Очистка зон 2 и 3
    //    /// </summary>
    //    private void ClearZone2AndZone3()
    //    {
    //        CompositionGrid.ItemsSource = null;
    //        TableTitle.Text = "Выберите категорию";
    //        DetailsCard.SetValue(null);
    //        _selectedCategory = null;
    //    }

    //    /// <summary>
    //    /// Создание сводок для Зоны 1
    //    /// </summary>
    //    private void UpdateSummaryPanels()
    //    {
    //        SummaryPanel.Children.Clear();

    //        if (_compositions == null || _compositions.Count == 0)
    //        {
    //            SummaryPanel.Children.Add(new TextBlock
    //            {
    //                Text = "Нет данных для отображения",
    //                Foreground = Brushes.Gray,
    //                FontStyle = FontStyles.Italic,
    //                TextAlignment = TextAlignment.Center,
    //                Margin = new Thickness(0, 20, 0, 0)
    //            });
    //            return;
    //        }

    //        // Анализируем все композиции и группируем их по категориям
    //        var compositionAnalysis = AnalyzeCompositionsByWidths();

    //        // 1. Нулевые форматы
    //        var zeroCompositions = compositionAnalysis
    //            .Where(c => c.Category == "zero" && c.ProblemWidthsCount > 0)
    //            .OrderByDescending(c => c.ProblemWidthsCount)
    //            .Take(10)
    //            .ToList();
    //        CreateCompositionSummaryCategory("❌ КОМПОЗИЦИИ С НУЛЕВЫМИ ФОРМАТАМИ", zeroCompositions, "zero");

    //        // 2. Критические форматы
    //        var criticalCompositions = compositionAnalysis
    //            .Where(c => c.Category == "critical" && c.ProblemWidthsCount > 0)
    //            .OrderByDescending(c => c.ProblemWidthsCount)
    //            .Take(10)
    //            .ToList();
    //        CreateCompositionSummaryCategory("🔴 КОМПОЗИЦИИ С КРИТИЧЕСКИМИ ФОРМАТАМИ", criticalCompositions, "critical");

    //        // 3. Низкие форматы
    //        var lowCompositions = compositionAnalysis
    //            .Where(c => c.Category == "low" && c.ProblemWidthsCount > 0)
    //            .OrderByDescending(c => c.ProblemWidthsCount)
    //            .Take(10)
    //            .ToList();
    //        CreateCompositionSummaryCategory("🟠 КОМПОЗИЦИИ С НИЗКИМИ ФОРМАТАМИ", lowCompositions, "low");

    //        // 4. Большие форматы
    //        var highCompositions = compositionAnalysis
    //            .Where(c => c.Category == "high" && c.ProblemWidthsCount > 0)
    //            .OrderByDescending(c => c.ProblemWidthsCount)
    //            .Take(10)
    //            .ToList();
    //        CreateCompositionSummaryCategory("🟢 КОМПОЗИЦИИ С БОЛЬШИМИ ФОРМАТАМИ", highCompositions, "high");
    //    }

    //    /// <summary>
    //    /// Метод анализа композиций
    //    /// </summary>
    //    private List<MaterialCompositionSummaryItem> AnalyzeCompositionsByWidths()
    //    {
    //        var result = new List<MaterialCompositionSummaryItem>();

    //        if (_compositions == null) return result;

    //        foreach (var composition in _compositions)
    //        {
    //            if (composition == null) continue;

    //            // Считаем проблемные форматы в каждой категории
    //            int zeroCount = 0;
    //            int criticalCount = 0;
    //            int lowCount = 0;
    //            int highCount = 0;

    //            foreach (var layer in composition.Layers)
    //            {
    //                if (layer?.Widths == null) continue;

    //                foreach (var width in layer.Widths)
    //                {
    //                    if (width.Width == 1600) continue;

    //                    if (width.StockKg == 0)
    //                        zeroCount++;
    //                    else if (width.StockKg <= 10000)
    //                        criticalCount++;
    //                    else if (width.StockKg <= 50000)
    //                        lowCount++;
    //                    else
    //                        highCount++;
    //                }
    //            }

    //            // Определяем основную категорию композиции (с максимальным количеством проблем)
    //            string mainCategory = "high";
    //            int maxCount = highCount;

    //            if (zeroCount > maxCount) { mainCategory = "zero"; maxCount = zeroCount; }
    //            if (criticalCount > maxCount) { mainCategory = "critical"; maxCount = criticalCount; }
    //            if (lowCount > maxCount) { mainCategory = "low"; maxCount = lowCount; }

    //            result.Add(new MaterialCompositionSummaryItem
    //            {
    //                CartonName = composition.CartonName,
    //                Idc = composition.Idc,
    //                TotalStockKg = composition.TotalStockKg,
    //                Category = mainCategory,
    //                ProblemWidthsCount = maxCount
    //            });
    //        }

    //        return result;
    //    }

    //    /// <summary>
    //    /// Создание сводки для категории композиций
    //    /// </summary>
    //    private void CreateCompositionSummaryCategory(string title, List<MaterialCompositionSummaryItem> items, string categoryId)
    //    {
    //        Border categoryContainer = new Border
    //        {
    //            Style = (Style)FindResource("SummaryCategoryStyle")
    //        };

    //        StackPanel categoryPanel = new StackPanel();

    //        // Заголовок категории
    //        Border headerBorder = new Border
    //        {
    //            Style = (Style)FindResource("SummaryHeaderStyle")
    //        };

    //        TextBlock titleText = new TextBlock
    //        {
    //            Text = $"{title} (Всего: {items.Count})",
    //            Style = (Style)FindResource("SummaryTitleStyle")
    //        };

    //        headerBorder.Child = titleText;
    //        categoryPanel.Children.Add(headerBorder);

    //        if (items.Count > 0)
    //        {
    //            // Находим максимальное количество проблемных форматов для шкалы
    //            int maxProblemCount = items.Max(c => c.ProblemWidthsCount);
    //            if (maxProblemCount == 0) maxProblemCount = 1;

    //            foreach (var item in items)
    //            {
    //                Border itemBorder = new Border
    //                {
    //                    Style = (Style)FindResource("SummaryItemStyle"),
    //                    Tag = new { Category = categoryId, CompositionId = item.Idc }
    //                };

    //                itemBorder.MouseLeftButtonDown += (s, e) =>
    //                {
    //                    HandleCompositionSummaryItemClick(categoryId, item.Idc);
    //                };

    //                Grid itemGrid = new Grid();
    //                itemGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
    //                itemGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
    //                itemGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

    //                // Название композиции
    //                StackPanel infoPanel = new StackPanel
    //                {
    //                    Orientation = Orientation.Vertical
    //                };

    //                TextBlock nameText = new TextBlock
    //                {
    //                    Text = item.CartonName,
    //                    FontSize = 11,
    //                    FontWeight = FontWeights.SemiBold,
    //                    TextTrimming = TextTrimming.CharacterEllipsis
    //                };

    //                TextBlock detailsText = new TextBlock
    //                {
    //                    Text = $"Всего: {item.TotalStockKg:N0} кг | Проблемных форматов: {item.ProblemWidthsCount}",
    //                    FontSize = 9,
    //                    Foreground = Brushes.Gray,
    //                    TextTrimming = TextTrimming.CharacterEllipsis
    //                };

    //                infoPanel.Children.Add(nameText);
    //                infoPanel.Children.Add(detailsText);

    //                Grid.SetColumn(infoPanel, 0);
    //                itemGrid.Children.Add(infoPanel);

    //                // Полоска-индикатор
    //                Border barChart = new Border
    //                {
    //                    Style = (Style)FindResource("BarChartStyle"),
    //                    Width = Math.Max(30, (item.ProblemWidthsCount / (double)maxProblemCount) * 120),
    //                    Background = item.CategoryColor,
    //                    ToolTip = $"{item.ProblemWidthsCount} проблемных форматов"
    //                };
    //                Grid.SetColumn(barChart, 1);
    //                itemGrid.Children.Add(barChart);

    //                // Общий остаток по композиции
    //                TextBlock valueText = new TextBlock
    //                {
    //                    Text = $"{item.TotalStockKg:N0} кг",
    //                    FontSize = 10,
    //                    FontWeight = FontWeights.SemiBold,
    //                    Foreground = item.CategoryColor,
    //                    VerticalAlignment = VerticalAlignment.Center,
    //                    MinWidth = 60,
    //                    TextAlignment = TextAlignment.Right
    //                };
    //                Grid.SetColumn(valueText, 2);
    //                itemGrid.Children.Add(valueText);

    //                itemBorder.Child = itemGrid;
    //                categoryPanel.Children.Add(itemBorder);
    //            }
    //        }
    //        else
    //        {
    //            categoryPanel.Children.Add(new TextBlock
    //            {
    //                Text = "Нет композиций",
    //                FontSize = 11,
    //                Foreground = Brushes.Gray,
    //                FontStyle = FontStyles.Italic,
    //                HorizontalAlignment = HorizontalAlignment.Center,
    //                Margin = new Thickness(0, 10, 0, 10)
    //            });
    //        }

    //        categoryContainer.Child = categoryPanel;
    //        SummaryPanel.Children.Add(categoryContainer);
    //    }

    //    /// <summary>
    //    /// Обработчик клика на композицию в сводке
    //    /// </summary>
    //    private void HandleCompositionSummaryItemClick(string categoryId, int compositionId)
    //    {
    //        _selectedCategory = categoryId;

    //        // Находим композицию по ID
    //        var targetComposition = _compositions.FirstOrDefault(c => c.Idc == compositionId);

    //        if (targetComposition == null) return;

    //        // Обновляем заголовок таблицы
    //        TableTitle.Text = GetCategoryTitle(categoryId);

    //        // Фильтруем композиции по категории
    //        var filteredCompositions = FilterCompositionsByCategory(categoryId);

    //        // Применяем дополнительные фильтры
    //        _filteredCompositions = ApplyAdditionalFilters(filteredCompositions);
    //        CompositionGrid.ItemsSource = _filteredCompositions;

    //        // Выбираем целевую композицию
    //        if (targetComposition != null && _filteredCompositions.Contains(targetComposition))
    //        {
    //            CompositionGrid.SelectedItem = targetComposition;
    //            DetailsCard.SetValue(targetComposition);
    //        }
    //        else if (_filteredCompositions.Count > 0)
    //        {
    //            CompositionGrid.SelectedIndex = 0;
    //        }
    //    }

    //    /// <summary>
    //    /// Получение заголовка категории
    //    /// </summary>
    //    private string GetCategoryTitle(string categoryId)
    //    {
    //        return categoryId switch
    //        {
    //            "zero" => "Композиции с нулевыми форматами",
    //            "critical" => "Композиции с критическими форматами (1-10 000 кг)",
    //            "low" => "Композиции с низкими форматами (10 001-50 000 кг)",
    //            "high" => "Композиции с большими форматами (>50 000 кг)",
    //            _ => "Все композиции"
    //        };
    //    }

    //    /// <summary>
    //    /// Фильтрация композиций по категории
    //    /// </summary>
    //    private List<MaterialDataComposition> FilterCompositionsByCategory(string categoryId)
    //    {
    //        if (_compositions == null) return new List<MaterialDataComposition>();

    //        var filtered = new List<MaterialDataComposition>();

    //        foreach (var composition in _compositions)
    //        {
    //            bool hasMatchingWidths = false;

    //            switch (categoryId)
    //            {
    //                case "zero":
    //                    hasMatchingWidths = composition.Layers?
    //                        .Any(l => l.Widths?.Any(w => w.StockKg == 0) == true) == true;
    //                    break;

    //                case "critical":
    //                    hasMatchingWidths = composition.Layers?
    //                        .Any(l => l.Widths?.Any(w => w.StockKg > 0 && w.StockKg <= 10000) == true) == true;
    //                    break;

    //                case "low":
    //                    hasMatchingWidths = composition.Layers?
    //                        .Any(l => l.Widths?.Any(w => w.StockKg > 10000 && w.StockKg <= 50000) == true) == true;
    //                    break;

    //                case "high":
    //                    hasMatchingWidths = composition.Layers?
    //                        .Any(l => l.Widths?.Any(w => w.StockKg > 50000) == true) == true;
    //                    break;

    //                default:
    //                    hasMatchingWidths = true;
    //                    break;
    //            }

    //            if (hasMatchingWidths)
    //            {
    //                filtered.Add(composition);
    //            }
    //        }

    //        return filtered;
    //    }

    //    /// <summary>
    //    /// Применение фильтров
    //    /// </summary>
    //    private void ApplyFilters()
    //    {
    //        if (_compositions == null) return;

    //        // Если выбрана категория в сводках, фильтруем по ней
    //        if (_selectedCategory != null)
    //        {
    //            var filteredCompositions = FilterCompositionsByCategory(_selectedCategory);
    //            _filteredCompositions = ApplyAdditionalFilters(filteredCompositions);
    //        }
    //        else
    //        {
    //            // Если нет выбранной категории, показываем все с применением фильтров
    //            _filteredCompositions = ApplyAdditionalFilters(_compositions);
    //        }

    //        CompositionGrid.ItemsSource = _filteredCompositions;

    //        // Автоматически выбираем первую композицию, если есть
    //        if (_filteredCompositions.Count > 0)
    //        {
    //            CompositionGrid.SelectedIndex = 0;
    //        }
    //        else
    //        {
    //            DetailsCard.SetValue(null);
    //        }
    //    }

    //    /// <summary>
    //    /// Расчет глобальных метрик
    //    /// </summary>
    //    private void CalculateMetrics()
    //    {
    //        _totalCompositions = _compositions?.Count ?? 0;
    //        _lowRemainsCount = 0;
    //        _criticalRemainsCount = 0;
    //        _highRemainsCount = 0;

    //        if (_compositions != null)
    //        {
    //            // Считаем композиции, которые имеют хоть один формат в категории
    //            foreach (var composition in _compositions)
    //            {
    //                if (composition == null || composition.Layers == null) continue;

    //                bool hasCritical = false;
    //                bool hasLow = false;
    //                bool hasHigh = false;

    //                foreach (var layer in composition.Layers)
    //                {
    //                    if (layer.Widths == null) continue;

    //                    foreach (var width in layer.Widths)
    //                    {
    //                        if (width.StockKg > 0 && width.StockKg <= 10000)
    //                            hasCritical = true;
    //                        else if (width.StockKg > 10000 && width.StockKg <= 50000)
    //                            hasLow = true;
    //                        else if (width.StockKg > 50000)
    //                            hasHigh = true;
    //                    }
    //                }

    //                if (hasCritical) _criticalRemainsCount++;
    //                if (hasLow) _lowRemainsCount++;
    //                if (hasHigh) _highRemainsCount++;
    //            }
    //        }
    //    }

    //    /// <summary>
    //    /// Обновление отображения метрик
    //    /// </summary>
    //    private void UpdateMetricsDisplay()
    //    {
    //        if (TotalCompositionsMetric != null)
    //        {
    //            TotalCompositionsMetric.Text = $"Всего: {_totalCompositions}";
    //            LowRemainsMetric.Text = $"Низкий: {_lowRemainsCount}";
    //            CriticalRemainsMetric.Text = $"Критич: {_criticalRemainsCount}";
    //            HighRemainsMetric.Text = $"Много: {_highRemainsCount}";
    //        }
    //    }

    //    /// <summary>
    //    /// Применение дополнительных фильтров
    //    /// </summary>
    //    private List<MaterialDataComposition> ApplyAdditionalFilters(List<MaterialDataComposition> sourceList)
    //    {
    //        if (sourceList == null) return new List<MaterialDataComposition>();

    //        var filtered = new List<MaterialDataComposition>(sourceList);

    //        // Фильтр по поиску
    //        string searchText = SearchTextBox?.Text ?? "";
    //        if (!string.IsNullOrWhiteSpace(searchText))
    //        {
    //            filtered = filtered
    //                .Where(c => c.CartonName != null &&
    //                           c.CartonName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
    //                .ToList();
    //        }

    //        // Фильтр по выбранному формату
    //        if (_selectedWidthFilter > 0)
    //        {
    //            filtered = filtered
    //                .Where(c => c.Layers != null && c.Layers.Any(l =>
    //                    l.Widths != null && l.Widths.Any(w => w.Width == _selectedWidthFilter)))
    //                .ToList();
    //        }

    //        return filtered;
    //    }

    //    /// <summary>
    //    /// Кнопка "Загрузить все"
    //    /// </summary>
    //    private void LoadAllButton_Click(object sender, RoutedEventArgs e)
    //    {
    //        if (_compositions == null) return;

    //        _selectedCategory = null;
    //        TableTitle.Text = "Все композиции";

    //        // Применяем все фильтры ко всем данным
    //        _filteredCompositions = ApplyAdditionalFilters(_compositions);
    //        CompositionGrid.ItemsSource = _filteredCompositions;

    //        if (_filteredCompositions.Count > 0)
    //        {
    //            CompositionGrid.SelectedIndex = 0;
    //        }
    //        else
    //        {
    //            DetailsCard.SetValue(null);
    //        }
    //    }

    //    // Обработчики событий
    //    private void CompositionGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    //    {
    //        var selected = CompositionGrid.SelectedItem as MaterialDataComposition;
    //        if (selected != null)
    //        {
    //            DetailsCard.SetValue(selected);
    //        }
    //        else
    //        {
    //            DetailsCard.SetValue(null);
    //        }
    //    }

    //    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    //    {
    //        ApplyFilters();
    //    }

    //    private void PlatformSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    //    {
    //        RefreshCompositionData();
    //    }

    //    private void Refresh_Click(object sender, RoutedEventArgs e)
    //    {
    //        RefreshCompositionData();
    //    }

    //    /// <summary>
    //    /// Обработчик изменения выбора формата
    //    /// </summary>
    //    private void WidthFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    //    {
    //        if (WidthFilterComboBox.SelectedItem == null) return;

    //        var selectedItem = WidthFilterComboBox.SelectedItem.ToString();

    //        if (selectedItem == "Все форматы")
    //        {
    //            _selectedWidthFilter = 0;
    //        }
    //        else if (int.TryParse(selectedItem, out int width))
    //        {
    //            _selectedWidthFilter = width;
    //        }

    //        ApplyFilters();
    //    }
    //}

   
        /// <summary>
        /// Класс для группировки по композициям в сводках
        /// </summary>
        public class MaterialCompositionSummaryItem
        {
            public string CartonName { get; set; }
            public int Idc { get; set; }
            public int TotalStockKg { get; set; }
            public string Category { get; set; }

            public SolidColorBrush CategoryColor
            {
                get
                {
                    return Category switch
                    {
                        "critical" => new SolidColorBrush(Colors.Red),
                        "low" => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                        "high" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                        _ => new SolidColorBrush(Colors.Gray)
                    };
                }
            }
        }

        /// <summary>
        /// Класс для данных по форматам для графика
        /// </summary>
        public class FormatDistributionItem
        {
            public int Width { get; set; }
            public int TotalStockKg { get; set; }
            public double Percentage { get; set; }
            public int CompositionCount { get; set; }

            public SolidColorBrush ChartColor
            {
                get
                {
                    // Генерируем цвет на основе ширины формата
                    int hue = (Width % 360) * 10;
                    return new SolidColorBrush(Color.FromArgb(200,
                        (byte)((hue * 5) % 255),
                        (byte)((hue * 3) % 255),
                        (byte)((hue * 7) % 255)));
                }
            }
        }

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

            // Новые пороги для классификации (в кг)
            private const int CRITICAL_THRESHOLD = 1000000;       // до 1 000 000 кг
            private const int LOW_THRESHOLD = 2500000;           // 1 000 001 - 2 500 000 кг

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
            /// Загрузка данных (из БД) с фильтрацией WIDTH = 1600
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

                            // ФИЛЬТРАЦИЯ: ИСКЛЮЧАЕМ WIDTH = 1600
                            if (width == 1600)
                            {
                                continue;
                            }

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

                // 1. Критические остатки (до 1 000 000 кг)
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
                CreateCompositionSummaryCategory("🔴 КРИТИЧЕСКИЕ ОСТАТКИ (до 1 000 000 кг)", criticalCompositions, CATEGORY_CRITICAL);

                // 2. Низкие остатки (1 000 001 - 2 500 000 кг)
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
                CreateCompositionSummaryCategory("🟠 НИЗКИЕ ОСТАТКИ (1 000 001 - 2 500 000 кг)", lowCompositions, CATEGORY_LOW);

                // 3. Большие остатки (> 2 500 000 кг)
                var highCompositions = _compositions
                    .Where(c => c.TotalStockKg > LOW_THRESHOLD)
                    .OrderByDescending(c => c.TotalStockKg)
                    .Take(10)
                    .Select(c => new MaterialCompositionSummaryItem
                    {
                        CartonName = c.CartonName,
                        Idc = c.Idc,
                        TotalStockKg = c.TotalStockKg,
                        Category = CATEGORY_HIGH
                    })
                    .ToList();
                CreateCompositionSummaryCategory("🟢 БОЛЬШИЕ ОСТАТКИ (> 2 500 000 кг)", highCompositions, CATEGORY_HIGH);

                // 4. График распределения по форматам
                CreateFormatDistributionChart();
            }

            /// <summary>
            /// Создание сводки для категории композиций
            /// </summary>
            private void CreateCompositionSummaryCategory(string title, List<MaterialCompositionSummaryItem> items, string categoryId)
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

                        Grid itemGrid = new Grid();
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
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        Grid.SetColumn(nameText, 0);
                        itemGrid.Children.Add(nameText);

                        // Полоска-индикатор
                        Border barChart = new Border
                        {
                            Style = (Style)FindResource("BarChartStyle"),
                            Width = Math.Max(50, (item.TotalStockKg / (double)maxStock) * 150),
                            Background = item.CategoryColor,
                            ToolTip = $"{item.TotalStockKg:N0} кг"
                        };
                        Grid.SetColumn(barChart, 1);
                        itemGrid.Children.Add(barChart);

                        // Общий остаток по композиции
                        TextBlock valueText = new TextBlock
                        {
                            Text = $"{item.TotalStockKg:N0} кг",
                            FontSize = 11,
                            FontWeight = FontWeights.SemiBold,
                            Foreground = item.CategoryColor,
                            VerticalAlignment = VerticalAlignment.Center,
                            MinWidth = 80,
                            TextAlignment = TextAlignment.Right
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
                var formatStats = new Dictionary<int, FormatDistributionItem>();
                int totalStockAllFormats = 0;

                foreach (var composition in _compositions)
                {
                    if (composition?.Layers == null) continue;

                    foreach (var layer in composition.Layers)
                    {
                        if (layer?.Widths == null) continue;

                        foreach (var width in layer.Widths)
                        {
                            if (width.Width == 1600) continue;

                            if (!formatStats.ContainsKey(width.Width))
                            {
                                formatStats[width.Width] = new FormatDistributionItem
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
                    .Take(15) // Берем ТОП-15 форматов
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
                    Text = "📊 РАСПРЕДЕЛЕНИЕ ПО ФОРМАТАМ (ТОП-15)",
                    Style = (Style)FindResource("SummaryTitleStyle")
                };

                headerBorder.Child = titleText;
                chartPanel.Children.Add(headerBorder);

                if (sortedFormats.Count > 0)
                {
                    // Максимальное значение для масштабирования
                    double maxPercentage = sortedFormats.Max(f => f.Percentage);
                    if (maxPercentage == 0) maxPercentage = 100;

                    foreach (var format in sortedFormats)
                    {
                        Grid formatGrid = new Grid();
                        formatGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(50) }); // Ширина
                        formatGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) }); // График
                        formatGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(70) }); // Процент
                        formatGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(80) }); // Количество
                        formatGrid.Margin = new Thickness(0, 2, 0, 2);

                        // Ширина формата
                        TextBlock widthText = new TextBlock
                        {
                            Text = format.Width.ToString(),
                            FontSize = 10,
                            FontWeight = FontWeights.Bold,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 5, 0)
                        };
                        Grid.SetColumn(widthText, 0);
                        formatGrid.Children.Add(widthText);

                        // График
                        Border chartBar = new Border
                        {
                            Style = (Style)FindResource("FormatBarStyle"),
                            Width = (format.Percentage / maxPercentage) * 200,
                            Background = format.ChartColor,
                            ToolTip = $"{format.TotalStockKg:N0} кг в {format.CompositionCount} композициях"
                        };
                        Grid.SetColumn(chartBar, 1);
                        formatGrid.Children.Add(chartBar);

                        // Процент
                        TextBlock percentText = new TextBlock
                        {
                            Text = $"{format.Percentage:0.0}%",
                            FontSize = 10,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(5, 0, 0, 0)
                        };
                        Grid.SetColumn(percentText, 2);
                        formatGrid.Children.Add(percentText);

                        // Количество
                        TextBlock stockText = new TextBlock
                        {
                            Text = $"{format.TotalStockKg:N0} кг",
                            FontSize = 10,
                            FontWeight = FontWeights.SemiBold,
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Right
                        };
                        Grid.SetColumn(stockText, 3);
                        formatGrid.Children.Add(stockText);

                        chartPanel.Children.Add(formatGrid);
                    }

                    // Итого
                    TextBlock totalText = new TextBlock
                    {
                        Text = $"Итого по всем форматам: {totalStockAllFormats:N0} кг",
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
                    CATEGORY_LOW => "Низкие остатки (1 000 001 - 2 500 000 кг)",
                    CATEGORY_HIGH => "Большие остатки (> 2 500 000 кг)",
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
                        .OrderByDescending(c => c.TotalStockKg)
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




