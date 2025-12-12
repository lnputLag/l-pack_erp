using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Text;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Client.Interfaces.Main.DataGridHelperColumn;
using System.Windows.Controls.Primitives;
using System.Printing;
using System.Windows.Input;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Отчёт Товары на СГП
    /// </summary>
    /// <author>sviridov_ae</author>
    public partial class ReportProduct : ControlBase
    {
        public ReportProduct()
        {
            ControlTitle = "Товары на СГП";
            DocumentationUrl = "/doc/l-pack-erp/warehouse/operations#block1";
            RoleName = "[erp]warehouse_report";
            InitializeComponent();

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

            //конструктор, будет вызван, когда объект создается
            //здесь создаются все внутренние структуры
            //впервые этот коллбэк будет вызван, когда данный таб станет активным
            //впервые (до этих пор, никакая работа внутри не происходит, что экономит ресурсы)
            OnLoad = () =>
            {
                SetDefaults();
                InitProductGrid();
                InitPalletGrid();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                ProductGrid.Destruct();
                PalletGrid.Destruct();      
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                ProductGrid.ItemsAutoUpdate = true;
                ProductGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                ProductGrid.ItemsAutoUpdate = false;
            };

            {
                Commander.Add(new CommandItem()
                {
                    Name = "refresh",
                    Group = "main",
                    Enabled = true,
                    Title = "Обновить",
                    Description = "Обновить данные",
                    ButtonUse = true,
                    ButtonControl = RefreshButton,
                    ButtonName = "RefreshButton",
                    Action = () =>
                    {
                        ProductGrid.LoadItems();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "report",
                    Group = "main",
                    Enabled = true,
                    Title = "Отчёты",
                    Description = "Отчёты",
                    ButtonUse = true,
                    ButtonControl = ReportButton,
                    ButtonName = "ReportButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        ReportBurgerMenu.IsOpen = true;
                    },
                });
                
                Commander.Add(new CommandItem()
                {
                    Name = "settings",
                    Group = "main",
                    Enabled = true,
                    Title = "Настройки",
                    Description = "Настройки",
                    ButtonUse = true,
                    ButtonControl = SettingsButton,
                    ButtonName = "SettingsButton",
                    Action = () =>
                    {
                        BurgerMenu.IsOpen = true;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "help",
                    Group = "main",
                    Enabled = true,
                    Title = "Справка",
                    Description = "Показать справочную информацию",
                    ButtonUse = true,
                    ButtonControl = HelpButton,
                    ButtonName = "HelpButton",
                    HotKey = "F1",
                    Action = () =>
                    {
                        Central.ShowHelp(DocumentationUrl);
                    },
                });
            }

            Commander.SetCurrentGridName("ProductGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "edit_inventory",
                    Title = "В Excel",
                    Description = "Экспортировать в Excel",
                    Group = "product_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = ExportToExcelButton,
                    ButtonName = "ExportToExcelButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        ProductGrid.ItemsExportExcel();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ProductGrid != null && ProductGrid.Items != null && ProductGrid.Items.Count > 0)
                        {
                            result = true;
                        }

                        return result;
                    },
                });
            }

            Commander.SetCurrentGridName("PalletGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "label_print",
                    Title = "Печать ярлыка",
                    Description = "Печать ярлыка для выбранного поддона",
                    Group = "pallet_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = LabelPrintButton,
                    ButtonName = "LabelPrintButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        PrintLabel();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (PalletGrid != null && PalletGrid.Items != null && PalletGrid.Items.Count > 0)
                        {
                            if (PalletGrid.SelectedItem != null && PalletGrid.SelectedItem.Count > 0)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "pallet_history",
                    Title = "История перемещения",
                    Description = "История перемещения для выбранного поддона",
                    Group = "pallet_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = PalletHistoryButton,
                    ButtonName = "PalletHistoryButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        PalletGetHistory();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (PalletGrid != null && PalletGrid.Items != null && PalletGrid.Items.Count > 0)
                        {
                            if (PalletGrid.SelectedItem != null && PalletGrid.SelectedItem.Count > 0)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
            }

            Commander.Init(this);

        }

        private int FactoryId = 1;

        private ListDataSet ProductDataSet { get; set; }

        private ListDataSet PalletDataSet { get; set; }

        private void SetDefaults()
        {
            ProductDataSet = new ListDataSet();
            PalletDataSet = new ListDataSet();
        }

        /// <summary>
        /// Инициализация грида товаров
        /// </summary>
        private void InitProductGrid()
        {
            var grid = ProductGrid;

            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "Ид",
                    Path = "PRODUCT_ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header = "Артикул",
                    Path = "PRODUCT_CODE",
                    ColumnType = ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn
                {
                    Header = "Наименование",
                    Path = "PRODUCT_NAME",
                    ColumnType = ColumnTypeRef.String,
                    Width2=43,
                },
                new DataGridHelperColumn
                {
                    Header = "Поддоны, шт",
                    Path = "PALLET_COUNT",
                    ColumnType = ColumnTypeRef.Double,
                    Width2=9,
                    Format="N0",
                },
                new DataGridHelperColumn
                {
                    Header = "Изделия, шт",
                    Path = "PRODUCT_QUANTITY",
                    ColumnType = ColumnTypeRef.Double,
                    Width2=9,
                    Format="N0",
                },
                new DataGridHelperColumn
                {
                    Header = "Площадь, м2",
                    Path = "SQUARE",
                    ColumnType = ColumnTypeRef.Double,
                    Format = "N2",
                    Width2=9,
                },
                new DataGridHelperColumn
                {
                    Header = "Срок хранения, дней",
                    Path = "DAYS_FROM_PRODUCTION",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2=13,
                },
            };
            grid.SetColumns(columns);
            grid.SetPrimaryKey("PRODUCT_ID");
            grid.SetSorting("PRODUCT_NAME");
            grid.SearchText = SearchText;
            grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            grid.Toolbar = ProductGridToolbar;
            grid.AutoUpdateInterval = 60 * 5;
            grid.OnSelectItem = selectedItem =>
            {
                LoadPalletItems();
            };
            grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>
            {
                {
                    StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result = DependencyProperty.UnsetValue;
                        var color = "";

                        if (row.CheckGet("DAYS_FROM_PRODUCTION").ToInt() > 10)
                        {
                            color = HColor.RedFG;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result = color.ToBrush();
                        }

                        return result;
                    }
                },
            };
            grid.OnLoadItems = LoadProductItems;
            grid.Commands = Commander;
            grid.Init();
        }

        /// <summary>
        /// Загрузка данных по товарам
        /// </summary>
        private async void LoadProductItems()
        {
            if (PalletGrid != null)
            {
                PalletGrid.ClearItems();
            }

            var grid = ProductGrid;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Product");
            q.Request.SetParam("Action", "List");
            q.Request.SetParam("FACTORY_ID", $"{FactoryId}");
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            ProductDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    ProductDataSet = ListDataSet.Create(result, "List");
                }
            }
            grid.UpdateItems(ProductDataSet);
        }

        /// <summary>
        /// Инициализация грида поддонов
        /// </summary>
        private void InitPalletGrid()
        {
            var grid = PalletGrid;

            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "Ид поддона",
                    Path = "PALLET_ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header = "№ поддона",
                    Path = "PALLET",
                    ColumnType = ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header = "Ячейка",
                    Path = "PLACE",
                    ColumnType = ColumnTypeRef.String,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество, шт.",
                    Path = "QUANTITY",
                    ColumnType = ColumnTypeRef.Double,
                    Width2=10,
                    Format="N0",
                },
                new DataGridHelperColumn
                {
                    Header = "Площадь, м2.",
                    Path = "SQUARE",
                    ColumnType = ColumnTypeRef.Double,
                    Width2=10,
                    Format="N2",
                },
                new DataGridHelperColumn
                {
                    Header = "Срок хранения, дней",
                    Path = "DAYS_FROM_PRODUCTION",
                    ColumnType = ColumnTypeRef.Double,
                    Width2=10,
                    Format="N0",
                },
                new DataGridHelperColumn
                {
                    Header = "Дата постановки",
                    Path = "MOVING_DTTM",
                    ColumnType = ColumnTypeRef.DateTime,
                    Width2=15,
                    Format="dd.MM.yyyy HH:mm:ss",
                },
                new DataGridHelperColumn
                {
                    Header = "Склад",
                    Path = "STORAGE_TYPE",
                    ColumnType = ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header = "Ид прихода",
                    Path = "INCOMING_ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2=8,
                },

                new DataGridHelperColumn
                {
                    Header = "Ид ПЗ",
                    Path = "PRODUCTION_TASK_ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2=5,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header = "Категория продукции",
                    Path = "PRODUCT_CATEGORY_ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2=5,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header = "Номер поддона",
                    Path = "PALLET_NUMBER",
                    ColumnType = ColumnTypeRef.String,
                    Width2=5,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header = "Дата начала кондиционирования",
                    Path = "CONDITION_DTTM",
                    ColumnType = ColumnTypeRef.String,
                    Width2=5,
                    Hidden=true,
                },
            };
            grid.SetColumns(columns);
            grid.SetPrimaryKey("PALLET_ID");
            grid.SetSorting("MOVING_DTTM");
            grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            grid.Toolbar = PalletGridToolbar;
            grid.AutoUpdateInterval = 0;
            grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>
            {
                {
                    StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result = DependencyProperty.UnsetValue;
                        var color = "";

                        // если срок хранения больше 10 дней
                        if (row.CheckGet("DAYS_FROM_PRODUCTION").ToInt() > 10)
                        {
                            color = HColor.RedFG;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };
            grid.OnLoadItems = LoadPalletItems;
            grid.Commands = Commander;
            grid.Init();
        }

        /// <summary>
        /// список паллет у выбранного продукта
        /// </summary>
        private async void LoadPalletItems()
        {
            if (ProductGrid.SelectedItem != null && ProductGrid.SelectedItem.Count > 0)
            {
                var grid = PalletGrid;

                var p = new Dictionary<string, string>
                {
                    ["PRODUCT_ID"] = ProductGrid.SelectedItem["PRODUCT_ID"],
                    ["FACTORY_ID"] = $"{FactoryId}",
                };

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Stock");
                q.Request.SetParam("Object", "Pallet");
                q.Request.SetParam("Action", "ListByProduct");

                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                PalletDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        PalletDataSet = ListDataSet.Create(result, "List");
                    }
                }
                grid.UpdateItems(PalletDataSet);
            }            
        }

        /// <summary>
        /// Срабатывает по правой кнопке мыши в выпадающем меню. Показывает историю перемещения поддона
        /// </summary>
        private async void PalletGetHistory()
        {
            var p = new Dictionary<string, string>
            {
                ["idp"] = PalletGrid.SelectedItem["INCOMING_ID"],
                ["num"] = PalletGrid.SelectedItem["PALLET_NUMBER"],
            };

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Pallet");
            q.Request.SetParam("Action", "ListHistory");

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
                    DialogWindow.ShowDialog(result["List"].Rows.Select(row => row[0]).Aggregate(string.Empty, (row, record) => row + record + "\n"), "История перемещения поддона");
                }
            }
        }

        /// <summary>
        /// Установка настроек для принтера
        /// </summary>
        public void SetPrintSettings()
        {
            LabelReport2.SetPrintingProfile();
        }

        /// <summary>
        /// Пучать ярлыка
        /// </summary>
        private async void PrintLabel()
        {
            LabelReport2 report = new LabelReport2(true);
            report.PrintLabel(PalletGrid.SelectedItem["PALLET_ID"]);
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings();
        }

        #region Reports

        /// <summary>
        /// Отчёт по заготовкам в буфере
        /// </summary>
        private async void ExportBlanksInBufferToExcel()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                EnableSplash();
            });

            await StockReporter.ReportBlankInBuffer(this.FactoryId);

            Application.Current.Dispatcher.Invoke(() =>
            {
                DisableSplash();
            });
        }

        /// <summary>
        /// Отчёт по продукции на кондиционировании в буфере
        /// </summary>
        private async void ExportConditioningProductsInBufferToExcel()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                EnableSplash();
            });

            await StockReporter.ReportProductConditioningInBuffer(this.FactoryId);

            Application.Current.Dispatcher.Invoke(() =>
            {
                DisableSplash();
            });
        }

        /// <summary>
        /// Отчёт паллеты на кондиционировании
        /// </summary>
        private async void ExportConditioningProductsListToExcel()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                EnableSplash();
            });

            await StockReporter.ReportPalletConditioning(this.FactoryId);

            Application.Current.Dispatcher.Invoke(() =>
            {
                DisableSplash();
            });
        }

        /// <summary>
        /// Отчёт по срокам хранения поддонов
        /// </summary>
        private async void OldListToExcel()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                EnableSplash();
            });

            await StockReporter.ReportPalletDayFromProduction(this.FactoryId);

            Application.Current.Dispatcher.Invoke(() =>
            {
                DisableSplash();
            });
        }

        /// <summary>
        /// Отчёт по товарам на складе
        /// </summary> 
        private async void ProductGridToExcel()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                EnableSplash();
            });

            await StockReporter.ReportProductInStock(this.FactoryId);

            Application.Current.Dispatcher.Invoke(() =>
            {
                DisableSplash();
            });
        }

        /// <summary>
        /// Отчёт по неликвидным товарам
        /// </summary>
        private async void PrintIlliquidProductList()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                EnableSplash();
            });

            await StockReporter.ReportProductIlliquid(this.FactoryId);

            Application.Current.Dispatcher.Invoke(() =>
            {
                DisableSplash();
            });
        }

        /// <summary>
        /// Отчёт по поддонам с выбранной продукцией
        /// </summary>
        private async void PrintPalletList()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                EnableSplash();
            });

            await StockReporter.ReportPalletByProduct(ProductGrid.SelectedItem["PRODUCT_ID"].ToInt(), this.FactoryId);

            Application.Current.Dispatcher.Invoke(() =>
            {
                DisableSplash();
            });
        }

        private async void PrintAllReport()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                EnableSplash();
            });

            await StockReporter.ReportBlankInBuffer(this.FactoryId);
            await StockReporter.ReportProductConditioningInBuffer(this.FactoryId);
            await StockReporter.ReportPalletConditioning(this.FactoryId);
            await StockReporter.ReportPalletDayFromProduction(this.FactoryId);
            await StockReporter.ReportProductInStock(this.FactoryId);

            Application.Current.Dispatcher.Invoke(() =>
            {
                DisableSplash();
            });
        }

        private void EnableSplash()
        {
            SplashControl.Message = $"Пожалуйста, подождите.{Environment.NewLine}Идёт формирование отчёта.";
            SplashControl.Visible = true;
        }

        private void DisableSplash()
        {
            SplashControl.Message = "";
            SplashControl.Visible = false;
        }

        private void BlanksInBufferButton_Click(object sender, RoutedEventArgs e)
        {
            ExportBlanksInBufferToExcel();
        }

        private void ConditioningProductsInBufferButton_Click(object sender, RoutedEventArgs e)
        {
            ExportConditioningProductsInBufferToExcel();
        }

        private void ConditioningProductsListButton_Click(object sender, RoutedEventArgs e)
        {
            ExportConditioningProductsListToExcel();
        }

        private void OldButton_Click(object sender, RoutedEventArgs e)
        {
            OldListToExcel();
        }

        private void ProductGridToExcel_Click(object sender, RoutedEventArgs e)
        {
            ProductGridToExcel();
        }

        private void PrintAllButton_Click(object sender, RoutedEventArgs e)
        {
            PrintAllReport();
        }

        private void PrintIlliquidProductListButton_Click(object sender, RoutedEventArgs e)
        {
            PrintIlliquidProductList();
        }

        private void PrintPalletListButton_Click(object sender, RoutedEventArgs e)
        {
            PrintPalletList();
        }

        #endregion
    }
}
