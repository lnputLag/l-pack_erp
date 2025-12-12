using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.Testing
{
    /// <summary>
    /// Список выполненных производственных заданий для теста продукции
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class ProductionTaskForTesting : ControlBase
    {
        public ProductionTaskForTesting()
        {
            InitializeComponent();

            ControlTitle = "Изделия для испытаний";
            RoleName = "[erp]production_testing";
            DocumentationUrl = "/doc/l-pack-erp-new/production_new/test_product/test_products";


            OnLoad = () =>
            {
                LoadRef();
                InitGrid();
                SetDefaults();
            };

            OnUnload = () =>
            {
                Grid.Destruct();
            };

            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverName == ControlName)
                {
                    ProcessCommand(msg.Action);
                }
            };

            OnFocusGot = () =>
            {
                Grid.ItemsAutoUpdate = true;
                Grid.Run();
            };

            OnFocusLost = () =>
            {
                Grid.ItemsAutoUpdate = false;
            };

            Commander.SetCurrentGroup("main");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "refresh",
                    Enabled = true,
                    Title = "Обновить",
                    Description = "Обновить данные",
                    ButtonUse = true,
                    ButtonName = "RefreshButton",
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        Grid.LoadItems();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "help",
                    Enabled = true,
                    Title = "Справка",
                    Description = "Показать справочную информацию",
                    ButtonUse = true,
                    ButtonName = "HelpButton",
                    HotKey = "F1",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        Central.ShowHelp(DocumentationUrl);
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "gridtoexcel",
                    Title = "В Excel",
                    MenuUse = false,
                    ButtonUse = true,
                    ButtonName = "ToExcelButton",
                    Description = "Выгрузка таблицы в Excel",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        Grid.ItemsExportExcel();
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        if (Grid.SelectedItem != null)
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
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// Обработка и выполнение команд
        /// </summary>
        /// <param name="command"></param>
        public void ProcessCommand(string command)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "refresh":
                        Grid.LoadItems();
                        break;

                    case "update":
                        Grid.UpdateItems();
                        break;
                }
            }
        }

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Номер ПЗГА",
                    Path="CORRUGATE_NUM",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Номер ПЗПР",
                    Path="TASK_NUM",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Линия переработки",
                    Path="MACHINE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Дата и время окончания задания",
                    Path="END_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=12,
                    Format="dd.MM.yyyy HH:mm",
                },
                new DataGridHelperColumn
                {
                    Header="Мастер",
                    Path="MASTER_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="Лаборатория",
                    Path="TESTING_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="Картон",
                    Path="CARDBOARD_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул изделия",
                    Path="PRODUCT_CODE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn
                {
                    Header="Название изделия",
                    Path="PRODUCT_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=50,
                },
                new DataGridHelperColumn
                {
                    Header="Схема производства",
                    Path="SCHEME_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=30,
                },
                new DataGridHelperColumn
                {
                    Header="Номер серии исследований",
                    Path="SERIES_NUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Дата последнего испытания",
                    Path="LAST_TEST_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=12,
                    Format="dd.MM.yyyy HH:mm",
                },
                new DataGridHelperColumn
                {
                    Header="ID картона",
                    Path="CARDBOARD_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ID задания на переработку",
                    Path="PROCESSING_TASK_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },

            };
            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("ID");
            Grid.SetSorting("ID", ListSortDirection.Descending);
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            Grid.Commands = Commander;
            Grid.SearchText = SearchText;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateActions(selectedItem);
                }
            };

            //данные грида
            Grid.OnLoadItems = LoadItems;
            Grid.OnFilterItems = FilterItems;
            Grid.Init();
        }

        /// <summary>
        /// Задание значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            FromDate.Text = DateTime.Now.Date.AddDays(-2).ToString("dd.MM.yyyy");
            ToDate.Text = DateTime.Now.Date.ToString("dd.MM.yyyy");
        }

        /// <summary>
        /// Загрузка справочников
        /// </summary>
        private async void LoadRef()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPr");
            q.Request.SetParam("Object", "CutterPr");
            q.Request.SetParam("Action", "GetSources");

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
                    var machineDS = ListDataSet.Create(result, "MACHINES");
                    var machineItems = new Dictionary<string, string>();
                    machineItems.Add("0", "Все");
                    foreach (var row in machineDS.Items)
                    {
                        int machineGroup = row.CheckGet("GROUP_ID").ToInt();
                        // Добавляем только станки для коробок, ИСВ и FOLD
                        if (machineGroup.ContainsIn(3, 4, 9, 10))
                        {
                            machineItems.Add(row.CheckGet("NAME2"), $"{row.CheckGet("NAME2")} ({row.CheckGet("NAME")})");
                        }
                    }
                    Machine.Items = machineItems;
                    Machine.SetSelectedItemByKey("0");
                }
            }
        }

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        private async void LoadItems()
        {
            GridToolbar.IsEnabled = false;
            Grid.ShowSplash();

            string includeSheet = (bool)WithSheetCheckBox.IsChecked ? "1" : "0";

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "ProductionTask");
            q.Request.SetParam("Action", "ListTaskTesting");
            q.Request.SetParam("DATE_FROM", FromDate.Text);
            q.Request.SetParam("DATE_TO", ToDate.Text);
            q.Request.SetParam("INCLUDE_SHEET", includeSheet);

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
                    var ds = ListDataSet.Create(result, "TASK_TESTING");
                    var processedDS = ProcessItems(ds);
                    Grid.UpdateItems(processedDS);
                    RefreshButton.Style = (Style)RefreshButton.TryFindResource("Button");
                }
            }

            GridToolbar.IsEnabled = true;
            Grid.HideSplash();
        }

        /// <summary>
        /// Обновление действий со строкой
        /// </summary>
        /// <param name="selectedItem"></param>
        private void UpdateActions(Dictionary<string, string> selectedItem)
        {
            SelectedItem = selectedItem;
        }

        /// <summary>
        /// Обработка строк перед загрузкой в таблицу
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        private ListDataSet ProcessItems(ListDataSet ds)
        {
            ListDataSet _ds = new ListDataSet();
            _ds.Init();
            // Предыдущая строка
            var prevRow = new Dictionary<string, string>();

            if (ds.Items != null)
            {
                if (ds.Items.Count > 0)
                {
                    foreach(var row in ds.Items)
                    {
                        bool include = true;
                        // Если id заготовки совпадает с предыдущей, и если у предыдущей строки есть номер исследования, а у текущей нет, то текущую строку пропускаем
                        if (row.CheckGet("BLANK_ID").ToInt() == prevRow.CheckGet("BLANK_ID").ToInt())
                        {
                            if ((row.CheckGet("SERIES_NUMBER").ToInt() == 0) && (prevRow.CheckGet("SERIES_NUMBER").ToInt() != 0))
                            {
                                include = false;
                            }
                        }

                        if (include)
                        {
                            _ds.Items.Add(row);
                            prevRow = row;
                        }
                    }
                }
            }

            return _ds;
        }

        /// <summary>
        /// Фильтрация данных в таблице
        /// </summary>
        public void FilterItems()
        {
            if (Grid.Items != null)
            {
                if (Grid.Items.Count > 0)
                {
                    bool doFilteringByMachine = false;
                    string machineCode = Machine.SelectedItem.Key;
                    if (!string.IsNullOrEmpty(machineCode))
                    {
                        if (machineCode != "0")
                        {
                            doFilteringByMachine = true;
                        }
                    }

                    if (doFilteringByMachine)
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (var row in Grid.Items)
                        {
                            bool includeByMachine = true;

                            if (doFilteringByMachine)
                            {
                                includeByMachine = false;

                                if (row.CheckGet("MACHINE_NAME") == machineCode)
                                {
                                    includeByMachine = true;
                                }
                            }

                            if (includeByMachine)
                            {
                                items.Add(row);
                            }
                        }

                        Grid.Items = items;
                    }
                }
            }
        }

        private void Date_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshButton.Style = (Style)RefreshButton.TryFindResource("FButtonPrimary");
        }

        private void WithSheetCheckBox_Click(object sender, RoutedEventArgs e)
        {
            ProcessCommand("refresh");
        }

        private void Machine_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ProcessCommand("update");
        }
    }
}
