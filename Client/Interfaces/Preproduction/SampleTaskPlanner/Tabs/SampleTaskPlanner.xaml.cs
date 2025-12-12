using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Рабочее место планировщика работы плоттерной мастерской
    /// </summary>
    public partial class SampleTaskPlanner : ControlBase
    {
        public SampleTaskPlanner()
        {
            ControlTitle = "Планировщик образцов";
            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/samples_new/plannibg_samples";
            RoleName = "[erp]sample_task_planner";

            OnLoad = () =>
            {
                InitializeComponent();

                SetDefaults();
                SampleGridInit();
                PlotterGridInit();

                ProcessPermissions();
            };

            OnUnload = () =>
            {
                SampleGrid.Destruct();
                Plotter1Grid.Destruct();
                Plotter2Grid.Destruct();
                Plotter3Grid.Destruct();
            };

            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverGroup == "PreproductionSample")
                {
                    if (msg.ReceiverName == ControlName)
                    {
                        ReceivedData.Clear();
                        if (msg.ContextObject != null)
                        {
                            ReceivedData = (Dictionary<string, string>)msg.ContextObject;
                        }
                        ProcessCommand(msg.Action);
                    }
                }
            };

            OnFocusGot = () =>
            {
                SampleGrid.ItemsAutoUpdate = true;
                SampleGrid.Run();
            };

            OnFocusLost = () =>
            {
                SampleGrid.ItemsAutoUpdate = false;
            };

            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    switch (e.Key)
                    {
                        case Key.F1:
                            ProcessCommand("help");
                            e.Handled = true;
                            break;
                        case Key.F5:
                            SampleGrid.LoadItems();
                            e.Handled = true;
                            break;

                        case Key.Home:
                            SampleGrid.SetSelectToFirstRow();
                            e.Handled = true;
                            break;

                        case Key.End:
                            SampleGrid.SetSelectToLastRow();
                            e.Handled = true;
                            break;
                    }
                }

                /*
                if (!e.Handled)
                {
                    SampleGrid.ProcessKeyboardEvents(e);
                }
                */
            };

        }
        /// <summary>
        /// Данные для таблиц
        /// </summary>
        public ListDataSet SampleDS { get; set; }

        /// <summary>
        /// Выбранные записи в таблицах
        /// </summary>
        public Dictionary<string, string> SampleSelectedItem;
        public Dictionary<string, string> Plotter1SelectedItem;
        public Dictionary<string, string> Plotter2SelectedItem;
        public Dictionary<string, string> Plotter3SelectedItem;

        private Dictionary<string, string> ReceivedData;

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel(this.RoleName);
            var userAccessMode = mode;
            switch (mode)
            {
                case Role.AccessMode.Special:
                    break;

                case Role.AccessMode.FullAccess:
                    break;

                case Role.AccessMode.ReadOnly:
                default:
                    break;
            }

            List<Button> buttons = UIUtil.GetVisualChilds<Button>(this.Content as DependencyObject);
            if (buttons != null && buttons.Count > 0)
            {
                foreach (var button in buttons)
                {
                    var buttonTagList = UIUtil.GetTagList(button);
                    var accessMode = Acl.FindTagAccessMode(buttonTagList);
                    if (accessMode > userAccessMode)
                    {
                        button.IsEnabled = false;
                    }
                }
            }

            if (SampleGrid != null && SampleGrid.Menu != null && SampleGrid.Menu.Count > 0)
            {
                foreach (var manuItem in SampleGrid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }

            if (Plotter1Grid != null && Plotter1Grid.Menu != null && Plotter1Grid.Menu.Count > 0)
            {
                foreach (var manuItem in Plotter1Grid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }

            if (Plotter2Grid != null && Plotter2Grid.Menu != null && Plotter2Grid.Menu.Count > 0)
            {
                foreach (var manuItem in Plotter2Grid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }

            if (Plotter3Grid != null && Plotter3Grid.Menu != null && Plotter3Grid.Menu.Count > 0)
            {
                foreach (var manuItem in Plotter3Grid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// Обработка и выполнение команд
        /// </summary>
        /// <param name="command"></param>
        public void ProcessCommand(string command)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "refresh":
                        SampleGrid.LoadItems();
                        break;

                    case "update":
                        SampleGrid.UpdateItems();
                        break;

                    case "help":
                        ShowHelp();
                        break;

                    //case "append":
                    //    AppendToSchedule(ReceivedData);
                    //    break;

                    case "setreason":
                        SetRevision(ReceivedData);
                        break;

                    case "savenote":
                        SaveNote(ReceivedData);
                        break;

                    case "cardboardselected":
                        // Меняем картон если есть данные
                        if (ReceivedData.Keys.Count > 0)
                        {
                            SetRawCardboard(ReceivedData);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Отображение статьи в справочной системе
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp(DocumentationUrl);
        }

        /// <summary>
        /// Выполнение начальных действий и заполнение значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            SampleDS = new ListDataSet();

            SampleSelectedItem = new Dictionary<string, string>();
            ReceivedData = new Dictionary<string, string>();

            Up1Button.IsEnabled = false;
            Down1Button.IsEnabled = false;
            Up2Button.IsEnabled = false;
            Down2Button.IsEnabled = false;
            Up3Button.IsEnabled = false;
            Down3Button.IsEnabled = false;
        }

        /// <summary>
        /// Инициализация таблицы списка образцов
        /// </summary>
        private void SampleGridInit()
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
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Дата заявки",
                    Path="DT_CREATED",
                    ColumnType=ColumnTypeRef.DateTime,
                    MinWidth=50,
                    MaxWidth=100,
                    Width2=5,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header="Плановая дата изготовления",
                    Path="DT_COMPLITED",
                    ColumnType=ColumnTypeRef.DateTime,
                    MinWidth=50,
                    MaxWidth=100,
                    Width2=5,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header="Заказчик",
                    Path="NAME_POK",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=280,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Размеры образца",
                    Path="SAMPLE_SIZE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=120,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header="Тип изделия",
                    Path="SAMPLE_CLASS",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=200,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Размеры развертки",
                    Path="BLANK_SIZE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=100,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header="Код FEFCO",
                    Path="FEFCO",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=40,
                    MaxWidth=60,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Клеить",
                    Path="GLUING_TEXT",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Количество изделий",
                    Path="QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=70,
                    Width2=3,
                },
                new DataGridHelperColumn
                {
                    Header="Дата отгрузки",
                    Path="SHIPMENT",
                    ColumnType=ColumnTypeRef.DateTime,
                    MinWidth=40,
                    MaxWidth=100,
                    Width2=8,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header="Дополнительные требования",
                    Path="NAME_NOTE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=250,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Картон",
                    Path="CARDBOARD_NAME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=100,
                    MaxWidth=280,
                    Width2=10,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (row["ANY_CARTON_FLAG"].ToInt() == 1)
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
                    MinWidth=40,
                    MaxWidth=100,
                    Width2=3,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (row["ANY_CARTON_FLAG"].ToInt() == 1)
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
                    Header="Листов в наличии",
                    Path="CARDBOARD_QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=50,
                    Width2=3,
                },
                new DataGridHelperColumn
                {
                    Header="Листов в резерве",
                    Path="RESERVE_QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=50,
                    Width2=3,
                },
                new DataGridHelperColumn
                {
                    Header="Тип доставки",
                    Path="DELIVERY_TYPE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=70,
                    MaxWidth=150,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Чертеж",
                    Path="DESIGN_FILE_IS",
                    ColumnType=ColumnTypeRef.Boolean,
                    MinWidth=30,
                    MaxWidth=40,
                    Width2=2,
                },
                new DataGridHelperColumn
                {
                    Header="Чертеж в другом формате",
                    Path="DESIGN_FILE_OTHER_IS",
                    ColumnType=ColumnTypeRef.Boolean,
                    MinWidth=30,
                    MaxWidth=40,
                    Width2=2,
                },
                new DataGridHelperColumn
                {
                    Header="Приложены файлы",
                    Path="FILE_IS",
                    ColumnType=ColumnTypeRef.Boolean,
                    MinWidth=30,
                    MaxWidth=40,
                    Width2=2,
                },
                new DataGridHelperColumn
                {
                    Header="Сообщений от клиента",
                    Path="CHAT_UNREAD",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=40,
                    Width2=2,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (row["CHAT_UNREAD"].ToInt() > 0)
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
                        },
                    },
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
                    Header="Примечание технолога",
                    Path="TECHNOLOG_NOTE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=70,
                    MaxWidth=100,
                    Width2=10,
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
                    Header="Длина развертки",
                    Path="BLANK_LENGTH",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Ширина развертки",
                    Path="BLANK_WIDTH",
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
                    Header="Марка картона",
                    Path="MARK_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Цвет картона",
                    Path="OUTER_COLOR_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ID картона",
                    Path="CARDBOARD_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Длина изделия",
                    Path="SAMPLE_LENGTH",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Ширина изделия",
                    Path="SAMPLE_WIDTH",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Высота изделия",
                    Path="SAMPLE_HEIGHT",
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
                new DataGridHelperColumn
                {
                    Header="Флаг отсутствия сырья",
                    Path="RAW_MISSING_FLAG",
                    ColumnType=ColumnTypeRef.Integer,
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
                    Header="Картон любой марки",
                    Path="ANY_CARTON_FLAG",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Признак склеивания",
                    Path="GLUING",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
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
            SampleGrid.SetColumns(columns);

            var today = DateTime.Now.Date;
            // раскраска строк
            SampleGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
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
                        // Если нет доступных листов картона
                        if (row["CARDBOARD_QTY"].ToInt() == 0)
                        {
                            color = HColor.OrangeFG;
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

            SampleGrid.PrimaryKey = "ID";

            SampleGrid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
            SampleGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            SampleGrid.Name = "Sample";
            SampleGrid.SearchText = SearchText;
            //SampleGrid.SelectItemMode = 0;

            SampleGrid.Menu = new Dictionary<string, DataGridContextMenuItem>()
            {
                { "SendToPlotter1", new DataGridContextMenuItem(){
                    Header="В очередь плоттера",
                    Tag = "access_mode_full_access",
                    Action=()=>
                    {
                        var i = PlotterTabs.SelectedIndex;
                        SendToPlotter(i + 1, SampleSelectedItem);
                    }
                }},
                { "Separator1", new DataGridContextMenuItem() {
                    Header="-",
                }},
                { "ShowDesign", new DataGridContextMenuItem(){
                    Header="Показать чертеж",
                    Action=()=>
                    {
                        ShowDesign(0);
                    }
                }},
                { "LoadDesign", new DataGridContextMenuItem(){
                    Header="Привязать чертеж",
                    Tag = "access_mode_full_access",
                    Action=()=>
                    {
                        LoadDesign(0);
                    }
                }},
                { "ShowAlterDesign", new DataGridContextMenuItem(){
                    Header="Показать другой формат чертежа",
                    Action=()=>
                    {
                        ShowDesign(1);
                    }
                }},
                { "LoadAlterDesign", new DataGridContextMenuItem(){
                    Header="Привязвть другой формат чертежа",
                    Tag = "access_mode_full_access",
                    Action=()=>
                    {
                        LoadDesign(1);
                    }
                }},
                { "ShowDesignFolder", new DataGridContextMenuItem(){
                    Header="Папка чертежа",
                    Action=()=>
                    {
                        DesignFolder();
                    }
                }},
                { "Separator2", new DataGridContextMenuItem() {
                    Header="-",
                }},
                { "SetBlankSize", new DataGridContextMenuItem(){
                    Header="Вычислить размер развертки",
                    Action=()=>
                    {
                        SetBlankSize();
                    }
                }},
                { "EditBlankSize", new DataGridContextMenuItem(){
                    Header="Изменить размер развертки",
                    Tag = "access_mode_full_access",
                    Action=()=>
                    {
                        EditBlankSize();
                    }
                }},
                { "ShowDrawing", new DataGridContextMenuItem(){
                    Header="Показать схему",
                    Action=()=>
                    {
                        ShowDrawing();
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
                { "Separator3", new DataGridContextMenuItem() {
                    Header="-",
                }},
                { "EditNote", new DataGridContextMenuItem(){
                    Header="Изменить примечание",
                    Tag = "access_mode_full_access",
                    Action=()=>
                    {
                        GetNote(SampleSelectedItem.CheckGet("ID").ToInt(), SampleSelectedItem.CheckGet("TECHNOLOG_NOTE"));
                    }
                }},
                {
                    "OpenAttachments",
                    new DataGridContextMenuItem()
                    {
                        Header="Прикрепленные файлы",
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

            SampleGrid.OnLoadItems = SampleLoadItems;
            SampleGrid.OnFilterItems = FilterSampleItems;
            SampleGrid.OnSelectItem = selectedItem =>
             {
                 if (selectedItem.Count > 0)
                 {
                     SampleGridUpdateActions(selectedItem);
                 }
             };
            SampleGrid.OnDblClick = selectedItem =>
            {
                Edit(selectedItem.CheckGet("ID").ToInt());
            };

            SampleGrid.Init();

            //SampleGrid.Run();
            //SampleGrid.Focus();
        }

        /// <summary>
        /// Инициализация таблиц очередей плоттеров
        /// </summary>
        private void PlotterGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=20,
                    MaxWidth=30,
                    Width2=2,
                },
                new DataGridHelperColumn
                {
                    Header="ИД образца",
                    Path="SAMPLE_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Плановая дата изготовления",
                    Path="DT_COMPLITED",
                    ColumnType=ColumnTypeRef.DateTime,
                    MinWidth=50,
                    MaxWidth=70,
                    Width2=5,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header="Заказчик",
                    Path="CUSTOMER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=120,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Размеры образца",
                    Path="SAMPLE_SIZE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=120,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header="Размеры развертки",
                    Path="BLANK_SIZE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=70,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header="Количество изделий",
                    Path="QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=20,
                    MaxWidth=50,
                    Width2=3,
                    Totals=(List<Dictionary<string,string>> rows) =>
                    {
                        var result=0;
                        if(rows != null)
                        {
                            foreach(Dictionary<string,string> row in rows)
                            {
                                result += row.CheckGet("QTY").ToInt();
                            }
                        }
                        return result;
                    },
                },
                new DataGridHelperColumn
                {
                    Header="На изготовление, минут",
                    Path="ESTIMATE_TIME",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=20,
                    MaxWidth=50,
                    Width2=3,
                },
                new DataGridHelperColumn
                {
                    Header="Начать до",
                    Path="START_TIME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=40,
                    MaxWidth=80,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header="Картон",
                    Path="CARDBOARD_NAME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=100,
                    MaxWidth=250,
                    Width2=14,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (row["ANY_CARTON_FLAG"].ToInt() == 1)
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
                    MinWidth=20,
                    MaxWidth=50,
                    Width2=3,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (row["ANY_CARTON_FLAG"].ToInt() == 1)
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
                    Header="Количество листов",
                    Path="CARDBOARD_QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=20,
                    MaxWidth=50,
                    Width2=3,
                },
                new DataGridHelperColumn
                {
                    Header="Габариты сырья",
                    Path="RAW_SIZE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=30,
                    MaxWidth=80,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Место хранения",
                    Path="RACK_PLACE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=30,
                    MaxWidth=50,
                    Width2=3,
                },
                new DataGridHelperColumn
                {
                    Header="Дополнительные требования",
                    Path="NAME_NOTE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=80,
                    MaxWidth=600,
                    Width2=15,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание технолога",
                    Path="TECHNOLOG_NOTE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=200,
                    Width2=12,
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
                    Header="Тип изготовления",
                    Path="PRODUCTION_TYPE",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Картон любой марки",
                    Path="ANY_CARTON_FLAG",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };

            // раскраска строк
            var today = DateTime.Now.Date;
            var rowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
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
                {
                    StylerTypeRef.ForegroundColor,
                    (Dictionary<string, string> row) =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        // Образец с линии
                        if (row["PRODUCTION_TYPE"].ToInt() == 1)
                        {
                            color = HColor.OliveFG;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };


            Plotter1Grid.SetColumns(columns);
            Plotter1Grid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
            Plotter1Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            Plotter1Grid.UseSorting = false;
            Plotter1Grid.AutoUpdateInterval = 0;

            Plotter1Grid.RowStylers = rowStylers;

            Plotter1Grid.Name = "Plotter1";
            Plotter1Grid.OnFilterItems = FilterPlotter1Items;

            Plotter1Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
            {
                { "EditTime", new DataGridContextMenuItem(){
                    Header="Изменить время",
                    Tag = "access_mode_full_access",
                    Action=()=>
                    {
                        EditTime(1);
                    }
                }},
                { "ShowTaskDesign", new DataGridContextMenuItem(){
                    Header="Показать чертеж",
                    Action=()=>
                    {
                        ShowTaskDesign(Plotter1SelectedItem, 0);
                    }
                }},
                { "ShowAlterTaskDesign", new DataGridContextMenuItem(){
                    Header="Показать другой формат чертежа",
                    Action=()=>
                    {
                        ShowTaskDesign(Plotter1SelectedItem, 1);
                    }
                }},
                { "SetBlankSize", new DataGridContextMenuItem(){
                    Header="Вычислить размер развертки",
                    Action=()=>
                    {
                        SetBlankSize(Plotter1SelectedItem.CheckGet("SAMPLE_ID").ToInt());
                    }
                }},
                { "EditNote", new DataGridContextMenuItem(){
                    Header="Изменить примечание",
                    Tag = "access_mode_full_access",
                    Action=()=>
                    {
                        GetNote(Plotter1SelectedItem.CheckGet("SAMPLE_ID").ToInt(), Plotter1SelectedItem.CheckGet("TECHNOLOG_NOTE"));
                    }
                }},
            };

            Plotter1Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    Plotter1GridUpdateActions(selectedItem);
                }
            };
            Plotter1Grid.Init();


            Plotter2Grid.SetColumns(columns);
            Plotter2Grid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
            Plotter2Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            Plotter2Grid.UseSorting = false;
            Plotter2Grid.AutoUpdateInterval = 0;
            // раскраска строк
            Plotter2Grid.RowStylers = rowStylers;

            Plotter2Grid.Name = "Plotter2";
            Plotter2Grid.OnFilterItems = FilterPlotter2Items;

            Plotter2Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
            {
                { "EditTime", new DataGridContextMenuItem(){
                    Header="Изменить время",
                    Tag = "access_mode_full_access",
                    Action=()=>
                    {
                        EditTime(2);
                    }
                }},
                { "ShowTaskDesign", new DataGridContextMenuItem(){
                    Header="Показать чертеж",
                    Action=()=>
                    {
                        ShowTaskDesign(Plotter2SelectedItem, 0);
                    }
                }},
                { "ShowAlterTaskDesign", new DataGridContextMenuItem(){
                    Header="Показать другой формат чертежа",
                    Action=()=>
                    {
                        ShowTaskDesign(Plotter2SelectedItem, 1);
                    }
                }},
                { "SetBlankSize", new DataGridContextMenuItem(){
                    Header="Вычислить размер развертки",
                    Action=()=>
                    {
                        SetBlankSize(Plotter2SelectedItem.CheckGet("SAMPLE_ID").ToInt());
                    }
                }},
                { "EditNote", new DataGridContextMenuItem(){
                    Header="Изменить примечание",
                    Tag = "access_mode_full_access",
                    Action=()=>
                    {
                        GetNote(Plotter2SelectedItem.CheckGet("SAMPLE_ID").ToInt(), Plotter2SelectedItem.CheckGet("TECHNOLOG_NOTE"));
                    }
                }},
            };

            Plotter2Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    Plotter2GridUpdateActions(selectedItem);
                }
            };
            Plotter2Grid.Init();

            Plotter3Grid.SetColumns(columns);
            Plotter3Grid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
            Plotter3Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            Plotter3Grid.UseSorting = false;
            Plotter3Grid.AutoUpdateInterval = 0;
            // раскраска строк
            Plotter3Grid.RowStylers = rowStylers;

            Plotter3Grid.Name = "Plotter3";
            Plotter3Grid.OnFilterItems = FilterPlotter3Items;

            Plotter3Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
            {
                { "EditTime", new DataGridContextMenuItem(){
                    Header="Изменить время",
                    Tag = "access_mode_full_access",
                    Action=()=>
                    {
                        EditTime(3);
                    }
                }},
                { "ShowTaskDesign", new DataGridContextMenuItem(){
                    Header="Показать чертеж",
                    Action=()=>
                    {
                        ShowTaskDesign(Plotter3SelectedItem, 0);
                    }
                }},
                { "ShowAlterTaskDesign", new DataGridContextMenuItem(){
                    Header="Показать другой формат чертежа",
                    Action=()=>
                    {
                        ShowTaskDesign(Plotter3SelectedItem, 1);
                    }
                }},
                { "SetBlankSize", new DataGridContextMenuItem(){
                    Header="Вычислить размер развертки",
                    Action=()=>
                    {
                        SetBlankSize(Plotter3SelectedItem.CheckGet("SAMPLE_ID").ToInt());
                    }
                }},
                { "EditNote", new DataGridContextMenuItem(){
                    Header="Изменить примечание",
                    Tag = "access_mode_full_access",
                    Action=()=>
                    {
                        GetNote(Plotter3SelectedItem.CheckGet("SAMPLE_ID").ToInt(), Plotter3SelectedItem.CheckGet("TECHNOLOG_NOTE"));
                    }
                }},
            };

            Plotter3Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    Plotter3GridUpdateActions(selectedItem);
                }
            };

            Plotter3Grid.Init();
        }

        /// <summary>
        /// Обновление действий для выбранной записи
        /// </summary>
        /// <param name="selectedItem">выбранная запись</param>
        private void SampleGridUpdateActions(Dictionary<string,string> selectedItem)
        {
            SampleSelectedItem = selectedItem;
            bool designExists = SampleSelectedItem.CheckGet("DESIGN_FILE_IS").ToBool();
            bool altDesignExists = SampleSelectedItem.CheckGet("DESIGN_FILE_OTHER_IS").ToBool();
            SampleGrid.Menu["ShowDesign"].Enabled = designExists;
            SampleGrid.Menu["ShowAlterDesign"].Enabled = altDesignExists;
            SampleGrid.Menu["ShowDesignFolder"].Enabled = altDesignExists || designExists;
        }

        /// <summary>
        /// Загрузка данных в таблицы
        /// </summary>
        private async void SampleLoadItems()
        {
            MainToolbar.IsEnabled = false;
            
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "ListScheduler");
            q.Request.SetParam("STATUS", "1");

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
                    var ds = ListDataSet.Create(result, "SAMPLES");
                    SampleDS = ProcessSampleItems(ds);
                    SampleGrid.UpdateItems(SampleDS);
                    //SampleGrid.CellHeaderWidthProcess();

                    var plotter1DS = ListDataSet.Create(result, "PLOTTER1");
                    Plotter1Grid.UpdateItems(plotter1DS);
                    //Plotter1Grid.CellHeaderWidthProcess();

                    var plotter2DS = ListDataSet.Create(result, "PLOTTER2");
                    Plotter2Grid.UpdateItems(plotter2DS);
                    //Plotter2Grid.CellHeaderWidthProcess();

                    var plotter3DS = ListDataSet.Create(result, "PLOTTER3");
                    Plotter3Grid.UpdateItems(plotter3DS);
                    //Plotter3Grid.CellHeaderWidthProcess();
                }
            }
            else
            {
                q.ProcessError();
            }
            
            MainToolbar.IsEnabled = true;
        }

        /// <summary>
        /// Загрузка данных в таблицу очереди выбранного плоттера
        /// </summary>
        /// <param name="i">Номер плоттера</param>
        private async void PlotterLoadItems(int i)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "SampleTask");
            q.Request.SetParam("Action", "List");
            q.Request.SetParam("MACHINE", i.ToString());
            q.Request.SetParam("STATUS", "1");

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
                    var plotterDS = ListDataSet.Create(result, "SCHEDULE");
                    switch (i)
                    {
                        case 1:
                            Plotter1Grid.UpdateItems(plotterDS);
                            break;
                        case 2:
                            Plotter2Grid.UpdateItems(plotterDS);
                            break;
                        case 3:
                            Plotter3Grid.UpdateItems(plotterDS);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Обработка строк перед загрузкой в таблицу
        /// </summary>
        /// <param name="ds">Данные из БД</param>
        /// <returns></returns>
        private ListDataSet ProcessSampleItems(ListDataSet ds)
        {
            ListDataSet _ds = ds;
            if (ds.Items != null)
            {
                if (ds.Items.Count > 0)
                {
                    foreach (var item in _ds.Items)
                    {
                        var l = new List<string>();
                        var s = item.CheckGet("SAMPLE_LENGTH").ToInt();
                        if (s > 0)
                        {
                            l.Add(s.ToString());
                        }
                        s = item.CheckGet("SAMPLE_WIDTH").ToInt();
                        if (s > 0)
                        {
                            l.Add(s.ToString());
                        }
                        s = item.CheckGet("SAMPLE_HEIGHT").ToInt();
                        if (s > 0)
                        {
                            l.Add(s.ToString());
                        }
                        item.CheckAdd("SAMPLE_SIZE", string.Join("х", l));

                        int gluing = item.CheckGet("GLUING").ToInt();
                        string gluingText = "";
                        switch (gluing)
                        {
                            case 1:
                                gluingText = "склеить";
                                break;
                            case 2:
                                gluingText = "не клеить";
                                break;
                        }
                        item.CheckAdd("GLUING_TEXT", gluingText);
                    }
                }
            }

            return _ds;
        }

        /// <summary>
        /// Фильтрация списка. Используем для вычисления поля
        /// </summary>
        public void FilterSampleItems()
        {
            if (SampleGrid.GridItems != null)
            {
                if (SampleGrid.GridItems.Count > 0)
                {
                    bool showWaiting = (bool)WaitingSource.IsChecked;

                    var list = new List<Dictionary<string, string>>();
                    foreach (var item in SampleGrid.GridItems)
                    {
                        bool includeByWaiting = true;

                        if (!showWaiting)
                        {
                            if (item.CheckGet("RAW_MISSING_FLAG").ToInt() == 1)
                            {
                                includeByWaiting = false;
                            }
                        }

                        if (includeByWaiting)
                        {
                            list.Add(item);
                        }
                    }
                    SampleGrid.GridItems = list;
                }
            }
        }

        /// <summary>
        /// Обновление действий для выбранной записи
        /// </summary>
        /// <param name="selectedItem">выбранная запись</param>
        private void Plotter1GridUpdateActions(Dictionary<string, string> selectedItem)
        {
            Plotter1SelectedItem = selectedItem;

            Up1Button.IsEnabled = false;
            Down1Button.IsEnabled = false;

            if (Plotter1Grid.GridItems.Count > 0)
            {
                int idx = Plotter1Grid.GridItems.IndexOf(selectedItem);

                if (idx != 0)
                {
                    Up1Button.IsEnabled = true;
                }
                if (idx != (Plotter1Grid.GridItems.Count - 1))
                {
                    Down1Button.IsEnabled = true;
                }
            }

            Plotter1Grid.Menu["ShowTaskDesign"].Enabled = !selectedItem.CheckGet("DESIGN_FILE").IsNullOrEmpty();
            Plotter1Grid.Menu["ShowAlterTaskDesign"].Enabled = !selectedItem.CheckGet("DESIGN_FILE_OTHER").IsNullOrEmpty();

            ProcessPermissions();
        }


        /// <summary>
        /// Обновление действий для выбранной записи
        /// </summary>
        /// <param name="selectedItem">выбранная запись</param>
        private void Plotter2GridUpdateActions(Dictionary<string, string> selectedItem)
        {
            Plotter2SelectedItem = selectedItem;

            Up2Button.IsEnabled = false;
            Down2Button.IsEnabled = false;

            if (Plotter2Grid.GridItems.Count > 0)
            {
                int idx = Plotter2Grid.GridItems.IndexOf(selectedItem);

                if (idx != 0)
                {
                    Up2Button.IsEnabled = true;
                }
                if (idx != (Plotter2Grid.GridItems.Count - 1))
                {
                    Down2Button.IsEnabled = true;
                }
            }

            Plotter2Grid.Menu["ShowTaskDesign"].Enabled = !selectedItem.CheckGet("DESIGN_FILE").IsNullOrEmpty();
            Plotter2Grid.Menu["ShowAlterTaskDesign"].Enabled = !selectedItem.CheckGet("DESIGN_FILE_OTHER").IsNullOrEmpty();
        }

        /// <summary>
        /// Обновление действий для выбранной записи
        /// </summary>
        /// <param name="selectedItem">выбранная запись</param>
        private void Plotter3GridUpdateActions(Dictionary<string, string> selectedItem)
        {
            Plotter3SelectedItem = selectedItem;

            Up3Button.IsEnabled = false;
            Down3Button.IsEnabled = false;

            if (Plotter3Grid.GridItems.Count > 0)
            {
                int idx = Plotter3Grid.GridItems.IndexOf(selectedItem);

                if (idx != 0)
                {
                    Up3Button.IsEnabled = true;
                }
                if (idx != (Plotter3Grid.GridItems.Count - 1))
                {
                    Down3Button.IsEnabled = true;
                }
            }

            Plotter3Grid.Menu["ShowTaskDesign"].Enabled = !selectedItem.CheckGet("DESIGN_FILE").IsNullOrEmpty();
            Plotter3Grid.Menu["ShowAlterTaskDesign"].Enabled = !selectedItem.CheckGet("DESIGN_FILE_OTHER").IsNullOrEmpty();
        }

        /// <summary>
        /// Обработка строк таблицы очереди плоттера 1
        /// </summary>
        public void FilterPlotter1Items()
        {
            ProcessPlotterGridItems(Plotter1Grid.GridItems);
        }

        /// <summary>
        /// Обработка строк таблицы очереди плоттера 2
        /// </summary>
        public void FilterPlotter2Items()
        {
            ProcessPlotterGridItems(Plotter2Grid.GridItems);
        }

        /// <summary>
        /// Обработка строк таблицы очереди плоттера 3
        /// </summary>
        public void FilterPlotter3Items()
        {
            ProcessPlotterGridItems(Plotter3Grid.GridItems);
        }

        public void ProcessPlotterGridItems(List<Dictionary<string, string>> gridItems)
        {
            if (gridItems != null)
            {
                if (gridItems.Count > 0)
                {
                    var startTime = DateTime.Now;
                    foreach (var item in gridItems)
                    {
                        item.CheckAdd("START_TIME", startTime.ToString("dd.MM HH:mm"));
                        var minutes = item.CheckGet("ESTIMATE_TIME").ToDouble();
                        startTime = startTime.AddMinutes(minutes);
                    }
                }
            }

        }

        /// <summary>
        /// Обработка переноса строки из таблицы образцов в очередь 1
        /// </summary>
        /// <param name="sourceName">имя таблицы источника</param>
        /// <param name="row">данные образца</param>
        public void Plotter1Drop(string sourceName, Dictionary<string, string> row)
        {
            if (row.Count > 0)
            {
                if (sourceName == "Sample")
                {
                    SendToPlotter(1, row);
                }
            }
        }

        /// <summary>
        /// Обработка переноса строки из таблицы образцов в очередь 2
        /// </summary>
        /// <param name="sourceName">имя таблицы источника</param>
        /// <param name="row">данные образца</param>
        public void Plotter2Drop(string sourceName, Dictionary<string, string> row)
        {
            if (row.Count > 0)
            {
                if (sourceName == "Sample")
                {
                    SendToPlotter(2, row);
                }
            }
        }

        /// <summary>
        /// Обработка переноса строки из таблицы образцов в очередь 2
        /// </summary>
        /// <param name="sourceName">имя таблицы источника</param>
        /// <param name="row">данные образца</param>
        public void Plotter3Drop(string sourceName, Dictionary<string, string> row)
        {
            if (row.Count > 0)
            {
                if (sourceName == "Sample")
                {
                    SendToPlotter(3, row);
                }
            }
        }

        /// <summary>
        /// Отправляет выбранный образец в очередь плоттера
        /// </summary>
        /// <param name="i">Номер плоттера</param>
        private void SendToPlotter(int i, Dictionary<string, string> row)
        {
            if (row != null)
            {
                int sampleId = row.CheckGet("ID").ToInt();
                if (sampleId > 0)
                {
                    var estimateWindow = new SampleTaskOperationTimeWindow();
                    estimateWindow.ReceiverName = ControlName;
                    var p = new Dictionary<string, string>() {
                        { "ID_SMPL", sampleId.ToString() },
                        { "MACHINE", i.ToString() },
                        { "ESTIMATE", "0" },
                        { "QTY", row.CheckGet("QTY") },
                        { "PRODUCT_CLASS_ID", row.CheckGet("PRODUCT_CLASS_ID") },
                        { "RAW_ID", row.CheckGet("ID_SC") },
                        { "CARDBOARD_ID", row.CheckGet("CARDBOARD_ID") },
                    };
                    estimateWindow.Edit(p);
                }
            }
        }

        /// <summary>
        /// Изменяет оценку времени изготовления образца
        /// </summary>
        /// <param name="machine"></param>
        private void EditTime(int machine)
        {
            var item = Plotter1Grid.SelectedItem;
            switch (machine)
            {
                case 2:
                    item = Plotter2Grid.SelectedItem;
                    break;
                case 3:
                    item = Plotter3Grid.SelectedItem;
                    break;
                default:
                    item = Plotter1Grid.SelectedItem;
                    break;
            }

            var editTimeWindow = new SampleTaskEditTime();
            editTimeWindow.ReceiverName = ControlName;
            editTimeWindow.Edit(item);
        }

        /// <summary>
        /// Удаление образца из очереди
        /// </summary>
        /// <param name="scheduleId">ID очереди</param>
        private async void DeleteFromSchedule(int scheduleId)
        {
            if (scheduleId > 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "SampleTask");
                q.Request.SetParam("Action", "Delete");

                q.Request.SetParam("ID", scheduleId.ToString());

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
        /// Загружает и открывает файл чертежа
        /// </summary>
        /// <param name="designType">Тип чертежа: 0 - основной, 1 - альтернативный</param>
        private async void ShowDesign(int designType)
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
        /// Сохранение для образца пути к файлу чертежа
        /// </summary>
        /// <param name="designType">Тип чертежа: 0 - основной, 1 - альтернативный</param>
        private async void LoadDesign(int designType)
        {
            var sampleId = SampleSelectedItem.CheckGet("ID").ToInt();
            if (sampleId > 0)
            {
                bool resume = true;
                var fd = new OpenFileDialog();
                var fdResult = (bool)fd.ShowDialog();
                if (fdResult)
                {
                    // В названии файла чертежа должен присутствовать размер образца (желание технологов)
                    // Если размер не нашли, уточняем, правильно ли выбран файл
                    string sampleSize = SampleSelectedItem.CheckGet("SAMPLE_SIZE");
                    if (fd.FileName.IndexOf(sampleSize) == -1)
                    {
                        var fn = Path.GetFileName(fd.FileName);
                        var dw = new DialogWindow($"Вы действительно хотите привязать чертеж {fn}?", "Привязать чертеж", "", DialogWindowButtons.YesNo);

                        if ((bool)dw.ShowDialog())
                        {
                            resume = dw.ResultButton == DialogResultButton.Yes;
                        }
                        else
                        {
                            resume = false;
                        }
                    }

                    if (resume)
                    {
                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Preproduction");
                        q.Request.SetParam("Object", "Samples");
                        q.Request.SetParam("Action", "SaveDesignFilePath");
                        q.Request.SetParam("ID", sampleId.ToString());
                        q.Request.SetParam("DESIGN_TYPE", designType.ToString());
                        q.Request.SetParam("FILE_PATH", fd.FileName);

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
                        else
                        {
                            q.ProcessError();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Открывает файл чертежа по полученному пути
        /// </summary>
        /// <param name="designType"></param>
        private void ShowTaskDesign(Dictionary<string, string> selectedItem,int designType)
        {
            string sourceFile;

            if (designType == 0)
            {
                sourceFile = selectedItem.CheckGet("DESIGN_FILE");
            }
            else
            {
                sourceFile = selectedItem.CheckGet("DESIGN_FILE_OTHER");
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
            var sourceFile = SampleSelectedItem.CheckGet("DESIGN_FILE");
            if (string.IsNullOrEmpty(sourceFile))
            {
                sourceFile = SampleSelectedItem.CheckGet("DESIGN_FILE_OTHER");
            }

            if (File.Exists(sourceFile))
            {
                var folder = Path.GetDirectoryName(sourceFile);
                Central.OpenFolder(folder);
            }
        }

        /// <summary>
        /// Выполнение обмена позиций очереди
        /// </summary>
        /// <param name="p"></param>
        private async void SwapItems(Dictionary<string,string> p)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "SampleTask");
            q.Request.SetParam("Action", "Swap");
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
                        if (p.ContainsKey("MACHINE"))
                        {
                            PlotterLoadItems(p["MACHINE"].ToInt());
                        }
                        else
                        {
                            SampleGrid.LoadItems();
                        }
                    }
                }
            }
        }

        private void StopReason(int sampleId)
        {
            var p = new Dictionary<string, string>()
            {
                { "ID", sampleId.ToString() },
            };

            var reasonForm = new SampleTaskStopReason();
            reasonForm.TaskValues = p;
            reasonForm.ReceiverName = ControlName;
            reasonForm.Show();
        }

        /// <summary>
        /// Изменение флага доработки и отсутствия сырья образца
        /// </summary>
        private async void SetRevision(Dictionary<string, string> p)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "SetRevision");
            q.Request.SetParams(p);

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
            else if (q.Answer.Error.Code == 145)
            {
                var dw = new DialogWindow(q.Answer.Error.Message, "В ожидание");
                dw.ShowDialog();
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
                sampleId = SampleSelectedItem.CheckGet("ID").ToInt();
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
                            SampleGrid.LoadItems();
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
        /// Вызов окна изменения габаритов развертки
        /// </summary>
        private void EditBlankSize()
        {
            if (SampleSelectedItem != null)
            {
                var sampleEditBlank = new SampleEditBlankSize();
                sampleEditBlank.ReceiverName = ControlName;
                sampleEditBlank.Edit(SampleSelectedItem);
            }
        }

        /// <summary>
        /// Вызов вкладки редактирования примечания
        /// </summary>
        /// <param name="sampleId"></param>
        public void GetNote(int sampleId, string note = "")
        {
            var sampleNote = new SampleNote();
            sampleNote.ReceiverName = ControlName;
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

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            SampleGrid.LoadItems();
        }

        private void Delete1Button_Click(object sender, RoutedEventArgs e)
        {
            if (Plotter1Grid.SelectedItem != null)
            {
                DeleteFromSchedule(Plotter1Grid.SelectedItem.CheckGet("ID").ToInt());
            }
        }

        private void Delete2Button_Click(object sender, RoutedEventArgs e)
        {
            if (Plotter2Grid.SelectedItem != null)
            {
                DeleteFromSchedule(Plotter2Grid.SelectedItem.CheckGet("ID").ToInt());
            }
        }

        private void Up1Button_Click(object sender, RoutedEventArgs e)
        {
            if (Plotter1Grid.SelectedItem != null)
            {
                var p = new Dictionary<string, string>();
                int idx = Plotter1Grid.GridItems.IndexOf(Plotter1Grid.SelectedItem);
                p.Add("OLD_ID", Plotter1Grid.SelectedItem.CheckGet("ID"));
                p.Add("NEW_ID", Plotter1Grid.GridItems[idx - 1].CheckGet("ID"));
                p.Add("MACHINE", "1");

                SwapItems(p);
            }
        }

        private void Down1Button_Click(object sender, RoutedEventArgs e)
        {
            if (Plotter1Grid.SelectedItem != null)
            {
                var p = new Dictionary<string, string>();
                int idx = Plotter1Grid.GridItems.IndexOf(Plotter1Grid.SelectedItem);
                p.Add("OLD_ID", Plotter1Grid.SelectedItem.CheckGet("ID"));
                p.Add("NEW_ID", Plotter1Grid.GridItems[idx + 1].CheckGet("ID"));
                p.Add("MACHINE", "1");

                SwapItems(p);
            }
        }

        private void Up2Button_Click(object sender, RoutedEventArgs e)
        {
            if (Plotter2Grid.SelectedItem != null)
            {
                var p = new Dictionary<string, string>();
                int idx = Plotter2Grid.GridItems.IndexOf(Plotter2Grid.SelectedItem);
                p.Add("OLD_ID", Plotter2Grid.SelectedItem.CheckGet("ID"));
                p.Add("NEW_ID", Plotter2Grid.GridItems[idx - 1].CheckGet("ID"));
                p.Add("MACHINE", "2");

                SwapItems(p);
            }
        }

        private void Down2Button_Click(object sender, RoutedEventArgs e)
        {
            if (Plotter2Grid.SelectedItem != null)
            {
                var p = new Dictionary<string, string>();
                int idx = Plotter2Grid.GridItems.IndexOf(Plotter2Grid.SelectedItem);
                p.Add("OLD_ID", Plotter2Grid.SelectedItem.CheckGet("ID"));
                p.Add("NEW_ID", Plotter2Grid.GridItems[idx + 1].CheckGet("ID"));
                p.Add("MACHINE", "2");

                SwapItems(p);
            }
        }

        /// <summary>
        /// Открытие вкладки с чатом по образцу
        /// </summary>
        private void OpenChat(int chatType = 0)
        {
            if (SampleSelectedItem != null)
            {
                var chatFrame = new SampleChat();
                chatFrame.ChatType = chatType;
                chatFrame.ChatId = SampleSelectedItem.CheckGet("CHAT_ID").ToInt();
                chatFrame.ObjectId = SampleSelectedItem.CheckGet("ID").ToInt();
                chatFrame.ReceiverName = ControlName;
                chatFrame.Recipient = 6;
                chatFrame.RawMissingFlag = 0;
                chatFrame.Edit();
            }
        }

        /// <summary>
        /// Открытие вкладки с приложенными файлами
        /// </summary>
        private void OpenAttachments()
        {
            if (SampleSelectedItem != null)
            {
                var sampleFiles = new SampleFiles();
                sampleFiles.SampleId = SampleSelectedItem.CheckGet("ID").ToInt();
                sampleFiles.ReturnTabName = ControlName;
                sampleFiles.Show();
            }
        }

        /// <summary>
        /// Открывает вкладку с историей изменения образца
        /// </summary>
        private void ShowHistory()
        {
            var historyFrame = new SampleHistory();
            historyFrame.SampleId = SampleSelectedItem.CheckGet("ID").ToInt();
            historyFrame.ReceiverName = ControlName;
            historyFrame.Show();
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
            q.Request.SetParam("ID", SampleSelectedItem.CheckGet("ID"));

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
        /// Изменение картона, из кторого будет изготавливаться образец
        /// </summary>
        private void EditRaw()
        {
            if (SampleSelectedItem != null)
            {
                var p = new Dictionary<string, string>()
                {
                    { "PROFILE", SampleSelectedItem.CheckGet("PROFILE_ID") },
                    { "MARK", SampleSelectedItem.CheckGet("MARK_ID") },
                    { "COLOR", SampleSelectedItem.CheckGet("OUTER_COLOR_ID") },
                    { "CARDBOARD", SampleSelectedItem.CheckGet("CARDBOARD_ID") },
                };
                var selectCardboard = new SampleSelectCardboard();
                selectCardboard.ReceiverName = ControlName;
                selectCardboard.AnswerType = 0;
                selectCardboard.SampleId = SampleSelectedItem.CheckGet("ID").ToInt();
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
                    SampleGrid.LoadItems();
                }
                else
                {
                    q.ProcessError();
                }

            }
            else
            {
                errorMsg = "Не удалось определить образец или картон";
            }

            if (!string.IsNullOrEmpty(errorMsg))
            {
                var d = new DialogWindow(errorMsg, "Схема образца");
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Создание вкладки редатирования образца
        /// </summary>
        /// <param name="sampleId"></param>
        private void Edit(int sampleId)
        {
            var sampleForm = new Sample();
            sampleForm.ReceiverName = ControlName;
            sampleForm.Edit(sampleId);
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void ToPlotter_Click(object sender, RoutedEventArgs e)
        {
            var i = PlotterTabs.SelectedIndex;
            SendToPlotter(i + 1, SampleSelectedItem);
        }

        private void Revision_Click(object sender, RoutedEventArgs e)
        {
            var sampleId = SampleSelectedItem.CheckGet("ID").ToInt();
            if (sampleId > 0)
            {
                StopReason(sampleId);
            }
        }

        private void WaitingSource_Click(object sender, RoutedEventArgs e)
        {
            SampleGrid.UpdateItems();
        }

        private void Up3Button_Click(object sender, RoutedEventArgs e)
        {
            if (Plotter3Grid.SelectedItem != null)
            {
                var p = new Dictionary<string, string>();
                int idx = Plotter3Grid.GridItems.IndexOf(Plotter3Grid.SelectedItem);
                p.Add("OLD_ID", Plotter3Grid.SelectedItem.CheckGet("ID"));
                p.Add("NEW_ID", Plotter3Grid.GridItems[idx - 1].CheckGet("ID"));
                p.Add("MACHINE", "3");

                SwapItems(p);
            }
        }

        private void Down3Button_Click(object sender, RoutedEventArgs e)
        {
            if (Plotter3Grid.SelectedItem != null)
            {
                var p = new Dictionary<string, string>();
                int idx = Plotter3Grid.GridItems.IndexOf(Plotter3Grid.SelectedItem);
                p.Add("OLD_ID", Plotter3Grid.SelectedItem.CheckGet("ID"));
                p.Add("NEW_ID", Plotter3Grid.GridItems[idx + 1].CheckGet("ID"));
                p.Add("MACHINE", "3");

                SwapItems(p);
            }
        }

        private void Delete3Button_Click(object sender, RoutedEventArgs e)
        {
            if (Plotter3Grid.SelectedItem != null)
            {
                DeleteFromSchedule(Plotter3Grid.SelectedItem.CheckGet("ID").ToInt());
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            Edit(SampleSelectedItem.CheckGet("ID").ToInt());
        }
    }
}
