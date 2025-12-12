using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Production.Complectation;
using Client.Interfaces.Stock;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using static Client.Interfaces.Production.ComplectationMainComplectationTab;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Комплектация переработка Кашира
    /// </summary>
    public partial class ComplectationProcessingKsh : UserControl
    {
        public ComplectationProcessingKsh()
        {
            InitializeComponent();
            Loaded += (object sender, RoutedEventArgs e) =>
            {
                Central.WM.SetActive(FrameName);
                Central.WM.SelectedTab = FrameName;
            };

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            InitForm();
            SetDefaults();

            InitProductGrid();
            InitPalletGrid();
            InitCompletedGrid();

            LoadProductItems();

            ProcessPermissions();
        }

        public int FactoryId = 2;

        public string RoleName = "[erp]complectation_pm_ksh";

        /// <summary>
        /// Техническое имя таба
        /// </summary>
        public string FrameName = "ComplectationProcessingKsh";

        public string ControlTitle = "Комплектация переработка КШ";

        /// <summary>
        /// Выбранный тип комплектации
        /// </summary>
        private ComplectationTypeRef ComplectationType { get; set; }

        /// <summary>
        /// Выбранная запись в гриде товаров
        /// </summary>
        private Dictionary<string, string> SelectedProductItem { get; set; }

        /// <summary>
        /// Датасет товаров, которые находятся в КПР0 для комплектации
        /// </summary>
        private ListDataSet ProductDataSet { get; set; }

        /// <summary>
        /// Выбранная запись в гриде поддонов, из которых комплектуются новые
        /// </summary>
        public Dictionary<string, string> SelectedPalletItem { get; set; }

        /// <summary>
        /// Датасет поддонов, из которых комплектуются новые
        /// </summary>
        private ListDataSet PalletDataSet { get; set; }

        /// <summary>
        /// Выбранная запис в гриде скомплектованных позиций
        /// </summary>
        public Dictionary<string, string> SelectedCompletedItem { get; set; }

        /// <summary>
        /// Датасет с данными по скомплектованным позициям
        /// </summary>
        public ListDataSet CompletedDataSet { get; set; }

        /// <summary>
        /// Количество товара на выбранных поддонах, из которых будут комплектоваться новые
        /// </summary>
        private int WritenOffLabelValue => PalletGrid.Items?.Sum(x => (x.ContainsKey("SelectedFlag") && x["SelectedFlag"].ToBool()) ? x["KOL"].ToInt() : 0) ?? 0;

        /// <summary>
        /// Количество выбранных поддонов, из которых будут скомплектованы новые 
        /// </summary>
        private int WritenOffPalletLabelValue => PalletGrid.Items?.Count(x => x.CheckGet("SelectedFlag").ToBool()) ?? 0;

        /// <summary>
        /// Флаг того, что запрос при сканировании ярлыка ещё работает
        /// </summary>
        private bool QueryInProgress { get; set; }

        #region default functions

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m">Сообщение</param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.Contains("ComplectationKsh"))
            {
                if (m.ReceiverName == this.FrameName)
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            SetDefaults();
                            LoadProductItems();
                            CompletedGrid.LoadItems();
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Обработчик нажатий клавиатуры
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;

            switch (e.Key)
            {
                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

                case Key.Enter:
                    var code = Central.WM.GetScannerInput();
                    if (!string.IsNullOrEmpty(code))
                    {
                        ScannerInputProcess(code);
                    }
                    break;
            }
        }

        private void ScannerInputProcess(string code)
        {
            // Если в данный момент не выполняется запрос, то можем обрабатывать новый введённый штрихкод
            if (!QueryInProgress)
            {
                if (!string.IsNullOrEmpty(code) && code.Length >= 13)
                {
                    MovePallet(code);
                    QueryInProgress = false;
                }
            }
        }

        /// <summary>
        /// Запрос на перемещение поддона
        /// Вызывается в моемент сканирования ярлыка
        /// </summary>
        /// <param name="code"></param>
        private async void MovePallet(string str)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                SetSplash(true, "Обработка поддона");
            });

            str = str.Trim();
            if (str.Length == 13 && int.TryParse(str.Substring(3, 9), out var palletId))
            {
                {
                    var p = new Dictionary<string, string>();
                    p.Add("PALLET_ID", $"{palletId}");

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Complectation");
                    q.Request.SetParam("Object", "Pallet");
                    q.Request.SetParam("Action", "MoveProcessingMachineKsh");
                    q.Request.SetParams(p);

                    q.Request.Timeout = Central.Parameters.RequestTimeoutDefault;
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
                                if (ds.Items[0].CheckGet("PALLET_ID").ToInt() > 0)
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        string cell = "";
                                        try
                                        {
                                            cell = ds.Items[0].CheckGet("CELL");
                                        }
                                        catch (Exception ex)
                                        {
                                        }

                                        string msg = $"Поддон успешно перемещён в ячейку {cell}";
                                        int status = 2;
                                        var d = new StackerScanedLableInfo(msg, status);
                                        d.WindowMaxSizeFlag = true;
                                        d.ShowAndAutoClose(1);

                                        LoadProductItems();
                                    });
                                }
                            }
                        }
                    }
                    else if (q.Answer.Status == 145)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            string msg = q.Answer.Error.Message;
                            int status = 1;
                            var d = new StackerScanedLableInfo(msg, status);
                            d.WindowMaxSizeFlag = true;
                            d.ShowAndAutoClose(1);
                        });

                        q.SilentErrorProcess = true;
                        q.ProcessError();
                    }
                    else
                    {
                        q.SilentErrorProcess = true;
                        q.ProcessError();
                    }
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                SetSplash(false);
            });
        }

        private void SetSplash(bool inProgressFlag, string msg = "Загрузка")
        {
            QueryInProgress = inProgressFlag;
            SplashControl.Visible = inProgressFlag;

            SplashControl.Message = msg;
        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel(this.RoleName);
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
        }

        /// <summary>
        /// Инициализация формы
        /// </summary>
        public void InitForm()
        {

        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            PalletDataSet = new ListDataSet();
            CompletedDataSet = new ListDataSet();
            ProductDataSet = new ListDataSet();

            SelectedProductItem = new Dictionary<string, string>();
            SelectedPalletItem = new Dictionary<string, string>();
            SelectedCompletedItem = new Dictionary<string, string>();

            PalletGrid.UpdateItems(PalletDataSet);
            ProductGrid.UpdateItems(ProductDataSet);
            CompletedGrid.UpdateItems(CompletedDataSet);

            ComplectationType = ComplectationTypeRef.ProcessingMachineKsh;
        }

        /// <summary>
        /// Вызов справки
        /// </summary>
        private void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp-new/production_new/complectation/pererab_complectation");
        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        private void Close()
        {
            Central.WM.RemoveTab(this.FrameName);
            Destroy();
        }

        /// <summary>
        /// Деструктор компонентов. Завершает вспомогательные процессы
        /// </summary>
        private void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage
            {
                ReceiverGroup = "ComplectationKsh",
                ReceiverName = "",
                SenderName = this.FrameName,
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// Обработчик нажатий клавиатуры
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessKeyboard(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// Отображение вкладки с формой редактирования данных
        /// </summary>
        private void Show()
        {
            var title = "Комплектация на ПР КШ";
            Central.WM.AddTab(this.FrameName, title, true, "add", this);
        }

        #endregion

        #region init grids

        private void InitProductGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "Ячейка",
                    Path = "PLACE",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header = "Наименование",
                    Path = "NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=39,
                },
                new DataGridHelperColumn
                {
                    Header = "Всего на поддонах, шт.",
                    Path = "KOL_SUM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header = "На поддоне по умолчанию, шт.",
                    Path = "KOL_PAK",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header = "Поддонов, шт.",
                    Path = "PALLETS",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header = "Закончить до",
                    Path = "FINISH_BEFORE_DTTM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Width2=15,
                },
                new DataGridHelperColumn
                {
                    Header = "Дата",
                    Path = "DT",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn
                {
                    Header = "Время",
                    Path = "TM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=15,
                },

                new DataGridHelperColumn
                {
                    Header = "Тех карта",
                    Path = "PATHTK",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 220,
                    MaxWidth = 600,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header = "SKLAD",
                    Path = "SKLAD",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 40,
                    MaxWidth = 50,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header = "NUM_PLACE",
                    Path = "NUM_PLACE",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 40,
                    MaxWidth = 50,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header = "IDORDERDATES",
                    Path = "IDORDERDATES",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 40,
                    MaxWidth = 50,
                    Hidden = true,
                },

                new DataGridHelperColumn
                {
                    Header = " ",
                    Path = " ",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 5,
                    MaxWidth = 2000,
                },
            };
            ProductGrid.SetColumns(columns);
            ProductGrid.SetSorting("FINISH_BEFORE_DTTM");
            ProductGrid.AutoUpdateInterval = 60;
            ProductGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;

            ProductGrid.Menu = new Dictionary<string, DataGridContextMenuItem>();

            ProductGrid.OnSelectItem = selectedItem =>
            {
                SelectedProductItem = selectedItem;
                if (selectedItem != null)
                {
                    LoadPalletItems();

                    UpdateNewPalletButtons();
                }
            };

            ProductGrid.RowStylers =
            new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>
            {
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor, row =>
                    {
                        var color = "";

                        if (!string.IsNullOrEmpty(row.CheckGet("FINISH_BEFORE_DTTM")))
                        {
                            if ((row.CheckGet("FINISH_BEFORE_DTTM").ToDateTime("dd.MM.yyyy HH:mm:ss") - DateTime.Now).TotalHours <= 2)
                            {
                                    color = HColor.Yellow;

                                if ((row.CheckGet("FINISH_BEFORE_DTTM").ToDateTime("dd.MM.yyyy HH:mm:ss") - DateTime.Now).TotalHours <= 1)
                                {
                                    color = HColor.Orange;

                                    if ((row.CheckGet("FINISH_BEFORE_DTTM").ToDateTime("dd.MM.yyyy HH:mm:ss") - DateTime.Now).TotalHours <= 0)
                                    {
                                        color = HColor.Red;
                                    }
                                }
                            }
                        }

                        var result = !string.IsNullOrEmpty(color) ? color.ToBrush() : DependencyProperty.UnsetValue;

                        return result;
                    }
                }
            };

            ProductGrid.Init();
            ProductGrid.Run();

            ProductGrid.UpdateItems(ProductDataSet);
        }

        private void InitPalletGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "*",
                    Path = "SelectedFlag",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth = 45,
                    MaxWidth = 45,
                    Editable = true,
                    OnClickAction = (row, el) =>
                    {
                       UpdateNewPalletButtons();
                       return null;
                    },
                },
                new DataGridHelperColumn
                {
                    Header = "Поддон",
                    Path = "PALLET",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 70,
                    MaxWidth = 300,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество на поддоне, шт.",
                    Path = "KOL",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 80,
                    MaxWidth = 82,
                },
                new DataGridHelperColumn
                {
                    Header = "Причина",
                    Path = "FTYPE_DESCR",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 70,
                    MaxWidth = 300,
                },
                new DataGridHelperColumn
                {
                    Header = "Дата",
                    Path = "DT",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width = 67,
                },
                new DataGridHelperColumn
                {
                    Header = "Время",
                    Path = "TM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width = 55,
                },
                new DataGridHelperColumn
                {
                    Header = "Ячейка",
                    Path = "PLACE",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 30,
                    MaxWidth = 57,
                },

                new DataGridHelperColumn
                {
                    Header = "Количество на остатке",
                    Doc = "По этому приходу",
                    Path = "QUANTITY_BY_CONSIGNMENT",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 55,
                    MaxWidth = 55,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header = "SKLAD",
                    Path = "SKLAD",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 40,
                    MaxWidth = 50,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header = "NUM_PLACE",
                    Path = "NUM_PLACE",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 40,
                    MaxWidth = 50,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header = "Начало кондиционирования",
                    Path = "CONDITION_DTTM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Hidden = true,
                },

                new DataGridHelperColumn
                {
                    Header = " ",
                    Path = "_",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 5,
                    MaxWidth = 800,
                },
            };
            PalletGrid.SetColumns(columns);
            PalletGrid.SetSorting("PALLET");

            PalletGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>
            {
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor, row =>
                    {
                        var color = "";

                        if (row.CheckGet("SelectedFlag").ToBool())
                        {
                            color = HColor.Yellow;
                        }

                        var result = !string.IsNullOrEmpty(color) ? color.ToBrush() : DependencyProperty.UnsetValue;

                        return result;
                    }
                }
            };

            PalletGrid.Menu = new Dictionary<string, DataGridContextMenuItem>()
            {
                {
                    "MovingHistory",
                    new DataGridContextMenuItem()
                    {
                        Header="История перемещения",
                        Action=()=>
                        {
                            MovingHistory(PalletGrid.SelectedItem);
                        },
                    }
                },
            };

            PalletGrid.OnSelectItem = selectedItem =>
            {
                SelectedPalletItem = selectedItem;
                UpdateNewPalletButtons();
            };

            PalletGrid.SelectItemMode = 2;
            PalletGrid.AutoUpdateInterval = 0;
            PalletGrid.Init();
            PalletGrid.Run();
        }

        /// <summary>
        /// Инициализация грида с скомплектованными позициями
        /// </summary>
        private void InitCompletedGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "Номер ПЗ",
                    Path = "NUM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 32,
                    MaxWidth = 50,
                },
                new DataGridHelperColumn
                {
                    Header = "Наименование",
                    Path = "CONSUMPTION_NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 120,
                    MaxWidth = 1600,
                },
                new DataGridHelperColumn
                {
                    Header = "Артикул",
                    Path = "CONSUMPTION_ARTIKUL",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 47,
                    MaxWidth = 120,
                },
                new DataGridHelperColumn
                {
                    Header = "Расход, шт.",
                    Path = "CONSUMPTION_QTY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 30,
                    MaxWidth = 80,
                },
                new DataGridHelperColumn
                {
                    Header = "Приход, шт.",
                    Path = "INCOMING_QTY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 30,
                    MaxWidth = 80,
                },

                new DataGridHelperColumn
                {
                    Header = "Дата",
                    Path = "DT",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Format = "HH:mm dd.MM.yyyy",
                    MinWidth = 65,
                    MaxWidth = 110,
                    Hidden = false,
                },
                new DataGridHelperColumn
                {
                    Header = "ИД ПЗ",
                    Path = "ID_PZ",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 40,
                    MaxWidth = 55,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header = "Старая продукция",
                    Path = "CONSUMPTION_PRODUCT_ID",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header = "Новая продукция",
                    Path = "INCOMING_PRODUCT_ID",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden = true,
                },
            };
            CompletedGrid.SetColumns(columns);

            CompletedGrid.AutoUpdateInterval = 0;

            CompletedGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>
            {
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result = DependencyProperty.UnsetValue;
                        var color = "";

                        if (row.CheckGet("CONSUMPTION_PRODUCT_ID").ToInt() != row.CheckGet("INCOMING_PRODUCT_ID").ToInt())
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

            CompletedGrid.OnSelectItem = selectedItem =>
            {
                SelectedCompletedItem = selectedItem;

                UpdateNewPalletButtons();
            };

            CompletedGrid.OnLoadItems = LoadCompletedItems;

            CompletedGrid.Init();
            CompletedGrid.Run();
        }

        #endregion

        #region load data

        /// <summary>
        /// Загрузка данных по поддонам с выбранной продукцией на комплектации
        /// </summary>
        private async void LoadPalletItems()
        {
            PalletGrid.ShowSplash();

            PalletGrid.ClearItems();

            if (SelectedProductItem != null)
            {
                var p = new Dictionary<string, string>
                {
                    ["id2"] = SelectedProductItem.CheckGet("ID2"),
                    ["SKLAD"] = SelectedProductItem.CheckGet("SKLAD"),
                    ["NUM_PLACE"] = SelectedProductItem.CheckGet("NUM_PLACE"),
                    ["IDORDERDATES"] = SelectedProductItem.CheckGet("IDORDERDATES"),
                    ["FACTORY_ID"] = $"{FactoryId}"
                };

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Complectation");
                q.Request.SetParam("Object", "Pallet");
                q.Request.SetParam("Action", "ListConversion");

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
                        PalletDataSet = ListDataSet.Create(result, "ITEMS");
                        PalletGrid.UpdateItems(PalletDataSet);
                    }
                }
            }

            PalletGrid.HideSplash();
        }

        private async void LoadProductItems()
        {
            ProductGrid.ShowSplash();

            ProductGrid.ClearItems();

            SelectedProductItem = null;
            if (PalletGrid != null)
            {
                PalletGrid.ClearItems();
            }

            var p = new Dictionary<string, string>();
            p.Add("FACTORY_ID", $"{FactoryId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Complectation");
            q.Request.SetParam("Object", "Product");
            q.Request.SetParam("Action", "ListConversion");

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
                    ProductDataSet = ListDataSet.Create(result, "ITEMS");
                    ProductGrid.UpdateItems(ProductDataSet);
                }
            }

            ProductGrid.HideSplash();
        }

        /// <summary>
        /// Получедие данных для грида с скомплектованными позициями
        /// </summary>
        public async void LoadCompletedItems()
        {
            CompletedGridToolbar.IsEnabled = false;

            CompletedGrid.ShowSplash();

            CompletedGrid.ClearItems();

            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Complectation");
            q.Request.SetParam("Object", "Operation");
            q.Request.SetParam("Action", "ListConversionKsh");

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
                    CompletedDataSet = ListDataSet.Create(result, "ITEMS");
                    CompletedGrid.UpdateItems(CompletedDataSet);
                }
            }
            else
            {
                q.ProcessError();
            }

            CompletedGrid.HideSplash();

            CompletedGridToolbar.IsEnabled = true;
        }

        #endregion

        /// <summary>
        /// Получить историю перемещения по выбранному поддону
        /// </summary>
        public void MovingHistory(Dictionary<string, string> selectedItem)
        {
            if (selectedItem != null && selectedItem.Count > 0)
            {
                string movingId = selectedItem.CheckGet("IDPER");
                string incomingId = selectedItem.CheckGet("IDP");
                string palletNumber = selectedItem.CheckGet("NUM");

                if (!string.IsNullOrEmpty(palletNumber) && !string.IsNullOrEmpty(incomingId))
                {
                    var p = new Dictionary<string, string>();
                    p.Add("idp", incomingId);
                    p.Add("num", palletNumber);
                    p.Add("ID_PER", movingId);

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Stock");
                    q.Request.SetParam("Object", "Pallet");
                    q.Request.SetParam("Action", "ListHistory");
                    q.Request.SetParams(p);
                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                        if (result != null)
                        {
                            DialogWindow.ShowDialog(result["List"].Rows.Select(row => row[0]).Aggregate(string.Empty, (row, record) => row + record + "\n"), $"История перемещения поддона {selectedItem.CheckGet("NUM")}");
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
            }
        }

        /// <summary>
        /// Комплектация на Переработке
        /// </summary>
        public void ComplectationPM()
        {
            var oldPalletList = PalletDataSet.Items.Where(x => x.CheckGet("SelectedFlag").ToInt() == 1).ToList();
            var tab = new ComplectationMainComplectationTab(FrameName, SelectedProductItem, oldPalletList);
            tab.Show();
        }

        /// <summary>
        /// обновляем доступность кнопок новых поддонов в зависимости от ситуации
        /// </summary>
        private void UpdateNewPalletButtons()
        {
            var palletsSelected = PalletGrid.Items?.Count(row => row.CheckGet("SelectedFlag").ToInt() == 1) > 0;

            ComplectationButton.IsEnabled = palletsSelected;

            // количество товара
            WritenOffLabel.Content = WritenOffLabelValue;
            WritenOffPalletLabel.Content = WritenOffPalletLabelValue;

            if (SelectedCompletedItem != null)
            {
                if (SelectedCompletedItem.Count > 0)
                {
                    LabelPrintButton.IsEnabled = true;
                }
                else
                {
                    LabelPrintButton.IsEnabled = false;
                }
            }
            else
            {
                LabelPrintButton.IsEnabled = false;
            }

            if (SelectedProductItem != null)
            {
                TechnologicalMapButton.IsEnabled = true;
            }
            else
            {
                TechnologicalMapButton.IsEnabled = false;
            }

            ProcessPermissions();
        }

        /// <summary>
        /// Печать ярлыка выбранной комплектации;
        /// Открывает окно выбора поддонов, по которым нужно напечатать ярлык
        /// </summary>
        public void LabelPrint()
        {
            if (SelectedCompletedItem != null)
            {
                int idPz = SelectedCompletedItem.CheckGet("ID_PZ").ToInt();
                int idSt = SelectedCompletedItem.CheckGet("ID_ST").ToInt();
                int idk1 = SelectedCompletedItem.CheckGet("IDK1").ToInt();

                int idr = SelectedCompletedItem.CheckGet("IDR").ToInt();
                int incomingQuantity = SelectedCompletedItem.CheckGet("INCOMING_QTY").ToInt();

                if (idPz > 0 && idSt > 0 && idk1 > 0)
                {
                    var i = new ComplectationCompletedPalletList(idPz, idSt, idk1, idr, incomingQuantity);
                    i.Show();
                }
                else
                {
                    var msg = "Не найден идентификатор производственного задания, станка или категории товара";
                    var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                var msg = "Не выбрана комплектация";
                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// открытие excel файла тех карты
        /// </summary>
        public void OpenTechnologicalMap()
        {
            if (SelectedProductItem != null)
            {
                string pathTk = SelectedProductItem.CheckGet("PATHTK");

                if (!string.IsNullOrEmpty(pathTk))
                {
                    if (System.IO.File.Exists(pathTk))
                    {
                        Central.OpenFile(pathTk);
                    }
                    else
                    {
                        var msg = $"Файл {pathTk} не найден по указанному пути";
                        var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    var msg = "Не найден путь к Excel файлу тех карты";
                    var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                var msg = "Не выбран товар, для которого нужно найти тех карту";
                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Обновляет данные для всех гридов формы
        /// </summary>
        public void Refresh()
        {
            SetDefaults();

            if (PalletDataSet.Items != null)
            {
                PalletDataSet.Items.Clear();
                PalletGrid.UpdateItems(PalletDataSet);
            }

            LoadProductItems();

            CompletedGrid.LoadItems();

            UpdateNewPalletButtons();
        }


        /// <summary>
        /// Установка настроек для принтера
        /// </summary>
        public void SetPrintSettings()
        {
            LabelReport2.SetPrintingProfile();
        }

        /// <summary>
        /// Комплектация из воздуха
        /// </summary>
        public void AddProduct()
        {
            var i = new ComplectationPMProductList(true);
            i.FactoryId = this.FactoryId;
            i.Show();
        }

        private void ConsumptionOther()
        {
            var i = new ComplectationPalletConsumption();
            i.RoleName = this.RoleName;
            i.ParentFrame = this.FrameName;
            i.ComplectationType = this.ComplectationType;
            i.Show();
        }

        private void ComplectationButton_Click(object sender, RoutedEventArgs e)
        {
            ComplectationPM();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void TechnologicalMapButton_Click(object sender, RoutedEventArgs e)
        {
            OpenTechnologicalMap();
        }

        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            AddProduct();
        }

        private void LabelPrintButton_Click(object sender, RoutedEventArgs e)
        {
            LabelPrint();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        private void ConsumptionOtherButton_Click(object sender, RoutedEventArgs e)
        {
            ConsumptionOther();
        }
    }
}
