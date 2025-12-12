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
using DevExpress.Xpf.Core.Native;
using System.Security.Cryptography;
using Client.Interfaces.Production.Corrugator.CorrugatorMachineOperator;
using NPOI.SS.Formula.Functions;

namespace Client.Interfaces.Production.MoldedContainer
{
    /// <summary>
    /// Графики скорости работы ЛТ
    /// </summary>
    /// <author>Greshnyh_ni</author>   
    public partial class RecyclingSpeedChart : UserControl
    {
        public RecyclingSpeedChart()
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

        public delegate void AfterLoad();
        public event AfterLoad OnAfterLoad;

        public DateTime MomentStart { get; set; }
        public int MachineId { get; internal set; }

        public void Init()
        {
            SpeedLowLimit = 0;
            SpeedMaxLowLimit = 0;

            //цвета графика 
            Colors = new Dictionary<string, string>();

            //линия текущая скорость, нормальная скорость
            //зеленый
            Colors.CheckAdd("line_speed_normal", "#ff289744");

            //линия текущая скорость, низкая скорость
            //красный
            Colors.CheckAdd("line_speed_low", "#ffCD4838");

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
                        Path="SPEED_CURRENT",
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

                                    double y = row.CheckGet("VALUE").ToInt();

                                    // вся линия одного цвета
                                    if (y > SpeedLowLimit)
                                    {
                                        intervals.Add((Colors.CheckGet("line_speed_normal").ToBrush(), y));
                                    }
                                    else if (y == SpeedLowLimit)
                                    {
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
                            {"LabelStep","5"},
                            //диаметр точки, пикс
                            {"PointDiameter","5"},
                            //толщина линии, пикс
                            {"LineThickness","2.5"},   //   {"LineThickness","1.5"},
                        },
                    },
                };

                SpeedGraph.YAxis.Min = 0;
                //  SpeedGraph.YAxis.Max = 150;
                SpeedGraph.YAxis.Max = 200;

                // цвет надписей
                SpeedGraph.LabelColor = "#000000";

                // ширина поля для надписей слева оси OY
                SpeedGraph.XOffset = 40; //25;
                SpeedGraph.XAxis.Step = 60; //120;
                SpeedGraph.YAxis.Step = 20; //200

                SpeedGraph.XAxisLabelStep = 5;// 5;
                SpeedGraph.AutoUpdateInterval = 0;

                SpeedGraph.DebugMode = Central.DebugMode;
                SpeedGraph.SetColumns(columns);
                SpeedGraph.PrimaryKey = "_SECONDS";
                SpeedGraph.PrimaryLabel = "TIME";
                SpeedGraph.Init();

                SpeedGraph.AutoRender = false;
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
        /// базовая дата, начало интервала
        /// BASE-12 часов
        /// </summary>
        /// <returns></returns>

        private DateTime GetBaseDate()
        {
            // для тестов
            //  var todayDateString = "02.10.2024 08:00:00";

            var todayDateString = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            var today = todayDateString.ToDateTime().AddHours(-12);
            return today;
        }

        /// <summary>
        /// Создание надписей на оси OX
        /// </summary>
        public Dictionary<double, string> GenerateXValues()
        {

            var result = new Dictionary<double, string>();
            var today = MomentStart;
            var date = today;

            var step = 60;
            var limit = 60 * 12 + 5; //60 * 24;

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

            MomentStart = GetBaseDate();

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_ST", MachineId.ToString());
            }

