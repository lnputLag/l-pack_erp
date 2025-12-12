using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Отчет по работе складских зон
    /// </summary>
    public partial class ReportSpeed : ControlBase
    {
        public ReportSpeed()
        {
            InitializeComponent();
            ControlTitle = "Отчет по работе складских зон";

            OnMessage = (ItemMessage message) => {
                DebugLog($"message=[{message.Message}]");
            };

            OnLoad = () =>
            {
                ProcessPermissions();
                SetDefaults();
                InitSpeedGraph();

                Messenger.Default.Register<ItemMessage>(this, ProcessMessages);
            };

            OnUnload = () =>
            {
                Messenger.Default.Unregister<ItemMessage>(this);

                //ItemsGrid.Destruct();
            };

            OnFocusGot = () =>
            {
                //ItemsGrid.ItemsAutoUpdate = true;
            };

            OnFocusLost = () =>
            {
                //ItemsGrid.ItemsAutoUpdate = false;
            };
        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel("[erp]warehouse_report");
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
                        Path="DTTM",
                        ColumnType=ColumnTypeRef.String,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Операции",
                        Path="COUNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Stylers=new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                        {
                            {
                                DataGridHelperColumn.StylerTypeRef.ForegroundColor,
                                row =>
                                {
                             
                                    // цвет интервала, координата Y конца интервала
                                    var intervals = new List<(Brush, double)>();

                                    double y = row.CheckGet("VALUE").ToDouble();
                                    double y2 = row.CheckGet("PREVIOUS_VALUE").ToDouble();
             
                                    // вся линия одного цвета
                                    intervals.Add((HColor.Red.ToBrush(), y));
                                   

                                    return intervals;
                                }
                            },
                        },
                        Params=new Dictionary<string, string>()
                        {
                            //позиция подписи у точки: 1=сверху(50),2=снизу(18)
                            {"LabelYPosition","2"},
                            //шаг между подписями (подпись у каждой N-ой точки)
                            {"LabelStep","1"},
                            //диаметр точки, пикс
                            {"PointDiameter","5"},
                            //толщина линии, пикс
                            {"LineThickness","2"},
                        },
                    },
                };

                SpeedGraph.YAxis.Min = 0;
                SpeedGraph.YAxis.Max = 100;
                // ширина поля для надписей слева оси OY
                SpeedGraph.XOffset = 24;
                SpeedGraph.XAxis.Step = 1;
                SpeedGraph.XAxisLabelStep = 1;
                SpeedGraph.AutoUpdateInterval = 60;

                SpeedGraph.DebugMode = Central.DebugMode;
                SpeedGraph.SetColumns(columns);
                SpeedGraph.PrimaryKey = "DTTM";
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

                SpeedGraph.PointBrush = HColor.BlackFG.ToBrush();

                SpeedGraph.Run();
            }
        }

        private async void LoadData()
        {
            if (Zone.SelectedItem.Key != null)
            {
                var p = new Dictionary<string, string>();

                p.Add("START", FromDate.Text);
                p.Add("END", ToDate.Text);

                {
                    p.Add("WMZO_ID", Zone.SelectedItem.Key.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "Operation");
                q.Request.SetParam("Action", "OperationsByDate");
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
                        var startdate = GetStartDate();

                        int Max = 10;
                        

                        foreach (var item in ds.Items)
                        {
                            int count = item.CheckGet("COUNT").ToInt();
                            if (count > Max)
                            {
                                Max = count;
                            }

                            item["DTTM"] = (item.CheckGet("DTTM").ToDateTime() - startdate).TotalHours.ToString();
                        }

                        SpeedGraph.YAxis.Max = Max;

                        SpeedGraph.UpdateItems(ds);
                    }
                }
            }
        }

        /// <summary>
        /// Создание надписей на оси OX
        /// </summary>
        public Dictionary<double, string> GenerateXValues()
        {
            var result = new Dictionary<double, string>();
            var startdate = GetStartDate();
            var enddate = GetEndDate();

            var span = TimeSpan.FromSeconds(60*60);

            for(var time = startdate; time < enddate; time += span)
            {
                var hours = time.Hour;

                if (hours == 0)
                {
                    result.Add((time - startdate).TotalHours, time.ToString("dd.MM"));
                }
                else
                {
                    result.Add((time - startdate).TotalHours,hours.ToString());
                }
            }

            return result;
        }

        /// <summary>
        /// базовая дата, начало интервала
        /// </summary>
        /// <returns> dd.MM.yyyy 08:00:00 </returns>
        public DateTime GetStartDate()
        {
            var todayDateString = FromDate.Text;
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
            var today2DateString = ToDate.Text;
            var today2DateTimeString = $"{today2DateString}";
            var today = today2DateTimeString.ToDateTime();
            return today;
        }

        private void ProcessMessages(ItemMessage obj)
        {
        }

      
        private void SetDefaults()
        {
            FormHelper.ComboBoxInitHelper(Zone, "Warehouse", "Zone", "List", "WMZO_ID", "ZONE_FULL_NAME", null, true);

            FromDate.Text = DateTime.Now.AddDays(-1).ToString(FromDate.Mask);
            ToDate.Text = DateTime.Now.AddDays(1).ToString(ToDate.Mask);
        }

        private void StorageFilter_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LoadData();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
