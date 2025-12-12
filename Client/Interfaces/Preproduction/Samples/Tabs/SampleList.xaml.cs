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

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleList : UserControl
    {
        public SampleList()
        {
            InitializeComponent();

            //регистрация обработчика клавиатуры
            PreviewKeyDown += ProcessKeyboard;

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            SetDefaults();
            LoadRef();
            InitGrid();

            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/samples_new/samples1";
            UIUtil.ProcessPermissions("[erp]sample", this);
        }

        /// <summary>
        /// данные для таблицы картона для образцов
        /// </summary>
        public ListDataSet SampleDS { get; set; }

        /// <summary>
        /// Код группы получателей сообщений для текущего пользователя
        /// </summary>
        private int Recipient;

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
                { "2", "Не в работе" },
                { "3", "Ожидание сырья" },
                { "4", "В доработке" },
                { "5", "Изготовленные" },
                { "6", "Переданные" },
                { "7", "Полученные" },
                { "8", "Отгруженные" },
                { "9", "Отмененные" },
                { "10", "Утилизированные" },
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
                            if (NoteTarget == 2)
                            {
                                NoteTarget = 0;
                                SaveNote(answer);
                            }
                            else if (NoteTarget == 1)
                            {
                                MakeSampleLabel(answer);
                            }
                            break;

                        case "CardboardSelected":
                            if (obj.ContextObject != null)
                            {
                                var v = (Dictionary<string, string>)obj.ContextObject;
                                SetRawCardboard(v);
                            }
                            break;
                    }
                }
            }
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
                    // менеджеры по работе с клиентами
                    var managersDS = ListDataSet.Create(result, "MANAGERS");
                    // менеджеры по продажам
                    var constructorDS = ListDataSet.Create(result, "CONSTRUCTORS");
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
                    ManagerName.SetSelectedItemByKey("-1");

                    // В этом интерфейсе работают технологи УПП, поэтому в чате получателями будут технологи
                    Recipient = 6;
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
                    Header=" ",
                    Path="CHECKING",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    Editable=true,
                    MinWidth=30,
                    MaxWidth=30,
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
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="Веб-заявка",
                    Path="WEB_FLAG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=30,
                    MaxWidth=30,
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
                    Header="Номер",
                    Path="SAMPLE_NUM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=100,
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
                    Header="Покупатель",
                    Path="CUSTOMER_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=250,
                },
                new DataGridHelperColumn
                {
                    Header="Образец",
                    Path="SAMPLE_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=300,
                },
                new DataGridHelperColumn
                {
                    Header="Размер развертки",
                    Path="BLANK_SIZE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Код FEFCO",
                    Path="FEFCO",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=40,
                    MaxWidth=60,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание",
                    Path="NAME_NOTE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=300,
                },
                new DataGridHelperColumn
                {
                    Header="Количество",
                    Path="QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="Изготовил",
                    Path="TECHNOLOG_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=120,
                },
                new DataGridHelperColumn
                {
                    Header="Картон",
                    Path="PRODUCED_CARDBOARD_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=250,
                },
                new DataGridHelperColumn
                {
                    Header="Без обязательной марки",
                    Path="ANY_CARTON_FLAG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=30,
                    MaxWidth=30,
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
                    Header="Листов в наличии",
                    Path="SOURCE_QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="Листов в резерве",
                    Path="RESERVE_QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
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
                    Header="Ожидание сырья",
                    Path="RAW_MISSING_FLAG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=30,
                    MaxWidth=30,
                },
                new DataGridHelperColumn
                {
                    Header="В доработке",
                    Path="REVISION_FLAG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=30,
                    MaxWidth=30,
                },
                new DataGridHelperColumn
                {
                    Header="Конструктор",
                    Path="CONSTRUCTOR_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=30,
                    MaxWidth=150,
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
                    MinWidth=30,
                    MaxWidth=30,
                },
                new DataGridHelperColumn
                {
                    Header="Чертеж",
                    Path="DESIGN_FILE_IS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=30,
                    MaxWidth=30,
                },
                new DataGridHelperColumn
                {
                    Header="Чертеж в другом формате",
                    Path="DESIGN_FILE_OTHER_IS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=30,
                    MaxWidth=30,
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

                                if (row.CheckGet("UNREAD_MESSAGE_QTY").ToInt() > 0)
                                {
                                    color = HColor.Red;
                                }
                                else if (row.CheckGet("MESSAGE_QTY").ToInt() > 0)
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
                    MaxWidth=60,
                },
                new DataGridHelperColumn
                {
                    Header="Отгрузка",
                    Path="SHIPMENT_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=30,
                    MaxWidth=60,
                },
                new DataGridHelperColumn
                {
                    Header="Ячейка",
                    Path="CELL_NUM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=30,
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
                    Header="Примечание кладовщика",
                    Path="STOREKEEPER_NOTE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=30,
                    MaxWidth=150,
                    Stylers=new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row=>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (!string.IsNullOrEmpty(row["STOREKEEPER_NOTE"]))
                                {
                                    color = HColor.Orange;
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
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
                    Header="Есть ПЗ",
                    Path="PZ_IS",
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
                    Header="Есть сообщения в чате",
                    Path="CHAT_MSG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Код FEFCO",
                    Path="ID_FEFCO",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ID картона для изготовления образца",
                    Path="PRODUCED_IDC",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ID чата с коллегами",
                    Path="CHAT_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            Grid.SetColumns(columns);

            var today = DateTime.Now.Date;
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

                        // плановая дата изготовления - сегодня
                        if ((DateTime.Compare(dtCompleted, today) == 0) && (currentStatus == SampleStates.InWork))
                        {
                            color=HColor.Yellow;
                        }
                        // просроченные образцы - плановая дата в прошлом
                        if ((DateTime.Compare(dtCompleted, today) < 0) && (currentStatus == SampleStates.InWork))
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
                // цвета шрифта строк
                {
                    DataGridHelperColumn.StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        // нет сырья для образца и на ГА есть подходящие ПЗ
                        if((row["RAW_MISSING_FLAG"].ToInt() == 1) && (row["PZ_IS"].ToInt() == 1))
                        {
                            color = HColor.RedFG;
                        }

                        switch (row["STATUS_ID"].ToInt())
                        {
                            case 3:
                            {
                                color = HColor.GreenFG;
                                break;
                            }
                            case 4:
                            {
                                color = HColor.BlueFG;
                                break;
                            }
                            case 7:
                            {
                                color = HColor.BlueFG;
                                break;
                            }
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
                { "SetBlankSize", new DataGridContextMenuItem(){
                    Header="Вычислить размер развертки",
                    Action=()=>
                    {
                        SetBlankSize();
                    }
                }},
                { "GetDrawings", new DataGridContextMenuItem(){
                        Header="Чертежи",
                        Action=() =>
                        {
                        },
                        Items=new Dictionary<string, DataGridContextMenuItem>()
                        {
                            { "ShowDesignFile", new DataGridContextMenuItem(){
                                Header="Показать чертеж",
                                Action=() =>
                                {
                                    ShowDesign(0);
                                },
                            }},
                            { "ShowAltDesignFile", new DataGridContextMenuItem(){
                                Header="Чертеж в другом формате",
                                Action=() =>
                                {
                                    ShowDesign(1);
                                },
                            }},
                            { "ShowDesignFolder", new DataGridContextMenuItem(){
                                Header="Папка чертежа",
                                Action=() =>
                                {
                                    DesignFolder();
                                },
                            }},
                            { "ShowDrawing", new DataGridContextMenuItem(){
                                Header="Показать схему",
                                Action=()=>
                                {
                                    ShowDrawing();
                                }
                            }},

                        }
                    }
                },
                { "Separator1", new DataGridContextMenuItem() {
                    Header="-",
                }},
                { "OpenAttachments",
                    new DataGridContextMenuItem()
                    {
                        Header="Прикрепленные файлы",
                        Action=() =>
                        {
                            OpenAttachments();
                        }
                    }
                },
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
                { "EditNote", new DataGridContextMenuItem(){
                    Header="Изменить примечание",
                    Tag = "access_mode_full_access",
                    Action=()=>
                    {
                        GetNote();
                    }
                }},
                { "EditRaw", new DataGridContextMenuItem(){
                    Header="Сменить картон",
                    Tag = "access_mode_full_access",
                    Action=()=>
                    {
                        EditRaw();
                    }
                }},
                { "GoToRaw", new DataGridContextMenuItem(){
                    Header="Перейти к сырью",
                    Action=()=>
                    {
                        GoToRaw();
                    }
                }},
                { "Separator2", new DataGridContextMenuItem() {
                    Header="-",
                }},
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

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateActions(selectedItem);
                }
            };

            //двойной клик на строке откроет форму просмотра
            Grid.OnDblClick = (Dictionary<string, string> selectedItem) =>
              {
                  Edit(selectedItem.CheckGet("ID").ToInt());
              };

            //данные грида
            Grid.OnLoadItems = LoadItems;
            Grid.OnFilterItems = FilterItems;
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
                q.Request.SetParam("Action", "List");
                q.Request.SetParam("FromDate", FromDate.Text);
                q.Request.SetParam("ToDate", ToDate.Text);
                q.Request.SetParam("Recipient", Recipient.ToString());

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                }
                );

                int ordersQty = 0;
                int newSamplesQty = 0;
                int plotterQty = 0;
                int prodQty = 0;

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        SampleDS = ListDataSet.Create(result, "SAMPLES");
                        Grid.UpdateItems(SampleDS);

                        // Данные для информационной строки
                        // Количество заявок/образцов за последние 24 часа
                        var newOrdersDS = ListDataSet.Create(result, "NEW_ORDERS");
                        if (newOrdersDS.Items.Count > 0)
                        {
                            var rec = newOrdersDS.Items[0];
                            ordersQty = rec.CheckGet("ORDER_QTY").ToInt();
                            newSamplesQty = rec.CheckGet("SAMPLE_QTY").ToInt();
                        }

                        // Количество образцов, изготовленных за предыдущую смену
                        var completedDS = ListDataSet.Create(result, "COMPLETED");
                        foreach (var item in completedDS.Items)
                        {
                            if (item.ContainsKey("PRODUCTION_TYPE"))
                            {
                                if (item["PRODUCTION_TYPE"].ToInt() == 0)
                                {
                                    plotterQty = item.CheckGet("QTY").ToInt();
                                }
                                else
                                {
                                    prodQty = item.CheckGet("QTY").ToInt();
                                }
                            }
                        }

                        RefreshButton.Style = (Style)RefreshButton.TryFindResource("Button");
                    }
                }

                TotalsDayOrdersValue.Text = ordersQty.ToString();
                TotalsDayNewSampleQtyValue.Text = newSamplesQty.ToString();
                TotalsCompletedQtyValue.Text = $"{plotterQty}/{prodQty}";
            }

            GridToolbar.IsEnabled = true;
            Grid.HideSplash();
        }

        /// <summary>
        /// Фильтрация строк
        /// </summary>
        public void FilterItems()
        {
            if (Grid.GridItems != null)
            {
                if (Grid.GridItems.Count > 0)
                {
                    {
                        bool doFilteringByStatus = false;
                        int status = -1;
                        if (SampleStatus.SelectedItem.Key != null)
                        {
                            status = SampleStatus.SelectedItem.Key.ToInt();
                            doFilteringByStatus = true;
                        }

                        bool doFilteringByManager = false;
                        int managerId = -1;
                        if (ManagerName.SelectedItem.Key != null)
                        {
                            managerId = ManagerName.SelectedItem.Key.ToInt();
                            if (managerId > 0)
                            {
                                doFilteringByManager = true;
                            }
                        }

                        if (
                            doFilteringByStatus
                            || doFilteringByManager
                        )
                        {
                            var items = new List<Dictionary<string, string>>();
                            foreach (Dictionary<string, string> row in Grid.GridItems)
                            {
                                bool includeByStatus = true;
                                bool includeByManager = true;

                                if (doFilteringByStatus)
                                {
                                    int statusId = row.CheckGet("STATUS_ID").ToInt();
                                    int rawMissing = row.CheckGet("RAW_MISSING_FLAG").ToInt();

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
                                        // Не в работе
                                        case 2:
                                            if (statusId == 11)
                                            {
                                                includeByStatus = true;
                                            }
                                            break;
                                        // Ожидание сырья
                                        case 3:
                                            if (((rawMissing == 1) && (statusId == 11)) || ((row.CheckGet("SOURCE_QTY").ToInt() == 0) && (statusId == 1)))
                                            {
                                                includeByStatus = true;
                                            }
                                            break;
                                        // В доработке
                                        case 4:
                                            if ((statusId == 11) && (row.CheckGet("REVISION_FLAG").ToInt() == 1))
                                            {
                                                includeByStatus = true;
                                            }
                                            break;
                                        //  Изготовленные
                                        case 5:
                                            if (statusId == SampleStates.Produced)
                                            {
                                                includeByStatus = true;
                                            }
                                            break;
                                        // Переданные
                                        case 6:
                                            if (statusId == SampleStates.Transferred)
                                            {
                                                includeByStatus = true;
                                            }
                                            break;
                                        // Полученные
                                        case 7:
                                            if (statusId == SampleStates.Received)
                                            {
                                                includeByStatus = true;
                                            }
                                            break;
                                        // Отгруженные
                                        case 8:
                                            if (statusId == SampleStates.Shipped)
                                            {
                                                includeByStatus = true;
                                            }
                                            break;
                                        // Отмененные
                                        case 9:
                                            if (statusId == SampleStates.Rejected)
                                            {
                                                includeByStatus = true;
                                            }
                                            break;
                                        // Утилизированные
                                        case 10:
                                            if (statusId == SampleStates.Utilized)
                                            {
                                                includeByStatus = true;
                                            }
                                            break;
                                    }
                                }

                                if (doFilteringByManager)
                                {
                                    includeByManager = false;
                                    if (managerId == row.CheckGet("MANAGER_ID").ToInt())
                                    {
                                        includeByManager = true;
                                    }
                                }

                                if (includeByStatus && includeByManager)
                                {
                                    items.Add(row);
                                }
                            }
                            Grid.GridItems = items;
                        }
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
            int currentStatus = SelectedItem["STATUS_ID"].ToInt();

            Grid.Menu["EditNote"].Enabled = currentStatus.ContainsIn(1, 3, 4, 7, 11);
            Grid.Menu["ChangeCellNum"].Enabled = currentStatus.ContainsIn(3, 4, 7);

            //пункт меню, переключающий на интерфейс картона для образцов.
            //Доступен только если у пользователя есть право на работу с этим интерфейсом
            var rawOpen = Central.Navigator.GetRoleLevel("[erp]sample_cardboard");
            Grid.Menu["GoToRaw"].Enabled = (rawOpen == Role.AccessMode.FullAccess) || (rawOpen == Role.AccessMode.Special) || (rawOpen == Role.AccessMode.ReadOnly);
        }

        /// <summary>
        /// Вызов вкладки редактирования примечания
        /// </summary>
        /// <param name="sampleId"></param>
        public void GetNote()
        {
            int sampleId = SelectedItem.CheckGet("ID").ToInt();
            if (sampleId > 0)
            {
                NoteTarget = 2;
                var sampleNote = new SampleNote();
                sampleNote.ReceiverName = TabName;
                var p = new Dictionary<string, string>()
                {
                    { "ID", sampleId.ToString() },
                    { "NOTE", SelectedItem.CheckGet("STOREKEEPER_NOTE") },
                };
                sampleNote.Edit(p);
            };
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
                }
                );

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

        /// <summary>
        /// Отправляет запрос, который вычисляет и заполняет размеры развертки для выбранного образца
        /// </summary>
        private async void SetBlankSize(int sampleId = 0)
        {
            // Если не задан ID образца, возьмем из выбранной строки
            if (sampleId == 0)
            {
                sampleId = SelectedItem.CheckGet("ID").ToInt();
            }
            if (sampleId > 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "Samples");
                q.Request.SetParam("Action", "SetBlankSize");
                q.Request.SetParam("ID", sampleId.ToString());

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
                            Grid.LoadItems();
                        }
                    }
                }
                else if (q.Answer.Error.Code == 147)
                {
                    var dw = new DialogWindow(q.Answer.Error.Message, "Размеры развертки");
                    dw.ShowDialog();
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

        private void Edit(int sampleId)
        {
            var sampleForm = new Sample();
            sampleForm.ReceiverName = TabName;
            sampleForm.Confirmation = 2;
            sampleForm.Edit(sampleId);

        }

        /// <summary>
        /// Открытие вкладки с чатом по образцу
        /// </summary>
        /// <param name="chatType">Тип чата: 0 - чат с клиентом, 1 - чат с коллегами</param>
        private void OpenChat(int chatType=0)
        {
            if (SelectedItem != null)
            {
                var chatFrame = new SampleChat();
                chatFrame.ObjectId = SelectedItem.CheckGet("ID").ToInt();
                chatFrame.ReceiverName = TabName;
                chatFrame.Recipient = Recipient;
                chatFrame.ChatType = chatType;
                chatFrame.ChatId = SelectedItem.CheckGet("CHAT_ID").ToInt();
                chatFrame.RawMissingFlag = SelectedItem.CheckGet("RAW_MISSING_FLAG").ToInt();
                chatFrame.Edit();
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
        /// Загружает и открывает файл чертежа
        /// </summary>
        /// <param name="designType">Тип чертежа: 0 - основной, 1 - альтернативный</param>
        private async void ShowDesign(int designType)
        {
            string sourceFile;

            if (designType == 0)
            {
                sourceFile = SelectedItem.CheckGet("DESIGN_FILE");
            }
            else
            {
                sourceFile = SelectedItem.CheckGet("DESIGN_FILE_OTHER");
            }

            if (File.Exists(sourceFile))
            {
                Central.OpenFile(sourceFile);
            }
            else
            {
                var dw = new DialogWindow("Файл не найден", "Чертеж для образца");
                dw.ShowDialog();
            }
        }

        /// <summary>
        /// Открывает папку чертежа в Explorer
        /// </summary>
        private void DesignFolder()
        {
            var sourceFile = SelectedItem.CheckGet("DESIGN_FILE");
            if (string.IsNullOrEmpty(sourceFile))
            {
                sourceFile = SelectedItem.CheckGet("DESIGN_FILE_OTHER");
            }

            if (File.Exists(sourceFile))
            {
                var folder = Path.GetDirectoryName(sourceFile);
                Central.OpenFolder(folder);
            }
        }

        /// <summary>
        /// Получение схемы развертки образца
        /// </summary>
        public async void ShowDrawing()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "GetScheme");
            q.Request.SetParam("ID", SelectedItem.CheckGet("ID"));

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Central.OpenFile(q.Answer.DownloadFilePath);
            }
            else if (q.Answer.Error.Code == 145)
            {
                var d = new DialogWindow($"{q.Answer.Error.Message}", "Схема образца");
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Сохранение примечания
        /// </summary>
        /// <param name="p"></param>
        public async void SaveNote(Dictionary<string, string> p)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "SaveStorekeeperNote");
            q.Request.SetParam("ID", p.CheckGet("ID"));
            q.Request.SetParam("STOREKEEPER_NOTE", p.CheckGet("NOTE"));

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    if (result.ContainsKey("Items"))
                    {
                        Grid.LoadItems();
                    }
                }
            }

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
                        // Показывать полное название картона или только профиль
                        var showCardboard = p.CheckGet("SHOW_CARDBOARD");
                        if (item.ContainsKey("HIDE_MARK"))
                        {
                            if (item["HIDE_MARK"].ToInt() == 1)
                            {
                                showCardboard = "0";
                            }
                        }

                        item.Add("SHOW_CARDBOARD", showCardboard);
                        var sampleLabel = new SampleTaskLabel();
                        sampleLabel.SampleItem = item;

                        sampleLabel.Make();
                    }
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
        /// Изменение картона, из кторого будет изготавливаться образец
        /// </summary>
        private void EditRaw()
        {
            if (SelectedItem != null)
            {
                var p = new Dictionary<string, string>()
                {
                    { "PROFILE", SelectedItem.CheckGet("PROFILE_ID") },
                    { "MARK", SelectedItem.CheckGet("MARK_ID") },
                    { "COLOR", SelectedItem.CheckGet("OUTER_COLOR_ID") },
                    { "CARDBOARD", SelectedItem.CheckGet("PRODUCED_IDC") },
                };
                var selectCardboard = new SampleSelectCardboard();
                selectCardboard.ReceiverName = TabName;
                selectCardboard.AnswerType = 0;
                selectCardboard.SampleId = SelectedItem.CheckGet("ID").ToInt();
                selectCardboard.Edit(p);
            }
        }


        private async void SetRawCardboard(Dictionary<string, string> v)
        {
            int sampleId = v.CheckGet("SAMPLE_ID").ToInt();
            int rawId = v.CheckGet("ID").ToInt();
            string errorMsg = "";

            if (sampleId > 0 && rawId > 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "Samples");
                q.Request.SetParam("Action", "UpdateRawCardboard");
                q.Request.SetParam("ID", sampleId.ToString());
                q.Request.SetParam("RAW_CARDBOARD_ID", rawId.ToString());

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
                else if (q.Answer.Error.Code == 145)
                {
                    errorMsg = q.Answer.Error.Message;
                }

            }
            else
            {
                errorMsg = "Не удалось определить образец или картон";
            }

            if (!string.IsNullOrEmpty(errorMsg))
            {
                var d = new DialogWindow(errorMsg, "Смена картона для образца");
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Открывает вкладку с картоном для образцов и переключает на сырье из выбранной строки
        /// Срабатывает 
        /// </summary>
        private void GoToRaw()
        {
            Central.Navigator.ProcessURL($"l-pack://l-pack_erp/preproduction/samples/sample_carton/list?id={SelectedItem["PRODUCED_IDC"].ToInt()}");
        }

        /// <summary>
        /// При сммене дат меняет стиль кнопки обновления данных
        /// </summary>
        private void DateChanged()
        {
            RefreshButton.Style = (Style)RefreshButton.TryFindResource("FButtonPrimary");
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem != null)
            {
                if (SelectedItem.ContainsKey("ID"))
                {
                    Edit(SelectedItem["ID"].ToInt());
                }
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            Edit(0);
        }

        private void SampleStatus_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void ManagerName_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void CompletedSample_Click(object sender, RoutedEventArgs e)
        {
            var reportSettings = new SampleCompletedReportSettings();
            reportSettings.Show();
        }

        private void SampleListReport_Click(object sender, RoutedEventArgs e)
        {
            MakeListReport();
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

        private void ToExcelButton_Click(object sender, RoutedEventArgs e)
        {
            Grid.ExportItemsExcel();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void PrintMenuButton_Click(object sender, RoutedEventArgs e)
        {
            PrintMenu.IsOpen = true;
        }
    }
}
