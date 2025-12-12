using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Форма истории изменений картона для образцов
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleCardboardHistory : UserControl
    {
        public SampleCardboardHistory()
        {
            InitializeComponent();

            SetDefaults();
            InitGrid();
        }

        /// <summary>
        /// ID картона
        /// </summary>
        public int CardboardId;

        /// <summary>
        /// Базовое имя вкладки
        /// </summary>
        public string TabName;

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
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            CardboardId = 0;
            TabName = "SampleCardboardHistory";
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
                    MinWidth=50,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="Дата операции",
                    Path="AUDIT_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    MinWidth=50,
                    MaxWidth=120,
                    Format="dd.MM.yyyy HH:mm:ss",
                },
                new DataGridHelperColumn
                {
                    Header="Пользователь",
                    Path="USER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=80,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header="Длина",
                    Path="L",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="Формат",
                    Path="B",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="Количество",
                    Path="QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="Номер стеллажа",
                    Path="RACK_NUM",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="Номер ячейки",
                    Path="PLACE_NUM",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="Номер ПЗГА",
                    Path="NUM",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=80,
                    MaxWidth=120,
                },
                new DataGridHelperColumn
                {
                    Header=" ",
                    Path="_",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=10,
                    MaxWidth=2000,
                },
            };
            Grid.SetColumns(columns);
            Grid.SearchText = SearchText;
            // Не обновляем
            Grid.AutoUpdateInterval = 0;
            Grid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
            Grid.UseSorting = false;
            Grid.Init();
            //данные грида
            Grid.OnLoadItems = LoadItems;
            Grid.OnFilterItems = FilterItems;
            Grid.Run();
        }

        /// <summary>
        /// Загрузка данных из БД
        /// </summary>
        private async void LoadItems()
        {
            if (CardboardId > 0)
            {
                Grid.ShowSplash();
                GridToolbar.IsEnabled = false;

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "SampleCardboards");
                q.Request.SetParam("Action", "ListHistory");
                q.Request.SetParam("ID", CardboardId.ToString());


                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                }
                );

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        // Форматы, которые есть для выбранного картона для образцов
                        var formatsDS = ListDataSet.Create(result, "RAW_FORMATS");

                        var formatsBoxList = new Dictionary<string, string>()
                        {
                            { "0", "Все" },
                        };

                        if (formatsDS.Items != null)
                        {
                            if (formatsDS.Items.Count > 0)
                            {
                                foreach (var item in formatsDS.Items)
                                {
                                    var itemName = item.CheckGet("SAMPLE_RAW_NAME");
                                    var itemFormat = item.CheckGet("FORMAT");
                                    if (!string.IsNullOrEmpty(itemName))
                                    {
                                        formatsBoxList.CheckAdd(itemFormat, itemFormat);
                                    }
                                }
                            }
                        }

                        FormatBox.Items = formatsBoxList;
                        FormatBox.SelectedItem = formatsBoxList.GetEntry("0");

                        var historyDS = ListDataSet.Create(result, "HISTORY");
                        Grid.UpdateItems(historyDS);
                    }
                }

                Grid.HideSplash();
                GridToolbar.IsEnabled = true;
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
                    int format = FormatBox.SelectedItem.Key.ToInt();
                    if (format > 0)
                    {
                        var list = new List<Dictionary<string, string>>();
                        foreach (var item in Grid.GridItems)
                        {
                            bool include = false;
                            if (item.CheckGet("B").ToInt() == format)
                            {
                                include = true;
                            }

                            if (include)
                            {
                                list.Add(item);
                            }
                        }

                        Grid.GridItems = list;
                    }
                }
            }
        }

        /// <summary>
        /// Создание вкладки
        /// </summary>
        public void Show()
        {
            Central.WM.AddTab($"{TabName}_{CardboardId}", "История изменений сырья", true, "add", this);
            Grid.LoadItems();
        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        public void Close()
        {
            Central.WM.RemoveTab($"{TabName}_{CardboardId}");

            Destroy();
        }

        private void FormatBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void ResreshButton_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
