using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Client.Interfaces.Main;
using static Client.Interfaces.Main.DataGridHelperColumn;
using Client.Assets.HighLighters;
using System.Data;
using DevExpress.Xpf.Grid;
using Client.Interfaces.Production;

namespace Client.Interfaces.Production.Corrugator.TaskPlanningKashira
{
    /// <summary>
    /// Интерфейс для управление планом для гофроагрегата
    /// Типы для управления:
    /// Unknow = 0, Gofra5 = 2, Gofra3 = 21, Fosber = 22 
    /// <author>eletskikh_ya</author>
    /// </summary>

    public partial class TaskDxList : UserControl, ITaskList
    {
        

        public TaskDxList()
        {
            InitializeComponent();

            TaskLists.Add(this);
        }

        /// <summary>
        /// Таймаут для сохраннеия
        /// </summary>
        private static int SaveTimeout => 10000;
        public bool EnableSortIdles { get; set; } = true;

        public event Action<TaskPlaningDataSet.TypeStanok, Dictionary<string,string>> OnSelectCheckbox;

        private TaskPlaningDataSet.TypeStanok _CurrentMachineId;
        private List<DataGridHelperColumn> Columns { get; set; }

        private int MaxFormat = 0;

        private double Hours { get; set; } = 0.0;

        private string CurrentPlaceName { get; set; }
        private ITaskPanel Panel { get; set; }
        public string GetTaskSearchText => Panel.SearchBox.Text;

        private bool DropDownFlag { get; set; }

        public TaskPlanningKsh TaskPlanning { get; private set; }

        private int CurrentRowIndex { 
            get; 
            set; 
        }

        private int DropedNumberId
        {
            get;set;
        }

        public TaskPlaningDataSet.TypeStanok CurrentMachineId
        {
            get
            {
                return _CurrentMachineId;
            }
            set
            {
                _CurrentMachineId = value;
            }
        }

        private DataTable GridTable
        {
            get
            {
                DataTable result = null;

                //Dispatcher.Invoke(() => result = TaskGrid.GridControl.ItemsSource as DataTable);

                result = TaskGrid.GridControl.ItemsSource as DataTable;

                return result;
            }
        }

        public static List<TaskDxList> TaskLists = new List<TaskDxList>();

        public Dictionary<string, string> LastSelectedItemFroMultiSelect { get; private set; }
        public Dictionary<string, string> SelectedTaskItem { get; private set; }
        public bool RefreshGridFlag { get; private set; } = false;

        public void Init(TaskPlanningKsh taskPlanning, TaskPlaningDataSet.TypeStanok machineId = TaskPlaningDataSet.TypeStanok.Unknow)
        {
            CurrentRowIndex = -1;
            _CurrentMachineId = machineId;
            TaskPlanning = taskPlanning;

            DropDownFlag = false;

            InitDefaults();
            InitGrid();
            
            if (_CurrentMachineId == TaskPlaningDataSet.TypeStanok.Unknow)
            {

                
            }
        }

