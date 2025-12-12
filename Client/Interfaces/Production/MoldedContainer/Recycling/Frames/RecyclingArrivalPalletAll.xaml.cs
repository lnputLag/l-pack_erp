using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Stock;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.MoldedContainer
{
    /// <summary>
    /// Форма со списком оприходованных паллет с готовой продукцией на ЛТ
    /// </summary>
    /// <author>greshnyh_ni</author>
    public partial class RecyclingArrivalPalletAll : ControlBase
    {
        public RecyclingArrivalPalletAll()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            InitGrid();
            SetDefaults();
        }


        /// <summary>
        /// ИД машины (311, 321)
        /// </summary>
        public string IdSt { get; set; }
        /// <summary>
        /// Список ID образцов в виде строки
        /// </summary>
        public string IdList;

        /// <summary>
        /// Имя вкладки, куда происходит возврат при закрытии этой вкладки
        /// </summary>
        public string ReceiverName;

        /// <summary>
        /// Имя этой вкладки
        /// </summary>
        public string TabName;

        public string ObjectName;

        /// <summary>
        /// Обработка сообщений
        /// </summary>
        /// <param name="obj"></param>
        private void ProcessMessages(ItemMessage obj)
        {
        }

        /// <summary>
        /// Инициализация таблицы изделий
        /// </summary>
        private void InitGrid()
        {
            //колонки грида
            //колонки грида
            var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Дата создания",
                        Path="PALLET_CREATED",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Description="",
                        Width2=14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ паллета",
                        Path="PALLET_NUMBER_CUSTOM",
                        Description="",
                        ColumnType=ColumnTypeRef.String,
                        Width2=9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество, шт",
                        Path="GOODS_QUANTITY",
                        Description="",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="GOODS_NAME",
                        Description="",
                        ColumnType=ColumnTypeRef.String,
                        Width2=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД ПЗ",
                        Path="PRODUCTION_TASK_ID",
                        Description="prot_id",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД паллета",
                        Path="PALLET_ID",
                        Description="(id_poddon)",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Оприходован",
                        Path="PALLET_POST",
                        Description="",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ПЗ",
                        Path="PRODUCTION_TASK2_ID",
                        Description="id_pz",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                        Hidden = true
                    },
                };

            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("PALLET_ID");
            Grid.AutoUpdateInterval = 0;
            Grid.SearchText = SearchText;
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.OnLoadItems = LoadItems;
            Grid.Init();

            

        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            IdSt = "";
            ObjectName = "ArrivialPalletAll";
            TabName = $"{ObjectName}";
        }

        public void Edit()
        {
            ControlName = $"ArrivialPalletAll_{IdSt}";
            Grid.LoadItems();
            Show();
        }

        private async void LoadItems()
        {
            var p = new Dictionary<string, string>();
            {
                p.Add("ID_ST", IdSt);
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Recycling");
            q.Request.SetParam("Action", "PalletAllList");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            var dataSet = new ListDataSet();
            int i = 0;

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    dataSet = ListDataSet.Create(result, "ITEMS");
                    if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                    {
                        Grid.UpdateItems(dataSet);
                    }
                }
            }
            else
            {
                //  q.ProcessError();
            }
        }

        public void Show()
        {
            string title = $"Список оприходованных паллет с готовой продукцией";
           // Central.WM.AddTab(TabName, title, true, "add", this);
           Central.WM.Show(ControlName, title, true, "add", this);
        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        public void Close()
        {
            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            Central.WM.Close(ControlName);
            Central.WM.SetActive(ReceiverName);
        }

                
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
                
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void PalletPrintButton_Click(object sender, RoutedEventArgs e)
        {
            PrintPallet();
        }

        public void PrintPallet()
        {
            Stock.LabelReport2 report = new LabelReport2(true);
            report.PrintLabel(Grid.SelectedItem.CheckGet("PALLET_ID").ToInt().ToString());
        }

    }
}
