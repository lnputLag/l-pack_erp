using Client.Assets.HighLighters;
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
    /// Вкладка добавления образца в очередь плоттера минуя планировщика
    /// </summary>
    public partial class SampleAppend : UserControl
    {
        public SampleAppend()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            InitGrid();
        }

        /// <summary>
        /// Имя вкладки, из которой вызван фрейм
        /// </summary>
        public string BackTabName;

        /// <summary>
        /// Номер плоттера, куда добавляем образец
        /// </summary>
        public int PlotterNum;

        /// <summary>
        /// отображение статьи в справочной системе
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/preproduction/sample_task_list");
        }

        /// <summary>
        /// Обработка сообщений
        /// </summary>
        /// <param name="obj"></param>
        private void ProcessMessages(ItemMessage obj)
        {

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
                    Header="ИД образца",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=70,
                },
                new DataGridHelperColumn
                {
                    Header="Дата заявки",
                    Path="DT_CREATED",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Плановая дата изготовления",
                    Path="DT_COMPLITED",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Очередь",
                    Path="_MACHINE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Заказчик",
                    Path="NAME_POK",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=280,
                },
                new DataGridHelperColumn
                {
                    Header="Размеры образца",
                    Path="SAMPLE_SIZE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=120,
                },
                new DataGridHelperColumn
                {
                    Header="Тип изделия",
                    Path="SAMPLE_CLASS",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=200,
                },
                new DataGridHelperColumn
                {
                    Header="Размеры развертки",
                    Path="BLANK_SIZE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Количество изделий",
                    Path="QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=70,
                },
                new DataGridHelperColumn
                {
                    Header="Дополнительные требования",
                    Path="NAME_NOTE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=250,
                },
                new DataGridHelperColumn
                {
                    Header="Картон",
                    Path="CARDBOARD_NAME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=100,
                    MaxWidth=280,
                    Stylers=new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row=>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if ((row["RAW_MISSING_FLAG"].ToInt() == 1) || (row["CARDBOARD_QTY"].ToInt() == 0))
                                {
                                    color = HColor.YellowOrange;
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header="Номер картона",
                    Path="CARDBOARD_NUM",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Габариты сырья",
                    Path="RAW_SIZE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=70,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Листов картона в наличии",
                    Path="CARDBOARD_QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=70,
                },
                new DataGridHelperColumn
                {
                    Header="Тип доставки",
                    Path="DELIVERY_TYPE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=70,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header="Чертеж",
                    Path="DESIGN_FILE_IS",
                    ColumnType=ColumnTypeRef.Boolean,
                    MinWidth=30,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="Чертеж в другом формате",
                    Path="DESIGN_FILE_OTHER_IS",
                    ColumnType=ColumnTypeRef.Boolean,
                    MinWidth=30,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="Приложены файлы",
                    Path="FILE_IS",
                    ColumnType=ColumnTypeRef.Boolean,
                    MinWidth=30,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание технолога",
                    Path="TECHNOLOG_NOTE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=70,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Номер очереди",
                    Path="NUM",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Профиль картона",
                    Path="PROFILE_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Флаг задачи",
                    Path="TASK_FLAG",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            Grid.SetColumns(columns);
            Grid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
            Grid.SearchText = SearchText;

            var today = DateTime.Now.Date;
            // раскраска строк
            Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                // Цвета фона строк
                {
                    StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        var dtCompleted = row["DT_COMPLITED"].ToDateTime();

                        // задания на сегодня
                        if (DateTime.Compare(dtCompleted, today) == 0)
                        {
                            color=HColor.Yellow;
                        }
                        // просроченные задания
                        if (DateTime.Compare(dtCompleted, today) < 0)
                        {
                            color=HColor.Pink;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
                // цвета шрифта строк
                {
                    StylerTypeRef.ForegroundColor,
                    (Dictionary<string, string> row) =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";
                        // Если нет чертежей
                        if (!(row["DESIGN_FILE_IS"].ToBool() || row["DESIGN_FILE_OTHER_IS"].ToBool()) )
                        {
                            color = HColor.BlueFG;
                        }
                        // Если выполнение задания в очереди было отменено
                        if (row["TASK_FLAG"].ToInt() == 1)
                        {
                            color = HColor.MagentaFG;
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
            Grid.OnFilterItems = FilterItems;
            Grid.OnDblClick = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    Save();
                }
            };
        }

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        private async void LoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "ListWork");

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
                    var sampleDS = ListDataSet.Create(result, "SAMPLES");
                    var processwdDS = ProcessItems(sampleDS);
                    Grid.UpdateItems(processwdDS);
                }
            }
        }

        /// <summary>
        /// Обработка строк перед загрузкой в таблицу
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        private ListDataSet ProcessItems(ListDataSet ds)
        {
            ListDataSet _ds = ds;

            if (ds != null)
            {
                if (ds.Items.Count > 0)
                {
                    foreach (var item in _ds.Items)
                    {
                        string s = "";
                        var machine = item.CheckGet("MACHINE");
                        if (machine.IsNullOrEmpty())
                        {
                            s = "Не в очереди";
                        }
                        else
                        {
                            s = $"Плоттер {machine.ToInt()}";
                        }

                        item.CheckAdd("_MACHINE_NAME", s);
                    }
                }
            }

            return _ds;
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
                    bool showWaiting = (bool)WaitingSource.IsChecked;
                    bool showQueue = (bool)ShowQueueCheckBox.IsChecked;

                    var list = new List<Dictionary<string, string>>();

                    foreach (var item in Grid.GridItems)
                    {
                        bool includeByWaiting = true;
                        bool includeByQueue = true;

                        if (!showWaiting)
                        {
                            if ((item.CheckGet("RAW_MISSING_FLAG").ToInt() == 1) || (item.CheckGet("CARDBOARD_QTY").ToInt() == 0))
                            {
                                includeByWaiting = false;
                            }
                        }

                        int machineNum = item.CheckGet("MACHINE").ToInt();
                        if (machineNum == PlotterNum)
                        {
                            includeByQueue = false;
                        }
                        else if (!showQueue)
                        {
                            if (machineNum > 0)
                            {
                                includeByQueue = false;
                            }
                        }

                        if (
                            includeByWaiting
                         && includeByQueue
                        )
                        {
                            list.Add(item);
                        }
                    }

                    Grid.GridItems = list;
                }
            }
        }

        /// <summary>
        /// Отображение вкладки
        /// </summary>
        public void Show()
        {
            if (PlotterNum > 0)
            {
                string title = $"Выбор образца";
                Central.WM.AddTab($"sample_append", title, true, "add", this);
                Grid.LoadItems();
            }
            else
            {

            }
        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        public void Close()
        {
            Central.WM.RemoveTab($"sample_append");
        }

        /// <summary>
        /// Сохранение выбранного образца в очереди
        /// </summary>
        public async void Save()
        {
            var sampleId = Grid.SelectedItem.CheckGet("ID").ToInt();
            if (sampleId > 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "SampleTask");
                q.Request.SetParam("Action", "Append");
                q.Request.SetParam("ID_SMPL", sampleId.ToString());
                q.Request.SetParam("MACHINE", PlotterNum.ToString());
                // Если переносим из другой очереди, передаем номер старого плоттера
                q.Request.SetParam("OLD_MACHINE", Grid.SelectedItem.CheckGet("MACHINE"));
                // Количество минут всегда делаем 10
                q.Request.SetParam("ESTIMATE", "10");

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
                        if (result.ContainsKey("ITEMS"))
                        {
                            Close();
                            //отправляем гриду сообщение о необходимости обновления
                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "PreproductionSample",
                                ReceiverName = BackTabName,
                                SenderName = "SampleAppend",
                                Action = "Refresh",
                            });
                        }
                    }
                }
            }

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (Grid.SelectedItem != null)
            {
                Save();
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void WaitingSource_Click(object sender, RoutedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void ShowQueue_Click(object sender, RoutedEventArgs e)
        {
            Grid.UpdateItems();
        }
    }
}
