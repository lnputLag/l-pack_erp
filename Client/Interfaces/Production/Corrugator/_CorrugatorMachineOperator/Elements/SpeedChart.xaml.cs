using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static Client.Interfaces.Main.DataGridHelperColumn;
using Client.Common;
using System;

namespace Client.Interfaces.Production.Corrugator.CorrugatorMachineOperator
{
    /// <summary>
    /// Графики скорости работы ГА
    /// </summary>
    /// <author>vlasov_ea</author>   
    public partial class SpeedChart : UserControl
    {
        public SpeedChart()
        {
            InitializeComponent();
        }

        /// <summary>
        /// цвета графика
        /// </summary>
        private Dictionary<string, string> Colors { get; set; }

        /// <summary>
        /// Минимальный порог скорости, м/с
        /// </summary>
        private int SpeedLowLimit { get; set; }
        /// <summary>
        /// Максимальный порог скорости, м/с
        /// </summary>
        private int SpeedMaxLowLimit { get; set; }

        public DateTime MomentStart { get; set; }
        public DateTime MomentEnd { get; set; }

        public delegate void AfterLoad();
        public event AfterLoad OnAfterLoad;

        public void Init()
        {
            MomentStart = GetStartDate();
            MomentEnd = GetEndDate();
            SpeedLowLimit = 100;
            SpeedMaxLowLimit = 0;

            //цвета графика 
            Colors = new Dictionary<string, string>();
            //линия текущая скорость, нормальная скорость
            //зеленый
            Colors.CheckAdd("line_speed_normal", "#ff289744");
            //линия текущая скорость, низкая скорость
            //красный
            Colors.CheckAdd("line_speed_low", "#ffCD4838");
            //линия максимальная скорость, нормальная скорость
            //синий
            Colors.CheckAdd("line_speed_max_normal", "#ff0078D7");
            //линия максимальная скорость, низкая скорость
            //красный
            Colors.CheckAdd("line_speed_max_low", "#ffCD4838");

            InitSpeedGraph();
        }

