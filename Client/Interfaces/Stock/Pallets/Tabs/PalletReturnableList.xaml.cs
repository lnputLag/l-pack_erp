using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Вкладка возвратных поддонов
    /// </summary>
    public partial class PalletReturnableList : UserControl
    {
        public PalletReturnableList()
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

            SelectedItem = new Dictionary<string, string>();
            InitGridPdn();
            InitGridItems();

            ProcessPermissions();
        }

        /// <summary>
        /// Данные для таблицы баланса расхода и возврата поддонов
        /// </summary>
        ListDataSet PalletBalanceDS { get; set; }

        /// <summary>
        /// Данные для сверки накладных расхода и возврата поддонов покупателем
        /// </summary>
        ListDataSet PalletVerificationDS { get; set; }

        /// <summary>
        /// Выбранная строка в таблице баланса
        /// </summary>
        Dictionary<string, string> SelectedItem;

        /// <summary>
        /// обработчик системы навигации по URL
        /// </summary>
        public void ProcessNavigation()
        {

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
            DateFrom.Text = DateTime.Now.AddMonths(-1).ToString("dd.MM.yyyy");
            DateTo.Text = DateTime.Now.ToString("dd.MM.yyyy");
            SearchText.Text="";
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
        /// обработчик сообщений
        /// </summary>
        /// <param name="obj">сообщение</param>
        private void ProcessMessages(ItemMessage obj)
        {
            if (obj.ReceiverGroup.IndexOf("Stock") > -1)
            {
                if (obj.ReceiverName.IndexOf("PalletExpenditureList") > -1)
                {
                    switch (obj.Action)
                    {
                        case "Refresh":
                            GridBalance.LoadItems();
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Деструктор компонента
        /// </summary>
        public void Destroy()
        {
            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
            //останавливаем таймеры гридов
            GridBalance.Destruct();
            GridItems.Destruct();
        }

        /// <summary>
        /// Инициализация таблицы баланса возвратных поддонов
        /// </summary>
        private void InitGridPdn()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=30,
                },
                new DataGridHelperColumn
                {
                    Header="Покупатель",
                    Path="NAME_POK",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=100,
                    MaxWidth=220,
                },
                new DataGridHelperColumn
                {
                    Header="Поддон",
                    Path="NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=80,
                    MaxWidth=160,
                },
                new DataGridHelperColumn
                {
                    Header="Сальдо, начало периода",
                    Path="SALDO_BEGIN",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=60,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header="Расход, шт",
                    Path="QTY_PC",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=85,
                },
                new DataGridHelperColumn
                {
                    Header="Возврат, шт",
                    Path="QTY_PD",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=85,
                },
                new DataGridHelperColumn
                {
                    Header="Сальдо, конец периода",
                    Path="SALDO_END",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=55,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header=" ",
                    Path="_",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=55,
                    MaxWidth=1500,
                },
                new DataGridHelperColumn
                {
                    Header="Ид поддона",
                    Path="ID_PAL",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Ид покупателя",
                    Path="ID_POK",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            GridBalance.SetColumns(columns);
            GridBalance.SearchText=SearchText;
            GridBalance.Init();

            GridBalance.OnLoadItems = LoadItemsBalance;
            GridBalance.Run();

            //при выборе строки в гриде, обновляются актуальные действия для записи
            GridBalance.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdatePdnActions(selectedItem);
                }
            };
        }

        /// <summary>
        /// Инициализация таблицы накладных по расходу и возврату поддонов
        /// </summary>
        private void InitGridItems()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=30,
                },
                new DataGridHelperColumn
                {
                    Header="Дата",
                    Path="DT",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    MinWidth=62,
                    MaxWidth=62,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header="Накладная",
                    Path="NUM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=80,
                    MaxWidth=160,
                },
                new DataGridHelperColumn
                {
                    Header="Количество, шт",
                    Path="CREDIT_QTY",
                    Group="Расход",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=70,
                    MaxWidth=110,
                },
                new DataGridHelperColumn
                {
                    Header="Сумма, р",
                    Path="CREDIT_SUM",
                    Format="N2",
                    Group="Расход",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Double,
                    MinWidth=50,
                    MaxWidth=70,
                },
                new DataGridHelperColumn
                {
                    Header="Количество, шт",
                    Path="DEBIT_QTY",
                    Group="Возврат",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=70,
                    MaxWidth=110,
                },
                new DataGridHelperColumn
                {
                    Header="Сумма, р",
                    Path="DEBIT_SUM",
                    Format="N2",
                    Group="Возврат",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Double,
                    MinWidth=50,
                    MaxWidth=70,
                },
                new DataGridHelperColumn
                {
                    Header=" ",
                    Path="_",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=55,
                    MaxWidth=1500,
                },
           };
            GridItems.SetColumns(columns);

            GridItems.Init();
            GridItems.OnLoadItems = LoadItemsItems;
        }

        /// <summary>
        /// Загрузка данных в таблицу баланса
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
                q.Request.SetParam("Action", "ListReturnableBalance");
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
                        PalletBalanceDS = ListDataSet.Create(result, "ReturnableBalance");
                        GridItems.UpdateItems(new ListDataSet());
                        GridBalance.UpdateItems(PalletBalanceDS);
                    }
                }
            }

            GridBalance.HideSplash();
            GridToolbar.IsEnabled = true;
        }

        /// <summary>
        /// Загрузка данных в таблицу накладных
        /// </summary>
        private async void LoadItemsItems()
        {
            bool resume = true;
            GridItems.ShowSplash();

            if (resume)
            {
                if (SelectedItem.CheckGet("ID_PAL").ToInt() == 0)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                if (SelectedItem.CheckGet("ID_POK").ToInt() == 0)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Stock");
                q.Request.SetParam("Object", "Pallet");
                q.Request.SetParam("Action", "ListReturnableInvoice");
                q.Request.SetParam("FACT_ID", FactIdSelectBox.SelectedItem.Key);
                q.Request.SetParam("DateFrom", DateFrom.Text);
                q.Request.SetParam("DateTo", DateTo.Text);
                q.Request.SetParam("IdPal", SelectedItem["ID_PAL"]);
                q.Request.SetParam("IdPok", SelectedItem["ID_POK"]);

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
                        PalletVerificationDS = ListDataSet.Create(result, "ReturnInvoices");
                        GridItems.UpdateItems(PalletVerificationDS);
                    }
                }
            }

            GridItems.HideSplash();
        }

        /// <summary>
        /// Выгружает акт сверки возвратных поддонов по выбранному покупателю и виду поддона
        /// </summary>
        public async void ShowReconciliationDoc()
        {
            bool resume = true;

            if (resume)
            {
                if (SelectedItem.CheckGet("ID_PAL").ToInt() == 0)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                if (SelectedItem.CheckGet("ID_POK").ToInt() == 0)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Stock");
                q.Request.SetParam("Object", "Pallet");
                q.Request.SetParam("Action", "GetReconciliationDoc");
                q.Request.SetParam("FACT_ID", FactIdSelectBox.SelectedItem.Key);
                q.Request.SetParam("DateFrom", DateFrom.Text);
                q.Request.SetParam("DateTo", DateTo.Text);
                q.Request.SetParam("IdPal", SelectedItem["ID_PAL"]);
                q.Request.SetParam("IdPok", SelectedItem["ID_POK"]);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    Central.OpenFile(q.Answer.DownloadFilePath);
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// Обновление действий с таблицей расходных накладных
        /// </summary>
        /// <param name="selectedItem"></param>
        private void UpdatePdnActions(Dictionary<string, string> selectedItem)
        {
            SelectedItem = selectedItem;

            LoadItemsItems();

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

            if (GridItems != null && GridItems.Menu != null && GridItems.Menu.Count > 0)
            {
                foreach (var manuItem in GridItems.Menu)
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
        /// Вызов справки
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/warehouse/pallets#block3");
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

        /// <summary>
        /// Обработчик нажатия на кнопку справки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void ReconciliationButton_Click(object sender,RoutedEventArgs e)
        {
            ShowReconciliationDoc();
        }
        
    }
}
