using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Stock;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DialogWindow = Client.Interfaces.Main.DialogWindow;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Комплектация СГП Кашира
    /// </summary>
    public partial class ComplectationStockKsh : UserControl
    {
        public ComplectationStockKsh()
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
            InitGridTo();
            InitCompletedGrid();

            ProcessPermissions();
        }

        public int FactoryId = 2;

        public string RoleName = "[erp]complectation_stock_ksh";

        /// <summary>
        /// Техническое имя таба
        /// </summary>
        public string FrameName = "ComplectationStockKsh";

        public string ControlTitle = "Комплектация СГП КШ";

        /// <summary>
        /// Датасет товаров
        /// </summary>
        private ListDataSet ProductDataSet { get; set; }

        /// <summary>
        /// Датасет поддонов, из которых комплектуются новые
        /// </summary>
        private ListDataSet PalletDataSet { get; set; }

        /// <summary>
        /// Датасет с данными по скомплектованным позициям
        /// </summary>
        public ListDataSet CompletedDataSet { get; set; }

        /// <summary>
        /// Выбранная запис в гриде скомплектованных позиций
        /// </summary>
        public Dictionary<string, string> SelectedCompletedItem { get; set; }

        /// <summary>
        /// Датасет новых поддонов, которые будут созданы в процессе комплектации
        /// </summary>
        private ListDataSet DataSetTo { get; set; }

        /// <summary>
        /// Выбранная запись в гриде новых поддонов, которые будут созданы в процессе комплектации
        /// </summary>
        private Dictionary<string, string> SelectedItemTo { get; set; }

        /// <summary>
        /// Количество товара на выбранных поддонах, из которых будут комплектоваться новые
        /// </summary>
        private int WritenOffLabelValue => PalletGrid.Items?.Sum(x => x.CheckGet("SelectedFlag").ToBool() ? x["KOL"].ToInt() : 0) ?? 0;

        /// <summary>
        /// Количество выбранных поддонов, из которых будут скомплектованы новые 
        /// </summary>
        private int WritenOffPalletLabelValue => PalletGrid.Items?.Count(x => x.CheckGet("SelectedFlag").ToBool()) ?? 0;

        /// <summary>
        /// Количество товара на новых поддонах, которые создадутся в процессе комплектации
        /// </summary>
        private int WillBeReceivedLabelValue => GridTo.Items?.Sum(x => x["QTY"].ToInt()) ?? 0;

        /// <summary>
        /// Количество новых поддонов, которые будут созданны в процессе комплектации
        /// </summary>
        private int WillBeReceivedPalletLabelValue => GridTo.Items?.Count ?? 0;

        /// <summary>
        /// Право мастера. Флаг того, что у данного пользователя есть особые права 
        /// (Комплектации из воздуха, комплектацию большего количества товара, чем списывается)
        /// </summary>
        public bool MasterRights;

        /// <summary>
        /// id товара
        /// </summary>
        private int _id2Current;

        private int _recommendedAmountCurrent;

        /// <summary>
        /// признак наличия упаковки
        /// </summary>
        private bool _packing;

        /// <summary>
        /// максимальная высота транспортного пакета
        /// </summary>
        private int _htmax;


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
                            CompletedGrid.LoadItems();
                            break;
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
            var mode = Central.Navigator.GetRoleLevel(this.RoleName);
            var userAccessMode = mode;
            switch (mode)
            {
                case Role.AccessMode.Special:
                    MasterRights = true;
                    break;

                case Role.AccessMode.FullAccess:
                    MasterRights = false;
                    break;

                case Role.AccessMode.ReadOnly:
                default:
                    MasterRights = false;
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
            ProductSearchText.Text = "";
            BarCodeText.Text = "";

            PalletDataSet = new ListDataSet();
            CompletedDataSet = new ListDataSet();
            ProductDataSet = new ListDataSet();
            DataSetTo = new ListDataSet();

            SelectedCompletedItem = new Dictionary<string, string>();
            SelectedItemTo = new Dictionary<string, string>();

            PalletGrid.UpdateItems(PalletDataSet);
            ProductGrid.UpdateItems(ProductDataSet);
            CompletedGrid.UpdateItems(CompletedDataSet);
            GridTo.UpdateItems(DataSetTo);

            UpdateNewPalletButtons();

            ProductSearchText.Focus();
        }

        /// <summary>
        /// Вызов справки
        /// </summary>
        private void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp-new/production_new/complectation/sgp_complectation");
            //Central.ShowHelp("/doc/l-pack-erp/warehouse/komplektatsija-sgp");
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
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;

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

                case Key.Down:
                    var code = Central.WM.GetScannerInput();
                    if (!string.IsNullOrEmpty(code))
                    {
                        ProcessBarcode(code);
                    }
                    break;
            }
        }

        /// <summary>
        /// Отображение вкладки с формой редактирования данных
        /// </summary>
        private void Show()
        {
            var title = "Новая комплектация СГП КШ";
            Central.WM.AddTab(this.FrameName, title, true, "add", this);
        }

        public void ProcessBarcode(string code)
        {
            if (code.Length >= 13)
            {
                code = code.Substring(code.Length - 13);
                if (code.Contains("777"))
                {
                    if (ProductSearchText.Text.Contains(code))
                    {
                        ProductSearchText.Text = ProductSearchText.Text.Substring(0, ProductSearchText.Text.Length - 13);
                    }

                    BarCodeText.Text = code;
                    SearchByBarcode(code);
                }
            }
        }

        #endregion

        #region init grids

        private void InitProductGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "ИД",
                    Path = "ID2",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width = 55,
                },
                new DataGridHelperColumn
                {
                    Header = "Наименование",
                    Path = "NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 270,
                    MaxWidth = 350,
                },
                new DataGridHelperColumn
                {
                    Header = "Артикул",
                    Path = "ARTIKUL",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 120,
                    MaxWidth = 120,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество, шт.",
                    Path = "KOL_PAK",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 50,
                    MaxWidth = 120,
                },

                new DataGridHelperColumn
                {
                    Header = "Количество стоп на поддоне",
                    Path = "QUANTITY_REAM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 50,
                    MaxWidth = 50,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header = "Толщина картона",
                    Path = "THIKNES",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    Format = "N2",
                    MinWidth = 50,
                    MaxWidth = 50,
                    Hidden = true,
                },
            };
            if (!MasterRights)
            {
                columns.Add(new DataGridHelperColumn
                {
                    Header = "Поддонов, шт.",
                    Path = "PALLET_COUNT",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 50,
                    MaxWidth = 120,
                }
                );
            }
            columns.Add(new DataGridHelperColumn
            {
                Header = " ",
                Path = "_",
                ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                MinWidth = 5,
                MaxWidth = 2000,
            });
            ProductGrid.SetColumns(columns);
            ProductGrid.SetSorting("NAME");
            ProductGrid.AutoUpdateInterval = 0;
            ProductGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;

            ProductGrid.Menu = new Dictionary<string, DataGridContextMenuItem>();

            ProductGrid.OnSelectItem = selectedItem =>
            {
                if (selectedItem != null)
                {
                    SetProductParameters(selectedItem);
                    LoadPalletItems();

                    UpdateNewPalletButtons();
                }
            };

            ProductGrid.OnLoadItems = LoadProductItems;
            ProductGrid.PrimaryKey = "KOD";

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
                    Header = "#",
                    Path = "_ROWNUMBER",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width = 35,
                    MaxWidth = 35,
                },
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
                    Header = "ИД поддона",
                    Path = "ID",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 50,
                    MaxWidth = 70,
                },
                new DataGridHelperColumn
                {
                    Header = "Номер ПЗ",
                    Path = "ZNUM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 50,
                    MaxWidth = 70,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество, шт.",
                    Path = "KOL",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 40,
                    MaxWidth = 80,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество по умолчанию, шт.",
                    Path = "KOL_PAK",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 40,
                    MaxWidth = 150,
                },
                new DataGridHelperColumn
                {
                    Header = "Поддон",
                    Path = "NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 120,
                    MaxWidth = 250,
                },
                new DataGridHelperColumn
                {
                    Header = "Артикул",
                    Path = "ARTIKUL",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 50,
                    MaxWidth = 120,
                },
                new DataGridHelperColumn
                {
                    Header = "Склад",
                    Path = "SKLAD",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 30,
                    MaxWidth = 60,
                },
                new DataGridHelperColumn
                {
                    Header = "Ячейка",
                    Path = "NUM_PLACE",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 30,
                    MaxWidth = 60,
                },
                new DataGridHelperColumn
                {
                    Header = "Заявка",
                    Path = "ORDER_NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 80,
                    MaxWidth = 180,
                },

                new DataGridHelperColumn
                {
                    Header = "Количество стоп на поддоне",
                    Path = "QUANTITY_REAM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 50,
                    MaxWidth = 50,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header = "Толщина картона",
                    Path = "THIKNES",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    Format = "N2",
                    MinWidth = 50,
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
            PalletGrid.SetSorting("ZNUM");

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
                    Header = "ИД ПЗ",
                    Path = "ID_PZ",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 40,
                    MaxWidth = 55,
                    Hidden = true,
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
            };
            CompletedGrid.SetColumns(columns);

            CompletedGrid.AutoUpdateInterval = 0;

            CompletedGrid.OnSelectItem = selectedItem =>
            {
                SelectedCompletedItem = selectedItem;

                UpdateNewPalletButtons();
            };

            CompletedGrid.OnLoadItems = LoadCompletedItems;

            CompletedGrid.Init();
            CompletedGrid.Run();
        }

        private void InitGridTo()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "№ поддона",
                    Path = "PODDON_NUMBER",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 80,
                    MaxWidth = 80,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество, шт.",
                    Path = "QTY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 80,
                    MaxWidth = 80,
                },

                new DataGridHelperColumn
                {
                    Header=" ",
                    Path="_",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=5,
                    MaxWidth=2000,
                },
            };

            GridTo.SetColumns(columns);
            GridTo.SetSorting("PODDON_NUMBER");

            GridTo.OnSelectItem = selectedItem => { SelectedItemTo = selectedItem; };

            GridTo.OnDblClick = selectedItem =>
            {
                if (Central.Navigator.GetRoleLevel(this.RoleName) > Role.AccessMode.ReadOnly)
                {
                    EditButton_Click(null, null);
                }
            };

            GridTo.Init();
            GridTo.Run();
        }

        #endregion

        #region load data

        private async void LoadPalletItems()
        {
            PalletGrid.ShowSplash();

            PalletGrid.ClearItems();

            var p = new Dictionary<string, string>
            {
                ["PRODUCT_ID"] = _id2Current.ToString(),
                ["FACTORY_ID"] = $"{FactoryId}"
            };

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Complectation");
            q.Request.SetParam("Object", "Pallet");
            q.Request.SetParam("Action", "ListStock");
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
                    PalletDataSet = ListDataSet.Create(result, "List");
                    PalletGrid.UpdateItems(PalletDataSet);
                }
            }

            PalletGrid.HideSplash();
        }

        private async void LoadProductItems()
        {
            ProductGridToolbar.IsEnabled = false;
            ProductGrid.ShowSplash();

            ProductGrid.ClearItems();

            BarCodeText.Clear();
            PalletGrid.ClearItems();

            var searchStr = ProductSearchText.Text.Trim();
            if (searchStr.Length >= 3)
            {
                var p = new Dictionary<string, string>
                {
                    ["searchString"] = searchStr,

                    // Если _masterRights=true, то в списке товаров будут присутствовать все возможные товары (в том числе со *)
                    ["onlyExistInStock"] = (!MasterRights).ToString(),

                    ["FACTORY_ID"] = $"{FactoryId}"
                };

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Complectation");
                q.Request.SetParam("Object", "Product");
                q.Request.SetParam("Action", "ListStock");

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
                        ProductDataSet = ListDataSet.Create(result, "List");
                        ProductGrid.UpdateItems(ProductDataSet);
                    }
                }
            }

            ProductGrid.HideSplash();
            ProductGridToolbar.IsEnabled = true;
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
            q.Request.SetParam("Action", "ListStockKsh");

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
        /// обновляем доступность кнопок новых поддонов в зависимости от ситуации
        /// </summary>
        private void UpdateNewPalletButtons()
        {
            // количество выделенных паллет > 0
            var palletsSelected = PalletGrid.Items?.Count(row => row.ContainsKey("SelectedFlag") && row["SelectedFlag"].ToBool()) > 0;


            // вы может из чего-то скомплектовать (есть паллеты) или мы имеем права мастера
            var weHaveSomethingToPack = palletsSelected || (MasterRights && _id2Current != 0);

            NewPalletAddButton.IsEnabled = weHaveSomethingToPack;
            NewPalletEditButton.IsEnabled = weHaveSomethingToPack && SelectedItemTo != null && SelectedItemTo.Count > 0;
            NewPalletDeleteButton.IsEnabled = weHaveSomethingToPack && DataSetTo.Items.Count > 0;

            ComplectationButton.IsEnabled = palletsSelected || (MasterRights && _id2Current != 0 && DataSetTo.Items.Count > 0);
            if (DataSetTo != null)
            {
                ComplectationButton.Content = DataSetTo.Items.Count > 0 ? "Скомплектовать" : "Списать";
            }

            // количество товара
            WritenOffLabel.Content = WritenOffLabelValue;
            if (WritenOffLabel.Content.ToInt() > 0)
            {
                PalletGridLabels.Visibility = Visibility.Visible;
            }
            else
            {
                PalletGridLabels.Visibility = Visibility.Hidden;
            }

            WillBeReceivedLabel.Content = WillBeReceivedLabelValue;
            if (WillBeReceivedLabel.Content.ToInt() > 0)
            {
                GridToLabels.Visibility = Visibility.Visible;
            }
            else
            {
                GridToLabels.Visibility = Visibility.Hidden;
            }

            WritenOffPalletLabel.Content = WritenOffPalletLabelValue;
            WillBeReceivedPalletLabel.Content = WillBeReceivedPalletLabelValue;

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
        /// Установка настроек для принтера
        /// </summary>
        public void SetPrintSettings()
        {
            LabelReport2.SetPrintingProfile();
        }

        private async void SearchByBarcode(string code = "")
        {
            var str = code != "" ? code : BarCodeText.Text.Trim();
            // если штрихкод нужного формата
            if (str.Length == 13 && int.TryParse(str.Substring(3, 9), out var barcode))
            {
                var p = new Dictionary<string, string>
                {
                    ["id_poddon"] = barcode.ToString()
                };

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Complectation");
                q.Request.SetParam("Object", "Pallet");
                q.Request.SetParam("Action", "ListStockByIdPallet");

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
                        var dataSet = ListDataSet.Create(result, "List");

                        // мы нашли поддон надо настроить номенклатуру еще
                        // и поддон может добавить если номенклатура совпадает
                        if (dataSet.Items.Count == 1)
                        {
                            var item = dataSet.Items[0];

                            if (_id2Current != 0 && _id2Current != item["ID2"].ToInt())
                            {
                                DialogWindow.ShowDialog("Сделана попытка добавить поддон с разной продукцией. Нельзя комплектовать из разной продукции");
                                _id2Current = 0;

                                PalletGrid.ClearItems();
                                PalletDataSet.Items.Clear();
                            }
                            ProductGrid.ClearItems();
                            ProductSearchText.Text = "";


                            if (PalletDataSet.Items.Count(x => x["ID"].ToInt() == barcode) > 0)
                            {
                                DialogWindow.ShowDialog("Поддон уже добавлен");
                            }
                            else
                            {
                                item["SelectedFlag"] = "1";

                                PalletDataSet.Items.Add(item);
                                PalletGrid.UpdateItems(PalletDataSet);

                                SetProductParameters(item);
                            }

                        }
                        else
                        {
                            DialogWindow.ShowDialog("Поддон не найден!");
                        }
                    }
                    else
                    {
                        DialogWindow.ShowDialog($"На сервере произошла ошибка. {Environment.NewLine}Пожалуйста сообщите о проблеме.");
                    }
                }
                else
                {
                    q.ProcessError();
                }

                BarCodeText.Clear();
            }
            else
            {
                DialogWindow.ShowDialog("Штрихкод неправильного формата. Он должен быть длинной 13 цифр");

            }

            UpdateNewPalletButtons();
        }

        /// <summary>
        /// Добавляем поддон в грид новых поддонов (справа снизу)
        /// </summary>
        public void AddPallet()
        {
            var numberEditView = new ComplectationNumberEdit();
            numberEditView.Show();
            if (numberEditView.OkFlag)
            {
                var isOk = true;

                // если пытаются разместить на поддоне больше рекомендуемого товара
                if (_recommendedAmountCurrent < numberEditView.Value)
                {
                    // если поддоны с упаковкой и максимальная высота транспортного пакета < 2400
                    if (_packing && _htmax < 2400)
                    {
                        if (MasterRights)
                        {
                            isOk = DialogWindow.ShowDialog(
                                $"Вы мастер СГП. Внимание, выбранный поддон с упаковкий и его максимальная высота транспортного пакета < 2400!" +
                                $"{Environment.NewLine}Рекомендуется размещать не более {_recommendedAmountCurrent} товара на поддоне. Вы указали {numberEditView.Value}. Продолжить?",
                               "Предупреждение", "", DialogWindowButtons.NoYes) == true;
                        }
                        else
                        {
                            DialogWindow.ShowDialog(
                                $"Требуется размещать не более {_recommendedAmountCurrent} товара на поддоне. Вы указали {numberEditView.Value}. Добавление не выполнено!",
                                "Ошибка");
                            isOk = false;
                        }
                    }
                    else
                    {
                        isOk = DialogWindow.ShowDialog($"Рекомендуется размещать не более {_recommendedAmountCurrent} товара на поддоне. Вы указали {numberEditView.Value}. Продолжить?",
                                   "Вопрос", "", DialogWindowButtons.YesNo) == true;
                    }
                }

                if (isOk)
                {
                    var n = DataSetTo.Items.Count + 1;

                    var item = new Dictionary<string, string>
                    {
                        ["PODDON_NUMBER"] = n.ToString(),
                        ["QTY"] = numberEditView.Value.ToString()
                    };

                    DataSetTo.Items.Add(item);
                    GridTo.UpdateItems(DataSetTo);

                    UpdateNewPalletButtons();
                }
            }
        }

        /// <summary>
        /// Редактируем (количество на поддоне) выбранный поддон из грида новых поддонов (справа снизу)
        /// </summary>
        public void EditPallet()
        {
            var currentPoddonNumber = SelectedItemTo["PODDON_NUMBER"];
            var currentQty = SelectedItemTo["QTY"];

            var numberEditView = new ComplectationNumberEdit { Value = currentQty.ToInt() };
            numberEditView.Show();
            if (numberEditView.OkFlag)
            {
                SelectedItemTo["QTY"] = numberEditView.Value.ToString();
                GridTo.UpdateItems(DataSetTo);
                GridTo.SelectRowByKey(currentPoddonNumber.ToInt(), "PODDON_NUMBER");

                UpdateNewPalletButtons();
            }
        }

        /// <summary>
        /// Удаляем выбранный поддон из грида новых поддонов (справа снизу)
        /// </summary>
        public void DeletePallet()
        {
            DataSetTo.Items.Remove(SelectedItemTo);
            SelectedItemTo = null;

            var n = 1;
            foreach (var item in DataSetTo.Items)
            {
                item["PODDON_NUMBER"] = n.ToString();
                n++;
            }

            GridTo.UpdateItems(DataSetTo);

            UpdateNewPalletButtons();
        }

        private void SetProductParameters(Dictionary<string, string> item)
        {
            _id2Current = item.CheckGet("ID2").ToInt();
            _recommendedAmountCurrent = item.CheckGet("KOL_PAK").ToInt();
            _packing = item.CheckGet("PACKING").ToBool();
            _htmax = item.CheckGet("HT_MAX").ToInt();
        }

        /// <summary>
        /// Обновление данных грида скомплектованных позиций
        /// </summary>
        public void RefreshCompleted()
        {
            SelectedCompletedItem = new Dictionary<string, string>();
            CompletedGrid.LoadItems();
        }

        /// <summary>
        /// Комплектация на СГП
        /// </summary>
        public async void ComplectationStockVoid()
        {
            var isOk = true;

            List<Dictionary<string, string>> oldPalletList = PalletDataSet.Items.Where(x => x.CheckGet("SelectedFlag").ToInt() == 1).ToList();
            int countOldPallet = oldPalletList.Count;
            int sumOldPallet = oldPalletList.Sum(x => x.CheckGet("KOL").ToInt());

            List<Dictionary<string, string>> newPalletList = DataSetTo.Items;
            int countNewPallet = newPalletList.Count;
            int sumNewPallet = newPalletList.Sum(x => x.CheckGet("QTY").ToInt());

            var reasonId = "0";
            var reasonMessage = "";

            // проверяем что не пытаются сделать товара больше чем есть
            if (MasterRights)
            {
                if (sumOldPallet < sumNewPallet)
                {
                    isOk = DialogWindow.ShowDialog(
                               $"Вы мастер СГП.{Environment.NewLine}" +
                               $"Внимание, вы комплектуете товара больше чем было до комплектации.{Environment.NewLine}" +
                               $"Продолжить?",
                               "Предупреждение", "", DialogWindowButtons.NoYes) == true;
                }
                else if ((sumOldPallet * 0.1) + sumNewPallet < sumOldPallet)
                {
                    isOk = DialogWindow.ShowDialog(
                                $"Вы мастер СГП.{Environment.NewLine}" +
                                $"Внимание, вы списываете больше 10% от поддона.{Environment.NewLine}" +
                                $"Продолжить?",
                                "Предупреждение", "", DialogWindowButtons.NoYes) == true;
                }
            }
            else 
            {
                if (sumOldPallet < sumNewPallet)
                {
                    DialogWindow.ShowDialog(
                        "Нельзя скомплектовать больше товара чем было до комплектации. Уменьшите суммарное количество получаемого товара",
                        "Ошибка");
                    isOk = false;
                }
                else if ((sumOldPallet * 0.1) + sumNewPallet < sumOldPallet)
                {
                    DialogWindow.ShowDialog(
                        $"Нельзя списать больше 10% от поддона.{Environment.NewLine}",
                        "Ошибка");
                    isOk = false;
                }
            }

            // Отграничение на комплектацию одного поддона с тем же количеством, что было до комплектации
            // INFO Временно убрано ограничение на комплектацию одного поддона, пока идёт избавление от старых поддонов
            //if (isOk)
            //{
            //    if (countOldPallet == 1 && countNewPallet == 1)
            //    {
            //        if (sumOldPallet == sumNewPallet)
            //        {
            //            if (MasterRights)
            //            {
            //                isOk = DialogWindow.ShowDialog(
            //                   $"Вы мастер СГП.{Environment.NewLine}Внимание, вы комплектуете один поддон без изменения количества. Продолжить?",
            //                   "Предупреждение", "", DialogWindowButtons.NoYes) == true;
            //            }
            //            else
            //            {
            //                DialogWindow.ShowDialog(
            //                    $"Комплектация одного поддона без изменения количества запрещена.{Environment.NewLine}Измените количество поддонов или товара.",
            //                    "Ошибка");
            //                isOk = false;
            //            }
            //        }
            //    }
            //}

            // указываем причину
            if (isOk)
            {
                // Если есть новые поддоны, то указываем причину комплектации
                if (countNewPallet > 0)
                {
                    var сomplectationReasonsEdit = new ComplectationReasonsEdit();
                    сomplectationReasonsEdit.StockFlag = 1;
                    сomplectationReasonsEdit.Show();

                    if (сomplectationReasonsEdit.OkFlag)
                    {
                        reasonId = сomplectationReasonsEdit.SelectedReason.Key;
                        reasonMessage = сomplectationReasonsEdit.ReasonMessage;
                    }
                    else
                    {
                        isOk = false;
                    }
                }
                // Если нет, то указываем причину списания
                else
                {
                    var сomplectationWriteOffReasonsEdit = new ComplectationWriteOffReasonsEdit();
                    сomplectationWriteOffReasonsEdit.StockFlag = 1;
                    сomplectationWriteOffReasonsEdit.Show();

                    if (сomplectationWriteOffReasonsEdit.OkFlag)
                    {
                        reasonId = сomplectationWriteOffReasonsEdit.SelectedReason.Key;
                    }
                    else
                    {
                        isOk = false;
                    }
                }
            }

            // Запрашиваем финальное подтверждение
            if (isOk)
            {
                var message =
                    $"Будет списано {Environment.NewLine}" +
                    $"{WritenOffLabelValue} товара на {WritenOffPalletLabelValue} поддонах {Environment.NewLine}" +
                    $"Будет скомплектовано {Environment.NewLine}" +
                    $"{WillBeReceivedLabelValue} товара на {WillBeReceivedPalletLabelValue} поддонах. {Environment.NewLine}" +
                    $"Продолжить?";

                if (DialogWindow.ShowDialog(message, "Предупреждение", "", DialogWindowButtons.YesNo) != true)
                {
                    isOk = false;
                }
            }

            // Запрашиваем подтверждение высоты скомплектованного поддона
            if (isOk)
            {
                if (newPalletList != null && newPalletList.Count > 0)
                {
                    int quantityReam = 0;
                    double thiknes = 0;
                    int productKategoryId = 0;

                    if (oldPalletList != null && oldPalletList.Count > 0)
                    {
                        quantityReam = oldPalletList.First().CheckGet("QUANTITY_REAM").ToInt();
                        thiknes = oldPalletList.First().CheckGet("THIKNES").ToDouble();
                        productKategoryId = oldPalletList.First().CheckGet("IDK1").ToInt();
                    }
                    else if (ProductGrid.SelectedItem != null && ProductGrid.SelectedItem.Count > 0)
                    {
                        quantityReam = ProductGrid.SelectedItem.CheckGet("QUANTITY_REAM").ToInt();
                        thiknes = ProductGrid.SelectedItem.CheckGet("THIKNES").ToDouble();
                        productKategoryId = ProductGrid.SelectedItem.CheckGet("IDK1").ToInt();
                    }

                    if (productKategoryId == 5 && quantityReam > 0 && thiknes > 0)
                    {
                        var message =
                            $"Внимание! Проверьте высоту поддонов.{Environment.NewLine}" +
                            $"Толщина картона: {thiknes} мм.{Environment.NewLine}" +
                            $"Стоп на поддоне: {quantityReam}{Environment.NewLine}" +
                            $"-----{Environment.NewLine}";

                        foreach (var newPallet in newPalletList)
                        {
                            message = $"{message}" +
                                $"Номер поддона: {newPallet.CheckGet("PODDON_NUMBER").ToInt()}{Environment.NewLine}" +
                                $"Продукции на поддоне: {newPallet.CheckGet("QTY").ToInt()}{Environment.NewLine}" +
                                $"Высота продукции на поддоне: {Math.Round((newPallet.CheckGet("QTY").ToDouble() / (double)quantityReam) * thiknes).ToInt()} мм.{Environment.NewLine}" +
                                $"-----{Environment.NewLine}";
                        }
                        message = $"{message}Продолжить?";

                        if (DialogWindow.ShowDialog(message, "Предупреждение", "", DialogWindowButtons.NoYes) != true)
                        {
                            isOk = false;
                        }
                    }
                }
            }

            // Проверка заявок
            List<string> orderList = oldPalletList.Select(x => x.CheckGet("IDORDERDATES")).ToList();
            var view = new ComplectationStockParamsEdit();
            view.ProductId = _id2Current.ToString();
            view.FactoryId = this.FactoryId;
            view.OrderList = orderList;
            if (isOk)
            {
                view.Show();

                if (!view.OkFlag)
                {
                    isOk = false;
                }
            }

            if (isOk)
            {
                SplashControl.Visible = true;

                var p = new Dictionary<string, string>
                {
                    ["id2_current"] = _id2Current.ToString(),
                    ["OldPalletList"] = JsonConvert.SerializeObject(oldPalletList),
                    ["NewPalletList"] = JsonConvert.SerializeObject(newPalletList),
                    ["idorderdates"] = view.SelectedValue?["IDORDERDATES"],
                    ["ReasonId"] = reasonId,
                    ["ReasonMessage"] = reasonMessage
                };

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Complectation");
                q.Request.SetParam("Object", "Pallet");
                q.Request.SetParam("Action", "CreateStockKsh");

                q.Request.SetParams(p);

                await Task.Run(() => { q.DoQuery(); });


                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds.Items.Count > 0)
                        {
                            var idpz = ds.Items.First().CheckGet("idpz");
                            var idk1 = ds.Items.First().CheckGet("IDK1");

                            if (WillBeReceivedLabelValue > 0 || idpz != "0")
                            {
                                // печать ярлыков
                                for (var i = 1; i <= countNewPallet; i++)
                                {
                                    LabelReport2 report = new LabelReport2(true);
                                    report.PrintLabel(idpz, i.ToString(), idk1);
                                }

                                // отправить сообщение списку комплектаций обновиться текущей датой
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "ComplectationKsh",
                                    ReceiverName = "ComplectationListKsh",
                                    SenderName = this.FrameName,
                                    Action = "Refresh",
                                });
                            }
                            else
                            {
                                DialogWindow.ShowDialog("Списание выполнено");

                                Messenger.Default.Send(new ItemMessage
                                {
                                    ReceiverGroup = "ComplectationKsh",
                                    ReceiverName = "ComplectationWriteOffListKsh",
                                    SenderName = this.FrameName,
                                    Action = "Refresh",
                                });
                            }
                        }
                        else
                        {
                            var msg = "На сервере произошла ошибка. Пожалуйста сообщите о проблеме.";
                            var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                    else
                    {
                        var msg = "На сервере произошла ошибка. Пожалуйста сообщите о проблеме.";
                        var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }

                SetDefaults();

                CompletedGrid.LoadItems();
            }

            SplashControl.Visible = false;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchByBarcode();
        }

        private void ComplectationButton_Click(object sender, RoutedEventArgs e)
        {
            ComplectationStockVoid();
        }

        private void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            LoadProductItems();
        }

        private void ProductSearchText_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            ShowButton.IsEnabled = ProductSearchText.Text.Trim().Length >= 3;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddPallet();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            DeletePallet();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            EditPallet();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void ClearAll_OnClickButton_Click(object sender, RoutedEventArgs e)
        {
            SetDefaults();
        }

        private void ProductSearchText_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoadProductItems();
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings();
        }

        private void LabelPrintButton_Click(object sender, RoutedEventArgs e)
        {
            LabelPrint();
        }

        private void RefreshCompletedButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshCompleted();
        }
    }
}
