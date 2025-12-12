using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Список образцов для учета менеджерами
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleAccountingList : UserControl
    {
        public SampleAccountingList()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            LoadRef();
            SetDefaults();
            InitGrid();
            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/samples_new/account_samples";
            UIUtil.ProcessPermissions("[erp]sample_accounting", this);
        }

        /// <summary>
        /// Имя вкладки
        /// </summary>
        public string TabName;

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        private List<int> ListIds { get; set; }

        /// <summary>
        /// Признак фильтрации по менеджеру: false - по списку из сессии, true - по выбранному в выпадающем списке
        /// </summary>
        private bool SingleManager;

        #region Common

        /// <summary>
        /// Ссылка на страницу документации
        /// </summary>
        private string DocumentationUrl;
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
                            var v = (Dictionary<string, string>)obj.ContextObject;
                            PrintLabels(v);
                            break;
                    }
                }
            }
        }

        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header=" ",
                    Path="CHECKING",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    Editable=true,
                    MinWidth=40,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="№",
                    Path="_ROWNUMBER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=50,
                },
                new DataGridHelperColumn
                {
                    Header="Веб-заявка",
                    Path="WEB_FLAG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=40,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="Дата создания",
                    Path="DT_CREATED",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="Дата изготовления",
                    Path="DT_COMPLITED",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="Тип изготовления",
                    Path="PRODUCTION_TYPE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="Покупатель",
                    Path="CUSTOMER_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=200,
                },
                new DataGridHelperColumn
                {
                    Header="Номер",
                    Path="SAMPLE_NUM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Образец",
                    Path="SAMPLE_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=200,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание",
                    Path="SAMPLE_REMARK",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header="Комментарий",
                    Path="SAMPLE_NOTE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header="Количество",
                    Path="QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="Статус",
                    Path="STATUS_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=70,
                    Stylers=new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row=>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (row["STATUS_ID"].ToInt() == 11)
                                {
                                    color = HColor.Yellow;
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header="Картон заготовки",
                    Path="CARDBOARD_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=150,
                    Stylers=new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row=>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if ((row["STATUS_ID"].ToInt() == SampleStates.InWork || row["STATUS_ID"].ToInt() == 11) && (row["RAW_MISSING_FLAG"].ToInt() == 1))
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
                    Header="В доработке",
                    Path="REVISION_FLAG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=40,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="Конструктор",
                    Path="CONSTRUCTOR_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=30,
                    MaxWidth=120,
                    Stylers=new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row=>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (row["DESIGN_ID"].ToInt() == SampleDesignTypes.InDesign)
                                {
                                    color = HColor.YellowOrange;
                                }
                                if ((row["STATUS_ID"].ToInt() == SampleStates.InWork || row["STATUS_ID"].ToInt() == 11) && (row["DESIGN_ID"].ToInt() == SampleDesignTypes.Performed))
                                {
                                    color = HColor.Green;
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
                    Header="Файы",
                    Path="FILE_IS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=40,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="Сообщений от клиента",
                    Path="UNREAD_MSG",
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
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=30,
                    MaxWidth=120,
                },
                new DataGridHelperColumn
                {
                    Header="Отгрузка",
                    Path="SHIPMENT_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=30,
                    MaxWidth=180,
                },
                new DataGridHelperColumn
                {
                    Header="Менеджер",
                    Path="MANAGER_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=30,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header="Код Статуса",
                    Path="STATUS_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Ожидание сырья",
                    Path="RAW_MISSING_FLAG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Есть сообщения в чате",
                    Path="CHAT_MSG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Код потребности в чертеже",
                    Path="DESIGN_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ИД отгрузки",
                    Path="SHIPMENT_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ИД покупателя",
                    Path="CUSTOMER_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Код типа доставки",
                    Path="DELIVERY_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ID внутреннего чата",
                    Path="CHAT_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };

            Grid.SetColumns(columns);
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
                        int currentStatus = row["STATUS_ID"].ToInt();
                        var dtCompleted = row["DT_COMPLITED"].ToDateTime();

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
                // цвета шрифта строк
                {
                    DataGridHelperColumn.StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        switch (row["STATUS_ID"].ToInt())
                        {
                            case 4:
                                color = HColor.BlueFG;
                                break;

                            case 7:
                                var emplId = Central.User.EmployeeId.ToInt();
                                // Если менеждер сопровождает образец, его статус - передан, доставка - не отгрузка, менеджер должен принять образец.
                                // Такие строки подсвечиваем красным, остальные - синим
                                if ((row.CheckGet("MANAGER_ID").ToInt() == emplId) && (row.CheckGet("DELIVERY_ID").ToInt() > 0))
                                {
                                    color = HColor.RedFG;
                                }
                                else
                                {
                                    color = HColor.BlueFG;
                                }
                                break;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                }
            };
            Grid.SearchText = SearchText;
            Grid.Init();


            // контекстное меню
            Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
            {
                {
                    "BindSample",
                    new DataGridContextMenuItem()
                    {
                        Header="Привязать к отгрузке",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                            BindToShipment();
                        }
                    }
                },
                {
                    "UnbindSample",
                    new DataGridContextMenuItem()
                    {
                        Header="Отвязать от отгрузки",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                            UnbindSample();
                        }
                    }
                },
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
                            {
                                "SetReceived",
                                new DataGridContextMenuItem()
                                {
                                    Header ="Получен",
                                    Action=() =>
                                    {
                                        ShippedSamples(SampleStates.Received);
                                    }
                                }
                            },
                            {
                                "SetShipped",
                                new DataGridContextMenuItem()
                                {
                                    Header ="Отгружен",
                                    Action=() =>
                                    {
                                        ShippedSamples(SampleStates.Shipped);
                                    }
                                }
                            },
                            {
                                "SetRejected",
                                new DataGridContextMenuItem()
                                {
                                    Header ="Отклонен",
                                    Action=() =>
                                    {
                                        RejectSample();
                                    }
                                }
                            },
                        }
                    }
                },
                { "CustomizeLabel",
                    new DataGridContextMenuItem()
                    {
                        Header="Настроить ярлык",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                            LabelCustomizing();
                        }
                    }
                },
                { "Separator1", new DataGridContextMenuItem() {
                    Header="-",
                }},
                {
                    "OpenAttachments",
                    new DataGridContextMenuItem()
                    {
                        Header="Прикрепленные файлы",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                            OpenAttachments();
                        }
                    }
                },
                {
                    "OpenChat",
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

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateActions(selectedItem);
                }
            };

            //данные грида
            Grid.OnLoadItems = LoadItems;
            Grid.OnFilterItems = FilterItems;
            Grid.Run();

            //фокус ввода           
            Grid.Focus();

        }

        public void SetDefaults()
        {
            FromDate.Text = DateTime.Now.AddDays(-28).ToString("dd.MM.yyyy");
            ToDate.Text = DateTime.Now.AddDays(14).ToString("dd.MM.yyyy");

            // Список статусов для фильтра
            var statusList = new Dictionary<string, string>()
            {
                { "-1", "Все" },
                { "1", "Действующие" },
                { "2", "В работе"},
                { "3", "Не в работе" },
                { "4", "Изготовленные" },
                { "5", "Переданные" },
                { "6", "Полученные" },
                { "7", "Отгруженные" },
                { "8", "Отмененные" },
                { "9", "Утилизированные" },
            };
            SampleStatus.Items = statusList;
            SampleStatus.SelectedItem = statusList.FirstOrDefault((x) => x.Key == "1");

            // Список типов доставки
            DeliveryType.Items = DeliveryTypes.ExtendItems();
            DeliveryType.SetSelectedItemByKey("-1");

            ListIds = new List<int>();
            SingleManager = false;
        }

        /// <summary>
        /// Загрузка общей информации
        /// </summary>
        public async void LoadRef()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "ListRef");

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    // менеджеры по работе с клиентами и продажам или пользователи определенной группы, куда входит авторизованный пользователь
                    string managerKey = "MANAGERS";
                    if (result.ContainsKey("USER_GROUP"))
                    {
                        managerKey = "USER_GROUP";
                    }
                    var managersDS = ListDataSet.Create(result, managerKey);
                    var list = new Dictionary<string, string>()
                    {
                        { "-1", "Все" },
                    };

                    var managers = new Dictionary<string, string>();
                    foreach (var item in managersDS.Items)
                    {
                        list.CheckAdd(item["ID"].ToInt().ToString(), item["FIO"]);
                    }

                    ManagerName.Items = list;

                    // Если активный пользователь есть в списке, установим его в выбранном значении
                    string emplId = Central.User.EmployeeId.ToString();
                    if (list.ContainsKey(emplId))
                    {
                        ManagerName.SetSelectedItemByKey(emplId);
                    }
                    else
                    {
                        ManagerName.SetSelectedItemByKey("-1");
                    }
                }
            }

        }

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        public async void LoadItems()
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
                q.Request.SetParam("Action", "ListAccounting");
                q.Request.SetParam("FromDate", FromDate.Text);
                q.Request.SetParam("ToDate", ToDate.Text);

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
                        var ds = ListDataSet.Create(result, "SAMPLES");
                        var sampleDS = ProcessItems(ds);
                        Grid.UpdateItems(sampleDS);

                    }
                }
            }

            GridToolbar.IsEnabled = true;
            Grid.HideSplash();
        }

        /// <summary>
        /// Обработка строк
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        private ListDataSet ProcessItems(ListDataSet ds)
        {
            var _ds = ds;
            if (ds.Items != null)
            {
                if (ds.Items.Count > 0)
                {
                    foreach (var item in _ds.Items)
                    {
                        item.CheckAdd("CHECKING", "0");
                    }
                }
            }

            return _ds;
        }

        /// <summary>
        /// Обработка и фильтрация строк
        /// </summary>
        public void FilterItems()
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

                    bool doFilteringByManager = false;
                    var managerIds = new List<int>();
                    if (SingleManager)
                    {
                        if (ManagerName.SelectedItem.Key != null)
                        {
                            var managerId = ManagerName.SelectedItem.Key.ToInt();
                            if (managerId > 0)
                            {
                                managerIds.Add(managerId);
                                doFilteringByManager = true;
                            }
                        }
                    }
                    else
                    {
                        var ids = Central.SessionValues["ManagersConfig"]["ListActive"];
                        if (!string.IsNullOrEmpty(ids))
                        {
                            var arr = ids.Split(',');
                            foreach (var item in arr)
                            {
                                managerIds.Add(item.ToInt());
                            }
                            doFilteringByManager = true;
                        }
                    }

                    bool doFilteringByDelivery = false;
                    int deliveryId = -1;
                    if (DeliveryType.SelectedItem.Key != null)
                    {
                        deliveryId = DeliveryType.SelectedItem.Key.ToInt();
                        if (deliveryId >= 0)
                        {
                            doFilteringByDelivery = true;
                        }
                    }

                    if (
                        doFilteringByStatus
                        || doFilteringByManager
                        || doFilteringByDelivery
                    )
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in Grid.GridItems)
                        {
                            bool includeByStatus = true;
                            //bool includeByManager = true;
                            bool includeByDelivery = true;

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
                                    // Действующие: в работе, ожидание, изготовленные, переданные, полученные
                                    case 1:
                                        if (
                                            (statusId == SampleStates.InWork)
                                            || (statusId == SampleStates.Produced)
                                            || (statusId == SampleStates.Received)
                                            || (statusId == SampleStates.Transferred)
                                            || (statusId == 11)
                                        )
                                        {
                                            includeByStatus = true;
                                        }
                                        break;
                                    // В работе
                                    case 2:
                                        if (statusId == SampleStates.InWork)
                                        {
                                            includeByStatus = true;
                                        }
                                        break;
                                    // Не в работе
                                    case 3:
                                        if (statusId == 11)
                                        {
                                            includeByStatus = true;
                                        }
                                        break;
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

                            bool includeByManager = false;
                            if (doFilteringByManager)
                            {
                                var l = managerIds.ToArray();
                                if (row.CheckGet("MANAGER_ID").ToInt().ContainsIn(l))
                                {
                                    includeByManager = true;
                                }
                            }
                            // если выбраны все, показываем только образцы из своей группы
                            else
                            {
                                int emplId = row.CheckGet("MANAGER_ID").ToInt();
                                if (ManagerName.Items.ContainsKey(emplId.ToString()))
                                {
                                    includeByManager = true;
                                }
                            }

                            if (doFilteringByDelivery)
                            {
                                includeByDelivery = false;
                                if (deliveryId == row.CheckGet("DELIVERY_ID").ToInt())
                                {
                                    includeByDelivery = true;
                                }
                            }

                            if (includeByStatus && includeByManager && includeByDelivery)
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

            // Доступность кнопок
            bool isShipment = false;
            if (SelectedItem.ContainsKey("DELIVERY_ID"))
            {
                if (!string.IsNullOrEmpty(SelectedItem["DELIVERY_ID"]))
                {
                    int delivery = SelectedItem.CheckGet("DELIVERY_ID").ToInt();
                    isShipment = (delivery == DeliveryTypes.Shipment) || (delivery == DeliveryTypes.ShipmentKashira);
                }
            }
            int status = SelectedItem.CheckGet("STATUS_ID").ToInt();
            bool activeStatus = status.ContainsIn(1, 3, 4, 7, 11);
            bool allowBind = activeStatus && isShipment && (SelectedItem.CheckGet("SHIPMENT_ID").ToInt() == 0);
            BindShipmentButton.IsEnabled = allowBind;
            Grid.Menu["BindSample"].Enabled = allowBind;

            bool allowUnbind = activeStatus && isShipment && (SelectedItem.CheckGet("SHIPMENT_ID").ToInt() != 0);
            UnbindShipmentButton.IsEnabled = allowUnbind;
            Grid.Menu["UnbindSample"].Enabled = allowUnbind;

            // Пункты изменения статуса
            Grid.Menu["ChangeStatus"].Items["SetReceived"].Enabled = status == SampleStates.Transferred;
            Grid.Menu["ChangeStatus"].Items["SetShipped"].Enabled = (status == SampleStates.Transferred || status == SampleStates.Received);
            Grid.Menu["ChangeStatus"].Items["SetRejected"].Enabled = activeStatus;
        }

        /// <summary>
        /// Проверяет, можно ли привязать образец к отгрузке
        /// </summary>
        /// <param name="row"></param>
        /// <returns>ID образца, если можно привязывать, или 0</returns>
        private int GetSampleForShipment(Dictionary<string, string> row)
        {
            int result = 0;

            // Привязываем только с типом доставки Отгрузка
            // Нужный Тип доставки 0, поэтому проверяем на заполненность значения
            bool isShipment = false;
            if (row.ContainsKey("DELIVERY_ID"))
            {
                if (!string.IsNullOrEmpty(row["DELIVERY_ID"]))
                {
                    int delivery = row["DELIVERY_ID"].ToInt();
                    isShipment = (delivery == DeliveryTypes.Shipment) || (delivery == DeliveryTypes.ShipmentKashira);
                }
            }

            if (isShipment)
            {
                // Нельзя привязывать отгруженные, утилизированные и отклоненные образцы
                int status = row.CheckGet("STATUS_ID").ToInt();
                if (!status.ContainsIn(2, 5, 6))
                {
                    result = row.CheckGet("ID").ToInt();
                }
            }

            return result;
        }

        private void BindToShipment()
        {
            if (Grid.Items.Count > 0)
            {
                int customerId = -1;
                //Площадка отгрузки еще не определена
                int delivery = -1;
                bool resume = true;

                var list = new List<int>();
                foreach (var row in Grid.Items)
                {
                    if (row["CHECKING"].ToBool())
                    {
                        var id = GetSampleForShipment(row);
                        if (id > 0)
                        {
                            int currentDelivery = row.CheckGet("DELIVERY_ID").ToInt();
                            if (delivery == -1)
                            {
                                delivery = currentDelivery;
                            }
                            else if (delivery != currentDelivery)
                            {
                                resume = false;
                                var dw = new DialogWindow("Выбераны образцы с отгрузками с разных площадок", "Привязка к отгрузке");
                                dw.ShowDialog();
                                break;
                            }


                            list.Add(id);
                            var currCustomerId = row.CheckGet("CUSTOMER_ID").ToInt();
                            if (customerId == -1)
                            {
                                customerId = currCustomerId;
                            }
                            else if ((customerId > 0) && (customerId != currCustomerId))
                            {
                                customerId = 0;
                            }
                        }
                    }
                }

                if (resume)
                {
                    // Если не отмечено ни одной строки, привяжем выбранную строку
                    if (list.Count == 0)
                    {
                        var id = GetSampleForShipment(SelectedItem);
                        if (id > 0)
                        {
                            list.Add(id);
                            customerId = SelectedItem.CheckGet("CUSTOMER_ID").ToInt();
                            delivery = SelectedItem.CheckGet("DELIVERY_ID").ToInt();
                        }
                        else
                        {
                            resume = false;
                            var dw = new DialogWindow("Нет подходящих образцов для привязки", "Привязка к отгрузке");
                            dw.ShowDialog();
                        }
                    }
                }

                if (resume)
                {
                    if (list.Count > 0)
                    {
                        int factoryId = 1;
                        if (delivery == 5)
                        {
                            factoryId = 2;
                        }

                        var bindToShipment = new SampleBindToShipment();
                        bindToShipment.ReceiverName = TabName;
                        bindToShipment.ObjectName = "Samples";
                        bindToShipment.CustomerId = customerId;
                        bindToShipment.FactoryId = factoryId;
                        bindToShipment.Bind(string.Join(",", list));
                    }
                    else
                    {
                        var dw = new DialogWindow("Нет подходящих образцов для привязки", "Привязка к отгрузке");
                        dw.ShowDialog();
                    }
                }
            }
        }

        /// <summary>
        /// Отвязывает выбранный образец от отгрузки
        /// </summary>
        private async void UnbindSample()
        {
            if (SelectedItem != null)
            {
                if (SelectedItem.CheckGet("SHIPMENT_ID").ToInt() > 0)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "Samples");
                    q.Request.SetParam("Action", "UpdateTS");
                    q.Request.SetParam("IdSmpl", SelectedItem.CheckGet("ID"));
                    q.Request.SetParam("IdTs", "0");

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
                            Grid.LoadItems();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Обновление статуса образцов
        /// </summary>
        /// <param name="newStatus">Новое значение статуса</param>
        private async void UpdateStatus(int newStatus)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "UpdateStatus");
            q.Request.SetParam("SAMPLE_ID", SelectedItem.CheckGet("ID"));
            q.Request.SetParam("STATUS", newStatus.ToString());
            q.Request.SetParam("SAMPLE_LIST", string.Join(",", ListIds));

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
                    if (result.Count > 0)
                    {
                        Grid.LoadItems();
                    }
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
                chatFrame.Recipient = 32;
                chatFrame.RawMissingFlag = 0;
                chatFrame.Edit();
            }
        }

        /// <summary>
        /// Установка статуса Получен или Отгружен для отмеченных образцов
        /// </summary>
        /// <param name="newStatus">Новое значение статуса</param>
        private void ShippedSamples(int newStatus)
        {
            if (SelectedItem != null)
            {
                // Проверяем заполненность чекбоксов
                var msg = GetListIds(0);

                if (string.IsNullOrEmpty(msg))
                {
                    UpdateStatus(newStatus);
                }
                else
                {
                    var dw = new DialogWindow(msg, "Смена статуса");
                    dw.ShowDialog();
                }
            }
        }

        /// <summary>
        /// Отклонение образца
        /// </summary>
        private void RejectSample()
        {
            if (SelectedItem != null)
            {
                var dw = new DialogWindow("Вы действительно хотите отклонить образец?", "Смена статуса", "", DialogWindowButtons.NoYes);
                if ((bool)dw.ShowDialog())
                {
                    if (dw.ResultButton == DialogResultButton.Yes)
                    {
                        ListIds.Clear();
                        UpdateStatus(SampleStates.Rejected);
                    }
                }
            }
        }

        /// <summary>
        /// Открытие вкладки с приложенными файлами
        /// </summary>
        private void OpenAttachments()
        {
            if (SelectedItem != null)
            {
                var sampleFiles = new SampleFiles();
                sampleFiles.SampleId = SelectedItem.CheckGet("ID").ToInt();
                sampleFiles.ReturnTabName = TabName;
                sampleFiles.Show();
            }
        }

        /// <summary>
        /// Вызов формы редактирования образца
        /// </summary>
        private void EditSample()
        {
            int sampleId = SelectedItem.CheckGet("ID").ToInt();
            if (sampleId > 0)
            {
                var productionType = SelectedItem.CheckGet("PRODUCTION_TYPE_ID").ToInt();
                if (productionType == 0)
                {
                    var sample = new Sample();
                    sample.ReceiverName = TabName;
                    sample.Edit(sampleId);
                }
                else
                {
                    var sample = new SampleProduction();
                    sample.ReceiverName = TabName;
                    sample.Edit(sampleId);
                }
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

        /// <summary>
        /// Формирование отчета со списком образцов. 
        /// </summary>
        private void MakeListReport()
        {
            if (Grid.GridItems != null)
            {
                if (Grid.GridItems.Count > 0)
                {
                    var list = new List<Dictionary<string, string>>();
                    // Добавляем в список записи, отмеченные флажками
                    foreach (var row in Grid.GridItems)
                    {
                        if (row.CheckGet("CHECKING").ToBool())
                        {
                            list.Add(row);
                        }
                    }

                    var completedReport = new SampleTaskCompletedReport();
                    completedReport.DeliveryType = "Все";
                    completedReport.ShowStatus = true;
                    // Если не отмечено ни одной записи, передаём все записи
                    if (list.Count > 0)
                    {
                        completedReport.SampleList = list;
                    }
                    else
                    {
                        completedReport.SampleList = Grid.GridItems;
                    }
                    completedReport.Make();
                }
            }
        }

        /// <summary>
        /// Получаем список ID у отмеченных строк
        /// </summary>
        /// <param name="sameCustomer">Признак совпадения покупателей: 0 - не сравниваем покупателей, 1 - для всех образцов покупатель должен совпадать</param>
        /// <returns>Сообщение об ошибке, пусто при успешном формировании списка</returns>
        private string GetListIds(int sameCustomer = 1)
        {
            // Проверяем отмеченные чекбоксы. У выделенных строк должен быть один покупатель и одинаковый тип доставки.
            // Если нет - выводим сообщение об ошибке.
            // Если нет отметок, печатаем для выбранной строки
            bool resume = true;
            string msg = "";
            ListIds.Clear();

            if (Grid.GridItems != null)
            {
                if (Grid.GridItems.Count > 0)
                {
                    int customerId = -1;
                    int deliveryId = -1;
                    foreach (var row in Grid.GridItems)
                    {
                        if (row.CheckGet("CHECKING").ToBool())
                        {
                            // Если сравниваем покупателей, то покупатели должны совпадать
                            if (sameCustomer == 1)
                            {
                                if (customerId > -1)
                                {
                                    if (row.CheckGet("CUSTOMER_ID").ToInt() != customerId)
                                    {
                                        resume = false;
                                        msg = "Выбранные образцы отличаются по покупателю";
                                        break;
                                    }
                                }
                                else
                                {
                                    customerId = row.CheckGet("CUSTOMER_ID").ToInt();
                                }
                            }

                            // Совпадение доставок
                            if (deliveryId > -1)
                            {
                                if (row.CheckGet("DELIVERY_ID").ToInt() != deliveryId)
                                {
                                    resume = false;
                                    msg = "Выбранные образцы отличаются по типу доставки";
                                    break;
                                }
                            }
                            else
                            {
                                deliveryId = row.CheckGet("DELIVERY_ID").ToInt();
                            }

                            ListIds.Add(row.CheckGet("ID").ToInt());
                        }
                    }

                    // Прошли весь список, но не нашли отмеченные строки, отправляем выбранную строку
                    if (resume && (ListIds.Count == 0))
                    {
                        ListIds.Add(SelectedItem.CheckGet("ID").ToInt());
                    }

                }
            }

            return msg;
        }

        /// <summary>
        /// Получение списка отмеченных образцов, получение комментария к ярлыку
        /// </summary>
        private void CheckLabels()
        {
            var msg = GetListIds(1);
            if (string.IsNullOrEmpty(msg))
            {
                var noteEdit = new SampleNote();
                noteEdit.ReceiverName = TabName;
                var p = new Dictionary<string, string>()
                {
                    { "ID", ListIds[0].ToString() },
                    { "NOTE", "" },
                    { "SHOW_CARDBOARD", "0" },
                };
                noteEdit.Edit(p);
            }
            else
            {
                var dw = new DialogWindow(msg, "Печать ярлыков");
                dw.ShowDialog();
            }
        }

        /// <summary>
        /// Формируем форму печати ярлыков
        /// </summary>
        /// <param name="v"></param>
        private async void PrintLabels(Dictionary<string, string> v)
        {
            string note = v.CheckGet("NOTE");
            var multiLabel = new SampleMultiLabel();
            multiLabel.PrintLabel(ListIds, note);
        }

        private void LabelCustomizing()
        {
            var msg = GetListIds(1);
            if (string.IsNullOrEmpty(msg))
            {
                var labelCistomizing = new SampleLabelCustomizing();
                labelCistomizing.SampleIdList = ListIds;
                labelCistomizing.Show();
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void SampleStatus_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void PrintMenuButton_Click(object sender, RoutedEventArgs e)
        {
            PrintMenu.IsOpen = true;
        }

        private void BindShipment_Click(object sender, RoutedEventArgs e)
        {
            BindToShipment();
        }

        private void UnbindShipment_Click(object sender, RoutedEventArgs e)
        {
            UnbindSample();
        }

        private void ManagerName_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SingleManager = true;
            Grid.UpdateItems();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem != null)
            {
                EditSample();
            }
        }

        private void DeliveryType_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            MakeListReport();
        }

        private void SampleLabelReport_Click(object sender, RoutedEventArgs e)
        {
            CheckLabels();
        }

        private void SelectManagerButton_Click(object sender, RoutedEventArgs e)
        {
            SingleManager = false;
            var selectManager = new SampleSelectManager();
            selectManager.ReceiverName = TabName;
            selectManager.Show();
        }

        private void SampleLabelCustomizing_Click(object sender, RoutedEventArgs e)
        {
            LabelCustomizing();
        }

        private void WorkloadReport_Click(object sender, RoutedEventArgs e)
        {
            var workloadReport = new SampleWorkloadReport();
            workloadReport.Make();
        }
    }
}
