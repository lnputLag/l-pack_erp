using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using NPOI.SS.Formula.Functions;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.MoldedContainer
{
    /// <summary>
    /// производственные задания на литую тару
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2024-07-10</released>
    /// <changed>2024-09-16</changed>
    public partial class ProductionTaskTab : ControlBase
    {
        /*
            ---
            OrderGrid
            ---
            ProductionTaskGrid
            
         */

        /// <summary>
        /// данные из выбранной в гриде строки
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        public FormHelper Form { get; set; }

        /// 
        /// <summary>
        /// 
        /// </summary>
        public ProductionTaskTab()
        {
            InitializeComponent();

            ControlSection = "production_task";
            RoleName = "[erp]molded_contnr_productn_task";
            ControlTitle = "ПЗ на ЛТ";
            DocumentationUrl = "/doc/l-pack-erp/production/molded_container/production_task";

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

            OnLoad = () =>
            {
                FormInit();
                OrderGridInit();
                ProductionTaskGridInit();
            };

            OnUnload = () =>
            {
                OrderGrid.Destruct();
                ProductionTaskGrid.Destruct();
            };

            OnFocusGot = () =>
            {
                OrderGrid.ItemsAutoUpdate = true;
                OrderGrid.Run();
                ProductionTaskGrid.ItemsAutoUpdate = true;
                ProductionTaskGrid.Run();
            };

            OnFocusLost = () =>
            {
                OrderGrid.ItemsAutoUpdate = false;
                ProductionTaskGrid.ItemsAutoUpdate = false;
            };

            OnNavigate = () =>
            {
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
                    Commander.SetCurrentGroup("common");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "order_refresh",
                            Enabled = true,
                            Title = "Обновить",
                            Description = "Обновить данные",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "OrderGridRefreshButton",
                            Action = () =>
                            {
                                OrderGrid.LoadItems();
                            },
                        });
                    }

                    Commander.SetCurrentGroup("item");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "order_create",
                            Title = "Создать ПЗ",
                            Description = "",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "ProductionTaskGridCreate2Button",
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            HotKey = "Return|DoubleCLick",
                            Action = () =>
                            {
                                var id = SelectedItem.CheckGet("ORDER_ID").ToInt();
                                var id2 = SelectedItem.CheckGet("GOODS_ID").ToInt();
                                var note = SelectedItem.CheckGet("PRODUCTION_NOTE");
                                
                                var h = new ProductionTaskForm();
                                h.CreateMode = 1;
                                h.OrderId = id;
                                h.ProductionTaskNote = note;

                                h.GoodsId = id2;
                                h.IdorderDates = SelectedItem.CheckGet("ORDER_POSITION_ID").ToInt();
                                h.TaskQuantity = SelectedItem.CheckGet("QTY_FOR_TASK").ToInt();

                                if (SelectedItem.CheckGet("ORDER_FLAG").ToInt() == 1)
                                {
                                    h.OrderReceiptData = SelectedItem.CheckGet("ORDER_RECEIPT_DT");
                                }

                                h.Init("create", "0");
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;

                                var k = OrderGrid.GetPrimaryKey();
                                var row = OrderGrid.SelectedItem;
                                if (row.CheckGet("PRODUCTION_TASK_ID").ToInt() == 0)
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });
                    }
                    
                    Commander.SetCurrentGroup("edit_mode");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "edit_mode_on",
                            Enabled = true,
                            Title = "Разрешить редактирование",
                            MenuUse = true,
                            AccessLevel = Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var orderId = OrderGrid.SelectedItem.CheckGet("ORDER_ID");
                                ToggleEditModeOrder(orderId, "1");
                            },
                        });
                        
                        Commander.Add(new CommandItem()
                        {
                            Name = "edit_mode_off",
                            Enabled = true,
                            Title = "Запретить редактирование",
                            MenuUse = true,
                            AccessLevel = Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var orderId = OrderGrid.SelectedItem.CheckGet("ORDER_ID");
                                ToggleEditModeOrder(orderId, "0");
                            },
                        });

                        Commander.Add(new CommandItem()
                        {
                            Name = "edit_note_order",
                            Enabled = true,
                            Title = "Изменить примечание",
                            ButtonUse = true,
                            ButtonName = "EditNote",
                            AccessLevel = Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var row = OrderGrid.SelectedItem;
                                var id = row.CheckGet("ORDER_POSITION_ID").ToInt();
                                var note = row.CheckGet("NOTE");
                                
                                var editFrame = new EditNoteFrame();
                                editFrame.Edit(note, id);
                            },
                            CheckEnabled = () => OrderGrid.SelectedItem.CheckGet("PRODUCTION_TASK_ID").ToInt() != 0,
                        });
                    }
                }

                Commander.SetCurrentGridName("ProductionTaskGrid");
                {
                    Commander.SetCurrentGroup("common");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "production_task_refresh",
                            Enabled = true,
                            Title = "Обновить",
                            Description = "Обновить данные",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "ProductionTaskGridRefreshButton",
                            Action = () =>
                            {
                                ProductionTaskGrid.LoadItems();
                            },
                        });
                    }

                    Commander.SetCurrentGroup("item");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "production_task_create",
                            Title = "Создать",
                            Description = "",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "ProductionTaskGridCreateButton",
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var id = SelectedItem.CheckGet("ORDER_ID").ToInt();
                                var id2 = SelectedItem.CheckGet("GOODS_ID").ToInt();
                                var note = SelectedItem.CheckGet("NOTE");
                                
                                var h = new ProductionTaskForm();
                                
                                h.OrderId = id;
                                h.GoodsId = id2;
                                h.ProductionTaskNote = note;
                                
                                h.CreateMode = 2;
                                h.IdorderDates = SelectedItem.CheckGet("ORDER_POSITION_ID").ToInt();
                                h.TaskQuantity = SelectedItem.CheckGet("QTY_FOR_TASK").ToInt();
                                h.Init("create", "0");
                            },
                            CheckEnabled = () =>
                            {
                                var result = true;
                                return result;
                            },
                        });

                        Commander.Add(new CommandItem()
                        {
                            Name = "production_task_untie",
                            Title = "Отвязать заявку от ПЗ",
                            MenuUse = true,
                            Enabled = true,
                            ButtonUse = true,
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var idTask = ProductionTaskGrid.SelectedItem.CheckGet("TASK_ID2").ToInt();
                                var dialog = new DialogWindow($"Отвязать заявку от производственного задания - {idTask}?", "Производственное задание", "", DialogWindowButtons.YesNo);
                                dialog.ShowDialog();

                                if (dialog.DialogResult == true)
                                {
                                    UntieOrderFromProductionTask(idTask.ToString());
                                }
                            },
                        });

                        Commander.Add(new CommandItem()
                        {
                            Name = "production_task_edit",
                            Title = "Изменить",
                            Description = "",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "ProductionTaskGridEditButton",
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            HotKey = "Return|DoubleCLick",
                            Action = () =>
                            {
                                var id = ProductionTaskGrid.SelectedItem.CheckGet("TASK_ID").ToInt();
                                var note = ProductionTaskGrid.SelectedItem.CheckGet("PRODUCTION_NOTE");
                                var h = new ProductionTaskForm();
                                h.CreateMode = 0;
                                h.ProductionTaskNote = note;
                                h.StatusTask = ProductionTaskGrid.SelectedItem.CheckGet("TASK_STATUS_ID").ToInt();
                                h.Init("edit", id.ToString());
                            },
                            CheckEnabled = () =>
                            {
                                if (ProductionTaskGrid.SelectedItem.CheckGet("TASK_STATUS_ID").ToInt() > 5)
                                {
                                    return false;
                                }
                                
                                return true;
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "production_task_delete",
                            Title = "Удалить",
                            Description = "",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "ProductionTaskGridDeleteButton",
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            HotKey = "",
                            Action = () =>
                            {
                                ProductionTaskDelete();
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;

                                if (ProductionTaskGrid.SelectedItem.CheckGet("TASK_STATUS_ID").ToInt() <= 3)
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });

                    }
                }

                Commander.Init(this);
            }

        }

        public FormHelper DepartmentForm { get; set; }
        private List<Dictionary<string, string>> UniqIdMachine { get; set; }
        private string MachineId { get; set; } = "-1";

        public void OrderGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="GOODS_CODE",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },

                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="ORDER_ID",
                    Description="Идентификатор заявки (nsthet)",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible = false,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header="Дата отгрузки",
                    Path="ORDER_SHIPMENT_DATE",
                    Description="",
                    ColumnType=ColumnTypeRef.DateTime,
                    Format="dd.MM.yyyy",
                    Width2=11,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="GOODS_NAME",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=44,
                },
                new DataGridHelperColumn
                {
                    Header="Количество, шт",
                   // Header="ORDER_QUANTITY",
                    Path="ORDER_QUANTITY",
                    Description="количество изделий в заявке",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=12,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("ORDER_QUANTITY").ToInt() != row.CheckGet("PRODUCTION_TASK_QUANTITY").ToInt())
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
                    }
                },
                new DataGridHelperColumn
                {
                    Header="В ПЗ, шт",
                    Path="PRODUCTION_TASK_QUANTITY",
                    Description="количество изделий в ПЗ",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="По заявке, шт",
                    //Header="BALANCE_QUANTITY_ORDER",
                    Path="BALANCE_QUANTITY_ORDER",
                    Description="количество изделий по заявке",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header = "По заявке по ПЗ",
                    Path = "BALANCE_QUANTITY_ORDER_TASK",
                    Description = "количество изделий по заявке по ПЗ",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 12,
                },
                new DataGridHelperColumn
                {
                    Header="На складе, шт",
                    Path="BALANCE_QUANTITY_STOCK",
                    Description="количество изделий на складе",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание производства",
                    Path="NOTE",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn
                {
                    Header="ИД изделия",
                    Path="GOODS_ID",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="ИД Позиции заявки",
                    Path="ORDER_POSITION_ID",
                    Description="Идентификатор позиции в заявке (IdOrderDates)",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=14,
                },
                new DataGridHelperColumn
                {
                  Header = "ПР",
                  Path = "EDIT_MODE",
                  Description = "Право на редактирование",
                  ColumnType = ColumnTypeRef.Boolean,
                  Width2 = 5,
                },
                new DataGridHelperColumn
                {
                    Header="QTY_FOR_TASK",
                    Path="QTY_FOR_TASK",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="TASK_DONE_IS",
                    Path="TASK_DONE_IS",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="PRODUCTION_TASK_ID",
                    Path="PRODUCTION_TASK_ID",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="Заказ этикетки",
                    Path="ORDER_FLAG",
                    Description="",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=7,
                    // Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="Дт.доставки этикетки",
                    Path="ORDER_RECEIPT_DT",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=13,
                    // Visible=false
                },
            };
            OrderGrid.SetColumns(columns);
            OrderGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    {
                        StylerTypeRef.BackgroundColor,
                        (Dictionary<string, string> row) =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            //var p = row.CheckGet("PRODUCTION_TASK_ID").ToInt();
                            //var t =row.CheckGet("TASK_DONE_IS").ToInt();
                            //var o = row.CheckGet("ORDER_QUANTITY").ToInt();
                            //var b =  row.CheckGet("BALANCE_QUANTITY_ORDER").ToInt();
                                                     
                                // Если ORDER_QUANTITY <= BALANCE_QUANTITY_ORDER  то выделяем строку зеленым
                                if (row.CheckGet("ORDER_QUANTITY").ToInt() <= row.CheckGet("BALANCE_QUANTITY_ORDER").ToInt())
                                {
                                   color = HColor.Green;
                                } 
                                // Если TASK_DONE_IS = 1 AND ORDER_QUANTITY > BALANCE_QUANTITY_ORDER, то желтым
                                else if ((row.CheckGet("TASK_DONE_IS").ToInt() == 1)
                                && ((row.CheckGet("ORDER_QUANTITY").ToInt() >  row.CheckGet("BALANCE_QUANTITY_ORDER").ToInt())))
                                {
                                    color = HColor.Yellow;
                                }
                                else if (row.CheckGet("PRODUCTION_TASK_ID").ToInt() == 0)
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

            OrderGrid.SetPrimaryKey("ORDER_POSITION_ID");   //("ORDER_ID");
            // OrderGrid.SetSorting("ORDER_SHIPMENT_DATE", ListSortDirection.Descending);
            OrderGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            OrderGrid.SearchText = OrderGridSearch;
            OrderGrid.Toolbar = OrderGridToolbar;
            OrderGrid.QueryLoadItems = new RequestData()
            {
                Module = "MoldedContainer",
                Object = "Order",
                Action = "List",
                AnswerSectionKey = "ITEMS",
            };

            OrderGrid.OnFilterItems = () =>
            {
                if (OrderGrid.Items.Count > 0)
                {
                    {
                        var showAll = false;
                        var v = Form.GetValues();

                        if (v.CheckGet("HIDE_COMPLETE").ToBool())
                        {
                            showAll = true;
                        }
                        
                        var items = new List<Dictionary<string, string>>();

                        foreach (Dictionary<string, string> row in OrderGrid.Items)
                        {
                            if (!showAll)
                            {
                                items.Add(row);
                            }
                            else
                            {
                                if (row.CheckGet("ORDER_QUANTITY").ToInt() >
                                    row.CheckGet("BALANCE_QUANTITY_ORDER").ToInt())
                                {
                                    items.Add(row);
                                }
                            }
                        }
                        
                        OrderGrid.Items = items;    
                    }
                }
            };

            //при выборе строки в гриде, обновляются актуальные действия для записи
            OrderGrid.OnSelectItem = selectedItem =>
            {
                SelectedItem = selectedItem;

            };

            OrderGrid.Commands = Commander;
            OrderGrid.Init();
        }

        public void ProductionTaskGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="GOODS_CODE",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },

                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="TASK_ID",
                    Description="Идентификатор ПЗЛТ",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header="Дата создания",
                    Path="TASK_CREATED",
                    Description="",
                    ColumnType=ColumnTypeRef.DateTime,
                    Format="dd.MM.yyyy",
                    Width2=11,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="GOODS_NAME",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=44,
                },
                new DataGridHelperColumn
                {
                    Header="Номер",
                    Path="TASK_NUMBER",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=9,
                },
                new DataGridHelperColumn
                {
                    Header="Станок",
                    Path="MACHINE_NAME",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Рабочий центр",
                    Path="WORK_CENTER_NAME",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=14,
                },
                new DataGridHelperColumn
                {
                    Header="Схема производства",
                    Path="PRODUCTION_SCHEME_NAME",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="Статус",
                    Path="TASK_STATUS_TITLE",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Количество, шт",
                    Path="TASK_QUANTITY",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Произведено, шт",
                    Path="BALANCE_QUANTITY_PRODUCED",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Заявка",
                    Path="ORDER_TITLE",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание",
                    Path="ORDER_NOTE_GENERAL",
                    Description="примечание ОПП и складу",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Создатель",
                    Path="CREATOR_NAME",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание производства",
                    Path="NOTE",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
                },
                 new DataGridHelperColumn
                {
                    Header="ИДПЗ",
                    Path="TASK_ID2",
                    Description="(proiz_zad)",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="ИД позиции заявки",
                    Path="ORDER_POSITION_ID",
                    Description="(idorderdates)",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="ИД изделия",
                    Path="GOODS_ID",
                    Description="id2",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Статус Id",
                    Path="TASK_STATUS_ID",
                    Description="task_status_id",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                    Hidden = true,
                },

            };

            ProductionTaskGrid.SetColumns(columns);
            ProductionTaskGrid.SetPrimaryKey("TASK_ID");
            ProductionTaskGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            ProductionTaskGrid.SearchText = ProductionTaskGridSearch;
            ProductionTaskGrid.Toolbar = ProductionTaskGridToolbar;
            ProductionTaskGrid.AutoUpdateInterval = 0;
            ProductionTaskGrid.QueryLoadItems = new RequestData()
            {
                Module = "MoldedContainer",
                Object = "ProductionTask",
                Action = "List",
                AnswerSectionKey = "ITEMS",
                OnCompleteGrid = UniqMachineId
            };
            ProductionTaskGrid.OnFilterItems = () =>
            {
                if (ProductionTaskGrid.Items.Count > 0)
                {
                    var v = Form.GetValues();
                    var showAll = v.CheckGet("SHOW_ALL").ToBool();
                    var selectedWorkCenterId = v.CheckGet("WORK_CENTER").ToInt();
                    var selectedMachineId = v.CheckGet("SELECT_MACHINE").ToInt();

                    var items = new List<Dictionary<string, string>>();
                    foreach (Dictionary<string, string> row in ProductionTaskGrid.Items)
                    {
                        bool includeRow = true;
                        
                        if (!showAll && row.CheckGet("TASK_STATUS_ID").ToInt() >= 5)
                        {
                            includeRow = false;
                        }
                        
                        if (selectedWorkCenterId != -1 && row.CheckGet("WORK_CENTER_ID").ToInt() != selectedWorkCenterId)
                        {
                            includeRow = false;
                        }

                        if (selectedMachineId != -1 && row.CheckGet("MACHINE_ID").ToInt() != selectedMachineId)
                        {
                            includeRow = false;
                        }

                        if (includeRow)
                        {
                            items.Add(row);
                        }
                    }

                    ProductionTaskGrid.Items = items;
                }
            };


            ProductionTaskGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                {
                    StylerTypeRef.BackgroundColor,
                    (Dictionary<string, string> row) =>
                    {
                        var result = DependencyProperty.UnsetValue;
                        var color = "";

                        // Задание выполненое
                        if (row.CheckGet("TASK_STATUS_ID").ToInt() == 5)
                        {
                            color = HColor.Green;
                        }
                        // Задание в работе
                        else if (row.CheckGet("TASK_STATUS_ID").ToInt() == 4)
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
            };

            ProductionTaskGrid.Commands = Commander;
            ProductionTaskGrid.Init();
        }
        
        /// <summary>
        /// Собираем униканльые станки
        /// </summary>
        /// <param name="ds"></param>

        private void UniqMachineId(ListDataSet ds)
        {
            UniqIdMachine = ds.Items.GroupBy(x => x.CheckGet("MACHINE_ID"))
                .Select(g => g.First())
                .ToList();
            
            var item = new Dictionary<string, string>()
            {
                { "-1", "Все" },
            };

            foreach (var rs in UniqIdMachine)
            {
                item.Add(rs.CheckGet("MACHINE_ID").ToInt().ToString(), rs.CheckGet("MACHINE_NAME"));
            }

            SelectMachine.Items = item;

            if (MachineId != "-1" || SelectWorkCenter.SelectedItem.Key != "-1")
            {
                foreach (var field in Form.Fields)
                {
                    if (field.Path == "WORK_CENTER")
                    {
                        var f = (SelectBox)field.Control;
                        FilterMachineId(f.SelectedItem.Key);
                    }
                }
            }
            else
            {
                SelectMachine.SetSelectedItemByKey("-1");
            }
        }

        private void FilterMachineId(string idGroup)
        {
            if (UniqIdMachine != null)
            {
                var item = new Dictionary<string, string>()
                {
                    { "-1", "Все" },
                };
            
                foreach (var rs in UniqIdMachine)
                {
                    if (rs.CheckGet("WORK_CENTER_ID").ToInt() == idGroup.ToInt() || idGroup.ToInt() == -1)
                    {
                        item.Add(rs.CheckGet("MACHINE_ID").ToInt().ToString(), rs.CheckGet("MACHINE_NAME"));
                    }
                }

                SelectMachine.Items = item;

                if (MachineId != "-1")
                {
                    SelectMachine.SetSelectedItemByKey(MachineId);
                }
                else
                {
                    SelectMachine.SetSelectedItemByKey("-1");
                }
            }
        }

        /// <summary>
        /// удаление производственного задания
        /// </summary>
        public async void ProductionTaskDelete()
        {
            var id = ProductionTaskGrid.SelectedItem.CheckGet("TASK_ID").ToInt();
            if (id > 0)
            {
                var dw = new DialogWindow("Вы действительно хотите удалить производственное задание?", "Удаление ПЗ", "Подтверждение удаления ПЗ", DialogWindowButtons.NoYes);
                if (dw.ShowDialog() == true)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "MoldedContainer");
                    q.Request.SetParam("Object", "ProductionTask");
                    q.Request.SetParam("Action", "DeleteTask");
                    q.Request.SetParam("TASK_ID", id.ToString());

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    if (q.Answer.Status == 0)
                    {
                        OrderGrid.LoadItems();
                        ProductionTaskGrid.LoadItems();
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
            }
        }

        public void FormInit()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="SEARCH",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductionTaskGridSearch,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
                new FormHelperField()
                {
                    Path="SHOW_ALL",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ShowAll,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
                new FormHelperField()
                {
                    Path = "HIDE_COMPLETE",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = HideComplete,
                    ControlType = "CheckBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{}
                },
                new FormHelperField()
                {
                    Path = "SELECT_MACHINE",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = SelectMachine,
                    ControlType = "SelectBox",
                    OnChange = (field, s) =>
                    {
                        var c = (SelectBox)field.Control;
                        MachineId = c.SelectedItem.Key;
                        
                        ProductionTaskGrid.UpdateItems();
                    },
                },
                new FormHelperField()
                {
                    Path = "WORK_CENTER",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = SelectWorkCenter,
                    Default = "-1",
                    ControlType = "SelectBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> { },
                    OnChange = (FormHelperField f, string s) =>
                    {
                        var c = (SelectBox)f.Control;
                        ProductionTaskGrid.UpdateItems();
                        FilterMachineId(c.SelectedItem.Key);
                    },
                    QueryLoadItems = new RequestData()
                    {
                        Module = "MoldedContainer",
                        Object = "WorkCenter",
                        Action = "List",
                        AnswerSectionKey = "WORK_CENTER",
                        OnComplete = (FormHelperField f, ListDataSet ds) =>
                        {
                            var row = new Dictionary<string, string>()
                            {
                                { "PRWO_ID", "-1" },
                                { "NAME", "Все" }
                            };
                            ds.ItemsPrepend(row);
                            var list = ds.GetItemsList("PRWO_ID", "NAME");
                            var c = (SelectBox)f.Control;
                            if (c != null)
                            {
                                c.Items = list;
                            }
                        }
                    }
                }
            };

            Form.SetFields(fields);
            Form.SetDefaults();
        }

        /// <summary>
        /// Функция для разрешения/запрета редактирования заявки
        /// </summary>
        private async void ToggleEditModeOrder(string orderId, string editMode)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Order");
            q.Request.SetParam("Action", "ChangeMode");
            q.Request.SetParam("ORDER_ID", orderId);
            q.Request.SetParam("EDIT_MODE", editMode);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                OrderGrid.LoadItems();
                ProductionTaskGrid.LoadItems();
            }
            else
            {
                q.ProcessError();
            }
        }

        private async void UntieOrderFromProductionTask(string idTask)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "ProductionTask");
            q.Request.SetParam("Action", "Untie");
            q.Request.SetParam("ID_PZ", idTask);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                OrderGrid.LoadItems();
                ProductionTaskGrid.LoadItems();
            }
            else
            {
                q.ProcessError();
            }
        }


        /// <summary>
        /// Показать все ПЗ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowAll_Click(object sender, RoutedEventArgs e)
        {
            ProductionTaskGrid.UpdateItems();
        }

        /// <summary>
        /// Показать выполненные ПЗ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowAllComplete_Click(object sender, RoutedEventArgs e)
        {
            OrderGrid.UpdateItems();
        }
    }
}
