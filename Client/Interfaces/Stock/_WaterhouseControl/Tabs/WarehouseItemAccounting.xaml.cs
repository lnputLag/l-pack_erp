using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Stock._WaterhouseControl;
using DevExpress.Mvvm.Xpf;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Printing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Xceed.Wpf.Toolkit.Primitives;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static System.Windows.Forms.AxHost;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Интерфейс учёта складских единиц WMS
    /// </summary>
    /// <author>eletskikh_ya</author>
    public partial class WarehouseItemAccounting : ControlBase
    {
        public WarehouseItemAccounting()
        {
            ControlTitle = "Учёт складских единиц";
            DocumentationUrl = "/doc/l-pack-erp/warehouse/warehouseControl/warehouseItemAccounting";
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
                ItemGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                ItemGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                ItemGrid.ItemsAutoUpdate = true;
                ItemGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                ItemGrid.ItemsAutoUpdate = false;
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
                    ButtonControl = RefreshItemsButton,
                    ButtonName = "RefreshItemsButton",
                    Action = () =>
                    {
                        Refresh();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "print_excel",
                    Group = "main",
                    Enabled = true,
                    Title = "В Excel",
                    Description = "Выгрузить данные в Excel",
                    ButtonUse = true,
                    ButtonControl = PrintExcel,
                    ButtonName = "PrintExcel",
                    Action = () =>
                    {
                        ExportExcel();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "print",
                    Group = "main",
                    Enabled = true,
                    Title = "Печать ярлыков",
                    Description = "Печать ярлыков для выбранных позиций",
                    ButtonUse = true,
                    ButtonControl = PrintButton,
                    ButtonName = "PrintButton",
                    Action = () =>
                    {
                        Print();
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

            Commander.SetCurrentGridName("ItemGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "move",
                    Title = "Переместить",
                    Description = "Переместить выбранную позицию",
                    Group = "item_grid_operation",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = MoveButton,
                    ButtonName = "MoveButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        Move();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ItemGrid != null && ItemGrid.Items != null && ItemGrid.Items.Count > 0)
                        {
                            if (ItemGrid.SelectedItem != null && ItemGrid.SelectedItem.Count > 0)
                            {
                                // Если данная позиция установлена в хранилище
                                if (!string.IsNullOrEmpty(ItemGrid.SelectedItem.CheckGet("WMST_ID")))
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
                    Name = "consmptn",
                    Title = "Списать",
                    Description = "Списать отмеченные позиции",
                    Group = "item_grid_operation",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = ConsmptnButton,
                    ButtonName = "ConsmptnButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        СonsumptionItem();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ItemGrid != null && ItemGrid.Items != null && ItemGrid.Items.Count > 0)
                        {
                            if (ItemGrid.SelectedItem != null && ItemGrid.SelectedItem.Count > 0)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "edit_item",
                    Enabled = false,
                    Title = "Изменить",
                    Description = "Изменить складскую единицу",
                    Group = "item_grid_operation",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = EditItemButton,
                    ButtonName = "EditItemButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        EditItem();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ItemGrid != null && ItemGrid.Items != null && ItemGrid.Items.Count > 0)
                        {
                            if (ItemGrid.SelectedItem != null && ItemGrid.SelectedItem.Count > 0)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "history_changes",
                    Title = "История изменений",
                    Group = "item_grid_default",
                    Enabled = true,
                    MenuUse = true,
                    ButtonUse = false,
                    Action = () =>
                    {
                        HistoryChanges();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "history_operations",
                    Title = "История операций",
                    Group = "item_grid_default",
                    Enabled = true,
                    MenuUse = true,
                    ButtonUse = false,
                    Action = () =>
                    {
                        HistoryOperations();
                    },
                });
            }

            Commander.Init(this);
        }

        private ListDataSet ItemDataSet { get; set; }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            ItemDataSet = new ListDataSet();

            ItemTypeSelectBox.Items.Add("0", "Все типы");
            FormHelper.ComboBoxInitHelper(ItemTypeSelectBox, "Warehouse", "ItemGroup", "List", "ID", "NAME", null, true);
            ItemTypeSelectBox.SetSelectedItemByKey("0");

            FormHelper.ComboBoxInitHelper(ZoneFilter, "Warehouse", "Zone", "List", "WMZO_ID", "ZONE_FULL_NAME", null, true, true);
            ZoneFilter.SetSelectedItemByKey("1");

            Dictionary<string, string> shelfLifeStatusSelectBoxItems = new Dictionary<string, string>();
            shelfLifeStatusSelectBoxItems.Add("0", "Все сроки хранения");
            shelfLifeStatusSelectBoxItems.Add("1", "Не просроченные");
            shelfLifeStatusSelectBoxItems.Add("2", "Просроченные");
            ShelfLifeStatusSelectBox.AddItems(shelfLifeStatusSelectBoxItems);
            ShelfLifeStatusSelectBox.SetSelectedItemByKey("0");
        }

        /// <summary>
        /// настройка отображения грида
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
                        Header="*",
                        Path="_SELECTED",
                        ColumnType=ColumnTypeRef.Boolean,
                        Editable=true,
                        Width2 = 2,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="WMIT_ID",
                        Description="Ид складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="NAME",
                        Description="Наименование складской единицы",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Path="QTY",
                        Description="Количество по единице измерения в складской единице",
                        ColumnType=ColumnTypeRef.Double,
                        Width2 = 6,
                        Format = "N0"
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ед. изм.",
                        Path="SHORT_NAME",
                        Description="Единица измерения складской единицы",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Группа",
                        Path="GROUP_NAME",
                        Description="Группа складской единицы",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Склад",
                        Path="WAREHOUSE",
                        Description="Наименование склада складской единицы",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Зона",
                        Path="ZONE",
                        Description="Наименование зоны складской единицы",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ячейка",
                        Path="NUM",
                        Description="Ячейка хранилища складской единицы",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Длина",
                        Path="LENGTH",
                        Description="Длина складской единицы, мм",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ширина",
                        Path="WIDTH",
                        Description="Ширина складской единицы, мм",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Высота",
                        Path="HEIGHT",
                        Description="Высота складской единицы, мм",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вес",
                        Path="WEIGHT",
                        Description="Вес складской единицы, кг",
                        ColumnType=ColumnTypeRef.Double,
                        Format = "N2",
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Внешний ИД",
                        Path="OUTER_ID",
                        Description="Внешний ИД",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Внешний номер",
                        Path="OUTER_NUM",
                        Description="Внешний номер",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Проверочный код",
                        Path="VERIFICATION_CODE",
                        Description="Проверочный код",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Групповой внешний ИД",
                        Path="OUTER_GROUP_ID",
                        Description="Внешний групповой ИД",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Примечание",
                        Path="NOTE",
                        Description="Примечание",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 18,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата производства",
                        Path="PRODUCED_DT",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format = "dd.MM.yyyy HH:mm:ss",
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата создания",
                        Path="CREATED_DTTM",
                        Description="Дата создания",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format = "dd.MM.yyyy HH:mm:ss",
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Срок хранения",
                        Path="SHELF_LIFE",
                        Description="Срок хранения этого вида ТМЦ, месяцы",
                        ColumnType=ColumnTypeRef.Double,
                        Format = "N0",
                        Width2 = 8,
                        Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = string.Empty;

                                    // Если не указан срок хранения ТМЦ, то подсвечиваем Синим
                                    if (string.IsNullOrEmpty(row.CheckGet("SHELF_LIFE")))
                                    {
                                        color = HColor.Blue;
                                    }
                                    else
                                    {
                                        DateTime producedDt = row.CheckGet("CREATED_DTTM").ToDateTime("dd.MM.yyyy HH:mm:ss");
                                        if (!string.IsNullOrEmpty(row.CheckGet("PRODUCED_DT")))
                                        {
                                            producedDt = row.CheckGet("PRODUCED_DT").ToDateTime("dd.MM.yyyy HH:mm:ss");
                                        }

                                        // Если срок хранение ТМЦ истёк, то подсвечиваем Красным
                                        if (DateTime.Now > producedDt.AddMonths(row.CheckGet("SHELF_LIFE").ToInt()))
                                        {
                                            color = HColor.Red;
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
                    new DataGridHelperColumn
                    {
                        Header="Дней до просрочки",
                        Path="DAYS_BEFORE_SHELF_LIFE",
                        Description="Осталось дней до конца срока хранения",
                        ColumnType=ColumnTypeRef.Double,
                        Format = "N0",
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Создатель",
                        Path="CREATOR_NAME",
                        Description="Создатель складской единицы",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 12,
                    },

                    new DataGridHelperColumn
                    {
                        Header="ИД группы",
                        Path="WMIG_ID",
                        Description="Ид группы складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД единицы измерения",
                        Path="UNIT_ID",
                        Description="Ид единицы измерения складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД создателя",
                        Path="CREATOR_ACCO_ID",
                        Description="Ид акаунта создателя складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД хранилища",
                        Path="WMST_ID",
                        Description="Ид хранилища складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД склада",
                        Path="WMWA_ID",
                        Description="Ид склада складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД зоны",
                        Path="WMZO_ID",
                        Description="Ид зоны складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД прихода",
                        Path="IDP",
                        Description="ИД прихода складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Идентификатор ТМЦ",
                        Path="WMII_ID",
                        Description="Идентификатор ТМЦ складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden = true,
                    },

                };
                ItemGrid.SetColumns(columns);
                ItemGrid.SetPrimaryKey("WMIT_ID");
                ItemGrid.SearchText = ItemsSearchBox;
                //данные грида
                ItemGrid.OnLoadItems = ItemGridLoadItems;
                ItemGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                ItemGrid.AutoUpdateInterval = 60 * 5;
                ItemGrid.Toolbar = ItemGridToolbar;

                ItemGrid.OnFilterItems = () =>
                {
                    if (ItemGrid.Items != null && ItemGrid.Items.Count > 0)
                    {
                        if (ZoneFilter != null && ZoneFilter.SelectedItem.Key != null)
                        {
                            var key = ZoneFilter.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            items.AddRange(ItemGrid.Items.Where(x => x.CheckGet("WMZO_ID").ToInt() == key));

                            ItemGrid.Items = items;
                        }

                        if (ShelfLifeStatusSelectBox != null && ShelfLifeStatusSelectBox.SelectedItem.Key != null)
                        {
                            var key = ShelfLifeStatusSelectBox.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            switch (key)
                            {
                                case 0:
                                    items = ItemGrid.Items;
                                    break;

                                case 1:
                                    items.AddRange(
                                        ItemGrid.Items.Where(x => string.IsNullOrEmpty(x.CheckGet("SHELF_LIFE")) 
                                        ||
                                            (
                                            !string.IsNullOrEmpty(x.CheckGet("PRODUCED_DT"))
                                            ?
                                            DateTime.Now <= x.CheckGet("PRODUCED_DT").ToDateTime("dd.MM.yyyy HH:mm:ss").AddMonths(x.CheckGet("SHELF_LIFE").ToInt())
                                            :
                                            DateTime.Now <= x.CheckGet("CREATED_DTTM").ToDateTime("dd.MM.yyyy HH:mm:ss").AddMonths(x.CheckGet("SHELF_LIFE").ToInt())
                                            )
                                        )
                                    );
                                    break;

                                case 2:
                                    items.AddRange(
                                        ItemGrid.Items.Where(x => !string.IsNullOrEmpty(x.CheckGet("SHELF_LIFE"))
                                        &&
                                            (
                                            !string.IsNullOrEmpty(x.CheckGet("PRODUCED_DT"))
                                            ?
                                            DateTime.Now > x.CheckGet("PRODUCED_DT").ToDateTime("dd.MM.yyyy HH:mm:ss").AddMonths(x.CheckGet("SHELF_LIFE").ToInt())
                                            :
                                            DateTime.Now > x.CheckGet("CREATED_DTTM").ToDateTime("dd.MM.yyyy HH:mm:ss").AddMonths(x.CheckGet("SHELF_LIFE").ToInt())
                                            )
                                        )
                                    );
                                    break;
                            }

                            ItemGrid.Items = items;
                        }

                        // Типы ТМЦ
                        if (ItemTypeSelectBox != null && ItemTypeSelectBox.SelectedItem.Key != null)
                        {
                            var key = ItemTypeSelectBox.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            switch (key)
                            {
                                // Все типы ТМЦ
                                case 0:
                                    items = ItemGrid.Items;
                                    break;

                                default:
                                    items.AddRange(ItemGrid.Items.Where(x => x.CheckGet("WMIG_ID").ToInt() == key));
                                    break;
                            }

                            ItemGrid.Items = items;
                        }
                    }
                };

                //при выборе строки в гриде, обновляются актуальные действия для записи
                ItemGrid.OnSelectItem = selectedItem =>
                {
                };

                ItemGrid.OnDblClick = selectedItem =>
                {
                    if (selectedItem != null)
                    {
                        EditItem();
                    }
                };

                ItemGrid.Commands = Commander;

                ItemGrid.Init();
            }
        }

        public void EnableControls()
        {
            ItemGrid.HideSplash();
            ItemGrid.Toolbar.IsEnabled = true;
        }

        public void DisableControls()
        {
            ItemGrid.ShowSplash();
            ItemGrid.Toolbar.IsEnabled = false;
        }

        /// <summary>
        /// Загрузка данными грида
        /// </summary>
        private async void ItemGridLoadItems()
        {
            DisableControls();

            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "Item");
            q.Request.SetParam("Action", "ListBalanceByZone");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            ItemDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    ItemDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            ItemGrid.UpdateItems(ItemDataSet);

            EnableControls();
        }

        public void Move()
        {
            if (ItemGrid != null && ItemGrid.SelectedItem != null && ItemGrid.SelectedItem.Count > 0)
            {
                var warehouseItemCell = new WarehouseItemCell();
                warehouseItemCell.ItemId = ItemGrid.SelectedItem["WMIT_ID"].ToInt();
                warehouseItemCell.CurrentItemAction = WarehouseItemCell.ItemAction.Move;
                warehouseItemCell.WarehouseSelectBox.SetSelectedItemByKey(ItemGrid.SelectedItem["WMWA_ID"].ToInt().ToString());
                warehouseItemCell.WarehouseSelectBox.IsReadOnly = true;
                warehouseItemCell.ZoneSelectBox.SetSelectedItemByKey(ItemGrid.SelectedItem["WMZO_ID"].ToInt().ToString());
                warehouseItemCell.Show();
            }
        }

        /// <summary>
        /// История изменений
        /// </summary>
        public void HistoryChanges()
        {
            if (ItemGrid != null && ItemGrid.SelectedItem != null && ItemGrid.SelectedItem.Count > 0)
            {
                ItemChangesHistory dlg = new ItemChangesHistory(ItemGrid.SelectedItem.CheckGet("WMIT_ID").ToInt(),
                    ItemGrid.SelectedItem.CheckGet("NUM"), ItemGrid.SelectedItem.CheckGet("NAME"));
                dlg.Show();
            }
        }

        /// <summary>
        /// История операций
        /// </summary>
        public void HistoryOperations()
        {
            if (ItemGrid != null && ItemGrid.SelectedItem != null && ItemGrid.SelectedItem.Count > 0)
            {
                ItemOperationsHistory dlg = new ItemOperationsHistory(ItemGrid.SelectedItem.CheckGet("WMIT_ID").ToInt(),
                     ItemGrid.SelectedItem.CheckGet("NUM"), ItemGrid.SelectedItem.CheckGet("NAME"));
                dlg.Show();
            }
        }

        public void Refresh()
        {
            ItemGrid.LoadItems();
        }

        public void EditItem()
        {
            if (ItemGrid != null && ItemGrid.SelectedItem != null && ItemGrid.SelectedItem.Count > 0)
            {
                new WarehouseItem().Edit(ItemGrid.SelectedItem.CheckGet("WMIT_ID").ToInt());
            }
        }

        /// <summary>
        /// Списание позиций
        /// </summary>
        private void СonsumptionItem()
        {
            List<Dictionary<string, string>> selectedItemList = ItemGrid.GetItemsSelected();
            if (selectedItemList != null && selectedItemList.Count > 0)
            {
                var d = DialogWindow.ShowDialog($"Вы действительно хотите списать {selectedItemList.Count} ТМЦ?", "Подтверждение списания", "", DialogWindowButtons.YesNo);
                if (d == true)
                {
                    bool succesFullFlag = true;
                    foreach (var selectedItem in selectedItemList)
                    {
                        if (!СonsumptionItemOne(selectedItem))
                        {
                            succesFullFlag = false;
                        }
                    }

                    if (succesFullFlag)
                    {
                        Refresh();
                    }
                    else
                    {
                        DialogWindow.ShowDialog("При выполнении списания произошла ошибка. Пожалуйста, сообщите о проблеме");
                    }
                }
            }
            else if (ItemGrid != null && ItemGrid.SelectedItem != null && ItemGrid.SelectedItem.Count > 0)
            {
                var d = DialogWindow.ShowDialog($"Вы действительно хотите списать ТМЦ {ItemGrid.SelectedItem["NAME"]}, находящуюся в ячейке {ItemGrid.SelectedItem["NUM"]}?", "Подтверждение списания", "", DialogWindowButtons.YesNo);
                if (d == true)
                {
                    bool succesFullFlag = СonsumptionItemOne(ItemGrid.SelectedItem);

                    if (succesFullFlag)
                    {
                        Refresh();
                    }
                    else
                    {
                        DialogWindow.ShowDialog("При выполнении списания произошла ошибка. Пожалуйста, сообщите о проблеме");
                    }
                }
            }
        }

        private bool СonsumptionItemOne(Dictionary<string, string> selectedItem)
        {
            bool _result = false;

            if (selectedItem != null && selectedItem.Count > 0)
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("WMIT_ID", selectedItem["WMIT_ID"]);
                    p.Add("WMST_ID", selectedItem["WMST_ID"]);
                    p.Add("QTY", selectedItem["QTY"].ToDouble().ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "Item");
                q.Request.SetParam("Action", "WriteOff");
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
                            if (ds.Items[0].CheckGet("ID").ToInt() == 0)
                            {
                                _result = true;
                            }
                        }
                    }
                }
            }

            return _result;
        }

        public async void ExportExcel()
        {
            ItemGrid.ItemsExportExcel();
        }

        public void Print()
        {
            if (ItemGrid != null && ItemGrid.Items != null && ItemGrid.Items.Count > 0)
            {
                // получить все выбранные строки:
                var list = ItemGrid.Items.Where(x => x.CheckGet("_SELECTED").ToInt() > 0).ToList();

                string message = "";
                if (list.Count > 0)
                {
                    message = $"Вы действительно хотите напечатать ярлыки для выбранных ТМЦ? (Выбранно {list.Count} ТМЦ)";
                }
                else
                {
                    if (ItemGrid.SelectedItem != null && ItemGrid.SelectedItem.Count > 0)
                    {
                        message = $"Вы действительно хотите напечатать ярлыки для выбранной позиций?";
                        list.Add(ItemGrid.SelectedItem);
                    }
                    else
                    {
                        return;
                    }
                }

                var dw = new DialogWindow(message, "Печать ярлыка", "Подтверждение печати ярлыков", DialogWindowButtons.YesNo);
                if (dw.ShowDialog() == true)
                {
                    int count = 1;
                    if (!Int32.TryParse(ItemsPrintCount.Text, out count))
                    {
                        count = 1;
                    }

                    bool exit = false;

                    PrintQueue queue = null;

                    foreach (var item in list)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            BarcodeGenerator generator = new BarcodeGenerator();
                            var doc = generator.GenerateItemDocument(item.CheckGet("WMIT_ID").TrimEnd(".0"), item.CheckGet("QTY").TrimEnd(".0") + " " + item.CheckGet("SHORT_NAME"), item.CheckGet("NAME"), item.CheckGet("PRODUCED_DT"));

                            PrintDialog printDlg = new PrintDialog();
                            printDlg.PrintTicket.PageOrientation = System.Printing.PageOrientation.Landscape;

                            if (queue == null)
                            {
                                /// если принтер еще не выбран, покажем диалог выбора
                                if (printDlg.ShowDialog() == true)
                                {
                                    queue = printDlg.PrintQueue;
                                }
                                else
                                {
                                    // при отменене печати, нужно прервать цикл печати всех документов,
                                    // что бы не спрашивать при печати каждой ТМЦ
                                    exit = true;
                                    break;
                                }
                            }
                            else
                            {
                                /// если принтер выбран то без показа создадим очередь печати с ранее выбоанным принтером
                                printDlg.PrintQueue = queue;
                            }

                            var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
                            printDlg.PrintDocument(paginator, "");
                        }

                        if (exit) break;
                    }
                }
            }
        }

        private void CheckShowAll_Checked(object sender, RoutedEventArgs e)
        {
            ItemGrid.UpdateItems();
        }

        private void CheckShowAll_Unchecked(object sender, RoutedEventArgs e)
        {
            ItemGrid.UpdateItems();
        }

        private void ZoneFilter_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ItemGrid.UpdateItems();
        }

        private void ShelfLifeStatusSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ItemGrid.UpdateItems();
        }

        private void ItemTypeSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ItemGrid.UpdateItems();
        }
    }
}
