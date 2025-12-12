﻿using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Client.Interfaces.Production.Corrugator.TaskPlanningKashira;
using Newtonsoft.Json;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.Corrugator.CorrugatorMachineOperatorKsh
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

            ControlName = "CorrugatorMachinePlanKsh";
            ControlSection = "CorrugatorMachinePlanKsh";
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
            };

            OnLoad = () =>
            {
                FormInit();
                GridInit();
            };

            OnUnload = () =>
            {
                GridQueue.Destruct();
            };

            OnFocusGot = () =>
            {
                LoadStatusAutoplan();

                var List = new List<TaskPlaningDataSet.TypeStanok>()
                {
                    TaskPlaningDataSet.TypeStanok.Js,
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
            if (MachineId == TaskPlaningDataSet.TypeStanok.Js)
                return GridQueue;

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
                GetGrid(TaskPlaningDataSet.TypeStanok.Js).IsEnabled = !show;
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
                if (item.CheckGet("PARAM_NAME") == "LPACK_CORRUGATE_AUTOPLAN_7")
                {
                    var parsLine = ParsLine(item.CheckGet("PARAM_VALUE"));
                    if (parsLine.Count > 0)
                    {
                        ChkAutoplanJS.IsChecked = parsLine[0].ToInt() == 1;
                        ChkAutoplanJS.ToolTip = parsLine[2];
                        PlanTimeTextJS.Text = parsLine[1];
                    }
                    
                    continue;
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
            
            {
                {

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
            await ChangeStatusAutoPlan(23, 7, ChkAutoplanJS.IsChecked == true ? "1" : "0", PlanTimeTextJS.Text);

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

            SetNewTimeJS.Style = (Style)TryFindResource("FButtonPrimary");
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
            SaveNewAutoPlanTime(PlanTimeTextJS.Text.ToInt(), 7);
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

                SetNewTimeJS.Style = (Style)TryFindResource("Button");

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
