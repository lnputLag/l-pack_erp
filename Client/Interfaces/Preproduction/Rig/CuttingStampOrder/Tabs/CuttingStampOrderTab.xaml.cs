using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Список заказов штанцформ
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class CuttingStampOrderTab : ControlBase
    {
        public CuttingStampOrderTab()
        {
            InitializeComponent();

            ControlTitle = "Заказ штанцформ";
            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/cutting_stamp_order";
            RoleName = "[erp]rig_cutting_stamp_order";

            SetDefaults();

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
                if (msg.ReceiverGroup == "Preproduction/Rig")
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
                        var selectProductFrame = new CuttingStampOrderSelectTechcard();
                        selectProductFrame.ReceiverName = ControlName;
                        selectProductFrame.FactoryId = FactoryId;
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
                            int techcardId = Grid.SelectedItem.CheckGet("TECHCARD_ID").ToInt();
                            int repairKitFlag = Grid.SelectedItem.CheckGet("REPAIR_KIT_FLAG").ToInt();
                            if (repairKitFlag > 0)
                            {
                                techcardId = -1;
                            }

                            var stampOrderFrame = new CuttingStampOrder();
                            stampOrderFrame.ReceiverName = ControlName;
                            stampOrderFrame.FactoryId = FactoryId;
                            stampOrderFrame.Edit(id, techcardId);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = true;
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
                            DeleteOrder();
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        //Удалять можно только незаказанные заяаки
                        if (row.CheckGet("STATUS_ID").ToInt() == 4)
                        {
                            result = true;
                        }
                        return result;
                    },

                });
                Commander.Add(new CommandItem()
                {
                    Name = "createrepairkit",
                    Title = "Заказать ремкомплект",
                    Group = "operations",
                    MenuUse = true,
                    ButtonUse = false,
                    Description = "Заказать ремкомплект",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var stampOrderFrame = new CuttingStampOrder();
                        stampOrderFrame.ReceiverName = ControlName;
                        stampOrderFrame.FactoryId = FactoryId;
                        stampOrderFrame.CreateRepairKit();
                    },
                    CheckEnabled = () =>
                    {
                        var result = true;
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "createbackward",
                    Title = "Возврат штанцформы",
                    Group = "operations",
                    MenuUse = true,
                    ButtonUse = false,
                    Description = "Создать заявку на возврат из заявки на изготовление",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        CreateBackward();
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (row != null)
                        {
                            int priority = row.CheckGet("STAMP_STATUS_PRIORITY").ToInt();
                            // Возвращать можно только переданные штанцформы
                            if (priority == 5)
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
                        //Нельзя ставить у отмененных заявок, можно у незаказанных или в пути
                        if (!row.CheckGet("CANCELED_FLAG").ToBool() && row.CheckGet("STATUS_ID").ToInt().ContainsIn(4, 6))
                        {
                            result = true;
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "set_status_on_way",
                    Title = "В пути",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Group = "statuses",
                    MenuUse = true,
                    ButtonUse = false,
                    Action = () =>
                    {
                        SetStatus(6);
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
                    Name = "set_status_received",
                    Title = "Получить",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Group = "statuses",
                    MenuUse = true,
                    ButtonUse = false,
                    Action = () =>
                    {
                        //SetStatus(7);
                        SetOrderReceived();
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        int status = row.CheckGet("STATUS_ID").ToInt();
                        if (status.ContainsIn(3, 5, 6))
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
                Commander.Add(new CommandItem()
                {
                    Name = "print",
                    Title = "Печать",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Group = "print",
                    MenuUse = false,
                    ButtonUse = true,
                    ButtonName = "PrintButton",
                    Action = () =>
                    {
                        var orderListReport = new CuttingStampOrderReport();
                        orderListReport.OrderList = Grid.Items;
                        orderListReport.PrintColumns = PrintColumns;
                        orderListReport.MakeExcel();
                    },
                    CheckEnabled = () =>
                    {
                        var result = true;
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "toexcel",
                    Title = "В Excel",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Group = "print",
                    MenuUse = false,
                    ButtonUse = true,
                    ButtonName = "ToExcelButton",
                    Action = () =>
                    {
                        Grid.ItemsExportExcel();
                    },
                    CheckEnabled = () =>
                    {
                        var result = true;
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "opendrawing",
                    Title = "Открыть чертеж",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Group = "files",
                    MenuUse = true,
                    ButtonUse = false,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id > 0)
                        {
                            string drawingFile = Grid.SelectedItem.CheckGet("DRAWING_FILE");
                            Central.OpenFile(drawingFile);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (row != null)
                        {
                            string drawingFile = row.CheckGet("DRAWING_FILE");
                            if (File.Exists(drawingFile))
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "opentechcard",
                    Title = "Открыть техкарту",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Group = "files",
                    MenuUse = true,
                    ButtonUse = false,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id > 0)
                        {
                            string drawingFile = Grid.SelectedItem.CheckGet("TECHCARD_PATH");
                            Central.OpenFile(drawingFile);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (row != null)
                        {
                            string drawingFile = row.CheckGet("TECHCARD_PATH");
                            if (File.Exists(drawingFile))
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "openorderfolder",
                    Title = "Открыть папку заказа",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Group = "files",
                    MenuUse = true,
                    ButtonUse = false,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id > 0)
                        {
                            string drawingFile = Grid.SelectedItem.CheckGet("DRAWING_FILE");
                            string folder = Path.GetDirectoryName(drawingFile);
                            Central.OpenFolder(folder);
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
                                string folder = Path.GetDirectoryName(drawingFile);
                                if (Directory.Exists(folder))
                                {
                                    result = true;
                                }
                            }
                        }

                        return result;
                    },
                });
            }
            Commander.Init(this);
        }

        /// <summary>
        /// Идентификатор производственной площадки: 1 - Липецк, 2 - Кашира
        /// </summary>
        public int FactoryId { get; set; }
        /// <summary>
        /// Данные для типов линий штанцформ
        /// </summary>
        private ListDataSet MachineDS { get; set; }

        public List<DataGridHelperColumn> PrintColumns { get; set; }

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
                    case "savepd":
                        if (obj.ContextObject != null)
                        {
                            var v = (Dictionary<string, string>)obj.ContextObject;
                            SaveReceivedPd(v);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Установка значений по умолчанию. Загрузка справочников
        /// </summary>
        public void SetDefaults()
        {
            FactoryId = 1;
            MachineDS = new ListDataSet();
            MachineDS.Init();

            PrintColumns = new List<DataGridHelperColumn>();

            DeliveryDate.IsEnabled = false;
            DeliveryDate.Text = "";
            DateFrom.Text = DateTime.Now.AddDays(-30).ToString("dd.MM.yyyy");
            DateTo.Text = DateTime.Now.ToString("dd.MM.yyyy");

            LoadRef();
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
                    Width2=10,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header="Плательщик",
                    Path="BUYER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=30,
                },
                new DataGridHelperColumn
                {
                    Header="Номер заявки",
                    Path="ORDER_NUM",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
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
                    Header="Ремкомплект",
                    Path="REPAIR_KIT_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Hidden=true,
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
                    Width2=10,
                    Format="dd.MM HH:mm",
                },
                new DataGridHelperColumn
                {
                    Header="Дата получения",
                    Path="RECEIPT_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=12,
                    Format="dd.MM.yyyy HH:mm",
                },
                new DataGridHelperColumn
                {
                    Header="Поставщик",
                    Path="SUPPLIER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=30,
                },
                new DataGridHelperColumn
                {
                    Header="Перезаказ",
                    Path="REORDER_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="Штанцформа",
                    Path="STAMP_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=30,
                },
                new DataGridHelperColumn
                {
                    Header="FEFCO",
                    Path="FEFCO",
                    ColumnType=ColumnTypeRef.String,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header="PD",
                    Path="PD",
                    ColumnType=ColumnTypeRef.Double,
                    Width2=7,
                    Format="N0",
                },
                new DataGridHelperColumn
                {
                    Header="Сотрудник",
                    Path="EMPLOYEE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Техкарта",
                    Path="TK_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=30,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание",
                    Path="NOTE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Доработка",
                    Path="MODIFICATION_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="Ид техкарты",
                    Path="TECHCARD_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Ручки",
                    Path="HOLE_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Hidden=true,
                },
            };

            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("ID");
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

            PrintColumns.Add(columns[2]);  //Плательщик
            PrintColumns.Add(columns[4]);  //Номер заказа
            PrintColumns.Add(columns[14]);  //Техкарта

            Grid.SearchText = GridSearch;
            Grid.Toolbar = GridToolbar;
            Grid.Commands = Commander;
            // Раскраска строк
            Grid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                // Цвета фона строк
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";
                        int statusId = row.CheckGet("STATUS_ID").ToInt();

                        switch (statusId)
                        {
                            case 5:
                            case 3:
                                color = HColor.Blue;
                                break;
                            case 6:
                                color = HColor.VioletDark;
                                break;
                            case 7:
                                color = HColor.Green;
                                break;
                        }
                        
                        /*
                        if (statusId == 5 || statusId == 6)
                        {
                            color = HColor.Blue;
                        }
                        else if (statusId == 7)
                        {
                            color = HColor.Green;
                        }
                        */

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
                // Цвета шрифта строк
                {
                    DataGridHelperColumn.StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";
                        int statusId = row.CheckGet("STATUS_ID").ToInt();

                        if (statusId == 6)
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
            };


            Grid.OnLoadItems = LoadItems;
            Grid.OnFilterItems = FilterItems;
            Grid.Init();
        }

        /// <summary>
        /// Загрузка справочников
        /// </summary>
        private async void LoadRef()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "CuttingStampOrder");
            q.Request.SetParam("Action", "GetRef");

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
                    MachineDS = ListDataSet.Create(result, "MACHINE");

                    var factoryDS = ListDataSet.Create(result, "FACTORY");
                    Factory.Items = factoryDS.GetItemsList("ID", "NAME");
                    Factory.SetSelectedItemByKey(FactoryId.ToString());
                }
            }
        }

        /// <summary>
        /// Получение данных и загрузка в таблицу
        /// </summary>
        private async void LoadItems()
        {
            bool resume = true;

            var f = DateFrom.Text.ToDateTime();
            var t = DateTo.Text.ToDateTime();

            if (DateTime.Compare(f, t) > 0)
            {
                const string msg = "Дата начала должна быть меньше даты окончания.";
                var d = new DialogWindow($"{msg}", this.ControlTitle);
                d.ShowDialog();
                resume = false;
            }

            if (resume)
            {
                bool allOrders = (bool)ShowAllCheckBox.IsChecked;

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Rig");
                q.Request.SetParam("Object", "CuttingStampOrder");
                q.Request.SetParam("Action", "List");
                q.Request.SetParam("FACTORY_ID", FactoryId.ToString());
                q.Request.SetParam("ALL_ORDERS", allOrders ? "1" : "0");
                q.Request.SetParam("DATE_FROM", DateFrom.Text);
                q.Request.SetParam("DATE_TO", DateTo.Text);

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
                        var ds = ListDataSet.Create(result, "STAMP_ORDER_LIST");
                        Grid.UpdateItems(ds);
                    }
                }
            }
        }

        /// <summary>
        /// Фмльтрация строк грида
        /// </summary>
        private void FilterItems()
        {
            if (Grid.Items != null)
            {
                if (Grid.Items.Count > 0)
                {
                    bool doFilteringByMachine = false;
                    bool doFilteringByDelivery = false;

                    int machineId = Machine.SelectedItem.Key.ToInt();
                    if (machineId > 0)
                    {
                        doFilteringByMachine = true;
                    }

                    if ((bool)DeliveryDateCheckBox.IsChecked)
                    {
                        doFilteringByDelivery = true;
                    }

                    if (doFilteringByMachine || doFilteringByDelivery)
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in Grid.Items)
                        {
                            bool includeByMachine = true;
                            bool includeByDelivery = true;

                            //Фильтр по станкам
                            if (doFilteringByMachine)
                            {
                                includeByMachine = false;
                                int rowMachineId = row.CheckGet("MACHINE_ID").ToInt();
                                if (machineId == 2)
                                {
                                    //Исключаем плоские ШФ: 9 и 16
                                    if ((rowMachineId != 9) && (rowMachineId != 16))
                                    {
                                        includeByMachine = true;
                                    }
                                }
                                else if (rowMachineId == machineId)
                                {
                                    includeByMachine = true;
                                }
                            }

                            //Фильтр по дате доставки
                            if (doFilteringByDelivery)
                            {
                                includeByDelivery = false;
                                var a = row.CheckGet("DELIVERY_DTTM");

                                var deliveryDate = row.CheckGet("DELIVERY_DTTM").ToDateTime("dd.MM.yyyy HH:mm:ss").Date;
                                var filterDate = DeliveryDate.Text.ToDateTime("dd.MM.yyyy");

                                if (DateTime.Compare(deliveryDate, filterDate) == 0)
                                {
                                    includeByDelivery = true;
                                }
                            }

                            if (includeByMachine && includeByDelivery)
                            {
                                items.Add(row);
                            }
                        }

                        Grid.Items = items;
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
            int supplierId = Grid.SelectedItem.CheckGet("SUPPLIER_ID").ToInt();
            if ((newStatus == 5) && (supplierId == 0))
            {
                //Нет поставщика, значит штанцформа заказана клиентом
                newStatus = 3;
            }

            if (id > 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Rig");
                q.Request.SetParam("Object", "CuttingStampOrder");
                q.Request.SetParam("Action", "SetStatus");
                q.Request.SetParam("ID", id.ToString());
                q.Request.SetParam("STATUS", newStatus.ToString());
                q.Request.SetParam("OLD_STATUS", Grid.SelectedItem.CheckGet("STATUS_ID"));

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
        /// Удаление заявки
        /// </summary>
        private async void DeleteOrder()
        {
            bool resume = false;
            var dw = new DialogWindow("Вы действительно хотите удалить заявку на штанцформу?", "Удаление заявки", "", DialogWindowButtons.NoYes);
            if ((bool)dw.ShowDialog())
            {
                if (dw.ResultButton == DialogResultButton.Yes)
                {
                    resume = true;
                }
            }
            
            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Rig");
                q.Request.SetParam("Object", "CuttingStampOrder");
                q.Request.SetParam("Action", "Delete");
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
                        if (result.ContainsKey("ITEM"))
                        {
                            Grid.LoadItems();
                        }
                    }
                }
                else if (q.Answer.Error.Code == 145)
                {
                    q.ProcessError();
                }
            }
        }

        private void MachineListUpdate()
        {
            var dc = new Dictionary<string, string>()
            {
                { "0", "Все" },
                { "2", "Роторные" },
            };
            foreach (var item in MachineDS.Items)
            {
                if (item.CheckGet("FACTORY_ID").ToInt() == FactoryId)
                {
                    dc.Add(item.CheckGet("ID"), item.CheckGet("NAME"));
                }
            }

            Machine.Items = dc;
            Machine.SetSelectedItemByKey("0");
        }

        private void UseDeliveryDate()
        {
            if ((bool)DeliveryDateCheckBox.IsChecked)
            {
                DeliveryDate.IsEnabled = true;
                DeliveryDate.Text = DateTime.Today.ToString("dd.MM.yyyy");
            }
            else
            {
                DeliveryDate.IsEnabled = false;
                DeliveryDate.Text = "";
            }
            Grid.UpdateItems();
        }

        /// <summary>
        /// Обработка приемки заявки
        /// </summary>
        private void SetOrderReceived()
        {
            if (Grid.SelectedItem != null)
            {
                bool repaireKitFlag = Grid.SelectedItem.CheckGet("REPAIR_KIT_FLAG").ToBool();
                bool holeFlag = Grid.SelectedItem.CheckGet("HOLE_FLAG").ToBool();
                int pdValue = Grid.SelectedItem.CheckGet("PD").ToInt();
                var stampId = Grid.SelectedItem.CheckGet("STAMP_ID");

                if (repaireKitFlag || holeFlag || (pdValue > 0))
                {
                    // Для ремкомплекта и ручек PD не заполняется
                    SetStatus(7);
                }
                else
                {
                    var v = new Dictionary<string, string>
                    {
                        { "ID", stampId },
                        { "PD", "0" },
                    };

                    var pdEditWin = new CuttingStampPdEdit();
                    pdEditWin.ReceiverName = ControlName;
                    pdEditWin.Edit(v);
                }
            }
        }

        /// <summary>
        /// Сохранение занчения PD штанцформы перед приемкой
        /// </summary>
        /// <param name="data"></param>
        private async void SaveReceivedPd(Dictionary<string, string> data)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "CuttingStamp");
            q.Request.SetParam("Action", "SavePd");
            q.Request.SetParams(data);

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
                        SetStatus(7);
                    }
                }
            }
            else if (q.Answer.Error.Code == 145)
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Создание заявки на возврат штанцформы от клиента
        /// </summary>
        private async void CreateBackward()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "CuttingStampOrder");
            q.Request.SetParam("Action", "CreateBackward");
            q.Request.SetParam("ID", Grid.SelectedItem.CheckGet("ID"));

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
                        Grid.LoadItems();
                    }
                }
            }
            else if (q.Answer.Error.Code == 145)
            {
                q.ProcessError();
            }
        }

        private void Factory_SelectedItemChanged(System.Windows.DependencyObject d, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            FactoryId = Factory.SelectedItem.Key.ToInt();
            MachineListUpdate();
            
            Grid.LoadItems();
        }

        private void Machine_SelectedItemChanged(System.Windows.DependencyObject d, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void ShowAllCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void DateChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void DeliveryDateCheckBox_Click(object sender, RoutedEventArgs e)
        {
            UseDeliveryDate();
        }
    }
}