        /// <summary>
        /// инициализация графика
        /// </summary>
        private void InitSpeedGraph()
        {
            {
                // ряды данных на графике
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Время",
                        Path="TIME",
                        ColumnType=ColumnTypeRef.String,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Время",
                        Path="_SECONDS",
                        ColumnType=ColumnTypeRef.Integer,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Скорость",
                        Path="SPEED_AVG",
                        ColumnType=ColumnTypeRef.Integer,
                        Stylers=new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                        {
                            {
                                DataGridHelperColumn.StylerTypeRef.ForegroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    // цвет интервала, координата Y конца интервала
                                    var intervals = new List<(Brush, double)>();

                                    double y = row.CheckGet("VALUE").ToDouble();
                                    double yPrevious = row.CheckGet("PREVIOUS_VALUE").ToDouble();

                                    // вся линия одного цвета
                                    if (
                                        yPrevious >= SpeedLowLimit
                                        && y >= SpeedLowLimit
                                    )
                                    {
                                        intervals.Add((Colors.CheckGet("line_speed_normal").ToBrush(), y));
                                    }
                                    else if (
                                        yPrevious < SpeedLowLimit
                                        && y < SpeedLowLimit
                                    )
                                    {
                                        intervals.Add((Colors.CheckGet("line_speed_low").ToBrush(), y));
                                    }
                                    // линия разделена на интервалы разного цвета
                                    else if (
                                        yPrevious < SpeedLowLimit
                                        && y >= SpeedLowLimit
                                    )
                                    {
                                        intervals.Add((Colors.CheckGet("line_speed_low").ToBrush(), SpeedLowLimit));
                                        intervals.Add((Colors.CheckGet("line_speed_normal").ToBrush(), y));
                                    }
                                    else if (
                                        yPrevious >= SpeedLowLimit
                                        && y < SpeedLowLimit
                                    )
                                    {
                                        intervals.Add((Colors.CheckGet("line_speed_normal").ToBrush(), SpeedLowLimit));
                                        intervals.Add((Colors.CheckGet("line_speed_low").ToBrush(), y));
                                    }

                                    if (intervals.Count > 0)
                                    {
                                        result = intervals;
                                    }

                                    return result;
                                }
                            },
                        },
                        Params=new Dictionary<string, string>()
                        {
                            //позиция подписи у точки: 1=сверху(50),2=снизу(18)
                            {"LabelYPosition","2"},
                            //шаг между подписями (подпись у каждой N-ой точки)
                            {"LabelStep","4"},
                            //диаметр точки, пикс
                            {"PointDiameter","5"},
                            //толщина линии, пикс
                            {"LineThickness","1.5"},
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Макс. скорость",
                        Path="SPEED_MAX",
                        ColumnType=ColumnTypeRef.Integer,
                        Stylers=new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                        {
                            {
                                DataGridHelperColumn.StylerTypeRef.ForegroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    // цвет интервала, координата Y конца интервала
                                    var intervals = new List<(Brush, double)>();

                                    double y = row.CheckGet("VALUE").ToDouble();
                                    double yPrevious = row.CheckGet("PREVIOUS_VALUE").ToDouble();

                                    // вся линия одного цвета
                                    if (
                                        yPrevious >= SpeedMaxLowLimit
                                        && y >= SpeedMaxLowLimit
                                    )
                                    {
                                        intervals.Add((Colors.CheckGet("line_speed_max_normal").ToBrush(), y));
                                    }
                                    else if (
                                        yPrevious < SpeedMaxLowLimit
                                        && y < SpeedMaxLowLimit
                                    )
                                    {
                                        intervals.Add((Colors.CheckGet("line_speed_max_low").ToBrush(), y));
                                    }
                                    // линия разделена на интервалы разного цвета
                                    else if (
                                        yPrevious < SpeedMaxLowLimit
                                        && y >= SpeedMaxLowLimit
                                    )
                                    {
                                        intervals.Add((Colors.CheckGet("line_speed_max_low").ToBrush(), SpeedMaxLowLimit));
                                        intervals.Add((Colors.CheckGet("line_speed_max_normal").ToBrush(), y));
                                    }
                                    else if (
                                        yPrevious >= SpeedMaxLowLimit
                                        && y < SpeedMaxLowLimit
                                    )
                                    {
                                        intervals.Add((Colors.CheckGet("line_speed_max_normal").ToBrush(), SpeedMaxLowLimit));
                                        intervals.Add((Colors.CheckGet("line_speed_max_low").ToBrush(), y));
                                    }

                                    if (intervals.Count > 0)
                                    {
                                        result = intervals;
                                    }

                                    return result;
                                }
                            },
                        },
                        Params=new Dictionary<string, string>()
                        {
                            //позиция подписи у точки: 1=сверху(50),2=снизу(18)
                            {"LabelYPosition","1"},
                            //шаг между подписями (подпись у каждой N-ой точки)
                            {"LabelStep","0"},
                            //диаметр точки, пикс
                            {"PointDiameter","7"},
                            //толщина линии, пикс
                            {"LineThickness","1.8"},
                            //ступенчатый график
                            {"IsStepChart","true"},
                        },
                    },
                };

                SpeedGraph.YAxis.Min = 0;
                SpeedGraph.YAxis.Max = 400;
                // ширина поля для надписей слева оси OY
                SpeedGraph.XOffset = 25;
                SpeedGraph.XAxis.Step = 120;
                SpeedGraph.XAxisLabelStep = 4;
                SpeedGraph.AutoUpdateInterval = 60;

                SpeedGraph.DebugMode = Central.DebugMode;
                SpeedGraph.SetColumns(columns);
                SpeedGraph.PrimaryKey = "_SECONDS";
                SpeedGraph.PrimaryLabel = "TIME";
                SpeedGraph.Init();

                SpeedGraph.AutoRender = true;
                SpeedGraph.ScrollToEnd = true;

