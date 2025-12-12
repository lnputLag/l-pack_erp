using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Список образцов с линии переработки
    /// </summary>
    public partial class SampleProductionList : UserControl
    {
        public SampleProductionList()
        {
            InitializeComponent();
            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/samples_new/samples1/samples_line";

            //регистрация обработчика клавиатуры
            PreviewKeyDown += ProcessKeyboard;

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            SetDefaults();
            InitGrid();

            UIUtil.ProcessPermissions("[erp]sample", this);
        }

        /// <summary>
        /// данные для таблицы картона для образцов
        /// </summary>
        public ListDataSet SampleDS { get; set; }

        /// <summary>
        /// Назначение примечания: 1 - для ярлыка, 2 - для образца
        /// </summary>
        private int NoteTarget;

        #region Common

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// Название вкладки
        /// </summary>
        public string TabName;
        /// <summary>
        /// Ссылка на страницу документации
        /// </summary>
        private string DocumentationUrl;

        /// <summary>
        /// Обработчики нажатий клавиш
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessKeyboard(object sender, KeyEventArgs e)
        {
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
        /// Деструктор. Остановка вспомогательных процессов при закрытии вкладки
        /// </summary>
        public void Destroy()
        {
            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры гридов
            Grid.Destruct();
        }

        /// <summary>
        /// Вызов справки
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp(DocumentationUrl);
        }

        public void ProcessNavigation()
        {

        }

        #endregion

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            FromDate.Text = DateTime.Now.AddDays(-28).ToString("dd.MM.yyyy");
            ToDate.Text = DateTime.Now.AddDays(14).ToString("dd.MM.yyyy");

            SearchText.Text = "";

            // Список статусов для фильтра
            var statusList = new Dictionary<string, string>()
            {
                { "-1", "Все" },
                { "1", "В работе" },
                { "4", "Изготовленные" },
                { "5", "Переданные" },
                { "6", "Полученные" },
                { "7", "Отгруженные" },
                { "8", "Отмененные" },
                { "9", "Утилизированные" },
            };
            SampleStatus.Items = statusList;
            SampleStatus.SelectedItem = statusList.FirstOrDefault((x) => x.Key == "1");
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="obj">сообщение</param>
        private void ProcessMessages(ItemMessage obj)
        {
            if (obj.ReceiverGroup.IndexOf("PreproductionSample") > -1)
            {
                if (obj.ReceiverName.IndexOf(TabName) > -1)
                {
                    switch (obj.Action)
                    {
                        case "Refresh":
                            Grid.LoadItems();
                            break;

                        case "SaveNote":
                            var answer = (Dictionary<string, string>)obj.ContextObject;
                            if (NoteTarget == 1)
                            {
                                MakeSampleLabel(answer);
                            }
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        public void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="№",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=60,
                },
                new DataGridHelperColumn
                {
                    Header="Дата создания",
                    Path="DT_CREATED",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=90,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="ARTIKUL",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=120,
                },
                new DataGridHelperColumn
                {
                    Header="Образец",
                    Path="SAMPLE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=400,
                },
                new DataGridHelperColumn
                {
                    Header="Количество",
                    Path="QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=60,
                },
                new DataGridHelperColumn
                {
                    Header="Дата отгрузки",
                    Path="SHIPMENT_DT",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=90,
                },
                new DataGridHelperColumn
                {
                    Header="ПЗ",
                    Path="PZ_NUM",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=90,
                },
                new DataGridHelperColumn
                {
                    Header="Станок",
                    Path="LINE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=70,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание ПЗ",
                    Path="PZ_NOTE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=200,
                },
                new DataGridHelperColumn
                {
                    Header="Статус",
                    Path="STATUS",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=90,
                },
                new DataGridHelperColumn
                {
                    Header="Плановое время начала работы станка",
                    Path="PLAN_START_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    MinWidth=50,
                    MaxWidth=120,
                    Format="dd.MM.yyyy HH:mm:ss",
                },
                new DataGridHelperColumn
                {
                    Header="Время окончания ПЗ",
                    Path="PZ_END_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    MinWidth=50,
                    MaxWidth=120,
                    Format="dd.MM.yyyy HH:mm:ss",
                },
                new DataGridHelperColumn
                {
                    Header="На складе",
                    Path="STOCK_QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=60,
                },
                new DataGridHelperColumn
                {
                    Header="Место на складе",
                    Path="STOCK_PLACE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=40,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Технолог",
                    Path="TECHNOLOG_NAME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=120,
                },
                new DataGridHelperColumn
                {
                    Header="Сообщений от клиента",
                    Path="UNREAD_MSG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=30,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row=>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (row["UNREAD_MSG"].ToInt() > 0)
                                {
                                    color = HColor.Red;
                                }
                                else if (row["CHAT_MSG"].ToInt() > 0)
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
                    Header="Сообщений от коллег",
                    Path="UNREAD_MESSAGE_QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=30,
                    Stylers=new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row=>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (row["UNREAD_MESSAGE_QTY"].ToInt() > 0)
                                {
                                    color = HColor.Red;
                                }
                                else if (row["MESSAGE_QTY"].ToInt() > 0)
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
                    Header="Доставка",
                    Path="DELIVERY",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=30,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header="Ячейка",
                    Path="CELL_NUM",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=30,
                },
                new DataGridHelperColumn
                {
                    Header="Менеджер",
                    Path="MANAGER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=30,
                    MaxWidth=150,
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
                    Header="Код Статуса",
                    Path="STATUS_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Код товара",
                    Path="PRODUCT_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ID ПЗ",
                    Path="ID_PZ",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Есть ПЗ",
                    Path="PZ_IS",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Путь к техкарте",
                    Path="PATHTK",
                    ColumnType=ColumnTypeRef.String,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ID внутреннего чата",
                    Path="CHAT_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            Grid.SetColumns(columns);

            // Раскраска строк
            Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                // Цвета фона строк
                {
                    StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";
                        int currentStatus = row["STATUS_ID"].ToInt();

                        if ((row["ID_PZ"].ToInt() == 0) && (row["PZ_IS"].ToInt() > 0) && (currentStatus == SampleStates.InWork))
                        {
                            color=HColor.Pink;
                        }

                        if ((currentStatus == SampleStates.Produced) || (currentStatus == SampleStates.Received) || (currentStatus == SampleStates.Shipped) || (currentStatus == SampleStates.Transferred))
                        {
                            color = HColor.Green;
                        }
                        else if ((currentStatus == SampleStates.Rejected) || (currentStatus == SampleStates.Utilized))
                        {
                            color = HColor.Red;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };

            Grid.SearchText = SearchText;
            Grid.Init();

            // контекстное меню
            Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
            {
                {
                    "ChangeStatus",
                    new DataGridContextMenuItem()
                    {
                        Header="Отметить статус",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                        },
                        Items=new Dictionary<string, DataGridContextMenuItem>()
                        {
                            { "SetRejected",
                                new DataGridContextMenuItem()
                                {
                                    Header ="Отменен",
                                    Action=() =>
                                    {
                                        UpdateStatus(SampleStates.Rejected);
                                    }
                                }
                            },
                            { "SetToWork",
                                new DataGridContextMenuItem()
                                {
                                    Header ="В работу",
                                    Action=() =>
                                    {
                                        UpdateStatus(SampleStates.InWork);
                                    }
                                }
                            },
                            { "SetCompleted",
                                new DataGridContextMenuItem()
                                {
                                    Header ="Изготовлен",
                                    Action=() =>
                                    {
                                        UpdateStatus(SampleStates.Produced);
                                    }
                                }
                            },
                            { "SetUtilized",
                                new DataGridContextMenuItem()
                                {
                                    Header ="Утилизирован",
                                    Action=() =>
                                    {
                                        UpdateStatus(SampleStates.Utilized);
                                    }
                                }
                            },
                            { "SetTransferred",
                                new DataGridContextMenuItem()
                                {
                                    Header ="Передан",
                                    Action=() =>
                                    {
                                        UpdateStatus(SampleStates.Transferred);
                                    }
                                }
                            },
                        }
                    }
                },
                { "Separator1", new DataGridContextMenuItem() {
                    Header="-",
                }},
                { "OpenChat",
                    new DataGridContextMenuItem()
                    {
                        Header="Открыть чат по образцу",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                            OpenChat(0);
                        }
                    }
                },
                { "OpenInnerChat",
                    new DataGridContextMenuItem()
                    {
                        Header="Открыть внутренний чат",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                            OpenChat(1);
                        }
                    }
                },
                { "BindConvertingTask",
                    new DataGridContextMenuItem()
                    {
                        Header="Привязать ПЗ",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                            BindConvertingTask();
                        }
                    }
                },
                { "ChangeCellNum",
                    new DataGridContextMenuItem()
                    {
                        Header="Изменить номер ячейки",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                            ChangeCellNum();
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
                },
            };

            //данные грида
            Grid.OnLoadItems = LoadItems;
            Grid.OnFilterItems = FilterItems;
            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateActions(selectedItem);
                }
            };
            // Двойное нажатие - просмотр или редактирование
            Grid.OnDblClick = (Dictionary<string, string> selectedItem) =>
            {
                SampleEdit(selectedItem.CheckGet("ID").ToInt());
            };

            Grid.Run();

            //фокус ввода           
            Grid.Focus();
        }

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        private async void LoadItems()
        {
            GridToolbar.IsEnabled = false;
            Grid.ShowSplash();
            bool resume = true;

            if (resume)
            {
                var f = FromDate.Text.ToDateTime();
                var t = ToDate.Text.ToDateTime();
                if (DateTime.Compare(f, t) > 0)
                {
                    var msg = "Дата начала не должна быть больше даты окончания.";
                    var d = new DialogWindow($"{msg}", "Проверка данных");
                    d.ShowDialog();
                    resume = false;
                }
            }

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "Samples");
                q.Request.SetParam("Action", "ListProduction");
                q.Request.SetParam("FromDate", FromDate.Text);
                q.Request.SetParam("ToDate", ToDate.Text);

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
                        SampleDS = ListDataSet.Create(result, "SAMPLES");
                        Grid.UpdateItems(SampleDS);
                    }
                    RefreshButton.Style = (Style)RefreshButton.TryFindResource("Button");
                }
            }

            GridToolbar.IsEnabled = true;
            Grid.HideSplash();
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
                    bool doFilteringByStatus = false;
                    int status = -1;
                    if (SampleStatus.SelectedItem.Key != null)
                    {
                        status = SampleStatus.SelectedItem.Key.ToInt();
                        doFilteringByStatus = true;
                    }

                    if (doFilteringByStatus)
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in Grid.GridItems)
                        {
                            bool includeByStatus = true;

                            if (doFilteringByStatus)
                            {
                                int statusId = row.CheckGet("STATUS_ID").ToInt();
                                includeByStatus = false;

                                switch (status)
                                {
                                    // Все, исключая отклоненные и утилизированные
                                    case -1:
                                        if ((statusId != SampleStates.Rejected) && (statusId != SampleStates.Utilized))
                                        {
                                            includeByStatus = true;
                                        }
                                        break;
                                    // В работе
                                    case 1:
                                        if (statusId == SampleStates.InWork)
                                        {
                                            includeByStatus = true;
                                        }
                                        break;
                                    //  Изготовленные
                                    case 4:
                                        if (statusId == SampleStates.Produced)
                                        {
                                            includeByStatus = true;
                                        }
                                        break;
                                    // Переданные
                                    case 5:
                                        if (statusId == SampleStates.Transferred)
                                        {
                                            includeByStatus = true;
                                        }
                                        break;
                                    // Полученные
                                    case 6:
                                        if (statusId == SampleStates.Received)
                                        {
                                            includeByStatus = true;
                                        }
                                        break;
                                    // Отгруженные
                                    case 7:
                                        if (statusId == SampleStates.Shipped)
                                        {
                                            includeByStatus = true;
                                        }
                                        break;
                                    // Отмененные
                                    case 8:
                                        if (statusId == SampleStates.Rejected)
                                        {
                                            includeByStatus = true;
                                        }
                                        break;
                                    // Утилизированные
                                    case 9:
                                        if (statusId == SampleStates.Utilized)
                                        {
                                            includeByStatus = true;
                                        }
                                        break;
                                }
                            }

                            if (includeByStatus)
                            {
                                items.Add(row);
                            }

                        }
                        Grid.GridItems = items;
                    }
                }
            }
        }

        /// <summary>
        /// обновление методов работы с выбранной записью
        /// </summary>
        /// <param name="selectedItem">выбранная запись</param>
        private void UpdateActions(Dictionary<string, string> selectedItem)
        {
            SelectedItem = selectedItem;
            int currentStatus = SelectedItem.CheckGet("STATUS_ID").ToInt();
            Grid.Menu["BindConvertingTask"].Enabled = currentStatus == SampleStates.InWork;
        }

        /// <summary>
        /// Обновление статуса образца
        /// </summary>
        /// <param name="newStatus"></param>
        private async void UpdateStatus(int newStatus)
        {
            if (SelectedItem != null)
            {
                var undoStatus = "0";
                //Если старый статус Утилизировано, а новый Передан, то восстанавливаем статус Получен
                if (newStatus == 7)
                {
                    int oldStatus = SelectedItem.CheckGet("STATUS_ID").ToInt();
                    if (oldStatus == 6)
                    {
                        newStatus = 4;
                        undoStatus = "1";
                    }
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "Samples");
                q.Request.SetParam("Action", "UpdateStatus");
                q.Request.SetParam("SAMPLE_ID", SelectedItem.CheckGet("ID"));
                q.Request.SetParam("STATUS", newStatus.ToString());
                q.Request.SetParam("UNDO_STATUS", undoStatus);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        if (result.Count > 0)
                        {
                            Grid.LoadItems();
                        }
                    }
                }

            }
        }

        /// <summary>
        /// Изменение номера ячейки хранения образцов на СГП
        /// </summary>
        private void ChangeCellNum()
        {
            if (SelectedItem != null)
            {
                var itemId = SelectedItem.CheckGet("ID").ToInt();
                if (itemId > 0)
                {
                    var cellNumWindow = new SampleCellNum();
                    cellNumWindow.Edit(itemId);
                }
            }
        }

        /// <summary>
        /// Открытие вкладки с чатом по образцу
        /// </summary>
        private void OpenChat(int chatType = 0)
        {
            if (SelectedItem != null)
            {
                var chatFrame = new SampleChat();
                chatFrame.ChatType = chatType;
                chatFrame.ChatId = SelectedItem.CheckGet("CHAT_ID").ToInt();
                chatFrame.ObjectId = SelectedItem.CheckGet("ID").ToInt();
                chatFrame.ReceiverName = TabName;
                chatFrame.RawMissingFlag = 0;
                chatFrame.Edit();
            }
        }

        private void BindConvertingTask()
        {
            if (SelectedItem != null)
            {
                var taskFrame = new SampleProductionListManual();
                taskFrame.ReceiverName = TabName;
                taskFrame.SampleId = SelectedItem.CheckGet("ID").ToInt();
                taskFrame.SelectedTaskId = SelectedItem.CheckGet("ID_PZ").ToInt();
                taskFrame.ProductId = SelectedItem.CheckGet("PRODUCT_ID").ToInt();
                taskFrame.Show();
            }
        }

        private void SampleStatus_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void SampleEdit(int sampleId)
        {
            var sampleForm = new SampleProduction();
            sampleForm.ReceiverName = TabName;
            sampleForm.Edit(sampleId);
        }

        /// <summary>
        /// Формирования ярлыка на образец
        /// </summary>
        /// <param name="technologsNote">Примечание технолога</param>
        private async void MakeSampleLabel(Dictionary<string, string> p)
        {
            NoteTarget = 0;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "GetTaskReport");

            q.Request.SetParam("ID_LIST", p.CheckGet("ID"));

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "SampleReport");
                    if (ds.Items.Count > 0)
                    {
                        var item = ds.Items[0];
                        int deliveryId = item.CheckGet("DELIVERY_ID").ToInt();
                        string place = "";
                        switch (deliveryId)
                        {
                            case 0:
                                place = "СГП";
                                break;

                            case 1:
                                place = "ОПП";
                                break;

                            case 2:
                                place = "Московский офис";
                                break;

                            case 3:
                                place = "Транспортная компания";
                                break;

                            case 4:
                                place = "Рег. представитель";
                                break;
                        }
                        item.Add("PLACE", place);
                        item.Add("TECHNOLOG_NOTE", p.CheckGet("NOTE"));
                        item.Add("SHOW_CARDBOARD", p.CheckGet("SHOW_CARDBOARD"));
                        var sampleLabel = new SampleTaskLabel();
                        sampleLabel.SampleItem = item;

                        sampleLabel.Make();
                    }
                }
            }
        }

        /// <summary>
        /// Формирование технического задания на образец
        /// </summary>
        private async void MakeTaskReport()
        {
            if (SelectedItem != null)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "Samples");
                q.Request.SetParam("Action", "GetTaskReport");

                q.Request.SetParam("ID_LIST", SelectedItem.CheckGet("ID"));

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "SampleReport");
                        if (ds.Items.Count > 0)
                        {
                            var item = ds.Items[0];
                            var taskReporter = new SampleTaskReporter();
                            taskReporter.SampleItem = item;
                            taskReporter.Make();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// При сммене дат меняет стиль кнопки обновления данных
        /// </summary>
        private void DateChanged()
        {
            RefreshButton.Style = (Style)RefreshButton.TryFindResource("FButtonPrimary");
        }

        /// <summary>
        /// Открывает файл техкарты
        /// </summary>
        private void OpenTechMap()
        {
            bool success = true;
            var path = SelectedItem.CheckGet("PATHTK");
            if (!string.IsNullOrEmpty(path))
            {
                if (File.Exists(path))
                {
                    Central.OpenFile(path);
                }
                else
                {
                    success = false;
                }
            }
            else
            {
                success = false;
            }

            if (!success)
            {
                var dw = new DialogWindow("Файл техкарты не найден", "Файл не найден");
                dw.ShowDialog();
            }
        }

        /// <summary>
        /// Открывает вкладку с историей изменения образца
        /// </summary>
        private void ShowHistory()
        {
            var historyFrame = new SampleHistory();
            historyFrame.SampleId = SelectedItem.CheckGet("ID").ToInt();
            historyFrame.ReceiverName = TabName;
            historyFrame.Show();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            SampleEdit(0);
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem != null)
            {
                SampleEdit(SelectedItem.CheckGet("ID").ToInt());
            }
        }

        private void TechnologicalMap_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem != null)
            {
                OpenTechMap();
            }
        }

        private void SampleTaskReport_Click(object sender, RoutedEventArgs e)
        {
            MakeTaskReport();
        }

        private void SampleLabelReport_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem != null)
            {
                NoteTarget = 1;
                var sampleNote = new SampleNote();
                sampleNote.ReceiverName = TabName;
                var values = new Dictionary<string, string>()
                {
                    { "ID", SelectedItem.CheckGet("ID") },
                    { "NOTE", "" },
                    { "SHOW_CARDBOARD", "0"},
                };
                sampleNote.CardboardCheckBoxBlock.Visibility = Visibility.Collapsed;
                sampleNote.Edit(values);
            }
        }

        private void FromDate_TextChanged(object sender, TextChangedEventArgs e)
        {
            DateChanged();
        }

        private void ToDate_TextChanged(object sender, TextChangedEventArgs e)
        {
            DateChanged();
        }

        private void PrintMenuButton_Click(object sender, RoutedEventArgs e)
        {
            PrintContextMenu.IsOpen = true;
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }
    }
}
