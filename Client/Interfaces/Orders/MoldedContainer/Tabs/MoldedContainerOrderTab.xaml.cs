using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Client.Interfaces.Orders.MoldedContainer.Frames;
using Client.Interfaces.Preproduction;
using static Client.Interfaces.Main.DataGridHelperColumn;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Client.Interfaces.Orders.MoldedContainer
{
    /// <summary>
    /// Список заявок на литую тару
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class MoldedContainerOrderTab : ControlBase
    {
        public MoldedContainerOrderTab()
        {
            ControlTitle = "Заявки на ЛТ";
            DocumentationUrl = "/doc/l-pack-erp/orders/molded_container_order";
            RoleName = "[erp]molded_contnr_order";

            InitializeComponent();

            SetDefaults();

            InitBuyerGrid();
            InitOrderGrid();
            InitPositionGrid();
            InitShipmentGrid();
            InitShipmentOrderGrid();
            InitShipmentPositionGrid();

            OnUnload = () =>
            {
                BuyerGrid.Destruct();
                OrderGrid.Destruct();
                PositionGrid.Destruct();
                ShipmentGrid.Destruct();
                ShipmentOrderGrid.Destruct();
                ShipmentPositionGrid.Destruct();
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
                    Group = "main",
                    Enabled = true,
                    Title = "Обновить",
                    Description = "Обновить данные",
                    ButtonUse = true,
                    ButtonName = "RefreshButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    MenuUse = true,
                    Action = () =>
                    {
                        BuyerGrid.LoadItems();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "help",
                    Group = "main",
                    Enabled = true,
                    Title = "Справка",
                    Description = "Показать справочную информацию",
                    ButtonUse = true,
                    ButtonName = "HelpButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    HotKey = "F1",
                    Action = () =>
                    {
                        Central.ShowHelp(DocumentationUrl);
                    },
                });
            }
            Commander.SetCurrentGridName("OrderGrid");
            {
                //Commander.SetCurrentGroup("orders");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "create_order",
                        Title = "Добавить",
                        Group = "orders",
                        MenuUse = false,
                        ButtonUse = false,
                        ButtonName = "CreateOrderButton",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            int buyerId = BuyerGrid.SelectedItem.CheckGet("ID").ToInt();

                            var orderFrame = new MoldedContainerOrder();
                            orderFrame.ReceiverName = ControlName;
                            orderFrame.BuyerId = buyerId;
                            orderFrame.Edit();
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            // Проверяем, что выбран покупатель, чтобы привязать к нему новую заявку
                            var row = BuyerGrid.SelectedItem;
                            if (row.CheckGet("ID").ToInt() != 0)
                            {
                                result = true;
                            }
                            return result;
                        },
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
                            var k = OrderGrid.GetPrimaryKey();
                            var id = OrderGrid.SelectedItem.CheckGet(k).ToInt();
                            int buyerId = BuyerGrid.SelectedItem.CheckGet("ID").ToInt();
                            if (id != 0)
                            {
                                var orderFrame = new MoldedContainerOrder();
                                orderFrame.ReceiverName = ControlName;
                                orderFrame.BuyerId = buyerId;
                                orderFrame.Edit(id);
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var k = OrderGrid.GetPrimaryKey();
                            var row = OrderGrid.SelectedItem;
                            var i = row.CheckGet(k).ToInt();
                            if (row.CheckGet(k).ToInt() != 0)
                            {
                                if ((row.CheckGet("STATUS").ToInt() == 0) && (row.CheckGet("NSTHET_NR").ToInt() == 0))
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
                        Name = "on_approval_order",
                        Title = "На согласование",
                        Group = "orders",
                        MenuUse = true,
                        ButtonUse = true,
                        ButtonName = "OnApproval",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var k = OrderGrid.GetPrimaryKey();
                            var id = OrderGrid.SelectedItem.CheckGet(k).ToInt();
                            if (id != 0)
                            {
                                OnApprovalOrder(id);
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;

                            if (OrderGrid.SelectedItem.CheckGet("STATUS").ToInt() == 1)
                            {
                                result = true;
                            }

                            return result;
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "cancel_order",
                        Title = "Отменить заявку",
                        Group = "orders",
                        MenuUse = true,
                        ButtonUse = false,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var row = OrderGrid.SelectedItem;
                            if (row != null)
                            {
                                SelectedShipmentId = row.CheckGet("SHIPMENT_ID").ToInt();
                                SelectedShipmentOrderId = row.CheckGet("ID").ToInt();
                                // Ставим только дату
                                ShipmentDate.Text = row.CheckGet("SHIPMENT_DATE").Substring(0, 10);
                                ShipmentGrid.LoadItems();
                                CancelOrder();
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = OrderGrid.SelectedItem;
                            if (row.CheckGet("ID").ToInt() != 0)
                            {
                                // Заявка принята, завершенных отгрузок нет
                                if ((row.CheckGet("STATUS").ToInt() == 0) && (row.CheckGet("NSTHET_NR").ToInt() == 0))
                                {
                                    result = true;
                                }
                            }

                            return result;
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "add_proxy_order",
                        Title = "Добавить доверенность",
                        Group = "orders_operations_proxy",
                        MenuUse = true,
                        Enabled = true,
                        ButtonUse = true,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var id = OrderGrid.SelectedItem.CheckGet("ID").ToInt();
                            AddProxy(id);
                        },
                        CheckEnabled = () =>
                        {
                            var proxy = OrderGrid.SelectedItem.CheckGet("PROXY");

                            if (string.IsNullOrEmpty(proxy))
                            {
                                return true;
                            }
                            
                            return false;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "delete_proxy_order",
                        Title = "Удалить доверенность",
                        Group = "orders_operations_proxy",
                        MenuUse = true,
                        Enabled = true,
                        ButtonUse = true,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var id = OrderGrid.SelectedItem.CheckGet("ID").ToInt();
                            var proxy = OrderGrid.SelectedItem.CheckGet("PROXY");
                            DeleteProxy(id, proxy);
                        },
                        CheckEnabled = () =>
                        {
                            var proxy = OrderGrid.SelectedItem.CheckGet("PROXY");

                            if (!string.IsNullOrEmpty(proxy))
                            {
                                return true;
                            }
                            
                            return false;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "open_proxy_order",
                        Title = "Открыть доверенность",
                        Group = "orders_operations_proxy",
                        MenuUse = true,
                        Enabled = true,
                        ButtonUse = true,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var id = OrderGrid.SelectedItem.CheckGet("ID").ToInt();
                            var proxy = OrderGrid.SelectedItem.CheckGet("PROXY");
                            OpenProxy(id, proxy);
                        },
                        CheckEnabled = () =>
                        {
                            var proxy = OrderGrid.SelectedItem.CheckGet("PROXY");

                            if (!string.IsNullOrEmpty(proxy))
                            {
                                return true;
                            }
                            
                            return false;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "shipment_for_order",
                        Title = "Создать отгрузку",
                        Group = "orders_operations",
                        MenuUse = true,
                        ButtonUse = true,
                        ButtonName = "CreateShipmentForOrderButton",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var k = OrderGrid.GetPrimaryKey();
                            var id = OrderGrid.SelectedItem.CheckGet(k).ToInt();
                            if (id != 0)
                            {
                                var d = new Dictionary<string, string>()
                                {
                                    { "ORDER_ID", id.ToString() },
                                    { "PAYER_ID", OrderGrid.SelectedItem.CheckGet("SELLER_ID") },
                                    { "DEFAULT_DATE", OrderGrid.SelectedItem.CheckGet("SHIPMENT_DATE") }
                                };

                                var transportFrame = new MoldedContainerTransport();
                                transportFrame.ReceiverName = ControlName;
                                transportFrame.SelfShipmentFlag =
                                    OrderGrid.SelectedItem.CheckGet("SELF_DELIVERY").ToBool();
                                transportFrame.ShowTab(d);
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var k = OrderGrid.GetPrimaryKey();
                            var row = OrderGrid.SelectedItem;
                            if (row.CheckGet(k).ToInt() != 0)
                            {
                                if (row.CheckGet("SHIPMENT_ID").ToInt() == 0 &&
                                    (row.CheckGet("STATUS").ToInt() == 0))
                                {
                                    // Проверяем наличие заявки
                                    if (PositionGrid.Items != null)
                                    {
                                        if (PositionGrid.Items.Count > 0)
                                        {
                                            result = true;
                                        }
                                    }
                                }
                            }

                            return result;
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "show_shipment",
                        Title = "Показать отгрузку",
                        Group = "orders_operations",
                        MenuUse = true,
                        ButtonUse = false,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var row = OrderGrid.SelectedItem;
                            if (row != null)
                            {
                                SelectedShipmentId = row.CheckGet("SHIPMENT_ID").ToInt();
                                SelectedShipmentOrderId = row.CheckGet("ID").ToInt();
                                // Ставим только дату
                                ShipmentDate.Text = row.CheckGet("DATETS").Substring(0, 10);
                                orderStatus = row.CheckGet("STATUS").ToInt();
                                orderNsthtNr = row.CheckGet("NSTHET_NR").ToInt();
                                Commander.ProcessSelectItem(row);
                                ShipmentGrid.LoadItems();
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = OrderGrid.SelectedItem;
                            if (row.CheckGet("SHIPMENT_ID").ToInt() != 0)
                            {
                                if (!row.CheckGet("SHIPMENT_DATE").IsNullOrEmpty())
                                {
                                    result = true;
                                }
                            }

                            return result;
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "show_history_order",
                        Title = "История заказа",
                        Group = "orders_history_operations",
                        Enabled = true,
                        MenuUse = true,
                        ButtonUse = false,
                        AccessLevel = Role.AccessMode.ReadOnly,
                        Action = () =>
                        {
                            var v = OrderGrid.SelectedItem;
                            var orderId = v.CheckGet("ID");
                            
                            var w = new MoldedContainerHistory();
                            w.ShowHistory("order", orderId: orderId);
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "prepaid_receipt",
                        Title = "Счет на предоплату",
                        Group = "shipment_prepaid",
                        MenuUse = true,
                        ButtonUse = false,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var k = OrderGrid.GetPrimaryKey();
                            var id = OrderGrid.SelectedItem.CheckGet(k).ToInt();

                            if (id != 0)
                            {
                                GetPrepaidReceipt(id);
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = OrderGrid.SelectedItem;
                            if (row != null)
                            {
                                int orderId = row.CheckGet("ID").ToInt();
                                if (orderId != 0)
                                {
                                    result = true;
                                }
                            }

                            return result;
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "delete_receipt",
                        Title = "Удалить счет на предоплату",
                        Group = "shipment_prepaid",
                        MenuUse = true,
                        ButtonUse = false,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var k = OrderGrid.GetPrimaryKey();
                            var id = OrderGrid.SelectedItem.CheckGet(k).ToInt();
                            if (id != 0)
                            {
                                DeletePrepaidReceipt(id);
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = OrderGrid.SelectedItem;
                            if (row != null)
                            {
                                int orderId = row.CheckGet("ID").ToInt();
                                int prepaidId = row.CheckGet("PREPAID_ID").ToInt();
                                if (orderId != 0 && prepaidId != 0)
                                {
                                    result = true;
                                }
                            }

                            return result;
                        },
                    });
                }
            }
            Commander.SetCurrentGridName("PositionGrid");
            {
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "create_position",
                        Title = "Добавить",
                        Group = "positions",
                        MenuUse = false,
                        ButtonUse = true,
                        ButtonName = "CreatePositionButton",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            int orderId = OrderGrid.SelectedItem.CheckGet("ID").ToInt();

                            var positionFrame = new MoldedContainerPosition();
                            positionFrame.ReceiverName = ControlName;
                            positionFrame.OrderId = orderId;
                            positionFrame.Edit();
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            // Проверяем, что выбрана заявка, чтобы привязать к нему новую заявку
                            var row = OrderGrid.SelectedItem;
                            if (row.CheckGet("ID").ToInt() != 0)
                            {
                                //Если заявка еще не отгружена
                                if (((row.CheckGet("STATUS").ToInt() == 0) || (row.CheckGet("STATUS").ToInt() == 1)) &&
                                    (row.CheckGet("NSTHET_NR").ToInt() == 0))
                                {
                                    result = true;
                                }
                            }

                            return result;
                        },
                        CheckVisible = () =>
                        {
                            bool editMode = OrderGrid.SelectedItem.CheckGet("EDIT_MODE").ToBool();
                            
                            return editMode;
                        }
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
                            var k = PositionGrid.GetPrimaryKey();
                            var id = PositionGrid.SelectedItem.CheckGet(k).ToInt();
                            int orderId = OrderGrid.SelectedItem.CheckGet("ID").ToInt();
                            bool havePz = PositionGrid.SelectedItem.CheckGet("TASK_EXISTS").ToBool();
                            bool isTender = OrderGrid.SelectedItem.CheckGet("TENDER_IS").ToBool();
                            bool editMode = OrderGrid.SelectedItem.CheckGet("EDIT_MODE").ToBool();
                            if (id != 0)
                            {
                                var positionFrame = new MoldedContainerPosition();
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
                            var k = PositionGrid.GetPrimaryKey();
                            var row = PositionGrid.SelectedItem;
                            var i = row.CheckGet(k).ToInt();

                            if (row.CheckGet(k).ToInt() != 0)
                            {
                                result = true;
                            }

                            var row1 = OrderGrid.SelectedItem;
                            if (row1.CheckGet("ID").ToInt() != 0)
                            {
                                //Если заявка еще не отгружена
                                if (row1.CheckGet("STATUS").ToInt() == 6 || row1.CheckGet("NSTHET_NR").ToInt() > 0)
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
                            var k = PositionGrid.GetPrimaryKey();
                            var id = PositionGrid.SelectedItem.CheckGet(k).ToInt();
                            if (id > 0)
                            {
                                DeletePosition(id);
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var k = PositionGrid.GetPrimaryKey();
                            var row = PositionGrid.SelectedItem;
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
                                //Если заявка еще не отгружена
                                if (row1.CheckGet("STATUS").ToInt() == 6)
                                {
                                    result = false;
                                }
                            }

                            return result;
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "show_history_position_order",
                        Title = "История позиции",
                        Group = "positions_history",
                        MenuUse = true,
                        Enabled= true,
                        ButtonUse = false,
                        AccessLevel = Role.AccessMode.ReadOnly,
                        Action = () =>
                        {
                            var r = PositionGrid.SelectedItem;
                            var positionId = r.CheckGet("ID");
                            
                            var w = new MoldedContainerHistory();
                            w.ShowHistory("position", positionId: positionId);
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "cut_position_order",
                        Title = "Вырезать",
                        Group = "operation",
                        MenuUse = true,
                        ButtonName = "CutPosition",
                        Enabled = true,
                        ButtonUse = true,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            IdBuyer = BuyerGrid.SelectedItem.CheckGet("ID").ToInt();
                            IdOrder = OrderGrid.SelectedItem.CheckGet("ID").ToInt();

                            SelectedPosition.Clear();

                            SelectedPosition.AddRange(PositionGrid.GetItemsSelected());

                            if (SelectedPosition.Count > 0)
                            {
                                var dialog = new DialogWindow($"Было скопировано {SelectedPosition.Count} позиции", "Перемещение");
                                BufferInfo.Content = $"В буфере {SelectedPosition.Count} позиций";
                                
                                StringBuilder sb = new StringBuilder();
                                foreach (var item in PositionGrid.GetItemsSelected())
                                {
                                    sb.AppendLine($"{item.CheckGet("SKU_CODE")} {item.CheckGet("PRODUCT_NAME")} {item.CheckGet("QTY")}");
                                }

                                BufferInfo.ToolTip = sb.ToString();
                                sb.Clear();
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = true;

                            if (PositionGrid.GetItemsSelected().Count == 0)
                            {
                                result = false;
                            }

                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "paste_position_order",
                        Title = "Вставить",
                        Group = "operation",
                        MenuUse = true,
                        ButtonName = "PastePosition",
                        Enabled = true,
                        ButtonUse = true,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var resume = true;

                            if (SelectedPosition.Count == 0)
                            {
                                var dialog = new DialogWindow("Список позиций пустой", "Перемещение");
                                dialog.ShowDialog();
                                resume = false;
                            }

                            if (IdBuyer != BuyerGrid.SelectedItem.CheckGet("ID").ToInt() && resume)
                            {
                                var dialog = new DialogWindow("Позиции нельзя перемещать среди разных покупателей", "Перемещение");
                                dialog.ShowDialog();
                                resume = false;
                            }

                            if (IdOrder == OrderGrid.SelectedItem.CheckGet("ID").ToInt() && resume)
                            {
                                var dialog = new DialogWindow("Вставка не возможна. Выбрана изначальная заявка.", "Перемещение");
                                dialog.ShowDialog();
                                resume = false;
                            }

                            if (OrderGrid.SelectedItem.CheckGet("FINISH_STATUS") == "Отгружена")
                            {
                                var dialog = new DialogWindow("Нельзя вставлять позиции в уже отгруженные заявки", "Перемещение");
                                resume = false;
                            }

                            if (resume)
                            {
                                foreach (var newItem in SelectedPosition)
                                {
                                    foreach (var existItem in PositionGrid.Items)
                                    {
                                        if (newItem.CheckGet("ID2").ToInt() == existItem.CheckGet("ID2").ToInt())
                                        {
                                            var dialog = new DialogWindow("Нельзя вставлять одинаковые позиции в 1 заявку", "Перемещение");
                                            dialog.ShowDialog();
                                            resume = false;
                                            return;
                                        }
                                    }
                                }
                            }

                            if (resume)
                            {
                                PositionMovement(OrderGrid.SelectedItem.CheckGet("ID"));

                                // После успешной вставки
                                foreach (var item in PositionGrid.Items)
                                {
                                    item.CheckAdd("_SELECTED", "0");
                                }
                                SelectedPosition.Clear();
                                BufferInfo.Content = $"В буфере 0 позиций";
                                BufferInfo.ToolTip = "";
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = true;

                            if (SelectedPosition.Count == 0)
                            {
                                result = false;
                            }

                            return result;
                        }
                    });
                }
            }
            Commander.SetCurrentGridName("ShipmentGrid");
            {
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "refresh_shipment",
                        Title = "Обновить",
                        Group = "shipments",
                        MenuUse = true,
                        ButtonUse = true,
                        ButtonName = "RefreshShipmentButton",
                        AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        ShipmentGrid.LoadItems();
                    },
                        CheckEnabled = () =>
                        {
                            var result = true;
                            return result;
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "create_shipment",
                        Title = "Добавить",
                        Group = "shipments",
                        MenuUse = true,
                        ButtonUse = true,
                        ButtonName = "CreateShipmentButton",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var d = new Dictionary<string, string>()
                            {
                                { "DEFAULT_DATE", ShipmentDate.Text },
                            };

                            var shipmentFrame = new MoldedContainerTransport();
                            shipmentFrame.ReceiverName = ControlName;
                            shipmentFrame.ShowTab(d);
                        },
                        CheckEnabled = () =>
                        {
                            var result = true;
                            return result;
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "edit_shipment",
                        Title = "Изменить",
                        Group = "shipments",
                        MenuUse = true,
                        ButtonUse = true,
                        ButtonName = "EditShipmentButton",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var k = ShipmentGrid.GetPrimaryKey();
                            var id = ShipmentGrid.SelectedItem.CheckGet(k).ToInt();
                            if (id > 0)
                            {
                                var d = new Dictionary<string, string>()
                                {
                                    { "SHIPMENT_ID", id.ToString() },
                                };

                                var shipmentFrame = new MoldedContainerTransport();
                                shipmentFrame.ReceiverName = ControlName;
                                shipmentFrame.OrderId = 0;
                                shipmentFrame.PayerId = 0;
                                shipmentFrame.ShowTab(d);
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var k = ShipmentGrid.GetPrimaryKey();
                            var row = ShipmentGrid.SelectedItem;
                            if (row.CheckGet(k).ToInt() != 0)
                            {
                                if (row.CheckGet("STATUS").ToInt() != 3)
                                {
                                    result = true;
                                }
                            }

                            return result;
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "delete_shipment",
                        Title = "Удалить",
                        Group = "shipments",
                        MenuUse = true,
                        ButtonUse = true,
                        ButtonName = "DeleteShipmentButton",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var k = ShipmentGrid.GetPrimaryKey();
                            var id = ShipmentGrid.SelectedItem.CheckGet(k).ToInt();
                            if (id > 0)
                            {
                                DeleteShipment(id);
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var k = ShipmentGrid.GetPrimaryKey();
                            var row = ShipmentGrid.SelectedItem;
                            if (row.CheckGet(k).ToInt() != 0)
                            {
                                if ((row.CheckGet("STATUS").ToInt() != 3) && (row.CheckGet("ORDER_IDTS").ToInt() == 0))
                                {
                                    result = true;
                                }
                            }

                            return result;
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "allow_shipment",
                        Title = "Разрешить отгрузку",
                        Group = "shipment_operations",
                        MenuUse = true,
                        ButtonUse = false,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var k = ShipmentGrid.GetPrimaryKey();
                            var id = ShipmentGrid.SelectedItem.CheckGet(k).ToInt();
                            if (id > 0)
                            {
                                AllowShipment(id, 1);
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var k = ShipmentGrid.GetPrimaryKey();
                            var row = ShipmentGrid.SelectedItem;
                            if (row.CheckGet(k).ToInt() != 0)
                            {
                                if (row.CheckGet("FINISH").ToInt() == 0)
                                {
                                    result = true;
                                }
                            }

                            return result;
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "disallow_shipment",
                        Title = "Запретить отгрузку",
                        Group = "shipment_operations",
                        MenuUse = true,
                        ButtonUse = false,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var k = ShipmentGrid.GetPrimaryKey();
                            var id = ShipmentGrid.SelectedItem.CheckGet(k).ToInt();
                            if (id > 0)
                            {
                                AllowShipment(id, 0);
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var k = ShipmentGrid.GetPrimaryKey();
                            var row = ShipmentGrid.SelectedItem;
                            if (row.CheckGet(k).ToInt() != 0)
                            {
                                if (row.CheckGet("FINISH").ToInt() == 1)
                                {
                                    result = true;
                                }
                            }

                            return result;
                        },
                    });
                }
            }
            Commander.SetCurrentGridName("ShipmentOrderGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "append_to_shipment",
                    Title = "Добавить в отгрузку",
                    Group = "shipment_order",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonName = "AppendToShipmentButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        OrderToShipment();
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var rowShipment = ShipmentGrid.SelectedItem;
                        var rowOrder = OrderGrid.SelectedItem;
                        if (rowShipment != null && rowOrder != null)
                        {
                            int status = rowOrder.CheckGet("STATUS").ToInt();
                            bool tender = rowShipment.CheckGet("TENDER").ToBool();
                            int orderShipment = rowOrder.CheckGet("SHIPMENT_ID").ToInt();
                            bool positionExists = rowOrder.CheckGet("NSTHET_OD").ToBool();
                            bool shipmentOrder = rowShipment.CheckGet("INVOICE_QTY").ToBool();

                            if ((status == 0) && !tender && (orderShipment == 0) && positionExists && !shipmentOrder)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "remove_from_shipment",
                    Title = "Удалить из отгрузки",
                    Group = "shipment_order",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonName = "RemoveFromShipmentButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        RemoveFromShipment();
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = ShipmentOrderGrid.SelectedItem;
                        if (row != null)
                        {
                            int orderId = row.CheckGet("ID").ToInt();
                            if (orderId != 0)
                            {
                                // Отгрузки еще не было
                                if (row.CheckGet("NSTHET_NR").ToInt() == 0 && orderStatus != 4)
                                {
                                    result = true;
                                }
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "edit_shipment_order",
                    Title = "Изменить",
                    Group = "shipment_order",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonName = "EditOrderShipmentButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = ShipmentOrderGrid.GetPrimaryKey();
                        var id = ShipmentOrderGrid.SelectedItem.CheckGet(k).ToInt();
                        int buyerId = ShipmentOrderGrid.SelectedItem.CheckGet("CUSTOMER_ID").ToInt();
                        if (id != 0)
                        {
                            var orderFrame = new MoldedContainerOrder();
                            orderFrame.ReceiverName = ControlName;
                            orderFrame.BuyerId = buyerId;
                            orderFrame.Edit(id);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = ShipmentOrderGrid.SelectedItem;
                        if (row != null)
                        {
                            int orderId = row.CheckGet("ID").ToInt();
                            if (orderId != 0)
                            {
                                // Отгрузки еще не было
                                if (row.CheckGet("NSTHET_NR").ToInt() == 0 && orderStatus != 4)
                                {
                                    result = true;
                                }
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                    {
                        Name = "add_proxy_order_ship",
                        Title = "Добавить доверенность",
                        Group = "shipment_orders_operations_proxy",
                        MenuUse = true,
                        Enabled = true,
                        ButtonUse = true,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var id = ShipmentOrderGrid.SelectedItem.CheckGet("ID").ToInt();
                            AddProxy(id);
                        },
                        CheckEnabled = () =>
                        {
                            var proxy = ShipmentOrderGrid.SelectedItem.CheckGet("NAME_PROXY");

                            if (string.IsNullOrEmpty(proxy))
                            {
                                return true;
                            }
                            
                            return false;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "delete_proxy_order_ship",
                        Title = "Удалить доверенность",
                        Group = "shipment_orders_operations_proxy",
                        MenuUse = true,
                        Enabled = true,
                        ButtonUse = true,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var id = ShipmentOrderGrid.SelectedItem.CheckGet("ID").ToInt();
                            var proxy = ShipmentOrderGrid.SelectedItem.CheckGet("NAME_PROXY");
                            DeleteProxy(id, proxy);
                        },
                        CheckEnabled = () =>
                        {
                            var proxy = ShipmentOrderGrid.SelectedItem.CheckGet("NAME_PROXY");

                            if (!string.IsNullOrEmpty(proxy))
                            {
                                return true;
                            }
                            
                            return false;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "open_proxy_order_ship",
                        Title = "Открыть доверенность",
                        Group = "shipment_orders_operations_proxy",
                        MenuUse = true,
                        Enabled = true,
                        ButtonUse = true,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var id = ShipmentOrderGrid.SelectedItem.CheckGet("ID").ToInt();
                            var proxy = ShipmentOrderGrid.SelectedItem.CheckGet("NAME_PROXY");
                            OpenProxy(id, proxy);
                        },
                        CheckEnabled = () =>
                        {
                            var proxy = ShipmentOrderGrid.SelectedItem.CheckGet("NAME_PROXY");

                            if (!string.IsNullOrEmpty(proxy))
                            {
                                return true;
                            }
                            
                            return false;
                        }
                    });
                Commander.Add(new CommandItem()
                {
                    Name = "prepaid_receipt",
                    Title = "Счет на предоплату",
                    Group = "shipment_order_operations",
                    MenuUse = true,
                    ButtonUse = false,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = ShipmentOrderGrid.GetPrimaryKey();
                        var id = ShipmentOrderGrid.SelectedItem.CheckGet(k).ToInt();

                        if (id != 0)
                        {
                            GetPrepaidReceipt(id);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = ShipmentOrderGrid.SelectedItem;
                        if (row != null)
                        {
                            int orderId = row.CheckGet("ID").ToInt();
                            if (orderId != 0)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "shipment_order_operations",
                    Title = "История",
                    Group = "shipment_order_history",
                    Enabled = true,
                    MenuUse = true,
                    ButtonUse = false,
                    Action = () =>
                    {
                        var v = ShipmentOrderGrid.SelectedItem;
                        var shipmentOrderId = v.CheckGet("ID");
                            
                        var w = new MoldedContainerHistory();
                        w.ShowHistory("shipment_order", shipmentOrderId: shipmentOrderId);
                    }
                });
                Commander.Add(new CommandItem()
                {
                    Name = "delete_receipt",
                    Title = "Удалить счет на предоплату",
                    Group = "shipment_order_operations",
                    MenuUse = true,
                    ButtonUse = false,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = ShipmentOrderGrid.GetPrimaryKey();
                        var id = ShipmentOrderGrid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            DeletePrepaidReceipt(id);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = ShipmentOrderGrid.SelectedItem;
                        if (row != null)
                        {
                            int orderId = row.CheckGet("ID").ToInt();
                            int prepaidId = row.CheckGet("PREPAID_ID").ToInt();
                            if (orderId != 0 && prepaidId != 0)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
            }
            Commander.SetCurrentGridName("ShipmentPositionGrid");
            {
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "create_shipment_position",
                        Title = "Добавить",
                        Group = "shipment_positions",
                        MenuUse = true,
                        ButtonUse = true,
                        ButtonName = "AppendToOrderShipmentButton",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            int orderId = ShipmentOrderGrid.SelectedItem.CheckGet("ID").ToInt();

                            var positionFrame = new MoldedContainerPosition();
                            positionFrame.ReceiverName = ControlName;
                            positionFrame.OrderId = orderId;
                            positionFrame.Edit();
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            // Проверяем, что выбрана заявка, чтобы привязать к нему новую заявку
                            var row = ShipmentOrderGrid.SelectedItem;
                            if (row.CheckGet("ID").ToInt() != 0)
                            {
                                // Отгрузки еще не было
                                var shipped = ShipmentGrid.SelectedItem.CheckGet("INVOICE_QTY").ToInt();
                                if (shipped == 0)
                                {
                                    result = true;
                                }
                            }

                            return result;
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "edit_shipment_position",
                        Title = "Изменить",
                        Group = "shipment_positions",
                        MenuUse = true,
                        ButtonUse = true,
                        ButtonName = "EditPositionOrderShipmentButton",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var k = ShipmentPositionGrid.GetPrimaryKey();
                            var id = ShipmentPositionGrid.SelectedItem.CheckGet(k).ToInt();
                            int orderId = ShipmentOrderGrid.SelectedItem.CheckGet("ID").ToInt();
                            if (id != 0)
                            {
                                var positionFrame = new MoldedContainerPosition();
                                positionFrame.ReceiverName = ControlName;
                                positionFrame.OrderId = orderId;
                                positionFrame.Edit(id);
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var k = ShipmentPositionGrid.GetPrimaryKey();
                            var row = ShipmentPositionGrid.SelectedItem;
                            var rowOrder = ShipmentOrderGrid.SelectedItem;
                            var i = row.CheckGet(k).ToInt();
                            if (row.CheckGet(k).ToInt() != 0 && rowOrder.CheckGet("NSTHET_NR").ToInt() == 0 && orderStatus != 4)
                            {
                                result = true;
                            }

                            return result;
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "delete_shipment_position",
                        Title = "Удалить",
                        Group = "shipment_positions",
                        MenuUse = true,
                        ButtonUse = true,
                        ButtonName = "RemoveFromOrderShipmentButton",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var k = ShipmentPositionGrid.GetPrimaryKey();
                            var id = ShipmentPositionGrid.SelectedItem.CheckGet(k).ToInt();
                            if (id > 0)
                            {
                                DeletePosition(id);
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var k = ShipmentPositionGrid.GetPrimaryKey();
                            var row = ShipmentPositionGrid.SelectedItem;
                            var rowOrder = ShipmentOrderGrid.SelectedItem;
                            if (row.CheckGet(k).ToInt() != 0)
                            {
                                if (row.CheckGet("INVOICE_ID").ToInt() == 0 && rowOrder.CheckGet("NSTHET_NR").ToInt() == 0 && orderStatus != 4)
                                {
                                    result = true;
                                }
                            }

                            return result;
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "shipment_position_order_history",
                        Title = "История",
                        Group = "shipment_positions_history",
                        MenuUse = true,
                        ButtonUse = false,
                        Enabled = true,
                        Action = () =>
                        {
                            var v = ShipmentPositionGrid.SelectedItem;
                            var shipmentPositionId = v.CheckGet("ID");

                            var w = new MoldedContainerHistory();
                            w.ShowHistory("shipment_position", shipmentPositionId: shipmentPositionId);
                        }
                    });
                }
            }
            Commander.Init(this);
        }

        /// <summary>
        /// Данные для таблицы позиций в отгрузке
        /// </summary>
        ListDataSet ShipmentPositionDS { get; set; }

        /// <summary>
        /// ID выбранной заявки в таблице заявок в отгрузке. Используется для подсветки строк позиций в отгрузке
        /// </summary>
        int SelectedShipmentOrderId;

        /// <summary>
        /// Для отслеживания статуса заказа
        /// </summary>
        private int orderStatus { get; set; }
        private int orderNsthtNr { get; set; }
        
        public static string ProxyStorageCode { get;  } = "order_proxy";
        /// <summary>
        /// Содержит список скопированных позиций
        /// </summary>
        private List<Dictionary<string, string>> SelectedPosition = new List<Dictionary<string, string>>();
        private int IdBuyer { get; set; } = 0;
        private int IdOrder { get; set; } = 0;

        /// <summary>
        /// ID отгрузки для выбора после загрузки данных для поиска по выбранной заявке
        /// </summary>
        int SelectedShipmentId;

        /// <summary>
        /// Обработка общих сообщений
        /// </summary>
        /// <param name="action"></param>
        /// <param name="obj"></param>
        private void ProcessMessage(ItemMessage obj = null)
        {
            string action = obj.Action;
            switch (action)
            {
                case "RefreshOrders":
                    OrderGrid.LoadItems();
                    ShipmentOrderGrid.LoadItems();
                    break;
                case "RefreshPosition":
                    OrderGrid.LoadItems();
                    ShipmentPositionGrid.LoadItems();
                    break;
                case "RefreshShipments":
                    ShipmentGrid.LoadItems();
                    break;
            }
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            ShipmentDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
        }

        /// <summary>
        /// Инициализация таблицы покупателей
        /// </summary>
        private void InitBuyerGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "#",
                    Path = "_ROWNUMBER",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 3,
                },
                new DataGridHelperColumn
                {
                    Header = "Покупатель",
                    Path = "CUSTOMER_NAME",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 25,
                },
                new DataGridHelperColumn
                {
                    Header = "ИД",
                    Path = "ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 8,
                },
            };
            BuyerGrid.SetColumns(columns);
            BuyerGrid.SetPrimaryKey("ID");
            BuyerGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            BuyerGrid.SearchText = BuyerSearchText;
            BuyerGrid.Toolbar = BuyerToolbar;
            BuyerGrid.Commands = Commander;

            BuyerGrid.AutoUpdateInterval = 0;

            BuyerGrid.OnLoadItems = BuyerLoadItems;

            BuyerGrid.OnSelectItem = (selectItem) => { UpdateBuyerAction(selectItem); };


            BuyerGrid.Init();
        }

        /// <summary>
        /// Инициализация таблицы заявок
        /// </summary>
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
                    Width2 = 12,
                    Format = "dd.MM.yyyy HH:mm",
                },
                new DataGridHelperColumn
                {
                    Header = "Дата доставки",
                    Path = "DELIVERY_DATE",
                    ColumnType = ColumnTypeRef.DateTime,
                    Width2 = 12,
                    Format = "dd.MM.yyyy HH:mm",
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
                    Header = "Доверенность",
                    Path = "IS_PROXY",
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
                    Width2 = 10,
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
                    Width2 = 8,
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
                    Width2 = 10,
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
                    Header = "Proxy",
                    Path = "PROXY",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 5,
                    Visible = false,
                },
                new DataGridHelperColumn
                {
                    Header = "Дата отгрузки",
                    Path = "DATETS",
                    ColumnType = ColumnTypeRef.DateTime,
                    Width2 = 12,
                    Format = "dd.MM.yyyy HH:mm",
                    Visible = false
                }
            };
            OrderGrid.SetColumns(columns);
            OrderGrid.SetPrimaryKey("ID");
            // OrderGrid.SetSorting("SHIPMENT_DATE", ListSortDirection.Ascending);
            OrderGrid.ItemsAutoUpdate = false;
            OrderGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            OrderGrid.SearchText = OrderSearchText;
            OrderGrid.Toolbar = OrderToolbar;
            OrderGrid.Commands = Commander;

            // Раскраска строк
            OrderGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                // Цвета шрифта строк
                {
                    StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result = DependencyProperty.UnsetValue;
                        var color = "";

                        // Есть накладная на отгрузку - отгрузка завершена
                        if ((row.CheckGet("STATUS").ToInt() == 4) || (row.CheckGet("NSTHET_NR").ToInt() > 0))
                        {
                            color = HColor.Green;
                        }

                        if ((row.CheckGet("STATUS").ToInt() == 1) || (row.CheckGet("STATUS").ToInt() == 6))
                        {
                            color = HColor.Blue;
                        }

                        if (row.CheckGet("STATUS").ToInt() == 2)
                        {
                            color = HColor.Red;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result = color.ToBrush();
                        }

                        return result;
                    }
                },
            };

            OrderGrid.OnLoadItems = OrdersLoadItems;
            OrderGrid.OnSelectItem = (selectItem) =>
            {
                UpdateOrderAction(selectItem);
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
                    Header = "*",
                    Path = "_SELECTED",
                    ColumnType = ColumnTypeRef.Boolean,
                    Width2 = 7,
                    Exportable = false,
                    Editable = true,
                    OnClickAction = (row, el) =>
                    {
                        if (OrderGrid.SelectedItem.CheckGet("FINISH_STATUS") == "Отгружена")
                        {
                            return false;
                        }

                        return true;
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "#",
                    Path = "_ROWNUMBER",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                    Header = "Номер точки доставки",
                    Path = "SHIP_ORDER",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 12,
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
                    Header = "Статус",
                    Path = "STATUS_NAME",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 10,
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
                new DataGridHelperColumn
                {
                    Header = "NSTHET",
                    Path = "NSTHET",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 5,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "ID2",
                    Path = "ID2",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 5,
                    Visible = false
                }
            };
            PositionGrid.SetColumns(columns);
            PositionGrid.SetPrimaryKey("ID");
            PositionGrid.ItemsAutoUpdate = false;
            PositionGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            PositionGrid.Toolbar = PositionToolbar;
            PositionGrid.Commands = Commander;

            PositionGrid.AutoUpdateInterval = 0;

            PositionGrid.OnLoadItems = PositionLoadItems;

            PositionGrid.Init();
        }

        /// <summary>
        /// Инициализация таблиицы отгрузок
        /// </summary>
        private void InitShipmentGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "#",
                    Path = "N",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                    Header = "Время",
                    Path = "TM",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 5,
                },
                new DataGridHelperColumn
                {
                    Header = "Покупатель",
                    Path = "CUSTOMER_NAME",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 15,
                },
                new DataGridHelperColumn
                {
                    Header = "Тендер",
                    Path = "TENDER",
                    ColumnType = ColumnTypeRef.Boolean,
                    Width2 = 5,
                },
                new DataGridHelperColumn
                {
                    Header = "ИД",
                    Path = "ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 5,
                },
            };
            ShipmentGrid.SetColumns(columns);
            ShipmentGrid.SetPrimaryKey("ID");
            ShipmentGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            ShipmentGrid.Toolbar = ShipmentToolbar;
            ShipmentGrid.Commands = Commander;

            // Раскраска строк
            ShipmentGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                // Цвета шрифта строк
                {
                    StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result = DependencyProperty.UnsetValue;
                        var color = "";

                        var currentStatus = row.CheckGet("FINISH").ToBool();
                        if (currentStatus == true)
                        {
                            color = HColor.Green;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result = color.ToBrush();
                        }

                        return result;
                    }
                },
            };

            ShipmentGrid.AutoUpdateInterval = 0;

            ShipmentGrid.OnLoadItems = ShipmentLoadItems;
            ShipmentGrid.OnSelectItem = (selectItem) =>
            {
                UpdateShipmentAction(selectItem);
            };

            ShipmentGrid.Init();
        }

        /// <summary>
        /// Инициализация таблицы заявок в отгрузке
        /// </summary>
        private void InitShipmentOrderGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "#",
                    Path = "SHIP_NUMBER",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                    Header = "Доставка",
                    Path = "SUPPLY_DATE",
                    ColumnType = ColumnTypeRef.DateTime,
                    Width2 = 10,
                    Format = "dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header = "Покупатель",
                    Path = "CUSTOMER_NAME",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 20,
                },
                new DataGridHelperColumn
                {
                    Header = "Грузополучатель",
                    Path = "CONSIGNEE_NAME",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 20,
                },
                new DataGridHelperColumn
                {
                    Header = "Самовывоз",
                    Path = "SELFSHIP",
                    ColumnType = ColumnTypeRef.Boolean,
                    Width2 = 5,
                },
                new DataGridHelperColumn
                {
                    Header = "Доверенность",
                    Path = "PROXY",
                    ColumnType = ColumnTypeRef.Boolean,
                    Width2 = 5,
                },
                new DataGridHelperColumn
                {
                    Header = "Номер",
                    Path = "NUMBER_ORDER",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                    Header = "Счет на предоплату",
                    Path = "PREPAID_NUMBER",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 8,
                },
                new DataGridHelperColumn
                {
                    Header = "ИД",
                    Path = "ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 5,
                },
                new DataGridHelperColumn
                {
                    Header = "NAME_PROXY",
                    Path = "NAME_PROXY",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 15,
                    Visible = false,
                },
            };

            ShipmentOrderGrid.SetColumns(columns);
            ShipmentOrderGrid.SetPrimaryKey("ID");
            ShipmentOrderGrid.ItemsAutoUpdate = false;
            ShipmentOrderGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            ShipmentOrderGrid.Toolbar = ShipmentOrderToolbar;
            ShipmentOrderGrid.Commands = Commander;
            ShipmentOrderGrid.AutoUpdateInterval = 0;
            ShipmentOrderGrid.OnLoadItems = ShipmentOrderLoadItems;
            ShipmentOrderGrid.OnSelectItem = (selectItem) =>
            {
                UpdateShipmentOrderAction(selectItem);
            };
            ShipmentOrderGrid.OnDblClick = selectedItem =>
            {
                Commander.ProcessCommand("edit_shipment_order");
            };


            ShipmentOrderGrid.Init();
        }

        /// <summary>
        /// Инициализация таблицы позиций в отгрузке
        /// </summary>
        private void InitShipmentPositionGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "Позиция отгрузки",
                    Path = "SHIP_ORDER",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 6,
                    Stylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("ORDER_ID").ToInt() == SelectedShipmentOrderId)
                                {
                                    color = HColor.Pink;
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        },
                    },
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
                new DataGridHelperColumn
                {
                    Header = "Количество под отгрузку",
                    Path = "PRODUCT_QTY",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество на складе",
                    Path = "TOTAL_PRODUCT_QTY",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                    Header = "Отгружено",
                    Path = "SHIPPED_QTY",
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
            ShipmentPositionGrid.SetColumns(columns);
            ShipmentPositionGrid.SetPrimaryKey("ID");
            ShipmentPositionGrid.ItemsAutoUpdate = false;
            ShipmentPositionGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            ShipmentPositionGrid.Toolbar = ShipmentPositionToolbar;
            ShipmentPositionGrid.Commands = Commander;
            ShipmentPositionGrid.AutoUpdateInterval = 0;
            ShipmentPositionGrid.OnLoadItems = ShipmentPositionLoadItems;
            ShipmentPositionGrid.OnDblClick = selectedItem =>
            {
                Commander.ProcessCommand("edit_shipment_position");
            };

            ShipmentPositionGrid.Init();
        }

        /// <summary>
        /// Загрузка данных в таблицу покупателей
        /// </summary>
        private async void BuyerLoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Orders");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "ListBuyer");

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    // Очищаем зависимые таблицы
                    OrderGrid.ClearItems();
                    OrderGrid.Items.Clear();
                    OrderGrid.SelectedItem.Clear();
                    PositionGrid.ClearItems();
                    PositionGrid.Items.Clear();

                    var ds = ListDataSet.Create(result, "BUYERS");
                    BuyerGrid.UpdateItems(ds);
                }
            }
        }

        /// <summary>
        /// Загрузка данных в таблицу заявок покупателя
        /// </summary>
        private async void OrdersLoadItems()
        {
            //OrderGrid.ShowSplash();
            OrderGrid.Toolbar.IsEnabled = false;

            bool allOrders = (bool)AllOrdersCheckBox.IsChecked;
            int buyerId = BuyerGrid.SelectedItem.CheckGet("ID").ToInt();

            if (buyerId > 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Orders");
                q.Request.SetParam("Object", "MoldedContainer");
                q.Request.SetParam("Action", "ListOrders");
                q.Request.SetParam("BUYER_ID", buyerId.ToString());
                q.Request.SetParam("ALL_ORDERS", allOrders ? "1" : "0");

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    // Очищаем зависимые таблицы
                    PositionGrid.Items.Clear();
                    PositionGrid.ClearItems();
                    PositionGrid.SelectedItem.Clear();

                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ORDERS");
                        OrderGrid.UpdateItems(ds);
                    }
                }
            }

            //OrderGrid.Toolbar.IsEnabled = true;
            OrderGrid.HideSplash();
        }

        private async void PositionMovement(string newOrderId)
        {
            var list = new List<int>();
            var count = SelectedPosition.Count;

            foreach (var item in SelectedPosition)
            {
                list.Add(item.CheckGet("ID").ToInt());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Orders");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "MovePosition");
            q.Request.SetParam("ORDER_ID", newOrderId);
            q.Request.SetParam("LIST_POSITION", JsonConvert.SerializeObject(list));

            await Task.Run(() => q.DoQuery());

            if (q.Answer.Status == 0)
            {
                OrderGrid.LoadItems();
                PositionGrid.LoadItems();

                var dialog = new DialogWindow($"Вставка {count} позиций прошла успешно", "Перемещение");
                dialog.ShowDialog();
            } 
            else
            {
                var dialog = new DialogWindow("Во время переноса произошла ошибка","Перемещение");
                dialog.ShowDialog();
            }
        }

        /// <summary>
        /// Загрузка данных в таблицу позиций
        /// </summary>
        private async void PositionLoadItems()
        {
            //PositionGrid.ShowSplash();
            PositionGrid.Toolbar.IsEnabled = false;

            int orderId = OrderGrid.SelectedItem.CheckGet("ID").ToInt();

            if (orderId > 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Orders");
                q.Request.SetParam("Object", "MoldedContainer");
                q.Request.SetParam("Action", "ListPosition");
                q.Request.SetParam("ORDER_ID", orderId.ToString());

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "POSITIONS");
                        PositionGrid.UpdateItems(ds);
                    }
                }
            }

            PositionGrid.Toolbar.IsEnabled = true;
            //PositionGrid.HideSplash();
        }


        /// <summary>
        /// Функция для отмены заявки в таблице OrderGrid
        /// </summary>
        private async void CancelOrder()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Orders");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "CancelOrder");
            q.Request.SetParam("ORDER_ID", OrderGrid.SelectedItem.CheckGet("ID").ToInt().ToString());
            
            await Task.Run(() =>
            {
                q.DoQuery();
            });
            
            if (q.Answer.Status == 0)
            {
                OrdersLoadItems();
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Загрузка данных в таблицу отгрузок
        /// </summary>
        private async void ShipmentLoadItems()
        {
            ShipmentGrid.ShowSplash();
            ShipmentGrid.Toolbar.IsEnabled = false;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Orders");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "ListShipment");
            q.Request.SetParam("DATE_TS", ShipmentDate.Text);
            q.Request.SetParam("KIND", "6");

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "LSIT_SHIPMENT");
                    ShipmentGrid.UpdateItems(ds);

                    // Если надо показать отгрузку
                    if (SelectedShipmentId > 0)
                    {
                        ShipmentGrid.SelectRowByKey(SelectedShipmentId.ToString());
                        SelectedShipmentId = 0;
                    }

                    // Очищаем зависимые таблицы
                    ShipmentOrderGrid.ClearItems();
                    ShipmentOrderGrid.SelectedItem.Clear();
                    ShipmentPositionGrid.ClearItems();
                    ShipmentPositionGrid.SelectedItem.Clear();
                }
            }
            else if (q.Answer.Error.Code == 145)
            {
                q.ProcessError();
            }

            ShipmentGrid.Toolbar.IsEnabled = true;
            ShipmentGrid.HideSplash();
        }

        /// <summary>
        /// Загрузка данных в таблицу заявок в отгрузке
        /// </summary>
        private async void ShipmentOrderLoadItems()
        {
            ShipmentOrderGrid.Toolbar.IsEnabled = false;
            //ShipmentOrderGrid.ShowSplash();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Orders");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "ListShipmentOrder");
            q.Request.SetParam("SHIPMENT_ID", ShipmentGrid.SelectedItem.CheckGet("ID"));

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "SHIPMENT_ORDERS");
                    ShipmentOrderGrid.UpdateItems(ds);
                }
            }
            else if (q.Answer.Error.Code == 145)
            {
                q.ProcessError();
            }

            //ShipmentOrderGrid.HideSplash();
            ShipmentOrderGrid.Toolbar.IsEnabled = true;
        }

        /// <summary>
        /// Загрузка данных в таблицу позиций в отгрузке
        /// </summary>
        private async void ShipmentPositionLoadItems()
        {
            ShipmentPositionGrid.Toolbar.IsEnabled = false;
            //ShipmentPositionGrid.ShowSplash();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Orders");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "ShipmentPosition");
            q.Request.SetParam("SHIPMENT_ID", ShipmentGrid.SelectedItem.CheckGet("ID"));

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    ShipmentPositionDS = ListDataSet.Create(result, "SHIPMENT_POSITIONS");
                    ShipmentPositionGrid.UpdateItems(ShipmentPositionDS);
                }
            }
            else if (q.Answer.Error.Code == 145)
            {
                q.ProcessError();
            }

            //ShipmentPositionGrid.HideSplash();
            ShipmentPositionGrid.Toolbar.IsEnabled = true;
        }

        /// <summary>
        /// Действия при выборе в таблице покупателей
        /// </summary>
        /// <param name="item"></param>
        private void UpdateBuyerAction(Dictionary<string, string> item)
        {
            //OrderGrid.Items.Clear();
            //OrderGrid.ClearItems();
            //OrderGrid.SelectedItem.Clear();
            OrderGrid.LoadItems();
        }

        /// <summary>
        /// Действия при выборе в таблице заявок
        /// </summary>
        /// <param name="item"></param>
        private void UpdateOrderAction(Dictionary<string, string> item)
        {
            //PositionGrid.Items.Clear();
            //PositionGrid.ClearItems();
            //PositionGrid.SelectedItem.Clear();
            PositionGrid.LoadItems();
        }

        private void UpdateShipmentAction(Dictionary<string, string> item)
        {
            //ShipmentOrderGrid.ClearItems();
            //ShipmentOrderGrid.SelectedItem.Clear();
            ShipmentOrderGrid.LoadItems();
            //ShipmentPositionGrid.ClearItems();
            //ShipmentPositionGrid.SelectedItem.Clear();
            ShipmentPositionGrid.LoadItems();
        }

        private void UpdateShipmentOrderAction(Dictionary<string, string> item)
        {
            SelectedShipmentOrderId = item.CheckGet("ID").ToInt();
            ShipmentPositionGrid.UpdateItems(ShipmentPositionDS);
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
                    q.Request.SetParam("Module", "Orders");
                    q.Request.SetParam("Object", "MoldedContainer");
                    q.Request.SetParam("Action", "DeleteOrder");
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
        /// Отмена заявки
        /// </summary>
        /// <param name="id"></param>
        private async void CancelOrder(int id)
        {
            var dw = new DialogWindow("Вы действительно хотите отменить заявку?", "Отмена заявки", "",
                DialogWindowButtons.NoYes);
            if ((bool)dw.ShowDialog())
            {
                if (dw.ResultButton == DialogResultButton.Yes)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Orders");
                    q.Request.SetParam("Object", "MoldedContainer");
                    q.Request.SetParam("Action", "SetStatus");
                    q.Request.SetParam("ORDER_ID", id.ToString());
                    q.Request.SetParam("STATUS", "2");

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
                    q.Request.SetParam("Module", "Orders");
                    q.Request.SetParam("Object", "MoldedContainer");
                    q.Request.SetParam("Action", "DeletePosition");
                    q.Request.SetParam("POSITION_ID", id.ToString());

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
                                ShipmentPositionGrid.LoadItems();
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
        /// Удаление позиции из заявки
        /// </summary>
        /// <param name="id"></param>
        private async void DeleteShipment(int id)
        {
            var dw = new DialogWindow("Вы действительно хотите удалить отгрузку?", "Удаление отгрузки", "",
                DialogWindowButtons.NoYes);
            if ((bool)dw.ShowDialog())
            {
                if (dw.ResultButton == DialogResultButton.Yes)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Orders");
                    q.Request.SetParam("Object", "MoldedContainer");
                    q.Request.SetParam("Action", "DeleteShipment");
                    q.Request.SetParam("SHIPMENT_ID", id.ToString());

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
                                ShipmentGrid.LoadItems();
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
        /// Добавление заявки в отгрузку
        /// </summary>
        private async void OrderToShipment()
        {
            bool resume = false;
            var orderDate = OrderGrid.SelectedItem.CheckGet("SHIPMENT_DATE").ToDateTime();
            var shipmentDate = ShipmentDate.Text.ToDateTime();
            if (DateTime.Compare(orderDate, shipmentDate) != 0)
            {
                var dw = new DialogWindow("Дата заявки и отгрузки отличаются. Добавить заявку в отгрузку?",
                    "Добавление заявки в отгрузку", "", DialogWindowButtons.NoYes);
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
                var d = new Dictionary<string, string>()
                {
                    { "ORDER_ID", OrderGrid.SelectedItem.CheckGet("ID") },
                    { "SHIPMENT_ID", ShipmentGrid.SelectedItem.CheckGet("ID") },
                    { "MODE", "1" },
                };
                SetOrderShipment(d);
            }
        }

        /// <summary>
        /// Удаление заявки из отгрузки
        /// </summary>
        private void RemoveFromShipment()
        {
            var dw = new DialogWindow("Вы действительно хотите удалить заявку из отгрузки?",
                "Удаление заявки из отгрузки", "", DialogWindowButtons.NoYes);
            if ((bool)dw.ShowDialog())
            {
                if (dw.ResultButton == DialogResultButton.Yes)
                {
                    var d = new Dictionary<string, string>()
                    {
                        { "ORDER_ID", OrderGrid.SelectedItem.CheckGet("ID") },
                        { "SHIPMENT_ID", ShipmentGrid.SelectedItem.CheckGet("ID") },
                        { "MODE", "0" },
                    };
                    SetOrderShipment(d);
                }
            }
        }

        private async void SetOrderShipment(Dictionary<string, string> v)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Orders");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "SetOrderShipment");
            q.Request.SetParams(v);

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
                        ShipmentGrid.LoadItems();
                    }
                }
            }
            else if (q.Answer.Error.Code == 145)
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Разрешение или запрещение отгрузки
        /// </summary>
        /// <param name="id"></param>
        /// <param name="allowed"></param>
        private async void AllowShipment(int id, int allowed = 0)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Orders");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "UpdateFinish");
            q.Request.SetParam("SHIPMENT_ID", id.ToString());
            q.Request.SetParam("ALLOWED", allowed.ToString());

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
                        ShipmentGrid.LoadItems();
                    }
                }
            }
            else if (q.Answer.Error.Code == 145)
            {
                q.ProcessError();
            }
        }

        private async void GetPrepaidReceipt(int id)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Orders");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "GetPrepaidReceipt");
            q.Request.SetParam("ORDER_ID", id.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                if (q.Answer.Type == LPackClientAnswer.AnswerTypeRef.File)
                {
                    Central.OpenFile(q.Answer.DownloadFilePath);

                    OrderGrid.LoadItems();
                    ShipmentGrid.LoadItems();
                }
            }
        }

        private async void DeletePrepaidReceipt(int id)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Orders");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "DeleteReceipt");
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
                        ShipmentGrid.LoadItems();
                    }
                }
            }
            else if (q.Answer.Error.Code == 145)
            {
                q.ProcessError();
            }
        }


        /// <summary>
        /// Отправить Order на согласование
        /// </summary>
        private async void OnApprovalOrder(int id)
        {
            var dw = new DialogWindow($"Вы действительно хотите отправить на согласование заявку {id}?",
                "Отправка на согласование", "", DialogWindowButtons.NoYes);

            if ((bool)dw.ShowDialog())
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Orders");
                q.Request.SetParam("Object", "MoldedContainer");
                q.Request.SetParam("Action", "UpdateOrderOnApproval");
                q.Request.SetParam("NSTHET", id.ToString());

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
                        OrderGrid.LoadItems();
                        var dw1 = new DialogWindow("Заявка отправлена на согласование", "Отправка на согласование", "");
                        dw1.ShowDialog();
                    }
                }
            };
        }
        
        
        /// <summary>
        /// Добавление доверенности
        /// </summary>
        /// <param name="id"></param>
        private void AddProxy(int id)
        {
            if (id != 0)
            {
                string storagePath = Central.GetStorageNetworkPathByCode(ProxyStorageCode);

                if (!string.IsNullOrEmpty(storagePath))
                {
                    if (System.IO.Directory.Exists(storagePath))
                    {
                        var fd = new Microsoft.Win32.OpenFileDialog();
                        fd.CheckFileExists = true;
                        fd.CheckPathExists = true;
                        fd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                        
                        if ((bool)fd.ShowDialog())
                        {
                            string fileFullPath = fd.FileName;

                            if (!string.IsNullOrEmpty(fileFullPath))
                            {
                                string fileExtension = System.IO.Path.GetExtension(fileFullPath);
                                string newFileName = $"{id}{fileExtension}";
                                string newFileFullPath = System.IO.Path.Combine(storagePath, newFileName);

                                bool resume = false;

                                try
                                {
                                    System.IO.File.Copy(fileFullPath, newFileFullPath, false);
                                    resume = true;
                                }
                                catch (Exception ex)
                                {
                                    string msg = $"При сохранении файла доверенности произошла ошибка. {ex.Message}. Пожалуйста, сообщите о проблеме.";
                                    var d = new DialogWindow($"{msg}", "Загрузка доверенности", "");
                                    d.ShowDialog();
                                }

                                if (resume)
                                {
                                    if (UpdateProxyData(id, newFileName))
                                    {
                                        OrderGrid.LoadItems();
                                        OrderGrid.SelectRowFirst();

                                        string msg = "Успешное добавление доверенности";
                                        var d = new DialogWindow($"{msg}", "Загрузка доверенности", "");
                                        d.ShowDialog();
                                    }
                                }
                            }
                            else
                            {
                                string msg = "Не выбран файл доверенности.";
                                var d = new DialogWindow($"{msg}", "Загрузка доверенности", "");
                                d.ShowDialog();
                            }
                        }
                    }
                    else
                    {
                        string msg = $"Папка {storagePath} для сохранения файла доверенности не найдена.";
                        var d = new DialogWindow($"{msg}", "Загрузка доверенности", "");
                        d.ShowDialog();
                    }
                    
                }
                else
                {
                    string msg = "Ошибка получения пути к папке для доверенностей. Пожалуйста, сообщите о проблеме.";
                    var d = new DialogWindow($"{msg}", "Загрузка доверенности", "");
                    d.ShowDialog();
                }
            }
            else
            {
                string msg = "Не выбрана заявка.";
                var d = new DialogWindow($"{msg}", "Загрузка доверенности", "");
                d.ShowDialog();
            }
        }

        private bool UpdateProxyData(int orderId, string proxyFileName)
        {
            bool resultFlag = false;

            var p = new Dictionary<string, string>();
            p.Add("ID", orderId.ToString());
            p.Add("PROXY_FILE_NAME", proxyFileName);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "ResponsibleStock");
            q.Request.SetParam("Action", "UpdateProxy");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestTimeoutDefault;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                bool succesfulFlag = false;

                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var dataSet = ListDataSet.Create(result, "ITEMS");
                    if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                    {
                        if (!string.IsNullOrEmpty(dataSet.Items.First().CheckGet("ID")))
                        {
                            succesfulFlag = true;
                        }
                    }
                }

                if (!succesfulFlag)
                {
                    string msg = "Ошибка обновления данных по доверенности в заявке. Пожалуйста, сообщите о проблеме.";
                    var d = new DialogWindow($"{msg}", "Загрузка доверенности", "");
                    d.ShowDialog();
                }
                else
                {
                    resultFlag = true;
                }
            }
            else
            {
                q.ProcessError();
            }

            return resultFlag;
        }
        
        /// <summary>
        /// Удаление доверенности
        /// </summary>
        public void DeleteProxy(int id, string proxyFileName)
        {
            
            if (id != 0)
            {
                var wd = new DialogWindow($"Удалить доверенность для заявки {id}?",
                    "Загрузка доверенности", "", DialogWindowButtons.NoYes);
                if (wd.ShowDialog() == true) 
                {
                    string storagePath = Central.GetStorageNetworkPathByCode(ProxyStorageCode);
                    
                    if (!string.IsNullOrEmpty(storagePath))
                    {
                        if (System.IO.Directory.Exists(storagePath))
                        {
                            string fileFullPath = System.IO.Path.Combine(storagePath, proxyFileName);
                            
                            if (System.IO.File.Exists(fileFullPath))
                            {
                                bool rusume = false;

                                try
                                {
                                    System.IO.File.Delete(fileFullPath);
                                    rusume = true;
                                }
                                catch (Exception ex)
                                {
                                    string msg = $"При удалении файла доверенности произошла ошибка. {ex.Message}. Пожалуйста, сообщите о проблеме.";
                                    var d = new DialogWindow($"{msg}", "Загрузка доверенности", "");
                                    d.ShowDialog();
                                }
                                
                                if (rusume)
                                {
                                    if (UpdateProxyData(id, ""))
                                    {
                                        OrderGrid.LoadItems();
                                        OrderGrid.SelectRowFirst();

                                        string msg = "Успешное удаление доверенности";
                                        var d = new DialogWindow($"{msg}", "Загрузка доверенности", "");
                                        d.ShowDialog();
                                    }
                                }
                            }
                            else
                            {
                                string msg = $"Файл доверенности {fileFullPath} не найден.";
                                var d = new DialogWindow($"{msg}", "Загрузка доверенности", "");
                                d.ShowDialog();
                            }
                        }
                        else
                        {
                            string msg = $"Папка {storagePath} для сохранения файла доверенности не найдена.";
                            var d = new DialogWindow($"{msg}", "Загрузка доверенности", "");
                            d.ShowDialog();
                        }
                    }
                    else
                    {
                        string msg = "Ошибка получения пути к папке для доверенностей. Пожалуйста, сообщите о проблеме.";
                        var d = new DialogWindow($"{msg}", "Загрузка доверенности", "");
                        d.ShowDialog();
                    }
                }
            }
            else
            {
                string msg = $"Не выбрана заявка.";
                var d = new DialogWindow($"{msg}", "Загрузка доверенности", "");
                d.ShowDialog();
            }
        }
        
        /// <summary>
        /// Открытие файла доверенности
        /// </summary>
        public void OpenProxy(int id, string proxyFileName)
        {
            if (id != 0)
            {
                string storagePath = Central.GetStorageNetworkPathByCode(ProxyStorageCode);
                
                if (!string.IsNullOrEmpty(storagePath))
                {
                    if (System.IO.Directory.Exists(storagePath))
                    {
                        string fileFullPath = System.IO.Path.Combine(storagePath, proxyFileName);
                        
                        if (System.IO.File.Exists(fileFullPath))
                        {
                            Central.OpenFile(fileFullPath);
                        }
                        else
                        {
                            string msg = $"Файл доверенности {fileFullPath} не найден.";
                            var d = new DialogWindow($"{msg}", "Загрузка доверенности", "");
                            d.ShowDialog();
                        }
                    }
                    else
                    {
                        string msg = $"Папка {storagePath} для сохранения файла доверенности не найдена.";
                        var d = new DialogWindow($"{msg}", "Загрузка доверенности", "");
                        d.ShowDialog();
                    }
                }
                else
                {
                    string msg = "Ошибка получения пути к папке для доверенностей. Пожалуйста, сообщите о проблеме.";
                    var d = new DialogWindow($"{msg}", "Загрузка доверенности", "");
                    d.ShowDialog();
                }
            }
            else
            {
                string msg = $"Не выбрана заявка.";
                var d = new DialogWindow($"{msg}", "Загрузка доверенности", "");
                d.ShowDialog();
            }
        }


        private void AllOrdersCheckBox_Click(object sender, RoutedEventArgs e)
        {
            OrderGrid.LoadItems();
        }

        private void ShipmentDate_TextChanged(object sender, TextChangedEventArgs e)
        {
            ShipmentGrid.LoadItems();
        }

        private void OwnerCheckBox_Click(object sender, RoutedEventArgs e)
        {
            ShipmentGrid.UpdateItems();
        }
    }
}