                ////данные грида
                SpeedGraph.OnLoadItems = LoadData;
                SpeedGraph.OnCalculateXValues = (GraphBox graphBox) =>
                {
                    graphBox.XValues = GenerateXValues();
                };
                SpeedGraph.Run();
            }
        }

        /// <summary>
        /// Создание надписей на оси OX
        /// </summary>
        public Dictionary<double, string> GenerateXValues()
        {
            var result = new Dictionary<double, string>();
            var today = GetStartDate();
            var date = today;

            var step = 60;
            var limit = 60 * 24;

            for (int i = 0; i <= limit; i++)
            {
                var x = Math.Abs((today - date).TotalSeconds);
                var y = date.ToString("HH:mm");
                result.Add(x, y);
                date = date.AddSeconds(step);
            }

            return result;
        }

        /// <summary>
        /// получение записей
        /// </summary>
        public async void LoadData()
        {
            SpeedGraph.ShowSplash();

            MomentStart = GetStartDate();
            MomentEnd = GetEndDate();

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_ST", CorrugatorMachineOperator.SelectedMachineId.ToString());
                p.CheckAdd("FROM_DATE", MomentStart.ToString("dd.MM.yyyy HH:mm:ss"));
                p.CheckAdd("TO_DATE", MomentEnd.ToString("dd.MM.yyyy HH:mm:ss"));
            }

            /*var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "CorrugatorMachineOperator");
            q.Request.SetParam("Action", "ListSpeed");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestGridAttempts;

            await Task.Run(() =>
            {
                q.DoQuery();
            });*/
            try
            {
                var q = await LPackClientQuery.DoQueryAsync("Production", "CorrugatorMachineOperator", "ListSpeed", string.Empty, p);

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        SpeedGraph.ClearData();
                        {
                            var ds = ListDataSet.Create(result, "SPEED");
                            if (ds.Items.Count > 0)
                            {
                                var ds2 = ListDataSet.Create(result, "MAX_SPEED");
                                int indexSpeedMax = 0;
                                bool isLastTask = false;

                                for (int i = 0; i < ds.Items.Count; i++)
                                {
                                    var timeStr = ds.Items[i].CheckGet("DTTM");
                                    var time = timeStr.ToDateTime();

                                    var seconds = Math.Abs((MomentStart - time).TotalSeconds);
                                    ds.Items[i].CheckAdd("_SECONDS", seconds.ToString());

                                    var startTaskTimeStr = ds2.Items[indexSpeedMax].CheckGet("DTBEGIN");
                                    var startTaskTime = startTaskTimeStr.ToDateTime();

                                    if (i == 0)
                                    {
                                        while (startTaskTime < time)
                                        {
                                            indexSpeedMax++;
                                            startTaskTimeStr = ds2.Items[indexSpeedMax].CheckGet("DTBEGIN");
                                            startTaskTime = startTaskTimeStr.ToDateTime();
                                        }
                                        if (indexSpeedMax != 0)
                                        {
                                            indexSpeedMax--;
                                        }
                                        startTaskTimeStr = ds2.Items[indexSpeedMax].CheckGet("DTBEGIN");
                                        startTaskTime = startTaskTimeStr.ToDateTime();
                                    }

                                    if (i == ds.Items.Count - 1)
                                    {
                                        var speedMax = ds2.Items[indexSpeedMax].CheckGet("SPEEDL");
                                        ds.Items[i].CheckAdd("SPEED_MAX", speedMax);
                                    }
                                    else if (startTaskTime < time
                                        && !isLastTask)
                                    {
                                        var speedMax = ds2.Items[indexSpeedMax].CheckGet("SPEEDL");
                                        ds.Items[i].CheckAdd("SPEED_MAX", speedMax);
                                        if (indexSpeedMax < ds2.Items.Count - 1)
                                        {
                                            indexSpeedMax++;
                                        }
                                        else
                                        {
                                            isLastTask = true;
                                        }
                                    }
                                }
                            }
                            SpeedGraph.UpdateItems(ds);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                CorrugatorErrors.LogError(ex);
            }

            SpeedGraph.HideSplash();

            OnAfterLoad?.Invoke();
        }

        /// <summary>
        /// базовая дата, начало интервала
        /// </summary>
        /// <returns> dd.MM.yyyy 08:00:00 </returns>
        public DateTime GetStartDate()
        {
            var todayDateString = DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy HH:mm:ss");
            var todayDateTimeString = $"{todayDateString}";
            var today = todayDateTimeString.ToDateTime();
            return today;
        }

        /// <summary>
        /// базовая дата, конец интервала
        /// </summary>
        /// <returns> dd.MM.yyyy 08:00:00 </returns>
        public DateTime GetEndDate()
        {
            var today = GetStartDate();
            today = today.ToString("dd.MM.yyyy HH:mm:ss").ToDateTime().AddDays(1);
            var today2DateString = today.ToString("dd.MM.yyyy HH:mm:ss");
            var today2DateTimeString = $"{today2DateString}";
            today = today2DateTimeString.ToDateTime();
            return today;
        }


        public void LoadItems()
        {
            SpeedGraph.LoadItems();
        }
    }
}
