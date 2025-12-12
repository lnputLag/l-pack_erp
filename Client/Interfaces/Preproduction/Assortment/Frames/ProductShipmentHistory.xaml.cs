using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Список отгрузок позиции ассортимента
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class ProductShipmentHistory : ControlBase
    {
        public ProductShipmentHistory()
        {
            InitializeComponent();

            OnLoad = () =>
            {
                InitGrid();
                SetDefaults();
            };

            OnUnload = () =>
            {
                Grid.Destruct();
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
        }

        /// <summary>
        /// Идентификатор изделия
        /// </summary>
        public int ProductId;
        /// <summary>
        /// Имя вкладки, откуда вызвана форма и куда передается ответ
        /// </summary>
        public string ReceiverName;
        /// <summary>
        /// Часть артикула для имени вкладки
        /// </summary>
        public string ProductSku;

        /// <summary>
        /// Обработка и выполнение команд
        /// </summary>
        /// <param name="command"></param>
        public void ProcessCommand(string command, ItemMessage m = null)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "close":
                        Close();
                        break;
                    case "refresh":
                        Grid.LoadItems();
                        break;
                    case "toexcel":
                        Grid.ItemsExportExcel();
                        break;
                }
            }
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            var today = DateTime.Now.Date;
            FromDate.Text = new DateTime(today.Year, today.Month - 1, 1).ToString("dd.MM.yyyy");
            ToDate.Text = new DateTime(today.Year, today.Month, 1).AddDays(-1).ToString("dd.MM.yyyy");
        }

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=3,
                },
                new DataGridHelperColumn
                {
                    Header="Внешние размеры",
                    Path="PRODUCT_SIZE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Кол-во в заявке",
                    Path="ORDER_QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                    Totals=(List<Dictionary<string,string>> rows) =>
                    {
                        int result=0;
                        if(rows != null)
                        {
                            foreach(Dictionary<string,string> row in rows)
                            {
                                result += row.CheckGet("ORDER_QTY").ToInt();
                            }
                        }

                        return result;
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Дата отгрузки",
                    Path="SHIPPED_DATE",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=10,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header="УПД",
                    Path="INCOME_DOC",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Счет",
                    Path="PAYMENT_DOC",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Сумма",
                    Path="AMOUNT",
                    ColumnType=ColumnTypeRef.Double,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Кол-во в отгрузке",
                    Path="SHIPPED_QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                    Totals=(List<Dictionary<string,string>> rows) =>
                    {
                        int result=0;
                        if(rows != null)
                        {
                            foreach(Dictionary<string,string> row in rows)
                            {
                                result += row.CheckGet("SHIPPED_QTY").ToInt();
                            }
                        }

                        return result;
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Цена с НДС",
                    Path="PRICE",
                    ColumnType=ColumnTypeRef.Double,
                    Width2=10,
                    Exportable=false,
                },
                new DataGridHelperColumn
                {
                    Header="Адрес доставки",
                    Path="ADDRESS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=40,
                },
            };
            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("_ROWNUMBER");
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            Grid.AutoUpdateInterval = 0;

            Grid.OnLoadItems = LoadItems;
            Grid.Init();
        }

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        private async void LoadItems()
        {
            bool allData = (bool)ShowAllCheckBox.IsChecked;
            
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Products");
            q.Request.SetParam("Object", "Assortment");
            q.Request.SetParam("Action", "ShipmentHistory");
            q.Request.SetParam("ID", ProductId.ToString());
            q.Request.SetParam("ALL_DATA", allData ? "1" : "0");
            q.Request.SetParam("FROM_DT", FromDate.Text);
            q.Request.SetParam("TO_DT", ToDate.Text);

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
                    var ds = ListDataSet.Create(result, "SHIPMENTS");
                    Grid.UpdateItems(ds);
                    RefreshButton.Style = (Style)RefreshButton.TryFindResource("Button");
                }
            }
        }

        /// <summary>
        /// Отображение вкладки
        /// </summary>
        public void ShowTab()
        {
            ControlName = $"ProductShipmentHistory{ProductId}";
            string productCode = ProductSku;
            if (productCode.IsNullOrEmpty())
            {
                productCode = ProductId.ToString();
            }
            ControlTitle = $"Отгрузки {productCode}";

            Central.WM.Show(ControlName, ControlTitle, true, "add", this);
        }

        /// <summary>
        /// Закрытие вкладки с формой
        /// </summary>
        public void Close()
        {
            Central.WM.Close(ControlName);
            Central.WM.SetActive(ReceiverName);
        }

        /// <summary>
        /// Обработчик нажатия на кнопку
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonOnClick(object sender, RoutedEventArgs e)
        {
            var b = (Button)sender;
            if (b != null)
            {
                var t = b.Tag.ToString();
                ProcessCommand(t);
            }
        }

        /// <summary>
        /// Обработка изменения дат
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DateChanged(object sender, TextChangedEventArgs e)
        {
            RefreshButton.Style = (Style)RefreshButton.TryFindResource("FButtonPrimary");
        }

        private void ShowAllCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }
    }
}
