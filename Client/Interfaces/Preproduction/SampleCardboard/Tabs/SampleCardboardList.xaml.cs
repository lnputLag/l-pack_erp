using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Вкладка с таблицей картона для образцов
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleCardboardList : UserControl
    {
        /// <summary>
        /// Инициализация
        /// </summary>
        public SampleCardboardList()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            InitCardboardGrid();
            InitPreformGrid();
            ProcessPermissions();
            
            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/samples_new/carton_samples";
        }

        public string RoleName = "[erp]sample_cardboard";

        /// <summary>
        /// данные для таблицы картона для образцов
        /// </summary>
        public ListDataSet CardboardDS { get; set; }

        public ListDataSet PreformDS { get; set; }

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        private Dictionary<string, string> CardboardSelectedItem { get; set; }

        /// <summary>
        /// Право на выполнение специальных действий
        /// </summary>
        public bool MasterRights;

        /// <summary>
        /// Идентификатор записи, которую надо выбрать после завершения загрузки
        /// </summary>
        private int ItemIdForSelect = 0;

        #region Common

        /// <summary>
        /// Имя вкладки
        /// </summary>
        public string TabName;
        /// <summary>
        /// Ссылка на страницу документации
        /// </summary>
        private string DocumentationUrl;

        /// <summary>
        /// обработка ввода с клавиатуры (роли)
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.F5:
                    CardboardGrid.LoadItems();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

                case Key.Home:
                    CardboardGrid.SetSelectToFirstRow();
                    e.Handled = true;
                    break;

                case Key.End:
                    CardboardGrid.SetSelectToLastRow();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// Деструктор компонента
        /// </summary>
        public void Destroy()
        {
            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры гридов
            CardboardGrid.Destruct();
        }

        /// <summary>
        /// Показывает страницу справки
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp(DocumentationUrl);
        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Если пользователь имеет спецправа, включаем режим мастера
            var mode = Central.Navigator.GetRoleLevel(this.RoleName);
            switch (mode)
            {
                case Role.AccessMode.Special:
                    MasterRights = true;
                    break;

                default:
                    MasterRights = false;
                    break;
            }

            List<Button> buttons = UIUtil.GetVisualChilds<Button>(this.Content as DependencyObject);
            if (buttons != null && buttons.Count > 0)
            {
                foreach (var button in buttons)
                {
                    var buttonTagList = UIUtil.GetTagList(button);
                    var accessMode = Acl.FindTagAccessMode(buttonTagList);
                    if (accessMode > mode)
                    {
                        button.IsEnabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// обработчик системы навигации по URL
        /// </summary>
        public void ProcessNavigation()
        {
            var p = Central.Navigator.Address.Params;
            var cardboardId = p.CheckGet("id").ToInt();
            if (cardboardId > 0)
            {
                AllCardboardCheckBox.IsChecked = true;
                //Ели данные в таблицу загружены, сделаем выбор строки. Если не загружены, сохраним значение для отложенного выбора
                if (CardboardGrid.GridItems != null)
                {
                    if (CardboardGrid.GridItems.Count > 0)
                    {
                        CardboardGrid.SelectRowByKey(cardboardId, "ID");
                    }
                }
                else
                {
                    ItemIdForSelect = cardboardId;
                }
            }
        }

        #endregion

        /// <summary>
        /// Обработчик сообщений
        /// </summary>
        /// <param name="m">сообщение</param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.IndexOf("Preproduction") > -1)
            {
                if (m.ReceiverName.IndexOf(TabName) > -1)
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            CardboardGrid.LoadItems();
                            break;
                        case "TaskCreated":
                            CardboardGrid.LoadItems();
                            break;
                    }

                }
                // Если приняты заготовки, также обновляем таблицу
                if (m.SenderName.IndexOf("ReceivePreforms") > -1)
                {
                    if (m.Action == "Refresh")
                    {
                        CardboardGrid.LoadItems();
                    }
                }
            }
        }

        /// <summary>
        /// Инициализация таблицы списка ассортимента картона
        /// </summary>
        public void InitCardboardGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=70,
                },
                new DataGridHelperColumn
                {
                    Header="Номер",
                    Path="NUM",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=70,
                },
                new DataGridHelperColumn
                {
                    Header="Картон",
                    Path="NAME_CARTON",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=100,
                    MaxWidth=200,
                },
                new DataGridHelperColumn
                {
                    Header="Тип",
                    Path="COMPOSITION",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=70,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("COMPOSITION_TYPE").ToInt() > 2)
                                {
                                    color = HColor.Gray;
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        },
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Количество",
                    Path="QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=80,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if ((row.CheckGet("ARCHIVE_FLAG").ToInt() == 0) && (row.CheckGet("QTY").ToInt() < 6))
                                {
                                    color = HColor.Blue;
                                }

                                // Если картон лежит на приемке
                                if (row.CheckGet("IN_CELL").ToInt() == 1)
                                {
                                    color = HColor.Green;
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        },
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Масса",
                    Path="WEIGHT",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="ПЗ на картон",
                    Path="PZ_IS",
                    ColumnType=ColumnTypeRef.Boolean,
                    MinWidth=40,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="Номер ПЗ",
                    Path="NUM_PZ",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=40,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Последнее ПЗ",
                    Path="MAX_DATA",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=40,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header=" ",
                    Path="_",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=20,
                    MaxWidth=1500,
                },
                new DataGridHelperColumn
                {
                    Header="Профиль",
                    Path="PROFILE_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Марка",
                    Path="MARK_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Цвет",
                    Path="OUTER_COLOR_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Цвет",
                    Path="COMPOSITION_TYPE",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="В ячейке на приемку",
                    Path="IN_CELL",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Архивный",
                    Path="ARCHIVE_FLAG",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            CardboardGrid.SetColumns(columns);

            // Раскраска строк
            // Дата годности - хранение не больше 30 дней
            var expiredDate = DateTime.Now.Date.AddDays(-30);
            CardboardGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";
                        if (row.CheckGet("QTY").ToInt() > 0)
                        {
                            // Дата постеднего ПЗ раньше даты годности
                            if (!string.IsNullOrEmpty(row["MAX_DATA"]))
                            {
                                var lastTaskDate = row["MAX_DATA"].ToDateTime();
                                if (DateTime.Compare(lastTaskDate, expiredDate) < 0)
                                {
                                    color = HColor.Yellow;
                                }
                            }
                        }

                        if (row.CheckGet("ARCHIVE_FLAG").ToInt() > 0)
                        {
                            color = HColor.Pink;
                        }

                        if (!color.IsNullOrEmpty())
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                }
            };


            CardboardGrid.SearchText = SearchText;
            CardboardGrid.Init();

            // контекстное меню
            CardboardGrid.Menu = new Dictionary<string, DataGridContextMenuItem>()
            {
                { "PrintLabel",
                    new DataGridContextMenuItem()
                    {
                        Header="Печатать ярлык",
                        Action=() =>
                        {
                            PrintLabel();
                        }
                    }
                },
                { "ShowHistory",
                    new DataGridContextMenuItem()
                    {
                        Header="История изменений",
                        Action=() =>
                        {
                            ShowHistory();
                        }
                    }
                }
            };

            //данные грида
            CardboardGrid.OnLoadItems = LoadCardboardItems;
            CardboardGrid.OnFilterItems = FilterCardboardItems;
            CardboardGrid.Run();

            //при выборе строки в гриде, обновляются актуальные действия для записи
            CardboardGrid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    CardboardUpdateActions(selectedItem);
                }
            };

            //фокус ввода           
            CardboardGrid.Focus();
        }

        /// <summary>
        /// Инициализация таблицы с листами картона в наличии
        /// </summary>
        public void InitPreformGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Длина",
                    Path="LENGTH",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Ширина",
                    Path="WIDTH",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Количество",
                    Path="QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="В резерве",
                    Path="RESERVE_QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Хранение",
                    Path="RACK_PLACE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание",
                    Path="NOTE_CARTON",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Дата производства",
                    Path="END_DTTM",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Номер задания",
                    Path="PZ_NUM",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header=" ",
                    Path="_",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=20,
                    MaxWidth=1500,
                },
                new DataGridHelperColumn
                {
                    Header="ИД изделия",
                    Path="PRODUCT_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            PreformGrid.SetColumns(columns);
            PreformGrid.Init();
            PreformGrid.AutoUpdateInterval = 0;
        }

        /// <summary>
        /// Загрузка данных из БД
        /// </summary>
        public async void LoadCardboardItems()
        {
            CardboardGridToolbar.IsEnabled = false;
            CardboardGrid.ShowSplash();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "SampleCardboards");
            q.Request.SetParam("Action", "ListRef");

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result.Count > 0)
                {
                    CardboardDS = ListDataSet.Create(result, "SampleCardboards");
                    CardboardGrid.UpdateItems(CardboardDS);

                    var ProfileDS = ListDataSet.Create(result, "Profiles");

                    var list = new Dictionary<string, string>();
                    list.Add("0", "");
                    list.AddRange<string, string>(ProfileDS.GetItemsList("ID", "NAME"));
                    Profile.Items = list;
                    Profile.SetSelectedItemByKey("0");

                    // Если есть ID для отложенного выбора, сделаем выбор
                    if (ItemIdForSelect > 0)
                    {
                        CardboardGrid.SelectRowByKey(ItemIdForSelect, "ID");
                        ItemIdForSelect = 0;
                    }
                }
            }

            CardboardGridToolbar.IsEnabled = true;
            CardboardGrid.HideSplash();
        }

        /// <summary>
        /// Фильтрация списка картона
        /// </summary>
        public void FilterCardboardItems()
        {
            if (CardboardGrid.GridItems != null)
            {
                if (CardboardGrid.GridItems.Count > 0)
                {
                    bool allRecords = (bool)AllCardboardCheckBox.IsChecked;

                    int profileId = Profile.SelectedItem.Key.ToInt();

                    var list = new List<Dictionary<string, string>>();
                    foreach(var item in CardboardGrid.GridItems)
                    {
                        bool include = true;
                        if (!allRecords && (item.CheckGet("QTY").ToInt() == 0))
                        {
                            include = false;
                        }

                        if ((profileId > 0) && (profileId != item.CheckGet("PROFILE_ID").ToInt()))
                        {
                            include = false;
                        }

                        if (include)
                        {
                            list.Add(item);
                        }
                    }

                    CardboardGrid.GridItems = list;
                }
            }
        }

        /// <summary>
        /// Загрузка данных в таблицу листов
        /// </summary>
        public async void LoadSamplePreformsItems()
        {
            PreformGridToolbar.IsEnabled = false;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "SampleCardboards");
            q.Request.SetParam("Action", "List");
            q.Request.SetParam("IDC", CardboardSelectedItem.CheckGet("ID"));

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result.Count > 0)
                {
                    EditButton.IsEnabled = true;
                    var PreformsDS = ListDataSet.Create(result, "SamplePreforms");
                    PreformGrid.UpdateItems(PreformsDS);
                    if (PreformsDS.Items.Count == 0)
                    {
                        EditButton.IsEnabled = false;
                    }
                }
            }

            PreformGridToolbar.IsEnabled = true;
        }

        /// <summary>
        /// обновление методов работы с выбранной записью
        /// </summary>
        /// <param name="selectedItem"></param>
        public void CardboardUpdateActions(Dictionary<string, string> selectedItem)
        {
            CardboardSelectedItem = selectedItem;
            if (CardboardSelectedItem != null)
            {
                // Кнопка добавления ПЗ доступна только для мастера и если есть ПЗ с таким картоном
                CreatePZButton.IsEnabled = MasterRights && CardboardSelectedItem.CheckGet("PZ_IS").ToBool();
                LoadSamplePreformsItems();

                ProcessPermissions();
            }
        }

        /// <summary>
        /// Формирование и вывод изображения с номером картона
        /// </summary>
        public void PrintLabel()
        {
            // Выводим ярлык для печати в графический файл
            var imageGen = new TextImageGenerator(240, "Arial", 40);
            var bitmap = imageGen.CreateBitmap(CardboardSelectedItem.CheckGet("NUM"));
            bitmap.RotateFlip(System.Drawing.RotateFlipType.Rotate90FlipNone);

            var path = Path.GetTempPath();
            var fileName = Path.Combine(path, "cardboard_label.jpg");
            bitmap.Save(fileName, System.Drawing.Imaging.ImageFormat.Jpeg);
            Central.OpenFile(fileName);

        }

        /// <summary>
        /// Открывает вкладку с историей изменения картона для образцов
        /// </summary>
        public void ShowHistory()
        {
            var historyForm = new SampleCardboardHistory();
            historyForm.CardboardId = CardboardSelectedItem.CheckGet("ID").ToInt();
            historyForm.Show();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку справки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку обновления
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            CardboardGrid.LoadItems();
        }

        /// <summary>
        /// Обработчик нажатия на чекбокс показа всех образцов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AllSamplesCheckBox_Click(object sender, RoutedEventArgs e)
        {
            CardboardGrid.UpdateItems();
        }

        /// <summary>
        /// Вызов фрейма создания нового ПЗ на картон
        /// </summary>
        private void CreateNewTask()
        {
            var idc = CardboardSelectedItem.CheckGet("ID").ToInt();
            if (idc > 0)
            {
                var createPZForm = new SampleCardboardCreateTask();
                createPZForm.CardboardName = CardboardSelectedItem["NAME_CARTON"];
                createPZForm.ReturnTabName = TabName;
                createPZForm.Show(idc);
            }
        }

        /// <summary>
        /// Получение данных для формирования отчета ревизии сырья
        /// </summary>
        private async void ShowRevision()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "SampleCardboards");
            q.Request.SetParam("Action", "ListRevision");

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result.Count > 0)
                {
                    var ds = ListDataSet.Create(result, "CARDBOARD");
                    var report = new SampleRawRevisionReport();
                    report.SampleRawData = ds;
                    report.Make();
                }
            }
        }

        /// <summary>
        /// Обработчик нажатия на кнопку создания ПЗ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreatePZButton_Click(object sender, RoutedEventArgs e)
        {
            if (CardboardSelectedItem != null)
            {
                if (!string.IsNullOrEmpty(CardboardSelectedItem.CheckGet("NUM_PZ")))
                {
                    var dw = new DialogWindow("На выбранный картон уже есть задание. Продолжить?", "Новое задание", "", DialogWindowButtons.YesNo);
                    if ((bool)dw.ShowDialog())
                    {
                        CreateNewTask();
                    }
                }
                else
                {
                    CreateNewTask();
                }
            }
        }

        private void Profile_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CardboardGrid.UpdateItems();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (CardboardSelectedItem != null)
            {
                var idc = CardboardSelectedItem.CheckGet("ID").ToInt();
                if (idc > 0)
                {
                    var addFrame = new SampleCardboardAdd();
                    addFrame.ReceiverName = TabName;
                    addFrame.Edit(idc);
                }
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (PreformGrid.SelectedItem != null)
            {
                var preformId = PreformGrid.SelectedItem.CheckGet("ID").ToInt();
                if (preformId > 0)
                {
                    var editForm = new SampleCardboardEditQty();
                    editForm.ReceiverName = TabName;
                    editForm.Edit(preformId);
                }
            }
        }

        private void FormatButton_Click(object sender, RoutedEventArgs e)
        {
            if (CardboardSelectedItem != null)
            {
                var p = new Dictionary<string, string>();
                p.Add("ID", CardboardSelectedItem.CheckGet("ID"));
                p.Add("NUM", CardboardSelectedItem.CheckGet("ID"));
                var rawFormatTab = new SampleCardboardRawFormat();
                rawFormatTab.Edit(p);
            }
        }

        private void RevisionList_Click(object sender, RoutedEventArgs e)
        {
            ShowRevision();
        }
    }
}
