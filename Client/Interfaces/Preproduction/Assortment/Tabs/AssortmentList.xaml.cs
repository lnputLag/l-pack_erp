using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Service;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Список асортимента выпускаемой продукции
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class AssortmentList : ControlBase
    {
        /// <summary>
        /// Конструктор списка асортимента выпускаемой продукции
        /// </summary>
        public AssortmentList()
        {
            InitializeComponent();
            ControlTitle = "Ассортимент";
            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/assortment";
            RoleName = "[erp]reference_assortment";

            OnLoad = () =>
            {
                SetDefaults();
                LoadUserGroup();
                InitGrid();
            };

            OnUnload = () =>
            {
                Grid.Destruct();
            };

            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverName == ControlName)
                {
                    ProcessMessage(msg);
                }
            };

            OnFocusGot = () =>
            {
                Grid.ItemsAutoUpdate = true;
                Grid.Run();
            };

            OnFocusLost = () =>
            {
                Grid.ItemsAutoUpdate = false;
            };

            Commander.SetCurrentGroup("main");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "refresh",
                    Group = "grid_base",
                    Enabled = true,
                    Title = "Показать",
                    Description = "Загрузить данные",
                    ButtonUse = true,
                    ButtonName = "RefreshButton",
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        if (TemplateText.Text.Length > 2)
                        {
                            CheckSearch();
                        }
                        else
                        {
                            var dw = new DialogWindow("Для загрузки данных введите что-нибудь в поле фильтра", "Список изделий");
                            dw.ShowDialog();
                        }
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
                    Name = "edit",
                    Title = "Изменить",
                    Group = "item",
                    MenuUse = true,
                    HotKey = "Return|DoubleCLick",
                    ButtonUse = true,
                    ButtonName = "EditButton",
                    Description = "Изменение изделия/заготовки",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            EditBlank();
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var k = Grid.GetPrimaryKey();
                        var row = Grid.SelectedItem;
                        if (row.CheckGet(k).ToInt() != 0)
                        {
                            result = true;
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "copyblank",
                    Title = "Копировать",
                    Group = "item",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonName = "CopyBlankButton",
                    Description = "Копировать изделие/заготовку",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            CopyBlank();
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var k = Grid.GetPrimaryKey();
                        var row = Grid.SelectedItem;
                        if (row.CheckGet(k).ToInt() != 0)
                        {
                            result = true;
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "archive",
                    Title = "В архив",
                    Group = "item",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonName = "ArchiveButton",
                    Description = "Отправить заготовку в архив",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            SetArchive();
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (row != null)
                        {
                            bool isBlank = row.CheckGet("CATEGORY_ID").ToInt() == 4;
                            bool archived = row.CheckGet("ARCHIVED_FLAG").ToBool();

                            result = isBlank && !archived;
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "techmap",
                    Title = "Техкарта",
                    Group = "operations",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonName = "TechMapButton",
                    Description = "Открыть техкарту",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            ShowTechnologicalMap();
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (row != null)
                        {
                            result = !row.CheckGet("PATH_TK").IsNullOrEmpty();
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "toexcel",
                    Title = "В Excel",
                    Group = "operations",
                    MenuUse = false,
                    ButtonUse = true,
                    ButtonName = "ToExcelButton",
                    Description = "Выгрузить таблицу в Excel",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        Grid.ItemsExportExcel();
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        if (Grid.Items != null && Grid.Items.Count > 0)
                        {
                            result = true;
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "showshipmenthistory",
                    Title = "История отгрузок",
                    Group = "operations",
                    MenuUse = true,
                    ButtonUse = false,
                    ButtonName = "",
                    Description = "История отгрузок изделия",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        var shipmentHistoryFrame = new ProductShipmentHistory();
                        shipmentHistoryFrame.ReceiverName = ControlName;
                        shipmentHistoryFrame.ProductId = Grid.SelectedItem.CheckGet("ID").ToInt();
                        shipmentHistoryFrame.ProductSku = Grid.SelectedItem.CheckGet("ARTIKUL").Substring(0, 7);
                        shipmentHistoryFrame.ShowTab();
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (row != null)
                        {
                            int category = row.CheckGet("CATEGORY_ID").ToInt();
                            result = category.ContainsIn(5, 6, 16);
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "showproductlosses",
                    Title = "Посмотреть припуски",
                    Group = "operations",
                    MenuUse = true,
                    ButtonUse = false,
                    ButtonName = "",
                    Description = "Посмотреть припуски на брак при раскрое заготовок",
                    AccessLevel = Role.AccessMode.Special,
                    Action = () =>
                    {
                        var productLossesFrame = new ProductLosses();
                        productLossesFrame.ReceiverName = ControlName;
                        productLossesFrame.ProductId = Grid.SelectedItem.CheckGet("ID").ToInt();
                        productLossesFrame.ProductSku = Grid.SelectedItem.CheckGet("ARTIKUL").Substring(0, 7);
                        productLossesFrame.ProductsFromBlankQty = Grid.SelectedItem.CheckGet("PRODUCTS_FROM_BLANK").ToDouble();
                        productLossesFrame.ProductsOnPalletQty = Grid.SelectedItem.CheckGet("PRODUCTS_IN_PALLET").ToInt();
                        productLossesFrame.ShowTab();
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (row != null)
                        {
                            bool archived = row.CheckGet("ARCHIVED_FLAG").ToBool();
                            result = (row.CheckGet("CATEGORY_ID").ToInt() != 4) && !archived;
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "showlabel",
                    Title = "Посмотреть ярлык",
                    Group = "operations",
                    MenuUse = true,
                    ButtonUse = false,
                    ButtonName = "",
                    Description = "Посмотреть макет ярлыка на продукцию",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            ShowLabelTemplate();
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (row != null)
                        {
                            result = !row.CheckGet("PATH_TK").IsNullOrEmpty();
                        }
                        return result;
                    },
                });
            }
            Commander.Init(this);
        }

        /// <summary>
        /// Список групп, в которые входит пользователь
        /// </summary>
        public List<string> UserGroups { get; set; }

        /// <summary>
        /// Таймер заполнения поля шаблона загрузки данных
        /// </summary>
        public DispatcherTimer TemplateTimeoutTimer;

        /// <summary>
        /// Обработка и выполнение команд
        /// </summary>
        /// <param name="command"></param>
        public void ProcessMessage(ItemMessage msg = null)
        {
            if (msg != null)
            {
                string action = msg.Action.ClearCommand();
                if (!action.IsNullOrEmpty())
                {
                    switch (action)
                    {
                        case "refresh":
                            if (TemplateText.Text.Length > 2)
                            {
                                CheckSearch();
                            }
                            else
                            {
                                var dw = new DialogWindow("Для загрузки данных введите что-нибудь в поле фильтра", "Список изделий");
                                dw.ShowDialog();
                            }
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            var productList = new Dictionary<string, string>()
            {
                { "0", "Все" },
                { "1", "Изделия" },
                { "2", "Заготовки" },
            };
            ProductType.Items = productList;
        }

        /// <summary>
        /// Инициализация грида
        /// </summary>
        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "Ид",
                    Path = "ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 8,
                },
                new DataGridHelperColumn
                {
                    Header = "Артикул",
                    Path = "ARTIKUL",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 15,
                },
                new DataGridHelperColumn
                {
                    Header = "Артикул для печати",
                    Path = "ARTICLE_FOR_PRINT",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 15,
                },
                new DataGridHelperColumn
                {
                    Header = "Наименование",
                    Path = "PRODUCT_NAME",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 26,
                },
                new DataGridHelperColumn
                {
                    Header = "Единица измерения",
                    Path = "MEASURE_UNIT",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 4,
                },
                new DataGridHelperColumn
                {
                    Header = "Тип продукции",
                    Path = "PRODUCT_CLASS_NAME",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 10,
                },
                new DataGridHelperColumn
                {
                    Header = "Потребитель",
                    Path = "CUSTOMER_NAME",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 16,
                },
                new DataGridHelperColumn
                {
                    Header = "Дата последней отгрузки",
                    Path = "LAST_SHIPMENT",
                    ColumnType = ColumnTypeRef.DateTime,
                    Width2 = 8,
                    Format = "dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header = "Дата следующей отгрузки",
                    Path = "NEXT_SHIPMENT",
                    ColumnType = ColumnTypeRef.DateTime,
                    Width2 = 8,
                    Format = "dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header = "Название 2",
                    Path = "SECOND_NAME",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 12,
                },
                new DataGridHelperColumn
                {
                    Header = "Описание",
                    Path = "DETAILS",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 12,
                },
                new DataGridHelperColumn
                {
                    Header = "Картон",
                    Path = "CARDBOARD",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 14,
                },
                new DataGridHelperColumn
                {
                    Header = "Длина, мм",
                    Path = "LENGTH",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                    Header = "Ширина, мм",
                    Path = "WIDTH",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                    Header = "Высота, мм",
                    Path = "HEIGHT",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                    Header = "Площадь, кв.м",
                    Path = "SQUARE",
                    ColumnType = ColumnTypeRef.Double,
                    Width2 = 8,
                    Format = "N6"
                },
                new DataGridHelperColumn
                {
                    Header = "Симметричная рилевка",
                    Path = "_SYMMETRIC",
                    ColumnType = ColumnTypeRef.Boolean,
                    Width2 = 4,
                },
                new DataGridHelperColumn
                {
                    Header = "Рилевки",
                    Path = "_SCORERS",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 10,
                },
                new DataGridHelperColumn
                {
                    Header = "Тип рилевки",
                    Path = "_SCORERS_TYPE",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 4,
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во ярлыков",
                    Path = "LABEL_QTY",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 3,
                },
                new DataGridHelperColumn
                {
                    Header = "Укладка",
                    Path = "LAYOUT",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                    Header = "Поддон",
                    Path = "PALLET_NAME",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 8,
                },
                new DataGridHelperColumn
                {
                    Header = "На поддоне",
                    Path = "PRODUCTS_IN_PALLET",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                    Header = "Упаковка",
                    Path = "PACKING",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 5,
                },
                new DataGridHelperColumn
                {
                    Header = "Нестандартные припуски",
                    Path = "IS_LOSSES",
                    ColumnType = ColumnTypeRef.Boolean,
                    Width2 = 4,
                },
                new DataGridHelperColumn
                {
                    Header = "Клише",
                    Path = "CLICHE",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 8,
                },
                new DataGridHelperColumn
                {
                    Header = "Штанцформа",
                    Path = "STANZE",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 8,
                },
                new DataGridHelperColumn
                {
                    Header = "Посредник",
                    Path = "INTERMEDIARY",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 12,
                },
                new DataGridHelperColumn
                {
                    Header = "Посредник на ярлыке",
                    Path = "INTERMEDIARY_LABEL",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 12,
                },
                new DataGridHelperColumn
                {
                    Header = "ТУ на ярлыке",
                    Path = "TECHNICAL_CONDITION",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 8,
                },
                new DataGridHelperColumn
                {
                    Header = "Ид категории",
                    Path = "CATEGORY_ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header = "В архиве",
                    Path = "ARCHIVED_FLAG",
                    ColumnType = ColumnTypeRef.Integer,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header = "Путь к техкарте",
                    Path = "PATH_TK",
                    ColumnType = ColumnTypeRef.String,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header = "Ид покупателя",
                    Path = "BUYER_ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header = "Штрихкод покупателя",
                    Path = "BARCODE_CUSTOMER",
                    ColumnType = ColumnTypeRef.String,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header = "Наименование первой единицы измерения",
                    Path = "MEASURE_NAME",
                    ColumnType = ColumnTypeRef.String,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header = "Ид первой единицы измерения",
                    Path = "MEASURE_ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header = "Наименование второй единицы измерения",
                    Path = "MEASURE2_NAME",
                    ColumnType = ColumnTypeRef.String,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header = "Ид второй единицы измерения",
                    Path = "MEASURE2_ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Hidden = true,
                },
            };
            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("ID");
            Grid.SetSorting("ID", ListSortDirection.Descending);
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.SearchText = SearchText;
            Grid.Toolbar = GridToolbar;
            Grid.Commands = Commander;

            // Раскраска строк
            Grid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                // Цвета шрифта строк
                {
                    DataGridHelperColumn.StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        var currentStatus = row.CheckGet("ARCHIVED_FLAG").ToBool();
                        if (currentStatus == true)
                        {
                            color = HColor.OliveFG;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };

            //данные грида
            Grid.OnLoadItems = LoadItems;
            Grid.OnFilterItems = FilterItems;
            Grid.UseProgressSplashAuto = false;

            /*
            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateActions(selectedItem);
                }
            };

            Grid.OnDblClick = selectedItem =>
            {
                EditBlank();
            };
            */
            // без автообновления
            Grid.AutoUpdateInterval = 0;
            Grid.Init();
        }

        /// <summary>
        /// Получение списка групп, в которые входит пользователь
        /// </summary>
        private async void LoadUserGroup()
        {
            UserGroups = new List<string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Accounts");
            q.Request.SetParam("Object", "Group");
            q.Request.SetParam("Action", "ListByUser");
            q.Request.SetParam("ID", Central.User.EmployeeId.ToString());

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestGridAttempts;

            await Task.Run(() =>
            {
                q.DoQuery();
            }
            );

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var employeeGroups = ListDataSet.Create(result, "ITEMS");
                    if (employeeGroups.Items.Count > 0)
                    {
                        foreach (var item in employeeGroups.Items)
                        {
                            if (item.CheckGet("ID").ToInt() != 1)
                            {
                                if (item.CheckGet("IN_GROUP").ToBool())
                                {
                                    string groupCode = item.CheckGet("CODE");
                                    if (!string.IsNullOrEmpty(groupCode))
                                    {
                                        UserGroups.Add(groupCode);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Для менеджеров в фильтре типа продукции выбираем изделия, для остальных показываем всё
            string key = "0";
            if (UserGroups.Contains("manager"))
            {
                key = "1";
            }
            ProductType.SetSelectedItemByKey(key);
        }

        /// <summary>
        /// Проверка содержимого поиска перед загрузкой
        /// </summary>
        public void CheckSearch()
        {
            if (TemplateText.Text.Length > 2)
            {
                Grid.LoadItems();
            }
        }

        /// <summary>
        /// Запуск таймера заполнения шаблона загрузки данных
        /// </summary>
        public void RunTemplateTimeoutTimer()
        {
            if (TemplateTimeoutTimer == null)
            {
                TemplateTimeoutTimer = new DispatcherTimer
                {
                    Interval = new TimeSpan(0, 0, 2)
                };

                {
                    var row = new Dictionary<string, string>();
                    row.CheckAdd("TIMEOUT", "2000");
                    row.CheckAdd("DESCRIPTION", "");
                    Central.Stat.TimerAdd("AssortmentList_RunTemplateTimeoutTimer", row);
                }

                TemplateTimeoutTimer.Tick += (s, e) =>
                {
                    // Если введены только один или два символа, ничего не загружаем, ждём следующий
                    if (TemplateText.Text.Length > 2)
                    {
                        CheckSearch();
                    }
                    StopTemplateTimeoutTimer();
                };
            }

            if (TemplateTimeoutTimer.IsEnabled)
            {
                TemplateTimeoutTimer.Stop();
            }
            TemplateTimeoutTimer.Start();
        }

        /// <summary>
        /// Остановка таймера заполнения заблона загрузки данных
        /// </summary>
        public void StopTemplateTimeoutTimer()
        {
            if (TemplateTimeoutTimer != null)
            {
                if (TemplateTimeoutTimer.IsEnabled)
                {
                    TemplateTimeoutTimer.Stop();
                }
            }
        }

        /// <summary>
        /// Загрузка данных из БД
        /// </summary>
        public async void LoadItems()
        {
            if (TemplateText.Text.Length > 2)
            {
                // Блокируем кнопок тулбара
                EditButton.IsEnabled = false;
                CopyBlankButton.IsEnabled = false;
                ArchiveButton.IsEnabled = false;
                TechMapButton.IsEnabled = false;
                ToExcelButton.IsEnabled = false;

                Grid.Toolbar.IsEnabled = false;
                Grid.ShowSplash();

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Products");
                q.Request.SetParam("Object", "Assortment");
                q.Request.SetParam("Action", "List");
                q.Request.SetParam("SEARCH", TemplateText.Text);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "PRODUCTS");
                        var processedDs = ProcessItems(ds);
                        Grid.UpdateItems(processedDs);

                        if (processedDs.Items.Count > 0)
                        {
                            EditButton.IsEnabled = true;
                            ToExcelButton.IsEnabled = true;

                            Grid.SelectRowFirst();
                        }
                    }
                }

                Grid.HideSplash();
                Grid.Toolbar.IsEnabled = true;
            }
        }

        /// <summary>
        /// Обработка данных перед загрузкой в таблицу
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        public ListDataSet ProcessItems(ListDataSet ds)
        {
            ListDataSet _ds = ds;
            if (ds != null)
            {
                if (ds.Items.Count > 0)
                {
                    foreach (var item in _ds.Items)
                    {
                        string scorers = "";
                        string scorerType = "";
                        string symmetric = "0";

                        bool schemeExists = item.CheckGet("SCHEME_EXISTS").ToBool();

                        if ((item.CheckGet("CATEGORY_ID").ToInt() == 4) || !schemeExists)
                        {
                            int width = item.CheckGet("WIDTH").ToInt();
                            int symmetricScorer = item.CheckGet("HEIGHT").ToInt();

                            // Визуализируем рилевки
                            if (symmetricScorer > 0)
                            {
                                item["HEIGHT"] = "";
                                if ((width > 0) && (width > symmetricScorer * 2))
                                {
                                    scorers = $"{symmetricScorer}/{width - symmetricScorer * 2}/{symmetricScorer}";
                                    symmetric = "1";
                                }
                            }
                            else
                            {
                                int crease1 = item.CheckGet("P1").ToInt();
                                int creaseLast = width - crease1;
                                if (crease1 > 0)
                                {
                                    scorers = crease1.ToString();
                                    for (var i = 2; i <= 24; i++)
                                    {
                                        var creaseKey = $"P{i}";
                                        int crease_i = item.CheckGet(creaseKey).ToInt();
                                        if (crease_i > 0)
                                        {
                                            scorers = $"{scorers}/{crease_i}";
                                            creaseLast -= crease_i;
                                        }
                                    }
                                    scorers = $"{scorers}/{creaseLast}";
                                }
                            }

                            if (!scorers.IsNullOrEmpty())
                            {
                                int scorerProfile = item.CheckGet("SCORER_PROFILE").ToInt();
                                switch (scorerProfile)
                                {
                                    case 1:
                                        scorerType = "п/м";
                                        break;
                                    case 2:
                                        scorerType = "пл";
                                        break;
                                    case 3:
                                        scorerType = "любой";
                                        break;
                                    case 4:
                                        scorerType = "п/п";
                                        break;
                                }
                            }
                        }

                        item.Add("_SCORERS", scorers);
                        item.Add("_SCORERS_TYPE", scorerType);
                        item.Add("_SYMMETRIC", symmetric);
                    }
                }
            }

            return _ds;
        }

        /// <summary>
        /// Фильтрация строк таблицы
        /// </summary>
        public void FilterItems()
        {
            if (Grid.Items != null)
            {
                if (Grid.Items.Count > 0)
                {
                    bool allRecords = (bool)ShowArchivedCheckBox.IsChecked;
                    int t = ProductType.SelectedItem.Key.ToInt();

                    var list = new List<Dictionary<string, string>>();
                    foreach (var item in Grid.Items)
                    {
                        bool includeByArchive = true;
                        bool includeByType = true;
                        if (!allRecords)
                        {
                            if (item.CheckGet("ARCHIVED_FLAG").ToInt() == 1)
                            {
                                includeByArchive = false;
                            }
                        }

                        if (t == 1)
                        {
                            if (item.CheckGet("CATEGORY_ID").ToInt() == 4)
                            {
                                includeByType = false;
                            }
                        }
                        else if (t == 2)
                        {
                            if (item.CheckGet("CATEGORY_ID").ToInt() == 5 || item.CheckGet("CATEGORY_ID").ToInt() == 6)
                            {
                                includeByType = false;
                            }
                        }

                        if (includeByArchive
                            && includeByType
                        )
                        {
                            list.Add(item);
                        }
                    }
                    Grid.Items = list;
                    Grid.SelectRowFirst();
                }
            }
        }

        /// <summary>
        /// обновление методов работы с выбранной записью
        /// </summary>
        /// <param name="selectedItem"></param>
        public void UpdateActions(Dictionary<string, string> selectedItem)
        {
            //bool isBlank2 = Grid.SelectedItem.CheckGet("CATEGORY_ID").ToInt() == 4;
            bool isBlank = selectedItem.CheckGet("CATEGORY_ID").ToInt() == 4;
            bool archived = selectedItem.CheckGet("ARCHIVED_FLAG").ToBool();

            CopyBlankButton.IsEnabled = isBlank && !archived;
            ArchiveButton.IsEnabled = isBlank && !archived;
            Grid.Menu["SetArchive"].Enabled = isBlank && !archived;
            Grid.Menu["ShowShipmentHistory"].Enabled = !isBlank;

            // Редактировать припуски могут только инженеры ОПП и ПДС
            Grid.Menu["ShowProductLosses"].Enabled = !isBlank && !archived;

            var path = selectedItem.CheckGet("PATH_TK");
            TechMapButton.IsEnabled = !path.IsNullOrEmpty();
            Grid.Menu["ShowTechnologicalMap"].Enabled = !path.IsNullOrEmpty();
            Grid.Menu["ShowLabel"].Enabled = !path.IsNullOrEmpty();
        }

        /// <summary>
        /// Загрузка файла техкарты
        /// </summary>
        private void ShowTechnologicalMap()
        {
            if (Grid.Items != null)
            {
                if (Grid.Items.Count > 0)
                {
                    if (Grid.SelectedItem != null)
                    {
                        var path = Grid.SelectedItem.CheckGet("PATH_TK");
                        if (!string.IsNullOrEmpty(path))
                        {
                            if (File.Exists(path))
                            {
                                Central.OpenFile(path);
                            }
                        }

                    }
                }
            }
        }

        /// <summary>
        /// Открытие вкладки редактирования изделия
        /// </summary>
        public void EditBlank()
        {
            int productId = Grid.SelectedItem.CheckGet("ID").ToInt();
            var productForm = new Product();
            productForm.ReceiverName = ControlName;
            productForm.CategoryId = Grid.SelectedItem.CheckGet("CATEGORY_ID").ToInt();
            productForm.Edit(productId);
        }

        /// <summary>
        /// Установка признака Архивный
        /// </summary>
        public async void SetArchive()
        {
            if (Grid.Items != null)
            {
                if (Grid.SelectedItem != null)
                {
                    var dw = new DialogWindow("Вы уверены, что заготовку надо отправить в архив?", "Ассортимент", "", DialogWindowButtons.NoYes);
                    if ((bool)dw.ShowDialog())
                    {
                        if (dw.ResultButton == DialogResultButton.Yes)
                        {
                            var q = new LPackClientQuery();
                            q.Request.SetParam("Module", "Products");
                            q.Request.SetParam("Object", "Assortment");
                            q.Request.SetParam("Action", "SetArchive");
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
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Копирование заготовки
        /// </summary>
        private void CopyBlank()
        {
            if (Grid.Items != null)
            {
                if (Grid.SelectedItem != null)
                {
                    var selectedItem = Grid.SelectedItem;
                    if (selectedItem.CheckGet("CATEGORY_ID").ToInt() == 4)
                    {
                        var dw = new DialogWindow("Вы уверены, что надо создать копию заготовки?", "Ассортимент", "", DialogWindowButtons.NoYes);
                        if ((bool)dw.ShowDialog())
                        {
                            if (dw.ResultButton == DialogResultButton.Yes)
                            {
                                CopyData(Grid.SelectedItem.CheckGet("ID"));
                            }
                        }
                    }
                    else
                    {
                        var dw = new DialogWindow("Скопировать можно только заготовку для Г-образного ящика", "Ассортимент", "");
                        dw.ShowDialog();
                    }
                }
            }
        }

        /// <summary>
        /// Отправка на сервер данных для создания копии заготовки
        /// </summary>
        /// <param name="blankId"></param>
        private async void CopyData(string blankId)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Products");
            q.Request.SetParam("Object", "Assortment");
            q.Request.SetParam("Action", "BlankCopy");
            q.Request.SetParam("ID", blankId);

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
                        var items = ListDataSet.Create(result, "ITEM");
                        string art = items.Items[0].CheckGet("ARTICLE");
                        Grid.LoadItems();
                        var dr = new DialogWindow($"Создана заготовка с артикулом {art}", "Ассортимент", "");
                        dr.ShowDialog();
                    }
                }
            }
            else if (q.Answer.Error.Code == 145)
            {
                var dw = new DialogWindow(q.Answer.Error.Message, "Ассортимент", "");
                dw.ShowDialog();
            }
        }

        /// <summary>
        /// Формирование шаблона ярлыка для позиции ассортимента
        /// </summary>
        private async void ShowLabelTemplate()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Label");
            q.Request.SetParam("Action", "MakeTemplate");
            q.Request.SetParam("ID", Grid.SelectedItem.CheckGet("ID"));

            {
                int buyerId = Grid.SelectedItem.CheckGet("BUYER_ID").ToInt();
                if (buyerId != 0 && !string.IsNullOrEmpty(Grid.SelectedItem.CheckGet("BARCODE_CUSTOMER")))
                {
                    switch (buyerId)
                    {
                        case 8144:
                        case 7600:
                        case 4638:
                        case 7251:
                        case 9497:
                        case 7690:
                        case 6454:
                        case 9834:
                        case 4396:
                        case 8948:
                        case 7469:
                            break;

                        default:
                            {
                                var w = new BarcodeType();
                                w.ParentFrame = this.FrameName;
                                w.Show();

                                if (!string.IsNullOrEmpty(w.SelectedType.Key))
                                {
                                    q.Request.SetParam("CUSTOMER_BARCODE_TYPE", w.SelectedType.Key);
                                }
                            }
                            break;
                    }
                }
            }

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
        }

        private void TemplateText_KeyUp(object sender, KeyEventArgs e)
        {
            RunTemplateTimeoutTimer();
        }

        private void ShowArchivedCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void ProductType_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }
    }
}
