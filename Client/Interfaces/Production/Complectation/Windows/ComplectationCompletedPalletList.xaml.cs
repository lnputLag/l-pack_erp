using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Stock;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
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

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Отображает данные по поддонам, которые получились в ходе выбранной комплектации
    /// </summary>
    public partial class ComplectationCompletedPalletList : UserControl
    {
        public ComplectationCompletedPalletList(int idPz = 0, int idSt = 0, int idk1 = 0, int consumptionId = 0, int incomingQuantity = -1)
        {
            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            InitializeComponent();

            FrameName = "ComplectationCompletedPalletList";

            IdPz = idPz;

            IdSt = idSt;

            IDK1 = idk1;

            ConsumptionId = consumptionId;

            IncomingQuantity = incomingQuantity;

            InitCompletedPalletGrid();

            SetDefaults();
        }

        /// <summary>
        /// Выбранная запись в гриде скомплектованных поддонов
        /// </summary>
        public Dictionary<string, string> SelectedCompletedPalletItem { get; set; }

        /// <summary>
        /// Датасет с данными по товарам
        /// </summary>
        public ListDataSet CompletedPalletDataSet { get; set; }

        public string FrameName { get; set; }

        /// <summary>
        /// Идентификатор производственного задания
        /// </summary>
        public int IdPz { get; set; }

        /// <summary>
        /// Идентификатор станка
        /// </summary>
        public int IdSt { get; set; }

        /// <summary>
        /// Идентификатор категории товара
        /// </summary>
        public int IDK1 { get; set; }

        /// <summary>
        /// Идентификатор расхода
        /// </summary>
        public int ConsumptionId { get; set; }

        /// <summary>
        /// Количество продукции в приходе
        /// </summary>
        public int IncomingQuantity { get; set; }

        /// <summary>
        /// Инициализация грида с поддонами, которые получились в ходе выбранной комплектации
        /// </summary>
        public void InitCompletedPalletGrid()
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
                        var c = (CheckBox)el;
                        if (row.CheckGet("QTY").ToInt() == 0)
                        {
                            c.IsChecked = false;
                        }

                        UpdateButtons();
                        return null;
                    },
                },
                new DataGridHelperColumn
                {
                    Header = "№ поддона",
                    Path = "NUM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 81,
                    MaxWidth = 81,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество, шт.",
                    Path = "QTY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 81,
                    MaxWidth = 105,
                },
                new DataGridHelperColumn
                {
                    Header = $"Высота, мм.{Environment.NewLine}Чистая высота стопы продукции, без учёта высоты самого поддона",
                    Path = "PALLET_HEIGTH",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 56,
                    MaxWidth = 80,
                },

                new DataGridHelperColumn
                {
                    Header = "Количество стоп на поддоне",
                    Path = "QUANTITY_REAM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 55,
                    MaxWidth = 55,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header = "Толщина картона",
                    Path = "THIKNES",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    Format = "N2",
                    MinWidth = 55,
                    MaxWidth = 55,
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

            CompletedPalletGrid.SetColumns(columns);

            CompletedPalletGrid.OnLoadItems = LoadCompletedPalletItems;

            CompletedPalletGrid.OnSelectItem = selectedItem =>
            {
                if (selectedItem != null)
                {
                    SelectedCompletedPalletItem = selectedItem;
                }
            };

            CompletedPalletGrid.AutoUpdateInterval = 0;
            CompletedPalletGrid.Init();
            CompletedPalletGrid.Run();
        }

        public async void LoadCompletedPalletItems()
        {
            CompletedPalletToolbar.IsEnabled = false;

            HeaderToolbar.IsEnabled = false;

            CompletedPalletGrid.ShowSplash();

            if (IdPz > 0)
            {
                var p = new Dictionary<string, string>();
                var q = new LPackClientQuery();

                if (ConsumptionId > 0 && IncomingQuantity == 0)
                {
                    p.Add("ID_PZ", IdPz.ToString());
                    p.Add("IDR", ConsumptionId.ToString());

                    q.Request.SetParam("Module", "Complectation");
                    q.Request.SetParam("Object", "Operation");
                    q.Request.SetParam("Action", "GetCompletedConsumptionPalletList");
                }
                else
                {
                    p.Add("ID_PZ", IdPz.ToString());

                    q.Request.SetParam("Module", "Complectation");
                    q.Request.SetParam("Object", "Operation");
                    q.Request.SetParam("Action", "GetCompletedIncomingPalletList");
                }

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
                        CompletedPalletDataSet = ListDataSet.Create(result, "ITEMS");

                        if (CompletedPalletDataSet != null && CompletedPalletDataSet.Items != null && CompletedPalletDataSet.Items.Count > 0)
                        {
                            foreach (var item in CompletedPalletDataSet.Items)
                            {
                                if ((IDK1 == 5 || IDK1 == 4) && item.CheckGet("QUANTITY_REAM").ToInt() > 0 && item.CheckGet("THIKNES").ToDouble() > 0)
                                {
                                    item.CheckAdd("PALLET_HEIGTH", $"{Math.Round((item.CheckGet("QTY").ToDouble() / item.CheckGet("QUANTITY_REAM").ToDouble()) * item.CheckGet("THIKNES").ToDouble())}");
                                }
                            }
                        }

                        CompletedPalletGrid.UpdateItems(CompletedPalletDataSet);
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
            else
            {
                var msg = "Не найден идентификатор производственного задания";
                var d = new DialogWindow($"{msg}", "Комплектация Переработка", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }

            CompletedPalletGrid.HideSplash();

            CompletedPalletToolbar.IsEnabled = true;

            HeaderToolbar.IsEnabled = true;
        }

        public void UpdateButtons()
        {
            var selectedRows = 0;

            if (CompletedPalletGrid.Items != null)
            {
                if (CompletedPalletGrid.Items.Count > 0)
                {
                    selectedRows = CompletedPalletGrid.Items.Count(x => x.CheckGet("SelectedFlag").ToBool());
                }
            }

            if (selectedRows > 0)
            {
                LabelPrintButton.IsEnabled = true;
            }
            else
            {
                LabelPrintButton.IsEnabled = false;
            }
        }

        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Show()
        {
            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 2;

            Dictionary<string, string> windowParametrs = new Dictionary<string, string>();
            windowParametrs.Add("no_resize", "1");
            windowParametrs.Add("center_screen", "1");
            Central.WM.Show(FrameName, "Скомплектованные поддоны", true, "add", this, "top", windowParametrs);
        }

        public void SetDefaults()
        {
            CompletedPalletDataSet = new ListDataSet();

            if (CompletedPalletGrid.Items != null)
            {
                CompletedPalletGrid.Items.Clear();

                CompletedPalletGrid.UpdateItems(CompletedPalletDataSet);
            }
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m">Сообщение</param>
        private void ProcessMessages(ItemMessage m)
        {

        }

        public void LabelPrint()
        {
            if (CompletedPalletGrid.Items != null)
            {
                if (CompletedPalletGrid.Items.Count > 0)
                {
                    if (IdPz > 0 && IdSt > 0 && IDK1 > 0)
                    {
                        foreach (var item in CompletedPalletGrid.Items)
                        {
                            if (item.CheckGet("SelectedFlag").ToBool())
                            {
                                LabelReport2 report = new LabelReport2(true);
                                string palletNumber = item.CheckGet("PALLET_NUMBER");
                                if (string.IsNullOrEmpty(palletNumber))
                                {
                                    palletNumber = item.CheckGet("PODDON_NUM");
                                }
                                report.PrintLabel(IdPz.ToString(), palletNumber, IDK1.ToString(), item.CheckGet("IDP").ToInt());
                            }
                        }
                    }
                    else
                    {
                        var msg = "Не найден идентификатор производственного задания, станка или категории товара";
                        var d = new DialogWindow($"{msg}", "Комплектация Переработка", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
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
        /// Вызов справки
        /// </summary>
        public void ShowHelp()
        {
            //Central.ShowHelp("/doc/l-pack-erp/production/pm_complectation");
        }

        private void LabelPrintButton_Click(object sender, RoutedEventArgs e)
        {
            LabelPrint();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }
    }
}