            try
            {
                var q = await LPackClientQuery.DoQueryAsync("MoldedContainer", "Recycling", "RecyclingListSpeed", "SPEED", p);

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        SpeedGraph.ClearData();

                        var ds = ListDataSet.Create(result, "SPEED");
                        var maxSpeed = 0;
                        var dateOld = MomentStart;

                        if (ds.Items.Count > 0)
                        {
                            foreach (var row in ds.Items)
                            {
                                if (maxSpeed < row.CheckGet("SPEED_CURRENT").ToInt())
                                {
                                    maxSpeed = row.CheckGet("SPEED_CURRENT").ToInt();
                                }
                            }

                            var dateCurrent = "";
                            var datePrev = "";
                            var m = 0;
                            var items = new List<Dictionary<string, string>>(ds.Items);
                            ds.Items = new List<Dictionary<string, string>>();
                            var rowAppend = 0;

                            // проверяем был ли простой до времени MomentStart
                            dateCurrent = items[0].CheckGet("DTTM");
                            var dateDiffStart = (TimeSpan)(dateCurrent.ToDateTime() - MomentStart);
                            m = (int)dateDiffStart.TotalMinutes;
                            if (m > 1)
                            {
                                for (int i = 1; i <= m; i++)
                                {
                                    var d = MomentStart.AddMinutes(i);
                                    var row1 = new Dictionary<string, string>();
                                    {
                                        row1.CheckAdd("TIME", d.ToString("HH:mm"));
                                        row1.CheckAdd("DTTM", d.ToString("dd.MM.yyyy HH:mm:ss"));
                                        row1.CheckAdd("SPEED_CURRENT", "0");
                                    }
                                    ds.Items.Add(row1);
                                }
                            }

                            // проверяем данные по простоям из списка
                            foreach (var row in items)
                            {
                                datePrev = dateCurrent;
                                dateCurrent = row.CheckGet("DTTM");

                                if (
                                   !dateCurrent.IsNullOrEmpty()
                                   && !datePrev.IsNullOrEmpty()
                                )
                                {
                                    var dateDiff = (TimeSpan)(dateCurrent.ToDateTime() - datePrev.ToDateTime());
                                    m = (int)dateDiff.TotalMinutes;
                                    if (m > 1)
                                    {
                                        for (int i = 1; i < m; i++)
                                        {
                                            var d = datePrev.ToDateTime().AddMinutes(i);
                                            var row1 = new Dictionary<string, string>();
                                            {
                                                row1.CheckAdd("TIME", d.ToString("HH:mm"));
                                                row1.CheckAdd("DTTM", d.ToString("dd.MM.yyyy HH:mm:ss"));
                                                row1.CheckAdd("SPEED_CURRENT", "0");
                                            }
                                            ds.Items.Add(row1);
                                            rowAppend++;
                                        }
                                    }
                                    else
                                    {
                                        ds.Items.Add(row);
                                    }
                                }
                            }

                            // проверяем продолжается ли простой

                            if (!dateCurrent.IsNullOrEmpty())
                            {
                                var todayDateString = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                                var dateDiff2 = (TimeSpan)(todayDateString.ToDateTime() - dateCurrent.ToDateTime());
                                m = (int)dateDiff2.TotalMinutes;
                                if (m > 1)
                                {
                                    for (int i = 1; i < m; i++)
                                    {
                                        var d = dateCurrent.ToDateTime().AddMinutes(i);
                                        var row1 = new Dictionary<string, string>();
                                        {
                                            row1.CheckAdd("TIME", d.ToString("HH:mm"));
                                            row1.CheckAdd("DTTM", d.ToString("dd.MM.yyyy HH:mm:ss"));
                                            row1.CheckAdd("SPEED_CURRENT", "0");
                                        }
                                        ds.Items.Add(row1);
                                    }
                                }

                                var dateFirst = "";
                                foreach (var row in ds.Items)
                                {
                                    dateCurrent = row.CheckGet("DTTM");
                                    if (dateFirst.IsNullOrEmpty())
                                    {
                                        dateFirst = dateCurrent;
                                    }

                                    var dateDiff = (TimeSpan)(dateCurrent.ToDateTime() - dateFirst.ToDateTime());
                                    var s = (int)dateDiff.TotalSeconds;
                                    row.CheckAdd("_SECONDS", s.ToString());
                                }
                            }

                            SpeedGraph.YAxis.Max = maxSpeed + 20;
                            SpeedGraph.UpdateItems(ds);

                            SpeedGraph.Render();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CorrugatorErrors.LogError(ex);
            }

            SpeedGraph.HideSplash();

            OnAfterLoad?.Invoke();
        }


        public void LoadItems()
        {
            SpeedGraph.LoadItems();
        }
    }
}
