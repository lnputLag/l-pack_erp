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
using System.Windows.Navigation;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Список образцов для технолога
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleViewList : ControlBase
    {
        public SampleViewList()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            SetDefaults();
            InitGrid();

            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/samples_new/task_samples/list_samples";
            UIUtil.ProcessPermissions("[erp]sample_task", this);
        }

        #region Common

        /// <summary>
        /// Название вкладки
        /// </summary>
        public string TabName;
        /// <summary>
        /// Ссылка на страницу документации
        /// </summary>
        private string DocumentationUrl;

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// Обработчики нажатий клавиш
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /*
        private void ProcessKeyboard2(object sender, KeyEventArgs e)
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
        */

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
                { "1", "Готовые" },
                { "2", "В работе"},
                { "3", "Не в работе" },
                { "4", "Ожидание сырья" },
                { "5", "Изготовленные" },
                { "6", "Переданные" },
                { "7", "Полученные" },
                { "8", "Отгруженные" },
                { "9", "Отмененные" },
                { "10", "Утилизированные" },
            };
            SampleStatus.Items = statusList;
            SampleStatus.SelectedItem = statusList.FirstOrDefault((x) => x.Key == "1");

            // Список типов доставки
            DeliveryType.Items = DeliveryTypes.ExtendItems();
            DeliveryType.SetSelectedItemByKey("-1");
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
                            MakeSampleLabel(answer);
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
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="№",
                    Path="_ROWNUMBER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Веб-заявка",
                    Path="WEB_FLAG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Номер",
                    Path="SAMPLE_NUM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn
                {
                    Header="Дата изготовления",
                    Path="DT_COMPLITED",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Width2=10,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header="Тип изготовления",
                    Path="PRODUCTION_TYPE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Покупатель",
                    Path="CUSTOMER_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=25,
                },
                new DataGridHelperColumn
                {
                    Header="Образец",
                    Path="SAMPLE_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=25,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание",
                    Path="SAMPLE_REMARK",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn
                {
                    Header="Комментарий",
                    Path="SAMPLE_NOTE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn
                {
                    Header="Количество",
                    Path="QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header="Статус",
                    Path="STATUS_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=10,
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
                    Width2=15,
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
                    Header="Номер картона",
                    Path="CARDBOAD_NUM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="В доработке",
                    Path="REVISION_FLAG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Конструктор",
                    Path="CONSTRUCTOR_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=15,
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
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Сообщений",
                    Path="UNREAD_MSG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=5,
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
                    Header="Доставка",
                    Path="DELIVERY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header="Ячейка склада",
                    Path="CELL_NUM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="Отгрузка",
                    Path="SHIPMENT_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Менеджер",
                    Path="MANAGER_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=15,
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
            };

            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("ID");
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
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
                        int currentStatus = row.CheckGet("STATUS_ID").ToInt();
                        var dtCompleted = row.CheckGet("DT_COMPLITED").ToDateTime();

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

                        switch (row.CheckGet("STATUS_ID").ToInt())
                        {
                            case 3:
                                color = HColor.GreenFG;
                                break;
                            case 4:
                            case 7:
                                color = HColor.BlueFG;
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

            // контекстное меню
            Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
            {
                { "Copy", new DataGridContextMenuItem(){
                    Header="Копировать текст в буфер обмена",
                    Action=()=>
                    {
                        Grid.CopyCellValue();
                    }
                }},
                { "Separator0", new DataGridContextMenuItem() {
                    Header="-",
                }},
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
                                "SetTransfer",
                                new DataGridContextMenuItem()
                                {
                                    Header ="Передан",
                                    Action=() =>
                                    {
                                        SetStatus(SampleStates.Transferred);
                                    }
                                }
                            },
                            {
                                "BackProduced",
                                new DataGridContextMenuItem()
                                {
                                    Header ="Изготовлен",
                                    Action=() =>
                                    {
                                        SetStatus(SampleStates.Produced);
                                    }
                                }
                            },
                            {
                                "BackWork",
                                new DataGridContextMenuItem()
                                {
                                    Header ="В работу",
                                    Action=() =>
                                    {
                                        SetStatus(SampleStates.InWork);
                                    }
                                }
                            },
                        }
                    }
                },
                { "UpdateDeliveryType",
                    new DataGridContextMenuItem()
                    {
                        Header="Изменить тип доставки",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                            UpdateDeliveryType();
                        }
                    }
                },
                { "OpenAttachments",
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
            Grid.Init();

            //фокус ввода           
            Grid.Focus();
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
                        var sampleDS = ListDataSet.Create(result, "SAMPLES");
                        var procesedDS = ProcessItems(sampleDS);
                        Grid.UpdateItems(procesedDS);

                    }
                }
            }

            GridToolbar.IsEnabled = true;
            Grid.HideSplash();
        }

        /// <summary>
        /// обновление методов работы с выбранной записью
        /// </summary>
        /// <param name="selectedItem">выбранная запись</param>
        private void UpdateActions(Dictionary<string, string> selectedItem)
        {
            SelectedItem = selectedItem;

            int status = SelectedItem.CheckGet("STATUS_ID").ToInt();
            Grid.Menu["UpdateDeliveryType"].Enabled = status.ContainsIn(1, 3, 4, 7, 11);

            int productId = selectedItem.CheckGet("PRODUCT_ID").ToInt();
            OpenTechCardItem.IsEnabled = productId > 0;
        }

        /// <summary>
        /// Обработка строк после загрузки
        /// </summary>
        public ListDataSet ProcessItems(ListDataSet ds)
        {
            ListDataSet _ds = ds;

            if (_ds.Items.Count > 0)
            {
                foreach (var item in _ds.Items)
                {
                    item.CheckAdd("CHECKING", "0");
                }
            }

            return _ds;
        }

        /// <summary>
        /// Фильтрация строк
        /// </summary>
        public void FilterItems()
        {
            if (Grid.Items != null)
            {
                if (Grid.Items.Count > 0)
                {
                    // делать фильтрацию по статусу
                    bool doFilteringByStatus = false;
                    int status = -1;
                    if (SampleStatus.SelectedItem.Key != null)
                    {
                        status = SampleStatus.SelectedItem.Key.ToInt();
                        doFilteringByStatus = true;
                    }

                    // делать фильтрацию по доставке

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
                        || doFilteringByDelivery
                    )
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in Grid.Items)
                        {
                            bool includeByStatus = true;
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
                                    // Готовые: изготовленные, переданные, полученные
                                    case 1:
                                        if (
                                            (statusId == SampleStates.Produced)
                                            || (statusId == SampleStates.Received)
                                            || (statusId == SampleStates.Transferred)
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
                                    // Ожидание сырья
                                    case 4:
                                        if ((statusId == 11) && (row.CheckGet("RAW_MISSING_FLAG").ToInt() == 1))
                                        {
                                            includeByStatus = true;
                                        }
                                        break;
                                    // Изготовленные
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

                            if (doFilteringByDelivery)
                            {
                                includeByDelivery = false;
                                if (deliveryId == row.CheckGet("DELIVERY_ID").ToInt())
                                {
                                    includeByDelivery = true;
                                }
                            }

                            if (includeByStatus && includeByDelivery)
                            {
                                items.Add(row);
                            }
                        }
                        Grid.Items = items;
                    }
                }
            }
        }

        /// <summary>
        /// Формирования ярлыка на образец
        /// </summary>
        /// <param name="p">Примечание технолога</param>
        private async void MakeSampleLabel(Dictionary<string, string> p)
        {
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
        /// Формирование отчета со списком образцов. 
        /// </summary>
        private void MakeListReport()
        {
            if (Grid.Items != null)
            {
                if (Grid.Items.Count > 0)
                {
                    var list = new List<Dictionary<string, string>>();
                    // Добавляем в список записи, отмеченные флажками
                    foreach (var row in Grid.Items)
                    {
                        if (row.CheckGet("CHECKING").ToBool())
                        {
                            list.Add(row);
                        }
                    }

                    var completedReport = new SampleTaskCompletedReport();
                    completedReport.DeliveryType = DeliveryType.SelectedItem.Value;
                    // Если не отмечено ни одной записи, передаём все записи
                    if (list.Count > 0)
                    {
                        completedReport.SampleList = list;
                    }
                    else
                    {
                        completedReport.SampleList = Grid.Items;
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
        /// Установка нового статуса образца
        /// </summary>
        private async void SetStatus(int status)
        {
            if (SelectedItem != null)
            {
                bool resume = true;
                int sampleId = SelectedItem.CheckGet("ID").ToInt();
                string statusName = "Передан";
                int undoStatus = 0;
                switch (status)
                {
                    case 1:
                        statusName = "В работе";
                        undoStatus = 1;
                        break;

                    case 3:
                        statusName = "Изготовлен";
                        undoStatus = 1;
                        break;
                }

                if (sampleId == 0)
                    resume = false;

                if (resume)
                {
                    var dw = new DialogWindow($"Вы действительно хотите поставить статус {statusName}?", "Смена статуса", "", DialogWindowButtons.YesNo);
                    if ((bool)dw.ShowDialog())
                    {
                        if (dw.ResultButton == DialogResultButton.Yes)
                        {
                            var q = new LPackClientQuery();
                            q.Request.SetParam("Module", "Preproduction");
                            q.Request.SetParam("Object", "Samples");
                            q.Request.SetParam("Action", "UpdateStatus");

                            q.Request.SetParam("SAMPLE_ID", sampleId.ToString());
                            q.Request.SetParam("STATUS", status.ToString());
                            q.Request.SetParam("UNDO_STATUS", undoStatus.ToString());

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
                                        // пришел непустой ответ, обновляем грид
                                        Grid.LoadItems();
                                    }
                                }
                            }
                            else
                            {
                                q.ProcessError();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Обновление типа доставки образца
        /// </summary>
        private void UpdateDeliveryType()
        {
            if (SelectedItem != null)
            {
                var sampleUpdateDelivery = new SampleUpdateDelivery();
                sampleUpdateDelivery.ReceiverName = TabName;
                sampleUpdateDelivery.Edit(SelectedItem);
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
        /// Получает с сервера и открывает файл техкарты для образца с линии
        /// </summary>
        private async void OpenTechnologicalCard()
        {
            int productId = SelectedItem.CheckGet("PRODUCT_ID").ToInt();
            if (productId > 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "SampleTask");
                q.Request.SetParam("Action", "GetTechCard");
                q.Request.SetParam("PRODUCT_ID", productId.ToString());
                await Task.Run(() =>
                {
                    q.DoQuery();
                }
                );

                if (q.Answer.Status == 0)
                {
                    Central.OpenFile(q.Answer.DownloadFilePath);
                }
                else if (q.Answer.Error.Code == 145)
                {
                    q.ProcessError();
                }
            }


            int j = 0;

        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void ViewButton_Click(object sender, RoutedEventArgs e)
        {
            int i = Grid.Items.Count;
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void SampleStatus_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void DeliveryType_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
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

        private void PrintMenuButton_Click(object sender, RoutedEventArgs e)
        {
            PrintContextMenu.IsOpen = true;
        }

        private void OpenTechCardItem_Click(object sender, RoutedEventArgs e)
        {
            OpenTechnologicalCard();
        }
    }
}
