using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Production.ProcessingMachines
{
    /// <summary>
    /// Выбор сырья (гильзовый картон) для изготовления комплекта решеток
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class ProductionTaskPrRawSelect : UserControl
    {
        public ProductionTaskPrRawSelect()
        {
            InitializeComponent();

            SetDefaults();
            InitGrid();
        }

        /// <summary>
        /// Имя вкладки
        /// </summary>
        public string TabName;
        /// <summary>
        /// Имя вкладки, в которую передаётся выбор и передаётся управление
        /// </summary>
        public string ReceiverName;

        /// <summary>
        /// обработка ввода с клавиатуры (роли)
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
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
        /// деструктор интерфейса
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "ProductionTaskProcessing",
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
        /// Вызов справки
        /// </summary>
        public void ShowHelp()
        {

        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            TabName = "ProductionTaskPrRawSelect";
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
                    Header = "Ид сырья",
                    Path = "ID",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 40,
                    MaxWidth = 60,
                },
                new DataGridHelperColumn
                {
                    Header = "Название",
                    Path = "NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 60,
                    MaxWidth = 400,
                },
                new DataGridHelperColumn
                {
                    Header = "Остаток на складе",
                    Path = "QTY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 40,
                    MaxWidth = 60,
                },
                new DataGridHelperColumn
                {
                    Header = "Требуется для заданий",
                    Path = "TASK_QTY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 40,
                    MaxWidth = 60,
                },
                new DataGridHelperColumn
                {
                    Header = "Ид категории",
                    Path = "CATEGORY_ID",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header = "Площадь",
                    Path = "SQUARE",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header = "Плотность",
                    Path = "DENSITY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            Grid.SetColumns(columns);
            Grid.AutoUpdateInterval = 0;
            Grid.Init();
            Grid.OnLoadItems = LoadItems;
            Grid.OnFilterItems = FilterItems;
            //двойной клик на строке делает выбор
            Grid.OnDblClick = (Dictionary<string, string> selectedItem) =>
            {
                Save();
            };
            Grid.Run();
        }

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        private async void LoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPr");
            q.Request.SetParam("Object", "ProductionTaskPr");
            q.Request.SetParam("Action", "ListRawPartitionalSet");
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
                    if (result.ContainsKey("SLEEVE"))
                    {
                        var ds = ListDataSet.Create(result, "SLEEVE");
                        Grid.UpdateItems(ds);
                    }
                }
            }
        }

        /// <summary>
        /// Фильтрация строк таблицы
        /// </summary>
        private void FilterItems()
        {
            if (Grid.GridItems != null)
            {
                if (Grid.GridItems.Count > 0)
                {

                }
            }
        }

        public void Show()
        {
            Central.WM.AddTab(TabName, "Выбор гильзового картона", true, "add", this);
            Central.WM.SetActive(TabName);
        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        public void Close()
        {
            Central.WM.RemoveTab(TabName);
            Destroy();
            Central.WM.SetActive(ReceiverName);
            ReceiverName = "";
        }

        /// <summary>
        /// Передача данных выбранной строки в форму создания задания
        /// </summary>
        private void Save()
        {
            if (Grid.SelectedItem != null)
            {
                //отправляем сообщение с данными в выбранной строке
                Messenger.Default.Send(new ItemMessage()
                {
                    ReceiverGroup = "ProductionTaskProcessing",
                    ReceiverName = ReceiverName,
                    SenderName = TabName,
                    Action = "PartitionalRawSelected",
                    ContextObject = Grid.SelectedItem,
                });
            }
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ShowAllCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Grid.UpdateItems();
        }
    }
}
