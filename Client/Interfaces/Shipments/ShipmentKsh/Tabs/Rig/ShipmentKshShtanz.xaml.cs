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

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Список пакетов передачи штанцформ для перередачи клиенту или на другую площадку
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class ShipmentKshShtanz : ControlBase
    {
        public ShipmentKshShtanz()
        {
            InitializeComponent();

            ControlTitle = "Штанцформы";
            DocumentationUrl = "/doc/l-pack-erp/shipments/control/equipments/shipmentshtanz";
            RoleName = "[erp]shipment_control_ksh";

            FactoryId = 2;

            OnLoad = () =>
            {
                GridInit();
                ItemsGridInit();
            };

            OnUnload = () =>
            {
                Grid.Destruct();
                ItemsGrid.Destruct();
            };

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (System.Windows.Input.KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
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
                    Group = "main",
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
                    Name = "exporttoexcel",
                    Group = "main",
                    Enabled = true,
                    Title = "В Excel",
                    Description = "Выгрузить данные в Excel файл",
                    ButtonUse = true,
                    MenuUse = false,
                    ButtonName = "ToExcelButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        Grid.ItemsExportExcel();
                    },
                });
            }
            Commander.SetCurrentGridName("Grid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "setreceived",
                    Group="operations",
                    Enabled = true,
                    Title="Отметить получение",
                    Description = "Отметить статус получения на складе",
                    ButtonUse = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            SetStatus(13);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (row != null)
                        {
                            int currentStatus = row.CheckGet("STATUS_ID").ToInt();
                            if (currentStatus.ContainsIn(12, 13, 17, 18))
                                result = true;
                        }
                        return result;
                    }
                });
                Commander.Add(new CommandItem()
                {
                    Name = "setshipped",
                    Group = "operations",
                    Enabled = true,
                    Title = "Отметить отгрузку",
                    Description = "Отметить статус отгружено",
                    ButtonUse = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            SetStatus(14);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (row != null)
                        {
                            int currentStatus = row.CheckGet("STATUS_ID").ToInt();
                            if (currentStatus.ContainsIn(12, 13, 17, 18))
                                result = true;
                        }
                        return result;
                    }
                });
                Commander.Add(new CommandItem()
                {
                    Name = "editnote",
                    Group = "storage",
                    Enabled = true,
                    Title = "Добавить примечание",
                    Description = "Добавить или изменить примечание у хранимого пакета передачи",
                    ButtonUse = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = true;
                        
                        return result;
                    }
                });
                Commander.Add(new CommandItem()
                {
                    Name = "setcell",
                    Group = "storage",
                    Enabled = true,
                    Title = "Изменить ячейку",
                    Description = "Изменить ячейку хранения пакета передачи",
                    ButtonUse = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {

                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = true;

                        return result;
                    }
                });
            }
            Commander.SetCurrentGridName("ItemsGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "openact",
                    Group = "document",
                    Enabled = true,
                    Title = "Открыть акт приема-передачи",
                    Description = "Открыть файл акта приема-передачи",
                    ButtonUse = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            OpenActFile();
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
        /// Идентификатор площадки отгрузки: 1 - Липецк, 2 - Кашира
        /// </summary>
        public int FactoryId { get; set; }


        /// <summary>
        /// Инициализация таблицы Управление отгрузками штанцформ
        /// </summary>
        public void GridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn()
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn()
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn()
                {
                    Header="Статус",
                    Path="STATUS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn()
                {
                    Header="Дата смены статуса",
                    Path="STATUS_DTTM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Width2=16,
                    Format="dd.MM.yyyy HH:mm"
                },
                new DataGridHelperColumn()
                {
                    Header="Плательщик",
                    Path="CUSTOMER_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=30,
                },
                new DataGridHelperColumn()
                {
                    Header="Дата",
                    Group="Отгрузка",
                    Path="SHIPMENT_DTTM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Format="dd.MM.yyyy HH:mm",
                    Width2=16,
                },

                new DataGridHelperColumn()
                {
                    Header="Покупатель",
                    Group="Отгрузка",
                    Path="SHIPMENT_OWNER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=30,
                },
                new DataGridHelperColumn()
                {
                    Header="Водитель",
                    Group="Отгрузка",
                    Path="DRIVER_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=26,
                },
                new DataGridHelperColumn()
                {
                    Header="Ячейка",
                    Path="STORAGE_NUM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Doc="Место хранения штанцформы на складе",
                    Width2=5,
                },
                new DataGridHelperColumn()
                {
                    Header="Кладовщик",
                    Path="STOREKEEPER_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=26,
                },
                new DataGridHelperColumn()
                {
                    Header="Примечание",
                    Path="NOTE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=20,
                    Doc="Примечание кладовщика",
                },
                new DataGridHelperColumn()
                {
                    Header="Статус транспорта",
                    Path="TRANSPORT_STATUS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true
                },
                new DataGridHelperColumn()
                {
                    Header="Код статуса",
                    Path="STATUS_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true
                },
            };
            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("ID");
            Grid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.AutoUpdateInterval = 900; //15 мин
            Grid.SearchText = SearchText;
            Grid.Commands = Commander;
            Grid.Toolbar = StampToolbar;
            Grid.OnLoadItems = LoadItems;
            Grid.OnSelectItem = selectedItem =>
            {
                ItemsLoadItems();
            };
            // Раскраска строк
            Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                {
                    StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";
                        int statusId = row.CheckGet("STATUS_ID").ToInt();

                        if (statusId.ContainsIn(12, 18))
                        {
                            color=HColor.GreenFG;
                        }
                        else if (statusId.ContainsIn(13, 17))
                        {
                            if (row.CheckGet("TRANSPORT_STATUS").ToInt() == 2)
                            {
                                color=HColor.MagentaFG;
                            }
                            else
                            {
                                color=HColor.BlueFG;
                            }
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };


            Grid.Init();
        }

        public void ItemsGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn()
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn()
                {
                    Header="Штанцформа",
                    Path="NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=20,
                    Doc="Имя элемента штанцформы на отгрузку",
                },
                new DataGridHelperColumn()
                {
                    Header="Акт",
                    Path="ACT_FILE_FLAG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    Width2=4,
                    Doc="Наличие акта приема-передачи штанцформы",
                },
                new DataGridHelperColumn()
                {
                    Header="Имя файла",
                    Path="ACT_FILE_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Hidden=true
                },
            };
            ItemsGrid.SetColumns(columns);
            ItemsGrid.SetPrimaryKey("_ROWNUMBER");
            ItemsGrid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
            ItemsGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            ItemsGrid.AutoUpdateInterval = 0;
            ItemsGrid.Commands = Commander;

            ItemsGrid.Init();
        }

        /// <summary>
        /// Загрузка данных в таблицу отгрузок ШФ
        /// </summary>
        public async void LoadItems()
        {
            StampToolbar.IsEnabled = false;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "RigTransfer");
            q.Request.SetParam("Action", "List");
            q.Request.SetParam("RIG_TYPE", "1");
            q.Request.SetParam("FACTORY_ID", FactoryId.ToString());

            // Состояние чекбокса преобразуем в строку и передаём как параметр запроса
            var allShtanz = (bool)AllShtanzCheckBox.IsChecked;
            q.Request.SetParam("ALL_RECORDS", allShtanz ? "1" : "0");

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
                    var ds = ListDataSet.Create(result, "TRANSFER");
                    Grid.UpdateItems(ds);
                }
            }

            StampToolbar.IsEnabled = true;
        }

        /// <summary>
        /// Загрузка данных в таблицу содержимого пакета передачи
        /// </summary>
        public async void ItemsLoadItems()
        {
            if (Grid.SelectedItem != null)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Rig");
                q.Request.SetParam("Object", "RigTransfer");
                q.Request.SetParam("Action", "ListStampItems");
                q.Request.SetParam("ID", Grid.SelectedItem.CheckGet("ID"));

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
                        ItemsGrid.UpdateItems(ds);
                    }
                }
            }
        }

        /// <summary>
        /// Фильтрация строк таблицы пакетов передачи
        /// </summary>
        public void FilterItems()
        {
            if (Grid.Items != null)
            {
                if (Grid.Items.Count > 0)
                {
                    var items = new List<Dictionary<string, string>>();
                    foreach (var row in Grid.Items)
                    {
                        if ((bool)ToShipCheckBox.IsChecked)
                        {
                            int currentStatus = row.CheckGet("STATUS_ID").ToInt();
                            if (currentStatus.ContainsIn(12,13,17,18)
                                && (row.CheckGet("TRANSPORT_STATUS").ToInt() == 2))
                            {
                                items.Add(row);
                            }
                        }
                        else
                        {
                            items.Add(row);
                        }
                    }

                    Grid.Items = items;
                }
            }
        }

        /// <summary>
        /// Устанвка нового статуса пакета передачи
        /// </summary>
        /// <param name="newStatus"></param>
        public async void SetStatus(int newStatus)
        {
            if (Grid.SelectedItem != null)
            {
                //Для передачи на другую площадку другие статусы
                bool transferToOtherFactory = Grid.SelectedItem.CheckGet("STATUS_ID").ToInt().ContainsIn(16, 17, 18);
                if (newStatus == 13 && transferToOtherFactory)
                {
                    newStatus = 17;
                }
                else if (newStatus == 14 && transferToOtherFactory)
                {
                    newStatus = 16;
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Rig");
                q.Request.SetParam("Object", "RigTransfer");
                q.Request.SetParam("Action", "SetStatus");
                q.Request.SetParam("ID", Grid.SelectedItem.CheckGet("ID"));
                q.Request.SetParam("STATUS", newStatus.ToString());
                q.Request.SetParam("RIG_TYPE", "1");

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    Grid.LoadItems();
                }
            }
        }

        /// <summary>
        /// Открытие файла акта приема-передачи
        /// </summary>
        private async void OpenActFile()
        {
            if (ItemsGrid.SelectedItem != null)
            {
                var actFileName = ItemsGrid.SelectedItem.CheckGet("ACT_FILE_NAME");
                if (!string.IsNullOrEmpty(actFileName))
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Rig");
                    q.Request.SetParam("Object", "CuttingStamp");
                    q.Request.SetParam("Action", "GetActFile");
                    q.Request.SetParam("ACT_FILE_NAME", actFileName);

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(q.Answer.Data);
                        if (result != null)
                        {
                            if (result.Count > 0)
                            {
                                if (result.ContainsKey("documentFile"))
                                {
                                    Central.OpenFile(result["documentFile"]);
                                }
                            }
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
        /// Отрытие окна редактирования ячейки хранения штанцформы
        /// </summary>
        private void ChangeStorageNum()
        {
            if (Grid.SelectedItem != null)
            {
                var storageCellWindow = new ShipmentRigStorageCell();
                storageCellWindow.ReceiverName = ControlName;
                storageCellWindow.Edit(Grid.SelectedItem);
            }
        }

        private void AllShtanzCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void ToShipCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Grid.UpdateItems();
        }
    }
}
