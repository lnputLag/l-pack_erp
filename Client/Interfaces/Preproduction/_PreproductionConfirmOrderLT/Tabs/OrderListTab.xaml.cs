using Client.Interfaces.Main;
using static Client.Interfaces.Main.DataGridHelperColumn;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Client.Common;
using Client.Interfaces.Preproduction.PreproductionConfirmOrderLt.Frames;
using Newtonsoft.Json;

namespace Client.Interfaces.Preproduction.PreproductionConfirmOrderLt.Tabs
{
    /// <summary>
    /// Главное окно интерфейса подтверждение заявок для ЛТ
    /// </summary>
    /// <author>volkov_as</author>
    public partial class OrderListTab : ControlBase
    {
        public OrderListTab()
        {
            InitializeComponent();

            ControlSection = "order_tk";
            RoleName = "[erp]confirm_order_lt";
            ControlTitle = "Подтверждение заявок на ЛТ";
            
            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverName == "OrderGrid")
                {
                    Commander.ProcessCommand(msg.Action, msg);
                }
            };

            OnLoad = () =>
            {
                InitOrderGrid();
                InitPositionGrid();
            };

            OnUnload = () =>
            {
                OrderGrid.Destruct();
                OrderPositionGrid.Destruct();
            };

            OnFocusGot = () =>
            {
                OrderGrid.ItemsAutoUpdate = true;
                OrderPositionGrid.ItemsAutoUpdate = true;
            };

            OnFocusLost = () =>
            {
                OrderGrid.ItemsAutoUpdate = false;
                OrderPositionGrid.ItemsAutoUpdate = false;
            };

