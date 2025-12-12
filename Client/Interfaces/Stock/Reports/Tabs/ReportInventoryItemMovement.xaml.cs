using Client.Common;
using Client.Interfaces.Main;
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
    /// Отчёт о движении ТМЦ
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class ReportInventoryItemMovement : ControlBase
    {
        public ReportInventoryItemMovement()
        {
            ControlTitle = "Движение ТМЦ";
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
                GridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                Grid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                Grid.ItemsAutoUpdate = true;
                Grid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                Grid.ItemsAutoUpdate = false;
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
            
            Commander.SetCurrentGridName("Grid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "export_to_excel",
                    Title = "В Excel",
                    Description = "Экспортировать в Excel",
                    Group = "grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = ExportToExcelButton,
                    ButtonName = "ExportToExcelButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        Grid.ItemsExportExcel();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (Grid != null && Grid.Items != null && Grid.Items.Count > 0)
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
        private ListDataSet GridDataSet { get; set; }

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
                        Path="FROM_DATE_TIME",
                        FieldType=FormHelperField.FieldTypeRef.DateTime,
                        Control=FromDateTime,
                        ControlType="dateedit",
                        Format="dd.MM.yyyy HH:mm:ss",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        }
                    },
                    new FormHelperField()
                    {
                        Path="TO_DATE_TIME",
                        FieldType=FormHelperField.FieldTypeRef.DateTime,
                        Control=ToDateTime,
                        ControlType="dateedit",
                        Format="dd.MM.yyyy HH:mm:ss",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
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
            GridDataSet = new ListDataSet();

            var operationSelectBoxItems = new Dictionary<string, string>();
            operationSelectBoxItems.Add("0", "Все операции");
            operationSelectBoxItems.Add("1", "Оприходование");
            operationSelectBoxItems.Add("2", "Перемещение");
            operationSelectBoxItems.Add("3", "Списание");
            OperationSelectBox.SetItems(operationSelectBoxItems);
            OperationSelectBox.SetSelectedItemByKey("0");

            ItemTypeSelectBox.Items.Add("0", "Все типы");
            FormHelper.ComboBoxInitHelper(ItemTypeSelectBox, "Warehouse", "ItemGroup", "List", "ID", "NAME", null, true);
            ItemTypeSelectBox.SelectedItem = new KeyValuePair<string, string>("0", "Все типы");

            WarehouseSelectBox.Items.Add("0", "Все склады");
            FormHelper.ComboBoxInitHelper(WarehouseSelectBox, "Warehouse", "Warehouse", "ListByFactory", "WMWA_ID", "WAREHOUSE", new Dictionary<string, string>() { { "FACTORY_ID", $"{FactoryId}"} }, true);
            WarehouseSelectBox.SelectedItem = new KeyValuePair<string, string>("0", "Все склады");

            var date = DateTime.Now;
            if (date.Hour >= 20 || date.Hour < 8)
            {
                if (date.Hour >= 20)
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.AddDays(1).ToString("dd.MM.yyyy")} 08:00:00");
                }
                else
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 08:00:00");
                }
            }
            else
            {
                Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 08:00:00");
                Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 20:00:00");
            }
        }

        private void GridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="#",
                        Description = "Порядковый номер записи",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата",
                        Description = "Дата операции",
                        Path="OPERATION_DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2=14,
                        Format="dd.MM.yyyy HH:mm:ss",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид ТМЦ",
                        Description = "Идентификатор ТМЦ",
                        Path="ITEM_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Description = "Наименование ТМЦ",
                        Path="ITEM_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=62,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Операция",
                        Description = "Наименование операции",
                        Path="OPERATION_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Из",
                        Description = "Наименование хранилища источника",
                        Path="STORAGE_FROM_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="В",
                        Description = "Наименование хранилища назначения",
                        Path="STORAGE_TO_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Зона",
                        Description = "Наименование зоны",
                        Path="ZONE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=19,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Склад",
                        Description = "Наименование склада",
                        Path="WAREHOUSE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Тип",
                        Description = "Тип ТМЦ",
                        Path="ITEM_GROUP_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Пользователь",
                        Description = "Наименование пользователя",
                        Path="ACCOUNT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=24,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Ид склада",
                        Description = "Идентификатор склада",
                        Path="WAREHOUSE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=4,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид зоны",
                        Description = "Идентификатор зоны",
                        Path="ZONE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=4,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид типа",
                        Description = "Идентификатор типа ТМЦ",
                        Path="ITEM_GROUP_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=4,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид пользователя",
                        Description = "Идентификатор пользователя",
                        Path="ACCOUNT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=4,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид хранилища источника",
                        Description = "Идентификатор хранилища источника",
                        Path="STORAGE_FROM_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=4,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид хранилища назначение",
                        Description = "Идентификатор хранилища назначение",
                        Path="STORAGE_TO_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=4,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид операции",
                        Description = "Идентификатор операции",
                        Path="OPERATION_TYPE",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=4,
                        Hidden=true,
                    },
                };
                Grid.SetColumns(columns);
                Grid.SearchText = SearchText;
                Grid.OnLoadItems = GridLoadItems;
                Grid.SetPrimaryKey("_ROWNUMBER");
                Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                Grid.AutoUpdateInterval = 5 * 60;
                Grid.Toolbar = GridToolbar;
                Grid.OnFilterItems = () =>
                {
                    OperatorProgressClearItems();

                    if (Grid.Items != null && Grid.Items.Count > 0)
                    {
                        // Фильтрация по складу
                        // 0 -- Все склады
                        if (WarehouseSelectBox.SelectedItem.Key != null)
                        {
                            var key = WarehouseSelectBox.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            switch (key)
                            {
                                // Все склады
                                case 0:
                                    items = Grid.Items;
                                    break;

                                default:
                                    items.AddRange(Grid.Items.Where(x => x.CheckGet("WAREHOUSE_ID").ToInt() == key));
                                    break;
                            }

                            Grid.Items = items;
                        }

                        // Фильтрация по зоне
                        // 0 -- Все зоны
                        if (ZoneSelectBox.SelectedItem.Key != null)
                        {
                            var key = ZoneSelectBox.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            switch (key)
                            {
                                // Все зоны
                                case 0:
                                    items = Grid.Items;
                                    break;

                                default:
                                    items.AddRange(Grid.Items.Where(x => x.CheckGet("ZONE_ID").ToInt() == key));
                                    break;
                            }

                            Grid.Items = items;
                        }

                        // Фильтрация по типу операции
                        // 0 -- Все операции
                        if (OperationSelectBox.SelectedItem.Key != null)
                        {
                            var key = OperationSelectBox.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            switch (key)
                            {
                                // Все операции
                                case 0:
                                    items = Grid.Items;
                                    break;

                                default:
                                    items.AddRange(Grid.Items.Where(x => x.CheckGet("OPERATION_TYPE").ToInt() == key));
                                    break;
                            }

                            Grid.Items = items;
                        }

                        // Фильтрация по типу ТМЦ
                        // 0 -- Все типы
                        if (ItemTypeSelectBox.SelectedItem.Key != null)
                        {
                            var key = ItemTypeSelectBox.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            switch (key)
                            {
                                // Все типы
                                case 0:
                                    items = Grid.Items;
                                    break;

                                default:
                                    items.AddRange(Grid.Items.Where(x => x.CheckGet("ITEM_GROUP_ID").ToInt() == key));
                                    break;
                            }

                            Grid.Items = items;
                        }

                        OperatorProgressLoadItems();
                    }
                };
                Grid.Commands = Commander;
                Grid.UseProgressSplashAuto = false;
                Grid.Init();
            }
        }

        private async void GridLoadItems()
        {
            bool resume = true;

            var f = Form.GetValueByPath("FROM_DATE_TIME").ToDateTime();
            var t = Form.GetValueByPath("TO_DATE_TIME").ToDateTime();

            if (DateTime.Compare(f, t) > 0)
            {
                const string msg = "Дата начала должна быть меньше даты окончания.";
                var d = new DialogWindow($"{msg}", this.ControlTitle);
                d.ShowDialog();
                resume = false;
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();
                p.Add("FROM_DTTM", Form.GetValueByPath("FROM_DATE_TIME"));
                p.Add("TO_DTTM", Form.GetValueByPath("TO_DATE_TIME"));
                p.Add("FACTORY_ID", $"{FactoryId}");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Stock");
                q.Request.SetParam("Object", "Report");
                q.Request.SetParam("Action", "InventoryItemMovement");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                GridDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        GridDataSet = ListDataSet.Create(result, "ITEMS");
                    }
                }
                else
                {
                    q.ProcessError();
                }
                Grid.UpdateItems(GridDataSet);
            }
        }

        private void OperatorProgressClearItems()
        {
            PanelScore.Children.Clear();
            PanelScoreFooter.Children.Clear();
        }

        private void OperatorProgressLoadItems()
        {
            if (Grid != null && Grid.Items != null && Grid.Items.Count > 0)
            {
                int defaultPanelHeight = 30;

                int summaryArrivalCount = 0;
                int summaryMovementCount = 0;
                int summaryConsumptionCount = 0;

                // Данные для прогресбаров по сторудникам
                {
                    var groupedItems = Grid.Items.GroupBy(x => x.CheckGet("ACCOUNT_NAME")).OrderBy(x => x.Key);
                    int maxOperationCount = groupedItems.Max(x => x.Count());
                    foreach (var groupedItem in groupedItems)
                    {
                        int arrivalCount = 0;
                        int movementCount = 0;
                        int consumptionCount = 0;
                        foreach (var item in groupedItem)
                        {
                            switch (item.CheckGet("OPERATION_TYPE").ToInt())
                            {
                                case 1:
                                    arrivalCount++;
                                    break;

                                case 2:
                                    movementCount++;
                                    break;

                                case 3:
                                    consumptionCount++;
                                    break;
                            }
                        }

                        var operatorProgress = new OperatorProgress();
                        operatorProgress.Height = defaultPanelHeight;
                        operatorProgress.Description.Text = groupedItem.Key;
                        operatorProgress.SetProgress((int)((double)groupedItem.Count() / maxOperationCount * 100), movementCount, arrivalCount, consumptionCount, $"{arrivalCount}/{movementCount}/{consumptionCount}");
                        operatorProgress.OnMouseDown += OperatorProgressMouseDown;
                        PanelScore.Children.Add(operatorProgress);

                        summaryArrivalCount += arrivalCount;
                        summaryMovementCount += movementCount;
                        summaryConsumptionCount += consumptionCount;
                    }
                }

                // Данные для прогресбаров по операциям
                {
                    int maxOperationCount = Grid.Items.Count;

                    {
                        var operatorProgress = new OperatorProgress();
                        operatorProgress.Height = defaultPanelHeight;
                        operatorProgress.Description.Text = "Оприходование";
                        operatorProgress.SetProgress((int)((double)summaryArrivalCount / maxOperationCount * 100), 0, summaryArrivalCount, 0, $"{summaryArrivalCount}");
                        operatorProgress.OnMouseDown += OperatorProgressMouseDown;
                        PanelScoreFooter.Children.Add(operatorProgress);
                    }

                    {
                        var operatorProgress = new OperatorProgress();
                        operatorProgress.Height = defaultPanelHeight;
                        operatorProgress.Description.Text = "Перемещение";
                        operatorProgress.SetProgress((int)((double)summaryMovementCount / maxOperationCount * 100), summaryMovementCount, 0, 0, $"{summaryMovementCount}");
                        operatorProgress.OnMouseDown += OperatorProgressMouseDown;
                        PanelScoreFooter.Children.Add(operatorProgress);
                    }

                    {
                        var operatorProgress = new OperatorProgress();
                        operatorProgress.Height = defaultPanelHeight;
                        operatorProgress.Description.Text = "Списание";
                        operatorProgress.SetProgress((int)((double)summaryConsumptionCount / maxOperationCount * 100), 0, 0, summaryConsumptionCount, $"{summaryConsumptionCount}");
                        operatorProgress.OnMouseDown += OperatorProgressMouseDown;
                        PanelScoreFooter.Children.Add(operatorProgress);
                    }
                }
            }
        }

        private void OperatorProgressMouseDown(object sender, MouseEventArgs e)
        {
            if (sender is OperatorProgress)
            {
                string description = (sender as OperatorProgress).Description.Text;
                if (SearchText.Text == description)
                {
                    SearchText.Text = string.Empty;
                }
                else
                {
                    SearchText.Text = description;
                }

                Grid.UpdateItems();
            }
        }

        private void GetZoneList()
        {
            SelectBox.ClearSelectBoxItems(ZoneSelectBox);

            if (WarehouseSelectBox != null && WarehouseSelectBox.SelectedItem.Key != null && WarehouseSelectBox.SelectedItem.Key.ToInt() != 0)
            {
                ZoneSelectBox.Items.Add("0", "Все зоны");
                FormHelper.ComboBoxInitHelper(ZoneSelectBox, "Warehouse", "Zone", "ListByWarehouse", "WMZO_ID", "ZONE", new Dictionary<string, string>() { { "WMWA_ID", WarehouseSelectBox.SelectedItem.Key } }, true, false);
                ZoneSelectBox.SelectedItem = new KeyValuePair<string, string>("0", "Все зоны");
            }
            else
            {
                ZoneSelectBox.Items.Add("0", "Все зоны");
                ZoneSelectBox.SelectedItem = new KeyValuePair<string, string>("0", "Все зоны");
            }
        }

        public void Refresh()
        {
            Grid.LoadItems();
        }

        private void WarehouseSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
            GetZoneList();
        }

        private void ZoneSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void OperationSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void ItemTypeSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void OnCurrentShift(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now;
            if (date.Hour >= 20 || date.Hour < 8)
            {
                if (date.Hour >= 20)
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.AddDays(1).ToString("dd.MM.yyyy")} 08:00:00");
                }
                else
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 08:00:00");
                }
            }
            else
            {
                Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 08:00:00");
                Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 20:00:00");
            }

            Refresh();
        }

        private void OnPrevShift(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now.AddHours(-12);
            if (date.Hour >= 20 || date.Hour < 8)
            {
                if (date.Hour >= 20)
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{date.ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{date.AddDays(1).ToString("dd.MM.yyyy")} 08:00:00");
                }
                else
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{date.AddDays(-1).ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{date.ToString("dd.MM.yyyy")} 08:00:00");
                }
            }
            else
            {
                Form.SetValueByPath("FROM_DATE_TIME", $"{date.ToString("dd.MM.yyyy")} 08:00:00");
                Form.SetValueByPath("TO_DATE_TIME", $"{date.ToString("dd.MM.yyyy")} 20:00:00");
            }

            Refresh();
        }

        private void OnCurrentDay(object sender, RoutedEventArgs e)
        {
            Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.AddDays(1).ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void OnPrevDay(object sender, RoutedEventArgs e)
        {
            Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void OnCurrentWeek(object sender, RoutedEventArgs e)
        {
            DayOfWeek day = DateTime.Now.DayOfWeek;
            int days = day - DayOfWeek.Monday;
            DateTime date = DateTime.Now.AddDays(-days);

            Form.SetValueByPath("FROM_DATE_TIME", $"{date.Date.ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{date.Date.AddDays(7).ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void OnPrevWeek(object sender, RoutedEventArgs e)
        {
            DayOfWeek day = DateTime.Now.DayOfWeek;
            int days = day - DayOfWeek.Monday;
            DateTime date = DateTime.Now.AddDays(-days).AddDays(-7);

            Form.SetValueByPath("FROM_DATE_TIME", $"{date.Date.ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{date.Date.AddDays(7).ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void OnCurrentMonth(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now;

            Form.SetValueByPath("FROM_DATE_TIME", $"{new DateTime(date.Year, date.Month, 1).ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{new DateTime(date.Year, date.Month, 1).AddMonths(1).ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void OnPrevMonth(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now.AddMonths(-1);

            Form.SetValueByPath("FROM_DATE_TIME", $"{new DateTime(date.Year, date.Month, 1).ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{new DateTime(date.Year, date.Month, 1).AddMonths(1).ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }
    }
}
