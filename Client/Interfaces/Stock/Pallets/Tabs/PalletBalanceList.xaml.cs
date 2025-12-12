using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Вкладка баланса поддонов
    /// </summary>
    public partial class PalletBalanceList : UserControl
    {
        public PalletBalanceList()
        {
            InitializeComponent();

            FactIdSelectBox.SelectedItemChanged += (DependencyObject d, DependencyPropertyChangedEventArgs e) => {
                GridBalance.LoadItems();
            };

            //регистрация обработчика клавиатуры
            PreviewKeyDown += ProcessKeyboard;

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            SetDefaults();
            PalletBalanceDS = new ListDataSet();
            PalletBalanceDS.Init();
            ReceiptDS = new ListDataSet();
            ReceiptDS.Init();
            ExpenditureDS = new ListDataSet();
            ExpenditureDS.Init();

            InitGridBalance();
            InitGridReceipt();
            InitGridExpenditure();

            ProcessPermissions();
        }

        /// <summary>
        /// Данные для таблицы баланса прихода и расхода поддонов
        /// </summary>
        ListDataSet PalletBalanceDS { get; set; }

        /// <summary>
        /// Данные для таблицы прихода выбранного поддона
        /// </summary>
        ListDataSet ReceiptDS { get; set; }

        /// <summary>
        /// Данные для таблицы расхода выбранного поддона
        /// </summary>
        ListDataSet ExpenditureDS { get; set; }

        /// <summary>
        /// Выбранная строка в таблице баланса прихода и расхода поддонов
        /// </summary>
        Dictionary<string, string> SelectedPalletItem { get; set; }

        /// <summary>
        /// обработчик системы навигации по URL
        /// </summary>
        public void ProcessNavigation()
        {

        }

        /// <summary>
        /// Обработчики нажатий клавиш
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessKeyboard(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F5:
                    GridBalance.LoadItems();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

                case Key.Home:
                    GridBalance.SetSelectToFirstRow();
                    e.Handled = true;
                    break;

                case Key.End:
                    GridBalance.SetSelectToLastRow();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// Деструктор. Остановка вспомогательных процессов при закрытии вкладки
        /// </summary>
        public void Destroy()
        {
            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры гридов
            GridBalance.Destruct();
            GridReceipt.Destruct();
            GridExpenditure.Destruct();
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            FactIdSelectBox.Items = new Dictionary<string, string>() {
                { "1", "Л-ПАК ЛИПЕЦК" },
                { "2", "Л-ПАК КАШИРА" },
            };
            FactIdSelectBox.SelectedItem = FactIdSelectBox.Items.First();
            DateFrom.Text = DateTime.Now.AddDays(-7).ToString("dd.MM.yyyy");
            DateTo.Text = DateTime.Now.ToString("dd.MM.yyyy");
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="obj">сообщение</param>
        private void ProcessMessages(ItemMessage obj)
        {
            if (obj.ReceiverGroup.IndexOf("Stock") > -1)
            {
                if (obj.ReceiverName.IndexOf("PalletBalanceList") > -1)
                {
                    switch (obj.Action)
                    {
                        case "Refresh":
                            GridBalance.LoadItems();
                            break;
                    }
                }
                switch (obj.Action)
                {
                    case "PalletListRefresh":
                        GridBalance.LoadItems();
                        break;
                }
            }
        }

        /// <summary>
        /// Инициализация таблицы баланса
        /// </summary>
        private void InitGridBalance()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Поддон",
                    Path="NAME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=150,
                    MaxWidth=250,
                },
                new DataGridHelperColumn
                {
                    Header="Сальдо, начало периода",
                    Path="SALDO_BEGIN",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=60,
                    MaxWidth=160,
                },
                new DataGridHelperColumn
                {
                    Header="Приход, шт",
                    Path="QTY_PD",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=85,
                },
                new DataGridHelperColumn
                {
                    Header="Расход, шт",
                    Path="QTY_PC",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=85,
                },
                new DataGridHelperColumn
                {
                    Header="Сальдо, конец периода",
                    Path="SALDO_END",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=55,
                    MaxWidth=160,
                },
                new DataGridHelperColumn
                {
                    Header=" ",
                    Path="_",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=55,
                    MaxWidth=1500,
                },
                new DataGridHelperColumn
                {
                    Header="Ид поддона",
                    Path="ID_PAL",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            GridBalance.SetColumns(columns);
            GridBalance.SetSorting("_ROWNNMBER", ListSortDirection.Ascending);
            GridBalance.SearchText = SearchText;
            GridBalance.Init();

            // контекстное меню
            GridBalance.Menu = new Dictionary<string, DataGridContextMenuItem>()
            {
                {
                    "SetBalance",
                    new DataGridContextMenuItem()
                    {
                        Header="Выравнивание остатков",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                            SetBalance();
                        }
                    }
                },

            };

            GridBalance.OnLoadItems = LoadItemsBalance;
            GridBalance.Run();

            //при выборе строки в гриде, обновляются актуальные действия для записи
            GridBalance.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateActionsBalance(selectedItem);
                }
            };

            //двойной клик на строке откроет форму редактирования
            GridBalance.OnDblClick = (Dictionary<string, string> selectedItem) =>
            {
                SetBalance();
            };

        }

        /// <summary>
        /// Инициализация таблицы прихода поддонов
        /// </summary>
        private void InitGridReceipt()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Дата создания",
                    Path="CREATED_DTTM",
                    Format="dd.MM.yyyy",
                    ColumnType=ColumnTypeRef.DateTime,
                    MinWidth=62,
                    MaxWidth=62,
                },
                new DataGridHelperColumn
                {
                    Header="Источник",
                    Path="SOURCE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=120,
                },
                new DataGridHelperColumn
                {
                    Header="Накладная",
                    Path="RECEIPT",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=80,
                    MaxWidth=900,
                },
                new DataGridHelperColumn
                {
                    Header="Количество",
                    Path="QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=45,
                    MaxWidth=45,
                    Totals=(List<Dictionary<string,string>> rows) =>
                    {
                        var result=0;
                        if(rows != null)
                        {
                            foreach(Dictionary<string,string> row in rows)
                            {
                                result += row.CheckGet("QTY").ToInt();
                            }
                        }
                        return result;
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Примечание",
                    Path="NOTE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=70,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header="Код источника",
                    Path="SOURCE_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            GridReceipt.SetColumns(columns);
            GridReceipt.SearchText = SearchReceipt;
            GridReceipt.Init();
        }

        /// <summary>
        /// Инициализация таблицы расхода поддонов
        /// </summary>
        private void InitGridExpenditure()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Дата создания",
                    Path="CREATED_DTTM",
                    Format="dd.MM.yyyy",
                    ColumnType=ColumnTypeRef.DateTime,
                    MinWidth=62,
                    MaxWidth=62,
                },
                new DataGridHelperColumn
                {
                    Header="Тип",
                    Path="PLEX_TYPE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=120,
                },
                new DataGridHelperColumn
                {
                    Header="Накладная",
                    Path="INVOICE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=80,
                    MaxWidth=900,
                },
                new DataGridHelperColumn
                {
                    Header="Отгрузка",
                    Path="SHIPMENT",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=80,
                    MaxWidth=900,
                },
                new DataGridHelperColumn
                {
                    Header="Количество",
                    Path="QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=30,
                    Totals=(List<Dictionary<string,string>> rows) =>
                    {
                        var result=0;
                        if(rows != null)
                        {
                            foreach(Dictionary<string,string> row in rows)
                            {
                                result += row.CheckGet("QTY").ToInt();
                            }
                        }
                        return result;
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Примечание",
                    Path="NOTE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=70,
                    MaxWidth=150,
                },
            };
            GridExpenditure.SetColumns(columns);
            GridExpenditure.SearchText = SearchExpenditure;
            GridExpenditure.Init();
        }

        /// <summary>
        /// Получение и загрузка данных в таблицу баланса прихода и расхода поддонов
        /// </summary>
        private async void LoadItemsBalance()
        {
            bool resume = true;
            GridToolbar.IsEnabled = false;
            GridBalance.ShowSplash();

            if (resume)
            {
                var df = DateFrom.Text.ToDateTime();
                var dt = DateTo.Text.ToDateTime();
                if (DateTime.Compare(df, dt) > 0)
                {
                    var msg = "Дата начала должна быть меньше даты окончания.";
                    var d = new DialogWindow($"{msg}", "Проверка данных");
                    d.ShowDialog();
                    resume = false;
                }
            }

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Stock");
                q.Request.SetParam("Object", "Pallet");
                q.Request.SetParam("Action", "ListBalance");
                q.Request.SetParam("FACT_ID", FactIdSelectBox.SelectedItem.Key);
                q.Request.SetParam("DateFrom", DateFrom.Text);
                q.Request.SetParam("DateTo", DateTo.Text);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        PalletBalanceDS = ListDataSet.Create(result, "ListBalance");
                        GridReceipt.UpdateItems(new ListDataSet());
                        GridExpenditure.UpdateItems(new ListDataSet());
                        GridBalance.UpdateItems(PalletBalanceDS);
                    }
                }
            }

            GridBalance.HideSplash();
            GridToolbar.IsEnabled = true;
        }

        /// <summary>
        /// Получение и загрузка данных в таблицу прихода поддонов
        /// </summary>
        private async void LoadItemsReceipt()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Pallet");
            q.Request.SetParam("Action", "ListReceiptPallet");
            q.Request.SetParam("FACT_ID", FactIdSelectBox.SelectedItem.Key);
            q.Request.SetParam("DateFrom", DateFrom.Text);
            q.Request.SetParam("DateTo", DateTo.Text);
            q.Request.SetParam("IdPal", SelectedPalletItem.CheckGet("ID_PAL"));

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    ReceiptDS = ListDataSet.Create(result, "ListReceipt");
                    GridReceipt.UpdateItems(ReceiptDS);
                }
            }
        }

        /// <summary>
        /// Получение и загрузка данных в таблицу расхода поддонов
        /// </summary>
        private async void LoadItemsExpenditure()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Pallet");
            q.Request.SetParam("Action", "ListExpenditurePallet");
            q.Request.SetParam("FACT_ID", FactIdSelectBox.SelectedItem.Key);
            q.Request.SetParam("DateFrom", DateFrom.Text);
            q.Request.SetParam("DateTo", DateTo.Text);
            q.Request.SetParam("IdPal", SelectedPalletItem.CheckGet("ID_PAL"));

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    ExpenditureDS = ListDataSet.Create(result, "ListExpenditure");
                    GridExpenditure.UpdateItems(ExpenditureDS);
                }
            }
        }

        private void UpdateActionsBalance(Dictionary<string,string> selectedItem)
        {
            SelectedPalletItem = selectedItem;
            LoadItemsReceipt();
            LoadItemsExpenditure();
            ProcessPermissions();
        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel("[erp]pallet");
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

            if (GridBalance != null && GridBalance.Menu != null && GridBalance.Menu.Count > 0)
            {
                foreach (var manuItem in GridBalance.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }

            if (GridReceipt != null && GridReceipt.Menu != null && GridReceipt.Menu.Count > 0)
            {
                foreach (var manuItem in GridReceipt.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }

            if (GridExpenditure != null && GridExpenditure.Menu != null && GridExpenditure.Menu.Count > 0)
            {
                foreach (var manuItem in GridExpenditure.Menu)
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
        /// Экспорт данных из таблицы в Excel
        /// </summary>
        /// <param name="items">строки для экспорта</param>
        public async void ExportToExcel(List<DataGridHelperColumn> columns, List<Dictionary<string, string>> items)
        {
            var eg = new ExcelGrid();
            eg.SetColumnsFromGrid(columns);
            eg.Items = items;
            await Task.Run(() =>
            {
                eg.Make();
            });

        }

        /// <summary>
        /// Вызов справки
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/warehouse/pallets#block4");
        }

        /// <summary>
        /// Обработчик нажатия на кнопку справки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку Показать
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadItemsBalance();
        }

        private void ExcelBalanceButton_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcel(GridBalance.Columns, GridBalance.GridItems);
        }

        private void ReceiptToExcelButton_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcel(GridReceipt.Columns, GridReceipt.GridItems);
        }

        private void ExpenditureToExcelButton_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcel(GridExpenditure.Columns, GridExpenditure.GridItems);
        }
        private void SetBalance()
        {
            if (Central.Navigator.GetRoleLevel("[erp]pallet") >= Role.AccessMode.FullAccess)
            {
                if (GridBalance.SelectedItem != null)
                {
                    if (GridBalance.SelectedItem.ContainsKey("ID_PAL"))
                    {
                        int id = GridBalance.SelectedItem["ID_PAL"].ToInt();
                        if (id > 0)
                        {
                            var locationForm = new SetBalance();
                            locationForm.ReturnTabName = "Pallets_Balance";
                            locationForm.Set(id, GridBalance.SelectedItem["SALDO_END"].ToInt(), FactIdSelectBox.SelectedItem.Key.ToInt());
                        }
                    }
                }
            }
        }
    }
}
