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
    /// Логика взаимодействия для SampleSelectCardboard.xaml
    /// </summary>
    public partial class SampleSelectCardboard : UserControl
    {
        public SampleSelectCardboard()
        {
            InitializeComponent();

            InitGrid();
            InitRawGrid();
            SetDefaults();
        }

        /// <summary>
        /// ИД профиля картона
        /// </summary>
        private int ProfileId;

        /// <summary>
        /// ИД марки картона
        /// </summary>
        private int MarkId;

        /// <summary>
        /// ИД цвета внешних слоев картона
        /// </summary>
        private int ColorId;
        /// <summary>
        /// ИД картона из заявки
        /// </summary>
        private int CardboardOrderId;
        /// <summary>
        /// Ид образца
        /// </summary>
        public int SampleId;
        /// <summary>
        /// Вкладка, из которой вызвали форму и в которую будет отправлен ответ
        /// </summary>
        public string ReceiverName;

        /// <summary>
        /// Тип ответа: 0 - данные из выбранной строки, 1 - список имён в отмеченных строках
        /// </summary>
        public int AnswerType;

        private bool TechnologMode;

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header=" ",
                    Path="CHECKING",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=30,
                    MaxWidth=40,
                    Editable=true,
                },
                new DataGridHelperColumn
                {
                    Header="№",
                    Path="_ROWNUMBER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Номер картона",
                    Path="CARDBOARD_NUM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="Картон",
                    Path="CARDBOARD_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=200,
                },
                new DataGridHelperColumn
                {
                    Header="Листов в наличии",
                    Path="QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=50,
                },
                new DataGridHelperColumn
                {
                    Header="Листов в резерве",
                    Path="RESERVE_QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=50,
                },
                new DataGridHelperColumn
                {
                    Header="Есть ПЗ",
                    Path="PZ_IS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=35,
                    MaxWidth=35,
                },
                new DataGridHelperColumn
                {
                    Header="Ближайшая отгрузка",
                    Path="SHIPMENT_DTTM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="Тип картона",
                    Path="COMPOSITION_TYPE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Есть ПЗ на картон для образцов",
                    Path="PZ_SMPL_IS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            Grid.SetColumns(columns);
            Grid.AutoUpdateInterval = 0;

            // Раскраска строк
            Grid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";
                        // Совпадает с картоном из заявки
                        if (CardboardOrderId > 0)
                        {
                            if (row.CheckGet("ID").ToInt() == CardboardOrderId)
                            {
                                color = HColor.Blue;
                            }
                        }

                        if (!color.IsNullOrEmpty())
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
                {
                    DataGridHelperColumn.StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        // Редкий и специальные типы
                        if (row.CheckGet("COMPOSITION_TYPE").ToInt() > 2)
                        {
                            color = HColor.OliveFG;
                        }
                        // Есть ПЗ на листы сырья такого картона
                        if (row.CheckGet("PZ_SMPL_IS").ToInt() == 1)
                        {
                            color = HColor.GreenFG;
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

            Grid.OnLoadItems = LoadItems;
            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateActions(selectedItem);
                }
            };
            //двойной клик на строке делает выбор
            Grid.OnDblClick = (Dictionary<string, string> selectedItem) =>
            {
                Save();
            };
            Grid.Run();
        }

        private void InitRawGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Длина",
                    Path="LENGTH",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=60,
                },
                new DataGridHelperColumn
                {
                    Header="Ширина",
                    Path="WIDTH",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=60,
                },
                new DataGridHelperColumn
                {
                    Header="Листов в наличии",
                    Path="QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=60,
                },
                new DataGridHelperColumn
                {
                    Header="Листов в резерве",
                    Path="RESERVE_QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=60,
                },
                new DataGridHelperColumn
                {
                    Header="Место хранения",
                    Path="RACK_PLACE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=30,
                    MaxWidth=50,
                },
            };
            RawGrid.SetColumns(columns);
            RawGrid.AutoUpdateInterval = 0;
            RawGrid.Init();
        }

        private void SetDefaults()
        {
            ProfileId = 0;
            MarkId = 0;
            ColorId = 0;
            CardboardOrderId = 0;
            ReceiverName = "";
            AnswerType = 0;
            SampleId = 0;

            var sampleRights = Central.Navigator.GetRoleLevel("[erp]sample");
            var plannerRights = Central.Navigator.GetRoleLevel("[erp]sample_task_planner");
            // Кнопка выбора доступна только технологам по образцам и планировщикам образцов
            TechnologMode = ((sampleRights == Role.AccessMode.FullAccess)
                || (sampleRights == Role.AccessMode.Special)
                || (plannerRights == Role.AccessMode.Special)
                || (plannerRights == Role.AccessMode.FullAccess));
        }

        private void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "PreproductionSample",
                ReceiverName = "",
                SenderName = "SelectCardboard",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            Grid.Destruct();

            if (!string.IsNullOrEmpty(ReceiverName))
            {
                Central.WM.SetActive(ReceiverName, true);
                ReceiverName = "";
            }

        }

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        private async void LoadItems()
        {
            SelectButton.IsEnabled = false;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "SampleCardboards");
            q.Request.SetParam("Action", "ListForSample");

            q.Request.SetParam("PROFILE_ID", ProfileId.ToString());
            q.Request.SetParam("MARK_ID", MarkId.ToString());
            q.Request.SetParam("COLOR_ID", ColorId.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "CARDBOARDS");
                    Grid.UpdateItems(ds);
                    if (CardboardOrderId > 0)
                    {
                        Grid.SelectRowByKey(CardboardOrderId);
                        //Grid.SetSelectedItemId(CardboardOrderId);
                    }
                    if (TechnologMode && (ds.Items.Count > 0))
                    {
                        SelectButton.IsEnabled = true;
                    }
                }
            }
        }

        /// <summary>
        /// обновление методов работы с выбранной записью
        /// </summary>
        /// <param name="selectedItem"></param>
        public void UpdateActions(Dictionary<string, string> selectedItem)
        {
            SelectedItem = selectedItem;
            int idc = 0;
            if (SelectedItem != null)
            {
                idc = SelectedItem.CheckGet("ID").ToInt();
            }

            if (idc > 0)
            {
                LoadRawItems();
            }
            else
            {
                RawGrid.ClearItems();
            }
        }

        private async void LoadRawItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "SampleCardboards");
            q.Request.SetParam("Action", "ListRack");

            q.Request.SetParam("IDC", SelectedItem.CheckGet("ID"));

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "SampleCardboard");
                    RawGrid.UpdateItems(ds);
                }
            }

        }

        public void Edit(Dictionary<string, string> p)
        {
            ProfileId = p.CheckGet("PROFILE").ToInt();
            MarkId = p.CheckGet("MARK").ToInt();
            ColorId = p.CheckGet("COLOR").ToInt();
            CardboardOrderId = p.CheckGet("CARDBOARD").ToInt();

            if ((ProfileId > 0) && (MarkId > 0) && (ColorId > 0))
            {
                Grid.LoadItems();
                Show();
            }
        }

        public void Show()
        {
            string title = "Выбор картона для образца";
            Central.WM.AddTab($"SelectCardboard", title, true, "add", this);
        }

        public void Close()
        {
            Central.WM.RemoveTab($"SelectCardboard");
            Destroy();
        }

        private void Save()
        {
            var p = new Dictionary<string, string>();
            if (AnswerType == 0)
            {
                if (Grid.SelectedItem != null)
                {
                    p = Grid.SelectedItem;
                    // Добавляем ИД образца
                    p.CheckAdd("SAMPLE_ID", SampleId.ToString());
                }
            }
            else
            {
                string names = "";
                bool first = true;
                foreach(var row in Grid.Items)
                {
                    if (row.CheckGet("CHECKING").ToBool())
                    {
                        if (first)
                        {
                            names = row["CARDBOARD_NAME"];
                            first = false;
                        }
                        else
                        {
                            names = $"{names}\n{row["CARDBOARD_NAME"]}";
                        }
                    }
                }

                if (string.IsNullOrEmpty(names))
                {
                    names = Grid.SelectedItem.CheckGet("CARDBOARD_NAME");
                }
                p.Add("ALTERNATIVES", names);
            }

            if (p.Count > 0)
            {
                //отправляем сообщение с выбранными данными
                Messenger.Default.Send(new ItemMessage()
                {
                    ReceiverGroup = "PreproductionSample",
                    ReceiverName = ReceiverName,
                    SenderName = "SelectCardboard",
                    Action = "CardboardSelected",
                    ContextObject = p,
                });
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "PreproductionSample",
                    ReceiverName = ReceiverName,
                    SenderName = "SelectCardboard",
                    Action = "CardboardSelected",
                    ContextObject = p,
                });
                Close();
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
    }
}
