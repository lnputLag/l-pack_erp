using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Shipments;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Привязка рулонов к ПЗ 
    /// </summary>
    /// <author>zelenskiy_sv</author>
    public partial class RollListForCompletedTasks : UserControl
    {
        
        /*
              0                  1  2
            ----------------------------------------------------
            |  TasksGridToolbar | |  RollsSelectedGridToolbar  | 0
            ----------------------------------------------------
            |  TasksGrid        | |  RollsSelectedGrid         | 1
            ----------------------------------------------------
            |                   | |  RollsAllGridToolbar       | 2
            ----------------------------------------------------
            |                   | |  RollsAllGrid              | 3
            ----------------------------------------------------

         */
    
    
        public RollListForCompletedTasks()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Init();
            // Производственные задания
            TaskGridInit();
            // Привязанные рулоны
            RollSelectedGridInit();
            // Рулоны
            RollGridInit();

            SetDefaults();

            ProcessPermissions();
        }

        public string RoleName = "[erp]roll_registration";

        /// <summary>
        /// данные из выбранной в гриде TaskGrid строки
        /// </summary>
        public Dictionary<string, string> SelectedTaskItem { get; set; }

        /// <summary>
        /// данные из выбранной в гриде RollGrid строки
        /// </summary>
        public Dictionary<string, string> SelectedRollItem { get; set; }

        /// <summary>
        /// данные из выбранной в гриде RollSelectedGrid строки
        /// </summary>
        public Dictionary<string, string> SelectedTaskRollItem { get; set; }

        /// <summary>
        /// признак того, что первая выделенная желтым строка найдена
        /// </summary>
        public bool SelectedRollFirstItemFlag { get; set; } = false;

        /// <summary>
        /// ИД выбранного ГА
        /// </summary>
        public int SelectedMachineId { get; set; } = 2;

        /// <summary>
        /// ИД выбранного раската (слои)
        /// </summary>
        public int SelectedReelId { get; set; } = 1;

        /// <summary>
        /// форма с полями для фильтрации данных
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// активный ИД группы сырья
        /// </summary>
        public int SelectedRawGroup { get; set; }


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

            if (RollGrid != null && RollGrid.Menu != null && RollGrid.Menu.Count > 0)
            {
                foreach (var manuItem in RollGrid.Menu)
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
        /// инициализация общих компонентов
        /// </summary>
        public void Init()
        {
            //инициализация формы
            {
                Form = new FormHelper();

                //колонки формы
                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path = "SEARCH",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = SearchTask,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path = "SEARCH2",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = SearchRoll,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path = "FROM_DTTM",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Default = DateTime.Now.Date.ToString("dd.MM.yyyy"),
                        Control = FromDate,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "TO_DTTM",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Default = DateTime.Now.Date.ToString("dd.MM.yyyy"),
                        Control = ToDate,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },

                };

                Form.SetFields(fields);

                //после установки значений
                Form.AfterSet = (Dictionary<string, string> v) =>
                {
                    //фокус на кнопку обновления
                    RefreshButton.Focus();
                };

                {
                    // начальная дата
                    FromDate.Text = DateTime.Now.Date.ToString("dd.MM.yyyy");
                    // конечная
                    ToDate.Text = FromDate.Text;

                    RefreshButton.Style = (Style)RefreshButton.TryFindResource("Button");

                }

            }
        }

        /// <summary>
        /// инициализация компонентов (список ПЗ)
        /// </summary>
        public void TaskGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД ПЗ",
                        Path="ID_PZ",
                        Doc="Идентификатор ПЗ",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=50,
                        MaxWidth=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ ПЗ",
                        Path="NUM",
                        Doc="Номер ПЗ",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Начало",
                        Path="START_DTTM",
                        Doc="Начало ПЗ",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=110,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Профиль",
                        Path="PROFIL_NAME",
                        Doc="Профиль",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=35,
                        MaxWidth=35,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Формат",
                        Path="WEB_WIDTH",
                        Doc="Ширина гофрополотна",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=50,
                        MaxWidth=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Длина",
                        Path="LEN",
                        Doc="Длина м/пог",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=50,
                        MaxWidth=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Слой 1",
                        Path="NAME_LAYER_1",
                        Doc="Слой 1",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=100,
                        OnClickAction=(row,el) =>
                        {
                            CheckReel(1);
                            //TaskGrid.LoadItems();
                            return null;
                        },
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                (Dictionary<string, string> row) =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    var color = GetLayerColor(row.CheckGet("PCWR_1").ToInt());

                                    if (!string.IsNullOrEmpty(color)
                                    && !string.IsNullOrEmpty(row.CheckGet("NAME_LAYER_1")))
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
                        Header="Слой 2",
                        Path="NAME_LAYER_2",
                        Doc="Слой 2",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=100,
                        OnClickAction=(row,el) =>
                        {
                            CheckReel(2);
                            //TaskGrid.LoadItems();
                            return null;
                        },
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                (Dictionary<string, string> row) =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    var color = GetLayerColor(row.CheckGet("PCWR_2").ToInt());

                                    if (!string.IsNullOrEmpty(color)
                                    && !string.IsNullOrEmpty(row.CheckGet("NAME_LAYER_2")))
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
                        Header="Слой 3",
                        Path="NAME_LAYER_3",
                        Doc="Слой 3",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=100,
                        OnClickAction=(row,el) =>
                        {
                            CheckReel(3);
                            //TaskGrid.LoadItems();
                            return null;
                        },
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                (Dictionary<string, string> row) =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    var color = GetLayerColor(row.CheckGet("PCWR_3").ToInt());

                                    if (!string.IsNullOrEmpty(color)
                                    && !string.IsNullOrEmpty(row.CheckGet("NAME_LAYER_3")))
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
                        Header="Слой 4",
                        Path="NAME_LAYER_4",
                        Doc="Слой 4",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=100,
                        OnClickAction=(row,el) =>
                        {
                            CheckReel(4);
                            //TaskGrid.LoadItems();
                            return null;
                        },
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                (Dictionary<string, string> row) =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    var color = GetLayerColor(row.CheckGet("PCWR_4").ToInt());

                                    if (!string.IsNullOrEmpty(color)
                                    && !string.IsNullOrEmpty(row.CheckGet("NAME_LAYER_4")))
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
                        Header="Слой 5",
                        Path="NAME_LAYER_5",
                        Doc="Слой 5",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=100,
                        OnClickAction=(row,el) =>
                        {
                            CheckReel(5);
                            //TaskGrid.LoadItems();
                            return null;
                        },
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                (Dictionary<string, string> row) =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    var color = GetLayerColor(row.CheckGet("PCWR_5").ToInt());

                                    if (!string.IsNullOrEmpty(color)
                                    && !string.IsNullOrEmpty(row.CheckGet("NAME_LAYER_5")))
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
                        Header="PRCW_ID",
                        Path="PRCW_ID",
                        Doc="PRCW_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="PCWR_1",
                        Path="PCWR_1",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="PCWR_2",
                        Path="PCWR_2",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="PCWR_3",
                        Path="PCWR_3",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="PCWR_4",
                        Path="PCWR_4",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="PCWR_5",
                        Path="PCWR_5",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ID_RAW_GROUP_1",
                        Path="ID_RAW_GROUP_1",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ID_RAW_GROUP_2",
                        Path="ID_RAW_GROUP_2",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ID_RAW_GROUP_3",
                        Path="ID_RAW_GROUP_3",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ID_RAW_GROUP_4",
                        Path="ID_RAW_GROUP_4",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ID_RAW_GROUP_5",
                        Path="ID_RAW_GROUP_5",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden = true,
                    },
                };
                TaskGrid.SetColumns(columns);

                TaskGrid.AutoUpdateInterval = 0;

                TaskGrid.SetSorting("START_DTTM", ListSortDirection.Ascending);
                TaskGrid.SearchText = SearchTask;
                TaskGrid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                TaskGrid.OnSelectItem = selectedItem =>
                {
                    //TaskGridUpdateActions(selectedItem);
                };

                //данные грида
                TaskGrid.OnLoadItems = TaskGridLoadItems;
                TaskGrid.PrimaryKey = "ID_PZ";
                TaskGrid.SelectItemMode = 2;

                TaskGrid.Run();

                //фокус ввода           
                TaskGrid.Focus();

            }
        }

        /// <summary>
        /// Получение цвета фона ячейки для группы сырья
        /// </summary>
        /// <param name="pcwr">ИД группы сырья</param>
        /// <returns>Цвет группы сырья</returns>
        private string GetLayerColor(int pcwr)
        {
            var color = "";

            switch (pcwr)
            {
                case 1:
                    // Один из привязанных рулонов некорректный
                    color = HColor.Yellow;
                    break;
                case 2:
                    // Все привязанные рулоны некорректные
                    color = HColor.Red;
                    break;
                case 3:
                    // Нет привязанных рулонов
                    color = HColor.Blue;
                    break;
                default:
                    break;
            }

            return color;
        }

        /// <summary>
        /// инициализация компонентов (рулоны на раскате)
        /// </summary>
        public void RollGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        Doc="",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=25,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Постановка",
                        Path="START_DTTM",
                        Doc="Постановка на раскат",
                        ColumnType=ColumnTypeRef.DateTime,
                        MinWidth = 110,
                        MaxWidth = 110,
                        //MinWidth = 77,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Начало",
                        Path="START_ACTIVE_DTTM",
                        Doc="Начало сматывания",
                        ColumnType=ColumnTypeRef.DateTime,
                        MinWidth = 110,
                        MaxWidth = 110,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Окончание",
                        Path="STOP_ACTIVE_DTTM",
                        Doc="Окончание сматывания",
                        ColumnType=ColumnTypeRef.DateTime,
                        MinWidth = 110,
                        MaxWidth = 110,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Снятие",
                        Path="STOP_DTTM",
                        Doc="Снятие с раската",
                        ColumnType=ColumnTypeRef.DateTime,
                        MinWidth = 110,
                        MaxWidth = 110,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Сторона",
                        Path="SIDE",
                        Doc="Сторона",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 35,
                        MaxWidth = 45,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Бумага",
                        Path="NAME",
                        Doc="Бумага",
                        ColumnType=ColumnTypeRef.String,
                        Width = 175,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер",
                        Path="NUM",
                        Doc="Номер",
                        ColumnType=ColumnTypeRef.String,
                        MaxWidth = 70,
                        MinWidth = 70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Колебание по влажности",
                        Path="HUMIDITY",
                        Doc="A: <0,6 B: 0,6-1,2 C: >1,2",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=30,
                        MaxWidth =30,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="IDP",
                        Doc="Идентификатор",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=37,
                        MaxWidth=50,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="SIDE_NUM",
                        Path="SIDE_NUM",
                        Doc="Сторона раската",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=37,
                        MaxWidth=50,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Привязан",
                        Path="IS_BIND",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=37,
                        MaxWidth=50,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Привязан",
                        Path="PCRT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид постановки рулона",
                        Path="PCRO_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=45,
                        MaxWidth=75,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид активности рулона",
                        Path="PCRA_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=45,
                        MaxWidth=75,
                    },
                };
                RollGrid.SetColumns(columns);

                // Цвета фона строк
                RollGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                {
                    {
                        DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";
                            var selectedDTTM = SelectedTaskItem.CheckGet("START_DTTM").ToDateTime();
                            var startDTTM = row.CheckGet("START_DTTM").ToDateTime(); //Постановка
                            var startActiveDTTM = row.CheckGet("START_ACTIVE_DTTM").ToDateTime(); //Начало
                            var stopActiveDTTM = row.CheckGet("STOP_ACTIVE_DTTM").ToDateTime(); //Окончание
                            var stopDTTM = row.CheckGet("STOP_DTTM").ToDateTime(); //Снятие

                            if (selectedDTTM >= startDTTM && selectedDTTM <= stopDTTM)
                            {
                                color = HColor.Yellow;
                            }

                            if (selectedDTTM >= startActiveDTTM && selectedDTTM <= stopActiveDTTM)
                            {
                                color = HColor.Green;
                            }

                            //if (selectedDTTM >= startDTTM && selectedDTTM <= stopDTTM)
                            //{
                            //    // Рулон установлен на раскат, но не привязан к выбранному ПЗ
                            //    color = HColor.Yellow;
                            //}

                            //if (row.CheckGet("IS_BIND").ToBool())
                            //{
                            //    // Рулон привязан к выбранному ПЗ
                            //    color = HColor.Green;
                            //}

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },
                };

                RollGrid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
                RollGrid.SearchText = SearchRoll;
                RollGrid.UseRowHeader = false;
                RollGrid.PrimaryKey = "_ROWNUMBER";
                RollGrid.SelectItemMode = 0;
                RollGrid.AutoUpdateInterval = 0;
                RollGrid.UseSorting = false;

                RollGrid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                RollGrid.OnSelectItem = selectedItem =>
                {
                    RollGridUpdateActions(selectedItem);
                };

                //привязка двойным кликом 
                RollGrid.OnDblClick = selectedItem =>
                {
                    if (AddRollButton.IsEnabled)
                    {
                        if (Central.Navigator.GetRoleLevel(this.RoleName) > Role.AccessMode.ReadOnly)
                        {
                            RollSelectedGridAddItem();
                        }
                    }
                };

                //контекстное меню
                RollGrid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    {
                        "Item1",
                        new DataGridContextMenuItem()
                        {
                            Header="Привязать к ПЗ",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                RollSelectedGridAddItem();
                            },
                        }
                    },
                };

                //данные грида
                RollGrid.OnLoadItems = RollGridLoadItems;

                RollGrid.Run();

                //фокус ввода           
                RollGrid.Focus();
            }

        }

        /// <summary>
        /// Поиск первой выделенной цветом строки в гриде
        /// </summary>
        private void RollGridUpdateSelection()
        {
            if (RollGrid.Items?.Count > 0)
            {
                SelectedRollFirstItemFlag = false;

                foreach (var item in RollGrid.Items)
                {
                    // Если строка не найдена
                    if (!SelectedRollFirstItemFlag)
                    {
                        // Время постановки рулона на раскат
                        var startDTTM = item.CheckGet("START_DTTM").ToDateTime();
                        // Время снятия рулона с раската
                        var stopDTTM = item.CheckGet("STOP_DTTM").ToDateTime();
                        // Время начала ПЗ
                        var selectedDTTM = SelectedTaskItem.CheckGet("START_DTTM").ToDateTime();

                        if (selectedDTTM >= startDTTM && selectedDTTM <= stopDTTM)
                        {
                            // Выделение первой найденной строки
                            RollGrid.SelectRowByKey(item.CheckGet("_ROWNUMBER").ToInt(), "_ROWNUMBER", true);
                            SelectedRollFirstItemFlag = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// инициализация компонентов (привязанные рулоны)
        /// </summary>
        public void RollSelectedGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Бумага",
                        Path="NAME",
                        Doc="Бумага",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=150,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер",
                        Path="NUM",
                        Doc="Номер",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Колебание по влажности",
                        Path="HUMIDITY",
                        Doc="A: <0,6 B: 0,6-1,2 C: >1,2",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=30,
                        MaxWidth=30,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Длина, м",
                        Path="LENGTH",
                        Doc="Длина, м",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вес, кг",
                        Path="WEIGHT",
                        Doc="Вес, кг",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="is_wrong",
                        Path="IS_WRONG",
                        Hidden = true,
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="PCWR_ID",
                        Path="PCWR_ID",
                        Doc="ИД PCWR_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=37,
                        MaxWidth=50,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header=" ",
                        Path="_",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=5,
                        MaxWidth=1000,
                    },
                };
                RollSelectedGrid.SetColumns(columns);

                // Раскраска строк
                RollSelectedGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                {
                    // Цвета фона строк
                    {
                        DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";
                            var currentStatus = row.CheckGet("IS_WRONG").ToInt();

                            if (currentStatus == 1)
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

                RollSelectedGrid.AutoUpdateInterval = 0;
                RollSelectedGrid.UseRowHeader = false;
                RollSelectedGrid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                RollSelectedGrid.OnSelectItem = selectedItem =>
                {
                    RollSelectedGridUpdateActions(selectedItem);
                };

                //данные грида
                RollSelectedGrid.OnLoadItems = RollSelectedGridLoadItems;

                RollSelectedGrid.Run();

                //фокус ввода           
                RollSelectedGrid.Focus();
            }

        }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Production",
                ReceiverName = "",
                SenderName = "RollListForCompletedTasks",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            TaskGrid.Destruct();
            RollGrid.Destruct();
            RollSelectedGrid.Destruct();
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.IndexOf("ListCompletedTasks") > -1)
            {
                switch (m.Action)
                {
                    case "Refresh":
                        // выделение на новую строку
                        var id = m.Message.ToInt();
                        TaskGrid.SetSelectedItemId(id);
                        break;
                }
            }
        }

        /// <summary>
        /// фильтрация записей в таблице 
        /// </summary>
/*        public void TaskGridFilterItems()
        {
            TaskGridUpdateActions(SelectedTaskItem);
        }
*/
        /// <summary>
        /// получение записей (список ПЗ)
        /// </summary>
        public async void TaskGridLoadItems()
        {
            TaskGridDisableControls();

            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();

                var idSt = SelectedMachineId;

                //FIXME:
                string toDateString = ToDate.Text.ToDateTime().AddDays(1).ToString("dd.MM.yyyy");

                p.CheckAdd("DT_FROM", FromDate.Text);
                p.CheckAdd("DT_TO", toDateString);
                p.CheckAdd("ID_ST", idSt.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "Roll");
                q.Request.SetParam("Action", "ListCompletedTask");
                q.Request.SetParams(p);

                q.Request.Timeout = 60000;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;
                //q.Request.Timeout = Central.Parameters.RequestTimeoutGrid;
                //q.Request.Attempts = Central.Parameters.RequestAttemptsGrid;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            TaskGrid.UpdateItems(ds);
                        }

                        {
                            if (TaskGrid.Items?.Count == 0)
                            {
                                RollSelectedGrid.ClearItems();
                                RollGrid.ClearItems();
                            }
                        }
                    }
                }
            }

            // установка доступности элементов управления
            SetElementActivity();

            TaskGridEnableControls();
        }

        /// <summary>
        /// получение записей (рулоны на раскате)
        /// </summary>
        public async void RollGridLoadItems()
        {
            RollGridDisableControls();

            bool resume = true;

            SelectedTaskItem=TaskGrid.SelectedItem;

            if (resume)
            {

                var p = new Dictionary<string, string>();

                // ИД ГА
                var idSt = SelectedMachineId;
                // номер раската
                var num = SelectedReelId;

                string toDateString = ToDate.Text.ToDateTime().AddDays(1).ToString("dd.MM.yyyy");

                p.CheckAdd("DT_FROM", FromDate.Text);
                p.CheckAdd("DT_TO", toDateString);
                p.CheckAdd("ID_ST", idSt.ToString());
                p.CheckAdd("NUM", num.ToString());
                p.CheckAdd("PRCW_ID", SelectedTaskItem.CheckGet("PRCW_ID").ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "Reel");
                q.Request.SetParam("Action", "ListRoll");
                q.Request.SetParams(p);

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
                        var ds = ListDataSet.Create(result, "ITEMS");

                        RollGrid.UpdateItems(ds);

                        RollGridUpdateSelection();
                    }
                }
            }

            // установка доступности элементов управления
            SetElementActivity();

            RollGridEnableControls();
        }

        /// <summary>
        /// установка доступности элементов управления
        /// </summary>
        private void SetElementActivity()
        {
            SearchRoll.IsEnabled = RollGrid.GridItems?.Count > 0;
            DelRollButton.IsEnabled = RollSelectedGrid.Items?.Count > 0;
            if (SelectedRawGroup > 0)
            {
                AddRollButton.IsEnabled = RollGrid.GridItems?.Count > 0;
            }
            else
            {
                AddRollButton.IsEnabled = false;
            }
            SearchTask.IsEnabled = TaskGrid.GridItems?.Count > 0;
            ExportButton.IsEnabled = TaskGrid.GridItems?.Count > 0;

            DataGridContextMenuItem outVal;
            if (RollGrid.Menu.TryGetValue("Item1", out outVal))
            {
                outVal.Enabled = AddRollButton.IsEnabled;
            }

            ProcessPermissions();
        }

        /// <summary>
        /// получение записей (привязанные рулоны)
        /// </summary>
        public async void RollSelectedGridLoadItems()
        {
            RollSelectedGridDisableControls();

            if (RollSelectedGrid!= null && RollSelectedGrid.Items != null)
            {
                RollSelectedGrid.Items.Clear();
            }

            bool resume = true;

            SelectedTaskItem=TaskGrid.SelectedItem;
            if (SelectedTaskItem == null)
            {
                resume = false;
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();

                p.CheckAdd("ID_RAW_GROUP", SelectedRawGroup.ToInt().ToString());
                p.CheckAdd("PRCW_ID", SelectedTaskItem.CheckGet("PRCW_ID").ToInt().ToString());
                p.CheckAdd("WEB_WIDTH", SelectedTaskItem.CheckGet("WEB_WIDTH").ToInt().ToString());
                p.CheckAdd("ID_ST", SelectedMachineId.ToInt().ToString());
                p.CheckAdd("NUM", SelectedReelId.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "Roll");
                q.Request.SetParam("Action", "ListForCompletedTask");
                q.Request.SetParams(p);

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
                        var ds = ListDataSet.Create(result, "ITEMS");
                        RollSelectedGrid.UpdateItems(ds);
                    }
                }
            }

            // установка доступности элементов управления
            SetElementActivity();

            RollSelectedGridEnableControls();
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void TaskGridDisableControls()
        {
            TaskGridToolbar.IsEnabled = false;
            TaskGrid.ShowSplash();
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void RollGridDisableControls()
        {
            RollsAllGridToolbar.IsEnabled = false;
            RollGrid.ShowSplash();
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void RollSelectedGridDisableControls()
        {
            RollsSelectedGridToolbar.IsEnabled = false;
            RollSelectedGrid.ShowSplash();
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void TaskGridEnableControls()
        {
            TaskGridToolbar.IsEnabled = true;
            TaskGrid.HideSplash();
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void RollGridEnableControls()
        {
            RollsAllGridToolbar.IsEnabled = true;
            RollGrid.HideSplash();
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void RollSelectedGridEnableControls()
        {
            RollsSelectedGridToolbar.IsEnabled = true;
            RollSelectedGrid.HideSplash();
        }

        /// <summary>
        /// обновление методов работы с выбранной в гриде строкой
        /// </summary>
        /// <param name="selectedItem"></param>
        public void TaskGridUpdateActions(Dictionary<string, string> selectedItem)
        {
            if (selectedItem != null)
            {
                SelectedTaskItem = selectedItem;
                CheckReel(SelectedReelId);

                if (SelectedTaskItem != null)
                {
                    RollSelectedGrid.LoadItems();
                    RollGrid.LoadItems();
                }
            }
        }

        /// <summary>
        /// обновление методов работы с выбранной в гриде строкой
        /// </summary>
        /// <param name="selectedItem"></param>
        public void RollGridUpdateActions(Dictionary<string, string> selectedItem)
        {
            SelectedRollItem = selectedItem;

            //SearchRoll.IsEnabled = SelectedTaskItem != null;
        }

        /// <summary>
        /// обновление методов работы с выбранной в гриде строкой
        /// </summary>
        /// <param name="selectedItem"></param>
        public void RollSelectedGridUpdateActions(Dictionary<string, string> selectedItem)
        {
            SelectedTaskRollItem = selectedItem;

            //DelRollButton.IsEnabled = SelectedTaskRollItem != null;
            //AddRollButton.IsEnabled = SelectedRollItem != null && SelectedTaskItem != null;
        }

        /// <summary>
        /// обработка ввода с клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.F5:
                    TaskGrid.LoadItems();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

                case Key.Home:
                    TaskGrid.SetSelectToFirstRow();
                    e.Handled = true;
                    break;

                case Key.End:
                    TaskGrid.SetSelectToLastRow();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// отображение справочной статьи
        /// (относительный путь)
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/production/rolls_diagramm/roll_pt_binding");
        }

        /// <summary>
        /// экспорт в Excel
        /// </summary>
        private async void Export()
        {
            if (TaskGrid != null)
            {
                if (TaskGrid.Items.Count > 0)
                {
                    var eg = new ExcelGrid();
                    var cols = TaskGrid.Columns;
                    eg.SetColumnsFromGrid(cols);
                    eg.Items = TaskGrid.Items;
                    await Task.Run(() =>
                    {
                        eg.Make();
                    });
                }
            }
        }

        /// <summary>
        /// Вызов настроек
        /// </summary>
        public void ShowSettings()
        {
            var settings = new ShipmentSettings(1);
            settings.ReceiverTabName = "RollListForCompletedTasks";
            settings.Edit();
        }

        /// <summary>
        /// установка активного ГА
        /// </summary>
        /// <param name="cmId"></param>
        private void CheckMachine(int cmId = 0)
        {
            if (cmId != 0)
            {
                SelectedMachineId = cmId;
                CheckReel(1);
            }

            //все остальные в исходное состояние
            CM1Button.Style = (Style)CM1Button.TryFindResource("Button");
            CM2Button.Style = (Style)CM2Button.TryFindResource("Button");
            CM3Button.Style = (Style)CM3Button.TryFindResource("Button");
            KshCM1Button.Style = (Style)KshCM1Button.TryFindResource("Button");

            //кнопку активного раската в активное состояние
            switch (SelectedMachineId)
            {
                case 2:
                    CM1Button.Style = (Style)CM1Button.TryFindResource("FButtonPrimary");
                    Reel4Button.Visibility = Visibility.Visible;
                    Reel5Button.Visibility = Visibility.Visible;
                    break;

                case 21:
                    CM2Button.Style = (Style)CM2Button.TryFindResource("FButtonPrimary");
                    Reel4Button.Visibility = Visibility.Collapsed;
                    Reel5Button.Visibility = Visibility.Collapsed;
                    break;

                case 22:
                    CM3Button.Style = (Style)CM3Button.TryFindResource("FButtonPrimary");
                    Reel4Button.Visibility = Visibility.Visible;
                    Reel5Button.Visibility = Visibility.Visible;
                    break;

                case 23:
                    KshCM1Button.Style = (Style)KshCM1Button.TryFindResource("FButtonPrimary");
                    Reel4Button.Visibility = Visibility.Visible;
                    Reel5Button.Visibility = Visibility.Visible;
                    break;
            }
        }

        /// <summary>
        /// установка активного раската ГА
        /// </summary>
        /// <param name="cmId"></param>
        private void CheckReel(int reelNum = 0)
        {
            if (reelNum != 0)
            {
                SelectedReelId = reelNum;
            }

            //все остальные в исходное состояние
            Reel1Button.Style = (Style)Reel1Button.TryFindResource("Button");
            Reel2Button.Style = (Style)Reel2Button.TryFindResource("Button");
            Reel3Button.Style = (Style)Reel3Button.TryFindResource("Button");
            Reel4Button.Style = (Style)Reel4Button.TryFindResource("Button");
            Reel5Button.Style = (Style)Reel5Button.TryFindResource("Button");

            //кнопку активного раската в активное состояние
            switch (SelectedReelId)
            {
                case 1:
                    Reel1Button.Style = (Style)Reel1Button.TryFindResource("FButtonPrimary");
                    SelectedRawGroup = TaskGrid.SelectedItem.CheckGet("ID_RAW_GROUP_1").ToInt();
                    break;

                case 2:
                    Reel2Button.Style = (Style)Reel2Button.TryFindResource("FButtonPrimary");
                    SelectedRawGroup = TaskGrid.SelectedItem.CheckGet("ID_RAW_GROUP_2").ToInt();
                    break;

                case 3:
                    Reel3Button.Style = (Style)Reel3Button.TryFindResource("FButtonPrimary");
                    SelectedRawGroup = TaskGrid.SelectedItem.CheckGet("ID_RAW_GROUP_3").ToInt();
                    break;

                case 4:
                    Reel4Button.Style = (Style)Reel4Button.TryFindResource("FButtonPrimary");
                    SelectedRawGroup = TaskGrid.SelectedItem.CheckGet("ID_RAW_GROUP_4").ToInt();
                    break;

                case 5:
                    Reel5Button.Style = (Style)Reel5Button.TryFindResource("FButtonPrimary");
                    SelectedRawGroup = TaskGrid.SelectedItem.CheckGet("ID_RAW_GROUP_5").ToInt();
                    break;
            }

            if (TaskGrid.SelectedItem != null)
            {
                RollSelectedGrid.LoadItems();
                RollGrid.LoadItems();
            }

        }

        /// <summary>
        /// Кнопка Показать
        /// </summary>
        private void Refresh()
        {
            bool resume = true;

            if (resume)
            {
                var f = FromDate.Text.ToDateTime();
                var t = ToDate.Text.ToDateTime();
                if (DateTime.Compare(f, t) > 0)
                {
                    var msg = "Дата начала должна быть меньше даты окончания.";
                    var d = new DialogWindow($"{msg}", "Проверка данных");
                    d.ShowDialog();
                    resume = false;
                }
            }

            if (resume)
            {
                TaskGrid.LoadItems();
            }

            RefreshButton.Style = (Style)RefreshButton.TryFindResource("Button");
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            Export();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
           Refresh();
        }

        private void CM1Button_Click(object sender, RoutedEventArgs e)
        {
            CheckMachine(2);
            TaskGrid.LoadItems();
        }

        private void CM2Button_Click(object sender, RoutedEventArgs e)
        {
            CheckMachine(21);
            TaskGrid.LoadItems();
        }

        private void CM3Button_Click(object sender, RoutedEventArgs e)
        {
            CheckMachine(22);
            TaskGrid.LoadItems();
        }

        private void KshCM1Button_Click(object sender, RoutedEventArgs e)
        {
            CheckMachine(23);
            TaskGrid.LoadItems();
        }

        private void Reel1Button_Click(object sender, RoutedEventArgs e)
        {
            CheckReel(1);
            TaskGrid.LoadItems();
        }

        private void Reel2Button_Click(object sender, RoutedEventArgs e)
        {
            CheckReel(2);
            TaskGrid.LoadItems();
        }

        private void Reel3Button_Click(object sender, RoutedEventArgs e)
        {
            CheckReel(3);
            TaskGrid.LoadItems();
        }

        private void Reel4Button_Click(object sender, RoutedEventArgs e)
        {
            CheckReel(4);
            TaskGrid.LoadItems();
        }

        private void Reel5Button_Click(object sender, RoutedEventArgs e)
        {
            CheckReel(5);
            TaskGrid.LoadItems();
        }

        private void AddRollButton_Click(object sender, RoutedEventArgs e)
        {
            RollSelectedGridAddItem();
        }

        /// <summary>
        /// добавление записи (привязанные рулоны)
        /// </summary>
        public async void RollSelectedGridAddItem()
        {
            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();

                p.CheckAdd("PRCW_ID", SelectedTaskItem.CheckGet("PRCW_ID"));
                p.CheckAdd("LENGTH", SelectedTaskItem.CheckGet("LEN").ToInt().ToString());
                p.CheckAdd("PCRT_ID", RollGrid.SelectedItem.CheckGet("PCRT_ID").ToInt().ToString());
                p.CheckAdd("PCRO_ID", RollGrid.SelectedItem.CheckGet("PCRO_ID").ToInt().ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "Roll");
                q.Request.SetParam("Action", "AddWorkRoll");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });
            }

            TaskGrid.LoadItems();
            RollSelectedGrid.LoadItems();
            RollGrid.LoadItems();
        }

        private void DelRollButton_Click(object sender, RoutedEventArgs e)
        {
            var dw = new DialogWindow("Вы действительно хотите удалить информацию о рулоне из ПЗ?", $"Удаление информации о рулоне",
                $"Подтверждение удаления информации\nо рулоне: {SelectedTaskRollItem.CheckGet("NUM")}\nиз ПЗ: {SelectedTaskItem.CheckGet("ID_PZ")}", DialogWindowButtons.NoYes);
            if (dw.ShowDialog() == true)
            {
                RollSelectedGridRemoveItem();
            }
        }

        private async void RollSelectedGridRemoveItem()
        {
            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();

                var pcwrId = SelectedTaskRollItem.CheckGet("PCWR_ID") != "" ? SelectedTaskRollItem.CheckGet("PCWR_ID") : null;

                p.CheckAdd("pcwr_id", pcwrId);

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "Roll");
                q.Request.SetParam("Action", "RemoveWorkRoll");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });
            }

            TaskGrid.LoadItems();
            RollSelectedGrid.LoadItems();
            RollGrid.LoadItems();
        }

        /// <summary>
        /// Обработчик изменения начальной даты отчета
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void ToDateTextChanged(object sender, TextChangedEventArgs args)
        {
            RefreshButton.Style = (Style)RefreshButton.TryFindResource("FButtonPrimary");
        }

        /// <summary>
        /// Обработчик изменения конечной даты отчета
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void FromDateTextChanged(object sender, TextChangedEventArgs args)
        {
            RefreshButton.Style = (Style)RefreshButton.TryFindResource("FButtonPrimary");
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowSettings();
        }
    }
}
