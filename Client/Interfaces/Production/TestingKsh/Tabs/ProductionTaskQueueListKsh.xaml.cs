using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.Testing
{
    /// <summary>
    /// Список производственных заданий на ГА для отметок о передаче изделий на тестирование в лабораторию
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class ProductionTaskQueueListKsh : ControlBase
    {
        public ProductionTaskQueueListKsh()
        {
            InitializeComponent();

            AutoTestingCheckBox.Click += AutoTestingCheckBox_Click;

            ControlTitle = "Очередь ПЗ";
            RoleName = "[erp]production_testing_ksh";
            DocumentationUrl = "/doc/l-pack-erp-new/production_new/test_product/task_queue";

            OnLoad = () =>
            {
                InitGrid();
                LoadRef();
                SetDefaults();
                LoadAutoTestingFlag();
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

            {
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
                        Name = "stat",
                        Title = "Подсчет",
                        MenuUse = true,
                        ButtonUse = true,
                        ButtonName = "StatButton",
                        Description = "Подсчет статистики распределения тестовых образцов",
                        AccessLevel = Role.AccessMode.ReadOnly,
                        Action = () =>
                        {
                            ShowStat();
                        },
                        CheckEnabled = () =>
                        {
                            var result = true;
                            return result;
                        },
                    });
                }
                Commander.Init(this);
            }
        }

        /// <summary>
        /// Загруженность лаборатории
        /// </summary>
        Dictionary<string, List<int>> TestingWorkload;

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        public void ProcessNavigation()
        {

        }

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
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            var machineList = new Dictionary<string, string>()
            {
                { "0", "Все" },
                //{ "2", "BHS-1" },
                //{ "21", "BHS-2" },
                //{ "22", "Fosber" },
                { "23", "JS" },
            };
            Machine.Items = machineList;
            Machine.SetSelectedItemByKey("0");

            var compositionTypeList = new Dictionary<string, string>()
            {
                { "0", "Все" },
                { "1", "Тип 1" },
                { "2", "Тип 2" },
                { "3", "Тип 3" },
                { "4", "Новый" },
                { "5", "Тест" },
            };
            CompositionType.Items = compositionTypeList;
            CompositionType.SetSelectedItemByKey("0");

            TestingWorkload = new Dictionary<string, List<int>>();
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
                    Header="№",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=40,
                },
                new DataGridHelperColumn
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=60,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, (Dictionary<string, string> row) =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                // 0 - в очереди ГА, 1 - в плане
                                if (row["IN_PLAN"].ToInt() == 0)
                                {
                                    color = HColor.Blue;
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        },
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Номер ПЗГА",
                    Path="TASK_NUM",
                    ColumnType=ColumnTypeRef.String,
                    Width2=80,
                },
                new DataGridHelperColumn
                {
                    Header="Дата и время начала",
                    Path="START_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=80,
                    Format="dd.MM HH:mm",
                },
                new DataGridHelperColumn
                {
                    Header="Гофроагрегат",
                    Path="MACHINE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=80,
                },
                new DataGridHelperColumn
                {
                    Header="Стекер",
                    Path="STACKER_NUM",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=40,
                },
                new DataGridHelperColumn
                {
                    Header="Картон",
                    Path="CARDBOARD_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=150,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, (Dictionary<string, string> row) =>
                            {
                                var num = (row.CheckGet("NUM_COLOR").ToInt() % 5).ToString();

                                return CardboardGroupColor.Items[num].ToBrush();
                            }
                        },
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Целевое ЕСТ",
                    Path="ECT",
                    ColumnType=ColumnTypeRef.Double,
                    Width2=60,
                },
                new DataGridHelperColumn
                {
                    Header="Целевое Толщина",
                    Path="THICKNESS",
                    ColumnType=ColumnTypeRef.Double,
                    Width2=60,
                },
                new DataGridHelperColumn
                {
                    Header="Мастер",
                    Path="MASTER_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=60,
                },
                new DataGridHelperColumn
                {
                    Header="Лаборатория",
                    Path="TESTING_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=60,
                    Editable=true,
                    OnClickAction = (row, el) =>
                    {
                        if (Central.Navigator.GetRoleLevel(this.RoleName) >= Role.AccessMode.FullAccess)
                        {
                            UpdateTestingFlag(row);
                        }

                        return true;
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Артикул заготовки",
                    Path="BLANK_CODE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=150,
                },
                new DataGridHelperColumn
                {
                    Header="Название заготовки",
                    Path="BLANK_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=350,
                },
                new DataGridHelperColumn
                {
                    Header="Номер ПЗПР",
                    Path="CONVERTING_NUM",
                    ColumnType=ColumnTypeRef.String,
                    Width2=80,
                },
                new DataGridHelperColumn
                {
                    Header="Дата и время начала переработки",
                    Path="CONVERTING_START_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=80,
                    Format="dd.MM HH:mm",
                },
                new DataGridHelperColumn
                {
                    Header="Линия переработки",
                    Path="CONVERTING_MACHINE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=110,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул изделия",
                    Path="PRODUCT_CODE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=150,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.ForegroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                // Если есть изделие после переработки
                                if (row["PRODUCT_ID"].ToInt() > 0)
                                {
                                    if (row["TESTING_FLAG"].ToInt() != row["PRODUCT_TESTING_FLAG"].ToInt())
                                    {
                                    color = HColor.MagentaFG;
                                    }
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        },
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Название изделия",
                    Path="PRODUCT_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=350,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.ForegroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                // Если есть изделие после переработки
                                if (row["PRODUCT_ID"].ToInt() > 0)
                                {
                                    if (row["TESTING_FLAG"].ToInt() != row["PRODUCT_TESTING_FLAG"].ToInt())
                                    {
                                    color = HColor.MagentaFG;
                                    }
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        },
                    },
                },
            };

            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("ID");
            Grid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            Grid.Commands = Commander;
            Grid.SearchText = SearchText;

            // раскраска строк
            Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                {
                    StylerTypeRef.BackgroundColor, (Dictionary<string, string> row) =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        if ((row.CheckGet("MASTER_FLAG").ToInt() == 1) || (row.CheckGet("TESTING_FLAG").ToInt() == 1))
                        {
                            color = HColor.YellowOrange;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };

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
        /// Обновление действий со строкой
        /// </summary>
        /// <param name="selectedItem"></param>
        private void UpdateActions(Dictionary<string, string> selectedItem)
        {
            SelectedItem = selectedItem;
        }

        /// <summary>
        /// Загрузка справочников
        /// </summary>
        private async void LoadRef()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");//Можно использовать общий модуль
            q.Request.SetParam("Object", "Cutter");
            q.Request.SetParam("Action", "GetSources");
            //MEK т.к сдесь берем с сервера только картон, то не нужно передавать FACTORY_ID. Если передать его то ничего не изменится
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "CARDBOARD");
                    var list = new Dictionary<string, string>();
                    list.Add("0", "");
                    list.AddRange<string, string>(ds.GetItemsList("ID", "NAME"));

                    Cardboard.Items = list;
                    Cardboard.SetSelectedItemByKey("0");
                }
            }
        }

        /// <summary>
        /// Загрузка настроек
        /// </summary>
        private async void LoadAutoTestingFlag()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Control");
            q.Request.SetParam("Object", "ConfigurationOption");
            q.Request.SetParam("Action", "Get");
            q.Request.SetParam("PARAM_NAME", "AUTO_TESTING_FLAG_KSH");
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                AutoTestingCheckBox.IsChecked = JsonConvert.DeserializeObject<string>(q.Answer.Data).ToBool();
            }
            else
            {
                AutoTestingCheckBox.IsChecked = null;
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
            string queueOnly = (bool)QueueOnlyCheckBox.IsChecked ? "1" : "0";

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "ProductionTask");
            q.Request.SetParam("Action", "ListQueueTesting");
            q.Request.SetParam("FACTORY_ID", "2");
            q.Request.SetParam("INCLUDE_SHEET", includeSheet);
            q.Request.SetParam("QUEUE_ONLY", queueOnly);

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
                    var ds = ListDataSet.Create(result, "QUEUE");
                    var processedDS = ProcessItems(ds);
                    Grid.UpdateItems(processedDS);
                }
            }

            GridToolbar.IsEnabled = true;
            Grid.HideSplash();
        }

        /// <summary>
        /// Обработка строк перед загрузкой в таблицу
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        private ListDataSet ProcessItems(ListDataSet ds)
        {
            var _ds = ds;

            if (ds.Items != null)
            {
                if (ds.Items.Count > 0)
                {
                    int i = 0;
                    int oldId = 0;

                    foreach (var item in _ds.Items)
                    {
                        int newId = item.CheckGet("CARDBOARD_ID").ToInt();
                        if (newId != oldId)
                        {
                            i++;
                            oldId = newId;
                        }
                        item.CheckAdd("NUM_COLOR", i.ToString());

                        // Подсчитываем количество флагов в разбивке по сменам
                        var d = item.CheckGet("CONVERTING_START_DTTM");
                        if (!string.IsNullOrEmpty(d))
                        {
                            var dttm = d.ToDateTime("dd.MM.yyyy HH:mm:ss");
                            string key = $"{dttm.Day}.{dttm.Month:00} день";
                            if (dttm.Hour < 8)
                            {
                                key = $"{dttm.Day - 1}.{dttm.Month:00} ночь";
                            }
                            else if (dttm.Hour > 19)
                            {
                                key = $"{dttm.Day}.{dttm.Month:00} ночь";
                            }

                            if (!TestingWorkload.ContainsKey(key))
                            {
                                TestingWorkload.Add(key, new List<int>() { 0, 0 });
                            }

                            if (item.CheckGet("MASTER_FLAG").ToInt() == 1)
                            {
                                TestingWorkload[key][0]++;
                            }

                            if (item.CheckGet("TESTING_FLAG").ToInt() == 1)
                            {
                                TestingWorkload[key][1]++;
                            }

                            var q = 0;
                        }
                    }
                }
            }

            return _ds;
        }

        /// <summary>
        /// Фильтрация строк
        /// </summary>
        private void FilterItems()
        {
            if (Grid.Items != null)
            {
                if (Grid.Items.Count > 0)
                {
                    var items = new List<Dictionary<string, string>>();
                    int machineId = Machine.SelectedItem.Key.ToInt();

                    int cardboardId = Cardboard.SelectedItem.Key.ToInt();
                    int composition = CompositionType.SelectedItem.Key.ToInt();

                    foreach (var item in Grid.Items)
                    {
                        bool includeByMachine = true;
                        bool includeByCardboard = true;
                        bool includeByComposition = true;

                        if (machineId > 0)
                        {
                            if (item.CheckGet("MACHINE_ID").ToInt() != machineId)
                            {
                                includeByMachine = false;
                            }
                        }

                        if (cardboardId > 0)
                        {
                            if (item.CheckGet("CARDBOARD_ID").ToInt() != cardboardId)
                            {
                                includeByCardboard = false;
                            }
                        }

                        if (composition > 0)
                        {
                            if (item.CheckGet("COMPOSITION_TYPE").ToInt() != composition)
                            {
                                includeByComposition = false;
                            }
                        }

                        if (includeByMachine && includeByCardboard && includeByComposition)
                        {
                            items.Add(item);
                        }
                    }

                    Grid.Items = items;
                }
            }
        }

        private void ShowStat()
        {
            string msg = "";
            int i = 0;
            foreach (var item in TestingWorkload)
            {
                if (i > 0)
                {
                    msg = $"{msg}\n{item.Key}: мастер - {item.Value[0]}, лаборатория - {item.Value[1]}";
                }
                else
                {
                    msg = $"{item.Key}: мастер - {item.Value[0]}, лаборатория - {item.Value[1]}";
                }
                i++;
            }

            var dw = new DialogWindow(msg, "Загруженност лаборатории");
            dw.ShowDialog();
        }

        /// <summary>
        /// Обновление признака отправки изделия на тестирование
        /// </summary>
        private async void UpdateTestingFlag(Dictionary<string, string> row)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");//Можно использовать общий модуль
            q.Request.SetParam("Object", "ProductionTask");
            q.Request.SetParam("Action", "UpdateTestingFlag");

            bool newTestingFlag = !row.CheckGet("TESTING_FLAG").ToBool();
            q.Request.SetParam("CORRUGATOR_ID", row.CheckGet("CORRUGATOR_ID"));
            q.Request.SetParam("BLANK_ID", row.CheckGet("BLANK_ID"));
            q.Request.SetParam("CONVERTING_ID", row.CheckGet("CONVERTING_ID"));
            q.Request.SetParam("PRODUCT_ID", row.CheckGet("PRODUCT_ID"));
            q.Request.SetParam("TESTING_FLAG", newTestingFlag ? "1" : "0");

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status != 0)
            {
                q.ProcessError();
            }
            // В любом случае загружаем данные в таблицу, чтобы видеть где данные нормалные, а где не обновились
            Grid.LoadItems();
        }

        private void SelectBoxItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ProcessCommand("update");
        }

        private void WithSheetCheckBox_Click(object sender, RoutedEventArgs e)
        {
            ProcessCommand("refresh");
        }

        private void QueueOnlyCheckBox_Click(object sender, RoutedEventArgs e)
        {
            ProcessCommand("refresh");
        }

        private void AutoTestingCheckBox_Click(object sender, RoutedEventArgs e)
        {
            SetAutoTestingFlag(AutoTestingCheckBox.IsChecked == true);
        }
        /// <summary>
        /// Загрузка настроек
        /// </summary>
        private async void SetAutoTestingFlag(bool value)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Control");
            q.Request.SetParam("Object", "ConfigurationOption");
            q.Request.SetParam("Action", "Set");
            q.Request.SetParam("PARAM_NAME", "AUTO_TESTING_FLAG_KSH");
            q.Request.SetParam("PARAM_VALUE", value.ToInt().ToString());
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status != 0)
            {
                AutoTestingCheckBox.IsChecked = null;
            }
        }
    }
}
