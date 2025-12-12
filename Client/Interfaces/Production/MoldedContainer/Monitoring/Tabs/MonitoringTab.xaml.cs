using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Stock;
using DevExpress.Utils.About;
using DevExpress.Utils.Filtering.Internal;
using DevExpress.Xpf.Core.DragDrop.Native;
using DevExpress.Xpf.Editors.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Policy;
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
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.MoldedContainer
{
    /// <summary>
    /// Мониторинг литой тары
    /// </summary>
    /// <autor>greshnyh_ni</autor>
    /// <changed>2025-09-30</changed>
    public partial class MonitoringTab : ControlBase
    {

        public FormHelper Form { get; set; }

        /// <summary>
        ////количество секунд до обновления информации
        /// </summary>
        private int CurSecund { get; set; }

        /// Таймер периодического обновления каждую  секунду
        /// </summary>
        private DispatcherTimer FastTimer { get; set; }

        /// <summary>
        ///  время обновления гридов (сек)
        /// </summary>
        private int RefreshTime { get; set; }

        /// <summary>
        /// Фактически произведенная готовая продукция за месяц 
        /// </summary>
        private double FinishedProductionFact { get; set; }

        /// <summary>
        /// Константа для месячной производительности готовой продукции
        /// берем из CONFIGURATION_OPTIONS.contner_plane_formed_month
        /// </summary>
        private double FinishedProductionPlanMonth { get; set; }

        /// <summary>
        /// Признак первого запуска мониторинга
        /// </summary>
        private bool FirstRun = false;

        public MonitoringTab()
        {
            ControlTitle = "Мониторинг ЛТ";
            DocumentationUrl = "/doc/l-pack-erp/";
            RoleName = "[erp]molded_contnr_monitoring";

            InitializeComponent();

            Form = null;

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

            //конструктор, будет вызван, когда объект создается
            //здесь создаются все внутренние структуры
            //впервые этот коллбэк будет вызван, когда данный таб станет активным
            //впервые (до этих пор, никакая работа внутри не происходит, что экономит ресурсы)
            OnLoad = () =>
            {
                InitForm();
                SetDefaults();

                BlanksProductionFormedGridInit();
                BlanksProductionGridInit();
                BlanksProductionIdlesGridInit();
                BlanksPerfomaceCurrentGridInit();

                FinishedProductionGridInit();
                FinishedProductionIdlesGridInit();
                FinishedPerfomaceCurrentGridInit();

                ProductGridInit();

                Init();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                BlanksProductionFormedGrid.Destruct();
                BlanksProductionGrid.Destruct();
                BlanksProductionIdlesGrid.Destruct();
                BlanksPerfomaceCurrentGrid.Destruct();

                FinishedProductionGrid.Destruct();
                FinishedProductionIdlesGrid.Destruct();
                FinishedPerfomaceCurrentGrid.Destruct();

                ProductGrid.Destruct();

                FastTimer?.Stop();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                BlanksProductionFormedGrid.ItemsAutoUpdate = true;
                BlanksProductionFormedGrid.Run();

                BlanksProductionGrid.ItemsAutoUpdate = true;
                BlanksProductionGrid.Run();

                BlanksProductionIdlesGrid.ItemsAutoUpdate = true;
                BlanksProductionIdlesGrid.Run();

                BlanksPerfomaceCurrentGrid.ItemsAutoUpdate = true;
                BlanksPerfomaceCurrentGrid.Run();

                FinishedProductionGrid.ItemsAutoUpdate = true;
                FinishedProductionGrid.Run();

                FinishedProductionIdlesGrid.ItemsAutoUpdate = true;
                FinishedProductionIdlesGrid.Run();

                FinishedPerfomaceCurrentGrid.ItemsAutoUpdate = true;
                FinishedPerfomaceCurrentGrid.Run();

                ProductGrid.ItemsAutoUpdate = true;
                ProductGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                BlanksProductionFormedGrid.ItemsAutoUpdate = false;
                BlanksProductionGrid.ItemsAutoUpdate = false;
                BlanksProductionIdlesGrid.ItemsAutoUpdate = false;
                BlanksPerfomaceCurrentGrid.ItemsAutoUpdate = false;

                FinishedProductionGrid.ItemsAutoUpdate = false;
                FinishedProductionIdlesGrid.ItemsAutoUpdate = false;
                FinishedPerfomaceCurrentGrid.ItemsAutoUpdate = false;

                ProductGrid.ItemsAutoUpdate = false;
            };

            {
                Commander.Add(new CommandItem()
                {
                    Name = "refresh",
                    Group = "main",
                    Enabled = true,
                    Title = "Обновить",
                    Description = "Обновить данные",
                    ButtonUse = true,
                    ButtonControl = RefreshButton,
                    ButtonName = "RefreshButton",
                    Action = () =>
                    {
                        Refresh();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "help",
                    Group = "main",
                    Enabled = true,
                    Title = "Справка",
                    Description = "Показать справочную информацию",
                    ButtonUse = true,
                    ButtonControl = HelpButton,
                    ButtonName = "HelpButton",
                    HotKey = "F1",
                    Action = () =>
                    {
                        Central.ShowHelp(DocumentationUrl);
                    },
                });
            }

            Commander.Init(this);
        }

        public void InitForm()
        {
            //инициализация формы, элементы тулбара
            {
                Form = new FormHelper();

                //после установки значений
                Form.AfterSet = (Dictionary<string, string> v) =>
                {
                    //фокус на кнопку обновления
                    RefreshButton.Focus();
                };
            }

        }

        /// <summary>
        /// Таймер частого обновления (1 секунда)
        /// </summary>
        public void SetFastTimer(int autoUpdateInterval)
        {
            FastTimer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, autoUpdateInterval)
            };

            FastTimer.Tick += (s, e) =>
            {
                RefreshButtonUpdate();
            };

            FastTimer.Start();
        }

        public void SetDefaults()
        {
            RefreshTime = 60;
        }

        /// <summary>
        /// Инициализация компонентов
        /// </summary>
        public void Init()
        {
            double nScale = 1.5;
            BlanksProductionFormedGrid.LayoutTransform = new ScaleTransform(nScale, nScale);
            BlanksProductionGrid.LayoutTransform = new ScaleTransform(nScale, nScale);
            BlanksProductionIdlesGrid.LayoutTransform = new ScaleTransform(nScale, nScale);
            BlanksPerfomaceCurrentGrid.LayoutTransform = new ScaleTransform(nScale, nScale);

            FinishedProductionGrid.LayoutTransform = new ScaleTransform(nScale, nScale);
            FinishedProductionIdlesGrid.LayoutTransform = new ScaleTransform(nScale, nScale);
            FinishedPerfomaceCurrentGrid.LayoutTransform = new ScaleTransform(nScale, nScale);

            ProductGrid.LayoutTransform = new ScaleTransform(nScale, nScale);

            CurSecund = 0;
            SetFastTimer(1);
                     
            var dataList = new List<Dictionary<string, string>>();
            var row = new Dictionary<string, string>();

            row.CheckAdd("PARAM_NAME", "FORECAST_FACT_COLOR");
            dataList.Add(row);
            GetDataConfig(dataList, 4);

            row = new Dictionary<string, string>();
            row.CheckAdd("PARAM_NAME", "FORECAST_FACT_QTY_WEEK");
            dataList.Add(row);
            GetDataConfig(dataList, 2);

            row = new Dictionary<string, string>();
            row.CheckAdd("PARAM_NAME", "FORECAST_FACT_QTY_MONTH");
            dataList.Add(row);
            GetDataConfig(dataList, 3);

        }


        /// <summary>
        /// обновляем время на кнопке до обновления информации
        /// </summary>
        private void RefreshButtonUpdate()
        {
            if (CurSecund >= RefreshTime)
            {
                Refresh();

            }
            CurSecund = CurSecund + 1;
            int secondsBeforeFirstUpdate = RefreshTime - CurSecund;
            RefreshButton.Content = $"Обновить {secondsBeforeFirstUpdate}";
        }

        public void Refresh()
        {
            CurSecund = 0;
            RefreshButton.IsEnabled = false;
            BlanksProductionGrid.LoadItems();
            BlanksProductionIdlesGrid.LoadItems();
            BlanksPerfomaceCurrentGrid.LoadItems();

            FinishedProductionGrid.LoadItems();
            FinishedProductionIdlesGrid.LoadItems();
            FinishedPerfomaceCurrentGrid.LoadItems();

            ProductGrid.LoadItems();

            FastTimer.Start();
            RefreshButton.IsEnabled = true;

            // данные по плановой производительности обновляем при первом запуске мониторинга и в начале каждой смены
            if (DateTime.Now.Hour == 8 || DateTime.Now.Hour == 20)
            {
                var m = DateTime.Now.ToString("mm").ToInt();
                if (m >= 1 || m <= 2)
                {
                    FirstRun = false;
                    var dataList = new List<Dictionary<string, string>>();
                    var row = new Dictionary<string, string>();

                    row.CheckAdd("PARAM_NAME", "FORECAST_FACT_COLOR");
                    dataList.Add(row);
                    GetDataConfig(dataList, 4);

                    row = new Dictionary<string, string>();
                    row.CheckAdd("PARAM_NAME", "FORECAST_FACT_QTY_WEEK");
                    dataList.Add(row);
                    GetDataConfig(dataList, 2);

                    row = new Dictionary<string, string>();
                    row.CheckAdd("PARAM_NAME", "FORECAST_FACT_QTY_MONTH");
                    dataList.Add(row);
                    GetDataConfig(dataList, 3);

                }
            }
        }

        /// <summary>
        /// обновляем данные
        /// </summary>
        public void LoadItems()
        {
            Refresh();
        }


        /// <summary>
        /// Загрузка данных для BlanksProductionGrid 
        /// </summary>
        public async void BlanksProductionGridLoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Monitoring");
            q.Request.SetParam("Action", "BlanksProductionList");

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
                    var ds = ListDataSet.Create(result, "FORMED");
                    BlanksProductionFormedGrid.UpdateItems(ds);

                    ds = ListDataSet.Create(result, "ITEMS");
                    BlanksProductionGrid.UpdateItems(ds);

                    ds = ListDataSet.Create(result, "IDLES");
                    BlanksProductionIdlesGrid.UpdateItems(ds);

                    ds = ListDataSet.Create(result, "PERFOMACE");
                    BlanksPerfomaceCurrentGrid.UpdateItems(ds);

                    ds = ListDataSet.Create(result, "QTY");
                    BlanksQty.Text = ds.Items[0].CheckGet("QTY").ToDouble().ToString("#,###,###,##0");
                }
            }
            else
            {
                q.ProcessError();
            }


        }

        /// <summary>
        /// инициализация грида BlanksProductionFormedGrid
        /// Сформовано ПФ
        /// </summary>
        public void BlanksProductionFormedGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header = "#",
                        Path = "_ROWNUMBER",
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 2,
                        Visible = false,

                    },
                    new DataGridHelperColumn
                    {
                        Header = "Станок",
                        Path = "NAME",
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 13,

                    },
                    new DataGridHelperColumn
                    {
                        Header = "За смену",
                        Path = "QTY_CUR",
                        ColumnType = ColumnTypeRef.Double,
                        Width2 = 9,
                        Format="N0",
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Предыдущую",
                        Path = "QTY_LAST",
                        ColumnType = ColumnTypeRef.Double,
                        Width2 = 12,
                        Format="N0",
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Неделю",
                        Path = "QTY_WEAK",
                        ColumnType = ColumnTypeRef.Double,
                        Width2 = 10,
                        Format="N0",
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Месяц",
                        Path = "QTY_MONTH",
                        ColumnType = ColumnTypeRef.Double,
                        Width2 = 12,
                        Format="N0",
                        TotalsType=TotalsTypeRef.Summ,
                    },
                };

                BlanksProductionFormedGrid.SetColumns(columns);
                BlanksProductionFormedGrid.SetPrimaryKey("_ROWNUMBER");
                BlanksProductionFormedGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                BlanksProductionFormedGrid.AutoUpdateInterval = 0;

                //данные грида
                BlanksProductionFormedGrid.OnLoadItems = BlanksProductionGridLoadItems;
                BlanksProductionFormedGrid.Commands = Commander;
                BlanksProductionFormedGrid.Init();

            }
        }


        /// <summary>
        /// инициализация грида BlanksProductionGrid
        /// </summary>
        public void BlanksProductionGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header = "#",
                        Path = "_ROWNUMBER",
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 2,
                        Visible = false,

                    },
                    new DataGridHelperColumn
                    {
                        Header = "Станок",
                        Path = "NAME",
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 13,

                    },
                    new DataGridHelperColumn
                    {
                        Header = "За смену",
                        Path = "QTY_CUR",
                        ColumnType = ColumnTypeRef.Double,
                        Width2 = 9,
                        Format="N0",
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Предыдущую",
                        Path = "QTY_LAST",
                        ColumnType = ColumnTypeRef.Double,
                        Width2 = 12,
                        Format="N0",
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Неделю",
                        Path = "QTY_WEAK",
                        ColumnType = ColumnTypeRef.Double,
                        Width2 = 10,
                        Format="N0",
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Месяц",
                        Path = "QTY_MONTH",
                        ColumnType = ColumnTypeRef.Double,
                        Width2 = 12,
                        Format="N0",
                        TotalsType=TotalsTypeRef.Summ,
                    },
                };

                BlanksProductionGrid.SetColumns(columns);
                BlanksProductionGrid.SetPrimaryKey("_ROWNUMBER");
                BlanksProductionGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                BlanksProductionGrid.AutoUpdateInterval = 0;

                //данные грида
                BlanksProductionGrid.OnLoadItems = BlanksProductionGridLoadItems;
                BlanksProductionGrid.Commands = Commander;
                BlanksProductionGrid.Init();

            }
        }

        /// <summary>
        /// инициализация грида BlanksProductionIdlesGrid
        /// </summary>
        public void BlanksProductionIdlesGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header = "#",
                        Path = "_ROWNUMBER",
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 2,
                        Visible = false,

                    },
                    new DataGridHelperColumn
                    {
                        Header = "Станок",
                        Path = "NAME",
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 13,

                    },
                    new DataGridHelperColumn
                    {
                        Header = "За смену",
                        Path = "TIME_CUR",
                        ColumnType = ColumnTypeRef.Double,
                        Width2 = 9,
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Предыдущую",
                        Path = "TIME_LAST",
                        ColumnType = ColumnTypeRef.Double,
                        Width2 = 12,
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Неделю",
                        Path = "TIME_WEAK",
                        ColumnType = ColumnTypeRef.Double,
                        Width2 = 10,
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Месяц",
                        Path = "TIME_MONTH",
                        ColumnType = ColumnTypeRef.Double,
                        Width2 = 12,
                        TotalsType=TotalsTypeRef.Summ,
                    },
                };

                BlanksProductionIdlesGrid.SetColumns(columns);
                BlanksProductionIdlesGrid.SetPrimaryKey("_ROWNUMBER");
                BlanksProductionIdlesGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                BlanksProductionIdlesGrid.AutoUpdateInterval = 0;

                //данные грида
                BlanksProductionIdlesGrid.OnLoadItems = BlanksProductionGridLoadItems;
                BlanksProductionIdlesGrid.Commands = Commander;
                BlanksProductionIdlesGrid.Init();

            }
        }

        /// <summary>
        /// инициализация грида BlanksPerfomaceCurrentGrid
        /// </summary>
        public void BlanksPerfomaceCurrentGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header = "#",
                        Path = "_ROWNUMBER",
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 2,
                        Visible = false,

                    },
                    new DataGridHelperColumn
                    {
                        Header = "Станок",
                        Path = "NAME",
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 10,

                    },
                    new DataGridHelperColumn
                    {
                        Header = "Счетчик",
                        Path = "COUNT",
                        ColumnType = ColumnTypeRef.Double,
                        Format="N0",
                        Width2 = 9,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "План (шт./мин.)",
                        Path = "PLAN_CNT",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2 = 12,
                        //TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Факт (шт./мин.)",
                        Path = "FACT_CNT",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2 = 12,
                        //TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Факт (шт./смену)",
                        Path = "FACT_CNT_CUR",
                        ColumnType = ColumnTypeRef.Double,
                        Width2 = 14,
                        Format="N0",
                        //TotalsType=TotalsTypeRef.Summ,
                    },
                };

                BlanksPerfomaceCurrentGrid.SetColumns(columns);
                BlanksPerfomaceCurrentGrid.SetPrimaryKey("_ROWNUMBER");
                BlanksPerfomaceCurrentGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                BlanksPerfomaceCurrentGrid.AutoUpdateInterval = 0;

                //данные грида
                BlanksPerfomaceCurrentGrid.OnLoadItems = BlanksProductionGridLoadItems;
                BlanksPerfomaceCurrentGrid.Commands = Commander;
                BlanksPerfomaceCurrentGrid.Init();

            }
        }


        /// <summary>
        /// Загрузка данных для FinishedProductionGridLoadItems 
        /// </summary>
        public async void FinishedProductionGridLoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Monitoring");
            q.Request.SetParam("Action", "FinishedProductionList");

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
                    // производительность за смену, пред., неделю, месяц ГП
                    var ds = ListDataSet.Create(result, "ITEMS");
                    FinishedProductionGrid.UpdateItems(ds);

                    //if (!FirstRun)
                    //{
                    //    FinishedProductionFact = 0;
                    //    // считаем фактически произведенную продукцию за месяц
                    //    foreach (var row in ds.Items)
                    //    {
                    //        FinishedProductionFact += row.CheckGet("QTY_MONTH").ToInt();
                    //    }

                    //    // вычисляем количестов дней в текущем месяце
                    //    int days = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);

                    //    // вычисляем количество дней с начала месяца
                    //    int countDays = DateTime.Now.Day - 1;

                    //    // вычисляем фактическую среднесуточную выработку
                    //    int avgDayProductionFact = 0;
                    //    if (countDays != 0)
                    //        avgDayProductionFact = (int)Math.Round(FinishedProductionFact / countDays, 0);

                    //    // вычисляем фактическую выработку за неделю
                    //    int weekProduction = avgDayProductionFact * 7;

                    //    // вычисляем фактическую выработку за месяц
                    //    int monthProduction = avgDayProductionFact * days;

                    //    // вычисляем плановую среднесуточную выработку
                    //    int avgDayProductionPlan = (int)(FinishedProductionPlanMonth / days);

                    //    ForecastFactQtyWeek.Text = $"{weekProduction.ToString("#,###,###,##0")}";
                    //    ForecastFactQtyMonth.Text = $"{monthProduction.ToString("#,###,###,##0")}";

                    //    if (avgDayProductionFact > avgDayProductionPlan)
                    //    {
                    //        ForecastFactQtyWeek.Foreground = Brushes.Green;
                    //        ForecastFactQtyMonth.Foreground = Brushes.Green;
                    //    }
                    //    else
                    //    {
                    //        ForecastFactQtyWeek.Foreground = Brushes.Red;
                    //        ForecastFactQtyMonth.Foreground = Brushes.Red;
                    //    }

                    //    FirstRun = true;
                    //}

                    // простои за смену, пред., неделю, месяц ГП
                    ds = ListDataSet.Create(result, "IDLES");
                    FinishedProductionIdlesGrid.UpdateItems(ds);

                    // текущая производительность ГП
                    ds = ListDataSet.Create(result, "PERFOMACE");
                    FinishedPerfomaceCurrentGrid.UpdateItems(ds);

                    // прогноз заказов ГП на неделю 
                    ds = ListDataSet.Create(result, "ORDER_FORECAST_MONTH");
                    OrderForecastWeek.Text = ds.Items[0].CheckGet("QTY").ToDouble().ToString("#,###,###,##0");

                    // прогноз заказов ГП на месяц 
                    ds = ListDataSet.Create(result, "ORDER_FORECAST_MONTH");
                    OrderForecastMonth.Text = ds.Items[1].CheckGet("QTY").ToDouble().ToString("#,###,###,##0");

                    // прогноз на месяц по отгрузке ГП
                    //ds = ListDataSet.Create(result, "SHIPPING_FORECAST");
                    //ShippingForecastMonth.Text = ds.Items[0].CheckGet("SHIPPING_QTY").ToDouble().ToString("#,###,###,##0");

                    // отгружено за месяц ГП
                    ds = ListDataSet.Create(result, "SHIPPED");
                    ShippedtMonth.Text = ds.Items[0].CheckGet("SHIPPED_QTY").ToDouble().ToString("#,###,###,##0");

                    // остатки на сладе ГП
                    ds = ListDataSet.Create(result, "QTY");
                    FinishedQty.Text = ds.Items[0].CheckGet("QTY").ToDouble().ToString("#,###,###,##0");

                }
            }
            else
            {
                q.ProcessError();
            }


        }

        /// <summary>
        /// инициализация грида FinishedProductionGridInit
        /// </summary>
        public void FinishedProductionGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header = "#",
                        Path = "_ROWNUMBER",
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 2,
                        Visible = false,

                    },
                    new DataGridHelperColumn
                    {
                        Header = "Станок",
                        Path = "NAME",
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 20,

                    },
                    new DataGridHelperColumn
                    {
                        Header = "За смену",
                        Path = "QTY_CUR",
                        ColumnType = ColumnTypeRef.Double,
                        Width2 = 7,
                        Format="N0",
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Предыдущую",
                        Path = "QTY_LAST",
                        ColumnType = ColumnTypeRef.Double,
                        Width2 = 8,
                        Format="N0",
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Неделю",
                        Path = "QTY_WEAK",
                        ColumnType = ColumnTypeRef.Double,
                        Width2 = 10,
                        Format="N0",
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Месяц",
                        Path = "QTY_MONTH",
                        ColumnType = ColumnTypeRef.Double,
                        Width2 = 12,
                        Format="N0",
                        TotalsType=TotalsTypeRef.Summ,
                    },
                };

                FinishedProductionGrid.SetColumns(columns);
                FinishedProductionGrid.SetPrimaryKey("_ROWNUMBER");
                FinishedProductionGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                FinishedProductionGrid.AutoUpdateInterval = 0;

                //данные грида
                FinishedProductionGrid.OnLoadItems = FinishedProductionGridLoadItems;
                FinishedProductionGrid.Commands = Commander;
                FinishedProductionGrid.Init();

            }
        }

        /// <summary>
        /// инициализация грида FinishedProductionGridInit
        /// </summary>
        public void FinishedProductionIdlesGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header = "#",
                        Path = "_ROWNUMBER",
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 2,
                        Visible = false,

                    },
                    new DataGridHelperColumn
                    {
                        Header = "Станок",
                        Path = "NAME",
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 20,

                    },
                    new DataGridHelperColumn
                    {
                        Header = "За смену",
                        Path = "TIME_CUR",
                        ColumnType = ColumnTypeRef.Double,
                        Width2 = 9,
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Предыдущую",
                        Path = "TIME_LAST",
                        ColumnType = ColumnTypeRef.Double,
                        Width2 = 12,
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Неделю",
                        Path = "TIME_WEAK",
                        ColumnType = ColumnTypeRef.Double,
                        Width2 = 8,
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Месяц",
                        Path = "TIME_MONTH",
                        ColumnType = ColumnTypeRef.Double,
                        Width2 = 8,
                        TotalsType=TotalsTypeRef.Summ,
                    },
                };

                FinishedProductionIdlesGrid.SetColumns(columns);
                FinishedProductionIdlesGrid.SetPrimaryKey("_ROWNUMBER");
                FinishedProductionIdlesGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                FinishedProductionIdlesGrid.AutoUpdateInterval = 0;

                //данные грида
                FinishedProductionIdlesGrid.OnLoadItems = FinishedProductionGridLoadItems;
                FinishedProductionIdlesGrid.Commands = Commander;
                FinishedProductionIdlesGrid.Init();

            }
        }

        /// <summary>
        /// текущая производительность ГП
        /// инициализация грида FinishedPerfomaceCurrentGrid
        /// </summary>
        public void FinishedPerfomaceCurrentGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header = "#",
                        Path = "_ROWNUMBER",
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 2,
                        Visible = false,

                    },
                    new DataGridHelperColumn
                    {
                        Header = "Станок",
                        Path = "NAME",
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 20,

                    },
                    new DataGridHelperColumn
                    {
                        Header = "Счетчик",
                        Path = "COUNT",
                        ColumnType = ColumnTypeRef.Double,
                        Width2 = 7,
                        Format="N0",
                    },
                    new DataGridHelperColumn
                    {
                        Header = "План (шт./мин.)",
                        Path = "PLAN_CNT",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2 = 8,
                        //TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Факт (шт./мин.)",
                        Path = "FACT_CNT",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2 = 9,
                        //TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Факт (шт./смену)",
                        Path = "FACT_CNT_CUR",
                        ColumnType = ColumnTypeRef.Double,
                        Width2 = 13,
                        Format="N0",
                        //TotalsType=TotalsTypeRef.Summ,
                    },
                };

                FinishedPerfomaceCurrentGrid.SetColumns(columns);
                FinishedPerfomaceCurrentGrid.SetPrimaryKey("_ROWNUMBER");
                FinishedPerfomaceCurrentGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                FinishedPerfomaceCurrentGrid.AutoUpdateInterval = 0;

                //данные грида
                FinishedPerfomaceCurrentGrid.OnLoadItems = FinishedProductionGridLoadItems;
                FinishedPerfomaceCurrentGrid.Commands = Commander;
                FinishedPerfomaceCurrentGrid.Init();

            }
        }

        /// <summary>
        /// остатки сырья (макулатуры) на складе
        /// </summary>
        public void ProductGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="ITEM_ID",
                        Description="Ид вида продукции",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 6,
                        TotalsType = TotalsTypeRef.Count,
                        Visible = false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="ITEM_NAME",
                        Description="Наименование вида продукции",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 24,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество, кип",
                        Description = "Количество единиц продукции на складе",
                        Path="ITEM_COUNT",
                        ColumnType=ColumnTypeRef.Double,
                        Width2 = 9,
                        Format="N0",
                       // TotalsType = TotalsTypeRef.Summ,
                        Totals = (List<Dictionary<string,string>> rows) =>
                        {
                            int balanceQty = 0;

                            if (rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                   if (row["ITEM_ID"].ToInt() != 147 )
                                   {
                                        balanceQty +=  row["ITEM_COUNT"].ToInt();
                                   }
                                }
                            }
                            return $"{balanceQty}";
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вес, кг",
                        Description = "Суммарный вес продукции на складе",
                        Path="ITEM_SUMMARY_WEIGHT",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2 = 10,
                    //    TotalsType = TotalsTypeRef.Summ,
                        Totals = (List<Dictionary<string,string>> rows) =>
                        {
                            int balanceWeight = 0;

                            if (rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                   if (row["ITEM_ID"].ToInt() != 147 )
                                   {
                                        balanceWeight +=  row["ITEM_SUMMARY_WEIGHT"].ToInt();
                                   }
                                }
                            }
                            return $"{balanceWeight}";
                        },

                    },
                    new DataGridHelperColumn
                    {
                        Header="Расход (нед.), кг",
                        Description = "Расход за неделю сырья",
                        Path="CONSUMPTION_WEIGHT",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2 = 11,
                        TotalsType = TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Приход (нед.), кг",
                        Description = "Приход за неделю",
                        Path="ARRIVAL_WEIGHT",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2 = 12,
                        TotalsType = TotalsTypeRef.Summ,
                    },

                };
                ProductGrid.SetColumns(columns);
                ProductGrid.SetPrimaryKey("ITEM_ID");

                //данные грида
                ProductGrid.OnLoadItems = ProductGridLoadItems;
                ProductGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                ProductGrid.AutoUpdateInterval = 0;

                ProductGrid.Commands = Commander;

                ProductGrid.Init();
            }
        }

        /// <summary>
        /// загружаем данные
        /// </summary>
        public async void ProductGridLoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "ListProductRemains");

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

                    double arrival_weight = 0.0;      // приход отходов
                    double consumption_weight = 0.0;  // расход сырья
                    double сontamination = 0.0;           // загрязненность сырья   

                    foreach (var row in ds.Items)
                    {
                        if (row.CheckGet("ITEM_ID").ToInt() == 147)
                        {
                            arrival_weight = row.CheckGet("ARRIVAL_WEIGHT").ToDouble();
                        }
                        if (row.CheckGet("ITEM_ID").ToInt() != 147)
                        {
                            consumption_weight += row.CheckGet("CONSUMPTION_WEIGHT").ToDouble();
                        }
                    }

                    if (consumption_weight != 0)
                    {
                        сontamination = Math.Round(arrival_weight / consumption_weight * 100, 2);
                    }

                    ProductGrid.UpdateItems(ds);

                    Сontamination.Text = $"{сontamination} %";
                }
            }
        }


        /// <summary>
        /// запрос на получение данных из CONFIGURATION_OPTIONS
        /// </summary>
        private void GetDataConfig(List<Dictionary<string, string>> list, int i)
        {
            FinishedProductionPlanMonth = 0;

            var listString = JsonConvert.SerializeObject(list);

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("DATA_LIST", listString);
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "PMFire");
            q.Request.SetParam("Action", "GetData");
            q.Request.SetParams(p);

            q.Request.Timeout = 5000;
            q.Request.Attempts = 1;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds.Items.Count > 0)
                    {
                        var DataStateList = ds.Items;
                        if (DataStateList.Count > 0)
                        {
                            var first = DataStateList.First();
                            if (first != null)
                            {
                               
                                if (i== 1)
                                {
                                    FinishedProductionPlanMonth = first.CheckGet("PARAM_VALUE").ToInt();
                                }

                                if (i == 2)
                                {
                                    var weekProduction = first.CheckGet("PARAM_VALUE").ToInt();
                                    ForecastFactQtyWeek.Text = $"{weekProduction.ToString("#,###,###,##0")}";
                                }
                                if (i == 3)
                                {
                                    var monthProduction = first.CheckGet("PARAM_VALUE").ToInt();
                                    ForecastFactQtyMonth.Text = $"{monthProduction.ToString("#,###,###,##0")}";

                                }
                                if (i == 4)
                                {
                                    var color = first.CheckGet("PARAM_VALUE").ToString();

                                    if (color == "Green")
                                    {
                                        ForecastFactQtyWeek.Foreground = Brushes.Green;
                                        ForecastFactQtyMonth.Foreground = Brushes.Green;
                                    }
                                    else
                                    if (color == "Red")
                                    {
                                        ForecastFactQtyWeek.Foreground = Brushes.Red;
                                        ForecastFactQtyMonth.Foreground = Brushes.Red;
                                    }
                                }
                            }
                        }
                    }
                }
            }

        }

        /// <summary>
        ///  вызываем показ вкладки Простои на литой таре
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IdlesButton_Click(object sender, RoutedEventArgs e)
        {
            //Central.Navigator.ProcessURL("/molded_container/idles_report");
            var recyclingMachineReportIdles = Central.WM.CheckAddTab<RecyclingMachineReportIdles>("RecyclingMachineReportIdles", "Простои ЛТ", true);
            Central.WM.SetActive("RecyclingMachineReportIdles");
        }
    }
}
