using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Остаток по сырьевым группам на складе
    /// </summary>
    /// <author>kurasovdp</author>
    //public partial class RawGroupMaterialMonitorTab : ControlBase
    //{
    //    // Известные форматы (заполняем из данных)
    //    private List<string> _formats = new List<string>
    //    {
    //        "1600", "1900", "2000", "2100", "2200",
    //        "2300", "2400", "2500", "2700", "2800"
    //    };

    //    // Константа для названия колонки с итогом
    //    private const string TOTAL_COLUMN_NAME = "TOTAL";

    //    public RawGroupMaterialMonitorTab()
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
    //            RawGroupTableGridInit();
    //            SetDefaults();
    //        };

    //        OnUnload = () =>
    //        {
    //            RawGroupTableGrid.Destruct();
    //        };

    //        OnFocusGot = () =>
    //        {
    //            RawGroupTableGrid.ItemsAutoUpdate = true;
    //            RawGroupTableGrid.Run();
    //        };

    //        OnFocusLost = () =>
    //        {
    //            RawGroupTableGrid.ItemsAutoUpdate = false;
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
    //                    Name = "export_to_excel",
    //                    Group = "main",
    //                    Enabled = true,
    //                    Title = "В Excel",
    //                    Description = "Выгрузить данные в Excel файл",
    //                    MenuUse = true,
    //                    ButtonUse = true,
    //                    ButtonName = "ExcelButton",
    //                    AccessLevel = Common.Role.AccessMode.ReadOnly,
    //                    Action = () =>
    //                    {
    //                        RawGroupTableGrid.ItemsExportExcel();
    //                    },
    //                });
    //            }
    //        }
    //        Commander.Init(this);
    //    }

    //    /// <summary>
    //    /// Преобразует данные для отображения в 3 колонках
    //    /// Каждая строка - это сырьевая группа + конкретный формат
    //    /// </summary>
    //    private List<Dictionary<string, string>> TransformDataForDisplay(List<Dictionary<string, string>> originalData)
    //    {
    //        var result = new List<Dictionary<string, string>>();

    //        // Просто используем оригинальные данные, но можем их отсортировать
    //        var sortedData = originalData
    //            .OrderBy(item => item["NAME"])
    //            .ThenBy(item => item["FORMAT"])
    //            .ToList();

    //        // Если в данных меньше строк, чем нужно, добавляем недостающие
    //        foreach (var item in sortedData)
    //        {
    //            var row = new Dictionary<string, string>
    //            {
    //                ["ID_RAW_GROUP"] = item.ContainsKey("ID_RAW_GROUP") ? item["ID_RAW_GROUP"] : "",
    //                ["NAME"] = item.ContainsKey("NAME") ? item["NAME"] : "",
    //                ["FORMAT"] = item.ContainsKey("FORMAT") ? item["FORMAT"] : "",
    //                ["QTY_STOCK_ONLY"] = item.ContainsKey("QTY_STOCK_ONLY") ? item["QTY_STOCK_ONLY"] : "0"
    //            };
    //            result.Add(row);
    //        }

    //        return result;
    //    }

    //    /// <summary>
    //    /// Второй вариант: группировка по сырьевым группам
    //    /// Каждая сырьевая группа - одна строка, формат в заголовке колонки
    //    /// </summary>

    //    private void RawGroupTableGridInit()
    //    {
    //        // Создаем список колонок
    //        var columns = new List<DataGridHelperColumn>();


    //        columns.Add(new DataGridHelperColumn
    //        {
    //            Hidden = true,
    //            Header = "",
    //            Path = "ID_RAW_GROUP",
    //            Description = "ИД сырьевой группы",
    //            ColumnType = ColumnTypeRef.Integer,
    //            Width2 = 12,
    //        });

    //        columns.Add(new DataGridHelperColumn
    //        {
    //            Header = "Сырьевая группа",
    //            Path = "NAME",
    //            Description = "Наименование сырьевой группы",
    //            ColumnType = ColumnTypeRef.String,
    //            Width2 = 20,
    //        });

    //        // Добавляем колонки для каждого формата
    //        foreach (var format in _formats)
    //        {
    //            columns.Add(new DataGridHelperColumn
    //            {
    //                Header = format,
    //                Path = $"FORMAT_{format}",
    //                Description = $"Остаток по формату {format}",
    //                ColumnType = ColumnTypeRef.Integer, 
    //                Width2 = 8,
    //                Group = "Формат",
    //                Format = "N0", // Отображаем как целые числа
    //                Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
    //                {
    //                    {
    //                        StylerTypeRef.BackgroundColor,
    //                        row =>
    //                        {
    //                            var result = DependencyProperty.UnsetValue;
    //                            if (row.ContainsKey($"FORMAT_{format}"))
    //                            {
    //                                var qtyStr = row[$"FORMAT_{format}"].ToString();
    //                                if (TryParseNumber(qtyStr, out decimal qty) && qty == 0)
    //                                {
    //                                    result = HColor.Red.ToBrush();
    //                                }
    //                            }
    //                            return result;
    //                        }
    //                    },

    //                }
    //            });
    //        }

    //        // ДОБАВЛЯЕМ НОВУЮ КОЛОНКУ С СУММОЙ
    //        columns.Add(new DataGridHelperColumn
    //        {
    //            Header = "Всего кг",
    //            Path = TOTAL_COLUMN_NAME,
    //            Description = "Общая сумма остатков по всем форматам",
    //            ColumnType = ColumnTypeRef.Integer,
    //            Width2 = 12,
    //            Format = "N0", // Форматирование с разделителями тысяч
    //            Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
    //            {
    //                {
    //                    StylerTypeRef.BackgroundColor,
    //                    row =>
    //                    {
    //                        var result = DependencyProperty.UnsetValue;
    //                        if (row.ContainsKey(TOTAL_COLUMN_NAME))
    //                        {
    //                            var qtyStr = row[TOTAL_COLUMN_NAME].ToString();
    //                            if (TryParseNumber(qtyStr, out decimal qty))
    //                            {
    //                                if (qty == 0)
    //                                {
    //                                    result = HColor.Red.ToBrush();
    //                                }
    //                            }
    //                        }
    //                        return result;
    //                    }
    //                },
    //                {
    //                    StylerTypeRef.FontWeight,
    //                    row => FontWeights.Bold
    //                },

    //            }
    //        });
    //        ///<summary>
    //        /// Привязка колонок
    //        ///</summary> 
    //        RawGroupTableGrid.SetColumns(columns);
    //        RawGroupTableGrid.SetPrimaryKey("ID_RAW_GROUP");
    //        RawGroupTableGrid.SetSorting("NAME", ListSortDirection.Ascending);
    //        RawGroupTableGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

    //        ///<summary>
    //        /// Как загружать данные (запрос) для второго варианта
    //        ///</summary>
    //        RawGroupTableGrid.QueryLoadItems = new RequestData()
    //        {
    //            Module = "Stock",
    //            Object = "RawMaterialResidueMonitor",
    //            Action = "RawGroupList",
    //            AnswerSectionKey = "ITEMS",
    //            Timeout = 60000,
    //            BeforeRequest = (RequestData rd) =>
    //            {
    //                rd.Params = new Dictionary<string, string>()
    //                {
    //                        { "FACTORY_ID", PlatformSelectBox.SelectedItem.Key},
    //                };
    //            },

    //            AfterRequest = (RequestData rd, ListDataSet ds) =>
    //            {
    //                if (ds != null && ds.Items != null)
    //                {
    //                    // Группируем данные по сырьевым группам
    //                    var groupedData = ds.Items
    //                        .GroupBy(item => new
    //                        {
    //                            Id = item["ID_RAW_GROUP"],
    //                            Name = item["NAME"]
    //                        })
    //                        .Select(group =>
    //                        {
    //                            var row = new Dictionary<string, string>
    //                            {
    //                                ["ID_RAW_GROUP"] = group.Key.Id,
    //                                ["NAME"] = group.Key.Name
    //                            };

    //                            decimal totalSum = 0; // Используем decimal для точности

    //                            // Заполняем все форматы
    //                            foreach (var format in _formats)
    //                            {
    //                                var formatKey = $"FORMAT_{format}";
    //                                var formatItem = group.FirstOrDefault(g => g["FORMAT"] == format);

    //                                var quantity = formatItem != null && formatItem.ContainsKey("QTY_STOCK_ONLY")
    //                                    ? formatItem["QTY_STOCK_ONLY"]
    //                                    : "0";

    //                                // Обрабатываем значение
    //                                if (TryParseNumber(quantity, out decimal qty))
    //                                {
    //                                    totalSum += qty;
    //                                    // Для отображения: если 0, то "0", иначе форматируем
    //                                    row[formatKey] = qty == 0 ? "0" : Math.Round(qty).ToString("N0");
    //                                }
    //                                else
    //                                {
    //                                    row[formatKey] = "0";
    //                                }
    //                            }

    //                            // Добавляем колонку с общей суммой
    //                            // Если сумма 0, то "0", иначе форматируем
    //                            row[TOTAL_COLUMN_NAME] = totalSum == 0 ? "0" : Math.Round(totalSum).ToString("N0");

    //                            return row;
    //                        })
    //                        .ToList();

    //                    ds.Items = groupedData;
    //                }

    //                return ds;
    //            }
    //        };

    //        ///<summary>
    //        /// Команды и инициализация
    //        ///</summary>
    //        RawGroupTableGrid.Commands = Commander;
    //        RawGroupTableGrid.Init();
    //    }

    //    /// <summary>
    //    /// Парсит число, которое может быть целым или десятичным, с разделителями
    //    /// </summary>
    //    private bool TryParseNumber(string value, out decimal result)
    //    {
    //        result = 0;

    //        if (string.IsNullOrWhiteSpace(value))
    //            return false;

    //        // Сначала пробуем стандартный парсинг
    //        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out result))
    //            return true;

    //        // Пробуем с инвариантной культурой
    //        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
    //            return true;

    //        // Очищаем строку от всех символов, кроме цифр, точки, запятой и минуса
    //        StringBuilder cleanValue = new StringBuilder();
    //        bool hasDecimalSeparator = false;

    //        foreach (char c in value)
    //        {
    //            if (char.IsDigit(c))
    //            {
    //                cleanValue.Append(c);
    //            }
    //            else if ((c == '.' || c == ',') && !hasDecimalSeparator)
    //            {
    //                // Заменяем на точку как десятичный разделитель
    //                cleanValue.Append('.');
    //                hasDecimalSeparator = true;
    //            }
    //            else if (c == '-' && cleanValue.Length == 0)
    //            {
    //                cleanValue.Append(c);
    //            }
    //            // Игнорируем пробелы и другие разделители тысяч
    //        }

    //        if (cleanValue.Length == 0)
    //            return false;

    //        return decimal.TryParse(cleanValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
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

    //    private void PlatformSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    //    {
    //        RawGroupTableGrid.LoadItems();
    //    }
    //}

    //public partial class RawGroupMaterialMonitorTab : ControlBase
    //{
    //    // Известные форматы (заполняем из данных)
    //    private List<string> _formats = new List<string>
    //    {
    //        "1600", "1900", "2000", "2100", "2200",
    //        "2300", "2400", "2500", "2700", "2800"
    //    };

    //    // Константа для названия колонки с итогом
    //    private const string TOTAL_COLUMN_NAME = "TOTAL";

    //    // Храним текущую выбранную группу
    //    private string _currentSelectedRawGroupId = null;

    //    public RawGroupMaterialMonitorTab()
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
    //            RawGroupTableGridInit();
    //            DetailGridInit();
    //            SetDefaults();
    //        };

    //        OnUnload = () =>
    //        {
    //            RawGroupTableGrid.Destruct();
    //            DetailGrid.Destruct();
    //        };

    //        OnFocusGot = () =>
    //        {
    //            RawGroupTableGrid.ItemsAutoUpdate = true;
    //            RawGroupTableGrid.Run();
    //            DetailGrid.ItemsAutoUpdate = false;
    //        };

    //        OnFocusLost = () =>
    //        {
    //            RawGroupTableGrid.ItemsAutoUpdate = false;
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
    //                    Name = "export_to_excel",
    //                    Group = "main",
    //                    Enabled = true,
    //                    Title = "В Excel",
    //                    Description = "Выгрузить данные в Excel файл",
    //                    MenuUse = true,
    //                    ButtonUse = true,
    //                    ButtonName = "ExcelButton",
    //                    AccessLevel = Common.Role.AccessMode.ReadOnly,
    //                    Action = () =>
    //                    {
    //                        // Экспорт левой таблицы
    //                        RawGroupTableGrid.ItemsExportExcel();
    //                    },
    //                });
    //                Commander.Add(new CommandItem()
    //                {
    //                    Name = "export_detail_to_excel",
    //                    Group = "main",
    //                    Enabled = true,
    //                    Title = "Экспорт состава",
    //                    Description = "Выгрузить состав сырьевой группы в Excel",
    //                    MenuUse = true,
    //                    AccessLevel = Common.Role.AccessMode.ReadOnly,
    //                    Action = () =>
    //                    {
    //                        // Экспорт правой таблицы
    //                        DetailGrid.ItemsExportExcel();
    //                    },
    //                });
    //            }
    //        }
    //        Commander.Init(this);
    //    }

    //    /// <summary>
    //    /// Инициализация левой таблицы с сырьевыми группами
    //    /// </summary>
    //    private void RawGroupTableGridInit()
    //    {
    //        // Создаем список колонок
    //        var columns = new List<DataGridHelperColumn>();

    //        columns.Add(new DataGridHelperColumn
    //        {
    //            Hidden = true,
    //            Header = "",
    //            Path = "ID_RAW_GROUP",
    //            Description = "ИД сырьевой группы",
    //            ColumnType = ColumnTypeRef.Integer,
    //            Width2 = 12,
    //        });

    //        columns.Add(new DataGridHelperColumn
    //        {
    //            Header = "Сырьевая группа",
    //            Path = "NAME",
    //            Description = "Наименование сырьевой группы",
    //            ColumnType = ColumnTypeRef.String,
    //            Width2 = 25,
    //        });

    //        // Добавляем колонки для каждого формата
    //        foreach (var format in _formats)
    //        {
    //            columns.Add(new DataGridHelperColumn
    //            {
    //                Header = format,
    //                Path = $"FORMAT_{format}",
    //                Description = $"Остаток по формату {format}",
    //                ColumnType = ColumnTypeRef.Integer,
    //                Width2 = 8,
    //                Group = "Формат",
    //                Format = "N0",
    //                Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
    //                {
    //                    {
    //                        StylerTypeRef.BackgroundColor,
    //                        row =>
    //                        {
    //                            var result = DependencyProperty.UnsetValue;
    //                            if (row.ContainsKey($"FORMAT_{format}"))
    //                            {
    //                                var qtyStr = row[$"FORMAT_{format}"].ToString();
    //                                if (TryParseNumber(qtyStr, out decimal qty) && qty == 0)
    //                                {
    //                                    result = HColor.Red.ToBrush();
    //                                }
    //                            }
    //                            return result;
    //                        }
    //                    }
    //                }
    //            });
    //        }

    //        // Колонка с суммой
    //        columns.Add(new DataGridHelperColumn
    //        {
    //            Header = "Всего кг",
    //            Path = TOTAL_COLUMN_NAME,
    //            Description = "Общая сумма остатков по всем форматам",
    //            ColumnType = ColumnTypeRef.Integer,
    //            Width2 = 12,
    //            Format = "N0",
    //            Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
    //            {
    //                {
    //                    StylerTypeRef.BackgroundColor,
    //                    row =>
    //                    {
    //                        var result = DependencyProperty.UnsetValue;
    //                        if (row.ContainsKey(TOTAL_COLUMN_NAME))
    //                        {
    //                            var qtyStr = row[TOTAL_COLUMN_NAME].ToString();
    //                            if (TryParseNumber(qtyStr, out decimal qty) && qty == 0)
    //                            {
    //                                result = HColor.Red.ToBrush();
    //                            }
    //                        }
    //                        return result;
    //                    }
    //                },
    //                {
    //                    StylerTypeRef.FontWeight,
    //                    row => FontWeights.Bold
    //                }
    //            }
    //        });

    //        // Привязка колонок
    //        RawGroupTableGrid.SetColumns(columns);
    //        RawGroupTableGrid.SetPrimaryKey("ID_RAW_GROUP");
    //        RawGroupTableGrid.SetSorting("NAME", ListSortDirection.Ascending);
    //        RawGroupTableGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

    //        // Запрос для загрузки данных
    //        RawGroupTableGrid.QueryLoadItems = new RequestData()
    //        {
    //            Module = "Stock",
    //            Object = "RawMaterialResidueMonitor",
    //            Action = "RawGroupList",
    //            AnswerSectionKey = "ITEMS",
    //            Timeout = 60000,
    //            BeforeRequest = (RequestData rd) =>
    //            {
    //                rd.Params = new Dictionary<string, string>()
    //                {
    //                    { "FACTORY_ID", PlatformSelectBox.SelectedItem.Key },
    //                };
    //            },

    //            AfterRequest = (RequestData rd, ListDataSet ds) =>
    //            {
    //                if (ds != null && ds.Items != null)
    //                {
    //                    // Группируем данные по сырьевым группам
    //                    var groupedData = ds.Items
    //                        .GroupBy(item => new
    //                        {
    //                            Id = item.ContainsKey("ID_RAW_GROUP") ? item["ID_RAW_GROUP"] : "",
    //                            Name = item.ContainsKey("NAME") ? item["NAME"] : ""
    //                        })
    //                        .Where(group => !string.IsNullOrEmpty(group.Key.Id))
    //                        .Select(group =>
    //                        {
    //                            var row = new Dictionary<string, string>
    //                            {
    //                                ["ID_RAW_GROUP"] = group.Key.Id,
    //                                ["NAME"] = group.Key.Name
    //                            };

    //                            decimal totalSum = 0;

    //                            // Заполняем все форматы
    //                            foreach (var format in _formats)
    //                            {
    //                                var formatKey = $"FORMAT_{format}";
    //                                var formatItem = group.FirstOrDefault(g =>
    //                                    g.ContainsKey("FORMAT") && g["FORMAT"] == format);

    //                                var quantity = formatItem != null && formatItem.ContainsKey("QTY_STOCK_ONLY")
    //                                    ? formatItem["QTY_STOCK_ONLY"]
    //                                    : "0";

    //                                if (TryParseNumber(quantity, out decimal qty))
    //                                {
    //                                    totalSum += qty;
    //                                    row[formatKey] = qty == 0 ? "0" : Math.Round(qty).ToString("N0");
    //                                }
    //                                else
    //                                {
    //                                    row[formatKey] = "0";
    //                                }
    //                            }

    //                            row[TOTAL_COLUMN_NAME] = totalSum == 0 ? "0" : Math.Round(totalSum).ToString("N0");
    //                            return row;
    //                        })
    //                        .ToList();

    //                    ds.Items = groupedData;
    //                }
    //                return ds;
    //            }
    //        };

    //        // Подписываемся на событие выбора строки
    //        RawGroupTableGrid.OnSelectItem = (selectedRow) =>
    //        {
    //            if (selectedRow != null && selectedRow.ContainsKey("ID_RAW_GROUP") && selectedRow.ContainsKey("NAME"))
    //            {
    //                string rawGroupId = selectedRow["ID_RAW_GROUP"];

    //                // Загружаем детальную информацию
    //                _currentSelectedRawGroupId = rawGroupId;
    //                DetailGrid.LoadItems();
    //            }
    //        };

    //        RawGroupTableGrid.Commands = Commander;
    //        RawGroupTableGrid.Init();
    //    }

    //    /// <summary>
    //    /// Инициализация правой таблицы с детальной информацией
    //    /// </summary>
    //    private void DetailGridInit()
    //    {
    //        var columns = new List<DataGridHelperColumn>
    //        {
    //            new DataGridHelperColumn
    //            {
    //                Header = "Производитель",
    //                Path = "PROIZVODITEL",
    //                Description = "Наименование производителя",
    //                ColumnType = ColumnTypeRef.String,
    //                Width2 = 30,
    //                Group = "Основное"
    //            },
    //            new DataGridHelperColumn
    //            {
    //                Header = "Наименование товара",
    //                Path = "TOVAR_NAME",
    //                Description = "Наименование товара",
    //                ColumnType = ColumnTypeRef.String,
    //                Width2 = 50,
    //                Group = "Основное"
    //            },
    //            new DataGridHelperColumn
    //            {
    //                Header = "Формат",
    //                Path = "FORMAT",
    //                Description = "Формат бумаги",
    //                ColumnType = ColumnTypeRef.Integer,
    //                Width2 = 10,
    //                Group = "Основное"
    //            },
    //            new DataGridHelperColumn
    //            {
    //                Header = "Остаток, кг",
    //                Path = "QTY_STOCK",
    //                Description = "Текущий остаток на складе",
    //                ColumnType = ColumnTypeRef.Integer,
    //                Width2 = 15,
    //                Format = "N0",
    //                Group = "Остатки",
    //                Stylers = new Dictionary<StylerTypeRef, StylerDelegate>
    //                {
    //                    {
    //                        StylerTypeRef.BackgroundColor,
    //                        row =>
    //                        {
    //                            var result = DependencyProperty.UnsetValue;
    //                            if (row.ContainsKey("QTY_STOCK"))
    //                            {
    //                                if (TryParseNumber(row["QTY_STOCK"], out decimal qty) && qty == 0)
    //                                {
    //                                    result = HColor.Red.ToBrush();
    //                                }
    //                            }
    //                            return result;
    //                        }
    //                    }
    //                }
    //            }
    //        };

    //        DetailGrid.SetColumns(columns);
    //        DetailGrid.SetSorting("PROIZVODITEL", ListSortDirection.Ascending);
    //        DetailGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

    //        // Запрос для получения детальной информации
    //        DetailGrid.QueryLoadItems = new RequestData()
    //        {
    //            Module = "Stock",
    //            Object = "RawMaterialResidueMonitorGroupDetail",
    //            Action = "RawGroupDetail",
    //            AnswerSectionKey = "ITEMS",
    //            Timeout = 60000,
    //            BeforeRequest = (RequestData rd) =>
    //            {
    //                string factoryId = "1";
    //                if (PlatformSelectBox.SelectedItem.Key != null)
    //                {
    //                    factoryId = PlatformSelectBox.SelectedItem.Key;
    //                }

    //                rd.Params = new Dictionary<string, string>
    //                {
    //                    { "RAW_GROUP_ID", _currentSelectedRawGroupId ?? "" },
    //                    { "FACTORY_ID", factoryId }
    //                };
    //            },

    //            AfterRequest = (RequestData rd, ListDataSet ds) =>
    //            {
    //                // Можно добавить пост-обработку данных если нужно
    //                return ds;
    //            }
    //        };

    //        DetailGrid.Commands = Commander;
    //        DetailGrid.Init();

    //        // Изначально правая таблица пуста (не загружаем данные)
    //        // Данные загрузятся только при выборе строки в левой таблице
    //    }

    //    /// <summary>
    //    /// Парсит число, которое может быть целым или десятичным, с разделителями
    //    /// </summary>
    //    private bool TryParseNumber(string value, out decimal result)
    //    {
    //        result = 0;

    //        if (string.IsNullOrWhiteSpace(value))
    //            return false;

    //        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out result))
    //            return true;

    //        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
    //            return true;

    //        StringBuilder cleanValue = new StringBuilder();
    //        bool hasDecimalSeparator = false;

    //        foreach (char c in value)
    //        {
    //            if (char.IsDigit(c))
    //            {
    //                cleanValue.Append(c);
    //            }
    //            else if ((c == '.' || c == ',') && !hasDecimalSeparator)
    //            {
    //                cleanValue.Append('.');
    //                hasDecimalSeparator = true;
    //            }
    //            else if (c == '-' && cleanValue.Length == 0)
    //            {
    //                cleanValue.Append(c);
    //            }
    //        }

    //        if (cleanValue.Length == 0)
    //            return false;

    //        return decimal.TryParse(cleanValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    //    }

    //    public void SetDefaults()
    //    {
    //        var platforms = new Dictionary<string, string>()
    //        {
    //            {"1", "Липецк"},
    //            {"2", "Кашира"},
    //        };

    //        PlatformSelectBox.SetItems(platforms);

    //        if (platforms.Count > 0)
    //        {
    //            var firstItem = platforms.First();
    //            PlatformSelectBox.SelectedItem = new KeyValuePair<string, string>(firstItem.Key, firstItem.Value);
    //        }
    //    }

    //    private void PlatformSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    //    {
    //        // Очищаем правую таблицу при смене площадки
    //        _currentSelectedRawGroupId = null;

    //        // Перезагружаем левую таблицу
    //        RawGroupTableGrid.LoadItems();

    //        // Очищаем правую таблицу (сбрасываем запрос)
    //        DetailGrid.QueryLoadItems = new RequestData()
    //        {
    //            Module = "Stock",
    //            Object = "RawMaterialResidueMonitor",
    //            Action = "RawGroupDetail",
    //            AnswerSectionKey = "ITEMS",
    //            Timeout = 60000,
    //            BeforeRequest = (RequestData rd) =>
    //            {
    //                string factoryId = "1";
    //                if (PlatformSelectBox.SelectedItem.Key != null)
    //                {
    //                    factoryId = PlatformSelectBox.SelectedItem.Key;
    //                }

    //                rd.Params = new Dictionary<string, string>
    //                {
    //                    { "RAW_GROUP_ID", _currentSelectedRawGroupId ?? "" },
    //                    { "FACTORY_ID", factoryId }
    //                };
    //            }
    //        };

    //        // Загружаем пустые данные
    //        DetailGrid.LoadItems();
    //    }
    //}

    public partial class RawGroupMaterialMonitorTab : ControlBase
    {
        // Известные форматы (заполняем из данных)
        private List<string> _formats = new List<string>
        {
            "1600", "1900", "2000", "2100", "2200",
            "2300", "2400", "2500", "2700", "2800"
        };

        // Константа для названия колонки с итогом
        private const string TOTAL_COLUMN_NAME = "TOTAL";

        public RawGroupMaterialMonitorTab()
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

            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };

            OnLoad = () =>
            {
                RawGroupTableGridInit();
                DetailGridInit();
                SetDefaults();
            };

            OnUnload = () =>
            {
                RawGroupTableGrid.Destruct();
                DetailGrid.Destruct();
            };

            OnFocusGot = () =>
            {
                RawGroupTableGrid.ItemsAutoUpdate = true;
                RawGroupTableGrid.Run();
                DetailGrid.ItemsAutoUpdate = false;
            };

            OnFocusLost = () =>
            {
                RawGroupTableGrid.ItemsAutoUpdate = false;
            };

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
                        Name = "export_to_excel",
                        Group = "main",
                        Enabled = true,
                        Title = "В Excel",
                        Description = "Выгрузить данные в Excel файл",
                        MenuUse = true,
                        ButtonUse = true,
                        ButtonName = "ExcelButton",
                        AccessLevel = Common.Role.AccessMode.ReadOnly,
                        Action = () =>
                        {
                            RawGroupTableGrid.ItemsExportExcel();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "export_detail_to_excel",
                        Group = "main",
                        Enabled = true,
                        Title = "Экспорт состава",
                        Description = "Выгрузить состав сырьевой группы в Excel",
                        MenuUse = true,
                        AccessLevel = Common.Role.AccessMode.ReadOnly,
                        Action = () =>
                        {
                            DetailGrid.ItemsExportExcel();
                        },
                    });
                }
            }
            Commander.Init(this);
        }

        /// <summary>
        /// Инициализация левой таблицы с сырьевыми группами
        /// </summary>
        private void RawGroupTableGridInit()
        {
             // Создаем список колонок
    var columns = new List<DataGridHelperColumn>();

    columns.Add(new DataGridHelperColumn
    {
        Hidden = true,
        Header = "",
        Path = "ID_RAW_GROUP",
        Description = "ИД сырьевой группы",
        ColumnType = ColumnTypeRef.Integer,
        Width2 = 12,
    });

    columns.Add(new DataGridHelperColumn
    {
        Header = "Сырьевая группа",
        Path = "NAME",
        Description = "Наименование сырьевой группы",
        ColumnType = ColumnTypeRef.String,
        Width2 = 25,
    });

    // Добавляем колонки для каждого формата
    foreach (var format in _formats)
    {
        var column = new DataGridHelperColumn
        {
            Header = format,
            Path = $"FORMAT_{format}",
            Description = $"Остаток по формату {format}",
            ColumnType = ColumnTypeRef.Integer,
            Width2 = 8,
            Group = "Формат",
            Format = "N0", // Форматирование с разделителями тысяч, без десятичных знаков
            Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                {
                    StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result = DependencyProperty.UnsetValue;
                        if (row.ContainsKey($"FORMAT_{format}"))
                        {
                            var qtyStr = row[$"FORMAT_{format}"].ToString();
                            // Убираем пробелы/запятые перед парсингом
                            qtyStr = qtyStr.Replace(",", "").Replace(" ", "");
                            if (TryParseNumber(qtyStr, out decimal qty) && qty == 0)
                            {
                                result = HColor.Red.ToBrush();
                            }
                        }
                        return result;
                    }
                }
            }
        };
        
        // Добавляем конвертер для форматирования чисел
        column.Converter = new GridBox4DataConverter();
        column.Converter.Type = ColumnTypeRef.Integer;
        column.Converter.Format = "N0"; // Формат с разделителями тысяч
        column.Converter.Init();
        
        columns.Add(column);
    }

            // Колонка с суммой
            columns.Add(new DataGridHelperColumn
            {
                Header = "Всего кг",
                Path = TOTAL_COLUMN_NAME,
                Description = "Общая сумма остатков по всем форматам",
                ColumnType = ColumnTypeRef.Integer,
                Width2 = 12,
                Format = "N0",
                Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    {
                        StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result = DependencyProperty.UnsetValue;
                            if (row.ContainsKey(TOTAL_COLUMN_NAME))
                            {
                                var qtyStr = row[TOTAL_COLUMN_NAME].ToString();
                                if (TryParseNumber(qtyStr, out decimal qty) && qty == 0)
                                {
                                    result = HColor.Red.ToBrush();
                                }
                            }
                            return result;
                        }
                    },
                    {
                        StylerTypeRef.FontWeight,
                        row => FontWeights.Bold
                    }
                }
            });

            // Привязка колонок
            RawGroupTableGrid.SetColumns(columns);
            RawGroupTableGrid.SetPrimaryKey("ID_RAW_GROUP");
            RawGroupTableGrid.SetSorting("NAME", ListSortDirection.Ascending);
            RawGroupTableGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

            // Запрос для загрузки данных
            RawGroupTableGrid.QueryLoadItems = new RequestData()
            {
                Module = "Stock",
                Object = "RawMaterialResidueMonitor",
                Action = "RawGroupList",
                AnswerSectionKey = "ITEMS",
                Timeout = 60000,
                BeforeRequest = (RequestData rd) =>
                {
                    rd.Params = new Dictionary<string, string>()
                    {
                        { "FACTORY_ID", PlatformSelectBox.SelectedItem.Key },
                    };
                },
                AfterRequest = (RequestData rd, ListDataSet ds) =>
                {
                    if (ds != null && ds.Items != null)
                    {
                        // Группируем данные по сырьевым группам
                        var groupedData = ds.Items
                            .GroupBy(item => new
                            {
                                Id = item.ContainsKey("ID_RAW_GROUP") ? item["ID_RAW_GROUP"] : "",
                                Name = item.ContainsKey("NAME") ? item["NAME"] : ""
                            })
                            .Where(group => !string.IsNullOrEmpty(group.Key.Id))
                            .Select(group =>
                            {
                                var row = new Dictionary<string, string>
                                {
                                    ["ID_RAW_GROUP"] = group.Key.Id,
                                    ["NAME"] = group.Key.Name
                                };

                                decimal totalSum = 0;

                                // Заполняем все форматы
                                foreach (var format in _formats)
                                {
                                    var formatKey = $"FORMAT_{format}";
                                    var formatItem = group.FirstOrDefault(g =>
                                        g.ContainsKey("FORMAT") && g["FORMAT"] == format);

                                    var quantity = formatItem != null && formatItem.ContainsKey("QTY_STOCK_ONLY")
                                        ? formatItem["QTY_STOCK_ONLY"]
                                        : "0";

                                    if (TryParseNumber(quantity, out decimal qty))
                                    {
                                        totalSum += qty;
                                        row[formatKey] = qty == 0 ? "0" : Math.Round(qty).ToString("N0");
                                    }
                                    else
                                    {
                                        row[formatKey] = "0";
                                    }
                                }

                                row[TOTAL_COLUMN_NAME] = totalSum == 0 ? "0" : Math.Round(totalSum).ToString("N0");
                                return row;
                            })
                            .ToList();

                        ds.Items = groupedData;
                    }
                    return ds;
                }
            };

            // Подписываемся на событие выбора строки - загружаем детальную информацию
            RawGroupTableGrid.OnSelectItem = (selectedItem) =>
            {
                DetailGrid.LoadItems();
            };

            RawGroupTableGrid.Commands = Commander;
            RawGroupTableGrid.Init();
        }

        /// <summary>
        /// Инициализация правой таблицы с детальной информацией
        /// </summary>
        private void DetailGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "Производитель",
                    Path = "PROIZVODITEL",
                    Description = "Наименование производителя",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 30,
                    Group = "Основное"
                },
                new DataGridHelperColumn
                {
                    Header = "Наименование товара",
                    Path = "TOVAR_NAME",
                    Description = "Наименование товара",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 50,
                    Group = "Основное"
                },
                new DataGridHelperColumn
                {
                    Header = "Формат",
                    Path = "FORMAT",
                    Description = "Формат бумаги",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 10,
                    Group = "Основное"
                },
                new DataGridHelperColumn
                {
                    Header = "Остаток, кг",
                    Path = "QTY_STOCK",
                    Description = "Текущий остаток на складе",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 15,
                    Format = "N0",
                    Group = "Остатки",
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                if (row.ContainsKey("QTY_STOCK"))
                                {
                                    if (TryParseNumber(row["QTY_STOCK"], out decimal qty) && qty == 0)
                                    {
                                        result = HColor.Red.ToBrush();
                                    }
                                }
                                return result;
                            }
                        }
                    }
                }
            };

            DetailGrid.SetColumns(columns);
            DetailGrid.SetSorting("PROIZVODITEL", ListSortDirection.Ascending);
            DetailGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            DetailGrid.ItemsAutoUpdate = false;

            // Используем OnLoadItems как в примере с AccountTab
            DetailGrid.OnLoadItems = _DetailGridLoadItems;

            DetailGrid.Commands = Commander;
            DetailGrid.Init();
        }

        /// <summary>
        /// Асинхронная загрузка детальной информации
        /// </summary>
        public async void _DetailGridLoadItems()
        {
            bool resume = true;

            if (resume)
            {
                // Получаем ID выбранной сырьевой группы из левого грида
                string rawGroupId = RawGroupTableGrid.SelectedItem.CheckGet("ID_RAW_GROUP");

                if (string.IsNullOrEmpty(rawGroupId))
                {
                    // Если ничего не выбрано, показываем пустой грид
                    DetailGrid.UpdateItems(new ListDataSet());
                    return;
                }

                string factoryId = PlatformSelectBox.SelectedItem.Key ?? "1";

                var p = new Dictionary<string, string>();
                {
                    p.Add("RAW_GROUP_ID", rawGroupId);
                    p.Add("FACTORY_ID", factoryId);
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Stock");
                q.Request.SetParam("Object", "RawMaterialResidueMonitor");
                q.Request.SetParam("Action", "RawGroupDetail");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        DetailGrid.UpdateItems(ds);
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// Парсит число, которое может быть целым или десятичным, с разделителями
        /// </summary>
        private bool TryParseNumber(string value, out decimal result)
        {
            result = 0;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out result))
                return true;

            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                return true;

            StringBuilder cleanValue = new StringBuilder();
            bool hasDecimalSeparator = false;

            foreach (char c in value)
            {
                if (char.IsDigit(c))
                {
                    cleanValue.Append(c);
                }
                else if ((c == '.' || c == ',') && !hasDecimalSeparator)
                {
                    cleanValue.Append('.');
                    hasDecimalSeparator = true;
                }
                else if (c == '-' && cleanValue.Length == 0)
                {
                    cleanValue.Append(c);
                }
            }

            if (cleanValue.Length == 0)
                return false;

            return decimal.TryParse(cleanValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }

        public void SetDefaults()
        {
            var platforms = new Dictionary<string, string>()
            {
                {"1", "Липецк"},
                {"2", "Кашира"},
            };

            PlatformSelectBox.SetItems(platforms);

            if (platforms.Count > 0)
            {
                var firstItem = platforms.First();
                PlatformSelectBox.SelectedItem = new KeyValuePair<string, string>(firstItem.Key, firstItem.Value);
            }
        }

        private void PlatformSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RawGroupTableGrid.LoadItems();

            // Очищаем правую таблицу
            DetailGrid.UpdateItems(new ListDataSet());
        }
    }
}
