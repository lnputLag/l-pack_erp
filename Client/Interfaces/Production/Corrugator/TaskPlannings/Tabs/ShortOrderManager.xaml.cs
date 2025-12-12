using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using DevExpress.Xpf.Grid;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.Corrugator.TaskPlannings
{
    /// <summary>
    /// Интерфейс показа коротких блоков
    /// <author>eletskikh_ya</author>
    /// </summary>
    public partial class ShortOrderManager : ControlBase
    {
        public ShortOrderManager()
        {
            InitializeComponent();

            ControlTitle = "Короткие блоки ПЗ на ГА";

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnLoad = () =>
            {
                //InitGrid();
                Init();
            };

            OnUnload = () =>
            {
                Destroy();
            };

            OnFocusGot = () =>
            {


                if (!_InitGrid)
                {
                    _InitGrid = true;
                    InitGrid();

                }
            };

            OnFocusLost = () =>
            {
                //AccountGrid.ItemsAutoUpdate = false;
            };

            //OnKeyPressed = (KeyEventArgs e) =>
            //{
            //    if (!e.Handled)
            //    {
            //        ProcessKeyboard(e);
            //    }

            //};
        }

        private bool _InitGrid = false;

        private string LayerFromRow(string o)
        {
            string res = " ";

            if (o != string.Empty)
            {
                res = o;
            }

            return res;
        }

        private string GetBlockName(Dictionary<string,string> item)
        {
            string blockName = item[TaskPlaningDataSet.Dictionary.ProfilName].ToString() +
                               LayerFromRow(item[TaskPlaningDataSet.Dictionary.Format]) +
                               LayerFromRow(item[TaskPlaningDataSet.Dictionary.Layer1]) +
                               LayerFromRow(item[TaskPlaningDataSet.Dictionary.Layer2]) +
                               LayerFromRow(item[TaskPlaningDataSet.Dictionary.Layer3]) +
                               LayerFromRow(item[TaskPlaningDataSet.Dictionary.Layer4]) +
                               LayerFromRow(item[TaskPlaningDataSet.Dictionary.Layer5]);

            return blockName;
        }

        private async void LoadGridData()
        {
            var PlanDataSet = TaskPlanning.PlanDataSet;

            var loading = await WaitLoadData(PlanDataSet);

            GridShortBlock.ShowSplash();

            if (loading)
            {
                // получим все данные из датасета и потом их отсортируем как нужно
                var alllist = await PlanDataSet.GetAllOrders();
                
                var list = alllist.Where(x=>x.CheckGet(TaskPlaningDataSet.Dictionary.DropdownId).ToInt()==0).OrderBy(x =>
                    x.CheckGet(TaskPlaningDataSet.Dictionary.ProfilName) +
                    x.CheckGet(TaskPlaningDataSet.Dictionary.Format) +
                    x.CheckGet(TaskPlaningDataSet.Dictionary.Layer1) +
                    x.CheckGet(TaskPlaningDataSet.Dictionary.Layer2) +
                    x.CheckGet(TaskPlaningDataSet.Dictionary.Layer3) +
                    x.CheckGet(TaskPlaningDataSet.Dictionary.Layer4) +
                    x.CheckGet(TaskPlaningDataSet.Dictionary.Layer5)).ToList();

                List <Dictionary<string, string>> copyOfList = new List<Dictionary<string, string>>();

                int rowNumber = 0;

                list.ForEach(x =>
                {
                    rowNumber++;

                    var newDictionary = x.ToDictionary(entry => entry.Key,
                                               entry => entry.Value);

                    var stanokId = (TaskPlaningDataSet.TypeStanok)newDictionary.CheckGet(TaskPlaningDataSet.Dictionary.StanokId).ToInt();

                    string stanokName = TaskPlaningDataSet.MachineName[stanokId];

                    if (stanokId == TaskPlaningDataSet.TypeStanok.Unknow)
                    {
                        Console.WriteLine("");
                    }

                    newDictionary[TaskPlaningDataSet.Dictionary.StanokId] = stanokName;

                    newDictionary[TaskPlaningDataSet.Dictionary.RowNumber] = rowNumber.ToString();

                    copyOfList.Add(newDictionary);
                });


                Dictionary<string, int> blockLength = new Dictionary<string, int>();
                var itemsCount = copyOfList.Count;

                for (int i = 0; i < itemsCount; i++)
                {
                    var item = copyOfList[i];
                    string blockName = GetBlockName(item);
                    var length = item[TaskPlaningDataSet.Dictionary.Length].ToInt();

                    if (blockLength.ContainsKey(blockName))
                    {
                        blockLength[blockName] += length;
                    }
                    else
                    {
                        blockLength.Add(blockName, length);
                    }
                }


                copyOfList.ForEach(x =>
                {
                    x[TaskPlaningDataSet.Dictionary.BlockLength] = blockLength[GetBlockName(x)].ToString();
                });


                /////////////////////////////////////////////////////////////
                ///

                var ds = new ListDataSet();

                var cols = new List<string>();
                var rows = new List<List<string>>();


                copyOfList.ForEach(x =>
                {
                    x.Keys.ForEach(y =>
                    {
                        if (!cols.Contains(y))
                        {
                            cols.Add(y);
                        }
                    });
                });


                foreach (Dictionary<string, string> row in copyOfList)
                {
                    var oneRow = new List<string>();

                    foreach (string col in cols)
                    {
                        if (row.ContainsKey(col))
                        {
                            oneRow.Add(row[col]);
                        }
                        else
                        {
                            oneRow.Add("");
                        }

                    }

                    rows.Add(oneRow);
                }

                ds.Cols = cols;
                ds.Rows = rows;
                ds.Init();


                //var ds = ListDataSet.Create(copyOfList);

                GridShortBlock.UpdateItems(ds);

            }

            GridShortBlock.HideSplash();

        }

        private void Destroy()
        {
           
        }

        private void Init()
        {
            
        }

        private void InitGrid()
        {

            int Width25 = 2;
            int Width65 = 8;
            int Width60 = 5;
            int Width9 = 9;
            int WidthLayer = 8;

            bool DefaultSortingOption = false;
            bool DefaultFilteringOption = true;

            //список колонок грида
            var Columns = new List<DataGridHelperColumn>()
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

                                row => TaskColors.GetColor("ON_MACHINE", row)
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
                        Visible = false,
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
                        DxEnableColumnFiltering = DefaultFilteringOption ,
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
                        Visible = false,
                        DxEnableColumnFiltering = DefaultFilteringOption,
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
                        DxEnableColumnFiltering = DefaultFilteringOption ,
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

                        Visible = true,
                        DxEnableColumnFiltering = DefaultFilteringOption,
                        DxEnableColumnSorting = DefaultSortingOption,
                        DxHeaderToolTip = "Сообщение об ошибке",


                        //Labels= CreateLabels("ERRORS")
                    },

                    new DataGridHelperColumn()
                    {
                        Header="ИД ПЗ",
                        Path="PRODUCTION_TASK_ID",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width2=7,
                        Visible = false,
                        DxEnableColumnFiltering = DefaultFilteringOption,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Номер ПЗ",
                        Path="PRODUCTION_TASK_NUMBER",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width2=7,
                        DxEnableColumnFiltering = DefaultFilteringOption,
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
                                row =>
                                {
                                    return TaskColors.GetColor(TaskPlaningDataSet.Dictionary.ProfilName, row);
                                }
                            },
                        },
                        DxEnableColumnFiltering = DefaultFilteringOption,
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
                        DxEnableColumnFiltering = DefaultFilteringOption,
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
                        DxEnableColumnFiltering = DefaultFilteringOption,
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
                        DxEnableColumnFiltering = DefaultFilteringOption,
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
                        DxEnableColumnFiltering = DefaultFilteringOption,
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
                        DxEnableColumnFiltering = DefaultFilteringOption,
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
                        DxEnableColumnFiltering = DefaultFilteringOption,
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
                        DxEnableColumnFiltering = DefaultFilteringOption,
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
                        DxEnableColumnFiltering = DefaultFilteringOption,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="FF",
                        Path="FANFOLD",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        Width2=3,
                        DxEnableColumnFiltering = DefaultFilteringOption,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Тандем",
                        Path="TANDEM",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        Width2=3,
                        DxEnableColumnFiltering = DefaultFilteringOption,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Обрезь, мм",
                        Path="TRIM",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width2=3,
                        DxEnableColumnFiltering = DefaultFilteringOption,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Склеивание",
                        Path="GLUED",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        Width2=3,
                        DxEnableColumnFiltering = DefaultFilteringOption,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Печать",
                        Path="COLOR",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        Width2=3,
                        DxEnableColumnFiltering = DefaultFilteringOption,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Присутствие представителя",
                        Path="REPRESENTATIVE_IS",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        Width2=3,
                        DxEnableColumnFiltering = DefaultFilteringOption,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="На горячую",
                        Path="HOT_IS",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        Width2=3,
                        DxEnableColumnFiltering = DefaultFilteringOption,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Нет клише",
                        Path=TaskPlaningDataSet.Dictionary.NonclicheIs,
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        Width2=3,
                        DxEnableColumnFiltering = DefaultFilteringOption,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Нет штанцформы",
                        Path=TaskPlaningDataSet.Dictionary.NonshtanzIs,
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        Width2=3,
                        DxEnableColumnFiltering = DefaultFilteringOption,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Артикул",
                        Path="ARTIKUL",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width2=Width60,
                        DxEnableColumnFiltering = DefaultFilteringOption,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="ИД ТС",
                        Path=TaskPlaningDataSet.Dictionary.TransportId,
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width2=Width60,
                        DxEnableColumnFiltering = DefaultFilteringOption,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Длина блока",
                        Path=TaskPlaningDataSet.Dictionary.BlockLength,
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width=5,
                        Visible = true, // _CurrentMachineId== TaskPlaningDataSet.TypeStanok.Unknow,
                        DxEnableColumnFiltering = DefaultFilteringOption,
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
                        Header="Гофроагрегат",
                        Path=TaskPlaningDataSet.Dictionary.StanokId,
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width2=10,
                        Visible = true,
                        DxEnableColumnFiltering = DefaultFilteringOption,
                        DxEnableColumnSorting = DefaultSortingOption,
                    },
                };


            //TaskGrid.OnFilterItems = FilterItems;
            GridShortBlock.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;


            GridShortBlock.SetColumns(Columns);


            GridShortBlock.SetPrimaryKey(TaskPlaningDataSet.Dictionary.RowNumber);


            GridShortBlock.AutoUpdateInterval = 0;

            //данные грида
            GridShortBlock.OnLoadItems = LoadGridData;

            GridShortBlock.Init();
            GridShortBlock.Run();
        }

        private async Task<bool> WaitLoadData(TaskPlaningDataSet dataset)
        {
            while (dataset.IsLoading)
            {
                Task.Delay(100);
            }

            return true;
        }

        private void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
