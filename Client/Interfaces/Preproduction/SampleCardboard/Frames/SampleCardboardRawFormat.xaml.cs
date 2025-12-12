using Client.Assets.HighLighters;
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
    /// Управление доступными форматами листов для образцов. 
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleCardboardRawFormat : UserControl
    {
        public SampleCardboardRawFormat()
        {
            InitializeComponent();

            InitGrid();
            ProcessPermissions();
            SetDefaults();
        }

        /// <summary>
        /// ИД картона
        /// </summary>
        public int CardboardId;
        /// <summary>
        /// Номер картона для отображения на вкладке
        /// </summary>
        public int CardboardNum;
        /// <summary>
        /// Данные по сырью из БД
        /// </summary>
        ListDataSet SampleRawDS { get; set; }
        /// <summary>
        /// Выбранная строка в списке асортимента картона для образцов
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }
        /// <summary>
        /// Имя вкладки
        /// </summary>
        public string TabName;
        /// <summary>
        /// Право на выполнение специальных действий
        /// </summary>
        public bool MasterRights;

        /// <summary>
        /// Остановка вспомогательных процессов при закрытии вкладки
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Preproduction",
                ReceiverName = "SampleCardboard",
                SenderName = TabName,
                Action = "Closed",
            });
            Grid.Destruct();
        }

        public void SetDefaults()
        {
            FormStatus.Text = "";
        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Если пользователь имеет спецправа, включаем режим мастера
            var mode = Central.Navigator.GetRoleLevel("[erp]sample_cardboard");
            MasterRights = mode == Role.AccessMode.Special;
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
                    MinWidth=70,
                    MaxWidth=70,
                },
                new DataGridHelperColumn
                {
                    Header="Формат",
                    Path="FORMAT",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=70,
                    MaxWidth=70,
                },
                new DataGridHelperColumn
                {
                    Header="Название сырья",
                    Path="SAMPLE_RAW_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=70,
                    MaxWidth=300,
                },
                new DataGridHelperColumn
                {
                    Header="ИД сырья",
                    Path="SAMPLE_RAW_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=70,
                    MaxWidth=90,
                },
                new DataGridHelperColumn
                {
                    Header="Признак архивный",
                    Path="TASK_IS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Количество сделанных ПЗ",
                    Path="TASK_QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            Grid.SetColumns(columns);
            // Раскраска строк
            Grid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        // Есть задания на этот формат
                        if (row.CheckGet("TASK_IS").ToInt() == 1)
                        {
                            color=HColor.Green;
                        }

                        if (!color.IsNullOrEmpty())
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                }
            };

            Grid.Init();
            Grid.AutoUpdateInterval = 0;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateActions(selectedItem);
                }
            };

            Grid.OnLoadItems = LoadItems;
        }

        private async void LoadItems()
        {
            Grid.ShowSplash();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "SampleCardboards");
            q.Request.SetParam("Action", "ListFormat");
            q.Request.SetParam("IDC", CardboardId.ToString());

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
                if (result.Count > 0)
                {
                    SampleRawDS = ListDataSet.Create(result, "SampleRaw");
                    Grid.UpdateItems(SampleRawDS);
                    FormStatus.Text = "";
                }
            }

            Grid.HideSplash();
        }

        public void Edit(Dictionary<string,string> p)
        {
            CardboardId = p.CheckGet("ID").ToInt();
            CardboardNum = p.CheckGet("NUM").ToInt();
            if (CardboardId > 0)
            {
                Show();
            }
        }

        /// <summary>
        /// Отображение вкладки
        /// </summary>
        public void Show()
        {
            TabName = $"RawFormat_{CardboardId}";
            Central.WM.AddTab(TabName, $"Форматы сырья картона {CardboardNum}", true, "add", this);
            LoadItems();
        }

        public void Close()
        {
            Central.WM.RemoveTab(TabName);

            Destroy();
        }

        private void UpdateActions(Dictionary<string, string> selectedItem)
        {
            SelectedItem = selectedItem;

            // Если нет спецправ на картон для образцов, кнопки управления всегда недоступны
            if (MasterRights)
            {
                string rawName = selectedItem.CheckGet("SAMPLE_RAW_NAME");
                // Блокируем кнопку Создать, если заполнено имя сырья на выбранном формате
                AddButton.IsEnabled = string.IsNullOrEmpty(rawName);
                // Блокируем кнопку архивации, если не зяполнено имя сырья на выбранном формате
                ArchiveButton.IsEnabled = !string.IsNullOrEmpty(rawName);
            }
            else
            {
                AddButton.IsEnabled = false;
                ArchiveButton.IsEnabled = false;
            }
        }

        private async void AddRawFormat()
        {
            Grid.ShowSplash();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "SampleCardboards");
            q.Request.SetParam("Action", "CreateTovar");
            q.Request.SetParam("IDC", CardboardId.ToString());
            q.Request.SetParam("FORMAT", SelectedItem.CheckGet("FORMAT"));

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
                    if (result.ContainsKey("ITEMS"))
                    {
                        Grid.LoadItems();
                    }
                }
            }
            else if (q.Answer.Error.Code == 145)
            {
                FormStatus.Text = q.Answer.Error.Message;
            }
            Grid.HideSplash();
        }

        private async void SetArchive()
        {
            Grid.ShowSplash();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "SampleCardboards");
            q.Request.SetParam("Action", "SetRawArchive");
            q.Request.SetParam("RAW_ID", SelectedItem.CheckGet("SAMPLE_RAW_ID"));

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
                    if (result.ContainsKey("ITEMS"))
                    {
                        Grid.LoadItems();
                    }
                }
            }
            else if (q.Answer.Error.Code == 145)
            {
                FormStatus.Text = q.Answer.Error.Message;
            }
            Grid.HideSplash();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem != null)
            {
                AddRawFormat();
            }
        }

        private void ArchiveButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem != null)
            {
                SetArchive();
            }

        }
    }
}
