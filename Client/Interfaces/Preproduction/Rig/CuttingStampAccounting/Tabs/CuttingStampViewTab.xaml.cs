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
using static Client.Interfaces.Preproduction.Rig.CuttingStampTab;
using static DevExpress.Data.Filtering.Helpers.SubExprHelper.ThreadHoppingFiltering;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Список штанцформ для инженеров
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class CuttingStampViewTab : ControlBase
    {
        public CuttingStampViewTab()
        {
            InitializeComponent();
            ControlTitle = "Штанцформы";
            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/cutting_stamp";
            RoleName = "[erp]rig_cutting_stamp_accnt";

            MachineDS = new ListDataSet();
            MachineDS.Init();

            OnLoad = () =>
            {
                LoadRef();
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
            }
            Commander.Init(this);
        }

        ListDataSet MachineDS { get; set; }

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
                }
            }
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
                    Header="Универсальная",
                    Path="VERSATILE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
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
                            color = HColor.Pink;
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

            StampItemGrid.Init();
        }

        /// <summary>
        /// Инициализация таблицы изделий
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
                        //Данные по станкам
                        MachineDS = ListDataSet.Create(result, "MACHINE_LIST");

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

                        var factoryDs = ListDataSet.Create(result, "FACTORY");
                        Factory.Items = factoryDs.GetItemsList("ID", "NAME");
                        Factory.SetSelectedItemFirst();

                    }
                }
            }
        }

        /// <summary>
        /// Загрузка списка штанцформ
        /// </summary>
        private async void LoadStampItems()
        {
            int machineId = Machine.SelectedItem.Key.ToInt();
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
                    bool doFilteringByVersatile = (bool)ShowVersatileCheckBox.IsChecked;

                    int statusId = Status.SelectedItem.Key.ToInt();
                    if (statusId > 0)
                    {
                        doFilteringByStatus = true;
                    }

                    if (doFilteringByStatus || doFilteringByVersatile)
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

                            // Фильтр по универсальной оснастке
                            bool includeByVersatile = true;
                            if (doFilteringByVersatile)
                            {
                                includeByVersatile = false;
                                if (!row.CheckGet("VERSATILE_NAME").IsNullOrEmpty())
                                {
                                    includeByVersatile = true;
                                }
                            }

                            if (includeByStatus && includeByVersatile)
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
        /// Обновление списка станков
        /// </summary>
        private void MachineListUpdate()
        {
            int factoryId = Factory.SelectedItem.Key.ToInt();
            var dc = new Dictionary<string, string>();
            foreach (var item in MachineDS.Items)
            {
                if (item.CheckGet("FACTORY_ID").ToInt() == factoryId)
                {
                    dc.Add(item.CheckGet("ID"), item.CheckGet("NAME"));
                }
            }

            Machine.Items = dc;
            Machine.SetSelectedItemFirst();
        }

        private void Factory_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MachineListUpdate();

            StampGrid.LoadItems();
        }

        private void MachineSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            StampGrid.LoadItems();
        }

        private void ShowVersatileCheckBox_Click(object sender, RoutedEventArgs e)
        {
            StampGrid.UpdateItems();
        }

        private void ShowArchiveCheckBox_Click(object sender, RoutedEventArgs e)
        {
            ProductGrid.UpdateItems();
        }

        private void Status_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            StampGrid.UpdateItems();
        }
    }
}
