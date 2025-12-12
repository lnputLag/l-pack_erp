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
    /// Список штанцформ
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class CuttingStampTab : ControlBase
    {
        public CuttingStampTab()
        {
            InitializeComponent();
            ControlTitle = "Штанцформы";
            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/cutting_stamp";
            RoleName = "[erp]rig_cutting_stamp_keep";

            FactoryId = 1;
            SelectedItemForBindTransfer = new Dictionary<string, string>();

            OnLoad = () =>
            {
                LoadRef();
                MachineListInit();
                StampGridInit();
                StampItemGridInit();
                ProductGridInit();
            };

            OnUnload = () =>
            {
                ProductGrid.Destruct();
                StampGrid.Destruct();
                StampItemGrid.Destruct();
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
                StampGrid.ItemsAutoUpdate = true;
                StampGrid.Run();
            };

            OnFocusLost = () =>
            {
                StampGrid.ItemsAutoUpdate = false;
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
                        StampGrid.LoadItems();
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
                        StampGrid.ItemsExportExcel();
                    },
                });
            }
            Commander.SetCurrentGridName("StampGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "edit",
                    Enabled = true,
                    Title = "Изменить",
                    Group = "operations",
                    Description = "Изменить штанцформу",
                    HotKey = "Return|DoubleCLick",
                    ButtonUse = true,
                    ButtonName = "EditButton",
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = StampGrid.GetPrimaryKey();
                        var id = StampGrid.SelectedItem.CheckGet(k).ToInt();

                        if (id != 0)
                        {
                            var editForm = new CuttingStamp();
                            editForm.ReceiverName = ControlName;
                            editForm.Edit(id);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var k = StampGrid.GetPrimaryKey();
                        var row = StampGrid.SelectedItem;
                        if (row.CheckGet(k).ToInt() != 0
                            && row.CheckGet("STATUS_ID").ToInt() != (int)StatusRef.TransferredToOtherFactory)
                        {
                            result = true;
                        }
                        return result;
                    }
                });
                Commander.Add(new CommandItem()
                {
                    Name = "opendrawing",
                    Enabled = true,
                    Title = "Посмотреть чертеж",
                    Group = "operations",
                    Description = "Открыть файл чертежа",
                    ButtonUse = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        var k = StampGrid.GetPrimaryKey();
                        var id = StampGrid.SelectedItem.CheckGet(k).ToInt();

                        if (id != 0)
                        {
                            Central.OpenFile(StampGrid.SelectedItem.CheckGet("DRAWING_FILE"));
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = StampGrid.SelectedItem;
                        if (row != null)
                        {
                            if (!row.CheckGet("DRAWING_FILE").IsNullOrEmpty())
                            {
                                result = true;
                            }
                        }
                        return result;
                    }
                });
            }
            Commander.SetCurrentGridName("StampItemGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "edititem",
                    Enabled = true,
                    Title = "Изменить",
                    Group = "operations",
                    Description = "Изменить",
                    ButtonUse = true,
                    ButtonName="EditItemButton",
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = StampItemGrid.GetPrimaryKey();
                        var id = StampItemGrid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            var editFrame = new CuttingStampItemEdit();
                            editFrame.ReceiverName = ControlName;
                            editFrame.Edit(id);
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
                        var k = StampItemGrid.GetPrimaryKey();
                        var id = StampItemGrid.SelectedItem.CheckGet(k).ToInt();
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
                            result = CheckEnabledForSetStatus(row.CheckGet("STATUS_ID").ToInt(), 1);
                        }
                        return result;
                    }
                });
                /*
                Commander.Add(new CommandItem()
                {
                    Name = "setrepaire",
                    Enabled = true,
                    Title = "В ремонт",
                    Group = "setstatus",
                    Description = "Штанцформа повреждена",
                    ButtonUse = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = StampItemGrid.GetPrimaryKey();
                        var id = StampItemGrid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            SetStatus(9);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = StampItemGrid.SelectedItem;
                        if (row != null)
                        {
                            result = CheckEnabledForSetStatus(row.CheckGet("STATUS_ID").ToInt(), 9);
                        }
                        return result;
                    }
                });
                */
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
                        var k = StampItemGrid.GetPrimaryKey();
                        var id = StampItemGrid.SelectedItem.CheckGet(k).ToInt();

                        if (id != 0)
                        {
                            PrepareTransfer(StatusRef.ReadyForTransfer);
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
                                result = CheckEnabledForSetStatus(row.CheckGet("STATUS_ID").ToInt(), 12);
                            }
                        }
                        return result;
                    }
                });
                Commander.Add(new CommandItem()
                {
                    Name = "setdisposed",
                    Enabled = true,
                    Title = "Утилизировать",
                    Group = "setstatus",
                    Description = "Утилизировать",
                    ButtonUse = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = StampItemGrid.GetPrimaryKey();
                        var id = StampItemGrid.SelectedItem.CheckGet(k).ToInt();
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
                            result = CheckEnabledForSetStatus(row.CheckGet("STATUS_ID").ToInt(), 8);
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
                        var k = StampItemGrid.GetPrimaryKey();
                        var id = StampItemGrid.SelectedItem.CheckGet(k).ToInt();
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
                            result = CheckEnabledForSetStatus(row.CheckGet("STATUS_ID").ToInt(), 10);
                        }
                        return result;
                    }
                });
                /*
                Commander.Add(new CommandItem()
                {
                    Name = "setcompleted",
                    Enabled = false,
                    Title = "Доработана",
                    Group = "setstatus",
                    Description = "Штанцформа доработана",
                    ButtonUse = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = StampItemGrid.GetPrimaryKey();
                        var id = StampItemGrid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            SetStatus(11);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = StampItemGrid.SelectedItem;
                        if (row != null)
                        {
                            result = CheckEnabledForSetStatus(row.CheckGet("STATUS_ID").ToInt(), 11);
                        }
                        return result;
                    }
                });
                */
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
                            result = CheckEnabledForSetStatus(row.CheckGet("STATUS_ID").ToInt(), 18);
                        }
                        return result;
                    }
                });
                Commander.Add(new CommandItem()
                {
                    Name = "setcell",
                    Enabled = true,
                    Title = "Поместить в ячейку",
                    Group = "move",
                    Description = "Поместить в ячейку",
                    ButtonUse = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = StampItemGrid.GetPrimaryKey();
                        var id = StampItemGrid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            MoveStampItem();
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = StampItemGrid.SelectedItem;
                        if (row != null)
                        {
                            var statusId = row.CheckGet("STATUS_ID").ToInt();
                            //Поместить в ячейку можно штанцформы в работе
                            if (statusId.ContainsIn(1))
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
                    Group = "move",
                    Description = "Поместить в ячейку",
                    ButtonUse = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        var k = StampItemGrid.GetPrimaryKey();
                        var id = StampItemGrid.SelectedItem.CheckGet(k).ToInt();
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
                    Name = "transportlabel",
                    Enabled = true,
                    Title = "Ярлык пакета передачи",
                    Group = "print",
                    Description = "Показать ярлык пакета передачи",
                    ButtonUse = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = StampItemGrid.GetPrimaryKey();
                        var row = StampItemGrid.SelectedItem;
                        var id = row.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            int transportId = row.CheckGet("STOCK_PLACE").ToInt();
                            MakeTransportLabel(transportId);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = StampItemGrid.SelectedItem;
                        if (row != null)
                        {
                            if (!row.CheckGet("STOCK_PLACE").IsNullOrEmpty())
                            {
                                result = true;
                            }
                        }
                        return result;
                    }
                });
                Commander.Add(new CommandItem()
                {
                    Name = "openact",
                    Enabled = true,
                    Title = "Открыть акт",
                    Group = "print",
                    Description = "Открыть файл акта",
                    ButtonUse = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        var row = StampItemGrid.SelectedItem;
                        string actFileName = row.CheckGet("ACT_FILE_NAME");
                        string path = "";
                        if (actFileName.IsNullOrEmpty())
                        {
                            actFileName = row.CheckGet("OLD_ACT_FILE_NAME");
                            if (!actFileName.IsNullOrEmpty())
                            {
                                string folder = Central.GetStorageNetworkPathByCode("rig_shtanzreport");
                                path = $"{folder}{actFileName}";
                                if (!File.Exists(path))
                                {
                                    path = "";
                                }
                            }
                        }
                        else
                        {
                            string folder = Central.GetStorageNetworkPathByCode("rig_stamp_act");
                            path = $"{folder}{actFileName}";
                            if (!File.Exists(path))
                            {
                                folder = Central.GetStorageNetworkPathByCode("rig_shtanzreport");
                                path = $"{folder}{actFileName}";
                                if (!File.Exists(path))
                                {
                                    path = "";
                                }
                            }
                        }

                        if (!path.IsNullOrEmpty())
                        {
                            Central.OpenFile(path);
                        }
                        else
                        {
                            var dw = new DialogWindow("Файл не найден", "Открыть акт");
                            dw.ShowDialog();
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = StampItemGrid.SelectedItem;
                        if (row != null)
                        {
                            if (row.CheckGet("ACT_FILE_FLAG").ToInt() == 1)
                            {
                                result = true;
                            }
                        }
                        return result;

                    }
                });
            }
            Commander.SetCurrentGridName("ProductGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "techcard",
                    Enabled = true,
                    Title = "Техкарта",
                    Description = "Показать техкарту изделия",
                    ButtonUse = true,
                    ButtonName = "TechCardButton",
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        var path = ProductGrid.SelectedItem.CheckGet("TECHCARD_PATH");
                        if (!path.IsNullOrEmpty())
                        {
                            Central.OpenFile(path);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var path = ProductGrid.SelectedItem.CheckGet("TECHCARD_PATH");
                        if (!path.IsNullOrEmpty())
                        {
                            result = true;
                        }
                        return result;
                    }
                });
                Commander.Add(new CommandItem()
                {
                    Name = "editpd",
                    Enabled = true,
                    Title = "Изменить PD",
                    Description = "Изменить настройку монтажа штанцформы",
                    ButtonUse = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var row = ProductGrid.SelectedItem;
                        if (row != null)
                        {
                            var v = new Dictionary<string, string>
                            {
                                { "ID", row.CheckGet("ID") },
                                { "PD", row.CheckGet("PD") },
                            };

                            var pdEditWin = new CuttingStampPdEdit();
                            pdEditWin.ReceiverName = ControlName;
                            pdEditWin.Edit(v);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = ProductGrid.SelectedItem;
                        if (row != null)
                        {
                            result = true;
                        }
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

        Dictionary<string, string> SelectedItemForBindTransfer { get; set; }

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
                        StampGrid.LoadItems();
                        break;
                    case "refreshproduct":
                        ProductGrid.LoadItems();
                        break;
                    case "savepd":
                        if (m.ContextObject != null)
                        {
                            var v = (Dictionary<string, string>)m.ContextObject;
                            SaveReceivedPd(v);
                        }
                        break;
                    case "transferbinded":
                        if (m.ContextObject != null)
                        {
                            var v = (Dictionary<string, string>)m.ContextObject;
                            StampGrid.LoadItems();
                        }
                        break;
                }
            }
        }

        private void MachineListInit()
        {
            FormHelper.ComboBoxInitHelper(MachineSelectBox, "Rig", "CuttingStamp", "ListMachine", "ID", "MACHINE_NAME", "MACHINE", new Dictionary<string, string>() { { "FACTORY_ID", $"{FactoryId}"} }, true);
            MachineSelectBox.SetSelectedItemFirst();
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
                    Header="Ид",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn()
                {
                    Header="Штанцформа",
                    Path="STAMP_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn()
                {
                    Header="Ручки/отверстия",
                    Path="HOLE_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=3,
                },
                new DataGridHelperColumn()
                {
                    Header="Статус",
                    Path="STATUS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn()
                {
                    Header="PD",
                    Path="PD",
                    ColumnType=ColumnTypeRef.Double,
                    Format="N0",
                    Width2=6,
                    Stylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";

                                bool holeFlag = row.CheckGet("HOLE_FLAG").ToBool();
                                if (!holeFlag)
                                {
                                    if (row.CheckGet("PD").ToInt() == 0)
                                    {
                                        color = HColor.Yellow;
                                    }
                                }

                                if (!color.IsNullOrEmpty())
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        },
                    },
                },
                new DataGridHelperColumn()
                {
                    Header="Место хранения",
                    Path="STORAGE_PLACE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=6,
                },
                new DataGridHelperColumn()
                {
                    Header="FEFCO",
                    Path="FEFCO",
                    ColumnType=ColumnTypeRef.String,
                    Width2=6,
                },
                new DataGridHelperColumn()
                {
                    Header="Ласточкин хвост",
                    Path="DOVETAIL_JOINT_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=3,
                },
                new DataGridHelperColumn()
                {
                    Header="Перфорация по рилевке",
                    Path="CREASE_PERFORATION_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=3,
                },
                new DataGridHelperColumn()
                {
                    Header="Размер заготовки",
                    Path="BLANK_SIZE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn()
                {
                    Header="Количество полумуфт",
                    Path="ITEMS_QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn()
                {
                    Header="Следующая отгрузка",
                    Path="NEXT_SHIPMENT",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=6,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn()
                {
                    Header="Последняя отгрузка",
                    Path="LAST_SHIPMENT",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=6,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn()
                {
                    Header="Примечание",
                    Path="NOTE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },

                new DataGridHelperColumn()
                {
                    Header="Ид статуса",
                    Path="STATUS_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                    Hidden=true,
                },
                new DataGridHelperColumn()
                {
                    Header="Номера полумуфт",
                    Path="ORDER_NUM",
                    ColumnType=ColumnTypeRef.String,
                    Hidden=true,
                    Searchable=true,
                },
            };
            StampGrid.SetColumns(columns);
            StampGrid.SetPrimaryKey("ID");
            StampGrid.SetSorting("ID", ListSortDirection.Ascending);
            StampGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            StampGrid.AutoUpdateInterval = 900;
            StampGrid.Commands = Commander;
            StampGrid.Toolbar = StampToolbar;
            StampGrid.SearchText = GridSearch;
            StampGrid.OnLoadItems = LoadStampItems;
            StampGrid.OnFilterItems = FilterStampItems;
            StampGrid.OnSelectItem = selectedItem =>
            {
                StampItemLoadItems();
                LoadProductItems();
            };
            // Раскраска строк
            StampGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                // Цвета фона строк
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        var statusId = row.CheckGet("STATUS_ID").ToInt();

                        if (statusId.ContainsIn(1, 9))
                        {
                            var nextShipment = row.CheckGet("NEXT_SHIPMENT");

                            if (!nextShipment.IsNullOrEmpty())
                            {
                                color = HColor.Orange;
                            }

                            var lastShipment = row.CheckGet("LAST_SHIPMENT");
                            if (!lastShipment.IsNullOrEmpty())
                            {
                                var lastDate = lastShipment.ToDateTime("dd.MM.yyyy");
                                if (DateTime.Compare(lastDate.AddDays(360), DateTime.Today) < 0)
                                {
                                    color = HColor.Blue;
                                }
                            }
                        }
                        else if (statusId == 8)
                        {
                            color = HColor.Red;
                        }
                        else if (statusId.ContainsIn(12,13,14,16,17,18))
                        {
                            color= HColor.Pink;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
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
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Название",
                    Path="NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=40,
                },
                new DataGridHelperColumn
                {
                    Header="Плательщик",
                    Path="OWNER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=40,
                },
                new DataGridHelperColumn
                {
                    Header="Ячейка",
                    Path="STORAGE_PLACE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
                    Stylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("STORAGE_PLACE").IsNullOrEmpty())
                                {
                                    color = HColor.Yellow;
                                }

                                if (!color.IsNullOrEmpty())
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        },
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Статус",
                    Path="STATUS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
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
                    Header="Начало использования",
                    Path="BEGIN_DT",
                    ColumnType=ColumnTypeRef.DateTime,
                    Format="dd.MM.yyyy",
                    Width2=10,
                },
                new DataGridHelperColumn()
                {
                    Header="Окончание эксплуатации",
                    Description="Дата окончания эксплуатации",
                    Path="END_DT",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=10,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn()
                {
                    Header="Прогонов",
                    Description="Количество прогонов",
                    Path="USAGE_CNT",
                    ColumnType=ColumnTypeRef.Double,
                    Width2=10,
                    Format="N0",
                },
                new DataGridHelperColumn()
                {
                    Header="Тех. обслуживание",
                    Description="Количество прогонов до следующего тех. обслуживания",
                    Path="MAINTENANCE_CNT",
                    ColumnType=ColumnTypeRef.Double,
                    Width2=10,
                    Format="N0",
                },
                new DataGridHelperColumn()
                {
                    Header="Дата рассылки",
                    Path="EXPIRTN_UNUSE_MAIL_DATE",
                    Description="Дата отправки письма об окончании срока хранения оснастки",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=10,
                },
                new DataGridHelperColumn()
                {
                    Header="Дата ответа клиента",
                    Path="CLIENT_ANSWER_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=10,
                },
                new DataGridHelperColumn()
                {
                    Header="Ответ клиента",
                    Path="CLIENT_ANSWER",
                    Description="Ответ клиента на уведомление об окончании срока хранения оснастки",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn()
                {
                    Header="Акт",
                    Path="ACT_FILE_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="ИД плательщика",
                    Path="OWNER_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Акт приема передачи или утилизации",
                    Path="ACT_FILE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Акт приема передачи или утилизации",
                    Path="OLD_ACT_FILE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Hidden=true,
                },

            };
            StampItemGrid.SetColumns(columns);
            StampItemGrid.SetPrimaryKey("ID");
            StampItemGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            StampItemGrid.SetSorting("NAME", System.ComponentModel.ListSortDirection.Ascending);
            StampItemGrid.AutoUpdateInterval = 0;
            StampItemGrid.Commands = Commander;
            StampItemGrid.OnLoadItems = StampItemLoadItems;
            StampItemGrid.OnSelectItem = selectedItem =>
            {
                if (StampItemGrid.Menu.Count > 0)
                {
                    int currentStatus = selectedItem.CheckGet("STATUS_ID").ToInt();
                    string menuTitle = "Открыть акт";
                    if (currentStatus == 8)
                    {
                        menuTitle = "Открыть акт утилизации";
                    }
                    else if (currentStatus == 14)
                    {
                        menuTitle = "Открыть акт приема-передачи";
                    }

                    StampItemGrid.Commands.SetMenuTitle("openact", menuTitle);
                }
            };
            // Раскраска строк
            StampGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                // Цвета фона строк
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        var statusId = row.CheckGet("STATUS_ID").ToInt();

                        if (statusId == 8)
                        {
                            color = HColor.Red;
                        }
                        else if (statusId.ContainsIn(12,13,14,16,17,18))
                        {
                            color= HColor.Pink;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };

            StampItemGrid.Init();
        }

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
                    Path="ARTICLE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn()
                {
                    Header="Название",
                    Path="PRODUCT_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=30,
                },
                new DataGridHelperColumn()
                {
                    Header="Потребитель",
                    Path="CUSTOMER",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn()
                {
                    Header="PD",
                    Path="PD",
                    ColumnType=ColumnTypeRef.Double,
                    Format="N0",
                    Width2=10,
                    Stylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("PD").ToInt() == 0)
                                {
                                    color = HColor.Yellow;
                                }

                                if (!color.IsNullOrEmpty())
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        },
                    },
                },
                new DataGridHelperColumn()
                {
                    Header="Последняя отгрузка",
                    Path="LAST_SHIPMENT",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=6,
                    Format="dd.MM.yyyy",
                },
            };
            ProductGrid.SetColumns(columns);
            ProductGrid.SetPrimaryKey("ID");
            ProductGrid.SetSorting("ID", ListSortDirection.Ascending);
            ProductGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            ProductGrid.AutoUpdateInterval = 0;
            ProductGrid.Commands = Commander;
            ProductGrid.Toolbar = ProductToolbar;
            ProductGrid.OnLoadItems = LoadProductItems;
            ProductGrid.OnFilterItems = FilterProductItems;
            // Раскраска строк
            ProductGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
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
                        else if (row.CheckGet("ARTICLE").IsNullOrEmpty())
                        {
                            color = HColor.Blue;
                        }

                        if (!color.IsNullOrEmpty())
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };
            ProductGrid.Init();
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
                        // По умолчанию - в работе
                        Status.SetSelectedItemByKey("1");
                    }
                }
            }
        }

        /// <summary>
        /// Загрузка списка штанцформ
        /// </summary>
        private async void LoadStampItems()
        {
            int machineId = MachineSelectBox.SelectedItem.Key.ToInt();
            if (machineId > 0)
            {
                StampGrid.Toolbar.IsEnabled = false;
                //Очищаем зависимую таблицу
                StampItemGrid.Items.Clear();
                StampItemGrid.ClearItems();
                ProductGrid.Items.Clear();
                ProductGrid.ClearItems();

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Rig");
                q.Request.SetParam("Object", "CuttingStamp");
                q.Request.SetParam("Action", "List");
                q.Request.SetParam("MACHINE_ID", $"{machineId}");

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
                        var ds = ListDataSet.Create(result, "STAMP_LIST");
                        StampGrid.UpdateItems(ds);
                    }
                }
                StampGrid.Toolbar.IsEnabled = true;
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
        /// Загрузка списка техкарт
        /// </summary>
        private async void LoadProductItems()
        {
            int stampId = StampGrid.SelectedItem.CheckGet("ID").ToInt();
            if (stampId > 0)
            {
                ProductGrid.Toolbar.IsEnabled = false;
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Rig");
                q.Request.SetParam("Object", "CuttingStamp");
                q.Request.SetParam("Action", "TkList");
                q.Request.SetParam("ID", stampId.ToString());

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
                        var ds = ListDataSet.Create(result, "TECHCARD_LIST");
                        ProductGrid.UpdateItems(ds);
                    }
                }
                ProductGrid.Toolbar.IsEnabled = true;
            }
        }

        /// <summary>
        /// Фильтрация строк таблицы штанцформ
        /// </summary>
        private void FilterStampItems()
        {
            if (StampGrid.Items != null)
            {
                if (StampGrid.Items.Count > 0)
                {
                    bool doFilteringByStatus = false;
                    bool doFilteringByDispose = (bool)DisposeCheckBox.IsChecked;

                    int statusId = Status.SelectedItem.Key.ToInt();
                    if (statusId > 0)
                    {
                        doFilteringByStatus = true;
                    }

                    if (doFilteringByStatus || doFilteringByDispose)
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in StampGrid.Items)
                        {
                            // Фильтр по статусу
                            bool includeByStatus = true;
                            if (doFilteringByStatus)
                            {
                                includeByStatus = false;
                                if (row.CheckGet("STATUS_ID").ToInt() == statusId)
                                {
                                    includeByStatus = true;
                                }
                            }

                            // Фильтр по интервалу без использования
                            bool includeByDispose = true;
                            if (doFilteringByDispose)
                            {
                                includeByDispose = false;

                                if (row.CheckGet("STATUS_ID").ToInt() == 1)
                                {
                                    var lastShipment = row.CheckGet("LAST_SHIPMENT").ToDateTime("dd.MM.yyyy");
                                    var beginDttm = row.CheckGet("BEGIN_DTTM").ToDateTime("dd.MM.yyyy HH:mm:ss");

                                    // Проверяем если нет следующей отгрузки
                                    if (row.CheckGet("NEXT_SHIPMENT").IsNullOrEmpty())
                                    {
                                        if (lastShipment > DateTime.MinValue)
                                        {
                                            if (lastShipment < DateTime.Now.AddMonths(-18))
                                            {
                                                includeByDispose = true;
                                            }
                                        }
                                        else
                                        {
                                            if (beginDttm < DateTime.Now.AddMonths(-6))
                                            {
                                                includeByDispose = true;
                                            }
                                        }
                                    }
                                }
                            }

                            if (includeByStatus && includeByDispose)
                            {
                                items.Add(row);
                            }
                        }

                        StampGrid.Items = items;
                    }
                }
            }
        }

        /// <summary>
        /// Фильтрация строк таблицы изделий
        /// </summary>
        private void FilterProductItems()
        {
            if (ProductGrid.Items != null)
            {
                if (ProductGrid.Items.Count > 0)
                {
                    var items = new List<Dictionary<string, string>>();
                    foreach (Dictionary<string, string> row in ProductGrid.Items)
                    {
                        bool includeByArchived = true;
                        if (!(bool)ShowArchiveCheckBox.IsChecked)
                        {
                            includeByArchived = false;
                            if (row.CheckGet("ARCHIVED_FLAG").ToInt() == 0)
                            {
                                includeByArchived = true;
                            }
                        }

                        if (includeByArchived)
                        {
                            items.Add(row);
                        }
                    }

                    ProductGrid.Items = items;
                }
            }
        }

        /// <summary>
        /// Сохранение нового статуса элемента
        /// </summary>
        /// <param name="newStatus"></param>
        public void SetStatus(int newStatus)
        {
            var row = StampItemGrid.SelectedItem;
            if (SelectedItemForBindTransfer.Count > 0)
            {
                // Если есть строка с данными полумуфты для привязки к передаче, то используем ее
                row = SelectedItemForBindTransfer;
            }

            if (row != null)
            {
                bool result = SetStampItemStatus(newStatus, row.CheckGet("ID").ToInt(), row.CheckGet("STATUS_ID").ToInt(), row.CheckGet("CELL_ID"), "", this.FactoryId);
                if (result)
                {
                    SelectedItemForBindTransfer.Clear();
                    StampGrid.LoadItems();
                }
            }
        }

        /// <summary>
        /// Помещение или перемещение элемента штанцформы в ячейку
        /// </summary>
        private void MoveStampItem()
        {
            var row = StampItemGrid.SelectedItem;
            if (row != null)
            {
                bool result = MoveStampItem(row.CheckGet("ID").ToInt(), row.CheckGet("CELL_ID"), "");
                if (result)
                {
                    StampGrid.LoadItems();
                }
            }
        }

        private void Status_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            StampGrid.UpdateItems();
        }

        private void MachineSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            StampGrid.LoadItems();
        }

        /// <summary>
        /// 1	В работе
        /// 2	Отсутствует
        /// 3	Заказана клиентом
        /// 4	Не заказана
        /// 5	Заказана
        /// 6	Заказ в пути
        /// 7	Заказ получен
        /// 8	Утилизирована
        /// 9	В ремонте
        /// 10	В архиве
        /// 11	Доработана
        /// 12	Готова к передаче
        /// 13	Получена на складе
        /// 14	Передана клиенту
        /// 15	Монтаж
        /// </summary>
        /// 
        public enum StatusRef
        {
            /// <summary>
            /// 1	В работе
            /// </summary>
            Worked = 1,
            /// <summary>
            /// 2	Отсутствует
            /// </summary>
            Missing = 2,
            /// <summary>
            /// 3	Заказана клиентом
            /// </summary>
            ClientOrdered = 3,
            /// <summary>
            /// 4	Не заказана
            /// </summary>
            NoOrdered = 4,
            /// <summary>
            /// 5	Заказана
            /// </summary>
            Ordered = 5,
            /// <summary>
            /// 6	Заказ в пути
            /// </summary>
            OrderOnWay = 6,
            /// <summary>
            /// 7	Заказ получен
            /// </summary>
            OrderReceived = 7,
            /// <summary>
            /// 8	Утилизирована
            /// </summary>
            Disposed = 8,
            /// <summary>
            /// 9	В ремонте
            /// </summary>
            Repaire = 9,
            /// <summary>
            /// 10	В архиве
            /// </summary>
            Archive = 10,
            /// <summary>
            /// 11	Доработана
            /// </summary>
            Completed = 11,
            /// <summary>
            /// 12	Готова к передаче
            /// </summary>
            ReadyForTransfer = 12,
            /// <summary>
            /// 13	Получена на складе
            /// </summary>
            ReceivedAtWarehouse = 13,
            /// <summary>
            /// 14	Передана клиенту
            /// </summary>
            TransferredToClient = 14,
            /// <summary>
            /// 15	Монтаж
            /// </summary>
            Installation = 15,
            /// <summary>
            /// 16	Передана на другую площадку
            /// </summary>
            TransferredToOtherFactory = 16,
            /// <summary>
            /// Получена для передачи на другую площадку
            /// </summary>
            RecieptTransferToOtherFactory = 17,
            /// <summary>
            /// Готова к передаче на другую площадку
            /// </summary>
            ReadyTransferToOtherFactory = 18,
        }

        /// <summary>
        /// Проверяем возможность установки нового статуса в зависимости от текущего статуса элемента штанцформы
        /// </summary>
        /// <param name="oldStatusId"></param>
        /// <param name="newStatusId"></param>
        /// <returns></returns>
        public static bool CheckEnabledForSetStatus(int oldStatusId, int newStatusId)
        {
            bool result = false;
            StatusRef oldStatus = (StatusRef)oldStatusId;
            StatusRef newStatus = (StatusRef)newStatusId;

            switch (oldStatus)
            {
                case StatusRef.Worked:
                    if (newStatus == StatusRef.Disposed
                        || newStatus == StatusRef.Repaire
                        || newStatus == StatusRef.ReadyForTransfer
                        || newStatus == StatusRef.Archive
                        || newStatus == StatusRef.Completed
                        || newStatus == StatusRef.ReadyTransferToOtherFactory)
                    {
                        result = true;
                    }
                    break;

                case StatusRef.Missing:
                    break;

                case StatusRef.ClientOrdered:
                    break;

                case StatusRef.NoOrdered:
                    break;

                case StatusRef.Ordered:
                    break;

                case StatusRef.OrderOnWay:
                    break;

                case StatusRef.OrderReceived:
                    if (newStatus == StatusRef.Worked
                        || newStatus == StatusRef.TransferredToOtherFactory)
                    {
                        result = true;
                    }
                    break;

                case StatusRef.Disposed:
                    break;

                case StatusRef.Repaire:
                    if (newStatus == StatusRef.Worked
                        || newStatus == StatusRef.Disposed)
                    {
                        result = true;
                    }
                    break;

                case StatusRef.Archive:
                    if (newStatus == StatusRef.Worked
                        || newStatus == StatusRef.ReadyForTransfer
                        || newStatus == StatusRef.Disposed
                        || newStatus == StatusRef.Completed
                        || newStatus == StatusRef.TransferredToOtherFactory)
                    {
                        result = true;
                    }
                    break;

                // Из этого статуса нет возврата
                case StatusRef.Completed:
                    result = false;
                    break;

                case StatusRef.ReadyForTransfer:
                    if (newStatus == StatusRef.Disposed
                        || newStatus == StatusRef.TransferredToOtherFactory)
                    {
                        result = true;
                    }
                    break;

                case StatusRef.ReceivedAtWarehouse:
                    if (newStatus == StatusRef.Disposed
                        || newStatus == StatusRef.TransferredToOtherFactory)
                    {
                        result = true;
                    }
                    break;

                // Из этого статуса нет возврата
                case StatusRef.TransferredToClient:
                    result = false;
                    break;

                case StatusRef.Installation:
                    break;

                // Из этого статуса нет возврата
                case StatusRef.TransferredToOtherFactory:
                    result = false;
                    break;
            }

            return result;
        }

        public static int GetAlternativeFactoryId(int currentFactoryId)
        {
            int alternativeFactoryId = 0;

            if (currentFactoryId == 1)
            {
                alternativeFactoryId = 2;
            }
            else if (currentFactoryId == 2)
            {
                alternativeFactoryId = 1;
            }

            return alternativeFactoryId;
        }

        /// <summary>
        /// Сохранение нового статуса элемента штанцформы
        /// </summary>
        /// <param name="newStatus"></param>
        public static bool SetStampItemStatus(int newStatusId, int stampItemId, int oldStatusId, string cellId, string rackId, int factoryId)
        {
            bool outputResult = false;

            bool resume = true;
            string newCellId = "";
            string newRackId = "";
            string newMachineId = "";
            string newDiecttngStampId = "";

            StatusRef oldStatus = (StatusRef)oldStatusId;
            StatusRef newStatus = (StatusRef)newStatusId;

            // Если перемещаем полумуфту на другую площадку
            if (newStatus == StatusRef.ReadyTransferToOtherFactory)
            {
                resume = false;

                var setStampForm = new CuttingStampSetStamp();
                setStampForm.FactoryId = GetAlternativeFactoryId(factoryId);
                setStampForm.Show();
                if (setStampForm != null)
                {
                    KeyValuePair<string, string> selectedMachine = setStampForm.SelectedMachine;
                    Dictionary<string, string> selectedStamp = setStampForm.SelectedStamp;

                    newMachineId = selectedMachine.Key;
                    newDiecttngStampId = selectedStamp.CheckGet("ID");
                    string newDiecttngStampName = "Новая штанцформа";
                    if (!string.IsNullOrEmpty(selectedStamp.CheckGet("STAMP_NAME")))
                    {
                        newDiecttngStampName = selectedStamp.CheckGet("STAMP_NAME");
                    }
                    if (!string.IsNullOrEmpty(newMachineId))
                    {
                        if (DialogWindow.ShowDialog(
                            $"Вы действительно хотите установить для полумуфты статус передана на другую площадку?" +
                            $"{Environment.NewLine}Выбранное место хранения другой площадки: {selectedMachine.Value}" +
                            $"{Environment.NewLine}Выбранная штанцформа другой площадки: {newDiecttngStampName}",
                            "Перемещение полумуфты на другую площадку", "", DialogWindowButtons.YesNo) == true)
                        {
                            resume = true;
                        }
                    }
                }
            }
            // Проверяем, нужно ли назначить ячейку
            else
            {
                // Если штанцформы вводится в работу (должна появиться в ячейке)
                // и сейчас не находится в ячейке
                if (newStatus == StatusRef.Worked
                    && string.IsNullOrEmpty(cellId))
                {
                    resume = false;

                    var d = new Dictionary<string, string>()
                        {
                            { "ID", $"{stampItemId}" },
                            { "CELL_ID", cellId },
                            { "RACK_ID", rackId },
                        };

                    var setCellForm = new CuttingStampSetCell();
                    setCellForm.Edit(d);
                    if (setCellForm != null && setCellForm.Form != null)
                    {
                        var formValues = setCellForm.Form.GetValues();
                        if (formValues != null && formValues.Count > 0)
                        {
                            newCellId = formValues.CheckGet("CELL_ID");
                            newRackId = formValues.CheckGet("RACK_ID");
                            if (!string.IsNullOrEmpty(newCellId) && !string.IsNullOrEmpty(newRackId))
                            {
                                resume = true;
                            }
                        }
                    }
                }


                //// Если штанцформы выведена из работы (не находится в ячейке)
                //if (oldStatus == StatusRef.Disposed
                //    || oldStatus == StatusRef.Archive
                //    || oldStatus == StatusRef.ReadyForTransfer
                //    || oldStatus == StatusRef.ReceivedAtWarehouse
                //    || oldStatus == StatusRef.TransferredToClient
                //    || oldStatus == StatusRef.Completed
                //    || oldStatus == StatusRef.TransferredToOtherFactory)
                //{
                //    // Если штанцформы вводится в работу (должна появиться в ячейке)
                //    if (newStatus == StatusRef.Worked)
                //    {
                //        resume = false;

                //        var d = new Dictionary<string, string>()
                //        {
                //            { "ID", $"{stampItemId}" },
                //            { "CELL_ID", cellId },
                //            { "RACK_ID", rackId },
                //        };

                //        var setCellForm = new CuttingStampSetCell();
                //        setCellForm.Edit(d);
                //        if (setCellForm != null && setCellForm.Form != null)
                //        {
                //            var formValues = setCellForm.Form.GetValues();
                //            if (formValues != null && formValues.Count > 0)
                //            {
                //                newCellId = formValues.CheckGet("CELL_ID");
                //                newRackId = formValues.CheckGet("RACK_ID");
                //                if (!string.IsNullOrEmpty(newCellId) && !string.IsNullOrEmpty(newRackId))
                //                {
                //                    resume = true;
                //                }
                //            }
                //        }
                //    }
                //}
            }

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Rig");
                q.Request.SetParam("Object", "CuttingStamp");
                q.Request.SetParam("Action", "SetStatus");
                q.Request.SetParam("ID", $"{stampItemId}");
                q.Request.SetParam("STATUS", $"{newStatusId}");

                q.Request.SetParam("CELL_ID", $"{newCellId}");
                q.Request.SetParam("RACK_ID", $"{newRackId}");

                q.Request.SetParam("NEW_MACHINE_ID", $"{newMachineId}");
                q.Request.SetParam("NEW_DIECTTNG_STAMP_ID", $"{newDiecttngStampId}");

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        if (result.ContainsKey("ITEM"))
                        {
                            outputResult = true;
                        }
                    }
                }
                else if (q.Answer.Error.Code == 145)
                {
                    q.ProcessError();
                }
            }

            return outputResult;
        }

        /// <summary>
        /// Сохранение нового местоположения элемента штанцформы
        /// </summary>
        /// <param name="stampItemId"></param>
        /// <param name="cellId"></param>
        /// <param name="rackId"></param>
        /// <returns></returns>
        public static bool MoveStampItem(int stampItemId, string cellId, string rackId)
        {
            bool outputResult = false;

            bool resume = false;
            string newCellId = "";
            string newRackId = "";

            {
                var d = new Dictionary<string, string>()
                {
                    { "ID", $"{stampItemId}" },
                    { "CELL_ID",  cellId },
                    { "RACK_ID", rackId },
                };

                var setCellForm = new CuttingStampSetCell();
                setCellForm.Edit(d);

                if (setCellForm != null && setCellForm.Form != null)
                {
                    var formValues = setCellForm.Form.GetValues();
                    if (formValues != null && formValues.Count > 0)
                    {
                        newCellId = formValues.CheckGet("CELL_ID");
                        newRackId = formValues.CheckGet("RACK_ID");
                        if (!string.IsNullOrEmpty(newCellId) && !string.IsNullOrEmpty(newRackId))
                        {
                            resume = true;
                        }
                    }
                }
            }

            if (resume)
            {
                var p = new Dictionary<string, string>()
                {
                    { "ID", $"{stampItemId}" },
                    { "CELL_ID", newCellId },
                    { "RACK_ID", newRackId },
                };

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Rig");
                q.Request.SetParam("Object", "CuttingStamp");
                q.Request.SetParam("Action", "SaveCell");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        if (result.ContainsKey("ITEM"))
                        {
                            outputResult = true;
                        }
                    }
                }
                else if (q.Answer.Error.Code == 145)
                {
                    q.ProcessError();
                }
            }

            return outputResult;
        }

        /// <summary>
        /// Сохранить PD для выбранной техкарты
        /// </summary>
        /// <param name="data"></param>
        private async void SaveReceivedPd(Dictionary<string, string> data)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "CuttingStamp");
            q.Request.SetParam("Action", "UpdateTkPd");
            q.Request.SetParams(data);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    if (result.ContainsKey("ITEM"))
                    {
                        ProductGrid.LoadItems();
                    }
                }
            }
            else if (q.Answer.Error.Code == 145)
            {
                q.ProcessError();
            }

        }

        /// <summary>
        /// Подготовка полумуфты к передаче. Формирование пакета передачи
        /// </summary>
        /// <param name="status"></param>
        private void PrepareTransfer(StatusRef status)
        {
            if (StampItemGrid.SelectedItem != null)
            {
                var id = StampItemGrid.SelectedItem.CheckGet("ID").ToInt();
                var bindTransferWindow = new RigTransferPackages();
                bindTransferWindow.ReceiverName = ControlName;
                bindTransferWindow.RigId = id;
                bindTransferWindow.RigType = 1;
                bindTransferWindow.FactoryId = FactoryId;
                bindTransferWindow.Status = (int)status;
                bindTransferWindow.Bind();
            }
        }

        /// <summary>
        /// Получение данных для ярлыка передачи
        /// </summary>
        /// <param name="transportId"></param>
        private async void MakeTransportLabel(int transportId)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "RigTransfer");
            q.Request.SetParam("Action", "GetLabelData");
            q.Request.SetParam("ID", transportId.ToString());
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
                    var ds = ListDataSet.Create(result, "TRANSFER_DATA");
                    var transportLabel = new StampTransportLabel();
                    transportLabel.RigTranportItem = ds;
                    transportLabel.Make();
                }
            }
        }

        /// <summary>
        /// Подготовка штанцформы к передаче на другую площадку
        /// </summary>
        private async void PrepareTransferOtherFactory()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "RigTransfer");
            q.Request.SetParam("Action", "PrepareForOtherFactory");
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
                        StampGrid.LoadItems();
                    }
                }
            }
            else if (q.Answer.Error.Code == 145)
            {
                q.ProcessError();
            }
        }

        private void ShowArchiveCheckBox_Click(object sender, RoutedEventArgs e)
        {
            ProductGrid.UpdateItems();
        }

        private void DisposeCheckBox_Click(object sender, RoutedEventArgs e)
        {
            StampGrid.UpdateItems();
        }
    }
}
