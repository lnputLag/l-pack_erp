using Client.Interfaces.Main;
using static Client.Interfaces.Main.DataGridHelperColumn;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Preproduction;
using Client.Interfaces.Sales.NewOrderLt.Frames;
using Newtonsoft.Json;

namespace Client.Interfaces.Sales.NewOrderLt.Tabs
{
    public partial class NewOrderTab : ControlBase
    {
        public NewOrderTab()
        {
            InitializeComponent();
            
            RoleName = "[erp]new_order_lt";
            ControlTitle = "Согласование заявок на ЛТ";
            ControlSection = "new_order_lt_list";

            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverName == "OrderPositionGrid")
                {
                    Commander.ProcessCommand(msg.Action, msg);
                }
                
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
                        Action = () => { Central.ShowHelp(DocumentationUrl); },
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
                            OrderGrid.SelectRowFirst();
                        },
                    });

                    Commander.SetCurrentGroup("item");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "create_order",
                            Title = "Добавить",
                            Group = "orders",
                            MenuUse = true,
                            ButtonUse = true,
                            Enabled = true,
                            ButtonName = "CreateOrderButton",
                            AccessLevel = Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var orderFrame = new CreateOrderFrame();
                                orderFrame.Edit();
                            }
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "edit_order",
                            Title = "Изменить",
                            Group = "orders",
                            MenuUse = true,
                            HotKey = "Return|DoubleCLick",
                            ButtonUse = true,
                            ButtonName = "EditOrderButton",
                            AccessLevel = Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var buyerId = OrderGrid.SelectedItem.CheckGet("ID_POK").ToInt();
                                
                                var k = OrderGrid.GetPrimaryKey();
                                var id = OrderGrid.SelectedItem.CheckGet(k).ToInt();
                                if (id != 0)
                                {
                                    var orderFrame = new CreateOrderFrame();
                                    orderFrame.BuyerId = buyerId;
                                    orderFrame.Edit(id);
                                }
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = OrderGrid.GetPrimaryKey();
                                var row = OrderGrid.SelectedItem;
                                if (row.CheckGet(k).ToInt() != 0)
                                {
                                    if ((row.CheckGet("STATUS").ToInt() == 1) ||
                                        (row.CheckGet("STATUS").ToInt() == 3))
                                    {
                                        result = true;
                                    }
                                }

                                return result;
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "delete_order",
                            Title = "Удалить",
                            Group = "orders",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "DeleteOrderButton",
                            AccessLevel = Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var k = OrderGrid.GetPrimaryKey();
                                var id = OrderGrid.SelectedItem.CheckGet(k).ToInt();
                                if (id != 0)
                                {
                                    DeleteOrder(id);
                                }
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = OrderGrid.GetPrimaryKey();
                                var row = OrderGrid.SelectedItem;
                                if (row.CheckGet(k).ToInt() != 0)
                                {
                                    // Нет позиций, нет тендеров, нет отгрузок
                                    if ((row.CheckGet("NSTHET_OD").ToInt() == 0) &&
                                        (row.CheckGet("TENDER_EXISTS").ToInt() == 0) &&
                                        (row.CheckGet("NSTHET_NR").ToInt() == 0))
                                    {
                                        result = true;
                                    }
                                }

                                return result;
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "accept_order",
                            Title = "Принять",
                            Group = "orders_status",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "AcceptButton",
                            AccessLevel = Role.AccessMode.FullAccess,
                            CheckEnabled = () =>
                            {
                                var item = OrderGrid.SelectedItem;
                                if (item.CheckGet("STATUS").ToInt() == 1)
                                {
                                    return true;
                                }
                                
                                return false;
                            },
                            Action = () =>
                            {
                                var orderId = OrderGrid.SelectedItem.CheckGet("ID").ToInt();
                                var statusId = OrderGrid.SelectedItem.CheckGet("STATUS").ToInt();
                                OrderOnAccept(orderId.ToString(), statusId.ToString());
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "approve_order",
                            Title = "Согласовать",
                            Group = "orders_status",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "ApproveButton",
                            AccessLevel = Role.AccessMode.FullAccess,
                            CheckEnabled = () =>
                            {
                                var item = OrderGrid.SelectedItem;
                                if (item.CheckGet("STATUS").ToInt() == 3 || item.CheckGet("STATUS").ToInt() == 23)
                                {
                                    return true;
                                }
                                
                                return false;
                            },
                            Action = () =>
                            {
                                var orderId = OrderGrid.SelectedItem.CheckGet("ID").ToInt();
                                var status = OrderGrid.SelectedItem.CheckGet("STATUS").ToInt();
                                
                                OrderOnApprove(orderId.ToString(), status.ToString());
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "cancel_order",
                            Title = "Отменить заявку",
                            Group = "orders_status",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "CancelButton",
                            AccessLevel = Role.AccessMode.FullAccess,
                            CheckEnabled = () =>
                            {
                                var item = OrderGrid.SelectedItem;
                                if (item.CheckGet("ID").ToInt() != 0)
                                {
                                    return true;
                                }
                                
                                return false;
                            },
                            Action = () =>
                            {
                                var orderId = OrderGrid.SelectedItem.CheckGet("ID").ToInt();
                                OrderCancel(orderId.ToString());
                            },
                        });
                    }
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
                        Action = () => { OrderGrid.SelectRowByKey(Id); },
                    });

                    Commander.SetCurrentGroup("item");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "create_position",
                            Title = "Добавить",
                            Group = "positions",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "CreatePositionButton",
                            AccessLevel = Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                int orderId = OrderGrid.SelectedItem.CheckGet("ID").ToInt();
                                int idBuyer = OrderGrid.SelectedItem.CheckGet("ID_POK").ToInt();

                                var positionFrame = new CreateOrderPositionFrame();
                                positionFrame.ReceiverName = ControlName;
                                positionFrame.idBuyer = idBuyer;
                                positionFrame.OrderId = orderId;
                                positionFrame.Edit();
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var row = OrderGrid.SelectedItem;
                                if (row.CheckGet("ID").ToInt() != 0)
                                {
                                    //Если заявка еще не отгружена
                                    if ((row.CheckGet("STATUS").ToInt() == 1) ||
                                        (row.CheckGet("STATUS").ToInt() == 3))
                                    {
                                        result = true;
                                    }
                                }

                                return result;
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "edit_position",
                            Title = "Изменить",
                            Group = "positions",
                            MenuUse = true,
                            HotKey = "Return|DoubleCLick",
                            ButtonUse = true,
                            ButtonName = "EditPositionButton",
                            AccessLevel = Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var k = OrderPositionGrid.GetPrimaryKey();
                                var id = OrderPositionGrid.SelectedItem.CheckGet(k).ToInt();
                                int orderId = OrderGrid.SelectedItem.CheckGet("ID").ToInt();
                                bool havePz = OrderPositionGrid.SelectedItem.CheckGet("TASK_EXISTS").ToBool();
                                bool isTender = OrderGrid.SelectedItem.CheckGet("TENDER_IS").ToBool();
                                bool editMode = OrderGrid.SelectedItem.CheckGet("EDIT_MODE").ToBool();
                                if (id != 0)
                                {
                                    var positionFrame = new CreateOrderPositionFrame();
                                    positionFrame.ReceiverName = ControlName;
                                    positionFrame.OrderId = orderId;
                                    positionFrame.tenderIs = isTender;
                                    positionFrame.hasPz = havePz;
                                    positionFrame.editMode = editMode;
                                    positionFrame.Edit(id);
                                }
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = OrderPositionGrid.GetPrimaryKey();
                                var row = OrderPositionGrid.SelectedItem;
                                
                                if (row.CheckGet(k).ToInt() != 0)
                                {
                                    result = true;
                                }
                                
                                var row1 = OrderGrid.SelectedItem;
                                if (row1.CheckGet("ID").ToInt() != 0)
                                {
                                    if (row1.CheckGet("STATUS").ToInt() == 6 ||
                                        row1.CheckGet("STATUS").ToInt() == 13 ||
                                        row1.CheckGet("STATUS").ToInt() == 16 ||
                                        row1.CheckGet("STATUS").ToInt() == 23 ||
                                        row1.CheckGet("STATUS").ToInt() == 33)
                                    {
                                        result = false;
                                    }
                                }
                                
                                return result;
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "delete_position",
                            Title = "Удалить",
                            Group = "positions",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "DeletePositionButton",
                            AccessLevel = Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var k = OrderPositionGrid.GetPrimaryKey();
                                var id = OrderPositionGrid.SelectedItem.CheckGet(k).ToInt();
                                if (id > 0)
                                {
                                    DeletePosition(id);
                                }
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = OrderPositionGrid.GetPrimaryKey();
                                var row = OrderPositionGrid.SelectedItem;
                                if (row.CheckGet(k).ToInt() != 0)
                                {
                                    bool taskExists = row.CheckGet("TASK_EXISTS").ToBool();
                                    if (!taskExists)
                                    {
                                        result = true;
                                    }
                                }
                                
                                var row1 = OrderGrid.SelectedItem;
                                if (row1.CheckGet("ID").ToInt() != 0)
                                {
                                    if (row1.CheckGet("STATUS").ToInt() == 6 ||
                                        row1.CheckGet("STATUS").ToInt() == 13 ||
                                        row1.CheckGet("STATUS").ToInt() == 16 ||
                                        row1.CheckGet("STATUS").ToInt() == 23 ||
                                        row1.CheckGet("STATUS").ToInt() == 33)
                                    {
                                        result = false;
                                    }
                                }

                                return result;
                            },
                        });
                    }
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
                    Header = "#",
                    Path = "_ROWNUMBER",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 8,
                },
                new DataGridHelperColumn
                {
                    Header = "Дата отгрузки",
                    Path = "SHIPMENT_DATE",
                    ColumnType = ColumnTypeRef.DateTime,
                    Width2 = 10,
                    Format = "dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header = "Согл. дт отрузки",
                    Path = "NEW_SHIPMENT_DATE",
                    Description = "Согласованная дата отгрузки",
                    ColumnType = ColumnTypeRef.DateTime,
                    Width2 = 10,
                    Format = "dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header = "Дата доставки",
                    Path = "DELIVERY_DATE",
                    ColumnType = ColumnTypeRef.DateTime,
                    Width2 = 10,
                    Format = "dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header = "Грузополучатель",
                    Path = "CONSIGNEE",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 30,
                },
                new DataGridHelperColumn
                {
                    Header = "Самовывоз",
                    Path = "SELF_DELIVERY",
                    ColumnType = ColumnTypeRef.Boolean,
                    Width2 = 5,
                },
                new DataGridHelperColumn
                {
                    Header = "Номер заявки",
                    Path = "NUMBER_ORDER",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 10,
                },
                new DataGridHelperColumn
                {
                    Header = "Статус",
                    Path = "FINISH_STATUS",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 24,
                },
                new DataGridHelperColumn
                {
                    Header = "STATUS",
                    Path = "STATUS",
                    ColumnType = ColumnTypeRef.Boolean,
                    Width2 = 5,
                    Visible = false,
                },
                new DataGridHelperColumn
                {
                    Header = "Счет на предоплату",
                    Path = "PREPAID_NUMBER",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 16,
                },
                new DataGridHelperColumn
                {
                    Header = "Уточнение даты",
                    Path = "DATE_CONFIRMATION",
                    ColumnType = ColumnTypeRef.Boolean,
                    Width2 = 5,
                },
                new DataGridHelperColumn
                {
                    Header = "Ожидание оплаты",
                    Path = "PREPAY_CONFIRMATION",
                    ColumnType = ColumnTypeRef.Boolean,
                    Width2 = 5,
                },
                new DataGridHelperColumn
                {
                    Header = "Печать счета",
                    Path = "PRINT_ACCOUNT",
                    ColumnType = ColumnTypeRef.Boolean,
                    Width2 = 5,
                },
                new DataGridHelperColumn
                {
                    Header = "Печать СФ",
                    Path = "PRINT_INVOICE",
                    ColumnType = ColumnTypeRef.Boolean,
                    Width2 = 5,
                },
                new DataGridHelperColumn
                {
                    Header = "Примечание грузчику",
                    Path = "NOTE",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 10,
                },
                new DataGridHelperColumn
                {
                    Header = "Примечание кладовщику",
                    Path = "NOTE_GENERAL",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 10,
                },
                new DataGridHelperColumn
                {
                    Header = "Примечание логисту",
                    Path = "NOTE_LOGISTIC",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 19,
                },
                new DataGridHelperColumn
                {
                    Header = "Продавец",
                    Path = "SELLER_NAME",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 30,
                },
                new DataGridHelperColumn
                {
                    Header = "Покупатель",
                    Path = "BUYER_NAME",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 25,
                },
                new DataGridHelperColumn
                {
                    Header = "№ СФ",
                    Path = "INVOICE_NUM",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 10,
                },
                new DataGridHelperColumn
                {
                    Header = "ИД",
                    Path = "ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 10,
                },
                new DataGridHelperColumn
                {
                    Header = "TENDER_IS",
                    Path = "TENDER_IS",
                    ColumnType = ColumnTypeRef.Boolean,
                    Width2 = 5,
                    Visible = false,
                },
                new DataGridHelperColumn
                {
                    Header = "ИД Покупателя",
                    Path = "ID_POK",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 5,
                    Visible = false,
                }
            };
            OrderGrid.SetColumns(columns);
            OrderGrid.SetPrimaryKey("ID");
            OrderGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            OrderGrid.SearchText = SearchText;
            OrderGrid.Toolbar = OrderGridToolbar;
            OrderGrid.Commands = Commander;
            
            OrderGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                {
                    StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result = DependencyProperty.UnsetValue;
                        var color = "";

                        if (row.CheckGet("STATUS").ToInt() == 1)
                        {
                            color = HColor.Blue;
                        }
                        
                        if (row.CheckGet("STATUS").ToInt() == 6)
                        {
                            color = HColor.Green;
                        }
                        
                        if (row.CheckGet("STATUS").ToInt() == 16)
                        {
                            color = HColor.Green;
                        }
                        
                        if (row.CheckGet("STATUS").ToInt() == 23)
                        {
                            color = HColor.Yellow;
                        }
                        
                        if (row.CheckGet("STATUS").ToInt() == 33)
                        {
                            color = HColor.Yellow;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result = color.ToBrush();
                        }

                        return result;
                    }
                },

                {
                    StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result = DependencyProperty.UnsetValue;
                        var color = "";
                        
                        if (row.CheckGet("STATUS").ToInt() == 13)
                        {
                            color = HColor.BlueDark;
                        }
                        
                        if (row.CheckGet("STATUS").ToInt() == 16)
                        {
                            color = HColor.BlueDark;
                        }
                        
                        if (row.CheckGet("STATUS").ToInt() == 33)
                        {
                            color = HColor.BlueDark;
                        }
                        
                        if (!string.IsNullOrEmpty(color))
                        {
                            result = color.ToBrush();
                        }

                        return result;
                    }
                }
            };
            
            
            OrderGrid.OnLoadItems = OrderGridLoadItems;
            OrderGrid.OnSelectItem = (selectItem) =>
            {
                Id = selectItem.CheckGet("ID");
                OrderPositionGridLoadItems(Id);
                
                Commander.ProcessSelectItem(selectItem);
                OrderPositionGrid.SelectRowFirst();
                OrderPositionGrid.Commands.ProcessSelectItem(OrderPositionGrid.SelectedItem);
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
                    Header = "#",
                    Path = "_ROWNUMBER",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                    Header = "Позиция отгрузки",
                    Path = "SHIP_ORDER",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                    Header = "Артикул",
                    Path = "SKU_CODE",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 15,
                },
                new DataGridHelperColumn
                {
                    Header = "Изделие",
                    Path = "PRODUCT_NAME",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 30,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество",
                    Path = "QTY",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 6,
                },
                new DataGridHelperColumn // Новое
                {
                    Header = "Количество под отгрузку",
                    Path = "PRODUCT_QTY",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 6,
                },
                new DataGridHelperColumn // Новое
                {
                    Header = "Количество на складе",
                    Path = "TOTAL_PRODUCT_QTY",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                    Header = "Цена без НДС",
                    Path = "PRICE_VAT_EXCLUDED",
                    ColumnType = ColumnTypeRef.Double,
                    Width2 = 10,
                },
                new DataGridHelperColumn
                {
                    Header = "Цена с НДС",
                    Path = "PRICE",
                    ColumnType = ColumnTypeRef.Double,
                    Width2 = 10,
                },
                new DataGridHelperColumn
                {
                    Header = "Фиксированная цена",
                    Path = "FIX_PRICE",
                    ColumnType = ColumnTypeRef.Boolean,
                    Width2 = 5,
                },
                new DataGridHelperColumn
                {
                    Header = "ПЗ",
                    Path = "TASK_EXISTS",
                    ColumnType = ColumnTypeRef.Boolean,
                    Width2 = 5,
                },
                new DataGridHelperColumn
                {
                    Header = "Адрес доставки",
                    Path = "ADDRESS",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 30,
                },
                new DataGridHelperColumn
                {
                    Header = "Примечание кладовщику",
                    Path = "NOTE_GENERAL",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 20,
                },
                new DataGridHelperColumn
                {
                    Header = "ИД",
                    Path = "ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 5,
                },
            };
            OrderPositionGrid.SetColumns(columns);
            OrderPositionGrid.SetPrimaryKey("ID");
            OrderPositionGrid.ItemsAutoUpdate = false;
            OrderPositionGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            OrderPositionGrid.Toolbar = PositionOrderGridToolbar;
            OrderPositionGrid.Commands = Commander;
            OrderPositionGrid.Init();
        }

        /// <summary>
        /// Загрузка списка заявок
        /// </summary>
        private async void OrderGridLoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "NewOrderLt");
            q.Request.SetParam("Object", "Order");
            q.Request.SetParam("Action", "List");
            
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
                    OrderGrid.UpdateItems(OrderGridDataSet);
                    
                    if (OrderGridDataSet.Items.Count > 0)
                    {
                        OrderGrid.SelectRowFirst();
                    }
                    else
                    {
                        OrderPositionGrid.GridControl.ItemsSource = null;
                        var buttons = PositionOrderGridToolbar.Children.OfType<Button>();
                        foreach (var button in buttons)
                        {
                            button.IsEnabled = false;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Удаление заявки
        /// </summary>
        /// <param name="id"></param>
        private async void DeleteOrder(int id)
        {
            var dw = new DialogWindow("Вы действительно хотите удалить заявку?", "Удаление заявки", "",
                DialogWindowButtons.NoYes);
            if ((bool)dw.ShowDialog())
            {
                if (dw.ResultButton == DialogResultButton.Yes)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "NewOrderLt");
                    q.Request.SetParam("Object", "Order");
                    q.Request.SetParam("Action", "Delete");
                    q.Request.SetParam("ORDER_ID", id.ToString());

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            if (result.ContainsKey("ITEMS"))
                            {
                                OrderGrid.LoadItems();
                            }
                        }
                    }
                    else if (q.Answer.Error.Code == 145)
                    {
                        q.ProcessError();
                    }
                }
            }
        }

        /// <summary>
        /// Смена статуса 1 -> 3
        /// </summary>
        /// <param name="orderId"></param>
        private async void OrderOnAccept(string orderId, string statusId)
        {
            var dw = new DialogWindow("Вы действительно хотите принять заявку?", "Согласование заявки", "",
                DialogWindowButtons.NoYes);
            if ((bool)dw.ShowDialog())
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "NewOrderLt");
                q.Request.SetParam("Object", "Order");
                q.Request.SetParam("Action", "OnAccept");
                q.Request.SetParam("ORDER_ID", orderId);
                q.Request.SetParam("STATUS", statusId);

                await Task.Run(() => { q.DoQuery(); });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result!= null)
                    {
                        if (result.ContainsKey("ITEM"))
                        {
                            OrderGrid.LoadItems();
                            OrderGrid.SelectRowByKey(orderId);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Смена статуса 3 -> 6; 23 -> 0
        /// </summary>
        /// <param name="orderId"></param>
        private async void OrderOnApprove(string orderId, string status)
        {
            var dw = new DialogWindow("Вы действительно хотите согласовать заявку?", "Согласование заявки", "",
                DialogWindowButtons.NoYes);
            if ((bool)dw.ShowDialog())
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "NewOrderLt");
                q.Request.SetParam("Object", "Order");
                q.Request.SetParam("Action", "OnApproval");
                q.Request.SetParam("ORDER_ID", orderId);
                q.Request.SetParam("STATUS", status);

                await Task.Run(() => { q.DoQuery(); });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        if (result.ContainsKey("ITEM"))
                        {
                            OrderGrid.LoadItems();
                            OrderGrid.SelectRowByKey(orderId);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Смена статуса на 2 (Отмена)
        /// </summary>
        /// <param name="orderId"></param>
        private async void OrderCancel(string orderId)
        {
            var dw = new DialogWindow("Вы действительно хотите отменить заявку?", "Отмена заявки", "",
                DialogWindowButtons.NoYes);
            if ((bool)dw.ShowDialog())
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "NewOrderLt");
                q.Request.SetParam("Object", "Order");
                q.Request.SetParam("Action", "Cancel");
                q.Request.SetParam("ORDER_ID", orderId);

                await Task.Run(() => { q.DoQuery(); });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        if (result.ContainsKey("ITEM"))
                        {
                            OrderGrid.LoadItems();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Удаление позиции из заявки
        /// </summary>
        /// <param name="id"></param>
        private async void DeletePosition(int id)
        {
            var dw = new DialogWindow("Вы действительно хотите удалить позицию из заявки?",
                "Удаление позиции из заявки", "", DialogWindowButtons.NoYes);
            if ((bool)dw.ShowDialog())
            {
                if (dw.ResultButton == DialogResultButton.Yes)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "NewOrderLt");
                    q.Request.SetParam("Object", "OrderPosition");
                    q.Request.SetParam("Action", "Delete");
                    q.Request.SetParam("POSITION_ID", id.ToString());

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    if (q.Answer.Status == 0)
                    {
                        OrderPositionGridLoadItems(Id);
                    }
                    else if (q.Answer.Error.Code == 145)
                    {
                        q.ProcessError();
                    }
                }
            }
        }

        /// <summary>
        /// Загрузка списка позиций заявки по ID заявки
        /// </summary>
        /// <param name="orderId">ИД</param>
        private async void OrderPositionGridLoadItems(string orderId)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "NewOrderLt");
            q.Request.SetParam("Object", "OrderPosition");
            q.Request.SetParam("Action", "List");
            q.Request.SetParam("ORDER_ID", orderId);
            
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
        }
    }
}