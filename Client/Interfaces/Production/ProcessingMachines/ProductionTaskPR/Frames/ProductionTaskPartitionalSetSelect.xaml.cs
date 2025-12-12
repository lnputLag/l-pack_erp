using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Production.ProcessingMachines
{
    /// <summary>
    /// Выбор комплекта решеток 
    /// </summary>
    public partial class ProductionTaskPartitionalSetSelect : UserControl
    {
        public ProductionTaskPartitionalSetSelect()
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

        public void ShowHelp()
        {

        }

        public void SetDefaults()
        {
            TabName = "ProductionTaskPartitionalSetSelect";
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
                    Header = "№",
                    Path = "_ROWNUMBER",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 40,
                    MaxWidth = 40,
                },
                new DataGridHelperColumn
                {
                    Header = "Ид заявки",
                    Path = "ID",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 40,
                    MaxWidth = 60,
                },
                new DataGridHelperColumn
                {
                    Header = "Дата отгрузки",
                    Path = "SHIPMENT_DATE_TIME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.DateTime,
                    MinWidth = 60,
                    MaxWidth = 100,
                    Format="dd.MM.yy HH:mm",
                },
                new DataGridHelperColumn
                {
                    Header = "Артикул",
                    Path = "ARTICLE",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 60,
                    MaxWidth = 120,
                },
                new DataGridHelperColumn
                {
                    Header = "Изделие",
                    Path = "PRODUCT_NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 60,
                    MaxWidth = 400,
                },
                new DataGridHelperColumn
                {
                    Header = "Запрет авт. утилизации поддонов",
                    Path = "UNDISPOSAL_PALLET_FLAG",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth = 40,
                    MaxWidth = 40,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество в заявке",
                    Path = "ORDER_QTY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 40,
                    MaxWidth = 60,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество в заданиях",
                    Path = "TASK_QTY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 40,
                    MaxWidth = 60,
                },
                new DataGridHelperColumn
                {
                    Header = "На складе",
                    Path = "COMPLETE_QTY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 40,
                    MaxWidth = 60,
                },
                new DataGridHelperColumn
                {
                    Header = "Ид изделия",
                    Path = "PRODUCT_ID",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header = "Ид категории изделия",
                    Path = "PRODUCT_CATEGORY_ID",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header = "Ид отгрузки",
                    Path = "SHIPMENT_ID",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            Grid.SetColumns(columns);
            Grid.AutoUpdateInterval = 0;
            // Раскраска строк
            Grid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                // Цвета фона строк
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";
                        // Задания покрывают заявку
                        if (row["TASK_QTY"].ToInt() + row["COMPLETE_QTY"].ToInt() >= row["ORDER_QTY"].ToInt())
                        {
                            color = HColor.Green;
                        }
                        // Задания есть, но не полностью покрывают заявку
                        else if (row["TASK_QTY"].ToInt() + row["COMPLETE_QTY"].ToInt() > 0)
                        {
                            color = HColor.Blue;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };
            Grid.Init();
            Grid.OnLoadItems = LoadItems;
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
            q.Request.SetParam("Action", "ListPartitionalSet");
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
                    if (result.ContainsKey("PR_SETS"))
                    {
                        var ds = ListDataSet.Create(result, "PR_SETS");
                        Grid.UpdateItems(ds);
                    }
                }
            }

        }

        public void Show()
        {
            Central.WM.AddTab(TabName, "Выбор комплекта решеток", true, "add", this);
            Central.WM.SetActive(TabName);
        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        private void Close()
        {
            Central.WM.RemoveTab(TabName);
            Destroy();
            Central.WM.SetActive(ReceiverName);
            ReceiverName = "";
        }

        /// <summary>
        /// Передача данных выбранной строки
        /// </summary>
        private void Save()
        {
            var v = Grid.SelectedItem;
            v.CheckAdd("ORDER_ID", Grid.SelectedItem.CheckGet("ID"));
            //отправляем сообщение с данными в выбранной строке
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "ProductionTaskProcessing",
                ReceiverName = ReceiverName,
                SenderName = TabName,
                Action = "PartitionalSetSelected",
                ContextObject = v,
            });
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (Grid.SelectedItem != null)
            {
                Save();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
