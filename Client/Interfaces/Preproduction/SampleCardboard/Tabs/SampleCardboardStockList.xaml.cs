using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Таблица списания сделанных заготовок картона для образцов
    /// </summary>
    /// <author>Рясной П.В.</author>
    public partial class SampleCardboardStockList : UserControl
    {
        public SampleCardboardStockList()
        {
            InitializeComponent();
            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/samples_new/carton_samples";

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            InitGrid();

            UIUtil.ProcessPermissions("[erp]sample_cardboard", this);
        }

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        public string TabName;

        public string DocumentationUrl;

        /// <summary>
        /// Деструктор компонента
        /// </summary>
        public void Destroy()
        {
            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры гридов
            Grid.Destruct();
        }

        /// <summary>
        /// Обработчики нажатий клавиш
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessKeyboard2(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F5:
                    Grid.LoadItems();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

                case Key.Home:
                    Grid.SetSelectToFirstRow();
                    e.Handled = true;
                    break;

                case Key.End:
                    Grid.SetSelectToLastRow();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// обработчик системы навигации по URL
        /// </summary>
        public void ProcessNavigation()
        {

        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.IndexOf("Preproduction") > -1)
            {
                if (m.ReceiverName.IndexOf(TabName) > -1)
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            Grid.LoadItems();
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        public void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="RAW_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=100,
                    MaxWidth=200,
                },
                new DataGridHelperColumn
                {
                    Header="Картон",
                    Path="CARDBOARD_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=200,
                },
                new DataGridHelperColumn
                {
                    Header="Номер ПЗ",
                    Path="TASK_NUM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=80,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Ряд",
                    Path="STOCK_RACK",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="Ячейка",
                    Path="STOCK_CELL",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество",
                    Path = "QTY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 50,
                    MaxWidth=90,
                },
                new DataGridHelperColumn
                {
                    Header=" ",
                    Path="_",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=5,
                    MaxWidth=1500,
                },
                new DataGridHelperColumn
                {
                    Header = "ИД прихода",
                    Path = "IDP",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header = "ИД товара",
                    Path = "ID2",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            Grid.SetColumns(columns);
            Grid.Init();

            //данные грида
            Grid.OnLoadItems = LoadItems;
            Grid.Run();

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateActions(selectedItem);
                }
            };

            //фокус ввода           
            Grid.Focus();
        }

        /// <summary>
        /// обновление методов работы с выбранной записью
        /// </summary>
        /// <param name="selectedItem"></param>
        public void UpdateActions(Dictionary<string, string> selectedItem)
        {
            SelectedItem = selectedItem;
        }

        /// <summary>
        /// Загрузка данных из БД
        /// </summary>
        public async void LoadItems()
        {
            GridToolbar.IsEnabled = false;
            Grid.ShowSplash();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "SampleCardboards");
            q.Request.SetParam("Action", "StockList");

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result.Count > 0)
                {
                    if (result.ContainsKey("StockList"))
                    {
                        var ds = ListDataSet.Create(result, "StockList");
                        Grid.UpdateItems(ds);
                    }
                }
            }

            GridToolbar.IsEnabled = true;
            Grid.HideSplash();
        }

        /// <summary>
        /// Показывает страницу справки
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp(DocumentationUrl);
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
        /// Обработчика нажатия на кнопку Обновить
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResreshButton_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void ReceiveButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem != null)
            {
                int productId = SelectedItem.CheckGet("ID2").ToInt();
                if (productId > 0)
                {
                    var receiverWindow = new SampleCardboardReceivePreforms();
                    receiverWindow.ReceiverName = TabName;
                    receiverWindow.Edit(productId);
                }
            }
        }
    }
}
