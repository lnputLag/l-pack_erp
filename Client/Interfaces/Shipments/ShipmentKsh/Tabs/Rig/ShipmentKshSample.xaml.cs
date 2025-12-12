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
    /// Список образцов для отгрузки из Каширы
    /// </summary>
    /// 
    public partial class ShipmentKshSample : ControlBase
    {
        /// <summary>
        /// Список образцов для отгрузки из Каширы
        /// </summary>
        public ShipmentKshSample()
        {
            InitializeComponent();

            ControlTitle = "Образцы";
            DocumentationUrl = "/doc/l-pack-erp/shipments/control/equipments/shipmentsamples";
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
                    Name = "toexcel",
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
                    Group = "setstatus",
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
                            SetStatus(4);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (row != null)
                        {
                            int currentStatus = row.CheckGet("STATUS").ToInt();
                            if (currentStatus.ContainsIn(3, 5, 7))
                                result = true;
                        }
                        return result;
                    }
                });
                Commander.Add(new CommandItem()
                {
                    Name = "settransferred",
                    Group = "setstatus",
                    Enabled = true,
                    Title = "Отметить передачу",
                    Description = "Отметить статус передачи со склада",
                    ButtonUse = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            SetStatus(7);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (row != null)
                        {
                            int currentStatus = row.CheckGet("STATUS").ToInt();
                            if (currentStatus.ContainsIn(4))
                                result = true;
                        }
                        return result;
                    }
                });
                Commander.Add(new CommandItem()
                {
                    Name = "setshipped",
                    Group = "setstatus",
                    Enabled = true,
                    Title = "Отметить отгрузку",
                    Description = "Отметить отгрузку со склада",
                    ButtonUse = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            SetStatus(5);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (row != null)
                        {
                            int currentStatus = row.CheckGet("STATUS").ToInt();
                            if (currentStatus.ContainsIn(4))
                                result = true;
                        }
                        return result;
                    }
                });
                Commander.Add(new CommandItem()
                {
                    Name = "cellnum",
                    Group = "storage",
                    Enabled = true,
                    Title = "Ячейка",
                    Description = "Поставить образец в ячейку хранения",
                    ButtonUse = true,
                    MenuUse = true,
                    ButtonName = "CellNumButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            var cellNumWindow = new ShipmentSampleCellNum();
                            cellNumWindow.ReceiverName = ControlName;
                            cellNumWindow.Edit(id);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (row != null)
                        {
                            //Поставить в ячейку можно только неотгруженные образцы
                            int currentStatus = row.CheckGet("STATUS").ToInt();
                            if (currentStatus.ContainsIn(5, 6))
                            {
                                result = true;
                            }
                        }

                        return result;
                    }
                });
                Commander.Add(new CommandItem()
                {
                    Name = "deattach",
                    Group = "storage",
                    Enabled = true,
                    Title = "Отвязать от отгрузки",
                    Description = "Добавить или изменить примечание у образца",
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
                            //отвязать можно только позиции с отгрузкой
                            string consignee = row.CheckGet("NAME_POK_SHIP");
                            if (!consignee.IsNullOrEmpty())
                            {
                                result = true;
                            }
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
                    Description = "Добавить или изменить примечание у образца",
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
                            StorekeeperNote.Object = "Samples";
                            StorekeeperNote.Edit();
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
        /// Инициализация таблицы
        /// </summary>
        private void GridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                // Номер строки результата запроса. Колонка нужна для первичной сортировки
                new DataGridHelperColumn()
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=6,
                    Exportable=false,
                },
                new DataGridHelperColumn()
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn()
                {
                    Header="Дата изготовления",
                    Path="DT_COMPLITED",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Width2=10,
                    Format="dd.MM.yyyy",
                    Doc="Дата изготовления образца",
                },
                new DataGridHelperColumn()
                {
                    Header="Тип изготовления",
                    Path="PRODUCTION_TYPE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=10,
                    Doc="Где произведен образец: на плоттере или на линии",
                },
                new DataGridHelperColumn()
                {
                    Header="Покупатель",
                    Path="NAME_POK",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn()
                {
                    Header="Образец",
                    Path="SAMPLE_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=30,
                },
                new DataGridHelperColumn()
                {
                    Header="Количество",
                    Path="QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn()
                {
                    Header="Статус",
                    Path="STATUS_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn()
                {
                    Header="Номер",
                    Path="NUM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=8,
                    Doc="Номер образца, присвоенный клиентом",
                },
                new DataGridHelperColumn()
                {
                    Header="Дата",
                    Group="Отгрузка",
                    Path="DT_SHIPMENT",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Format="dd.MM.yyyy",
                    Width2=10,
                },
                new DataGridHelperColumn()
                {
                    Header="Покупатель",
                    Group="Отгрузка",
                    Path="NAME_POK_SHIP",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn()
                {
                    Header="Водитель",
                    Group="Отгрузка",
                    Path="DRIVER_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn()
                {
                    Header="Менеджер",
                    Path="EMPLOYEE_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn()
                {
                    Header="Номер ячейки",
                    Path="CELL_NUM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn()
                {
                    Header="Кладовщик",
                    Path="STOREKEEPER_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn()
                {
                    Header="Примечание",
                    Path="STOREKEEPER_NOTE",
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
                    Path="STATUS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true
                }
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
                        int status = row.CheckGet("STATUS").ToInt();

                        // Изготовлен или передан
                        if (status == 3 || status == 7)
                        {
                            color=HColor.GreenFG;
                        }
                        // Получен
                        else if (status == 4)
                        {
                            color=HColor.BlueFG;
                        }

                        // 
                        if ((row.CheckGet("TRANSPORT_STATUS").ToInt() > 0) && (status == 3 || status == 7 || status == 4))
                        {
                            color=HColor.MagentaFG;
                        }

                        if (status == 6)
                        {
                             color=HColor.RedFG;
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

        private async void LoadItems()
        {
            GridToolbar.IsEnabled = false;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "ListShipment");

            var allSamples = (bool)AllSamplesCheckBox.IsChecked;
            // Состояние чекбокса преобразуем в строку и передаём как параметр запроса
            q.Request.SetParam("AllRec", allSamples ? "1" : "0");
            q.Request.SetParam("FACTORY_ID", FactoryId.ToString());

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
                    var ds = ListDataSet.Create(result, "ShipmentSamples");
                    Grid.UpdateItems(ds);
                }
            }

            GridToolbar.IsEnabled = true;
        }


        /// <summary>
        /// Фильтрация записей
        /// </summary>
        public void FilterItems()
        {
            if (Grid.Items != null && Grid.Items.Count > 0)
            {
                bool doFilteringByStatus = false;
                int selStatus = -1;
                if (StatusComboBox.SelectedItem.Key != null)
                {
                    selStatus = StatusComboBox.SelectedItem.Key.ToInt();
                    if (selStatus > 0)
                    {
                        doFilteringByStatus = true;
                    }
                }

                bool doFiltegingByTransport = (bool)ToShipCheckBox.IsChecked;

                if (doFilteringByStatus || doFiltegingByTransport)
                {
                    var items = new List<Dictionary<string, string>>();
                    foreach (var row in Grid.Items)
                    {
                        var rowStatus = row.CheckGet("STATUS").ToInt();

                        bool includeByStatus = true;
                        bool includeByTransport = true;

                        // Если отмечен чекбокс На отгрузку, показываем строки на отгрузку, не учитывая фильтр статуса
                        if (doFiltegingByTransport)
                        {
                            includeByTransport = (row.CheckGet("TRANSPORT_STATUS").ToInt() > 0) && rowStatus.ContainsIn(3, 4, 7);
                        }
                        
                        if (doFilteringByStatus)
                        {
                            includeByStatus = false;
                            if (rowStatus == selStatus)
                            {
                                includeByStatus = true;
                            }
                        }

                        if (includeByTransport && doFilteringByStatus)
                        {
                            items.Add(row);
                        }
                    }
                    Grid.Items = items;
                }
            }
        }

        /// <summary>
        /// обновление статуса отгрузки
        /// </summary>
        /// <param name="newStatus">Значение нового статуса</param>
        private async void SetStatus(int newStatus)
        {
            if (Grid.SelectedItem != null)
            {
                var id = Grid.SelectedItem.CheckGet("ID").ToInt();
                if (id != 0)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "Samples");
                    q.Request.SetParam("Action", "UpdateStatus");

                    q.Request.SetParam("SAMPLE_ID", id.ToString());
                    q.Request.SetParam("STATUS", newStatus.ToString());

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
                            if (result.Count > 0)
                            {
                                // пришел непустой ответ, обновляем грид
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
        /// Отвязать от отгрузки        
        /// </summary>
        private async void DeattachShipment()
        {
            var resume = false;

            var itemId = 0;
            if (resume)
            {
                itemId = Grid.SelectedItem.CheckGet("ID").ToInt();

                if (itemId > 0)
                {
                    resume = true;
                }
            }

            if (resume)
            {
                var msg = "";
                msg = $"Отвязать образец №{itemId} от отгрузки?";

                resume = false;
                var d = new DialogWindow($"{msg}", "Отвязка от отгрузки", "", DialogWindowButtons.NoYes);
                if ((bool)d.ShowDialog())
                {
                    if (d.ResultButton == DialogResultButton.Yes)
                        resume = true;
                }
            }

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "Samples");
                q.Request.SetParam("Action", "UpdateTS");

                q.Request.SetParam("IdSmpl", itemId.ToString());
                q.Request.SetParam("IdTs", "0");

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
                        Grid.LoadItems();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        private void UpdateGridItems(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void AllSamplesCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void ToShipCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Grid.UpdateItems();
        }
    }
}
