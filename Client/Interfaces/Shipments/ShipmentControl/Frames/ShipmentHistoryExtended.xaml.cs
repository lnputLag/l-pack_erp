using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Показ истории изменений
    /// <author>eletskikh_ya</author>
    /// </summary>
    public partial class ShipmentHistoryExtended : UserControl
    {
        public ShipmentHistoryExtended()
        {
            InitializeComponent();

            ShipmentId = 0;
            ReturnTabName = "";

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            //регистрация обработчика клавиатуры
            PreviewKeyDown += ProcessKeyboard;

            InitGrid();
            SetDefaults();
        }


        public int ShipmentId { get; set; }
        public string ReturnTabName { get; set; }

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        public Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// Деструктор интерфейса
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "ShipmentControl",
                ReceiverName = "",
                SenderName = "ShipmentHistory",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            Grid.Destruct();
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            SearchText.Text = "";
        }

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        public void InitGrid()
        {
            //список колонок грида
            var columns = new List<DataGridHelperColumn>()
            {
                new DataGridHelperColumn()
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=50,
                },
                new DataGridHelperColumn()
                {
                    Header="Дата",
                    Path="AUDIT_DTTM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Format="dd.MM.yy HH:mm",
                    Width=120,
                },
                new DataGridHelperColumn()
                {
                    Header="Пользователь",
                    Path="NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=70,
                },
                new DataGridHelperColumn()
                {
                    Header="Дата отгрузки",
                    Path="DTTM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Format="dd.MM.yy HH:mm",
                    Width=120,
                },
                new DataGridHelperColumn()
                {
                    Header="Водитель",
                    Path="FIO",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=120,
                },
                new DataGridHelperColumn()
                {
                    Header="Перевозчик",
                    Path="NAME_TR",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=120,
                },
                new DataGridHelperColumn()
                {
                    Header="Начало отгрузки",
                    Path="SHIPMENT_BEGIN",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Format="dd.MM.yy HH:mm",
                    Width=120,
                },
                new DataGridHelperColumn()
                {
                    Header="Окончание отгрузки",
                    Path="SHIPMENT_END",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Format="dd.MM.yy HH:mm",
                    Width=120,
                },
                new DataGridHelperColumn()
                {
                    Header="Статус отгрузки",
                    Path="FINISH_STATUS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=70,
                },
            };

            Grid.SetColumns(columns);


            Grid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
            Grid.SearchText = SearchText;
            Grid.Init();

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem = (Dictionary<string, string> selectedItem) =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateActions(selectedItem);
                }
            };

            //данные грида
            Grid.OnLoadItems = LoadItems;
            
            Grid.Run();

            //фокус ввода           
            Grid.Focus();
        }

        public void Init()
        {
            Grid.LoadItems();
            Show();
        }

        /// <summary>
        /// обновление записей
        /// </summary>
        public async void LoadItems()
        {
            GridToolbar.IsEnabled = false;
            Grid.ShowSplash();

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID", ShipmentId.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Shipment");
            q.Request.SetParam("Action", "ListHistoryExtended");

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
                    var ds = ListDataSet.Create(result, "ITEMS");
                    Grid.UpdateItems(ds);
                }
            }

            GridToolbar.IsEnabled = true;
            Grid.HideSplash();
        }


        /// <summary>
        /// обновление методов работы с выбранной записью
        /// </summary>
        /// <param name="selectedItem"></param>
        public void UpdateActions(Dictionary<string, string> selectedItem)
        {
            if (selectedItem.Count > 0)
            {
                SelectedItem = selectedItem;
            }
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            //Group ProductionTask
            if (m.ReceiverGroup.IndexOf("ShipmentControl") > -1)
            {

            }
        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessKeyboard(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F5:
                    Grid.LoadItems();
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
        /// Отображение вкладки с формой редактирования данных водителя
        /// </summary>
        public void Show()
        {
            string title = $"#{ShipmentId}";
            if (ShipmentId == 0)
            {
                title = "Отгрузка";
            }
            Central.WM.AddTab($"shipmenthistoryextend_{ShipmentId}", title, true, "add", this);

        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        public void Close()
        {
            Central.WM.RemoveTab($"shipmenthistoryextend_{ShipmentId}");
            if (ReturnTabName == "add")
            {
                Central.WM.SetLayer("add");
                ReturnTabName = "";
            }

            Destroy();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку отмены
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }


        /// <summary>
        /// Обработчик нажатия на кнопку обновления
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

    }
}
