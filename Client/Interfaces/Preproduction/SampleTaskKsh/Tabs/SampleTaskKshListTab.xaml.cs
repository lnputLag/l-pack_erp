using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Логика взаимодействия для SampleTaskKshListTab.xaml
    /// </summary>
    public partial class SampleTaskKshListTab : ControlBase
    {
        public SampleTaskKshListTab()
        {
            InitializeComponent();
            DestFolder = Central.Config.DownloadFolder; // @"c:\Work\tmp";
            if (string.IsNullOrEmpty(DestFolder))
            {
                DestFolder = @"c:\temp";
            }
            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/samples_new/task_samples";
            RoleName = "[erp]sample_task_ksh";
        }

        /// <summary>
        /// Номер плоттера (3)
        /// </summary>
        int PlotterNum;

        /// <summary>
        /// Папкадля сохранения чертежей
        /// </summary>
        string DestFolder;
        /// <summary>
        /// Имя вкладки
        /// </summary>
        public string TabName;
        /// <summary>
        /// Данные выбранной в гриде строки
        /// </summary>
        public Dictionary<string, string> SampleSelectedItem;
        /// <summary>
        /// Время последнего обновления данных. Требуется для определения текущей смены
        /// </summary>
        private DateTime LastUpdated;
        /// <summary>
        /// Назначение примечания: 1 - для ярлыка, 2 - для образца
        /// </summary>
        private int NoteTarget;

        /// <summary>
        /// Отображение статьи в справочной системе
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp(DocumentationUrl);
        }

        /// <summary>
        /// Инициализация
        /// </summary>
        public void Init()
        {
            LastUpdated = DateTime.Now;
            LoadRef();
            SampleGridInit();

            UIUtil.ProcessPermissions(RoleName, this);
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.IndexOf("PreproductionSample") > -1)
            {
                if (m.ReceiverName.IndexOf(TabName) > -1)
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            SampleGrid.LoadItems();
                            break;

                        case "SetReason":
                            var p = (Dictionary<string, string>)m.ContextObject;
                            FinishProduce(p);
                            break;

                        // Окно ввода примечаний
                        case "SaveNote":
                            var answer = (Dictionary<string, string>)m.ContextObject;
                            if (NoteTarget == 1)
                            {
                                MakeSampleLabel(answer);
                            }
                            else if (NoteTarget == 2)
                            {
                                SaveNote(answer);
                            }
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// обработчик системы навигации по URL
        /// </summary>
        public void ProcessNavigation()
        {
            PlotterNum = 3;

            //параметры запуска
            var p = Central.Navigator.Address.Params;

            var plotterNum = p.CheckGet("plotter_num").ToInt();
            if (plotterNum > 0)
            {
                if (plotterNum.ContainsIn(3))
                {
                    PlotterNum = plotterNum;
                    SetPlotterButton.IsEnabled = false;
                }
            }

            SetPlotterButton.Content = $"Плоттер {PlotterNum}";

            Init();
        }

        /// <summary>
        /// Получение справочников
        /// </summary>
        private async void LoadRef()
        {
            
            
            // список технологов для выпадающего списка
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "ListRef");
            q.Request.SetParam("FACTORY_ID", "2");

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var technologDS = ListDataSet.Create(result, "TECHNOLOGS");
                    var list = new Dictionary<string, string>()
                    {
                        { "-1", " " },
                    };
                    foreach (var item in technologDS.Items)
                    {
                        if (item.CheckGet("INGROUP").ToInt() == 1)
                        {
                            //Исключаем обезличенного технолога из списка
                            int userId = item.CheckGet("ID").ToInt();
                            if (userId != 398)
                            {
                                list.CheckAdd(userId.ToString(), item["FIO"]);
                            }
                        }
                    }
                    TechnologName.Items = list;
                    // Если активный пользователь есть в списке, установим его в выбранном значении
                    string emplId = Central.User.EmployeeId.ToString();
                    if (list.ContainsKey(emplId))
                    {
                        TechnologName.SetSelectedItemByKey(emplId);
                    }
                    else
                    {
                        TechnologName.SetSelectedItemByKey("-1");
                    }
                }
            }

            // Список типов доставки
            DeliveryType.Items = DeliveryTypes.ExtendItems();
            DeliveryType.SetSelectedItemByKey("-1");
        }

        /// <summary>
        /// Инициализация таблицы заданий для изготовления образцов
        /// </summary>
        private void SampleGridInit()
        {
            var columns = new List<DataGridHelperColumn>()
            {
                new DataGridHelperColumn()
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    Doc="Номер по порядку в списке",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="ИД очереди",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ИД образца",
                    Path="SAMPLE_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Тип производства",
                    Path="PRODUCTION_TYPE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Клиент",
                    Path="CUSTOMER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=24,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row=>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";
                                var dtCompleted = row.CheckGet("DT_COMPLITED").ToDateTime("dd.MM.yyyy");

                                if (DateTime.Compare(DateTime.Now.Date, dtCompleted) > 0)
                                {
                                    color = HColor.Red;
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
                    Header="Номер образца",
                    Path="SAMPLE_NUM",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Размеры образца",
                    Path="SAMPLE_SIZE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Тип изделия",
                    Path="SAMPLE_CLASS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Размеры развертки",
                    Path="BLANK_SIZE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Код FEFCO",
                    Path="FEFCO",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Клеить",
                    Path="GLUING",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Статус",
                    Path="STATUS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Количество изделий",
                    Path="QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="На изготовление, минут",
                    Path="ESTIMATE_TIME",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Начать до",
                    Path="START_TIME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Начато",
                    Path="BEGIN_DTTM",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Картон",
                    Path="CARDBOARD_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("ANY_CARTON_FLAG").ToInt() == 1)
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
                    Header="Номер картона",
                    Path="CARDBOARD_NUM",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("ANY_CARTON_FLAG").ToInt() == 1)
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
                    Header="Габариты сырья",
                    Path="RAW_SIZE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Место хранения",
                    Path="RACK_PLACE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Листов для задания",
                    Path="BLANK_QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Тип упаковки",
                    Path="PACKING_TYPE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Дополнительные требования",
                    Path="NAME_NOTE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание технолога",
                    Path="TECHNOLOG_NOTE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Чертеж",
                    Path="DESIGN_FILE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="Чертеж в другом формате",
                    Path="DESIGN_FILE_OTHER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="Сообщений от клиента",
                    Path="UNREAD_MSG",
                    ColumnType=ColumnTypeRef.Double,
                    Format="N0",
                    Width2=6,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (!string.IsNullOrEmpty(row.CheckGet("CHAT_MSG")))
                                {
                                    color = HColor.YellowOrange;
                                }
                                if (row.CheckGet("UNREAD_MSG").ToInt() > 0)
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
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Сообщений от коллег",
                    Path="UNREAD_MESSAGE_QTY",
                    ColumnType=ColumnTypeRef.Double,
                    Format="N0",
                    Width2=6,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
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
                    Header="Путь к чертежу",
                    Path="DESIGN_FILE",
                    ColumnType=ColumnTypeRef.String,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Путь к чертежу в другом формате",
                    Path="DESIGN_FILE_OTHER",
                    ColumnType=ColumnTypeRef.String,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Код статуса",
                    Path="STATUS_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Завершено",
                    Path="END_DTTM",
                    ColumnType=ColumnTypeRef.String,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Код типа доставки",
                    Path="DELIVERY_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Код типа упаковки",
                    Path="PACKING_TYPE_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Тип изготовления",
                    Path="PRODUCTION_TYPE_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Флаг любой картон",
                    Path="ANY_CARTON_FLAG",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ИД профиля картона",
                    Path="PROFILE_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ИД картона",
                    Path="CARDBOARD_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Признак завершения на станке переработки",
                    Path="PZ_END",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ID чата с коллегами",
                    Path="CHAT_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Плановая дата изготовления",
                    Path="DT_COMPLITED",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            SampleGrid.SetColumns(columns);
            SampleGrid.SetPrimaryKey("ID");
            SampleGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

            // раскраска строк
            SampleGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                {
                    StylerTypeRef.BackgroundColor,
                    (Dictionary<string, string> row) =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        // Начали задание на плоттере
                        if (!string.IsNullOrEmpty(row.CheckGet("BEGIN_DTTM")) && string.IsNullOrEmpty(row.CheckGet("END_DTTM")))
                        {
                            color = HColor.Blue;
                        }
                        // Едут задание на линии
                        if ((row.CheckGet("PZ_END").ToInt() == 1) && (row.CheckGet("STATUS_ID").ToInt() == 1))
                        {
                            color = HColor.Yellow;
                        }
                        // Полностью завершено задание на линии
                        if ((row.CheckGet("PZ_END").ToInt() == 2) && (row.CheckGet("STATUS_ID").ToInt() == 1))
                        {
                            color = HColor.Pink;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
                {
                    StylerTypeRef.ForegroundColor,
                    (Dictionary<string, string> row) =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        int statusId = row.CheckGet("STATUS_ID").ToInt();
                        switch (statusId)
                        {
                            // Статус В работе
                            case 1:
                                // Образец с линии
                                if (row.CheckGet("PRODUCTION_TYPE_ID").ToInt() == 1)
                                {
                                    color = HColor.OliveFG;
                                }
                                //есть время завершения - отменён
                                if (!string.IsNullOrEmpty(row.CheckGet("END_DTTM")))
                                {
                                    color = HColor.MagentaFG;
                                }
                                break;
                            // Статус Изготовлен
                            case 3:
                                color = HColor.GreenFG;
                                break;
                            // Статус Передан
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
                },
            };

            SampleGrid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
            //SampleGrid.UseSorting = false;
            SampleGrid.OnFilterItems = FilterItems;

            SampleGrid.Menu = new Dictionary<string, DataGridContextMenuItem>()
            {
                { "Copy", new DataGridContextMenuItem(){
                    Header="Копировать текст в буфер обмена",
                    Action=()=>
                    {
                        SampleGrid.CopyCellValue();
                    }
                }},
                { "Separator0", new DataGridContextMenuItem() {
                    Header="-",
                }},
                { "ShowDesign", new DataGridContextMenuItem(){
                    Header="Показать чертеж",
                    Action=()=>
                    {
                        ShowDesign(0);
                    }
                }},
                { "SaveDesign", new DataGridContextMenuItem(){
                    Header="Сохранить чертеж",
                    Action=()=>
                    {
                        SaveDesign(0);
                    }
                }},
                { "ShowAlterDesign", new DataGridContextMenuItem(){
                    Header="Показать другой формат чертежа",
                    Action=()=>
                    {
                        ShowDesign(1);
                    }
                }},
                { "SaveAlterDesign", new DataGridContextMenuItem(){
                    Header="Сохранить другой формат чертежа",
                    Action=()=>
                    {
                        SaveDesign(1);
                    }
                }},
                { "ShowDrawing", new DataGridContextMenuItem(){
                    Header="Показать схему",
                    Action=()=>
                    {
                        ShowDrawing();
                    }
                }},
                { "Separator1", new DataGridContextMenuItem() {
                    Header="-",
                }},
                { "Transfer", new DataGridContextMenuItem(){
                    Header="Отметить передачу",
                    Tag = "access_mode_full_access",
                    Action=()=>
                    {
                        SetTransfer();
                    }
                }},
                { "SelectRaw", new DataGridContextMenuItem(){
                    Header="Сменить картон",
                    Tag = "access_mode_full_access",
                    Action=()=>
                    {
                        SelectRaw();
                    }
                }},
                { "EditNote", new DataGridContextMenuItem(){
                    Header="Изменить примечание",
                    Tag = "access_mode_full_access",
                    Action=()=>
                    {
                        GetNote(SampleSelectedItem.CheckGet("SAMPLE_ID").ToInt(), SampleSelectedItem.CheckGet("TECHNOLOG_NOTE"));
                    }
                }},
                { "OpenChat",
                    new DataGridContextMenuItem()
                    {
                        Header="Открыть чат с клиентом",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                            OpenChat(0);
                        }
                }},
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
            };

            SampleGrid.OnLoadItems = SampleLoadItems;
            SampleGrid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    SampleGridUpdateActions(selectedItem);
                }
            };
            SampleGrid.Init();
        }

        /// <summary>
        /// Обновление действий для выбранной записи
        /// </summary>
        /// <param name="selectedItem">выбранная запись</param>
        private void SampleGridUpdateActions(Dictionary<string, string> selectedItem)
        {
            SampleSelectedItem = selectedItem;
            int status = SampleSelectedItem.CheckGet("STATUS_ID").ToInt();
            bool started = !SampleSelectedItem.CheckGet("BEGIN_DTTM").IsNullOrEmpty();
            bool completed = !SampleSelectedItem.CheckGet("END_DTTM").IsNullOrEmpty();

            SampleGrid.Menu["ShowDesign"].Enabled = SampleSelectedItem.CheckGet("DESIGN_FILE_IS").ToBool();
            SampleGrid.Menu["ShowAlterDesign"].Enabled = SampleSelectedItem.CheckGet("DESIGN_FILE_OTHER_IS").ToBool();
            SampleGrid.Menu["SaveDesign"].Enabled = SampleSelectedItem.CheckGet("DESIGN_FILE_IS").ToBool();
            SampleGrid.Menu["SaveAlterDesign"].Enabled = SampleSelectedItem.CheckGet("DESIGN_FILE_OTHER_IS").ToBool();
            SampleGrid.Menu["SelectRaw"].Enabled = (status == 1) && (SampleSelectedItem.CheckGet("CARDBOARD_ID").ToInt() > 0);

            StartButton.IsEnabled = !started && (status == 1);
            FinishButton.IsEnabled = started && !completed;
            StopButton.IsEnabled = started && !completed;
            SampleGrid.Menu["Transfer"].Enabled = status == 3;
        }

        /// <summary>
        /// Обработка строк таблицы
        /// </summary>
        public void FilterItems()
        {
            var items = new List<Dictionary<string, string>>();
            if (SampleGrid.Items != null)
            {
                if (SampleGrid.Items.Count > 0)
                {
                    int deliveryId = DeliveryType.SelectedItem.Key.ToInt();
                    foreach (var item in SampleGrid.Items)
                    {
                        bool include = true;
                        if (deliveryId > -1)
                        {
                            if (item.CheckGet("DELIVERY_ID").ToInt() != deliveryId)
                            {
                                include = false;
                            }
                        }

                        // Дополнительная обработка включаемых строк
                        if (include)
                        {
                            // Если есть отменённые задания, поставим им статус Отменён
                            int statusId = item.CheckGet("STATUS_ID").ToInt();
                            if ((statusId == 1) && !item.CheckGet("END_DTTM").IsNullOrEmpty())
                            {
                                item.CheckAdd("STATUS", "Отменен");
                            }

                            // Заполним имена файлов
                            string designPath = item.CheckGet("DESIGN_FILE");
                            if (!designPath.IsNullOrEmpty())
                            {
                                var fn = Path.GetFileName(designPath);
                                item.CheckAdd("DESIGN_FILE_NAME", fn);
                            }
                            string designOtherPath = item.CheckGet("DESIGN_FILE_OTHER");
                            if (!designOtherPath.IsNullOrEmpty())
                            {
                                var fn = Path.GetFileName(designOtherPath);
                                item.CheckAdd("DESIGN_FILE_OTHER_NAME", fn);
                            }

                            items.Add(item);
                        }
                    }

                    SampleGrid.Items = items;
                }
            }
        }

        /// <summary>
        /// Вычисление времени начала и окончания смены
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, DateTime> GetShiftTime()
        {
            var nowTime = DateTime.Now;
            var result = new Dictionary<string, DateTime>()
            {
                { "StartShift", nowTime },
                { "EndShift", nowTime},
            };
            var morning = nowTime.Date.AddHours(8);
            var evening = nowTime.Date.AddHours(20);

            if (DateTime.Compare(morning, nowTime) > 0)
            {
                result["StartShift"] = evening.AddDays(-1);
                result["EndShift"] = morning;
            }
            else if (DateTime.Compare(evening, nowTime) < 0)
            {
                result["StartShift"] = evening;
                result["EndShift"] = morning.AddDays(1);
            }
            else
            {
                result["StartShift"] = morning;
                result["EndShift"] = evening;
            }

            return result;
        }

        /// <summary>
        /// Загрузка данных в таблицу. Загружаем очередь за текущую смену
        /// </summary>
        private async void SampleLoadItems()
        {
            // Определяем время начала и окончания смены
            // Если последнее обновление было до начала текущей смены, то сбрасываем имя технолога
            var shiftTime = GetShiftTime();
            DateTime startShift = DateTime.Now.Date;
            if (shiftTime.ContainsKey("StartShift"))
            {
                startShift = shiftTime["StartShift"];
            }
            if (DateTime.Compare(LastUpdated, startShift) < 0)
            {
                TechnologName.SetSelectedItemByKey("-1");
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "SampleTask");
            q.Request.SetParam("Action", "List");

            q.Request.SetParam("MACHINE", PlotterNum.ToString());
            q.Request.SetParam("BEGIN_TIME", startShift.ToString("dd.MM.yyyy HH:mm:ss"));
            q.Request.SetParam("STATUS", "0");

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
                    var sampleDS = ListDataSet.Create(result, "SCHEDULE");

                    // Добавим колонку с плановым временем начала изготовления
                    var startTime = DateTime.Now;
                    var startShiftTime = GetShiftTime();
                    if (startShiftTime.ContainsKey("StartShift"))
                    {
                        startTime = startShiftTime["StartShift"];
                    }
                    foreach (var item in sampleDS.Items)
                    {
                        item.CheckAdd("START_TIME", startTime.ToString("dd.MM HH:mm"));
                        var minutes = item.CheckGet("ESTIMATE_TIME").ToDouble();
                        startTime = startTime.AddMinutes(minutes);
                    }

                    SampleGrid.UpdateItems(sampleDS);
                    LastUpdated = DateTime.Now;
                }
            }
        }

        /// <summary>
        /// Отметка начала изготовления образца
        /// </summary>
        private async void StartProduce()
        {
            if (SampleSelectedItem != null)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "SampleTask");
                q.Request.SetParam("Action", "Start");

                q.Request.SetParam("ID", SampleSelectedItem.CheckGet("ID"));

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
                            SampleGrid.LoadItems();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Отметка завершения изготовления образца
        /// </summary>
        /// <param name="p">параметры запроса</param>
        private async void FinishProduce(Dictionary<string, string> p)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "SampleTask");
            q.Request.SetParam("Action", "Finish");
            q.Request.SetParams(p);

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
                        SampleGrid.LoadItems();
                    }
                }
            }
        }

        /// <summary>
        /// Установка отметки передачи образца
        /// </summary>
        private async void SetTransfer()
        {
            if (SampleSelectedItem != null)
            {
                bool resume = true;
                int sampleId = SampleSelectedItem.CheckGet("SAMPLE_ID").ToInt();

                if (sampleId == 0)
                    resume = false;

                int technologId = TechnologName.SelectedItem.Key.ToInt();
                if (resume)
                {
                    if (technologId <= 0)
                        resume = false;
                }

                if (resume)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "Samples");
                    q.Request.SetParam("Action", "UpdateStatus");

                    q.Request.SetParam("SAMPLE_ID", sampleId.ToString());
                    q.Request.SetParam("STATUS", "7");
                    q.Request.SetParam("EMPLOYEE_ID", technologId.ToString());

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
                                SampleGrid.LoadItems();
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

        /// <summary>
        /// Открывает файл чертежа
        /// </summary>
        /// <param name="designType"></param>
        private void ShowDesign(int designType)
        {
            string sourceFile;

            if (designType == 0)
            {
                sourceFile = SampleSelectedItem.CheckGet("DESIGN_FILE");
            }
            else
            {
                sourceFile = SampleSelectedItem.CheckGet("DESIGN_FILE_OTHER");
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
        /// Сохраняет файл чертежа в папке загрузок из настроек
        /// </summary>
        /// <param name="designType"></param>
        private void SaveDesign(int designType)
        {
            // Путь к файлу
            string sourceFile;

            if (designType == 0)
            {
                sourceFile = SampleSelectedItem.CheckGet("DESIGN_FILE");
            }
            else
            {
                sourceFile = SampleSelectedItem.CheckGet("DESIGN_FILE_OTHER");
            }

            string fileName = Path.GetFileName(sourceFile);
            string newPath = Path.Combine(DestFolder, fileName);
            File.Copy(sourceFile, newPath, true);
        }

        /// <summary>
        /// Формирование итогового отчета по изготовленным за смену образцам
        /// </summary>
        private async void MakeTaskReport()
        {
            if (SampleSelectedItem != null)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "Samples");
                q.Request.SetParam("Action", "GetTaskReport");

                q.Request.SetParam("ID_LIST", SampleSelectedItem.CheckGet("SAMPLE_ID"));

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
        /// Вызов вкладки редактирования примечания
        /// </summary>
        /// <param name="sampleId"></param>
        public void GetNote(int sampleId, string note = "")
        {
            NoteTarget = 2;
            var sampleNote = new SampleNote();
            sampleNote.ReceiverName = TabName;
            var p = new Dictionary<string, string>()
            {
                { "ID", sampleId.ToString() },
                { "NOTE", note },
            };
            sampleNote.Edit(p);
        }

        /// <summary>
        /// Сохранение примечания
        /// </summary>
        /// <param name="p"></param>
        public async void SaveNote(Dictionary<string, string> p)
        {
            NoteTarget = 0;

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
                        SampleGrid.LoadItems();
                    }
                }
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
            q.Request.SetParam("ID", SampleSelectedItem.CheckGet("SAMPLE_ID"));

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
        /// Формирования ярлыка на образец
        /// </summary>
        /// <param name="technologsNote">Примечание технолога</param>
        private async void MakeSampleLabel(Dictionary<string, string> p)
        {
            NoteTarget = 0;

            if (SampleSelectedItem != null)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "Samples");
                q.Request.SetParam("Action", "GetTaskReport");

                q.Request.SetParam("ID_LIST", SampleSelectedItem.CheckGet("SAMPLE_ID"));

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
        }

        /// <summary>
        /// Формирует итоговый отчет по сделанным образцам для передачи в доставку
        /// </summary>
        private void MakeDeliveryReport()
        {
            if (SampleGrid.Items != null)
            {
                if (SampleGrid.Items.Count > 0)
                {
                    var list = new List<Dictionary<string, string>>();
                    // Отбираем из содержимого таблицы только записи в статусе Изготовлен
                    foreach (var item in SampleGrid.Items)
                    {
                        if (item.CheckGet("STATUS_ID").ToInt() == 7)
                        {
                            list.Add(item);
                        }
                    }

                    var completedReport = new SampleTaskCompletedReport();
                    completedReport.FieldIdName = "SAMPLE_ID";
                    completedReport.DeliveryType = DeliveryType.SelectedItem.Value;
                    completedReport.SampleList = list;
                    completedReport.Make();
                }
            }
        }

        /// <summary>
        /// Открытие вкладки с чатом по образцу
        /// </summary>
        /// <param name="chatType">Тип чата: 0 - чат с клиентом, 1 - чат с коллегами</param>
        private void OpenChat(int chatType = 0)
        {
            if (SampleSelectedItem != null)
            {
                var chatFrame = new SampleChat();
                chatFrame.ObjectId = SampleSelectedItem.CheckGet("SAMPLE_ID").ToInt();
                chatFrame.ReceiverName = TabName;
                chatFrame.Recipient = 6;
                chatFrame.ChatType = chatType;
                chatFrame.ChatId = SampleSelectedItem.CheckGet("CHAT_ID").ToInt();

                chatFrame.Edit();
            }
        }

        /// <summary>
        /// Вызывает форму смены картона для изготовления образца
        /// </summary>
        private void SelectRaw()
        {
            int rawId = SampleSelectedItem.CheckGet("CARDBOARD_ID").ToInt();
            int profileId = SampleSelectedItem.CheckGet("PROFILE_ID").ToInt();

            if ((rawId > 0) && (profileId > 0))
            {
                var selectRaw = new SampleTaskSelectRaw();
                selectRaw.ReceiverName = TabName;
                var p = new Dictionary<string, string>()
                {
                    { "ID", SampleSelectedItem.CheckGet("ID") },
                    { "IDC", rawId.ToString() },
                    { "PROFILE_ID", profileId.ToString() },
                    { "ANY_CARTON_FLAG", SampleSelectedItem.CheckGet("ANY_CARTON_FLAG") },
                };
                selectRaw.Edit(p);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            SampleGrid.LoadItems();
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            StartProduce();
        }

        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            var technologId = TechnologName.SelectedItem.Key.ToInt();
            if (technologId > 0)
            {
                var p = new Dictionary<string, string>()
                {
                    { "ID", SampleSelectedItem.CheckGet("ID") },
                    { "SAMPLE_ID", SampleSelectedItem.CheckGet("SAMPLE_ID") },
                    { "TECHNOLOG_ID", TechnologName.SelectedItem.Key },
                    { "SUCCESS", "1" },
                };

                FinishProduce(p);
            }
            else
            {
                var dw = new DialogWindow("В поле технолог должен быть указан, кто изготовил образец", "Изготовление образцов");
                dw.ShowDialog();
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            var technologId = TechnologName.SelectedItem.Key.ToInt();
            if (technologId > 0)
            {
                var p = new Dictionary<string, string>()
                {
                    { "ID", SampleSelectedItem.CheckGet("ID") },
                    { "SAMPLE_ID", SampleSelectedItem.CheckGet("SAMPLE_ID") },
                    { "TECHNOLOG_ID", TechnologName.SelectedItem.Key },
                    { "SUCCESS", "0" },
                };

                var reasonForm = new SampleTaskStopReason();
                reasonForm.TaskValues = p;
                reasonForm.ReceiverName = TabName;
                reasonForm.Show();
            }
            else
            {
                var dw = new DialogWindow("В поле технолог должен быть указан, кто изготавливал образец", "Изготовление образцов");
                dw.ShowDialog();
            }
        }

        private void PrintTask_Click(object sender, RoutedEventArgs e)
        {
            MakeTaskReport();
        }

        private void PrintLabel_Click(object sender, RoutedEventArgs e)
        {
            if (SampleSelectedItem != null)
            {
                NoteTarget = 1;
                var sampleNote = new SampleNote();
                sampleNote.ReceiverName = TabName;
                var values = new Dictionary<string, string>()
                {
                    { "ID", SampleSelectedItem.CheckGet("SAMPLE_ID") },
                    { "NOTE", "" },
                    { "SHOW_CARDBOARD", "0"},
                };
                sampleNote.CardboardCheckBoxBlock.Visibility = Visibility.Collapsed;
                sampleNote.Edit(values);
            }
        }

        private void DeliveryType_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SampleGrid.UpdateItems();
        }

        private void DeliveryReport_Click(object sender, RoutedEventArgs e)
        {
            MakeDeliveryReport();
        }

        private void SetPlotter_Click(object sender, RoutedEventArgs e)
        {
            PlotterNum = 3;
            SetPlotterButton.Content = $"Плоттер {PlotterNum}";
            SampleGrid.LoadItems();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var appendFrame = new SampleAppend();
            appendFrame.BackTabName = TabName;
            appendFrame.PlotterNum = PlotterNum;
            appendFrame.Show();
        }

        private void PrintMenuButton_Click(object sender, RoutedEventArgs e)
        {
            PrintContextMenu.IsOpen = true;
        }
    }
}
