using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Список изделий, повторяющих параметры образца. Используется для подтверждения менеджером
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleProductDuplicated : UserControl
    {
        public SampleProductDuplicated()
        {
            InitializeComponent();

            InitGrid();
            SetDefaults();
        }

        public int SampleId;

        /// <summary>
        /// Имя вкладки, куда происходит возврат при закрытии этой вкладки
        /// </summary>
        public string ReceiverName;

        /// <summary>
        /// Имя этой вкладки
        /// </summary>
        public string TabName;

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            SampleId = 0;
        }

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>()
            {
                new DataGridHelperColumn()
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    Doc="Номер по порядку в списке",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="PRODUCT_CODE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=80,
                    MaxWidth=130,
                },
                new DataGridHelperColumn
                {
                    Header="Название",
                    Path="NAME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=100,
                    MaxWidth=350,
                },
                new DataGridHelperColumn
                {
                    Header="Дата отгрузки",
                    Path="LAST_SHIPMENT",
                    ColumnType=ColumnTypeRef.DateTime,
                    MinWidth=50,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="Путь к ТК",
                    Path="PATHTK",
                    ColumnType=ColumnTypeRef.String,
                    Hidden=true,
                },
            };
            Grid.SetColumns(columns);
            Grid.AutoUpdateInterval = 0;
            Grid.Init();

            Grid.OnLoadItems = LoadItems;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateActions(selectedItem);
                }
            };
        }

        private async void LoadItems()
        {

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "ListDuplicated");
            q.Request.SetParam("ID", SampleId.ToString());

            q.Request.Timeout = Central.Parameters.RequestTimeoutMax;
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
                    var ds = ListDataSet.Create(result, "PRODUCTS");
                    Grid.UpdateItems(ds);
                }
            }
        }

        /// <summary>
        /// обновление методов работы с выбранной записью
        /// </summary>
        /// <param name="selectedItem">выбранная запись</param>
        private void UpdateActions(Dictionary<string, string> selectedItem)
        {
            TechMapButton.IsEnabled = true;
            if (string.IsNullOrEmpty(selectedItem.CheckGet("PATHTK")))
            {
                TechMapButton.IsEnabled = false;
            }
        }

        public void Show()
        {
            string title = $"Похожие изделия";
            TabName = $"SampleSameProduct";
            Central.WM.AddTab(TabName, title, true, "add", this);
        }

        public void Edit(int sampleId)
        {
            SampleId = sampleId;
            Grid.LoadItems();
            Show();
        }

        public void Close()
        {
            Central.WM.RemoveTab(TabName);

            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "PreproductionSample",
                ReceiverName = "",
                SenderName = TabName,
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            Central.WM.SetActive(ReceiverName, true);
            ReceiverName = "";
        }

        private void TechMapButton_Click(object sender, RoutedEventArgs e)
        {
            if (Grid.SelectedItem != null)
            {
                if (!string.IsNullOrEmpty(Grid.SelectedItem.CheckGet("PATHTK")))
                {
                    Central.OpenFile(Grid.SelectedItem.CheckGet("PATHTK"));
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
