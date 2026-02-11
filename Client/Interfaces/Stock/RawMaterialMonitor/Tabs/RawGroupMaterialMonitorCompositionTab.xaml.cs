using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using DevExpress.Drawing.Internal.Fonts.Interop;
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
    /// Остатки на складе по сырьевым композициям
    /// </summary>
    /// <author>kurasov_dp</author>
    public partial class RawGroupMaterialMonitorCompositionTab : ControlBase
    {
        // Известные форматы (ширины)
        private List<int> _width = new List<int>
        {
            1600, 1900, 2000, 2100, 2200,
            2300, 2400, 2500, 2700, 2800
        };

        private List<int> _layer = new List<int>
        {
            1,2,3,4,5
        };

        public RawGroupMaterialMonitorCompositionTab()
        {
            InitializeComponent();

            RoleName = "[erp]raw_material_monitor";
            ControlTitle = "Монитор остатков сырья (композиции)";
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
                CompositionTableGridInit();
                SetDefaults();
            };

            OnUnload = () =>
            {
                CompositionTableGrid.Destruct();
            };

            OnFocusGot = () =>
            {
                CompositionTableGrid.ItemsAutoUpdate = true;
                CompositionTableGrid.Run();
            };

            OnFocusLost = () =>
            {
                CompositionTableGrid.ItemsAutoUpdate = false;
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
                            CompositionTableGrid.ItemsExportExcel();
                        },
                    });
                }
            }
            Commander.Init(this);
        }

        /// <summary>
        /// Настраивает Grid для отображения таблицы композиций
        /// </summary>

        private void CompositionTableGridInit()
        {
            var columns = new List<DataGridHelperColumn>();

            columns.Add(new DataGridHelperColumn
            {
                Hidden = true,
                Header = "",
                Path = "IDC",
                Description = "ИД композиции",
                ColumnType = ColumnTypeRef.Integer,
                Width2 = 8,
            });
            columns.Add(new DataGridHelperColumn
            {
                Header = "Композиция",
                Path = "CARTON_NAME",
                Description = "Наименование композиции",
                ColumnType = ColumnTypeRef.String,
                Width2 = 25,
            });
           

            foreach(var layer in _layer)
            {
                columns.Add(new DataGridHelperColumn
                {
                    Header = "Слой",
                    Path = $"LAYER_NUMBER_{layer}",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 8,
                });
                columns.Add(new DataGridHelperColumn
                {
                    Header = "Сырьевая группа",
                    Path = $"RAW_GROUP_{layer}",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 8,
                });
                foreach(var format in _width)
                {
                    columns.Add(new DataGridHelperColumn
                    {
                        Header = "Формат",
                        Path = $"WIDTH_{layer}_{format}",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2 = 8,
                    });
                    columns.Add(new DataGridHelperColumn
                    {
                        Header = "Остаток",
                        Path = $"STOCK_KG_{layer}_{format}",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2 = 8,
                    });
                }
            }

            //// Добавление колонки для каждого формата
            //foreach (var width in _width)
            //{
            //    columns.Add(new DataGridHelperColumn
            //    {
            //        Header = width.ToString(),
            //        Path = $"WIDTH_{width}",
            //        Description = $"Остаток по формату {width}",
            //        ColumnType = ColumnTypeRef.Integer, 
            //        Width2 = 8,
            //        Group = "Формат",
            //        Format = "N0", //  целые числа
            //        Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            //        {
            //            {
            //                StylerTypeRef.BackgroundColor,
            //                row =>
            //                {
            //                    var result = DependencyProperty.UnsetValue;
            //                    if (row.ContainsKey($"WIDTH_{width}"))
            //                    {
            //                        var qtyStr = row[$"WIDTH_{width}"].ToString();
            //                        if (TryParseNumber(qtyStr, out decimal qty) && qty == 0)
            //                        {
            //                            result = HColor.Red.ToBrush();
            //                        }
            //                    }
            //                    return result;
            //                }
            //            },
            //        }
            //    });
            //}

            // Добавление новой колонки с остатком
            columns.Add(new DataGridHelperColumn
            {
                Header = "Всего кг",
                Path = "TOTAL",
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
                            if (row.ContainsKey("TOTAL"))
                            {
                                var qtyStr = row["TOTAL"].ToString();
                                if (TryParseNumber(qtyStr, out decimal qty))
                                {
                                    if (qty == 0)
                                    {
                                        result = HColor.Red.ToBrush();
                                    }
                                }
                            }
                            return result;
                        }
                    },
                    {
                        StylerTypeRef.FontWeight,
                        row => FontWeights.Bold
                    },

                }
            });

            ///<summary>
            /// Привязка колонок и базовые настройки сетки
            ///</summary> 
            CompositionTableGrid.SetColumns(columns);
            CompositionTableGrid.SetPrimaryKey("IDC");
            CompositionTableGrid.SetSorting("CARTON_NAME", ListSortDirection.Ascending);
            CompositionTableGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

            ///<summary>
            /// Как загружать данные (запрос)
            ///</summary>
            CompositionTableGrid.QueryLoadItems = new RequestData()
            {
                Module = "Stock",
                Object = "RawMaterialResidueMonitor",
                Action = "RawCompositionList",
                AnswerSectionKey = "ITEMS",
                Timeout = 80000,
                BeforeRequest = (RequestData rd) =>
                {
                    rd.Params = new Dictionary<string, string>()
                    {
                        { "FACTORY_ID", PlatformSelectBox.SelectedItem.Key},
                    };
                },

                //AfterRequest = (RequestData rd, ListDataSet ds) =>
                //{
                //    if (ds != null && ds.Items != null)
                //    {
                //        // Группируем данные по IDC, слою и сырьевой группе
                //        var groupedData = ds.Items
                //            .GroupBy(item => new
                //            {
                //                Idc = item["IDC"],
                //                CartonName = item["CARTON_NAME"],
                //                LayerNumber = item.ContainsKey("LAYER_NUMBER") ? item["LAYER_NUMBER"] : "1",
                //                RawGroup = item.ContainsKey("RAW_GROUP") ? item["RAW_GROUP"] : ""
                //            })
                //            .Select(group =>
                //            {
                //                var row = new Dictionary<string, string>
                //                {
                //                    ["IDC"] = group.Key.Idc,
                //                    ["CARTON_NAME"] = group.Key.CartonName,
                //                    ["LAYER_NUMBER"] = group.Key.LayerNumber,
                //                    ["RAW_GROUP"] = group.Key.RawGroup
                //                };

                //                decimal totalSum = 0; // Используем decimal для точности

                //                // Заполняем все форматы
                //                foreach (var width in _width)
                //                {
                //                    var formatKey = $"WIDTH_{width}";

                //                    // Ищем данные для этой ширины в текущей группе
                //                    var formatItem = group.FirstOrDefault(g =>
                //                        g.ContainsKey("WIDTH") &&
                //                        g["WIDTH"] == width.ToString());

                //                    var quantity = formatItem != null && formatItem.ContainsKey("QTY_STOCK_ONLY")
                //                        ? formatItem["QTY_STOCK_ONLY"]
                //                        : (formatItem != null && formatItem.ContainsKey("STOCK_KG")
                //                            ? formatItem["STOCK_KG"]
                //                            : "0");

                //                    // Обрабатываем значение
                //                    if (TryParseNumber(quantity, out decimal qty))
                //                    {
                //                        totalSum += qty;
                //                        // Сохраняем значение для стилизации
                //                        row[formatKey] = qty.ToString(CultureInfo.InvariantCulture);
                //                    }
                //                    else
                //                    {
                //                        row[formatKey] = "0";
                //                    }
                //                }

                //                // Добавляем колонку с общей суммой
                //                row["TOTAL"] = totalSum.ToString(CultureInfo.InvariantCulture);

                //                return row;
                //            })
                //            .OrderBy(r => r["CARTON_NAME"])
                //            .ThenBy(r => r["LAYER_NUMBER"])
                //            .ThenBy(r => r["RAW_GROUP"])
                //            .ToList();

                //        ds.Items = groupedData;
                //    }

                //    return ds;
                //}
            };

            ///<summary>
            /// Команды и инициализация
            ///</summary>
            CompositionTableGrid.Commands = Commander;
            CompositionTableGrid.Init();
        }

        /// <summary>
        /// Парсит число, которое может быть целым или десятичным, с разделителями
        /// </summary>
        private bool TryParseNumber(string value, out decimal result)
        {
            result = 0;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            // Сначала пробуем стандартный парсинг
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out result))
                return true;

            // Пробуем с инвариантной культурой
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                return true;

            // Очищаем строку от всех символов, кроме цифр, точки, запятой и минуса
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
                    // Заменяем на точку как десятичный разделитель
                    cleanValue.Append('.');
                    hasDecimalSeparator = true;
                }
                else if (c == '-' && cleanValue.Length == 0)
                {
                    cleanValue.Append(c);
                }
                // Игнорируем пробелы и другие разделители тысяч
            }

            if (cleanValue.Length == 0)
                return false;

            return decimal.TryParse(cleanValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
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

        private void PlatformSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CompositionTableGrid.LoadItems();
        }
    }
}
