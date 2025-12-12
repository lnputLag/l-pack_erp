using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Client.Interfaces.Main.DataGridHelperColumn;


namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Управление клише литой тары
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class ContainerClicheTab : ControlBase
    {
        public ContainerClicheTab()
        {
            InitializeComponent();
            ControlTitle = "Операции с клише ЛТ";
            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/clents_samples";
            RoleName = "[erp]rig_contner_control";

            OnLoad = () =>
            {
                LoadRef();
                ProductGridInit();
                ClicheGridInit();
            };

            OnUnload = () =>
            {
                ProductGrid.Destruct();
                ClicheGrid.Destruct();
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
                ProductGrid.ItemsAutoUpdate = true;
                ProductGrid.Run();
            };

            OnFocusLost = () =>
            {
                ProductGrid.ItemsAutoUpdate = false;
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
                        ProductGrid.LoadItems();
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
            Commander.SetCurrentGridName("ClicheGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "techcard",
                    Title = "Техкарта",
                    Group = "operations",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonName = "TechCardButton",
                    Description = "Открыть техкарту",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        var k = ProductGrid.GetPrimaryKey();
                        var id = ProductGrid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            ShowTechnologicalCard();
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = ProductGrid.SelectedItem;
                        if (row != null)
                        {
                            result = !row.CheckGet("TECHCARD_PATH").IsNullOrEmpty();
                        }
                        return result;
                    },
                });
            }
            Commander.SetCurrentGridName("ClicheGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "edit",
                    Title = "Изменить",
                    Group = "item",
                    MenuUse = true,
                    HotKey = "Return|DoubleCLick",
                    ButtonUse = true,
                    ButtonName = "EditButton",
                    Description = "Изменение данных клише",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = ClicheGrid.GetPrimaryKey();
                        var id = ClicheGrid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            SetCell();
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = ClicheGrid.SelectedItem;
                        if (row != null)
                        {
                            var currentStatusId = row.CheckGet("STATUS_ID").ToInt();
                            if (currentStatusId.ContainsIn(7, 1, 9, 10, 12))
                            {
                                result = true;
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "workstatus",
                    Title="В работу",
                    Group="changestatus",
                    MenuUse = true,
                    ButtonUse=false,
                    Description="Установить статус В работе",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = ClicheGrid.GetPrimaryKey();
                        var id = ClicheGrid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            //Проверяем, что заполнены ячейка
                            string sellNum = ClicheGrid.SelectedItem.CheckGet("CELL_NUM");
                            if (!sellNum.IsNullOrEmpty())
                            {
                                SetStatus(1);
                            }
                            else
                            {
                                var dw = new DialogWindow("Перед от правкой в работу клише надо поместить в ячейки", "Смена статуса");
                                dw.ShowDialog();
                            }
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = ClicheGrid.SelectedItem;
                        if (row != null)
                        {
                            var currentStatusId = row.CheckGet("STATUS_ID").ToInt();
                            if (currentStatusId.ContainsIn(7,9,10,12,13))
                            {
                                result = true;
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "repairstatus",
                    Title = "В ремонт",
                    Group = "changestatus",
                    MenuUse = true,
                    ButtonUse = false,
                    Description = "Установить статус В ремонт",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        SetStatus(9);
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = ClicheGrid.SelectedItem;
                        if (row != null)
                        {
                            var currentStatusId = row.CheckGet("STATUS_ID").ToInt();
                            if (currentStatusId.ContainsIn(1, 10, 12, 13))
                            {
                                result = true;
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "transferstatus",
                    Title = "Готова к передаче",
                    Group = "changestatus",
                    MenuUse = true,
                    ButtonUse = false,
                    Description = "Установить статус Готова к передаче",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        SetStatus(12);
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = ClicheGrid.SelectedItem;
                        if (row != null)
                        {
                            var currentStatusId = row.CheckGet("STATUS_ID").ToInt();
                            if (currentStatusId.ContainsIn(1, 10))
                            {
                                result = true;
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "archivestatus",
                    Title = "В архив",
                    Group = "changestatus",
                    MenuUse = true,
                    ButtonUse = false,
                    Description = "Установить статус В архиве",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        SetStatus(10);
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = ClicheGrid.SelectedItem;
                        if (row != null)
                        {
                            var currentStatusId = row.CheckGet("STATUS_ID").ToInt();
                            if (currentStatusId.ContainsIn(1))
                            {
                                result = true;
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "scrapstatus",
                    Title = "Утилизировать",
                    Group = "changestatus",
                    MenuUse = true,
                    ButtonUse = false,
                    Description = "Установить статус Утилизирован",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        SetStatus(8);
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = ClicheGrid.SelectedItem;
                        if (row != null)
                        {
                            var currentStatusId = row.CheckGet("STATUS_ID").ToInt();
                            if (currentStatusId.ContainsIn(1, 9, 10))
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
                        ProductGrid.LoadItems();
                        break;
                    case "refreshcliche":
                        ClicheGrid.LoadItems();
                        break;
                }
            }
        }

        /// <summary>
        /// Инициализация таблицы техкарт
        /// </summary>
        private void ProductGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn()
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn()
                {
                    Header="Артикул",
                    Path="SKU_CODE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn()
                {
                    Header="Покупатель",
                    Path="CUSTOMER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=26,
                },
                new DataGridHelperColumn()
                {
                    Header="Техкарта",
                    Path="TECHCARD_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn()
                {
                    Header="Дата следующей отгрузки",
                    Path="SHIPMENT_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=14,
                    Doc="Дата получения образца",
                    Format="dd.MM.yyyy HH:mm",
                },
            };
            ProductGrid.SetColumns(columns);
            ProductGrid.SetPrimaryKey("ID");
            ProductGrid.SetSorting("ID", ListSortDirection.Descending);
            ProductGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            ProductGrid.SearchText = SearchText;
            ProductGrid.Commands = Commander;

            //данные грида
            ProductGrid.OnLoadItems = ProductGridLoadItems;
            ProductGrid.OnSelectItem = selectedItem =>
            {
                ClicheGrid.LoadItems();
            };

            ProductGrid.Init();
        }

        /// <summary>
        /// Инициализация таблицы списка клише выбранной техкарты
        /// </summary>
        private void ClicheGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn()
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn()
                {
                    Header="_",
                    Path="_SELECTED",
                    ColumnType=ColumnTypeRef.Boolean,
                    Editable=true,
                    Width2=4,
                },

                new DataGridHelperColumn()
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn()
                {
                    Header="Название",
                    Path="CLICHE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn()
                {
                    Header="Статус",
                    Path="STATUS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=14,
                },
                new DataGridHelperColumn()
                {
                    Header="Станок",
                    Path="MACHINE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=6,
                },
                new DataGridHelperColumn()
                {
                    Header="Ячейка",
                    Path="CELL_NUM",
                    ColumnType=ColumnTypeRef.String,
                    Width2=6,
                },
                new DataGridHelperColumn()
                {
                    Header="Место печати",
                    Path="SPOT_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=14,
                },
                new DataGridHelperColumn()
                {
                    Header="Цвет печати",
                    Path="COLOR_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
            };
            ClicheGrid.SetColumns(columns);
            ClicheGrid.SetPrimaryKey("ID");
            ClicheGrid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
            ClicheGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            ClicheGrid.Commands = Commander;
            //Без автообновления
            ClicheGrid.AutoUpdateInterval = 0;
            ClicheGrid.OnLoadItems = ClicheLoadItems;
            ClicheGrid.OnFilterItems = FilterClicheItems;

            ClicheGrid.Init();
        }

        /// <summary>
        /// Загрузка справочников для фильтров
        /// </summary>
        private async void LoadRef()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "ContainerRig");
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
                {
                    if (result != null)
                    {
                        // Заполняем список станков
                        var machineDS = ListDataSet.Create(result, "MACHINE_LIST");
                        var machineList = new Dictionary<string, string> { { "0", "Все" } };
                        foreach (var item in machineDS.Items)
                        {
                            machineList.CheckAdd(item.CheckGet("ID"), item.CheckGet("NAME"));
                        }
                        Machine.Items = machineList;
                        Machine.SetSelectedItemByKey("0");

                        // Заполняем список статусов
                        var statusDS = ListDataSet.Create(result, "STATUS_LIST");
                        var statusList = new Dictionary<string, string> { { "0", "Все" } };
                        foreach (var item in statusDS.Items)
                        {
                            statusList.CheckAdd(item.CheckGet("ID"), item.CheckGet("NAME"));
                        }
                        Status.Items = statusList;
                        Status.SetSelectedItemByKey("0");
                    }
                }
            }
        }

        /// <summary>
        /// Загрузка данных в таблицу техкарт
        /// </summary>
        private async void ProductGridLoadItems()
        {
            bool archivedFlag = (bool)ArchivedCheckBox.IsChecked;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "ContainerRig");
            q.Request.SetParam("Action", "ListTechcardPrinting");
            q.Request.SetParam("ARCHIVED_FLAG", archivedFlag ? "1" : "0");

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                ClicheGrid.ClearItems();
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "TECHCARD_LIST");
                    ProductGrid.UpdateItems(ds);
                }
            }
        }

        /// <summary>
        /// Загрузка жанных в таблицу клише
        /// </summary>
        private async void ClicheLoadItems()
        {
            if (ProductGrid.Items != null)
            {
                if (ProductGrid.Items.Count > 0)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Rig");
                    q.Request.SetParam("Object", "ContainerRig");
                    q.Request.SetParam("Action", "ClicheListByTechcard");
                    q.Request.SetParam("TECHCARD_ID", ProductGrid.SelectedItem.CheckGet("ID"));

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
                            var ds = ListDataSet.Create(result, "CLICHE_LIST");
                            ClicheGrid.UpdateItems(ds);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Фильтрация строк табщицы
        /// </summary>
        public void FilterClicheItems()
        {
            if (ClicheGrid.Items != null)
            {
                if (ClicheGrid.Items.Count > 0)
                {
                    bool doFilteringByMachine = false;
                    bool doFilteringByStatus = false;

                    int machineId = Machine.SelectedItem.Key.ToInt();
                    if (machineId > 0)
                    {
                        doFilteringByMachine = true;
                    }

                    int statusId = Status.SelectedItem.Key.ToInt();
                    if (statusId > 0)
                    {
                        doFilteringByStatus = true;
                    }

                    if (doFilteringByMachine || doFilteringByStatus)
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in ClicheGrid.Items)
                        {
                            // фильтрация по станку
                            bool includeByMachine = true;
                            if (doFilteringByMachine)
                            {
                                includeByMachine = false;
                                if (row.CheckGet("MACHINE_ID").ToInt() == machineId)
                                {
                                    includeByMachine = true;
                                }
                            }

                            // фильтрация по статусу
                            bool includeByStatus = true;
                            if (doFilteringByStatus)
                            {
                                includeByStatus = false;
                                if (row.CheckGet("STATUS_ID").ToInt() == statusId)
                                {
                                    includeByMachine = true;
                                }
                            }

                            if (includeByMachine && includeByStatus)
                            {
                                items.Add(row);
                            }
                        }

                        ClicheGrid.Items = items;
                    }
                }
            }
        }

        private void SetCell()
        {
            var selectdRows = new List<Dictionary<string, string>>();
            foreach(var row in ClicheGrid.Items)
            {
                if (row.CheckGet("_SELECTED").ToBool())
                {
                    selectdRows.Add(row);
                }
            }

            if (selectdRows.Count == 0)
            {
                if (ClicheGrid.SelectedItem != null)
                {
                    selectdRows.Add(ClicheGrid.SelectedItem);
                }
            }

            if (selectdRows.Count > 0)
            {
                //Проверяем, что в выбранных строках совпадают станки
                int machineId = selectdRows[0].CheckGet("MACHINE_ID").ToInt();
                bool resume = true;
                var ids = new List<int>();
                string cellId = selectdRows[0].CheckGet("CELL_ID");

                foreach (var row in selectdRows)
                {
                    if (row.CheckGet("MACHINE_ID").ToInt() != machineId)
                    {
                        var dw = new DialogWindow("Нельзя выбрать клише от разных станков", "Редактирование клише");
                        dw.ShowDialog();
                        resume = false;
                        break;
                    }
                    ids.Add(row.CheckGet("ID").ToInt());
                    string currentCellId = row.CheckGet("CELL_ID");
                    if (cellId.IsNullOrEmpty())
                    {
                        if (!currentCellId.IsNullOrEmpty())
                        {
                            cellId = currentCellId;
                        }
                    }
                }

                if (resume)
                {
                    var values = new Dictionary<string, string>
                    {
                        { "CELL_ID", cellId },
                        { "IDS", string.Join(",", ids) }
                    };
                    var clicheCellForm = new ContainerClicheSetCell();
                    clicheCellForm.ReceiverName = ControlName;
                    clicheCellForm.Edit(values);
                }
            }
            else
            {
                var dw = new DialogWindow("Ничего не выбрано", "Редактирование клише");
                dw.ShowDialog();
            }
        }

        /// <summary>
        /// Установить новый статус
        /// </summary>
        /// <param name="newStatus"></param>
        private async void SetStatus(int newStatus)
        {
            string statusName = "";
            bool resume = true;

            foreach (var item in Status.Items)
            {
                if (item.Key.ToInt() == newStatus)
                {
                    statusName = item.Value.ToString();
                }
            }

            //Просим подтвердить перед утилизацией
            if (newStatus == 8)
            {
                var dw = new DialogWindow($"Вы действительно хотите установить статус {statusName}?", "Сменить статус", "", DialogWindowButtons.YesNo);
                if ((bool)dw.ShowDialog())
                {
                    resume = dw.ResultButton == DialogResultButton.Yes;
                }
                else
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Rig");
                q.Request.SetParam("Object", "ContainerRig");
                q.Request.SetParam("Action", "SetStatus");
                q.Request.SetParam("ID", ClicheGrid.SelectedItem.CheckGet("ID"));
                q.Request.SetParam("STATUS_ID", newStatus.ToString());

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
                            ClicheGrid.LoadItems();
                        }
                    }
                }
                else if (q.Answer.Error.Code == 145)
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// Загрузка файла техкарты
        /// </summary>
        private void ShowTechnologicalCard()
        {
            if (ProductGrid.Items != null)
            {
                if (ProductGrid.Items.Count > 0)
                {
                    if (ProductGrid.SelectedItem != null)
                    {
                        var path = ProductGrid.SelectedItem.CheckGet("TECHCARD_PATH");
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

        private void ProductGridLoadItems(object sender, RoutedEventArgs e)
        {
            ProductGrid.LoadItems();
        }

        private void ClicheGridUpdateItems(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ClicheGrid.UpdateItems();
        }

    }
}
