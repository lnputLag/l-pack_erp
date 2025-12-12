using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Production;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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
using static Client.Interfaces.Main.DataGridHelperColumn;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Header;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Склад макулатуры Литой тары
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class MoldedContainerWarehouseScrapPaper : ControlBase
    {
        public MoldedContainerWarehouseScrapPaper()
        {
            ControlTitle = "Склад макулатуры";
            DocumentationUrl = "/doc/l-pack-erp-new/lt/sklad_lt/sklad_waste_paper";
            RoleName = "[erp]molded_contnr_warehouse";
            InitializeComponent();

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (KeyEventArgs e) =>
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
                SetDefaults();
                CellGridInit();
                CellPositionGridInit();
                IncomingGridInit();
                ConsumptionGridInit();
                ProductGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                CellGrid.Destruct();
                CellPositionGrid.Destruct();
                IncomingGrid.Destruct();
                ConsumptionGrid.Destruct();
                ProductGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                CellGrid.ItemsAutoUpdate = true;
                CellGrid.Run();

                IncomingGrid.ItemsAutoUpdate = true;
                IncomingGrid.Run();

                ConsumptionGrid.ItemsAutoUpdate = true;
                ConsumptionGrid.Run();

                ProductGrid.ItemsAutoUpdate = true;
                ProductGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                CellGrid.ItemsAutoUpdate = false;

                IncomingGrid.ItemsAutoUpdate = false;

                ConsumptionGrid.ItemsAutoUpdate = false;

                ProductGrid.ItemsAutoUpdate = false;
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
                    Name = "export_to_excel",
                    Group = "main",
                    Enabled = true,
                    Title = "В Excel",
                    Description = "Выгрузить данные в Excel файл",
                    ButtonUse = true,
                    ButtonControl = ExportToExcelButton,
                    ButtonName = "ExportToExcelButton",
                    Action = () =>
                    {
                        ExportToExcel();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "report_balance",
                    Group = "main",
                    Enabled = true,
                    Title = "Отчёт по остаткам",
                    Description = "Сформировать отчёт по складским остаткам в Excel файле",
                    ButtonUse = true,
                    ButtonControl = ReportBalanceButton,
                    ButtonName = "ReportBalanceButton",
                    Action = () =>
                    {
                        ReportBalance();
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
            }

            Commander.SetCurrentGridName("CellPositionGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "move_item",
                    Title = "Переместить",
                    Description = "Переместить выбранную позицию",
                    Group = "cell_position_grid_default",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = MoveItemButton,
                    ButtonName = "MoveItemButton",
                    Enabled = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        MoveItem();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        SelectAllButton.IsEnabled = false;

                        if (CellPositionGrid != null && CellPositionGrid.SelectedItem != null && CellPositionGrid.SelectedItem.Count > 0)
                        {
                            result = true;
                            SelectAllButton.IsEnabled = true;
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "consumption_item",
                    Title = "Списать",
                    Description = "Списать выбранную позицию",
                    Group = "cell_position_grid_default",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = ConsumptionItemButton,
                    ButtonName = "ConsumptionItemButton",
                    Enabled = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        ConsumptionItem();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (CellPositionGrid != null && CellPositionGrid.SelectedItem != null && CellPositionGrid.SelectedItem.Count > 0)
                        {
                            result = true;
                        }

                        return result;
                    },
                });

                Commander.Add(new CommandItem()
                {
                    Name = "move_item_all",
                    Title = "Переместить все",
                    Description = "Переместить все позиции",
                    Group = "cell_position_grid_default",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = MoveItemAllButton,
                    ButtonName = "MoveItemAllButton",
                    Enabled = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        MoveItemAll();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (CellPositionGrid != null && CellPositionGrid.SelectedItem != null && CellPositionGrid.SelectedItem.Count > 0)
                        {
                            result = true;
                        }

                        return result;
                    },
                });

            }

            Commander.SetCurrentGridName("IncomingGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "incoming_item",
                    Title = "Оприходовать",
                    Description = "Оприходовать выбранную позицию",
                    Group = "incoming_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = IncomingItemButton,
                    ButtonName = "IncomingItemButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        IncomingItem();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (IncomingGrid != null && IncomingGrid.Items != null && IncomingGrid.Items.Count > 0)
                        {
                            if (IncomingGrid.SelectedItem != null && IncomingGrid.SelectedItem.Count > 0)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
            }

            Commander.SetCurrentGridName("ConsumptionGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "cancel_consumption",
                    Title = "Отменить списание",
                    Description = "Отменить списание выбранной позиции",
                    Group = "consumption_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = CancelConsumptionButton,
                    ButtonName = "CancelConsumptionButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        CancelConsumption();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ConsumptionGrid != null && ConsumptionGrid.Items != null && ConsumptionGrid.Items.Count > 0)
                        {
                            if (ConsumptionGrid.SelectedItem != null && ConsumptionGrid.SelectedItem.Count > 0)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "consumption_grid_export_to_excel",
                    Group = "main",
                    Enabled = true,
                    Title = "В Excel",
                    Description = "Выгрузить данные в Excel файл",
                    ButtonUse = true,
                    ButtonControl = ConsumptionGridExportToExcelButton,
                    ButtonName = "ConsumptionGridExportToExcelButton",
                    Action = () =>
                    {
                        ConsumptionGridExportToExcel();
                    },
                });
            }

            Commander.Init(this);
        }

        public int WarehouseId = 4;

        public int ZoneId = 8;

        private ListDataSet CellDataSet { get; set; }

        private ListDataSet CellPositionDataSet { get; set; }

        private ListDataSet IncomingDataSet { get; set; }

        private ListDataSet ConsumptionDataSet { get; set; }

        private ListDataSet ProductDataSet { get; set; }

        private ListDataSet ShiftDataSet { get; set; }

        public void SetDefaults()
        {
            CellDataSet = new ListDataSet();
            CellPositionDataSet = new ListDataSet();
            IncomingDataSet = new ListDataSet();
            ConsumptionDataSet = new ListDataSet();
            ProductDataSet = new ListDataSet();
            ShiftDataSet = new ListDataSet();

            GetShiftList();
        }

        public void CellGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Path="STORAGE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        TotalsType = TotalsTypeRef.Count,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Место",
                        Path="STORAGE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество, шт",
                        Description = "Количество складских единиц в ячейке",
                        Path="ITEM_COUNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 7,
                        TotalsType = TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вес, кг",
                        Description = "Суммарный вес складских единиц в ячейке",
                        Path="ITEM_SUMMARY_WEIGHT",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width2 = 9,
                        TotalsType = TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Категория",
                        Path="SCRAP_CATEGORY",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 35,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Поставщик",
                        Path="SCRAP_PROVIDER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 35,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Задача",
                        Path="WMS_TASK_FLAG",
                        Description = "Есть задача на списание продукции в производство из этой ячейки",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус задачи",
                        Path="WMS_TASK_STATUS_NAME",
                        Description = "Статус задачи на списание продукции в производство из этой ячейки",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 11,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Склад",
                        Path="SKLAD",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ячейка",
                        Path="NUM_PLACE",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид статуса задачи",
                        Path="WMS_TASK_STATUS_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                };
                CellGrid.SetColumns(columns);
                CellGrid.SetPrimaryKey("STORAGE_ID");
                CellGrid.SearchText = CellSearchBox;
                //данные грида
                CellGrid.OnLoadItems = CellGridLoadItems;
                CellGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                CellGrid.AutoUpdateInterval = 60 * 5;
                CellGrid.Toolbar = CellGridToolbar;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                CellGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem != null)
                    {
                        if (CellGrid != null && CellGrid.Items != null && CellGrid.Items.Count > 0)
                        {
                            if (CellGrid.Items.FirstOrDefault(x => x.CheckGet("STORAGE_ID").ToInt() == selectedItem.CheckGet("STORAGE_ID").ToInt()) == null)
                            {
                                CellGrid.SelectRowFirst();
                            }
                        }

                        CellPositionGridLoadItems();
                        SelectAllButton.IsChecked = false;
                    }
                };

                CellGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    // определение цветов фона строк
                    {
                        StylerTypeRef.BackgroundColor,
                        (Dictionary<string, string> row) =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            // 
                            if (row.CheckGet("WMS_TASK_STATUS_ID").ToInt() == 1)
                            {
                                color = HColor.Blue;
                            }

                            if (row.CheckGet("WMS_TASK_STATUS_ID").ToInt() == 2)
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
                };

                CellGrid.OnFilterItems = CellGridFilterItems;

                CellGrid.Commands = Commander;

                CellGrid.Init();
            }
        }

        public async void CellGridLoadItems()
        {
            var p = new Dictionary<string, string>();
            p.Add("WAREHOUSE_ID", $"{WarehouseId}");
            p.Add("ZONE_ID", $"{ZoneId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "ListCell");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            CellDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    CellDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            CellGrid.UpdateItems(CellDataSet);
        }

        public void CellGridFilterItems()
        {
            CellPositionGrid.ClearItems();

            if (CellGrid != null && CellGrid.SelectedItem != null && CellGrid.SelectedItem.Count > 0)
            {
                CellGrid.Commands.Message = new ItemMessage() { Action = "refresh", Message = $"{CellGrid.SelectedItem.CheckGet("STORAGE_ID")}" };
            }
        }

        public void CellPositionGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header = "*",
                        Path = "_SELECTED",
                        ColumnType = ColumnTypeRef.Boolean,
                        Width2=3,
                        Editable = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="WMIT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 16,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Description="Количество, шт",
                        Path="QTY",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вес",
                        Description="Вес, кг",
                        Path="WEIGHT",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата поступления",
                        Path="CREATED_DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД прихода",
                        Path="IDP",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                        Hidden=true,
                    },
                };
                CellPositionGrid.SetColumns(columns);
                CellPositionGrid.SetPrimaryKey("WMIT_ID");
                CellPositionGrid.SearchText = CellPositionSearchBox;
                //данные грида
                CellPositionGrid.OnLoadItems = CellPositionGridLoadItems;
                CellPositionGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                CellPositionGrid.AutoUpdateInterval = 0;
                CellPositionGrid.Toolbar = CellPositionGridToolbar;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                CellPositionGrid.OnSelectItem = selectedItem =>
                {
               
                };

                CellPositionGrid.Commands = Commander;

                CellPositionGrid.Init();
            }
        }

        public async void CellPositionGridLoadItems()
        {
            var p = new Dictionary<string, string>();
            p.Add("STORAGE_ID", $"{CellGrid.SelectedItem.CheckGet("STORAGE_ID")}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "ListCellPosition");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            CellPositionDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    CellPositionDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            CellPositionGrid.UpdateItems(CellPositionDataSet);
        }

        public void ConsumptionItem()
        {
            if (CellPositionGrid != null && CellPositionGrid.Items != null && CellPositionGrid.Items.Count > 0)
            {
                var checkedRowList = CellPositionGrid.GetItemsSelected();
                if (checkedRowList != null && checkedRowList.Count > 0)
                {
                    if (checkedRowList.Count > 1)
                    {
                        if (new DialogWindow($"Хотите списать {checkedRowList.Count} позиций?", "Сообщение", "", DialogWindowButtons.YesNo).ShowDialog() == true)
                        {
                            bool errorFlag = false;

                            foreach (var checkedRow in checkedRowList)
                            {
                                var p = new Dictionary<string, string>();
                                {
                                    p.Add("WMIT_ID", checkedRow["WMIT_ID"]);
                                    p.Add("WMST_ID", CellGrid.SelectedItem["STORAGE_ID"]);
                                    p.Add("QTY", checkedRow["QTY"]);
                                }

                                var q = new LPackClientQuery();
                                q.Request.SetParam("Module", "Warehouse");
                                q.Request.SetParam("Object", "Item");
                                q.Request.SetParam("Action", "WriteOff");
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
                                            if (ds.Items[0].CheckGet("ID").ToInt() == 0)
                                            {
                                                succesfullFlag = true;
                                            }
                                        }
                                    }

                                    if (!succesfullFlag)
                                    {
                                        errorFlag = true;
                                    }
                                }
                                else
                                {
                                    errorFlag = true;
                                }
                            }

                            if (errorFlag)
                            {
                                DialogWindow.ShowDialog($"При выполнении списания произошла ошибка. Пожалуйста, сообщите о проблеме.");
                            }

                            Refresh();
                        }
                    }
                    else
                    {
                        var checkedRow = checkedRowList.First();
                        if (new DialogWindow($"Хотите списать позицию # {checkedRow["WMIT_ID"].ToInt()} {checkedRow["NAME"]} из ячейки {CellGrid.SelectedItem["STORAGE_NAME"]}?", "Сообщение", "", DialogWindowButtons.YesNo).ShowDialog() == true)
                        {
                            var p = new Dictionary<string, string>();
                            {
                                p.Add("WMIT_ID", checkedRow["WMIT_ID"]);
                                p.Add("WMST_ID", CellGrid.SelectedItem["STORAGE_ID"]);
                                p.Add("QTY", checkedRow["QTY"]);
                            }

                            var q = new LPackClientQuery();
                            q.Request.SetParam("Module", "Warehouse");
                            q.Request.SetParam("Object", "Item");
                            q.Request.SetParam("Action", "WriteOff");
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
                                        if (ds.Items[0].CheckGet("ID").ToInt() == 0)
                                        {
                                            succesfullFlag = true;
                                        }
                                    }
                                }

                                if (!succesfullFlag)
                                {
                                    DialogWindow.ShowDialog($"При выполнении списания произошла ошибка. Пожалуйста, сообщите о проблеме.");
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
                }
                else
                {
                    if (CellPositionGrid.SelectedItem != null && CellPositionGrid.SelectedItem.Count > 0)
                    {
                        if (new DialogWindow($"Хотите списать позицию # {CellPositionGrid.SelectedItem["WMIT_ID"].ToInt()} {CellPositionGrid.SelectedItem["NAME"]} из ячейки {CellGrid.SelectedItem["STORAGE_NAME"]}?", "Сообщение", "", DialogWindowButtons.YesNo).ShowDialog() == true)
                        {
                            var p = new Dictionary<string, string>();
                            {
                                p.Add("WMIT_ID", CellPositionGrid.SelectedItem["WMIT_ID"]);
                                p.Add("WMST_ID", CellGrid.SelectedItem["STORAGE_ID"]);
                                p.Add("QTY", CellPositionGrid.SelectedItem["QTY"]);
                            }

                            var q = new LPackClientQuery();
                            q.Request.SetParam("Module", "Warehouse");
                            q.Request.SetParam("Object", "Item");
                            q.Request.SetParam("Action", "WriteOff");
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
                                        if (ds.Items[0].CheckGet("ID").ToInt() == 0)
                                        {
                                            succesfullFlag = true;
                                        }
                                    }
                                }

                                if (!succesfullFlag)
                                {
                                    DialogWindow.ShowDialog($"При выполнении списания произошла ошибка. Пожалуйста, сообщите о проблеме.");
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
                }
            }
        }

        public void MoveItem()
        {
            var warehouseItemCell = new WarehouseItemCell();
            warehouseItemCell.ItemId = CellPositionGrid.SelectedItem["WMIT_ID"].ToInt();
            warehouseItemCell.CurrentItemAction = WarehouseItemCell.ItemAction.Move;
            warehouseItemCell.WarehouseSelectBox.SetSelectedItemByKey($"{WarehouseId}");
            warehouseItemCell.WarehouseSelectBox.IsReadOnly = true;
            warehouseItemCell.ZoneSelectBox.SetSelectedItemByKey($"{ZoneId}");
            warehouseItemCell.Show();
        }

        public void ConsumptionGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header = "*",
                        Path = "_SELECTED",
                        ColumnType = ColumnTypeRef.Boolean,
                        Width2=3,
                        Editable = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД списания",
                        Path="CONSUMPTION_ID",
                        Description="Ид списания складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата",
                        Path="CONSUMPTION_DATE",
                        Description="Дата списания складской единицы",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Path="QUANTITY",
                        Description="Списанное количество",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width2 = 5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Складская единица",
                        Path="ITEM_NAME",
                        Description="Наименование списанной складской единицы",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 26,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Хранилище",
                        Path="STORAGE_NAME",
                        Description="Наименование хранилища, откуда была списана складская единица",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Причина",
                        Path="CONSUMPTION_REASON",
                        Description="Причина списания",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Пользователь",
                        Path="USER_NAME",
                        Description="Имя пользователя, совершившего списание",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД складской единицы",
                        Path="ITEM_ID",
                        Description="Ид списанной складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД прихода",
                        Path="INCOMING_ID",
                        Description="Ид прихода складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД внешний",
                        Path="OUTER_ID",
                        Description="Внешний Ид складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                    },

                    new DataGridHelperColumn
                    {
                        Header="ИД пользователя",
                        Path="USER_ID",
                        Description="Ид пользователя, совершившего списание",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД расхода",
                        Path="OUTER_CONSUMPTION_ID",
                        Description="Внешний Ид списания",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД накладной",
                        Path="CONSUMPTION_INVOICE_ID",
                        Description="Ид накладной расхода",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Склад",
                        Path="WAREHOUSE_NAME",
                        Description="Наименование склада, откуда была списана складская единица",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 13,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Зона",
                        Path="ZONE_NAME",
                        Description="Наименование зоны, откуда была списана складская единица",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 16,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД хранилища",
                        Path="STORAGE_ID",
                        Description="Ид хранилища, откуда была списана складская единица",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 12,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД склада",
                        Path="WAREHOUSE_ID",
                        Description="Ид склада, откуда была списана складская единица",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 9,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД зоны",
                        Path="ZONE_ID",
                        Description="Ид зоны, откуда была списана складская единица",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 7,
                        Hidden=true,
                    },
                };
                ConsumptionGrid.SetColumns(columns);
                ConsumptionGrid.SetPrimaryKey("CONSUMPTION_ID");
                ConsumptionGrid.SearchText = ConsumptionSearchBox;
                //данные грида
                ConsumptionGrid.OnLoadItems = ConsumptionGridLoadItems;
                ConsumptionGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                ConsumptionGrid.AutoUpdateInterval = 60 * 5;
                ConsumptionGrid.Toolbar = ConsumptionGridToolbar;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                ConsumptionGrid.OnSelectItem = selectedItem =>
                {
                    if (ConsumptionGrid != null && ConsumptionGrid.Items != null && ConsumptionGrid.Items.Count > 0)
                    {
                        if (ConsumptionGrid.Items.FirstOrDefault(x => x.CheckGet("CONSUMPTION_ID").ToInt() == selectedItem.CheckGet("CONSUMPTION_ID").ToInt()) == null)
                        {
                            ConsumptionGrid.SelectRowFirst();
                        }
                    }
                };

                ConsumptionGrid.OnFilterItems = ConsumptionGridFilterItems;

                ConsumptionGrid.Commands = Commander;

                ConsumptionGrid.Init();
            }
        }

        public void GetShiftList()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "ListShift");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    ShiftDataSet = ListDataSet.Create(result, "ITEMS");
                    ShiftSelectBox.SetItems(ShiftDataSet, "SHIFT_ID", "SHIFT_NAME");

                    if (ShiftDataSet != null && ShiftDataSet.Items != null && ShiftDataSet.Items.Count > 0)
                    {
                        string today = "";
                        if (DateTime.Now.Hour < 8)
                        {
                            today = $"{DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy")} 20:00:00";
                        }
                        else
                        {
                            if (DateTime.Now.Hour < 20)
                            {
                                today = $"{DateTime.Now.ToString("dd.MM.yyyy")} 08:00:00";
                            }
                            else
                            {
                                today = $"{DateTime.Now.ToString("dd.MM.yyyy")} 20:00:00";
                            }
                        }

                        var todayItem = ShiftDataSet.Items.FirstOrDefault(x => x.CheckGet("SHIFT_START_DTTM") == today);
                        if (todayItem != null)
                        {
                            ShiftSelectBox.SetSelectedItemByKey(todayItem.CheckGet("SHIFT_ID"));
                        }
                    }
                }
            }
        }

        public async void ConsumptionGridLoadItems()
        {
            var p = new Dictionary<string, string>();
            p.Add("WMWA_ID", $"{WarehouseId}");
            p.Add("WMZO_ID", $"{ZoneId}");

            if (ShiftSelectBox.SelectedItem.Key != null)
            {
                p.Add("FROM_DATE", $"{ShiftDataSet.Items.FirstOrDefault(x => x.CheckGet("SHIFT_ID").ToInt() == ShiftSelectBox.SelectedItem.Key.ToInt()).CheckGet("SHIFT_START_DTTM")}");
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "Consumption");
            q.Request.SetParam("Action", "ListByWarehouseAndZoneAndDate");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            ConsumptionDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    ConsumptionDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            ConsumptionGrid.UpdateItems(ConsumptionDataSet);
        }

        public void ConsumptionGridFilterItems()
        {
            if (ConsumptionGrid != null && ConsumptionGrid.SelectedItem != null && ConsumptionGrid.SelectedItem.Count > 0)
            {
                ConsumptionGrid.Commands.Message = new ItemMessage() { Action = "refresh", Message = $"{ConsumptionGrid.SelectedItem.CheckGet("CONSUMPTION_ID")}" };
            }
        }

        public void CancelConsumption()
        {
            if (ConsumptionGrid != null && ConsumptionGrid.Items != null && ConsumptionGrid.Items.Count > 0)
            {
                var checkedRowList = ConsumptionGrid.GetItemsSelected();
                if (checkedRowList != null && checkedRowList.Count > 0)
                {
                    if (checkedRowList.Count > 1)
                    {
                        if (new DialogWindow($"Хотите отменить списание {checkedRowList.Count} позиций?", "Сообщение", "", DialogWindowButtons.YesNo).ShowDialog() == true)
                        {
                            bool errorFlag = false;

                            foreach (var checkedRow in checkedRowList)
                            {
                                var p = new Dictionary<string, string>();
                                p.Add("WMCO_ID", checkedRow.CheckGet("CONSUMPTION_ID"));

                                var q = new LPackClientQuery();
                                q.Request.SetParam("Module", "Warehouse");
                                q.Request.SetParam("Object", "Consumption");
                                q.Request.SetParam("Action", "Cancel");
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
                                            if (ds.Items.First().CheckGet("ID").ToInt() == 0)
                                            {
                                                succesfullFlag = true;
                                            }
                                        }
                                    }

                                    if (!succesfullFlag)
                                    {
                                        errorFlag = true;
                                    }
                                }
                                else
                                {
                                    errorFlag = true;
                                }
                            }

                            if (errorFlag)
                            {
                                DialogWindow.ShowDialog($"При выполнении отмены списания произошла ошибка. Пожалуйста, сообщите о проблеме.");
                            }

                            Refresh();
                        }
                    }
                    else
                    {
                        var checkedRow = checkedRowList.First();
                        if (new DialogWindow($"Хотите отменить списание # {checkedRow["CONSUMPTION_ID"].ToInt()} позиции # {checkedRow["ITEM_ID"].ToInt()} {checkedRow["ITEM_NAME"]} из ячейки {checkedRow["STORAGE_NAME"]}?", "Сообщение", "", DialogWindowButtons.YesNo).ShowDialog() == true)
                        {
                            var p = new Dictionary<string, string>();
                            p.Add("WMCO_ID", checkedRow.CheckGet("CONSUMPTION_ID"));

                            var q = new LPackClientQuery();
                            q.Request.SetParam("Module", "Warehouse");
                            q.Request.SetParam("Object", "Consumption");
                            q.Request.SetParam("Action", "Cancel");
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
                                        if (ds.Items.First().CheckGet("ID").ToInt() == 0)
                                        {
                                            succesfullFlag = true;
                                        }
                                    }
                                }

                                if (!succesfullFlag)
                                {
                                    DialogWindow.ShowDialog($"При выполнении отмены списания произошла ошибка. Пожалуйста, сообщите о проблеме.");
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
                }
                else
                {
                    if (ConsumptionGrid.SelectedItem != null && ConsumptionGrid.SelectedItem.Count > 0)
                    {
                        if (new DialogWindow($"Хотите отменить списание # {ConsumptionGrid.SelectedItem["CONSUMPTION_ID"].ToInt()} позиции # {ConsumptionGrid.SelectedItem["ITEM_ID"].ToInt()} {ConsumptionGrid.SelectedItem["ITEM_NAME"]} из ячейки {ConsumptionGrid.SelectedItem["STORAGE_NAME"]}?", "Сообщение", "", DialogWindowButtons.YesNo).ShowDialog() == true)
                        {
                            var p = new Dictionary<string, string>();
                            p.Add("WMCO_ID", ConsumptionGrid.SelectedItem.CheckGet("CONSUMPTION_ID"));

                            var q = new LPackClientQuery();
                            q.Request.SetParam("Module", "Warehouse");
                            q.Request.SetParam("Object", "Consumption");
                            q.Request.SetParam("Action", "Cancel");
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
                                        if (ds.Items.First().CheckGet("ID").ToInt() == 0)
                                        {
                                            succesfullFlag = true;
                                        }
                                    }
                                }

                                if (!succesfullFlag)
                                {
                                    DialogWindow.ShowDialog($"При выполнении отмены списания произошла ошибка. Пожалуйста, сообщите о проблеме.");
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
                }
            }
        }

        public void IncomingGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="ITEM_ID",
                        Description="Ид складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Складская единица",
                        Path="ITEM_NAME",
                        Description="Наименование складской единицы",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 28,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Path="ITEM_QUANTITY",
                        Description="Количество",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Группа",
                        Path="ITEM_GROUP_NAME",
                        Description="Наименование группы складских единиц",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Примечание",
                        Path="ITEM_NOTE",
                        Description="Примечание",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 46,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата",
                        Path="CREATED_DTTM",
                        Description="Дата создания складской единицы",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД состояния",
                        Path="ITEM_STATE_ID",
                        Description="Ид состояния складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 12,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД группы",
                        Path="ITEM_GROUP_ID",
                        Description="Ид группы складских единиц",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 9,
                        Hidden=true,
                    },
                };
                IncomingGrid.SetColumns(columns);
                IncomingGrid.SetPrimaryKey("INCOMING_ID");
                IncomingGrid.SearchText = IncomingSearchBox;
                //данные грида
                IncomingGrid.OnLoadItems = IncomingGridLoadItems;
                IncomingGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                IncomingGrid.AutoUpdateInterval = 60 * 5;
                IncomingGrid.Toolbar = IncomingGridToolbar;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                //при выборе строки в гриде, обновляются актуальные действия для записи
                IncomingGrid.OnSelectItem = selectedItem =>
                {
                    if (IncomingGrid != null && IncomingGrid.Items != null && IncomingGrid.Items.Count > 0)
                    {
                        if (IncomingGrid.Items.FirstOrDefault(x => x.CheckGet("INCOMING_ID").ToInt() == selectedItem.CheckGet("INCOMING_ID").ToInt()) == null)
                        {
                            IncomingGrid.SelectRowFirst();
                        }
                    }
                };

                IncomingGrid.OnFilterItems = IncomingGridFilterItems;

                IncomingGrid.Commands = Commander;

                IncomingGrid.Init();
            }
        }

        public async void IncomingGridLoadItems()
        {
            var p = new Dictionary<string, string>();
            p.Add("ITEM_GROUP_ID_LIST", "4, 6");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "Item");
            q.Request.SetParam("Action", "ListArrivalByItemGroup");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            IncomingDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    IncomingDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            IncomingGrid.UpdateItems(IncomingDataSet);
        }

        public void IncomingGridFilterItems()
        {
            if (IncomingGrid != null && IncomingGrid.SelectedItem != null && IncomingGrid.SelectedItem.Count > 0)
            {
                IncomingGrid.Commands.Message = new ItemMessage() { Action = "refresh", Message = $"{IncomingGrid.SelectedItem.CheckGet("INCOMING_ID")}" };
            }
        }

        public void IncomingItem()
        {
            if (IncomingGrid != null && IncomingGrid.SelectedItem != null && IncomingGrid.SelectedItem.Count > 0)
            {
                var warehouseItemCell = new WarehouseItemCell();
                warehouseItemCell.ItemId = IncomingGrid.SelectedItem["ITEM_ID"].ToInt();
                warehouseItemCell.CurrentItemAction = WarehouseItemCell.ItemAction.Register;
                warehouseItemCell.WarehouseSelectBox.SetSelectedItemByKey($"{WarehouseId}");
                warehouseItemCell.WarehouseSelectBox.IsReadOnly = true;
                warehouseItemCell.ZoneSelectBox.SetSelectedItemByKey($"{ZoneId}");
                warehouseItemCell.ItemQuantity = IncomingGrid.SelectedItem["ITEM_QUANTITY"].ToDouble();
                warehouseItemCell.Show();
            }
        }

        public void ProductGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="ITEM_ID",
                        Description="Ид вида продукции",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 6,
                        TotalsType = TotalsTypeRef.Count,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="ITEM_NAME",
                        Description="Наименование вида продукции",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 28,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество, шт",
                        Description = "Количество единиц продукции на складе",
                        Path="ITEM_COUNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 7,
                        TotalsType = TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вес, кг",
                        Description = "Суммарный вес продукции на складе",
                        Path="ITEM_SUMMARY_WEIGHT",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width2 = 9,
                        TotalsType = TotalsTypeRef.Summ,
                    },
                };
                ProductGrid.SetColumns(columns);
                ProductGrid.SetPrimaryKey("ITEM_ID");
                ProductGrid.SearchText = ProductSearchBox;
                //данные грида
                ProductGrid.OnLoadItems = ProductGridLoadItems;
                ProductGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                ProductGrid.AutoUpdateInterval = 60 * 5;
                ProductGrid.Toolbar = ProductGridToolbar;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                ProductGrid.OnSelectItem = selectedItem =>
                {
                    if (ProductGrid != null && ProductGrid.Items != null && ProductGrid.Items.Count > 0)
                    {
                        if (ProductGrid.Items.FirstOrDefault(x => x.CheckGet("ITEM_ID").ToInt() == selectedItem.CheckGet("ITEM_ID").ToInt()) == null)
                        {
                            ProductGrid.SelectRowFirst();
                        }
                    }
                };

                ProductGrid.OnFilterItems = ProductGridFilterItems;

                ProductGrid.Commands = Commander;

                ProductGrid.Init();
            }
        }

        public async void ProductGridLoadItems()
        {
            var p = new Dictionary<string, string>();
            p.Add("WAREHOUSE_ID", $"{WarehouseId}");
            p.Add("ZONE_ID", $"{ZoneId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "ListProduct");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            ProductDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    ProductDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            ProductGrid.UpdateItems(ProductDataSet);
        }

        public void ProductGridFilterItems()
        {
            if (ProductGrid != null && ProductGrid.SelectedItem != null && ProductGrid.SelectedItem.Count > 0)
            {
                ProductGrid.Commands.Message = new ItemMessage() { Action = "refresh", Message = $"{ProductGrid.SelectedItem.CheckGet("ITEM_ID")}" };
            }
        }

        public void Refresh()
        {
            CellGrid.LoadItems();
            ConsumptionGrid.LoadItems();
            IncomingGrid.LoadItems();
            ProductGrid.LoadItems();
        }

        public void ExportToExcel()
        {
            CellGrid.ItemsExportExcel();
        }

        public void ConsumptionGridExportToExcel()
        {
            ConsumptionGrid.ItemsExportExcel();
        }

        public async void ReportBalance()
        {
            var p = new Dictionary<string, string>();
            p.Add("WAREHOUSE_ID", WarehouseId.ToString());
            p.Add("ZONE_ID", ZoneId.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "GetReportBalance");
            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                if (q.Answer.Type == LPackClientAnswer.AnswerTypeRef.File)
                {
                    Central.OpenFile(q.Answer.DownloadFilePath);
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        private void ShiftSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ConsumptionGrid.LoadItems();
        }

        /// <summary>
        /// Выбранные записи в гриде кип
        /// </summary>
        private List<Dictionary<string, string>> itemList = new List<Dictionary<string, string>>();

        /// <summary>
        ////отметить все позиции
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = (bool)SelectAllButton.IsChecked;
            itemList = new List<Dictionary<string, string>>();

            if (CellPositionGrid.Items != null)
            {
                if (CellPositionGrid.Items.Count > 0)
                {
                    foreach (Dictionary<string, string> row in CellPositionGrid.Items)
                    {
                        row.CheckAdd("_SELECTED", selected ? "1" : "0");
                    }

                    CellPositionGrid.UpdateItems();

                    foreach (Dictionary<string, string> row in CellPositionGrid.Items)
                    {
                        Dictionary<string, string> parameters = new Dictionary<string, string>();
                        if (row.CheckGet("_SELECTED").ToInt() == 1)
                        {
                            parameters.Add("WMIT_ID", row.CheckGet("WMIT_ID").ToInt().ToString());
                            itemList.Add(parameters);
                        }
                    }

                }
            }

        }

        /// <summary>
        /// перемещаем все кипы из ячейки
        /// Грешных Н.И.
        /// </summary>
        public void MoveItemAll()
        {
            var warehouseItemCell = new WarehouseItemCell();
            warehouseItemCell.ItemAllList = itemList;
            warehouseItemCell.CurrentItemAction = WarehouseItemCell.ItemAction.MoveAll;
            warehouseItemCell.WarehouseSelectBox.SetSelectedItemByKey($"{WarehouseId}");
            warehouseItemCell.WarehouseSelectBox.IsReadOnly = true;
            warehouseItemCell.ZoneSelectBox.SetSelectedItemByKey($"{ZoneId}");
            warehouseItemCell.Show();
        }
        
    }
}
