using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    //    /// Настраивает Grid для отображения списка изделий на складе.
    //    /// Отображает колонки, оформление строк(цвет)
    //    /// </summary>
    //    private void RawGroupTableGridInit()
    //    {
    //        var columns = new List<DataGridHelperColumn>
    //        {
    //            new DataGridHelperColumn
    //            {
    //                Hidden= true,
    //                Header="",
    //                Path="ID_RAW_GROUP",
    //                Description="ИД сырьевой группы",
    //                ColumnType=ColumnTypeRef.Integer,
    //                Width2=12,
    //            },
    //            new DataGridHelperColumn
    //            {
    //                Header="Сырьевая группа",
    //                Path="NAME",
    //                Description="Наименование сырьевой группы",
    //                ColumnType=ColumnTypeRef.String,
    //                Width2=14,
    //            },
    //            new DataGridHelperColumn
    //            {
    //                Header="Формат",
    //                Path="FORMAT",
    //                Description="Формат бумаги/картона",
    //                ColumnType=ColumnTypeRef.String,
    //                Width2=10,
    //            },
    //            new DataGridHelperColumn
    //            {
    //                Header="Остаток",
    //                Path="QTY_STOCK_ONLY",
    //                Description="Остаток на складе по сырьевой группе",
    //                ColumnType=ColumnTypeRef.Integer,
    //                Width2=10,
    //                Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
    //                {
    //                        {
    //                            StylerTypeRef.BackgroundColor,
    //                            row =>
    //                            {
    //                                var result=DependencyProperty.UnsetValue;
    //                                var color = "";

    //                                   if (row["QTY_STOCK_ONLY"].ToInt() == 0)
    //                                    {
    //                                        color = HColor.Red;
    //                                    }

    //                                    if (!string.IsNullOrEmpty(color))
    //                                    {
    //                                    result=color.ToBrush();
    //                                    }

    //                                return result;
    //                            }
    //                        },
    //                },
    //            },
    //        };

    //        ///<summary>
    //        /// Привязка колонок и базовые настройки сетки
    //        ///</summary> 
    //        RawGroupTableGrid.SetColumns(columns);
    //        RawGroupTableGrid.SetPrimaryKey("_ROWNUMBER");
    //        RawGroupTableGrid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
    //        RawGroupTableGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
    //        //RawGroupTableGrid.Toolbar = RawGroupTableGridToolbar;

    //        ///<summary>
    //        /// Как загружать данные (запрос)
    //        ///</summary>
    //        RawGroupTableGrid.QueryLoadItems = new RequestData()
    //        {
    //            Module = "Stock",
    //            Object = "RawMaterialResidueMonitor",
    //            Action = "RawGroupList",
    //            AnswerSectionKey = "ITEMS", // имя ключа для ответа от сервера
    //            Timeout = 60000,
    //            BeforeRequest = (RequestData rd) =>
    //            {
    //                rd.Params = new Dictionary<string, string>()
    //                        {
    //                            { "FACTORY_ID", PlatformSelectBox.SelectedItem.Key},

    //                        };
    //            },

    //            AfterRequest = (RequestData rd, ListDataSet ds) =>
    //            {
    //                if (rd.AnswerData.ContainsKey("FORMATS"))
    //                {
    //                    var formats = rd.AnswerData["FORMATS"];
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

    //    private void FormatSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    //    {

    //        RawGroupTableGrid.LoadItems();
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

            OnKeyPressed = (System.Windows.Input.KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };

            OnLoad = () =>
            {
                RawGroupTableGridInitAlternative();
                //RawGroupTableGridInit();
                SetDefaults();
            };

            OnUnload = () =>
            {
                RawGroupTableGrid.Destruct();
            };

            OnFocusGot = () =>
            {
                RawGroupTableGrid.ItemsAutoUpdate = true;
                RawGroupTableGrid.Run();
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
                }
            }
            Commander.Init(this);
        }

        /// <summary>
        /// Преобразует данные для отображения в 3 колонках
        /// Каждая строка - это сырьевая группа + конкретный формат
        /// </summary>
        private List<Dictionary<string, string>> TransformDataForDisplay(List<Dictionary<string, string>> originalData)
        {
            var result = new List<Dictionary<string, string>>();

            // Просто используем оригинальные данные, но можем их отсортировать
            var sortedData = originalData
                .OrderBy(item => item["NAME"])
                .ThenBy(item => item["FORMAT"])
                .ToList();

            // Если в данных меньше строк, чем нужно, добавляем недостающие
            foreach (var item in sortedData)
            {
                var row = new Dictionary<string, string>
                {
                    ["ID_RAW_GROUP"] = item.ContainsKey("ID_RAW_GROUP") ? item["ID_RAW_GROUP"] : "",
                    ["NAME"] = item.ContainsKey("NAME") ? item["NAME"] : "",
                    ["FORMAT"] = item.ContainsKey("FORMAT") ? item["FORMAT"] : "",
                    ["QTY_STOCK_ONLY"] = item.ContainsKey("QTY_STOCK_ONLY") ? item["QTY_STOCK_ONLY"] : "0"
                };
                result.Add(row);
            }

            return result;
        }

        /// <summary>
        /// Настраивает Grid для отображения списка изделий на складе.
        /// </summary>
        private void RawGroupTableGridInit()
        {
            // Создаем фиксированные колонки - ровно 3, как в оригинале
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Hidden = true,
                    Header = "",
                    Path = "ID_RAW_GROUP",
                    Description = "ИД сырьевой группы",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 12,
                },
                new DataGridHelperColumn
                {
                    Header = "Сырьевая группа",
                    Path = "NAME",
                    Description = "Наименование сырьевой группы",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 20,
                },
                new DataGridHelperColumn
                {
                    Header = "Формат",
                    Path = "FORMAT",
                    Description = "Формат бумаги/картона",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 15,
                },
                new DataGridHelperColumn
                {
                    Header = "Остаток",
                    Path = "QTY_STOCK_ONLY",
                    Description = "Остаток на складе по сырьевой группе",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 15,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                if (row.ContainsKey("QTY_STOCK_ONLY"))
                                {
                                    var qtyStr = row["QTY_STOCK_ONLY"].ToString();
                                    if (int.TryParse(qtyStr, out int qty) && qty == 0)
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

            ///<summary>
            /// Привязка колонок и базовые настройки сетки
            ///</summary> 
            RawGroupTableGrid.SetColumns(columns);
            RawGroupTableGrid.SetPrimaryKey("ID_RAW_GROUP");
            RawGroupTableGrid.SetSorting("NAME", ListSortDirection.Ascending);
            RawGroupTableGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

            ///<summary>
            /// Как загружать данные (запрос)
            ///</summary>
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
                        { "FACTORY_ID", PlatformSelectBox.SelectedItem.Key},
                    };
                },

                AfterRequest = (RequestData rd, ListDataSet ds) =>
                {
                    if (ds != null && ds.Items != null)
                    {
                        // Преобразуем данные для отображения
                        ds.Items = TransformDataForDisplay(ds.Items);
                    }

                    return ds;
                }
            };

            ///<summary>
            /// Команды и инициализация
            ///</summary>
            RawGroupTableGrid.Commands = Commander;
            RawGroupTableGrid.Init();
        }

        /// <summary>
        /// Второй вариант: группировка по сырьевым группам
        /// Каждая сырьевая группа - одна строка, формат в заголовке колонки
        /// </summary>
        private void RawGroupTableGridInitAlternative()
        {
            // Создаем фиксированные колонки
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Hidden = true,
                    Header = "",
                    Path = "ID_RAW_GROUP",
                    Description = "ИД сырьевой группы",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 12,
                },
                new DataGridHelperColumn
                {
                    Header = "Сырьевая группа",
                    Path = "NAME",
                    Description = "Наименование сырьевой группы",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 20,
                },
                // Статические колонки для каждого известного формата
                new DataGridHelperColumn
                {
                    Header = "1600",
                    Path = "FORMAT_1600",
                    Description = "Остаток по формату 1600",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 8,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                if (row.ContainsKey("FORMAT_1600"))
                                {
                                    var qtyStr = row["FORMAT_1600"].ToString();
                                    if (int.TryParse(qtyStr, out int qty) && qty == 0)
                                    {
                                        result = HColor.Red.ToBrush();
                                    }
                                }
                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "1900",
                    Path = "FORMAT_1900",
                    Description = "Остаток по формату 1900",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 8,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                if (row.ContainsKey("FORMAT_1900"))
                                {
                                    var qtyStr = row["FORMAT_1900"].ToString();
                                    if (int.TryParse(qtyStr, out int qty) && qty == 0)
                                    {
                                        result = HColor.Red.ToBrush();
                                    }
                                }
                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "2000",
                    Path = "FORMAT_2000",
                    Description = "Остаток по формату 2000",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 8,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                if (row.ContainsKey("FORMAT_2000"))
                                {
                                    var qtyStr = row["FORMAT_2000"].ToString();
                                    if (int.TryParse(qtyStr, out int qty) && qty == 0)
                                    {
                                        result = HColor.Red.ToBrush();
                                    }
                                }
                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "2100",
                    Path = "FORMAT_2100",
                    Description = "Остаток по формату 2100",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 8,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                if (row.ContainsKey("FORMAT_2100"))
                                {
                                    var qtyStr = row["FORMAT_2100"].ToString();
                                    if (int.TryParse(qtyStr, out int qty) && qty == 0)
                                    {
                                        result = HColor.Red.ToBrush();
                                    }
                                }
                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "2200",
                    Path = "FORMAT_2200",
                    Description = "Остаток по формату 2200",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 8,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                if (row.ContainsKey("FORMAT_2200"))
                                {
                                    var qtyStr = row["FORMAT_2200"].ToString();
                                    if (int.TryParse(qtyStr, out int qty) && qty == 0)
                                    {
                                        result = HColor.Red.ToBrush();
                                    }
                                }
                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "2300",
                    Path = "FORMAT_2300",
                    Description = "Остаток по формату 2300",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 8,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                if (row.ContainsKey("FORMAT_2300"))
                                {
                                    var qtyStr = row["FORMAT_2300"].ToString();
                                    if (int.TryParse(qtyStr, out int qty) && qty == 0)
                                    {
                                        result = HColor.Red.ToBrush();
                                    }
                                }
                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "2400",
                    Path = "FORMAT_2400",
                    Description = "Остаток по формату 2400",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 8,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                if (row.ContainsKey("FORMAT_2400"))
                                {
                                    var qtyStr = row["FORMAT_2400"].ToString();
                                    if (int.TryParse(qtyStr, out int qty) && qty == 0)
                                    {
                                        result = HColor.Red.ToBrush();
                                    }
                                }
                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "2500",
                    Path = "FORMAT_2500",
                    Description = "Остаток по формату 2500",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 8,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                if (row.ContainsKey("FORMAT_2500"))
                                {
                                    var qtyStr = row["FORMAT_2500"].ToString();
                                    if (int.TryParse(qtyStr, out int qty) && qty == 0)
                                    {
                                        result = HColor.Red.ToBrush();
                                    }
                                }
                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "2700",
                    Path = "FORMAT_2700",
                    Description = "Остаток по формату 2700",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 8,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                if (row.ContainsKey("FORMAT_2700"))
                                {
                                    var qtyStr = row["FORMAT_2700"].ToString();
                                    if (int.TryParse(qtyStr, out int qty) && qty == 0)
                                    {
                                        result = HColor.Red.ToBrush();
                                    }
                                }
                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "2800",
                    Path = "FORMAT_2800",
                    Description = "Остаток по формату 2800",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 8,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                if (row.ContainsKey("FORMAT_2800"))
                                {
                                    var qtyStr = row["FORMAT_2800"].ToString();
                                    if (int.TryParse(qtyStr, out int qty) && qty == 0)
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

            ///<summary>
            /// Привязка колонок
            ///</summary> 
            RawGroupTableGrid.SetColumns(columns);
            RawGroupTableGrid.SetPrimaryKey("ID_RAW_GROUP");
            RawGroupTableGrid.SetSorting("NAME", ListSortDirection.Ascending);
            RawGroupTableGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

            ///<summary>
            /// Как загружать данные (запрос) для второго варианта
            ///</summary>
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
                        { "FACTORY_ID", PlatformSelectBox.SelectedItem.Key},
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
                                Id = item["ID_RAW_GROUP"],
                                Name = item["NAME"]
                            })
                            .Select(group =>
                            {
                                var row = new Dictionary<string, string>
                                {
                                    ["ID_RAW_GROUP"] = group.Key.Id,
                                    ["NAME"] = group.Key.Name
                                };

                                // Заполняем все форматы
                                foreach (var format in _formats)
                                {
                                    var formatKey = $"FORMAT_{format}";
                                    var formatItem = group.FirstOrDefault(g => g["FORMAT"] == format);
                                    row[formatKey] = formatItem != null && formatItem.ContainsKey("QTY_STOCK_ONLY")
                                        ? formatItem["QTY_STOCK_ONLY"]
                                        : "0";
                                }

                                return row;
                            })
                            .ToList();

                        ds.Items = groupedData;
                    }

                    return ds;
                }
            };

            ///<summary>
            /// Команды и инициализация
            ///</summary>
            RawGroupTableGrid.Commands = Commander;
            RawGroupTableGrid.Init();
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
            RawGroupTableGrid.LoadItems();
        }
    }
}
