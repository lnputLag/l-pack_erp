using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Фрейм для выбора техкарты, для которой создается новый заказ штанцформы или ремкомплекта
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class CuttingStampOrderSelectTechcard : ControlBase 
    {
        public CuttingStampOrderSelectTechcard()
        {
            InitializeComponent();

            FactoryId = 1;
            InitGrid();

            OnUnload = () =>
            {
                Grid.Destruct();
            };

        }

        /// <summary>
        /// Имя вкладки, которая вызвала открытие фрейма, и в которую возвращается фокус после закрытия фрейма
        /// </summary>
        public string ReceiverName { get; set; }

        /// <summary>
        /// Идентификатор производственной площадки: 1 - Липецк, 2 - Кашира
        /// </summary>
        public int FactoryId { get; set; }

        /// <summary>
        /// Таймер заполнения поля шаблона загрузки данных
        /// </summary>
        public DispatcherTimer TemplateTimeoutTimer;

        /// <summary>
        /// Обработка команд
        /// </summary>
        /// <param name="command"></param>
        private void ProcessCommand(string command)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "save":
                        Save();
                        break;
                    case "bind":
                        if (Grid.SelectedItem != null)
                        {
                            var d = new Dictionary<string, string>()
                            {
                                { "ID", Grid.SelectedItem.CheckGet("ID") },
                                { "SKU_CODE", Grid.SelectedItem.CheckGet("SKU_CODE") },
                                { "TK_SIZE", Grid.SelectedItem.CheckGet("TECHCARD_SIZE") }
                            };
                            var stampBindFrame = new CuttingStampBind();
                            stampBindFrame.ReceiverName = ReceiverName;
                            stampBindFrame.Show(d);
                            Close();
                        }
                        break;
                    case "repairkit":
                        CreateRepairKit();
                        break;
                    case "close":
                        Close();
                        break;
                    case "showtechcard":
                        ShowTechnologicalCard();
                        break;
                }
            }
        }

        /// <summary>
        /// Иницифлизация таблицы
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
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Покупатель",
                    Path="CUSTOMER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=26,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="SKU_CODE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn
                {
                    Header="Название",
                    Path="TECHCARD_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=40,
                },
                new DataGridHelperColumn
                {
                    Header="Дата утверждения",
                    Path="ACCEPTED_DT",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=10,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header="Оплата",
                    Path="PAYMENT_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=6,
                    Stylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("PAYMENT_FLAG").ToInt() == 1)
                                {
                                    color = HColor.Green;
                                }
                                else
                                {
                                    color = HColor.YellowOrange;
                                }

                                if (!color.IsNullOrEmpty())
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        },
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Разрешение на заказ",
                    Path="ALLOWED_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=6,
                    Stylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("ALLOWED_FLAG").ToInt() == 1)
                                {
                                    color = HColor.Green;
                                }
                                else
                                {
                                    color = HColor.YellowOrange;
                                }

                                if (!color.IsNullOrEmpty())
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        },
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Штанцформы",
                    Path="STAMP_LIST",
                    ColumnType=ColumnTypeRef.String,
                    Width2=40,
                },
                new DataGridHelperColumn
                {
                    Header="Статус",
                    Path="STATUS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=30,
                },
                new DataGridHelperColumn
                {
                    Header="Получена",
                    Path="EXISTS_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Файл техкарты",
                    Path="TECHCARD_PATH",
                    ColumnType=ColumnTypeRef.String,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Размер изделия техкарты",
                    Path="TECHCARD_SIZE",
                    ColumnType=ColumnTypeRef.String,
                    Hidden=true,
                },
            };
            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("_ROWNUMBER");
            Grid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
            Grid.AutoUpdateInterval = 0;
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

            Grid.OnLoadItems = LoadItems;
            Grid.OnSelectItem = selectedItem =>
            {
                FormStatus.Text = "";
                bool existsFlag = selectedItem.CheckGet("EXISTS_FLAG").ToBool();
                bool allowedFlag = selectedItem.CheckGet("ALLOWED_FLAG").ToBool();
                bool paymentFlag = selectedItem.CheckGet("PAYMENT_FLAG").ToBool();
                bool archivedFlag = selectedItem.CheckGet("ARCHIVED_FLAG").ToBool();

                if (existsFlag)
                {
                    SaveButton.IsEnabled = !archivedFlag;
                }
                else
                {
                    SaveButton.IsEnabled = paymentFlag && allowedFlag;
                }
            };
            Grid.OnDblClick = selectedItem =>
            {
                Save();
            };
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

                        if (row.CheckGet("EXISTS_FLAG").ToBool())
                        {
                            color = HColor.Green;
                        }

                        if (row.CheckGet("ARCHIVED_FLAG").ToBool())
                        {
                            color = HColor.Olive;
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
        }

        /// <summary>
        /// Запуск таймера заполнения шаблона загрузки данных
        /// </summary>
        public void RunTemplateTimeoutTimer()
        {
            if (TemplateTimeoutTimer == null)
            {
                TemplateTimeoutTimer = new DispatcherTimer
                {
                    Interval = new TimeSpan(0, 0, 2)
                };

                {
                    var row = new Dictionary<string, string>();
                    row.CheckAdd("TIMEOUT", "2000");
                    row.CheckAdd("DESCRIPTION", "");
                    Central.Stat.TimerAdd("AssortmentList_RunTemplateTimeoutTimer", row);
                }

                TemplateTimeoutTimer.Tick += (s, e) =>
                {
                    // Если введены только один или два символа, ничего не загружаем, ждём следующий
                    if (GridSearch.Text.Length > 2)
                    {
                        Grid.LoadItems();
                    }
                    else if (GridSearch.Text.IsNullOrEmpty())
                    {
                        Grid.LoadItems();
                    }
                    StopTemplateTimeoutTimer();
                };
            }

            if (TemplateTimeoutTimer.IsEnabled)
            {
                TemplateTimeoutTimer.Stop();
            }
            TemplateTimeoutTimer.Start();
        }

        /// <summary>
        /// Остановка таймера заполнения заблона загрузки данных
        /// </summary>
        public void StopTemplateTimeoutTimer()
        {
            if (TemplateTimeoutTimer != null)
            {
                if (TemplateTimeoutTimer.IsEnabled)
                {
                    TemplateTimeoutTimer.Stop();
                }
            }
        }

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        private async void LoadItems()
        {
            string archived = (bool)ShowArchiveCheckBox.IsChecked ? "1" : "0";

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "CuttingStampOrder");
            q.Request.SetParam("Action", "ListTkForOrder");
            q.Request.SetParam("FACTORY_ID", FactoryId.ToString());
            q.Request.SetParam("SEARCH", GridSearch.Text);
            q.Request.SetParam("ARCHIVED", archived);

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
                    var ds = ListDataSet.Create(result, "TECHCARDS");
                    Grid.UpdateItems(ds);
                }
            }
        }

        /// <summary>
        /// Отображение вкладки со списком техкарт
        /// </summary>
        public void Show()
        {
            string title = $"Выбор техкарты для заявки";

            Central.WM.Show(ControlName, title, true, "add", this);
        }

        /// <summary>
        /// Передача выбранной строк в форму заказа оснастки
        /// </summary>
        public void Save()
        {
            var row = Grid.SelectedItem;
            if (row != null)
            {
                var stampOrderFrame = new CuttingStampOrder();
                stampOrderFrame.ReceiverName = ReceiverName;
                stampOrderFrame.FactoryId = FactoryId;
                stampOrderFrame.Create(row);
            }
            
            Close();
        }

        public void CreateRepairKit()
        {
            var stampOrderFrame = new CuttingStampOrder();
            stampOrderFrame.ReceiverName = ReceiverName;
            stampOrderFrame.FactoryId = FactoryId;
            stampOrderFrame.CreateRepairKit();
        }

        /// <summary>
        /// Закрытие формы
        /// </summary>
        public void Close()
        {
            Central.WM.Close(ControlName);
        }

        /// <summary>
        /// Загрузка файла техкарты
        /// </summary>
        private void ShowTechnologicalCard()
        {
            if (Grid.Items != null)
            {
                if (Grid.Items.Count > 0)
                {
                    var path = Grid.SelectedItem.CheckGet("TECHCARD_PATH");
                    if (!string.IsNullOrEmpty(path))
                    {
                        if (File.Exists(path))
                        {
                            Central.OpenFile(path);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Обработка нажатия на кнопку
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonOnClick(object sender, RoutedEventArgs e)
        {
            var b = (Button)sender;
            if (b != null)
            {
                var t = b.Tag.ToString();
                ProcessCommand(t);
            }
        }

        private void GridSearch_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            RunTemplateTimeoutTimer();
        }

        private void ShowArchiveCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }
    }
}
