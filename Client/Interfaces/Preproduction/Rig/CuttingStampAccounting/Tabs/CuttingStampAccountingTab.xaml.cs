using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Список техкарт для учета штанцформ
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class CuttingStampAccountingTab : ControlBase
    {
        public CuttingStampAccountingTab()
        {
            InitializeComponent();
            ControlTitle = "Техкарты";
            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/cutting_stamp_accounting";
            RoleName = "[erp]rig_cutting_stamp_accnt";

            OnLoad = () =>
            {
                SetDefaults();
                LoadRef();
                GridInit();
                StampGridInit();
                StampItemGridInit();
            };

            OnUnload = () =>
            {
                TechCardGrid.Destruct();
                StampGrid.Destruct();
            };

            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverName == ControlName)
                {
                    ProcessMessage(msg);
                }
            };

            OnFocusGot = () =>
            {
                TechCardGrid.ItemsAutoUpdate = true;
                TechCardGrid.Run();
            };

            OnFocusLost = () =>
            {
                TechCardGrid.ItemsAutoUpdate = false;
            };

            Commander.SetCurrentGroup("main");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "refresh",
                    Group = "grid_base",
                    Enabled = true,
                    Title = "Обновить",
                    Description = "Обновить данные",
                    ButtonUse = true,
                    ButtonName = "RefreshButton",
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        TechCardGrid.LoadItems();
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
            }
            Commander.SetCurrentGridName("TechCardGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "showtechcard",
                    Title = "Техкарта",
                    Group = "operations",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonName = "ShowTechcardButton",
                    Description = "Открыть техкарту",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        var k = TechCardGrid.GetPrimaryKey();
                        var id = TechCardGrid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            ShowTechnologicalCard();
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = TechCardGrid.SelectedItem;
                        if (row != null)
                        {
                            result = !row.CheckGet("TK_FILE_PATH").IsNullOrEmpty();
                        }
                        return result;
                    },
                });
            }
            Commander.SetCurrentGridName("StampGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "bind",
                    Title = "Привязать",
                    Group = "operations",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonName = "BindButton",
                    Description = "Привязать штанцформу",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = TechCardGrid.GetPrimaryKey();
                        var id = TechCardGrid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            var stampBindFrame = new CuttingStampBind();
                            stampBindFrame.ReceiverName = ControlName;
                            stampBindFrame.Show(TechCardGrid.SelectedItem);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = TechCardGrid.SelectedItem;
                        if (row != null)
                        {
                            result = !row.CheckGet("ARCHIVED_FLAG").ToBool();
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "unbind",
                    Title = "Отвязать",
                    Group = "operations",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonName = "UnbindButton",
                    Description = "Отвязать штанцформу",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = StampGrid.GetPrimaryKey();
                        var id = StampGrid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            UnbindStamp();
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        if (StampGrid.Items.Count > 0)
                        {
                            var row = StampGrid.SelectedItem;
                            if (row != null)
                            {
                                result = true;
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "edit",
                    Title = "Изменить",
                    Group = "operations",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonName = "EditButton",
                    Description = "Изменить параметры штанцформы, посмотреть элементы",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = StampGrid.GetPrimaryKey();
                        var id = StampGrid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            var stampFrame = new CuttingStamp();
                            stampFrame.ReceiverName = ControlName;
                            stampFrame.Edit(id);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = StampGrid.SelectedItem;
                        if (row != null)
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
        /// Список производственных площадок для выпадающего списка
        /// </summary>
        public Dictionary<string, string> FactoryItems {  get; set; }

        /// <summary>
        /// Таймер заполнения поля шаблона загрузки данных
        /// </summary>
        public DispatcherTimer TemplateTimeoutTimer;

        /// <summary>
        /// Обработка и выполнение команд
        /// </summary>
        /// <param name="command"></param>
        public void ProcessMessage(ItemMessage m)
        {
            string command = m.Action;
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "refresh":
                        TechCardGrid.LoadItems();
                        break;
                    case "refreshstamp":
                        StampGrid.LoadItems();
                        break;
                }
            }
        }

        public void SetDefaults()
        {
            FactoryItems = new Dictionary<string, string>();
        }

        /// <summary>
        /// Инициализация таблицы техкарт
        /// </summary>
        private void GridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn()
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn()
                {
                    Header="ИД",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn()
                {
                    Header="Артикул",
                    Path="SKU_CODE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn()
                {
                    Header="Потребитель",
                    Path="CUSTOMER",
                    ColumnType=ColumnTypeRef.String,
                    Width2=26,
                },
                new DataGridHelperColumn()
                {
                    Header="Наименование",
                    Path="TK_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=36,
                },
                new DataGridHelperColumn()
                {
                    Header="Вид изделия",
                    Path="PRODUCT_CLASS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=22,
                },
                new DataGridHelperColumn()
                {
                    Header="Список штанцформ",
                    Path="STAMP_LIST",
                    ColumnType=ColumnTypeRef.String,
                    Hidden=true,
                    Searchable=true,
                },
            };
            TechCardGrid.SetColumns(columns);
            TechCardGrid.SetPrimaryKey("_ROWNUMBER");
            TechCardGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            TechCardGrid.AutoUpdateInterval = 900;
            TechCardGrid.Toolbar = Toolbar;
            TechCardGrid.SearchText = GridSearch;
            TechCardGrid.Commands = Commander;

            TechCardGrid.OnLoadItems = LoadItems;
            TechCardGrid.OnSelectItem =  selectedItem =>
            {
                StampGrid.ClearItems();
                StampItemGrid.ClearItems();

                StampGrid.LoadItems();
            };

            // Раскраска строк
            TechCardGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                // Цвета фона строк
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=System.Windows.DependencyProperty.UnsetValue;
                        var color = "";

                        if (row.CheckGet("ARCHIVED_FLAG").ToBool())
                        {
                            color = HColor.Olive;
                        }
                        else if (row.CheckGet("NO_STAMP").ToBool())
                        {
                            if (row.CheckGet("SKU_CODE").IsNullOrEmpty())
                            {
                                color = HColor.Blue;
                            }
                            else
                            {
                                color = HColor.Yellow;
                            }
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                }
            };

            TechCardGrid.Init();
        }

        /// <summary>
        /// Инициализация таблицы штанцформ
        /// </summary>
        private void StampGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn()
                {
                    Header="ИД",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn()
                {
                    Header="Наименование",
                    Path="STAMP_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=30,
                },
                new DataGridHelperColumn()
                {
                    Header="Станок",
                    Path="STAMP_MACHINE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn()
                {
                    Header="Универсальная",
                    Path="HOLE_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=5,
                },
                new DataGridHelperColumn()
                {
                    Header="Статус",
                    Path="STATUS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn()
                {
                    Header="Площадка",
                    Path="FACTORY",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn()
                {
                    Header="Список штампов",
                    Path="STAMP_LIST",
                    ColumnType=ColumnTypeRef.String,
                    Hidden=true,
                    Searchable=true,
                },
            };
            StampGrid.SetColumns(columns);
            StampGrid.SetPrimaryKey("ID");
            StampGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            StampGrid.Commands = Commander;

            StampGrid.OnLoadItems = StampLoadItems;
            StampGrid.OnSelectItem = selectedItem =>
            {
                StampItemGrid.LoadItems();
            };

            StampGrid.Init();
        }

        /// <summary>
        /// Инициализация таблицы элементов штанцформ
        /// </summary>
        private void StampItemGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="Название",
                    Path="NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="Плательщик",
                    Path="OWNER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="Ячейка",
                    Path="STORAGE_PLACE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Статус",
                    Path="STATUS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
            };
            StampItemGrid.SetColumns(columns);
            StampItemGrid.SetPrimaryKey("ID");
            StampItemGrid.SetSorting("NAME", System.ComponentModel.ListSortDirection.Ascending);

            StampItemGrid.Commands = Commander;

            StampItemGrid.OnLoadItems = StampItemLoadItems;

            StampItemGrid.Init();

        }

        /// <summary>
        /// Запуск таймера заполнения шаблона загрузки данных
        /// </summary>
        public void RunTemplateTimeoutTimer()
        {
            if (TemplateTimeoutTimer == null)
            {
                TemplateTimeoutTimer = new DispatcherTimer
                {
                    Interval = new TimeSpan(0, 0, 2)
                };

                {
                    var row = new Dictionary<string, string>();
                    row.CheckAdd("TIMEOUT", "2000");
                    row.CheckAdd("DESCRIPTION", "");
                    Central.Stat.TimerAdd("AssortmentList_RunTemplateTimeoutTimer", row);
                }

                TemplateTimeoutTimer.Tick += (s, e) =>
                {
                    // Если введены только один или два символа, ничего не загружаем, ждём следующий
                    if (GridSearch.Text.Length > 2)
                    {
                        TechCardGrid.LoadItems();
                    }
                    StopTemplateTimeoutTimer();
                };
            }

            if (TemplateTimeoutTimer.IsEnabled)
            {
                TemplateTimeoutTimer.Stop();
            }
            TemplateTimeoutTimer.Start();
        }

        /// <summary>
        /// Остановка таймера заполнения заблона загрузки данных
        /// </summary>
        public void StopTemplateTimeoutTimer()
        {
            if (TemplateTimeoutTimer != null)
            {
                if (TemplateTimeoutTimer.IsEnabled)
                {
                    TemplateTimeoutTimer.Stop();
                }
            }
        }

        /// <summary>
        /// Загрузка справочников
        /// </summary>
        private async void LoadRef()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "CuttingStamp");
            q.Request.SetParam("Action", "LoadRef");

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
                    var factoryDS = ListDataSet.Create(result, "FACTORY");
                    FactoryItems.Add("0", "ВСЕ");
                    foreach (var item in factoryDS.Items)
                    {
                        FactoryItems.Add(item.CheckGet("ID"), item.CheckGet("NAME"));
                    }
                }
            }
        }

        /// <summary>
        /// Загрузка данных в таблицу техкарт
        /// </summary>
        private async void LoadItems()
        {
            TechCardGrid.Toolbar.IsEnabled = false;

            string archivedFlag = (bool)ShowArchivedCheckBox.IsChecked ? "1" : "0";

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "CuttingStamp");
            q.Request.SetParam("Action", "ListTk");

            q.Request.SetParam("ARCHIVED_FLAG", archivedFlag);

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
                    var ds = ListDataSet.Create(result, "LIST_TECHCARD");
                    TechCardGrid.UpdateItems(ds);
                }
            }

            TechCardGrid.Toolbar.IsEnabled = true;

        }

        /// <summary>
        /// Загрузка данных в таблицу штанцформ
        /// </summary>
        private async void StampLoadItems()
        {
            if (TechCardGrid.Items != null)
            {
                if (TechCardGrid.Items.Count > 0)
                {
                    StampItemGrid.Items.Clear();

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Rig");
                    q.Request.SetParam("Object", "CuttingStamp");
                    q.Request.SetParam("Action", "ListTkStamp");
                    q.Request.SetParam("TECHCARD_ID", TechCardGrid.SelectedItem.CheckGet("ID"));

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
                            var ds = ListDataSet.Create(result, "LIST_TK_STAMP");
                            StampGrid.UpdateItems(ds);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Загрузка данных в таблицу элементов штанцформ
        /// </summary>
        private async void StampItemLoadItems()
        {
            if (StampGrid.Items != null)
            {
                if (StampGrid.Items.Count > 0)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Rig");
                    q.Request.SetParam("Object", "CuttingStamp");
                    q.Request.SetParam("Action", "ListStampItem");
                    q.Request.SetParam("ID", StampGrid.SelectedItem.CheckGet("ID"));

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
                            var ds = ListDataSet.Create(result, "STAMP_ITEM_LIST");
                            StampItemGrid.UpdateItems(ds);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Загрузка файла техкарты
        /// </summary>
        private void ShowTechnologicalCard()
        {
            if (TechCardGrid.Items != null)
            {
                if (TechCardGrid.Items.Count > 0)
                {
                    if (TechCardGrid.SelectedItem != null)
                    {
                        var path = TechCardGrid.SelectedItem.CheckGet("TK_FILE_PATH");
                        if (!string.IsNullOrEmpty(path))
                        {
                            if (File.Exists(path))
                            {
                                Central.OpenFile(path);
                            }
                        }

                    }
                }
            }
        }

        /// <summary>
        /// Отвязывание штанцформы от техкарты
        /// </summary>
        private async void UnbindStamp()
        {
            if (StampGrid.Items != null)
            {
                if (StampGrid.Items.Count > 0)
                {
                    int techcardId = TechCardGrid.SelectedItem.CheckGet("ID").ToInt();
                    if (techcardId > 0)
                    {
                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Rig");
                        q.Request.SetParam("Object", "CuttingStamp");
                        q.Request.SetParam("Action", "SetUnbind");
                        q.Request.SetParam("TECHCARD_ID", techcardId.ToString());
                        q.Request.SetParam("STAMP_ID", StampGrid.SelectedItem.CheckGet("ID"));

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
                                if (result.ContainsKey("ITEM"))
                                {
                                    TechCardGrid.LoadItems();
                                }
                            }
                        }
                    }
                    else
                    {
                        var dw = new DialogWindow("Ошибка определения техкарты для отвязывания", "Отвязать штанцформу");
                        dw.ShowDialog();
                    }
                }
            }
        }

        private void ShowArchivedCheckBox_Click(object sender, RoutedEventArgs e)
        {
            TechCardGrid.LoadItems();
        }

        private void GridSearch_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            //RunTemplateTimeoutTimer();
        }
    }
}
