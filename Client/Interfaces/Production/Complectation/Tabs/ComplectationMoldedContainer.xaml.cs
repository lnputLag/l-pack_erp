using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Printing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Stock;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Комплектация Литой тары
    /// </summary>
    public partial class ComplectationMoldedContainer : UserControl
    {
        public ComplectationMoldedContainer()
        {
            InitializeComponent();
            RoleName = "[erp]compl_molded_contnr";
            FrameName = "ProductionComplectationMoldedContainer";
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

        public string RoleName { get; set; }

        /// <summary>
        /// Техническое имя таба
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// Выбранная запись в гриде товаров, которые находятся в К0 для комплектации
        /// </summary>
        private Dictionary<string, string> SelectedProductItem { get; set; }

        /// <summary>
        /// Датасет товаров, которые находятся в К0 для комплектации
        /// </summary>
        private ListDataSet ProductDataSet { get; set; }

        /// <summary>
        /// Выбранная запись в гриде поддонов, из которых будут комплектоваться новые
        /// </summary>
        public Dictionary<string, string> SelectedPalletItem { get; set; }

        /// <summary>
        /// Датасет поддонов, из которых будут комплектоваться новые
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

        #region default functions

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m">Сообщение</param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.Contains("Complectation"))
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
            ProductDataSet = new ListDataSet();
            PalletDataSet = new ListDataSet();
            CompletedDataSet = new ListDataSet();

            SelectedCompletedItem = new Dictionary<string, string>();

            PalletGrid.UpdateItems(PalletDataSet);
            ProductGrid.UpdateItems(ProductDataSet);
            CompletedGrid.UpdateItems(CompletedDataSet);
        }

        /// <summary>
        /// Вызов справки
        /// </summary>
        private void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp-new/production_new/complectation/complectation_lt");
        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        private void Close()
        {
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
                ReceiverGroup = "Complectation",
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
            var title = "Новая комплектация на ЛТ";
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
                    Path = "SKLAD",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header = "№ ячейки",
                    Path = "NUM_PLACE",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header = "Наименование",
                    Path = "NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn
                {
                    Header = $"Всего на поддонах, шт.{Environment.NewLine}Суммарное количество продукции на поддонах в ячейке по этой заявке",
                    Path = "KOL_SUM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header = $"На полном, шт.{Environment.NewLine}Количество продукции на полном поддоне по умолчанию",
                    Path = "KOL_PAK",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header = $"Поддонов, шт.{Environment.NewLine}Количество поддонов в ячейке по этой заявке",
                    Path = "PALLETS",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=5,
                },

                new DataGridHelperColumn
                {
                    Header = "Артикул",
                    Path = "ARTIKUL",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=5,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header = "Ид продукции",
                    Path = "ID2",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=5,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header = "Ид категории продукции",
                    Path = "IDK1",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=5,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header = "Путь к техкарте",
                    Path = "PATHTK",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=5,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header = "Ид заявки",
                    Path = "IDORDERDATES",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=5,
                    Hidden=true,
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

            ProductGrid.AutoUpdateInterval = 60 * 5;
            ProductGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            ProductGrid.SetColumns(columns);
            ProductGrid.SetSorting("NAME");

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

            ProductGrid.Init();
            ProductGrid.Run();

            ProductGrid.UpdateItems(ProductDataSet);
        }

        /// <summary>
        /// Инициализация грида списываемых поддонов
        /// </summary>
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
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество на поддоне, шт.",
                    Path = "KOL",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header = "Причина забраковки",
                    Path = "DESCRIPTION",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=17,
                },
                new DataGridHelperColumn
                {
                    Header = "Ячейка",
                    Path = "SKLAD",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header = "№ ячейки",
                    Path = "NUM_PLACE",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=9,
                },

                new DataGridHelperColumn
                {
                    Header = "Ид продукции",
                    Path = "ID2",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 45,
                    MaxWidth = 45,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header = "Ид прихода",
                    Path = "IDP",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 45,
                    MaxWidth = 45,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header = "Номер поддона",
                    Path = "NUM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 45,
                    MaxWidth = 45,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header = "Заявка",
                    Path = "IDORDERDATES",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 45,
                    MaxWidth = 45,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header = "Отдел",
                    Path = "ID1",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 45,
                    MaxWidth = 45,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header = "Ид производственного задания",
                    Path = "ID_PZ",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 45,
                    MaxWidth = 45,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header = "Ид категории продукции",
                    Path = "IDK1",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 45,
                    MaxWidth = 45,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header = "Ид поддона",
                    Path = "PALLET_ID",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 45,
                    MaxWidth = 45,
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
            PalletGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            PalletGrid.AutoUpdateInterval = 0;

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

            PalletGrid.Init();
            PalletGrid.Run();
            PalletGrid.Focus();
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

        private async void LoadPalletItems()
        {
            PalletGrid.ShowSplash();

            PalletGrid.ClearItems();

            if (SelectedProductItem != null)
            {
                var p = new Dictionary<string, string>();
                p.CheckAdd("ID2", SelectedProductItem.CheckGet("ID2"));
                p.CheckAdd("IDORDERDATES", SelectedProductItem.CheckGet("IDORDERDATES"));
                p.CheckAdd("SKLAD", SelectedProductItem.CheckGet("SKLAD"));
                p.CheckAdd("NUM_PLACE", SelectedProductItem.CheckGet("NUM_PLACE"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Complectation");
                q.Request.SetParam("Object", "Pallet");
                q.Request.SetParam("Action", "ListMoldedContainer");

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

                        foreach (var item in PalletDataSet.Items)
                        {
                            item.Add("SelectedFlag", 0.ToString());
                        }
                        
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

            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Complectation");
            q.Request.SetParam("Object", "Product");
            q.Request.SetParam("Action", "ListMoldedContainer");

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
            q.Request.SetParam("Action", "ListMoldedContainer");

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
        /// Комплактация на ГА
        /// </summary>
        public async void Complectation()
        {
            var oldPalletList = PalletDataSet.Items.Where(x => x.CheckGet("SelectedFlag").ToInt() == 1).ToList();
            var tab = new ComplectationMainComplectationTab(FrameName, SelectedProductItem, oldPalletList);
            tab.Show();
        }

        /// <summary>
        /// Делает неактивными все тулбары вкладки
        /// </summary>
        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
            CompletedGridToolbar.IsEnabled = false;
        }

        /// <summary>
        /// Делает активными все тулбары вкладки
        /// Вызывает метод установки активности кнопок
        /// </summary>
        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
            CompletedGridToolbar.IsEnabled = true;

            UpdateNewPalletButtons();
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
                    var d = new DialogWindow($"{msg}", "Комплектация ЛТ", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                var msg = "Не выбрана комплектация";
                var d = new DialogWindow($"{msg}", "Комплектация ЛТ", "", DialogWindowButtons.OK);
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
                        var d = new DialogWindow($"{msg}", "Комплектация ЛТ", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    var msg = "Не найден путь к Excel файлу тех карты";
                    var d = new DialogWindow($"{msg}", "Комплектация ЛТ", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                var msg = "Не выбран товар, для которого нужно найти тех карту";
                var d = new DialogWindow($"{msg}", "Комплектация ЛТ", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        private void ComplectationButton_Click(object sender, RoutedEventArgs e)
        {
            Complectation();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void LabelPrintButton_Click(object sender, RoutedEventArgs e)
        {
            LabelPrint();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings();
        }

        private void TechnologicalMapButton_Click(object sender, RoutedEventArgs e)
        {
            OpenTechnologicalMap();
        }
    }
}
