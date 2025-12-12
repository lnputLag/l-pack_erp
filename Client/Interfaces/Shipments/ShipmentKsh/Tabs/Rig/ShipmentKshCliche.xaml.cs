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
using static DevExpress.Data.Filtering.Helpers.SubExprHelper.ThreadHoppingFiltering;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Список клише на отгрузку для перередачи клиенту или на другую площадку
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class ShipmentKshCliche : ControlBase
    {
        public ShipmentKshCliche()
        {
            InitializeComponent();

            ControlTitle = "Клише";
            DocumentationUrl = "/doc/l-pack-erp/shipments/control/equipments/shipmentcliche";
            RoleName = "[erp]shipment_control_ksh";
            FactoryId = 2;

            OnLoad = () =>
            {
                GridInit();
            };

            OnUnload = () =>
            {
                Grid.Destruct();
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
                        Grid.ItemsExportExcel();
                    },
                });
            }
            Commander.SetCurrentGridName("Grid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "setreceived",
                    Group = "operations",
                    Enabled = true,
                    Title = "Отметить получение",
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
                            SetStatus(6);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (row != null)
                        {
                            int currentStatus = row.CheckGet("STATUS_ID").ToInt();
                            if (currentStatus.ContainsIn(2, 6, 17, 18))
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
                            SetStatus(3);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (row != null)
                        {
                            int currentStatus = row.CheckGet("STATUS_ID").ToInt();
                            if (currentStatus.ContainsIn(2, 6, 17, 18))
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
                            var StorekeeperNote = new ShipmentNote();
                            StorekeeperNote.ObjectId = id;
                            StorekeeperNote.Object = "Cliche";
                            StorekeeperNote.Edit();
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
                    Name = "deattachshipment",
                    Group = "storage",
                    Enabled = true,
                    Title = "Отвязать от отгрузки",
                    Description = "Отвязать выбранное клише от отгрузки",
                    ButtonUse = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            DeattachShipment();
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (row != null)
                        {
                            int shipmentId = row.CheckGet("ID_TS").ToInt();
                            int statusId = row.CheckGet("STATUS_ID").ToInt();
                            if (shipmentId > 0 && statusId.ContainsIn())
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
                    Header="Номер клише",
                    Path="CLICHE_NUM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=10,
                    Doc="Станок, номер ячейки и место, где хранится клише",
                },
                new DataGridHelperColumn()
                {
                    Header="Статус",
                    Path="STATUS_NAME",
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
                    Header="Артикулы",
                    Path="ARTIKUL",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn()
                {
                    Header="Код статуса",
                    Path="STATUS_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn()
                {
                    Header="Акт",
                    Path="ACT_FILE_NAME_FLAG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    Width2=4,
                    Doc="Наличие файла акта приема-передачи",
                },
                new DataGridHelperColumn()
                {
                    Header="Плательщик",
                    Path="POK_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=30,
                },
                new DataGridHelperColumn()
                {
                    Header="Дата",
                    Group="Отгрузка",
                    Path="DT_OTGR",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Width2=16,
                    Format="dd.MM.yyyy HH:mm"
                },
                new DataGridHelperColumn()
                {
                    Header="Покупатель",
                    Group="Отгрузка",
                    Path="POK_OTGR",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=30,
                },

                new DataGridHelperColumn()
                {
                    Header="Водитель",
                    Group="Отгрузка",
                    Path="DRIVER_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=30,
                },
                new DataGridHelperColumn()
                {
                    Header="Кладовщик",
                    Path="STOREKEEPER_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn()
                {
                    Header="Примечание",
                    Path="STOREKEEPER_NOTE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=30,
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
                    Header="Имя файла",
                    Path="ACT_FILE_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
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
            Grid.Toolbar = GridToolbar;
            Grid.OnLoadItems = LoadItems;
            Grid.OnFilterItems = FilterItems;
            // Раскраска строк
            Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                {
                    StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";
                        // status = 2 - готово к передаче
                        if (row.CheckGet("STATUS_ID").ToInt() == 2)
                        {
                            color=HColor.GreenFG;
                        }

                        // status = 6 - получено на СГП
                        if (row.CheckGet("STATUS_ID").ToInt() == 6)
                        {
                            // transport_status = 2 - машина для отгрузки на терминале
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

        /// <summary>
        /// Загрузка данных из БД.
        /// </summary>
        public async void LoadItems()
        {
            GridToolbar.IsEnabled = false;
            Grid.ShowSplash();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Cliche");
            //FIXME: rename action: ShipmentList -> List*
            q.Request.SetParam("Action", "ShipmentList");

            // Состояние чекбокса преобразуем в строку и передаём как параметр запроса
            var allCliche = (bool)AllClicheCheckBox.IsChecked;
            q.Request.SetParam("AllRec", allCliche ? "1" : "0");
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
                    if (result.Count > 0)
                    {
                        var ds = ListDataSet.Create(result, "ShipmentCliche");
                        Grid.UpdateItems(ds);
                    }
                }
            }

            GridToolbar.IsEnabled = true;
            Grid.HideSplash();
        }

        /// <summary>
        /// Фильтрация строк грида
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
                            if (((row.CheckGet("STATUS_ID").ToInt() == 2) || (row.CheckGet("STATUS_ID").ToInt() == 6))
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
        /// обновление статуса клише при отгрузке
        /// </summary>
        /// <param name="newStatus">Значение нового статуса.</param>
        private async void SetStatus(int newStatus)
        {
            if (Grid.SelectedItem != null)
            {
                var clicheId = Grid.SelectedItem.CheckGet("ID").ToInt();
                //Для передачи на другую площадку другие статусы
                bool transferToOtherFactory = Grid.SelectedItem.CheckGet("STATUS_ID").ToInt().ContainsIn(16, 17, 18);

                if (clicheId != 0)
                {
                    if (newStatus == 6 && transferToOtherFactory)
                    {
                        newStatus = 17;
                    }
                    else if (newStatus == 3 && transferToOtherFactory)
                    {
                        newStatus = 16;
                    }
                    
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "Cliche");
                    q.Request.SetParam("Action", "UpdateStatus");
                    q.Request.SetParam("IdClic", clicheId.ToString());
                    q.Request.SetParam("Status", newStatus.ToString());

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
                            if (result.ContainsKey("ITEMS"))
                            {
                                Grid.LoadItems();
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
        /// Вызов окна редактирования примечания кладовщика
        /// </summary>
        private void AddStorekeeperNote()
        {
            if (Grid.SelectedItem != null)
            {
                int clicheId = Grid.SelectedItem.CheckGet("ID").ToInt();
                var StorekeeperNote = new ShipmentNote();
                StorekeeperNote.ObjectId = clicheId;
                StorekeeperNote.Object = "Cliche";
                StorekeeperNote.Edit();
            }
        }

        /// <summary>
        /// Открытие акта приема-передачи
        /// </summary>
        private async void OpenActFile()
        {
            if (Grid.SelectedItem != null)
            {
                var actFileName = Grid.SelectedItem.CheckGet("ACT_FILE_NAME");
                if (!string.IsNullOrEmpty(actFileName))
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "Cliche");
                    q.Request.SetParam("Action", "GetActFile");
                    q.Request.SetParam("actFileName", actFileName);

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    }
                    );

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
        /// Отвязать от отгрузки        
        /// </summary>
        private async void DeattachShipment()
        {
            var resume = true;

            var itemId = 0;
            if (resume)
            {
                if (Grid.SelectedItem != null)
                {
                    itemId = Grid.SelectedItem.CheckGet("ID").ToInt();
                    if (itemId == 0)
                    {
                        resume = false;
                    }
                }
                else
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var msg = $"Отвязать клише №{itemId} от отгрузки?\n";

                var d = new DialogWindow($"{msg}", "Отвязка от отгрузки", "", DialogWindowButtons.NoYes);
                if (d.ShowDialog() != true)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "Cliche");
                q.Request.SetParam("Action", "UpdateTS");
                q.Request.SetParam("IdClic", itemId.ToString());
                q.Request.SetParam("IdTs", "0");

                await Task.Run(() =>
                {
                    q.DoQuery();
                }
                );

                if (q.Answer.Status == 0)
                {
                    Grid.LoadItems();
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        private void AllClicheCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void ToShipCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Grid.UpdateItems();
        }
    }
}
