using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Production;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Вкладка учета и хранения элементов штанцформ в Кашире
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class CuttingStampKeepingKshTab : ControlBase
    {
        public CuttingStampKeepingKshTab()
        {
            InitializeComponent();
            ControlTitle = "Хранение штанцформ";
            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/cutting_stamp_keeping";
            RoleName = "[erp]rig_cutting_stamp_keep_ksh";

            FactoryId = 2;

            OnLoad = () =>
            {
                LoadRef();
                MachineGridInit();
                StampItemGridInit();
            };

            OnUnload = () =>
            {
                MachineGrid.Destruct();
                StampItemGrid.Destruct();
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
                    Title = "Обновить",
                    Description = "Обновить данные",
                    ButtonUse = true,
                    ButtonName = "RefreshButton",
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        MachineGrid.LoadItems();
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
                    Name = "export_to_excel",
                    Group = "main",
                    Enabled = true,
                    Title = "В Excel",
                    Description = "Выгрузить данные в Excel файл",
                    ButtonUse = true,
                    ButtonName = "ExportToExcelButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        StampItemGrid.ItemsExportExcel();
                    },
                });
            }
            Commander.SetCurrentGridName("StampItemGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "place",
                    Enabled = true,
                    Title = "Поставить",
                    Group = "operations",
                    Description = "Поставить свободную полумуфту в ячейку",
                    ButtonUse = true,
                    ButtonName = "PlaceButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        PlaceItem();
                    },
                    CheckEnabled = () =>
                    {
                        var result = true;
                        return result;
                    }
                });
                Commander.Add(new CommandItem()
                {
                    Name="move",
                    Enabled = true,
                    Title = "Переместить",
                    Group = "operations",
                    Description = "Поставить полумуфту в другую ячейку",
                    ButtonUse = true,
                    ButtonName = "MoveButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        MoveStampItem();

                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = StampItemGrid.SelectedItem;
                        if (row != null)
                        {
                            if (!row.CheckGet("STAMP_ITEM_ID").IsNullOrEmpty())
                            {
                                result = true;
                            }
                        }
                        return result;
                    }
                });
                Commander.Add(new CommandItem()
                {
                    Name = "setworked",
                    Enabled = true,
                    Title = "В работу",
                    Group = "setstatus",
                    Description = "Штанцформа готова к работе",
                    ButtonUse = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var id = StampItemGrid.SelectedItem.CheckGet("STAMP_ITEM_ID").ToInt();
                        if (id != 0)
                        {
                            SetStatus(1);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = StampItemGrid.SelectedItem;
                        if (row != null)
                        {
                            result = CuttingStampTab.CheckEnabledForSetStatus(row.CheckGet("STATUS_ID").ToInt(), 1);
                        }
                        return result;
                    }
                });
                Commander.Add(new CommandItem()
                {
                    Name = "setdisposed",
                    Enabled = false,
                    Title = "Утилизирован",
                    Group = "setstatus",
                    Description = "Штанцформа утилизирована",
                    ButtonUse = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var id = StampItemGrid.SelectedItem.CheckGet("STAMP_ITEM_ID").ToInt();
                        if (id != 0)
                        {
                            SetStatus(8);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = StampItemGrid.SelectedItem;
                        if (row != null)
                        {
                            result = CuttingStampTab.CheckEnabledForSetStatus(row.CheckGet("STATUS_ID").ToInt(), 8);
                        }
                        return result;
                    }
                });
                Commander.Add(new CommandItem()
                {
                    Name = "setprepared",
                    Enabled = true,
                    Title = "Подготовить к передаче клиенту",
                    Group = "setstatus",
                    Description = "Штанцформа готовится к передаче на склад",
                    ButtonUse = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var id = StampItemGrid.SelectedItem.CheckGet("STAMP_ITEM_ID").ToInt();

                        if (id != 0)
                        {
                            var bindTransferWindow = new RigTransferPackages();
                            bindTransferWindow.ReceiverName = ControlName;
                            bindTransferWindow.RigId = id;
                            bindTransferWindow.RigType = 1;
                            bindTransferWindow.FactoryId = FactoryId;
                            bindTransferWindow.Status = 12;
                            bindTransferWindow.Bind();
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = StampItemGrid.SelectedItem;
                        if (row != null)
                        {
                            //Не передаем штанцформы, у которых плательщик Л-ПАК или Л-ПАК Кашира
                            if (!row.CheckGet("OWNER_ID").ToInt().ContainsIn(4014, 8991))
                            {
                                result = CuttingStampTab.CheckEnabledForSetStatus(row.CheckGet("STATUS_ID").ToInt(), 12);
                            }
                        }
                        return result;
                    }
                });
                Commander.Add(new CommandItem()
                {
                    Name = "setarchive",
                    Enabled = false,
                    Title = "В архив",
                    Group = "setstatus",
                    Description = "Штанцформа архивная",
                    ButtonUse = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var id = StampItemGrid.SelectedItem.CheckGet("STAMP_ITEM_ID").ToInt();
                        if (id != 0)
                        {
                            SetStatus(10);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = StampItemGrid.SelectedItem;
                        if (row != null)
                        {
                            result = CuttingStampTab.CheckEnabledForSetStatus(row.CheckGet("STATUS_ID").ToInt(), 10);
                        }
                        return result;
                    }
                });
                Commander.Add(new CommandItem()
                {
                    Name = "settransferredtootherfactory",
                    Enabled = false,
                    Title = "Подготовить к передаче на другую площадку",
                    Group = "setstatus",
                    Description = "Штанцформа передаётся на другую площадку",
                    ButtonUse = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = StampItemGrid.GetPrimaryKey();
                        var id = StampItemGrid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            PrepareTransferOtherFactory();
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = StampItemGrid.SelectedItem;
                        if (row != null)
                        {
                            result = CuttingStampTab.CheckEnabledForSetStatus(row.CheckGet("STATUS_ID").ToInt(), 16);
                        }
                        return result;
                    }
                });
                Commander.Add(new CommandItem()
                {
                    Name = "printlabel",
                    Enabled = true,
                    Title = "Печатать ярлык",
                    Group = "service",
                    Description = "Распечатать ярлык",
                    ButtonUse = true,
                    ButtonName = "PrintLabelButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var row = StampItemGrid.SelectedItem;
                        if (row != null)
                        {
                            var id = row.CheckGet("STAMP_ITEM_ID").ToInt();
                            if (id > 0)
                            {
                                PrintLabel(id);
                            }
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = StampItemGrid.SelectedItem;
                        if (row != null)
                        {
                            if (!row.CheckGet("STAMP_ITEM_ID").IsNullOrEmpty())
                            {
                                result = true;
                            }
                        }
                        return result;
                    }
                });
                Commander.Add(new CommandItem()
                {
                    Name = "showtechcard",
                    Enabled = true,
                    Title = "Показать техкарту",
                    Group = "service",
                    Description = "Показать техкарту",
                    ButtonUse = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var row = StampItemGrid.SelectedItem;
                        if (row != null)
                        {
                            var path = row.CheckGet("TECHCARD_PATH");
                            Central.OpenFile(path);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = StampItemGrid.SelectedItem;
                        if (row != null)
                        {
                            var path = row.CheckGet("TECHCARD_PATH");
                            if (File.Exists(path))
                            {
                                result = true;
                            }
                        }
                        return result;
                    }
                });
                Commander.Add(new CommandItem()
                {
                    Name = "showprevious",
                    Enabled = true,
                    Title = "Предыдущие задания",
                    Group = "service",
                    Description = "Список предыдущих заданий",
                    ButtonUse = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        var id = StampItemGrid.SelectedItem.CheckGet("STAMP_ITEM_ID").ToInt();
                        if (id != 0)
                        {
                            var previousTaskFrame = new CuttingStampItemUsage();
                            previousTaskFrame.ReceiverName = ControlName;
                            previousTaskFrame.StampItemId = id;
                            previousTaskFrame.ShowTab();
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = StampItemGrid.SelectedItem;
                        if (row != null)
                        {
                            result = true;
                        }
                        return result;
                    }
                });
                Commander.Add(new CommandItem()
                {
                    Name = "set_maintenance",
                    Enabled = false,
                    Title = "Cледующее тех. обслуживание",
                    Group = "maintenance",
                    Description = "Указать следующее тех. обслуживание",
                    ButtonUse = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        SetMaintenance();
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;

                        if (StampItemGrid != null && StampItemGrid.SelectedItem != null)
                        {
                            if (!string.IsNullOrEmpty(StampItemGrid.SelectedItem.CheckGet("STAMP_ITEM_ID")))
                            {
                                result = true;
                            }
                        }

                        return result;
                    }
                });
                Commander.Add(new CommandItem()
                {
                    Name = "editcellname",
                    Enabled = false,
                    Title = "Переименовать ячейку",
                    Group = "maintenance",
                    ButtonUse = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var row = StampItemGrid.SelectedItem;
                        var id = row.CheckGet("ID").ToInt();
                        if (id > 0)
                        {
                            var d = new Dictionary<string, string>
                            {
                                { "ID", id.ToString() },
                                { "CELL_NUM", row.CheckGet("RACK_NUM") },
                                { "CELL_PLACE", row.CheckGet("PLACE_NUM") },
                                { "OLD_NUM", row.CheckGet("OLD_NUM") },
                                { "STAMP_ITEM_ID", row.CheckGet("STAMP_ITEM_ID") },
                            };
                            var cellEditWindow = new CuttingStampCellEdit();
                            cellEditWindow.ReceiverName = ControlName;
                            cellEditWindow.Edit(d);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = true;
                        return result;
                    }
                });
            }

            Commander.Init(this);
        }

        /// <summary>
        /// Идентификатор производственной площадки: 1 - Липецк, 2 - Кашира
        /// </summary>
        public int FactoryId { get; set; }

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
                        MachineGrid.LoadItems();
                        break;
                    case "refreshstamp":
                        StampItemGrid.LoadItems();
                        break;
                }
            }
        }

        /// <summary>
        /// Загрузка справочников для фильтров
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
                {
                    if (result != null)
                    {
                        // Заполняем список статусов
                        var statusDS = ListDataSet.Create(result, "STATUS_LIST");
                        var statusList = new Dictionary<string, string> { { "0", "Все" } };
                        foreach (var item in statusDS.Items)
                        {
                            statusList.CheckAdd(item.CheckGet("ID"), item.CheckGet("NAME"));
                        }
                        Status.Items = statusList;
                        // По умолчанию - Все
                        Status.SetSelectedItemByKey("0");
                    }
                }
            }
        }

        /// <summary>
        /// Инициализация таблицы станков (групп стелажей)
        /// </summary>
        private void MachineGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn()
                {
                    Header="Место хранения",
                    Path="MACHINE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn()
                {
                    Header="Количество ячеек",
                    Path="CELL_QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn()
                {
                    Header="Занято ячеек",
                    Path="BUISY_QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn()
                {
                    Header="ИД",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                },
            };
            MachineGrid.SetColumns(columns);
            MachineGrid.SetPrimaryKey("ID");
            MachineGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            MachineGrid.EnableSortingGrid = false;
            MachineGrid.Toolbar = StampToolbar;
            MachineGrid.Commands = Commander;
            MachineGrid.AutoUpdateInterval = 600;

            MachineGrid.OnLoadItems = MachineLoadItems;
            MachineGrid.OnSelectItem = (selectItem) =>
            {
                LoadStampItems();
            };

            MachineGrid.Init();
        }

        /// <summary>
        /// Инициализация таблицы ячеек
        /// </summary>
        private void StampItemGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn()
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn()
                {
                    Header="Стеллаж",
                    Path="RACK_NUM",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn()
                {
                    Header="Номер места",
                    Path="PLACE_NUM",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn()
                {
                    Header="Полумуфта",
                    Path="STAMP_ITEM_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=39,
                },
                new DataGridHelperColumn()
                {
                    Header="Статус",
                    Path="STATUS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
                },
                new DataGridHelperColumn()
                {
                    Header="Дата статуса",
                    Description="Дата изменения статуса",
                    Path="STATUS_CHANGED_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=14,
                    Format="dd.MM.yyyy HH:mm:ss",
                },
                new DataGridHelperColumn()
                {
                    Header="Номер заказа",
                    Path="ORDER_NUM",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn()
                {
                    Header="Штанцформа",
                    Path="STAMP_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=26,
                },
                new DataGridHelperColumn()
                {
                    Header="Прогонов",
                    Description="Количество прогонов",
                    Path="STAMP_ITEM_USAGE_CNT",
                    ColumnType=ColumnTypeRef.Double,
                    Width2=8,
                    Format="N0",
                },
                new DataGridHelperColumn()
                {
                    Header="Тех. обслуживание",
                    Description="Количество прогонов до следующего тех. обслуживания",
                    Path="STAMP_ITEM_MAINTENANCE_CNT",
                    ColumnType=ColumnTypeRef.Double,
                    Width2=8,
                    Format="N0",
                },
                new DataGridHelperColumn()
                {
                    Header="Начало эксплуатации",
                    Description="Дата начала эксплуатации",
                    Path="STAMP_ITEM_BEGIN_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=14,
                    Format="dd.MM.yyyy HH:mm:ss",
                },
                new DataGridHelperColumn()
                {
                    Header="Ид полумуфты",
                    Path="STAMP_ITEM_ID",
                    Doc="Идентификатор полумуфты",
                    ColumnType=ColumnTypeRef.Double,
                    Format = "N0",
                    Width2=6,
                },
                new DataGridHelperColumn()
                {
                    Header="Ид штанцформы",
                    Path="STAMP_ID",
                    ColumnType=ColumnTypeRef.Double,
                    Format="N0",
                    Width2=6,
                },
            };
            StampItemGrid.SetColumns(columns);
            StampItemGrid.SetPrimaryKey("_ROWNUMBER");
            StampItemGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            StampItemGrid.AutoUpdateInterval = 0;
            StampItemGrid.SearchText = GridSearch;
            StampItemGrid.Commands = Commander;

            StampItemGrid.OnLoadItems = LoadStampItems;
            StampItemGrid.OnFilterItems = () =>
            {
                if (StampItemGrid.Items != null && StampItemGrid.Items.Count > 0)
                {
                    if (Status != null && Status.SelectedItem.Key != null)
                    {
                        var key = Status.SelectedItem.Key.ToInt();
                        var items = new List<Dictionary<string, string>>();

                        switch (key)
                        {
                            // Все статусы
                            case 0:
                                items = StampItemGrid.Items;
                                break;

                            default:
                                items.AddRange(StampItemGrid.Items.Where(x => x.CheckGet("STATUS_ID").ToInt() == key));
                                break;
                        }

                        StampItemGrid.Items = items;
                    }
                }
            };

            StampItemGrid.Init();
        }

        /// <summary>
        /// Загрузка данных в таблицу станков
        /// </summary>
        private async void MachineLoadItems()
        {
            MachineGrid.Toolbar.IsEnabled = false;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "CuttingStamp");
            q.Request.SetParam("Action", "ListMachine");
            q.Request.SetParam("FACTORY_ID", FactoryId.ToString());

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
                    var ds = ListDataSet.Create(result, "MACHINE");
                    MachineGrid.UpdateItems(ds);
                }
            }

            MachineGrid.Toolbar.IsEnabled = true;
        }

        /// <summary>
        /// Загрузка данных в таблицу ячеек
        /// </summary>
        private async void LoadStampItems()
        {
            int machineId = MachineGrid.SelectedItem.CheckGet("ID").ToInt();
            if (machineId > 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Rig");
                q.Request.SetParam("Object", "CuttingStamp");
                q.Request.SetParam("Action", "ListCell");
                q.Request.SetParam("MACHINE_ID", machineId.ToString());

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
                        var ds = ListDataSet.Create(result, "LIST_CELL");
                        StampItemGrid.UpdateItems(ds);
                    }
                }
            }
        }

        /// <summary>
        /// Изменение статуса
        /// </summary>
        /// <param name="newStatus"></param>
        private void SetStatus(int newStatus)
        {
            var row = StampItemGrid.SelectedItem;
            if (row != null)
            {
                bool result = CuttingStampTab.SetStampItemStatus(newStatus, row.CheckGet("STAMP_ITEM_ID").ToInt(), row.CheckGet("STATUS_ID").ToInt(), row.CheckGet("ID"), row.CheckGet("RACK_NUM"), this.FactoryId);
                if (result)
                {
                    StampItemGrid.LoadItems();
                }
            }
        }

        /// <summary>
        /// Установить ячейку для элемента штанцформы
        /// </summary>
        private void MoveStampItem()
        {
            var row = StampItemGrid.SelectedItem;
            if (row != null)
            {
                bool result = CuttingStampTab.MoveStampItem(row.CheckGet("STAMP_ITEM_ID").ToInt(), row.CheckGet("ID"), row.CheckGet("RACK_NUM"));
                if (result)
                {
                    StampItemGrid.LoadItems();
                }
            }
        }

        /// <summary>
        /// Подготовка штанцформы к передаче на другую площадку
        /// </summary>
        public async void PrepareTransferOtherFactory()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "RigTransfer");
            q.Request.SetParam("Action", "PrepareForOtherFactory");
            q.Request.SetParam("STAMP_ID", StampItemGrid.SelectedItem.CheckGet("STAMP_ID"));

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
                        StampItemGrid.LoadItems();
                    }
                }
            }
            else if (q.Answer.Error.Code == 145)
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Поместить полумуфту в ячейку
        /// </summary>
        private void PlaceItem()
        {
            bool resume = false;

            var id = StampItemGrid.SelectedItem.CheckGet("STAMP_ITEM_ID").ToInt();
            if (id > 0)
            {
                var dw = new DialogWindow("Ячейка занята. Заменить в ней полумуфту?", "Поставить в ячейку", "", DialogWindowButtons.YesNo);
                if ((bool)dw.ShowDialog())
                {
                    if (dw.ResultButton == DialogResultButton.Yes)
                    {
                        resume = true;
                    }
                }
            }
            else
            {
                resume = true;
            }

            if (resume)
            {
                var cellFreeFrame = new CuttingStampKeepingSelectFreeStamp();
                cellFreeFrame.ReceiverName = ControlName;
                cellFreeFrame.MachineId = MachineGrid.SelectedItem.CheckGet("ID").ToInt();
                cellFreeFrame.CellId = StampItemGrid.SelectedItem.CheckGet("ID").ToInt();
                cellFreeFrame.Show();
            }
        }

        /// <summary>
        /// Изменение срока следующего тех. обслуживания
        /// </summary>
        private async void SetMaintenance()
        {
            if (StampItemGrid != null && StampItemGrid.SelectedItem != null)
            {
                string stampItemId = StampItemGrid.SelectedItem.CheckGet("STAMP_ITEM_ID");

                int defaultQuantity = StampItemGrid.SelectedItem.CheckGet("STAMP_ITEM_MAINTENANCE_CNT").ToInt();
                int maintenanceQuantity = 0;
                var i = new ComplectationCMQuantity(defaultQuantity, false);
                i.Show("Прогонов");
                if (i.OkFlag)
                {
                    maintenanceQuantity = i.QtyInt;

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Rig");
                    q.Request.SetParam("Object", "CuttingStamp");
                    q.Request.SetParam("Action", "SetMaintenance");
                    q.Request.SetParam("ID", stampItemId);
                    q.Request.SetParam("MAINTENANCE_CNT", $"{maintenanceQuantity}");

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    if (q.Answer.Status == 0)
                    {
                        bool succesfullFlag = false;

                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            var ds = ListDataSet.Create(result, "ITEM");
                            if (ds != null && ds.Items != null && ds.Items.Count > 0)
                            {
                                if (!string.IsNullOrEmpty(ds.Items[0].CheckGet("ID")))
                                {
                                    succesfullFlag = true;
                                }
                            }
                        }

                        if (succesfullFlag)
                        {
                            StampItemGrid.LoadItems();
                        }
                        else
                        {
                            DialogWindow.ShowDialog("При установке следующего тех. обслуживания произошла ошибка. Пожалуйста, сообщите о проблеме.", this.ControlTitle, "", DialogWindowButtons.OK);
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
            }
        }

        /// <summary>
        /// Получение файла для печати ярлыка
        /// </summary>
        /// <param name="id"></param>
        private async void PrintLabel(int id)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "CuttingStamp");
            q.Request.SetParam("Action", "PrintLabel");
            q.Request.SetParam("ID", id.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Central.OpenFile(q.Answer.DownloadFilePath);
            }

        }

        private void Status_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            StampItemGrid.UpdateItems();
        }
    }
}
