using System;
using Client.Common;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using GalaSoft.MvvmLight.Messaging;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using Client.Interfaces.Main;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Справочник склада
    /// </summary>
    /// <author>eletskikh_ya</author>
    public partial class CatalogTopology : UserControl
    {
        public CatalogTopology()
        {
            InitializeComponent();

            WarehouseGridInit();
            ZoneGridInit();
            RowsGridInit();
            CellsGridInit();
            LevelGridInit();

            //FIXME:
            // предотвращение 2йного срабатывания события selectItem
            WarehouseGrid.SelectItemMode = 0;
            RowsGrid.SelectItemMode = 0;
            CellsGrid.SelectItemMode = 0;
            LevelGrid.SelectItemMode = 0;

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            ProcessPermissions();
        }

        /// <summary>
        /// текущий склад
        /// </summary>
        Dictionary<string, string> warehouseSelectedItem;

        /// <summary>
        /// текущий ряд
        /// </summary>
        Dictionary<string, string> rowGridSelectedItem;

        /// <summary>
        /// текущая ячейка
        /// </summary>
        Dictionary<string, string> cellGridSelectedItem;

        /// <summary>
        /// текущий уровень
        /// </summary>
        Dictionary<string, string> levelGridSelectedItem;

        /// <summary>
        /// выбранная зона
        /// </summary>
        private Dictionary<string, string> zoneSelectedItem;

   
        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
           
        }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "wms",
                ReceiverName = "",
                SenderName = "wms_list",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            LevelGrid.Destruct();
            WarehouseGrid.Destruct();
            RowsGrid.Destruct();
            CellsGrid.Destruct();
            ZoneGrid.Destruct();

        }

        /// <summary>
        /// обработка ввода с клавиатуры (роли)
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.F5:
                    CellsGrid.LoadItems();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

                case Key.Home:
                    CellsGrid.SetSelectToFirstRow();
                    e.Handled = true;
                    break;

                case Key.End:
                    CellsGrid.SetSelectToLastRow();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// Документация
        /// </summary>
        private void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/warehouse/warehouseDirectory/warehouseDirectory");
        }

        /// <summary>
        /// Инициализация грида уровней
        /// </summary>
        public void LevelGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="WMLE_ID",
                        Doc="ИД Уровня",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ярус",
                        Path="LEVEL_NUM",
                        Doc="Наименование уровня",
                        ColumnType=ColumnTypeRef.String,
                        Width = 120,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Приоритет",
                        Path="PRIORITY",
                        Doc="Приоритет",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 80,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Сортировка",
                        Path="ORDER_NUM",
                        Doc="Сортировка",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 85,
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

                LevelGrid.SelectItemMode = 2;

                LevelGrid.PrimaryKey = "WMLE_ID";
                LevelGrid.SetColumns(columns);

                LevelGrid.SearchText = SearchLevel;

                LevelGrid.Label = "LevelGrid";
                LevelGrid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                LevelGrid.OnSelectItem = selectedItem =>
                {
                    levelGridSelectedItem = selectedItem;
                    DeleteLevelButton.IsEnabled = levelGridSelectedItem.CheckGet("DELETE_IS").ToInt() == 1;

                    ProcessPermissions();
                };

                //двойной клик на строке откроет форму редактирования
                LevelGrid.OnDblClick = selectedItem =>
                {
                    EditWarehouseLevel();
                };

                // подготовка меню
                LevelGrid.Menu = new Dictionary<string, DataGridContextMenuItem>
                {
                    {
                        "1",
                        new DataGridContextMenuItem()
                        {
                            Header = "Создать хранилище",
                            Tag = "access_mode_full_access",
                            Action = () => { CreateStorageFromCell(true); }
                        }
                    },

                };

                //данные грида
                LevelGrid.OnLoadItems = LevelGridLoadItems;

                LevelGrid.Run();
            }
        }

        /// <summary>
        /// Инициализация грида ячеек
        /// </summary>
        public void CellsGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="WMCE_ID",
                        Doc="ИД Ячейки",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Место",
                        Path="CELL_NUM",
                        Doc="Наименование ячейки",
                        ColumnType=ColumnTypeRef.String,
                        Width = 120,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Приоритет",
                        Path="PRIORITY",
                        Doc="Приоритет",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 80,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Сортировка",
                        Path="ORDER_NUM",
                        Doc="Сортировка",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 85,
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

                CellsGrid.PrimaryKey = "WMCE_ID";
                CellsGrid.SetColumns(columns);

                CellsGrid.SearchText = SearchCell;
                CellsGrid.Label = "CellsGrid";
                CellsGrid.Init();

                //данные грида
                CellsGrid.OnLoadItems = CellsGridLoadItems;
                
                CellsGrid.Run();

                // подготовка меню
                CellsGrid.Menu = new Dictionary<string, DataGridContextMenuItem>
                {
                    {
                        "1",
                        new DataGridContextMenuItem()
                        {
                            Header = "Создать хранилище",
                            Tag = "access_mode_full_access",
                            Action = () => { CreateStorageFromCell(); }
                        }
                    },
                };

                //при выборе строки в гриде, обновляются актуальные действия для записи
                CellsGrid.OnSelectItem = selectedItem =>
                {
                    cellGridSelectedItem = selectedItem;
                    CellsGrid.Menu["1"].Enabled = false;
                    DellCellButton.IsEnabled = cellGridSelectedItem.CheckGet("DELETE_IS").ToInt() == 1;
                    CheckPossibilityCreateStorageFromRow();

                    ProcessPermissions();
                };

                //двойной клик на строке откроет форму редактирования
                CellsGrid.OnDblClick = selectedItem =>
                {
                    EditWarehouseCell();
                };
            }
        }

        private async void CheckPossibilityCreateStorageFromRow()
        {
            Dictionary<string, string> p = new Dictionary<string, string>();
            p.CheckAdd("WMRO_ID", rowGridSelectedItem.CheckGet("WMRO_ID"));
            p.CheckAdd("WMCE_ID", cellGridSelectedItem.CheckGet("WMCE_ID"));

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "Storage");
            q.Request.SetParam("Action", "GetByRowCell");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            // асинхронно не успевает обновить статус меню
            //await Task.Run(() =>
            //{
                q.DoQuery();
            //});

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if(ds.Items.Count > 0)
                    {
                        CellsGrid.Menu["1"].Enabled = false;
                    }
                    else
                    {
                        CellsGrid.Menu["1"].Enabled = true;
                    }
                }
            }
        }

        /// <summary>
        /// Инициализация грида рядов
        /// </summary>
        public void RowsGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="WMRO_ID",
                        Doc="ИД Ряда",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ряд",
                        Path="ROW_NUM",
                        Doc="Наименование Ряда",
                        ColumnType=ColumnTypeRef.String,
                        Width = 120,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Приоритет",
                        Path="PRIORITY",
                        Doc="Приоритет",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 80,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Сортировка",
                        Path="ORDER_NUM",
                        Doc="Сортировка",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 85,
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

                RowsGrid.SelectItemMode = 2;

                RowsGrid.PrimaryKey = "WMRO_ID";
                RowsGrid.SetColumns(columns);

                RowsGrid.SearchText = SearchRows;
                RowsGrid.Label = "RowsGrid";
                RowsGrid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                RowsGrid.OnSelectItem = selectedItem =>
                {
                    rowGridSelectedItem = selectedItem;
                    DeleteRowsButton.IsEnabled = rowGridSelectedItem.CheckGet("DELETE_IS").ToInt() == 1;

                    ProcessPermissions();
                };

                //двойной клик на строке откроет форму редактирования
                RowsGrid.OnDblClick = selectedItem =>
                {
                    EditWarehouseRow();
                };

                //данные грида
                RowsGrid.OnLoadItems = RowsGridLoadItems;

                RowsGrid.Run();
            }
        }

        private void CreateStorageFromCell(bool fromLevel = false)
        {
            var warehouseStorage = new WarehouseStorage();
            warehouseStorage.FromLevel = fromLevel;
            warehouseStorage._WarehouseId = warehouseSelectedItem.CheckGet("WMWA_ID").ToInt();
            warehouseStorage._ZoneId = zoneSelectedItem.CheckGet("WMZO_ID").ToInt();
            warehouseStorage._RowId = rowGridSelectedItem.CheckGet("WMRO_ID").ToInt();
            warehouseStorage._CellId = cellGridSelectedItem.CheckGet("WMCE_ID").ToInt();
            warehouseStorage._LevelId = fromLevel ? levelGridSelectedItem.CheckGet("WMLE_ID").ToInt() : 0;
            warehouseStorage.Show();
        }

        /// <summary>
        /// Инициализация грида склад
        /// </summary>
        public void WarehouseGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="WMWA_ID",
                        Doc="ИД Склада",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Склад",
                        Path="WAREHOUSE",
                        Doc="Наименование склада",
                        ColumnType=ColumnTypeRef.String,
                        Width = 190,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Площадка",
                        Path="FACTORY_NAME",
                        Doc="Наименование площадки",
                        ColumnType=ColumnTypeRef.String,
                        Width = 120,
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

                WarehouseGrid.SelectItemMode = 2;

                WarehouseGrid.PrimaryKey = "WMWA_ID";
                WarehouseGrid.SetColumns(columns);

                WarehouseGrid.SearchText = WarehouseSearchBox;
                
                WarehouseGrid.Label = "WarehouseGrid";
                WarehouseGrid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                WarehouseGrid.OnSelectItem = selectedItem =>
                {
                    WarehouseGridUpdateActions(selectedItem);
                };

                //двойной клик на строке откроет форму редактирования
                WarehouseGrid.OnDblClick = selectedItem =>
                {
                    //WarehouseEdit();
                    if (Central.Navigator.GetRoleLevel("[erp]warehouse_directory") >= Role.AccessMode.FullAccess)
                    {
                        EditWarehouse();
                    }
                };

                //данные грида
                WarehouseGrid.OnLoadItems = WarehouseGridLoadItems;

                WarehouseGrid.Run();

                //фокус ввода           
                WarehouseGrid.Focus();
            }
        }

        /// <summary>
        /// Инициализация грида зон
        /// </summary>
        public void ZoneGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="WMZO_ID",
                        Doc="ИД Склада",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Зона",
                        Path="ZONE",
                        Doc="Зона склада",
                        ColumnType=ColumnTypeRef.String,
                        Width = 190,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Штрихкод ячейки{Environment.NewLine}Использование штрихкода для ячейки",
                        Path="BARCODE_STORAGE_USAGE_FLAG",
                        Doc="Использование штрихкода для ячейки",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width = 102,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Штрихкод ТМЦ{Environment.NewLine}Использование штрихкода для ТМЦ",
                        Path="BARCODE_ITEM_USAGE_FLAG",
                        Doc="Использование штрихкода для ТМЦ",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width = 102,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Проверочный код{Environment.NewLine}Использование проверочного кода",
                        Path="CODE_ITEM_VERIFY_FLAG",
                        Doc="Использование проверочного кода",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width = 102,
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

                ZoneGrid.PrimaryKey = "WMZO_ID";
                ZoneGrid.SetColumns(columns);

                ZoneGrid.SelectItemMode = 2;

                ZoneGrid.SearchText = ZoneSearchBox;

                ZoneGrid.Label = "ZoneGrid";
                ZoneGrid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                ZoneGrid.OnSelectItem = selectedItem =>
                {
                    ZoneGridUpdateActions(selectedItem);
                };

                //двойной клик на строке откроет форму редактирования
                ZoneGrid.OnDblClick = selectedItem =>
                {
                    ZoneEdit();
                };

                //данные грида
                ZoneGrid.OnLoadItems = ZoneGridLoadItems;

                ZoneGrid.Run();               
            }
        }

        private async void ZoneGridLoadItems()
        {
            if(warehouseSelectedItem!=null)
            {
                var p = new Dictionary<string, string>();

                p.Add("WMWA_ID", warehouseSelectedItem.CheckGet("WMWA_ID"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "Zone");
                q.Request.SetParam("Action", "ListByWarehouse");
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
                        ZoneGrid.UpdateItems(ds);
                    }
                }
            }
        }

        private void ZoneEdit()
        {
            if (Central.Navigator.GetRoleLevel("[erp]warehouse_directory") >= Role.AccessMode.FullAccess)
            {
                if (zoneSelectedItem != null)
                {
                    int id = zoneSelectedItem.CheckGet("WMZO_ID").ToInt();

                    var editZone = new FormExtend()
                    {
                        FrameName = $"Zone_{id}", /// можно просто генерировать некий уникальный guid
                        ID = $"WMZO_ID",
                        Id = id,
                        Title = $"Зона {id}",

                        QueryGet = new FormExtend.RequestData()
                        {
                            Module = "Warehouse",
                            Object = "Zone",
                            Action = "Get"
                        },

                        QuerySave = new FormExtend.RequestData()
                        {
                            Module = "Warehouse",
                            Object = "Zone",
                            Action = "Save"
                        },

                        Fields = new List<FormHelperField>()
                    {
                        new FormHelperField()
                        {
                            Path="ZONE",
                            FieldType=FormHelperField.FieldTypeRef.String,
                            Description = "Наименование: *",
                            ControlType = "TextBox",
                            Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                                { FormHelperField.FieldFilterRef.Required, null },
                                { FormHelperField.FieldFilterRef.MaxLen, 32 },
                            },

                            Width = 220
                        },
                        new FormHelperField()
                        {
                            Path="WMWA_ID",
                            FieldType=FormHelperField.FieldTypeRef.Integer,
                            Description = "Склад: *",
                            ControlType="SelectBox",
                            Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                                { FormHelperField.FieldFilterRef.Required, null },
                            },
                            Width = 180,
                        },
                        new FormHelperField()
                        {
                            Path="WOGR_ID",
                            Description = "Водители:  ",
                            FieldType=FormHelperField.FieldTypeRef.Integer,
                            ControlType="SelectBox",
                            Width = 190,
                        },
                        new FormHelperField()
                        {
                            Path="BARCODE_STORAGE_USAGE_FLAG",
                            FieldType=FormHelperField.FieldTypeRef.Boolean,
                            Description = "Штрихкод для ячейки",
                            ControlType="CheckBox",
                        },
                        new FormHelperField()
                        {
                            Path="BARCODE_ITEM_USAGE_FLAG",
                            FieldType=FormHelperField.FieldTypeRef.Boolean,
                            Description = "Штрихкод для ТМЦ",
                            ControlType="CheckBox",
                        },
                        new FormHelperField()
                        {
                            Path="CODE_ITEM_VERIFY_FLAG",
                            FieldType=FormHelperField.FieldTypeRef.Boolean,
                            Description = "Использование проверочных кодов",
                            ControlType="CheckBox",
                        },
                    }
                    };

                    editZone["WMWA_ID"].OnAfterCreate += (control) =>
                    {
                        FormHelper.ComboBoxInitHelper(control as SelectBox, "Warehouse", "Warehouse", "List", "WMWA_ID", "WAREHOUSE", null, true);
                        control.IsEnabled = true;
                    };

                    editZone["WOGR_ID"].OnAfterCreate += (control) =>
                    {
                        List<string> groups = new List<string>()
                    {
                        "driver_bdm_1",
                        "driver_bdm_2",
                        "driver_stock_stacker",
                        "driver_container_scrap",
                        "driver_container_stock"
                    };

                        var ForkliftDriverGroup = control as SelectBox;
                        ForkliftDriverGroup.Items.CheckAdd("0", "Нет");

                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Accounts");
                        q.Request.SetParam("Object", "Group");
                        q.Request.SetParam("Action", "List");

                        q.DoQuery();

                        if (q.Answer.Status == 0)
                        {
                            var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                            if (result != null)
                            {
                                var items = ListDataSet.Create(result, "ITEMS");
                                foreach (var item in items.Items)
                                {
                                    if (groups.Contains(item.CheckGet("CODE")))
                                    {
                                        ForkliftDriverGroup.Items.CheckAdd(item.CheckGet("ID").ToInt().ToString(), item.CheckGet("NAME"));
                                        ForkliftDriverGroup.UpdateListItems(ForkliftDriverGroup.Items);
                                    }
                                }
                            }
                        }
                    };

                    editZone.OnAfterSave += (id, result) =>
                    {
                        ZoneGrid.LoadItems();
                        ZoneGrid.SetSelectedItemId(id.ToString(), "WMZO_ID");
                    };

                    editZone.Show();
                }
            }
        }

        private void ZoneCreate()
        {
            new WarehouseZone().Create( warehouseSelectedItem );
        }

        private void ZoneGridUpdateActions(Dictionary<string, string> selectedItem)
        {
            zoneSelectedItem = selectedItem;
            ProcessPermissions();
        }

        /// <summary>
        /// обновление методов работы с выбранной в гриде строкой
        /// </summary>
        /// <param name="selectedItem"></param>
        public void WarehouseGridUpdateActions(Dictionary<string, string> selectedItem)
        {
            warehouseSelectedItem = selectedItem;

            ZoneGrid.ClearItems();
            LevelGrid.ClearItems();
            CellsGrid.ClearItems();
            RowsGrid.ClearItems();
            
            ZoneGrid.LoadItems();
            RowsGrid.LoadItems();
            CellsGrid.LoadItems();
            LevelGrid.LoadItems();

            ProcessPermissions();
        }

        /// <summary>
        /// Загрузка данными грида с уровнями
        /// </summary>
        public async void LevelGridLoadItems()
        {
            if (warehouseSelectedItem != null)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("WMWA_ID", warehouseSelectedItem.CheckGet("WMWA_ID"));
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "Level");
                q.Request.SetParam("Action", "ListByWarehouse");
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
                        LevelGrid.UpdateItems(ds);
                    }
                }
            }
        }

        /// <summary>
        /// Загрузка данными грида с ячейками
        /// </summary>
        public async void CellsGridLoadItems()
        {
            if(warehouseSelectedItem != null)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("WMWA_ID", warehouseSelectedItem.CheckGet("WMWA_ID"));
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "Cell");
                q.Request.SetParam("Action", "ListByWarehouse");
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
                        CellsGrid.UpdateItems(ds);

                        if (CellsGrid.Items == null || CellsGrid.Items.Count == 0)
                        {
                            DeleteRowsButton.IsEnabled = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// загрузка данными грида с рядами
        /// </summary>
        public async void RowsGridLoadItems()
        {
            DeleteRowsButton.IsEnabled = false;

            if (warehouseSelectedItem != null)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("WMWA_ID", warehouseSelectedItem.CheckGet("WMWA_ID"));
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "Row");
                q.Request.SetParam("Action", "ListByWarehouse");
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
                        RowsGrid.UpdateItems(ds);
                    }
                }
            }
        }

        /// <summary>
        /// загрузка грида со складами
        /// </summary>
        public async void WarehouseGridLoadItems()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "Warehouse");
            q.Request.SetParam("Action", "List");
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
                    WarehouseGrid.UpdateItems(ds);
                }
            }

        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.IndexOf("WMS") > -1)
            {
                //Обновить грид в зависимости от того кто отослал сообщение
                if (m.ReceiverName.IndexOf("WMS_list") > -1)
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            if (m.SenderName == "WMSRow" || m.SenderName == "WarehouseRow")
                            {
                                // был изменен или создан ряд, обновим его и выставим на нужную позицию
                                // ".0" в ID необходима для правильной работы, потенциально может принести проблемы
                                // когда данная проблема будет устранена, то это, возможно, перестанет работать
                                RowsGrid.LoadItems();
                                RowsGrid.SetSelectedItemId(m.Message + ".0", "WMRO_ID");
                            }
                            else if(m.SenderName== "WMSCell" || m.SenderName == "WarehouseCell")
                            {
                                // была обновлена или создана ячейка, обновим грид яччеек и выставим на нужное положение
                                CellsGrid.LoadItems();
                                CellsGrid.SetSelectedItemId(m.Message + ".0", "WMCE_ID");
                            }
                            else if(m.SenderName== "WMSLevel")
                            {
                                // был обновлен или создан уровень, обновим грид уровней и выставим на нужное положение
                                LevelGrid.LoadItems();
                                LevelGrid.SetSelectedItemId(m.Message + ".0", "WMLE_ID");
                            }
                            else if(m.SenderName == "WMSZone")
                            {
                                ZoneGrid.LoadItems();
                                ZoneGrid.SetSelectedItemId(m.Message + ".0", "WMZO_ID");
                            }
                            else if(m.SenderName == "WMSStorage" || m.SenderName == "WarehouseStorage")
                            {
                                if(m.ContextObject!=null)
                                {
                                    if(m.ContextObject is bool)
                                    {
                                        bool FromLevel = (bool)m.ContextObject;

                                        if(FromLevel)
                                        {
                                            LevelGrid.LoadItems();
                                        }
                                        else
                                        {
                                            CellsGrid.LoadItems();
                                        }
                                    }
                                }

                            }
                            else
                            {
                                WarehouseGrid.LoadItems();
                                WarehouseGrid.SetSelectedItemId(m.Message + ".0", "WMWA_ID");
                            }

                            break;
                    }
                }
            }
        }


        /// <summary>
        ///  Редактирование склада
        /// </summary>
        private void EditWarehouse()
        {
            if (warehouseSelectedItem != null)
            {
                var i = new Warehouse();
                // вызов без id приводит к созданию новго склада
                i.Edit(warehouseSelectedItem.CheckGet("WMWA_ID").ToInt());
            }
        }

        /// <summary>
        /// Редактирование склада
        /// </summary>
        private void WarehouseEdit()
        {
            if (Central.Navigator.GetRoleLevel("[erp]warehouse_directory") >= Role.AccessMode.FullAccess)
            {
                if (warehouseSelectedItem != null)
                {
                    int id = warehouseSelectedItem.CheckGet("WMWA_ID").ToInt();

                    var createWarehouse = new FormExtend()
                    {
                        FrameName = "Warehouse",

                        ID = "WMWA_ID",
                        Id = id,

                        Title = $"Склад {id}",

                        QueryGet = new FormExtend.RequestData()
                        {
                            Module = "Warehouse",
                            Object = "Warehouse",
                            Action = "Get"
                        },

                        QuerySave = new FormExtend.RequestData()
                        {
                            Module = "Warehouse",
                            Object = "Warehouse",
                            Action = "Save"
                        },

                        Fields = new List<FormHelperField>()
                    {
                        new FormHelperField()
                        {
                            Path="WAREHOUSE",
                            Description = "Наименование: *",
                            ControlType = "TextBox",
                            FieldType=FormHelperField.FieldTypeRef.String,
                            Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                                { FormHelperField.FieldFilterRef.Required, null },
                                { FormHelperField.FieldFilterRef.ToUpperCase, null },
                                { FormHelperField.FieldFilterRef.MaxLen, 32 },
                            },
                            Width = 200,
                        },
                    },
                    };

                    createWarehouse["WAREHOUSE"].OnAfterCreate += (control) =>
                    {
                        (control as TextBox).IsReadOnly = true;
                    };

                    createWarehouse.OnAfterSave += (id, result) =>
                    {
                        WarehouseGrid.LoadItems();
                        WarehouseGrid.SetSelectedItemId(id.ToString() + ".0", "WMWA_ID");
                    };

                    createWarehouse.Show();
                }
            }
        }

        /// <summary>
        /// создание ячейки
        /// </summary>
        private void CreateWarehouseCell()
        {
            var warehouseCell = new WarehouseCell();
            warehouseCell.WarehouseId = warehouseSelectedItem.CheckGet("WMWA_ID").ToInt();
            warehouseCell.Show();
        }

        /// <summary>
        /// создание Уровня
        /// </summary>
        private void CreateWarehouseLevel()
        {
            var i = new WarehouseLevel();
            i.Create(warehouseSelectedItem);
        }

        /// <summary>
        /// Редактирование уровня
        /// </summary>
        private void EditWarehouseLevel()
        {
            if (Central.Navigator.GetRoleLevel("[erp]warehouse_directory") >= Role.AccessMode.FullAccess)
            {
                var i = new WarehouseLevel();
                i.Edit(levelGridSelectedItem.CheckGet("WMLE_ID").ToInt(), warehouseSelectedItem);
            }
        }

        /// <summary>
        /// создание ряда
        /// </summary>
        private void CreateWarehouseRow()
        {
            var warehouseRow = new WarehouseRow();
            warehouseRow.WarehouseId = warehouseSelectedItem.CheckGet("WMWA_ID").ToInt();
            warehouseRow.Show();
        }

        /// <summary>
        /// Редактирование ряда
        /// </summary>
        private void EditWarehouseRow()
        {
            if (Central.Navigator.GetRoleLevel("[erp]warehouse_directory") >= Role.AccessMode.FullAccess)
            {
                var warehouseRow = new WarehouseRow();
                warehouseRow.RowId = rowGridSelectedItem.CheckGet("WMRO_ID").ToInt();
                warehouseRow.WarehouseId = warehouseSelectedItem.CheckGet("WMWA_ID").ToInt();
                warehouseRow.Show();
            }
        }

        /// <summary>
        ///  редактирование ячейки
        /// </summary>
        private void EditWarehouseCell()
        {
            if (Central.Navigator.GetRoleLevel("[erp]warehouse_directory") >= Role.AccessMode.FullAccess)
            {
                var warehouseCell = new WarehouseCell();
                warehouseCell.CellId = cellGridSelectedItem.CheckGet("WMCE_ID").ToInt();
                warehouseCell.WarehouseId = warehouseSelectedItem.CheckGet("WMWA_ID").ToInt();
                warehouseCell.Show();
            }
        }

        /// <summary>
        /// создание склада
        /// </summary>
        private void CreateWarehouse()
        {
            var i = new Warehouse();
            // вызов без id приводит к созданию новго склада
            i.Edit(); 
        }

        /// <summary>
        /// Удаление ячейки
        /// </summary>
        private void DeleteCell()
        {
            if (cellGridSelectedItem != null)
            {
                var dw = new DialogWindow($"Вы действительно хотите удалить ячейку {cellGridSelectedItem.CheckGet("CELL_NUM")}?", "Добавление хранилища", "", DialogWindowButtons.NoYes);
                if (dw.ShowDialog() == true)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Warehouse");
                    q.Request.SetParam("Object", "Cell");
                    q.Request.SetParam("Action", "Delete");

                    var p = new Dictionary<string, string>();
                    {
                        p.Add("WMCE_ID", cellGridSelectedItem.CheckGet("WMCE_ID"));
                    }

                    q.Request.SetParams(p);

                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            var id = ds.GetFirstItemValueByKey("ID").ToInt();
                            if (id == 0)
                            {
                                CellsGrid.SetSelectToPrevRow();
                                string selectedItem = cellGridSelectedItem != null ? cellGridSelectedItem.CheckGet("WMCE_ID") : null;
                                CellsGrid.LoadItems();
                                if (selectedItem != null)
                                {
                                    CellsGrid.SetSelectedItemId(selectedItem, "WMCE_ID");
                                }
                            }
                            else
                            {
                                new DialogWindow($"Не удалось удалить ячейку {cellGridSelectedItem.CheckGet("CELL_NUM")} код ошибки {id}", "Удаление ячейки", "", DialogWindowButtons.OK).Show();

                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Удаление ряда
        /// </summary>
        private void DeleteRow()
        {
            if(rowGridSelectedItem!=null)
            {
                var dw = new DialogWindow($"Вы действительно хотите удалить ряд {rowGridSelectedItem.CheckGet("ROW_NUM")}?", "Удаление ряда", "", DialogWindowButtons.NoYes);
                if (dw.ShowDialog() == true)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Warehouse");
                    q.Request.SetParam("Object", "Row");
                    q.Request.SetParam("Action", "Delete");

                    var p = new Dictionary<string, string>();
                    {
                        p.Add("WMRO_ID", rowGridSelectedItem.CheckGet("WMRO_ID"));
                    }

                    q.Request.SetParams(p);

                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            var id = ds.GetFirstItemValueByKey("ID").ToInt();
                            if (id == 0)
                            {
                                RowsGrid.SetSelectToPrevRow();
                                string selectedItem = rowGridSelectedItem != null ? rowGridSelectedItem.CheckGet("WMRO_ID") : null;
                                RowsGrid.LoadItems();
                                if (selectedItem != null)
                                {
                                    LevelGrid.SetSelectedItemId(selectedItem, "WMRO_ID");
                                }
                            }
                            else
                            {
                                new DialogWindow($"Не удалось удалить ячейку {cellGridSelectedItem.CheckGet("CELL_NUM")} код ошибки {id}", "Удаление ячейки", "", DialogWindowButtons.OK).Show();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Удаление уровня
        /// </summary>
        private void DeleteLevel()
        {
            if (levelGridSelectedItem != null)
            {
                var dw = new DialogWindow($"Вы действительно хотите удалить ярус {levelGridSelectedItem.CheckGet("LEVEL_NUM")}?", "Удаления уровня", "", DialogWindowButtons.NoYes);
                if (dw.ShowDialog() == true)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Warehouse");
                    q.Request.SetParam("Object", "Level");
                    q.Request.SetParam("Action", "Delete");

                    var p = new Dictionary<string, string>();
                    {
                        p.Add("WMLE_ID", levelGridSelectedItem.CheckGet("WMLE_ID"));
                    }

                    q.Request.SetParams(p);

                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            var id = ds.GetFirstItemValueByKey("ID").ToInt();
                            if (id == 0)
                            {
                                LevelGrid.SetSelectToPrevRow();
                                string selectedItem = levelGridSelectedItem != null ? levelGridSelectedItem.CheckGet("WMLE_ID") : null;
                                LevelGrid.LoadItems();
                                if (selectedItem != null)
                                {
                                    LevelGrid.SetSelectedItemId(selectedItem, "WMLE_ID");
                                }
                            }
                            else
                            {
                                new DialogWindow($"Не удалось удалить ячейку {cellGridSelectedItem.CheckGet("CELL_NUM")} код ошибки {id}", "Удаление ячейки", "", DialogWindowButtons.OK).Show();

                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel("[erp]warehouse_directory");
            var userAccessMode = mode;
            switch (mode)
            {
                case Role.AccessMode.Special:
                    break;

                case Role.AccessMode.FullAccess:
                    break;

                case Role.AccessMode.ReadOnly:
                default:
                    break;
            }

            List<Button> buttons = UIUtil.GetVisualChilds<Button>(this.Content as DependencyObject);
            if (buttons != null && buttons.Count > 0)
            {
                foreach (var button in buttons)
                {
                    var buttonTagList = UIUtil.GetTagList(button);
                    var accessMode = Acl.FindTagAccessMode(buttonTagList);
                    if (accessMode > userAccessMode)
                    {
                        button.IsEnabled = false;
                    }
                }
            }

            if (WarehouseGrid != null && WarehouseGrid.Menu != null && WarehouseGrid.Menu.Count > 0)
            {
                foreach (var manuItem in WarehouseGrid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }

            if (ZoneGrid != null && ZoneGrid.Menu != null && ZoneGrid.Menu.Count > 0)
            {
                foreach (var manuItem in ZoneGrid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }

            if (ZoneGrid != null && ZoneGrid.Menu != null && ZoneGrid.Menu.Count > 0)
            {
                foreach (var manuItem in ZoneGrid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }

            if (ZoneGrid != null && ZoneGrid.Menu != null && ZoneGrid.Menu.Count > 0)
            {
                foreach (var manuItem in ZoneGrid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }

            if (LevelGrid != null && LevelGrid.Menu != null && LevelGrid.Menu.Count > 0)
            {
                foreach (var manuItem in LevelGrid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// нажатие кнопки создание склада
        /// </summary>
        private void CreateWarehouseButton_Click(object sender, RoutedEventArgs e)
        {
            CreateWarehouse();
        }

        /// <summary>
        /// нажатие кнопки редактирование склада
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            //WarehouseEdit();
            EditWarehouse();
        }

        /// <summary>
        /// нажатие кнопки обновление склада
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshWarehouseButton_Click(object sender, RoutedEventArgs e)
        {
            WarehouseGrid.LoadItems();
        }

        /// <summary>
        /// нажатие кнопки создать ряд
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateRowsButton_Click(object sender, RoutedEventArgs e)
        {
            CreateWarehouseRow();
        }

        /// <summary>
        /// нажатие кнопки создание ячейки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateCellButton_Click(object sender, RoutedEventArgs e)
        {
            CreateWarehouseCell();
        }

        /// <summary>
        /// нажатие кнопки редактирование ряда
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditRowsButton_Click(object sender, RoutedEventArgs e)
        {
            EditWarehouseRow();
        }

        private void CreateLevelButton_Click(object sender, RoutedEventArgs e)
        {
            CreateWarehouseLevel();
        }

        private void EditLevelButton_Click(object sender, RoutedEventArgs e)
        {
            EditWarehouseLevel();
        }

        private void DellCellButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteCell();
        }

        private void RefreshCellsButton_Click(object sender, RoutedEventArgs e)
        {
            CellsGrid.LoadItems();
        }

        private void RefreshLevelsButton_Click(object sender, RoutedEventArgs e)
        {
            LevelGrid.LoadItems();
        }

        private void DeleteLevelButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteLevel();
        }

        private void DeleteRowsButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteRow();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void EditZoneButton_Click(object sender, RoutedEventArgs e)
        {
            ZoneEdit();
        }

        private void CreateZoneButton_Click(object sender, RoutedEventArgs e)
        {
            ZoneCreate();
        }        
    }
}
