using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Interaction logic for WMSItem.xaml
    /// диалог отображения истории изменений
    /// </summary>
    /// <author>eletskikh_ya</author>
    public partial class ItemOperationsHistory : UserControl
    {

        public ItemOperationsHistory(int id, string currentCell = null, string itemName = null)
        {
            InitializeComponent();
            Id = id;
            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            SetDefaults();

            Init();

            HistoryGridInit();

            ItemName = itemName ?? string.Empty;
        }

        private string ItemName
        {
            get;set;
        }
        

        private Dictionary<string, string> StorageGridSelectedItem { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// идентификатор ячейки, с которой работает форма
        /// (primary key записи таблицы)
        /// </summary>
        private int Id { get; set; }

        /// <summary>
        /// ингициалищация компонентов формы
        /// </summary>
        public void SetDefaults()
        {
            Form = new FormHelper();
            Form.SetDefaults();
        }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void Init()
        {

        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
        }

        private void HistoryGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Дата",
                        Path="DTTM",
                        Doc="Дата",
                        ColumnType=ColumnTypeRef.String,
                        Width = 110,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Пользователь",
                        Path="NAME",
                        Doc="Пользователь",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Операция",
                        Path="OPERATION",
                        Doc="Операция",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ячейка",
                        Path="FROM_NUM",
                        Doc="Ячейка",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 140,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Назначение",
                        Path="TO_NUM",
                        Doc="Назначение",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 140,
                    },
                    new DataGridHelperColumn
                    {
                        Header=" ",
                        Path="_",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=5,
                        MaxWidth=2000,
                    },
                };

                HistoryGrid.SetColumns(columns);

                HistoryGrid.Label = "StorageGrid";
                HistoryGrid.Init();

                //данные грида
                HistoryGrid.OnLoadItems = StorageGridLoadItems;

                HistoryGrid.Run();

                //фокус ввода           
                HistoryGrid.Focus();
            }

        }

      

       /// <summary>
       /// Функция загрузки таблицу данными
       /// </summary>
        private async void StorageGridLoadItems()
        {
            var p = new Dictionary<string, string>();
            {
                p.Add("WMIT_ID", Id.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "Item");
            q.Request.SetParam("Action", "HistoryOperations");
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
                    HistoryGrid.UpdateItems(ds);
                }
            }
        }


        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.Down:
                    HistoryGrid.SetSelectToNextRow();
                    e.Handled = true;
                    break;

                case Key.Up:
                    HistoryGrid.SetSelectToPrevRow();
                    e.Handled = true;
                    break;
               
                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;
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
            Central.WM.FrameMode = 1;

            var frameName = GetFrameName();
            Central.WM.Show(frameName, "История операций", true, "add", this);

            //HistoryGrid.LoadItems();
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            var frameName = GetFrameName();
            Central.WM.Close(frameName);

            //вся работа по утилизации ресурсов происходит в Destroy
            //он будет вызван при закрытии фрейма
        }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "wms",
                ReceiverName = "",
                SenderName = GetFrameName(),
                Action = "Closed",
            });


            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// формирует уникальный идентификатор фрейма
        /// </summary>
        /// <returns></returns>
        public string GetFrameName()
        {
            string result = "";

            result = $"{FrameName}_{Id}";

            return result;
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
        }

        private void Filters_TextChanged(object sender, TextChangedEventArgs e)
        {
            HistoryGrid.UpdateItems();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void CheckShowAll_Checked(object sender, RoutedEventArgs e)
        {
            HistoryGrid.UpdateItems();
        }

        private void CheckShowAll_Unchecked(object sender, RoutedEventArgs e)
        {
            HistoryGrid.UpdateItems();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
