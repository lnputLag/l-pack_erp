using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Форма добавления листов заготовок для образцов
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleCardboardAdd : UserControl
    {
        public SampleCardboardAdd()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            InitGrid();
            SetDefaults();
        }

        public int Idc;

        public string TabName;

        public string ReceiverName;

        public void SetDefaults()
        {
            Idc = 0;
            TabName = "CardboardPreforms";
            Status.Text = "";
        }

        /// <summary>
        /// Остановка вспомогательных процессов при закрытии вкладки
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Preproduction",
                ReceiverName = "",
                SenderName = TabName,
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
            //останавливаем таймеры грида
            Grid.Destruct();
        }

        /// <summary>
        /// Обработчик сообщений
        /// </summary>
        /// <param name="m">сообщение</param>
        private void ProcessMessages(ItemMessage m)
        {

        }


        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=50,
                },
                new DataGridHelperColumn
                {
                    Header="Название",
                    Path="NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=100,
                    MaxWidth=1000,
                },
                new DataGridHelperColumn
                {
                    Header="Листов в наличии",
                    Path="QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="Код товара",
                    Path="ID2",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Код картона для образцов",
                    Path="ID_SC",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };

            Grid.SetColumns(columns);
            Grid.Init();
            Grid.AutoUpdateInterval = 0;
            Grid.UseSorting = false;
            Grid.Run();
        }


        /// <summary>
        /// Получение данных из БД
        /// </summary>
        private async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "SampleCardboards");
            q.Request.SetParam("Action", "ProductList");
            q.Request.SetParam("IDC", Idc.ToString());

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result.Count > 0)
                {
                    var ds = ListDataSet.Create(result, "ProductList");
                    Grid.UpdateItems(ds);
                }
            }

        }

        public void Edit(int idc)
        {
            Idc = idc;
            GetData();
            Show();
        }

        /// <summary>
        /// Создание вкладки
        /// </summary>
        public void Show()
        {
            Central.WM.AddTab($"{TabName}_{Idc}", "Добавить заготовки", true, "add", this);
        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        public void Close()
        {
            Central.WM.RemoveTab($"{TabName}_{Idc}");

            Destroy();
        }

        public async void Save()
        {
            int qty = PreformQty.Text.ToInt();
            int id2 = Grid.SelectedItem.CheckGet("ID2").ToInt();
            if ((qty > 0) && (id2 > 0))
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "SampleCardboards");
                q.Request.SetParam("Action", "PreformAppend");
                q.Request.SetParam("ID2", id2.ToString());
                q.Request.SetParam("QTY", qty.ToString());
                q.Request.SetParam("RACK_NUM", RackNum.Text);
                q.Request.SetParam("PLACE_NUM", CellNum.Text);
                q.Request.SetParam("NOTE_CARTON", Note.Text);
                q.Request.SetParam("MANUAL", "1");

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result.Count > 0)
                    {
                        if (result.ContainsKey("ITEMS"))
                        {
                            //отправляем сообщение об обновлении грида
                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "Preproduction",
                                ReceiverName = ReceiverName,
                                SenderName = TabName,
                                Action = "Refresh",
                            });

                            Close();
                        }
                    }
                }
                else if (q.Answer.Error.Code == 147)
                {
                    Status.Text = q.Answer.Error.Message;
                }

            }
            else
            {
                Status.Text = "Не все обязательные поля заполнены верно";
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }
    }
}
