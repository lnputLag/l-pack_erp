using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Production.Corrugator;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Client.Interfaces.Production.Corrugator.TaskPlannings;
using Newtonsoft.Json;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.Corrugator.CorrugatorMachineOperator
{
    /// <summary>
    /// Интерфейс отображения планирования для гофроагрегатов
    /// <author>eletskikh_ya</author>
    /// </summary>
    public partial class CorrugatorMachinePlan : ControlBase
    {
        public CorrugatorMachinePlan()
        {
            InitializeComponent();

            ControlSection = "CorrugatorMachinePlan";
            RoleName = "[erp]corrugator_operator";
            ControlTitle = "План ГА";

            InitGrids = false;
            //DocumentationUrl = "/doc/l-pack-erp/service/agent/agents";

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }

                //if(!e.Handled)
                //{
                //    AccountGrid.ProcessKeyboard(e);
                //}
            };

            OnLoad = () =>
            {
                FormInit();
                GridInit();
            };

            OnUnload = () =>
            {
                GridQueue1.Destruct();
                GridQueue2.Destruct();
                GridQueue3.Destruct();
            };

            OnFocusGot = () =>
            {
                LoadStatusAutoplan();

                var List = new List<TaskPlaningDataSet.TypeStanok>()
                {
                    TaskPlaningDataSet.TypeStanok.Gofra3,
                    TaskPlaningDataSet.TypeStanok.Gofra5,
                    TaskPlaningDataSet.TypeStanok.Fosber
                };

                if (!InitGrids)
                {
                    InitGrids = true;

                    foreach (var gridType in List)
                    {
                        GridInit(gridType);
                    }



                    PlanDataSet = new TaskPlaningDataSet();
                    PlanDataSet.Load(true);

                    PlanDataSet.UpdateGrid += PlanDataSet_UpdateGrid;
                }

                foreach (var gridType  in List)
                {
                    var grid = GetGrid(gridType);
                    if(grid != null)
                    {
                        grid.ItemsAutoUpdate = true;
                        grid.Run();
                    }
                }
            };

            OnFocusLost = () =>
            {
                //AccountGrid.ItemsAutoUpdate = false;
            };

            OnNavigate = () =>
            {
                var login = Parameters.CheckGet("login");
                if (!login.IsNullOrEmpty())
                {
                    //AccountGridSearch.Text = login;
                }
            };
        }

        public TaskPlaningDataSet PlanDataSet { get; set; }
        public bool InitGrids { get; private set; }
        
        private List<DataGridHelperColumn> CreateColumns(TaskPlaningDataSet.TypeStanok _CurrentMachineId)
        {
            // размеры полей из пикселей в зщнакоместа
            int Width25 = 2;
            int Width65 = 8;
            int Width60 = 5;
            int Width9 = 9;
            int WidthLayer = 8;

            bool DefaultSortingOption = false;
            bool DefaultFilteringOption = false;
            bool FreeOrderFilteringEnable = false;

            return new List<DataGridHelperColumn>()
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
                        Width2=Width25,
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
                                    var result = TaskColors.GetColor(TaskPlaningDataSet.Dictionary.StartBeforeTime, row);
                                    if ((result as Brush) == null)
                                    {
                                        result = TaskColors.GetColor("ON_MACHINE", row);
                                    }
                                    return result;
                                }
                            },
                        },
                        DxEnableColumnFiltering = DefaultFilteringOption || FreeOrderFilteringEnable,
                        DxEnableColumnSorting = DefaultSortingOption,
                        //Visible = false,
                    },
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
                        Width2=7,
                        DxEnableColumnFiltering = DefaultFilteringOption || FreeOrderFilteringEnable,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },

                    new DataGridHelperColumn()
                    {
                        Header="Профиль",
                        Path=TaskPlaningDataSet.Dictionary.ProfilName,
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width2=3,
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

                                    return fontWeight;
                                }
                            },
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

                                    return fontWeight;
                                }
                            },
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

                                    return fontWeight;
                                }
                            },
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

                                    return fontWeight;
                                }
                            },
                        },
                        DxEnableColumnFiltering = DefaultFilteringOption || FreeOrderFilteringEnable,
                        DxEnableColumnSorting = DefaultSortingOption,
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
                    },
                    new DataGridHelperColumn
                    {
                        Header="Станок",
                        Path="MACHINE",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 6,
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
                        Path="FANFOLD",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        Width2=3,
                        DxEnableColumnFiltering = DefaultFilteringOption || FreeOrderFilteringEnable,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Тандем",
                        Path="TANDEM",
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
                        Visible = _CurrentMachineId == TaskPlaningDataSet.TypeStanok.Unknow,

                    },

                };
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
            };

            return result;
        }

        private void GridInit(TaskPlaningDataSet.TypeStanok MachineId)
        {
            GridBox4 TaskGrid = GetGrid(MachineId);

            //TaskGrid.OnFilterItems = FilterItems;
            TaskGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            TaskGrid.SetColumns(CreateColumns(MachineId));
            TaskGrid.SetPrimaryKey(TaskPlaningDataSet.Dictionary.RowNumber);
            TaskGrid.AutoUpdateInterval = 0;

            //данные грида
            TaskGrid.OnLoadItems = () =>
            {
                LoadItems(MachineId);
            };

            TaskGrid.UseProgressSplashAuto = false;
            TaskGrid.Init();
        }

        public GridBox4 GetGrid(TaskPlaningDataSet.TypeStanok MachineId)
        {
            if (MachineId == TaskPlaningDataSet.TypeStanok.Gofra3)
                return GridQueue1;

            if (MachineId == TaskPlaningDataSet.TypeStanok.Gofra5)
                return GridQueue2;

            if (MachineId == TaskPlaningDataSet.TypeStanok.Fosber)
                return GridQueue3;

            return null;
        }

        public void ShowSplash(bool show, TaskPlaningDataSet.TypeStanok MachineId= TaskPlaningDataSet.TypeStanok.Unknow)
        {
            ShowButton.IsEnabled = !show;


            if (show)
            {
                Console.WriteLine(MachineId.ToString());
            }

            if(MachineId== TaskPlaningDataSet.TypeStanok.Unknow)
            {
                GetGrid(TaskPlaningDataSet.TypeStanok.Gofra3).IsEnabled = !show;
                GetGrid(TaskPlaningDataSet.TypeStanok.Gofra5).IsEnabled = !show;
                GetGrid(TaskPlaningDataSet.TypeStanok.Fosber).IsEnabled = !show;
            }
            else
            {
                GetGrid(MachineId).IsEnabled = !show;
            }
        }

        /// <summary>
        /// обновление записей
        /// </summary>
        public async void LoadItems(TaskPlaningDataSet.TypeStanok MachineId)
        {
            if (MachineId != TaskPlaningDataSet.TypeStanok.Unknow)
            {
                GridBox4 taskGrid = GetGrid(MachineId);

                var ds = await PlanDataSet.GetDataAsync(MachineId);

                if (ds != null)
                {
                    CalculateStartDates(ds.Items, MachineId);
                    taskGrid.UpdateItems(ds);

                    
                }

                ShowSplash(false, MachineId);
            }
        }

        private async Task LoadStatusAutoplan()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "CorrugatorMachineOperator");
            q.Request.SetParam("Action", "ListKeyAutoplan");

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "KEYS");
                    if (ds.Items.Count > 0)
                    {
                        UpdateStatusAutoplan(ref ds);
                    }
                }
            }
        }

        private List<string> ParsLine(string line)
        {
            var list = new List<string>();
            list.AddRange(line.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries));
            return list;
        }

        private Task UpdateStatusAutoplan(ref ListDataSet ds)
        {
            foreach (var item in ds.Items)
            {
                if (item.CheckGet("PARAM_NAME") == "LPACK_CORRUGATE_AUTOPLAN_2")
                {
                    var parsLine = ParsLine(item.CheckGet("PARAM_VALUE"));
                    if (parsLine.Count > 0)
                    {
                        ChkAutoplanBhs1.IsChecked = parsLine[0].ToInt() == 1;
                        ChkAutoplanBhs1.ToolTip = parsLine[2];
                        PlanTimeTextBhs1.Text = parsLine[1];
                    }
                    
                    continue;
                }
                
                if (item.CheckGet("PARAM_NAME") == "LPACK_CORRUGATE_AUTOPLAN_4")
                {
                    var parsLine = ParsLine(item.CheckGet("PARAM_VALUE"));
                    if (parsLine.Count > 1)
                    {
                        ChkAutoplanBhs2.IsChecked = parsLine[0].ToInt() == 1;
                        ChkAutoplanBhs2.ToolTip = parsLine[2];
                        PlanTimeTextBhs2.Text = parsLine[1];
                    }
                    
                    continue;
                }
                
                if (item.CheckGet("PARAM_NAME") == "LPACK_CORRUGATE_AUTOPLAN_1")
                {
                    var parsLine = ParsLine(item.CheckGet("PARAM_VALUE"));
                    if (parsLine.Count > 2)
                    {
                        ChkAutoplanFosber.IsChecked = parsLine[0].ToInt() == 1;
                        ChkAutoplanFosber.ToolTip = parsLine[2];
                        PlanTimeTextFosber.Text = parsLine[1];
                    }
                }
            }
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Items"></param>
        public void CalculateStartDates(List<Dictionary<string, string>> Items, TaskPlaningDataSet.TypeStanok MachineId)
        {
            double kpd = 100;

            //CurrentProgress = 0;
            {
                {
                    //TaskPlaningDataSet.CalculateStartDates(Items, kpd, CurrentProdTask.ToInt(), CurrentProgress, (int)_CurrentMachineId);
                    // проверить есть ли простои которые нужно выставить в правильное место?

                    TaskPlaningDataSet.CalculateStartDates(Items, kpd, 0, 0, (int)MachineId);

                    var listOfDownTime = Items.Where(x => x.CheckGet(TaskPlaningDataSet.Dictionary.DropdownId).ToInt() > 0);
                    if (listOfDownTime.Any())
                    {
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
                                    // необходимо переместить простой и пересчитать даты снова
                                    List<Dictionary<string, string>> downTimeItem = new List<Dictionary<string, string>>()
                                    {
                                        downTime
                                    };

                                    PlanDataSet.MakeAction(TaskPlaningDataSet.UserAction.ActionType.Move, downTimeItem, (int)MachineId, (int)MachineId, indexBefore - 1);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void GridInit()
        {
            ShowSplash(true, TaskPlaningDataSet.TypeStanok.Unknow);

            GridQueue1.LayoutTransform = new ScaleTransform(1.3, 1.3);
            GridQueue2.LayoutTransform = new ScaleTransform(1.3, 1.3);
            GridQueue3.LayoutTransform = new ScaleTransform(1.3, 1.3);
        }

        private void PlanDataSet_UpdateGrid(TaskPlaningDataSet.TypeStanok type)
        {
            LoadItems(type);
        }

        private void FormInit()
        {
        }

        private void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            ShowSplash(true, TaskPlaningDataSet.TypeStanok.Unknow);

            PlanDataSet.Load(true);
        }
        
        private async void AutoPlan_Checked(object sender, RoutedEventArgs e)
        {
            var chk = sender as CheckBox;
            
            if (chk is { Name: "ChkAutoplanBhs1" })
            {
                await ChangeStatusAutoPlan(2, 2, ChkAutoplanBhs1.IsChecked == true ? "1" : "0", PlanTimeTextBhs1.Text);
            } else if (chk is { Name: "ChkAutoplanBhs2" })
            {
                await ChangeStatusAutoPlan(21, 4, ChkAutoplanBhs2.IsChecked == true ? "1" : "0", PlanTimeTextBhs2.Text);
            }
            else if (chk is { Name: "ChkAutoplanFosber" })
            {
                await ChangeStatusAutoPlan(22, 1, ChkAutoplanFosber.IsChecked == true ? "1" : "0", PlanTimeTextFosber.Text);
            }
            
            LoadStatusAutoplan();
        }

        private async Task ChangeStatusAutoPlan(int idSt, int code, string statusNew, string time)
        {
            var q = await LPackClientQuery.DoQueryAsync("Production", "TaskPlanning", "AutoplanSet", "",
                new Dictionary<string, string>()
                {
                    { "ID_ST", idSt.ToString() },
                    { "CODE", code.ToString() },
                    { "ENABLE", statusNew },
                    { "TIME", string.IsNullOrEmpty(time) ? "0" : time }
                });
        }
        
        /// <summary>
        /// Фильтр ввода
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlanTimeText_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var tb = sender as TextBox;
            var btnName = string.Empty;
            
            if (tb is { Name: "PlanTimeTextBhs1" })
            {
                btnName = "SetNewTimeBhs1";
            }
            else if (tb is { Name: "PlanTimeTextBhs2" })
            {
                btnName = "SetNewTimeBhs2";
            }
            else if (tb is { Name: "PlanTimeTextFosber" })
            {
                btnName = "SetNewTimeFosber";
            }

            if (FindName(btnName) is Button btn) btn.Style = (Style)TryFindResource("FButtonPrimary");
            var regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        
        /// <summary>
        /// Установка нового времени для автопланирования
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetNewTime_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            
            if (btn is { Name: "SetNewTimeBhs1" })
            {
                SaveNewAutoPlanTime(PlanTimeTextBhs1.Text.ToInt(), 2);
            }
            else if (btn is { Name: "SetNewTimeBhs2" })
            {
                SaveNewAutoPlanTime(PlanTimeTextBhs2.Text.ToInt(), 4);
            }
            else if (btn is { Name: "SetNewTimeFosber" })
            {
                SaveNewAutoPlanTime(PlanTimeTextFosber.Text.ToInt(), 1);
            }
        }
        
        /// <summary>
        /// Сохранение нового времени для станка
        /// </summary>
        /// <param name="time">Время которое ввели в TextBox Name="PlanTimeText"</param>
        private async void SaveNewAutoPlanTime(int time, int code)
        {
            try
            {
                var q = await LPackClientQuery.DoQueryAsync("Production", "TaskPlanning", "AutoplanSaveNewTime", "",
                    new Dictionary<string, string>()
                    {
                        { "CODE", code.ToString() },
                        { "TIME", time.ToString() }
                    });
                
                if (code == 2)
                {
                    SetNewTimeBhs1.Style = (Style)TryFindResource("Button");
                } else if (code == 4)
                {
                    SetNewTimeBhs2.Style = (Style)TryFindResource("Button");
                }
                else if (code == 1)
                {
                    SetNewTimeFosber.Style = (Style)TryFindResource("Button");
                }

                LoadStatusAutoplan();
            }
            catch (Exception e)
            {
                var d = new DialogWindow(
                    $"При изменении времени расчета произошла ошибка",
                    $"Ошибка",
                    "");

                d.ShowDialog();
            }
        }

    }
}
