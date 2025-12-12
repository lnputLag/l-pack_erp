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
using Client.Interfaces.Stock;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Комплектация переработка
    /// </summary>
    /// <author>михеев</author> 
    public partial class ComplectationProcesing : UserControl
    {
        public ComplectationProcesing()
        {
            InitializeComponent();
            FrameName = "ProductionPMConversion";
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
            LoadCompletedItems();

            ProcessPermissions();
        }

        public int FactoryId = 1;

        /// <summary>
        /// Техническое имя таба
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// Датасет товаров, которые находятся в Ц0 для комплектации
        /// </summary>
        private ListDataSet ProductDataSet { get; set; }

        /// <summary>
        /// Выбранная запись в гриде товаров
        /// </summary>
        private Dictionary<string, string> SelectedProductItem { get; set; }

        /// <summary>
        /// Датасет поддонов, из которых комплектуются новые
        /// </summary>
        private ListDataSet PalletDataSet { get; set; }

        /// <summary>
        /// Выбранная запись в гриде поддонов, из которых комплектуются новые
        /// </summary>
        public Dictionary<string, string> SelectedPalletItem { get; set; }

        /// <summary>
        /// Датасет с данными по скомплектованным позициям
        /// </summary>
        public ListDataSet CompletedDS { get; set; }

        /// <summary>
        /// Выбранная запис в гриде скомплектованных позиций
        /// </summary>
        public Dictionary<string, string> SelectedCompletedItem { get; set; }

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
            CompletedDS = new ListDataSet();
            ProductDataSet = new ListDataSet();

            SelectedProductItem = new Dictionary<string, string>();
            SelectedPalletItem = new Dictionary<string, string>();
            SelectedCompletedItem = new Dictionary<string, string>();
        }

        private void InitProductGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
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
                    Header = "Наименование",
                    Path = "NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 220,
                    MaxWidth = 600,
                },
                new DataGridHelperColumn
                {
                    Header = "Всего на поддонах, шт.",
                    Path = "KOL_SUM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 50,
                    MaxWidth = 120,
                },
                new DataGridHelperColumn
                {
                    Header = "На поддоне по умолчанию, шт.",
                    Path = "KOL_PAK",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 50,
                    MaxWidth = 140,
                },
                new DataGridHelperColumn
                {
                    Header = "Поддонов, шт.",
                    Path = "PALLETS",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 50,
                    MaxWidth = 100,
                },
                new DataGridHelperColumn
                {
                    Header = "Закончить до",
                    Path = "FINISH_BEFORE_DTTM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.DateTime,
                    MinWidth = 110,
                    MaxWidth = 110,
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
            ProductGrid.AutoUpdateInterval = 30;            

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

            PalletGrid.OnSelectItem = selectedItem =>
            {
                SelectedPalletItem = selectedItem;
                UpdateNewPalletButtons();
            };

            PalletGrid.SelectItemMode = 2;
            PalletGrid.AutoUpdateInterval = 0;
            PalletGrid.Init();
        }

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
            };
            CompletedGrid.SetColumns(columns);

            CompletedGrid.OnSelectItem = selectedItem =>
            {
                SelectedCompletedItem = selectedItem;
                UpdateNewPalletButtons();
            };

            CompletedGrid.AutoUpdateInterval = 0;
            CompletedGrid.Init();
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
                    CompletedDS = ListDataSet.Create(result, "ITEMS");
                    CompletedGrid.UpdateItems(CompletedDS);
                }
            }
            else
            {
                q.ProcessError();
            }

            CompletedGrid.HideSplash();
            CompletedGridToolbar.IsEnabled = true;
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
            // Выбран поддон из которого будут комплектовать для обычной комплектации или списания
            ComplectationButton.IsEnabled = PalletGrid.Items?.Count(row => row.CheckGet("SelectedFlag").ToBool()) > 0;

            // количество товара
            WritenOffLabel.Content = PalletGrid.Items?.Sum(x => (x.ContainsKey("SelectedFlag") && x["SelectedFlag"].ToBool()) ? x["KOL"].ToInt() : 0) ?? 0;
            WritenOffPalletLabel.Content = PalletGrid.Items?.Count(x => x.CheckGet("SelectedFlag").ToBool()) ?? 0;

            if (SelectedProductItem != null)
            {
                TechnologicalMapButton.IsEnabled = true;
            }
            else
            {
                TechnologicalMapButton.IsEnabled = false;
            }

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
                        var d = new DialogWindow($"{msg}", "Комплектация Переработка", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    var msg = "Не найден путь к Excel файлу тех карты";
                    var d = new DialogWindow($"{msg}", "Комплектация Переработка", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                var msg = "Не выбран товар, для которого нужно найти тех карту";
                var d = new DialogWindow($"{msg}", "Комплектация Переработка", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
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
                    var d = new DialogWindow($"{msg}", "Комплектация Переработка", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                var msg = "Не выбрана комплектация";
                var d = new DialogWindow($"{msg}", "Комплектация Переработка", "", DialogWindowButtons.OK);
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

        /// <summary>
        /// Комплектация из воздуха
        /// </summary>
        public void AddProduct()
        {
            var i = new ComplectationPMProductList(true);
            i.FactoryId = this.FactoryId;
            i.Show();
        }

        /// <summary>
        /// Обновляет данные для всех гридов формы
        /// </summary>
        public void Refresh()
        {
            SetDefaults();

            LoadProductItems();
            LoadCompletedItems();
            UpdateNewPalletButtons();
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m">Сообщение</param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.Contains("Complectation"))
            {
                if (m.ReceiverName.Contains("PM"))
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            SetDefaults();
                            LoadProductItems();
                            LoadCompletedItems();
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
            var mode = Central.Navigator.GetRoleLevel("[erp]complectation_pm");
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
        /// Вызов справки
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp-new/production_new/complectation/pererab_complectation");
        }

        /// <summary>
        /// Деструктор компонентов. Завершает вспомогательные процессы
        /// </summary>
        private void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage
            {
                ReceiverGroup = "Complecation",
                ReceiverName = "",
                SenderName = "ComplectationConversionEdit",
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
                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;
            }
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
    }
}