        /// <summary>
        /// инициализация грида
        /// </summary>
        public void InitGrid()
        {
            //инициализация грида
            {

                int Width25 = 2;
                int Width65 = 8;
                int Width60 = 5;
                int Width9 = 9;
                int WidthLayer = 8;

                bool DefaultSortingOption = false;
                bool DefaultFilteringOption = false;

                bool FreeOrderFilteringEnable = _CurrentMachineId == TaskPlaningDataSet.TypeStanok.Unknow;


                //список колонок грида
                Columns = new List<DataGridHelperColumn>()
                {
                    new DataGridHelperColumn
                    {
                        Header = "*",
                        Path="_SELECTED",
                        ColumnType=ColumnTypeRef.Boolean,
                        Editable = true,
                        Width2=1,

                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskColors.GetColor("ON_MACHINE", row)
                            },
                        },

                        DxEnableColumnFiltering = DefaultFilteringOption,
                        DxEnableColumnSorting = DefaultSortingOption,
                       
                    },
                    new DataGridHelperColumn
                    {
                        Header="_#",
                        Path="_ROWNUMBER",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width2=1,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {

                                StylerTypeRef.BackgroundColor,

                                row => TaskColors.GetColor(_CurrentMachineId == TaskPlaningDataSet.TypeStanok.Unknow ? TaskPlaningDataSet.Dictionary.OtherMachine :"ON_MACHINE", row)
                            },
                        },
                        DxEnableColumnFiltering = DefaultFilteringOption,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="NUMBER_ID_FREE",
                        Path="NUMBER_ID_FREE",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width2=20,
                        Visible = false,
                        DxEnableColumnFiltering = DefaultFilteringOption,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn
                    {
                        Header="#",
                        Path="NUMBER_ID",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width2=Width25,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskColors.GetColor("ON_MACHINE", row)
                            },
                        },
                        Visible = false, //_CurrentMachineId!= TaskPlaningDataSet.TypeStanok.Unknow,
                        DxEnableColumnFiltering = DefaultFilteringOption,
                        DxEnableColumnSorting = DefaultSortingOption,

                    },
                    new DataGridHelperColumn
                    {
                        Header="Начало",
                        Path=TaskPlaningDataSet.Dictionary.StartPlanedTime,
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2=Width65,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskColors.GetColor("ON_MACHINE", row)
                            },
                        },
                        Visible = false,
                        DxEnableColumnFiltering = DefaultFilteringOption || FreeOrderFilteringEnable,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Расчетное время",
                        Path=TaskPlaningDataSet.Dictionary.CalculatedTime,
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2=Width9,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = TaskColors.GetColor(TaskPlaningDataSet.Dictionary.StartBeforeTime, row);
                                    if ((result as Brush) == null)
                                    {
                                        result = TaskColors.GetColor("ON_MACHINE", row);
                                    }
                                    return result;
                                }
                            },
                        },
                        Visible = _CurrentMachineId != 0,
                        DxEnableColumnFiltering = DefaultFilteringOption || FreeOrderFilteringEnable,
                        DxEnableColumnSorting = DefaultSortingOption,
                        Format = TaskPlaningDataSet.DateTimeFormat,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Начать до",
                        Path=TaskPlaningDataSet.Dictionary.StartBeforeTime,
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2=Width65,
                        Format = TaskPlaningDataSet.DateTimeFormat,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {   
                                    object result;
                                    if(CurrentMachineId== TaskPlaningDataSet.TypeStanok.Unknow)
                                    {
                                        result = TaskColors.GetColor(TaskPlaningDataSet.Dictionary.StartBeforeTime, row);
                                    }
                                    else
                                    {
                                        result = TaskColors.GetColor("ON_MACHINE", row);
                                    }


                                    var dateTime = row.CheckGet(TaskPlaningDataSet.Dictionary.StartBeforeTime).ToDateTime();

                                    if(dateTime>TaskPlaningDataSet.LastDateTime)
                                    {
                                        result = HColor.Yellow.ToBrush();
                                    }


                                    /*// TaskColors.GetColor(TaskPlaningDataSet.Dictionary.StartBeforeTime, row);
                                    if ((result as Brush) == null)
                                    {
                                        result = TaskColors.GetColor("ON_MACHINE", row);
                                    }*/

                                    return result;
                                }
                            },
                        },
                        DxEnableColumnFiltering = DefaultFilteringOption || FreeOrderFilteringEnable,
                        DxEnableColumnSorting = DefaultSortingOption,
                        //Visible = false,
                    },
                    //new DataGridHelperColumn()
                    //{
                    //    Header="Начало отгрузки",
                    //    Path="SHIPMENT_START",
                    //    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    //    Width=65,
                    //},
                    //new DataGridHelperColumn()
                    //{
                    //    Header="Окончание отгрузки",
                    //    Path="SHIPMENT_FINISH",
                    //    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    //    Width=65,
                    //},
                    //new DataGridHelperColumn()
                    //{
                    //    Header="Крайняя дата начала отгрузки",
                    //    Path="SHIPMENT_DEADLINE",
                    //    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    //    Width=65,
                    //},

                    //new DataGridHelperColumn()
                    //{
                    //    Header="Примечание",
                    //    Path="NOTE",
                    //    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    //    Width=60,
                    //},

                    new DataGridHelperColumn()
                    {
                        Header="Ошибки и инфо",
                        Path="ERRORS",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width2=20,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.ForegroundColor,
                                row => HColor.RedFG
                            },
                            {
                                StylerTypeRef.FontWeight,
                                row => FontWeights.SemiBold
                            },
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskColors.GetColor("ON_MACHINE", row)
                            },
                        },
                        
                        Visible = _CurrentMachineId != 0,
                        DxEnableColumnFiltering = DefaultFilteringOption,
                        DxEnableColumnSorting = DefaultSortingOption,
                        DxHeaderToolTip = "Сообщение об ошибке",


                        Labels= CreateLabels("ERRORS")
                    },

                    new DataGridHelperColumn()
                    {
                        Header="ИД ПЗ",
                        Path="PRODUCTION_TASK_ID",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width2=7,
                        Visible = false,
                        DxEnableColumnFiltering = DefaultFilteringOption || FreeOrderFilteringEnable,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Номер ПЗ",
                        Path="PRODUCTION_TASK_NUMBER",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width2=9,

                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = TaskColors.GetColor("PRODUCTION_TASK_NUMBER", row);
                                    
                                    return result;
                                }
                            },
                        },

                        DxEnableColumnFiltering = DefaultFilteringOption || FreeOrderFilteringEnable,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },

                    new DataGridHelperColumn()
                    {
                        Header="Профиль",
                        Path=TaskPlaningDataSet.Dictionary.ProfilName,
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width2=1,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskColors.GetColor(TaskPlaningDataSet.Dictionary.ProfilName, row)
                            },
                        },
                        DxEnableColumnFiltering = DefaultFilteringOption || FreeOrderFilteringEnable,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Формат",
                        Path="WIDTH",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width2=4,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskColors.GetColor("WIDTH", row)
                            },
                            {
                                StylerTypeRef.FontWeight,
                                row =>
                                {
                                    var fontWeight= new FontWeight();
                                    fontWeight=FontWeights.Normal;


                                    var width = row.CheckGet("WIDTH").ToInt();
                                    if(width>0)
                                    {
                                        width/=100;
                                        if(width%2!=0)
                                        {
                                            fontWeight=FontWeights.Bold;
                                        }
                                    }

                                    return fontWeight;
                                }
                            },
                        },
                        DxEnableColumnFiltering = DefaultFilteringOption || FreeOrderFilteringEnable,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Длина, м",
                        Path="LENGTH",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width2=5,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskColors.GetColor("LENGTH", row)
                            },
                        },
                        DxEnableColumnFiltering = DefaultFilteringOption || FreeOrderFilteringEnable,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="1",
                        Path="LAYER_1",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width2=WidthLayer,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskColors.GetColor("LAYER_1", row)
                            },
                            {
                                StylerTypeRef.FontWeight,
                                row =>
                                {
                                    var fontWeight= new FontWeight();
                                    fontWeight=FontWeights.Normal;

                                    

                                    if( row.CheckGet(TaskPlaningDataSet.Dictionary.Reel).Contains("5"))
                                    {
                                        fontWeight=FontWeights.Bold;
                                    }

                                    return fontWeight;
                                }
                            },
                            {
                                StylerTypeRef.ForegroundColor,
                                row=>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    int flag = row.CheckGet(TaskPlaningDataSet.Dictionary.FLAG).ToInt();

                                    if((flag&1)==1)
                                    {
                                        result = HColor.BlueFG.ToBrush();
                                    }

                                    return result;
                                }
                            }
                        },
                        DxEnableColumnFiltering = DefaultFilteringOption || FreeOrderFilteringEnable,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="2",
                        Path="LAYER_2",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width2=WidthLayer,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskColors.GetColor("LAYER_2", row)
                            },
                            {
                                StylerTypeRef.FontWeight,
                                row =>
                                {
                                    var fontWeight= new FontWeight();
                                    fontWeight=FontWeights.Normal;

                                    if( row.CheckGet(TaskPlaningDataSet.Dictionary.Reel).Contains("4"))
                                    {
                                        fontWeight=FontWeights.Bold;
                                    }
                                    else if(string.IsNullOrEmpty(row.CheckGet(TaskPlaningDataSet.Dictionary.Layer4)))
                                    {
                                        // проверим, возможно валы поменены
                                        if( row.CheckGet(TaskPlaningDataSet.Dictionary.Reel).Contains("2"))
                                        {
                                            fontWeight=FontWeights.Bold;
                                        }
                                    }
                                        

                                    return fontWeight;
                                }
                            },
                            {
                                StylerTypeRef.ForegroundColor,
                                row=>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    int flag = row.CheckGet(TaskPlaningDataSet.Dictionary.FLAG).ToInt();

                                    if((flag&2)==2)
                                    {
                                        result = HColor.BlueFG.ToBrush();
                                    }

                                    return result;
                                }
                            }
                        },
                        DxEnableColumnFiltering = DefaultFilteringOption || FreeOrderFilteringEnable,
                        DxEnableColumnSorting = DefaultSortingOption,

                    },
                    new DataGridHelperColumn()
                    {
                        Header="3",
                        Path="LAYER_3",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width2=WidthLayer,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskColors.GetColor("LAYER_3", row)
                            },
                            {
                                StylerTypeRef.FontWeight,
                                row =>
                                {
                                    var fontWeight= new FontWeight();
                                    fontWeight=FontWeights.Normal;

                                    if( row.CheckGet(TaskPlaningDataSet.Dictionary.Reel).Contains("3"))
                                    {
                                        fontWeight=FontWeights.Bold;
                                    }
                                    else if(string.IsNullOrEmpty(row.CheckGet(TaskPlaningDataSet.Dictionary.Layer5)))
                                    {
                                        // проверим, возможно валы поменены
                                        if( row.CheckGet(TaskPlaningDataSet.Dictionary.Reel).Contains("1"))
                                        {
                                            fontWeight=FontWeights.Bold;
                                        }
                                    }

                                    return fontWeight;
                                }
                            },
                            {
                                StylerTypeRef.ForegroundColor,
                                row=>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    int flag = row.CheckGet(TaskPlaningDataSet.Dictionary.FLAG).ToInt();

                                    if((flag&4)==4)
                                    {
                                        result = HColor.BlueFG.ToBrush();
                                    }

                                    return result;
                                }
                            }
                        },

                        DxEnableColumnFiltering = DefaultFilteringOption || FreeOrderFilteringEnable,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="4",
                        Path="LAYER_4",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width2=WidthLayer,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskColors.GetColor("LAYER_4", row)
                            },
                            {
                                StylerTypeRef.FontWeight,
                                row =>
                                {
                                    var fontWeight= new FontWeight();
                                    fontWeight=FontWeights.Normal;

                                    if( row.CheckGet(TaskPlaningDataSet.Dictionary.Reel).Contains("2"))
                                    {
                                        fontWeight=FontWeights.Bold;
                                    }
                                    else if(string.IsNullOrEmpty(row.CheckGet(TaskPlaningDataSet.Dictionary.Layer2)))
                                    {
                                        // проверим, возможно валы поменены
                                        if( row.CheckGet(TaskPlaningDataSet.Dictionary.Reel).Contains("4"))
                                        {
                                            fontWeight=FontWeights.Bold;
                                        }
                                    }

                                    return fontWeight;
                                }
                            },
                            {
                                StylerTypeRef.ForegroundColor,
                                row=>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    int flag = row.CheckGet(TaskPlaningDataSet.Dictionary.FLAG).ToInt();

                                    if((flag&8)==8)
                                    {
                                        result = HColor.BlueFG.ToBrush();
                                    }

                                    return result;
                                }
                            }
                        },
                        DxEnableColumnFiltering = DefaultFilteringOption || FreeOrderFilteringEnable,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="5",
                        Path="LAYER_5",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width2=WidthLayer,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskColors.GetColor("LAYER_5", row)
                            },
                            {
                                StylerTypeRef.FontWeight,
                                row =>
                                {
                                    var fontWeight= new FontWeight();
                                    fontWeight=FontWeights.Normal;

                                    if( row.CheckGet(TaskPlaningDataSet.Dictionary.Reel).Contains("1"))
                                    {
                                        fontWeight=FontWeights.Bold;
                                    }
                                    else if(string.IsNullOrEmpty(row.CheckGet(TaskPlaningDataSet.Dictionary.Layer3)))
                                    {
                                        // проверим, возможно валы поменены
                                        if( row.CheckGet(TaskPlaningDataSet.Dictionary.Reel).Contains("3"))
                                        {
                                            fontWeight=FontWeights.Bold;
                                        }
                                    }

                                    return fontWeight;
                                }
                            },
                            {
                                StylerTypeRef.ForegroundColor,
                                row=>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    int flag = row.CheckGet(TaskPlaningDataSet.Dictionary.FLAG).ToInt();

                                    if((flag&16)==16)
                                    {
                                        result = HColor.BlueFG.ToBrush();
                                    }

                                    return result;
                                }
                            }
                        },
                        DxEnableColumnFiltering = DefaultFilteringOption || FreeOrderFilteringEnable,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Флаг разрыва сырья в блоке",
                        Path=TaskPlaningDataSet.Dictionary.FLAG,
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width2=3,
                        DxEnableColumnFiltering = false,
                        DxEnableColumnSorting = false,
                        Visible = false,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Самокройные",
                        Path=TaskPlaningDataSet.Dictionary.ID2Count,
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width2=3,
                        DxEnableColumnFiltering = false,
                        DxEnableColumnSorting = false,
                        Visible = false,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Плановое время выполнения, минут",
                        Path=TaskPlaningDataSet.Dictionary.Duration,
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Double,
                        Width2=3,
                        DxEnableColumnFiltering = false,
                        DxEnableColumnSorting = false,
                        Visible = false,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Расчетное время выполнения, минут",
                        Path=TaskPlaningDataSet.Dictionary.CalculatedDuration,
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Double,
                        Width2=3,
                        DxEnableColumnFiltering = DefaultFilteringOption,
                        DxEnableColumnSorting = DefaultSortingOption,

                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskColors.GetColor(TaskPlaningDataSet.Dictionary.CalculatedDuration, row)
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Станок",
                        Path="MACHINE",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 20,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskColors.GetColor("MACHINE", row)
                            },
                        },
                        DxEnableColumnFiltering = DefaultFilteringOption,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Качество",
                        Path="QID",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 7,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskColors.GetColor("CHECK_QID", row)
                            },
                        },
                        DxEnableColumnFiltering = DefaultFilteringOption || FreeOrderFilteringEnable,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="FF",
                        Path=TaskPlaningDataSet.Dictionary.Fanfold,
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        Width2=3,
                        DxEnableColumnFiltering = DefaultFilteringOption || FreeOrderFilteringEnable,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Тандем",
                        Path=TaskPlaningDataSet.Dictionary.Tandem,
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        Width2=3,
                        DxEnableColumnFiltering = DefaultFilteringOption || FreeOrderFilteringEnable,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Обрезь, мм",
                        Path="TRIM",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width2=3,
                        DxEnableColumnFiltering = DefaultFilteringOption || FreeOrderFilteringEnable,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Склеивание",
                        Path="GLUED",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        Width2=3,
                        DxEnableColumnFiltering = DefaultFilteringOption || FreeOrderFilteringEnable,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Печать",
                        Path="COLOR",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        Width2=3,
                        DxEnableColumnFiltering = DefaultFilteringOption || FreeOrderFilteringEnable,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Присутствие представителя",
                        Path="REPRESENTATIVE_IS",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        Width2=3,
                        DxEnableColumnFiltering = DefaultFilteringOption || FreeOrderFilteringEnable,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="На горячую",
                        Path="HOT_IS",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        Width2=3,
                        DxEnableColumnFiltering = DefaultFilteringOption || FreeOrderFilteringEnable,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Нет клише",
                        Path=TaskPlaningDataSet.Dictionary.NonclicheIs,
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        Width2=3,
                        DxEnableColumnFiltering = DefaultFilteringOption || FreeOrderFilteringEnable,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Нет штанцформы",
                        Path=TaskPlaningDataSet.Dictionary.NonshtanzIs, 
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        Width2=3,
                        DxEnableColumnFiltering = DefaultFilteringOption || FreeOrderFilteringEnable,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Артикул",
                        Path="ARTIKUL",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width2=Width60,
                        DxEnableColumnFiltering = DefaultFilteringOption || FreeOrderFilteringEnable,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="ИД ТС",
                        Path=TaskPlaningDataSet.Dictionary.TransportId,
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width2=Width60,
                        DxEnableColumnFiltering = DefaultFilteringOption || FreeOrderFilteringEnable,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Длина блока",
                        Path=TaskPlaningDataSet.Dictionary.BlockLength,
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width=5,
                        Visible = true, // _CurrentMachineId== TaskPlaningDataSet.TypeStanok.Unknow,
                        DxEnableColumnFiltering = DefaultFilteringOption || FreeOrderFilteringEnable,
                        DxEnableColumnSorting = DefaultSortingOption,

                    },
                    new DataGridHelperColumn()
                    {
                        Header="Продолжительность блока",
                        Path=TaskPlaningDataSet.Dictionary.BlockTimeLength,
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width=5,
                        Visible = true, // _CurrentMachineId== TaskPlaningDataSet.TypeStanok.Unknow,
                        DxEnableColumnFiltering = DefaultFilteringOption || FreeOrderFilteringEnable,
                        DxEnableColumnSorting = DefaultSortingOption,

                    },
                    new DataGridHelperColumn()
                    {
                        Header="Сырьё",
                        Path=TaskPlaningDataSet.Dictionary.Reel,
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width2=5,
                        Visible =false,
                        DxEnableColumnFiltering = DefaultFilteringOption,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Id простоя",
                        Path=TaskPlaningDataSet.Dictionary.DropdownId,
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width2=5,
                        Visible=false,
                        DxEnableColumnFiltering = DefaultFilteringOption,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Id станка",
                        Path=TaskPlaningDataSet.Dictionary.StanokId,
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width2=5,
                        Visible = false,
                        DxEnableColumnFiltering = DefaultFilteringOption,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },

                    new DataGridHelperColumn()
                    {
                        Header="Вал 1",
                        Path=TaskPlaningDataSet.Dictionary.MF1,
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width2=2,
                        Visible = false,
                        DxEnableColumnFiltering = DefaultFilteringOption,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },

                    new DataGridHelperColumn()
                    {
                        Header="Вал 2",
                        Path=TaskPlaningDataSet.Dictionary.MF2,
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width2=2,
                        Visible = false,
                        DxEnableColumnFiltering = DefaultFilteringOption,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },

                    new DataGridHelperColumn()
                    {
                        Header="Вал",
                        Path=TaskPlaningDataSet.Dictionary.VAL,
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width2=2,
                        Visible = true,
                        DxEnableColumnFiltering = DefaultFilteringOption,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },

                    new DataGridHelperColumn()
                    {
                        Header="на станке",
                        Path=TaskPlaningDataSet.Dictionary.OnMachine,
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width2=2,
                        Visible = false,
                    },

                    new DataGridHelperColumn()
                    {
                        Header="Присутствие",
                        Path=TaskPlaningDataSet.Dictionary.OtherMachine,
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width2=2,
                        Visible = false,
                        
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Возможность выполнения на различных ГА",
                        Path=TaskPlaningDataSet.Dictionary.PossibleMachine,
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width2=2,
                        Visible = false,
                    },

                    new DataGridHelperColumn()
                    {
                        Header="Крайняя дата",
                        Path=TaskPlaningDataSet.Dictionary.LastDate,
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                        Width2=2,
                        Visible = CurrentMachineId== TaskPlaningDataSet.TypeStanok.Unknow,
                    },
                    
                    new DataGridHelperColumn
                    {
                        Header = "CHECK_QID",
                        Path = TaskPlaningDataSet.Dictionary.CheckQid,
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 5,
                        Visible = false,
                    },
                    
                    new DataGridHelperColumn
                    {
                        Header = "NKLISHE",
                        Path = TaskPlaningDataSet.Dictionary.NameCliche,
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 5,
                        Visible = false,
                    }
                  


                };


                //TaskGrid.OnFilterItems = FilterItems;
                TaskGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;


                TaskGrid.SetColumns(Columns);

                //if (_CurrentMachineId == 0)
                TaskGrid.SetPrimaryKey(TaskPlaningDataSet.Dictionary.RowNumber);
                //else
                //    TaskGrid.SetPrimaryKey(TaskPlaningDataSet.Dictionary.NumberId);

                //TaskGrid.PrimaryKey = "PRODUCTION_TASK_ID";
                TaskGrid.SearchText = Panel.SearchBox;

                // к сожалению не работает
                //TaskGrid.UseRowDragDrop = true;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                //TaskGrid.OnSelectItem = (Dictionary<string, string> selectedItem) =>
                //{
                //    if (selectedItem != null)
                //    {
                //        UpdateActions(selectedItem);
                //    }
                //};



                TaskGrid.OnDblClick = (Dictionary<string, string> selectedItem) =>
                {
                    if (SelectedTaskItem != null)
                    {
                        //  если мы кликаем на свободных заданиях, то попросим остальные гриды
                        //  найти подходящие для данного грида задания 
                        if (SelectedTaskItem.CheckGet(TaskPlaningDataSet.Dictionary.DropdownId).ToInt() == 0)
                        {
                            //if (_CurrentMachineId == TaskPlaningDataSet.TypeStanok.Unknow)
                            {
                                TaskLists.ForEach(item =>
                                {
                                    if (item.CurrentMachineId != _CurrentMachineId)
                                    {
                                        item.FindBestPosition(SelectedTaskItem);
                                    }
                                });
                            }
                        }
                        else
                        {
                            new IdleItem().Edit(SelectedTaskItem);
                        }
                    }
                };


                TaskGrid.AutoUpdateInterval = 0;

                //данные грида
                TaskGrid.OnLoadItems = LoadItems;

                TaskGrid.Init();

                //фокус ввода           
                TaskGrid.Focus();

                ShowSplash();

                TaskGrid.GridView.AllowColumnFiltering = true;
                TaskGrid.GridView.AllowDragDrop = true;
                TaskGrid.GridView.DropRecord += GridView_DropRecord;
                TaskGrid.GridView.StartRecordDrag += GridView_StartRecordDrag;
                TaskGrid.GridView.DragRecordOver += GridView_DragRecordOver;
                TaskGrid.GridView.CompleteRecordDragDrop += GridView_CompleteRecordDragDrop;
                TaskGrid.GridControl.SelectedItemChanged += GridControl_SelectedItemChanged;

                TaskGrid.GridControl.FilterChanged += Grid_FilterChanged;

                //TaskGrid.GridControl.MouseMove += GridControl_MouseMove;
                //TaskGrid.GridView.CellToolTipTemplate = new DataTemplate(typeof(TaskItem));
                //TaskGrid.GridView.CellToolTipTemplate
                //TaskGrid.GridView.CellToolTipTemplate


                TaskGrid.Menu = new Dictionary<string, DataGridContextMenuItem>
                {
                    {
                        "up",
                        new DataGridContextMenuItem()
                        {
                            Header = "Вверх",
                            Action = () => { MenuMoveTasks("up"); }
                        }
                    },
                    {
                        "down",
                        new DataGridContextMenuItem()
                        {
                            Header = "Вниз",
                            Action = () => { MenuMoveTasks("down"); }
                        }
                    },
                    {
                        "del",
                        new DataGridContextMenuItem()
                        {
                            Header = "Удалить",
                            Action = () => {
                                MenuDelete();
                            }
                            // , Tag = "access_mode_full_access"
                        }
                    },
                    {
                        "delFromMachine",
                        new DataGridContextMenuItem()
                        {
                            Header = "Удалить со станка",
                            Action = () => {
                                MenuDeleteFromMachine();
                            }
                            // , Tag = "access_mode_full_access"
                        }
                    },
                    {
                        "changeLayers",
                        new DataGridContextMenuItem()
                        {
                            Header = "Сменить валы",
                            Action = () => {
                                ChangeLayers();
                            }
                            // , Tag = "access_mode_full_access"
                        }
                    },

                    {
                        "changeFormat",
                        new DataGridContextMenuItem()
                        {
                            Header = "Формат"
                            // , Tag = "access_mode_full_access"
                        }
                    },

                    {
                        "Update",
                        new DataGridContextMenuItem()
                        {
                            Header = "Обновить",
                             Action = () => {
                                Update();
                            }
                        }
                    },

                    {
                        "DiffResources",
                        new DataGridContextMenuItem()
                        {
                            Header = "Поиск разрывов сырья",
                             Action = () => {
                                FindBreakResources();
                            }
                             ,
                             Enabled = CurrentMachineId!= TaskPlaningDataSet.TypeStanok.Unknow
                        }
                    },

                    {
                        "Excel",
                        new DataGridContextMenuItem()
                        {
                            Header = "Экспортировать в Excel",
                             Action = () => {
                                Excel();
                            }
                        }
                    },

                    {
                        "Edit",
                        new DataGridContextMenuItem()
                        {
                            Header = "Раскрой",
                             Action = () => {
                                Edit();
                            }
                        }

                    },

                };


                // меню прееноса выделенных заказов на гофроагрегат в нужную позицию
                if(_CurrentMachineId== TaskPlaningDataSet.TypeStanok.Unknow)
                {
                    TaskGrid.Menu.Add("move", 
                        new DataGridContextMenuItem()
                        {
                            Header = "Перенос", 
                            Items = new Dictionary<string,DataGridContextMenuItem>
                            {
                                {
                                    "Js",
                                    new DataGridContextMenuItem()
                                    {
                                        Header = "JS",
                                        Action = () => MoveTo(TaskPlaningDataSet.TypeStanok.Js)
                                        , Tag = "access_mode_full_access"
                                    }
                                },
                                // {
                                //     "Gofra3",
                                //     new DataGridContextMenuItem()
                                //     {
                                //         Header = "Гофра 3",
                                //         Action = () => MoveTo(TaskPlaningDataSet.TypeStanok.Gofra3)
                                //         , Tag = "access_mode_full_access"
                                //     }
                                // },
                                // {
                                //     "Fosber",
                                //     new DataGridContextMenuItem()
                                //     {
                                //         Header = "Фосбер",
                                //         Action = () => MoveTo(TaskPlaningDataSet.TypeStanok.Fosber)
                                //         , Tag = "access_mode_full_access"
                                //     }
                                // },
                            }
                        }
                        );


                }



                //var header = LayoutHelper.FindElement(TaskGrid.GridControl, n => n is GridColumnHeader) as GridColumnHeader;

                //if(header != null)
                //{
                //    header.Height = 20;
                //}


                //TaskGrid.GridControl.SelectionMode = DevExpress.Xpf.Grid.MultiSelectMode.MultipleRow;
                //TaskGrid.GridView.CheckBoxSelectorColumnWidth = 20;
                //TaskGrid.GridView.ShowCheckBoxSelectorColumn = true;
            }
        }

        private void FindBreakResources()
        {
            TaskPlanningKsh.PlanDataSet.MakeAction(TaskPlaningDataSet.UserAction.ActionType.DiffResources, null, (int)CurrentMachineId);
        }

        private void Excel()
        {
            TaskGrid.ExportItemsExcel();
        }


        public void Edit()
        {
            int id = SelectedTaskItem.CheckGet(TaskPlaningDataSet.Dictionary.ProductionTaskId).ToInt();
            var cuttingManual = new ProductionTask();
            cuttingManual.BackTabName = "TaskPlanningKashira";
            cuttingManual.FactoryId = 2;
            cuttingManual.Edit(id);
        }

        public void ProcessPermissions(string roleCode = "[erp]prod_task_plan_kashira")
        {
            var userAccessMode = Central.Navigator.GetRoleLevel(roleCode);

            //UIUtil.SetFrameworkElementEnabledByTagAccessMode(this.Content as DependencyObject, Acl.AccessMode.ReadOnly);

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

            if (TaskGrid != null && TaskGrid.Menu != null && TaskGrid.Menu.Count > 0)
            {
                foreach (var manuItem in TaskGrid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }

            if(Panel is TaskPanel panel)
            {
                panel.ProcessPermissions(roleCode);
            }
        }

        private List<DataGridHelperColumnLabel> CreateLabels(string v)
        {
            var result =
            new List<DataGridHelperColumnLabel>()
            {
                // «!» на красном фоне – нагорячую,
                new DataGridHelperColumnLabel()
                {
                    Construct=()=>
                    {
                        var block=DataGridHelperColumnLabel.MakeElement("!", HColor.Red);
                        return block;
                    },
                    Update = (Dictionary<string, string> row) =>
                    {
                        var result = Visibility.Hidden;

                        if(row.CheckGet("HOT_IS").ToInt() > 0)
                        {
                            result=Visibility.Visible;
                        }

                        return result;
                    },
                },
                // «П» на жёлтом фоне – присутствие представителя,
                new DataGridHelperColumnLabel()
                {
                    Construct=()=>
                    {
                        var block=DataGridHelperColumnLabel.MakeElement("П", HColor.Yellow);
                        return block;
                    },
                    Update = (Dictionary<string, string> row) =>
                    {
                        var result = Visibility.Hidden;

                        if(row.CheckGet("REPRESENTATIVE_IS").ToInt() > 0)
                        {
                            result=Visibility.Visible;
                        }

                        return result;
                    },
                },
                //«н/к» и «н/шф» на синем фоне – нет клише и нет штанцформы.
                new DataGridHelperColumnLabel()
                {
                    Construct=()=>
                    {
                        var block=DataGridHelperColumnLabel.MakeElement("НК", HColor.Blue);
                        return block;
                    },
                    Update = (Dictionary<string, string> row) =>
                    {
                        var result = Visibility.Hidden;

                        if(row.CheckGet(TaskPlaningDataSet.Dictionary.NonclicheIs).ToInt() > 0)
                        {
                            result=Visibility.Visible;
                        }

                        return result;
                    },
                },
                new DataGridHelperColumnLabel()
                {
                    Construct=()=>
                    {
                        var block=DataGridHelperColumnLabel.MakeElement("Ȼ", HColor.Blue);
                        return block;
                    },
                    Update = (Dictionary<string, string> row) =>
                    {
                        var result = Visibility.Hidden;

                        if(row.CheckGet(TaskPlaningDataSet.Dictionary.ID2Count).ToInt() == 2)
                        {
                            result=Visibility.Visible;
                        }

                        return result;
                    },
                },
                new DataGridHelperColumnLabel()
                {
                    Construct=()=>
                    {
                        var block=DataGridHelperColumnLabel.MakeElement("НШ", HColor.Blue);
                        return block;
                    },
                    Update = (Dictionary<string, string> row) =>
                    {
                        var result = Visibility.Hidden;

                        if(row.CheckGet(TaskPlaningDataSet.Dictionary.NonshtanzIs).ToInt() > 0)
                        {
                            result=Visibility.Visible;
                        }

                        return result;
                    },
                },

                new DataGridHelperColumnLabel()
                {
                    Construct=()=>
                    {
                        var block=DataGridHelperColumnLabel.MakeElement("Т", HColor.Blue);
                        return block;
                    },
                    Update = (Dictionary<string, string> row) =>
                    {
                        var result = Visibility.Hidden;

                        if(row.CheckGet(TaskPlaningDataSet.Dictionary.Tandem).ToBool())
                        {
                            result=Visibility.Visible;
                        }

                        return result;
                    },
                },

                 new DataGridHelperColumnLabel()
                {
                    Construct=()=>
                    {
                        var block=DataGridHelperColumnLabel.MakeElement("ff", HColor.Blue);
                        return block;
                    },
                    Update = (Dictionary<string, string> row) =>
                    {
                        var result = Visibility.Hidden;

                        if(row.CheckGet(TaskPlaningDataSet.Dictionary.Fanfold).ToBool())
                        {
                            result=Visibility.Visible;
                        }

                        return result;
                    },
                },

            };

            return result;
        }

        private void MoveTo(TaskPlaningDataSet.TypeStanok stanokId)
        {
            var Items = GetSelectedItems();

            if(Items != null)
            {
                TaskPlanning.MachineTaskGrids[stanokId].MoveTasksInToCurrentPossition(Items, stanokId, CurrentMachineId);
            }
        }

        private void ChangeLayers()
        {
            var Items = GetSelectedItems();

            bool resume = true;

            if (Items.Any())
            {

                var d = new DialogWindow(
                           "Вы уверены, что хотите сменить валы на выделеных " + Items.ToList().Count() + " заданиях?",
                           "Смена валов!",
                           "Для смены нажмите \"Да\".",
                           DialogWindowButtons.YesNo);

                d.ShowDialog();

                if (d.DialogResult == true)
                {
                    TaskPlanningKsh.PlanDataSet.MakeAction(TaskPlaningDataSet.UserAction.ActionType.ChangeLayers, Items, (int)CurrentMachineId, (int)CurrentMachineId);
                }
            }
            else
            {
                var d = new DialogWindow(
                           "Для смены валов выюерите одно млм несколько заданий?",
                           "Смена валов!", ""
                           ,
                           DialogWindowButtons.OK);

                d.ShowDialog();
            }
        }

        /// <summary>
        /// Вызывается при смене текущей позиции в гриде
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GridControl_SelectedItemChanged(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.NewItem is DataRowView rowv)
            {
                if (rowv != null)
                {
                    if (rowv.Row != null)
                    {
                        var SelectedItem = rowv.Row.ToDictionary();
                        UpdateActions(SelectedItem);
                    }
                }
            }

            if (CurrentMachineId == TaskPlaningDataSet.TypeStanok.Unknow)
            {
                if (GetSelectedItems().Any())
                {
                    TaskGrid.Menu["move"].Enabled = true;
                }
                else
                {
                    TaskGrid.Menu["move"].Enabled = false;
                }
            }

        }

        /// <summary>
        /// Удаление со станка
        /// </summary>
        private void MenuDeleteFromMachine()
        {

            if(SelectedTaskItem!=null)
            {
                if (SelectedTaskItem.CheckGet(TaskPlaningDataSet.Dictionary.OnMachine).ToInt()!=0)
                {
                    var d = new DialogWindow(
                            "Вы уверены, что хотите удалить из очереди заказ "+SelectedTaskItem.CheckGet(TaskPlaningDataSet.Dictionary.Order)+"?",
                            "Удаление задачи!",
                            "Для удаления нажмите \"Да\".",
                            DialogWindowButtons.YesNo);

                    d.ShowDialog();

                    if (d.DialogResult == true)
                    {
                        var Items = new List<Dictionary<string, string>>()
                        {
                            SelectedTaskItem
                        };

                        TaskPlanningKsh.PlanDataSet.MakeAction(TaskPlaningDataSet.UserAction.ActionType.DeleteFromProd, Items, (int)CurrentMachineId, (int)CurrentMachineId);
                    }
                }
            }
        }

        /// <summary>
        /// проверка возможности перемещения заказа(ов)
        /// </summary>
        /// <param name="items">позиции</param>
        /// <param name="destination">id станка источника</param>
        /// <param name="source">id станка назнгачения</param>
        /// <returns></returns>
        public bool CanMoveTask(IEnumerable<Dictionary<string, string>> items, TaskPlaningDataSet.TypeStanok destination, TaskPlaningDataSet.TypeStanok source)
        {
            return TaskPlanning.CanMoveTask(items, destination, source);
        }

        #region DragAndDrop
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GridView_DragRecordOver(object sender, DevExpress.Xpf.Core.DragRecordOverEventArgs e)
        {
            if(e.Source!=e.OriginalSource) 
            { 
            }

            e.Effects = DragDropEffects.Copy;
        }

        private void GridView_CompleteRecordDragDrop(object sender, DevExpress.Xpf.Core.CompleteRecordDragDropEventArgs e)
        {
            e.Handled = true;
        }

        private void GridView_DropRecord(object sender, DevExpress.Xpf.Core.DropRecordEventArgs e)
        {
            DropDownFlag = true;
            e.Effects = DragDropEffects.Link;

            object data = e.Data.GetData(typeof(IEnumerable<Dictionary<string,string>>));

            if (data is List<Dictionary<string, string>> Items)
            {
                if (Items.Count > 0)
                {
                    bool shift = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

                    if(shift)
                    {
                        var item = Items[0];
                        var machineId = item.CheckGet(TaskPlaningDataSet.Dictionary.StanokId).ToInt();
                        Items = TaskPlanningKsh.PlanDataSet.Select(machineId, item);
                    }

                    if (e.TargetRecord is DataRowView rv)
                    {
                        var row = rv.Row;
                        int DropedRowIndex = row[TaskPlaningDataSet.Dictionary.RowNumber].ToInt();
                        DropedNumberId = row[TaskPlaningDataSet.Dictionary.NumberId].ToInt();

                        //for (int i = 0; i < Items.Count; i++)
                        //{
                        //= row[TaskPlaningDataSet.Dictionary.NumberId].ToInt();
                        //}


                        TaskPlaningDataSet.TypeStanok sourceMachineId = (TaskPlaningDataSet.TypeStanok)Items[0].CheckGet(TaskPlaningDataSet.Dictionary.StanokId).ToInt();// (int)CurrentMachineId;// row[TaskPlaningDataSet.Dictionary.StanokId].ToInt();

                        if (e.DropPosition == DevExpress.Xpf.Core.DropPosition.After)
                        {
                            if (CanMoveTask(Items, CurrentMachineId, sourceMachineId))
                            {
                                CurrentRowIndex = DropedRowIndex;
                                TaskPlanningKsh.PlanDataSet.MakeAction(TaskPlaningDataSet.UserAction.ActionType.Move, Items, (int)sourceMachineId, (int)CurrentMachineId, DropedRowIndex);
                            }
                            else
                            {
                                //resume = false;
                            }
                        }
                        else if (e.DropPosition == DevExpress.Xpf.Core.DropPosition.Before)
                        {
                            // перемещение внутри грида
                            if (CanMoveTask(Items, CurrentMachineId, sourceMachineId))
                            {
                                //TaskPlanning.PlanDataSet.MakeAction(TaskPlaningDataSet.UserAction.ActionType.Selected, Items);
                                CurrentRowIndex = DropedRowIndex;
                                TaskPlanningKsh.PlanDataSet.MakeAction(TaskPlaningDataSet.UserAction.ActionType.Move, Items, (int)sourceMachineId, (int)CurrentMachineId, DropedRowIndex-1);
                            }
                            else
                            {
                                //resume = false;
                            }
                        }

                    }
                    else
                    {
                        // сброс идет на пустой грид или в область в которой нет заданий
                        TaskPlaningDataSet.TypeStanok sourceMachineId = (TaskPlaningDataSet.TypeStanok)Items[0].CheckGet(TaskPlaningDataSet.Dictionary.StanokId).ToInt();

                        if (CanMoveTask(Items, CurrentMachineId, sourceMachineId))
                        {
                            TaskPlanningKsh.PlanDataSet.MakeAction(TaskPlaningDataSet.UserAction.ActionType.MoveInsertDown, Items, (int)sourceMachineId, (int)CurrentMachineId);
                        }

                    }
                }
            }

            // если не установить этот флаг будет unhandeled exception так как DataTadble не реализует IList интерфейс
            e.Handled = true;

            //Task.Run(async () =>
            //{
            //    await Task.Delay(1000);
            //    TaskPlanning.ChangeActiveTaskGrid(CurrentMachineId, null);
            //});
        }

        private void GridView_StartRecordDrag(object sender, DevExpress.Xpf.Core.StartRecordDragEventArgs e)
        {
            DropedNumberId = -1;
            var items = GetSelectedItems();

            DropDownFlag = true;

            if(TaskPlanning.CanMoveTask(items, _CurrentMachineId, _CurrentMachineId))
            {
                e.Data.SetData(typeof(IEnumerable<Dictionary<string, string>>), items);

                // для GridBox
                MovingObject movingObject = new MovingObject();
                movingObject.SourceName = _CurrentMachineId.ToString();
                movingObject.Data = items.FirstOrDefault();

                e.Data.SetData(typeof(MovingObject), movingObject);

                e.AllowDrag = true;
                e.Handled = true;
            }
            else
            {
                e.AllowDrag = false;
                e.Handled = true;
            }
        }

        #endregion

        /// <summary>
        /// действие при смене текущаго задания
        /// </summary>
        /// <param name="selectedItem"></param>
        private void UpdateActions(Dictionary<string, string> selectedItem)
        {
            bool changeFlag = true;
            bool isFocused = BorderControl.BorderBrush != null;

            // обновление транспорт id

            Panel?.Update(selectedItem);

            if (SelectedTaskItem!=null)
            {
                if (SelectedTaskItem[TaskPlaningDataSet.Dictionary.RowNumber] == selectedItem[TaskPlaningDataSet.Dictionary.RowNumber])
                {
                    changeFlag = false;
                }
            }

            SelectedTaskItem = selectedItem;

            CurrentRowIndex = SelectedTaskItem[TaskPlaningDataSet.Dictionary.RowNumber].ToInt();

            if (changeFlag)
            {
                bool shift = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

                if (!shift)
                {
                    LastSelectedItemFroMultiSelect = selectedItem;

                    if (Keyboard.IsKeyDown(Key.LeftCtrl))
                    {
                        if (selectedItem.CheckGet(TaskPlaningDataSet.Dictionary.OnMachine).ToInt() == 0)
                        {
                            List<Dictionary<string, string>> items = new List<Dictionary<string, string>>();
                            items.Add(selectedItem);

                            if (selectedItem.CheckGet(TaskPlaningDataSet.Dictionary.SelectedColumn).ToInt() == 0)
                            {
                                TaskPlanningKsh.PlanDataSet.MakeAction(TaskPlaningDataSet.UserAction.ActionType.Selected, items);
                                items.ForEach(x => SelectRow(x, true));
                               
                            }
                            else
                            {
                                TaskPlanningKsh.PlanDataSet.MakeAction(TaskPlaningDataSet.UserAction.ActionType.DeSelected, items);
                                items.ForEach(x => SelectRow(x, false));
                            }

                            // Вариант с перезагрузкой всего грида не удачен, нужно сделать выделение вручную
                            //LoadItems();
                        }
                    }
                }
                else
                {
                    // реализация выделения с шифтом
                    if (LastSelectedItemFroMultiSelect != null)
                    {
                        int minimalRowIndex = SelectedTaskItem.CheckGet(TaskPlaningDataSet.Dictionary.RowNumber).ToInt();
                        int maximalRowIndex = LastSelectedItemFroMultiSelect.CheckGet(TaskPlaningDataSet.Dictionary.RowNumber).ToInt();

                        if (minimalRowIndex != maximalRowIndex)
                        {
                            if (minimalRowIndex > maximalRowIndex)
                            {
                                (minimalRowIndex, maximalRowIndex) = (maximalRowIndex, minimalRowIndex);
                            }

                            bool resume = false;

                            // отмечаем с учетом фильтра
                            var items = TaskGrid.GridControl.DataController.GetAllFilteredAndSortedRows();
                            
                            foreach(DataRowView itemv in items)
                            {
                                var item = itemv.Row;

                                if (item.CheckGet(TaskPlaningDataSet.Dictionary.RowNumber).ToInt() >= minimalRowIndex &&
                                    item.CheckGet(TaskPlaningDataSet.Dictionary.RowNumber).ToInt() <= maximalRowIndex)
                                {
                                    if (item.CheckGet(TaskPlaningDataSet.Dictionary.SelectedColumn).ToInt() == 0)
                                    {
                                        item[TaskPlaningDataSet.Dictionary.SelectedColumn] = true;

                                        //selected.Add(item);
                                    }
                                }
                                else
                                {
                                    if (item.CheckGet(TaskPlaningDataSet.Dictionary.SelectedColumn).ToInt() != 0)
                                    {
                                        item[TaskPlaningDataSet.Dictionary.SelectedColumn] = false;

                                        //unselected.Add(item);
                                    }
                                }
                            }
                            
                        }
                    }
                    else
                    {
                        LastSelectedItemFroMultiSelect = SelectedTaskItem;
                    }
                }


                TaskPlanning.ChangeActiveTaskGrid(CurrentMachineId, SelectedTaskItem);
            }

            // prepare menu
            if(selectedItem!=null)
            {
                if (selectedItem.CheckGet(TaskPlaningDataSet.Dictionary.OnMachine).ToInt() == 0)
                {
                    TaskGrid.Menu["del"].Enabled = true;
                    TaskGrid.Menu["up"].Enabled = true;
                    TaskGrid.Menu["down"].Enabled = true;
                    TaskGrid.Menu["delFromMachine"].Enabled = false;
                }
                else
                {
                    TaskGrid.Menu["del"].Enabled = false;
                    TaskGrid.Menu["up"].Enabled = false;
                    TaskGrid.Menu["down"].Enabled = false;
                    TaskGrid.Menu["delFromMachine"].Enabled = true;
                }

                
                bool enableChangeLayers = _CurrentMachineId != 0 ;

                if (enableChangeLayers)
                {
                    if (selectedItem.CheckGet(TaskPlaningDataSet.Dictionary.OnMachine).ToInt() == 0)
                    {
                        if ((selectedItem.CheckGet(TaskPlaningDataSet.Dictionary.Layer2) == string.Empty &&
                             selectedItem.CheckGet(TaskPlaningDataSet.Dictionary.Layer3) == string.Empty) ||
                            (selectedItem.CheckGet(TaskPlaningDataSet.Dictionary.Layer4) == string.Empty &&
                             selectedItem.CheckGet(TaskPlaningDataSet.Dictionary.Layer5) == string.Empty))
                        {
                            enableChangeLayers = true;
                        }
                        else
                        {
                            enableChangeLayers = false;
                        }
                    }
                }

                TaskGrid.Menu["changeLayers"].Enabled = enableChangeLayers;


                // генерация меню изменения формата
                bool canChangeFormat = CurrentMachineId != TaskPlaningDataSet.TypeStanok.Unknow;

                if (canChangeFormat)
                {
                    var formats = new Dictionary<string, DataGridContextMenuItem>();
                    int currentFormat = selectedItem.CheckGet(TaskPlaningDataSet.Dictionary.Format).ToInt();
                    // int maxFormat = CurrentMachineId == TaskPlaningDataSet.TypeStanok.Fosber ? 2800 : 2500;

                    // for (int i = currentFormat + 100; i <= maxFormat; i += 100)
                    // {
                    //     int j = i;
                    //
                    //     formats.Add($"up{i}",
                    //         new DataGridContextMenuItem()
                    //         {
                    //             Header = $"{i}",
                    //             Action = () => { UpFormat(j); }
                    //         }
                    //     );
                    // }

                    TaskGrid.Menu["changeFormat"].Items = formats;
                }
                else
                {
                    TaskGrid.Menu["changeFormat"].Enabled = false;
                }
            }
        }

        private void UpFormat(int newFormat)
        {
            if (SelectedTaskItem != null)
            {
                var Items = new List<Dictionary<string, string>>
                {
                    SelectedTaskItem
                };

                TaskPlanningKsh.PlanDataSet.MakeAction(TaskPlaningDataSet.UserAction.ActionType.ChangeFormat, Items, (int)_CurrentMachineId, (int)_CurrentMachineId, newFormat);
            }
        }

        private void SelectRow(Dictionary<string, string> item, bool v)
        {
            var selected = GridTable.AsEnumerable()
                    .Where(x => x[TaskPlaningDataSet.Dictionary.ProductionTaskId].ToInt() == item[TaskPlaningDataSet.Dictionary.ProductionTaskId].ToInt())
                    .FirstOrDefault();

            if(selected != null )
            {
                selected[TaskPlaningDataSet.Dictionary.SelectedColumn] = v;
            }
        }

        private async void InitDefaults()
        {
            if (_CurrentMachineId == TaskPlaningDataSet.TypeStanok.Unknow)
            {
                Panel = new TaskPanelFreeOrders();
                Panel.OnFindFormat += Panel_OnFindFormat;
                Panel.OnHoursFilter += Panel_OnHoursFilter;
            }
            else
            {
                Panel = new TaskPanel(_CurrentMachineId);
                (Panel as TaskPanel).OnFullScreen += FullScreen;
            }

            Toolbar.Children.Add(Panel as UserControl);

            var p = new Dictionary<string, string>()
            {
                { "ID_ST", ((int)_CurrentMachineId).ToString() }
            };

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "CorrugatorMachineOperator");
            q.Request.SetParam("Action", "GetPlaceName");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout * 5;
            q.Request.Attempts = 4;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "PLACE");
                    if (ds.Items.Count > 0)
                    {
                        CurrentPlaceName = ds.Items[0].CheckGet("NAME");
                        Panel.Kpd = ds.Items[0].CheckGet("KPD_PLAN").ToInt().ToString();
                        Panel.NameStanok = CurrentPlaceName;
                    }
                }
            }
        }

        private void FullScreen()
        {
            TaskPlanning.FullScreen(this);
        }

        private void Panel_OnFindFormat(int format)
        {
            // фильтр по формату

            ClearAllCheckBoxes();
            MaxFormat = format;
            CalculateErrors();


        }

        private void Panel_OnHoursFilter(int format)
        {
            // необходимо посчитать фильтр для даты
            if (format != 0)
            {
                var sdate = DateTime.Now.AddHours(format).ToString(TaskPlaningDataSet.DateTimeFormat); ;

                TaskGrid.GridControl.FilterString = $"([{TaskPlaningDataSet.Dictionary.StartBeforeTime}] < '{sdate}')";
            }
            else
            {
                // сброс фильтра даты

                TaskGrid.GridControl.FilterString = string.Empty;
            }

        }


        private void Grid_FilterChanged(object sender, RoutedEventArgs e)
        {
            //throw new NotImplementedException();

            if(TaskGrid.GridControl.FilterString == string.Empty)
            {
                MaxFormat = 0;
                //format = 0;
            }
        }

        private string LayerFromRow(object o)
        {
            string res = o==null ? string.Empty : o.ToString().TrimEnd(' ').TrimStart(' ');

            if (res == string.Empty) res = " ";

            return res;
        }

        private string GetBlockName(DataRow item)
        {
            string blockName = LayerFromRow(TaskPlaningDataSet.Dictionary.ProfilName) +
                               LayerFromRow(item[TaskPlaningDataSet.Dictionary.Format].ToString().ToInt().ToString()) +
                               LayerFromRow(item[TaskPlaningDataSet.Dictionary.Layer1]) +
                               LayerFromRow(item[TaskPlaningDataSet.Dictionary.Layer2]) +
                               LayerFromRow(item[TaskPlaningDataSet.Dictionary.Layer3]) +
                               LayerFromRow(item[TaskPlaningDataSet.Dictionary.Layer4]) +
                               LayerFromRow(item[TaskPlaningDataSet.Dictionary.Layer5]) ;

            return blockName;
        }

        private string GetBlockName(Dictionary<string,string> item)
        {
            if(item[TaskPlaningDataSet.Dictionary.Format]==null) return string.Empty;

            string blockName = LayerFromRow(item[TaskPlaningDataSet.Dictionary.ProfilName]) +
                               LayerFromRow(item[TaskPlaningDataSet.Dictionary.Format].ToString().ToInt().ToString()) +
                               LayerFromRow(item[TaskPlaningDataSet.Dictionary.Layer1]) +
                               LayerFromRow(item[TaskPlaningDataSet.Dictionary.Layer2]) +
                               LayerFromRow(item[TaskPlaningDataSet.Dictionary.Layer3]) +
                               LayerFromRow(item[TaskPlaningDataSet.Dictionary.Layer4]) +
                               LayerFromRow(item[TaskPlaningDataSet.Dictionary.Layer5]);

            return blockName.TrimEnd(' ');
        }

        /// <summary>
        /// Расчет ошибок
        /// </summary>
        private void CalculateErrors()
        {
            int minBlockLength = Settings.MinimalBlockSize;
            double CorrugatorMaxTime = 30;

            // if (_CurrentMachineId == TaskPlaningDataSet.TypeStanok.Fosber) CorrugatorMaxTime = 40;
            //
            // {
            //     // необходиом посчитать общую длину блоков
            //     var filteredItems = TaskGrid.GridControl.DataController.GetAllFilteredAndSortedRows();
            //     var itemsCount = filteredItems.Count;
            //
            //     Dictionary<string, int> blockLength = new Dictionary<string, int>();
            //     Dictionary<string, int> blockLengthCount = new Dictionary<string, int>();
            //     
            //
            //     for (int i = 0; i < itemsCount; i++)
            //     {
            //         var itemv = filteredItems[i] as DataRowView;
            //         var item = itemv.Row;
            //         string blockName = GetBlockName(item);
            //
            //         var length = item[TaskPlaningDataSet.Dictionary.Length].ToInt();
            //
            //         if (blockLength.ContainsKey(blockName))
            //         {
            //             blockLength[blockName] += length;
            //             blockLengthCount[blockName]++;
            //         }
            //         else
            //         {
            //             blockLength.Add(blockName, length);
            //             blockLengthCount.Add(blockName, 1);
            //         }
            //     }
            //
            //     for (int i = 0; i < itemsCount; i++)
            //     {
            //         var itemv = filteredItems[i] as DataRowView;
            //         var item = itemv.Row;
            //         string blockName = GetBlockName(item);
            //
            //         item[TaskPlaningDataSet.Dictionary.BlockLength] = blockLength[blockName];
            //     }
            // }

            /////////////////////////////////////////////////////////////////////////////////////////////////

            if (_CurrentMachineId == TaskPlaningDataSet.TypeStanok.Unknow)
            {
                if (GridTable != null)
                {
                    //если задан фильтр по коротким блокам, то отфильтруем по блокам, возможно эту проверку стоит задать выше
                    if (MaxFormat == 0)
                    {
                        //TaskGrid.Items = TaskGrid.GridItems;
                    }
                    else
                    {
                        if (TaskGrid.GridControl.FilterString != string.Empty)
                        {
                            if (!TaskGrid.GridControl.FilterString.Contains(TaskPlaningDataSet.Dictionary.BlockLength))
                            {
                                TaskGrid.GridControl.FilterString += $" AND ([{TaskPlaningDataSet.Dictionary.BlockLength}] < {MaxFormat})";
                            }
                        }
                        else
                        {
                            TaskGrid.GridControl.FilterString = $"([{TaskPlaningDataSet.Dictionary.BlockLength}] < {MaxFormat})";
                        }
                    }
                }

            }
            else
            {
                if (GridTable != null)
                {
                    DataRow prevItem = null;

                    List<DataRow> items = GridTable.AsEnumerable().ToList();
                    TimeSpan ProfileTime = TimeSpan.FromSeconds(0);

                    string Separator = ", ";

                    Dictionary<string, int> keyList = new Dictionary<string, int>();

                    var errors = new StringBuilder();
                    var itemsCount = GridTable.Rows.Count;
                    for (int i = 0; i < itemsCount; i++)
                    {
                        errors.Clear();

                        var curItem = items[i];
                        if (i > 0)
                        {
                            prevItem = items[i - 1];

                            string currentKey = TaskPlaningDataSet.GetKeyRow(curItem);
                            string prevKey = TaskPlaningDataSet.GetKeyRow(prevItem);

                            if(currentKey != prevKey)
                            {
                                // предыдущий формат необходимо добавить в массив, а новый проверить что бы такого не было ранее

                                if (!keyList.ContainsKey(prevKey))
                                {
                                    keyList.Add(prevKey, prevItem.CheckGet(TaskPlaningDataSet.Dictionary.RowNumber).ToInt());
                                }

                                if(keyList.ContainsKey(currentKey))
                                {
                                    errors.Append($"Уже встречалось {keyList[currentKey]}");
                                }
                            }
                        }

                        if(i>2)
                        {
                            int selfCutMinLength = 500;
                            // Необходимо с текущего момента выполнять условия по нежелательности 3х подряд несамокройных заданий,
                            // каждое из которых менее 500 метров в блоке ВС,ЕВ . Т.е.если стоят 3 и более несамокройных заданий ВС ,
                            // ЕВ каждое менее 500м, то программа должна сообщить об ошибке « 3 и более несамокройных заданий < 500м» 

                            int selfcut = 2;

                            if (items[i].CheckGet(TaskPlaningDataSet.Dictionary.ID2Count).ToInt()== selfcut)
                            {
                                if (items[i].CheckGet(TaskPlaningDataSet.Dictionary.Length).ToInt() < selfCutMinLength && (items[i].CheckGet(TaskPlaningDataSet.Dictionary.ProfilName)== "ВС" || items[i].CheckGet(TaskPlaningDataSet.Dictionary.ProfilName) == "ЕВ"))
                                {
                                    if (items[i-1].CheckGet(TaskPlaningDataSet.Dictionary.ID2Count).ToInt() == selfcut)
                                    {
                                        if (items[i-1].CheckGet(TaskPlaningDataSet.Dictionary.Length).ToInt() < selfCutMinLength && (items[i-1].CheckGet(TaskPlaningDataSet.Dictionary.ProfilName) == "ВС" || items[i-1].CheckGet(TaskPlaningDataSet.Dictionary.ProfilName) == "ЕВ"))
                                        {
                                            if (items[i - 2].CheckGet(TaskPlaningDataSet.Dictionary.ID2Count).ToInt() == selfcut)
                                            {
                                                if (items[i - 2].CheckGet(TaskPlaningDataSet.Dictionary.Length).ToInt() < selfCutMinLength && (items[i - 2].CheckGet(TaskPlaningDataSet.Dictionary.ProfilName) == "ВС" || items[i - 2].CheckGet(TaskPlaningDataSet.Dictionary.ProfilName) == "ЕВ"))
                                                {
                                                    errors.Append($"3 и более несамокройных заданий < 500м");
                                                }
                                            }
                                        }
                                    }
                                }

                            }
                        }

                        int layerCount =  (string.IsNullOrEmpty(curItem[TaskPlaningDataSet.Dictionary.Layer2].ToString()) ? 0 : 1) +
                                             (string.IsNullOrEmpty(curItem[TaskPlaningDataSet.Dictionary.Layer3].ToString()) ? 0 : 1) +
                                             (string.IsNullOrEmpty(curItem[TaskPlaningDataSet.Dictionary.Layer4].ToString()) ? 0 : 1) +
                                             (string.IsNullOrEmpty(curItem[TaskPlaningDataSet.Dictionary.Layer5].ToString()) ? 0 : 1) +
                                             (string.IsNullOrEmpty(curItem[TaskPlaningDataSet.Dictionary.Layer1].ToString()) ? 0 : 1);

                        // проверка на простой
                        if (curItem[TaskPlaningDataSet.Dictionary.DropdownId].ToInt() > 0)
                        {
                            errors.Clear();
                            errors.Append("Простой");
                        }
                        else
                        {
                            var blockLength = curItem.CheckGet(TaskPlaningDataSet.Dictionary.BlockLength).ToInt();
                            var currentLength = curItem.CheckGet(TaskPlaningDataSet.Dictionary.Length).ToInt();

                            string prevRollName = string.Empty;

                            #region // 2. Некорректные переходы на другой профиль:
                            {
                                var currentRollName = curItem.CheckGet(TaskPlaningDataSet.Dictionary.VAL);
                                if(prevRollName!=null)
                                {
                                    prevRollName = prevItem.CheckGet(TaskPlaningDataSet.Dictionary.VAL);
                                }

                                if (!string.IsNullOrEmpty(currentRollName) && prevItem != null)
                                {
                                    if (!string.IsNullOrEmpty(prevRollName))
                                    {
                                        if (currentRollName != prevRollName)
                                        {
                                            if (Errors.IsWrongLayerChanges(prevRollName, currentRollName))
                                            {
                                                errors.Append($"Смена профиля {prevRollName}-{currentRollName}{Separator}");
                                            }
                                        }
                                    }
                                }
                            }
                            #endregion

                            #region // 3. Минимальное время блока на одном <профиле> 30 минут для BHS1/2, 40 минут для Фосбер, поэтому ошибка должна быть для BHS1/2 - «Блок<30мин», для Фосбера «Блок<40мин»
                            {
                                var currentRollName = curItem.CheckGet(TaskPlaningDataSet.Dictionary.ProfilName);
                                double TotalProfileTime = 0;

                                if (!string.IsNullOrEmpty(currentRollName))
                                {
                                    var time = curItem.CheckGet(TaskPlaningDataSet.Dictionary.CalculatedDuration).ToDouble();
                                    var ts = TimeSpan.FromMinutes(time);
                                    // это один и тот же профиль
                                    ProfileTime = ProfileTime.Add(ts);

                                    if (i < itemsCount - 1)
                                    {
                                        var nextItem = items[i + 1];

                                        var nextRollName = nextItem.CheckGet(TaskPlaningDataSet.Dictionary.ProfilName);
                                        if (!string.IsNullOrEmpty(nextRollName))
                                        {
                                            // профиль сменится
                                            if (currentRollName != nextRollName)
                                            {
                                                TotalProfileTime = ProfileTime.TotalMinutes;
                                            }
                                        }
                                        else
                                        {// следующая позиция простой
                                            TotalProfileTime = ProfileTime.TotalMinutes;
                                        }
                                    }
                                    else
                                    {
                                        // это последняя запись
                                        TotalProfileTime = ProfileTime.TotalMinutes;
                                    }

                                    
                                }
                                else
                                {
                                    // простой
                                    TotalProfileTime = ProfileTime.TotalMinutes;
                                }

                                if (TotalProfileTime != 0)
                                {
                                    if (TotalProfileTime < CorrugatorMaxTime)
                                    {
                                        errors.Append($"Блок<{CorrugatorMaxTime}мин{Separator}");
                                    }

                                    ProfileTime = TimeSpan.FromSeconds(0);
                                }

                            }
                            #endregion

                            if (blockLength>0 && blockLength< minBlockLength)
                            {
                                errors.Append($"Длина блока < {minBlockLength}{Separator}");
                            }

                            if (currentLength < TaskSettings.FirstLastBlockOrderMinimalLength)
                            {
                                if (curItem.CheckGet(TaskPlaningDataSet.Dictionary.DropdownId).ToInt() == 0)
                                {
                                    // 17. Задания длиной менее 300-400 метров не должны стоять последними в блоке по качеству.
                                    // Так же не рекомендовано их ставить первыми в блоке по качеству (ставим только при отсутствии альтернативы)
                                    // Если задания стоят в неправильном месте, то должна выдаваться ошибка
                                    bool isLast = false;
                                    bool isFirst = false;

                                    // проверим снизу другое качестов? если другое, значит данные заказ последний по качеству в блоке
                                    if (i < GridTable.Rows.Count - 1)
                                    {
                                        isLast = !TaskPlaningDataSet.IsEqualQuality(items[i], items[i + 1]);
                                    }

                                    // проверим сверху другое качество? елси так, то значит данный заказ первый в блоке по качеству.
                                    if (i > 1)
                                    {
                                        isFirst = !TaskPlaningDataSet.IsEqualQuality(items[i], items[i - 1]);
                                    }

                                    // если этот заказ и первый и последний, тогда это не ошибка
                                    if (!(isLast&&isFirst))
                                    {
                                        if (isLast)
                                        {
                                            //errors.Append("Задания длиной менее 300-400 метров не должны стоять последними в блоке по качеству\n");
                                            errors.Append($"L<400 последнее{Separator}");
                                        }
                                        else if (isFirst)
                                        {
                                            //errors.Append("Задания длиной менее 300-400 метров не должны стоять первым в блоке по качеству\n");
                                            errors.Append($"L<400 первое{Separator}");
                                        }
                                    }
                                }
                            }


                            if (i > 0)
                            {
                                // При переходе с профиля на профиль нельзя начинать с задания у которого на [b]гофрослое[/b] стоит сырье более более 125 грамм / м2
                                // сменился профиль, необходимо проверить плотность
                                if (items[i].CheckGet("PROFIL_NAME") != items[i - 1].CheckGet("PROFIL_NAME"))
                                {
                                    bool error =
                                        items[i].CheckGet("LAYER2_MARK").All(char.IsDigit).ToInt() > 125 ||
                                        items[i].CheckGet("LAYER4_MARK").All(char.IsDigit).ToInt() > 125;

                                    if (error)
                                    {
                                        errors.Append($"При переходе с профиля на профиль нельзя начинать с задания у которого на гофрослое стоит сырье более 125 грамм/м2{Separator}");
                                    }
                                }

                                //1. При переходе с одного профиля на другой производство нового профиля должно начинаться с максимального формата и заканчиваться минимальным по убыванию;
                                if (items[i].CheckGet("PROFIL_NAME") == items[i - 1].CheckGet("PROFIL_NAME")
                                    && items[i].CheckGet("WIDTH").ToInt() > items[i - 1].CheckGet("WIDTH").ToInt())
                                {
                                    //errors.Append("Производство профиля должно начинаться с максимального формата и заканчиваться минимальным по убыванию\n");
                                    errors.Append($"Смена формата{Separator}");
                                }


                                // переход другое количество слоев
                                if (items[i].CheckGet("PROFIL_NAME").Length != items[i - 1].CheckGet("PROFIL_NAME").Length)
                                {
                                    // считаем сумму плотности по всем слоям, будем сравнивать по ней
                                    var itemsDensityForSameProfilWidth = items
                                        .Skip(i)
                                        .TakeWhile(item => item.CheckGet("WIDTH") == items[i].CheckGet("WIDTH"))
                                        .Select(item => item.CheckGet("LAYER1_MARK").All(char.IsDigit).ToInt()
                                                      + item.CheckGet("LAYER2_MARK").All(char.IsDigit).ToInt()
                                                      + item.CheckGet("LAYER3_MARK").All(char.IsDigit).ToInt()
                                                      + item.CheckGet("LAYER4_MARK").All(char.IsDigit).ToInt()
                                                      + item.CheckGet("LAYER5_MARK").All(char.IsDigit).ToInt());

                                    //если переход осуществляется с трехслойного на пятислойный картон, то первое задание в блоке пятислойки должно быть на сырье с минимальной плотностью;
                                    if (items[i - 1].CheckGet("PROFIL_NAME").Length < items[i].CheckGet("PROFIL_NAME").Length)
                                    {
                                        if (itemsDensityForSameProfilWidth.First() != itemsDensityForSameProfilWidth.Min())
                                        {
                                            errors.Append($"При переходе с трехслойного на пятислойный картон первое задание в блоке пятислойки должно быть на сырье с минимальной плотностью{Separator}");
                                        }
                                    }
                                    //если переход осуществляется с пятислойного на трехслойный картон, то первое задание в блоке трехслойки должно быть на сырье с максимальной плотностью;
                                    if (items[i - 1].CheckGet("PROFIL_NAME").Length < items[i].CheckGet("PROFIL_NAME").Length)
                                    {
                                        if (itemsDensityForSameProfilWidth.First() != itemsDensityForSameProfilWidth.Max())
                                        {
                                            errors.Append($"При переходе с пятислойного на трехслойный картон первое задание в блоке трехслойки должно быть на сырье с максимальной плотностью{Separator}");
                                        }
                                    }
                                }

                                //3. Блок заданий на одном сырье должен быть не менее 1000 м/пог.
                                for (int layerNum = 1; layerNum <= 5; layerNum++)
                                {
                                    // блок на сырье только начался
                                    if (items[i - 1].CheckGet($"LAYER{layerNum}_MARK") != items[i].CheckGet($"LAYER{layerNum}_MARK"))
                                    {
                                        var itemsSameLayer = items
                                            .Skip(i)
                                            .TakeWhile(item => item.CheckGet($"LAYER{layerNum}_MARK") == items[i].CheckGet($"LAYER{layerNum}_MARK"));
                                        if (itemsSameLayer.Sum(item => item.CheckGet($"LENGTH").ToInt()) < 1000)
                                        {
                                            //errors.Append($"Блок заданий на одном сырье должен быть не менее 1000 метров (слой {layerNum})\n");
                                            errors.Append($"Блок <1000м (слой {layerNum}){Separator}");
                                        }
                                    }
                                }

                                //4. Задание, перед/после тандема должно быть длиной не менее 400 метров.
                                if (items[i].CheckGet("TANDEM") == "1")
                                {
                                    if (items[i - 1].CheckGet($"LENGTH").ToInt() < 400)
                                    {
                                        errors.Append($"Задание перед тандемом должно быть длиной не менее 400 метров{Separator}");
                                    }
                                    if (i != items.Count - 1
                                        && items[i + 1].CheckGet($"LENGTH").ToInt() < 400)
                                    {
                                        errors.Append($"Задание после тандема должно быть длиной не менее 400 метров{Separator}");
                                    }
                                }

                                //5. Задание, после которого будет фэнфолд должно быть длиной не менее 300 метров.
                                if (items[i].CheckGet("FANFOLD") == "1")
                                {
                                    if (items[i - 1].CheckGet($"LENGTH").ToInt() < 300)
                                    {
                                        errors.Append($"Задание перед фэнфолдом должно быть длиной не менее 300 метров{Separator}");
                                    }
                                }

                                //6.При попадании на Фосбер по заданиям должна всплывать ошибка, если Обрезь пз ≤ 15 мм (если 0, не подсвечивается); Обрезь пз для 3 слойного ≥ 240 мм.Обрезь пз для 5 слойного ≥ 200 мм
                                // if (_CurrentMachineId == TaskPlaningDataSet.TypeStanok.Fosber)
                                // {
                                //     int trim = items[i].CheckGet($"TRIM").ToInt();
                                //     if (trim > 0 && trim < 16)
                                //     {
                                //         errors.Append($"Обрезь на фосбере должна быть > 15 мм, либо 0 мм{Separator}");
                                //     }
                                //     // трёхслойка
                                //     if (trim >= 240
                                //         && items[i].CheckGet("PROFIL_NAME").Length == 1)
                                //     {
                                //         errors.Append($"Обрезь на трёхслойке > 240мм{Separator}");
                                //     }
                                //     // пятислойка
                                //     else if (trim >= 200
                                //         && items[i].CheckGet("PROFIL_NAME").Length == 2)
                                //     {
                                //         errors.Append($"Обрезь на пятислойке > 200мм{Separator}");
                                //     }
                                // }

                                //8. Блоки по качеству должны быть не менее 400 метров
                                // Эта надпись неактуальна- убери ее , пожалуйста. Пусть для трехслойки пишет если блок менее  1000м. а для пятислойки- если менее 1500м
                                //if (items[i].CheckGet("QID") != items[i - 1].CheckGet("QID"))
                                //{
                                //    var itemsSameLayer = items
                                //            .Skip(i)
                                //            .TakeWhile(item => item.CheckGet($"QID") == items[i].CheckGet("QID"));
                                //    if (itemsSameLayer.Sum(item => item.CheckGet($"LENGTH").ToInt()) < 400)
                                //    {
                                //        //errors.Append("Блок заданий одного качества должен быть не менее 400 метров\n");
                                //        errors.Append($"Блок <400м{Separator}");
                                //    }
                                //}


                                if (items[i].CheckGet("QID") != items[i - 1].CheckGet("QID"))
                                {
                                    var itemsSameLayer = items
                                            .Skip(i)
                                            .TakeWhile(item => item.CheckGet($"QID") == items[i].CheckGet("QID"));

                                    if (layerCount == 5)
                                    {
                                        if (itemsSameLayer.Sum(item => item.CheckGet($"LENGTH").ToInt()) < 1500)
                                        {
                                            //errors.Append("Блок заданий одного качества должен быть не менее 400 метров\n");
                                            errors.Append($"Блок <1500м{Separator}");
                                        }
                                    }
                                    else if(layerCount==3)
                                    {
                                        if (itemsSameLayer.Sum(item => item.CheckGet($"LENGTH").ToInt()) < 1000)
                                        {
                                            //errors.Append("Блок заданий одного качества должен быть не менее 400 метров\n");
                                            errors.Append($"Блок <1000м{Separator}");
                                        }
                                    }
                                }
                            }

                            if (items[i].CheckGet("CHECK_QID").ToInt() == 1)
                            {
                                //errors.Append("Качество встречается первый раз\n");
                                errors.Append($"Новое качество{Separator}");
                            }

                            if (items[i].CheckGet("FANFOLD").ToInt() == 1)
                            {
                                errors.Append($"{items[i].CheckGet("NKLISHE")}{Separator}");
                            }
                        }

                        //if (errors.Length > 0)
                        {
                            items[i].CheckAdd("ERRORS", errors.ToString().TrimEnd(Separator));
                        }

                        prevItem = curItem;
                    }
                }
            }
        }

        /// <summary>
        /// Снятие галочек со всех чекбоксов грида очереди заданий
        /// </summary>
        public void ClearAllCheckBoxes()
        {
            if (!DropDownFlag)
            {
                var items = GetSelectedItems();

                if (items.Any())
                {
                    TaskPlanningKsh.PlanDataSet.MakeAction(TaskPlaningDataSet.UserAction.ActionType.DeSelected, items);

                    if (TaskGrid.GridControl.ItemsSource is DataTable table)
                    {
                        foreach (DataRow row in table.Rows)
                        {
                            if (row[TaskPlaningDataSet.Dictionary.SelectedColumn].ToInt() != 0)
                            {
                                row[TaskPlaningDataSet.Dictionary.SelectedColumn] = false;
                            }
                        }
                    }
                }
            }
            else
            {
                DropDownFlag = false;
            }

            LastSelectedItemFroMultiSelect = SelectedTaskItem;

            //SelectedTaskItem;
        }

        public void Delete()
        {
            if(_CurrentMachineId!= TaskPlaningDataSet.TypeStanok.Unknow)
            {
                var selectedItems = GetSelectedItems().ToList();

                if(selectedItems.Any())
                {
                    
                }
                else
                {
                    if(TaskGrid.SelectedItem!=null)
                    {
                        selectedItems = new List<Dictionary<string, string>>()
                        {
                            TaskGrid.SelectedItem
                        };
                    }
                }

                if (selectedItems.Any())
                {
                    CurrentRowIndex = selectedItems.First().CheckGet(TaskPlaningDataSet.Dictionary.RowNumber).ToInt();

                    if (CanMoveTask(selectedItems, TaskPlaningDataSet.TypeStanok.Unknow, CurrentMachineId))
                    {
                        TaskPlanningKsh.PlanDataSet.MakeAction(TaskPlaningDataSet.UserAction.ActionType.MoveInsertDown, selectedItems, (int)CurrentMachineId, (int)TaskPlaningDataSet.TypeStanok.Unknow);
                    }
                }
            }
        }

        public void Destruct()
        {
            TaskLists.Remove(this);
            TaskGrid.Destruct();
        }

        public void EnableEdit(bool enable)
        {
            if (!enable)
            {
                ShowSplash();
            }
            else
            {
                HideSplash();
            }
        }

        public double GetHours()
        {
            return Hours;
        }

        public IEnumerable<Dictionary<string, string>> GetSelectedItems()
        {
            try
            {
                List<Dictionary<string,string>> list = new List<Dictionary<string,string>>();

                if (TaskGrid.GridControl.ItemsSource is DataTable table)
                {
                    foreach (DataRow row in table.Rows)
                    {
                        if (row[TaskPlaningDataSet.Dictionary.SelectedColumn].ToInt() != 0)
                        {
                            list.Add(row.ToDictionary());
                        }
                    }
                }

                return list;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="Items"></param>
        public void CalculateStartDates(List<Dictionary<string, string>> Items)
        {
            double kpd = 100;

            //CurrentProgress = 0;
            {
                if (double.TryParse(Panel.Kpd.Replace('.', ','), out kpd))
                {
                    TaskPlaningDataSet.CalculateStartDates(Items, kpd, CurrentProdTask.ToInt(), CurrentProgress, (int)_CurrentMachineId);

                    // проверить есть ли простои которые нужно выставить в правильное место?
                    if (EnableSortIdles)
                    {
                        var listOfDownTime = Items.Where(x => x.CheckGet(TaskPlaningDataSet.Dictionary.DropdownId).ToInt() > 0);
                        if (listOfDownTime.Any())
                        {
                            bool sorted = false;

                            foreach (var downTime in listOfDownTime)
                            {
                                var dateStart = downTime.CheckGet(TaskPlaningDataSet.Dictionary.StartBeforeTime).ToDateTime();
                                var itemABeforeInsert = Items.FirstOrDefault(x => x.CheckGet(TaskPlaningDataSet.Dictionary.CalculatedTime).ToDateTime() > dateStart);

                                if (itemABeforeInsert != null)
                                {
                                    int indexBefore = itemABeforeInsert.CheckGet(TaskPlaningDataSet.Dictionary.RowNumber).ToInt();
                                    int indexDownTime = downTime.CheckGet(TaskPlaningDataSet.Dictionary.RowNumber).ToInt();

                                    if (Math.Abs(indexBefore - indexDownTime) > 1)
                                    {
                                        sorted = true;
                                        // необходимо переместить простой и пересчитать даты снова
                                        List<Dictionary<string, string>> downTimeItem = new List<Dictionary<string, string>>()
                                        {
                                            downTime
                                        };

                                        TaskPlanningKsh.PlanDataSet.MakeAction(TaskPlaningDataSet.UserAction.ActionType.Move, downTimeItem, (int)_CurrentMachineId, (int)_CurrentMachineId, indexBefore - 1);
                                        break;
                                    }
                                }
                            }

                            if(!sorted)
                            {
                                // простои все на месте
                                EnableSortIdles = false;
                            }
                        }
                    }


                    
                }

                // расчет времени блока
                Dictionary<string, TimeSpan> blockTimeLength = new Dictionary<string, TimeSpan>();

                foreach (var task in Items)
                {
                    string blockName = GetBlockName(task);

                    if (!blockTimeLength.ContainsKey(blockName))
                    {
                        blockTimeLength.Add(blockName, TimeSpan.Zero);
                    }

                    blockTimeLength[blockName] += TimeSpan.FromMinutes(task.CheckGet(TaskPlaningDataSet.Dictionary.CalculatedDuration).ToDouble());
                }

                string key = "В2400МБ90МБ80МБ80";
                if (blockTimeLength.ContainsKey(key))
                {
                    var time = blockTimeLength[key];
                }

                foreach (var task in Items)
                {
                    string blockName = GetBlockName(task);
                    task[TaskPlaningDataSet.Dictionary.BlockTimeLength] = blockTimeLength[blockName].ToString();
                }
            }
        }

        private void Update()
        {
            LoadItems();
        }


        /// <summary>
        /// обновление записей
        /// </summary>
        public async void LoadItems()
        {
            Console.WriteLine("start LoadItems " + _CurrentMachineId);

            var ds = await TaskPlanningKsh.PlanDataSet.GetDataAsync(_CurrentMachineId);

            if (ds != null)
            {
                // необходимо посчитать расчетную дату для заказов
                if (_CurrentMachineId != TaskPlaningDataSet.TypeStanok.Unknow)
                {
                    CalculateStartDates(ds.Items);
                }

                TaskGrid.UpdateItems(ds);

                CalculateErrors();

                //FilterItems();
                RefreshGridFlag = false;

                // при переносе заказов необходимо установить фокус на этот заказ, как правило текущий выбраный заказ самый верхний и 
                // и перезагрузка грида возвращает фокус на выделеный первый, а не в позицию куда заказ заброшен
                if(DropedNumberId > 0)
                {
                    var row = GridTable.AsEnumerable().Where(x => x.CheckGet(TaskPlaningDataSet.Dictionary.NumberId).ToInt() == DropedNumberId).FirstOrDefault();
                    if(row != null)
                    {
                        

                        int rowid = row.CheckGet(TaskPlaningDataSet.Dictionary.RowNumber).ToInt();
                        if(rowid>0)
                        {
                            TaskGrid.GridControl.View.FocusedRowHandle = rowid;
                            TaskGrid.GridControl.SelectItem(rowid);

                        }
                    }

                    DropedNumberId = -1;
                }
                else
                {
                    try
                    {
                        var items = GetSelectedItems();
                        if(items.Any())
                        {
                            int rowid = items.First().CheckGet(TaskPlaningDataSet.Dictionary.RowNumber).ToInt();
                            int rowHandle = rowid - 1;// TaskGrid.GridControl.GetRowHandleByListIndex(rowid);
                            TaskGrid.GridControl.View.FocusedRowHandle = rowHandle;
                            TaskGrid.GridControl.SelectItem(rowHandle);
                        }

                    }
                    catch 
                    { 
                    }
                }


                HideSplash();
            }

            Console.WriteLine("end LoadItems " + _CurrentMachineId);
        }

        public void MoveTasks(string direction)
        {
            if (!RefreshGridFlag)
            {
                var items = GetSelectedItems();

                if (items.Any())
                {
                    CurrentRowIndex = items.First().CheckGet(TaskPlaningDataSet.Dictionary.RowNumber).ToInt();

                    if (CanMoveTask(items, CurrentMachineId, CurrentMachineId))
                    {
                        RefreshGridFlag = true;

                        // на 1 вверх
                        if (direction == "up")
                        {
                            
                            TaskPlanningKsh.PlanDataSet.MakeAction(TaskPlaningDataSet.UserAction.ActionType.Selected, items);
                            TaskPlanningKsh.PlanDataSet.MakeAction(TaskPlaningDataSet.UserAction.ActionType.MoveUp, items);
                        }
                        else
                        // на 1 вниз
                        if (direction == "down")
                        {

                            TaskPlanningKsh.PlanDataSet.MakeAction(TaskPlaningDataSet.UserAction.ActionType.Selected, items);
                            TaskPlanningKsh.PlanDataSet.MakeAction(TaskPlaningDataSet.UserAction.ActionType.MoveDown, items);
                        }
                    }
                }
            }
        }

        private void MenuDelete()
        {
            if (_CurrentMachineId != TaskPlaningDataSet.TypeStanok.Unknow)
            {

                var items = GetSelectedItems();

                if (!items.Any())
                {
                    if (SelectedTaskItem != null)
                    {
                        items = new List<Dictionary<string, string>>()
                        {
                            SelectedTaskItem
                        };
                    }
                }

                if(items.Any())
                {
                    if (CanMoveTask(items, TaskPlaningDataSet.TypeStanok.Unknow, _CurrentMachineId))
                    {
                        TaskPlanningKsh.PlanDataSet.MakeAction(TaskPlaningDataSet.UserAction.ActionType.MoveInsertDown, items, (int)_CurrentMachineId, (int)TaskPlaningDataSet.TypeStanok.Unknow);
                    }
                }
            }
        }

        private void MenuMoveTasks(string direction)
        {
            if (SelectedTaskItem != null)
            {
                List<Dictionary<string, string>> items = new List<Dictionary<string, string>>()
                        {
                            SelectedTaskItem
                        };

                if (direction == "up")
                {
                    TaskPlanningKsh.PlanDataSet.MakeAction(TaskPlaningDataSet.UserAction.ActionType.MoveUp, items);
                }
                else
                {
                    TaskPlanningKsh.PlanDataSet.MakeAction(TaskPlaningDataSet.UserAction.ActionType.MoveDown, items);
                }
            }
        }

        /// <summary>
        /// Сохранение очереди заданий на станке
        /// </summary>
        public Task<LPackClientQuery> SaveQueue()
        {
            Task<LPackClientQuery> result = null;

            if (CurrentMachineId != 0)
            //if (_CurrentMachineId == TaskPlaningDataSet.TypeStanok.Gofra3)
            {
                if (GridTable != null)
                {
                    bool resume = true;
                    if (GridTable.Rows.Count == 0)
                    {
                        var d = new DialogWindow(
                            "Вы уверены, что хотите сохранить пустую очередь на гофроагрегате?",
                            "Пустая очередь заданий!",
                            "Если вы действительно хотите сохранить пустую очередь на гофроагрегате, просто нажмите \"Да\".",
                            DialogWindowButtons.YesNo);
                        d.ShowDialog();
                        if (d.DialogResult == false)
                        {
                            resume = false;
                        }
                    }

                    if (resume)
                    {
                        var virtualList = new List<Dictionary<string, string>>();
                        foreach (DataRow taskItem in GridTable.Rows)
                        {
                            var taskInfoForAdding = new Dictionary<string, string>();

                            taskInfoForAdding.CheckAdd("PRODUCTION_TASK_ID", taskItem.CheckGet("PRODUCTION_TASK_ID"));
                            taskInfoForAdding.CheckAdd(TaskPlaningDataSet.Dictionary.StartBeforeTime, taskItem.CheckGet(TaskPlaningDataSet.Dictionary.StartBeforeTime));
                            taskInfoForAdding.CheckAdd(TaskPlaningDataSet.Dictionary.Duration, taskItem.CheckGet(TaskPlaningDataSet.Dictionary.Duration));
                            taskInfoForAdding.CheckAdd(TaskPlaningDataSet.Dictionary.CalculatedDuration, taskItem.CheckGet(TaskPlaningDataSet.Dictionary.CalculatedDuration));
                            taskInfoForAdding.CheckAdd(TaskPlaningDataSet.Dictionary.StartPlanedTime, taskItem.CheckGet(TaskPlaningDataSet.Dictionary.StartPlanedTime));
                            taskInfoForAdding.CheckAdd(TaskPlaningDataSet.Dictionary.CalculatedTime, taskItem.CheckGet(TaskPlaningDataSet.Dictionary.CalculatedTime));
                            taskInfoForAdding.CheckAdd("IS_ROLL_1", taskItem.CheckGet(TaskPlaningDataSet.Dictionary.MF1).Trim(' ').IsNullOrEmpty() ? "0" : "1");
                            taskInfoForAdding.CheckAdd("IS_ROLL_2", taskItem.CheckGet(TaskPlaningDataSet.Dictionary.MF2).Trim(' ').IsNullOrEmpty() ? "0" : "1");
                            taskInfoForAdding.CheckAdd(TaskPlaningDataSet.Dictionary.DropdownId, taskItem.CheckGet(TaskPlaningDataSet.Dictionary.DropdownId));

                            virtualList.Add(taskInfoForAdding);
                        }

                        var json = JsonConvert.SerializeObject(virtualList);

                        var p = new Dictionary<string, string>();
                        {
                            p.CheckAdd(TaskPlaningDataSet.Dictionary.StanokId, ((int)_CurrentMachineId).ToString());
                            p.CheckAdd("KPD", Panel.Kpd);
                            p.CheckAdd("VIRTUAL", json);
                        }


                        result = LPackClientQuery.DoQueryAsync("Production", "TaskPlanningKashira", "Save", "ITEMS", p, SaveTimeout);
                    }
                }
            }

            return result;
        }

        public void SetActivate(Action<TaskPlaningDataSet.TypeStanok, Dictionary<string,string>> func)
        {
            OnSelectCheckbox += func;
        }

        public void SetActive(bool active)
        {
            if (active)
            {
                BorderControl.BorderBrush = HColor.Blue.ToBrush();
            }
            else
            {
                // сброс настроек для "деактивированного" гофраагрегата
                LastSelectedItemFroMultiSelect = null;
                BorderControl.BorderBrush = null;
            }
        }

        public void SetSelectToLastRow()
        {
            //TaskGrid.SetSelectToLastRow();
        }

        private string CurrentProdTask { get; set; }
        private string CurrentProgressTask { get; set; }

        private double CurrentProgress { get; set; }

        public async void UpdateTask()
        {
            if (_CurrentMachineId != TaskPlaningDataSet.TypeStanok.Unknow)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID_ST", ((int)_CurrentMachineId).ToString());
                }

                var q = new LPackClientQuery();

                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "CorrugatorMachineOperator");
                q.Request.SetParam("Action", "GetTaskData");

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

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
                        var ds = ListDataSet.Create(result, "TASK");
                        if (ds != null)
                        {
                            if (ds.Items.Count > 0)
                            {
                                CurrentProdTask = ds.Items[0].CheckGet("CRT_ID_PZ");
                                CurrentProgressTask = ds.Items[0].CheckGet("CRT_LENGTH");
                                var TotalTaskLength = ds.Items[0].CheckGet("TSK_LENGTH");
                                var AvgSpeed = ds.Items[0].CheckGet("CRT_SPEED");

                                //var text = "Задание: " + CurrentProdTask.ToInt().ToString() + " выполнено " + CurrentProgressTask + " из " + ds.Items[0].CheckGet("TSK_LENGTH") + ". Средняя скорость " + ds.Items[0].CheckGet("CRT_SPEED");

                                int done = CurrentProgressTask.ToInt();
                                int totalLength = ds.Items[0].CheckGet("TSK_LENGTH").ToInt();

                                if (done != 0 && totalLength != 0)
                                {
                                    CurrentProgress = (double)done / (double)totalLength;
                                }
                                else
                                {
                                    CurrentProgress = 0;
                                }


                                {

                                    var item = TaskGrid.Items.Select(x => x.CheckGet("PRODUCTION_TASK_ID") == CurrentProdTask).FirstOrDefault();

                                    CalculateStartDates(TaskGrid.Items);
                                }

                                Panel.SetMachineData(CurrentProdTask, CurrentProgress, CurrentProgressTask, TotalTaskLength, AvgSpeed);
                                //Panel.Description = text;
                            }
                        }
                    }
                }


                string currentLength = string.Empty;

                if (TaskGrid.Items != null)
                {
                    int totalLenght = 0;
                    double duration = 0.0;
                    double lastDuration = 0.0;

                    TaskGrid.Items.ForEach(x =>
                    {
                        totalLenght += x.CheckGet("LENGTH").ToInt();

                        var d = x.ContainsKey(TaskPlaningDataSet.Dictionary.CalculatedDuration) ? x.CheckGet(TaskPlaningDataSet.Dictionary.CalculatedDuration).ToDouble() : 0.0;
                        if (d == 0.0) d = x.CheckGet(TaskPlaningDataSet.Dictionary.Duration).ToDouble();

                        duration += d;
                        lastDuration = d;
                    });

                    TimeSpan time = TimeSpan.FromMinutes(duration);

                    DateTime lastDateTime = DateTime.Now.AddMinutes(duration);

                    if(CurrentMachineId != TaskPlaningDataSet.TypeStanok.Unknow)
                    {
                        TaskPlanning?.SetLastDataTime(CurrentMachineId, lastDateTime, lastDuration);
                    }

                    Panel.SetPlanData(totalLenght, time);
                }

            }
        }

        /// <summary>
        /// Поиск лучшей позиции для того что бы вставить данную позицию в грид
        /// </summary>
        /// <param name="position"></param>
        public void FindBestPosition(Dictionary<string, string> position)
        {

            //if(_CurrentMachineId != TaskPlaningDataSet.TypeStanok.Unknow)
            {
                if (TaskGrid.Items != null)
                {
                    var itemsDensityForSameProfilWidth = TaskGrid.Items
                                .Where(item =>

                                    TaskPlaningDataSet.IsEqualQuality(item, position)
                                    //item.CheckGet(TaskPlaningDataSet.Dictionary.ProfilName) == position.CheckGet(TaskPlaningDataSet.Dictionary.ProfilName) &&
                                    //item.CheckGet(TaskPlaningDataSet.Dictionary.Layer1) == position.CheckGet(TaskPlaningDataSet.Dictionary.Layer1) &&
                                    //item.CheckGet(TaskPlaningDataSet.Dictionary.Layer2) == position.CheckGet(TaskPlaningDataSet.Dictionary.Layer2) &&
                                    //item.CheckGet(TaskPlaningDataSet.Dictionary.Layer3) == position.CheckGet(TaskPlaningDataSet.Dictionary.Layer3) &&
                                    //item.CheckGet(TaskPlaningDataSet.Dictionary.Layer4) == position.CheckGet(TaskPlaningDataSet.Dictionary.Layer4) &&
                                    //item.CheckGet(TaskPlaningDataSet.Dictionary.Layer5) == position.CheckGet(TaskPlaningDataSet.Dictionary.Layer5)
                                    
                                    //// совпадает полномтью
                                    //&& item.CheckGet(TaskPlaningDataSet.Dictionary.Format).ToInt() == position.CheckGet(TaskPlaningDataSet.Dictionary.Format).ToInt()
                                    );

                    if (itemsDensityForSameProfilWidth.Any())
                    {
                        var item = itemsDensityForSameProfilWidth.LastOrDefault();
                        if (item != null)
                        {
                            // возможно есть более подходящее место?
                            var item2 = itemsDensityForSameProfilWidth.Where(item => item.CheckGet(TaskPlaningDataSet.Dictionary.Format).ToInt() == position.CheckGet(TaskPlaningDataSet.Dictionary.Format).ToInt()).LastOrDefault();

                            int focus = -1;

                            if (item2 != null)
                            {
                                TaskGrid.SelectedItem = item2;

                                focus = item2.CheckGet(TaskPlaningDataSet.Dictionary.RowNumber).ToInt();
                            }
                            else
                            {
                                TaskGrid.SelectedItem = item;

                                focus = item.CheckGet(TaskPlaningDataSet.Dictionary.RowNumber).ToInt();

                            }

                            if (focus >= 0)
                            {
                                focus = TaskGrid.Items.Where(item => item.CheckGet(TaskPlaningDataSet.Dictionary.RowNumber).ToInt() <= focus).Count();

                                TaskGrid.GridControl.View.FocusedRowHandle = focus - 1;
                                TaskGrid.GridControl.SelectItem(focus - 1);
                            }
                        }
                    }
                }
            }
        }




        public async void UpdateLineAsync(TaskPlaningDataSet.TypeStanok typeStanok, string key)
        {
            // означает что загрузились данные
            if (typeStanok == _CurrentMachineId && _CurrentMachineId!= TaskPlaningDataSet.TypeStanok.Unknow)
            {
                await Task.Delay(100);
                Console.WriteLine("Dispatcher.Invoke(new Action(() => { LoadItems(); }))" + _CurrentMachineId);
                Dispatcher.Invoke(new Action(() => { LoadItems(); }));
            }

        }

        public void CurrentItemSelect()
        {
            if (SelectedTaskItem != null)
            {
                var res = GridTable.AsEnumerable().FirstOrDefault(x => x.CheckGet(TaskPlaningDataSet.Dictionary.RowNumber).ToInt() == SelectedTaskItem.CheckGet(TaskPlaningDataSet.Dictionary.RowNumber).ToInt());

                if (res != null)
                {
                    res[TaskPlaningDataSet.Dictionary.SelectedColumn] = res[TaskPlaningDataSet.Dictionary.SelectedColumn].ToInt() == 0 ? true : false;
                }
            }
        }

        private void HideSplash()
        {
            //TaskGrid.IsEnabled = true;
            TaskGrid.HideSplash();
        }

        public void ShowSplash()
        {
            //TaskGrid.IsEnabled = false;
            TaskGrid.ShowSplash();
        }

        public void UpdateDownTime(int dopl_id, DateTime start, DateTime end)
        {
            // для простоя изменилось время, необходимо поправить его и поставить простой в нужное место
            if(SelectedTaskItem!=null)
            {
                int id_st = (int)_CurrentMachineId;
                var item = TaskGrid.Items.FirstOrDefault(x => x.CheckGet(TaskPlaningDataSet.Dictionary.StanokId).ToInt() == id_st &&
                                                                x.CheckGet(TaskPlaningDataSet.Dictionary.DropdownId).ToInt()==dopl_id);
                if(item != null)
                {
                    item[TaskPlaningDataSet.Dictionary.CalculatedTime] = start.ToString(TaskPlaningDataSet.DateTimeFormat);
                    item[TaskPlaningDataSet.Dictionary.StartBeforeTime] = start.ToString(TaskPlaningDataSet.DateTimeFormat);
                }
            }
        }


        public void MoveTasksInToCurrentPossition(IEnumerable<Dictionary<string, string>> Items, TaskPlaningDataSet.TypeStanok stanokId, TaskPlaningDataSet.TypeStanok sourceMachineId)
        {
            if (SelectedTaskItem != null)
            {
                //int DropedRowIndex = SelectedTaskItem.CheckGet(TaskPlaningDataSet.Dictionary.NumberId).ToInt() - 1;
                int RowNumber = SelectedTaskItem.CheckGet(TaskPlaningDataSet.Dictionary.RowNumber).ToInt();

                bool CanMove = true;

                // необходимо проверить куда переносятся задания
                if (SelectedTaskItem.CheckGet(TaskPlaningDataSet.Dictionary.OnMachine).ToInt()!=0)
                {
                    var item = GridTable.AsEnumerable().FirstOrDefault(x => x.CheckGet(TaskPlaningDataSet.Dictionary.RowNumber).ToInt() > RowNumber);
                    if(item!=null)
                    {
                        if(item.CheckGet(TaskPlaningDataSet.Dictionary.OnMachine).ToInt() != 0)
                        {
                            CanMove = false;
                        }
                    }

                }

                if (CanMove)
                {
                    if (CanMoveTask(Items, stanokId, sourceMachineId))
                    {
                        TaskPlanningKsh.PlanDataSet.MakeAction(TaskPlaningDataSet.UserAction.ActionType.Move, Items, (int)sourceMachineId, (int)CurrentMachineId, RowNumber);
                    }
                }
                else
                {
                    var d = new DialogWindow(
                            "Перенести задание в текущее место не возможно.",
                            "Перенос не возможен!",
                            "",
                            DialogWindowButtons.OK);
                    d.ShowDialog();
                    if (d.DialogResult == true)
                    {

                    }

                }
            }
        }
    }
}
