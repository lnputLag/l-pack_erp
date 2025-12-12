using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
using Client.Interfaces.Service;
using Client.Interfaces.Service.Printing;
using Client.Interfaces.Stock.ForkliftDrivers.Windows;
using DevExpress.Xpf.Grid.Printing;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static Client.Interfaces.Production.PaperProduction.DeviceConfig;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Управление отгрузками площадки Кашира
    /// </summary>
    public partial class ShipmentKshList : ControlBase
    {
        public ShipmentKshList()
        {
            ControlTitle = "Управление отгрузками";
            DocumentationUrl = "/doc/l-pack-erp/shipments/control/listing";
            RoleName = "[erp]shipment_control_ksh";
            InitializeComponent();

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

            //конструктор, будет вызван, когда объект создается
            //здесь создаются все внутренние структуры
            //впервые этот коллбэк будет вызван, когда данный таб станет активным
            //впервые (до этих пор, никакая работа внутри не происходит, что экономит ресурсы)
            OnLoad = () =>
            {
                FormInit();
                SetDefaults();
                ShipmentGridInit();
                PositionGridInit();
                DriverGridInit();
                TerminalGridInit();

                GetShipmentCountToPrint();
                RunGetShipmentCountToPrintTimer();

                //регистрация обработчика сообщений
                Messenger.Default.Register<ItemMessage>(this, _ProcessMessages);
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                ShipmentGrid.Destruct();
                PositionGrid.Destruct();
                DriverGrid.Destruct();
                TerminalGrid.Destruct();

                Messenger.Default.Unregister<ItemMessage>(this, _ProcessMessages);

                if (GetShipmentCountToPrintTimer != null)
                {
                    GetShipmentCountToPrintTimer.Stop();
                }
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                ShipmentGrid.ItemsAutoUpdate = true;
                ShipmentGrid.Run();

                DriverGrid.ItemsAutoUpdate = true;
                DriverGrid.Run();

                TerminalGrid.ItemsAutoUpdate = true;
                TerminalGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                ShipmentGrid.ItemsAutoUpdate = false;

                DriverGrid.ItemsAutoUpdate = false;

                TerminalGrid.ItemsAutoUpdate = false;
            };

            {
                Commander.Add(new CommandItem()
                {
                    Name = "refresh",
                    Group = "main",
                    Enabled = true,
                    Title = "Обновить",
                    Description = "Обновить данные",
                    ButtonUse = true,
                    ButtonControl = RefreshButton,
                    ButtonName = "RefreshButton",
                    Action = () =>
                    {
                        Refresh();
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
                    ButtonControl = HelpButton,
                    ButtonName = "HelpButton",
                    HotKey = "F1",
                    Action = () =>
                    {
                        Central.ShowHelp(DocumentationUrl);
                    },
                });

                Commander.Add(new CommandItem()
                {
                    Name = "add_driver",
                    Title = "Добавить",
                    Enabled = true,
                    ButtonUse = true,
                    ButtonControl = AddDriverButton,
                    ButtonName = "AddDriverButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        AddDriver();
                    },
                });
                //Commander.Add(new CommandItem()
                //{
                //    Name = "open_gate",
                //    Title = "Ожидаемый допуск",
                //    Enabled = true,
                //    ButtonUse = true,
                //    ButtonControl = OpenGateButton,
                //    ButtonName = "OpenGateButton",
                //    AccessLevel = Role.AccessMode.ReadOnly,
                //    Action = () =>
                //    {
                //        OpenGate();
                //    },
                //});
            }

            Commander.SetCurrentGridName("ShipmentGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "edit_shipment",
                    Title = "Изменить",
                    Group = "shipment_grid_edit",
                    Enabled = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        EditShipment();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ShipmentGrid != null && ShipmentGrid.Items != null && ShipmentGrid.Items.Count > 0)
                        {
                            if (ShipmentGrid.SelectedItem != null && ShipmentGrid.SelectedItem.Count > 0
                                && !string.IsNullOrEmpty(ShipmentGrid.SelectedItem.CheckGet("ID")))
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "set_comment",
                    Title = "Изменить примечание",
                    Group = "shipment_grid_edit",
                    Enabled = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        SetComment();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ShipmentGrid != null && ShipmentGrid.Items != null && ShipmentGrid.Items.Count > 0)
                        {
                            if (ShipmentGrid.SelectedItem != null && ShipmentGrid.SelectedItem.Count > 0
                                && !string.IsNullOrEmpty(ShipmentGrid.SelectedItem.CheckGet("ID")))
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "set_late_reason",
                    Title = "Причина опоздания",
                    Group = "shipment_grid_edit",
                    Enabled = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        SetLateReason();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ShipmentGrid != null && ShipmentGrid.Items != null && ShipmentGrid.Items.Count > 0)
                        {
                            if (ShipmentGrid.SelectedItem != null && ShipmentGrid.SelectedItem.Count > 0
                                && !string.IsNullOrEmpty(ShipmentGrid.SelectedItem.CheckGet("ID")))
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "edit_shipment_time",
                    Title = "Изменить время отгрузки",
                    Group = "shipment_grid_edit",
                    Enabled = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        EditShipmentTime();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ShipmentGrid != null && ShipmentGrid.Items != null && ShipmentGrid.Items.Count > 0)
                        {
                            if (ShipmentGrid.SelectedItem != null && ShipmentGrid.SelectedItem.Count > 0
                                && !string.IsNullOrEmpty(ShipmentGrid.SelectedItem.CheckGet("ID")))
                            {
                                if (!(ShipmentGrid.SelectedItem.CheckGet("C").ToInt() > 0 && ShipmentGrid.SelectedItem.CheckGet("ATTERMINAL").ToInt() == 0))
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
                    Name = "bind_driver",
                    Title = "Привязать водителя",
                    Group = "shipment_grid_driver",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = BindDriverButton,
                    ButtonName = "BindDriverButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        BindDriver();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ShipmentGrid != null && ShipmentGrid.Items != null && ShipmentGrid.Items.Count > 0)
                        {
                            if (ShipmentGrid.SelectedItem != null && ShipmentGrid.SelectedItem.Count > 0
                                && !string.IsNullOrEmpty(ShipmentGrid.SelectedItem.CheckGet("ID")))
                            {
                                if (string.IsNullOrEmpty(ShipmentGrid.SelectedItem.CheckGet("DLDTTMENTRY")))
                                {
                                    if (DriverGrid != null && DriverGrid.Items != null && DriverGrid.Items.Count > 0)
                                    {
                                        if (DriverGrid.SelectedItem != null && DriverGrid.SelectedItem.Count > 0
                                            && !string.IsNullOrEmpty(DriverGrid.SelectedItem.CheckGet("ID")))
                                        {
                                            //не въехал
                                            if (string.IsNullOrEmpty(DriverGrid.SelectedItem.CheckGet("ENTRYDATE")))
                                            {
                                                result = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "Unbind_driver",
                    Title = "Отвязать водителя",
                    Group = "shipment_grid_driver",
                    Enabled = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        UnbindDriver();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ShipmentGrid != null && ShipmentGrid.Items != null && ShipmentGrid.Items.Count > 0)
                        {
                            if (ShipmentGrid.SelectedItem != null && ShipmentGrid.SelectedItem.Count > 0
                                && !string.IsNullOrEmpty(ShipmentGrid.SelectedItem.CheckGet("ID")))
                            {
                                if (ShipmentGrid.SelectedItem.CheckGet("UNKNOWN_DRIVER_FLAG").ToInt() == 0)
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
                    Name = "show_shipment_info",
                    Title = "Информация об отгрузке",
                    Group = "shipment_grid_info",
                    Enabled = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        ShowShipmentInfo();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ShipmentGrid != null && ShipmentGrid.Items != null && ShipmentGrid.Items.Count > 0)
                        {
                            if (ShipmentGrid.SelectedItem != null && ShipmentGrid.SelectedItem.Count > 0
                                && !string.IsNullOrEmpty(ShipmentGrid.SelectedItem.CheckGet("ID")))
                            {
                                if (ShipmentGrid.SelectedItem.CheckGet("PRODUCTIONTYPE").ToInt() != 8)
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
                    Name = "show_history_extend",
                    Title = "История изменения отгрузки",
                    Group = "shipment_grid_info",
                    Enabled = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        ShowHistoryExtend();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ShipmentGrid != null && ShipmentGrid.Items != null && ShipmentGrid.Items.Count > 0)
                        {
                            if (ShipmentGrid.SelectedItem != null && ShipmentGrid.SelectedItem.Count > 0
                                && !string.IsNullOrEmpty(ShipmentGrid.SelectedItem.CheckGet("ID")))
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "loading_order",
                    Title = "Порядок загрузки",
                    Group = "shipment_grid_info",
                    Enabled = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        LoadingOrder();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ShipmentGrid != null && ShipmentGrid.Items != null && ShipmentGrid.Items.Count > 0)
                        {
                            if (ShipmentGrid.SelectedItem != null && ShipmentGrid.SelectedItem.Count > 0
                                && !string.IsNullOrEmpty(ShipmentGrid.SelectedItem.CheckGet("ID")))
                            {
                                if (ShipmentGrid.SelectedItem.CheckGet("PRODUCTIONTYPE").ToInt() != 8)
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
                    Name = "edit_loading_order",
                    Title = "Изменить порядок загрузки",
                    Group = "shipment_grid_info",
                    Enabled = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        EditLoadingOrder();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ShipmentGrid != null && ShipmentGrid.Items != null && ShipmentGrid.Items.Count > 0)
                        {
                            if (ShipmentGrid.SelectedItem != null && ShipmentGrid.SelectedItem.Count > 0
                                && !string.IsNullOrEmpty(ShipmentGrid.SelectedItem.CheckGet("ID")))
                            {
                                if (ShipmentGrid.SelectedItem.CheckGet("PRODUCTIONTYPE").ToInt() != 8
                                    && !(ShipmentGrid.SelectedItem.CheckGet("C").ToInt() > 0 && ShipmentGrid.SelectedItem.CheckGet("ATTERMINAL").ToInt() == 0))
                                {
                                    if (GetLoadingOrderTwoVisible())
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
                    Name = "print",
                    Title = "Печать",
                    Group = "shipment_grid_print",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = PrintButton,
                    ButtonName = "PrintButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        BurgerMenu.IsOpen = true;
                        SetPrintControlEnabled();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ShipmentGrid != null && ShipmentGrid.Items != null && ShipmentGrid.Items.Count > 0)
                        {
                            if (ShipmentGrid.SelectedItem != null && ShipmentGrid.SelectedItem.Count > 0
                                && !string.IsNullOrEmpty(ShipmentGrid.SelectedItem.CheckGet("ID")))
                            {
                                if (ShipmentGrid.SelectedItem.CheckGet("PRODUCTIONTYPE").ToInt() != 8)
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
                    Name = "shipment_document_list",
                    Title = "Список документов",
                    Group = "shipment_grid_print",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = ShipmentDocumentListButton,
                    ButtonName = "ShipmentDocumentListButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        ShipmentDocumentList();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ShipmentGrid != null && ShipmentGrid.Items != null && ShipmentGrid.Items.Count > 0)
                        {
                            if (ShipmentGrid.SelectedItem != null && ShipmentGrid.SelectedItem.Count > 0
                                && !string.IsNullOrEmpty(ShipmentGrid.SelectedItem.CheckGet("ID")))
                            {
                                if (ShipmentGrid.SelectedItem.CheckGet("PRODUCTIONTYPE").ToInt() != 8)
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
                    Name = "create_auto_shipment",
                    Title = "Автосоздание отгрузки",
                    Group = "shipment_grid_create",
                    Enabled = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        CreateAutoShipment();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ShipmentGrid != null && ShipmentGrid.Items != null && ShipmentGrid.Items.Count > 0)
                        {
                            if (ShipmentGrid.SelectedItem != null && ShipmentGrid.SelectedItem.Count > 0)
                            {
                                if (
                                    ShipmentGrid.SelectedItem["C"].ToInt() > 0
                                    && ShipmentGrid.SelectedItem["EXPORTACCEPTED"].ToInt() == 1
                                    && ShipmentGrid.SelectedItem["ATTERMINAL"].ToInt() == 0
                                )
                                {
                                    if (ShipmentGrid.SelectedItem["PRODUCTIONTYPE"].ToInt() != 8)
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
                    Name = "pallet_consumption",
                    Title = "Расход поддонов",
                    Group = "shipment_grid_create",
                    Enabled = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        PalletConsumption();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ShipmentGrid != null && ShipmentGrid.Items != null && ShipmentGrid.Items.Count > 0)
                        {
                            if (ShipmentGrid.SelectedItem != null && ShipmentGrid.SelectedItem.Count > 0)
                            {
                                if (
                                    ShipmentGrid.SelectedItem["C"].ToInt() > 0
                                    && ShipmentGrid.SelectedItem["RETURNABLE_PDN_POK"].ToInt() == 1
                                    && ShipmentGrid.SelectedItem["ATTERMINAL"].ToInt() == 0
                                )
                                {
                                    if (ShipmentGrid.SelectedItem["PRODUCTIONTYPE"].ToInt() != 8)
                                    {
                                        result = true;
                                    }
                                }
                            }
                        }

                        return result;
                    },
                });
            }

            Commander.SetCurrentGridName("DriverGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "edit_driver",
                    Title = "Изменить",
                    Group = "driver_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = EditDriverButton,
                    ButtonName = "EditDriverButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        EditDriver();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (DriverGrid != null && DriverGrid.Items != null && DriverGrid.Items.Count > 0)
                        {
                            if (DriverGrid.SelectedItem != null && DriverGrid.SelectedItem.Count > 0
                                && !string.IsNullOrEmpty(DriverGrid.SelectedItem.CheckGet("ID")))
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "delete_driver",
                    Title = "Удалить",
                    Group = "driver_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = DeleteDriverButton,
                    ButtonName = "DeleteDriverButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        DeleteDriver();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (DriverGrid != null && DriverGrid.Items != null && DriverGrid.Items.Count > 0)
                        {
                            if (DriverGrid.SelectedItem != null && DriverGrid.SelectedItem.Count > 0
                                && !string.IsNullOrEmpty(DriverGrid.SelectedItem.CheckGet("ID")))
                            {
                                //не въехал
                                if (string.IsNullOrEmpty(DriverGrid.SelectedItem.CheckGet("ENTRYDATE")))
                                {
                                    // Не на терминале
                                    if (string.IsNullOrEmpty(DriverGrid.SelectedItem.CheckGet("TERMINALNUMBER")))
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
                    Name = "mark_departure",
                    Title = "Отметить убытие",
                    Group = "driver_grid_default2",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = MarkDepartureButton,
                    ButtonName = "MarkDepartureButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        MarkDeparture();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (DriverGrid != null && DriverGrid.Items != null && DriverGrid.Items.Count > 0)
                        {
                            if (DriverGrid.SelectedItem != null && DriverGrid.SelectedItem.Count > 0
                                && !string.IsNullOrEmpty(DriverGrid.SelectedItem.CheckGet("ID")))
                            {
                                //въехал
                                //не привязан к терминалу
                                if (!string.IsNullOrEmpty(DriverGrid.SelectedItem.CheckGet("ENTRYDATE")) 
                                    && string.IsNullOrEmpty(DriverGrid.SelectedItem.CheckGet("TERMINALNUMBER")))
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
                    Name = "cancel_entry",
                    Title = "Отменить въезд",
                    Group = "driver_grid_default2",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = CancelEntryButton,
                    ButtonName = "CancelEntryButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        CancelDriverEntry();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (DriverGrid != null && DriverGrid.Items != null && DriverGrid.Items.Count > 0)
                        {
                            if (DriverGrid.SelectedItem != null && DriverGrid.SelectedItem.Count > 0
                                && !string.IsNullOrEmpty(DriverGrid.SelectedItem.CheckGet("ID")))
                            {
                                //въехал
                                //не привязан к терминалу
                                if (!string.IsNullOrEmpty(DriverGrid.SelectedItem.CheckGet("ENTRYDATE"))
                                    && string.IsNullOrEmpty(DriverGrid.SelectedItem.CheckGet("TERMINALNUMBER")))
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
                    Name = "show_shipment",
                    Title = "Показать отгрузку",
                    Group = "driver_grid_default2",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = ShowShipmentButton,
                    ButtonName = "ShowShipmentButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        ShowShipment();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (DriverGrid != null && DriverGrid.Items != null && DriverGrid.Items.Count > 0)
                        {
                            if (DriverGrid.SelectedItem != null && DriverGrid.SelectedItem.Count > 0
                                && !string.IsNullOrEmpty(DriverGrid.SelectedItem.CheckGet("ID")))
                            {
                                //привязан к отгрузке
                                if (!string.IsNullOrEmpty(DriverGrid.SelectedItem.CheckGet("TRANSPORTID")))
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
                    Name = "make_driver_report",
                    Title = "Печать",
                    Description = "Печать списка водителей",
                    Group = "driver_grid_default3",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = PrintButton2,
                    ButtonName = "PrintButton2",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        MakeDriverReport();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (DriverGrid != null && DriverGrid.Items != null && DriverGrid.Items.Count > 0)
                        {
                            result = true;
                        }

                        return result;
                    },
                });

            }

            Commander.SetCurrentGridName("TerminalGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "bind_terminal",
                    Title = "Привязать",
                    Group = "terminal_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = BindTerminalButton,
                    ButtonName = "BindTerminalButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        BindTerminal();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (TerminalGrid != null && TerminalGrid.Items != null && TerminalGrid.Items.Count > 0)
                        {
                            if (TerminalGrid.SelectedItem != null && TerminalGrid.SelectedItem.Count > 0
                                && !string.IsNullOrEmpty(TerminalGrid.SelectedItem.CheckGet("TERMINAL_ID")))
                            {
                                if (ShipmentGrid != null && ShipmentGrid.Items != null && ShipmentGrid.Items.Count > 0)
                                {
                                    if (ShipmentGrid.SelectedItem != null && ShipmentGrid.SelectedItem.Count > 0
                                         && !string.IsNullOrEmpty(ShipmentGrid.SelectedItem.CheckGet("ID")))
                                    {
                                        // отгрузка не запрещена
                                        if (ShipmentGrid.SelectedItem.CheckGet("FINISHED").ToInt() > 0)
                                        {
                                            // Пустой терминал
                                            if (string.IsNullOrEmpty(TerminalGrid.SelectedItem.CheckGet("BUYER_NAME")))
                                            {
                                                // терминал не заблокирован
                                                if (TerminalGrid.SelectedItem.CheckGet("BLOCKED_FLAG").ToInt() == 0)
                                                {
                                                    int countShipmentOnTerminal = TerminalGrid.Items.Count(x => x.CheckGet("TRANSPORT_ID").ToInt() == ShipmentGrid.SelectedItem.CheckGet("ID").ToInt());
                                                    if (countShipmentOnTerminal < 2)
                                                    {
                                                        // отгрузка привязана к терминалу
                                                        if (countShipmentOnTerminal == 1)
                                                        {
                                                            // у машины для отгрузки есть прицеп
                                                            if (ShipmentGrid.SelectedItem.CheckGet("TRAILER_FLAG").ToInt() > 0)
                                                            {
                                                                result = true;
                                                            }
                                                        }
                                                        // отгрузка ещё не привязана к терминалу
                                                        else
                                                        {
                                                            result = true;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "unbind_terminal",
                    Title = "Отвязать",
                    Group = "terminal_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = UnbindTerminalButton,
                    ButtonName = "UnbindTerminalButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        UnbindTerminal();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (TerminalGrid != null && TerminalGrid.Items != null && TerminalGrid.Items.Count > 0)
                        {
                            if (TerminalGrid.SelectedItem != null && TerminalGrid.SelectedItem.Count > 0
                                && !string.IsNullOrEmpty(TerminalGrid.SelectedItem.CheckGet("TERMINAL_ID")))
                            {
                                // Не пустой терминал
                                if (!string.IsNullOrEmpty(TerminalGrid.SelectedItem.CheckGet("BUYER_NAME")))
                                {
                                    // терминал не заблокирован
                                    if (TerminalGrid.SelectedItem.CheckGet("BLOCKED_FLAG").ToInt() == 0)
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
                    Name = "set_show_all_pallet_flag",
                    Title = "Показать погрузчику все поддоны",
                    Group = "terminal_grid_forklift_driver",
                    Enabled = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.Special,
                    Action = () =>
                    {
                        SetShowPalletFlag(1);
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (TerminalGrid != null && TerminalGrid.Items != null && TerminalGrid.Items.Count > 0)
                        {
                            if (TerminalGrid.SelectedItem != null && TerminalGrid.SelectedItem.Count > 0
                                && !string.IsNullOrEmpty(TerminalGrid.SelectedItem.CheckGet("TERMINAL_ID")))
                            {
                                if (TerminalGrid.SelectedItem.CheckGet("TRANSPORT_ID").ToInt() > 0)
                                {
                                    if (TerminalGrid.SelectedItem.CheckGet("KIND").ToInt() != 8) 
                                    {
                                        if (TerminalGrid.SelectedItem.CheckGet("SHOW_ALL_PALLET_FLAG").ToInt() != 1)
                                        {
                                            result = true;
                                        }
                                    }
                                }
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "set_show_incomplete_pallet_flag",
                    Title = "Показать погрузчику все неполные поддоны",
                    Group = "terminal_grid_forklift_driver",
                    Enabled = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.Special,
                    Action = () =>
                    {
                        SetShowPalletFlag(2);
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (TerminalGrid != null && TerminalGrid.Items != null && TerminalGrid.Items.Count > 0)
                        {
                            if (TerminalGrid.SelectedItem != null && TerminalGrid.SelectedItem.Count > 0
                                && !string.IsNullOrEmpty(TerminalGrid.SelectedItem.CheckGet("TERMINAL_ID")))
                            {
                                if (TerminalGrid.SelectedItem.CheckGet("TRANSPORT_ID").ToInt() > 0)
                                {
                                    if (TerminalGrid.SelectedItem.CheckGet("KIND").ToInt() != 8) 
                                    {
                                        if (TerminalGrid.SelectedItem.CheckGet("SHOW_ALL_PALLET_FLAG").ToInt() != 2)
                                        {
                                            result = true;
                                        }
                                    }
                                }
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "remove_shipment_block_flag",
                    Title = "Снять блокировку отгрузки из-за несоответствия габаритов ТС",
                    Group = "terminal_grid_shipment",
                    Enabled = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.Special,
                    Action = () =>
                    {
                        RemoveShipmentBlockFlag();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (TerminalGrid != null && TerminalGrid.Items != null && TerminalGrid.Items.Count > 0)
                        {
                            if (TerminalGrid.SelectedItem != null && TerminalGrid.SelectedItem.Count > 0
                                && !string.IsNullOrEmpty(TerminalGrid.SelectedItem.CheckGet("TERMINAL_ID")))
                            {
                                if (TerminalGrid.SelectedItem.CheckGet("TRANSPORT_ID").ToInt() > 0)
                                {
                                    if (TerminalGrid.SelectedItem.CheckGet("SHPMENT_BLOCKED_FLAG").ToInt() > 0)
                                    {
                                        result = true;
                                    }
                                }
                            }
                        }

                        return result;
                    },
                });
            }

            Commander.Init(this);
        }

        private ListDataSet ShipmentDataSet { get; set; }

        private ListDataSet PositionDataSet { get; set; }

        private ListDataSet DriverDataSet { get; set; }

        private ListDataSet TerminalDataSet { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Идентификатор площадки
        /// </summary>
        private int FactoryId = 2;

        /// <summary>
        /// Количество завершённых отгрузок, для которых нужно распечатать документы
        /// </summary>
        private int ShipmentToPrintCount { get; set; }

        /// <summary>
        /// Стандартный текст для информационного поля с количеством отгрузок, для которых нужно распечатать документы
        /// </summary>

        private static string ShipmentCountToPrintText = "Требуется печать: ";

        /// <summary>
        /// Таймер для вызова функции получения количества отгрузок в текущих производственных сутках, для которых нужно распечатать документы
        /// </summary>
        public DispatcherTimer GetShipmentCountToPrintTimer { get; set; }

        /// <summary>
        /// Интервал работы таймера для вызова функции получения количества отгрузок в текущих производственных сутках, для которых нужно распечатать документы
        /// </summary>
        public int GetShipmentCountToPrintTimerInterval { get; set; }

        //public Dictionary<string, string> ArrivedDriverData { get; set; }

        /// <summary>
        /// Ссылка на таб плана отгрузок
        /// </summary>
        private ShipmentKshPlan ShipmentPlan { get; set; }

        public void FormInit()
        {
            //инициализация формы
            {
                Form = new FormHelper();

                //колонки формы
                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path = "FROM_DATE_TIME",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = FromDate,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "TO_DATE_TIME",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = ToDate,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                };

                Form.SetFields(fields);
            }
        }

        public void SetDefaults()
        {
            ShipmentDataSet = new ListDataSet();
            PositionDataSet = new ListDataSet();
            DriverDataSet = new ListDataSet();
            TerminalDataSet = new ListDataSet();

            Form.SetDefaults();

            GetShipmentCountToPrintTimerInterval = 10;

            var date = DateTime.Now;
            if (date.Hour >= 20 || date.Hour < 8)
            {
                if (date.Hour >= 20)
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{date.ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{date.AddDays(1).ToString("dd.MM.yyyy")} 08:00:00");
                }
                else
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{date.AddDays(-1).ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{date.ToString("dd.MM.yyyy")} 08:00:00");
                }
            }
            else
            {
                Form.SetValueByPath("FROM_DATE_TIME", $"{date.ToString("dd.MM.yyyy")} 08:00:00");
                Form.SetValueByPath("TO_DATE_TIME", $"{date.ToString("dd.MM.yyyy")} 20:00:00");
            }

            {
                var list = new Dictionary<string, string>();
                list.Add("-1", "Все виды доставки");
                list.Add("0", "Без самовывоза");
                list.Add("1", "Самовывоз");
                list.Add("2", "Самовывоз без доверенности");
                DeliveryTypes.Items = list;
                DeliveryTypes.SetSelectedItemFirst();
            }

            {
                var list = new Dictionary<string, string>();
                list.Add("-1", "Все отгрузки");
                list.Add("1", "Неразрешенные");
                list.Add("2", "Разрешенные");
                list.Add("3", "Неотгруженные");
                list.Add("4", "Транспорт");
                list.Add("5", "На терминале");
                list.Add("6", "Отгруженные");
                list.Add("7", "На печать");
                list.Add("8", "Возврат поддонов");
                list.Add("9", "Расход возвратных поддонов");
                list.Add("10", "Автоотгрузка");
                list.Add("11", "Поставка ТМЦ");
                ShipmentTypes.Items = list;
                ShipmentTypes.SetSelectedItemFirst();
            }

            {
                var list = new Dictionary<string, string>();
                list.Add("-1", "Все типы");
                list.Add("0", "Изделия");
                list.Add("2", "Рулоны");
                list.Add("8", "ТМЦ");
                list.Add("9", "Макулатура");
                Types.Items = list;
                Types.SetSelectedItemFirst();
            }

            // Активность пункта НАстройки склада в выпадающем меню Шестерёнки
            {
                var mode = Central.Navigator.GetRoleLevel(this.RoleName);
                if (mode >= Role.AccessMode.Special)
                {
                    BurgerStockSettings.IsEnabled = true;
                }
                else
                {
                    BurgerStockSettings.IsEnabled = false;
                }
            }
        }

        public void Refresh()
        {
            ShipmentGrid.LoadItems();
            DriverGrid.LoadItems();
            TerminalGrid.LoadItems();
        }

        public void ShipmentGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn()
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=4,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="ИД Отгрузки",
                        Path="ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Дата отгрузки",
                        Path="SHIPMENTDATE",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM",
                        Width2=5,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Дт/вр отгрузки",
                        Path="SHIPMENTDATETIME",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM HH:mm",
                        MinWidth=70,
                        Width2=9,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    //перенесена на другой день
                                    if (row.ContainsKey("UNSHIPPED"))
                                    {
                                        if( row["UNSHIPPED"].ToInt() == 1 )
                                        {
                                            color = HColor.Orange;
                                        }
                                    }

                                    //опоздавшая
                                    if (row.ContainsKey("LATE"))
                                    {
                                        if( row["LATE"].ToInt() == 1 )
                                        {
                                            color = HColor.Yellow;
                                        }
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Транспорт",
                        Path="TRANSPORT",
                        Width2=4,
                        ColumnType=ColumnTypeRef.Boolean,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Дт/вр приезда водителя",
                        Path="DRIVERARRIVEDATETIME",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm",
                        Width2=12,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Дт/вр выезда водителя",
                        Path="DRIVERDEPARTDATETIME",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm",
                        Width2=12,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Готова к отгрузке",
                        Path="READY",
                        Width2=2,
                        ColumnType=ColumnTypeRef.Boolean,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Статус",
                        Path="STATUS",
                        ColumnType=ColumnTypeRef.String,
                        Width2=9,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.ForegroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    if( row.CheckGet("STATUSID").ToInt() == 10 )
                                    {
                                        color = HColor.BlueFG;
                                    }
                                    if( row.CheckGet("STATUSID").ToInt() == 5 )
                                    {
                                        color = HColor.BlackFG;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn()
                    {
                        Header="% готовности",
                        Path="PROGRESS",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    //готова
                                    if( row.CheckGet("READY").ToInt() == 1 )
                                    {
                                        color = HColor.VioletPink;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                            {
                                StylerTypeRef.ForegroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    if( row.CheckGet("STATUSID").ToInt() == 10 )
                                    {
                                        color = HColor.BlueFG;
                                    }
                                    if( row.CheckGet("STATUSID").ToInt() == 5 )
                                    {
                                        color = HColor.BlackFG;
                                    }


                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Самовывоз",
                        Path="EXPORTACCEPTED",
                        Width2=4,
                        ColumnType=ColumnTypeRef.Boolean,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Упаковка",
                        Path="PACKAGINGTYPETEXT",
                        ColumnType=ColumnTypeRef.String,
                        Width2=4,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Перевозчик",
                        Path="CARRIER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=20,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Грузополучатель",
                        Path="СONSIGNEE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=18,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Водитель",
                        Path="DRIVER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=16,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Автомобиль",
                        Path="CAR",
                        ColumnType=ColumnTypeRef.String,
                        Width2=16,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Телефон",
                        Path="PHONE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=11,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Доверенность",
                        Path="ATTORNEYLETTER",
                        Width2=2,
                        ColumnType=ColumnTypeRef.Boolean,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Паспорт",
                        Path="PASSPORT",
                        ColumnType=ColumnTypeRef.String,
                        Width2=16,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Покупатель",
                        Path="BUYER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=16,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // тип отгрузки - поставка ТМЦ
                                    if( row.CheckGet("PRODUCTIONTYPE").ToInt() == 8 )
                                    {
                                        color = HColor.VioletPink;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Адрес доставки",
                        Path="SHIPMENTADDRESS",
                        ColumnType=ColumnTypeRef.String,
                        Width2=31,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Количество позиций",
                        Path="QUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=4,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Количество заявок",
                        Path="ORDERSCOUNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=4,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Заявки",
                        Path="ORDERSLIST",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Загруженность",
                        Path="LOADED",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Площадь",
                        Path="SQUARE",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Масса",
                        Path="WEIGHT",
                        ColumnType=ColumnTypeRef.Double,
                        Format = "N1",
                        Width2=5,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Образец",
                        Path="SAMPLE",
                        Width2=2,
                        ColumnType=ColumnTypeRef.Boolean,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Клише",
                        Path="CLICHE",
                        Width2=2,
                        ColumnType=ColumnTypeRef.Boolean,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Штанцформа",
                        Path="SHTANTSFORM",
                        Width2=2,
                        ColumnType=ColumnTypeRef.Boolean,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Тендер",
                        Path="TENDERDOC",
                        Width2=2,
                        ColumnType=ColumnTypeRef.Boolean,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Карта проезда",
                        Path="ADDRESSCNT",
                        Width2=2,
                        ColumnType=ColumnTypeRef.Boolean,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Запрещена",
                        Path="FORBIDDEN",
                        Width2=2,
                        ColumnType=ColumnTypeRef.Boolean,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Возврат поддонов",
                        Path="PALLETRETURN",
                        Width2=2,
                        ColumnType=ColumnTypeRef.Boolean,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Расход возвратных поддонов",
                        Path="RETURNABLEPALLETCHECK",
                        Width2=2,
                        ColumnType=ColumnTypeRef.Boolean,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Дт. фактического окончания производства",
                        Path="PRODUCTIONFINISHACTUALLY",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm",
                        Width2=12,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Дт/вр окончания производства",
                        Path="PRODUCTIONFINISH",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm",
                        Width2=12,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Примечания",
                        Path="COMMENTS",
                        ColumnType=ColumnTypeRef.String,
                        Width2=16,
                    },

                    new DataGridHelperColumn()
                    {
                        Header="APPLICATIONTRANSPORTID",
                        Path="APPLICATIONTRANSPORTID",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="DOCUMENT_PRINT_FLAG",
                        Path="DOCUMENT_PRINT_FLAG",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="FINISHED",
                        Path="FINISHED",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="PRODUCTIONTYPE",
                        Path="PRODUCTIONTYPE",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="CARRYACCEPTED",
                        Path="CARRYACCEPTED",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="ORDERNUMBER",
                        Path="ORDERNUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="SHIPMENTTYPE",
                        Path="SHIPMENTTYPE",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="PRODUCTION_TYPE_ID",
                        Path="PRODUCTION_TYPE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="LOADINGSCHEMESTATUS",
                        Path="LOADINGSCHEMESTATUS",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="LOADINGSCHEMEFILE",
                        Path="LOADINGSCHEMEFILE",
                        ColumnType=ColumnTypeRef.String,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="TRAILER_FLAG",
                        Path="TRAILER_FLAG",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="DRIVERID",
                        Path="DRIVERID",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="CARNUMBER",
                        Path="CARNUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="CARMARK",
                        Path="CARMARK",
                        ColumnType=ColumnTypeRef.String,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="DLDTTMENTRY",
                        Path="DLDTTMENTRY",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM HH:mm",
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="DLIDTS",
                        Path="DLIDTS",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="ATTERMINAL",
                        Path="ATTERMINAL",
                        ColumnType=ColumnTypeRef.Boolean,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="PRICEIS",
                        Path="PRICEIS",
                        ColumnType=ColumnTypeRef.Boolean,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="STATUSID",
                        Path="STATUSID",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="C",
                        Path="C",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="RETURNABLE_PDN_POK",
                        Path="RETURNABLE_PDN_POK",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="SHIPMENTDATETIMEFULL",
                        Path="SHIPMENTDATETIMEFULL",
                        ColumnType=ColumnTypeRef.String,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="UNSHIPPED",
                        Path="UNSHIPPED",
                        ColumnType=ColumnTypeRef.Boolean,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="LATE",
                        Path="LATE",
                        ColumnType=ColumnTypeRef.Boolean,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="STOCKPERCENT",
                        Path="STOCKPERCENT",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="ORDER_ID",
                        Path="ORDER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="UNKNOWN_DRIVER_FLAG",
                        Path="UNKNOWN_DRIVER_FLAG",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                };
                ShipmentGrid.SetColumns(columns);
                ShipmentGrid.SetPrimaryKey("ID");
                ShipmentGrid.SearchText = ShipmentSearchBox;
                //данные грида
                ShipmentGrid.OnLoadItems = ShipmentGridLoadItems;
                ShipmentGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                ShipmentGrid.AutoUpdateInterval = 60 * 5;
                ShipmentGrid.Toolbar = ShipmentGridToolbar;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                ShipmentGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem != null)
                    {
                        if (ShipmentGrid != null && ShipmentGrid.Items != null && ShipmentGrid.Items.Count > 0)
                        {
                            if (ShipmentGrid.Items.FirstOrDefault(x => x.CheckGet("ID").ToInt() == selectedItem.CheckGet("ID").ToInt()) == null)
                            {
                                ShipmentGrid.SelectRowFirst();
                            }
                        }

                        {
                            if (PositionGrid != null && PositionGrid.Initialized)
                            {
                                PositionGridLoadItems();

                                //подстроим набор колонок в зависимости от типа продукции выбранной отгрузки
                                //ProductionType 2=бумага, *=изделия
                                List<string> hiddenColumns = new List<string>();
                                List<string> visibleColumns = new List<string>();
                                switch (ShipmentGrid.SelectedItem.CheckGet("PRODUCTIONTYPE").ToInt())
                                {
                                    //бумага (рулоны)
                                    case 2:
                                        hiddenColumns.Add("VENDORCODE");
                                        hiddenColumns.Add("QUANTITYLIMIT");
                                        hiddenColumns.Add("PRODUCTIONSCHEME");
                                        hiddenColumns.Add("PACKAGING");
                                        hiddenColumns.Add("PALLETQUANTITY");

                                        visibleColumns.Add("ROLLDIAMETER");
                                        visibleColumns.Add("ROLLQUANTITY");
                                        break;

                                    //изделия
                                    default:
                                        hiddenColumns.Add("ROLLDIAMETER");
                                        hiddenColumns.Add("ROLLQUANTITY");

                                        visibleColumns.Add("VENDORCODE");
                                        visibleColumns.Add("QUANTITYLIMIT");
                                        visibleColumns.Add("PRODUCTIONSCHEME");
                                        visibleColumns.Add("PACKAGING");
                                        visibleColumns.Add("PALLETQUANTITY");
                                        break;
                                }

                                foreach (var column in hiddenColumns)
                                {
                                    PositionGrid.SetGridColumnVisible(column, false);
                                }

                                foreach (var column in visibleColumns)
                                {
                                    PositionGrid.SetGridColumnVisible(column, true);
                                }

                                PositionGrid.GridColumnIsVisible(0);
                            }
                        }
                    }
                };

                ShipmentGrid.OnFilterItems = ShipmentGridFilterItems;

                ShipmentGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    // определение цветов фона строк
                    {
                        StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            //отгрузка запрещена: голубой
                            if(row.CheckGet("FINISHED").ToInt()==0)
                            {
                                color = HColor.Blue;
                            }
                            
                            //водитель приехал, отгрузка готова: розовый
                            if(
                                row.CheckGet("READY").ToInt() == 1
                                && row.CheckGet("TRANSPORT").ToBool() == true
                            )
                            {
                                color = HColor.VioletPink;
                            }

                            //создана накладная: зеленый
                            if(row.CheckGet("APPLICATIONTRANSPORTID").ToInt()!=0)
                            {
                                color = HColor.Green;
                            }

                            //необходимо провести расход возвратных поддонов
                            if(row.CheckGet("RETURNABLEPALLETCHECK").ToInt()==1)
                            {
                                color = HColor.Pink;
                            }

                            // зелёный -- если тип отгрузки - поставка ТМЦ, и отгрузка была привязана к терминалу (заполнено поле driver_log.dttm_entry)
                            if (row.CheckGet("PRODUCTIONTYPE").ToInt() == 8 && !string.IsNullOrEmpty(row.CheckGet("DLDTTMENTRY")))
                            {
                                color = HColor.Green;
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },

                    // определение цветов шрифта строк
                    {
                        StylerTypeRef.ForegroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            //отгрузка запрещена: голубой
                            if(row.CheckGet("FINISHED").ToInt() == 0)
                            {
                                //не отгружено, установлена цена: зеленый
                                if(row.ContainsKey("FINISHED") && row.ContainsKey("PRICEIS"))
                                {
                                    if(row["FINISHED"].ToInt() == 0)
                                    {
                                        if(row["PRICEIS"].ToInt() == 1)
                                        {
                                            color=HColor.GreenFG;
                                        }
                                    }
                                }
                            }

                            //создана накладная: зеленый
                            if(row.CheckGet("APPLICATIONTRANSPORTID").ToInt()!=0)
                            {
                                //на терминале: синий
                                if (row.ContainsKey("ATTERMINAL"))
                                {
                                    if(row["ATTERMINAL"].ToInt() == 1)
                                    {
                                        color = HColor.BlueFG;
                                    }
                                }
                            }

                            // Отгрузка совершена, но первичные документы ещё не распечатаны
                            if (row.CheckGet("C").ToInt() > 0 && row.CheckGet("ATTERMINAL").ToInt() == 0 && row.CheckGet("DOCUMENT_PRINT_FLAG").ToInt() == 0)
                            {
                                color = HColor.MagentaFG;
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },
                };

                ShipmentGrid.Commands = Commander;

                ShipmentGrid.Init();
            }
        }

        public void ShipmentGridFilterItems()
        {
            {
                if (ShipmentGrid.Items != null && ShipmentGrid.Items.Count > 0)
                {
                    // Фильтрация по типу доставки
                    if (DeliveryTypes.SelectedItem.Key != null)
                    {
                        var key = DeliveryTypes.SelectedItem.Key.ToInt();
                        var items = new List<Dictionary<string, string>>();

                        switch (key)
                        {
                            // доставка
                            case 0:
                                items.AddRange(ShipmentGrid.Items.Where(x => x.CheckGet("EXPORTACCEPTED").ToInt() == 0));
                                break;

                            // самовывоз
                            case 1:
                                items.AddRange(ShipmentGrid.Items.Where(x => x.CheckGet("EXPORTACCEPTED").ToInt() == 1));
                                break;

                            // самовывоз без доверенности
                            case 2:
                                items.AddRange(ShipmentGrid.Items.Where(x => x.CheckGet("EXPORTACCEPTED").ToInt() == 1
                                    && x.CheckGet("ATTORNEYLETTER").ToInt() == 0));
                                break;

                            // все
                            case -1:
                            default:
                                items = ShipmentGrid.Items;
                                break;
                        }

                        ShipmentGrid.Items = items;
                    }

                    // Фильтрация по статусу отгрузки
                    if (ShipmentTypes.SelectedItem.Key != null)
                    {
                        var key = ShipmentTypes.SelectedItem.Key.ToInt();
                        var items = new List<Dictionary<string, string>>();

                        switch (key)
                        {
                            // Неразрешенные
                            case 1:
                                items.AddRange(ShipmentGrid.Items.Where(x => x.CheckGet("FINISHED").ToInt() == 0
                                    && x.CheckGet("C").ToInt() == 0));
                                break;

                            // Разрешенные (не голубой)
                            case 2:
                                items.AddRange(ShipmentGrid.Items.Where(x => x.CheckGet("FINISHED").ToInt() == 1
                                    && x.CheckGet("C").ToInt() == 0));
                                break;

                            // Неотгруженные (незеленый + оранжевый string.IsNullOrEmpty(x["DlDttmEntry"]) + на терминале)
                            case 3:
                                items.AddRange(ShipmentGrid.Items.Where(x => x.CheckGet("C").ToInt() == 0
                                    || (x.CheckGet("C").ToInt() > 0 && x.CheckGet("ATTERMINAL").ToInt() == 1)));
                                break;

                            // транспорт
                            case 4:
                                items.AddRange(ShipmentGrid.Items.Where(x => x.CheckGet("TRANSPORT").ToInt() == 1));
                                break;

                            // На терминале
                            case 5:
                                items.AddRange(ShipmentGrid.Items.Where(x => x.CheckGet("ATTERMINAL").ToInt() == 1));
                                break;

                            // Отгруженные
                            case 6:
                                items.AddRange(ShipmentGrid.Items.Where(x => x.CheckGet("ATTERMINAL").ToInt() == 0
                                    && x.CheckGet("C").ToInt() > 0));
                                break;

                            // На печать
                            case 7:
                                items.AddRange(ShipmentGrid.Items.Where(x => x.CheckGet("ATTERMINAL").ToInt() == 0
                                    && x.CheckGet("C").ToInt() > 0 && x.CheckGet("DOCUMENT_PRINT_FLAG").ToInt() == 0));
                                break;

                            // возврат поддонов
                            case 8:
                                items.AddRange(ShipmentGrid.Items.Where(x => x.CheckGet("PALLETRETURN").ToInt() == 1));
                                break;

                            // расход возвратных поддонов
                            case 9:
                                items.AddRange(ShipmentGrid.Items.Where(x => x.CheckGet("RETURNABLEPALLETCHECK").ToInt() == 1));
                                break;

                            // автоотгрузка
                            case 10:
                                items.AddRange(ShipmentGrid.Items.Where(x => x.CheckGet("EXPORTACCEPTED").ToInt() == 1
                                    && x.CheckGet("C").ToInt() > 0 && x.CheckGet("ATTERMINAL").ToInt() == 0));
                                break;

                            // поставка ТМЦ
                            case 11:
                                items.AddRange(ShipmentGrid.Items.Where(x => x.CheckGet("PRODUCTIONTYPE").ToInt() == 8));
                                break;

                            // все
                            case -1:
                            default:
                                items = ShipmentGrid.Items;
                                break;
                        }

                        ShipmentGrid.Items = items;
                    }

                    // Фильтрация по типу продукции
                    if (Types.SelectedItem.Key != null)
                    {
                        var key = Types.SelectedItem.Key.ToInt();
                        var items = new List<Dictionary<string, string>>();

                        /*
                            list.Add("-1", "Все типы");
                            list.Add("0", "Изделия");
                            list.Add("2", "Рулоны");
                            list.Add("8", "ТМЦ");
                            list.Add("9", "Макулатура");
                         */

                        switch (key)
                        {
                            // Изделия
                            case 0:
                                items.AddRange(ShipmentGrid.Items.Where(x => x.CheckGet("PRODUCTIONTYPE").ToInt() != 2));
                                break;

                            // все
                            case -1:
                                items = ShipmentGrid.Items;
                                break;
                            
                            default:
                                items.AddRange(ShipmentGrid.Items.Where(x => x.CheckGet("PRODUCTIONTYPE").ToInt() == key));
                                break;
                        }

                        ShipmentGrid.Items = items;
                    }

                    // скрыть отгруженные
                    {
                        var items = new List<Dictionary<string, string>>();

                        if (HideCompleteCheckbox.IsChecked == true)
                        {
                            items.AddRange(ShipmentGrid.Items.Where(x => x.CheckGet("C").ToInt() == 0
                                || (x.CheckGet("C").ToInt() > 0 && x.CheckGet("ATTERMINAL").ToInt() == 1)));
                        }
                        else
                        {
                            items = ShipmentGrid.Items;
                        }

                        ShipmentGrid.Items = items;
                    }
                }
            }

            PositionGrid.ClearItems();

            if (ShipmentGrid != null && ShipmentGrid.SelectedItem != null && ShipmentGrid.SelectedItem.Count > 0)
            {
                ShipmentGrid.Commands.Message = new ItemMessage() { Action = "refresh", Message = $"{ShipmentGrid.SelectedItem.CheckGet("SHIPMENT_ID")}" };
            }
        }

        public async void ShipmentGridLoadItems()
        {
            if (Form.Validate())
            {
                string fromDateTime = $"{Form.GetValueByPath("FROM_DATE_TIME")}";
                string toDateTime = $"{Form.GetValueByPath("TO_DATE_TIME")}";

                var dtFrom = fromDateTime.ToDateTime("dd.MM.yyyy HH:mm:ss");
                var dtTo = toDateTime.ToDateTime("dd.MM.yyyy HH:mm:ss");
                if (DateTime.Compare(dtFrom, dtTo) > 0)
                {
                    const string msg = "Дата начала должна быть меньше даты окончания.";
                    var d = new DialogWindow($"{msg}", "Проверка данных");
                    d.ShowDialog();
                }
                else
                {
                    var p = new Dictionary<string, string>();
                    p.Add("FROM_DATE", fromDateTime);
                    p.Add("TO_DATE", toDateTime);
                    p.Add("FACTORY_ID", $"{FactoryId}");

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Shipments/ShipmentKsh");
                    q.Request.SetParam("Object", "Shipment");
                    q.Request.SetParam("Action", "List");
                    q.Request.SetParams(p);

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    ShipmentDataSet = new ListDataSet();
                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            ShipmentDataSet = ListDataSet.Create(result, "ITEMS");
                        }
                    }
                    ShipmentGrid.UpdateItems(ShipmentDataSet);

                    LoadShipmentsTotals();
                }
            }
        }

        /// <summary>
        /// итоги под гридом Отгрузки (верхний грид)
        /// </summary>
        public async void LoadShipmentsTotals()
        {
            bool complete = false;
            ListDataSet totalsDS = new ListDataSet();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Shipment");
            q.Request.SetParam("Action", "GetTotals");

            q.Request.SetParam("FromDate", FromDate.Text.ToDateTime("dd.MM.yyyy HH:mm:ss").ToString("dd.MM.yyyy"));
            q.Request.SetParam("ToDate", ToDate.Text.ToDateTime("dd.MM.yyyy HH:mm:ss").ToString("dd.MM.yyyy"));
            q.Request.SetParam("FACTORY_ID", $"{FactoryId}");

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
                    totalsDS = ListDataSet.Create(result, "TOTALS");
                    if (totalsDS.Items.Count > 0)
                    {
                        complete = true;
                    }
                }
            }

            if (complete)
            {
                InitShipmentsTotals(totalsDS);
            }
            else
            {
                InitShipmentsTotals();
            }
        }

        /// <summary>
        /// Инициализация строки итоговых значений
        /// </summary>
        public void InitShipmentsTotals()
        {
            TotalsSquareValue.Text = "";
            TotalsWeightValue.Text = "";

            TotalsSquareValue.Visibility = Visibility.Collapsed;
            TotalsSquareUnit.Visibility = Visibility.Collapsed;
            TotalsWeightValue.Visibility = Visibility.Collapsed;
            TotalsWeightUnit.Visibility = Visibility.Collapsed;
            TotalsTitle.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Заполнение строки итоговых значений
        /// </summary>
        /// <param name="ds"></param>
        public void InitShipmentsTotals(ListDataSet ds)
        {
            if (ds.Initialized)
            {
                var item = ds.GetFirstItem();

                bool values = false;

                {
                    var x = item.CheckGet("SQUARE");
                    if (!string.IsNullOrEmpty(x))
                    {
                        TotalsSquareValue.Text = x.ToInt().ToString();
                        TotalsSquareValue.Visibility = Visibility.Visible;
                        TotalsSquareUnit.Visibility = Visibility.Visible;
                        values = true;
                    }
                }

                {
                    var x = item.CheckGet("WEIGHT");
                    if (!string.IsNullOrEmpty(x))
                    {
                        TotalsWeightValue.Text = x.ToInt().ToString();
                        TotalsWeightValue.Visibility = Visibility.Visible;
                        TotalsWeightUnit.Visibility = Visibility.Visible;
                        values = true;
                    }
                }

                if (values)
                {
                    TotalsTitle.Visibility = Visibility.Visible;
                }
            }
        }

        public void PositionGridInit()
        {
            //инициализация грида
            {
                //список колонок грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Комплект",
                        Path="POSITIONNUM",
                        ColumnType=ColumnTypeRef.String,
                        Width2=4,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД Заявки",
                        Path="APPLICATIONID",
                        ColumnType=ColumnTypeRef.String,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Path="VENDORCODE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=15,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="PRODUCTNAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=37,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Диаметр рулона",
                        Path="ROLLDIAMETER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ограничение количества",
                        Path="QUANTITYLIMIT",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Поддонов по заявке, шт.",
                        Path="PALLETQUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество в заявке",
                        Path="QUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Рулонов",
                        Path="ROLLQUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Склад под отгр.",
                        Path="INSTOCKQUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Склад всего",
                        Path="INSTOCKQUANTITYTOTAL",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Отгружено",
                        Path="SHIPPEDQUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Отклонение, %",
                        Path="QUANTITY_PERCENTAGE_DEVIATION",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=5,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // Если позиция отгружена
                                    if (row.CheckGet("STATUSID").ToInt() == 0)
                                    {
                                        // процентное отклонение отгруженного количества к количеству по заявке превышает допустимые значения
                                        if (!PercentageDeviation.CheckPercentageDeviation(row.CheckGet("QUANTITY").ToInt(), row["QUANTITY_PERCENTAGE_DEVIATION"].ToDouble()))
                                        {
                                            if (row["QUANTITY_PERCENTAGE_DEVIATION"].ToDouble() >= 0)
                                            {
                                                color = HColor.Red;
                                            }
                                            else
                                            {
                                                color = HColor.Yellow;
                                            }
                                        }
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Вес брутто по заявке, кг.",
                        Path="WEIGHT_GROSS",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=7,
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
                        Header="Цена без НДС",
                        Path="PRICE",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=5,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Цена с НДС",
                        Path="PRICEVAT",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=5,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Ед. измерения",
                        Path="UNITOFMEASUREMENT",
                        ColumnType=ColumnTypeRef.String,
                        Width2=4,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Схема производства",
                        Path="PRODUCTIONSCHEME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=12,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Упаковка",
                        Path="PACKAGING",
                        ColumnType=ColumnTypeRef.String,
                        Width2=4,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Покупатель",
                        Path="CUSTOMER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=16,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Адрес доставки",
                        Path="DELIVERYADDRESS",
                        ColumnType=ColumnTypeRef.String,
                        Width2=31,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Примеч. кладовщику",
                        Path="STOREKEEPERNOTE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=20,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Примеч. грузчику",
                        Path="PORTERNOTE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=20,
                    },

                    new DataGridHelperColumn
                    {
                        Header="номер строки",
                        Path="ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="номер точки отгрузки",
                        Path="LOADORDER",
                        ColumnType=ColumnTypeRef.Integer,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Id позиции",
                        Path="POSITIONID",
                        ColumnType=ColumnTypeRef.Integer,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Id продукции",
                        Path="PRODUCTID",
                        ColumnType=ColumnTypeRef.Integer,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Id корневой позиции (для комплектов)",
                        Path="MAINPOSITIONID",
                        ColumnType=ColumnTypeRef.Integer,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="INNERPOSITIONLABEL",
                        Path="INNERPOSITIONLABEL",
                        ColumnType=ColumnTypeRef.String,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Id категории продукции",
                        Path="CATEGORYID",
                        ColumnType=ColumnTypeRef.Integer,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="код товара",
                        Path="CODE",
                        ColumnType=ColumnTypeRef.String,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул для печати на ярлыке (для покупателя)",
                        Path="VENDORCODEPRINTING",
                        ColumnType=ColumnTypeRef.String,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="STATUSID",
                        Path="STATUSID",
                        ColumnType=ColumnTypeRef.Integer,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="общая площадь",
                        Path="SQUARE",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N4",
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="На складе|под отгр.",
                        Path="INSTOCKQUANTITYAPPLICATION",
                        ColumnType=ColumnTypeRef.Integer,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="PRICEACTUAL",
                        Path="PRICEACTUAL",
                        ColumnType=ColumnTypeRef.String,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="транспортный пакет",
                        Path="TRANSPORTPACK",
                        ColumnType=ColumnTypeRef.String,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид покупателя",
                        Path="CUSTOMER_ID",
                        ColumnType=ColumnTypeRef.String,
                        Visible=false,
                    },
                    
                };
                PositionGrid.SetColumns(columns);

                PositionGrid.SetPrimaryKey("POSITIONNUM");
                PositionGrid.SearchText = PositionSearchBox;
                //данные грида
                PositionGrid.OnLoadItems = PositionGridLoadItems;
                PositionGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                PositionGrid.AutoUpdateInterval = 0;
                PositionGrid.Toolbar = PositionGridToolbar;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                PositionGrid.OnSelectItem = selectedItem =>
                {
                };

                PositionGrid.Commands = Commander;

                PositionGrid.Init();
            }
        }

        public async void PositionGridLoadItems()
        {
            var p = new Dictionary<string, string>();
            p.Add("SHIPMENT_ID", $"{ShipmentGrid.SelectedItem.CheckGet("ID")}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Position");
            q.Request.SetParam("Action", "List");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            PositionDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    PositionDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            PositionGrid.UpdateItems(PositionDataSet);
        }

        public void DriverGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Description="Идентификатор записи",
                        Path="ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид водителя",
                        Description="Идентификатор водителя",
                        Path="DRIVERID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Приезд",
                        Description="Дата приезда водителя",
                        Path="ARRIVEDATE",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm",
                        Width2 = 12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Въезд",
                        Description="Дата въезда водителя под отгрузку",
                        Path="ENTRYDATE",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm",
                        Width2 = 12,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // перенесённая отгрузка
                                    if (row.CheckGet("UNSHIPPED").ToInt() > 0)
                                    {
                                        color = HColor.Orange;
                                    }
                                    // опоздавшая отгрузка
                                    else if (row.CheckGet("LATE").ToInt() > 0)
                                    {
                                        color = HColor.Yellow;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Водитель",
                        Description="Наименование водителя",
                        Path="DRIVERNAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 30,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // Флаг того, что водитель зарегистрировался удалённо (через сайт)
                                    if (row.CheckGet("REMOTE_REGISTRATION_FLAG").ToInt() > 0)
                                    {
                                        color = HColor.VioletDark; //"#FF9400D3";
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Телефон",
                        Description="Телефон водителя",
                        Path="DRIVERPHONE",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 11,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Машина",
                        Description="Данные транспортного средства",
                        Path="CAR",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 24,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Покупатель",
                        Description="Наименование покупателя",
                        Path="BUYER",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 30,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Терминал",
                        Description="Номер терминала",
                        Path="TERMINALNUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Паспорт",
                        Description="Паспортные данные водителя",
                        Path="PASSPORT",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 17,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Отгрузка",
                        Description="Дата отгрузки",
                        Path="SHIPMENTDATE",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy",
                        Width2 = 5,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид отгрузки",
                        Description="Идентификатор отгрузки",
                        Path="TRANSPORTID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Привязан",
                        Description="Привязан к транспорту",
                        Path="TRANSPORTASSIGNED",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Марка",
                        Description="Марка автомобиля",
                        Path="CARMARK",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 5,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Регистрация через сайт",
                        Description="водитель самостоятельно зарегистрировался через сайт",
                        Path="REMOTE_REGISTRATION_FLAG",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Опоздавшая",
                        Description="Опоздавшая отгрузка",
                        Path="Late",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Перенесённая",
                        Description="Перенесённая отгрузка",
                        Path="Unshipped",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden = true,
                    },
                    
                };
                DriverGrid.SetColumns(columns);
                DriverGrid.SetPrimaryKey("DRIVERID");
                DriverGrid.SearchText = DriverSearchBox;
                //данные грида
                DriverGrid.OnLoadItems = DriverGridLoadItems;
                DriverGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                DriverGrid.AutoUpdateInterval = 60 * 5;
                DriverGrid.Toolbar = DriverGridToolbar;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                DriverGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem != null)
                    {
                        if (DriverGrid != null && DriverGrid.Items != null && DriverGrid.Items.Count > 0)
                        {
                            if (DriverGrid.Items.FirstOrDefault(x => x.CheckGet("ID").ToInt() == selectedItem.CheckGet("ID").ToInt()) == null)
                            {
                                DriverGrid.SelectRowFirst();
                            }
                        }
                    }
                };

                DriverGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    {
                        StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";


                            //водитель въехал
                            if (row.ContainsKey("ENTRYDATE"))
                            {
                                if(!string.IsNullOrEmpty(row["ENTRYDATE"]))
                                {
                                    color = HColor.Green;
                                }
                            }

                            //отгрузка не привязана
                            if(row.CheckGet("TRANSPORTID").ToInt()==0)
                            {
                                color = HColor.Blue;
                            }


                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },
                };

                DriverGrid.OnFilterItems = DriverGridFilterItems;

                DriverGrid.Commands = Commander;

                DriverGrid.Init();
            }
        }

        public void DriverGridFilterItems()
        {
            if (DriverGrid != null && DriverGrid.SelectedItem != null && DriverGrid.SelectedItem.Count > 0)
            {
                DriverGrid.Commands.Message = new ItemMessage() { Action = "refresh", Message = $"{DriverGrid.SelectedItem.CheckGet("ID")}" };
            }
        }

        public async void DriverGridLoadItems()
        {
            var p = new Dictionary<string, string>();
            p.Add("FACTORY_ID", $"{FactoryId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments/ShipmentKsh");
            q.Request.SetParam("Object", "TransportDriver");
            q.Request.SetParam("Action", "List");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            DriverDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    DriverDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            DriverGrid.UpdateItems(DriverDataSet);
        }

        public void TerminalGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Терминал",
                        Description="Наименование терминала",
                        Path="TERMINAL_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Покупатель",
                        Description="Наименование покупателя",
                        Path="BUYER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 14,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // Отгрузка заблокирована из-за несоответствия габаритов транспорта
                                    if (row.CheckGet("SHPMENT_BLOCKED_FLAG").ToInt() > 0)
                                    {
                                        color = HColor.Red;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Перевозчик",
                        Description="Водитель по отгрузке",
                        Path="DRIVER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 22,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Автомобиль",
                        Description="Данные по транспортному средству",
                        Path="CAR",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 13,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Постановка",
                        Description="Дата постановки отгрузки на терминал",
                        Path="BIND_DATE",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format = "dd.MM.yyyy HH:mm:ss",
                        Width2 = 12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Погрузчик",
                        Description="Водитель погрузчика",
                        Path="FORKLIFTDRIVER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Все",
                        Description="Да - Показать погрузчику все поддоны" +
                        $"{Environment.NewLine}Неполные - Показать погрузчику все неполные поддоны" +
                        $"{Environment.NewLine}Нет - Не показывать погрузчику все поддоны",
                        Path="SHOW_ALL_PALLET_FLAG_VALUE",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width2 = 5,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Все",
                        Description="Показать погрузчику все поддоны",
                        Path="SHOW_ALL_PALLET_FLAG",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Отгрузка заблокирована",
                        Description="Отгрузка заблокирована из-за несоответствия габаритов транспорта",
                        Path="SHPMENT_BLOCKED_FLAG",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид отгрукзки",
                        Description="Идентификатор отгрукзки",
                        Path="TRANSPORT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус",
                        Description="Статус терминала",
                        Path="TERMINAL_STATUS",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер терминала",
                        Description="Номер терминала",
                        Path="TERMINAL_NUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид погрузчика",
                        Description="Идентификатор водителя погрузчика",
                        Path="FORKLIFTDRIVER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Тип",
                        Description="Тип терминала",
                        Path="TERMINAL_TYPE",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },                   
                    new DataGridHelperColumn
                    {
                        Header="Заблокирован",
                        Description="Флаг заблокированного терминала",
                        Path="BLOCKED_FLAG",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид терминала",
                        Description="Идентификатор терминала",
                        Path="TERMINAL_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 4,
                        Hidden=true,
                    },
                };
                TerminalGrid.SetColumns(columns);
                TerminalGrid.SetPrimaryKey("TERMINAL_ID");
                TerminalGrid.SearchText = TerminalSearchBox;
                //данные грида
                TerminalGrid.OnLoadItems = TerminalGridLoadItems;
                TerminalGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                TerminalGrid.AutoUpdateInterval = 60 * 5;
                TerminalGrid.Toolbar = TerminalGridToolbar;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                TerminalGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem != null)
                    {
                        if (TerminalGrid != null && TerminalGrid.Items != null && TerminalGrid.Items.Count > 0)
                        {
                            if (TerminalGrid.Items.FirstOrDefault(x => x.CheckGet("TERMINAL_ID").ToInt() == selectedItem.CheckGet("TERMINAL_ID").ToInt()) == null)
                            {
                                TerminalGrid.SelectRowFirst();
                            }
                        }
                    }
                };

                TerminalGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    {
                        StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";
                            
                            // нет отгрузки -- голубой
                            if(string.IsNullOrEmpty(row.CheckGet("BUYER_NAME")))
                            {
                                color=HColor.Blue;
                            }

                            // 1 - внешняя перестройка -- желтый
                            if(row.CheckGet("TERMINAL_STATUS").ToInt()==1)
                            {
                                color=HColor.Yellow;
                            }

                            // терминал заблокирован
                            if (row.CheckGet("BLOCKED_FLAG") == "1")
                            {
                                color = HColor.Red;
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },
                };

                TerminalGrid.OnFilterItems = TerminalGridFilterItems;

                TerminalGrid.Commands = Commander;

                TerminalGrid.Init();
            }
        }

        public void TerminalGridFilterItems()
        {
            if (TerminalGrid != null && TerminalGrid.SelectedItem != null && TerminalGrid.SelectedItem.Count > 0)
            {
                TerminalGrid.Commands.Message = new ItemMessage() { Action = "refresh", Message = $"{TerminalGrid.SelectedItem.CheckGet("TERMINAL_ID")}" };
            }
        }

        public async void TerminalGridLoadItems()
        {
            var p = new Dictionary<string, string>();
            p.Add("FACTORY_ID", $"{FactoryId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments/ShipmentKsh");
            q.Request.SetParam("Object", "Terminal");
            q.Request.SetParam("Action", "List");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            TerminalDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    TerminalDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            TerminalGrid.UpdateItems(TerminalDataSet);
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void _ProcessMessages(ItemMessage m)
        {
            if(m.ReceiverName.IndexOf(this.ControlName) > -1)
            {
                switch (m.Action)
                {
                    //// регистрация приехавшего водителя
                    //case "SelectItem":
                    //    {
                    //        ArrivedDriverData = new Dictionary<string, string>();
                    //        if (m.ContextObject != null)
                    //        {
                    //            ArrivedDriverData = (Dictionary<string, string>)m.ContextObject;
                    //        }

                    //        ChoiseShipmentDateTime();
                    //    }
                    //    break;

                    case "ShowShipmentToPrint":
                        {
                            Central.WM.SetActive("ShipmentKshList");
                            ShowShipmentToPrint();
                        }
                        break;

                    case "Refresh":
                        {
                            Refresh();
                        }
                        break;
                }
            }

            //if (m.ReceiverGroup.IndexOf("ShipmentControl") > -1)
            //{
            //    if (m.ReceiverName.IndexOf("ShipmentKshListSelectShipmentDateTime") > -1)
            //    {
            //        switch (m.Action)
            //        {
            //            // регистрация приехавшего водителя
            //            case "Save":
            //                {
            //                    var p = new Dictionary<string, string>();
            //                    if (m.ContextObject != null)
            //                    {
            //                        p = (Dictionary<string, string>)m.ContextObject;
            //                    }
            //                    ArrivedDriverData.AddRange(p);
            //                    SetArrived();
            //                }
            //                break;
            //        }
            //    }
            //}
        }

        public void DisableControls()
        {
            ShipmentGrid.SetBusy(true);
            ShipmentGridToolbar.IsEnabled = false;
        }

        public void EnabledControls()
        {
            ShipmentGrid.SetBusy(false);
            ShipmentGridToolbar.IsEnabled = true;
        }

        private bool GetLoadingOrderTwoVisible()
        {
            bool result = false;

            var mode = Central.Navigator.GetRoleLevel("[erp]manually_loading_scheme");
            switch (mode)
            {
                case Role.AccessMode.Special:
                case Role.AccessMode.FullAccess:
                case Role.AccessMode.ReadOnly:
                    result = true;
                    break;

                default:
                    result = false;
                    break;
            }

            return result;
        }

        private void EditShipment()
        {
            int shipmentId = ShipmentGrid.SelectedItem.CheckGet("ID").ToInt();
            if (shipmentId > 0)
            {
                var h = new ShipmentEdit();
                h.Edit(shipmentId);
            }
            else
            {
                var msg = "Выберите отгрузку";
                var d = new DialogWindow($"{msg}", "Изменение отгрузки", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        private void SetComment()
        {
            int shipmentId = ShipmentGrid.SelectedItem.CheckGet("ID").ToInt();
            if (shipmentId > 0)
            {
                var h = new ShipmentComment();
                h.Edit(shipmentId);
            }
            else
            {
                var msg = "Выберите отгрузку";
                var d = new DialogWindow($"{msg}", "Изменение примечания", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        private void SetLateReason()
        {
            int shipmentId = ShipmentGrid.SelectedItem.CheckGet("ID").ToInt();
            if (shipmentId > 0)
            {
                var h = new ShipmentReasonOfLateness();
                h.Edit(shipmentId);
            }
            else
            {
                var msg = "Выберите отгрузку";
                var d = new DialogWindow($"{msg}", "Причина опоздания", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        private void EditShipmentTime()
        {
            if (!(ShipmentGrid.SelectedItem.CheckGet("C").ToInt() > 0 && ShipmentGrid.SelectedItem.CheckGet("ATTERMINAL").ToInt() == 0))
            {
                int shipmentId = ShipmentGrid.SelectedItem.CheckGet("ID").ToInt();
                if (shipmentId > 0)
                {
                    var i = new ShipmentDateChange();
                    i.ShipmentId = shipmentId;
                    i.ShipmentType = ShipmentGrid.SelectedItem.CheckGet("SHIPMENTTYPE").ToInt();
                    i.FactoryId = this.FactoryId;
                    i.RoleName = this.RoleName;
                    i.Show();
                }
                else
                {
                    var msg = "Выберите отгрузку";
                    var d = new DialogWindow($"{msg}", "Перенос времени отгрузки", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
        }

        private void ShowShipmentInfo()
        {
            int shipmentId = ShipmentGrid.SelectedItem.CheckGet("ID").ToInt();
            if (shipmentId > 0)
            {
                var h = new ShipmentInformation();
                h.Id = shipmentId;
                h.Init();
                h.Open();
            }
            else
            {
                var msg = "Выберите отгрузку";
                var d = new DialogWindow($"{msg}", "Информация об отгрузке", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        private void ShowHistoryExtend()
        {
            int shipmentId = ShipmentGrid.SelectedItem.CheckGet("ID").ToInt();
            if (shipmentId > 0)
            {
                var h = new ShipmentHistoryExtended();
                h.ShipmentId = shipmentId;
                h.Init();
            }
            else
            {
                var msg = "Выберите отгрузку";
                var d = new DialogWindow($"{msg}", "История изменения отгрузки", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        private void LoadingOrder()
        {
            int shipmentId = ShipmentGrid.SelectedItem.CheckGet("ID").ToInt();
            if (shipmentId > 0)
            {
                var h = new Shipment(shipmentId);
                h.ShowLoadingScheme(ShipmentGrid.SelectedItem);
            }
            else
            {
                var msg = "Выберите отгрузку";
                var d = new DialogWindow($"{msg}", "Порядок отгрузки", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        private void EditLoadingOrder()
        {
            if (ShipmentGrid.SelectedItem != null)
            {
                if (ShipmentGrid.SelectedItem.CheckGet("PACKAGINGTYPETEXT") == "су" || ShipmentGrid.SelectedItem.CheckGet("PACKAGINGTYPETEXT") == "бу")
                {
                    if (ShipmentGrid.SelectedItem.CheckGet("LOADINGSCHEMESTATUS") == "1")
                    {
                        string msg = "Автоматическая схема погрузки для данного транспортного средства запрещена менеджером.";
                        var d = new DialogWindow($"{msg}", "Схема погрузки", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                    else if (ShipmentGrid.SelectedItem.CheckGet("LOADINGSCHEMESTATUS") == "2")
                    {
                        int id = ShipmentGrid.SelectedItem.CheckGet("ID").ToInt();
                        var shipment = new Shipment(id);
                        shipment.ShowLoadingScheme(ShipmentGrid.SelectedItem);
                    }
                    else
                    {
                        var id = ShipmentGrid.SelectedItem.CheckGet("ID").ToInt();
                        var loadingOrderTwo = new ShipmentShemeTwo();
                        loadingOrderTwo.ReturnTabName = this.FrameName;
                        loadingOrderTwo.ShipmentId = id;
                        loadingOrderTwo.Init();
                    }
                }
                else
                {
                    string msg = "Для данной отгрузки нет схемы, т.к. она содержит бумагу в рулонах";
                    var d = new DialogWindow($"{msg}", "Схема погрузки 2", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
        }

        private void BindDriver()
        {
            var resume = true;
            var bindDriver = new BindDriver();

            if (resume)
            {
                if (
                    ShipmentGrid.SelectedItem == null
                    || DriverGrid.SelectedItem == null
                )
                {
                    var msg = "Выберите отгрузку и водителя.";
                    var d = new DialogWindow($"{msg}", "Привязка водителя", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                    resume = false;
                }
            }

            if (resume)
            {
                bindDriver.Id = ShipmentGrid.SelectedItem.CheckGet("ID").ToInt();

                if (bindDriver.Id > 0)
                {
                }
                else
                {
                    resume = false;
                }
            }

            if (resume)
            {
                bindDriver.DriverLogId = DriverGrid.SelectedItem.CheckGet("ID").ToInt();
                bindDriver.DriverId = DriverGrid.SelectedItem.CheckGet("DRIVERID").ToInt();

                if (bindDriver.DriverLogId > 0
                    && bindDriver.DriverId > 0)
                {
                }
                else
                {
                    resume = false;
                }
            }

            if (resume)
            {
                if (ShipmentDataSet != null)
                {
                    bindDriver.ShipmentsDS = ShipmentDataSet;
                }
                else
                {
                    resume = false;
                }

                if (DriverDataSet != null)
                {
                    bindDriver.DriversDS = DriverDataSet;
                }
                else
                {
                    resume = false;
                }
            }

            // Проверяем, что этот водитель не привязан к более ранней отгрузке
            if (resume)
            {
                resume = CheckDriverEarlyShipment(bindDriver.DriverId, ShipmentGrid.SelectedItem.CheckGet("SHIPMENTDATETIME"));
            }

            if (resume)
            {
                bindDriver.Edit();
            }
        }

        /// <summary>
        /// отвязать водителя
        /// </summary>
        public async void UnbindDriver()
        {
            bool resume = true;
            int shipmentId = 0;

            if (resume)
            {
                shipmentId = ShipmentGrid.SelectedItem.CheckGet("ID").ToInt();
                if (shipmentId == 0)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var msg = "";
                msg = $"{msg}Отвязать водителя от отгрузки?\n";

                if (!string.IsNullOrEmpty(ShipmentGrid.SelectedItem.CheckGet("BUYER")))
                {
                    msg = $"{msg}{ShipmentGrid.SelectedItem.CheckGet("BUYER")}\n";
                }

                if (!string.IsNullOrEmpty(ShipmentGrid.SelectedItem.CheckGet("DRIVER")))
                {
                    msg = $"{msg}{ShipmentGrid.SelectedItem.CheckGet("DRIVER")}\n";
                }

                var d = new DialogWindow($"{msg}", "Отвязка водителя", "", DialogWindowButtons.NoYes);
                if (d.ShowDialog() != true)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("ID", shipmentId.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "Shipment");
                q.Request.SetParam("Action", "UnbindDriver");

                q.Request.SetParams(p);

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
                        var ds = ListDataSet.Create(result, "ITEMS");
                        var id = ds.GetFirstItemValueByKey("ID").ToInt();
                        if (id != 0)
                        {
                            succesfullFlag = true;
                        }
                    }

                    if (succesfullFlag)
                    {
                        Refresh();
                    }
                    else
                    {
                        var msg = "При отвязывании водителя от отгрузки произошла ошибка. Пожалуйста, сообщите о проблеме";
                        var d = new DialogWindow($"{msg}", "Привязка водителя", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        public bool CheckDriverEarlyShipment(int driverId, string shipmentDate)
        {
            bool checkResult = true;

            if (!string.IsNullOrEmpty(shipmentDate))
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("DRIVER_ID", driverId.ToString());
                    p.Add("SHIPMENT_DATE", shipmentDate);
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "Shipment");
                q.Request.SetParam("Action", "GetDriverEarlyShipment");

                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                        {
                            var msg = $"Водитель уже привязан к более ранней неотгруженной отгрузке.{Environment.NewLine}" +
                                $"Сначала отгрузите отгрузку от {ds.Items[0].CheckGet("SHIPMENT_DATETIME")}";
                            var d = new DialogWindow($"{msg}", "Привязка водителя", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                            checkResult = false;
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                    checkResult = false;
                }
            }

            return checkResult;
        }

        private void CreateAutoShipment()
        {
            int shipmentId = ShipmentGrid.SelectedItem.CheckGet("ID").ToInt();
            if (shipmentId > 0)
            {
                var view = new ShipmentAuto
                {
                    IdTs = $"{shipmentId}",
                };
                view.Edit();
            }
            else
            {
                var msg = "Выберите отгрузку";
                var d = new DialogWindow($"{msg}", "Автосоздание отгрузки", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        private void PalletConsumption()
        {
            int shipmentId = ShipmentGrid.SelectedItem.CheckGet("ID").ToInt();
            if (shipmentId > 0)
            {
                Central.LoadServerParams();

                var view = new PalletConsumption
                {
                    IdTs = $"{shipmentId}",
                };
                view.Edit();
            }
            else
            {
                var msg = "Выберите отгрузку";
                var d = new DialogWindow($"{msg}", "Расход поддонов", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Открытие вкладки накладных по выбранной отгрузке
        /// </summary>
        private void ShipmentDocumentList()
        {
            if (ShipmentGrid.SelectedItem != null && ShipmentGrid.SelectedItem.Count > 0)
            {
                var shipmentDocumentList = new ShipmentDocumentList();
                shipmentDocumentList.RoleName = this.RoleName;
                shipmentDocumentList.TransportId = ShipmentGrid.SelectedItem.CheckGet("ID").ToInt();
                shipmentDocumentList.SelectedShipmentItem = ShipmentGrid.SelectedItem;
                shipmentDocumentList.Show();
            }
        }

        private void SetPrintControlEnabled()
        {
            PrintProxyDocsButton.IsEnabled = false;
            PrintDriverBootCardButton.IsEnabled = false;
            PrintShipmentOrderBootCardButton.IsEnabled = false;
            PrintMapButton.IsEnabled = false;
            PrintRouteMapsButton.IsEnabled = false;

            if (ShipmentGrid.SelectedItem != null && ShipmentGrid.SelectedItem.Count > 0)
            {
                PrintDriverBootCardButton.IsEnabled = true;
                PrintShipmentOrderBootCardButton.IsEnabled = true;
                PrintMapButton.IsEnabled = true;

                if (
                    ShipmentGrid.SelectedItem["ATTORNEYLETTER"].ToBool()
                    || ShipmentGrid.SelectedItem["TENDERDOC"].ToBool()
                )
                {
                    PrintProxyDocsButton.IsEnabled = true;
                }

                if (ShipmentGrid.SelectedItem["ADDRESSCNT"].ToInt() > 0)
                {
                    PrintRouteMapsButton.IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// печать всех документов
        /// </summary>
        private void PrintAll()
        {
            if (ShipmentGrid.SelectedItem != null)
            {
                var id = ShipmentGrid.SelectedItem.CheckGet("ID").ToInt();
                var reporter = new ShipmentReport(id);
                reporter.PrintProxy(true);
                reporter.PrintBootcard(true);
                reporter.PrintShipmenttask(true);
                reporter.PrintStockmap(true);
                reporter.PrintRoutemap(true);
            }
        }

        private void ShowAll()
        {
            if (ShipmentGrid.SelectedItem != null)
            {
                var id = ShipmentGrid.SelectedItem.CheckGet("ID").ToInt();
                var reporter = new ShipmentReport(id);
                reporter.PrintProxy();
                reporter.PrintBootcard();
                reporter.PrintShipmenttask();
                reporter.PrintStockmap();
                reporter.PrintRoutemap();
            }
        }

        /// <summary>
        /// Вывод печатной формы доверенности
        /// </summary>
        private void PrintProxy()
        {
            if (ShipmentGrid.SelectedItem != null)
            {
                var id = ShipmentGrid.SelectedItem.CheckGet("ID").ToInt();
                var reporter = new ShipmentReport(id);
                reporter.PrintProxy();
            }
        }

        /// <summary>
        /// Вывод печатной формы загрузочной карты водителя
        /// </summary>
        private void PrintBootcard()
        {
            if (ShipmentGrid.SelectedItem != null)
            {
                var id = ShipmentGrid.SelectedItem.CheckGet("ID").ToInt();
                var reporter = new ShipmentReport(id);
                reporter.PrintBootcard();
            }
        }

        /// <summary>
        /// Вывод печатной формы задания на отгрузку
        /// </summary>
        private void PrintShipmenttask()
        {
            if (ShipmentGrid.SelectedItem != null)
            {
                var id = ShipmentGrid.SelectedItem.CheckGet("ID").ToInt();
                var reporter = new ShipmentReport(id);
                reporter.PrintShipmenttask();
            }
        }

        /// <summary>
        /// Вывод печатной формы карты склада
        /// </summary>
        private void PrintStockmap()
        {
            if (ShipmentGrid.SelectedItem != null)
            {
                var id = ShipmentGrid.SelectedItem.CheckGet("ID").ToInt();
                var reporter = new ShipmentReport(id);
                reporter.PrintStockmap();
            }
        }

        /// <summary>
        /// Вывод печатной формы карты проезда
        /// </summary>
        private void PrintRoutemap()
        {
            if (ShipmentGrid.SelectedItem != null)
            {
                var id = ShipmentGrid.SelectedItem.CheckGet("ID").ToInt();
                var reporter = new ShipmentReport(id);
                reporter.PrintRoutemap();
            }
        }

        /// <summary>
        /// Показать закрытые отгрузки, для которых нужно распечатать документы
        /// </summary>
        private void ShowShipmentToPrint()
        {
            if (ShipmentToPrintCount > 0)
            {
                ShipmentSearchBox.Clear();
                // На печать
                ShipmentTypes.SetSelectedItemByKey("7");
                // Показываем завершённые
                HideCompleteCheckbox.IsChecked = false;
                // Получаем данные для грида отгрузок, чтобы получить актуальные статусы отгрузок
                ShipmentGrid.LoadItems();
                // Получаем актуальное количество отгрузок, для которых нужно распечатать документы
                GetShipmentCountToPrint();
            }
        }

        /// <summary>
        /// Получаем колчество отгрузок в текущих производственных сутках, для которых нужно распечатать документы
        /// </summary>
        public async void GetShipmentCountToPrint()
        {
            string dateFrom = "";
            string dateTo = "";

            // Какой диапазон дат пользователи выбрали в интерфейсе для списка отгрузок, за такой диапазон и будем искать закрытые отгрузки
            dateFrom = $"{FromDate.Text.ToDateTime("dd.MM.yyyy HH:mm:ss").ToString("dd.MM.yyyy")}";
            dateTo = $"{ToDate.Text.ToDateTime("dd.MM.yyyy HH:mm:ss").ToString("dd.MM.yyyy")}";

            var p = new Dictionary<string, string>();
            {
                p.Add("FROM_DATE", dateFrom);
                p.Add("TO_DATE", dateTo);
                p.Add("FACTORY_ID", $"{FactoryId}");
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Shipment");
            q.Request.SetParam("Action", "GetCountToPrint");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            ShipmentToPrintCount = 0;
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        ShipmentToPrintCount = ds.Items.First().CheckGet("SHIPMENT_COUNT").ToInt();
                    }
                }
            }

            // Считаем количество отгрузок, для которых нужно распечатать документы
            {
                ShipmentCountToPrintButton.Content = $"{ShipmentCountToPrintText}{ShipmentToPrintCount}";
                if (ShipmentToPrintCount > 0)
                {
                    ShipmentCountToPrintButton.IsEnabled = true;
                }
                else
                {
                    ShipmentCountToPrintButton.IsEnabled = false;
                }

                if (ShipmentPlan != null && ShipmentPlan.ShipmentCountToPrintButton != null)
                {
                    ShipmentPlan.ShipmentCountToPrintButton.Content = ShipmentCountToPrintButton.Content;
                    ShipmentPlan.ShipmentCountToPrintButton.IsEnabled = ShipmentCountToPrintButton.IsEnabled;
                }
                else
                {
                    if (Central.WM.TabItems.FirstOrDefault(x => x.Key == "ShipmentKshPlan").Value != null)
                    {
                        ShipmentPlan = (ShipmentKshPlan)Central.WM.TabItems.FirstOrDefault(x => x.Key == "ShipmentKshPlan").Value.Content;
                        if (ShipmentPlan != null && ShipmentPlan.ShipmentCountToPrintButton != null)
                        {
                            ShipmentPlan.ShipmentCountToPrintButton.Content = ShipmentCountToPrintButton.Content;
                            ShipmentPlan.ShipmentCountToPrintButton.IsEnabled = ShipmentCountToPrintButton.IsEnabled;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Запуск работы таймера для вызова функции получения количества отгрузок в текущих производственных сутках, для которых нужно распечатать документы
        /// </summary>
        public void RunGetShipmentCountToPrintTimer()
        {
            if (GetShipmentCountToPrintTimerInterval != 0)
            {
                if (GetShipmentCountToPrintTimer == null)
                {
                    GetShipmentCountToPrintTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0, 0, GetShipmentCountToPrintTimerInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", GetShipmentCountToPrintTimerInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("ShipmentsListKsh_RunGetShipmentCountToPrintTimer", row);
                    }

                    GetShipmentCountToPrintTimer.Tick += (s, e) =>
                    {
                        GetShipmentCountToPrint();
                    };
                }

                if (GetShipmentCountToPrintTimer.IsEnabled)
                {
                    GetShipmentCountToPrintTimer.Stop();
                }

                GetShipmentCountToPrintTimer.Start();
            }
        }

        /// <summary>
        /// открытие списка машин с допуском на СГП
        /// </summary>
        private void OpenGate()
        {
            Central.WM.AddTab("transport_access", "Допуск автотранспорта");
            Central.WM.CheckAddTab<ExpectedCarList>("pending", "Ожидаемый допуск", false, "transport_access", "bottom");
            Central.WM.SetActive("pending");
        }

        /// <summary>
        /// Отметить убытие водителя
        /// </summary>
        private async void MarkDeparture()
        {
            var resume = true;

            if (resume)
            {
                var msg = "";
                msg = $"{msg}Отметить убытие водителя?\n";
                msg = $"{msg}{DriverGrid.SelectedItem.CheckGet("DRIVERNAME")}\n";
                var d = new DialogWindow($"{msg}", "Убытие водителя", "", DialogWindowButtons.NoYes);
                if (d.ShowDialog() != true)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("ID", DriverGrid.SelectedItem.CheckGet("ID"));
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "TransportDriver");
                q.Request.SetParam("Action", "MarkDeparture");

                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        var id = ds.GetFirstItemValueByKey("ID").ToInt();
                        if (id != 0)
                        {
                            Refresh();
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// Отменить въезд водителя
        /// </summary>
        private async void CancelDriverEntry()
        {
            var resume = true;

            if (resume)
            {
                var msg = "";
                msg = $"{msg}Отменить въезд водителя?\n";
                msg = $"{msg}{DriverGrid.SelectedItem.CheckGet("DRIVERNAME")}\n";
                var d = new DialogWindow($"{msg}", "Въезд водителя", "", DialogWindowButtons.NoYes);
                if (d.ShowDialog() != true)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("ID", DriverGrid.SelectedItem.CheckGet("ID"));
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "TransportDriver");
                q.Request.SetParam("Action", "CancelEntry");

                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        var id = ds.GetFirstItemValueByKey("ID").ToInt();
                        if (id != 0)
                        {
                            Refresh();
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        public async void DeleteDriver()
        {
            var resume = true;

            if (resume)
            {
                var msg = "";
                msg = $"{msg}Удалить водителя из списка приехавших?\n";
                msg = $"{msg}{DriverGrid.SelectedItem.CheckGet("DRIVERNAME")}\n";
                var d = new DialogWindow($"{msg}", "Приезд водителя", "", DialogWindowButtons.NoYes);
                if (d.ShowDialog() != true)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("ID", DriverGrid.SelectedItem.CheckGet("ID"));
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "TransportDriver");
                q.Request.SetParam("Action", "DeleteArrived");

                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        var id = ds.GetFirstItemValueByKey("ID").ToInt();
                        if (id != 0)
                        {
                            Refresh();
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        public void EditDriver()
        {
            int id = DriverGrid.SelectedItem.CheckGet("DRIVERID").ToInt();
            var driver = new Driver
            {
                Id = id,
                DriverLogId = DriverGrid.SelectedItem.CheckGet("ID").ToInt()
            };
            driver.ReturnTabName = this.ControlName;
            driver.Edit(id);
        }

        //public void AddDriver()
        //{
        //    Central.WM.RemoveTab($"AddDriver");

        //    var i = new AddDriver();
        //    Central.WM.SetLayer("add");
        //    try
        //    {
        //        ((DriverListExpected)Central.WM.TabItems.FirstOrDefault(x => x.Key == "AddDriver_ExpectedDrivers").Value.Content).ParentFrameType = DriverListExpected.ParentFrameTypeDefault.ShipmentKshList;
        //        ((DriverListExpected)Central.WM.TabItems.FirstOrDefault(x => x.Key == "AddDriver_ExpectedDrivers").Value.Content).Types.SetSelectedItemByKey("-2");
        //        ((DriverListExpected)Central.WM.TabItems.FirstOrDefault(x => x.Key == "AddDriver_ExpectedDrivers").Value.Content).LoadItems();
        //        ((DriverListAll)Central.WM.TabItems.FirstOrDefault(x => x.Key == "AddDriver_AllDrivers").Value.Content).ParentFrameType = DriverListAll.ParentFrameTypeDefault.ShipmentKshList;
        //    }
        //    catch (Exception)
        //    {
        //    }
        //}

        public void AddDriver()
        {
            var i = new DriverListKshInterface();
            i.ParentFrame = this.FrameName;
            i.RoleName = this.RoleName;
            i.FactoryId = this.FactoryId;
            i.OnChoiceDriver = ChoiceDriver;
            i.SetValues();
        }

        private void ChoiceDriver(Dictionary<string, string> driverItem)
        {
            if (driverItem != null && driverItem.Count > 0)
            {
                if (driverItem.CheckGet("UNSHIPPED").ToInt() == 1
                    || driverItem.CheckGet("LATE").ToInt() == 1)
                {
                    SetArrived(driverItem);
                    ShipmentGrid.SelectRowByKey(driverItem.CheckGet("SHIPMENTID"));
                    EditShipmentTime();
                    DriverGrid.LoadItems();
                }
                else
                {
                    SetArrived(driverItem);
                    DriverGrid.LoadItems();
                }
            }
        }

        private void SetArrived(Dictionary<string, string> driverItem)
        {
            var p = new Dictionary<string, string>();
            p.Add("FACTORY_ID", $"{FactoryId}");
            p.Add("DRIVER_ID", driverItem.CheckGet("ID"));
            p.Add("SHIPMENT_ID",driverItem.CheckGet("SHIPMENTID"));

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments/ShipmentKsh");
            q.Request.SetParam("Object", "TransportDriver");
            q.Request.SetParam("Action", "SetArrived");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                bool succesfullFlag = false;

                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        if (ds.Items[0].CheckGet("ID").ToInt() > 0)
                        {
                            succesfullFlag = true;
                        }
                    }
                }

                if (!succesfullFlag)
                {
                    var msg = "При регистрации водителя произошла ошибка. Пожалуйста, сообщите о проблеме.";
                    var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        //public void ChoiseShipmentDateTime()
        //{
        //    if (ArrivedDriverData != null && ArrivedDriverData.Count > 0)
        //    {
        //        bool checkDateTime = false;

        //        if (!string.IsNullOrEmpty(ArrivedDriverData.CheckGet("SHIPMENTID")))
        //        {
        //            if (
        //                ArrivedDriverData.CheckGet("UNSHIPPED").ToInt() == 1
        //                || ArrivedDriverData.CheckGet("LATE").ToInt() == 1
        //            )
        //            {
        //                checkDateTime = true;
        //            }
        //        }

        //        if (checkDateTime)
        //        {
        //            var i = new ShipmentDateTime();
        //            i.ShipmentType = ArrivedDriverData.CheckGet("SHIPMENTTYPE").ToInt();
        //            i.ShipmentId = ArrivedDriverData.CheckGet("SHIPMENTID").ToInt();
        //            i.ReceiverName = "ShipmentKshListSelectShipmentDateTime";
        //            i.Edit();
        //        }
        //        else
        //        {
        //            SetArrived();
        //        }
        //    }
        //}

        ///// <summary>
        ///// отпарвка запроса "отметить водителя как приехавшего"
        ///// </summary>
        ///// <param name="p"></param>
        //public async void SetArrived()
        //{
        //    var resume = false;

        //    if (ArrivedDriverData != null && ArrivedDriverData.Count > 0)
        //    {
        //        resume = true;
        //    }

        //    if (resume)
        //    {
        //        var p = new Dictionary<string, string>();

        //        var expected = false;
        //        var shipmentId = ArrivedDriverData.CheckGet("SHIPMENTID").ToInt();
        //        if (shipmentId != 0)
        //        {
        //            expected = true;
        //            p.CheckAdd("SHIPMENT_DATE", ArrivedDriverData.CheckGet("SHIPMENT_DATE"));
        //            p.CheckAdd("SHIPMENT_TIME", ArrivedDriverData.CheckGet("SHIPMENT_TIME"));
        //            p.CheckAdd("SET_DATETIME", "1");
        //        }

        //        p.CheckAdd("ID", ArrivedDriverData.CheckGet("ID"));
        //        p.CheckAdd("EXPECTED", expected.ToInt().ToString());
        //        p.CheckAdd("SHIPMENT_ID", shipmentId.ToString());
        //        p.CheckAdd("FACTORY_ID", $"{FactoryId}");

        //        var q = new LPackClientQuery();
        //        q.Request.SetParam("Module", "Shipments");
        //        q.Request.SetParam("Object", "TransportDriver");
        //        q.Request.SetParam("Action", "SetArrived");

        //        q.Request.SetParams(p);

        //        await Task.Run(() =>
        //        {
        //            q.DoQuery();
        //        });

        //        if (q.Answer.Status == 0)
        //        {
        //            bool succesfullFlag = false;

        //            var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
        //            if (result != null)
        //            {
        //                var ds = ListDataSet.Create(result, "ITEMS");
        //                if (ds != null && ds.Items != null && ds.Items.Count > 0)
        //                {
        //                    if (ds.Items.First().CheckGet("ID").ToInt() > 0)
        //                    {
        //                        succesfullFlag = true;
        //                    }
        //                }
        //            }

        //            if (succesfullFlag)
        //            {
        //                Refresh();
        //                Central.WM.RemoveTab($"AddDriver");
        //            }
        //            else
        //            {
        //                string msg = "При добавления водителя в список приехавших произошла ошибка. Пожалуйста, сообщите о проблеме.";
        //                var d = new DialogWindow($"{msg}", "Приезд водителя", "", DialogWindowButtons.OK);
        //                d.ShowDialog();
        //            }
        //        }
        //        else
        //        {
        //            q.ProcessError();
        //        }
        //    }

        //    ArrivedDriverData = new Dictionary<string, string>();
        //}

        public async void UnbindTerminal()
        {
            if (!string.IsNullOrEmpty(TerminalGrid.SelectedItem.CheckGet("TERMINAL_ID")))
            {
                if (!string.IsNullOrEmpty(TerminalGrid.SelectedItem.CheckGet("TRANSPORT_ID")))
                {
                    var resume = true;

                    if (resume)
                    {
                        var msg = "";
                        msg = $"{msg}Отвязать отгрузку от терминала?\n";

                        msg = $"{msg}Терминал: {TerminalGrid.SelectedItem.CheckGet("TERMINAL_NUMBER")}\n";
                        msg = $"{msg}Покупатель: {TerminalGrid.SelectedItem.CheckGet("BUYER_NAME")}\n";
                        msg = $"{msg}Водитель: {TerminalGrid.SelectedItem.CheckGet("DRIVER_NAME")}\n";

                        var d = new DialogWindow($"{msg}", "Отвязка от терминала", "", DialogWindowButtons.NoYes);
                        if (d.ShowDialog() != true)
                        {
                            resume = false;
                        }
                    }

                    if (resume)
                    {
                        string underloadMessage = "";

                        var p = new Dictionary<string, string>();
                        {
                            p.CheckAdd("SHIPMENT_ID", TerminalGrid.SelectedItem.CheckGet("TRANSPORT_ID"));
                        }

                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Shipments");
                        q.Request.SetParam("Object", "Position");
                        q.Request.SetParam("Action", "ListQuantityDeviation");

                        q.Request.SetParams(p);

                        q.DoQuery();

                        if (q.Answer.Status == 0)
                        {
                            var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                            if (result != null)
                            {
                                var ds = ListDataSet.Create(result, "ITEMS");
                                if (ds != null && ds.Items != null && ds.Items.Count > 0)
                                {
                                    foreach (var item in ds.Items)
                                    {
                                        if (item.CheckGet("QUANTITY_BY_CONSUMPTION").ToInt() < item.CheckGet("QUANTITY_BY_ORDER").ToInt())
                                        {
                                            underloadMessage = $"{underloadMessage}{Environment.NewLine}" +
                                                $"Позиция: {item.CheckGet("PRODUCT_NAME")}{Environment.NewLine}" +
                                                $"По заявке: {item.CheckGet("QUANTITY_BY_ORDER").ToInt()} Погружено: {item.CheckGet("QUANTITY_BY_CONSUMPTION").ToInt()}{Environment.NewLine}" +
                                                $"Отклонение: {item.CheckGet("QUANTITY_BY_ORDER").ToInt() - item.CheckGet("QUANTITY_BY_CONSUMPTION").ToInt()}шт. ({item.CheckGet("QUANTITY_PERCENTAGE_DEVIATION").ToDouble()}%){Environment.NewLine}";
                                        }
                                    }
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(underloadMessage))
                        {
                            underloadMessage = $"Внимание, остались недогруженные позиции! Вы хотите продолжить?{Environment.NewLine}" +
                                $"{underloadMessage}";
                            var d = new DialogWindow($"{underloadMessage}", "Отвязка от терминала", "", DialogWindowButtons.NoYes);
                            if (d.ShowDialog() != true)
                            {
                                resume = false;
                            }
                        }
                    }

                    if (resume)
                    {
                        var p = new Dictionary<string, string>();
                        {
                            p.CheckAdd("ID", TerminalGrid.SelectedItem.CheckGet("TERMINAL_ID"));
                            p.CheckAdd("IDTS", TerminalGrid.SelectedItem.CheckGet("TRANSPORT_ID"));
                        }

                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Shipments");
                        q.Request.SetParam("Object", "Shipment");
                        q.Request.SetParam("Action", "UnbindTerminal");

                        q.Request.SetParams(p);

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
                                var ds = ListDataSet.Create(result, "ITEMS");
                                if (ds != null && ds.Items != null && ds.Items.Count > 0)
                                {
                                    if (ds.Items.First().CheckGet("ID").ToInt() > 0)
                                    {
                                        succesfullFlag = true;
                                    }
                                }
                            }

                            if (!succesfullFlag)
                            {
                                string msg = "При отвязывании отгрузки к терминалу произошла ошибка. Пожалуйста, сообщите о проблеме.";
                                var d = new DialogWindow($"{msg}", "Отвязка от терминала", "", DialogWindowButtons.OK);
                                d.ShowDialog();
                            }
                            else
                            {
                                Refresh();
                            }
                        }
                        else
                        {
                            q.ProcessError();
                        }
                    }
                }
                else
                {
                    string msg = "Не найдена отгрузка, привязанная к выбранному терминалу";
                    var d = new DialogWindow($"{msg}", "Отвязка от терминала", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                string msg = "Не выбран терминал";
                var d = new DialogWindow($"{msg}", "Отвязка от терминала", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        public void BindTerminal()
        {
            var resume = true;

            if (resume)
            {
                if (
                    ShipmentGrid.SelectedItem == null
                    || TerminalGrid.SelectedItem == null
                )
                {
                    var msg = "Выберите отгрузку и свободный терминал.";
                    var d = new DialogWindow($"{msg}", "Привязка к терминалу", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                    resume = false;
                }
            }

            // Если не отмечено время прибытия водителя, то не даём ставить отгрузку на терминал
            if (string.IsNullOrEmpty(ShipmentGrid.SelectedItem.CheckGet("DRIVERARRIVEDATETIME")))
            {
                string msg = "Нельзя привязать к терминалу отгрузку, для которой не отмечен приезд водителя.";
                var d = new DialogWindow($"{msg}", "Привязка к терминалу", "", DialogWindowButtons.OK);
                d.ShowDialog();
                resume = false;
                return;
            }

            var id = 0;
            var terminalId = 0;

            if (resume)
            {
                id = ShipmentGrid.SelectedItem["ID"].ToInt();
                if (!(id > 0))
                {
                    resume = false;
                }
            }

            if (resume)
            {
                terminalId = TerminalGrid.SelectedItem["TERMINAL_ID"].ToInt();
                if (!(terminalId > 0))
                {
                    resume = false;
                }
            }

            if (resume)
            {
                if (!CheckShipmentAtTerminal(id))
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var bindTerminal = new BindTerminal();
                bindTerminal.ShipmentId = id;
                bindTerminal.TerminalId = terminalId;
                bindTerminal.ShipmentType = ShipmentGrid.SelectedItem.CheckGet("PRODUCTIONTYPE").ToInt();
                bindTerminal.Edit();
            }
        }

        /// <summary>
        /// Проверяем, стоит ли отгрузка на терминале.
        /// Если стоит, то запрашиваем подтвержение на выполнение последующих действий.
        /// </summary>
        /// <param name="transportId"></param>
        /// <returns></returns>
        public bool CheckShipmentAtTerminal(int transportId)
        {
            bool result = false;

            var p = new Dictionary<string, string>();
            p.Add("TRANSPORT_ID", $"{transportId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Terminal");
            q.Request.SetParam("Action", "CheckShipment");

            q.Request.SetParams(p);

            q.Request.Timeout = 10000;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var queryResult = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (queryResult != null)
                {
                    var ds = ListDataSet.Create(queryResult, "ITEMS");
                    if (ds != null && ds.Items != null)
                    {
                        if (ds.Items.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(ds.Items.First().CheckGet("TERMINAL_ID")))
                            {
                                string msg = "Отгрузка уже стоит на терминале. Вы хотите продолжить?";
                                var d = new DialogWindow($"{msg}", "Приезд водителя", "", DialogWindowButtons.NoYes);
                                if (d.ShowDialog() == true)
                                {
                                    result = true;
                                }
                            }
                            else
                            {
                                result = true;
                            }
                        }
                        else
                        {
                            result = true;
                        }
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            return result;
        }

        private async void SetShowPalletFlag(int type)
        {
            var p = new Dictionary<string, string>();
            {
                p.Add("TERMINAL_ID", TerminalGrid.SelectedItem.CheckGet("TERMINAL_ID"));
                p.Add("SHOW_ALL_PALLET_FLAG", $"{type}");
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Terminal");
            q.Request.SetParam("Action", "UpdateShowAllPalletFlag");

            q.Request.SetParams(p);

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
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        if (ds.Items.First().CheckGet("TERMINAL_ID").ToInt() > 0)
                        {
                            TerminalGrid.LoadItems();
                        }
                    }
                }
            }
        }

        private async void RemoveShipmentBlockFlag()
        {
            var p = new Dictionary<string, string>();
            {
                p.Add("TERMINAL_ID", TerminalGrid.SelectedItem.CheckGet("TERMINAL_ID"));
                p.Add("SHPMENT_BLOCKED_FLAG", "0");
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Terminal");
            q.Request.SetParam("Action", "SetShpmentBlockedFlag");

            q.Request.SetParams(p);

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
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        if (ds.Items.First().CheckGet("TERMINAL_ID").ToInt() > 0)
                        {
                            TerminalGrid.LoadItems();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Вывод печатной формы со списком водителей
        /// </summary>
        private void MakeDriverReport()
        {
            var reporter = new DriverReporter();
            reporter.Drivers = DriverDataSet.Items;
            reporter.MakeDriverReport();
        }

        /// <summary>
        /// Установка активных строк в отгрузках и терминалах для выбранного водителя
        /// </summary>
        public void ShowShipment()
        {
            ShipmentGrid.SelectRowByKey($"{DriverGrid.SelectedItem.CheckGet("TRANSPORTID").ToInt()}");

            if (!string.IsNullOrEmpty(DriverGrid.SelectedItem.CheckGet("TERMINALNUMBER")))
            {
                TerminalGrid.SelectRowByKey(DriverGrid.SelectedItem.CheckGet("TERMINALNUMBER"));
            }
        }

        /// <summary>
        /// Открытие вкладки с настройками склада
        /// </summary>
        private void ShowStockSettings()
        {
            var settings = new ShipmentSettings(2);
            settings.ReceiverTabName = this.FrameName;
            settings.Edit();
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            var i = new PrintingInterface();
        }

        private void ShipmentCountToPrintButton_Click(object sender, RoutedEventArgs e)
        {
            ShowShipmentToPrint();
        }

        private void Types_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ShipmentGrid.UpdateItems();
        }

        private void DeliveryTypes_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ShipmentGrid.UpdateItems();
        }

        private void ShipmentTypes_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ShipmentGrid.UpdateItems();
        }

        private void HideCompleteCheckbox_Click(object sender, RoutedEventArgs e)
        {
            ShipmentGrid.UpdateItems();
        }

        private void PrintProxyDocsButton_Click(object sender, RoutedEventArgs e)
        {
            PrintProxy();
        }

        private void PrintDriverBootCardButton_Click(object sender, RoutedEventArgs e)
        {
            PrintBootcard();
        }

        private void PrintShipmentOrderBootCardButton_Click(object sender, RoutedEventArgs e)
        {
            PrintShipmenttask();
        }

        private void PrintMapButton_Click(object sender, RoutedEventArgs e)
        {
            PrintStockmap();
        }

        private void PrintRouteMapsButton_Click(object sender, RoutedEventArgs e)
        {
            PrintRoutemap();
        }

        private void ShowAllButton_Click(object sender, RoutedEventArgs e)
        {
            ShowAll();
        }

        private void PrintAllButton_Click(object sender, RoutedEventArgs e)
        {
            PrintAll();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsBurgerMenu.IsOpen = true;
        }

        private void BurgerStockSettings_Click(object sender, RoutedEventArgs e)
        {
            ShowStockSettings();
        }
    }
}
