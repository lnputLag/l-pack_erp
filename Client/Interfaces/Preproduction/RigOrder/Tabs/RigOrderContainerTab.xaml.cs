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
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Список заявок на оснастку для литой тары
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class RigOrderContainerTab : ControlBase
    {
        public RigOrderContainerTab()
        {
            InitializeComponent();
            ControlTitle = "Заказ клише ЛТ";
            DocumentationUrl = "/doc/l-pack-erp/preproduction/rig_order_list_container";
            RoleName = "[erp]rig_order_contnr";

            OnLoad = () =>
            {
                InitGrid();
            };

            OnUnload = () =>
            {
                Grid.Destruct();
            };

            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverGroup == "PreproductionContainer")
                {
                    if (msg.ReceiverName == ControlName)
                    {
                        ProcessCommand(msg.Action, msg);
                    }
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
                Commander.Add(new CommandItem()
                {
                    Name = "create",
                    Title = "Создать",
                    Group = "operations",
                    MenuUse = true,
                    HotKey = "Insert",
                    ButtonUse = true,
                    ButtonName = "CreateButton",
                    Description = "Создание новой заявки",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var selectProductFrame = new RigOrderSelectProducts();
                        selectProductFrame.ReceiverName = ControlName;
                        selectProductFrame.Show();
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
                    Group = "operations",
                    MenuUse = true,
                    HotKey = "Return|DoubleCLick",
                    ButtonUse = true,
                    ButtonName = "EditButton",
                    Description = "Изменение заявки",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id > 0)
                        {
                            var rigOrderFrame = new RigOrderContainer();
                            rigOrderFrame.ReceiverName = ControlName;
                            rigOrderFrame.Edit(id);
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
                    Name = "delete",
                    Title = "Удалить",
                    Group = "operations",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonName = "DeleteButton",
                    Description = "Удаление заявки",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id > 0)
                        {
                            string orderFolder = Path.GetDirectoryName(Grid.SelectedItem.CheckGet("DRAWING_FILE"));
                            if (!orderFolder.IsNullOrEmpty())
                            {
                                DeleteOrder(id);
                            }
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var k = Grid.GetPrimaryKey();
                        var row = Grid.SelectedItem;
                        if (row.CheckGet(k).ToInt() != 0)
                        {
                            if (row.CheckGet("STATUS_ID").ToInt() == 4 && !row.CheckGet("CANCELED_FLAG").ToBool())
                            {
                                result = true;
                            }
                        }
                        return result;
                    },
                });
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
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            ShowTechnologicalCard();
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (row != null)
                        {
                            result = !row.CheckGet("TECHCARD_PATH").IsNullOrEmpty();
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "open_order_folder",
                    Title = "Открыть папку заявки",
                    Group = "folder",
                    MenuUse = true,
                    ButtonUse = false,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id > 0)
                        {
                            string drawingFile = Grid.SelectedItem.CheckGet("DRAWING_FILE");
                            string p = System.IO.Path.GetDirectoryName(drawingFile);
                            Central.OpenFolder(p);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (row != null)
                        {
                            string drawingFile = row.CheckGet("DRAWING_FILE");
                            if (!drawingFile.IsNullOrEmpty())
                            {
                                result = true;
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "set_status_ordered",
                    Title = "Заказать",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Group = "statuses",
                    MenuUse = true,
                    ButtonUse = false,
                    Action = () =>
                    {
                        SetStatus(5);
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (!row.CheckGet("CANCELED_FLAG").ToBool() && row.CheckGet("STATUS_ID").ToInt() == 4)
                        {
                            result = true;
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "set_status_received",
                    Title = "Получить",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Group = "statuses",
                    MenuUse = true,
                    ButtonUse = false,
                    Action = () =>
                    {
                        SetStatus(7);
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (row.CheckGet("STATUS_ID").ToInt() == 5)
                        {
                            result = true;
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "set_status_cancelled",
                    Title = "Отменить",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Group = "statuses",
                    MenuUse = true,
                    ButtonUse = false,
                    Action = () =>
                    {
                        SetStatus(-1);
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (row.CheckGet("STATUS_ID").ToInt() == 5)
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
        /// Обработка общих сообщений
        /// </summary>
        /// <param name="action"></param>
        /// <param name="obj"></param>
        private void ProcessCommand(string command, ItemMessage obj = null)
        {
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
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Дата заявки",
                    Path="CREATED_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=8,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header="Номер заявки",
                    Path="ORDER_NUM",
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
                    Header="Дата доставки",
                    Path="DELIVERY_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=8,
                    Format="dd.MM HH:mm",
                },
                new DataGridHelperColumn
                {
                    Header="Дата получения",
                    Path="RECEIPT_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=8,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header="Название заявки",
                    Path="ORDER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=30,
                },
                new DataGridHelperColumn
                {
                    Header="Количество элементов",
                    Path="FORM_QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Ремкомплект",
                    Path="REPAIR_KIT_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="Поставщик",
                    Path="SUPPLIER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=22,
                },
                new DataGridHelperColumn
                {
                    Header="Номер поставщика",
                    Path="OUTER_NUM",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Плательщик",
                    Path="BUYER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=25,
                },
                new DataGridHelperColumn
                {
                    Header="Техкарта",
                    Path="TECHCARD_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=35,
                },
                new DataGridHelperColumn
                {
                    Header="Дизайнер",
                    Path="DESIGNER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание",
                    Path="NOTE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
                },
            };

            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("ID");
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            Grid.SearchText = GridSearch;
            Grid.Toolbar = GridToolbar;
            Grid.Commands = Commander;

            Grid.OnLoadItems = LoadItems;
            Grid.Init();
        }

        private async void LoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "RigOrder");
            q.Request.SetParam("Action", "ListContainer");

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Grid.ClearItems();
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "RIG_ORDER");
                    Grid.UpdateItems(ds);
                }
            }
        }

        /// <summary>
        /// Удаление заявки на оснастку. Удалять можно только не заказанную
        /// </summary>
        /// <param name="id"></param>
        private async void DeleteOrder(int id)
        {
            var resume = false;
            var dw = new DialogWindow($"Вы действительно хотите удалить заявку?", "Удаление заявки", "", DialogWindowButtons.NoYes);
            if ((bool)dw.ShowDialog())
            {
                if (dw.ResultButton == DialogResultButton.Yes)
                {
                    resume = true;
                }
            }

            if (resume)
            {
                //Папка заявки
                string orderFolder = Path.GetDirectoryName(Grid.SelectedItem.CheckGet("DRAWING_FILE"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Rig");
                q.Request.SetParam("Object", "RigOrder");
                q.Request.SetParam("Action", "Delete");
                q.Request.SetParam("ID", id.ToString());
                q.Request.SetParam("RIG_TYPE", "3");

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
                            //Удаление прошло успешно, удаляем папку заявки
                            Directory.Delete(orderFolder, true);
                            

                            Grid.LoadItems();
                        }
                    }
                }

            }
        }

        /// <summary>
        /// Изменение статуса заявки
        /// </summary>
        /// <param name="newStatus"></param>
        private async void SetStatus(int newStatus)
        {
            int id = Grid.SelectedItem.CheckGet("ID").ToInt();
            
            if (id > 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Rig");
                q.Request.SetParam("Object", "RigOrder");
                q.Request.SetParam("Action", "SetStatus");
                q.Request.SetParam("ID", id.ToString());
                q.Request.SetParam("STATUS", newStatus.ToString());
                q.Request.SetParam("RIG_TYPE", "3");

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
                            Grid.LoadItems();
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
            if (Grid.Items != null)
            {
                if (Grid.Items.Count > 0)
                {
                    if (Grid.SelectedItem != null)
                    {
                        var path = Grid.SelectedItem.CheckGet("TECHCARD_PATH");
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
    }
}
