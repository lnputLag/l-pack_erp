using Client.Common;
using Client.Interfaces.Main;
using DevExpress.Xpf.Grid;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Отчёт по обороту продукции за период
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class ReportProductTurnover : ControlBase
    {
        public ReportProductTurnover()
        {
            ControlTitle = "Оборот продукции";
            RoleName = "[erp]warehouse_report";
            DocumentationUrl = "/doc/l-pack-erp/";
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
                FormInit();
                SetDefaults();
                ProductGridInit();
                PalletGridInit();
                SummaryGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                ProductGrid.Destruct();
                PalletGrid.Destruct();
                SummaryGrid.Destruct();
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
                        Refresh();
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

            Commander.SetCurrentGridName("ForkliftGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "export_to_excel",
                    Title = "В Excel",
                    Description = "Экспортировать в Excel",
                    Group = "forklift_grid_default",
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
                    Name = "pallet_export_to_excel",
                    Title = "В Excel",
                    Description = "Экспортировать в Excel",
                    Group = "pallet_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = PalletExportToExcelButton,
                    ButtonName = "PalletExportToExcelButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        PalletGrid.ItemsExportExcel();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (PalletGrid != null && PalletGrid.Items != null && PalletGrid.Items.Count > 0)
                        {
                            result = true;
                        }

                        return result;
                    },
                });
            }

            Commander.Init(this);
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Датасет с данными грида
        /// </summary>
        private ListDataSet ProductGridDataSet { get; set; }

        /// <summary>
        /// Датасет с данными грида
        /// </summary>
        private ListDataSet PalletGridDataSet { get; set; }

        private ListDataSet SummaryGridDataSet { get; set; }

        public int FactoryId = 1;

        private void FormInit()
        {
            //инициализация формы
            {
                Form = new FormHelper();

                //колонки формы
                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="SEARCH",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=SearchText,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="FROM_DATE",
                        FieldType=FormHelperField.FieldTypeRef.DateTime,
                        Control=FromDate,
                        ControlType="TextBox",
                        Format="dd.MM.yyyy",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        }
                    },
                    new FormHelperField()
                    {
                        Path="TO_DATE",
                        FieldType=FormHelperField.FieldTypeRef.DateTime,
                        Control=ToDate,
                        ControlType="TextBox",
                        Format="dd.MM.yyyy",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        }
                    },

                    new FormHelperField()
                    {
                        Path="PALLET_SEARCH",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=PalletSearchText,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                };

                Form.SetFields(fields);
            }
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            ProductGridDataSet = new ListDataSet();
            PalletGridDataSet = new ListDataSet();
            SummaryGridDataSet = new ListDataSet();

            Dictionary<string, string> productCategoryItems = new Dictionary<string, string>();
            productCategoryItems.Add("0", "Все типы продукции");
            productCategoryItems.Add("4", "Заготовки");
            productCategoryItems.Add("5", "Листы");
            productCategoryItems.Add("6", "Готовая продукция");
            ProductCategorySelectBox.Items = productCategoryItems;
            ProductCategorySelectBox.SetSelectedItemByKey("0");

            Dictionary<string, string> palletOperationZoneItems = new Dictionary<string, string>();
            palletOperationZoneItems.Add("0", "Все участки");
            palletOperationZoneItems.Add("1", "ГА");
            palletOperationZoneItems.Add("2", "Переработка");
            palletOperationZoneItems.Add("3", "СГП");
            PalletOperationZoneSelectBox.Items = palletOperationZoneItems;
            PalletOperationZoneSelectBox.SetSelectedItemByKey("0");

            Dictionary<string, string> palletOperationTypeItems = new Dictionary<string, string>();
            palletOperationTypeItems.Add("0", "Все операции");
            palletOperationTypeItems.Add("1", "Приход");
            palletOperationTypeItems.Add("2", "Расход");
            PalletOperationTypeSelectBox.Items = palletOperationTypeItems;
            PalletOperationTypeSelectBox.SetSelectedItemByKey("0");

            Form.SetValueByPath("FROM_DATE", DateTime.Now.ToString("dd.MM.yyyy"));
            Form.SetValueByPath("TO_DATE", DateTime.Now.ToString("dd.MM.yyyy"));
        }

        private void ProductGridInit()
        {
            //инициализация грида
            {
                //колонки грида
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
                        Header="На начало",
                        Description = "Суммарное количество на начало периода, шт.",
                        Path="BEFORE_QUANTITY",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2 = 10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Приход",
                        Description = "Суммарное количество в приходе за период, шт.",
                        Path="ARRIVAL_QUANTITY",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2 = 10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Расход",
                        Description = "Суммарное количество в расходе за период, шт.",
                        Path="CONSUMPTION_QUANTITY",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2 = 10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="На конец",
                        Description = "Суммарное количество на конец периода, шт.",
                        Path="QUANTITY",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2 = 10,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Потребитель",
                        Path = "CUSTOMER_NAME",
                        ColumnType = ColumnTypeRef.String,
                        Width2=24,
                    },

                    new DataGridHelperColumn
                    {
                        Header = "Ид категории продукции",
                        Path = "PRODUCT_CATEGORY_ID",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2 = 1,
                        Hidden = true,
                    },
                };
                ProductGrid.SetColumns(columns);
                ProductGrid.SearchText = SearchText;
                ProductGrid.Toolbar = GridToolbar;
                ProductGrid.OnLoadItems = ProductGridLoadItems;
                ProductGrid.SetPrimaryKey("PRODUCT_ID");
                ProductGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                ProductGrid.AutoUpdateInterval = 5 * 60;
                ProductGrid.OnFilterItems = () =>
                {
                    if (ProductGrid.Items != null && ProductGrid.Items.Count > 0)
                    {
                        // 0 -- Все Категории
                        if (ProductCategorySelectBox.SelectedItem.Key != null)
                        {
                            var key = ProductCategorySelectBox.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            switch (key)
                            {
                                // Все Категории 
                                case 0:
                                    items = ProductGrid.Items;
                                    break;

                                default:
                                    items.AddRange(ProductGrid.Items.Where(x => x.CheckGet("PRODUCT_CATEGORY_ID").ToInt() == key));
                                    break;
                            }

                            ProductGrid.Items = items;
                        }
                    }

                    SummaryGrid.LoadItems();
                };
                ProductGrid.OnSelectItem = (selectedItem) =>
                {
                    PalletGrid.LoadItems();
                };
                ProductGrid.Commands = Commander;
                ProductGrid.UseProgressSplashAuto = false;
                ProductGrid.Init();
            }
        }

        private async void ProductGridLoadItems()
        {
            bool resume = true;

            var f = Form.GetValueByPath("FROM_DATE").ToDateTime();
            var t = Form.GetValueByPath("TO_DATE").ToDateTime();

            if (DateTime.Compare(f, t) > 0)
            {
                const string msg = "Дата начала должна быть меньше даты окончания.";
                var d = new DialogWindow($"{msg}", this.ControlTitle);
                d.ShowDialog();
                resume = false;
            }

            if (resume)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    EnableSplash();
                });

                var p = new Dictionary<string, string>();
                p.Add("DT_FROM", Form.GetValueByPath("FROM_DATE"));
                p.Add("DT_TO", Form.GetValueByPath("TO_DATE"));
                p.Add("FACTORY_ID", $"{FactoryId}");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Stock");
                q.Request.SetParam("Object", "Report");
                q.Request.SetParam("Action", "ProductTurnover");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                ProductGridDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        ProductGridDataSet = ListDataSet.Create(result, "ITEMS");
                    }
                }
                else
                {
                    q.ProcessError();
                }
                ProductGrid.UpdateItems(ProductGridDataSet);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    DisableSplash();
                });
            }
        }

        private void PalletGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header = "#",
                        Path = "_ROWNUMBER",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2=4,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Ид поддона",
                        Path = "PALLET_ID",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Поддон",
                        Path = "PALLET_FULL_NUMBER",
                        ColumnType = ColumnTypeRef.String,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Дата",
                        Path = "OPERATION_DTTM",
                        ColumnType = ColumnTypeRef.DateTime,
                        Width2=14,
                        Format = "dd.MM.yyyy HH:mm:ss",
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Операция",
                        Path = "OPERATION_TYPE_NAME",
                        ColumnType = ColumnTypeRef.String,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Количество, шт.",
                        Path = "QUANTITY",
                        ColumnType = ColumnTypeRef.Double,
                        Width2=6,
                        Format="N0",
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Место",
                        Path = "CELL",
                        ColumnType = ColumnTypeRef.String,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Участок",
                        Path = "ZONE_NAME",
                        ColumnType = ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Пользователь",
                        Path = "USER_NAME",
                        ColumnType = ColumnTypeRef.String,
                        Width2=11,
                    },
                    
                    new DataGridHelperColumn
                    {
                        Header = "Ид прихода",
                        Path = "INCOMING_ID",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2=6,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Ид участка",
                        Path = "ZONE_ID",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2=6,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Ид типа операции",
                        Path = "OPERATION_TYPE_ID",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2=6,
                        Hidden = true,
                    },
                };
                PalletGrid.SetColumns(columns);
                PalletGrid.SearchText = PalletSearchText;
                PalletGrid.Toolbar = PalletGridToolbar;
                PalletGrid.OnLoadItems = PalletGridLoadItems;
                PalletGrid.SetPrimaryKey("_ROWNUMBER");
                PalletGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                PalletGrid.AutoUpdateInterval = 0;
                PalletGrid.ItemsAutoUpdate = false;
                PalletGrid.OnFilterItems = () =>
                {
                    if (PalletGrid.Items != null && PalletGrid.Items.Count > 0)
                    {
                        // 0 -- Все зоны
                        if (PalletOperationZoneSelectBox.SelectedItem.Key != null)
                        {
                            var key = PalletOperationZoneSelectBox.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            switch (key)
                            {
                                // Все зоны 
                                case 0:
                                    items = PalletGrid.Items;
                                    break;

                                default:
                                    items.AddRange(PalletGrid.Items.Where(x => x.CheckGet("ZONE_ID").ToInt() == key));
                                    break;
                            }

                            PalletGrid.Items = items;
                        }

                        // 0 -- Все типы операции
                        if (PalletOperationTypeSelectBox.SelectedItem.Key != null)
                        {
                            var key = PalletOperationTypeSelectBox.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            switch (key)
                            {
                                // Все типы операции 
                                case 0:
                                    items = PalletGrid.Items;
                                    break;

                                default:
                                    items.AddRange(PalletGrid.Items.Where(x => x.CheckGet("OPERATION_TYPE_ID").ToInt() == key));
                                    break;
                            }

                            PalletGrid.Items = items;
                        }
                    }
                };
                PalletGrid.Commands = Commander;
                PalletGrid.UseProgressSplashAuto = false;
                PalletGrid.Init();
                PalletGrid.Run();
            }
        }

        private async void PalletGridLoadItems()
        {
            PalletGridDataSet = new ListDataSet();

            if (ProductGrid.SelectedItem != null && !string.IsNullOrEmpty(ProductGrid.SelectedItem.CheckGet("PRODUCT_ID")))
            {
                var p = new Dictionary<string, string>();
                p.Add("PRODUCT_ID", ProductGrid.SelectedItem.CheckGet("PRODUCT_ID"));
                p.Add("DT_FROM", Form.GetValueByPath("FROM_DATE"));
                p.Add("DT_TO", Form.GetValueByPath("TO_DATE"));
                p.Add("FACTORY_ID", $"{FactoryId}");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Stock");
                q.Request.SetParam("Object", "Report");
                q.Request.SetParam("Action", "ReportProductTurnoverDetail");
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
                        PalletGridDataSet = ListDataSet.Create(result, "ITEMS");
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
            
            PalletGrid.UpdateItems(PalletGridDataSet);
        }

        private void SummaryGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header = "*",
                        Path = "NAME",
                        ColumnType = ColumnTypeRef.String,
                        Width2=64,
                    },
                    new DataGridHelperColumn
                    {
                        Header="На начало",
                        Description = "Суммарное количество на начало периода, шт.",
                        Path="BEFORE_QUANTITY",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2 = 10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Приход",
                        Description = "Суммарное количество в приходе за период, шт.",
                        Path="ARRIVAL_QUANTITY",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2 = 10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Расход",
                        Description = "Суммарное количество в расходе за период, шт.",
                        Path="CONSUMPTION_QUANTITY",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2 = 10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="На конец",
                        Description = "Суммарное количество на конец периода, шт.",
                        Path="QUANTITY",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2 = 10,
                    },
                };
                SummaryGrid.SetColumns(columns);
                SummaryGrid.OnLoadItems = SummaryGridLoadItems;
                SummaryGrid.SetPrimaryKey("NAME");
                SummaryGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                SummaryGrid.AutoUpdateInterval = 0;
                SummaryGrid.ItemsAutoUpdate = false;
                SummaryGrid.Commands = Commander;
                SummaryGrid.UseProgressSplashAuto = false;
                SummaryGrid.Init();
                SummaryGrid.Run();
            }
        }

        private async void SummaryGridLoadItems()
        {
            SummaryGridDataSet = new ListDataSet();

            if (ProductGrid.Items != null && ProductGrid.Items.Count > 0)
            {
                int beforeQuantity = 0;
                int arrivalQuantity = 0;
                int consumptionQuantity = 0;
                int quantity = 0;
                foreach (var item in ProductGrid.Items)
                {
                    beforeQuantity += item["BEFORE_QUANTITY"].ToInt();
                    arrivalQuantity += item["ARRIVAL_QUANTITY"].ToInt();
                    consumptionQuantity += item["CONSUMPTION_QUANTITY"].ToInt();
                    quantity += item["QUANTITY"].ToInt();
                }

                SummaryGridDataSet.Items = new List<Dictionary<string, string>>()
                {
                    new Dictionary<string, string>()
                    {
                        {"NAME", "Итого"},
                        {"BEFORE_QUANTITY", $"{beforeQuantity}"},
                        {"ARRIVAL_QUANTITY", $"{arrivalQuantity}"},
                        {"CONSUMPTION_QUANTITY", $"{consumptionQuantity}"},
                        {"QUANTITY", $"{quantity}"},
                    }
                };
            }

            SummaryGrid.UpdateItems(SummaryGridDataSet);
        }

        private void EnableSplash()
        {
            SplashControl.Message = $"Пожалуйста, подождите.{Environment.NewLine}Идёт загрузка данных.";
            SplashControl.Visible = true;
        }

        private void DisableSplash()
        {
            SplashControl.Message = "";
            SplashControl.Visible = false;
        }

        public void Refresh()
        {
            ProductGrid.LoadItems();
        }

        private void ProductCategorySelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ProductGrid.UpdateItems();
        }

        private void OnCurrentDay(object sender, RoutedEventArgs e)
        {
            Form.SetValueByPath("FROM_DATE", $"{DateTime.Now.ToString("dd.MM.yyyy")}");
            Form.SetValueByPath("TO_DATE", $"{DateTime.Now.ToString("dd.MM.yyyy")}");

            Refresh();
        }

        private void OnPrevDay(object sender, RoutedEventArgs e)
        {
            Form.SetValueByPath("FROM_DATE", $"{DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE", $"{DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void OnCurrentWeek(object sender, RoutedEventArgs e)
        {
            DayOfWeek day = DateTime.Now.DayOfWeek;
            int days = day - DayOfWeek.Monday;
            DateTime date = DateTime.Now.AddDays(-days);

            Form.SetValueByPath("FROM_DATE", $"{date.Date.ToString("dd.MM.yyyy")}");
            Form.SetValueByPath("TO_DATE", $"{date.Date.AddDays(6).ToString("dd.MM.yyyy")}");

            Refresh();
        }

        private void OnPrevWeek(object sender, RoutedEventArgs e)
        {
            DayOfWeek day = DateTime.Now.DayOfWeek;
            int days = day - DayOfWeek.Monday;
            DateTime date = DateTime.Now.AddDays(-days).AddDays(-7);

            Form.SetValueByPath("FROM_DATE", $"{date.Date.ToString("dd.MM.yyyy")}");
            Form.SetValueByPath("TO_DATE", $"{date.Date.AddDays(6).ToString("dd.MM.yyyy")}");

            Refresh();
        }

        private void OnCurrentMonth(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now;

            Form.SetValueByPath("FROM_DATE", $"{new DateTime(date.Year, date.Month, 1).ToString("dd.MM.yyyy")}");
            Form.SetValueByPath("TO_DATE", $"{new DateTime(date.Year, date.Month, 1).AddMonths(1).AddDays(-1).ToString("dd.MM.yyyy")}");

            Refresh();
        }

        private void OnPrevMonth(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now.AddMonths(-1);

            Form.SetValueByPath("FROM_DATE", $"{new DateTime(date.Year, date.Month, 1).ToString("dd.MM.yyyy")}");
            Form.SetValueByPath("TO_DATE", $"{new DateTime(date.Year, date.Month, 1).AddMonths(1).AddDays(-1).ToString("dd.MM.yyyy")}");

            Refresh();
        }

        private void PalletOperationZoneSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PalletGrid.UpdateItems();
        }

        private void PalletOperationTypeSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PalletGrid.UpdateItems();
        }
    }
}
