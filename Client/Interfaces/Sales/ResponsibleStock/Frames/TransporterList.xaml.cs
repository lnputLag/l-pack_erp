using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client.Interfaces.Sales
{
    /// <summary>
    /// Логика взаимодействия для TransporterList.xaml
    /// </summary>
    public partial class TransporterList : UserControl
    {
        public TransporterList(bool usedForAnyFrameFlag, string receiverGroup, string receiverName, string parentFrame)
        {
            InitializeComponent();

            TabName = "TransporterList";

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            InitGrid();
            SetDefaults();

            UsedForAnyFrameFlag = usedForAnyFrameFlag;
            _ReceiverGroup = receiverGroup;
            _ReceiverName = receiverName;
            _ParentFrame = parentFrame;
        }

        /// <summary>
        /// Имя фрейма
        /// Техническое имя для идентификации в системе WM
        /// </summary>
        public string TabName { get; set; }

        /// <summary>
        /// Флаг того, что фрейм используется для выбора перевозчика в другом фрейме
        /// </summary>
        private bool UsedForAnyFrameFlag { get; set; }

        /// <summary>
        /// Группа получателей для отправки выбранного перевозчика по шине сообщений
        /// </summary>
        private string _ReceiverGroup { get; set; }

        /// <summary>
        /// Имя объекта получателя для отправки выбранного перевозчика по шине сообщений
        /// </summary>
        private string _ReceiverName { get; set; }

        /// <summary>
        /// Наименование таба, который вызвал этот таб
        /// </summary>
        private string _ParentFrame { get; set; }

        /// <summary>
        /// Выбранная в гриде запись
        /// </summary>
        public Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// Датасет с данными для заполнения грида перевозчиков
        /// </summary>
        public ListDataSet GridDataSet { get; set; }

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
                    Path="TRANSPORTER_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=50,
                },
                new DataGridHelperColumn()
                {
                    Header="Перевозчик",
                    Path="TRANSPORTER_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width=255,
                },
                new DataGridHelperColumn()
                {
                    Header="Телефон",
                    Path="TRANSPORTER_PHONE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width=90,
                },

                new DataGridHelperColumn
                {
                    Header = " ",
                    Path = "_",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 5,
                    MaxWidth = 2000,
                }
            };
            Grid.SetColumns(columns);

            Grid.SetSorting("TRANSPORTER_NAME", ListSortDirection.Ascending);
            Grid.SearchText = SearchText;
            Grid.Init();

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem = (Dictionary<string, string> selectedItem) =>
            {
                SelectedItem = selectedItem;
            };

            //данные грида
            Grid.OnLoadItems = LoadItems;
            Grid.Run();

            //фокус ввода           
            Grid.Focus();
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            SearchText.Text = "";
            GridDataSet = new ListDataSet();
            SelectedItem = new Dictionary<string, string>();
        }

        /// <summary>
        /// обновление записей
        /// </summary>
        public async void LoadItems()
        {
            GridToolbar.IsEnabled = false;
            Grid.ShowSplash();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "ResponsibleStock");
            q.Request.SetParam("Action", "ListTransporter");

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
                    GridDataSet = ListDataSet.Create(result, "ITEMS");
                    if (GridDataSet != null && GridDataSet.Items != null)
                    {
                        Dictionary<string, string> emptyDictionary = new Dictionary<string, string>();
                        emptyDictionary.Add("TRANSPORTER_ID", "0");
                        emptyDictionary.Add("TRANSPORTER_NAME", "неизвестно");
                        emptyDictionary.Add("TRANSPORTER_PHONE", "");
                        GridDataSet.Items.Add(emptyDictionary);
                    }
                    Grid.UpdateItems(GridDataSet);
                }
            }

            GridToolbar.IsEnabled = true;
            Grid.HideSplash();
        }

        public void SelectTransporter()
        {
            if (SelectedItem != null && SelectedItem.Count > 0)
            {
                if (UsedForAnyFrameFlag)
                {
                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = _ReceiverGroup,
                        ReceiverName = _ReceiverName,
                        SenderName = "TransporterList",
                        Action = "SelectTransporter",
                        ContextObject = SelectedItem,
                    });

                    Close();
                }
            }
        }

        /// <summary>
        /// Отображение окна
        /// </summary>
        public void Show()
        {
            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 1;

            var dt = DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss");
            TabName = $"{TabName}_new_{dt}";

            Central.WM.Show(TabName, $"Список перевозчиков", true, "add", this);
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            Central.WM.SetActive(_ParentFrame, true);
            Central.WM.Close(TabName);

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
                ReceiverGroup = "Sales",
                ReceiverName = "",
                SenderName = "TransporterList",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {

        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            SelectTransporter();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadItems();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
