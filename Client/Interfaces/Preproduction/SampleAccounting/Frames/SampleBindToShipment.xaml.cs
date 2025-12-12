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
    /// Форма со списком доступных отгрузок для привязки образцов или оснастки
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleBindToShipment : UserControl
    {
        public SampleBindToShipment()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            InitGrid();
            SetDefaults();
        }


        /// <summary>
        /// ИД заказчика/клиента
        /// </summary>
        public int CustomerId;
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

        public int FactoryId;

        public int TypeOrder;

        /// <summary>
        /// Обработка сообщений
        /// </summary>
        /// <param name="obj"></param>
        private void ProcessMessages(ItemMessage obj)
        {
            if (obj.ReceiverGroup.IndexOf("PreproductionSample") > -1)
            {
                if (obj.ReceiverName.IndexOf(TabName) > -1)
                {
                }
            }
        }

        /// <summary>
        /// Инициализация таблицы изделий
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
                    MinWidth=50,
                    MaxWidth=50,
                },
                new DataGridHelperColumn
                {
                    Header="Дата отгрузки",
                    Path="SHIPMENT_DATE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="Покупатель",
                    Path="CUSTOMER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=300,
                },
                new DataGridHelperColumn
                {
                    Header="Водитель",
                    Path="DRIVER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=300,
                },
                new DataGridHelperColumn
                {
                    Header="Номер заявки",
                    Path="NUMBER_ORDER",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=300,
                },
                new DataGridHelperColumn
                {
                    Header="ИД отгрузки",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            Grid.SetColumns(columns);
            Grid.AutoUpdateInterval = 0;
            Grid.SearchText = SearchText;
            Grid.Init();

            Grid.OnLoadItems = LoadItems;
            Grid.OnDblClick = selectedItem =>
            {
                Save();
            };
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            CustomerId = 0;
            TabName = "";
            IdList = "";
            ObjectName = "Samples";
            FactoryId = 1;
            TypeOrder = 1;
        }

        private async void LoadItems()
        {
            // Если отмечен чекбокс Все отгрузки, то вместо ИД покупателя отправляем 0
            string customerId = (bool)AllShipments.IsChecked ? "0" : CustomerId.ToString();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "ListAvaliableShipment");
            q.Request.SetParam("CUSTOMER_ID", customerId);
            q.Request.SetParam("FACTORY_ID", FactoryId.ToString());
            q.Request.SetParam("TYPE_ORDER", TypeOrder.ToString());

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
                    var ds = ListDataSet.Create(result, "SHIPMENTS");
                    Grid.UpdateItems(ds);
                }
            }
        }

        /// <summary>
        /// Вызов метода привязки отгрузки к образцам
        /// </summary>
        /// <param name="list">список Id образцов в виде строки с разделителем запятая</param>
        public void Bind(string list)
        {
            if (!string.IsNullOrEmpty(list))
            {
                IdList = list;
                if (TypeOrder == 2)
                {
                    AllShipments.IsChecked = false;
                    AllShipments.IsEnabled = false;
                    OtherFactory.IsChecked = true;
                }
                else if (CustomerId == 0)
                {
                    AllShipments.IsChecked = true;
                    AllShipments.IsEnabled = false;
                }

                Grid.LoadItems();
                Show();
            }
        }

        public void Show()
        {
            string title = $"Отгрузки для привязки";
            TabName = $"{ObjectName}BindShipment";
            Central.WM.AddTab(TabName, title, true, "add", this);
        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
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

        private async void Save()
        {
            if (Grid.SelectedItem != null)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", ObjectName);
                q.Request.SetParam("Action", "BindShipment");
                q.Request.SetParam("SHIPMENT_ID", Grid.SelectedItem.CheckGet("ID"));
                q.Request.SetParam("ID_LIST", IdList);

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
                        //отправляем сообщение о закрытии окна
                        Messenger.Default.Send(new ItemMessage()
                        {
                            ReceiverGroup = "PreproductionSample",
                            ReceiverName = ReceiverName,
                            SenderName = TabName,
                            Action = "Refresh",
                        });
                        Close();
                    }
                }
            }
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AllShipments_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void OtherFactory_Click(object sender, RoutedEventArgs e)
        {
            bool toOtherFactory = (bool)OtherFactory.IsChecked;
            if (toOtherFactory)
            {
                TypeOrder = 2;
                AllShipments.IsEnabled = false;
                FactoryId = FactoryId == 1 ? 2 : 1;
            }
            else
            {
                TypeOrder= 1;
                AllShipments.IsEnabled = true;
                FactoryId = FactoryId == 1 ? 2 : 1;
            }
            
            Grid.LoadItems();
        }
    }
}
