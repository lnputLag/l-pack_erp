using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Service.Printing;
using Client.Interfaces.Stock._WaterhouseControl;
using DevExpress.Mvvm.Xpf;
using Newtonsoft.Json;
using NLog.LayoutRenderers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
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
using static Client.Common.FormHelperField;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Поступление складских единиц WMS
    /// </summary>
    public partial class WarehouseListArrival : ControlBase
    {
        public WarehouseListArrival()
        {
            ControlTitle = "Поступление складских единиц";
            DocumentationUrl = "/doc/l-pack-erp/";
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
                ArrivalGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                ArrivalGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                ArrivalGrid.ItemsAutoUpdate = true;
                ArrivalGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                ArrivalGrid.ItemsAutoUpdate = false;
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
                    Name = "add_item",
                    Group = "main",
                    Enabled = true,
                    Title = "Добавить",
                    Description = "Добавить складскую единицу",
                    ButtonUse = true,
                    ButtonControl = AddItemButton,
                    ButtonName = "AddItemButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        AddItem();
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

            Commander.SetCurrentGridName("ArrivalGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "edit_item",
                    Enabled = false,
                    Title = "Изменить",
                    Description = "Изменить складскую единицу",
                    Group = "arrival_grid_operation",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = EditItemButton,
                    ButtonName = "EditItemButton",
                    HotKey = "DoubleCLick",
                    AccessLevel= Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        EditItem();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ArrivalGrid != null && ArrivalGrid.Items != null && ArrivalGrid.Items.Count > 0)
                        {
                            if (ArrivalGrid.SelectedItem != null && ArrivalGrid.SelectedItem.Count > 0)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "delete_item",
                    Enabled = false,
                    Title = "Удалить",
                    Description = "Удалить складскую единицу",
                    Group = "arrival_grid_operation",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = DeleteItemButton,
                    ButtonName = "DeleteItemButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        DeleteItem();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ArrivalGrid != null && ArrivalGrid.Items != null && ArrivalGrid.Items.Count > 0)
                        {
                            if (ArrivalGrid.SelectedItem != null && ArrivalGrid.SelectedItem.Count > 0)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "arrival_item",
                    Title = "Оприходовать",
                    Description = "Оприходовать выбранную позицию",
                    Group = "arrival_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = ArrivalItemButton,
                    ButtonName = "ArrivalItemButton",
                    AccessLevel= Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        ArrivalItem();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ArrivalGrid != null && ArrivalGrid.Items != null && ArrivalGrid.Items.Count > 0)
                        {
                            if (ArrivalGrid.SelectedItem != null && ArrivalGrid.SelectedItem.Count > 0)
                            {
                                if (string.IsNullOrEmpty(ArrivalGrid.SelectedItem.CheckGet("ITEM_STORAGE_ID")))
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
                    Name = "print_item_label",
                    Title = "Печать",
                    Description = "Печать этикетки для выбранных позиций",
                    Group = "arrival_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = PrintItemLabelButton,
                    ButtonName = "PrintItemLabelButton",
                    Action = () =>
                    {
                        PrintItemLabel();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ArrivalGrid != null && ArrivalGrid.Items != null && ArrivalGrid.Items.Count > 0)
                        {
                            if (ArrivalGrid.SelectedItem != null && ArrivalGrid.SelectedItem.Count > 0)
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

        private ListDataSet ArrivalDataSet { get; set; }

        public FormHelper Form { get; set; }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            ArrivalDataSet = new ListDataSet();
        }

        /// <summary>
        /// настройка отображения грида
        /// </summary>
        private void ArrivalGridInit()
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
                        Path="ITEM_ID",
                        Description="Идентификатор складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="ITEM_NAME",
                        Description="Наименование складской единицы",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 57,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Path="ITEM_QUANTITY",
                        Description="Количество складской единицы",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width2 = 10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ед. изм.",
                        Path="ITEM_UNIT_NAME",
                        Description="Единица измерения складской единицы",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Длина",
                        Path="ITEM_LENGTH",
                        Description="Длина складской единицы, мм",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ширина",
                        Path="ITEM_WIDTH",
                        Description="Ширина складской единицы, мм",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Высота",
                        Path="ITEM_HEIGHT",
                        Description="Высота складской единицы, мм",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вес",
                        Path="ITEM_WEIGHT",
                        Description="Вес складской единицы, кг",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Группа",
                        Path="ITEM_GROUP_NAME",
                        Description="Группа ТМЦ складской единицы",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Склад",
                        Path="ITEM_WAREHOUSE_NAME",
                        Description="Склад складской единицы",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 13,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Создание",
                        Path="ITEM_CREATED_DTTM",
                        Description="Дата создания складской единицы",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Производство",
                        Path="ITEM_PRODUCED_DT",
                        Description="Дата производства складской единицы",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Срок хранения",
                        Path="ITEM_SHELF_LIFE",
                        Description="Срок хранения этого вида ТМЦ, месяцы",
                        ColumnType=ColumnTypeRef.Double,
                        Format = "N0",
                        Width2 = 12,
                        Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = string.Empty;

                                    // Если не указан срок хранения ТМЦ, то подсвечиваем Синим
                                    if (string.IsNullOrEmpty(row.CheckGet("ITEM_SHELF_LIFE")))
                                    {
                                        color = HColor.Blue;
                                    }
                                    else
                                    {
                                        DateTime producedDt = row.CheckGet("ITEM_CREATED_DTTM").ToDateTime("dd.MM.yyyy HH:mm:ss");
                                        if (!string.IsNullOrEmpty(row.CheckGet("ITEM_PRODUCED_DT")))
                                        {
                                            producedDt =  row.CheckGet("ITEM_PRODUCED_DT").ToDateTime("dd.MM.yyyy HH:mm:ss");
                                        }

                                        // Если срок хранение ТМЦ истёк, то подсвечиваем Красным
                                        if (DateTime.Now > producedDt.AddMonths(row.CheckGet("ITEM_SHELF_LIFE").ToInt()))
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
                        Header="Примечание",
                        Path="ITEM_NOTE",
                        Description="Примечание к складской единицы",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 32,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Создатель",
                        Path="ITEM_CREATOR_NAME",
                        Description="Наименование создателя складской единицы",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 12,
                    },

                    new DataGridHelperColumn
                    {
                        Header="ИД ед. изм.",
                        Path="ITEM_UNIT_ID",
                        Description="Идентификатор списания складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД группы",
                        Path="ITEM_GROUP_ID",
                        Description="Идентификатор группы ТМЦ складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД склада",
                        Path="ITEM_WAREHOUSE_ID",
                        Description="Идентификатор склада складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД прихода",
                        Path="IDP",
                        Description="Идентификатор прихода складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД создателя",
                        Path="ITEM_CREATOR_ID",
                        Description="Идентификатор создателя складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД хранилища",
                        Path="ITEM_STORAGE_ID",
                        Description="Идентификатор хранилища складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },

                };
                ArrivalGrid.SetColumns(columns);
                ArrivalGrid.SetPrimaryKey("ITEM_ID");
                ArrivalGrid.SearchText = ArrivalSearchBox;
                //данные грида
                ArrivalGrid.OnLoadItems = ArrivalGridLoadItems;
                ArrivalGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                ArrivalGrid.AutoUpdateInterval = 60;
                ArrivalGrid.Toolbar = ArrivalGridToolbar;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                ArrivalGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem != null)
                    {
                        if (ArrivalGrid != null && ArrivalGrid.Items != null && ArrivalGrid.Items.Count > 0)
                        {
                            if (ArrivalGrid.Items.FirstOrDefault(x => x.CheckGet("ITEM_ID").ToInt() == selectedItem.CheckGet("ITEM_ID").ToInt()) == null)
                            {
                                ArrivalGrid.SelectRowFirst();
                            }
                        }
                    }
                };

                ArrivalGrid.OnFilterItems = ArrivalGridFilterItems;

                ArrivalGrid.Commands = Commander;

                ArrivalGrid.Init();
            }
        }

        public void ArrivalGridFilterItems()
        {
            if (ArrivalGrid != null && ArrivalGrid.SelectedItem != null && ArrivalGrid.SelectedItem.Count > 0)
            {
                ArrivalGrid.Commands.Message = new ItemMessage() { Action = "refresh", Message = $"{ArrivalGrid.SelectedItem.CheckGet("ITEM_ID")}" };
            }
        }

        /// <summary>
        /// Загрузка данными грида
        /// </summary>
        private async void ArrivalGridLoadItems()
        {
            ArrivalGrid.ShowSplash();

            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "Item");
            q.Request.SetParam("Action", "ListArrival");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            ArrivalDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    ArrivalDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            ArrivalGrid.UpdateItems(ArrivalDataSet);

            ArrivalGrid.HideSplash();
        }

        public void PrintItemLabel()
        {
            if (ArrivalGrid != null && ArrivalGrid.Items != null && ArrivalGrid.Items.Count > 0)
            {
                var checkedRowList = ArrivalGrid.GetItemsSelected();
                if (checkedRowList != null && checkedRowList.Count > 0)
                {
                    if (checkedRowList.Count > 1)
                    {
                        foreach (var checkedRow in checkedRowList)
                        {
                            BarcodeGenerator generator = new BarcodeGenerator();
                            var doc = generator.GenerateItemDocument(
                                  checkedRow.CheckGet("ITEM_ID")
                                , $"{checkedRow.CheckGet("ITEM_QUANTITY").ToInt()} {checkedRow.CheckGet("ITEM_UNIT_NAME")}"
                                , checkedRow.CheckGet("ITEM_NAME")
                                , checkedRow.CheckGet("ITEM_PRODUCED_DT")
                            );
                            var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;

                            PrintDocument(paginator);
                        }

                        ArrivalGrid.UpdateItems();
                    }
                    else
                    {
                        var checkedRow = checkedRowList.First();
                        if (checkedRow != null && checkedRow.Count > 0)
                        {
                            BarcodeGenerator generator = new BarcodeGenerator();
                            var doc = generator.GenerateItemDocument(
                                  checkedRow.CheckGet("ITEM_ID")
                                , $"{checkedRow.CheckGet("ITEM_QUANTITY").ToInt()} {checkedRow.CheckGet("ITEM_UNIT_NAME")}"
                                , checkedRow.CheckGet("ITEM_NAME")
                                , checkedRow.CheckGet("ITEM_PRODUCED_DT")
                            );
                            var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;

                            PrintDocument(paginator);
                        }

                        ArrivalGrid.UpdateItems();
                    }
                }
                else
                {
                    if (ArrivalGrid.SelectedItem != null && ArrivalGrid.SelectedItem.Count > 0)
                    {
                        BarcodeGenerator generator = new BarcodeGenerator();
                        var doc = generator.GenerateItemDocument(
                              ArrivalGrid.SelectedItem.CheckGet("ITEM_ID")
                            , $"{ArrivalGrid.SelectedItem.CheckGet("ITEM_QUANTITY").ToInt()} {ArrivalGrid.SelectedItem.CheckGet("ITEM_UNIT_NAME")}"
                            , ArrivalGrid.SelectedItem.CheckGet("ITEM_NAME")
                            , ArrivalGrid.SelectedItem.CheckGet("ITEM_PRODUCED_DT")
                        );
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

        public void ArrivalItem()
        {
            if (ArrivalGrid != null && ArrivalGrid.SelectedItem != null && ArrivalGrid.SelectedItem.Count > 0)
            {
                var warehouseItemCell = new WarehouseItemCell();
                warehouseItemCell.ItemId = ArrivalGrid.SelectedItem["ITEM_ID"].ToInt();
                warehouseItemCell.CurrentItemAction = WarehouseItemCell.ItemAction.Register;
                warehouseItemCell.WarehouseSelectBox.SetSelectedItemByKey(ArrivalGrid.SelectedItem["ITEM_WAREHOUSE_ID"].ToInt().ToString());
                warehouseItemCell.ItemQuantity = ArrivalGrid.SelectedItem["ITEM_QUANTITY"].ToDouble();
                warehouseItemCell.Show();
            }
        }

        /// <summary>
        /// добавить позицию ТМЦ
        /// </summary>
        public void AddItem()
        {
            new WarehouseItem().Edit();
        }

        public void EditItem()
        {
            if (ArrivalGrid != null && ArrivalGrid.SelectedItem != null && ArrivalGrid.SelectedItem.Count > 0)
            {
                new WarehouseItem().Edit(ArrivalGrid.SelectedItem.CheckGet("ITEM_ID").ToInt());
            }
        }

        public void DeleteItem() 
        {
            List<Dictionary<string, string>> selectedItemList = ArrivalGrid.GetItemsSelected();
            if (selectedItemList != null && selectedItemList.Count > 0)
            {
                var d = DialogWindow.ShowDialog($"Вы действительно хотите удалить {selectedItemList.Count} ТМЦ?", "Подтверждение удаления", "", DialogWindowButtons.YesNo);
                if (d == true)
                {
                    bool succesFullFlag = true;
                    bool resume = true;
                    var accessMode = Central.Navigator.GetRoleLevel(this.RoleName);
                    bool showWarningMessageFlag = false;
                    foreach (var selectedItem in selectedItemList)
                    {
                        resume = true;
                        if (selectedItem.CheckGet("ITEM_GROUP_ID").ToInt() != 5)
                        {
                            if (accessMode < Role.AccessMode.Special)
                            {
                                resume = false;
                            }
                        }

                        if (resume)
                        {
                            if (!DeleteItemOne(selectedItem))
                            {
                                succesFullFlag = false;
                            }
                        }
                        else
                        {
                            showWarningMessageFlag = true;
                        }
                    }

                    if (succesFullFlag)
                    {
                        Refresh();
                    }
                    else
                    {
                        DialogWindow.ShowDialog("При выполнении удаления произошла ошибка. Пожалуйста, сообщите о проблеме");
                    }

                    if (showWarningMessageFlag)
                    {
                        DialogWindow.ShowDialog("Нельзя удалить позицию, которая не относится к группе ТМЦ");
                    }
                }
            }
            else if (ArrivalGrid != null && ArrivalGrid.SelectedItem != null && ArrivalGrid.SelectedItem.Count > 0)
            {
                var d = DialogWindow.ShowDialog($"Вы действительно хотите удалить ТМЦ {ArrivalGrid.SelectedItem["ITEM_NAME"]}?", "Подтверждение удаления", "", DialogWindowButtons.YesNo);
                if (d == true)
                {
                    bool resume = true;
                    if (ArrivalGrid.SelectedItem.CheckGet("ITEM_GROUP_ID").ToInt() != 5)
                    {
                        if (Central.Navigator.GetRoleLevel(this.RoleName) < Role.AccessMode.Special)
                        {
                            resume = false;
                        }
                    }

                    if (resume)
                    {
                        bool succesFullFlag = DeleteItemOne(ArrivalGrid.SelectedItem);

                        if (succesFullFlag)
                        {
                            Refresh();
                        }
                        else
                        {
                            DialogWindow.ShowDialog("При выполнении удаления произошла ошибка. Пожалуйста, сообщите о проблеме");
                        }
                    }
                    else
                    {
                        DialogWindow.ShowDialog("Нельзя удалить позицию, которая не относится к группе ТМЦ");
                    }
                }
            }
        }

        /// <summary>
        /// Удаление тмц, если это возможно будет установлен статус "удалена"
        /// </summary>
        public bool DeleteItemOne(Dictionary<string, string> selectedItem)
        {
            bool _result = false;

            if (selectedItem != null && selectedItem.Count > 0)
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("WMIT_ID", selectedItem["ITEM_ID"]);
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "Item");
                q.Request.SetParam("Action", "SetStatusDeleted");
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

        public void Refresh()
        {
            ArrivalGrid.LoadItems();
        }

        public void SetPrintSettings()
        {
            var i = new PrintingInterface();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings();
        }
    }
}
