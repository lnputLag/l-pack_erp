using System;
using Client.Common;
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
using GalaSoft.MvvmLight.Messaging;
using Client.Interfaces.Main;
using static Client.Interfaces.Main.DataGridHelperColumn;
using Newtonsoft.Json;
using Client.Interfaces.Shipments;
using System.ComponentModel;
using Client.Assets.HighLighters;
using NPOI.SS.Formula.Functions;
using Gu.Wpf.DataGrid2D;
using System.Data;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Client.Interfaces.Stock._WaterhouseControl;
using AutoUpdaterDotNET;
using Client.Interfaces.Production;
using CodeReason.Reports;
using System.IO.Packaging;
using System.IO;
using System.Windows.Xps.Packaging;
using System.Reflection;
using Xceed.Wpf.Toolkit;
using System.Printing;
using static DevExpress.XtraPrinting.Native.ExportOptionsPropertiesNames;
using Xceed.Wpf.Toolkit.Primitives;
using Client.Interfaces.Service.Printing;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Состояние хранилищ wms
    /// </summary>
    /// <author>eletskikh_ya</author>
    public partial class WaterhouseState : ControlBase
    {
        public WaterhouseState()
        {
            ControlTitle = "Состояние склада";
            DocumentationUrl = "doc/l-pack-erp/warehouse/warehouseControl/warehouse_state";
            RoleName = "[erp]warehouse_control";
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
                StorageGridInit();
                ItemGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                StorageGrid.Destruct();
                ItemGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                StorageGrid.ItemsAutoUpdate = true;
                StorageGrid.Run();

            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                StorageGrid.ItemsAutoUpdate = false;
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
                    ButtonControl = RefreshStorageButton,
                    ButtonName = "RefreshStorageButton",
                    Action = () =>
                    {
                        StorageGrid?.LoadItems();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "print",
                    Group = "main",
                    Enabled = false,
                    Title = "Печать этикеток",
                    Description = "Печать этикеток для выбранных хранилищ",
                    ButtonUse = true,
                    ButtonControl = PrintButton,
                    ButtonName = "PrintButton",
                    Action = () =>
                    {
                        Print();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (StorageGrid != null && StorageGrid.Items != null && StorageGrid.Items.Count > 0)
                        {
                            if ((StorageGrid.SelectedItem != null && StorageGrid.SelectedItem.Count > 0)
                                || (StorageGrid.GetItemsSelected().Count > 0))
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "export_to_excel",
                    Group = "main",
                    Enabled = false,
                    Title = "В Excel",
                    Description = "Выгрузить данные в Excel",
                    ButtonUse = true,
                    ButtonControl = ExportToExcelButton,
                    ButtonName = "ExportToExcelButton",
                    Action = () =>
                    {
                        ExportToExcel();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (StorageGrid != null && StorageGrid.Items != null && StorageGrid.Items.Count > 0)
                        {
                            result = true;
                        }

                        return result;
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

            Commander.SetCurrentGridName("StorageGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "add_storage",
                    Title = "Добавить",
                    Description = "Добавить хранилище",
                    Group = "storage_grid_default",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = AddStorageButton,
                    ButtonName = "AddStorageButton",
                    Enabled = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        AddStorage();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "edit_storage",
                    Title = "Изменить",
                    Description = "Изменить хранилище",
                    Group = "storage_grid_default",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = EditStorageButton,
                    ButtonName = "EditStorageButton",
                    Enabled = false,
                    AccessLevel = Role.AccessMode.FullAccess,
                    HotKey = "DoubleCLick",
                    Action = () =>
                    {
                        EditStorage();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (StorageGrid != null && StorageGrid.SelectedItem != null && StorageGrid.SelectedItem.Count > 0)
                        {
                            result = true;
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "edit_storage_status_block",
                    Title = "Заблокировать",
                    Description = "Заблокировать хранилища",
                    Group = "storage_grid_edit_status",
                    MenuUse = true,
                    Enabled = false,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        EditStorageStatus(StorageStatus.Blocked);
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (StorageGrid != null && StorageGrid.Items != null && StorageGrid.Items.Count > 0)
                        {
                            if ((StorageGrid.SelectedItem != null && StorageGrid.SelectedItem.Count > 0 && StorageGrid.SelectedItem.CheckGet("WMSS_ID").ToInt() == (int)StorageStatus.Unblocked)
                                || (StorageGrid.GetItemsSelected()?.Count(x => x.CheckGet("WMSS_ID").ToInt() == (int)StorageStatus.Unblocked) > 0))
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "edit_storage_status_unblock",
                    Title = "Разблокировать",
                    Description = "Разблокировать хранилища",
                    Group = "storage_grid_edit_status",
                    MenuUse = true,
                    Enabled = false,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        EditStorageStatus(StorageStatus.Unblocked);
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (StorageGrid != null && StorageGrid.Items != null && StorageGrid.Items.Count > 0)
                        {
                            if ((StorageGrid.SelectedItem != null && StorageGrid.SelectedItem.Count > 0 && StorageGrid.SelectedItem.CheckGet("WMSS_ID").ToInt() == (int)StorageStatus.Blocked)
                                || (StorageGrid.GetItemsSelected()?.Count(x => x.CheckGet("WMSS_ID").ToInt() == (int)StorageStatus.Blocked) > 0))
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
            }

            Commander.Init(this);
        }

        private ListDataSet StorageGridDataSet { get; set; }

        private ListDataSet ItemGridDataSet { get; set; }

        enum StorageStatus
        {
            Blocked = 1,
            Unblocked = 2
        };

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            StorageGridDataSet = new ListDataSet();
            ItemGridDataSet = new ListDataSet();

            FormHelper.ComboBoxInitHelper(ZoneFilter, "Warehouse", "Zone", "List", "WMZO_ID", "ZONE_FULL_NAME", null, true, true);
            ZoneFilter.SetSelectedItemByKey("1");
        }

        /// <summary>
        /// Инициализация грида хранилищ
        /// </summary>
        private void StorageGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="*",
                        Path="_SELECTED",
                        ColumnType=ColumnTypeRef.Boolean,
                        Editable=true,
                        Width2 = 4,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="WMST_ID",
                        Doc="ID хранилища",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ячейка",
                        Path="NUM",
                        Doc="Ячейка",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус",
                        Path="STATUS",
                        Doc="Статус",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 13,
                        Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = string.Empty;

                                    int currentStatus = row.CheckGet("WMSS_ID").ToInt();

                                    switch(currentStatus)
                                    {
                                        case 1: // Заблокирована
                                            color = HColor.Red;
                                            break;
                                         case 6: // Недоступна
                                            color = HColor.Yellow;
                                            break;
                                        case 2: // Свободна
                                            color = HColor.Blue;
                                            break;
                                        case 3: // Забронирована
                                            color = HColor.Green;
                                            break;
                                        case 4: // Частично занята
                                            break;
                                        case 5: // Занята
                                            color = HColor.Green;
                                            break;
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
                        Header="Тип ячейки",
                        Path="STORAGE_TYPE",
                        Doc="Тип ячейки",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 20,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Область хранения",
                        Path="AREA",
                        Doc="Область хранения",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 26,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вес",
                        Description="Ограничение по весу для этойго хранилища",
                        Path="WEIGHT_NAME",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Глубина",
                        Description="Глубина хранилища",
                        Path="DEPTH_NAME",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ширина",
                        Description="Ширина хранилища",
                        Path="WIDTH_NAME",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Высота",
                        Description="Высота хранилища",
                        Path="HEIGHT_NAME",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ограничение по количеству",
                        Description="Ограничение по количеству ТМЦ в хранилище",
                        Path="ITEM_LIMIT_CNT_NAME",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество в хранилище",
                        Description="Текущее количество ТМЦ в хранилище",
                        Path="ITEM_CNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Зона",
                        Path="ZONE",
                        Doc="Зона",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 15,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Приоритет",
                        Path="PRIORITY",
                        Doc="Приоритет",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Фиксированный приоритет",
                        Path="PRIORITY_FIXED_FLAG",
                        Doc="Фиксированный приоритет",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2 = 6,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Ид статуса",
                        Path="WMSS_ID",
                        Doc="Ид статуса",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden =true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид зоны",
                        Path="WMZO_ID",
                        Doc="Ид зоны",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden =true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Склад",
                        Path="WAREHOUSE",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 5,
                        Hidden =true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид склада",
                        Path="WMWA_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden =true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид типа ячейки",
                        Path="WMSY_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden =true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ограничение по весу хранилища",
                        Path="WEIGHT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden =true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Глубина хранилища",
                        Path="DEPTH",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden =true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ширина хранилища",
                        Path="WIDTH",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden =true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Высота хранилища",
                        Path="HEIGHT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden =true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ограничение по количеству в хранилище",
                        Path="ITEM_LIMIT_CNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden =true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид области хранения",
                        Path="WMSA_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden =true,
                    },
                };
                StorageGrid.SetColumns(columns);
                StorageGrid.SetPrimaryKey("WMST_ID");
                StorageGrid.SearchText = StorageSearchBox;
                StorageGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                StorageGrid.AutoUpdateInterval = 60 * 5;
                StorageGrid.Toolbar = StorageGridToolbar;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                StorageGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem != null)
                    {
                        if (StorageGrid != null && StorageGrid.Items != null && StorageGrid.Items.Count > 0)
                        {
                            if (StorageGrid.Items.FirstOrDefault(x => x.CheckGet("WMST_ID").ToInt() == selectedItem.CheckGet("WMST_ID").ToInt()) == null)
                            {
                                StorageGrid.SelectRowFirst();
                            }
                        }

                        ItemGridLoadItems();
                    }
                };

                StorageGrid.OnLoadItems = StorageGridLoadItems;
                StorageGrid.OnFilterItems = () =>
                {
                    if (StorageGrid.Items != null && StorageGrid.Items.Count > 0)
                    {
                        if (ZoneFilter != null && ZoneFilter.SelectedItem.Key != null)
                        {
                            var key = ZoneFilter.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            items.AddRange(StorageGrid.Items.Where(x => x.CheckGet("WMZO_ID").ToInt() == key));

                            StorageGrid.Items = items;
                        }
                    }
                };

                StorageGrid.Commands = Commander;

                StorageGrid.Init();
            }
        }

        /// <summary>
        /// Загрузка данных
        /// </summary>
        private async void StorageGridLoadItems()
        {
            if (StorageGrid != null && StorageGrid.SelectedItem != null && StorageGrid.SelectedItem.Count > 0)
            {
                StorageGrid.Commands.Message = new ItemMessage() { Action = "refresh", Message = $"{StorageGrid.SelectedItem.CheckGet("WMST_ID")}" };
            }

            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "Storage");
            q.Request.SetParam("Action", "List");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            StorageGridDataSet.Items.Clear();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    StorageGridDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            StorageGrid.UpdateItems(StorageGridDataSet);
        }

        /// <summary>
        /// Инициализация грида складских единиц в хранилище
        /// </summary>
        private void ItemGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="WMIT_ID",
                        Doc="ID ТМЦ",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="NAME",
                        Doc="Наименование тмц",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 60,
                    },
                    new DataGridHelperColumn
                    {
                        Header=" ",
                        Path="_",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=5,
                        MaxWidth=2000,
                    },
                };
                ItemGrid.SetColumns(columns);
                ItemGrid.SetPrimaryKey("WMIT_ID");
                ItemGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                ItemGrid.AutoUpdateInterval = 0;
                ItemGrid.Toolbar = ItemGridToolbar;
                
                //данные грида
                ItemGrid.OnLoadItems = ItemGridLoadItems;

                ItemGrid.Commands = Commander;

                ItemGrid.Init();
            }
        }

        /// <summary>
        /// Загрузка данными грида
        /// </summary>
        private void ItemGridLoadItems()
        {
            if (StorageGrid.SelectedItem != null)
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("WMST_ID", StorageGrid.SelectedItem.CheckGet("WMST_ID"));
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "Item");
                q.Request.SetParam("Action", "ListByCell");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                ItemGridDataSet.Items.Clear();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        ItemGridDataSet = ListDataSet.Create(result, "ITEMS");
                    }
                }
                ItemGrid.UpdateItems(ItemGridDataSet);
            }
        }

        private void AddStorage()
        {
            var warehouseStorage = new WarehouseStorage();
            warehouseStorage.Show();
        }

        public void EditStorage()
        {
            if (StorageGrid.SelectedItem != null)
            {
                var warehouseStorage = new WarehouseStorage();
                warehouseStorage.StorageId = StorageGrid.SelectedItem.CheckGet("WMST_ID").ToInt();
                warehouseStorage.Show();
            }
        }

        public void ExportToExcel()
        {
            StorageGrid.ItemsExportExcel();
        }

        /// <summary>
        /// Печать этикеток для хранилища
        /// </summary>
        private void Print()
        {
            if (StorageGrid != null && StorageGrid.Items != null && StorageGrid.Items.Count > 0
                && 
                (StorageGrid.GetItemsSelected().Count > 0
                || StorageGrid.SelectedItem != null)
                )
            {
                var selectedRowList = StorageGrid.GetItemsSelected();
                if (selectedRowList.Count > 0)
                {
                    string message = $"Вы действительно хотите напечатать этикетки для выбранных {selectedRowList.Count} хранилищ?";
                    var dw = new DialogWindow(message, this.ControlTitle, "Подтверждение печати этикеток", DialogWindowButtons.YesNo);
                    if (dw.ShowDialog() == true)
                    {
                        foreach (var selectedRow in selectedRowList)
                        {
                            BarcodeGenerator generator = new BarcodeGenerator();
                            generator.AddStorage(selectedRow.CheckGet("NUM"), selectedRow.CheckGet("WMST_ID").ToInt().ToString());
                            var doc = generator.GenerateDocument();
                            var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;

                            PrintDocument(paginator);
                        }

                        SelectAllStorageCheckBox.IsChecked = false;
                    }
                }
                else if(StorageGrid.SelectedItem != null)
                {
                    string message = $"Вы действительно хотите напечатать этикетку для выбранного хранилища?";
                    var dw = new DialogWindow(message, this.ControlTitle, "Подтверждение печати этикеток", DialogWindowButtons.YesNo);
                    if (dw.ShowDialog() == true)
                    {
                        BarcodeGenerator generator = new BarcodeGenerator();
                        generator.AddStorage(StorageGrid.SelectedItem.CheckGet("NUM"), StorageGrid.SelectedItem.CheckGet("WMST_ID").ToInt().ToString());
                        var doc = generator.GenerateDocument();
                        var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;

                        PrintDocument(paginator);
                    }
                }
            }
        }

        public void PrintDocument(DocumentPaginator documentPaginator)
        {
            var printHelper = new PrintHelper();
            printHelper.PrintingProfile = PrintingSettings.RawLabelPrinter.ProfileName;
            printHelper.PrintingDuplex = System.Drawing.Printing.Duplex.Simplex;
            printHelper.PrintingLandscape = true;
            printHelper.Init();
            var printingResult = printHelper.StartPrinting(documentPaginator);
            printHelper.Dispose();
        }

        public void SetPrintSettings()
        {
            var i = new PrintingInterface();
        }

        private void UpdateStatus(int storageId, int storageStatusId)
        {
            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("WMST_ID", storageId.ToString());
                p.CheckAdd("WMSS_ID", storageStatusId.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "Storage");
            q.Request.SetParam("Action", "UpdateStatus");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status != 0)
            {
                q.ProcessError();
            }
        }

        private void EditStorageStatus(StorageStatus status)
        {
            bool resume = false;

            string operationName = "";
            int needStorageStatus = 0;
            switch (status)
            {
                case StorageStatus.Blocked:
                    operationName = "заблокировать";
                    needStorageStatus = (int)StorageStatus.Unblocked;
                    break;
                case StorageStatus.Unblocked:
                    operationName = "разблокировать";
                    needStorageStatus = (int)StorageStatus.Blocked;
                    break;
            }

            var selectedItems = StorageGrid.GetItemsSelected();
            if (selectedItems.Count > 0)
            {
                var dw = new DialogWindow($"Вы действительно хотите {operationName} {selectedItems.Count} выбранные хранилища?", this.ControlName, "Подтверждение смены статуса хранилища", DialogWindowButtons.YesNo);
                if (dw.ShowDialog() == true)
                {
                    resume = true;
                    foreach (var item in selectedItems)
                    {
                        if (item.CheckGet("WMSS_ID").ToInt() == needStorageStatus)
                        {
                            UpdateStatus(item.CheckGet("WMST_ID").ToInt(), (int)status);
                        }
                    }

                    SelectAllStorageCheckBox.IsChecked = false;
                }
            }
            else if (StorageGrid.SelectedItem != null)
            {
                var dw = new DialogWindow($"Вы действительно хотите {operationName} выбранное хранилище?", this.ControlName, "Подтверждение смены статуса хранилища", DialogWindowButtons.YesNo);
                if (dw.ShowDialog() == true)
                {
                    resume = true;

                    if (StorageGrid.SelectedItem.CheckGet("WMSS_ID").ToInt() == needStorageStatus)
                    {
                        UpdateStatus(StorageGrid.SelectedItem.CheckGet("WMST_ID").ToInt(), (int)status);
                    }
                }
            }

            if (resume)
            {
                StorageGrid.LoadItems();
            }
        }

        /// <summary>
        /// отметка всех строк в гриде хранилищ
        /// </summary>
        public void SelectAllStorage()
        {
            var isChecked = (bool)SelectAllStorageCheckBox.IsChecked;
            StorageGrid.SelectAllRows(isChecked);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings();
        }

        private void StorageFilter_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            StorageGrid.UpdateItems();
        }

        private void SelectAllStorageCheckBox_Click(object sender, RoutedEventArgs e)
        {
            SelectAllStorage();
        }









        /// <summary>
        /// Возвращает Dictionary<string,string> данными из БД
        /// </summary>
        /// <param name="Action">Имя контролера</param>
        /// <param name="Key">Имя ключа</param>
        /// <param name="Value">Имя значения</param>
        /// <param name="param">параметры для получения данных, если параметры не нужны то null</param>
        /// <param name="Int2Num">Если данный параметр true то данные в ключ selectbox будут преборазоованны Key.ToInt().ToString() что бы убрать .0 </param>
        public static Dictionary<string, string> GetDictionaryHelper(string Action, string Key, string Value, Dictionary<string, string> param = null, bool Int2Num = false)
        {
            Dictionary<string, string> resultList = null;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "Warehouse");
            q.Request.SetParam("Action", Action);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            if (param != null)
            {
                q.Request.SetParams(param);
            }

            q.DoQuery();

            if (q.Answer.Status == 0)
            {

                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    resultList = ListDataSet.Create(result, "ITEMS").GetItemsList(Key, Value);
                }
            }

            return resultList;
        }


    }
}
