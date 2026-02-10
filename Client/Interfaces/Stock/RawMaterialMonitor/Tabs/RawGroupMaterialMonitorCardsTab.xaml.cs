using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Stock.RawMaterialMonitor;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static DevExpress.Data.Filtering.Helpers.SubExprHelper.ThreadHoppingFiltering;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Остаток по сырьевым группам на складе
    /// в карточном виде
    /// </summary>
    /// <author>kurasov_dp</author>

    public partial class RawGroupMaterialMonitorCardsTab : ControlBase
    {
        private List<MaterialData> _allMaterials;
        private List<MaterialData> _filteredMaterials;

        // Счетчики для метрик
        private int _totalGroups = 0;
        private int _criticalRemainsCount = 0;
        private int _lowRemainsCount = 0;
        private int _highRemainsCount = 0;

        // Пороги для категорий (в кг)
        private const int CRITICAL_THRESHOLD = 0;       // 0 кг
        private const int LOW_THRESHOLD = 99999;        // до 99,999 кг
                                                        // Все что выше 100,000 кг - "Много в остатке"

        // Константы для категорий
        private const string CATEGORY_TOTAL = "total";
        private const string CATEGORY_CRITICAL = "critical";
        private const string CATEGORY_LOW = "low";
        private const string CATEGORY_HIGH = "high";

        // Выбранный формат
        private string _selectedFormat = "Все форматы";

        // Выбранная категория
        private string _selectedCategory = null;

        // Флаг, что данные загружены
        private bool _isDataLoaded = false;

        public RawGroupMaterialMonitorCardsTab()
        {
            InitializeComponent();

            RoleName = "[erp]raw_material_monitor";
            ControlTitle = "Монитор остатков сырья (группы)";
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
                RefreshData();
                ShowInitialMessage();
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
                }
            }
            Commander.Init(this);
        }

        /// <summary>
        /// Показать начальное сообщение
        /// </summary>
        private void ShowInitialMessage()
        {
            CardsContainer.Visibility = Visibility.Collapsed;
            InitialMessageContainer.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Показать карточки
        /// </summary>
        private void ShowCards()
        {
            CardsContainer.Visibility = Visibility.Visible;
            InitialMessageContainer.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Основной метод загрузки данных
        /// </summary>
        private void RefreshData()
        {
            try
            {
                // Загрузка данных
                _allMaterials = LoadMaterialsData();
                _isDataLoaded = true;

                // Расчет метрик
                CalculateMetrics();

                // Обновление списка форматов
                UpdateFormatList();
            }
            catch (Exception ex)
            {
                _isDataLoaded = false;
            }
        }

        /// <summary>
        /// Загрузка данных (из БД)
        /// В модель карточек
        /// </summary>
        private List<MaterialData> LoadMaterialsData()
        {
            var materials = new List<MaterialData>();

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
            q.Request.SetParam("Action", "RawGroupList");
            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
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
                        if (materials.Count(x => x.IdRawGroup == item.CheckGet("ID_RAW_GROUP").ToInt()) > 0)
                        {
                            var m = materials.FirstOrDefault(x => x.IdRawGroup == item.CheckGet("ID_RAW_GROUP").ToInt());
                            m.MaterialDataFormats.Add(new MaterialDataFormat()
                            {
                                Name = item.CheckGet("FORMAT"),
                                QUTY = item.CheckGet("QTY_STOCK_ONLY").ToInt()
                            });
                        }
                        else
                        {
                            var m = new MaterialData();
                            m.IdRawGroup = item.CheckGet("ID_RAW_GROUP").ToInt();
                            m.Name = item.CheckGet("NAME");
                            m.MaterialDataFormats.Add(new MaterialDataFormat()
                            {
                                Name = item.CheckGet("FORMAT"),
                                QUTY = item.CheckGet("QTY_STOCK_ONLY").ToInt()
                            });
                            materials.Add(m);
                        }
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            return materials;
        }

        /// <summary>
        /// Расчет метрик
        /// </summary>
        private void CalculateMetrics()
        {
            if (_allMaterials == null) return;

            _totalGroups = _allMaterials.Count;
            _criticalRemainsCount = 0;
            _lowRemainsCount = 0;
            _highRemainsCount = 0;

            foreach (var material in _allMaterials)
            {
                int totalQty = material.MaterialDataFormats.Sum(f => f.QUTY);

                if (totalQty == CRITICAL_THRESHOLD)
                    _criticalRemainsCount++;
                else if (totalQty > CRITICAL_THRESHOLD && totalQty <= LOW_THRESHOLD)
                    _lowRemainsCount++;
                else if (totalQty > LOW_THRESHOLD)
                    _highRemainsCount++;
            }

            UpdateMetricsDisplay();
        }

        /// <summary>
        /// Обновление отображения метрик
        /// </summary>
        private void UpdateMetricsDisplay()
        {
            Dispatcher.Invoke(() =>
            {
                TotalMetricText.Text = $"Всего: {_totalGroups}";
                CriticalMetricText.Text = $"Критич: {_criticalRemainsCount}";
                LowMetricText.Text = $"Низкий: {_lowRemainsCount}";
                HighMetricText.Text = $"Много: {_highRemainsCount}";
            });
        }

        /// <summary>
        /// Обновление списка форматов в выпадающем списке
        /// </summary>
        private void UpdateFormatList()
        {
            if (_allMaterials == null || _allMaterials.Count == 0) return;

            // Собираем все уникальные форматы
            var allFormats = new HashSet<string>();
            foreach (var material in _allMaterials)
            {
                foreach (var format in material.MaterialDataFormats)
                {
                    if (!string.IsNullOrEmpty(format.Name))
                    {
                        allFormats.Add(format.Name);
                    }
                }
            }

            // Создаем словарь для SelectBox
            var formatItems = new Dictionary<string, string>();
            formatItems.Add("ALL", "Все форматы");

            // Сортируем форматы по числовому значению
            var sortedFormats = allFormats
                .Where(f => int.TryParse(f, out _))
                .Select(f => int.Parse(f))
                .OrderBy(f => f)
                .Select(f => f.ToString())
                .ToList();

            // Добавляем числовые форматы
            foreach (var format in sortedFormats)
            {
                formatItems.Add(format, format);
            }

            // Добавляем нечисловые форматы
            var nonNumericFormats = allFormats
                .Where(f => !int.TryParse(f, out _))
                .OrderBy(f => f)
                .ToList();

            foreach (var format in nonNumericFormats)
            {
                formatItems.Add(format, format);
            }

            // Обновляем UI в основном потоке
            Dispatcher.Invoke(() =>
            {
                // Устанавливаем элементы в SelectBox
                FormatSelectBox.SetItems(formatItems);

                // Сохраняем текущий выбор, если он есть
                var currentSelection = FormatSelectBox.SelectedItem;
                if (!currentSelection.Equals(default(KeyValuePair<string, string>)) &&
                    formatItems.ContainsKey(currentSelection.Key))
                {
                    FormatSelectBox.SelectedItem = new KeyValuePair<string, string>(
                        currentSelection.Key,
                        currentSelection.Value);
                }
                else
                {
                    // Иначе выбираем "Все форматы"
                    var allFormatsItem = formatItems.FirstOrDefault(x => x.Key == "ALL");
                    if (!allFormatsItem.Equals(default(KeyValuePair<string, string>)))
                    {
                        FormatSelectBox.SelectedItem = allFormatsItem;
                    }
                    else if (formatItems.Any())
                    {
                        FormatSelectBox.SelectedItem = formatItems.First();
                    }
                }
            });
        }

        /// <summary>
        /// Применение фильтров
        /// </summary>
        private void ApplyFilters()
        {
            if (!_isDataLoaded) return;
            if (_allMaterials == null) return;

            // Фильтрация по категории
            var filtered = FilterByCategory(_allMaterials);

            // Фильтрация по формату (если это не критическая категория)
            if (_selectedCategory != CATEGORY_CRITICAL)
            {
                filtered = FilterByFormat(filtered);
            }

            // Фильтрация по поиску
            filtered = FilterBySearch(filtered);

            _filteredMaterials = filtered;

            // Обновление карточек
            UpdateCards();
        }

        /// <summary>
        /// Фильтрация по категории
        /// </summary>
        private List<MaterialData> FilterByCategory(List<MaterialData> materials)
        {
            if (string.IsNullOrEmpty(_selectedCategory) || _selectedCategory == CATEGORY_TOTAL)
            {
                return materials;
            }

            return materials.Where(material =>
            {
                int totalQty = material.MaterialDataFormats.Sum(f => f.QUTY);

                return _selectedCategory switch
                {
                    CATEGORY_CRITICAL => totalQty == CRITICAL_THRESHOLD,
                    CATEGORY_LOW => totalQty > CRITICAL_THRESHOLD && totalQty <= LOW_THRESHOLD,
                    CATEGORY_HIGH => totalQty > LOW_THRESHOLD,
                    _ => true
                };
            }).ToList();
        }

        /// <summary>
        /// Фильтрация по формату (только для не-критических)
        /// </summary>
        private List<MaterialData> FilterByFormat(List<MaterialData> materials)
        {
            if (string.IsNullOrEmpty(_selectedFormat) || _selectedFormat == "Все форматы")
            {
                return materials;
            }

            return materials.Where(material =>
            {
                // Если у группы есть формат с ненулевым количеством
                return material.MaterialDataFormats
                    .Any(f => f.Name == _selectedFormat && f.QUTY > 0);
            }).ToList();
        }

        /// <summary>
        /// Фильтрация по поиску
        /// </summary>
        private List<MaterialData> FilterBySearch(List<MaterialData> materials)
        {
            string searchText = SearchTextBox?.Text ?? "";
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return materials;
            }

            return materials
                .Where(m => m.Name != null &&
                           m.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        /// <summary>
        /// Обновление карточек
        /// </summary>
        private void UpdateCards()
        {
            ClearCards();

            if (_filteredMaterials == null || _filteredMaterials.Count == 0)
            {
                // Показываем сообщение, если нет данных
                var noDataText = new TextBlock
                {
                    Text = "Нет данных для отображения",
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 50, 0, 0)
                };

                var grid = new Grid();
                grid.Children.Add(noDataText);

                CardsContainer.Children.Add(grid);
            }
            else
            {
                // Создание карточек
                foreach (var material in _filteredMaterials)
                {
                    AddMaterialCard(material);
                }
            }
        }

        /// <summary>
        /// Добавление карточки материала
        /// </summary>
        private void AddMaterialCard(MaterialData material)
        {
            var materialGroupElement = new MaterialGroupElement();

            // Для критических всегда показываем "Все форматы"
            string formatToShow = _selectedCategory == CATEGORY_CRITICAL ? "Все форматы" : _selectedFormat;

            materialGroupElement.SetValue(material, formatToShow, _selectedCategory == CATEGORY_CRITICAL);

            CardsContainer.Children.Add(materialGroupElement);
        }

        /// <summary>
        /// Очистка всех карточек
        /// </summary>
        private void ClearCards()
        {
            CardsContainer.Children.Clear();
        }

        public void SetDefaults()
        {
            // Площадка
            PlatformSelectBox.SetItems(new Dictionary<string, string>()
            {
                {"1", "Липецк"},
                {"2", "Кашира"},
            });

            // Выбираем первый элемент только если он есть
            if (PlatformSelectBox.Items.Any())
            {
                PlatformSelectBox.SelectedItem = PlatformSelectBox.Items.First();
            }

            // Категория
            var categoryItems = new Dictionary<string, string>()
            {
                {CATEGORY_TOTAL, "Все группы"},
                {CATEGORY_CRITICAL, "Критические (0 кг)"},
                {CATEGORY_LOW, "Низкие (1 - 99,999 кг)"},
                {CATEGORY_HIGH, "Много (> 100,000 кг)"}
            };
            CategorySelectBox.SetItems(categoryItems);

            // Выбираем первый элемент только если он есть
            if (CategorySelectBox.Items.Any())
            {
                CategorySelectBox.SelectedItem = categoryItems.First();
            }

            // Формат - инициализируем пустым списком, он заполнится после загрузки данных
            FormatSelectBox.SetItems(new Dictionary<string, string>
            {
                {"ALL", "Все форматы"}
            });

            // Выбираем первый элемент только если он есть
            if (FormatSelectBox.Items.Any())
            {
                FormatSelectBox.SelectedItem = FormatSelectBox.Items.First();
            }

            // Блокируем список форматов по умолчанию
            FormatSelectBox.IsEnabled = false;
        }

        // Обработчики событий
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshData();
            _selectedCategory = null;

            // Сбрасываем выбор категории
            if (CategorySelectBox.Items.Any())
            {
                CategorySelectBox.SelectedItem = CategorySelectBox.Items.First();
            }

            // Блокируем список форматов
            FormatSelectBox.IsEnabled = false;

            // Показываем начальное сообщение
            ShowInitialMessage();
        }

        private void PlatformSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RefreshData();
            _selectedCategory = null;

            // Сбрасываем выбор категории
            if (CategorySelectBox.Items.Any())
            {
                CategorySelectBox.SelectedItem = CategorySelectBox.Items.First();
            }

            // Блокируем список форматов
            FormatSelectBox.IsEnabled = false;

            // Показываем начальное сообщение
            ShowInitialMessage();
        }

        private void CategorySelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var selectedItem = CategorySelectBox.SelectedItem;
            if (!selectedItem.Equals(default(KeyValuePair<string, string>)))
            {
                _selectedCategory = selectedItem.Key;

                // Загружаем данные если еще не загружены
                if (!_isDataLoaded)
                {
                    RefreshData();
                }

                // Управляем блокировкой списка форматов
                if (_selectedCategory == CATEGORY_CRITICAL)
                {
                    // Для критических категорий блокируем список
                    FormatSelectBox.IsEnabled = false;

                    if (FormatSelectBox.Items.Any())
                    {
                        FormatSelectBox.SelectedItem = FormatSelectBox.Items.First();
                    }
                    _selectedFormat = "Все форматы";
                }
                else
                {
                    // Для всех остальных категорий (Все группы, Низкие, Много) разблокируем
                    FormatSelectBox.IsEnabled = true;
                }

                ApplyFilters();
                ShowCards();
            }
            else
            {
                // Если элемент не выбран, блокируем список форматов
                FormatSelectBox.IsEnabled = false;
            }
        }

        private void FormatSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var selectedItem = FormatSelectBox.SelectedItem;
            if (!selectedItem.Equals(default(KeyValuePair<string, string>)))
            {
                _selectedFormat = selectedItem.Value;
                if (_isDataLoaded && !string.IsNullOrEmpty(_selectedCategory))
                {
                    ApplyFilters();
                }
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isDataLoaded && !string.IsNullOrEmpty(_selectedCategory))
            {
                ApplyFilters();
            }
        }
    }
}