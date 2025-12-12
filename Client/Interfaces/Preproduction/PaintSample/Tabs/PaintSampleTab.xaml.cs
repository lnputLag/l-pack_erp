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

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Список заявок на изготовление выкрасов для клиентов
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class PaintSampleTab : ControlBase
    {
        public PaintSampleTab()
        {
            InitializeComponent();

            ControlTitle = "Заявки на выкрасы";
            RoleName = "[erp]paint_sample";
            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/osn_calculation";

            OnLoad = () =>
            {
                LoadRef();
                SetDefaults();
                InitGrid();
                InitPaintGrid();
            };

            OnUnload = () =>
            {
                Grid.Destruct();
                PaintGrid.Destruct();
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

            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverName == ControlName)
                {
                    ProcessMessage(msg);
                }
            };

            Commander.SetCurrentGroup("main");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "refresh",
                    Group = "grid_base",
                    Enabled = true,
                    Title = "Показать",
                    Description = "Загрузить данные",
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
            }
            Commander.SetCurrentGridName("Grid");
            {
                //Commander.SetCurrentGroup("item");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "create",
                        Title = "Создать",
                        Group = "item",
                        MenuUse = true,
                        HotKey = "Insert",
                        ButtonUse = true,
                        ButtonName = "CreateButton",
                        Description = "Создание новой заявки на выкрасы",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var paintSampleFrame = new PaintSample();
                            paintSampleFrame.ReceiverName = ControlName;
                            paintSampleFrame.Edit();
                        },
                        CheckEnabled = () =>
                        {
                            var result = true;
                            return result;
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "edit",
                        Title = "Изменить",
                        Group = "item",
                        MenuUse = true,
                        HotKey = "Return|DoubleCLick",
                        ButtonUse = true,
                        ButtonName = "EditButton",
                        Description = "Внесение изменений в заявку на выкрасы",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var k = Grid.GetPrimaryKey();
                            var id = Grid.SelectedItem.CheckGet(k).ToInt();
                            if (id != 0)
                            {
                                var paintSampleFrame = new PaintSample();
                                paintSampleFrame.ReceiverName = ControlName;
                                paintSampleFrame.Edit(id);
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var k = Grid.GetPrimaryKey();
                            var row = Grid.SelectedItem;
                            if (row.CheckGet(k).ToInt() != 0)
                            {
                                result = true;
                            }
                            return result;
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "print",
                        Title = "Печать",
                        Group = "item",
                        MenuUse = true,
                        ButtonUse = true,
                        ButtonName = "PrintButton",
                        Description = "Печать документа",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var k = Grid.GetPrimaryKey();
                            var id = Grid.SelectedItem.CheckGet(k).ToInt();
                            if (id != 0)
                            {
                                GetDocument(id);
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var k = Grid.GetPrimaryKey();
                            var row = Grid.SelectedItem;
                            if (row.CheckGet(k).ToInt() != 0)
                            {
                                result = true;
                            }
                            return result;
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "setwork",
                        Title = "В работу",
                        Group = "setstatus",
                        MenuUse = true,
                        ButtonUse = false,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            SetStatus(2);
                        },
                        CheckEnabled = () =>
                        {
                            var result = true;

                            return result;
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "setcompleted",
                        Title = "Завершить",
                        Group = "setstatus",
                        MenuUse = true,
                        ButtonUse = false,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            SetStatus(3);
                        },
                        CheckEnabled = () =>
                        {
                            var result = true;

                            return result;
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "setrejected",
                        Title = "Отклонить",
                        Group = "setstatus",
                        MenuUse = true,
                        ButtonUse = false,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            SetStatus(4);
                        },
                        CheckEnabled = () =>
                        {
                            var result = true;

                            return result;
                        },
                    });
                }
            }
            Commander.Init(this);
        }

        /// <summary>
        /// Обработка и выполнение команд
        /// </summary>
        /// <param name="command"></param>
        public void ProcessMessage(ItemMessage msg)
        {
            string command = msg.Action;
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "refresh":
                        Grid.LoadItems();
                        break;
                }
            }
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            FromDate.Text = DateTime.Now.AddDays(-30).ToString("dd.MM.yyyy");
            ToDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
        }

        /// <summary>
        /// Получение списка групп, в которые входит пользователь
        /// </summary>
        private async void LoadRef()
        {
            // Активный сотрудник
            string emplId = Central.User.EmployeeId.ToString();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PaintSample");
            q.Request.SetParam("Action", "GetRef");
            q.Request.SetParam("EMPLOYEE_ID", emplId);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            }
            );

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    // менеджеры по работе с клиентами
                    var managersDS = ListDataSet.Create(result, "MANAGERS");
                    var managers = new Dictionary<string, string>()
                    {
                        { "-1", "Все" },
                    };

                    foreach (var item in managersDS.Items)
                    {
                        managers.CheckAdd(item["ID"].ToInt().ToString(), item["FIO"]);
                    }

                    ManagerName.Items = managers;
                    // Если активный пользователь есть в списке, установим его в выбранном значении
                    if (managers.ContainsKey(emplId))
                    {
                        ManagerName.SetSelectedItemByKey(emplId);
                    }
                    else
                    {
                        ManagerName.SetSelectedItemByKey("-1");
                    }
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
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="Дата создания",
                    Path="CREATED_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=12,
                    Format="dd.MM.yy HH:mm",
                },
                new DataGridHelperColumn
                {
                    Header="Заказчик",
                    Path="CUSTOMER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=25,
                },
                new DataGridHelperColumn
                {
                    Header="Тип",
                    Path="DEMAND_TYPE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="Менеджер",
                    Path="MANAGER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=18,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание",
                    Path="NOTE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=60,
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
                    Header="Технолог",
                    Path="TECHNOLOG_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
                },

            };

            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("ID");
            Grid.SetSorting("ID", ListSortDirection.Ascending);
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.SearchText = GridSearch;
            Grid.Toolbar = GridToolbar;
            Grid.Commands = Commander;
            // Раскраска строк
            Grid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                // Цвета фона строк
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=System.Windows.DependencyProperty.UnsetValue;
                        var color = "";
                        int currentStatus = row.CheckGet("STATUS_ID").ToInt();

                        // готова
                        if (currentStatus == 3)
                        {
                            color = HColor.Green;
                        }
                        // В работе
                        if (currentStatus == 2)
                        {
                            color = HColor.Yellow;
                        }
                        // Отменена
                        if (currentStatus == 4)
                        {
                            color = HColor.Red;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };

            Grid.OnLoadItems = LoadItems;
            Grid.OnFilterItems = FilterItems;
            Grid.OnSelectItem = selectedItem =>
            {
                LoadItemsItems();
            };
            Grid.Init();
        }

        /// <summary>
        /// Загрузка данных в таблицу заявок на выкрасы
        /// </summary>
        private async void LoadItems()
        {
            Grid.Toolbar.IsEnabled = false;

            if (FromDate.Text.IsNullOrEmpty())
            {
                FromDate.Text = DateTime.Now.AddDays(-30).ToString("dd.MM.yyyy");
            }
            if (ToDate.Text.IsNullOrEmpty())
            {
                ToDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
            }

            // Очистка подчиненной таблицы
            PaintGrid.ClearItems();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PaintSample");
            q.Request.SetParam("Action", "List");
            q.Request.SetParam("FROM_DATE", FromDate.Text);
            q.Request.SetParam("TO_DATE", ToDate.Text);

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "PAINT_SAMPLES");
                    Grid.UpdateItems(ds);
                }
            }

            Grid.Toolbar.IsEnabled = true;
            RefreshButton.Style = (Style)RefreshButton.TryFindResource("Button");
        }

        /// <summary>
        /// Инициализация таблицы с выкрасами
        /// </summary>
        private void InitPaintGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="Цвет бумаги",
                    Path="RAW_COLOR",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Тип бумаги",
                    Path="RAW_TYPE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Краска",
                    Path="PAINT_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
            };
            PaintGrid.SetColumns(columns);
            PaintGrid.SetPrimaryKey("ID");
            PaintGrid.SetSorting("PAINT_ORDER", ListSortDirection.Ascending);
            PaintGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            PaintGrid.Commands = Commander;
            PaintGrid.AutoUpdateInterval = 0;
            PaintGrid.Init();
        }

        /// <summary>
        /// Загрузка данных в таблицу выкрасов в заявке
        /// </summary>
        private async void LoadItemsItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PaintSample");
            q.Request.SetParam("Action", "ListItems");
            q.Request.SetParam("ID", Grid.SelectedItem.CheckGet("ID"));

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "PAINT_SMP_ITEMS");
                    PaintGrid.UpdateItems(ds);
                }
            }
        }

        /// <summary>
        /// Фильтрация элементов в таблицу заявок
        /// </summary>
        private void FilterItems()
        {
            if (Grid.Items != null)
            {
                if (Grid.Items.Count > 0)
                {
                    bool doFilteringByManager = false;
                    var managerId = ManagerName.SelectedItem.Key.ToInt();
                    if (managerId > 0)
                    {
                        doFilteringByManager = true;
                    }

                    if (doFilteringByManager)
                    {
                        var items = new List<Dictionary<string, string>>();
                        var gridItems = Grid.GetItems();
                        foreach (Dictionary<string, string> row in Grid.Items)
                        {
                            bool includeByManager = true;
                            if (doFilteringByManager)
                            {
                                includeByManager = false;
                                if (row.CheckGet("MANAGER_ID").ToInt() == managerId)
                                {
                                    includeByManager = true;
                                }
                            }

                            if (includeByManager)
                            {
                                items.Add(row);
                            }
                        }

                        Grid.Items = items;
                    }
                }
            }
        }

        /// <summary>
        /// Изменение статуса заявки на выкрасы
        /// </summary>
        /// <param name="newStatus"></param>
        private async void SetStatus(int newStatus)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PaintSample");
            q.Request.SetParam("Action", "SetStatus");
            q.Request.SetParam("ID", Grid.SelectedItem.CheckGet("ID"));
            q.Request.SetParam("STATUS", newStatus.ToString());

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
                        PaintGrid.LoadItems();
                    }
                }
            }
        }

        /// <summary>
        /// Получение печатной формы документа для выкрасов
        /// </summary>
        /// <param name="id"></param>
        private async void GetDocument(int id)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PaintSample");
            q.Request.SetParam("Action", "GetDocument");
            q.Request.SetParam("ID", Grid.SelectedItem.CheckGet("ID"));
            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                string filePath = q.Answer.DownloadFilePath;
                if (!filePath.IsNullOrEmpty())
                {
                    Central.OpenFile(filePath);
                }
            }

        }

        private void ManagerName_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void DateChanged(object sender, TextChangedEventArgs e)
        {
            RefreshButton.Style = (Style)RefreshButton.TryFindResource("FButtonPrimary");
        }
    }
}