            {
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "help",
                        Enabled = true,
                        Title = "Справка",
                        Description = "Показать справочную информацию",
                        ButtonUse = true,
                        ButtonName = "HelpButton",
                        HotKey = "F1",
                        Action = () =>
                        {
                            Central.ShowHelp(DocumentationUrl);
                        },
                    });
                }

                Commander.SetCurrentGridName("OrderGrid");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "refresh_order_grid",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить данные",
                        ButtonUse = true,
                        ButtonName = "RefreshOrder",
                        MenuUse = true,
                        Action = () =>
                        {
                            OrderGrid.LoadItems();
                        },
                    });

                    Commander.SetCurrentGroup("item");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "accept_order",
                            Title = "Принять",
                            MenuUse = true,
                            Enabled = true,
                            ButtonUse = true,
                            ButtonName = "AcceptOrder",
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                if (OrderGrid.GetItemsSelected().Count > 0)
                                {
                                    var itemId = "";

                                    foreach (var item in OrderGrid.Items)
                                    {
                                        if (item.CheckGet("_SELECTED") == "True")
                                        {
                                            itemId += $" №{item.CheckGet("ID")},";
                                        }
                                    }
                                    
                                    if (itemId.EndsWith(","))
                                    {
                                        itemId = itemId.TrimEnd(',');
                                    }
                                    
                                    var dialog = new DialogWindow($"Согласовать заявки - {itemId}?", "Управление заявкой", "",
                                        DialogWindowButtons.YesNo);

                                    dialog.ShowDialog();

                                    if (dialog.DialogResult == true)
                                    {
                                        foreach (var item in OrderGrid.Items)
                                        {
                                            if (item.CheckGet("_SELECTED") == "True")
                                            {
                                                AcceptOrderLt(item.CheckGet("ID"));
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    var row = OrderGrid.SelectedItem;

                                    var dialog = new DialogWindow($"Согласовать заявку - №{row.CheckGet("ID")}?", "Управление заявкой", "",
                                        DialogWindowButtons.YesNo);

                                    if (!row.CheckGet("ID").IsNullOrEmpty())
                                    {
                                        dialog.ShowDialog();
                                    }
                                    else
                                    {
                                        var dialog1 = new DialogWindow("Заявка не выбрана", "Управление заявкой", "");
                                        dialog1.ShowDialog();
                                    }

                                    if (dialog.DialogResult == true)
                                    {
                                        AcceptOrderLt(row.CheckGet("ID"));
                                    }
                                }
                            },
                            CheckEnabled = () =>
                            {
                                var row = OrderGrid.SelectedItem;

                                if (row.CheckGet("STATUS_NUM").ToInt() == 6 ||
                                    row.CheckGet("STATUS_NUM").ToInt() == 16 )
                                {
                                    return true;
                                }

                                return false;
                            }
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "approve_date_order",
                            Title = "Утвердить дату",
                            MenuUse = true,
                            Enabled = true,
                            ButtonUse = true,
                            ButtonName = "ApproveDate",
                            AccessLevel = Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var row = OrderGrid.SelectedItem;

                                var approve = new ApproveDateOrderFrame();
                                var id = row.CheckGet("ID").ToInt();
                                var date = row.CheckGet("SHIPMENT_DATE");
                                approve.Edit(orderId: id, date: date);
                            },
                            CheckEnabled = () =>
                            {
                                var row = OrderGrid.SelectedItem;

                                if (row.CheckGet("STATUS_NUM").ToInt() == 6 ||
                                    row.CheckGet("STATUS_NUM").ToInt() == 16 )
                                {
                                    return true;
                                }

                                return false;
                            }
                        });
                    };
                }

                Commander.SetCurrentGridName("OrderPositionGrid");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "refresh_order_position_grid",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить данные",
                        ButtonUse = true,
                        ButtonName = "RefreshOrderPosition",
                        MenuUse = true,
                        Action = () =>
                        {
                            OrderPositionGrid.LoadItems();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "show_tech_map",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Показать ТК",
                        Description = "Показать ТК",
                        ButtonUse = true,
                        ButtonName = "ShowTechMap",
                        MenuUse = true,
                        Action = () =>
                        {
                            TechnologicalMapShow();
                        }
                    });
                }

                Commander.Init(this);
            }
        }

        public ListDataSet OrderGridDataSet { get; set; }
        public ListDataSet OrderPositionGridDataSet { get; set; }
        private string Id { get; set; }

        private void InitOrderGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="*",
                    Path="_SELECTED",
                    ColumnType = ColumnTypeRef.Boolean,
                    Width2=7,
                    Exportable = false,
                    Editable = true,
                    OnClickAction = (row, el) =>
                    {
                        if (row.CheckGet("STATUS_NUM").ToInt() == 6 ||
                            row.CheckGet("STATUS_NUM").ToInt() == 16)
                        {
                            return true;
                        }

                        return false;
                    }
                },
                new DataGridHelperColumn
                {
                    Header="Дата отгрузки",
                    Path="SHIPMENT_DATE",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=10,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header="SHIPMENT_ID",
                    Path="SHIPMENT_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=0,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header="Дата доставки",
                    Path="DELIVERY_DATE",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=10,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header = "Предложенная дата",
                    Path = "NEW_SHIPMENT_DATE",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=10,
                    Format="dd.MM.yyyy"
                },
                new DataGridHelperColumn
                {
                    Header="Грузополучатель",
                    Path="CONSIGNEE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=30,
                },
                new DataGridHelperColumn
                {
                    Header="Самовывоз",
                    Path="SELF_DELIVERY",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="Номер заявки",
                    Path="NUMBER_ORDER",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Статус",
                    Path="STATUS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn
                {
                    Header="Счет на предоплату",
                    Path="PREPAID_NUMBER",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Уточнение даты",
                    Path="DATE_CONFIRMATION",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="Ожидание оплаты",
                    Path="PREPAY_CONFIRMATION",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание кладовщику",
                    Path="NOTE_GENERAL",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
            };
            OrderGrid.SetColumns(columns);
            OrderGrid.SetPrimaryKey("ID");
            OrderGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            OrderGrid.SearchText = SearchText;
            OrderGrid.Toolbar = OrderGridToolbar;
            OrderGrid.Commands = Commander;
            OrderGrid.OnLoadItems = OrderGridLoadItems;
            OrderGrid.OnSelectItem = (selectItem) =>
            {
                Id = selectItem.CheckGet("ID").ToString();
                OrderPositionGrid.LoadItems();
            };
            OrderGrid.Init();
        }

        /// <summary>
        /// Инициализация таблицы позиций заявки
        /// </summary>
        private void InitPositionGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Позиция отгрузки",
                    Path="SHIP_ORDER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="SKU_CODE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn
                {
                    Header="Изделие",
                    Path="PRODUCT_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=30,
                },
                new DataGridHelperColumn
                {
                    Header="Количество",
                    Path="QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=9,
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во под отрузку",
                    Path = "PRODUCT_QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=9,
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во на складе",
                    Path = "TOTAL_PRODUCT_QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=9,
                },
                new DataGridHelperColumn
                {
                    Header="ПЗ",
                    Path="TASK_EXISTS",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header = "Рабочий центр",
                    Path = "WORK_CENTER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=14,
                },
                new DataGridHelperColumn
                {
                    Header="Адрес доставки",
                    Path="ADDRESS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=30,
                },
                new DataGridHelperColumn
                {
                    Header="Этикетка",
                    Path="STICKER",
                    ColumnType=ColumnTypeRef.String,
                    Width2=18,
                },
                new DataGridHelperColumn
                {
                    Header="Краска",
                    Path="INK",
                    ColumnType=ColumnTypeRef.String,
                    Width2=43,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание кладовщику",
                    Path="NOTE_GENERAL",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                  Header = "Дата последней отгрузки",
                  Path = "LAST_SHIPPED",
                  ColumnType = ColumnTypeRef.DateTime,
                  Width2 = 10,
                },
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                },
            };
            OrderPositionGrid.SetColumns(columns);
            OrderPositionGrid.SetPrimaryKey("ID");
            OrderPositionGrid.ItemsAutoUpdate = false;
            OrderPositionGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            OrderPositionGrid.Toolbar = PositionOrderGridToolbar;
            OrderPositionGrid.Commands = Commander;
            OrderPositionGrid.AutoUpdateInterval = 0;
            OrderPositionGrid.OnLoadItems = OrderPositionGridLoadItems;
            OrderPositionGrid.Init();
        }


        /// <summary>
        /// Загрузка списка заявок
        /// </summary>
        private async void OrderGridLoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "ConfirmOrderLT");
            q.Request.SetParam("Action", "List");

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
                    OrderGridDataSet = ListDataSet.Create(result, "ITEMS");
                    
                    if (OrderGridDataSet.Items != null)
                    {
                        foreach (var item in OrderGridDataSet.Items)
                        {
                            var row = OrderGridDataSet.Items.FirstOrDefault(x => x.CheckGet("RIG_ID").ToInt() == item.CheckGet("RIG_ID").ToInt());
                            if (row != null)
                            {
                                item.CheckAdd("_SELECTED", row.CheckGet("_SELECTED"));
                            }
                            else
                            {
                                item.CheckAdd("_SELECTED", "0");
                            }
                        }
                    }
                }
            }

            OrderGrid.UpdateItems(OrderGridDataSet);
        }


        /// <summary>
        /// Загрузка позиций заявки
        /// </summary>
        private async void OrderPositionGridLoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "ConfirmOrderLT");
            q.Request.SetParam("Action", "PositionList");
            q.Request.SetParam("NSTHET", Id);

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
                    OrderPositionGridDataSet = ListDataSet.Create(result, "ITEMS");
                    OrderPositionGrid.UpdateItems(OrderPositionGridDataSet);
                }
            }
            else if (q.Answer.Status == 142)
            {
                OrderPositionGrid.ClearItems();
            }

            
        }
        
        /// <summary>
        /// Обработчик для кнопки "Показать ТК"
        /// </summary>
        private void TechnologicalMapShow()
        {
            if (OrderPositionGrid.Items.Count > 0)
            {
                if (OrderPositionGrid.SelectedItem != null)
                {
                    var path = OrderPositionGrid.SelectedItem.CheckGet("PATHTK");
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


        /// <summary>
        /// Запрос который принимает задачу
        /// </summary>
        private async void AcceptOrderLt(string id)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "ConfirmOrderLT");
            q.Request.SetParam("Action", "UpdateAcceptOrder");
            q.Request.SetParam("NSTHET", id);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                OrderGrid.LoadItems();
                if (OrderGrid.Items.Count - 1 > 0)
                {
                    OrderGrid.SelectRowFirst();
                }
                else
                {
                    Id = "";
                    OrderPositionGridLoadItems();
                }

            }
        }
    }
}
